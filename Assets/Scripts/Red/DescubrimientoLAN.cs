using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// La capa de "¿quién encuentra a quién?". Está separada A PROPÓSITO del resto de la red:
    /// cuando el juego esté en Steamworks, solo se escribe otra clase que implemente esta
    /// interfaz (con Steam Lobbies) y se cambia una línea en Global. La sincronización del
    /// juego (SalaRed, Personaje, Enemigos...) no se toca.
    /// </summary>
    public interface IDescubrimientoSalas : IDisposable
    {
        /// <summary>El host empieza a responder búsquedas de su sala.</summary>
        void IniciarAnuncio(string nombreSala);
        void DetenerAnuncio();
        /// <summary>Se llama cada frame (el host revisa si alguien está buscando).</summary>
        void Actualizar();
        /// <summary>El cliente busca una sala por nombre. Llama a alEncontrar(ip) si alguien responde.</summary>
        IEnumerator Buscar(string nombreSala, float tiempoMaximo, Action<string> alEncontrar);
    }

    /// <summary>
    /// Descubrimiento en red local por UDP broadcast — misma lógica que Global.gd:
    ///   Cliente -> broadcast al puerto 7778: "BUSCAR_SALA:&lt;nombre&gt;:&lt;puerto_respuesta&gt;"
    ///   Host con ese nombre -> responde directo: "SALA_ENCONTRADA:&lt;nombre&gt;"
    ///   Cliente usa la IP de quien respondió para conectarse por Netcode al puerto 7777.
    /// La contraseña NUNCA viaja en el broadcast; se valida en el host al conectarse.
    ///
    /// Mejoras respecto a Godot:
    ///  - Se manda a 255.255.255.255, al broadcast de CADA adaptador de red y a 127.0.0.1
    ///    (Windows con adaptadores virtuales —LDPlayer, VPN, Hyper-V— a veces manda el
    ///    broadcast general por el adaptador equivocado; y 127.0.0.1 permite probar 2
    ///    instancias en la misma PC).
    ///  - La búsqueda se reenvía cada 0.5 s por si se pierde un paquete (UDP no garantiza entrega).
    ///  - Un nombre de sala con ":" ya no rompe el mensaje.
    /// </summary>
    public class DescubrimientoLAN : IDescubrimientoSalas
    {
        const string PrefijoBuscar = "BUSCAR_SALA:";
        const string PrefijoRespuesta = "SALA_ENCONTRADA:";
        const float IntervaloReenvio = 0.5f;

        readonly int _puerto;
        UdpClient _escucha;
        string _nombreSala;

        public DescubrimientoLAN(int puertoDescubrimiento)
        {
            _puerto = puertoDescubrimiento;
        }

        // ------------------------------------------------------------------ HOST
        public void IniciarAnuncio(string nombreSala)
        {
            DetenerAnuncio();
            _nombreSala = nombreSala;
            try
            {
                _escucha = new UdpClient();
                _escucha.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _escucha.Client.Bind(new IPEndPoint(IPAddress.Any, _puerto));
                _escucha.EnableBroadcast = true;
                IgnorarErroresDeConexion(_escucha);
                Debug.Log($"[LAN] Anunciando la sala '{nombreSala}' en el puerto {_puerto}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LAN] No se pudo abrir el puerto de descubrimiento {_puerto}: {e.Message}");
                _escucha?.Close();
                _escucha = null;
            }
        }

        public void DetenerAnuncio()
        {
            _escucha?.Close();
            _escucha = null;
        }

        public void Actualizar()
        {
            if (_escucha == null) return;
            try
            {
                while (_escucha != null && _escucha.Available > 0)
                {
                    IPEndPoint remoto = null;
                    byte[] datos = _escucha.Receive(ref remoto);
                    ProcesarBusqueda(Encoding.UTF8.GetString(datos), remoto);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LAN] Error leyendo búsquedas: {e.Message}");
            }
        }

        void ProcesarBusqueda(string mensaje, IPEndPoint remoto)
        {
            if (!mensaje.StartsWith(PrefijoBuscar)) return;
            string resto = mensaje.Substring(PrefijoBuscar.Length);
            int separador = resto.LastIndexOf(':');
            if (separador < 0) return;

            string nombreBuscado = resto.Substring(0, separador);
            if (!int.TryParse(resto.Substring(separador + 1), out int puertoRespuesta)) return;
            if (nombreBuscado != _nombreSala) return;

            byte[] respuesta = Encoding.UTF8.GetBytes(PrefijoRespuesta + _nombreSala);
            _escucha.Send(respuesta, respuesta.Length, new IPEndPoint(remoto.Address, puertoRespuesta));
        }

        // --------------------------------------------------------------- CLIENTE
        public IEnumerator Buscar(string nombreSala, float tiempoMaximo, Action<string> alEncontrar)
        {
            UdpClient socket = CrearSocketBusqueda();
            if (socket == null) yield break;

            int miPuerto = ((IPEndPoint)socket.Client.LocalEndPoint).Port;
            byte[] paquete = Encoding.UTF8.GetBytes($"{PrefijoBuscar}{nombreSala}:{miPuerto}");
            string respuestaEsperada = PrefijoRespuesta + nombreSala;

            float tiempo = 0f;
            float siguienteEnvio = 0f;
            while (tiempo < tiempoMaximo)
            {
                if (tiempo >= siguienteEnvio)
                {
                    EnviarATodos(socket, paquete);
                    siguienteEnvio += IntervaloReenvio;
                }

                string ip = LeerRespuesta(socket, respuestaEsperada);
                if (ip != null)
                {
                    socket.Close();
                    Debug.Log($"[LAN] Sala '{nombreSala}' encontrada en {ip}.");
                    alEncontrar?.Invoke(ip);
                    yield break;
                }

                yield return null;
                tiempo += Time.unscaledDeltaTime;
            }
            socket.Close();
        }

        static UdpClient CrearSocketBusqueda()
        {
            try
            {
                var socket = new UdpClient(new IPEndPoint(IPAddress.Any, 0)); // el sistema nos da un puerto libre
                socket.EnableBroadcast = true;
                IgnorarErroresDeConexion(socket);
                return socket;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LAN] No se pudo crear el socket de búsqueda: {e.Message}");
                return null;
            }
        }

        void EnviarATodos(UdpClient socket, byte[] paquete)
        {
            foreach (IPAddress destino in DireccionesDestino())
            {
                try { socket.Send(paquete, paquete.Length, new IPEndPoint(destino, _puerto)); }
                catch (Exception) { /* un adaptador sin red no debe detener la búsqueda */ }
            }
        }

        static string LeerRespuesta(UdpClient socket, string respuestaEsperada)
        {
            try
            {
                while (socket.Available > 0)
                {
                    IPEndPoint remoto = null;
                    byte[] datos = socket.Receive(ref remoto);
                    if (Encoding.UTF8.GetString(datos) == respuestaEsperada)
                        return remoto.Address.ToString();
                }
            }
            catch (Exception) { }
            return null;
        }

        static List<IPAddress> DireccionesDestino()
        {
            var lista = new List<IPAddress> { IPAddress.Broadcast, IPAddress.Loopback };
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation info in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily != AddressFamily.InterNetwork || info.IPv4Mask == null) continue;
                        byte[] ip = info.Address.GetAddressBytes();
                        byte[] mascara = info.IPv4Mask.GetAddressBytes();
                        var broadcast = new byte[4];
                        for (int i = 0; i < 4; i++) broadcast[i] = (byte)(ip[i] | ~mascara[i]);
                        var dir = new IPAddress(broadcast);
                        if (!lista.Contains(dir)) lista.Add(dir);
                    }
                }
            }
            catch (Exception) { /* en algunas plataformas no se pueden listar adaptadores */ }
            return lista;
        }

        /// <summary>
        /// En Windows, si un paquete UDP llega a un puerto cerrado, el SIGUIENTE Receive lanza
        /// "ConnectionReset". Esto lo desactiva para que un cliente que ya se fue no rompa al host.
        /// </summary>
        static void IgnorarErroresDeConexion(UdpClient socket)
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor) return;
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                socket.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            DetenerAnuncio();
        }
    }
}
