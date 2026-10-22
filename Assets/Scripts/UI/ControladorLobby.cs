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
        private IdHabilidad _habilidadSeleccionadaCuarto = IdHabilidad.Ninguna;
        private GameObject _modalMaestriaLobby;
        private Text _txtDetalleTecnicoCuarto;

        void Awake()
        {
            Instancia = this;
        }

        void Start()
        {
            Controles.Bloqueado = false;
            ComandosAdmin.ResetearAjustesAdmin();
            GestorAudio.DetenerTodosLosSFX();
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
            GestorAudio.PlayBGM(TipoMusica.Lobby);
            AsegurarBotonOpcionesLobby();
            AplicarEstiloLobbyUI();

            // Si este jugador aún no ha pulsado "Volver al Lobby" tras caer todo el equipo,
            // mantener su pantalla de fin de partida activa hasta que lo pulse individualmente
            if (SalaRed.MostrarFinPartidaPendiente)
            {
                MostrarOverlayFinPartidaIndividual(SalaRed.UltimoResultadoVictoria);
            }
        }

        private void AplicarEstiloLobbyUI()
        {
            if (botonIniciar != null)
            {
                EstiloUIUX.AplicarEstiloBoton(botonIniciar, TipoBotonUI.Verde, 20, "play", new Vector2(24f, 24f));
            }
            if (botonSalir != null)
            {
                EstiloUIUX.AplicarEstiloBoton(botonSalir, TipoBotonUI.Piedra, 18, "puerta", new Vector2(24f, 24f));
            }
            if (botonCopiarIP != null)
            {
                EstiloUIUX.AplicarEstiloBoton(botonCopiarIP, TipoBotonUI.Hielo, 15);
            }
            if (textoNombreSala != null)
            {
                textoNombreSala.font = FuenteUtil.ObtenerFuentePixelify();
                textoNombreSala.fontStyle = FontStyle.Bold;
                textoNombreSala.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoNombreSala.verticalOverflow = VerticalWrapMode.Overflow;
                BordeTextoPixelArt.Aplicar(textoNombreSala, 6, new Vector2(0f, -3f));
            }
            if (textoInfoConexion != null)
            {
                textoInfoConexion.font = FuenteUtil.ObtenerFuentePixelify();
                textoInfoConexion.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoInfoConexion.verticalOverflow = VerticalWrapMode.Overflow;
                var p = textoInfoConexion.transform.parent;
                if (p != null)
                {
                    var imgP = p.GetComponent<Image>();
                    if (imgP != null)
                    {
                        imgP.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                        imgP.type = Image.Type.Sliced;
                        imgP.color = Color.white;
                    }
                }
            }
            if (textoEstado != null)
            {
                textoEstado.font = FuenteUtil.ObtenerFuentePixelify();
                textoEstado.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoEstado.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (panelVistaCuarto != null)
            {
                var imgCuarto = panelVistaCuarto.GetComponent<Image>();
                if (imgCuarto != null)
                {
                    imgCuarto.sprite = EstiloUIUX.ObtenerSpritePanel();
                    imgCuarto.type = Image.Type.Sliced;
                    imgCuarto.color = Color.white;
                }
                EstiloUIUX.CrearRemaches(panelVistaCuarto.transform, 18f);

                if (textoTituloCuarto != null)
                {
                    textoTituloCuarto.font = FuenteUtil.ObtenerFuentePixelify();
                    textoTituloCuarto.fontSize = 28;
                    textoTituloCuarto.fontStyle = FontStyle.Bold;
                    textoTituloCuarto.color = EstiloUIUX.ColDorado;
                    textoTituloCuarto.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textoTituloCuarto.verticalOverflow = VerticalWrapMode.Overflow;
                    BordeTextoPixelArt.Aplicar(textoTituloCuarto, 8, new Vector2(0f, -4f));
                }
                if (textoMaestriaCuarto != null)
                {
                    textoMaestriaCuarto.font = FuenteUtil.ObtenerFuentePixelify();
                    textoMaestriaCuarto.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textoMaestriaCuarto.verticalOverflow = VerticalWrapMode.Overflow;
                }
                if (textoEstadoCuarto != null)
                {
                    textoEstadoCuarto.font = FuenteUtil.ObtenerFuentePixelify();
                    textoEstadoCuarto.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textoEstadoCuarto.verticalOverflow = VerticalWrapMode.Overflow;
                }

                if (botonSeleccionarCuarto != null)
                {
                    EstiloUIUX.AplicarEstiloBoton(botonSeleccionarCuarto, TipoBotonUI.Verde, 16);
                }
                if (botonLiberarCuarto != null)
                {
                    EstiloUIUX.AplicarEstiloBoton(botonLiberarCuarto, TipoBotonUI.Rojo, 16);
                }
                if (botonVolverCuarto != null)
                {
                    EstiloUIUX.AplicarEstiloBoton(botonVolverCuarto, TipoBotonUI.Piedra, 16);
                }
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
                imgFondo.color = new Color(0.04f, 0.04f, 0.06f, 0.94f);

                // Ventana modal pixel art
                var modalObj = new GameObject("VentanaFinPartida");
                modalObj.transform.SetParent(_panelFinPartidaIndividual.transform, false);
                var rtModal = modalObj.AddComponent<RectTransform>();
                rtModal.anchorMin = new Vector2(0.5f, 0.5f);
                rtModal.anchorMax = new Vector2(0.5f, 0.5f);
                rtModal.pivot = new Vector2(0.5f, 0.5f);
                rtModal.anchoredPosition = Vector2.zero;
                rtModal.sizeDelta = new Vector2(760f, 380f);

                var imgModal = modalObj.AddComponent<Image>();
                imgModal.sprite = EstiloUIUX.ObtenerSpritePanel();
                imgModal.type = Image.Type.Sliced;
                imgModal.color = Color.white;

                EstiloUIUX.CrearRemaches(modalObj.transform, 16f);
                EstiloUIUX.CrearCabeceraModal(modalObj.transform, victoria ? "¡VICTORIA!" : "EXPEDICIÓN FALLIDA", 80f, "huella");

                var txtObj = new GameObject("TextoResultado");
                txtObj.transform.SetParent(modalObj.transform, false);
                var txt = txtObj.AddComponent<Text>();
                txt.font = FuenteUtil.ObtenerFuentePixelify();
                txt.fontSize = 26;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = victoria ? EstiloUIUX.ColDorado : EstiloUIUX.ColRojo;
                txt.text = victoria ? "¡HAS COMPLETADO LA MAZMORRA!" : "TODO EL EQUIPO HA CAÍDO EN COMBATE";
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                BordeTextoPixelArt.Aplicar(txt, 6, new Vector2(0f, -3f));

                var rtTxt = txtObj.GetComponent<RectTransform>();
                rtTxt.anchorMin = new Vector2(0.5f, 0.5f);
                rtTxt.anchorMax = new Vector2(0.5f, 0.5f);
                rtTxt.pivot = new Vector2(0.5f, 0.5f);
                rtTxt.anchoredPosition = new Vector2(0f, 15f);
                rtTxt.sizeDelta = new Vector2(700f, 50f);

                var btn = EstiloUIUX.CrearBoton(modalObj.transform, "BotonVolverLobbyIndividual", "VOLVER AL LOBBY", TipoBotonUI.Verde, new Vector2(300f, 60f), () =>
                {
                    SalaRed.MostrarFinPartidaPendiente = false;
                    Controles.Bloqueado = false;
                    if (_panelFinPartidaIndividual != null)
                    {
                        _panelFinPartidaIndividual.SetActive(false);
                    }
                }, "puerta", new Vector2(24f, 24f));

                var rtBtn = btn.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 35f);
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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (PanelOpcionesControles.Instancia != null && PanelOpcionesControles.Instancia.panelContenedor != null && PanelOpcionesControles.Instancia.panelContenedor.activeSelf)
                {
                    PanelOpcionesControles.Instancia.Cerrar();
                }
                else if (_modalMaestriaLobby != null && _modalMaestriaLobby.activeSelf)
                {
                    _modalMaestriaLobby.SetActive(false);
                }
                else if (_estadoActual == EstadoLobbyUI.VistaCuarto)
                {
                    VolverDeCuarto();
                }
                else
                {
                    Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        PanelOpcionesControles.ObtenerOCrear(canvas.transform).Abrir();
                    }
                }
            }
        }

        private void AsegurarBotonOpcionesLobby()
        {
            Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Transform cont = canvas.transform;
            var existente = cont.Find("BotonOpcionesLobby");
            if (existente != null)
            {
                existente.SetAsLastSibling();
                return;
            }

            var btn = EstiloUIUX.CrearBoton(cont, "BotonOpcionesLobby", "OPCIONES", TipoBotonUI.Piedra, new Vector2(175f, 44f), () =>
            {
                PanelOpcionesControles.ObtenerOCrear(canvas.transform).Abrir();
            }, "controles", new Vector2(24f, 20f));

            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-25f, -25f);
            btn.transform.SetAsLastSibling();
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
                var rtTit = textoTituloCuarto.GetComponent<RectTransform>();
                if (rtTit != null)
                {
                    rtTit.anchoredPosition = new Vector2(0f, 275f);
                    rtTit.sizeDelta = new Vector2(620f, 32f);
                }
                string emojiPj = pj switch
                {
                    TipoPersonaje.Fuego => "🔥",
                    TipoPersonaje.Hielo => "❄️",
                    TipoPersonaje.Electrico => "⚡",
                    _ => "⭐"
                };
                textoTituloCuarto.text = $"{emojiPj} PERSONAJE: {pj.ToString().ToUpper()}";
                textoTituloCuarto.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoTituloCuarto.fontSize = 18;
            }

            int xpMaestria = GestorMaestria.ObtenerPuntos(pj);
            int nivelMaestria = GestorMaestria.ObtenerNivelMaestria(pj);
            bool esMaxMaestria = nivelMaestria >= GestorMaestria.NIVEL_MAXIMO_MAESTRIA;
            int xpProgresoNivel = esMaxMaestria ? GestorMaestria.XP_POR_NIVEL_MAESTRIA : (xpMaestria % GestorMaestria.XP_POR_NIVEL_MAESTRIA);

            if (textoMaestriaCuarto != null)
            {
                var rtM = textoMaestriaCuarto.GetComponent<RectTransform>();
                if (rtM != null)
                {
                    rtM.anchoredPosition = new Vector2(-75f, 238f);
                    rtM.sizeDelta = new Vector2(460f, 26f);
                }
                textoMaestriaCuarto.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoMaestriaCuarto.fontSize = 11;
                textoMaestriaCuarto.alignment = TextAnchor.MiddleLeft;

                string infoSiguiente = esMaxMaestria
                    ? "<color=#ffd700>(¡MAX!)</color>"
                    : $"<color=#a0d8ef>({xpProgresoNivel}/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP)</color>";

                textoMaestriaCuarto.text = $"<b>⭐ MAESTRÍA: NV.{nivelMaestria}</b>  {infoSiguiente}";

                // Botón interactivo de ayuda de maestría al lado
                if (panelVistaCuarto != null)
                {
                    var btnAyuda = panelVistaCuarto.transform.Find("BotonInfoMaestriaCuarto");
                    if (btnAyuda == null)
                    {
                        var btnAyudaObj = new GameObject("BotonInfoMaestriaCuarto");
                        btnAyudaObj.transform.SetParent(panelVistaCuarto.transform, false);
                        var rtBtnA = btnAyudaObj.AddComponent<RectTransform>();
                        rtBtnA.anchorMin = new Vector2(0.5f, 0.5f);
                        rtBtnA.anchorMax = new Vector2(0.5f, 0.5f);
                        rtBtnA.pivot = new Vector2(0.5f, 0.5f);
                        rtBtnA.anchoredPosition = new Vector2(230f, 238f);
                        rtBtnA.sizeDelta = new Vector2(145f, 24f);

                        var imgBtnA = btnAyudaObj.AddComponent<Image>();
                        imgBtnA.color = new Color(0.20f, 0.35f, 0.55f, 0.95f);

                        var txtAyudaObj = new GameObject("Texto");
                        txtAyudaObj.transform.SetParent(btnAyudaObj.transform, false);
                        var rtTa = txtAyudaObj.AddComponent<RectTransform>();
                        rtTa.anchorMin = Vector2.zero;
                        rtTa.anchorMax = Vector2.one;
                        rtTa.sizeDelta = Vector2.zero;
                        var txtTa = txtAyudaObj.AddComponent<Text>();
                        txtTa.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        txtTa.fontSize = 9;
                        txtTa.fontStyle = FontStyle.Bold;
                        txtTa.alignment = TextAnchor.MiddleCenter;
                        txtTa.color = Color.white;
                        txtTa.text = "[?] ¿CÓMO FUNCIONA?";

                        var btnA = btnAyudaObj.AddComponent<Button>();
                        btnA.onClick.AddListener(MostrarExplicacionMaestriaLobby);
                    }
                }
            }

            ulong miId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            ulong ocupante = ObtenerOcupanteDePersonaje(pj);
            bool ocupadoPorMi = (ocupante == miId);
            bool ocupadoPorOtro = (ocupante != ulong.MaxValue && ocupante != miId);

            if (textoEstadoCuarto != null)
            {
                var rtE = textoEstadoCuarto.GetComponent<RectTransform>();
                if (rtE != null)
                {
                    rtE.anchoredPosition = new Vector2(0f, 212f);
                    rtE.sizeDelta = new Vector2(620f, 22f);
                }
                textoEstadoCuarto.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoEstadoCuarto.fontSize = 11;
                textoEstadoCuarto.alignment = TextAnchor.MiddleCenter;

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
                if (txtBtnSel != null)
                {
                    txtBtnSel.font = FuenteUtil.ObtenerFuentePixelify();
                    txtBtnSel.fontSize = 15;
                    txtBtnSel.fontStyle = FontStyle.Bold;
                    txtBtnSel.horizontalOverflow = HorizontalWrapMode.Overflow;
                    txtBtnSel.verticalOverflow = VerticalWrapMode.Overflow;
                }
                if (ocupadoPorMi)
                {
                    if (txtBtnSel) txtBtnSel.text = "CONFIRMAR ELECCIÓN";
                    botonSeleccionarCuarto.interactable = true;
                }
                else if (ocupadoPorOtro)
                {
                    if (txtBtnSel) txtBtnSel.text = "OCUPADO";
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
                var txtBtnLib = botonLiberarCuarto.GetComponentInChildren<Text>();
                if (txtBtnLib != null)
                {
                    txtBtnLib.font = FuenteUtil.ObtenerFuentePixelify();
                    txtBtnLib.fontSize = 15;
                    txtBtnLib.fontStyle = FontStyle.Bold;
                    txtBtnLib.horizontalOverflow = HorizontalWrapMode.Overflow;
                    txtBtnLib.verticalOverflow = VerticalWrapMode.Overflow;
                    txtBtnLib.text = "LIBERAR";
                }
            }

            if (botonVolverCuarto != null)
            {
                var txtBtnVol = botonVolverCuarto.GetComponentInChildren<Text>();
                if (txtBtnVol != null)
                {
                    txtBtnVol.font = FuenteUtil.ObtenerFuentePixelify();
                    txtBtnVol.fontSize = 15;
                    txtBtnVol.fontStyle = FontStyle.Bold;
                    txtBtnVol.horizontalOverflow = HorizontalWrapMode.Overflow;
                    txtBtnVol.verticalOverflow = VerticalWrapMode.Overflow;
                    txtBtnVol.text = "VOLVER";
                }
            }

            if (contenedorSlotsHabilidades == null) return;

            // Eliminar tarjeta externa previa si existiera en panelVistaCuarto
            if (panelVistaCuarto != null)
            {
                var cardPrevia = panelVistaCuarto.transform.Find("TarjetaDetalleTecnicoCuarto");
                if (cardPrevia != null) Destroy(cardPrevia.gameObject);
            }

            // Asegurar que contenedorSlotsHabilidades use VerticalLayoutGroup sin interferencias
            var hlg = contenedorSlotsHabilidades.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Destroy(hlg);

            var vlg = contenedorSlotsHabilidades.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = contenedorSlotsHabilidades.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var rtContHabs = contenedorSlotsHabilidades.GetComponent<RectTransform>();
            if (rtContHabs != null)
            {
                rtContHabs.anchoredPosition = new Vector2(0f, -20f);
                rtContHabs.sizeDelta = new Vector2(620f, 320f);
            }

            // Limpiar slots viejos
            foreach (Transform hijo in contenedorSlotsHabilidades)
            {
                Destroy(hijo.gameObject);
            }

            // Obtener fuente pixel art del juego
            Font fuentePixel = FuenteUtil.ObtenerFuentePixelify();

            // Obtener habilidades del personaje
            var todasHabilidades = CatalogoHabilidades.ObtenerHabilidadesPersonaje(pj);
            if (_habilidadSeleccionadaCuarto == IdHabilidad.Ninguna && todasHabilidades.Count > 0)
            {
                _habilidadSeleccionadaCuarto = todasHabilidades[0].id;
            }

            string[] teclas = new string[] { "CLICK IZQ", "E", "Q", "R" };

            // Construir lista vertical: Icono grande, nombre a la derecha con tipografía pixel y descripción que no se solapa
            for (int i = 0; i < todasHabilidades.Count && i < 4; i++)
            {
                var hab = todasHabilidades[i];
                bool seleccionada = (_habilidadSeleccionadaCuarto == hab.id);
                string teclaStr = i < teclas.Length ? teclas[i] : "HAB";
                string desc = !string.IsNullOrEmpty(hab.descripcionTecnica) ? hab.descripcionTecnica : hab.descripcion;

                var slotObj = new GameObject($"SlotHab_{i}");
                slotObj.transform.SetParent(contenedorSlotsHabilidades, false);

                var rtSlot = slotObj.AddComponent<RectTransform>();
                float altoSlot = seleccionada ? 124f : 54f;
                rtSlot.sizeDelta = new Vector2(620f, altoSlot);

                var le = slotObj.AddComponent<LayoutElement>();
                le.preferredHeight = altoSlot;
                le.minHeight = altoSlot;
                le.flexibleWidth = 1f;

                // Borde exterior resaltado al seleccionar
                var imgBorde = slotObj.AddComponent<Image>();
                imgBorde.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                imgBorde.type = Image.Type.Sliced;
                imgBorde.color = seleccionada ? new Color(0.35f, 0.85f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f, 0.9f);

                // Fondo interior con marco pixel-art
                var fondoInt = new GameObject("FondoInterno");
                fondoInt.transform.SetParent(slotObj.transform, false);
                var rtFi = fondoInt.AddComponent<RectTransform>();
                rtFi.anchorMin = Vector2.zero;
                rtFi.anchorMax = Vector2.one;
                rtFi.offsetMin = new Vector2(3f, 3f);
                rtFi.offsetMax = new Vector2(-3f, -3f);
                var imgFi = fondoInt.AddComponent<Image>();
                imgFi.color = seleccionada
                    ? new Color(0.12f, 0.18f, 0.32f, 0.98f)
                    : new Color(0.08f, 0.10f, 0.16f, 0.95f);
                imgFi.raycastTarget = false;

                // 1. Icono grande a la izquierda (48x48) con marco propio
                var icoMarco = new GameObject("IconoMarco");
                icoMarco.transform.SetParent(fondoInt.transform, false);
                var rtIcoMarco = icoMarco.AddComponent<RectTransform>();
                rtIcoMarco.anchorMin = new Vector2(0f, 0.5f);
                rtIcoMarco.anchorMax = new Vector2(0f, 0.5f);
                rtIcoMarco.pivot = new Vector2(0f, 0.5f);
                rtIcoMarco.anchoredPosition = new Vector2(10f, 0f);
                rtIcoMarco.sizeDelta = new Vector2(48f, 48f);

                var imgMarco = icoMarco.AddComponent<Image>();
                imgMarco.color = seleccionada
                    ? new Color(0.25f, 0.65f, 0.90f, 0.85f)
                    : new Color(0.16f, 0.20f, 0.28f, 0.80f);
                imgMarco.raycastTarget = false;

                var icoImgObj = new GameObject("Icono");
                icoImgObj.transform.SetParent(icoMarco.transform, false);
                var rtIco = icoImgObj.AddComponent<RectTransform>();
                rtIco.anchorMin = Vector2.zero;
                rtIco.anchorMax = Vector2.one;
                rtIco.offsetMin = new Vector2(2f, 2f);
                rtIco.offsetMax = new Vector2(-2f, -2f);

                var imgIco = icoImgObj.AddComponent<Image>();
                imgIco.sprite = CatalogoHabilidades.ObtenerSpriteHabilidad(hab.id);
                imgIco.preserveAspect = true;
                imgIco.color = Color.white;
                imgIco.raycastTarget = false;

                // 2. Textos
                var txtNombreObj = new GameObject("TextoNombre");
                txtNombreObj.transform.SetParent(fondoInt.transform, false);
                var rtTxtNom = txtNombreObj.AddComponent<RectTransform>();

                var txtNom = txtNombreObj.AddComponent<Text>();
                txtNom.font = fuentePixel;
                txtNom.fontSize = 15;
                txtNom.fontStyle = FontStyle.Bold;
                txtNom.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtNom.verticalOverflow = VerticalWrapMode.Overflow;
                txtNom.raycastTarget = false;

                if (!seleccionada)
                {
                    rtTxtNom.anchorMin = new Vector2(0f, 0f);
                    rtTxtNom.anchorMax = new Vector2(1f, 1f);
                    rtTxtNom.offsetMin = new Vector2(70f, 0f);
                    rtTxtNom.offsetMax = new Vector2(-12f, 0f);
                    txtNom.alignment = TextAnchor.MiddleLeft;
                    txtNom.color = EstiloUIUX.ColTextoCrema;
                    txtNom.text = $"{hab.nombre.ToUpper()}  <color=#ffcf4a>({teclaStr})</color>";
                }
                else
                {
                    rtTxtNom.anchorMin = new Vector2(0f, 1f);
                    rtTxtNom.anchorMax = new Vector2(1f, 1f);
                    rtTxtNom.pivot = new Vector2(0f, 1f);
                    rtTxtNom.anchoredPosition = new Vector2(70f, -10f);
                    rtTxtNom.sizeDelta = new Vector2(-80f, 24f);
                    txtNom.alignment = TextAnchor.MiddleLeft;
                    txtNom.color = EstiloUIUX.ColCyan;
                    txtNom.text = $"{hab.nombre.ToUpper()}  <color=#ffcf4a>({teclaStr})</color>";

                    // Descripción técnica
                    var txtDescObj = new GameObject("TextoDescripcion");
                    txtDescObj.transform.SetParent(fondoInt.transform, false);
                    var rtTxtDesc = txtDescObj.AddComponent<RectTransform>();
                    rtTxtDesc.anchorMin = new Vector2(0f, 0f);
                    rtTxtDesc.anchorMax = new Vector2(1f, 1f);
                    rtTxtDesc.pivot = new Vector2(0f, 0f);
                    rtTxtDesc.offsetMin = new Vector2(70f, 8f);
                    rtTxtDesc.offsetMax = new Vector2(-14f, -38f);

                    var txtDesc = txtDescObj.AddComponent<Text>();
                    txtDesc.font = fuentePixel;
                    txtDesc.fontSize = 13;
                    txtDesc.lineSpacing = 1.35f;
                    txtDesc.alignment = TextAnchor.UpperLeft;
                    txtDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                    txtDesc.verticalOverflow = VerticalWrapMode.Overflow;
                    txtDesc.color = EstiloUIUX.ColTextoTenue;
                    txtDesc.raycastTarget = false;
                    txtDesc.text = desc;
                }

                // Botón interactivo para desplegar al hacer clic
                var btn = slotObj.AddComponent<Button>();
                IdHabilidad idClick = hab.id;
                btn.onClick.AddListener(() =>
                {
                    _habilidadSeleccionadaCuarto = idClick;
                    ConstruirVistaCuarto(pj);
                });
            }
        }

        public void MostrarExplicacionMaestriaLobby()
        {
            if (_modalMaestriaLobby == null)
            {
                _modalMaestriaLobby = new GameObject("ModalExplicacionMaestriaLobby");
                Canvas canvasDestino = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
                if (canvasDestino != null) _modalMaestriaLobby.transform.SetParent(canvasDestino.transform, false);
                _modalMaestriaLobby.transform.SetAsLastSibling();

                var rt = _modalMaestriaLobby.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                var imgFondo = _modalMaestriaLobby.AddComponent<Image>();
                imgFondo.color = new Color(0f, 0f, 0f, 0.88f);

                var cardObj = new GameObject("Tarjeta");
                cardObj.transform.SetParent(_modalMaestriaLobby.transform, false);
                var rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                rtCard.pivot = new Vector2(0.5f, 0.5f);
                rtCard.anchoredPosition = Vector2.zero;
                rtCard.sizeDelta = new Vector2(640f, 440f);

                var imgCard = cardObj.AddComponent<Image>();
                imgCard.sprite = EstiloUIUX.ObtenerSpritePanel();
                imgCard.type = Image.Type.Sliced;
                imgCard.color = Color.white;

                EstiloUIUX.CrearRemaches(cardObj.transform, 16f);
                EstiloUIUX.CrearCabeceraModal(cardObj.transform, "GUÍA DE MAESTRÍA", 70f, "huella");

                // Cuerpo
                var cuerpoObj = new GameObject("TextoCuerpo");
                cuerpoObj.transform.SetParent(cardObj.transform, false);
                var rtCuerpo = cuerpoObj.AddComponent<RectTransform>();
                rtCuerpo.anchorMin = new Vector2(0f, 0f);
                rtCuerpo.anchorMax = new Vector2(1f, 1f);
                rtCuerpo.offsetMin = new Vector2(30f, 75f);
                rtCuerpo.offsetMax = new Vector2(-30f, -80f);
                var txtCuerpo = cuerpoObj.AddComponent<Text>();
                txtCuerpo.font = FuenteUtil.ObtenerFuentePixelify();
                txtCuerpo.fontSize = 15;
                txtCuerpo.lineSpacing = 1.35f;
                txtCuerpo.alignment = TextAnchor.UpperLeft;
                txtCuerpo.color = EstiloUIUX.ColTextoCrema;
                txtCuerpo.horizontalOverflow = HorizontalWrapMode.Wrap;
                txtCuerpo.verticalOverflow = VerticalWrapMode.Overflow;
                txtCuerpo.text =
                    "• La Maestría es PERMANENTE en tu cuenta y se conserva para siempre.\n\n" +
                    "• Ganas XP de maestría eliminando monstruos y jefes en mazmorras (~35% de la XP de combate).\n\n" +
                    "• Desbloqueo progresivo de habilidades por personaje:\n" +
                    "   <color=#8fe0ff>Nv. 1:</color> Habilidad Básica (Click Izquierdo)\n" +
                    "   <color=#8fe0ff>Nv. 2:</color> Habilidad Activa 1 (Tecla E)\n" +
                    "   <color=#8fe0ff>Nv. 3:</color> Habilidad Activa 2 (Tecla Q)\n" +
                    "   <color=#8fe0ff>Nv. 4:</color> Habilidad Definitiva (Tecla R / Ultimate)\n" +
                    "   <color=#ffcf4a>Nv. 5 a 20:</color> Bonificaciones de maestría de cuenta.\n\n" +
                    "¡Completa partidas para alcanzar el nivel máximo y dominar a todos los gatos!";

                // Botón Cerrar
                var btn = EstiloUIUX.CrearBoton(cardObj.transform, "BotonCerrar", "ENTENDIDO", TipoBotonUI.Piedra, new Vector2(220f, 48f), () =>
                {
                    if (_modalMaestriaLobby != null) _modalMaestriaLobby.SetActive(false);
                }, "palomita", new Vector2(22f, 16f));

                var rtBtn = btn.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 18f);
            }

            _modalMaestriaLobby.SetActive(true);
            _modalMaestriaLobby.transform.SetAsLastSibling();
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

        private Image _fadeNegroLobby;

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

            SalaRed.Instancia.IniciarFadeNegroLobbyClientRpc();
            StartCoroutine(RutinaIniciarPartidaConFade());
        }

        public void IniciarFadeNegroLobby()
        {
            StartCoroutine(RutinaFadeNegroLobbySoloVisual());
        }

        private System.Collections.IEnumerator RutinaFadeNegroLobbySoloVisual()
        {
            Controles.Bloqueado = true;
            AsegurarFadeNegroLobby();

            float t = 0f;
            float dur = 0.8f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f, t / dur);
                if (_fadeNegroLobby != null)
                {
                    _fadeNegroLobby.color = new Color(0f, 0f, 0f, alpha);
                }
                yield return null;
            }

            if (_fadeNegroLobby != null) _fadeNegroLobby.color = Color.black;
        }

        private System.Collections.IEnumerator RutinaIniciarPartidaConFade()
        {
            Controles.Bloqueado = true;
            AsegurarFadeNegroLobby();

            float t = 0f;
            float dur = 0.8f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f, t / dur);
                if (_fadeNegroLobby != null)
                {
                    _fadeNegroLobby.color = new Color(0f, 0f, 0f, alpha);
                }
                yield return null;
            }

            if (_fadeNegroLobby != null) _fadeNegroLobby.color = Color.black;
            yield return new WaitForSeconds(0.25f);

            foreach (var p in Personaje.Todos)
            {
                if (p != null)
                {
                    p.LimpiarEstadosYHabilidades();
                }
            }
            GestorAudio.DetenerTodosLosSFX();

            SalaRed.Instancia.IniciarPartida();
        }

        private void AsegurarFadeNegroLobby()
        {
            if (_fadeNegroLobby != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var obj = new GameObject("PanelFadeNegroLobby");
            obj.transform.SetParent(canvas.transform, false);
            obj.transform.SetAsLastSibling();

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            _fadeNegroLobby = obj.AddComponent<Image>();
            _fadeNegroLobby.color = new Color(0f, 0f, 0f, 0f);
            _fadeNegroLobby.raycastTarget = true;
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
