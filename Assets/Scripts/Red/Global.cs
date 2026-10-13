using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight
{
    /// <summary>
    /// Equivalente al Autoload "Global" de Godot: un objeto que existe SIEMPRE (DontDestroyOnLoad)
    /// y centraliza: nombre del jugador guardado, crear sala, buscar/unirse a sala,
    /// contraseña (validada en el host) y salir de la partida.
    ///
    /// Se crea solo al arrancar el juego (desde Resources/Global.prefab), así que puedes darle
    /// Play a CUALQUIER escena en el editor y Global ya estará ahí, igual que un autoload.
    /// El mismo prefab trae el NetworkManager y el UnityTransport de Netcode.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class Global : MonoBehaviour
    {
        public const ushort Puerto = 7777;
        public const int PuertoDescubrimiento = 7778;
        public static int MaxJugadores => CatalogoHabilidades.MaxPersonajesJugables;
        const string ClaveNombreGuardado = "nombre_jugador"; // PlayerPrefs (era user://jugador.save)

        public static Global Instancia { get; private set; }

        /// <summary>Mensaje que el Menú Principal muestra al volver (ej. "El host cerró la sala").</summary>
        public static string MensajeParaMenu;

        [Header("Prefabs de red (los asigna SoulKnight > Reconstruir proyecto)")]
        [SerializeField] GameObject prefabSalaRed;
        [Tooltip("TODOS los prefabs con NetworkObject que se pueden spawnear en red.")]
        [SerializeField] List<GameObject> prefabsRed = new List<GameObject>();

        public string NombreLocal { get; private set; } = "";
        public string NombreSala { get; private set; } = "";

        public NetworkManager Red { get; private set; }
        public bool EsHost => Red != null && Red.IsHost;

        // Eventos para la UI (equivalen a las señales de Global.gd).
        public event Action<string> ConexionFallida;   // trae el motivo
        public event Action BusquedaIniciada;
        public event Action SalaNoEncontrada;

        string _contrasenaSala = "";
        IDescubrimientoSalas _descubrimiento;
        readonly Dictionary<ulong, string> _nombresPendientes = new Dictionary<ulong, string>();
        bool _saliendo;

        UnityTransport Transporte => (UnityTransport)Red.NetworkConfig.NetworkTransport;

        // ------------------------------------------------------------ ARRANQUE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CrearAlIniciar()
        {
            if (Instancia != null) return;
            var prefab = Resources.Load<GameObject>("Global");
            if (prefab == null)
            {
                Debug.LogError("Falta Resources/Global.prefab. Usa el menú SoulKnight > Reconstruir proyecto.");
                return;
            }
            var go = Instantiate(prefab);
            go.name = "Global";
        }

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            NombreLocal = PlayerPrefs.GetString(ClaveNombreGuardado, "");
            _descubrimiento = new DescubrimientoLAN(PuertoDescubrimiento);

            Red = GetComponent<NetworkManager>();
            foreach (GameObject prefab in prefabsRed)
            {
                if (prefab != null && !Red.NetworkConfig.Prefabs.Contains(prefab))
                    Red.AddNetworkPrefab(prefab);
            }

            // Conexiones de eventos POR CÓDIGO (lección de Godot: no depender del editor).
            Red.ConnectionApprovalCallback = AprobarConexion;
            Red.OnClientDisconnectCallback += AlDesconectarseAlguien;
        }

        void Update()
        {
            _descubrimiento?.Actualizar();
        }

        void OnDestroy()
        {
            if (Instancia != this) return;
            _descubrimiento?.Dispose();
            if (Red != null)
            {
                Red.ConnectionApprovalCallback = null;
                Red.OnClientDisconnectCallback -= AlDesconectarseAlguien;
            }
        }

        void OnApplicationQuit()
        {
            _descubrimiento?.Dispose();
        }

        // ------------------------------------------------ NOMBRE GUARDADO LOCALMENTE
        public bool HayNombreGuardado => !string.IsNullOrWhiteSpace(NombreLocal);

        public void GuardarNombre(string nombre)
        {
            NombreLocal = string.IsNullOrWhiteSpace(nombre) ? "Jugador" : nombre.Trim();
            PlayerPrefs.SetString(ClaveNombreGuardado, NombreLocal);
            PlayerPrefs.Save();
        }

        /// <summary>Solo para pruebas automáticas: usa un nombre sin guardarlo en disco.</summary>
        public void UsarNombreTemporal(string nombre) => NombreLocal = nombre;

        public string IPPublicaHost { get; private set; } = "";
        public string IPLocalHost { get; private set; } = "";
        public event Action IPActualizada;

        // ------------------------------------------------------------ CREAR SALA
        public void CrearSala(string nombreDeSala, string contrasena)
        {
            if (Red.IsListening || Red.ShutdownInProgress)
            {
                StartCoroutine(ReintentarTrasApagar(() => CrearSala(nombreDeSala, contrasena)));
                return;
            }

            NombreSala = string.IsNullOrWhiteSpace(nombreDeSala) ? "Sala de " + NombreLocal : nombreDeSala.Trim();
            _contrasenaSala = contrasena ?? "";
            _nombresPendientes.Clear();
            _saliendo = false;

            IPLocalHost = UPnP.ObtenerIPLocal();
            IPPublicaHost = "";
            StartCoroutine(ConsultarIPPublica());

            // Abrir puerto UDP automáticamente en router mediante UPnP
            _ = UPnP.AbrirPuertoAsync(Puerto);

            Transporte.SetConnectionData("127.0.0.1", Puerto, "0.0.0.0"); // escucha en todas las tarjetas de red
            Red.NetworkConfig.ConnectionData = CodificarSolicitud(NombreLocal, _contrasenaSala);

            if (!Red.StartHost())
            {
                ConexionFallida?.Invoke($"No se pudo crear la sala. ¿El puerto {Puerto} ya está en uso?");
                return;
            }

            // El "diccionario autoritativo" de jugadores vive en este objeto de red persistente.
            var sala = Instantiate(prefabSalaRed).GetComponent<SalaRed>();
            sala.NetworkObject.Spawn(destroyWithScene: false);
            sala.NombreSala.Value = NombreSala;
            sala.RegistrarJugador(Red.LocalClientId, NombreLocal);

            _descubrimiento.IniciarAnuncio(NombreSala);

            // Con la gestión de escenas de Netcode, los clientes que se unan después
            // llegarán automáticamente a la escena donde esté el host (el Lobby).
            Red.SceneManager.LoadScene(Escenas.Lobby, LoadSceneMode.Single);
        }

        private IEnumerator ConsultarIPPublica()
        {
            using (var req = UnityEngine.Networking.UnityWebRequest.Get("https://api.ipify.org"))
            {
                req.timeout = 4;
                yield return req.SendWebRequest();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    IPPublicaHost = req.downloadHandler.text.Trim();
                    IPActualizada?.Invoke();
                }
            }
        }

        // --------------------------------------------- BUSCAR Y UNIRSE POR INTERNET O LAN
        public void BuscarSala(string nombreOIp, string contrasena)
        {
            string texto = (nombreOIp ?? "").Trim();
            // Si el texto parece una IP o dirección (contiene puntos o dos puntos)
            if (texto.Contains(".") || texto.Contains(":") || texto.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                ConectarPorIP(texto, Puerto, contrasena);
            }
            else
            {
                StartCoroutine(BuscarYConectar(texto, contrasena ?? ""));
            }
        }

        public void ConectarPorIP(string direccionIP, ushort puertoDefecto, string contrasena)
        {
            StartCoroutine(ConectarDirectoIP(direccionIP.Trim(), puertoDefecto, contrasena ?? ""));
        }

        private IEnumerator ConectarDirectoIP(string direccionIP, ushort puertoDefecto, string contrasena)
        {
            if (Red.IsListening || Red.ShutdownInProgress)
            {
                Red.Shutdown();
                while (Red.ShutdownInProgress || Red.IsListening) yield return null;
            }

            BusquedaIniciada?.Invoke();

            string host = direccionIP;
            ushort puerto = puertoDefecto;

            if (direccionIP.Contains(":"))
            {
                string[] partes = direccionIP.Split(':');
                host = partes[0].Trim();
                if (partes.Length > 1 && ushort.TryParse(partes[1].Trim(), out ushort p))
                {
                    puerto = p;
                }
            }

            NombreSala = "Servidor " + host;
            _saliendo = false;
            Transporte.SetConnectionData(host, puerto);
            Red.NetworkConfig.ConnectionData = CodificarSolicitud(NombreLocal, contrasena);

            if (!Red.StartClient())
            {
                ConexionFallida?.Invoke($"No se pudo conectar a {host}:{puerto}. Revisa que el host tenga el juego abierto.");
            }
        }

        IEnumerator BuscarYConectar(string nombreABuscar, string contrasena)
        {
            if (Red.IsListening || Red.ShutdownInProgress)
            {
                Red.Shutdown();
                while (Red.ShutdownInProgress || Red.IsListening) yield return null;
            }

            BusquedaIniciada?.Invoke();

            string ipHost = null;
            yield return _descubrimiento.Buscar(nombreABuscar, 3f, ip => ipHost = ip);

            if (ipHost == null)
            {
                SalaNoEncontrada?.Invoke();
                yield break;
            }

            NombreSala = nombreABuscar;
            _saliendo = false;
            Transporte.SetConnectionData(ipHost, Puerto);
            Red.NetworkConfig.ConnectionData = CodificarSolicitud(NombreLocal, contrasena);

            if (!Red.StartClient())
                ConexionFallida?.Invoke("No se pudo iniciar la conexión con la sala.");
        }

        // ------------------------------- VALIDACIÓN EN EL HOST (contraseña, cupo...)
        void AprobarConexion(NetworkManager.ConnectionApprovalRequest solicitud,
                             NetworkManager.ConnectionApprovalResponse respuesta)
        {
            respuesta.CreatePlayerObject = false; // los personajes se crean al empezar la partida
            respuesta.Pending = false;

            if (solicitud.ClientNetworkId == NetworkManager.ServerClientId)
            {
                respuesta.Approved = true; // el propio host
                return;
            }

            DecodificarSolicitud(solicitud.Payload, out string nombre, out string contrasena);

            if (!string.IsNullOrEmpty(_contrasenaSala) && contrasena != _contrasenaSala)
                Rechazar(respuesta, "Se encontró la sala, pero la contraseña es incorrecta.");
            else if (Red.ConnectedClientsIds.Count >= MaxJugadores)
                Rechazar(respuesta, $"La sala está llena (máximo {MaxJugadores} jugadores).");
            else if (SalaRed.Instancia != null && SalaRed.Instancia.PartidaEnCurso.Value)
                Rechazar(respuesta, "La partida ya empezó. Espera a que vuelvan al Lobby.");
            else
            {
                _nombresPendientes[solicitud.ClientNetworkId] = string.IsNullOrWhiteSpace(nombre) ? "Jugador" : nombre;
                respuesta.Approved = true;
            }
        }

        static void Rechazar(NetworkManager.ConnectionApprovalResponse respuesta, string motivo)
        {
            respuesta.Approved = false;
            respuesta.Reason = motivo;
        }

        /// <summary>SalaRed lo usa al conectarse un cliente aprobado para saber cómo se llama.</summary>
        public string TomarNombrePendiente(ulong idCliente)
        {
            if (_nombresPendientes.TryGetValue(idCliente, out string nombre))
            {
                _nombresPendientes.Remove(idCliente);
                return nombre;
            }
            return idCliente == NetworkManager.ServerClientId ? NombreLocal : "Jugador " + idCliente;
        }

        static byte[] CodificarSolicitud(string nombre, string contrasena) =>
            Encoding.UTF8.GetBytes((nombre ?? "") + "\n" + (contrasena ?? ""));

        static void DecodificarSolicitud(byte[] datos, out string nombre, out string contrasena)
        {
            string texto = datos != null ? Encoding.UTF8.GetString(datos) : "";
            int corte = texto.IndexOf('\n');
            nombre = corte >= 0 ? texto.Substring(0, corte) : texto;
            contrasena = corte >= 0 ? texto.Substring(corte + 1) : "";
        }

        // ------------------------------------------------------- DESCONEXIONES
        void AlDesconectarseAlguien(ulong idCliente)
        {
            // En el host esto lo maneja SalaRed (quitar de la lista, avisar a los demás).
            if (Red.IsServer || _saliendo) return;

            // Soy cliente y me desconectaron (o el host cerró la sala).
            string motivo = Red.DisconnectReason;
            string escena = SceneManager.GetActiveScene().name;
            bool estabaDentro = escena == Escenas.Lobby || escena == Escenas.Juego;

            if (!estabaDentro)
            {
                // Rechazo al intentar unirse: nos quedamos en la pantalla de Unirse y mostramos el motivo.
                if (Red.IsListening) Red.Shutdown();
                ConexionFallida?.Invoke(string.IsNullOrEmpty(motivo)
                    ? "No se pudo conectar con la sala (revisa el firewall de Windows)."
                    : motivo);
            }
            else
            {
                SalirAlMenu(string.IsNullOrEmpty(motivo) ? "Se perdió la conexión con el host." : motivo);
            }
        }

        /// <summary>
        /// Salir de la sala/partida y volver al menú principal.
        /// Si soy el host, primero aviso a todos los clientes para que sepan por qué se cerró.
        /// </summary>
        public void SalirAlMenu(string mensaje = null)
        {
            if (_saliendo) return;
            _saliendo = true;
            MensajeParaMenu = mensaje;
            StartCoroutine(RutinaSalir());
        }

        IEnumerator RutinaSalir()
        {
            _descubrimiento.DetenerAnuncio();

            if (Red.IsServer && Red.IsListening)
            {
                foreach (ulong id in new List<ulong>(Red.ConnectedClientsIds))
                {
                    if (id != NetworkManager.ServerClientId)
                        Red.DisconnectClient(id, "El host cerró la sala.");
                }
                yield return new WaitForSecondsRealtime(0.25f); // da tiempo a que llegue el aviso
            }

            if (Red.IsListening) Red.Shutdown();
            while (Red.ShutdownInProgress) yield return null;

            Controles.Bloqueado = false;
            SceneManager.LoadScene(Escenas.MenuPrincipal);
            _saliendo = false;
        }

        IEnumerator ReintentarTrasApagar(Action accion)
        {
            Red.Shutdown();
            while (Red.ShutdownInProgress || Red.IsListening) yield return null;
            accion();
        }
    }
}
