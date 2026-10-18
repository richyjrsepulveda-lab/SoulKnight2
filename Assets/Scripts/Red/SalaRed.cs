using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight
{
    /// <summary>
    /// El estado compartido de la sala. Lo crea el host al crear la sala y sobrevive a los
    /// cambios de escena (Lobby -> Juego -> Lobby...).
    ///
    /// En Godot esto estaba repartido en Global.gd: el diccionario `jugadores`, los RPC
    /// `actualizar_lista`, `_rpc_elegir_personaje` y `cambiar_escena_global`. Aquí:
    ///  - `Jugadores` es un NetworkList: el host lo modifica y Netcode lo replica solo a todos.
    ///  - Elegir personaje = ServerRpc (el cliente PIDE, el host decide).
    ///  - Cambiar de escena = NetworkSceneManager (el host carga y todos lo siguen).
    /// </summary>
    public class SalaRed : NetworkBehaviour
    {
        public static SalaRed Instancia { get; private set; }

        /// <summary>Se dispara en TODAS las computadoras cuando cambia la lista (equivale a la señal lista_jugadores_actualizada).</summary>
        public static event Action ListaActualizada;
        /// <summary>Mensajes cortos para mostrar en pantalla ("X salió de la partida").</summary>
        public static event Action<string> AvisoRecibido;
        /// <summary>Fin de partida: true = victoria, false = cayó todo el equipo.</summary>
        public static event Action<bool> PartidaTerminada;

        public NetworkList<InfoJugador> Jugadores;
        public readonly NetworkVariable<FixedString64Bytes> NombreSala = new NetworkVariable<FixedString64Bytes>();
        public readonly NetworkVariable<bool> PartidaEnCurso = new NetworkVariable<bool>();
        public readonly NetworkVariable<int> SemillaMapaActual = new NetworkVariable<int>(0);
        public readonly NetworkVariable<int> BiomaMapaActual = new NetworkVariable<int>(0);
        public readonly NetworkVariable<bool> TieneJefeActual = new NetworkVariable<bool>(false);

        // Votación para volver al Lobby (Estilo LoL / Rendición)
        public readonly NetworkVariable<bool> VotacionLobbyActiva = new NetworkVariable<bool>(false);
        public readonly NetworkVariable<int> VotosSiLobby = new NetworkVariable<int>(0);
        public readonly NetworkVariable<int> VotosNoLobby = new NetworkVariable<int>(0);
        public readonly NetworkVariable<int> TotalVotantesLobby = new NetworkVariable<int>(0);
        public readonly NetworkVariable<float> TiempoVotacionRestante = new NetworkVariable<float>(0f);

        private readonly HashSet<ulong> _jugadoresQueYaVotaron = new HashSet<ulong>();
        private float _cooldownNuevaVotacion = 0f;

        void Awake()
        {
            // Netcode recomienda crear los NetworkList en Awake (no en la declaración).
            Jugadores = new NetworkList<InfoJugador>();
        }

        public override void OnNetworkSpawn()
        {
            Instancia = this;
            Jugadores.OnListChanged += AlCambiarLista;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += AlConectarseCliente;
                NetworkManager.OnClientDisconnectCallback += AlDesconectarseCliente;
                NetworkManager.SceneManager.OnLoadEventCompleted += AlTerminarCargaDeEscena;
            }
            ListaActualizada?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            Jugadores.OnListChanged -= AlCambiarLista;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AlConectarseCliente;
                NetworkManager.OnClientDisconnectCallback -= AlDesconectarseCliente;
                if (NetworkManager.SceneManager != null)
                    NetworkManager.SceneManager.OnLoadEventCompleted -= AlTerminarCargaDeEscena;
            }
            if (Instancia == this) Instancia = null;
            ListaActualizada?.Invoke();
        }

        public override void OnDestroy()
        {
            Jugadores?.Dispose();
            base.OnDestroy();
        }

        void AlCambiarLista(NetworkListEvent<InfoJugador> _) => ListaActualizada?.Invoke();

        // ---------------------------------------------------- CONSULTAS (todos)
        public bool Contiene(ulong idCliente) => IndiceDe(idCliente) >= 0;

        public int IndiceDe(ulong idCliente)
        {
            for (int i = 0; i < Jugadores.Count; i++)
                if (Jugadores[i].idCliente == idCliente) return i;
            return -1;
        }

        public string NombreDe(ulong idCliente)
        {
            int i = IndiceDe(idCliente);
            return i >= 0 ? Jugadores[i].nombre.ToString() : "Jugador";
        }

        public bool TodosEligieron()
        {
            if (Jugadores.Count == 0) return false;
            foreach (InfoJugador j in Jugadores)
                if (j.personaje == TipoPersonaje.Ninguno) return false;
            return true;
        }

        // -------------------------------------------------- REGISTRO (solo host)
        public void RegistrarJugador(ulong idCliente, string nombre)
        {
            if (!IsServer || Contiene(idCliente)) return;
            Jugadores.Add(new InfoJugador
            {
                idCliente = idCliente,
                nombre = new FixedString64Bytes(Recortar(nombre, 30)),
                personaje = TipoPersonaje.Ninguno
            });
        }

        void AlConectarseCliente(ulong idCliente)
        {
            if (idCliente == NetworkManager.ServerClientId) return; // el host se registra en Global.CrearSala
            string nombre = Global.Instancia.TomarNombrePendiente(idCliente);
            RegistrarJugador(idCliente, nombre);
            EnviarAvisoTodos($"{Recortar(nombre, 30)} se unió a la sala.");
        }

        void AlDesconectarseCliente(ulong idCliente)
        {
            int i = IndiceDe(idCliente);
            if (i < 0) return;
            string nombre = Jugadores[i].nombre.ToString();
            Jugadores.RemoveAt(i);
            EnviarAvisoTodos(PartidaEnCurso.Value ? $"{nombre} salió de la partida." : $"{nombre} salió de la sala.");
            if (PartidaEnCurso.Value && GestorPartida.Instancia != null)
                GestorPartida.Instancia.RevisarFinDePartida();
        }

        // ------------------------------------------ SELECCIÓN DE PERSONAJE (Lobby con Cuartos Exclusivos)
        public void ElegirPersonaje(TipoPersonaje personaje)
        {
            if (IsServer)
            {
                ulong miId = NetworkManager != null ? NetworkManager.LocalClientId : 0;
                int i = IndiceDe(miId);
                if (i < 0 && Jugadores.Count > 0) i = 0;
                if (i >= 0)
                {
                    if (personaje != TipoPersonaje.Ninguno)
                    {
                        foreach (var j in Jugadores)
                        {
                            if (j.idCliente != miId && j.personaje == personaje)
                            {
                                return; // Ocupado
                            }
                        }
                    }
                    InfoJugador info = Jugadores[i];
                    info.personaje = personaje;
                    Jugadores[i] = info;
                }
            }
            else
            {
                ElegirPersonajeServerRpc(personaje);
            }
        }

        public void LiberarPersonaje() => ElegirPersonaje(TipoPersonaje.Ninguno);

        [ServerRpc(RequireOwnership = false)]
        void ElegirPersonajeServerRpc(TipoPersonaje personaje, ServerRpcParams parametros = default)
        {
            if (PartidaEnCurso.Value) return;
            ulong senderId = parametros.Receive.SenderClientId;
            int i = IndiceDe(senderId);
            if (i < 0)
            {
                if (Jugadores.Count == 1) i = 0;
                else return;
            }

            // Si intenta elegir un personaje específico (no liberar)
            if (personaje != TipoPersonaje.Ninguno)
            {
                // Validación estricta en servidor: 1 cuarto = 1 jugador (no duplicados)
                foreach (var j in Jugadores)
                {
                    if (j.idCliente != senderId && j.personaje == personaje)
                    {
                        // Rechazar reclamación: ya está ocupado por otro jugador
                        EnviarAvisoCliente(senderId, $"El cuarto de {personaje} ya está ocupado por {j.nombre}.");
                        return;
                    }
                }
            }

            InfoJugador info = Jugadores[i];
            info.personaje = personaje;
            Jugadores[i] = info; // reasignar el elemento es lo que hace que se replique
        }

        public static bool MostrarFinPartidaPendiente = false;
        public static bool UltimoResultadoVictoria = false;

        // ------------------------------------------- INICIAR / TERMINAR (solo host)
        [ClientRpc]
        public void IniciarFadeNegroLobbyClientRpc()
        {
            if (ControladorLobby.Instancia != null)
            {
                ControladorLobby.Instancia.IniciarFadeNegroLobby();
            }
        }

        public void IniciarPartida()
        {
            if (!IsServer || PartidaEnCurso.Value) return;
            MostrarFinPartidaPendiente = false;
            PartidaEnCurso.Value = true;
            NetworkManager.SceneManager.LoadScene(Escenas.Juego, LoadSceneMode.Single);
        }

        void Update()
        {
            if (IsServer)
            {
                if (_cooldownNuevaVotacion > 0f)
                {
                    _cooldownNuevaVotacion -= Time.deltaTime;
                }

                if (VotacionLobbyActiva.Value)
                {
                    TiempoVotacionRestante.Value -= Time.deltaTime;
                    if (TiempoVotacionRestante.Value <= 0f)
                    {
                        ResolverVotacion(tiempoAgotado: true);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SolicitarVolverAlLobbyServerRpc(ServerRpcParams rpcParams = default)
        {
            // Llamado únicamente desde el Menú de Pausa a media run: inicia votación si hay >1 jugador
            ulong iniciador = rpcParams.Receive.SenderClientId;
            if (Jugadores.Count > 1)
            {
                IniciarVotacionVolverLobby(iniciador);
            }
            else
            {
                MostrarFinPartidaPendiente = false;
                VolverAlLobby();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SolicitarVolverAlLobbyFinPartidaServerRpc(ServerRpcParams rpcParams = default)
        {
            // Llamado al pulsar el botón de Volver en la pantalla de Fin de Partida (cuando mueren todos)
            if (!IsServer) return;
            MostrarFinPartidaPendiente = false;
            if (SceneManager.GetActiveScene().name != Escenas.Lobby)
            {
                CargarLobbyTrasFinPartida();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SolicitarAvanzarBucleServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (GestorPartida.Instancia != null)
            {
                GestorPartida.Instancia.AvanzarAlBucleInfinito();
            }
        }

        public void IniciarVotacionVolverLobby(ulong idIniciador = ulong.MaxValue)
        {
            if (!IsServer || VotacionLobbyActiva.Value) return;

            if (idIniciador == ulong.MaxValue && NetworkManager != null)
            {
                idIniciador = NetworkManager.LocalClientId;
            }

            if (_cooldownNuevaVotacion > 0f)
            {
                EnviarAvisoTodos($"Debes esperar {Mathf.CeilToInt(_cooldownNuevaVotacion)}s antes de iniciar otra votación.");
                return;
            }

            int total = Jugadores.Count;
            if (total <= 1)
            {
                MostrarFinPartidaPendiente = false;
                VolverAlLobby();
                return;
            }

            _jugadoresQueYaVotaron.Clear();
            _jugadoresQueYaVotaron.Add(idIniciador);

            TotalVotantesLobby.Value = total;
            VotosSiLobby.Value = 1; // El jugador que inicia la votación vota SÍ automáticamente
            VotosNoLobby.Value = 0;
            TiempoVotacionRestante.Value = 16f;
            VotacionLobbyActiva.Value = true;

            string nombreIniciador = NombreDe(idIniciador);
            EnviarAvisoTodos($"🗳️ {nombreIniciador} ha iniciado una votación para volver al Lobby.");
            AbrirPanelVotacionClientRpc(idIniciador);
        }

        [ClientRpc]
        private void AbrirPanelVotacionClientRpc(ulong idIniciador)
        {
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.MostrarPanelVotacion(idIniciador);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegistrarVotoLobbyServerRpc(bool votoSi, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || !VotacionLobbyActiva.Value) return;
            ulong idCliente = rpcParams.Receive.SenderClientId;

            if (_jugadoresQueYaVotaron.Contains(idCliente)) return;
            _jugadoresQueYaVotaron.Add(idCliente);

            if (votoSi) VotosSiLobby.Value++;
            else VotosNoLobby.Value++;

            ResolverVotacion(tiempoAgotado: false);
        }

        private void ResolverVotacion(bool tiempoAgotado)
        {
            if (!IsServer || !VotacionLobbyActiva.Value) return;

            int total = TotalVotantesLobby.Value;
            int si = VotosSiLobby.Value;
            int no = VotosNoLobby.Value;

            // En 2 jugadores: unanimidad (2/2 SÍ). Si alguien vota NO, se rechaza.
            // En 3+ jugadores: mayoría estricta (> 50%).
            int siRequeridos = (total <= 2) ? 2 : (total / 2 + 1);
            int noParaRechazar = total - siRequeridos + 1;

            if (si >= siRequeridos)
            {
                ConcluirVotacion(aprobada: true);
            }
            else if (no >= noParaRechazar || (_jugadoresQueYaVotaron.Count >= total) || tiempoAgotado)
            {
                ConcluirVotacion(aprobada: false);
            }
        }

        private void ConcluirVotacion(bool aprobada)
        {
            VotacionLobbyActiva.Value = false;
            _cooldownNuevaVotacion = 25f;

            if (aprobada)
            {
                MostrarFinPartidaPendiente = false;
                EnviarAvisoTodos("✅ ¡Votación APROBADA! Regresando al Lobby en 2 segundos...");
                NotificarFinVotacionClientRpc(true);
                Invoke(nameof(VolverAlLobby), 2f);
            }
            else
            {
                EnviarAvisoTodos("❌ Votación RECHAZADA. La partida continúa.");
                NotificarFinVotacionClientRpc(false);
            }
        }

        [ClientRpc]
        private void NotificarFinVotacionClientRpc(bool aprobada)
        {
            if (aprobada) MostrarFinPartidaPendiente = false;
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.CerrarPanelVotacion(aprobada);
            }
        }

        [ClientRpc]
        public void DesbloquearControlesClientRpc()
        {
            if (!MostrarFinPartidaPendiente)
            {
                Controles.Bloqueado = false;
            }
        }

        public void VolverAlLobby()
        {
            if (!IsServer) return;
            MostrarFinPartidaPendiente = false;
            Controles.Bloqueado = false;
            DesbloquearControlesClientRpc();
            PartidaEnCurso.Value = false;
            NetworkManager.SceneManager.LoadScene(Escenas.Lobby, LoadSceneMode.Single);
        }

        private void CargarLobbyTrasFinPartida()
        {
            if (!IsServer) return;
            PartidaEnCurso.Value = false;
            if (SceneManager.GetActiveScene().name != Escenas.Lobby)
            {
                NetworkManager.SceneManager.LoadScene(Escenas.Lobby, LoadSceneMode.Single);
            }
        }

        /// <summary>Cuando TODOS (host y clientes) terminaron de cargar el mapa, el host spawnea a los jugadores.</summary>
        void AlTerminarCargaDeEscena(string nombreEscena, LoadSceneMode modo,
                                     List<ulong> completaron, List<ulong> tardaronDemasiado)
        {
            if (!MostrarFinPartidaPendiente)
            {
                Controles.Bloqueado = false;
            }
            if (nombreEscena == Escenas.Juego && GestorPartida.Instancia != null)
            {
                MostrarFinPartidaPendiente = false;
                Controles.Bloqueado = false;
                GestorPartida.Instancia.IniciarEnServidor();
            }
        }

        // ------------------------------------------------------- MENSAJES A TODOS
        [ClientRpc]
        public void AvisoClientRpc(FixedString128Bytes mensaje)
        {
            AvisoRecibido?.Invoke(mensaje.ToString());
        }

        public void EnviarAvisoTodos(string mensaje) => AvisoClientRpc(new FixedString128Bytes(Recortar(mensaje, 120)));

        public void EnviarAvisoCliente(ulong idCliente, string mensaje)
        {
            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { idCliente } }
            };
            AvisoClienteClientRpc(new FixedString128Bytes(Recortar(mensaje, 120)), rpcParams);
        }

        [ClientRpc]
        private void AvisoClienteClientRpc(FixedString128Bytes mensaje, ClientRpcParams rpcParams = default)
        {
            AvisoRecibido?.Invoke(mensaje.ToString());
        }

        [ClientRpc]
        public void FinPartidaClientRpc(bool victoria)
        {
            MostrarFinPartidaPendiente = true;
            UltimoResultadoVictoria = victoria;

            // Otorgar puntos de maestría persistente al personaje usado en este run
            ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            int idx = IndiceDe(localId);
            if (idx >= 0)
            {
                TipoPersonaje miPj = Jugadores[idx].personaje;
                if (miPj != TipoPersonaje.Ninguno)
                {
                    int niveles = (GestorPartida.Instancia != null) ? GestorPartida.Instancia.NivelActual : 1;
                    int xpMaestria = GestorMaestria.CalcularXPFinPartida(10, niveles, victoria);
                    GestorMaestria.AgregarPuntos(miPj, xpMaestria);
                    Debug.Log($"[Maestría] Otorgados {xpMaestria} XP a {miPj}. Total: {GestorMaestria.ObtenerPuntos(miPj)}");
                }
            }

            PartidaTerminada?.Invoke(victoria);
            GestorAudio.PlayBGM(TipoMusica.Ninguna, 0.4f);
            GestorAudio.PlaySFX(victoria ? TipoSFX.Victoria : TipoSFX.MuerteJugador);
        }

        [ClientRpc]
        public void ActivarPasajeClientRpc()
        {
            if (PasajeSiguienteNivel.Instancia != null)
            {
                PasajeSiguienteNivel.Instancia.Abrir();
            }
        }

        [ClientRpc]
        public void IniciarTransicionNivelClientRpc()
        {
            Controles.Bloqueado = true;
            GestorAudio.PlaySFX(TipoSFX.PasarNivel);
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.IniciarFadeOscurecer(1.0f);
            }
        }

        [ClientRpc]
        public void FinalizarTransicionNivelClientRpc(int nivel, FixedString64Bytes textoBanner)
        {
            if (SistemaVision.Instancia != null)
            {
                SistemaVision.Instancia.ForzarActualizacionExploracion();
            }
            if (MinimapaHUD.Instancia != null)
            {
                MinimapaHUD.Instancia.RenderizarMinimapa();
            }
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.PrecargarMenuPausa();
                string banner = textoBanner.IsEmpty ? $"NIVEL {nivel}" : textoBanner.ToString();
                HUDJugador.Instancia.IniciarFadeAclarar(1.2f, banner, () =>
                {
                    Controles.Bloqueado = false;
                });
            }
            else
            {
                Controles.Bloqueado = false;
            }
        }

        [ClientRpc]
        public void SincronizarMapaClientRpc(int semilla, TipoBioma bioma, bool tieneJefe = true, bool esNivelDecisionLoop = false)
        {
            if (GeneradorMazmorra.Instancia != null)
            {
                GeneradorMazmorra.Instancia.GenerarConSemilla(semilla, bioma, tieneJefe, esNivelDecisionLoop);
            }
            GestorAudio.PlayBGM(TipoMusica.Mazmorra);
        }

        [ClientRpc]
        public void SincronizarEstadoSalaClientRpc(int indiceSala, bool descubierta, bool bloqueada, bool despejada)
        {
            if (GeneradorMazmorra.Instancia == null) return;
            if (indiceSala < 0 || indiceSala >= GeneradorMazmorra.Instancia.salasGeneradas.Count) return;

            var sala = GeneradorMazmorra.Instancia.salasGeneradas[indiceSala];
            if (sala == null) return;

            sala.EncenderAntorchas(descubierta);
            sala.BloquearSala(bloqueada);
            sala.despejada = despejada;

            if (bloqueada)
            {
                GestorAudio.PlaySFX(TipoSFX.PuertaCerrar);
                if (sala.esSalaJefe)
                {
                    GestorAudio.PlayBGM(TipoMusica.Jefe);
                    GestorAudio.PlaySFX(TipoSFX.RugidoJefe);
                }
            }
            else if (despejada)
            {
                GestorAudio.PlaySFX(TipoSFX.PuertaAbrir);
                GestorAudio.PlaySFX(TipoSFX.SalaDespejada);
                if (sala.esSalaJefe)
                {
                    GestorAudio.PlayBGM(TipoMusica.Mazmorra);
                    sala.AbrirMuroSalidaJefe();
                }
            }
        }

        [ClientRpc]
        public void MostrarPortalesSalaClientRpc(int indiceSala, bool mostrar)
        {
            if (GeneradorMazmorra.Instancia == null) return;
            if (indiceSala < 0 || indiceSala >= GeneradorMazmorra.Instancia.salasGeneradas.Count) return;

            var sala = GeneradorMazmorra.Instancia.salasGeneradas[indiceSala];
            if (sala != null)
            {
                sala.MostrarPortalesSpawn(mostrar);
                if (mostrar)
                {
                    GestorAudio.PlaySFX(TipoSFX.Portal);
                }
            }
        }

        static string Recortar(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));
    }
}
