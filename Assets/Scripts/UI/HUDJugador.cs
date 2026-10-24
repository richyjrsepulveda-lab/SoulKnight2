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
        public Button botonVideoPausa;
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
        private int _oroVisualLocal = -1;

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
                    barraMaestria.textoVida.text = "Maestría Nv. 1";
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
                textoAvisoPasaje.font = FuenteUtil.ObtenerFuentePixelify();
                textoAvisoPasaje.fontSize = 24;
                textoAvisoPasaje.fontStyle = FontStyle.Bold;
                textoAvisoPasaje.alignment = TextAnchor.MiddleCenter;
                textoAvisoPasaje.color = EstiloUIUX.ColDorado;
                textoAvisoPasaje.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoAvisoPasaje.verticalOverflow = VerticalWrapMode.Overflow;
                textoAvisoPasaje.raycastTarget = false;
                BordeTextoPixelArt.Aplicar(textoAvisoPasaje, 6, new Vector2(0f, -3f));
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
                textoBannerNivel.font = FuenteUtil.ObtenerFuentePixelify();
                textoBannerNivel.fontSize = 44;
                textoBannerNivel.fontStyle = FontStyle.Bold;
                textoBannerNivel.alignment = TextAnchor.MiddleCenter;
                textoBannerNivel.color = EstiloUIUX.ColCyan;
                textoBannerNivel.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoBannerNivel.verticalOverflow = VerticalWrapMode.Overflow;
                textoBannerNivel.raycastTarget = false;
                BordeTextoPixelArt.Aplicar(textoBannerNivel, 8, new Vector2(0f, -4f));
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

            // Nombre del Jefe arriba de la barra (Estilo Dark Souls con tipografía Pixelify Sans y borde pixel art)
            var nomObj = new GameObject("NombreJefeDS");
            nomObj.transform.SetParent(objJefe.transform, false);
            textoNombreJefeDS = nomObj.AddComponent<Text>();
            textoNombreJefeDS.font = FuenteUtil.ObtenerFuentePixelify();
            textoNombreJefeDS.fontSize = 24;
            textoNombreJefeDS.fontStyle = FontStyle.Bold;
            textoNombreJefeDS.color = EstiloUIUX.ColDorado;
            textoNombreJefeDS.alignment = TextAnchor.MiddleCenter;
            textoNombreJefeDS.text = "JEFE";
            textoNombreJefeDS.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoNombreJefeDS.verticalOverflow = VerticalWrapMode.Overflow;
            BordeTextoPixelArt.Aplicar(textoNombreJefeDS, 6, new Vector2(0f, -3f));
            var rtNom = nomObj.GetComponent<RectTransform>();
            rtNom.anchorMin = new Vector2(0f, 1f);
            rtNom.anchorMax = new Vector2(1f, 1f);
            rtNom.pivot = new Vector2(0.5f, 1f);
            rtNom.anchoredPosition = new Vector2(0f, 0f);
            rtNom.sizeDelta = new Vector2(0f, 32f);

            // Marco de la barra con caja escalonada pixel art
            var marcoBarra = new GameObject("MarcoBarraJefe");
            marcoBarra.transform.SetParent(objJefe.transform, false);
            var imgMarco = marcoBarra.AddComponent<Image>();
            imgMarco.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("MarcoJefeDS", EstiloUIUX.ColPanel, EstiloUIUX.ColDorado, EstiloUIUX.ColContornoUniversal, 4, 2, 2);
            imgMarco.type = Image.Type.Sliced;
            imgMarco.color = Color.white;
            var rtMarco = marcoBarra.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(0f, 0f);
            rtMarco.anchorMax = new Vector2(1f, 0f);
            rtMarco.pivot = new Vector2(0.5f, 0f);
            rtMarco.anchoredPosition = new Vector2(0f, 6f);
            rtMarco.sizeDelta = new Vector2(0f, 26f);

            // Fondo negro interior
            var fondoBarra = new GameObject("FondoBarraJefe");
            fondoBarra.transform.SetParent(marcoBarra.transform, false);
            var imgFondo = fondoBarra.AddComponent<Image>();
            imgFondo.color = EstiloUIUX.ColContornoUniversal;
            var rtFondo = fondoBarra.GetComponent<RectTransform>();
            rtFondo.anchorMin = Vector2.zero;
            rtFondo.anchorMax = Vector2.one;
            rtFondo.offsetMin = new Vector2(4f, 4f);
            rtFondo.offsetMax = new Vector2(-4f, -4f);

            // Relleno carmesí Dark Souls
            var rellenoObj = new GameObject("RellenoBarraJefe");
            rellenoObj.transform.SetParent(fondoBarra.transform, false);
            imagenRellenoJefeDS = rellenoObj.AddComponent<Image>();
            imagenRellenoJefeDS.sprite = spriteBlanco;
            imagenRellenoJefeDS.type = Image.Type.Filled;
            imagenRellenoJefeDS.fillMethod = Image.FillMethod.Horizontal;
            imagenRellenoJefeDS.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenRellenoJefeDS.color = new Color(0.78f, 0.12f, 0.15f, 1f);
            imagenRellenoJefeDS.fillAmount = 1f;
            var rtRelleno = rellenoObj.GetComponent<RectTransform>();
            rtRelleno.anchorMin = Vector2.zero;
            rtRelleno.anchorMax = Vector2.one;
            rtRelleno.sizeDelta = Vector2.zero;

            // Texto numérico de HP sobre la barra
            var txtHpObj = new GameObject("TextoHPJefe");
            txtHpObj.transform.SetParent(fondoBarra.transform, false);
            textoVidaJefeDS = txtHpObj.AddComponent<Text>();
            textoVidaJefeDS.font = FuenteUtil.ObtenerFuentePixelify();
            textoVidaJefeDS.fontSize = 14;
            textoVidaJefeDS.fontStyle = FontStyle.Bold;
            textoVidaJefeDS.color = EstiloUIUX.ColTextoCrema;
            textoVidaJefeDS.alignment = TextAnchor.MiddleCenter;
            textoVidaJefeDS.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoVidaJefeDS.verticalOverflow = VerticalWrapMode.Overflow;
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
            imgFondo.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgFondo.type = Image.Type.Sliced;
            imgFondo.color = Color.white;

            var rt = votObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 180);
            rt.sizeDelta = new Vector2(500, 180);

            EstiloUIUX.CrearRemaches(votObj.transform, 14f);

            // Título
            var objTit = new GameObject("TextoTitulo");
            objTit.transform.SetParent(votObj.transform, false);
            textoTituloVotacion = objTit.AddComponent<Text>();
            textoTituloVotacion.font = FuenteUtil.ObtenerFuentePixelify();
            textoTituloVotacion.fontSize = 20;
            textoTituloVotacion.fontStyle = FontStyle.Bold;
            textoTituloVotacion.color = EstiloUIUX.ColDorado;
            textoTituloVotacion.alignment = TextAnchor.MiddleCenter;
            textoTituloVotacion.text = "¿REGRESAR AL LOBBY?";
            textoTituloVotacion.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoTituloVotacion.verticalOverflow = VerticalWrapMode.Overflow;
            BordeTextoPixelArt.Aplicar(textoTituloVotacion, 6, new Vector2(0f, -3f));
            var rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchoredPosition = new Vector2(0, 56);
            rtTit.sizeDelta = new Vector2(460, 32);

            // Estado de votos (Verdes / Rojos)
            var objVotos = new GameObject("TextoEstadoVotos");
            objVotos.transform.SetParent(votObj.transform, false);
            textoEstadoVotos = objVotos.AddComponent<Text>();
            textoEstadoVotos.font = FuenteUtil.ObtenerFuentePixelify();
            textoEstadoVotos.fontSize = 16;
            textoEstadoVotos.fontStyle = FontStyle.Bold;
            textoEstadoVotos.color = EstiloUIUX.ColTextoCrema;
            textoEstadoVotos.alignment = TextAnchor.MiddleCenter;
            textoEstadoVotos.text = "Votos: [0 SÍ] vs [0 NO]";
            textoEstadoVotos.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoEstadoVotos.verticalOverflow = VerticalWrapMode.Overflow;
            var rtVotos = objVotos.GetComponent<RectTransform>();
            rtVotos.anchoredPosition = new Vector2(0, 20);
            rtVotos.sizeDelta = new Vector2(460, 28);

            // Temporizador
            var objTiempo = new GameObject("TextoTiempo");
            objTiempo.transform.SetParent(votObj.transform, false);
            textoTemporizadorVotacion = objTiempo.AddComponent<Text>();
            textoTemporizadorVotacion.font = FuenteUtil.ObtenerFuentePixelify();
            textoTemporizadorVotacion.fontSize = 13;
            textoTemporizadorVotacion.color = EstiloUIUX.ColTextoTenue;
            textoTemporizadorVotacion.alignment = TextAnchor.MiddleCenter;
            textoTemporizadorVotacion.text = "Tiempo restante: 15s";
            textoTemporizadorVotacion.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoTemporizadorVotacion.verticalOverflow = VerticalWrapMode.Overflow;
            var rtTiempo = objTiempo.GetComponent<RectTransform>();
            rtTiempo.anchoredPosition = new Vector2(0, -10);
            rtTiempo.sizeDelta = new Vector2(460, 24);

            // Botón SÍ [F1]
            botonVotarSi = EstiloUIUX.CrearBoton(votObj.transform, "BotonSi", "SÍ  [F1]", TipoBotonUI.Verde, new Vector2(180f, 44f), () => Votar(true), "palomita", new Vector2(20f, 16f));
            var rtSi = botonVotarSi.GetComponent<RectTransform>();
            rtSi.anchoredPosition = new Vector2(-110, -52);

            // Botón NO [F2]
            botonVotarNo = EstiloUIUX.CrearBoton(votObj.transform, "BotonNo", "NO  [F2]", TipoBotonUI.Rojo, new Vector2(180f, 44f), () => Votar(false), "candado", new Vector2(18f, 20f));
            var rtNo = botonVotarNo.GetComponent<RectTransform>();
            rtNo.anchoredPosition = new Vector2(110, -52);

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
                if (_modalMapaGrandeHUD != null && _modalMapaGrandeHUD.activeSelf)
                {
                    AlternarMapaGrande();
                }
                else if (_modalMaestriaHUD != null && _modalMaestriaHUD.activeSelf)
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

            // Atajo M: alternar mapa grande de la mazmorra
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (!TiendaAbierta && (panelPausa == null || !panelPausa.activeSelf))
                {
                    AlternarMapaGrande();
                }
            }

            if (_modalMapaGrandeHUD != null && _modalMapaGrandeHUD.activeSelf)
            {
                _timerRefrescoMapaGrande += Time.unscaledDeltaTime;
                if (_timerRefrescoMapaGrande >= 0.1f)
                {
                    _timerRefrescoMapaGrande = 0f;
                    ActualizarContenidoMapaGrande();
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
                    textoMiNombreHUD.font = FuenteUtil.ObtenerFuentePixelify();
                    textoMiNombreHUD.fontSize = 20; // Nombre del jugador visiblemente más grande
                    textoMiNombreHUD.fontStyle = FontStyle.Bold;
                    textoMiNombreHUD.alignment = TextAnchor.MiddleLeft;
                    textoMiNombreHUD.color = EstiloUIUX.ColTextoCrema;
                    textoMiNombreHUD.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textoMiNombreHUD.verticalOverflow = VerticalWrapMode.Overflow;
                    textoMiNombreHUD.raycastTarget = false;
                    BordeTextoPixelArt.Aplicar(textoMiNombreHUD, 6, new Vector2(0f, -3f));
                }
                else
                {
                    textoMiNombreHUD.font = FuenteUtil.ObtenerFuentePixelify();
                    textoMiNombreHUD.fontSize = 20;
                    textoMiNombreHUD.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textoMiNombreHUD.verticalOverflow = VerticalWrapMode.Overflow;
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
                        barraMaestria.textoVida.font = FuenteUtil.ObtenerFuentePixelify();
                        barraMaestria.textoVida.fontSize = 10;
                        barraMaestria.textoVida.fontStyle = FontStyle.Bold;
                        barraMaestria.textoVida.alignment = TextAnchor.MiddleCenter;
                        barraMaestria.textoVida.horizontalOverflow = HorizontalWrapMode.Overflow;
                        barraMaestria.textoVida.verticalOverflow = VerticalWrapMode.Overflow;
                        barraMaestria.textoVida.raycastTarget = false;
                    }

                    // Remover cualquier botón o interacción de la barra en la run
                    var btnBarra = barraMaestria.GetComponent<Button>();
                    if (btnBarra != null)
                    {
                        Destroy(btnBarra);
                    }

                    // Quitar el botón de información de maestría durante la run
                    var btnInfoObj = contArribaIzq.Find("BotonInfoMaestriaHUD");
                    if (btnInfoObj != null)
                    {
                        Destroy(btnInfoObj.gameObject);
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
                    "¡Derrota monstruos en la run para maximizar el nivel de tu personaje!";

                // Botón Cerrar
                var btnC = EstiloUIUX.CrearBoton(cardObj.transform, "BotonCerrar", "ENTENDIDO [ESC]", TipoBotonUI.Piedra, new Vector2(220f, 48f), () => _modalMaestriaHUD.SetActive(false), "palomita", new Vector2(22f, 16f));
                var rtBtn = btnC.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 18f);
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
            contAbajo.anchoredPosition = new Vector2(0f, 10f);
            contAbajo.sizeDelta = new Vector2(480f, 195f);

            // 1. Fila de Habilidades en la base (slots más grandes y claros)
            var filaHab = contAbajo.Find("FilaHabilidades") as RectTransform;
            if (filaHab != null)
            {
                filaHab.anchorMin = new Vector2(0.5f, 0f);
                filaHab.anchorMax = new Vector2(0.5f, 0f);
                filaHab.pivot = new Vector2(0.5f, 0f);
                filaHab.anchoredPosition = new Vector2(0f, 8f);
                filaHab.sizeDelta = new Vector2(460f, 102f);

                float[] posX = new float[] { -174f, -58f, 58f, 174f };
                for (int i = 0; i < filaHab.childCount && i < 4; i++)
                {
                    var slotChild = filaHab.GetChild(i) as RectTransform;
                    if (slotChild != null)
                    {
                        slotChild.anchoredPosition = new Vector2(posX[i], 0f);
                        slotChild.sizeDelta = new Vector2(98f, 98f);

                        var iconoChild = slotChild.Find("Icono") as RectTransform;
                        if (iconoChild != null)
                        {
                            iconoChild.sizeDelta = new Vector2(-8f, -8f);
                            var imgIco = iconoChild.GetComponent<Image>();
                            if (imgIco != null) imgIco.preserveAspect = true;
                        }

                        var sombraChild = slotChild.Find("SombraCooldown") as RectTransform;
                        if (sombraChild != null)
                        {
                            sombraChild.sizeDelta = new Vector2(-8f, -8f);
                        }

                        var cdChild = slotChild.Find("TextoCooldown") as RectTransform;
                        if (cdChild != null)
                        {
                            var txtCd = cdChild.GetComponent<Text>();
                            if (txtCd != null)
                            {
                                txtCd.fontSize = 22;
                                txtCd.horizontalOverflow = HorizontalWrapMode.Overflow;
                                txtCd.verticalOverflow = VerticalWrapMode.Overflow;
                            }
                        }

                        var badgeChild = slotChild.Find("BadgeTecla") as RectTransform;
                        if (badgeChild != null)
                        {
                            badgeChild.sizeDelta = new Vector2(50f, 22f);
                            var txtBadge = badgeChild.GetComponentInChildren<Text>();
                            if (txtBadge != null)
                            {
                                txtBadge.fontSize = 13;
                                txtBadge.horizontalOverflow = HorizontalWrapMode.Overflow;
                                txtBadge.verticalOverflow = VerticalWrapMode.Overflow;
                            }
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
                    rtXp.anchoredPosition = new Vector2(0f, 118f);
                    rtXp.sizeDelta = new Vector2(450f, 26f);
                }
                if (barraXPRun.textoVida != null)
                {
                    barraXPRun.textoVida.font = FuenteUtil.ObtenerFuentePixelify();
                    barraXPRun.textoVida.fontSize = 15;
                    barraXPRun.textoVida.horizontalOverflow = HorizontalWrapMode.Overflow;
                    barraXPRun.textoVida.verticalOverflow = VerticalWrapMode.Overflow;
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
                    rtBv.anchoredPosition = new Vector2(0f, 150f);
                    rtBv.sizeDelta = new Vector2(450f, 32f);
                }
                if (barraVida.textoVida != null)
                {
                    barraVida.textoVida.font = FuenteUtil.ObtenerFuentePixelify();
                    barraVida.textoVida.fontSize = 18;
                    barraVida.textoVida.horizontalOverflow = HorizontalWrapMode.Overflow;
                    barraVida.textoVida.verticalOverflow = VerticalWrapMode.Overflow;
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
                // Minimapa está en X = -16, Y = -54, Alto = 260 (borde inferior en Y = -314)
                // Colocamos el oro inmediatamente debajo del minimapa con margen de 10px
                rtCont.anchoredPosition = new Vector2(-16f, -324f);
                rtCont.sizeDelta = new Vector2(160f, 44f);

                var imgFondo = contOroObj.AddComponent<Image>();
                imgFondo.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                imgFondo.type = Image.Type.Sliced;
                imgFondo.color = Color.white;

                // GameObject hijo dedicado al texto (evita conflicto Graphic de Unity UI)
                var txtObj = new GameObject("TextoOro");
                txtObj.transform.SetParent(contOroObj.transform, false);
                var rtTxt = txtObj.AddComponent<RectTransform>();
                rtTxt.anchorMin = Vector2.zero;
                rtTxt.anchorMax = Vector2.one;
                rtTxt.sizeDelta = Vector2.zero;

                textoOroRun = txtObj.AddComponent<Text>();
                textoOroRun.font = FuenteUtil.ObtenerFuentePixelify();
                textoOroRun.fontSize = 20;
                textoOroRun.fontStyle = FontStyle.Bold;
                textoOroRun.alignment = TextAnchor.MiddleCenter;
                textoOroRun.color = EstiloUIUX.ColDorado;
                textoOroRun.text = "🪙 0";
                textoOroRun.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoOroRun.verticalOverflow = VerticalWrapMode.Overflow;
                textoOroRun.raycastTarget = false;
            }
            else
            {
                // Asegurar posición correcta si ya existía
                var rt = textoOroRun.transform.parent as RectTransform;
                if (rt != null && rt.name == "ContenedorOroRunHUD")
                {
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-16f, -324f);
                    rt.sizeDelta = new Vector2(160f, 44f);
                }
                textoOroRun.fontSize = 20;
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
                txtTit.font = FuenteUtil.ObtenerFuentePixelify();
                txtTit.fontSize = 44;
                txtTit.fontStyle = FontStyle.Bold;
                txtTit.alignment = TextAnchor.MiddleCenter;
                txtTit.color = EstiloUIUX.ColDorado;
                txtTit.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtTit.verticalOverflow = VerticalWrapMode.Overflow;
                BordeTextoPixelArt.Aplicar(txtTit, 8, new Vector2(0f, -4f));
            }

            // 2. Columna izquierda: 4 botones verticales limpios
            if (botonVideoPausa != null) botonVideoPausa.gameObject.SetActive(false);
            var bVidExist = panelPausa.transform.Find("BotonVideoPausa");
            if (bVidExist != null) bVidExist.gameObject.SetActive(false);

            AsegurarBotonControlesPausa();
            AjustarBotonPausaIndividual(botonReanudar, "REANUDAR", -235f, 75f, TipoBotonUI.Verde, "play", new Vector2(22f, 22f));
            AjustarBotonPausaIndividual(botonControlesPausa, "OPCIONES", -235f, 15f, TipoBotonUI.Piedra, "controles", new Vector2(24f, 20f));
            AjustarBotonPausaIndividual(botonVolverLobbyPausa, "VOLVER AL LOBBY", -235f, -45f, TipoBotonUI.Piedra, "puerta", new Vector2(22f, 22f));
            AjustarBotonPausaIndividual(botonSalirPartida, "SALIR AL MENÚ", -235f, -105f, TipoBotonUI.Rojo, "puerta", new Vector2(22f, 22f));

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
                    imgMarcoBox.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                    imgMarcoBox.type = Image.Type.Sliced;
                    imgMarcoBox.color = Color.white;

                    var fondoInt = new GameObject("Fondo", typeof(RectTransform));
                    fondoInt.transform.SetParent(obj.transform, false);
                    var rtFi = fondoInt.GetComponent<RectTransform>();
                    rtFi.anchorMin = Vector2.zero;
                    rtFi.anchorMax = Vector2.one;
                    rtFi.offsetMin = new Vector2(3f, 3f);
                    rtFi.offsetMax = new Vector2(-3f, -3f);
                    var imgFi = fondoInt.AddComponent<Image>();
                    imgFi.color = EstiloUIUX.ColTarjeta;

                    var txtObj = new GameObject("Texto", typeof(RectTransform));
                    txtObj.transform.SetParent(fondoInt.transform, false);
                    var rtTxt = txtObj.GetComponent<RectTransform>();
                    rtTxt.anchorMin = Vector2.zero;
                    rtTxt.anchorMax = Vector2.one;
                    rtTxt.offsetMin = new Vector2(14f, 8f);
                    rtTxt.offsetMax = new Vector2(-14f, -8f);

                    _textoDescHabilidadPausa = txtObj.AddComponent<Text>();
                    _textoDescHabilidadPausa.font = FuenteUtil.ObtenerFuentePixelify();
                    _textoDescHabilidadPausa.fontSize = 19;
                    _textoDescHabilidadPausa.lineSpacing = 1.35f;
                    _textoDescHabilidadPausa.alignment = TextAnchor.MiddleLeft;
                    _textoDescHabilidadPausa.color = EstiloUIUX.ColTextoCrema;
                    _textoDescHabilidadPausa.horizontalOverflow = HorizontalWrapMode.Wrap;
                    _textoDescHabilidadPausa.verticalOverflow = VerticalWrapMode.Overflow;
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
                    rtDesc.anchoredPosition = new Vector2(posXDerecha, 35f);
                    rtDesc.sizeDelta = new Vector2(430f, 160f);
                }

                // --- 3C. Marco del Mapa (abajo de la descripción) ---
                Transform marcoMapa = panelPausa.transform.Find("MarcoMapaPausa");
                if (marcoMapa == null)
                {
                    var obj = new GameObject("MarcoMapaPausa", typeof(RectTransform));
                    obj.transform.SetParent(panelPausa.transform, false);
                    marcoMapa = obj.transform;

                    var imgMarco = obj.AddComponent<Image>();
                    imgMarco.sprite = EstiloUIUX.ObtenerSpritePanel();
                    imgMarco.type = Image.Type.Sliced;
                    imgMarco.color = Color.white;

                    var fondoInt = new GameObject("Fondo", typeof(RectTransform));
                    fondoInt.transform.SetParent(obj.transform, false);
                    var rtFi = fondoInt.GetComponent<RectTransform>();
                    rtFi.anchorMin = Vector2.zero;
                    rtFi.anchorMax = Vector2.one;
                    rtFi.offsetMin = new Vector2(6f, 6f);
                    rtFi.offsetMax = new Vector2(-6f, -6f);
                    var imgFi = fondoInt.AddComponent<Image>();
                    imgFi.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                    imgFi.type = Image.Type.Sliced;
                    imgFi.color = Color.white;

                    var rawObj = new GameObject("Lienzo", typeof(RectTransform));
                    rawObj.transform.SetParent(fondoInt.transform, false);
                    var rtRaw = rawObj.GetComponent<RectTransform>();
                    rtRaw.anchorMin = Vector2.zero;
                    rtRaw.anchorMax = Vector2.one;
                    rtRaw.offsetMin = new Vector2(4f, 4f);
                    rtRaw.offsetMax = new Vector2(-4f, -24f);
                    _rawMapaPausa = rawObj.AddComponent<RawImage>();

                    var titObj = new GameObject("Titulo", typeof(RectTransform));
                    titObj.transform.SetParent(fondoInt.transform, false);
                    var rtTit = titObj.GetComponent<RectTransform>();
                    rtTit.anchorMin = new Vector2(0f, 1f);
                    rtTit.anchorMax = new Vector2(1f, 1f);
                    rtTit.pivot = new Vector2(0.5f, 1f);
                    rtTit.anchoredPosition = new Vector2(0f, -3f);
                    rtTit.sizeDelta = new Vector2(0f, 20f);
                    var txtTit = titObj.AddComponent<Text>();
                    txtTit.font = FuenteUtil.ObtenerFuentePixelify();
                    txtTit.fontSize = 13;
                    txtTit.fontStyle = FontStyle.Bold;
                    txtTit.alignment = TextAnchor.MiddleCenter;
                    txtTit.color = EstiloUIUX.ColDorado;
                    txtTit.text = "✦ MAPA DE LA MAZMORRA ✦";
                    txtTit.horizontalOverflow = HorizontalWrapMode.Overflow;
                    txtTit.verticalOverflow = VerticalWrapMode.Overflow;
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
                    rtMm.anchoredPosition = new Vector2(posXDerecha, -125f);
                    rtMm.sizeDelta = new Vector2(370f, 130f);
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

            // Tipografía Pixelify Sans nítida con overflow protegido y tamaño ampliado
            _textoDescHabilidadPausa.font = FuenteUtil.ObtenerFuentePixelify();
            _textoDescHabilidadPausa.fontSize = 19;
            _textoDescHabilidadPausa.lineSpacing = 1.35f;
            _textoDescHabilidadPausa.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textoDescHabilidadPausa.verticalOverflow = VerticalWrapMode.Overflow;

            var tipo = ObtenerTipoPersonajeLocal();
            var habs = CatalogoHabilidades.ObtenerHabilidadesPersonaje(tipo);

            string[] teclas = new string[] { "CLICK IZQ", "E", "Q", "R" };
            int idx = Mathf.Clamp(_habilidadSeleccionadaPausa, 0, 3);

            if (idx < habs.Count)
            {
                var h = habs[idx];
                string desc = !string.IsNullOrEmpty(h.descripcionTecnica) ? h.descripcionTecnica : h.descripcion;

                string textoStatsRun = "";
                if (_personajeLocal != null)
                {
                    float multDano = _personajeLocal.MultiplicadorDanoTotal;
                    float redCd = (1f - _personajeLocal.FactorReduccionCooldown) * 100f;
                    float incArea = (_personajeLocal.MultiplicadorAreaHabilidades - 1f) * 100f;
                    float probCrit = _personajeLocal.ProbabilidadCritico * 100f;

                    textoStatsRun = $"\n\n<color=#4ade80><b>ESTADÍSTICAS MEJORADAS DE LA RUN:</b></color>" +
                                   $"\n• Daño Total: <color=#ffd700>{multDano:F2}x</color>" +
                                   (redCd > 0.5f ? $"\n• Reducción de Enfriamiento: <color=#38bdf8>-{redCd:F0}%</color>" : "") +
                                   (incArea > 0.5f ? $"\n• Área / Rango de Habilidad: <color=#a7f3d0>+{incArea:F0}%</color>" : "") +
                                   (probCrit > 0.5f ? $"\n• Probabilidad Crítica: <color=#f87171>{probCrit:F0}% (1.8x)</color>" : "") +
                                   $"\n• Vida Máxima: <color=#fb7185>{_personajeLocal.VidaMaxSincronizada.Value} HP</color>";
                }

                _textoDescHabilidadPausa.text = $"<b><size=22><color=#5ce1e6>{h.nombre.ToUpper()}</color></size></b>  <color=#ffd700>({teclas[idx]})</color>\n{desc}{textoStatsRun}";
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
                botonControlesPausa = EstiloUIUX.CrearBoton(panelPausa.transform, "BotonControlesPausa", "OPCIONES", TipoBotonUI.Piedra, new Vector2(240f, 50f), null, "controles", new Vector2(24f, 20f));
            }

            botonControlesPausa.onClick.RemoveAllListeners();
            botonControlesPausa.onClick.AddListener(() =>
            {
                PanelOpcionesControles.ObtenerOCrear(transform).Abrir();
            });
        }

        private void AsegurarBotonVideoPausa()
        {
            if (panelPausa == null) return;

            if (botonVideoPausa == null)
            {
                var exist = panelPausa.transform.Find("BotonVideoPausa");
                if (exist != null)
                {
                    botonVideoPausa = exist.GetComponent<Button>();
                }
            }

            if (botonVideoPausa == null)
            {
                botonVideoPausa = EstiloUIUX.CrearBoton(panelPausa.transform, "BotonVideoPausa", "VIDEO", TipoBotonUI.Piedra, new Vector2(240f, 50f), null, "monitor", new Vector2(24f, 20f));
            }

            botonVideoPausa.onClick.RemoveAllListeners();
            botonVideoPausa.onClick.AddListener(() =>
            {
                PanelOpcionesControles.ObtenerOCrear(transform).Abrir(PestanaOpciones.Video);
            });
        }

        private void AjustarBotonPausaIndividual(Button btn, string textoDefecto, float posX, float posY, TipoBotonUI tipo, string icono = null, Vector2? iconoSize = null)
        {
            if (btn == null) return;
            var rt = btn.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(posX, posY);
                rt.sizeDelta = new Vector2(230f, 48f);
            }

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null && !string.IsNullOrEmpty(textoDefecto))
            {
                txt.text = textoDefecto;
            }

            EstiloUIUX.AplicarEstiloBoton(btn, tipo, 23, icono, iconoSize);
        }

        // =========================================================================
        // MAPA GRANDE DE LA MAZMORRA (TECLA M)
        // =========================================================================

        private GameObject _modalMapaGrandeHUD;
        private RawImage _rawMapaGrande;
        private Text _txtTituloMapaGrande;
        private Text _txtSubtituloMapaGrande;
        private float _timerRefrescoMapaGrande = 0f;

        public bool MapaGrandeAbierto => _modalMapaGrandeHUD != null && _modalMapaGrandeHUD.activeSelf;

        private void CrearModalMapaGrandeSiNoExiste()
        {
            if (_modalMapaGrandeHUD != null) return;

            var modalObj = new GameObject("PanelMapaGrandeModal");
            modalObj.transform.SetParent(transform, false);
            modalObj.transform.SetAsLastSibling();

            var rtFull = modalObj.AddComponent<RectTransform>();
            rtFull.anchorMin = Vector2.zero;
            rtFull.anchorMax = Vector2.one;
            rtFull.sizeDelta = Vector2.zero;

            // Fondo translúcido oscuro
            var imgFondo = modalObj.AddComponent<Image>();
            imgFondo.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            // Ventana modal centrada
            var ventanaObj = new GameObject("VentanaMapaGrande");
            ventanaObj.transform.SetParent(modalObj.transform, false);
            var rtVentana = ventanaObj.AddComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.anchoredPosition = Vector2.zero;
            rtVentana.sizeDelta = new Vector2(920f, 660f);

            var imgVentana = ventanaObj.AddComponent<Image>();
            imgVentana.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgVentana.type = Image.Type.Sliced;
            imgVentana.color = Color.white;

            EstiloUIUX.CrearRemaches(ventanaObj.transform, 18f);
            EstiloUIUX.CrearCabeceraModal(ventanaObj.transform, "MAPA DE LA MAZMORRA", 76f, "huella");

            // Subtítulo
            var subObj = new GameObject("SubtituloMapaGrande");
            subObj.transform.SetParent(ventanaObj.transform, false);
            _txtSubtituloMapaGrande = subObj.AddComponent<Text>();
            _txtSubtituloMapaGrande.font = FuenteUtil.ObtenerFuentePixelify();
            _txtSubtituloMapaGrande.fontSize = 14;
            _txtSubtituloMapaGrande.alignment = TextAnchor.MiddleCenter;
            _txtSubtituloMapaGrande.color = EstiloUIUX.ColTextoTenue;
            _txtSubtituloMapaGrande.text = "Pulsa [M] o [ESC] para cerrar";
            _txtSubtituloMapaGrande.horizontalOverflow = HorizontalWrapMode.Overflow;
            _txtSubtituloMapaGrande.verticalOverflow = VerticalWrapMode.Overflow;

            var rtSub = subObj.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -84f);
            rtSub.sizeDelta = new Vector2(800f, 22f);

            // Marco del mapa grande (RawImage)
            var marcoMapa = new GameObject("MarcoMapa");
            marcoMapa.transform.SetParent(ventanaObj.transform, false);
            var rtMarco = marcoMapa.AddComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(0.5f, 0.5f);
            rtMarco.anchorMax = new Vector2(0.5f, 0.5f);
            rtMarco.pivot = new Vector2(0.5f, 0.5f);
            rtMarco.anchoredPosition = new Vector2(0f, -25f);
            rtMarco.sizeDelta = new Vector2(840f, 480f);

            var imgMarco = marcoMapa.AddComponent<Image>();
            imgMarco.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
            imgMarco.type = Image.Type.Sliced;
            imgMarco.color = Color.white;

            // RawImage mapa
            var rawObj = new GameObject("RawMapaGrande");
            rawObj.transform.SetParent(marcoMapa.transform, false);
            var rtRaw = rawObj.AddComponent<RectTransform>();
            rtRaw.anchorMin = Vector2.zero;
            rtRaw.anchorMax = Vector2.one;
            rtRaw.offsetMin = new Vector2(6f, 6f);
            rtRaw.offsetMax = new Vector2(-6f, -6f);
            rtRaw.anchoredPosition = Vector2.zero;

            _rawMapaGrande = rawObj.AddComponent<RawImage>();
            _rawMapaGrande.raycastTarget = false;

            // Botón Cerrar [X] en la esquina superior derecha
            var btnC = EstiloUIUX.CrearBoton(ventanaObj.transform, "BotonCerrarMapa", "X", TipoBotonUI.Rojo, new Vector2(40f, 40f), () => AlternarMapaGrande());
            var rtBtnCerrar = btnC.GetComponent<RectTransform>();
            rtBtnCerrar.anchorMin = new Vector2(1f, 1f);
            rtBtnCerrar.anchorMax = new Vector2(1f, 1f);
            rtBtnCerrar.pivot = new Vector2(1f, 1f);
            rtBtnCerrar.anchoredPosition = new Vector2(-16f, -16f);

            _modalMapaGrandeHUD = modalObj;
            _modalMapaGrandeHUD.SetActive(false);
        }

        public void AlternarMapaGrande()
        {
            if (_modalMapaGrandeHUD == null)
            {
                CrearModalMapaGrandeSiNoExiste();
            }

            bool nuevoEstado = !_modalMapaGrandeHUD.activeSelf;
            _modalMapaGrandeHUD.SetActive(nuevoEstado);

            if (nuevoEstado)
            {
                _timerRefrescoMapaGrande = 0f;
                ActualizarContenidoMapaGrande();
            }
        }

        public void ActualizarContenidoMapaGrande()
        {
            if (_modalMapaGrandeHUD == null || !_modalMapaGrandeHUD.activeSelf) return;

            int nivel = (GestorPartida.Instancia != null) ? GestorPartida.Instancia.NivelActual : 1;
            string bioma = (GeneradorMazmorra.Instancia != null) ? GeneradorMazmorra.Instancia.biomaActual.ToString().ToUpper() : "MAZMORRA";
            if (_txtTituloMapaGrande != null)
            {
                _txtTituloMapaGrande.text = $"🗺️ MAPA - NIVEL {nivel} ({bioma})";
            }

            if (SistemaVision.Instancia != null)
            {
                SistemaVision.Instancia.ForzarActualizacionExploracion();
            }

            if (MinimapaHUD.Instancia != null)
            {
                MinimapaHUD.Instancia.RenderizarMapaCompleto();
                if (_rawMapaGrande != null)
                {
                    _rawMapaGrande.texture = MinimapaHUD.Instancia.TextureMapaCompleto;
                    _rawMapaGrande.color = Color.white;
                    _rawMapaGrande.enabled = true;
                }
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

            // Ventana modal centrada (ampliada y con excelente aire/interlineado)
            var cardObj = new GameObject("VentanaTienda");
            cardObj.transform.SetParent(tiendaObj.transform, false);
            var rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.pivot = new Vector2(0.5f, 0.5f);
            rtCard.anchoredPosition = Vector2.zero;
            rtCard.sizeDelta = new Vector2(960f, 620f);

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgCard.type = Image.Type.Sliced;
            imgCard.color = Color.white;

            EstiloUIUX.CrearRemaches(cardObj.transform, 18f);
            EstiloUIUX.CrearCabeceraModal(cardObj.transform, "TIENDA MÍSTICA DEL GATO MERCADER", 76f, "huella");

            // Subtítulo
            var subObj = new GameObject("SubtituloTienda");
            subObj.transform.SetParent(cardObj.transform, false);
            var txtSub = subObj.AddComponent<Text>();
            txtSub.font = FuenteUtil.ObtenerFuentePixelify();
            txtSub.fontSize = 14;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = EstiloUIUX.ColTextoTenue;
            txtSub.text = "Mejoras de características para tu gato en esta run. Hasta 5 niveles por rasgo.";
            txtSub.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtSub.verticalOverflow = VerticalWrapMode.Overflow;
            var rtSub = subObj.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -80f);
            rtSub.sizeDelta = new Vector2(880f, 24f);

            // Contador de Oro
            var oroObj = new GameObject("TextoOroTienda");
            oroObj.transform.SetParent(cardObj.transform, false);
            _textoOroTienda = oroObj.AddComponent<Text>();
            _textoOroTienda.font = FuenteUtil.ObtenerFuentePixelify();
            _textoOroTienda.fontSize = 18;
            _textoOroTienda.fontStyle = FontStyle.Bold;
            _textoOroTienda.alignment = TextAnchor.MiddleCenter;
            _textoOroTienda.color = EstiloUIUX.ColDorado;
            _textoOroTienda.text = "🪙 Oro Disponible: 0";
            _textoOroTienda.horizontalOverflow = HorizontalWrapMode.Overflow;
            _textoOroTienda.verticalOverflow = VerticalWrapMode.Overflow;

            var rtOro = oroObj.GetComponent<RectTransform>();
            rtOro.anchorMin = new Vector2(0.5f, 1f);
            rtOro.anchorMax = new Vector2(0.5f, 1f);
            rtOro.pivot = new Vector2(0.5f, 1f);
            rtOro.anchoredPosition = new Vector2(0f, -106f);
            rtOro.sizeDelta = new Vector2(500f, 26f);

            // Contenedor de 6 Artículos (2 filas de 3 columnas con amplias dimensiones)
            float[] posX = new float[] { -305f, 0f, 305f, -305f, 0f, 305f };
            float[] posY = new float[] { 45f, 45f, 45f, -145f, -145f, -145f };
            _botonesCompraTienda.Clear();
            _textosBotonTienda.Clear();

            for (int i = 0; i < 6; i++)
            {
                int itemIndex = i;
                var itemCardObj = new GameObject($"CardArticulo_{i}");
                itemCardObj.transform.SetParent(cardObj.transform, false);
                var rtIc = itemCardObj.AddComponent<RectTransform>();
                rtIc.anchorMin = new Vector2(0.5f, 0.5f);
                rtIc.anchorMax = new Vector2(0.5f, 0.5f);
                rtIc.pivot = new Vector2(0.5f, 0.5f);
                rtIc.anchoredPosition = new Vector2(posX[i], posY[i]);
                rtIc.sizeDelta = new Vector2(290f, 175f);

                var imgIc = itemCardObj.AddComponent<Image>();
                imgIc.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
                imgIc.type = Image.Type.Sliced;
                imgIc.color = Color.white;

                // Nombre
                var nomObj = new GameObject("Nombre");
                nomObj.transform.SetParent(itemCardObj.transform, false);
                var rtNom = nomObj.AddComponent<RectTransform>();
                rtNom.anchorMin = new Vector2(0.5f, 1f);
                rtNom.anchorMax = new Vector2(0.5f, 1f);
                rtNom.pivot = new Vector2(0.5f, 1f);
                rtNom.anchoredPosition = new Vector2(0f, -10f);
                rtNom.sizeDelta = new Vector2(276f, 24f);
                var txtNom = nomObj.AddComponent<Text>();
                txtNom.font = FuenteUtil.ObtenerFuentePixelify();
                txtNom.fontSize = 15;
                txtNom.fontStyle = FontStyle.Bold;
                txtNom.alignment = TextAnchor.MiddleCenter;
                txtNom.color = EstiloUIUX.ColTextoCrema;
                txtNom.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtNom.verticalOverflow = VerticalWrapMode.Overflow;

                // Badge de Clase Compatible
                var badgeObj = new GameObject("ClaseBadge");
                badgeObj.transform.SetParent(itemCardObj.transform, false);
                var rtBadge = badgeObj.AddComponent<RectTransform>();
                rtBadge.anchorMin = new Vector2(0.5f, 1f);
                rtBadge.anchorMax = new Vector2(0.5f, 1f);
                rtBadge.pivot = new Vector2(0.5f, 1f);
                rtBadge.anchoredPosition = new Vector2(0f, -34f);
                rtBadge.sizeDelta = new Vector2(276f, 20f);
                var txtBadge = badgeObj.AddComponent<Text>();
                txtBadge.font = FuenteUtil.ObtenerFuentePixelify();
                txtBadge.fontSize = 12;
                txtBadge.fontStyle = FontStyle.Bold;
                txtBadge.alignment = TextAnchor.MiddleCenter;
                txtBadge.color = EstiloUIUX.ColCyan;
                txtBadge.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtBadge.verticalOverflow = VerticalWrapMode.Overflow;

                // Descripción con interlineado generoso
                var descObj = new GameObject("Desc");
                descObj.transform.SetParent(itemCardObj.transform, false);
                var rtDesc = descObj.AddComponent<RectTransform>();
                rtDesc.anchorMin = new Vector2(0.5f, 1f);
                rtDesc.anchorMax = new Vector2(0.5f, 1f);
                rtDesc.pivot = new Vector2(0.5f, 1f);
                rtDesc.anchoredPosition = new Vector2(0f, -56f);
                rtDesc.sizeDelta = new Vector2(276f, 52f);
                var txtDesc = descObj.AddComponent<Text>();
                txtDesc.font = FuenteUtil.ObtenerFuentePixelify();
                txtDesc.fontSize = 12;
                txtDesc.lineSpacing = 1.35f;
                txtDesc.alignment = TextAnchor.MiddleCenter;
                txtDesc.color = EstiloUIUX.ColTextoTenue;
                txtDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                txtDesc.verticalOverflow = VerticalWrapMode.Overflow;

                // Botón Comprar
                var btnBuy = EstiloUIUX.CrearBoton(itemCardObj.transform, "BotonComprar", "COMPRAR", TipoBotonUI.Verde, new Vector2(270f, 38f), () => ComprarArticuloTiendaIndex(itemIndex));
                var rtBtn = btnBuy.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0f, 8f);

                var txtBtn = btnBuy.GetComponentInChildren<Text>();
                _botonesCompraTienda.Add(btnBuy);
                _textosBotonTienda.Add(txtBtn);
            }

            // Botón Salir de la Tienda
            var btnSalir = EstiloUIUX.CrearBoton(cardObj.transform, "BotonSalirTienda", "SALIR DE LA TIENDA (ESC)", TipoBotonUI.Rojo, new Vector2(360f, 46f), CerrarTienda, "puerta", new Vector2(24f, 24f));
            var rtSalir = btnSalir.GetComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(0.5f, 0f);
            rtSalir.anchorMax = new Vector2(0.5f, 0f);
            rtSalir.pivot = new Vector2(0.5f, 0f);
            rtSalir.anchoredPosition = new Vector2(0f, 14f);

            panelTienda = tiendaObj;
            panelTienda.SetActive(false);
        }

        public void AbrirTienda(PuestitoTienda puestito)
        {
            if (puestito == null) return;
            if (_modalMapaGrandeHUD != null) _modalMapaGrandeHUD.SetActive(false);
            if (panelPausa != null && panelPausa.activeSelf) return;
            if (panelFinPartida != null && panelFinPartida.activeSelf) return;

            CrearPanelTiendaSiNoExiste();
            _puestitoActivo = puestito;
            _oroVisualLocal = (_personajeLocal != null) ? _personajeLocal.OroRun.Value : 0;
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

            if (_personajeLocal != null && (_oroVisualLocal < 0 || _personajeLocal.OroRun.Value < _oroVisualLocal))
            {
                _oroVisualLocal = _personajeLocal.OroRun.Value;
            }
            int oroLocal = (_oroVisualLocal >= 0) ? _oroVisualLocal : ((_personajeLocal != null) ? _personajeLocal.OroRun.Value : 0);
            if (_textoOroTienda != null)
            {
                _textoOroTienda.text = $"💰 Oro Disponible en la Run: {oroLocal}";
            }

            var cardObj = panelTienda.transform.Find("VentanaTienda");
            if (cardObj == null) return;

            for (int i = 0; i < 6 && i < _botonesCompraTienda.Count; i++)
            {
                var card = cardObj.Find($"CardArticulo_{i}");
                var btn = _botonesCompraTienda[i];
                var txtBtn = _textosBotonTienda[i];
                var imgBtn = btn != null ? btn.GetComponent<Image>() : null;

                if (i >= _puestitoActivo.itemsOfrecidos.Count)
                {
                    if (card != null) card.gameObject.SetActive(false);
                    continue;
                }

                if (card != null) card.gameObject.SetActive(true);

                TipoMejoraTienda tipo = _puestitoActivo.itemsOfrecidos[i];
                int nvActual = (_personajeLocal != null) ? _personajeLocal.ObtenerNivelMejora(tipo) : 0;
                CatalogoMejorasTienda.ObtenerInfoMejora(tipo, nvActual, out string nombre, out string desc, out TipoPersonaje claseReq);
                int costo = CatalogoMejorasTienda.ObtenerCostoNivel(tipo, nvActual);

                bool esCompatible = (claseReq == TipoPersonaje.Ninguno) || (_personajeLocal != null && _personajeLocal.TipoPj == claseReq);

                if (card != null)
                {
                    var txtNom = card.Find("Nombre")?.GetComponent<Text>();
                    if (txtNom != null) txtNom.text = nombre;

                    var txtBadge = card.Find("ClaseBadge")?.GetComponent<Text>();
                    if (txtBadge != null)
                    {
                        if (claseReq == TipoPersonaje.Ninguno)
                        {
                            txtBadge.text = "[Universal]";
                            txtBadge.color = new Color(0.4f, 0.9f, 1f);
                        }
                        else if (claseReq == TipoPersonaje.Fuego)
                        {
                            txtBadge.text = "[Gato de Fuego]";
                            txtBadge.color = new Color(1f, 0.55f, 0.15f);
                        }
                        else if (claseReq == TipoPersonaje.Hielo)
                        {
                            txtBadge.text = "[Gato de Hielo]";
                            txtBadge.color = new Color(0.45f, 0.85f, 1f);
                        }
                        else if (claseReq == TipoPersonaje.Electrico)
                        {
                            txtBadge.text = "[Gato Eléctrico]";
                            txtBadge.color = new Color(1f, 0.95f, 0.25f);
                        }
                    }

                    var txtDesc = card.Find("Desc")?.GetComponent<Text>();
                    if (txtDesc != null)
                    {
                        txtDesc.text = desc;
                    }
                }

                if (btn != null && txtBtn != null)
                {
                    if (!esCompatible)
                    {
                        btn.interactable = false;
                        txtBtn.text = $"❌ Incompatible ({claseReq})";
                        txtBtn.color = new Color(0.85f, 0.45f, 0.45f);
                        if (imgBtn != null) imgBtn.color = new Color(0.25f, 0.14f, 0.14f, 1f);
                    }
                    else if (tipo != TipoMejoraTienda.PescadoAsado && nvActual >= 5)
                    {
                        btn.interactable = false;
                        txtBtn.text = "✔ NIVEL MÁXIMO (5/5)";
                        txtBtn.color = new Color(0.7f, 0.7f, 0.7f);
                        if (imgBtn != null) imgBtn.color = new Color(0.18f, 0.20f, 0.24f, 1f);
                    }
                    else if (tipo == TipoMejoraTienda.VidaMaxima && _personajeLocal != null && _personajeLocal.vidaMax >= 300)
                    {
                        btn.interactable = false;
                        txtBtn.text = "✔ MÁXIMO (300 HP)";
                        txtBtn.color = new Color(0.7f, 0.7f, 0.7f);
                        if (imgBtn != null) imgBtn.color = new Color(0.18f, 0.20f, 0.24f, 1f);
                    }
                    else if (oroLocal < costo)
                    {
                        btn.interactable = false;
                        txtBtn.text = $"{costo} 💰 (Falta oro)";
                        txtBtn.color = new Color(1f, 0.6f, 0.6f);
                        if (imgBtn != null) imgBtn.color = new Color(0.35f, 0.18f, 0.18f, 1f);
                    }
                    else
                    {
                        btn.interactable = true;
                        txtBtn.text = (tipo == TipoMejoraTienda.PescadoAsado)
                            ? $"COMPRAR ({costo} 💰)"
                            : $"COMPRAR NV {nvActual + 1} ({costo} 💰)";
                        txtBtn.color = Color.white;
                        if (imgBtn != null) imgBtn.color = new Color(0.18f, 0.60f, 0.35f, 1f);
                    }
                }
            }
        }

        private void ComprarArticuloTiendaIndex(int index)
        {
            if (_puestitoActivo == null || _personajeLocal == null) return;
            if (index < 0 || index >= _puestitoActivo.itemsOfrecidos.Count) return;

            TipoMejoraTienda tipo = _puestitoActivo.itemsOfrecidos[index];
            TipoPersonaje req = CatalogoMejorasTienda.ObtenerClaseCompatible(tipo);
            if (req != TipoPersonaje.Ninguno && _personajeLocal.TipoPj != req)
            {
                TextoFlotante.Crear(_personajeLocal.transform.position + Vector3.up * 1.2f, "¡Incompatible con tu gato!", Color.red);
                return;
            }

            int nvActual = _personajeLocal.ObtenerNivelMejora(tipo);
            if (tipo != TipoMejoraTienda.PescadoAsado && nvActual >= 5)
            {
                TextoFlotante.Crear(_personajeLocal.transform.position + Vector3.up * 1.2f, "¡Ya tienes el nivel máximo!", Color.yellow);
                return;
            }

            if (tipo == TipoMejoraTienda.VidaMaxima && _personajeLocal.vidaMax >= 300)
            {
                TextoFlotante.Crear(_personajeLocal.transform.position + Vector3.up * 1.2f, "¡Vida al límite máximo (300 HP)!", Color.yellow);
                return;
            }

            int costo = CatalogoMejorasTienda.ObtenerCostoNivel(tipo, nvActual);
            int oroDisponible = (_oroVisualLocal >= 0) ? _oroVisualLocal : _personajeLocal.OroRun.Value;
            if (oroDisponible >= costo)
            {
                // Actualización visual reactiva e instantánea de la tienda en 0ms
                _oroVisualLocal = Mathf.Max(0, oroDisponible - costo);
                ActualizarItemsTiendaUI();

                _personajeLocal.SolicitarCompraTienda(tipo, costo);
            }
        }

        private void AplicarTipografiasPixelArt()
        {
            Font pixelFont = FuenteUtil.ObtenerFuentePixelify();
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
            imgPanel.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
            imgPanel.type = Image.Type.Sliced;
            imgPanel.color = Color.white;
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
            txtNom.font = FuenteUtil.ObtenerFuentePixelify();
            txtNom.fontSize = 14;
            txtNom.fontStyle = FontStyle.Bold;
            txtNom.alignment = TextAnchor.MiddleLeft;
            txtNom.color = EstiloUIUX.ColTextoCrema;
            txtNom.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtNom.verticalOverflow = VerticalWrapMode.Overflow;
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
            imgFondoBarra.color = EstiloUIUX.ColContornoUniversal;
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
            txtHp.font = FuenteUtil.ObtenerFuentePixelify();
            txtHp.fontSize = 11;
            txtHp.fontStyle = FontStyle.Bold;
            txtHp.alignment = TextAnchor.MiddleCenter;
            txtHp.color = Color.white;
            txtHp.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtHp.verticalOverflow = VerticalWrapMode.Overflow;
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

        public void LimpiarEfectosConsumibles()
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

            personaje.OroRun.OnValueChanged += (ant, act) =>
            {
                _oroVisualLocal = act;
                if (TiendaAbierta)
                {
                    ActualizarItemsTiendaUI();
                }
            };

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
                    ? $"Maestría Nv. {nivelMaestria} (MÁX)"
                    : $"Maestría Nv. {nivelMaestria}";
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
            if (_modalMapaGrandeHUD != null) _modalMapaGrandeHUD.SetActive(false);

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

            var modalObj = new GameObject("ModalAdvertenciaBucle");
            modalObj.transform.SetParent(transform, false);
            var rt = modalObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var imgFondoOscuro = modalObj.AddComponent<Image>();
            imgFondoOscuro.color = new Color(0f, 0f, 0f, 0.88f);

            // Ventana central con estilo panel
            var cajaObj = new GameObject("CajaModal");
            cajaObj.transform.SetParent(modalObj.transform, false);
            var rtCaja = cajaObj.AddComponent<RectTransform>();
            rtCaja.anchorMin = new Vector2(0.5f, 0.5f);
            rtCaja.anchorMax = new Vector2(0.5f, 0.5f);
            rtCaja.sizeDelta = new Vector2(660, 480);

            var imgCaja = cajaObj.AddComponent<Image>();
            imgCaja.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgCaja.type = Image.Type.Sliced;
            imgCaja.color = Color.white;

            EstiloUIUX.CrearRemaches(cajaObj.transform, 16f);
            EstiloUIUX.CrearCabeceraModal(cajaObj.transform, "BUCLE INFINITO", 70f, "huella");

            // Mensaje explicativo
            var msgObj = new GameObject("Mensaje");
            msgObj.transform.SetParent(cajaObj.transform, false);
            var rtMsg = msgObj.AddComponent<RectTransform>();
            rtMsg.anchorMin = new Vector2(0f, 0f);
            rtMsg.anchorMax = new Vector2(1f, 1f);
            rtMsg.offsetMin = new Vector2(30, 85);
            rtMsg.offsetMax = new Vector2(-30, -80);

            var txtMsg = msgObj.AddComponent<Text>();
            txtMsg.font = FuenteUtil.ObtenerFuentePixelify();
            txtMsg.fontSize = 15;
            txtMsg.lineSpacing = 1.35f;
            txtMsg.alignment = TextAnchor.UpperLeft;
            txtMsg.color = EstiloUIUX.ColTextoCrema;
            txtMsg.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtMsg.verticalOverflow = VerticalWrapMode.Overflow;
            txtMsg.text =
                "Has conquistado las recámaras de la mazmorra con éxito.\n\n" +
                "Si decides entrar al Bucle (Niveles 11 al 20+):\n" +
                "• Los enemigos serán más numerosos, letales y con habilidades reforzadas.\n" +
                "• Los jefes aumentarán su daño, resistencia y ferocidad.\n\n" +
                "⚠️ <color=#ffcf4a>CONDICIONES DE RIESGO:</color>\n" +
                "• Conservarás TODO tu oro acumulado.\n" +
                "• Sin embargo, perderás las recompensas compradas si todo el equipo cae en el bucle.\n\n" +
                "¿Aceptas el desafío extremo?";

            // Botón Aceptar / Continuar
            var btnA = EstiloUIUX.CrearBoton(cajaObj.transform, "BtnAceptar", "¡ACEPTAR DESAFÍO!", TipoBotonUI.Verde, new Vector2(230, 50), () =>
            {
                Destroy(modalObj);
                onConfirmar?.Invoke();
            }, "palomita", new Vector2(24, 18));
            var rtBtnA = btnA.GetComponent<RectTransform>();
            rtBtnA.anchorMin = new Vector2(0.5f, 0f);
            rtBtnA.anchorMax = new Vector2(0.5f, 0f);
            rtBtnA.pivot = new Vector2(0.5f, 0f);
            rtBtnA.anchoredPosition = new Vector2(130, 20);

            // Botón Cancelar / Volver
            var btnC = EstiloUIUX.CrearBoton(cajaObj.transform, "BtnCancelar", "VOLVER ATRÁS", TipoBotonUI.Piedra, new Vector2(200, 50), () =>
            {
                Destroy(modalObj);
            }, "puerta", new Vector2(22, 22));
            var rtBtnC = btnC.GetComponent<RectTransform>();
            rtBtnC.anchorMin = new Vector2(0.5f, 0f);
            rtBtnC.anchorMax = new Vector2(0.5f, 0f);
            rtBtnC.pivot = new Vector2(0.5f, 0f);
            rtBtnC.anchoredPosition = new Vector2(-130, 20);

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
            if (_modalMapaGrandeHUD != null) _modalMapaGrandeHUD.SetActive(false);
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
                ConfigurarEstiloFinPartida(victoria);
            }
        }

        private void ConfigurarEstiloFinPartida(bool victoria)
        {
            if (panelFinPartida == null) return;

            var imgFondo = panelFinPartida.GetComponent<Image>();
            if (imgFondo != null)
            {
                imgFondo.color = new Color(0.04f, 0.04f, 0.06f, 0.94f);
            }

            var modalVentana = panelFinPartida.transform.Find("VentanaFinPartidaModal");
            if (modalVentana == null)
            {
                var modalObj = new GameObject("VentanaFinPartidaModal");
                modalObj.transform.SetParent(panelFinPartida.transform, false);
                var rtM = modalObj.AddComponent<RectTransform>();
                rtM.anchorMin = new Vector2(0.5f, 0.5f);
                rtM.anchorMax = new Vector2(0.5f, 0.5f);
                rtM.pivot = new Vector2(0.5f, 0.5f);
                rtM.anchoredPosition = Vector2.zero;
                rtM.sizeDelta = new Vector2(760f, 380f);

                var imgModal = modalObj.AddComponent<Image>();
                imgModal.sprite = EstiloUIUX.ObtenerSpritePanel();
                imgModal.type = Image.Type.Sliced;
                imgModal.color = Color.white;

                EstiloUIUX.CrearRemaches(modalObj.transform, 16f);
                EstiloUIUX.CrearCabeceraModal(modalObj.transform, victoria ? "¡VICTORIA!" : "EXPEDICIÓN FALLIDA", 80f, "huella");

                modalVentana = modalObj.transform;

                if (textoResultado != null)
                {
                    textoResultado.transform.SetParent(modalVentana, false);
                    var rtTxt = textoResultado.GetComponent<RectTransform>();
                    rtTxt.anchorMin = new Vector2(0.5f, 0.5f);
                    rtTxt.anchorMax = new Vector2(0.5f, 0.5f);
                    rtTxt.pivot = new Vector2(0.5f, 0.5f);
                    rtTxt.anchoredPosition = new Vector2(0f, 15f);
                    rtTxt.sizeDelta = new Vector2(700f, 50f);
                }

                if (botonVolverLobby != null)
                {
                    botonVolverLobby.transform.SetParent(modalVentana, false);
                    var rtBtn = botonVolverLobby.GetComponent<RectTransform>();
                    rtBtn.anchorMin = new Vector2(0.5f, 0f);
                    rtBtn.anchorMax = new Vector2(0.5f, 0f);
                    rtBtn.pivot = new Vector2(0.5f, 0f);
                    rtBtn.anchoredPosition = new Vector2(0f, 35f);
                    rtBtn.sizeDelta = new Vector2(300f, 60f);
                }
            }

            if (textoResultado != null)
            {
                textoResultado.font = FuenteUtil.ObtenerFuentePixelify();
                textoResultado.fontSize = 28;
                textoResultado.fontStyle = FontStyle.Bold;
                textoResultado.alignment = TextAnchor.MiddleCenter;
                textoResultado.color = victoria ? EstiloUIUX.ColDorado : EstiloUIUX.ColRojo;
                textoResultado.text = victoria ? "¡HAS COMPLETADO LA MAZMORRA!" : "TODO EL EQUIPO HA CAÍDO EN COMBATE";
                textoResultado.horizontalOverflow = HorizontalWrapMode.Overflow;
                textoResultado.verticalOverflow = VerticalWrapMode.Overflow;
                BordeTextoPixelArt.Aplicar(textoResultado, 6, new Vector2(0f, -3f));
            }

            if (botonVolverLobby != null)
            {
                var txtBtn = botonVolverLobby.GetComponentInChildren<Text>();
                if (txtBtn != null) txtBtn.text = "VOLVER AL LOBBY";
                EstiloUIUX.AplicarEstiloBoton(botonVolverLobby, TipoBotonUI.Verde, 20, "puerta", new Vector2(24f, 24f));
            }
        }
    }
}
