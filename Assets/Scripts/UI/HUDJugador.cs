using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    [Serializable]
    public class SlotHabilidadHUD
    {
        public GameObject contenedor;
        public Image icono;
        public Image sombraCooldown;
        public Text textoTiempo;
        public Text textoTecla;
    }

    /// <summary>
    /// HUD en pantalla durante el juego:
    /// - Barra de vida del personaje local abajo al centro sujeta a la cámara.
    /// - Iconos de habilidades con enfriamientos radiales (cooldowns) abajo al centro.
    /// - Menús emergentes: Menú de Pausa (tecla ESC) y Fin de Partida (Victoria / Derrota).
    /// </summary>
    public class HUDJugador : MonoBehaviour
    {
        public static HUDJugador Instancia { get; private set; }

        [Header("Vida")]
        public Text textoMiNombreHUD;
        public BarraVida barraVida;

        [Header("Barras de Progresión")]
        public BarraVida barraMaestria; // Barra de nivel de maestría abajo de la barra de vida
        public BarraVida barraXPRun;    // Barra de experiencia sobre la barra de habilidades (run actual)
        public Text textoOroRun;        // Contador de oro actual en la run

        [Header("Slots de Habilidades")]
        public SlotHabilidadHUD[] slotsHabilidades;
        public Image[] iconosHabilidades; // Compatibilidad con referencias directas

        [Header("Sprites de Habilidades - Fuego")]
        public Sprite iconoFuegoLMB;
        public Sprite iconoFuegoE;
        public Sprite iconoFuegoQ;
        public Sprite iconoFuegoR;

        [Header("Sprites de Habilidades - Hielo")]
        public Sprite iconoHieloLMB;
        public Sprite iconoHieloE;
        public Sprite iconoHieloQ;
        public Sprite iconoHieloR;

        [Header("Sprites de Habilidades - Eléctrico")]
        public Sprite iconoElecLMB;
        public Sprite iconoElecE;
        public Sprite iconoElecQ;
        public Sprite iconoElecR;

        [Header("Botón de Pausa HUD")]
        public Button botonPausaHUD;

        [Header("Menú de Pausa")]
        public GameObject panelPausa;
        public Button botonReanudar;
        public Button botonControlesPausa;
        public Button botonVolverLobbyPausa;
        public Button botonSalirPartida;

        [Header("Menú Fin de Partida")]
        public GameObject panelFinPartida;
        public Text textoResultado; // "¡Victoria!" o "Has muerto..."
        public Button botonVolverLobby;

        [Header("Votación Volver al Lobby (Estilo LoL)")]
        public GameObject panelVotacion;
        public Text textoTituloVotacion;
        public Text textoEstadoVotos;
        public Text textoTemporizadorVotacion;
        public Button botonVotarSi;
        public Button botonVotarNo;
        private bool _yaVoteLocalmente = false;

        [Header("Tienda Secreta Modal")]
        public GameObject panelTienda;
        public bool TiendaAbierta => panelTienda != null && panelTienda.activeSelf;
        private PuestitoTienda _puestitoActivo;
        public PuestitoTienda PuestitoActivo => _puestitoActivo;
        private Text _textoOroTienda;
        private readonly List<Button> _botonesCompraTienda = new List<Button>();
        private readonly List<Text> _textosBotonTienda = new List<Text>();

        [Header("Menú de Pausa Mejorado")]
        private RawImage _rawMapaPausa;
        private Text _textoDescHabilidadPausa;
        private readonly List<Image> _iconosHabilidadesPausa = new List<Image>();
        private readonly List<Image> _bordesHabilidadesPausa = new List<Image>();
        private readonly List<Button> _botonesHabilidadesPausa = new List<Button>();
        private int _habilidadSeleccionadaPausa = 0;
        private bool _menuPausaPrecargado = false;
        private GameObject _modalMaestriaHUD;

        public static bool EnTransicionCarga = true;
        [Header("Transición de Nivel (Fade a Negro)")]
        public Image panelFadeNegro;
        public Text textoBannerNivel;
        public Text textoAvisoPasaje;

        [Header("Barra de Jefe (Estilo Dark Souls) y Minimapa")]
        public GameObject panelJefeDarkSouls;
        public Text textoNombreJefeDS;
        public Image imagenRellenoJefeDS;
        public Text textoVidaJefeDS;
        public MinimapaHUD minimapaHUD;

        private Coroutine _rutinaFade;
        private float _tiempoAvisoPasaje = 0f;

        private Personaje _personajeLocal;

        void Awake()
        {
            Instancia = this;
            if (panelPausa) panelPausa.SetActive(false);
            if (panelFinPartida) panelFinPartida.SetActive(false);

            if (botonPausaHUD)
            {
                botonPausaHUD.onClick.AddListener(AlternarPausa);
                // Asegurar que el botón de pausa esté arriba del minimapa (y = -12) y sea un botón cuadrado limpio con 3 rayas
                var rtContPausa = botonPausaHUD.transform.parent as RectTransform;
                if (rtContPausa != null && rtContPausa.name == "ContenedorHUD_ArribaDerecha")
                {
                    rtContPausa.anchorMin = new Vector2(1f, 1f);
                    rtContPausa.anchorMax = new Vector2(1f, 1f);
                    rtContPausa.pivot = new Vector2(1f, 1f);
                    rtContPausa.anchoredPosition = new Vector2(-16f, -12f);
                    rtContPausa.sizeDelta = new Vector2(40f, 40f);
                }

                var bRt = botonPausaHUD.GetComponent<RectTransform>();
                if (bRt != null)
                {
                    bRt.sizeDelta = new Vector2(38f, 38f);
                }

                var txtPausa = botonPausaHUD.GetComponentInChildren<Text>();
                if (txtPausa != null)
                {
                    txtPausa.text = "☰";
                    txtPausa.fontSize = 24;
                    txtPausa.fontStyle = FontStyle.Bold;
                    txtPausa.alignment = TextAnchor.MiddleCenter;
                }
            }
            if (botonReanudar) botonReanudar.onClick.AddListener(CerrarPausa);
            if (botonVolverLobbyPausa) botonVolverLobbyPausa.onClick.AddListener(VolverAlLobbyDesdePausa);
            if (botonSalirPartida) botonSalirPartida.onClick.AddListener(SalirAlMenuPrincipal);
            if (botonVolverLobby) botonVolverLobby.onClick.AddListener(VolverAlLobbyFinPartidaIndividual);

            try { AsegurarTextoMiNombreHUD(); } catch (System.Exception e) { Debug.LogError($"[HUD] Error AsegurarTextoMiNombreHUD: {e}"); }
            try { AsegurarLayoutInferior(); } catch (System.Exception e) { Debug.LogError($"[HUD] Error AsegurarLayoutInferior: {e}"); }
            try { AsegurarTextoOroRun(); } catch (System.Exception e) { Debug.LogError($"[HUD] Error AsegurarTextoOroRun: {e}"); }
            try { ConfigurarLayoutMenuPausa(); } catch (System.Exception e) { Debug.LogError($"[HUD] Error ConfigurarLayoutMenuPausa: {e}"); }

            // Iniciar las barras de progresión en 0 con su texto de nivel correspondiente
            if (barraVida != null)
            {
                barraVida.Establecer(100, 100, false);
                if (barraVida.textoVida != null)
                {
                    barraVida.textoVida.text = "100 / 100 HP";
                }
            }
            if (barraMaestria != null)
            {
                barraMaestria.Establecer(0, GestorMaestria.XP_POR_NIVEL_MAESTRIA, false);
                if (barraMaestria.textoVida != null)
                {
                    barraMaestria.textoVida.text = $"Maestría Nv.1 (0/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP)";
                }
            }
            if (barraXPRun != null)
            {
                barraXPRun.Establecer(0, 100, false);
                if (barraXPRun.textoVida != null)
                {
                    barraXPRun.textoVida.text = "Nv.1 (0/100 XP)";
                }
            }

            GestorMaestria.AlGanarMaestria += OnMaestriaActualizada;

            // Crear dinámicamente el panel de fade a negro si no está asignado
            if (panelFadeNegro == null)
            {
                var fadeObj = new GameObject("PanelFadeNegro");
                fadeObj.transform.SetParent(transform, false);
                fadeObj.transform.SetAsLastSibling(); // Por encima de todo
                panelFadeNegro = fadeObj.AddComponent<Image>();
                panelFadeNegro.color = Color.black;
                panelFadeNegro.raycastTarget = true;
                fadeObj.SetActive(true);

                var rt = fadeObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }
            else
            {
                panelFadeNegro.color = Color.black;
                panelFadeNegro.raycastTarget = true;
                panelFadeNegro.gameObject.SetActive(true);
            }

            // Crear dinámicamente texto de aviso de pasaje si no existe
            if (textoAvisoPasaje == null)
            {
                var avisoObj = new GameObject("TextoAvisoPasaje");
                avisoObj.transform.SetParent(transform, false);
                textoAvisoPasaje = avisoObj.AddComponent<Text>();
                textoAvisoPasaje.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoAvisoPasaje.fontSize = 24;
                textoAvisoPasaje.alignment = TextAnchor.MiddleCenter;
                textoAvisoPasaje.color = Color.yellow;
                textoAvisoPasaje.raycastTarget = false;
                var rt = avisoObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 160);
                rt.sizeDelta = new Vector2(600, 50);
                avisoObj.SetActive(false);
            }

            // Crear dinámicamente banner de nuevo nivel
            if (textoBannerNivel == null)
            {
                var banObj = new GameObject("TextoBannerNivel");
                banObj.transform.SetParent(transform, false);
                textoBannerNivel = banObj.AddComponent<Text>();
                textoBannerNivel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoBannerNivel.fontSize = 42;
                textoBannerNivel.alignment = TextAnchor.MiddleCenter;
                textoBannerNivel.color = Color.cyan;
                textoBannerNivel.raycastTarget = false;
                var rt = banObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 40);
                rt.sizeDelta = new Vector2(800, 100);
                banObj.SetActive(false);
            }

            // Crear sprite blanco plano para las sombras radiales
            Texture2D texBlanca = Texture2D.whiteTexture;
            Sprite spriteBlanco = Sprite.Create(texBlanca, new Rect(0, 0, texBlanca.width, texBlanca.height), new Vector2(0.5f, 0.5f));

            if (slotsHabilidades != null)
            {
                foreach (var s in slotsHabilidades)
                {
                    if (s == null) continue;
                    if (s.icono != null)
                    {
                        s.icono.color = Color.white; // 100% brillante y crudo
                    }
                    if (s.sombraCooldown != null)
                    {
                        if (s.sombraCooldown.sprite == null)
                        {
                            s.sombraCooldown.sprite = spriteBlanco;
                        }
                        s.sombraCooldown.color = new Color(0f, 0f, 0f, 0.75f);
                        s.sombraCooldown.fillAmount = 0f;
                        s.sombraCooldown.gameObject.SetActive(false); // Iniciar apagada para no oscurecer
                    }
                    if (s.textoTiempo != null)
                    {
                        s.textoTiempo.text = "";
                    }
                }
            }

            // Asegurar carga de sprites de habilidades para no depender de dependencias vacías
            if (iconoFuegoLMB == null) iconoFuegoLMB = Resources.Load<Sprite>("IconosHabilidades/Fuego_BolaFuego");
            if (iconoFuegoE == null) iconoFuegoE = Resources.Load<Sprite>("IconosHabilidades/Fuego_Lanzallamas");
            if (iconoFuegoQ == null) iconoFuegoQ = Resources.Load<Sprite>("IconosHabilidades/Fuego_Armadura");
            if (iconoFuegoR == null) iconoFuegoR = Resources.Load<Sprite>("IconosHabilidades/Fuego_Meteorito");

            if (iconoHieloLMB == null) iconoHieloLMB = Resources.Load<Sprite>("IconosHabilidades/Hielo_RayoHielo");
            if (iconoHieloE == null) iconoHieloE = Resources.Load<Sprite>("IconosHabilidades/Hielo_BolaHielo");
            if (iconoHieloQ == null) iconoHieloQ = Resources.Load<Sprite>("IconosHabilidades/Hielo_Armadura");
            if (iconoHieloR == null) iconoHieloR = Resources.Load<Sprite>("IconosHabilidades/Hielo_EraHielo");

            if (iconoElecLMB == null) iconoElecLMB = Resources.Load<Sprite>("IconosHabilidades/Elec_Espada");
            if (iconoElecE == null) iconoElecE = Resources.Load<Sprite>("IconosHabilidades/Elec_Dash");
            if (iconoElecQ == null) iconoElecQ = Resources.Load<Sprite>("IconosHabilidades/Elec_Teleport");
            if (iconoElecR == null) iconoElecR = Resources.Load<Sprite>("IconosHabilidades/Elec_Rayo");

            // Crear dinámicamente panel de votación estilo League of Legends si no existe
            CrearPanelVotacionSiNoExiste();

            // Crear Minimapa arriba a la derecha (inicia estrictamente oculto durante la carga)
            if (minimapaHUD == null)
            {
                minimapaHUD = MinimapaHUD.CrearEnHUD(transform);
            }
            if (minimapaHUD != null)
            {
                minimapaHUD.gameObject.SetActive(false);
            }

            // Crear Barra de Jefe estilo Dark Souls arriba al centro
            CrearBarraJefeDarkSoulsSiNoExiste(spriteBlanco);

            // Asegurar que la pantalla de fade a negro esté por encima de absolutamente todos los elementos del HUD
            if (panelFadeNegro != null)
            {
                panelFadeNegro.transform.SetAsLastSibling();
                panelFadeNegro.color = Color.black;
                panelFadeNegro.gameObject.SetActive(true);
                panelFadeNegro.raycastTarget = true;
            }
            if (textoBannerNivel != null)
            {
                textoBannerNivel.transform.SetAsLastSibling();
            }

            // Ocultar botón de volver al lobby en pausa si no es servidor
            ActualizarVisibilidadBotonLobbyPausa();

            // Pantalla negra inicial cinematográfica: esperar a que todo esté 100% generado y cargado
            StartCoroutine(RutinaCargaInicialMazmorra());
        }

        private void CrearBarraJefeDarkSoulsSiNoExiste(Sprite spriteBlanco)
        {
            if (panelJefeDarkSouls != null) return;

            var objJefe = new GameObject("PanelJefeDarkSouls");
            objJefe.transform.SetParent(transform, false);

            var rtJefe = objJefe.GetComponent<RectTransform>();
            if (rtJefe == null) rtJefe = objJefe.AddComponent<RectTransform>();
            rtJefe.anchorMin = new Vector2(0.5f, 1f);
            rtJefe.anchorMax = new Vector2(0.5f, 1f);
            rtJefe.pivot = new Vector2(0.5f, 1f);
            rtJefe.anchoredPosition = new Vector2(0f, -18f);
            rtJefe.sizeDelta = new Vector2(640f, 68f);

            // Nombre del Jefe arriba de la barra (Estilo Dark Souls)
            var nomObj = new GameObject("NombreJefeDS");
            nomObj.transform.SetParent(objJefe.transform, false);
            textoNombreJefeDS = nomObj.AddComponent<Text>();
            textoNombreJefeDS.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoNombreJefeDS.fontSize = 20;
            textoNombreJefeDS.fontStyle = FontStyle.Bold;
            textoNombreJefeDS.color = new Color(0.95f, 0.88f, 0.72f, 1f);
            textoNombreJefeDS.alignment = TextAnchor.MiddleCenter;
            textoNombreJefeDS.text = "JEFE";
            var rtNom = nomObj.GetComponent<RectTransform>();
            rtNom.anchorMin = new Vector2(0f, 1f);
            rtNom.anchorMax = new Vector2(1f, 1f);
            rtNom.pivot = new Vector2(0.5f, 1f);
            rtNom.anchoredPosition = new Vector2(0f, 0f);
            rtNom.sizeDelta = new Vector2(0f, 28f);

            // Marco dorado oscuro de la barra
            var marcoBarra = new GameObject("MarcoBarraJefe");
            marcoBarra.transform.SetParent(objJefe.transform, false);
            var imgMarco = marcoBarra.AddComponent<Image>();
            imgMarco.color = new Color(0.35f, 0.28f, 0.15f, 0.95f);
            var rtMarco = marcoBarra.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(0f, 0f);
            rtMarco.anchorMax = new Vector2(1f, 0f);
            rtMarco.pivot = new Vector2(0.5f, 0f);
            rtMarco.anchoredPosition = new Vector2(0f, 6f);
            rtMarco.sizeDelta = new Vector2(0f, 24f);

            // Fondo negro interior
            var fondoBarra = new GameObject("FondoBarraJefe");
            fondoBarra.transform.SetParent(marcoBarra.transform, false);
            var imgFondo = fondoBarra.AddComponent<Image>();
            imgFondo.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);
            var rtFondo = fondoBarra.GetComponent<RectTransform>();
            rtFondo.anchorMin = Vector2.zero;
            rtFondo.anchorMax = Vector2.one;
            rtFondo.offsetMin = new Vector2(3f, 3f);
            rtFondo.offsetMax = new Vector2(-3f, -3f);

            // Relleno carmesí Dark Souls
            var rellenoObj = new GameObject("RellenoBarraJefe");
            rellenoObj.transform.SetParent(fondoBarra.transform, false);
            imagenRellenoJefeDS = rellenoObj.AddComponent<Image>();
            imagenRellenoJefeDS.sprite = spriteBlanco;
            imagenRellenoJefeDS.type = Image.Type.Filled;
            imagenRellenoJefeDS.fillMethod = Image.FillMethod.Horizontal;
            imagenRellenoJefeDS.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenRellenoJefeDS.color = new Color(0.68f, 0.10f, 0.12f, 1f);
            imagenRellenoJefeDS.fillAmount = 1f;
            var rtRelleno = rellenoObj.GetComponent<RectTransform>();
            rtRelleno.anchorMin = Vector2.zero;
            rtRelleno.anchorMax = Vector2.one;
            rtRelleno.sizeDelta = Vector2.zero;

            // Texto numérico de HP sobre la barra
            var txtHpObj = new GameObject("TextoHPJefe");
            txtHpObj.transform.SetParent(fondoBarra.transform, false);
            textoVidaJefeDS = txtHpObj.AddComponent<Text>();
            textoVidaJefeDS.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoVidaJefeDS.fontSize = 12;
            textoVidaJefeDS.fontStyle = FontStyle.Bold;
            textoVidaJefeDS.color = new Color(1f, 0.95f, 0.9f, 0.9f);
            textoVidaJefeDS.alignment = TextAnchor.MiddleCenter;
            var rtTxtHp = txtHpObj.GetComponent<RectTransform>();
            rtTxtHp.anchorMin = Vector2.zero;
            rtTxtHp.anchorMax = Vector2.one;
            rtTxtHp.sizeDelta = Vector2.zero;

            panelJefeDarkSouls = objJefe;
            panelJefeDarkSouls.SetActive(false);
        }

        public void MostrarBarraJefeDarkSouls(string nombre, int actual, int max)
        {
            if (panelJefeDarkSouls == null) return;
            panelJefeDarkSouls.SetActive(true);
            if (textoNombreJefeDS != null)
            {
                textoNombreJefeDS.text = nombre.ToUpper();
            }
            ActualizarBarraJefeDarkSouls(actual, max);
        }

        public void ActualizarBarraJefeDarkSouls(int actual, int max)
        {
            if (panelJefeDarkSouls == null) return;
            if (!panelJefeDarkSouls.activeSelf && actual > 0)
            {
                panelJefeDarkSouls.SetActive(true);
            }
            int m = Mathf.Max(1, max);
            int a = Mathf.Clamp(actual, 0, m);
            float ratio = Mathf.Clamp01((float)a / m);
            if (imagenRellenoJefeDS != null)
            {
                imagenRellenoJefeDS.fillAmount = ratio;
                var rt = imagenRellenoJefeDS.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(ratio, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            if (textoVidaJefeDS != null)
            {
                textoVidaJefeDS.text = $"{a} / {m}";
            }
            if (a <= 0)
            {
                OcultarBarraJefeDarkSouls();
            }
        }

        public void OcultarBarraJefeDarkSouls()
        {
            if (panelJefeDarkSouls != null)
            {
                panelJefeDarkSouls.SetActive(false);
            }
        }

        private void ActualizarVisibilidadBotonLobbyPausa()
        {
            if (botonVolverLobbyPausa != null)
            {
                botonVolverLobbyPausa.gameObject.SetActive(true);
            }
        }

        private void CrearPanelVotacionSiNoExiste()
        {
            if (panelVotacion != null) return;

            var votObj = new GameObject("PanelVotacionLobby");
            votObj.transform.SetParent(transform, false);
            var imgFondo = votObj.AddComponent<Image>();
            imgFondo.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            var rt = votObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 180);
            rt.sizeDelta = new Vector2(460, 150);

            // Título
            var objTit = new GameObject("TextoTitulo");
            objTit.transform.SetParent(votObj.transform, false);
            textoTituloVotacion = objTit.AddComponent<Text>();
            textoTituloVotacion.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoTituloVotacion.fontSize = 17;
            textoTituloVotacion.fontStyle = FontStyle.Bold;
            textoTituloVotacion.color = new Color(1f, 0.85f, 0.3f);
            textoTituloVotacion.alignment = TextAnchor.MiddleCenter;
            textoTituloVotacion.text = "🗳️ ¿REGRESAR AL LOBBY?";
            var rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchoredPosition = new Vector2(0, 48);
            rtTit.sizeDelta = new Vector2(440, 32);

            // Estado de votos (Verdes / Rojos)
            var objVotos = new GameObject("TextoEstadoVotos");
            objVotos.transform.SetParent(votObj.transform, false);
            textoEstadoVotos = objVotos.AddComponent<Text>();
            textoEstadoVotos.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoEstadoVotos.fontSize = 15;
            textoEstadoVotos.color = Color.white;
            textoEstadoVotos.alignment = TextAnchor.MiddleCenter;
            textoEstadoVotos.text = "Votos: [0 SÍ] vs [0 NO]";
            var rtVotos = objVotos.GetComponent<RectTransform>();
            rtVotos.anchoredPosition = new Vector2(0, 14);
            rtVotos.sizeDelta = new Vector2(440, 28);

            // Temporizador
            var objTiempo = new GameObject("TextoTiempo");
            objTiempo.transform.SetParent(votObj.transform, false);
            textoTemporizadorVotacion = objTiempo.AddComponent<Text>();
            textoTemporizadorVotacion.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoTemporizadorVotacion.fontSize = 13;
            textoTemporizadorVotacion.color = new Color(0.7f, 0.7f, 0.7f);
            textoTemporizadorVotacion.alignment = TextAnchor.MiddleCenter;
            textoTemporizadorVotacion.text = "Tiempo restante: 15s";
            var rtTiempo = objTiempo.GetComponent<RectTransform>();
            rtTiempo.anchoredPosition = new Vector2(0, -14);
            rtTiempo.sizeDelta = new Vector2(440, 24);

            // Botón SÍ [F1]
            var objBtnSi = new GameObject("BotonSi");
            objBtnSi.transform.SetParent(votObj.transform, false);
            var imgSi = objBtnSi.AddComponent<Image>();
            imgSi.color = new Color(0.18f, 0.58f, 0.28f);
            botonVotarSi = objBtnSi.AddComponent<Button>();
            var rtSi = objBtnSi.GetComponent<RectTransform>();
            rtSi.anchoredPosition = new Vector2(-110, -48);
            rtSi.sizeDelta = new Vector2(170, 34);

            var objTxtSi = new GameObject("Txt");
            objTxtSi.transform.SetParent(objBtnSi.transform, false);
            var txtSi = objTxtSi.AddComponent<Text>();
            txtSi.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSi.fontSize = 13;
            txtSi.fontStyle = FontStyle.Bold;
            txtSi.color = Color.white;
            txtSi.alignment = TextAnchor.MiddleCenter;
            txtSi.text = "SÍ  [F1]";
            objTxtSi.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 34);

            botonVotarSi.onClick.AddListener(() => Votar(true));

            // Botón NO [F2]
            var objBtnNo = new GameObject("BotonNo");
            objBtnNo.transform.SetParent(votObj.transform, false);
            var imgNo = objBtnNo.AddComponent<Image>();
            imgNo.color = new Color(0.68f, 0.22f, 0.22f);
            botonVotarNo = objBtnNo.AddComponent<Button>();
            var rtNo = objBtnNo.GetComponent<RectTransform>();
            rtNo.anchoredPosition = new Vector2(110, -48);
            rtNo.sizeDelta = new Vector2(170, 34);

            var objTxtNo = new GameObject("Txt");
            objTxtNo.transform.SetParent(objBtnNo.transform, false);
            var txtNo = objTxtNo.AddComponent<Text>();
            txtNo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtNo.fontSize = 13;
            txtNo.fontStyle = FontStyle.Bold;
            txtNo.color = Color.white;
            txtNo.alignment = TextAnchor.MiddleCenter;
            txtNo.text = "NO  [F2]";
            objTxtNo.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 34);

            botonVotarNo.onClick.AddListener(() => Votar(false));

            panelVotacion = votObj;
            panelVotacion.SetActive(false);
        }

        void Start()
        {
            SalaRed.PartidaTerminada += AlTerminarPartida;
            ActualizarVisibilidadBotonLobbyPausa();
            AplicarTipografiasPixelArt();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                PausarPorPerdidaFoco();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PausarPorPerdidaFoco();
            }
        }

        private void PausarPorPerdidaFoco()
        {
            // Solo pausar si ha pasado un breve tiempo desde que cargó el nivel (evitar pausa en arranque)
            if (Time.timeSinceLevelLoad < 2.0f) return;
            if (panelFinPartida != null && panelFinPartida.activeSelf) return;
            if (panelPausa != null && !panelPausa.activeSelf)
            {
                AlternarPausa();
            }
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instancia == this) Instancia = null;
            SalaRed.PartidaTerminada -= AlTerminarPartida;
            GestorMaestria.AlGanarMaestria -= OnMaestriaActualizada;
        }

        void Update()
        {
            // Atajo ESC: siempre responde para cerrar modales o alternar pausa
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(Controles.Pausa))
            {
                if (_modalMaestriaHUD != null && _modalMaestriaHUD.activeSelf)
                {
                    _modalMaestriaHUD.SetActive(false);
                }
                else if (TiendaAbierta)
                {
                    CerrarTienda();
                }
                else if (PanelOpcionesControles.Instancia != null && PanelOpcionesControles.Instancia.gameObject.activeSelf)
                {
                    PanelOpcionesControles.Instancia.Cerrar();
                }
                else
                {
                    AlternarPausa();
                }
            }

            // Asegurar que el minimapa permanezca visible durante el gameplay normal (estrictamente oculto en cargas)
            if (minimapaHUD == null)
            {
                minimapaHUD = MinimapaHUD.Instancia ?? MinimapaHUD.CrearEnHUD(transform);
            }

            if (minimapaHUD != null)
            {
                bool fadeActivo = (panelFadeNegro != null && panelFadeNegro.gameObject.activeSelf && panelFadeNegro.color.a > 0.02f);
                bool debeEstarActivo = !EnTransicionCarga
                    && !fadeActivo
                    && (panelPausa == null || !panelPausa.activeSelf)
                    && (panelFinPartida == null || !panelFinPartida.activeSelf)
                    && !TiendaAbierta;
                if (minimapaHUD.gameObject.activeSelf != debeEstarActivo)
                {
                    minimapaHUD.gameObject.SetActive(debeEstarActivo);
                }
            }

            // Atajos de votación F1 (SÍ) y F2 (NO)
            if (panelVotacion != null && panelVotacion.activeSelf && !_yaVoteLocalmente)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    Votar(true);
                }
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    Votar(false);
                }
            }

            // Actualizar tiempo y estado de votación si está activa
            if (panelVotacion != null && panelVotacion.activeSelf && SalaRed.Instancia != null)
            {
                if (SalaRed.Instancia.VotacionLobbyActiva.Value)
                {
                    float t = Mathf.Max(0f, SalaRed.Instancia.TiempoVotacionRestante.Value);
                    if (textoTemporizadorVotacion != null)
                    {
                        textoTemporizadorVotacion.text = $"Tiempo restante: {Mathf.CeilToInt(t)}s (Atajos: F1=SÍ, F2=NO)";
                    }
                    if (textoEstadoVotos != null)
                    {
                        int si = SalaRed.Instancia.VotosSiLobby.Value;
                        int no = SalaRed.Instancia.VotosNoLobby.Value;
                        int total = SalaRed.Instancia.TotalVotantesLobby.Value;
                        textoEstadoVotos.text = $"<color=#55ff55><b>{si} SÍ</b></color>  —  <color=#ff5555><b>{no} NO</b></color>  (Total: {total})";
                    }
                }
            }

            if (_tiempoAvisoPasaje > 0f)
            {
                _tiempoAvisoPasaje -= Time.deltaTime;
                if (_tiempoAvisoPasaje <= 0f && textoAvisoPasaje != null)
                {
                    textoAvisoPasaje.gameObject.SetActive(false);
                }
            }

            if (_personajeLocal == null || !_personajeLocal.EsControlLocal)
            {
                _personajeLocal = null;
                for (int i = 0; i < Personaje.Todos.Count; i++)
                {
                    var p = Personaje.Todos[i];
                    if (p != null && p.EsControlLocal)
                    {
                        ConectarPersonaje(p);
                        break;
                    }
                }
            }

            ActualizarHUDJugadorLocal();
            ActualizarEfectosConsumiblesDerecha(Time.deltaTime);
            ActualizarCompanerosArribaIzquierda();
        }

        private void AsegurarTextoMiNombreHUD()
        {
            var contArribaIzq = transform.Find("ContenedorHUD_ArribaIzquierda") as RectTransform;
            if (contArribaIzq == null && barraVida != null)
            {
                var p = barraVida.transform.parent as RectTransform;
                if (p != null && p.name == "ContenedorHUD_ArribaIzquierda") contArribaIzq = p;
            }

            if (contArribaIzq != null)
            {
                contArribaIzq.anchorMin = new Vector2(0f, 1f);
                contArribaIzq.anchorMax = new Vector2(0f, 1f);
                contArribaIzq.pivot = new Vector2(0f, 1f);
                contArribaIzq.anchoredPosition = new Vector2(25f, -10f);
                contArribaIzq.sizeDelta = new Vector2(400f, 60f);

                if (textoMiNombreHUD == null)
                {
                    var existente = contArribaIzq.Find("TextoMiNombreHUD");
                    if (existente != null)
                    {
                        textoMiNombreHUD = existente.GetComponent<Text>();
                    }
                }

                if (textoMiNombreHUD == null)
                {
                    var nomObj = new GameObject("TextoMiNombreHUD");
                    nomObj.transform.SetParent(contArribaIzq, false);
                    var rtNom = nomObj.AddComponent<RectTransform>();
                    rtNom.anchorMin = new Vector2(0f, 1f);
                    rtNom.anchorMax = new Vector2(0f, 1f);
                    rtNom.pivot = new Vector2(0f, 1f);
                    rtNom.anchoredPosition = new Vector2(2f, 0f);
                    rtNom.sizeDelta = new Vector2(380f, 26f);

                    textoMiNombreHUD = nomObj.AddComponent<Text>();
                    textoMiNombreHUD.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    textoMiNombreHUD.fontSize = 20; // Nombre del jugador visiblemente más grande
                    textoMiNombreHUD.fontStyle = FontStyle.Bold;
                    textoMiNombreHUD.alignment = TextAnchor.MiddleLeft;
                    textoMiNombreHUD.color = new Color(0.95f, 0.98f, 1f, 1f);
                    textoMiNombreHUD.raycastTarget = false;

                    var sombra = nomObj.AddComponent<Shadow>();
                    sombra.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    sombra.effectDistance = new Vector2(1.5f, -1.5f);
                }
                else
                {
                    textoMiNombreHUD.fontSize = 20;
                }

                // Encoger la barra de maestría para que el nombre resalte por encima
                if (barraMaestria != null && barraMaestria.transform.parent == contArribaIzq)
                {
                    var rtBm = barraMaestria.GetComponent<RectTransform>();
                    if (rtBm != null)
                    {
                        rtBm.anchorMin = new Vector2(0f, 1f);
                        rtBm.anchorMax = new Vector2(0f, 1f);
                        rtBm.pivot = new Vector2(0f, 1f);
                        rtBm.anchoredPosition = new Vector2(2f, -28f);
                        rtBm.sizeDelta = new Vector2(175f, 16f); // Barra de maestría más compacta
                    }
                    if (barraMaestria.textoVida != null)
                    {
                        barraMaestria.textoVida.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        barraMaestria.textoVida.fontSize = 8;
                        barraMaestria.textoVida.alignment = TextAnchor.MiddleCenter;
                        barraMaestria.textoVida.raycastTarget = false;
                    }

                    // Botón para abrir la explicación de maestría al hacer clic en la barra
                    var btnBarra = barraMaestria.GetComponent<Button>();
                    if (btnBarra == null)
                    {
                        btnBarra = barraMaestria.gameObject.AddComponent<Button>();
                    }
                    btnBarra.onClick.RemoveAllListeners();
                    btnBarra.onClick.AddListener(MostrarExplicacionMaestriaHUD);

                    // Botón [?] accesible justo al lado de la barra de maestría
                    var btnInfoObj = contArribaIzq.Find("BotonInfoMaestriaHUD");
                    if (btnInfoObj == null)
                    {
                        var objInfo = new GameObject("BotonInfoMaestriaHUD");
                        objInfo.transform.SetParent(contArribaIzq, false);
                        var rtInfo = objInfo.AddComponent<RectTransform>();
                        rtInfo.anchorMin = new Vector2(0f, 1f);
                        rtInfo.anchorMax = new Vector2(0f, 1f);
                        rtInfo.pivot = new Vector2(0f, 1f);
                        rtInfo.anchoredPosition = new Vector2(182f, -28f);
                        rtInfo.sizeDelta = new Vector2(16f, 16f);

                        var imgInfo = objInfo.AddComponent<Image>();
                        imgInfo.color = new Color(0.18f, 0.24f, 0.38f, 0.95f);

                        var btnInfo = objInfo.AddComponent<Button>();
                        btnInfo.onClick.AddListener(MostrarExplicacionMaestriaHUD);

                        var txtInfoObj = new GameObject("Texto");
                        txtInfoObj.transform.SetParent(objInfo.transform, false);
                        var rtTxt = txtInfoObj.AddComponent<RectTransform>();
                        rtTxt.anchorMin = Vector2.zero;
                        rtTxt.anchorMax = Vector2.one;
                        rtTxt.sizeDelta = Vector2.zero;
                        var txtInfo = txtInfoObj.AddComponent<Text>();
                        txtInfo.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        txtInfo.fontSize = 9;
                        txtInfo.fontStyle = FontStyle.Bold;
                        txtInfo.alignment = TextAnchor.MiddleCenter;
                        txtInfo.color = new Color(1f, 0.88f, 0.35f);
                        txtInfo.text = "?";
                        txtInfo.raycastTarget = false;
                    }
                }
            }
        }

        public void MostrarExplicacionMaestriaHUD()
        {
            if (_modalMaestriaHUD == null)
            {
                _modalMaestriaHUD = new GameObject("ModalExplicacionMaestriaHUD");
                Canvas canvasDestino = GetComponentInChildren<Canvas>() ?? GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
                if (canvasDestino != null) _modalMaestriaHUD.transform.SetParent(canvasDestino.transform, false);
                _modalMaestriaHUD.transform.SetAsLastSibling();

                var rt = _modalMaestriaHUD.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                var imgFondo = _modalMaestriaHUD.AddComponent<Image>();
                imgFondo.color = new Color(0f, 0f, 0f, 0.85f);

                // Cierre por clic en el fondo oscuro
                var btnFondo = _modalMaestriaHUD.AddComponent<Button>();
                btnFondo.onClick.AddListener(() => _modalMaestriaHUD.SetActive(false));

                var cardObj = new GameObject("Tarjeta");
                cardObj.transform.SetParent(_modalMaestriaHUD.transform, false);
                var rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                rtCard.pivot = new Vector2(0.5f, 0.5f);
                rtCard.anchoredPosition = Vector2.zero;
                rtCard.sizeDelta = new Vector2(580f, 360f);

                var imgCardBorde = cardObj.AddComponent<Image>();
                imgCardBorde.color = new Color(0.32f, 0.65f, 0.95f, 1f);

                var cardFondo = new GameObject("Fondo");
                cardFondo.transform.SetParent(cardObj.transform, false);
                var rtCf = cardFondo.AddComponent<RectTransform>();
                rtCf.anchorMin = Vector2.zero;
                rtCf.anchorMax = Vector2.one;
                rtCf.offsetMin = new Vector2(3f, 3f);
                rtCf.offsetMax = new Vector2(-3f, -3f);
                var imgCf = cardFondo.AddComponent<Image>();
                imgCf.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);

                // Título
                var titObj = new GameObject("Titulo");
                titObj.transform.SetParent(cardFondo.transform, false);
                var rtTit = titObj.AddComponent<RectTransform>();
                rtTit.anchorMin = new Vector2(0f, 1f);
                rtTit.anchorMax = new Vector2(1f, 1f);
                rtTit.pivot = new Vector2(0.5f, 1f);
                rtTit.anchoredPosition = new Vector2(0f, -14f);
                rtTit.sizeDelta = new Vector2(-30f, 32f);
                var txtTit = titObj.AddComponent<Text>();
                txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtTit.fontSize = 13;
                txtTit.fontStyle = FontStyle.Bold;
                txtTit.alignment = TextAnchor.MiddleCenter;
                txtTit.color = new Color(1f, 0.88f, 0.35f);
                txtTit.text = "✦ GUÍA DE MAESTRÍA PERMANENTE ✦";

                // Cuerpo
                var cuerpoObj = new GameObject("TextoCuerpo");
                cuerpoObj.transform.SetParent(cardFondo.transform, false);
                var rtCuerpo = cuerpoObj.AddComponent<RectTransform>();
                rtCuerpo.anchorMin = new Vector2(0f, 0f);
                rtCuerpo.anchorMax = new Vector2(1f, 1f);
                rtCuerpo.offsetMin = new Vector2(24f, 55f);
                rtCuerpo.offsetMax = new Vector2(-24f, -50f);
                var txtCuerpo = cuerpoObj.AddComponent<Text>();
                txtCuerpo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtCuerpo.fontSize = 13;
                txtCuerpo.lineSpacing = 1.3f;
                txtCuerpo.alignment = TextAnchor.UpperLeft;
                txtCuerpo.color = new Color(0.9f, 0.95f, 1f);
                txtCuerpo.text =
                    "• La Maestría es PERMANENTE en tu cuenta y se conserva para siempre.\n\n" +
                    "• Ganas XP de maestría eliminando monstruos y jefes en mazmorras (~35% de la XP de combate).\n\n" +
                    "• Desbloqueo progresivo de habilidades por personaje:\n" +
                    "   <color=#a0e0ff>Nv. 1:</color> Habilidad Básica (Click Izquierdo)\n" +
                    "   <color=#a0e0ff>Nv. 2:</color> Habilidad Activa 1 (Tecla E)\n" +
                    "   <color=#a0e0ff>Nv. 3:</color> Habilidad Activa 2 (Tecla Q)\n" +
                    "   <color=#a0e0ff>Nv. 4:</color> Habilidad Definitiva (Tecla R / Ultimate)\n" +
                    "   <color=#ffd700>Nv. 5 a 20:</color> Bonificaciones de maestría de cuenta.\n\n" +
                    "¡Derrota monstruos en la run para maximizar el nivel de tu personaje!";

                // Botón Cerrar
                var btnCerrarObj = new GameObject("BotonCerrar");
                btnCerrarObj.transform.SetParent(cardFondo.transform, false);
                var rtBtn = btnCerrarObj.AddComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 12f);
                rtBtn.sizeDelta = new Vector2(220f, 32f);

                var imgBtn = btnCerrarObj.AddComponent<Image>();
                imgBtn.color = new Color(0.18f, 0.24f, 0.36f, 1f);
                var btnC = btnCerrarObj.AddComponent<Button>();
                btnC.onClick.AddListener(() => _modalMaestriaHUD.SetActive(false));

                var txtBtnObj = new GameObject("Texto");
                txtBtnObj.transform.SetParent(btnCerrarObj.transform, false);
                var rtTxtB = txtBtnObj.AddComponent<RectTransform>();
                rtTxtB.anchorMin = Vector2.zero;
                rtTxtB.anchorMax = Vector2.one;
                rtTxtB.sizeDelta = Vector2.zero;
                var txtBtn = txtBtnObj.AddComponent<Text>();
                txtBtn.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtBtn.fontSize = 10;
                txtBtn.fontStyle = FontStyle.Bold;
                txtBtn.alignment = TextAnchor.MiddleCenter;
                txtBtn.color = Color.white;
                txtBtn.text = "ENTENDIDO [ESC]";
            }

            _modalMaestriaHUD.SetActive(true);
            _modalMaestriaHUD.transform.SetAsLastSibling();
        }

        private void AsegurarLayoutInferior()
        {
            var contAbajo = transform.Find("ContenedorHUD_AbajoCentro") as RectTransform;
            if (contAbajo == null) return;

            contAbajo.anchorMin = new Vector2(0.5f, 0f);
            contAbajo.anchorMax = new Vector2(0.5f, 0f);
            contAbajo.pivot = new Vector2(0.5f, 0f);
            contAbajo.anchoredPosition = new Vector2(0f, 6f);
            contAbajo.sizeDelta = new Vector2(400f, 155f);

            // 1. Fila de Habilidades en la base
            var filaHab = contAbajo.Find("FilaHabilidades") as RectTransform;
            if (filaHab != null)
            {
                filaHab.anchorMin = new Vector2(0.5f, 0f);
                filaHab.anchorMax = new Vector2(0.5f, 0f);
                filaHab.pivot = new Vector2(0.5f, 0f);
                filaHab.anchoredPosition = new Vector2(0f, 6f);
                filaHab.sizeDelta = new Vector2(380f, 84f);

                float[] posX = new float[] { -141f, -47f, 47f, 141f };
                for (int i = 0; i < filaHab.childCount && i < 4; i++)
                {
                    var slotChild = filaHab.GetChild(i) as RectTransform;
                    if (slotChild != null)
                    {
                        slotChild.anchoredPosition = new Vector2(posX[i], 0f);
                        slotChild.sizeDelta = new Vector2(80f, 80f);

                        var iconoChild = slotChild.Find("Icono") as RectTransform;
                        if (iconoChild != null)
                        {
                            iconoChild.sizeDelta = new Vector2(-6f, -6f);
                            var imgIco = iconoChild.GetComponent<Image>();
                            if (imgIco != null) imgIco.preserveAspect = true;
                        }

                        var sombraChild = slotChild.Find("SombraCooldown") as RectTransform;
                        if (sombraChild != null)
                        {
                            sombraChild.sizeDelta = new Vector2(-6f, -6f);
                        }

                        var cdChild = slotChild.Find("TextoCooldown") as RectTransform;
                        if (cdChild != null)
                        {
                            var txtCd = cdChild.GetComponent<Text>();
                            if (txtCd != null) txtCd.fontSize = 15;
                        }

                        var badgeChild = slotChild.Find("BadgeTecla") as RectTransform;
                        if (badgeChild != null)
                        {
                            badgeChild.sizeDelta = new Vector2(42f, 18f);
                        }
                    }
                }
            }

            // 2. Barra de Experiencia de Run colocada SOBRE las habilidades
            if (barraXPRun != null)
            {
                if (barraXPRun.transform.parent != contAbajo)
                {
                    barraXPRun.transform.SetParent(contAbajo, false);
                }
                var rtXp = barraXPRun.GetComponent<RectTransform>();
                if (rtXp != null)
                {
                    rtXp.anchorMin = new Vector2(0.5f, 0f);
                    rtXp.anchorMax = new Vector2(0.5f, 0f);
                    rtXp.pivot = new Vector2(0.5f, 0f);
                    rtXp.anchoredPosition = new Vector2(0f, 96f);
                    rtXp.sizeDelta = new Vector2(370f, 20f);
                }
            }

            // 3. Barra de Vida colocada SOBRE la Barra de Experiencia de Run (vida arriba de la barra de nivel)
            if (barraVida != null)
            {
                if (barraVida.transform.parent != contAbajo)
                {
                    barraVida.transform.SetParent(contAbajo, false);
                }
                var rtBv = barraVida.GetComponent<RectTransform>();
                if (rtBv != null)
                {
                    rtBv.anchorMin = new Vector2(0.5f, 0f);
                    rtBv.anchorMax = new Vector2(0.5f, 0f);
                    rtBv.pivot = new Vector2(0.5f, 0f);
                    rtBv.anchoredPosition = new Vector2(0f, 122f);
                    rtBv.sizeDelta = new Vector2(370f, 24f);
                }
            }

            // Limpiar texto viejo de oro si aún estaba en contAbajo
            var existOroViejo = contAbajo.Find("TextoOroRun");
            if (existOroViejo != null && (textoOroRun == null || existOroViejo != textoOroRun.transform))
            {
                Destroy(existOroViejo.gameObject);
            }
        }

        private void AsegurarTextoOroRun()
        {
            if (textoOroRun == null)
            {
                var existCont = transform.Find("ContenedorOroRunHUD");
                if (existCont != null)
                {
                    textoOroRun = existCont.GetComponentInChildren<Text>();
                }
            }

            if (textoOroRun == null)
            {
                // Contenedor visual de oro (lleva la imagen de fondo)
                var contOroObj = new GameObject("ContenedorOroRunHUD");
                contOroObj.transform.SetParent(transform, false);

                var rtCont = contOroObj.AddComponent<RectTransform>();
                rtCont.anchorMin = new Vector2(1f, 1f);
                rtCont.anchorMax = new Vector2(1f, 1f);
                rtCont.pivot = new Vector2(1f, 1f);
                // Minimapa está en X = -16, Ancho = 285 (borde izquierdo en -301)
                // Colocamos el oro arriba a la derecha, al ladito a la izquierda del minimapa
                rtCont.anchoredPosition = new Vector2(-310f, -54f);
                rtCont.sizeDelta = new Vector2(130f, 34f);

                var imgFondo = contOroObj.AddComponent<Image>();
                imgFondo.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

                // GameObject hijo dedicado al texto (evita conflicto Graphic de Unity UI)
                var txtObj = new GameObject("TextoOro");
                txtObj.transform.SetParent(contOroObj.transform, false);
                var rtTxt = txtObj.AddComponent<RectTransform>();
                rtTxt.anchorMin = Vector2.zero;
                rtTxt.anchorMax = Vector2.one;
                rtTxt.sizeDelta = Vector2.zero;

                textoOroRun = txtObj.AddComponent<Text>();
                textoOroRun.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoOroRun.fontSize = 12;
                textoOroRun.fontStyle = FontStyle.Bold;
                textoOroRun.alignment = TextAnchor.MiddleCenter;
                textoOroRun.color = new Color(1f, 0.88f, 0.2f, 1f);
                textoOroRun.text = "🪙 0";
                textoOroRun.raycastTarget = false;

                var sombra = txtObj.AddComponent<Shadow>();
                sombra.effectColor = new Color(0f, 0f, 0f, 0.95f);
                sombra.effectDistance = new Vector2(1f, -1f);
            }
        }

        public void ConfigurarLayoutMenuPausa()
        {
            if (panelPausa == null) return;

            // Asegurar fondo translúcido uniforme
            var imgFondoPausa = panelPausa.GetComponent<Image>();
            if (imgFondoPausa != null)
            {
                imgFondoPausa.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
            }

            // 1. Título "PAUSA" centrado arriba
            Text txtTit = null;
            var titPausa = panelPausa.transform.Find("TextoTituloPausa");
            if (titPausa != null)
            {
                txtTit = titPausa.GetComponent<Text>();
            }
            else
            {
                foreach (var t in panelPausa.GetComponentsInChildren<Text>(true))
                {
                    if (t.text.Trim().ToUpper() == "PAUSA")
                    {
                        txtTit = t;
                        break;
                    }
                }
            }

            if (txtTit != null)
            {
                var rtTit = txtTit.GetComponent<RectTransform>();
                rtTit.anchorMin = new Vector2(0.5f, 0.5f);
                rtTit.anchorMax = new Vector2(0.5f, 0.5f);
                rtTit.pivot = new Vector2(0.5f, 0.5f);
                rtTit.anchoredPosition = new Vector2(0f, 220f);
                rtTit.sizeDelta = new Vector2(400f, 60f);
                txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtTit.fontSize = 42;
                txtTit.fontStyle = FontStyle.Bold;
                txtTit.alignment = TextAnchor.MiddleCenter;
                txtTit.color = Color.white;
            }

            // 2. Columna izquierda: 4 botones verticales alineados a la izquierda
            AsegurarBotonControlesPausa();
            AjustarBotonPausaIndividual(botonReanudar, "▶ REANUDAR", -235f, 75f);
            AjustarBotonPausaIndividual(botonControlesPausa, "⚙️ CONTROLES", -235f, 20f);
            AjustarBotonPausaIndividual(botonVolverLobbyPausa, "🏠 VOLVER AL LOBBY", -235f, -35f);
            AjustarBotonPausaIndividual(botonSalirPartida, "🚪 SALIR AL MENÚ", -235f, -90f);

            // 3. Columna derecha: Habilidades arriba, Tarjeta de Descripción en medio, Mapa abajo
            CrearOActualizarSeccionDerechaPausa();
        }

        private void CrearOActualizarSeccionDerechaPausa()
        {
            if (panelPausa == null) return;

            try
            {
                const float posXDerecha = 205f;

                // --- 3A. Fila de Habilidades (4 casillas en filita, SOLO ICONOS) ---
                Transform contHabs = panelPausa.transform.Find("FilaHabilidadesPausa");
                if (contHabs == null)
                {
                    var obj = new GameObject("FilaHabilidadesPausa", typeof(RectTransform));
                    obj.transform.SetParent(panelPausa.transform, false);
                    contHabs = obj.transform;
                }
                var rtHabs = contHabs.GetComponent<RectTransform>();
                if (rtHabs != null)
                {
                    rtHabs.anchorMin = new Vector2(0.5f, 0.5f);
                    rtHabs.anchorMax = new Vector2(0.5f, 0.5f);
                    rtHabs.pivot = new Vector2(0.5f, 0.5f);
                    rtHabs.anchoredPosition = new Vector2(posXDerecha, 142f);
                    rtHabs.sizeDelta = new Vector2(370f, 56f);
                }

                float[] slotsX = new float[] { -105f, -35f, 35f, 105f };

                bool necesitaReconstruir = (_botonesHabilidadesPausa.Count < 4 || _iconosHabilidadesPausa.Count < 4);
                if (!necesitaReconstruir)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (_botonesHabilidadesPausa[i] == null || _iconosHabilidadesPausa[i] == null || _bordesHabilidadesPausa[i] == null)
                        {
                            necesitaReconstruir = true;
                            break;
                        }
                    }
                }

                if (necesitaReconstruir)
                {
                    for (int c = contHabs.childCount - 1; c >= 0; c--)
                    {
                        var ch = contHabs.GetChild(c);
                        if (ch != null) DestroyImmediate(ch.gameObject);
                    }
                    _botonesHabilidadesPausa.Clear();
                    _iconosHabilidadesPausa.Clear();
                    _bordesHabilidadesPausa.Clear();

                    for (int i = 0; i < 4; i++)
                    {
                        int idx = i;
                        var slotBtnObj = new GameObject($"SlotHabPausa_{i}", typeof(RectTransform));
                        slotBtnObj.transform.SetParent(contHabs, false);

                        var rtSlot = slotBtnObj.GetComponent<RectTransform>();
                        rtSlot.anchorMin = new Vector2(0.5f, 0.5f);
                        rtSlot.anchorMax = new Vector2(0.5f, 0.5f);
                        rtSlot.pivot = new Vector2(0.5f, 0.5f);
                        rtSlot.anchoredPosition = new Vector2(slotsX[i], 0f);
                        rtSlot.sizeDelta = new Vector2(52f, 52f);

                        // Marco exterior (borde de selección)
                        var imgBorde = slotBtnObj.AddComponent<Image>();
                        imgBorde.color = (idx == _habilidadSeleccionadaPausa)
                            ? new Color(0.25f, 0.85f, 1.0f, 1f)
                            : new Color(0.20f, 0.28f, 0.40f, 0.7f);

                        var btn = slotBtnObj.AddComponent<Button>();
                        btn.onClick.AddListener(() => SeleccionarHabilidadPausa(idx));

                        // Fondo interior del icono
                        var fondoIcoObj = new GameObject("FondoInterno", typeof(RectTransform));
                        fondoIcoObj.transform.SetParent(slotBtnObj.transform, false);
                        var rtFondoIco = fondoIcoObj.GetComponent<RectTransform>();
                        rtFondoIco.anchorMin = Vector2.zero;
                        rtFondoIco.anchorMax = Vector2.one;
                        rtFondoIco.offsetMin = new Vector2(2f, 2f);
                        rtFondoIco.offsetMax = new Vector2(-2f, -2f);
                        var imgFondo = fondoIcoObj.AddComponent<Image>();
                        imgFondo.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);
                        imgFondo.raycastTarget = false;

                        // SOLO ICONO de la habilidad (sin badges ni letras)
                        var icoObj = new GameObject("Icono", typeof(RectTransform));
                        icoObj.transform.SetParent(fondoIcoObj.transform, false);
                        var rtIco = icoObj.GetComponent<RectTransform>();
                        rtIco.anchorMin = new Vector2(0.08f, 0.08f);
                        rtIco.anchorMax = new Vector2(0.92f, 0.92f);
                        rtIco.offsetMin = Vector2.zero;
                        rtIco.offsetMax = Vector2.zero;
                        var imgIco = icoObj.AddComponent<Image>();
                        imgIco.preserveAspect = true;
                        imgIco.raycastTarget = false;

                        _botonesHabilidadesPausa.Add(btn);
                        _iconosHabilidadesPausa.Add(imgIco);
                        _bordesHabilidadesPausa.Add(imgBorde);
                    }
                }

                // --- 3B. Caja de Descripción de Habilidad (desplegada abajo de los iconos) ---
                Transform descBox = panelPausa.transform.Find("CajaDescHabilidadPausa");
                if (descBox == null)
                {
                    var obj = new GameObject("CajaDescHabilidadPausa", typeof(RectTransform));
                    obj.transform.SetParent(panelPausa.transform, false);
                    descBox = obj.transform;

                    var imgMarcoBox = obj.AddComponent<Image>();
                    imgMarcoBox.color = new Color(0.24f, 0.40f, 0.62f, 0.85f); // Borde estético

                    var fondoInt = new GameObject("Fondo", typeof(RectTransform));
                    fondoInt.transform.SetParent(obj.transform, false);
                    var rtFi = fondoInt.GetComponent<RectTransform>();
                    rtFi.anchorMin = Vector2.zero;
                    rtFi.anchorMax = Vector2.one;
                    rtFi.offsetMin = new Vector2(2f, 2f);
                    rtFi.offsetMax = new Vector2(-2f, -2f);
                    var imgFi = fondoInt.AddComponent<Image>();
                    imgFi.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);

                    var txtObj = new GameObject("Texto", typeof(RectTransform));
                    txtObj.transform.SetParent(fondoInt.transform, false);
                    var rtTxt = txtObj.GetComponent<RectTransform>();
                    rtTxt.anchorMin = Vector2.zero;
                    rtTxt.anchorMax = Vector2.one;
                    rtTxt.offsetMin = new Vector2(10f, 6f);
                    rtTxt.offsetMax = new Vector2(-10f, -6f);

                    _textoDescHabilidadPausa = txtObj.AddComponent<Text>();
                    _textoDescHabilidadPausa.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    _textoDescHabilidadPausa.fontSize = 12;
                    _textoDescHabilidadPausa.lineSpacing = 1.22f;
                    _textoDescHabilidadPausa.alignment = TextAnchor.MiddleLeft;
                    _textoDescHabilidadPausa.color = new Color(0.90f, 0.95f, 1f);
                    _textoDescHabilidadPausa.raycastTarget = false;
                }
                else if (_textoDescHabilidadPausa == null)
                {
                    _textoDescHabilidadPausa = descBox.GetComponentInChildren<Text>(true);
                }
                var rtDesc = descBox.GetComponent<RectTransform>();
                if (rtDesc != null)
                {
                    rtDesc.anchorMin = new Vector2(0.5f, 0.5f);
                    rtDesc.anchorMax = new Vector2(0.5f, 0.5f);
                    rtDesc.pivot = new Vector2(0.5f, 0.5f);
                    rtDesc.anchoredPosition = new Vector2(posXDerecha, 70f);
                    rtDesc.sizeDelta = new Vector2(370f, 72f);
                }

                // --- 3C. Marco del Mapa (abajo de la descripción) ---
                Transform marcoMapa = panelPausa.transform.Find("MarcoMapaPausa");
                if (marcoMapa == null)
                {
                    var obj = new GameObject("MarcoMapaPausa", typeof(RectTransform));
                    obj.transform.SetParent(panelPausa.transform, false);
                    marcoMapa = obj.transform;

                    var imgMarco = obj.AddComponent<Image>();
                    imgMarco.color = new Color(0.32f, 0.65f, 0.95f, 0.95f); // Borde azul cian definido

                    var fondoInt = new GameObject("Fondo", typeof(RectTransform));
                    fondoInt.transform.SetParent(obj.transform, false);
                    var rtFi = fondoInt.GetComponent<RectTransform>();
                    rtFi.anchorMin = Vector2.zero;
                    rtFi.anchorMax = Vector2.one;
                    rtFi.offsetMin = new Vector2(3f, 3f);
                    rtFi.offsetMax = new Vector2(-3f, -3f);
                    var imgFi = fondoInt.AddComponent<Image>();
                    imgFi.color = new Color(0.05f, 0.07f, 0.11f, 0.98f);

                    var rawObj = new GameObject("Lienzo", typeof(RectTransform));
                    rawObj.transform.SetParent(fondoInt.transform, false);
                    var rtRaw = rawObj.GetComponent<RectTransform>();
                    rtRaw.anchorMin = Vector2.zero;
                    rtRaw.anchorMax = Vector2.one;
                    rtRaw.offsetMin = new Vector2(4f, 4f);
                    rtRaw.offsetMax = new Vector2(-4f, -22f);
                    _rawMapaPausa = rawObj.AddComponent<RawImage>();

                    var titObj = new GameObject("Titulo", typeof(RectTransform));
                    titObj.transform.SetParent(fondoInt.transform, false);
                    var rtTit = titObj.GetComponent<RectTransform>();
                    rtTit.anchorMin = new Vector2(0f, 1f);
                    rtTit.anchorMax = new Vector2(1f, 1f);
                    rtTit.pivot = new Vector2(0.5f, 1f);
                    rtTit.anchoredPosition = new Vector2(0f, -2f);
                    rtTit.sizeDelta = new Vector2(0f, 18f);
                    var txtTit = titObj.AddComponent<Text>();
                    txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txtTit.fontSize = 10;
                    txtTit.fontStyle = FontStyle.Bold;
                    txtTit.alignment = TextAnchor.MiddleCenter;
                    txtTit.color = new Color(0.85f, 0.95f, 1f, 0.95f);
                    txtTit.text = "✦ MAPA DE LA MAZMORRA ✦";
                }
                if (_rawMapaPausa == null && marcoMapa != null)
                {
                    _rawMapaPausa = marcoMapa.GetComponentInChildren<RawImage>(true);
                }
                var rtMm = marcoMapa.GetComponent<RectTransform>();
                if (rtMm != null)
                {
                    rtMm.anchorMin = new Vector2(0.5f, 0.5f);
                    rtMm.anchorMax = new Vector2(0.5f, 0.5f);
                    rtMm.pivot = new Vector2(0.5f, 0.5f);
                    rtMm.anchoredPosition = new Vector2(posXDerecha, -76f);
                    rtMm.sizeDelta = new Vector2(370f, 196f);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HUD] Excepción en CrearOActualizarSeccionDerechaPausa: {ex}");
            }
        }

        private void SeleccionarHabilidadPausa(int index)
        {
            _habilidadSeleccionadaPausa = index;
            ActualizarDetalleHabilidadPausa();
        }

        private TipoPersonaje ObtenerTipoPersonajeLocal()
        {
            if (_personajeLocal != null && _personajeLocal.TipoPj != TipoPersonaje.Ninguno)
            {
                return _personajeLocal.TipoPj;
            }
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                if (Personaje.Todos[i] != null && Personaje.Todos[i].EsControlLocal && Personaje.Todos[i].TipoPj != TipoPersonaje.Ninguno)
                {
                    _personajeLocal = Personaje.Todos[i];
                    return _personajeLocal.TipoPj;
                }
            }
            if (SalaRed.Instancia != null && Unity.Netcode.NetworkManager.Singleton != null)
            {
                ulong myId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
                int idx = SalaRed.Instancia.IndiceDe(myId);
                if (idx >= 0 && idx < SalaRed.Instancia.Jugadores.Count)
                {
                    var pj = SalaRed.Instancia.Jugadores[idx].personaje;
                    if (pj != TipoPersonaje.Ninguno) return pj;
                }
            }
            return TipoPersonaje.Fuego;
        }

        public void ActualizarHabilidadesPausaUI()
        {
            var tipo = ObtenerTipoPersonajeLocal();
            var habs = CatalogoHabilidades.ObtenerHabilidadesPersonaje(tipo);

            // Re-enlazar si la lista interna está incompleta o con referencias nulas
            if (_iconosHabilidadesPausa.Count < 4 || _iconosHabilidadesPausa[0] == null)
            {
                _iconosHabilidadesPausa.Clear();
                _bordesHabilidadesPausa.Clear();
                _botonesHabilidadesPausa.Clear();
                if (panelPausa != null)
                {
                    var cont = panelPausa.transform.Find("FilaHabilidadesPausa");
                    if (cont != null)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            var slot = cont.Find($"SlotHabPausa_{k}");
                            if (slot != null)
                            {
                                var btn = slot.GetComponent<Button>();
                                var borde = slot.GetComponent<Image>();
                                var ico = slot.Find("FondoInterno/Icono")?.GetComponent<Image>();
                                if (btn != null) _botonesHabilidadesPausa.Add(btn);
                                if (borde != null) _bordesHabilidadesPausa.Add(borde);
                                if (ico != null) _iconosHabilidadesPausa.Add(ico);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < _iconosHabilidadesPausa.Count && i < 4; i++)
            {
                Sprite sp = null;
                if (i < habs.Count)
                {
                    sp = CatalogoHabilidades.ObtenerSpriteHabilidad(habs[i].id);
                }

                // Fallback 1: Si ya está en los slots de habilidades activos del HUD del jugador
                if (sp == null && slotsHabilidades != null && i < slotsHabilidades.Length && slotsHabilidades[i] != null && slotsHabilidades[i].icono != null)
                {
                    sp = slotsHabilidades[i].icono.sprite;
                }

                // Fallback 2: Iconos del HUD
                if (sp == null)
                {
                    if (tipo == TipoPersonaje.Fuego)
                    {
                        if (i == 0) sp = iconoFuegoLMB;
                        else if (i == 1) sp = iconoFuegoE;
                        else if (i == 2) sp = iconoFuegoQ;
                        else if (i == 3) sp = iconoFuegoR;
                    }
                    else if (tipo == TipoPersonaje.Hielo)
                    {
                        if (i == 0) sp = iconoHieloLMB;
                        else if (i == 1) sp = iconoHieloE;
                        else if (i == 2) sp = iconoHieloQ;
                        else if (i == 3) sp = iconoHieloR;
                    }
                    else if (tipo == TipoPersonaje.Electrico)
                    {
                        if (i == 0) sp = iconoElecLMB;
                        else if (i == 1) sp = iconoElecE;
                        else if (i == 2) sp = iconoElecQ;
                        else if (i == 3) sp = iconoElecR;
                    }
                }

                // Fallback 3: Carga directa desde Resources
                if (sp == null && i < habs.Count)
                {
                    sp = Resources.Load<Sprite>($"IconosHabilidades/{habs[i].id}");
                }

                if (_iconosHabilidadesPausa[i] != null)
                {
                    if (sp != null)
                    {
                        _iconosHabilidadesPausa[i].sprite = sp;
                        _iconosHabilidadesPausa[i].color = Color.white;
                        _iconosHabilidadesPausa[i].enabled = true;
                        _iconosHabilidadesPausa[i].preserveAspect = true;
                        _iconosHabilidadesPausa[i].SetAllDirty();
                    }
                    else
                    {
                        _iconosHabilidadesPausa[i].sprite = null;
                        _iconosHabilidadesPausa[i].color = new Color(0.2f, 0.25f, 0.35f, 0.8f);
                        _iconosHabilidadesPausa[i].SetAllDirty();
                    }
                }
            }

            ActualizarDetalleHabilidadPausa();
        }

        private void ActualizarDetalleHabilidadPausa()
        {
            // Actualizar realce visual en el borde de los iconos de habilidad
            for (int i = 0; i < _bordesHabilidadesPausa.Count; i++)
            {
                if (_bordesHabilidadesPausa[i] != null)
                {
                    _bordesHabilidadesPausa[i].color = (i == _habilidadSeleccionadaPausa)
                        ? new Color(0.25f, 0.85f, 1.0f, 1f)
                        : new Color(0.20f, 0.28f, 0.40f, 0.7f);
                }
            }

            if (_textoDescHabilidadPausa == null)
            {
                if (panelPausa != null)
                {
                    var descBox = panelPausa.transform.Find("CajaDescHabilidadPausa");
                    if (descBox != null)
                    {
                        _textoDescHabilidadPausa = descBox.GetComponentInChildren<Text>(true);
                    }
                }
            }

            if (_textoDescHabilidadPausa == null) return;

            // Asegurar tipografía LegacyRuntime sin distorsión
            _textoDescHabilidadPausa.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _textoDescHabilidadPausa.fontSize = 12;
            _textoDescHabilidadPausa.lineSpacing = 1.22f;

            var tipo = ObtenerTipoPersonajeLocal();
            var habs = CatalogoHabilidades.ObtenerHabilidadesPersonaje(tipo);

            string[] teclas = new string[] { "CLICK IZQ", "E", "Q", "R" };
            int idx = Mathf.Clamp(_habilidadSeleccionadaPausa, 0, 3);

            if (idx < habs.Count)
            {
                var h = habs[idx];
                string desc = !string.IsNullOrEmpty(h.descripcionTecnica) ? h.descripcionTecnica : h.descripcion;
                _textoDescHabilidadPausa.text = $"<b><size=13><color=#5ce1e6>{h.nombre.ToUpper()}</color></size></b> <color=#ffd700>({teclas[idx]})</color>\n{desc}";
            }
            else
            {
                _textoDescHabilidadPausa.text = "<color=#94a3b8><i>Haz clic en una habilidad para ver su descripción detallada.</i></color>";
            }
        }

        private void ActualizarMapaPausa()
        {
            if (panelPausa == null) return;

            if (_rawMapaPausa == null)
            {
                var marco = panelPausa.transform.Find("MarcoMapaPausa");
                if (marco != null)
                {
                    _rawMapaPausa = marco.GetComponentInChildren<RawImage>(true);
                }
            }

            if (minimapaHUD == null)
            {
                minimapaHUD = MinimapaHUD.Instancia ?? MinimapaHUD.CrearEnHUD(transform);
            }

            if (SistemaVision.Instancia != null)
            {
                SistemaVision.Instancia.ForzarActualizacionExploracion();
            }

            if (MinimapaHUD.Instancia != null)
            {
                MinimapaHUD.Instancia.RenderizarMinimapa();
                if (_rawMapaPausa != null)
                {
                    _rawMapaPausa.texture = MinimapaHUD.Instancia.TextureMapa;
                    _rawMapaPausa.color = Color.white;
                    _rawMapaPausa.enabled = true;
                    _rawMapaPausa.gameObject.SetActive(true);
                    _rawMapaPausa.SetAllDirty();
                }
            }
        }

        private void AsegurarBotonControlesPausa()
        {
            if (panelPausa == null) return;

            if (botonControlesPausa == null)
            {
                var exist = panelPausa.transform.Find("BotonControlesPausa");
                if (exist != null)
                {
                    botonControlesPausa = exist.GetComponent<Button>();
                }
            }

            if (botonControlesPausa == null)
            {
                var btnObj = new GameObject("BotonControlesPausa", typeof(RectTransform));
                btnObj.transform.SetParent(panelPausa.transform, false);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.18f, 0.24f, 0.36f, 1f);
                botonControlesPausa = btnObj.AddComponent<Button>();

                var rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(230f, 44f);

                var txtObj = new GameObject("Texto", typeof(RectTransform));
                txtObj.transform.SetParent(btnObj.transform, false);
                var txt = txtObj.AddComponent<Text>();
                txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.text = "⚙️ CONTROLES";
                var rtTxt = txtObj.GetComponent<RectTransform>();
                rtTxt.anchorMin = Vector2.zero;
                rtTxt.anchorMax = Vector2.one;
                rtTxt.sizeDelta = Vector2.zero;
            }

            botonControlesPausa.onClick.RemoveAllListeners();
            botonControlesPausa.onClick.AddListener(() =>
            {
                PanelOpcionesControles.ObtenerOCrear(transform).Abrir();
            });
        }

        private void AjustarBotonPausaIndividual(Button btn, string textoDefecto, float posX, float posY)
        {
            if (btn == null) return;
            var rt = btn.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.sizeDelta = new Vector2(230f, 44f);
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            }

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
            }
        }

        // =========================================================================
        // TIENDA SECRETA MODAL (PUESTITO)
        // =========================================================================

        private void CrearPanelTiendaSiNoExiste()
        {
            if (panelTienda != null) return;

            var tiendaObj = new GameObject("PanelTiendaModal");
            tiendaObj.transform.SetParent(transform, false);
            tiendaObj.transform.SetAsLastSibling();

            var rtFull = tiendaObj.AddComponent<RectTransform>();
            rtFull.anchorMin = Vector2.zero;
            rtFull.anchorMax = Vector2.one;
            rtFull.sizeDelta = Vector2.zero;

            // Fondo translúcido oscuro para oscurecer la pantalla
            var imgFondo = tiendaObj.AddComponent<Image>();
            imgFondo.color = new Color(0.02f, 0.02f, 0.04f, 0.88f);

            // Ventana modal centrada
            var cardObj = new GameObject("VentanaTienda");
            cardObj.transform.SetParent(tiendaObj.transform, false);
            var rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.anchoredPosition = Vector2.zero;
            rtCard.sizeDelta = new Vector2(700f, 440f);

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            // Título
            var titObj = new GameObject("TituloTienda");
            titObj.transform.SetParent(cardObj.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 20;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.88f, 0.25f);
            txtTit.text = "🏪 TIENDA SECRETA DEL GATO MERCADER";
            var rtTit = titObj.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0.5f, 1f);
            rtTit.anchorMax = new Vector2(0.5f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0f, -16f);
            rtTit.sizeDelta = new Vector2(660f, 32f);

            // Subtítulo
            var subObj = new GameObject("SubtituloTienda");
            subObj.transform.SetParent(cardObj.transform, false);
            var txtSub = subObj.AddComponent<Text>();
            txtSub.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSub.fontSize = 11;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = new Color(0.75f, 0.82f, 0.92f);
            txtSub.text = "Haz clic sobre una mejora para adquirirla con tu oro de la run.";
            var rtSub = subObj.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -48f);
            rtSub.sizeDelta = new Vector2(660f, 22f);

            // Contador de Oro
            var oroObj = new GameObject("TextoOroTienda");
            oroObj.transform.SetParent(cardObj.transform, false);
            _textoOroTienda = oroObj.AddComponent<Text>();
            _textoOroTienda.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _textoOroTienda.fontSize = 14;
            _textoOroTienda.fontStyle = FontStyle.Bold;
            _textoOroTienda.alignment = TextAnchor.MiddleCenter;
            _textoOroTienda.color = new Color(1f, 0.85f, 0.2f);
            _textoOroTienda.text = "💰 Oro Disponible: 0";
            var rtOro = oroObj.GetComponent<RectTransform>();
            rtOro.anchorMin = new Vector2(0.5f, 1f);
            rtOro.anchorMax = new Vector2(0.5f, 1f);
            rtOro.pivot = new Vector2(0.5f, 1f);
            rtOro.anchoredPosition = new Vector2(0f, -72f);
            rtOro.sizeDelta = new Vector2(400f, 24f);

            // Contenedor de Artículos (4 columnas)
            float[] posX = new float[] { -234f, -78f, 78f, 234f };
            _botonesCompraTienda.Clear();
            _textosBotonTienda.Clear();

            for (int i = 0; i < 4; i++)
            {
                int itemIndex = i;
                var itemCardObj = new GameObject($"CardArticulo_{i}");
                itemCardObj.transform.SetParent(cardObj.transform, false);
                var rtIc = itemCardObj.AddComponent<RectTransform>();
                rtIc.anchorMin = new Vector2(0.5f, 0.5f);
                rtIc.anchorMax = new Vector2(0.5f, 0.5f);
                rtIc.pivot = new Vector2(0.5f, 0.5f);
                rtIc.anchoredPosition = new Vector2(posX[i], -12f);
                rtIc.sizeDelta = new Vector2(148f, 240f);

                var imgIc = itemCardObj.AddComponent<Image>();
                imgIc.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                // Icono
                var icObj = new GameObject("Icono");
                icObj.transform.SetParent(itemCardObj.transform, false);
                var rtIco = icObj.AddComponent<RectTransform>();
                rtIco.anchorMin = new Vector2(0.5f, 1f);
                rtIco.anchorMax = new Vector2(0.5f, 1f);
                rtIco.pivot = new Vector2(0.5f, 1f);
                rtIco.anchoredPosition = new Vector2(0f, -14f);
                rtIco.sizeDelta = new Vector2(58f, 58f);
                var imgSprite = icObj.AddComponent<Image>();
                imgSprite.preserveAspect = true;

                // Nombre
                var nomObj = new GameObject("Nombre");
                nomObj.transform.SetParent(itemCardObj.transform, false);
                var rtNom = nomObj.AddComponent<RectTransform>();
                rtNom.anchorMin = new Vector2(0.5f, 1f);
                rtNom.anchorMax = new Vector2(0.5f, 1f);
                rtNom.pivot = new Vector2(0.5f, 1f);
                rtNom.anchoredPosition = new Vector2(0f, -78f);
                rtNom.sizeDelta = new Vector2(140f, 32f);
                var txtNom = nomObj.AddComponent<Text>();
                txtNom.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtNom.fontSize = 11;
                txtNom.fontStyle = FontStyle.Bold;
                txtNom.alignment = TextAnchor.MiddleCenter;
                txtNom.color = Color.white;

                // Descripción
                var descObj = new GameObject("Desc");
                descObj.transform.SetParent(itemCardObj.transform, false);
                var rtDesc = descObj.AddComponent<RectTransform>();
                rtDesc.anchorMin = new Vector2(0.5f, 1f);
                rtDesc.anchorMax = new Vector2(0.5f, 1f);
                rtDesc.pivot = new Vector2(0.5f, 1f);
                rtDesc.anchoredPosition = new Vector2(0f, -114f);
                rtDesc.sizeDelta = new Vector2(136f, 64f);
                var txtDesc = descObj.AddComponent<Text>();
                txtDesc.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtDesc.fontSize = 9;
                txtDesc.alignment = TextAnchor.MiddleCenter;
                txtDesc.color = new Color(0.72f, 0.85f, 0.95f);

                // Botón Comprar
                var btnBuyObj = new GameObject("BotonComprar");
                btnBuyObj.transform.SetParent(itemCardObj.transform, false);
                var rtBtn = btnBuyObj.AddComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 12f);
                rtBtn.sizeDelta = new Vector2(134f, 36f);

                var imgBtn = btnBuyObj.AddComponent<Image>();
                imgBtn.color = new Color(0.20f, 0.55f, 0.35f, 1f);
                var btn = btnBuyObj.AddComponent<Button>();

                var txtBtnObj = new GameObject("TextoBtn");
                txtBtnObj.transform.SetParent(btnBuyObj.transform, false);
                var rtTb = txtBtnObj.AddComponent<RectTransform>();
                rtTb.anchorMin = Vector2.zero;
                rtTb.anchorMax = Vector2.one;
                rtTb.sizeDelta = Vector2.zero;
                var txtBtn = txtBtnObj.AddComponent<Text>();
                txtBtn.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtBtn.fontSize = 11;
                txtBtn.fontStyle = FontStyle.Bold;
                txtBtn.alignment = TextAnchor.MiddleCenter;
                txtBtn.color = Color.white;
                txtBtn.text = "COMPRAR";

                btn.onClick.AddListener(() => ComprarArticuloTiendaIndex(itemIndex));
                _botonesCompraTienda.Add(btn);
                _textosBotonTienda.Add(txtBtn);
            }

            // Botón Salir de la Tienda
            var btnSalirObj = new GameObject("BotonSalirTienda");
            btnSalirObj.transform.SetParent(cardObj.transform, false);
            var rtSalir = btnSalirObj.AddComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(0.5f, 0f);
            rtSalir.anchorMax = new Vector2(0.5f, 0f);
            rtSalir.pivot = new Vector2(0.5f, 0f);
            rtSalir.anchoredPosition = new Vector2(0f, 14f);
            rtSalir.sizeDelta = new Vector2(260f, 38f);

            var imgSalir = btnSalirObj.AddComponent<Image>();
            imgSalir.color = new Color(0.48f, 0.18f, 0.18f, 1f);
            var btnSalir = btnSalirObj.AddComponent<Button>();
            btnSalir.onClick.AddListener(CerrarTienda);

            var txtSalirObj = new GameObject("Texto");
            txtSalirObj.transform.SetParent(btnSalirObj.transform, false);
            var rtTs = txtSalirObj.AddComponent<RectTransform>();
            rtTs.anchorMin = Vector2.zero;
            rtTs.anchorMax = Vector2.one;
            rtTs.sizeDelta = Vector2.zero;
            var txtSalir = txtSalirObj.AddComponent<Text>();
            txtSalir.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSalir.fontSize = 12;
            txtSalir.fontStyle = FontStyle.Bold;
            txtSalir.alignment = TextAnchor.MiddleCenter;
            txtSalir.color = Color.white;
            txtSalir.text = "✖ SALIR DE LA TIENDA (ESC)";

            panelTienda = tiendaObj;
            panelTienda.SetActive(false);
        }

        public void AbrirTienda(PuestitoTienda puestito)
        {
            if (puestito == null) return;
            if (panelPausa != null && panelPausa.activeSelf) return;
            if (panelFinPartida != null && panelFinPartida.activeSelf) return;

            CrearPanelTiendaSiNoExiste();
            _puestitoActivo = puestito;
            panelTienda.SetActive(true);
            Controles.Bloqueado = true;
            ActualizarItemsTiendaUI();
        }

        public void CerrarTienda()
        {
            if (panelTienda != null)
            {
                panelTienda.SetActive(false);
            }
            Controles.Bloqueado = false;
            _puestitoActivo = null;
        }

        public void ActualizarItemsTiendaUI()
        {
            if (_puestitoActivo == null || panelTienda == null || !panelTienda.activeSelf) return;

            int oroLocal = (_personajeLocal != null) ? _personajeLocal.OroRun.Value : 0;
            if (_textoOroTienda != null)
            {
                _textoOroTienda.text = $"💰 Oro Disponible en la Run: {oroLocal}";
            }

            var cardObj = panelTienda.transform.Find("VentanaTienda");
            if (cardObj == null) return;

            for (int i = 0; i < _puestitoActivo.articulos.Count && i < _botonesCompraTienda.Count; i++)
            {
                var art = _puestitoActivo.articulos[i];
                var card = cardObj.Find($"CardArticulo_{i}");
                if (card != null)
                {
                    var img = card.Find("Icono")?.GetComponent<Image>();
                    if (img != null) img.sprite = ItemMejoraRun.ObtenerSpriteMejora(art.tipo);

                    var txtNom = card.Find("Nombre")?.GetComponent<Text>();
                    if (txtNom != null) txtNom.text = art.nombre;

                    var txtDesc = card.Find("Desc")?.GetComponent<Text>();
                    if (txtDesc != null) txtDesc.text = $"{art.descripcion}\n\n<color=#ffd700><b>{art.costoOro} 💰</b></color>";
                }

                var btn = _botonesCompraTienda[i];
                var txtBtn = _textosBotonTienda[i];
                var imgBtn = btn != null ? btn.GetComponent<Image>() : null;

                if (btn != null && txtBtn != null)
                {
                    if (art.comprado)
                    {
                        btn.interactable = false;
                        txtBtn.text = "✔ COMPRADO";
                        txtBtn.color = new Color(0.7f, 0.7f, 0.7f);
                        if (imgBtn != null) imgBtn.color = new Color(0.18f, 0.20f, 0.24f, 1f);
                    }
                    else if (oroLocal < art.costoOro)
                    {
                        btn.interactable = false;
                        txtBtn.text = $"{art.costoOro} 💰 (Falta oro)";
                        txtBtn.color = new Color(1f, 0.6f, 0.6f);
                        if (imgBtn != null) imgBtn.color = new Color(0.35f, 0.18f, 0.18f, 1f);
                    }
                    else
                    {
                        btn.interactable = true;
                        txtBtn.text = $"COMPRAR ({art.costoOro} 💰)";
                        txtBtn.color = Color.white;
                        if (imgBtn != null) imgBtn.color = new Color(0.18f, 0.60f, 0.35f, 1f);
                    }
                }
            }
        }

        private void ComprarArticuloTiendaIndex(int index)
        {
            if (_puestitoActivo == null || _personajeLocal == null) return;
            if (index < 0 || index >= _puestitoActivo.articulos.Count) return;

            var art = _puestitoActivo.articulos[index];
            if (art.comprado) return;

            if (_personajeLocal.OroRun.Value >= art.costoOro)
            {
                bool exito = _personajeLocal.SolicitarCompraTienda(art.tipo, art.costoOro);
                if (exito)
                {
                    _puestitoActivo.MarcarArticuloComprado(index);
                    ActualizarItemsTiendaUI();
                }
            }
        }

        private void AplicarTipografiasPixelArt()
        {
            Font pixelFont = FuenteUtil.ObtenerFuentePixelArt();
            if (pixelFont == null) return;

            foreach (var txt in GetComponentsInChildren<Text>(true))
            {
                if (txt != null)
                {
                    txt.font = pixelFont;
                }
            }
        }

        private void ActualizarHUDJugadorLocal()
        {
            if (textoMiNombreHUD == null)
            {
                AsegurarTextoMiNombreHUD();
            }

            string miNombre = "Jugador";
            if (_personajeLocal != null)
            {
                miNombre = _personajeLocal.ObtenerNombreJugador();
            }
            else if (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
            {
                miNombre = Global.Instancia.NombreLocal.Trim();
            }

            bool caidoLocal = _personajeLocal != null && _personajeLocal.EstaMuerto.Value;

            if (textoMiNombreHUD != null)
            {
                textoMiNombreHUD.text = caidoLocal ? $"💀 {miNombre} (CAÍDO)" : $"👤 {miNombre}";
                textoMiNombreHUD.color = caidoLocal
                    ? new Color(1f, 0.42f, 0.42f, 1f)
                    : new Color(0.95f, 0.98f, 1f, 1f);
            }

            if (_personajeLocal != null)
            {
                if (barraVida != null)
                {
                    int hpMax = (_personajeLocal.VidaMaxSincronizada.Value > 0)
                        ? _personajeLocal.VidaMaxSincronizada.Value
                        : Mathf.Max(1, _personajeLocal.vidaMax);
                    int hpActual = caidoLocal ? 0 : Mathf.Clamp(_personajeLocal.VidaActual.Value, 0, hpMax);
                    barraVida.Establecer(hpActual, hpMax, false);
                    if (barraVida.textoVida != null)
                    {
                        barraVida.textoVida.text = $"{hpActual} / {hpMax} HP";
                    }
                }

                if (_personajeLocal.nivelProgreso != null && barraXPRun != null)
                {
                    ActualizarProgresoRun(
                        _personajeLocal.nivelProgreso.NivelActual.Value,
                        _personajeLocal.nivelProgreso.XPActual.Value,
                        _personajeLocal.nivelProgreso.XPSiguienteNivel.Value);
                }

                if (barraMaestria != null && _personajeLocal.TipoPj != TipoPersonaje.Ninguno)
                {
                    ActualizarBarraMaestria(_personajeLocal.TipoPj);
                }

                if (textoOroRun == null)
                {
                    AsegurarTextoOroRun();
                }

                if (textoOroRun != null)
                {
                    textoOroRun.text = $"💰 {_personajeLocal.OroRun.Value} ORO";
                }
            }
        }

        private class SlotCompaneroHUD
        {
            public ulong idCliente;
            public GameObject contenedor;
            public RectTransform rtContenedor;
            public Text textoNombre;
            public Image rellenoVida;
            public RectTransform rtRellenoVida;
            public Text textoVidaNumerica;
        }

        private readonly System.Collections.Generic.List<SlotCompaneroHUD> _slotsCompaneros = new System.Collections.Generic.List<SlotCompaneroHUD>();

        private void ActualizarCompanerosArribaIzquierda()
        {
            bool redActiva = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening;

            // Recolectar todos los compañeros (personajes que no sean el jugador local)
            System.Collections.Generic.List<Personaje> companeros = new System.Collections.Generic.List<Personaje>();
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var p = Personaje.Todos[i];
                if (p == null) continue;
                if (redActiva && !p.IsSpawned) continue;
                if (p == _personajeLocal || p.EsControlLocal) continue;
                companeros.Add(p);
            }

            companeros.Sort((a, b) =>
            {
                int cmp = a.OwnerClientId.CompareTo(b.OwnerClientId);
                return cmp != 0 ? cmp : a.NetworkObjectId.CompareTo(b.NetworkObjectId);
            });

            // Eliminar slots de compañeros que ya no existen (asociados por NetworkObjectId exacto del Personaje)
            for (int i = _slotsCompaneros.Count - 1; i >= 0; i--)
            {
                bool sigueActivo = false;
                for (int j = 0; j < companeros.Count; j++)
                {
                    ulong keyComp = companeros[j].IsSpawned ? companeros[j].NetworkObjectId : (ulong)companeros[j].GetInstanceID();
                    if (keyComp == _slotsCompaneros[i].idCliente)
                    {
                        sigueActivo = true;
                        break;
                    }
                }
                if (!sigueActivo)
                {
                    if (_slotsCompaneros[i].contenedor != null)
                    {
                        Destroy(_slotsCompaneros[i].contenedor);
                    }
                    _slotsCompaneros.RemoveAt(i);
                }
            }

            // Crear o actualizar cada compañero arriba a la izquierda (debajo de las barras del jugador local)
            for (int idx = 0; idx < companeros.Count; idx++)
            {
                var comp = companeros[idx];
                ulong keyComp = comp.IsSpawned ? comp.NetworkObjectId : (ulong)comp.GetInstanceID();

                SlotCompaneroHUD slot = null;
                for (int s = 0; s < _slotsCompaneros.Count; s++)
                {
                    if (_slotsCompaneros[s].idCliente == keyComp)
                    {
                        slot = _slotsCompaneros[s];
                        break;
                    }
                }

                if (slot == null || slot.contenedor == null)
                {
                    slot = CrearSlotCompaneroArribaIzquierda(keyComp);
                    _slotsCompaneros.Add(slot);
                }

                // Posicionar en lista vertical arriba a la izquierda (debajo de BarraMaestriaHUD)
                if (slot.rtContenedor != null)
                {
                    slot.rtContenedor.anchoredPosition = new Vector2(25f, -68f - (idx * 50f));
                }

                string nombreComp = comp.ObtenerNombreJugador();
                bool caido = comp.EstaMuerto.Value;
                int hpMax = (comp.VidaMaxSincronizada.Value > 0) ? comp.VidaMaxSincronizada.Value : Mathf.Max(1, comp.vidaMax);
                int hpActual = caido ? 0 : Mathf.Clamp(comp.VidaActual.Value, 0, hpMax);
                float ratio = Mathf.Clamp01((float)hpActual / Mathf.Max(1, hpMax));

                if (slot.textoNombre != null)
                {
                    slot.textoNombre.text = caido ? $"💀 {nombreComp} (CAÍDO)" : $"👤 {nombreComp}";
                    slot.textoNombre.color = caido ? new Color(1f, 0.42f, 0.42f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
                }

                if (slot.rellenoVida != null)
                {
                    slot.rellenoVida.fillAmount = ratio;
                    slot.rellenoVida.color = caido
                        ? new Color(0.45f, 0.12f, 0.12f, 1f)
                        : (ratio > 0.35f ? new Color(0.22f, 0.85f, 0.38f, 1f) : new Color(0.95f, 0.55f, 0.15f, 1f));
                }

                if (slot.rtRellenoVida != null)
                {
                    slot.rtRellenoVida.anchorMin = Vector2.zero;
                    slot.rtRellenoVida.anchorMax = new Vector2(ratio, 1f);
                    slot.rtRellenoVida.offsetMin = new Vector2(2f, 2f);
                    slot.rtRellenoVida.offsetMax = new Vector2(-2f, -2f);
                }

                if (slot.textoVidaNumerica != null)
                {
                    slot.textoVidaNumerica.text = caido ? "CAÍDO - [Mantén F cerca]" : $"{hpActual} / {hpMax}";
                }
            }
        }

        private SlotCompaneroHUD CrearSlotCompaneroArribaIzquierda(ulong idCliente)
        {
            var contObj = new GameObject($"CompaneroHUD_{idCliente}");
            contObj.transform.SetParent(transform, false);

            var rtCont = contObj.AddComponent<RectTransform>();
            rtCont.anchorMin = new Vector2(0f, 1f);
            rtCont.anchorMax = new Vector2(0f, 1f);
            rtCont.pivot = new Vector2(0f, 1f);
            rtCont.sizeDelta = new Vector2(270f, 44f);

            var imgPanel = contObj.AddComponent<Image>();
            imgPanel.color = new Color(0.05f, 0.07f, 0.11f, 0.86f);
            imgPanel.raycastTarget = false;

            // Nombre del compañero arriba de su barra
            var nomObj = new GameObject("NombreCompanero");
            nomObj.transform.SetParent(contObj.transform, false);
            var rtNom = nomObj.AddComponent<RectTransform>();
            rtNom.anchorMin = new Vector2(0f, 1f);
            rtNom.anchorMax = new Vector2(1f, 1f);
            rtNom.pivot = new Vector2(0.5f, 1f);
            rtNom.anchoredPosition = new Vector2(0f, -2f);
            rtNom.sizeDelta = new Vector2(-12f, 18f);

            var txtNom = nomObj.AddComponent<Text>();
            txtNom.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtNom.fontSize = 13;
            txtNom.fontStyle = FontStyle.Bold;
            txtNom.alignment = TextAnchor.MiddleLeft;
            txtNom.color = new Color(0.85f, 0.95f, 1f, 1f);
            txtNom.raycastTarget = false;

            // Fondo de la barra de vida del compañero
            var barraFondoObj = new GameObject("FondoBarraCompanero");
            barraFondoObj.transform.SetParent(contObj.transform, false);
            var rtBarra = barraFondoObj.AddComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 0f);
            rtBarra.anchorMax = new Vector2(1f, 0f);
            rtBarra.pivot = new Vector2(0.5f, 0f);
            rtBarra.anchoredPosition = new Vector2(0f, 4f);
            rtBarra.sizeDelta = new Vector2(-12f, 18f);

            var imgFondoBarra = barraFondoObj.AddComponent<Image>();
            imgFondoBarra.color = new Color(0.18f, 0.05f, 0.05f, 0.95f);
            imgFondoBarra.raycastTarget = false;

            // Relleno de vida en tiempo real
            var rellenoObj = new GameObject("RellenoVidaCompanero");
            rellenoObj.transform.SetParent(barraFondoObj.transform, false);
            var rtRelleno = rellenoObj.AddComponent<RectTransform>();
            rtRelleno.anchorMin = Vector2.zero;
            rtRelleno.anchorMax = Vector2.one;
            rtRelleno.offsetMin = new Vector2(2f, 2f);
            rtRelleno.offsetMax = new Vector2(-2f, -2f);

            var imgRelleno = rellenoObj.AddComponent<Image>();
            imgRelleno.color = new Color(0.22f, 0.85f, 0.38f, 1f);
            imgRelleno.raycastTarget = false;

            // Texto numérico de vida sobre la barra
            var txtHpObj = new GameObject("TextoVidaCompanero");
            txtHpObj.transform.SetParent(barraFondoObj.transform, false);
            var rtTxtHp = txtHpObj.AddComponent<RectTransform>();
            rtTxtHp.anchorMin = Vector2.zero;
            rtTxtHp.anchorMax = Vector2.one;
            rtTxtHp.sizeDelta = Vector2.zero;

            var txtHp = txtHpObj.AddComponent<Text>();
            txtHp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtHp.fontSize = 11;
            txtHp.fontStyle = FontStyle.Bold;
            txtHp.alignment = TextAnchor.MiddleCenter;
            txtHp.color = Color.white;
            txtHp.raycastTarget = false;

            return new SlotCompaneroHUD
            {
                idCliente = idCliente,
                contenedor = contObj,
                rtContenedor = rtCont,
                textoNombre = txtNom,
                rellenoVida = imgRelleno,
                rtRellenoVida = rtRelleno,
                textoVidaNumerica = txtHp
            };
        }

        private class SlotEfectoConsumibleHUD
        {
            public TipoMejoraRun tipo;
            public GameObject contenedor;
            public RectTransform rtContenedor;
            public Image icono;
            public Image veloOscuro;
            public RectTransform rtVeloOscuro;
            public float duracionTotal;
            public float tiempoRestante;
        }

        private readonly System.Collections.Generic.List<SlotEfectoConsumibleHUD> _efectosActivosDerecha = new System.Collections.Generic.List<SlotEfectoConsumibleHUD>();

        public void ActivarEfectoConsumibleDerecha(TipoMejoraRun tipo, float duracion)
        {
            if (duracion <= 0f) return;

            // Si ya existe el efecto activo a la derecha, refrescar su tiempo al 100%
            for (int i = 0; i < _efectosActivosDerecha.Count; i++)
            {
                if (_efectosActivosDerecha[i].tipo == tipo && _efectosActivosDerecha[i].contenedor != null)
                {
                    _efectosActivosDerecha[i].duracionTotal = duracion;
                    _efectosActivosDerecha[i].tiempoRestante = duracion;
                    return;
                }
            }

            // Crear recuadro de efecto en el borde izquierdo de la pantalla (debajo de nombre y maestría)
            var slotObj = new GameObject($"EfectoHUD_{tipo}");
            slotObj.transform.SetParent(transform, false);

            var rtSlot = slotObj.AddComponent<RectTransform>();
            rtSlot.anchorMin = new Vector2(0f, 1f);
            rtSlot.anchorMax = new Vector2(0f, 1f);
            rtSlot.pivot = new Vector2(0f, 1f);
            rtSlot.sizeDelta = new Vector2(46f, 46f);

            var imgBorde = slotObj.AddComponent<Image>();
            imgBorde.color = new Color(0.24f, 0.27f, 0.34f, 0.94f);
            imgBorde.raycastTarget = false;

            var fondoIntObj = new GameObject("FondoInterior");
            fondoIntObj.transform.SetParent(slotObj.transform, false);
            var rtFondoInt = fondoIntObj.AddComponent<RectTransform>();
            rtFondoInt.anchorMin = Vector2.zero;
            rtFondoInt.anchorMax = Vector2.one;
            rtFondoInt.offsetMin = new Vector2(3f, 3f);
            rtFondoInt.offsetMax = new Vector2(-3f, -3f);
            var imgFondoInt = fondoIntObj.AddComponent<Image>();
            imgFondoInt.color = new Color(0.07f, 0.08f, 0.12f, 0.94f);
            imgFondoInt.raycastTarget = false;

            // Dibujito del efecto consumible
            var iconoObj = new GameObject("DibujitoEfecto");
            iconoObj.transform.SetParent(fondoIntObj.transform, false);
            var rtIcono = iconoObj.AddComponent<RectTransform>();
            rtIcono.anchorMin = Vector2.zero;
            rtIcono.anchorMax = Vector2.one;
            rtIcono.offsetMin = new Vector2(4f, 4f);
            rtIcono.offsetMax = new Vector2(-4f, -4f);
            var imgIcono = iconoObj.AddComponent<Image>();
            imgIcono.sprite = ItemMejoraRun.ObtenerSpriteMejora(tipo);
            imgIcono.preserveAspect = true;
            imgIcono.color = Color.white;
            imgIcono.raycastTarget = false;

            // Velo de oscurecimiento progresivo que va cubriendo y oscureciendo el dibujito hasta desaparecer
            var veloObj = new GameObject("VeloOscurecimiento");
            veloObj.transform.SetParent(fondoIntObj.transform, false);
            var rtVelo = veloObj.AddComponent<RectTransform>();
            rtVelo.anchorMin = new Vector2(0f, 0f);
            rtVelo.anchorMax = new Vector2(1f, 0f);
            rtVelo.offsetMin = Vector2.zero;
            rtVelo.offsetMax = Vector2.zero;
            var imgVelo = veloObj.AddComponent<Image>();
            imgVelo.color = new Color(0f, 0f, 0f, 0.72f);
            imgVelo.raycastTarget = false;

            var nuevoSlot = new SlotEfectoConsumibleHUD
            {
                tipo = tipo,
                contenedor = slotObj,
                rtContenedor = rtSlot,
                icono = imgIcono,
                veloOscuro = imgVelo,
                rtVeloOscuro = rtVelo,
                duracionTotal = duracion,
                tiempoRestante = duracion
            };

            _efectosActivosDerecha.Add(nuevoSlot);
            ReposicionarEfectosDerecha();
        }

        public void LimpiarEfectosConsumiblesDerecha()
        {
            for (int i = _efectosActivosDerecha.Count - 1; i >= 0; i--)
            {
                if (_efectosActivosDerecha[i].contenedor != null)
                {
                    Destroy(_efectosActivosDerecha[i].contenedor);
                }
            }
            _efectosActivosDerecha.Clear();
        }

        private void ReposicionarEfectosDerecha()
        {
            for (int i = 0; i < _efectosActivosDerecha.Count; i++)
            {
                var s = _efectosActivosDerecha[i];
                if (s.rtContenedor != null)
                {
                    // Ubicados a la izquierda de la pantalla, debajo del nombre y barra de maestría
                    s.rtContenedor.anchoredPosition = new Vector2(25f, -65f - (i * 52f));
                }
            }
        }

        private void ActualizarEfectosConsumiblesDerecha(float dt)
        {
            if (_efectosActivosDerecha.Count == 0) return;

            bool huboEliminado = false;
            for (int i = _efectosActivosDerecha.Count - 1; i >= 0; i--)
            {
                var s = _efectosActivosDerecha[i];
                if (s == null || s.contenedor == null)
                {
                    _efectosActivosDerecha.RemoveAt(i);
                    huboEliminado = true;
                    continue;
                }

                s.tiempoRestante -= dt;
                if (s.tiempoRestante <= 0f)
                {
                    Destroy(s.contenedor);
                    _efectosActivosDerecha.RemoveAt(i);
                    huboEliminado = true;
                    continue;
                }

                // Fracción de tiempo restante (1 = recién obtenido, 0 = a punto de desaparecer)
                float ratio = Mathf.Clamp01(s.tiempoRestante / Mathf.Max(0.1f, s.duracionTotal));
                float fraccionGastada = 1f - ratio;

                // 1) El dibujito se va oscureciendo gradualmente conforme pasa el tiempo
                if (s.icono != null)
                {
                    float brillo = Mathf.Lerp(0.22f, 1.0f, ratio);
                    float alpha = 1.0f;
                    // En los últimos 3.5 segundos parpadea suavemente como en Minecraft antes de desaparecer
                    if (s.tiempoRestante < 3.5f)
                    {
                        alpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 7.5f));
                    }
                    s.icono.color = new Color(brillo, brillo, brillo, alpha);
                }

                // 2) El velo oscuro va subiendo/cubriendo el recuadro a medida que se agota
                if (s.rtVeloOscuro != null)
                {
                    s.rtVeloOscuro.anchorMin = new Vector2(0f, 1f - fraccionGastada);
                    s.rtVeloOscuro.anchorMax = new Vector2(1f, 1f);
                    s.rtVeloOscuro.offsetMin = Vector2.zero;
                    s.rtVeloOscuro.offsetMax = Vector2.zero;
                }
            }

            if (huboEliminado)
            {
                ReposicionarEfectosDerecha();
            }
        }

        public void ConectarPersonaje(Personaje personaje)
        {
            _personajeLocal = personaje;
            ConfigurarIconosPersonaje(personaje);
            ActualizarVida(personaje.VidaActual.Value, personaje.vidaMax);

            if (personaje.nivelProgreso != null)
            {
                ActualizarProgresoRun(personaje.nivelProgreso.NivelActual.Value, personaje.nivelProgreso.XPActual.Value, personaje.nivelProgreso.XPSiguienteNivel.Value);
                personaje.nivelProgreso.NivelActual.OnValueChanged += (ant, act) =>
                {
                    ActualizarProgresoRun(act, personaje.nivelProgreso.XPActual.Value, personaje.nivelProgreso.XPSiguienteNivel.Value);
                    ActualizarDesbloqueoHabilidadesVisual();
                };
                personaje.nivelProgreso.XPActual.OnValueChanged += (ant, act) =>
                {
                    ActualizarProgresoRun(personaje.nivelProgreso.NivelActual.Value, act, personaje.nivelProgreso.XPSiguienteNivel.Value);
                };
            }

            ActualizarBarraMaestria(personaje.TipoPj);
            ActualizarDesbloqueoHabilidadesVisual();
            ActualizarHabilidadesPausaUI();
        }

        public void ActualizarBarraMaestria(TipoPersonaje pj)
        {
            if (barraMaestria == null || pj == TipoPersonaje.Ninguno) return;
            int xpTotal = GestorMaestria.ObtenerPuntos(pj);
            int nivelMaestria = GestorMaestria.ObtenerNivelMaestria(pj);
            bool esMax = nivelMaestria >= GestorMaestria.NIVEL_MAXIMO_MAESTRIA;
            int xpEnNivel = esMax ? GestorMaestria.XP_POR_NIVEL_MAESTRIA : (xpTotal % GestorMaestria.XP_POR_NIVEL_MAESTRIA);
            barraMaestria.Establecer(xpEnNivel, GestorMaestria.XP_POR_NIVEL_MAESTRIA, false);
            if (barraMaestria.textoVida != null)
            {
                barraMaestria.textoVida.text = esMax
                    ? $"Maestría Nv.{nivelMaestria} (MÁXIMO)"
                    : $"Maestría Nv.{nivelMaestria} ({xpEnNivel}/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP)";
            }
        }

        public void ActualizarProgresoRun(int nivel, int xpActual, int xpMax)
        {
            if (barraXPRun == null) return;
            bool esMax = nivel >= NivelEnPartida.NivelMaximo;
            int xpMostrar = esMax ? Mathf.Max(1, xpMax) : xpActual;
            barraXPRun.Establecer(xpMostrar, Mathf.Max(1, xpMax), false);
            if (barraXPRun.textoVida != null)
            {
                barraXPRun.textoVida.text = esMax 
                    ? $"⭐ NIVEL RUN {NivelEnPartida.NivelMaximo} (MÁXIMO)" 
                    : $"Nv.{nivel} ({xpActual}/{xpMax} XP)";
            }
        }



        private void OnMaestriaActualizada(TipoPersonaje tipo, int totalXP)
        {
            if (_personajeLocal != null && _personajeLocal.TipoPj == tipo)
            {
                ActualizarBarraMaestria(tipo);
                ActualizarDesbloqueoHabilidadesVisual();
            }
        }

        public void ActualizarDesbloqueoHabilidadesVisual()
        {
            if (_personajeLocal == null || slotsHabilidades == null) return;
            int nivelRun = _personajeLocal.nivelProgreso != null ? _personajeLocal.nivelProgreso.NivelActual.Value : 1;

            for (int i = 0; i < slotsHabilidades.Length; i++)
            {
                var slot = slotsHabilidades[i];
                if (slot == null || slot.icono == null) continue;

                IdHabilidad habId = ObtenerIdHabilidadDeSlot(_personajeLocal, i);
                if (habId == IdHabilidad.Ninguna) continue;

                var def = CatalogoHabilidades.Obtener(habId);
                if (def == null) continue;

                // 1. Condición Absoluta: Desbloqueada por Maestría permanente
                bool desbloqueadaPorMaestria = GestorMaestria.EstaDesbloqueada(_personajeLocal.TipoPj, habId);
                if (!desbloqueadaPorMaestria)
                {
                    // Totalmente oscurecida porque aún no se alcanza la maestría necesaria
                    slot.icono.color = new Color(0.12f, 0.12f, 0.16f, 0.38f);
                    if (slot.textoTiempo != null)
                    {
                        slot.textoTiempo.fontSize = 10;
                        slot.textoTiempo.text = "🔒 BLOQ";
                        slot.textoTiempo.color = new Color(1f, 0.70f, 0.25f);
                    }
                    continue;
                }

                // 2. Condición Temporal: Nivel dentro de la run actual
                bool bloqueadaPorRun = nivelRun < def.nivelRequeridoRun;
                if (bloqueadaPorRun)
                {
                    // Oscurecida según el nivel de la run
                    slot.icono.color = new Color(0.25f, 0.25f, 0.30f, 0.65f);
                    if (slot.textoTiempo != null)
                    {
                        slot.textoTiempo.fontSize = 11;
                        slot.textoTiempo.text = $"Nv.{def.nivelRequeridoRun}";
                        slot.textoTiempo.color = new Color(0.85f, 0.85f, 0.85f);
                    }
                    continue;
                }

                // 3. Desbloqueada y disponible para usar
                slot.icono.color = Color.white;
                if (slot.textoTiempo != null && (slot.textoTiempo.text.StartsWith("Nv.") || slot.textoTiempo.text.Contains("BLOQ") || slot.textoTiempo.text.Contains("MAESTRÍA")))
                {
                    slot.textoTiempo.text = "";
                }
            }
        }

        public IdHabilidad ObtenerIdHabilidadDeSlot(Personaje personaje, int index)
        {
            if (personaje == null) return IdHabilidad.Ninguna;
            return personaje.TipoPj switch
            {
                TipoPersonaje.Fuego => index switch
                {
                    0 => IdHabilidad.Fuego_BolaFuego,
                    1 => IdHabilidad.Fuego_Lanzallamas,
                    2 => IdHabilidad.Fuego_Armadura,
                    3 => IdHabilidad.Fuego_Meteorito,
                    _ => IdHabilidad.Ninguna
                },
                TipoPersonaje.Hielo => index switch
                {
                    0 => IdHabilidad.Hielo_RayoHielo,
                    1 => IdHabilidad.Hielo_BolaHielo,
                    2 => IdHabilidad.Hielo_Armadura,
                    3 => IdHabilidad.Hielo_EraHielo,
                    _ => IdHabilidad.Ninguna
                },
                TipoPersonaje.Electrico => index switch
                {
                    0 => IdHabilidad.Elec_Espada,
                    1 => IdHabilidad.Elec_Dash,
                    2 => IdHabilidad.Elec_Teleport,
                    3 => IdHabilidad.Elec_Rayo,
                    _ => IdHabilidad.Ninguna
                },
                _ => IdHabilidad.Ninguna
            };
        }

        public void ConfigurarIconosPersonaje(Personaje personaje)
        {
            if (personaje == null || slotsHabilidades == null) return;

            if (personaje is PersonajeFuego)
            {
                ConfigurarSlot(0, iconoFuegoLMB, "LMB", true);
                ConfigurarSlot(1, iconoFuegoE, "E", true);
                ConfigurarSlot(2, iconoFuegoQ, "Q", true);
                ConfigurarSlot(3, iconoFuegoR, "R", true);
            }
            else if (personaje is PersonajeHielo)
            {
                ConfigurarSlot(0, iconoHieloLMB, "LMB", true);
                ConfigurarSlot(1, iconoHieloE, "E", true);
                ConfigurarSlot(2, iconoHieloQ, "Q", true);
                ConfigurarSlot(3, iconoHieloR, "R", true);
            }
            else if (personaje is PersonajeElectrico)
            {
                ConfigurarSlot(0, iconoElecLMB, "LMB", true);
                ConfigurarSlot(1, iconoElecE, "E", true);
                ConfigurarSlot(2, iconoElecQ, "Q", true);
                ConfigurarSlot(3, iconoElecR, "R", true);
            }
        }

        private void ConfigurarSlot(int index, Sprite sprite, string tecla, bool activo)
        {
            if (slotsHabilidades == null || index < 0 || index >= slotsHabilidades.Length) return;

            var slot = slotsHabilidades[index];
            if (slot == null) return;

            if (slot.contenedor != null)
            {
                slot.contenedor.SetActive(activo);
            }

            if (!activo) return;

            if (slot.icono != null)
            {
                if (sprite != null)
                {
                    slot.icono.sprite = sprite;
                    slot.icono.enabled = true;
                }
                slot.icono.color = Color.white;
            }

            if (slot.textoTecla != null)
            {
                slot.textoTecla.text = tecla;
            }

            if (slot.sombraCooldown != null)
            {
                slot.sombraCooldown.fillAmount = 0f;
                slot.sombraCooldown.gameObject.SetActive(false);
            }

            if (slot.textoTiempo != null)
            {
                slot.textoTiempo.text = "";
            }
        }

        public void ActualizarVida(int actual, int max)
        {
            if (barraVida != null)
            {
                barraVida.Establecer(actual, max, false);
                if (barraVida.textoVida != null)
                {
                    barraVida.textoVida.text = $"{actual} / {max} HP";
                }
            }
        }

        public void ActualizarCooldown(int index, float tiempoRestante, float tiempoTotal)
        {
            // Actualización de slots avanzados
            if (slotsHabilidades != null && index >= 0 && index < slotsHabilidades.Length)
            {
                var slot = slotsHabilidades[index];
                if (slot != null && _personajeLocal != null)
                {
                    IdHabilidad habId = ObtenerIdHabilidadDeSlot(_personajeLocal, index);
                    if (habId != IdHabilidad.Ninguna)
                    {
                        var def = CatalogoHabilidades.Obtener(habId);
                        if (def != null)
                        {
                            // 1. Maestría
                            if (!GestorMaestria.EstaDesbloqueada(_personajeLocal.TipoPj, habId))
                            {
                                if (slot.icono != null) slot.icono.color = new Color(0.12f, 0.12f, 0.16f, 0.38f);
                                if (slot.textoTiempo != null)
                                {
                                    slot.textoTiempo.fontSize = 10;
                                    slot.textoTiempo.text = "🔒 BLOQ";
                                    slot.textoTiempo.color = new Color(1f, 0.70f, 0.25f);
                                }
                                return;
                            }

                            // 2. Nivel en run
                            int nivelRun = _personajeLocal.nivelProgreso != null ? _personajeLocal.nivelProgreso.NivelActual.Value : 1;
                            if (nivelRun < def.nivelRequeridoRun)
                            {
                                if (slot.icono != null) slot.icono.color = new Color(0.25f, 0.25f, 0.30f, 0.65f);
                                if (slot.textoTiempo != null)
                                {
                                    slot.textoTiempo.fontSize = 11;
                                    slot.textoTiempo.text = $"Nv.{def.nivelRequeridoRun}";
                                    slot.textoTiempo.color = new Color(0.85f, 0.85f, 0.85f);
                                }
                                return;
                            }
                        }
                    }

                    bool enCooldown = tiempoRestante > 0.05f;

                    // Oscurecer el sprite claramente al usar la habilidad, y dejarlo 100% brillante al estar listo
                    if (slot.icono != null)
                    {
                        slot.icono.color = enCooldown ? new Color(0.32f, 0.32f, 0.32f, 1f) : Color.white;
                    }

                    // Sombra de barrido radial
                    if (slot.sombraCooldown != null)
                    {
                        if (enCooldown)
                        {
                            slot.sombraCooldown.gameObject.SetActive(true);
                            float factor = (tiempoTotal > 0f) ? Mathf.Clamp01(tiempoRestante / tiempoTotal) : 0f;
                            slot.sombraCooldown.fillAmount = factor;
                        }
                        else
                        {
                            slot.sombraCooldown.fillAmount = 0f;
                            slot.sombraCooldown.gameObject.SetActive(false);
                        }
                    }

                    // Contador numérico de segundos
                    if (slot.textoTiempo != null)
                    {
                        if (enCooldown)
                        {
                            slot.textoTiempo.fontSize = 13;
                            slot.textoTiempo.color = Color.white;
                            slot.textoTiempo.text = tiempoRestante >= 10f 
                                ? Mathf.CeilToInt(tiempoRestante).ToString() 
                                : tiempoRestante.ToString("F1");
                        }
                        else
                        {
                            slot.textoTiempo.text = "";
                        }
                    }
                }
            }

            // Compatibilidad hacia atrás si hay iconos enlazados directamente
            if (iconosHabilidades != null && index >= 0 && index < iconosHabilidades.Length)
            {
                Image icono = iconosHabilidades[index];
                if (icono != null && tiempoTotal > 0f)
                {
                    icono.fillAmount = Mathf.Clamp01(tiempoRestante / tiempoTotal);
                }
            }
        }

        public void AlternarPausa()
        {
            if (panelPausa == null) return;
            if (panelFinPartida != null && panelFinPartida.activeSelf) return;

            bool estado = !panelPausa.activeSelf;
            panelPausa.SetActive(estado);
            Controles.Bloqueado = estado;

            // Ocultar el minimapa mientras el menú de pausa esté abierto
            if (minimapaHUD != null)
            {
                minimapaHUD.gameObject.SetActive(!estado);
            }

            // Modo solitario: pausar el tiempo del juego si solo hay 1 jugador
            bool esSolitario = Unity.Netcode.NetworkManager.Singleton == null 
                || !Unity.Netcode.NetworkManager.Singleton.IsListening 
                || Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds.Count <= 1;

            if (esSolitario)
            {
                Time.timeScale = estado ? 0f : 1f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (estado)
            {
                panelPausa.transform.SetAsLastSibling();
                panelPausa.SetActive(true);

                var rtP = panelPausa.GetComponent<RectTransform>();
                if (rtP != null)
                {
                    rtP.anchorMin = Vector2.zero;
                    rtP.anchorMax = Vector2.one;
                    rtP.sizeDelta = Vector2.zero;
                    rtP.anchoredPosition = Vector2.zero;
                }

                try
                {
                    ConfigurarLayoutMenuPausa();
                    ActualizarVisibilidadBotonLobbyPausa();

                    Canvas.ForceUpdateCanvases();
                    if (rtP != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rtP);
                    }

                    ActualizarHabilidadesPausaUI();
                    ActualizarMapaPausa();

                    Canvas.ForceUpdateCanvases();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HUD] Excepción al actualizar menú de pausa: {ex}");
                }
            }
            else
            {
                panelPausa.SetActive(false);
                if (PanelOpcionesControles.Instancia != null)
                {
                    PanelOpcionesControles.Instancia.Cerrar();
                }
            }
        }

        public void CerrarPausa()
        {
            if (panelPausa) panelPausa.SetActive(false);
            Controles.Bloqueado = false;
            Time.timeScale = 1f;

            if (minimapaHUD != null)
            {
                minimapaHUD.gameObject.SetActive(true);
            }

            if (PanelOpcionesControles.Instancia != null)
            {
                PanelOpcionesControles.Instancia.Cerrar();
            }
        }

        public void SalirAlMenuPrincipal()
        {
            CerrarTienda();
            PuestitoTienda.DestruirTodos();
            Controles.Bloqueado = false;
            Time.timeScale = 1f;
            if (Global.Instancia != null)
            {
                Global.Instancia.SalirAlMenu("Has salido de la partida.");
            }
        }

        /// <summary>
        /// Llamado al pulsar "Volver al Lobby" desde el Menú de Pausa a media run (inicia votación si hay varios jugadores).
        /// </summary>
        public void VolverAlLobbyDesdePausa()
        {
            CerrarTienda();
            PuestitoTienda.DestruirTodos();
            CerrarPausa();

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SolicitarVolverAlLobbyServerRpc();
            }
            else
            {
                SalirAlMenuPrincipal();
            }
        }

        /// <summary>
        /// Llamado al pulsar "Volver al Lobby" en la pantalla de Fin de Partida (cuando mueren todos).
        /// Manda a cada jugador individualmente al Lobby según lo vaya pulsando, sin votación.
        /// </summary>
        public void VolverAlLobbyFinPartidaIndividual()
        {
            CerrarTienda();
            PuestitoTienda.DestruirTodos();
            SalaRed.MostrarFinPartidaPendiente = false;
            Controles.Bloqueado = false;

            if (panelFinPartida != null)
            {
                panelFinPartida.SetActive(false);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SolicitarVolverAlLobbyFinPartidaServerRpc();
            }
            else
            {
                SalirAlMenuPrincipal();
            }
        }

        public void VolverAlLobby()
        {
            if (panelFinPartida != null && panelFinPartida.activeSelf)
            {
                VolverAlLobbyFinPartidaIndividual();
            }
            else
            {
                VolverAlLobbyDesdePausa();
            }
        }

        public void MostrarPanelVotacion(ulong idIniciador = ulong.MaxValue)
        {
            CrearPanelVotacionSiNoExiste();
            _yaVoteLocalmente = false;

            if (panelVotacion != null)
            {
                panelVotacion.SetActive(true);
            }

            if (botonVotarSi != null) botonVotarSi.interactable = true;
            if (botonVotarNo != null) botonVotarNo.interactable = true;

            // Si soy quien inició la votación, ya voté que SÍ automáticamente
            ulong miId = Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
            if (idIniciador == miId)
            {
                _yaVoteLocalmente = true;
                if (botonVotarSi != null) botonVotarSi.interactable = false;
                if (botonVotarNo != null) botonVotarNo.interactable = false;
            }
        }

        public void Votar(bool votoSi)
        {
            if (_yaVoteLocalmente) return;
            _yaVoteLocalmente = true;

            if (botonVotarSi != null) botonVotarSi.interactable = false;
            if (botonVotarNo != null) botonVotarNo.interactable = false;

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.RegistrarVotoLobbyServerRpc(votoSi);
            }
        }

        public void CerrarPanelVotacion(bool aprobada)
        {
            if (panelVotacion != null)
            {
                panelVotacion.SetActive(false);
            }
        }

        private GameObject _modalAdvertenciaBucle;
        public void MostrarAdvertenciaBucle(Action onConfirmar)
        {
            if (_modalAdvertenciaBucle != null)
            {
                Destroy(_modalAdvertenciaBucle);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var modalObj = new GameObject("ModalAdvertenciaBucle");
            modalObj.transform.SetParent(transform, false);
            var rt = modalObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var imgFondoOscuro = modalObj.AddComponent<Image>();
            imgFondoOscuro.color = new Color(0f, 0f, 0f, 0.75f);

            // Ventana central
            var cajaObj = new GameObject("CajaModal");
            cajaObj.transform.SetParent(modalObj.transform, false);
            var rtCaja = cajaObj.AddComponent<RectTransform>();
            rtCaja.anchorMin = new Vector2(0.5f, 0.5f);
            rtCaja.anchorMax = new Vector2(0.5f, 0.5f);
            rtCaja.sizeDelta = new Vector2(560, 370);

            var imgCaja = cajaObj.AddComponent<Image>();
            imgCaja.color = new Color(0.10f, 0.12f, 0.16f, 0.98f);

            var outline = cajaObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.65f, 0.15f, 1f);
            outline.effectDistance = new Vector2(3, 3);

            // Título
            var titObj = new GameObject("Titulo");
            titObj.transform.SetParent(cajaObj.transform, false);
            var rtTit = titObj.AddComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0f, 1f);
            rtTit.anchorMax = new Vector2(1f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0, -18);
            rtTit.sizeDelta = new Vector2(-40, 42);

            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = font;
            txtTit.fontSize = 24;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.8f, 0.2f);
            txtTit.text = "⚠️ ¡DESAFÍO DEL BUCLE INFINITO! ⚠️";

            // Mensaje explicativo
            var msgObj = new GameObject("Mensaje");
            msgObj.transform.SetParent(cajaObj.transform, false);
            var rtMsg = msgObj.AddComponent<RectTransform>();
            rtMsg.anchorMin = new Vector2(0f, 0f);
            rtMsg.anchorMax = new Vector2(1f, 1f);
            rtMsg.offsetMin = new Vector2(25, 75);
            rtMsg.offsetMax = new Vector2(-25, -65);

            var txtMsg = msgObj.AddComponent<Text>();
            txtMsg.font = font;
            txtMsg.fontSize = 15;
            txtMsg.lineSpacing = 1.18f;
            txtMsg.alignment = TextAnchor.UpperLeft;
            txtMsg.color = Color.white;
            txtMsg.text =
                "Has conquistado las 3 recámaras de la mazmorra con éxito.\n\n" +
                "Si decides entrar al Bucle (Niveles 11 al 20+):\n" +
                "• Los enemigos serán más numerosos, letales y con habilidades reforzadas.\n" +
                "• Los jefes aumentarán su daño, resistencia y ferocidad.\n\n" +
                "⚠️ CONDICIONES DE RIESGO:\n" +
                "• Conservarás TODO tu oro acumulado.\n" +
                "• Sin embargo, perderás las recompensas compradas y mejoras temporales de run si todo el equipo muere en el bucle.\n\n" +
                "¿Aceptas el desafío extremo?";

            // Botón Aceptar / Continuar
            var btnAceptarObj = new GameObject("BtnAceptar");
            btnAceptarObj.transform.SetParent(cajaObj.transform, false);
            var rtBtnA = btnAceptarObj.AddComponent<RectTransform>();
            rtBtnA.anchorMin = new Vector2(0.5f, 0f);
            rtBtnA.anchorMax = new Vector2(0.5f, 0f);
            rtBtnA.anchoredPosition = new Vector2(120, 36);
            rtBtnA.sizeDelta = new Vector2(200, 42);

            var imgBtnA = btnAceptarObj.AddComponent<Image>();
            imgBtnA.color = new Color(0.18f, 0.65f, 0.24f);
            var btnA = btnAceptarObj.AddComponent<Button>();

            var txtBtnAObj = new GameObject("TextoBtnAceptar");
            txtBtnAObj.transform.SetParent(btnAceptarObj.transform, false);
            var rtTxtA = txtBtnAObj.AddComponent<RectTransform>();
            rtTxtA.anchorMin = Vector2.zero;
            rtTxtA.anchorMax = Vector2.one;
            rtTxtA.sizeDelta = Vector2.zero;
            var txtBtnA = txtBtnAObj.AddComponent<Text>();
            txtBtnA.font = font;
            txtBtnA.fontSize = 16;
            txtBtnA.alignment = TextAnchor.MiddleCenter;
            txtBtnA.color = Color.white;
            txtBtnA.text = "¡ACEPTAR DESAFÍO!";

            btnA.onClick.AddListener(() =>
            {
                Destroy(modalObj);
                onConfirmar?.Invoke();
            });

            // Botón Cancelar / Volver
            var btnCancelarObj = new GameObject("BtnCancelar");
            btnCancelarObj.transform.SetParent(cajaObj.transform, false);
            var rtBtnC = btnCancelarObj.AddComponent<RectTransform>();
            rtBtnC.anchorMin = new Vector2(0.5f, 0f);
            rtBtnC.anchorMax = new Vector2(0.5f, 0f);
            rtBtnC.anchoredPosition = new Vector2(-120, 36);
            rtBtnC.sizeDelta = new Vector2(200, 42);

            var imgBtnC = btnCancelarObj.AddComponent<Image>();
            imgBtnC.color = new Color(0.55f, 0.18f, 0.18f);
            var btnC = btnCancelarObj.AddComponent<Button>();

            var txtBtnCObj = new GameObject("TextoBtnCancelar");
            txtBtnCObj.transform.SetParent(btnCancelarObj.transform, false);
            var rtTxtC = txtBtnCObj.AddComponent<RectTransform>();
            rtTxtC.anchorMin = Vector2.zero;
            rtTxtC.anchorMax = Vector2.one;
            rtTxtC.sizeDelta = Vector2.zero;
            var txtBtnC = txtBtnCObj.AddComponent<Text>();
            txtBtnC.font = font;
            txtBtnC.fontSize = 16;
            txtBtnC.alignment = TextAnchor.MiddleCenter;
            txtBtnC.color = Color.white;
            txtBtnC.text = "VOLVER ATRÁS";

            btnC.onClick.AddListener(() =>
            {
                Destroy(modalObj);
            });

            _modalAdvertenciaBucle = modalObj;
        }

        public void MostrarAvisoPasaje(string mensaje)
        {
            if (textoAvisoPasaje != null)
            {
                textoAvisoPasaje.text = mensaje;
                textoAvisoPasaje.gameObject.SetActive(true);
                _tiempoAvisoPasaje = 3.5f;
            }
        }

        public void IniciarFadeOscurecer(float duracion = 0.8f, Action alTerminar = null)
        {
            EnTransicionCarga = true;
            if (minimapaHUD != null) minimapaHUD.gameObject.SetActive(false);
            if (_rutinaFade != null) StopCoroutine(_rutinaFade);
            _rutinaFade = StartCoroutine(RutinaFade(1f, duracion, alTerminar));
        }

        public void IniciarFadeAclarar(float duracion = 0.8f, string textoBanner = "", Action alTerminar = null)
        {
            if (_rutinaFade != null) StopCoroutine(_rutinaFade);
            if (textoBannerNivel != null && !string.IsNullOrEmpty(textoBanner))
            {
                textoBannerNivel.text = textoBanner;
                textoBannerNivel.gameObject.SetActive(true);
                textoBannerNivel.transform.SetAsLastSibling();
            }
            _rutinaFade = StartCoroutine(RutinaFade(0f, duracion, () =>
            {
                EnTransicionCarga = false;
                if (textoBannerNivel != null)
                {
                    StartCoroutine(RutinaOcultarBanner());
                }
                alTerminar?.Invoke();
            }));
        }

        private System.Collections.IEnumerator RutinaFade(float alfaObjetivo, float duracion, Action alTerminar)
        {
            if (panelFadeNegro == null)
            {
                alTerminar?.Invoke();
                yield break;
            }

            panelFadeNegro.gameObject.SetActive(true);
            panelFadeNegro.raycastTarget = alfaObjetivo > 0.01f;
            panelFadeNegro.transform.SetAsLastSibling();
            if (textoBannerNivel != null && textoBannerNivel.gameObject.activeSelf)
            {
                textoBannerNivel.transform.SetAsLastSibling();
            }

            Color c = panelFadeNegro.color;
            float alfaInicial = c.a;
            float t = 0f;

            while (t < duracion)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duracion);
                c.a = Mathf.SmoothStep(alfaInicial, alfaObjetivo, p);
                panelFadeNegro.color = c;
                yield return null;
            }

            c.a = alfaObjetivo;
            panelFadeNegro.color = c;
            if (alfaObjetivo <= 0.01f)
            {
                panelFadeNegro.gameObject.SetActive(false);
            }

            alTerminar?.Invoke();
        }

        private System.Collections.IEnumerator RutinaOcultarBanner()
        {
            yield return new WaitForSecondsRealtime(2.8f);
            if (textoBannerNivel != null)
            {
                textoBannerNivel.gameObject.SetActive(false);
            }
        }

        public void PrecargarMenuPausa()
        {
            if (panelPausa == null) return;
            try
            {
                panelPausa.transform.SetAsLastSibling();
                panelPausa.SetActive(true);
                if (panelFadeNegro != null && panelFadeNegro.gameObject.activeSelf)
                {
                    panelFadeNegro.transform.SetAsLastSibling();
                }

                var rtP = panelPausa.GetComponent<RectTransform>();
                if (rtP != null)
                {
                    rtP.anchorMin = Vector2.zero;
                    rtP.anchorMax = Vector2.one;
                    rtP.sizeDelta = Vector2.zero;
                    rtP.anchoredPosition = Vector2.zero;
                }

                ConfigurarLayoutMenuPausa();
                ActualizarVisibilidadBotonLobbyPausa();

                Canvas.ForceUpdateCanvases();
                if (rtP != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rtP);
                }

                ActualizarHabilidadesPausaUI();
                ActualizarMapaPausa();

                Canvas.ForceUpdateCanvases();
                panelPausa.SetActive(false);
                _menuPausaPrecargado = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HUD] Error al precargar menu de pausa: {ex}");
                if (panelPausa != null) panelPausa.SetActive(false);
            }
        }

        private System.Collections.IEnumerator RutinaCargaInicialMazmorra()
        {
            EnTransicionCarga = true;
            // Bloquear controles e iniciar pantalla completamente negra
            Controles.Bloqueado = true;
            if (minimapaHUD != null) minimapaHUD.gameObject.SetActive(false);
            if (panelFadeNegro != null)
            {
                panelFadeNegro.color = Color.black;
                panelFadeNegro.gameObject.SetActive(true);
                panelFadeNegro.raycastTarget = true;
                panelFadeNegro.transform.SetAsLastSibling();
            }

            // Esperar brevemente a que la escena se inicialice
            yield return new WaitForSecondsRealtime(0.2f);

            // Esperar activamente hasta que el generador de mazmorra tenga el grid listo y el jugador local esté conectado
            float tiempoMaximoEspera = 2.5f;
            float tiempoTranscurrido = 0f;
            while (tiempoTranscurrido < tiempoMaximoEspera)
            {
                bool mazmorraLista = GeneradorMazmorra.Instancia != null 
                    && GeneradorMazmorra.Instancia.gridActual != null
                    && GeneradorMazmorra.Instancia.salasGeneradas.Count > 0;
                bool jugadorListo = (_personajeLocal != null)
                    || Personaje.Todos.Exists(p => p != null && p.EsControlLocal);
                if (mazmorraLista && jugadorListo)
                {
                    break;
                }
                tiempoTranscurrido += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            try
            {
                if (_personajeLocal == null)
                {
                    for (int i = 0; i < Personaje.Todos.Count; i++)
                    {
                        var p = Personaje.Todos[i];
                        if (p != null && p.EsControlLocal)
                        {
                            ConectarPersonaje(p);
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HUD] Excepción al conectar personaje en carga: {ex.Message}");
            }

            // Dejar pasar dos frames para que todos los Start/Awake de salas y antorchas completen
            yield return null;
            yield return null;

            try
            {
                // Actualizar visión completa antes de que el jugador vea nada
                if (SistemaVision.Instancia != null)
                {
                    SistemaVision.Instancia.ForzarActualizacionExploracion();
                }
                if (MinimapaHUD.Instancia != null)
                {
                    MinimapaHUD.Instancia.RenderizarMinimapa();
                }

                // Precargar el menú de pausa y hornear su renderizado en GPU tras el fade negro
                if (panelPausa != null)
                {
                    panelPausa.transform.SetAsLastSibling();
                    panelPausa.SetActive(true);
                    if (panelFadeNegro != null)
                    {
                        panelFadeNegro.transform.SetAsLastSibling(); // Mantener pantalla negra por encima
                    }

                    var rtP = panelPausa.GetComponent<RectTransform>();
                    if (rtP != null)
                    {
                        rtP.anchorMin = Vector2.zero;
                        rtP.anchorMax = Vector2.one;
                        rtP.sizeDelta = Vector2.zero;
                        rtP.anchoredPosition = Vector2.zero;
                    }

                    ConfigurarLayoutMenuPausa();
                    ActualizarVisibilidadBotonLobbyPausa();

                    Canvas.ForceUpdateCanvases();
                    if (rtP != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rtP);
                    }

                    ActualizarHabilidadesPausaUI();
                    ActualizarMapaPausa();

                    Canvas.ForceUpdateCanvases();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HUD] Excepción al precargar pausa/mapa: {ex}");
            }

            // Dejar 2 frames reales mientras está oculto detrás de la pantalla negra para que Unity compile shaders y cargue mallas y texturas en la GPU
            yield return null;
            yield return null;

            if (panelPausa != null)
            {
                panelPausa.SetActive(false);
                _menuPausaPrecargado = true;
            }

            // Pausa breve y suave para asentar el escenario
            yield return new WaitForSecondsRealtime(0.4f);

            // Re-verificar visión y mapa justo antes del fade
            try
            {
                if (SistemaVision.Instancia != null)
                {
                    SistemaVision.Instancia.ForzarActualizacionExploracion();
                }
                if (MinimapaHUD.Instancia != null)
                {
                    MinimapaHUD.Instancia.RenderizarMinimapa();
                }
                ActualizarMapaPausa();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HUD] Advertencia al refrescar mapa: {ex.Message}");
            }

            int nivel = (GestorPartida.Instancia != null) ? GestorPartida.Instancia.NivelActual : 1;
            string bioma = (GeneradorMazmorra.Instancia != null) ? GeneradorMazmorra.Instancia.biomaActual.ToString().ToUpper() : "MAZMORRA";
            string textoBanner = "";
            if (GestorPartida.Instancia != null && GestorPartida.Instancia.EnNivelDecisionLoop)
            {
                textoBanner = "SANTUARIO DEL TRIUNFO\n(NIVEL 10.1)";
            }
            else
            {
                int loop = (GestorPartida.Instancia != null) ? GestorPartida.Instancia.LoopActual : 0;
                string extraLoop = loop > 0 ? $" [BUCLE +{loop}]" : "";
                textoBanner = $"NIVEL {nivel}{extraLoop}";
            }

            IniciarFadeAclarar(0.8f, textoBanner, () =>
            {
                Controles.Bloqueado = false;
                EnTransicionCarga = false;
            });
        }

        private void AlTerminarPartida(bool victoria)
        {
            Controles.Bloqueado = true;
            Time.timeScale = 1f;
            if (minimapaHUD != null) minimapaHUD.gameObject.SetActive(false);
            if (panelPausa) panelPausa.SetActive(false);
            if (panelFinPartida)
            {
                panelFinPartida.SetActive(true);
                if (textoResultado)
                {
                    textoResultado.text = victoria ? "¡VICTORIA!" : "TODO EL EQUIPO HA CAÍDO";
                }
            }
        }
    }
}
