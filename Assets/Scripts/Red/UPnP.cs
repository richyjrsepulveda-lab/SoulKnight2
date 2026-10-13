using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Utilidad UPnP para abrir automáticamente el puerto UDP 7777 en routers compatibles.
    /// Permite que amigos se conecten por Internet sin configuración manual en el router.
    /// </summary>
    public static class UPnP
    {
        public static string IPPublica { get; private set; } = "";
        public static string IPLocal { get; private set; } = "";
        public static bool PuertoMapeado { get; private set; } = false;

        private static string _controlURL = null;
        private static string _serviceType = null;
        private static int _puertoAbierto = 0;

        public static string ObtenerIPLocal()
        {
            if (!string.IsNullOrEmpty(IPLocal)) return IPLocal;
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        IPLocal = endPoint.Address.ToString();
                        return IPLocal;
                    }
                }
            }
            catch
            {
                IPLocal = "127.0.0.1";
            }
            return IPLocal;
        }

        public static async Task<bool> AbrirPuertoAsync(int puerto)
        {
            _puertoAbierto = puerto;
            ObtenerIPLocal();

            try
            {
                bool descubierto = await DescubrirRouterAsync();
                if (descubierto && !string.IsNullOrEmpty(_controlURL))
                {
                    bool ok = await EnviarMapeoAsync(puerto, "UDP", IPLocal, "SoulKnight2");
                    PuertoMapeado = ok;
                    if (ok)
                    {
                        Debug.Log($"[UPnP] Puerto {puerto} UDP abierto exitosamente en el router.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UPnP] No se pudo mapear automáticamente con UPnP: {ex.Message}");
            }

            return false;
        }

        public static async Task CerrarPuertoAsync()
        {
            if (!PuertoMapeado || string.IsNullOrEmpty(_controlURL) || _puertoAbierto == 0) return;

            try
            {
                string soap =
                    "<?xml version=\"1.0\"?>\r\n" +
                    "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n" +
                    "<s:Body>\r\n" +
                    $"<u:DeletePortMapping xmlns:u=\"{_serviceType}\">\r\n" +
                    "<NewRemoteHost></NewRemoteHost>\r\n" +
                    $"<NewExternalPort>{_puertoAbierto}</NewExternalPort>\r\n" +
                    "<NewProtocol>UDP</NewProtocol>\r\n" +
                    "</u:DeletePortMapping>\r\n" +
                    "</s:Body>\r\n" +
                    "</s:Envelope>";

                await EnviarSoapAsync(_controlURL, _serviceType + "#DeletePortMapping", soap);
                PuertoMapeado = false;
                Debug.Log($"[UPnP] Puerto {_puertoAbierto} liberado del router.");
            }
            catch
            {
                // Ignorar error al salir
            }
        }

        private static async Task<bool> DescubrirRouterAsync()
        {
            try
            {
                using (var udp = new UdpClient())
                {
                    udp.Client.ReceiveTimeout = 2500;
                    udp.EnableBroadcast = true;

                    string req =
                        "M-SEARCH * HTTP/1.1\r\n" +
                        "HOST: 239.255.255.250:1900\r\n" +
                        "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n" +
                        "MAN: \"ssdp:discover\"\r\n" +
                        "MX: 2\r\n\r\n";

                    byte[] reqBytes = Encoding.ASCII.GetBytes(req);
                    var endPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
                    await udp.SendAsync(reqBytes, reqBytes.Length, endPoint);

                    DateTime limite = DateTime.UtcNow.AddSeconds(2.5);
                    while (DateTime.UtcNow < limite)
                    {
                        if (udp.Available > 0)
                        {
                            var res = await udp.ReceiveAsync();
                            string text = Encoding.ASCII.GetString(res.Buffer);
                            string location = ExtraerValorCabecera(text, "LOCATION:");
                            if (!string.IsNullOrEmpty(location))
                            {
                                bool parseado = await ParsearDescripcionDispositivoAsync(location);
                                if (parseado) return true;
                            }
                        }
                        await Task.Delay(50);
                    }
                }
            }
            catch
            {
                // Router no responde a SSDP
            }

            return false;
        }

        private static async Task<bool> ParsearDescripcionDispositivoAsync(string urlLocation)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(urlLocation);
                req.Timeout = 3000;
                using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                using (var stream = resp.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string xml = await reader.ReadToEndAsync();

                    string[] services = { "urn:schemas-upnp-org:service:WANIPConnection:1", "urn:schemas-upnp-org:service:WANPPPConnection:1" };
                    foreach (var s in services)
                    {
                        int idx = xml.IndexOf(s, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            _serviceType = s;
                            int controlStart = xml.IndexOf("<controlURL>", idx, StringComparison.OrdinalIgnoreCase);
                            if (controlStart >= 0)
                            {
                                int controlEnd = xml.IndexOf("</controlURL>", controlStart, StringComparison.OrdinalIgnoreCase);
                                if (controlEnd > controlStart)
                                {
                                    string relUrl = xml.Substring(controlStart + 12, controlEnd - controlStart - 12).Trim();
                                    Uri baseUri = new Uri(urlLocation);
                                    _controlURL = new Uri(baseUri, relUrl).ToString();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static async Task<bool> EnviarMapeoAsync(int puerto, string protocolo, string ipLocal, string descripcion)
        {
            string soap =
                "<?xml version=\"1.0\"?>\r\n" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n" +
                "<s:Body>\r\n" +
                $"<u:AddPortMapping xmlns:u=\"{_serviceType}\">\r\n" +
                "<NewRemoteHost></NewRemoteHost>\r\n" +
                $"<NewExternalPort>{puerto}</NewExternalPort>\r\n" +
                $"<NewProtocol>{protocolo}</NewProtocol>\r\n" +
                $"<NewInternalPort>{puerto}</NewInternalPort>\r\n" +
                $"<NewInternalClient>{ipLocal}</NewInternalClient>\r\n" +
                "<NewEnabled>1</NewEnabled>\r\n" +
                $"<NewPortMappingDescription>{descripcion}</NewPortMappingDescription>\r\n" +
                "<NewLeaseDuration>0</NewLeaseDuration>\r\n" +
                "</u:AddPortMapping>\r\n" +
                "</s:Body>\r\n" +
                "</s:Envelope>";

            string res = await EnviarSoapAsync(_controlURL, _serviceType + "#AddPortMapping", soap);
            return res != null && !res.Contains("errorCode");
        }

        private static async Task<string> EnviarSoapAsync(string url, string soapAction, string soapBody)
        {
            try
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(soapBody);
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "text/xml; charset=\"utf-8\"";
                req.Headers.Add("SOAPACTION", $"\"{soapAction}\"");
                req.ContentLength = bodyBytes.Length;
                req.Timeout = 3500;

                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }

                using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                using (var respStream = resp.GetResponseStream())
                using (var reader = new StreamReader(respStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string ExtraerValorCabecera(string response, string cabecera)
        {
            int idx = response.IndexOf(cabecera, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int fin = response.IndexOf("\r\n", idx);
            if (fin < 0) fin = response.Length;
            return response.Substring(idx + cabecera.Length, fin - idx - cabecera.Length).Trim();
        }
    }
}
