using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    public enum EstadoLobbyUI
    {
        VistaGeneral,   // Salón del Lobby con cuartos físicos accesibles y estado de jugadores
        VistaCuarto     // Dentro de un cuarto específico: personalización de loadout y maestría
    }

    /// <summary>
    /// Controlador del Lobby con Cuartos Físicos Accesibles:
    /// - Cuartos físicos perimetrales transitables para Fuego, Hielo, Eléctrico y Próximamente.
    /// - Al inicio los jugadores NO arrancan con Fuego por defecto; arrancan como avatar Neutral.
    /// - Al entrar caminando a un cuarto, se abre la vista detallada y el jugador NO se puede mover (Controles.Bloqueado = true).
    /// - Botón "SELECCIONAR PERSONAJE": Reclama el personaje (1 cuarto = 1 jugador exclusivo).
    /// - Botón "LIBERAR PERSONAJE": Permite soltar el personaje actual para elegir otro.
    /// - Botón "VOLVER": Cierra el menú y desbloquea el movimiento para salir caminando.
    /// - Botón "Iniciar Mazmorra": Visible solo para el host, bloqueado si hay 0 jugadores con personaje asignado.
    /// </summary>
    public class ControladorLobby : MonoBehaviour
    {
        public static ControladorLobby Instancia { get; private set; }

        [Header("Textos Principales")]
        public Text textoNombreSala;
        public Text textoEstado;
        public Text textoInfoConexion;
        public Button botonCopiarIP;

        [Header("Controles Host")]
        public Button botonIniciar;
        public Button botonSalir;

        [Header("Paneles de Vistas")]
        public GameObject panelVistaGeneral;
        public GameObject panelVistaCuarto;

        [Header("Vista Detallada de Cuarto (Loadout y Maestría)")]
        public Text textoTituloCuarto;
        public Text textoMaestriaCuarto;
        public Text textoEstadoCuarto;
        public Transform contenedorSlotsHabilidades;
        public Button botonSeleccionarCuarto;
        public Button botonLiberarCuarto;
        public Button botonVolverCuarto;

        [Header("Spawns y Prefabs de Jugadores")]
        public GameObject prefabNeutral;
        public GameObject prefabFuego;
        public GameObject prefabHielo;
        public GameObject prefabElectrico;
        public Transform[] puntosSpawnLobby;

        private readonly Dictionary<ulong, GameObject> _personajesLobby = new Dictionary<ulong, GameObject>();
        private readonly Dictionary<ulong, TipoPersonaje> _personajeElegidoLobby = new Dictionary<ulong, TipoPersonaje>();

        private EstadoLobbyUI _estadoActual = EstadoLobbyUI.VistaGeneral;
        private TipoPersonaje _cuartoAbierto = TipoPersonaje.Ninguno;

        private GameObject _panelFinPartidaIndividual;

        void Awake()
        {
            Instancia = this;
        }

        void Start()
        {
            Controles.Bloqueado = false;
            SalaRed.ListaActualizada += ActualizarUI;
            SalaRed.ListaActualizada += ActualizarPersonajesLobbyServer;

            if (Global.Instancia != null)
            {
                Global.Instancia.IPActualizada += ActualizarUI;
            }

            if (botonCopiarIP) botonCopiarIP.onClick.AddListener(CopiarIP);
            if (botonIniciar) botonIniciar.onClick.AddListener(IniciarPartida);
            if (botonSalir)
            {
                botonSalir.onClick.AddListener(() =>
                {
                    SalaRed.MostrarFinPartidaPendiente = false;
                    Controles.Bloqueado = false;
                    if (Global.Instancia != null) Global.Instancia.SalirAlMenu("Has salido de la sala.");
                });
            }

            if (botonSeleccionarCuarto)
            {
                botonSeleccionarCuarto.onClick.AddListener(SeleccionarPersonajeActual);
            }
            if (botonLiberarCuarto)
            {
                botonLiberarCuarto.onClick.AddListener(LiberarPersonajeActual);
            }
            if (botonVolverCuarto)
            {
                botonVolverCuarto.onClick.AddListener(VolverDeCuarto);
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                // Modo prueba local sin red
                if (GameObject.FindObjectOfType<Personaje>() == null)
                {
                    var prefab = prefabNeutral != null ? prefabNeutral : (prefabFuego != null ? prefabFuego : null);
                    if (prefab != null)
                    {
                        var pjObj = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
                        if (CamaraSeguir.Instancia != null) CamaraSeguir.Instancia.objetivo = pjObj.transform;
                    }
                }
            }

            CambiarVista(EstadoLobbyUI.VistaGeneral);
            ActualizarUI();
            ActualizarPersonajesLobbyServer();

            // Si este jugador aún no ha pulsado "Volver al Lobby" tras caer todo el equipo,
            // mantener su pantalla de fin de partida activa hasta que lo pulse individualmente
            if (SalaRed.MostrarFinPartidaPendiente)
            {
                MostrarOverlayFinPartidaIndividual(SalaRed.UltimoResultadoVictoria);
            }
        }

        private void MostrarOverlayFinPartidaIndividual(bool victoria)
        {
            Controles.Bloqueado = true;

            Canvas canvasDestino = GetComponentInChildren<Canvas>();
            if (canvasDestino == null) canvasDestino = FindObjectOfType<Canvas>();
            if (canvasDestino == null) return;

            if (_panelFinPartidaIndividual == null)
            {
                _panelFinPartidaIndividual = new GameObject("PanelFinPartidaIndividual");
                _panelFinPartidaIndividual.transform.SetParent(canvasDestino.transform, false);
                _panelFinPartidaIndividual.transform.SetAsLastSibling();

                var rt = _panelFinPartidaIndividual.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                var imgFondo = _panelFinPartidaIndividual.AddComponent<Image>();
                imgFondo.color = new Color(0.04f, 0.04f, 0.06f, 0.96f);

                var txtObj = new GameObject("TextoResultado");
                txtObj.transform.SetParent(_panelFinPartidaIndividual.transform, false);
                var txt = txtObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 36;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = victoria ? new Color(1f, 0.85f, 0.25f) : new Color(0.95f, 0.25f, 0.25f);
                txt.text = victoria ? "¡VICTORIA!" : "TODO EL EQUIPO HA CAÍDO";
                var rtTxt = txtObj.GetComponent<RectTransform>();
                rtTxt.anchorMin = new Vector2(0.5f, 0.5f);
                rtTxt.anchorMax = new Vector2(0.5f, 0.5f);
                rtTxt.anchoredPosition = new Vector2(0f, 55f);
                rtTxt.sizeDelta = new Vector2(700f, 80f);

                var btnObj = new GameObject("BotonVolverLobbyIndividual");
                btnObj.transform.SetParent(_panelFinPartidaIndividual.transform, false);
                var imgBtn = btnObj.AddComponent<Image>();
                imgBtn.color = new Color(0.20f, 0.28f, 0.42f, 1f);
                var btn = btnObj.AddComponent<Button>();
                var rtBtn = btnObj.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
                rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
                rtBtn.anchoredPosition = new Vector2(0f, -35f);
                rtBtn.sizeDelta = new Vector2(280f, 48f);

                var lblBtnObj = new GameObject("Texto");
                lblBtnObj.transform.SetParent(btnObj.transform, false);
                var lblBtn = lblBtnObj.AddComponent<Text>();
                lblBtn.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblBtn.fontSize = 18;
                lblBtn.fontStyle = FontStyle.Bold;
                lblBtn.alignment = TextAnchor.MiddleCenter;
                lblBtn.color = Color.white;
                lblBtn.text = "Volver al Lobby";
                var rtLbl = lblBtnObj.GetComponent<RectTransform>();
                rtLbl.anchorMin = Vector2.zero;
                rtLbl.anchorMax = Vector2.one;
                rtLbl.sizeDelta = Vector2.zero;

                btn.onClick.AddListener(() =>
                {
                    SalaRed.MostrarFinPartidaPendiente = false;
                    Controles.Bloqueado = false;
                    if (_panelFinPartidaIndividual != null)
                    {
                        _panelFinPartidaIndividual.SetActive(false);
                    }
                });
            }

            _panelFinPartidaIndividual.SetActive(true);
            _panelFinPartidaIndividual.transform.SetAsLastSibling();
        }

        void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                ActualizarPersonajesLobbyServer();
            }

            if (_estadoActual == EstadoLobbyUI.VistaCuarto && Input.GetKeyDown(KeyCode.Escape))
            {
                VolverDeCuarto();
            }
        }

        void OnDestroy()
        {
            Controles.Bloqueado = false;
            SalaRed.ListaActualizada -= ActualizarUI;
            SalaRed.ListaActualizada -= ActualizarPersonajesLobbyServer;
            if (Global.Instancia != null)
            {
                Global.Instancia.IPActualizada -= ActualizarUI;
            }
            if (Instancia == this) Instancia = null;
        }

        /// <summary>
        /// Llamado cuando el jugador local entra físicamente al trigger de un cuarto en el mapa.
        /// </summary>
        public void AlEntrarCuartoFisico(CuartoFisicoLobby cuarto)
        {
            if (cuarto == null) return;

            if (cuarto.esProximamente)
            {
                if (textoEstado != null)
                {
                    textoEstado.text = "🔒 CUARTO PRÓXIMAMENTE: Reservado para futuros personajes.";
                }
                return;
            }

            AbrirVistaCuarto(cuarto.personaje);
        }

        public void AlSalirDeCuartoFisico(CuartoFisicoLobby cuarto)
        {
            // Opcional: Si el jugador sale del cuarto mientras tenía el menú abierto, no se cierra automáticamente
            // para permitirle terminar de elegir su loadout hasta que pulse "Volver".
        }

        public void AbrirVistaCuarto(TipoPersonaje pj)
        {
            if (pj == TipoPersonaje.Ninguno) return;

            // Bloquear el movimiento mientras selecciona personaje
            Controles.Bloqueado = true;
            _cuartoAbierto = pj;
            CambiarVista(EstadoLobbyUI.VistaCuarto);
            ConstruirVistaCuarto(pj);
        }

        public void SeleccionarPersonajeActual()
        {
            if (_cuartoAbierto == TipoPersonaje.Ninguno) return;

            ulong miId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            ulong ocupante = ObtenerOcupanteDePersonaje(_cuartoAbierto);

            if (ocupante == ulong.MaxValue || ocupante == miId)
            {
                TipoPersonaje seleccionado = _cuartoAbierto;

                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.ElegirPersonaje(seleccionado);
                }
                else
                {
                    // Fallback para pruebas locales en Editor sin red
                    AplicarPersonajeLocal(seleccionado);
                }

                if (textoEstado != null)
                {
                    textoEstado.text = $"🎉 ¡Has seleccionado a {seleccionado}! Dirígete al portal norte para iniciar la mazmorra.";
                }

                // Marcar el cuarto físico para no volver a desplegar la ventana inmediatamente mientras el jugador permanezca adentro
                var todosCuartos = GameObject.FindObjectsOfType<CuartoFisicoLobby>();
                foreach (var c in todosCuartos)
                {
                    if (c != null && c.personaje == seleccionado)
                    {
                        c.MarcarSeleccionado();
                    }
                }

                // Desbloquear controles inmediatamente para que el jugador pueda moverse
                Controles.Bloqueado = false;
                _cuartoAbierto = TipoPersonaje.Ninguno;

                // Cerrar vista del cuarto y volver a la vista general del lobby
                CambiarVista(EstadoLobbyUI.VistaGeneral);
                ActualizarUI();
            }
            else
            {
                string nombre = SalaRed.Instancia != null ? SalaRed.Instancia.NombreDe(ocupante) : "Otro jugador";
                if (textoEstadoCuarto != null)
                {
                    textoEstadoCuarto.text = $"⚠️ Este cuarto ya está ocupado por {nombre}.";
                }
            }
        }

        public void LiberarPersonajeActual()
        {
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.LiberarPersonaje();
            }
            else
            {
                AplicarPersonajeLocal(TipoPersonaje.Ninguno);
            }

            if (textoEstado != null)
            {
                textoEstado.text = "Personaje liberado. Ahora estás en modo Neutral.";
            }

            Controles.Bloqueado = false;
            _cuartoAbierto = TipoPersonaje.Ninguno;
            CambiarVista(EstadoLobbyUI.VistaGeneral);
            ActualizarUI();
        }

        private void AplicarPersonajeLocal(TipoPersonaje pj)
        {
            var pjExistente = GameObject.FindObjectOfType<Personaje>();
            Vector3 pos = pjExistente != null ? pjExistente.transform.position : Vector3.zero;
            if (pjExistente != null) Destroy(pjExistente.gameObject);

            GameObject prefab = pj switch
            {
                TipoPersonaje.Fuego => prefabFuego,
                TipoPersonaje.Hielo => prefabHielo,
                TipoPersonaje.Electrico => prefabElectrico,
                _ => (prefabNeutral != null ? prefabNeutral : prefabFuego)
            };

            if (prefab != null)
            {
                var nuevo = Instantiate(prefab, pos, Quaternion.identity);
                if (CamaraSeguir.Instancia != null) CamaraSeguir.Instancia.objetivo = nuevo.transform;
            }
        }

        public void VolverDeCuarto()
        {
            // Marcar el cuarto para no reabrirse de inmediato hasta que salga
            var todosCuartos = GameObject.FindObjectsOfType<CuartoFisicoLobby>();
            foreach (var c in todosCuartos)
            {
                if (c != null && c.personaje == _cuartoAbierto)
                {
                    c.MarcarSeleccionado();
                }
            }

            // Desbloquear controles para poder moverse de nuevo por el Lobby
            Controles.Bloqueado = false;
            _cuartoAbierto = TipoPersonaje.Ninguno;
            CambiarVista(EstadoLobbyUI.VistaGeneral);
            ActualizarUI();
        }

        private void CambiarVista(EstadoLobbyUI estado)
        {
            _estadoActual = estado;
            if (panelVistaGeneral != null) panelVistaGeneral.SetActive(estado == EstadoLobbyUI.VistaGeneral);
            if (panelVistaCuarto != null) panelVistaCuarto.SetActive(estado == EstadoLobbyUI.VistaCuarto);
        }

        private void ConstruirVistaCuarto(TipoPersonaje pj)
        {
            if (textoTituloCuarto != null)
            {
                string emojiPj = pj switch
                {
                    TipoPersonaje.Fuego => "🔥",
                    TipoPersonaje.Hielo => "❄️",
                    TipoPersonaje.Electrico => "⚡",
                    _ => "⭐"
                };
                textoTituloCuarto.text = $"{emojiPj} PERSONAJE: {pj.ToString().ToUpper()}";
            }

            int xpMaestria = GestorMaestria.ObtenerPuntos(pj);
            int nivelMaestria = GestorMaestria.ObtenerNivelMaestria(pj);
            bool esMaxMaestria = nivelMaestria >= GestorMaestria.NIVEL_MAXIMO_MAESTRIA;
            int xpProgresoNivel = esMaxMaestria ? GestorMaestria.XP_POR_NIVEL_MAESTRIA : (xpMaestria % GestorMaestria.XP_POR_NIVEL_MAESTRIA);

            if (textoMaestriaCuarto != null)
            {
                string infoSiguiente = esMaxMaestria
                    ? "<size=12><color=#ffd700>(¡NIVEL MÁXIMO ALCANZADO!)</color></size>"
                    : $"<size=12><color=#a0d8ef>({xpProgresoNivel}/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP al Nv.{nivelMaestria + 1})</color></size>";

                textoMaestriaCuarto.text = $"<b>⭐ MAESTRÍA PERMANENTE: NIVEL {nivelMaestria}</b>  {infoSiguiente}\n" +
                    $"<size=11><color=#f1fa8c>💡 La Maestría es permanente para este héroe (se guarda en tu cuenta y no se pierde al morir).\nSe gana eliminando monstruos y completando salas.\n• Nv.1 a 4: Desbloquea tus 4 habilidades (Básica, E, Q y Ultimate R).\n• Nv.5 a 20: Concede bonificaciones base.</color></size>";
            }

            ulong miId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            ulong ocupante = ObtenerOcupanteDePersonaje(pj);
            bool ocupadoPorMi = (ocupante == miId);
            bool ocupadoPorOtro = (ocupante != ulong.MaxValue && ocupante != miId);

            if (textoEstadoCuarto != null)
            {
                if (ocupadoPorMi)
                {
                    textoEstadoCuarto.text = "👑 SELECCIONADO POR TI (LISTO)";
                    textoEstadoCuarto.color = new Color(0.25f, 1f, 0.45f);
                }
                else if (ocupadoPorOtro)
                {
                    string nom = SalaRed.Instancia != null ? SalaRed.Instancia.NombreDe(ocupante) : "Otro jugador";
                    textoEstadoCuarto.text = $"🔒 OCUPADO POR: {nom}";
                    textoEstadoCuarto.color = new Color(1f, 0.35f, 0.35f);
                }
                else
                {
                    textoEstadoCuarto.text = "✨ DISPONIBLE PARA SELECCIONAR";
                    textoEstadoCuarto.color = new Color(0.35f, 0.9f, 1f);
                }
            }

            // Botón Seleccionar
            if (botonSeleccionarCuarto != null)
            {
                var txtBtnSel = botonSeleccionarCuarto.GetComponentInChildren<Text>();
                if (ocupadoPorMi)
                {
                    if (txtBtnSel) txtBtnSel.text = "✅ CONFIRMAR ELECCIÓN";
                    botonSeleccionarCuarto.interactable = true;
                }
                else if (ocupadoPorOtro)
                {
                    if (txtBtnSel) txtBtnSel.text = "🔒 OCUPADO";
                    botonSeleccionarCuarto.interactable = false;
                }
                else
                {
                    if (txtBtnSel) txtBtnSel.text = "ELEGIR ESTE PERSONAJE";
                    botonSeleccionarCuarto.interactable = true;
                }
            }

            // Botón Liberar (solo visible si este personaje está reclamado por mí)
            if (botonLiberarCuarto != null)
            {
                botonLiberarCuarto.gameObject.SetActive(ocupadoPorMi);
            }

            if (contenedorSlotsHabilidades == null) return;

            // Limpiar slots viejos
            foreach (Transform hijo in contenedorSlotsHabilidades)
            {
                Destroy(hijo.gameObject);
            }

            // Obtener todas las habilidades del personaje y el loadout seleccionado actualmente
            var todasHabilidades = CatalogoHabilidades.ObtenerHabilidadesPersonaje(pj);
            var loadoutActual = GestorLoadout.ObtenerLoadout(pj);

            foreach (var hab in todasHabilidades)
            {
                bool desbloqueada = GestorMaestria.EstaDesbloqueada(pj, hab.id);
                bool equipada = loadoutActual.Contains(hab.id);

                var slotObj = new GameObject($"Slot_{hab.id}");
                slotObj.transform.SetParent(contenedorSlotsHabilidades, false);
                var img = slotObj.AddComponent<Image>();
                img.color = equipada 
                    ? new Color(0.12f, 0.32f, 0.20f, 0.95f) 
                    : (desbloqueada ? new Color(0.14f, 0.16f, 0.22f, 0.90f) : new Color(0.08f, 0.08f, 0.11f, 0.70f));
                var rt = slotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(620, 60);

                var txtObj = new GameObject("TextoSlot");
                txtObj.transform.SetParent(slotObj.transform, false);
                var txt = txtObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 14;
                txt.color = desbloqueada ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                txt.alignment = TextAnchor.MiddleLeft;
                txt.rectTransform.anchoredPosition = new Vector2(-70, 0);
                txt.rectTransform.sizeDelta = new Vector2(440, 52);

                string estadoRun = desbloqueada 
                    ? $"<color=#8be9fd>Desbloquea en Run Nv.{hab.nivelRequeridoRun}</color>" 
                    : $"<color=#ffb86c>Requiere Maestría Nv.{hab.nivelMaestriaRequerido}</color>";

                txt.text = $"<b>{hab.nombre}</b>  —  {estadoRun}\n<size=11><color=#b0b0b0>{hab.descripcion}</color></size>";

                // Botón interactivo o etiqueta de estado
                if (desbloqueada)
                {
                    var btnEquiparObj = new GameObject("BotonEquipar");
                    btnEquiparObj.transform.SetParent(slotObj.transform, false);
                    var imgBtn = btnEquiparObj.AddComponent<Image>();
                    imgBtn.color = equipada ? new Color(0.65f, 0.22f, 0.22f) : new Color(0.20f, 0.55f, 0.30f);
                    var rtBtn = btnEquiparObj.GetComponent<RectTransform>();
                    rtBtn.anchoredPosition = new Vector2(230, 0);
                    rtBtn.sizeDelta = new Vector2(100, 36);

                    var txtBtnObj = new GameObject("TxtBtn");
                    txtBtnObj.transform.SetParent(btnEquiparObj.transform, false);
                    var txtBtn = txtBtnObj.AddComponent<Text>();
                    txtBtn.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txtBtn.fontSize = 12;
                    txtBtn.fontStyle = FontStyle.Bold;
                    txtBtn.color = Color.white;
                    txtBtn.alignment = TextAnchor.MiddleCenter;
                    txtBtn.text = equipada ? "EQUIPADA" : "+ EQUIPAR";
                    txtBtn.rectTransform.sizeDelta = new Vector2(100, 36);

                    var btn = btnEquiparObj.AddComponent<Button>();
                    IdHabilidad idClick = hab.id;
                    btn.onClick.AddListener(() =>
                    {
                        AlternarHabilidadEnLoadout(pj, idClick);
                        ConstruirVistaCuarto(pj);
                    });
                }
                else
                {
                    var txtBloqObj = new GameObject("TxtBloq");
                    txtBloqObj.transform.SetParent(slotObj.transform, false);
                    var txtBloq = txtBloqObj.AddComponent<Text>();
                    txtBloq.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txtBloq.fontSize = 12;
                    txtBloq.fontStyle = FontStyle.Bold;
                    txtBloq.color = new Color(1f, 0.70f, 0.25f);
                    txtBloq.alignment = TextAnchor.MiddleCenter;
                    txtBloq.text = $"🔒 NV.{hab.nivelMaestriaRequerido}";
                    txtBloq.rectTransform.anchoredPosition = new Vector2(230, 0);
                    txtBloq.rectTransform.sizeDelta = new Vector2(100, 36);
                }
            }
        }

        private void AlternarHabilidadEnLoadout(TipoPersonaje pj, IdHabilidad id)
        {
            var loadout = GestorLoadout.ObtenerLoadout(pj);
            if (loadout.Contains(id))
            {
                if (loadout.Count > 1)
                {
                    loadout.Remove(id);
                }
            }
            else
            {
                if (loadout.Count < 4)
                {
                    loadout.Add(id);
                }
            }
            GestorLoadout.GuardarLoadout(pj, loadout);
        }

        private ulong ObtenerOcupanteDePersonaje(TipoPersonaje pj)
        {
            if (SalaRed.Instancia == null) return ulong.MaxValue;
            foreach (var j in SalaRed.Instancia.Jugadores)
            {
                if (j.personaje == pj) return j.idCliente;
            }
            return ulong.MaxValue;
        }

        public void IniciarPartida()
        {
            if (SalaRed.Instancia == null || !SalaRed.Instancia.IsServer) return;

            int totalJugadores = SalaRed.Instancia.Jugadores.Count;
            if (totalJugadores == 0) return;

            int jugadoresConPersonaje = 0;
            foreach (var j in SalaRed.Instancia.Jugadores)
            {
                if (j.personaje != TipoPersonaje.Ninguno) jugadoresConPersonaje++;
            }

            if (jugadoresConPersonaje < totalJugadores)
            {
                if (textoEstado != null)
                {
                    textoEstado.text = $"⚠️ Faltan jugadores por elegir personaje ({jugadoresConPersonaje}/{totalJugadores} listos). Entren a sus cuartos.";
                }
                return;
            }

            Controles.Bloqueado = false;
            SalaRed.Instancia.IniciarPartida();
        }

        public void RevisarPortalMazmorra(HashSet<ulong> jugadoresEnPortal, bool mostrarAviso)
        {
            if (SalaRed.Instancia == null)
            {
                // Modo pruebas locales
                IniciarPartida();
                return;
            }

            int totalJugadores = SalaRed.Instancia.Jugadores.Count;
            int enPortal = jugadoresEnPortal.Count;
            int jugadoresConPersonaje = 0;

            foreach (var j in SalaRed.Instancia.Jugadores)
            {
                if (j.personaje != TipoPersonaje.Ninguno) jugadoresConPersonaje++;
            }

            if (enPortal < totalJugadores)
            {
                if (mostrarAviso && textoEstado != null)
                {
                    textoEstado.text = $"⏳ Esperando al resto del equipo en el portal ({enPortal}/{totalJugadores} jugadores en el portal).";
                }
                return;
            }

            if (jugadoresConPersonaje < totalJugadores)
            {
                if (mostrarAviso && textoEstado != null)
                {
                    textoEstado.text = $"⚠️ ¡Todos están en el portal pero faltan jugadores por elegir personaje! ({jugadoresConPersonaje}/{totalJugadores} elegidos).";
                }
                return;
            }

            // Si todos están en el portal y todos eligieron personaje:
            if (SalaRed.Instancia.IsServer)
            {
                if (textoEstado != null)
                {
                    textoEstado.text = "⚔️ ¡Todo el equipo está listo y en el portal! Iniciando mazmorra...";
                }
                IniciarPartida();
            }
        }

        public void AlTocarPortalMazmorra()
        {
            // Método mantenido por compatibilidad
            var portal = FindObjectOfType<PortalLobby>();
            if (portal != null)
            {
                RevisarPortalMazmorra(portal.JugadoresEnPortal, mostrarAviso: true);
            }
            else
            {
                IniciarPartida();
            }
        }

        private void CopiarIP()
        {
            if (Global.Instancia == null) return;
            string ipObjetivo = !string.IsNullOrEmpty(Global.Instancia.IPPublicaHost) 
                ? Global.Instancia.IPPublicaHost 
                : Global.Instancia.IPLocalHost;

            string codigoSecreto = CodigoSala.EncriptarIP(ipObjetivo, Global.Puerto);
            GUIUtility.systemCopyBuffer = codigoSecreto;
            if (textoEstado != null)
            {
                textoEstado.text = $"¡Código Secreto ({codigoSecreto}) copiado! Pásaselo a tus compañeros.";
            }
        }

        private bool _actualizandoPersonajesLobby = false;

        public void ActualizarPersonajesLobbyServer()
        {
            if (_actualizandoPersonajesLobby) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
            if (SalaRed.Instancia == null) return;

            _actualizandoPersonajesLobby = true;
            try
            {
                var jugadoresActuales = new HashSet<ulong>();
                int index = 0;

                for (int i = 0; i < SalaRed.Instancia.Jugadores.Count; i++)
                {
                    var info = SalaRed.Instancia.Jugadores[i];
                    if (!jugadoresActuales.Add(info.idCliente)) continue;

                    TipoPersonaje tipoActual = info.personaje; // Si es Ninguno, se genera el prefab neutral!

                    bool necesitaSpawn = false;
                    Vector3 spawnPos = Vector3.zero;

                    if (!_personajesLobby.TryGetValue(info.idCliente, out GameObject objActual) || objActual == null)
                    {
                        necesitaSpawn = true;
                        if (puntosSpawnLobby != null && puntosSpawnLobby.Length > 0)
                            spawnPos = puntosSpawnLobby[index % puntosSpawnLobby.Length].position;
                        else
                            spawnPos = new Vector3(-3f + index * 2f, 0f, 0f);
                    }
                    else if (_personajeElegidoLobby.TryGetValue(info.idCliente, out TipoPersonaje tipoPrevio) && tipoPrevio != tipoActual)
                    {
                        necesitaSpawn = true;
                        spawnPos = objActual.transform.position;
                        var no = objActual.GetComponent<NetworkObject>();
                        if (no != null && no.IsSpawned) no.Despawn(true);
                        else Destroy(objActual);
                        _personajesLobby.Remove(info.idCliente);
                    }

                    if (necesitaSpawn)
                    {
                        GameObject prefab = tipoActual switch
                        {
                            TipoPersonaje.Fuego => prefabFuego,
                            TipoPersonaje.Hielo => prefabHielo,
                            TipoPersonaje.Electrico => prefabElectrico,
                            _ => (prefabNeutral != null ? prefabNeutral : prefabFuego)
                        };

                        if (prefab != null)
                        {
                            GameObject nuevoPj = Instantiate(prefab, spawnPos, Quaternion.identity);
                            _personajesLobby[info.idCliente] = nuevoPj;
                            _personajeElegidoLobby[info.idCliente] = tipoActual;

                            var netObj = nuevoPj.GetComponent<NetworkObject>();
                            if (netObj != null)
                            {
                                netObj.SpawnWithOwnership(info.idCliente, destroyWithScene: true);
                            }
                        }
                    }
                    index++;
                }

                // Limpiar desconectados
                var aEliminar = new List<ulong>();
                foreach (var kvp in _personajesLobby)
                {
                    if (!jugadoresActuales.Contains(kvp.Key))
                    {
                        if (kvp.Value != null)
                        {
                            var no = kvp.Value.GetComponent<NetworkObject>();
                            if (no != null && no.IsSpawned) no.Despawn(true);
                            else Destroy(kvp.Value);
                        }
                        aEliminar.Add(kvp.Key);
                    }
                }
                foreach (var id in aEliminar)
                {
                    _personajesLobby.Remove(id);
                    _personajeElegidoLobby.Remove(id);
                }
            }
            finally
            {
                _actualizandoPersonajesLobby = false;
            }
        }

        private void ActualizarUI()
        {
            if (SalaRed.Instancia == null) return;

            if (textoNombreSala != null)
            {
                textoNombreSala.text = "Sala: " + SalaRed.Instancia.NombreSala.Value;
            }

            bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            // Calcular cuántos jugadores tienen personaje asignado
            int totalJugadores = SalaRed.Instancia.Jugadores.Count;
            int jugadoresConPersonaje = 0;
            foreach (var j in SalaRed.Instancia.Jugadores)
            {
                if (j.personaje != TipoPersonaje.Ninguno) jugadoresConPersonaje++;
            }

            // Habilitar/Deshabilitar botón Iniciar Mazmorra (todos los jugadores deben haber elegido personaje)
            if (botonIniciar != null)
            {
                botonIniciar.gameObject.SetActive(esHost);
                botonIniciar.interactable = (totalJugadores > 0 && jugadoresConPersonaje == totalJugadores);
            }

            if (botonCopiarIP != null)
            {
                botonCopiarIP.gameObject.SetActive(esHost);
            }

            if (textoInfoConexion != null)
            {
                if (esHost && Global.Instancia != null)
                {
                    string ipObjetivo = !string.IsNullOrEmpty(Global.Instancia.IPPublicaHost) 
                        ? Global.Instancia.IPPublicaHost 
                        : Global.Instancia.IPLocalHost;
                    string codigo = CodigoSala.EncriptarIP(ipObjetivo, Global.Puerto);
                    textoInfoConexion.text = $"🔑 Código de Sala:\n<size=22><b>{codigo}</b></size>";
                }
                else
                {
                    textoInfoConexion.text = "Salón de Espera - Entra a un cuarto para elegir personaje";
                }
            }

            // Si está viendo un cuarto actualmente, refrescar su estado
            if (_estadoActual == EstadoLobbyUI.VistaCuarto && _cuartoAbierto != TipoPersonaje.Ninguno)
            {
                ConstruirVistaCuarto(_cuartoAbierto);
            }
        }
    }
}
