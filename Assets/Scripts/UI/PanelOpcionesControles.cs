using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SoulKnight
{
    public enum PestanaOpciones
    {
        Audio,
        Video,
        Controles
    }

    /// <summary>
    /// Panel de Opciones y Configuración unificado y espacioso:
    /// - Pestaña 1: Audio (Sliders Master, Música, SFX + Canje de Códigos de Eventos/Comunidad) según Audio.dc.html.
    /// - Pestaña 2: Video (Daño acumulativo, Modo de pantalla, V-Sync, Calidad gráfica, Resolución) según Video.dc.html.
    /// - Pestaña 3: Controles (Grid 2 columnas: Movimiento, Combate, Sistema con keycaps) según Controls.dc.html.
    /// - Todo pixel art estricto 1:1 a 1920x1080 con esquinas mordidas y paleta oficial.
    /// </summary>
    public class PanelOpcionesControles : MonoBehaviour
    {
        public static PanelOpcionesControles Instancia { get; private set; }

        public GameObject panelContenedor;
        public Transform contenedorFilas;
        public Button botonCerrar;
        public Button botonRestaurar;
        public Text textoEstado;

        // Pestañas
        private GameObject _panelAudio;
        private GameObject _panelVideo;
        private GameObject _panelControles;
        private Button _btnTabAudio;
        private Button _btnTabVideo;
        private Button _btnTabControles;
        private Image _imgTabAudio;
        private Image _imgTabVideo;
        private Image _imgTabControles;
        private Text _txtTabAudio;
        private Text _txtTabVideo;
        private Text _txtTabControles;

        // Opciones de Video Pixel Art 1:1
        private InterruptorToggleUI _toggleDanoAcumulativo;
        private SelectorOpcionesUI _selectorPantallaCompleta;
        private InterruptorToggleUI _toggleVSync;
        private SelectorOpcionesUI _selectorCalidad;
        private SelectorOpcionesUI _selectorResolucion;
        private GameObject _wellContenedor;

        // Referencias heredadas para compatibilidad
        private Button _btnDanoAcumulativo;
        private Text _txtDanoAcumulativo;
        private Button _btnPantallaCompleta;
        private Text _txtPantallaCompleta;
        private Button _btnVSync;
        private Text _txtVSync;
        private Button _btnCalidad;
        private Text _txtCalidad;
        private Button _btnResolucion;
        private Text _txtResolucion;
        private readonly Resolution[] _resolucionesDisponibles = new Resolution[]
        {
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1366, height = 768 },
            new Resolution { width = 1280, height = 720 }
        };
        private int _indiceResolucion = 0;

        // Sliders Audio
        private Slider _sliderMaster;
        private Slider _sliderMusica;
        private Slider _sliderSFX;
        private Text _txtPctMaster;
        private Text _txtPctMusica;
        private Text _txtPctSFX;

        // Comandos y Canje de Códigos
        private InputField _inputCodigoAdmin;
        private Text _txtFeedbackComando;
        private GameObject _seccionAccesoAdmin;
        private GameObject _modalAdmin;
        private Text _txtFeedbackModalAdmin;
        private InputField _inputComandoModal;
        private bool _adminDesbloqueado = false;

        private PestanaOpciones _pestanaActual = PestanaOpciones.Audio;

        // Espera de tecla para controles
        private AccionControl? _accionEsperandoTecla = null;
        private Text _textoBotonEsperando = null;
        private Image _imgKeyEsperando = null;
        private RectTransform _rtKeyEsperando = null;

        private readonly Dictionary<AccionControl, Button> _botonesAccion = new Dictionary<AccionControl, Button>();
        private readonly Dictionary<AccionControl, Text> _textosAccion = new Dictionary<AccionControl, Text>();

        public static void AgregarOutline(Text txt, Color? color = null, Vector2? distancia = null)
        {
            if (txt == null) return;
            var outline = txt.GetComponent<Outline>();
            if (outline == null) outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = color ?? new Color(0.01f, 0.01f, 0.03f, 0.95f);
            outline.effectDistance = distancia ?? new Vector2(1.5f, -1.5f);
        }

        void Awake()
        {
            Instancia = this;
            AsegurarInterfazCompleta();
        }

        public static PanelOpcionesControles ObtenerOCrear(Transform parent)
        {
            if (Instancia != null)
            {
                if (parent != null && Instancia.transform.parent != parent)
                {
                    Instancia.transform.SetParent(parent, false);
                }
                Instancia.AsegurarInterfazCompleta();
                return Instancia;
            }

            var go = new GameObject("PanelOpcionesControles");
            if (parent != null) go.transform.SetParent(parent, false);

            var comp = go.AddComponent<PanelOpcionesControles>();
            comp.AsegurarInterfazCompleta();
            go.SetActive(false);
            return comp;
        }

        public void AsegurarInterfazCompleta()
        {
            if (_panelAudio != null && _panelVideo != null && _panelControles != null) return;

            // Limpiar hijos residuales antiguos para garantizar diseño renovado espacioso
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            panelContenedor = gameObject;

            // Canvas propio de alta prioridad para nunca quedar oculto
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            _adminDesbloqueado = PlayerPrefs.GetInt("AdminDesbloqueado", 0) == 1;

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var imgFondo = GetComponent<Image>();
            if (imgFondo == null) imgFondo = gameObject.AddComponent<Image>();
            imgFondo.color = new Color(0.03f, 0.04f, 0.07f, 0.95f);

            // ==================== FONDO Y CANVAS RESPONSIVO ====================
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Fondo con patrón de ladrillos pixel art
            var bgObj = new GameObject("FondoLadrillosMosaico");
            bgObj.transform.SetParent(transform, false);
            var rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;
            var rawBg = bgObj.AddComponent<RawImage>();
            rawBg.texture = EstiloUIUX.ObtenerTexturaLadrillos();
            rawBg.uvRect = new Rect(0, 0, 1920f / 128f, 1080f / 64f);
            rawBg.raycastTarget = false;

            // Capa oscura translúcida rgba(8, 4, 20, 0.62)
            var overObj = new GameObject("CapaOscuraFondo");
            overObj.transform.SetParent(transform, false);
            var rtOver = overObj.AddComponent<RectTransform>();
            rtOver.anchorMin = Vector2.zero;
            rtOver.anchorMax = Vector2.one;
            rtOver.sizeDelta = Vector2.zero;
            var imgOver = overObj.AddComponent<Image>();
            imgOver.color = new Color(8f / 255f, 4f / 255f, 20f / 255f, 0.62f);
            imgOver.raycastTarget = true;

            // ==================== PANEL PRINCIPAL 1240 x 940 ====================
            var ventanaObj = new GameObject("VentanaModal");
            ventanaObj.transform.SetParent(transform, false);
            var rtVentana = ventanaObj.AddComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.anchoredPosition = Vector2.zero;
            rtVentana.sizeDelta = new Vector2(1240f, 940f);

            var imgVentana = ventanaObj.AddComponent<Image>();
            imgVentana.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgVentana.type = Image.Type.Sliced;

            // Remaches dorados 8x8 en esquinas inferiores a 16px
            CrearRemache(ventanaObj.transform, "RemacheIzq", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f));
            CrearRemache(ventanaObj.transform, "RemacheDer", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f));

            // ==================== CABECERA DEL PANEL (Alto 124px) ====================
            var cabeceraObj = new GameObject("CabeceraPanel");
            cabeceraObj.transform.SetParent(ventanaObj.transform, false);
            var rtCab = cabeceraObj.AddComponent<RectTransform>();
            rtCab.anchorMin = new Vector2(0f, 1f);
            rtCab.anchorMax = new Vector2(1f, 1f);
            rtCab.pivot = new Vector2(0.5f, 1f);
            rtCab.anchoredPosition = Vector2.zero;
            rtCab.sizeDelta = new Vector2(0f, 124f);

            var imgCab = cabeceraObj.AddComponent<Image>();
            imgCab.color = EstiloUIUX.ColCabecera;

            // Borde inferior cabecera 6px #0c0818
            var lineaBorde = new GameObject("BordeInferiorCabecera");
            lineaBorde.transform.SetParent(cabeceraObj.transform, false);
            var rtLin = lineaBorde.AddComponent<RectTransform>();
            rtLin.anchorMin = new Vector2(0f, 0f);
            rtLin.anchorMax = new Vector2(1f, 0f);
            rtLin.pivot = new Vector2(0.5f, 0f);
            rtLin.sizeDelta = new Vector2(0f, 6f);
            var imgLin = lineaBorde.AddComponent<Image>();
            imgLin.color = EstiloUIUX.ColContornoUniversal;

            // Contenido Cabecera: Huella dorada 45x40 | Textos | Huella dorada 45x40
            var contCab = new GameObject("ContenidoCabecera");
            contCab.transform.SetParent(cabeceraObj.transform, false);
            var rtContCab = contCab.AddComponent<RectTransform>();
            rtContCab.anchorMin = Vector2.zero;
            rtContCab.anchorMax = Vector2.one;
            rtContCab.sizeDelta = Vector2.zero;

            var hlgCab = contCab.AddComponent<HorizontalLayoutGroup>();
            hlgCab.childAlignment = TextAnchor.MiddleCenter;
            hlgCab.spacing = 40f;
            hlgCab.childControlWidth = false;
            hlgCab.childControlHeight = false;

            CrearIconoCabecera(contCab.transform, "HuellaIzq");

            var bloqueTxtCab = new GameObject("BloqueTitulos");
            bloqueTxtCab.transform.SetParent(contCab.transform, false);
            var rtBloque = bloqueTxtCab.AddComponent<RectTransform>();
            rtBloque.sizeDelta = new Vector2(600f, 100f);
            var vlgCab = bloqueTxtCab.AddComponent<VerticalLayoutGroup>();
            vlgCab.childAlignment = TextAnchor.MiddleCenter;
            vlgCab.spacing = 4f;
            vlgCab.childControlWidth = true;
            vlgCab.childControlHeight = false;

            var txtTit = new GameObject("TituloOpciones").AddComponent<Text>();
            txtTit.transform.SetParent(bloqueTxtCab.transform, false);
            txtTit.font = FuenteUtil.ObtenerFuentePixelify();
            txtTit.fontSize = 60;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = EstiloUIUX.ColDorado;
            txtTit.text = "OPCIONES";
            txtTit.raycastTarget = false;
            txtTit.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 64f);
            BordeTextoPixelArt.Aplicar(txtTit, 8, new Vector2(0f, -6f));

            var txtSub = new GameObject("SubtituloOpciones").AddComponent<Text>();
            txtSub.transform.SetParent(bloqueTxtCab.transform, false);
            txtSub.font = FuenteUtil.ObtenerFuentePixelify();
            txtSub.fontSize = 20;
            txtSub.fontStyle = FontStyle.Bold;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = EstiloUIUX.ColTextoTenue;
            txtSub.text = "CONFIGURACIÓN DEL JUEGO";
            txtSub.raycastTarget = false;
            txtSub.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 24f);

            CrearIconoCabecera(contCab.transform, "HuellaDer");

            // ==================== BARRA DE PESTAÑAS (3 Botones de 364x68, Gap 20px) ====================
            var barraTabs = new GameObject("BarraPestanas");
            barraTabs.transform.SetParent(ventanaObj.transform, false);
            var rtTabs = barraTabs.AddComponent<RectTransform>();
            rtTabs.anchorMin = new Vector2(0.5f, 1f);
            rtTabs.anchorMax = new Vector2(0.5f, 1f);
            rtTabs.pivot = new Vector2(0.5f, 1f);
            rtTabs.anchoredPosition = new Vector2(0f, -152f);
            rtTabs.sizeDelta = new Vector2(1132f, 68f);

            _btnTabAudio = CrearBotonTab(barraTabs.transform, "TabAudio", "AUDIO", "audio", new Vector2(28f, 28f), new Vector2(-384f, 0f), 364f, out _imgTabAudio, out _txtTabAudio);
            _btnTabVideo = CrearBotonTab(barraTabs.transform, "TabVideo", "VIDEO", "monitor", new Vector2(35f, 28f), new Vector2(0f, 0f), 364f, out _imgTabVideo, out _txtTabVideo);
            _btnTabControles = CrearBotonTab(barraTabs.transform, "TabControles", "CONTROLES", "controles", new Vector2(40f, 28f), new Vector2(384f, 0f), 364f, out _imgTabControles, out _txtTabControles);

            // ==================== POZO DE CONTENIDO (WELL 1132 x 546) ====================
            _wellContenedor = new GameObject("PozoContenido");
            _wellContenedor.transform.SetParent(ventanaObj.transform, false);
            var rtWell = _wellContenedor.AddComponent<RectTransform>();
            rtWell.anchorMin = new Vector2(0.5f, 1f);
            rtWell.anchorMax = new Vector2(0.5f, 1f);
            rtWell.pivot = new Vector2(0.5f, 1f);
            rtWell.anchoredPosition = new Vector2(0f, -250f);
            rtWell.sizeDelta = new Vector2(1132f, 546f);

            var imgWell = _wellContenedor.AddComponent<Image>();
            imgWell.sprite = EstiloUIUX.ObtenerSpritePozo();
            imgWell.type = Image.Type.Sliced;

            // ==================== PESTAÑAS (AUDIO, VIDEO, CONTROLES) ====================
            ConstruirPanelAudio(_wellContenedor.transform);
            ConstruirPanelVideo(_wellContenedor.transform);
            ConstruirPanelControles(_wellContenedor.transform);

            // ==================== BARRA INFERIOR DE ACCIÓN GLOBAL ====================
            var barraInferior = new GameObject("BarraInferior");
            barraInferior.transform.SetParent(ventanaObj.transform, false);
            var rtBarraInf = barraInferior.AddComponent<RectTransform>();
            rtBarraInf.anchorMin = new Vector2(0.5f, 0f);
            rtBarraInf.anchorMax = new Vector2(0.5f, 0f);
            rtBarraInf.pivot = new Vector2(0.5f, 0f);
            rtBarraInf.anchoredPosition = new Vector2(0f, 36f);
            rtBarraInf.sizeDelta = new Vector2(1132f, 72f);

            // Botón Restaurar Controles (Rojo 440x72, solo para pestaña Controles)
            var btnRestObj = new GameObject("BotonRestaurar");
            btnRestObj.transform.SetParent(barraInferior.transform, false);
            var rtRest = btnRestObj.AddComponent<RectTransform>();
            rtRest.anchorMin = new Vector2(0f, 0.5f);
            rtRest.anchorMax = new Vector2(0f, 0.5f);
            rtRest.pivot = new Vector2(0f, 0.5f);
            rtRest.anchoredPosition = Vector2.zero;
            rtRest.sizeDelta = new Vector2(440f, 72f);

            var imgRest = btnRestObj.AddComponent<Image>();
            imgRest.type = Image.Type.Sliced;
            imgRest.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Rojo);

            botonRestaurar = btnRestObj.AddComponent<Button>();
            ConfigurarBotonPixel(botonRestaurar, RestaurarPorDefecto);

            var hlgRest = btnRestObj.AddComponent<HorizontalLayoutGroup>();
            hlgRest.childAlignment = TextAnchor.MiddleCenter;
            hlgRest.spacing = 14f;
            hlgRest.childControlWidth = false;
            hlgRest.childControlHeight = false;

            var icoRestObj = new GameObject("IconoRestaurar");
            icoRestObj.transform.SetParent(btnRestObj.transform, false);
            var imgIcoRest = icoRestObj.AddComponent<Image>();
            imgIcoRest.sprite = EstiloUIUX.ObtenerSpriteIcono("restaurar", EstiloUIUX.ColBtnRojoTexto);
            imgIcoRest.raycastTarget = false;
            icoRestObj.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

            var txtRestObj = new GameObject("Texto");
            txtRestObj.transform.SetParent(btnRestObj.transform, false);
            var txtRest = txtRestObj.AddComponent<Text>();
            txtRest.font = FuenteUtil.ObtenerFuentePixelify();
            txtRest.fontSize = 26;
            txtRest.fontStyle = FontStyle.Bold;
            txtRest.alignment = TextAnchor.MiddleCenter;
            txtRest.color = EstiloUIUX.ColBtnRojoTexto;
            txtRest.text = "RESTAURAR CONTROLES";
            txtRest.raycastTarget = false;
            txtRestObj.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 40f);

            // Botón Cerrar y Guardar (Verde 460x72 en Audio/Video, 440x72 en Controles)
            var btnCerrarObj = new GameObject("BotonCerrar");
            btnCerrarObj.transform.SetParent(barraInferior.transform, false);
            var rtCerrar = btnCerrarObj.AddComponent<RectTransform>();
            rtCerrar.anchorMin = new Vector2(0.5f, 0.5f);
            rtCerrar.anchorMax = new Vector2(0.5f, 0.5f);
            rtCerrar.pivot = new Vector2(0.5f, 0.5f);
            rtCerrar.anchoredPosition = Vector2.zero;
            rtCerrar.sizeDelta = new Vector2(460f, 72f);

            var imgCerrar = btnCerrarObj.AddComponent<Image>();
            imgCerrar.type = Image.Type.Sliced;
            imgCerrar.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Verde);

            botonCerrar = btnCerrarObj.AddComponent<Button>();
            ConfigurarBotonPixel(botonCerrar, Cerrar);

            var hlgCerrar = btnCerrarObj.AddComponent<HorizontalLayoutGroup>();
            hlgCerrar.childAlignment = TextAnchor.MiddleCenter;
            hlgCerrar.spacing = 14f;
            hlgCerrar.childControlWidth = false;
            hlgCerrar.childControlHeight = false;

            var icoPalObj = new GameObject("IconoPalomita");
            icoPalObj.transform.SetParent(btnCerrarObj.transform, false);
            var imgPal = icoPalObj.AddComponent<Image>();
            imgPal.sprite = EstiloUIUX.ObtenerSpriteIcono("palomita", EstiloUIUX.ColBtnVerdeTexto);
            imgPal.raycastTarget = false;
            icoPalObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 20f);

            var txtCerrarObj = new GameObject("Texto");
            txtCerrarObj.transform.SetParent(btnCerrarObj.transform, false);
            var txtCerrar = txtCerrarObj.AddComponent<Text>();
            txtCerrar.font = FuenteUtil.ObtenerFuentePixelify();
            txtCerrar.fontSize = 30;
            txtCerrar.fontStyle = FontStyle.Bold;
            txtCerrar.alignment = TextAnchor.MiddleCenter;
            txtCerrar.color = EstiloUIUX.ColBtnVerdeTexto;
            txtCerrar.text = "CERRAR Y GUARDAR";
            txtCerrar.raycastTarget = false;
            txtCerrarObj.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 40f);

            // Listeners pestañas
            _btnTabAudio.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Audio));
            if (_btnTabVideo != null) _btnTabVideo.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Video));
            _btnTabControles.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Controles));

            ConstruirModalAdmin(transform);
            CambiarPestana(PestanaOpciones.Audio);
        }

        // =========================================================================
        // PESTAÑA AUDIO (1:1 con Audio.dc.html)
        // =========================================================================
        private void ConstruirPanelAudio(Transform wellTransform)
        {
            _panelAudio = new GameObject("PanelAudio");
            _panelAudio.transform.SetParent(wellTransform, false);
            var rtAudio = _panelAudio.AddComponent<RectTransform>();
            rtAudio.anchorMin = Vector2.zero;
            rtAudio.anchorMax = Vector2.one;
            rtAudio.sizeDelta = Vector2.zero;
            rtAudio.offsetMin = new Vector2(40f, 24f);
            rtAudio.offsetMax = new Vector2(-40f, -24f);

            // Header Volúmenes (Alto 52px)
            var headerVol = new GameObject("HeaderVolumenes");
            headerVol.transform.SetParent(_panelAudio.transform, false);
            var rtHVol = headerVol.AddComponent<RectTransform>();
            rtHVol.anchorMin = new Vector2(0f, 1f);
            rtHVol.anchorMax = new Vector2(1f, 1f);
            rtHVol.pivot = new Vector2(0.5f, 1f);
            rtHVol.anchoredPosition = new Vector2(0f, 0f);
            rtHVol.sizeDelta = new Vector2(0f, 52f);

            // Izquierda: Huella + "NIVELES DE VOLUMEN" + "Se guardan automáticamente"
            var izqVol = new GameObject("LadoIzq");
            izqVol.transform.SetParent(headerVol.transform, false);
            var rtIzqVol = izqVol.AddComponent<RectTransform>();
            rtIzqVol.anchorMin = new Vector2(0f, 0.5f);
            rtIzqVol.anchorMax = new Vector2(0f, 0.5f);
            rtIzqVol.pivot = new Vector2(0f, 0.5f);
            rtIzqVol.anchoredPosition = Vector2.zero;
            rtIzqVol.sizeDelta = new Vector2(750f, 52f);

            var hlgIzqVol = izqVol.AddComponent<HorizontalLayoutGroup>();
            hlgIzqVol.childAlignment = TextAnchor.MiddleLeft;
            hlgIzqVol.spacing = 14f;
            hlgIzqVol.childControlWidth = false;
            hlgIzqVol.childControlHeight = false;

            var icoH = new GameObject("IconoHuella").AddComponent<Image>();
            icoH.transform.SetParent(izqVol.transform, false);
            icoH.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            icoH.raycastTarget = false;
            icoH.rectTransform.sizeDelta = new Vector2(27f, 24f);

            var txtTitH = new GameObject("Titulo").AddComponent<Text>();
            txtTitH.transform.SetParent(izqVol.transform, false);
            txtTitH.font = FuenteUtil.ObtenerFuentePixelify();
            txtTitH.fontSize = 28;
            txtTitH.fontStyle = FontStyle.Bold;
            txtTitH.color = EstiloUIUX.ColDorado;
            txtTitH.text = "NIVELES DE VOLUMEN";
            txtTitH.raycastTarget = false;
            txtTitH.rectTransform.sizeDelta = new Vector2(300f, 32f);

            var txtSubH = new GameObject("Subtitulo").AddComponent<Text>();
            txtSubH.transform.SetParent(izqVol.transform, false);
            txtSubH.font = FuenteUtil.ObtenerFuentePixelify();
            txtSubH.fontSize = 20;
            txtSubH.color = EstiloUIUX.ColTextoTenue;
            txtSubH.text = "Se guardan automáticamente";
            txtSubH.raycastTarget = false;
            txtSubH.rectTransform.sizeDelta = new Vector2(320f, 26f);

            // Botón Probar Efecto (Hielo 240x52px)
            var btnProbarObj = new GameObject("BotonProbarEfecto");
            btnProbarObj.transform.SetParent(headerVol.transform, false);
            var rtProbar = btnProbarObj.AddComponent<RectTransform>();
            rtProbar.anchorMin = new Vector2(1f, 0.5f);
            rtProbar.anchorMax = new Vector2(1f, 0.5f);
            rtProbar.pivot = new Vector2(1f, 0.5f);
            rtProbar.anchoredPosition = Vector2.zero;
            rtProbar.sizeDelta = new Vector2(240f, 52f);

            var imgProbar = btnProbarObj.AddComponent<Image>();
            imgProbar.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Hielo);
            imgProbar.type = Image.Type.Sliced;
            var btnProbar = btnProbarObj.AddComponent<Button>();
            ConfigurarBotonPixel(btnProbar, () => GestorAudio.PlaySFX(TipoSFX.Mejora, 1.0f));

            var hlgProbar = btnProbarObj.AddComponent<HorizontalLayoutGroup>();
            hlgProbar.childAlignment = TextAnchor.MiddleCenter;
            hlgProbar.spacing = 14f;
            hlgProbar.childControlWidth = false;
            hlgProbar.childControlHeight = false;

            var icoProbar = new GameObject("Icono").AddComponent<Image>();
            icoProbar.transform.SetParent(btnProbarObj.transform, false);
            icoProbar.sprite = EstiloUIUX.ObtenerSpriteIcono("destello", EstiloUIUX.ColBtnHieloTexto);
            icoProbar.raycastTarget = false;
            icoProbar.rectTransform.sizeDelta = new Vector2(24f, 24f);

            var txtProbar = new GameObject("Texto").AddComponent<Text>();
            txtProbar.transform.SetParent(btnProbarObj.transform, false);
            txtProbar.font = FuenteUtil.ObtenerFuentePixelify();
            txtProbar.fontSize = 22;
            txtProbar.fontStyle = FontStyle.Bold;
            txtProbar.color = EstiloUIUX.ColBtnHieloTexto;
            txtProbar.text = "PROBAR EFECTO";
            txtProbar.raycastTarget = false;
            txtProbar.rectTransform.sizeDelta = new Vector2(160f, 28f);

            // Contenedor de Sliders (Alto 3 x 56 + 2 x 6 = 180px)
            var contSliders = new GameObject("ContenedorSliders");
            contSliders.transform.SetParent(_panelAudio.transform, false);
            var rtContSl = contSliders.AddComponent<RectTransform>();
            rtContSl.anchorMin = new Vector2(0f, 1f);
            rtContSl.anchorMax = new Vector2(1f, 1f);
            rtContSl.pivot = new Vector2(0.5f, 1f);
            rtContSl.anchoredPosition = new Vector2(0f, -58f);
            rtContSl.sizeDelta = new Vector2(0f, 180f);

            var vlgSl = contSliders.AddComponent<VerticalLayoutGroup>();
            vlgSl.childAlignment = TextAnchor.UpperCenter;
            vlgSl.spacing = 6f;
            vlgSl.childControlWidth = true;
            vlgSl.childControlHeight = false;

            _sliderMaster = CrearSliderFilaUIUX(contSliders.transform, "SliderMaster", "audio", new Color32(0x8F, 0xE0, 0xFF, 0xFF),
                "Volumen general", "Master", "ice", GestorAudio.Instancia != null ? GestorAudio.Instancia.VolumenMaster : 0.8f,
                (val) => { if (GestorAudio.Instancia != null) GestorAudio.Instancia.SetVolumenMaster(val); }, out _txtPctMaster);

            _sliderMusica = CrearSliderFilaUIUX(contSliders.transform, "SliderMusica", "musica", new Color32(0xFF, 0x8F, 0xB1, 0xFF),
                "Música de fondo", "BGM", "pink", GestorAudio.Instancia != null ? GestorAudio.Instancia.VolumenMusica : 0.6f,
                (val) => { if (GestorAudio.Instancia != null) GestorAudio.Instancia.SetVolumenMusica(val); }, out _txtPctMusica);

            _sliderSFX = CrearSliderFilaUIUX(contSliders.transform, "SliderSFX", "destello", new Color32(0xFF, 0xB3, 0x6B, 0xFF),
                "Efectos de sonido", "SFX", "fire", GestorAudio.Instancia != null ? GestorAudio.Instancia.VolumenSFX : 0.8f,
                (val) => { if (GestorAudio.Instancia != null) GestorAudio.Instancia.SetVolumenSFX(val); }, out _txtPctSFX);

            // Divisor 4px #2a2150
            var divObj = new GameObject("Divisor");
            divObj.transform.SetParent(_panelAudio.transform, false);
            var rtDiv = divObj.AddComponent<RectTransform>();
            rtDiv.anchorMin = new Vector2(0f, 1f);
            rtDiv.anchorMax = new Vector2(1f, 1f);
            rtDiv.pivot = new Vector2(0.5f, 1f);
            rtDiv.anchoredPosition = new Vector2(0f, -250f);
            rtDiv.sizeDelta = new Vector2(0f, 4f);
            var imgDiv = divObj.AddComponent<Image>();
            imgDiv.color = EstiloUIUX.ColPanel;

            // ==================== SECCIÓN 2: CANJEAR CÓDIGO ====================
            var secCanje = new GameObject("SeccionCanjearCodigo");
            secCanje.transform.SetParent(_panelAudio.transform, false);
            var rtCanje = secCanje.AddComponent<RectTransform>();
            rtCanje.anchorMin = new Vector2(0f, 1f);
            rtCanje.anchorMax = new Vector2(1f, 1f);
            rtCanje.pivot = new Vector2(0.5f, 1f);
            rtCanje.anchoredPosition = new Vector2(0f, -266f);
            rtCanje.sizeDelta = new Vector2(0f, 210f);

            // Header Canje
            var hCanje = new GameObject("HeaderCanje");
            hCanje.transform.SetParent(secCanje.transform, false);
            var rtHCanje = hCanje.AddComponent<RectTransform>();
            rtHCanje.anchorMin = new Vector2(0f, 1f);
            rtHCanje.anchorMax = new Vector2(1f, 1f);
            rtHCanje.pivot = new Vector2(0f, 1f);
            rtHCanje.anchoredPosition = Vector2.zero;
            rtHCanje.sizeDelta = new Vector2(0f, 36f);

            var hlgHCanje = hCanje.AddComponent<HorizontalLayoutGroup>();
            hlgHCanje.childAlignment = TextAnchor.MiddleLeft;
            hlgHCanje.spacing = 16f;
            hlgHCanje.childControlWidth = false;
            hlgHCanje.childControlHeight = false;

            var icoCanje = new GameObject("Icono").AddComponent<Image>();
            icoCanje.transform.SetParent(hCanje.transform, false);
            icoCanje.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            icoCanje.raycastTarget = false;
            icoCanje.rectTransform.sizeDelta = new Vector2(27f, 24f);

            var txtTitC = new GameObject("Titulo").AddComponent<Text>();
            txtTitC.transform.SetParent(hCanje.transform, false);
            txtTitC.font = FuenteUtil.ObtenerFuentePixelify();
            txtTitC.fontSize = 28;
            txtTitC.fontStyle = FontStyle.Bold;
            txtTitC.color = EstiloUIUX.ColDorado;
            txtTitC.text = "CANJEAR CÓDIGO";
            txtTitC.raycastTarget = false;
            txtTitC.rectTransform.sizeDelta = new Vector2(250f, 32f);

            // Badge EVENTOS / COMUNIDAD
            var badgeObj = new GameObject("BadgeComunidad");
            badgeObj.transform.SetParent(hCanje.transform, false);
            var imgBadge = badgeObj.AddComponent<Image>();
            imgBadge.color = EstiloUIUX.ColBtnDoradoCara;
            badgeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 32f);

            var txtBadge = new GameObject("Texto").AddComponent<Text>();
            txtBadge.transform.SetParent(badgeObj.transform, false);
            txtBadge.font = FuenteUtil.ObtenerFuentePixelify();
            txtBadge.fontSize = 18;
            txtBadge.fontStyle = FontStyle.Bold;
            txtBadge.alignment = TextAnchor.MiddleCenter;
            txtBadge.color = EstiloUIUX.ColBtnDoradoTexto;
            txtBadge.text = "EVENTOS / COMUNIDAD";
            txtBadge.raycastTarget = false;
            var rtTxtBadge = txtBadge.GetComponent<RectTransform>();
            rtTxtBadge.anchorMin = Vector2.zero;
            rtTxtBadge.anchorMax = Vector2.one;
            rtTxtBadge.sizeDelta = Vector2.zero;

            // Subtítulo Canje
            var descC = new GameObject("DescCanje");
            descC.transform.SetParent(secCanje.transform, false);
            var txtDescC = descC.AddComponent<Text>();
            txtDescC.font = FuenteUtil.ObtenerFuentePixelify();
            txtDescC.fontSize = 20;
            txtDescC.color = EstiloUIUX.ColTextoTenue;
            txtDescC.text = "Introduce un código promocional de evento o una clave para canjear recompensas.";
            txtDescC.raycastTarget = false;
            var rtDescC = descC.GetComponent<RectTransform>();
            rtDescC.anchorMin = new Vector2(0f, 1f);
            rtDescC.anchorMax = new Vector2(1f, 1f);
            rtDescC.pivot = new Vector2(0f, 1f);
            rtDescC.anchoredPosition = new Vector2(0f, -40f);
            rtDescC.sizeDelta = new Vector2(0f, 24f);

            // Fila Input + Botón (Alto 64px, gap 36px)
            var filaInput = new GameObject("FilaInput");
            filaInput.transform.SetParent(secCanje.transform, false);
            var rtFilaInp = filaInput.AddComponent<RectTransform>();
            rtFilaInp.anchorMin = new Vector2(0f, 1f);
            rtFilaInp.anchorMax = new Vector2(1f, 1f);
            rtFilaInp.pivot = new Vector2(0f, 1f);
            rtFilaInp.anchoredPosition = new Vector2(0f, -68f);
            rtFilaInp.sizeDelta = new Vector2(0f, 64f);

            // InputField 700px ancho
            var inpObj = new GameObject("InputField");
            inpObj.transform.SetParent(filaInput.transform, false);
            var rtInp = inpObj.AddComponent<RectTransform>();
            rtInp.anchorMin = new Vector2(0f, 0.5f);
            rtInp.anchorMax = new Vector2(1f, 0.5f);
            rtInp.pivot = new Vector2(0f, 0.5f);
            rtInp.offsetMin = new Vector2(0f, -32f);
            rtInp.offsetMax = new Vector2(-320f, 32f);

            var imgInp = inpObj.AddComponent<Image>();
            imgInp.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputCodigo", EstiloUIUX.ColValorFondo, EstiloUIUX.ColValorSombra, EstiloUIUX.ColValorFondo, 6, 6, 0);
            imgInp.type = Image.Type.Sliced;

            _inputCodigoAdmin = inpObj.AddComponent<InputField>();

            // Placeholder
            var plObj = new GameObject("Placeholder");
            plObj.transform.SetParent(inpObj.transform, false);
            var txtPl = plObj.AddComponent<Text>();
            txtPl.font = FuenteUtil.ObtenerFuentePixelify();
            txtPl.fontSize = 26;
            txtPl.color = EstiloUIUX.ColTextoMuyTenue;
            txtPl.text = "Escribe tu código aquí…";
            txtPl.alignment = TextAnchor.MiddleLeft;
            var rtPl = plObj.GetComponent<RectTransform>();
            rtPl.anchorMin = Vector2.zero;
            rtPl.anchorMax = Vector2.one;
            rtPl.offsetMin = new Vector2(24f, 0f);
            rtPl.offsetMax = new Vector2(-24f, 0f);

            // Texto Input
            var txtInpVal = new GameObject("Text").AddComponent<Text>();
            txtInpVal.transform.SetParent(inpObj.transform, false);
            txtInpVal.font = FuenteUtil.ObtenerFuentePixelify();
            txtInpVal.fontSize = 26;
            txtInpVal.color = EstiloUIUX.ColTextoCrema;
            txtInpVal.alignment = TextAnchor.MiddleLeft;
            txtInpVal.supportRichText = false;
            var rtTxtInp = txtInpVal.GetComponent<RectTransform>();
            rtTxtInp.anchorMin = Vector2.zero;
            rtTxtInp.anchorMax = Vector2.one;
            rtTxtInp.offsetMin = new Vector2(24f, 0f);
            rtTxtInp.offsetMax = new Vector2(-24f, 0f);

            _inputCodigoAdmin.textComponent = txtInpVal;
            _inputCodigoAdmin.placeholder = txtPl;
            _inputCodigoAdmin.caretColor = EstiloUIUX.ColDorado;
            _inputCodigoAdmin.onEndEdit.AddListener((val) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ProcesarCodigoInput(val);
                }
            });

            // Botón Canjear 300x64px
            var btnCanjear = new GameObject("BotonCanjear");
            btnCanjear.transform.SetParent(filaInput.transform, false);
            var rtBtnC = btnCanjear.AddComponent<RectTransform>();
            rtBtnC.anchorMin = new Vector2(1f, 0.5f);
            rtBtnC.anchorMax = new Vector2(1f, 0.5f);
            rtBtnC.pivot = new Vector2(1f, 0.5f);
            rtBtnC.anchoredPosition = Vector2.zero;
            rtBtnC.sizeDelta = new Vector2(300f, 64f);

            var imgBtnC = btnCanjear.AddComponent<Image>();
            imgBtnC.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Verde);
            imgBtnC.type = Image.Type.Sliced;
            var btnC = btnCanjear.AddComponent<Button>();
            ConfigurarBotonPixel(btnC, () =>
            {
                if (_inputCodigoAdmin != null) ProcesarCodigoInput(_inputCodigoAdmin.text);
            });

            var txtBtnC = new GameObject("Texto").AddComponent<Text>();
            txtBtnC.transform.SetParent(btnCanjear.transform, false);
            txtBtnC.font = FuenteUtil.ObtenerFuentePixelify();
            txtBtnC.fontSize = 24;
            txtBtnC.fontStyle = FontStyle.Bold;
            txtBtnC.alignment = TextAnchor.MiddleCenter;
            txtBtnC.color = EstiloUIUX.ColBtnVerdeTexto;
            txtBtnC.text = "CANJEAR CÓDIGO";
            txtBtnC.raycastTarget = false;
            var rtTxtBC = txtBtnC.GetComponent<RectTransform>();
            rtTxtBC.anchorMin = Vector2.zero;
            rtTxtBC.anchorMax = Vector2.one;
            rtTxtBC.sizeDelta = Vector2.zero;

            // Nota informativa + Feedback
            var notaC = new GameObject("NotaCanje");
            notaC.transform.SetParent(secCanje.transform, false);
            _txtFeedbackComando = notaC.AddComponent<Text>();
            _txtFeedbackComando.font = FuenteUtil.ObtenerFuentePixelify();
            _txtFeedbackComando.fontSize = 18;
            _txtFeedbackComando.color = EstiloUIUX.ColTextoTenue;
            _txtFeedbackComando.text = "Escribe tu código y pulsa «Canjear» para activar recompensas o modos especiales.";
            var rtNotaC = notaC.GetComponent<RectTransform>();
            rtNotaC.anchorMin = new Vector2(0f, 1f);
            rtNotaC.anchorMax = new Vector2(1f, 1f);
            rtNotaC.pivot = new Vector2(0f, 1f);
            rtNotaC.anchoredPosition = new Vector2(0f, -140f);
            rtNotaC.sizeDelta = new Vector2(0f, 22f);

            // Botón rápido de Acceso Admin si ya fue desbloqueado
            _seccionAccesoAdmin = new GameObject("BotonAccesoAdminRapido");
            _seccionAccesoAdmin.transform.SetParent(secCanje.transform, false);
            var rtBtnAdm = _seccionAccesoAdmin.AddComponent<RectTransform>();
            rtBtnAdm.anchorMin = new Vector2(1f, 1f);
            rtBtnAdm.anchorMax = new Vector2(1f, 1f);
            rtBtnAdm.pivot = new Vector2(1f, 1f);
            rtBtnAdm.anchoredPosition = new Vector2(0f, -136f);
            rtBtnAdm.sizeDelta = new Vector2(220f, 32f);
            var imgBtnAdm = _seccionAccesoAdmin.AddComponent<Image>();
            imgBtnAdm.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Dorado);
            imgBtnAdm.type = Image.Type.Sliced;
            var btnAdm = _seccionAccesoAdmin.AddComponent<Button>();
            ConfigurarBotonPixel(btnAdm, AbrirModalAdmin);

            var txtBtnAdm = new GameObject("Texto").AddComponent<Text>();
            txtBtnAdm.transform.SetParent(_seccionAccesoAdmin.transform, false);
            txtBtnAdm.font = FuenteUtil.ObtenerFuentePixelify();
            txtBtnAdm.fontSize = 16;
            txtBtnAdm.fontStyle = FontStyle.Bold;
            txtBtnAdm.alignment = TextAnchor.MiddleCenter;
            txtBtnAdm.color = EstiloUIUX.ColBtnDoradoTexto;
            txtBtnAdm.text = "👑 PANEL ADMIN";
            txtBtnAdm.raycastTarget = false;
            var rtTxtBA = txtBtnAdm.GetComponent<RectTransform>();
            rtTxtBA.anchorMin = Vector2.zero;
            rtTxtBA.anchorMax = Vector2.one;
            rtTxtBA.sizeDelta = Vector2.zero;

            ActualizarVisibilidadAdmin();
        }

        private Slider CrearSliderFilaUIUX(Transform parent, string nombre, string icono, Color32 colorIcono,
            string titulo, string subtitulo, string tipoColorFill, float valorInicial, Action<float> onCambio, out Text outTxtPct)
        {
            var filaObj = new GameObject(nombre);
            filaObj.transform.SetParent(parent, false);
            var rtFila = filaObj.AddComponent<RectTransform>();
            rtFila.sizeDelta = new Vector2(0f, 56f);

            // Ícono izquierda (40x40)
            var icoObj = new GameObject("Icono").AddComponent<Image>();
            icoObj.transform.SetParent(filaObj.transform, false);
            icoObj.sprite = EstiloUIUX.ObtenerSpriteIcono(icono, colorIcono);
            icoObj.raycastTarget = false;
            var rtIco = icoObj.rectTransform;
            rtIco.anchorMin = new Vector2(0f, 0.5f);
            rtIco.anchorMax = new Vector2(0f, 0.5f);
            rtIco.pivot = new Vector2(0f, 0.5f);
            rtIco.anchoredPosition = new Vector2(0f, 0f);
            rtIco.sizeDelta = new Vector2(40f, 40f);

            // Bloque Textos (ancho 300px)
            var txtCont = new GameObject("Textos");
            txtCont.transform.SetParent(filaObj.transform, false);
            var rtTxtCont = txtCont.AddComponent<RectTransform>();
            rtTxtCont.anchorMin = new Vector2(0f, 0.5f);
            rtTxtCont.anchorMax = new Vector2(0f, 0.5f);
            rtTxtCont.pivot = new Vector2(0f, 0.5f);
            rtTxtCont.anchoredPosition = new Vector2(56f, 0f);
            rtTxtCont.sizeDelta = new Vector2(300f, 54f);

            var vlgTxt = txtCont.AddComponent<VerticalLayoutGroup>();
            vlgTxt.childAlignment = TextAnchor.MiddleLeft;
            vlgTxt.spacing = 2f;
            vlgTxt.childControlWidth = true;
            vlgTxt.childControlHeight = false;

            var txtTit = new GameObject("Titulo").AddComponent<Text>();
            txtTit.transform.SetParent(txtCont.transform, false);
            txtTit.font = FuenteUtil.ObtenerFuentePixelify();
            txtTit.fontSize = 28;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.color = EstiloUIUX.ColTextoCrema;
            txtTit.text = titulo;
            txtTit.lineSpacing = 1.1f;
            txtTit.raycastTarget = false;
            txtTit.rectTransform.sizeDelta = new Vector2(300f, 28f);

            var txtSub = new GameObject("Subtitulo").AddComponent<Text>();
            txtSub.transform.SetParent(txtCont.transform, false);
            txtSub.font = FuenteUtil.ObtenerFuentePixelify();
            txtSub.fontSize = 18;
            txtSub.color = EstiloUIUX.ColTextoTenue;
            txtSub.text = subtitulo;
            txtSub.raycastTarget = false;
            txtSub.rectTransform.sizeDelta = new Vector2(300f, 20f);

            // Track Slider (flex entre 370px y el porcentaje derecho)
            var trackObj = new GameObject("Track");
            trackObj.transform.SetParent(filaObj.transform, false);
            var rtTrack = trackObj.AddComponent<RectTransform>();
            rtTrack.anchorMin = new Vector2(0f, 0.5f);
            rtTrack.anchorMax = new Vector2(1f, 0.5f);
            rtTrack.pivot = new Vector2(0.5f, 0.5f);
            rtTrack.offsetMin = new Vector2(370f, -12f);
            rtTrack.offsetMax = new Vector2(-120f, 12f);

            var imgTrack = trackObj.AddComponent<Image>();
            imgTrack.sprite = EstiloUIUX.ObtenerSpritePistaSlider();
            imgTrack.type = Image.Type.Sliced;

            // Fill Area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(trackObj.transform, false);
            var rtFillArea = fillArea.AddComponent<RectTransform>();
            rtFillArea.anchorMin = Vector2.zero;
            rtFillArea.anchorMax = Vector2.one;
            rtFillArea.offsetMin = Vector2.zero;
            rtFillArea.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var rtFill = fillObj.AddComponent<RectTransform>();
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.offsetMin = Vector2.zero;
            rtFill.offsetMax = Vector2.zero;
            var imgFill = fillObj.AddComponent<Image>();
            imgFill.sprite = EstiloUIUX.ObtenerSpriteFillSlider(tipoColorFill);
            imgFill.type = Image.Type.Sliced;

            // Handle Slide Area
            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(trackObj.transform, false);
            var rtHandleArea = handleArea.AddComponent<RectTransform>();
            rtHandleArea.anchorMin = Vector2.zero;
            rtHandleArea.anchorMax = Vector2.one;
            rtHandleArea.offsetMin = new Vector2(12f, 0f);
            rtHandleArea.offsetMax = new Vector2(-12f, 0f);

            var thumbObj = new GameObject("Thumb");
            thumbObj.transform.SetParent(handleArea.transform, false);
            var rtThumb = thumbObj.AddComponent<RectTransform>();
            rtThumb.sizeDelta = new Vector2(24f, 48f);
            var imgThumb = thumbObj.AddComponent<Image>();
            imgThumb.sprite = EstiloUIUX.ObtenerSpriteThumbSlider();
            imgThumb.raycastTarget = true;

            // Slider component
            var slider = trackObj.AddComponent<Slider>();
            slider.targetGraphic = imgThumb;
            slider.fillRect = rtFill;
            slider.handleRect = rtThumb;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            // Porcentaje Texto
            var pctObj = new GameObject("TextoPorcentaje").AddComponent<Text>();
            pctObj.transform.SetParent(filaObj.transform, false);
            pctObj.font = FuenteUtil.ObtenerFuentePixelify();
            pctObj.fontSize = 36;
            pctObj.fontStyle = FontStyle.Bold;
            pctObj.alignment = TextAnchor.MiddleRight;
            pctObj.color = EstiloUIUX.ColDorado;
            pctObj.raycastTarget = false;
            var rtPct = pctObj.rectTransform;
            rtPct.anchorMin = new Vector2(1f, 0.5f);
            rtPct.anchorMax = new Vector2(1f, 0.5f);
            rtPct.pivot = new Vector2(1f, 0.5f);
            rtPct.anchoredPosition = Vector2.zero;
            rtPct.sizeDelta = new Vector2(110f, 44f);
            outTxtPct = pctObj;

            var pctRef = pctObj;
            slider.onValueChanged.AddListener((val) =>
            {
                if (pctRef != null) pctRef.text = $"{Mathf.RoundToInt(val * 100f)}%";
                onCambio?.Invoke(val);
            });

            slider.value = valorInicial;
            pctObj.text = $"{Mathf.RoundToInt(valorInicial * 100f)}%";

            return slider;
        }

        // =========================================================================
        // PESTAÑA VIDEO (1:1 con Video.dc.html)
        // =========================================================================
        private void ConstruirPanelVideo(Transform wellTransform)
        {
            _panelVideo = new GameObject("PanelVideo");
            _panelVideo.transform.SetParent(wellTransform, false);
            var rtVideo = _panelVideo.AddComponent<RectTransform>();
            rtVideo.anchorMin = Vector2.zero;
            rtVideo.anchorMax = Vector2.one;
            rtVideo.sizeDelta = Vector2.zero;
            rtVideo.offsetMin = new Vector2(40f, 28f);
            rtVideo.offsetMax = new Vector2(-40f, -28f);

            // Encabezado: Huella dorada 27x24 + "AJUSTES DE VIDEO Y PANTALLA"
            var titSecObj = new GameObject("EncabezadoSeccion");
            titSecObj.transform.SetParent(_panelVideo.transform, false);
            var rtTitSec = titSecObj.AddComponent<RectTransform>();
            rtTitSec.anchorMin = new Vector2(0f, 1f);
            rtTitSec.anchorMax = new Vector2(1f, 1f);
            rtTitSec.pivot = new Vector2(0f, 1f);
            rtTitSec.anchoredPosition = new Vector2(6f, 0f);
            rtTitSec.sizeDelta = new Vector2(0f, 36f);

            var hlgTit = titSecObj.AddComponent<HorizontalLayoutGroup>();
            hlgTit.childAlignment = TextAnchor.MiddleLeft;
            hlgTit.spacing = 14f;
            hlgTit.childControlWidth = false;
            hlgTit.childControlHeight = false;

            var icoHuella = new GameObject("IconoHuella");
            icoHuella.transform.SetParent(titSecObj.transform, false);
            var imgHuella = icoHuella.AddComponent<Image>();
            imgHuella.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            imgHuella.raycastTarget = false;
            var rtIcoH = icoHuella.GetComponent<RectTransform>();
            rtIcoH.sizeDelta = new Vector2(27f, 24f);

            var txtTitSec = new GameObject("Texto").AddComponent<Text>();
            txtTitSec.transform.SetParent(titSecObj.transform, false);
            txtTitSec.font = FuenteUtil.ObtenerFuentePixelify();
            txtTitSec.fontSize = 28;
            txtTitSec.fontStyle = FontStyle.Bold;
            txtTitSec.color = EstiloUIUX.ColDorado;
            txtTitSec.text = "AJUSTES DE VIDEO Y PANTALLA";
            txtTitSec.raycastTarget = false;
            var rtTxtSec = txtTitSec.GetComponent<RectTransform>();
            rtTxtSec.sizeDelta = new Vector2(700f, 32f);

            // Contenedor de las 5 Cards
            var contCards = new GameObject("ContenedorCards");
            contCards.transform.SetParent(_panelVideo.transform, false);
            var rtCards = contCards.AddComponent<RectTransform>();
            rtCards.anchorMin = new Vector2(0f, 0f);
            rtCards.anchorMax = new Vector2(1f, 1f);
            rtCards.pivot = new Vector2(0.5f, 1f);
            rtCards.offsetMin = new Vector2(6f, 0f);
            rtCards.offsetMax = new Vector2(-6f, -44f);

            var vlg = contCards.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // 1. Daño acumulativo en pantalla
            var card1 = CrearFilaCard(contCards.transform, "CardDanoAcumulativo",
                "Daño acumulativo en pantalla",
                "Suma los golpes seguidos en una sola cifra.");
            _toggleDanoAcumulativo = InterruptorToggleUI.Crear(card1.transform, "ToggleDanoAcumulativo",
                AjustesVideo.DanoAcumulativo,
                (val) =>
                {
                    AjustesVideo.DanoAcumulativo = val;
                });
            ConfigurarPosicionControlEnCard(_toggleDanoAcumulativo.GetComponent<RectTransform>());
            _btnDanoAcumulativo = _toggleDanoAcumulativo.GetComponent<Button>();

            // 2. Modo de pantalla
            var card2 = CrearFilaCard(contCards.transform, "CardModoPantalla",
                "Modo de pantalla",
                "Alterna entre ventana y pantalla completa.");
            _selectorPantallaCompleta = SelectorOpcionesUI.Crear(card2.transform, "SelectorModoPantalla",
                () => AlternarModoPantalla(),
                () => AlternarModoPantalla());
            ConfigurarPosicionControlEnCard(_selectorPantallaCompleta.GetComponent<RectTransform>());

            // 3. Sincronización vertical (V-Sync)
            var card3 = CrearFilaCard(contCards.transform, "CardVSync",
                "Sincronización vertical (V-Sync)",
                "Limita los FPS a la tasa de refresco del monitor.");
            _toggleVSync = InterruptorToggleUI.Crear(card3.transform, "ToggleVSync",
                QualitySettings.vSyncCount > 0,
                (val) =>
                {
                    QualitySettings.vSyncCount = val ? 1 : 0;
                    PlayerPrefs.SetInt("VSyncSetting", QualitySettings.vSyncCount);
                    PlayerPrefs.Save();
                });
            ConfigurarPosicionControlEnCard(_toggleVSync.GetComponent<RectTransform>());
            _btnVSync = _toggleVSync.GetComponent<Button>();

            // 4. Calidad gráfica
            var card4 = CrearFilaCard(contCards.transform, "CardCalidad",
                "Calidad gráfica",
                "Sombras, suavizado y nivel de renderizado.");
            _selectorCalidad = SelectorOpcionesUI.Crear(card4.transform, "SelectorCalidad",
                () => CambiarCalidadGrafica(-1),
                () => CambiarCalidadGrafica(1));
            ConfigurarPosicionControlEnCard(_selectorCalidad.GetComponent<RectTransform>());

            // 5. Resolución de pantalla
            var card5 = CrearFilaCard(contCards.transform, "CardResolucion",
                "Resolución de pantalla",
                "Cambia las dimensiones nativas de la ventana.");
            _selectorResolucion = SelectorOpcionesUI.Crear(card5.transform, "SelectorResolucion",
                () => CambiarResolucionPantalla(-1),
                () => CambiarResolucionPantalla(1));
            ConfigurarPosicionControlEnCard(_selectorResolucion.GetComponent<RectTransform>());

            ActualizarTodosVisualesVideo();
        }

        // =========================================================================
        // PESTAÑA CONTROLES (1:1 con Controls.dc.html)
        // =========================================================================
        private void ConstruirPanelControles(Transform wellTransform)
        {
            _panelControles = new GameObject("PanelControles");
            _panelControles.transform.SetParent(wellTransform, false);
            var rtContr = _panelControles.AddComponent<RectTransform>();
            rtContr.anchorMin = Vector2.zero;
            rtContr.anchorMax = Vector2.one;
            rtContr.sizeDelta = Vector2.zero;
            rtContr.offsetMin = new Vector2(40f, 20f);
            rtContr.offsetMax = new Vector2(-40f, -6f);

            // Subtítulo superior: "Haz clic en una tecla para cambiarla. Pulsa ESC para cancelar la espera."
            var subObj = new GameObject("SubtituloControles");
            subObj.transform.SetParent(_panelControles.transform, false);
            var txtSub = subObj.AddComponent<Text>();
            txtSub.font = FuenteUtil.ObtenerFuentePixelify();
            txtSub.fontSize = 20;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = EstiloUIUX.ColTextoTenue;
            txtSub.supportRichText = true;
            txtSub.text = "Haz clic en una tecla para cambiarla. Pulsa <color=#ffcf4a>ESC</color> para cancelar la espera.";
            var rtSub = subObj.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, 0f);
            rtSub.sizeDelta = new Vector2(1000f, 28f);
            textoEstado = txtSub;

            // Grid 2 Columnas (Ancho interior 1052px, cada columna 500px, gap 44px)
            var gridObj = new GameObject("GridColumnas");
            gridObj.transform.SetParent(_panelControles.transform, false);
            var rtGrid = gridObj.AddComponent<RectTransform>();
            rtGrid.anchorMin = new Vector2(0f, 1f);
            rtGrid.anchorMax = new Vector2(1f, 1f);
            rtGrid.pivot = new Vector2(0.5f, 1f);
            rtGrid.anchoredPosition = new Vector2(0f, -32f);
            rtGrid.sizeDelta = new Vector2(0f, 440f);

            // ==================== COLUMNA 1: MOVIMIENTO ====================
            var col1 = new GameObject("ColumnaMovimiento");
            col1.transform.SetParent(gridObj.transform, false);
            var rtCol1 = col1.AddComponent<RectTransform>();
            rtCol1.anchorMin = new Vector2(0f, 1f);
            rtCol1.anchorMax = new Vector2(0f, 1f);
            rtCol1.pivot = new Vector2(0f, 1f);
            rtCol1.anchoredPosition = new Vector2(4f, 0f);
            rtCol1.sizeDelta = new Vector2(498f, 440f);

            var vlg1 = col1.AddComponent<VerticalLayoutGroup>();
            vlg1.childAlignment = TextAnchor.UpperCenter;
            vlg1.spacing = 8f;
            vlg1.childControlWidth = true;
            vlg1.childControlHeight = false;

            CrearHeaderSeccionControles(col1.transform, "MOVIMIENTO");
            CrearCardControl(col1.transform, AccionControl.MoverArriba, "Mover arriba");
            CrearCardControl(col1.transform, AccionControl.MoverAbajo, "Mover abajo");
            CrearCardControl(col1.transform, AccionControl.MoverIzquierda, "Mover izquierda");
            CrearCardControl(col1.transform, AccionControl.MoverDerecha, "Mover derecha");
            CrearCardControl(col1.transform, AccionControl.Correr, "Correr / Sprint");

            // ==================== COLUMNA 2: COMBATE + SISTEMA ====================
            var col2 = new GameObject("ColumnaCombateSistema");
            col2.transform.SetParent(gridObj.transform, false);
            var rtCol2 = col2.AddComponent<RectTransform>();
            rtCol2.anchorMin = new Vector2(1f, 1f);
            rtCol2.anchorMax = new Vector2(1f, 1f);
            rtCol2.pivot = new Vector2(1f, 1f);
            rtCol2.anchoredPosition = new Vector2(-4f, 0f);
            rtCol2.sizeDelta = new Vector2(498f, 440f);

            var vlg2 = col2.AddComponent<VerticalLayoutGroup>();
            vlg2.childAlignment = TextAnchor.UpperCenter;
            vlg2.spacing = 8f;
            vlg2.childControlWidth = true;
            vlg2.childControlHeight = false;

            CrearHeaderSeccionControles(col2.transform, "COMBATE");
            CrearCardControl(col2.transform, AccionControl.AtaquePrincipal, "Ataque básico");
            CrearCardControl(col2.transform, AccionControl.Habilidad1, "Habilidad 1");
            CrearCardControl(col2.transform, AccionControl.Habilidad2, "Habilidad 2");
            CrearCardControl(col2.transform, AccionControl.Ultimate, "Definitiva");

            CrearHeaderSeccionControles(col2.transform, "SISTEMA", 8f);
            CrearCardControl(col2.transform, AccionControl.Pausa, "Menú / Opciones");

            ActualizarTextos();
        }

        private void CrearHeaderSeccionControles(Transform parent, string titulo, float margenArriba = 0f)
        {
            var headerObj = new GameObject($"Header_{titulo}");
            headerObj.transform.SetParent(parent, false);
            var rtH = headerObj.AddComponent<RectTransform>();
            rtH.sizeDelta = new Vector2(498f, 32f + margenArriba);

            var contH = new GameObject("Contenido");
            contH.transform.SetParent(headerObj.transform, false);
            var rtContH = contH.AddComponent<RectTransform>();
            rtContH.anchorMin = new Vector2(0f, 0f);
            rtContH.anchorMax = new Vector2(1f, 1f);
            rtContH.sizeDelta = Vector2.zero;

            var hlg = contH.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.LowerLeft;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var icoObj = new GameObject("Icono").AddComponent<Image>();
            icoObj.transform.SetParent(contH.transform, false);
            icoObj.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            icoObj.raycastTarget = false;
            icoObj.rectTransform.sizeDelta = new Vector2(27f, 24f);

            var txtObj = new GameObject("Texto").AddComponent<Text>();
            txtObj.transform.SetParent(contH.transform, false);
            txtObj.font = FuenteUtil.ObtenerFuentePixelify();
            txtObj.fontSize = 26;
            txtObj.fontStyle = FontStyle.Bold;
            txtObj.color = EstiloUIUX.ColDorado;
            txtObj.text = titulo;
            txtObj.raycastTarget = false;
            txtObj.rectTransform.sizeDelta = new Vector2(300f, 28f);
        }

        private GameObject CrearCardControl(Transform parent, AccionControl accion, string nombreEtiqueta)
        {
            var cardObj = new GameObject($"Card_{accion}");
            cardObj.transform.SetParent(parent, false);
            var rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.sizeDelta = new Vector2(498f, 52f);

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
            imgCard.type = Image.Type.Sliced;

            // Texto Acción (Izquierda)
            var txtObj = new GameObject("Nombre").AddComponent<Text>();
            txtObj.transform.SetParent(cardObj.transform, false);
            txtObj.font = FuenteUtil.ObtenerFuentePixelify();
            txtObj.fontSize = 24;
            txtObj.fontStyle = FontStyle.Bold;
            txtObj.alignment = TextAnchor.MiddleLeft;
            txtObj.color = EstiloUIUX.ColTextoCrema;
            txtObj.text = nombreEtiqueta;
            txtObj.raycastTarget = false;
            var rtTxt = txtObj.rectTransform;
            rtTxt.anchorMin = new Vector2(0f, 0.5f);
            rtTxt.anchorMax = new Vector2(0f, 0.5f);
            rtTxt.pivot = new Vector2(0f, 0.5f);
            rtTxt.anchoredPosition = new Vector2(24f, 0f);
            rtTxt.sizeDelta = new Vector2(280f, 32f);

            // Botón Keycap (Derecha)
            var btnKeyObj = new GameObject("BotonKeycap");
            btnKeyObj.transform.SetParent(cardObj.transform, false);
            var rtBtnKey = btnKeyObj.AddComponent<RectTransform>();
            rtBtnKey.anchorMin = new Vector2(1f, 0.5f);
            rtBtnKey.anchorMax = new Vector2(1f, 0.5f);
            rtBtnKey.pivot = new Vector2(1f, 0.5f);
            rtBtnKey.anchoredPosition = new Vector2(-14f, 0f);
            rtBtnKey.sizeDelta = new Vector2(100f, 38f);

            var imgKey = btnKeyObj.AddComponent<Image>();
            imgKey.sprite = EstiloUIUX.ObtenerSpriteKeycap(false);
            imgKey.type = Image.Type.Sliced;

            var btnKey = btnKeyObj.AddComponent<Button>();

            var txtKeyObj = new GameObject("TextoTecla").AddComponent<Text>();
            txtKeyObj.transform.SetParent(btnKeyObj.transform, false);
            txtKeyObj.font = FuenteUtil.ObtenerFuenteSilkscreen() ?? FuenteUtil.ObtenerFuentePixelify();
            txtKeyObj.fontSize = 20;
            txtKeyObj.fontStyle = FontStyle.Bold;
            txtKeyObj.alignment = TextAnchor.MiddleCenter;
            txtKeyObj.color = new Color32(0x1B, 0x14, 0x38, 0xFF);
            txtKeyObj.raycastTarget = false;
            var rtTxtKey = txtKeyObj.rectTransform;
            rtTxtKey.anchorMin = Vector2.zero;
            rtTxtKey.anchorMax = Vector2.one;
            rtTxtKey.sizeDelta = Vector2.zero;

            AccionControl act = accion;
            Text txtRef = txtKeyObj;
            Image imgKeyRef = imgKey;
            RectTransform rtKeyRef = rtBtnKey;

            btnKey.onClick.AddListener(() => IniciarEsperaTecla(act, txtRef, imgKeyRef, rtKeyRef));

            _botonesAccion[accion] = btnKey;
            _textosAccion[accion] = txtKeyObj;

            return cardObj;
        }

        private static string FormatearNombreTecla(KeyCode key)
        {
            return key switch
            {
                KeyCode.Mouse0 => "CLIC IZQ.",
                KeyCode.Mouse1 => "CLIC DER.",
                KeyCode.Mouse2 => "CLIC CEN.",
                KeyCode.LeftShift => "SHIFT IZQ.",
                KeyCode.RightShift => "SHIFT DER.",
                KeyCode.LeftControl => "CTRL IZQ.",
                KeyCode.RightControl => "CTRL DER.",
                KeyCode.LeftAlt => "ALT IZQ.",
                KeyCode.RightAlt => "ALT DER.",
                KeyCode.Space => "ESPACIO",
                KeyCode.Return => "ENTER",
                KeyCode.KeypadEnter => "ENTER",
                KeyCode.Escape => "ESC",
                KeyCode.Tab => "TAB",
                KeyCode.Backspace => "BORRAR",
                KeyCode.Alpha0 => "0",
                KeyCode.Alpha1 => "1",
                KeyCode.Alpha2 => "2",
                KeyCode.Alpha3 => "3",
                KeyCode.Alpha4 => "4",
                KeyCode.Alpha5 => "5",
                KeyCode.Alpha6 => "6",
                KeyCode.Alpha7 => "7",
                KeyCode.Alpha8 => "8",
                KeyCode.Alpha9 => "9",
                KeyCode.UpArrow => "ARRIBA",
                KeyCode.DownArrow => "ABAJO",
                KeyCode.LeftArrow => "IZQ.",
                KeyCode.RightArrow => "DER.",
                _ => key.ToString().ToUpperInvariant()
            };
        }

        private void IniciarEsperaTecla(AccionControl accion, Text txtBoton, Image imgKey, RectTransform rtKey)
        {
            FinalizarEsperaTecla();

            _accionEsperandoTecla = accion;
            _textoBotonEsperando = txtBoton;
            _imgKeyEsperando = imgKey;
            _rtKeyEsperando = rtKey;

            if (imgKey != null) imgKey.sprite = EstiloUIUX.ObtenerSpriteKeycap(true);
            if (txtBoton != null)
            {
                txtBoton.text = "ESPERANDO_";
                txtBoton.color = EstiloUIUX.ColBtnDoradoTexto;
            }
            if (rtKey != null)
            {
                rtKey.sizeDelta = new Vector2(170f, 38f);
            }
            if (textoEstado != null)
            {
                textoEstado.text = $"Pulsa cualquier tecla para <color=#ffcf4a>{ObtenerNombreLegible(accion)}</color>. Pulsa <color=#ffcf4a>ESC</color> para cancelar.";
            }
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private void AsignarNuevaTecla(AccionControl accion, KeyCode nuevaTecla)
        {
            GestorControles.AsignarTecla(accion, nuevaTecla);
            FinalizarEsperaTecla();
            ActualizarTextos();
            GestorAudio.PlaySFX(TipoSFX.Mejora, 1.0f);
        }

        private void FinalizarEsperaTecla()
        {
            if (_imgKeyEsperando != null) _imgKeyEsperando.sprite = EstiloUIUX.ObtenerSpriteKeycap(false);
            if (_textoBotonEsperando != null) _textoBotonEsperando.color = new Color32(0x1B, 0x14, 0x38, 0xFF);
            _accionEsperandoTecla = null;
            _textoBotonEsperando = null;
            _imgKeyEsperando = null;
            _rtKeyEsperando = null;

            if (textoEstado != null)
            {
                textoEstado.text = "Haz clic en una tecla para cambiarla. Pulsa <color=#ffcf4a>ESC</color> para cancelar la espera.";
            }
            ActualizarTextos();
        }

        private void ActualizarTextos()
        {
            foreach (var kvp in _textosAccion)
            {
                KeyCode tecla = GestorControles.ObtenerTecla(kvp.Key);
                string nombreTecla = FormatearNombreTecla(tecla);
                kvp.Value.text = nombreTecla;

                var rtBtn = kvp.Value.transform.parent.GetComponent<RectTransform>();
                if (rtBtn != null)
                {
                    float ancho = Mathf.Max(80f, nombreTecla.Length * 16f + 32f);
                    rtBtn.sizeDelta = new Vector2(ancho, 38f);
                }
            }
        }

        private string ObtenerNombreLegible(AccionControl accion)
        {
            return accion switch
            {
                AccionControl.MoverArriba => "Mover arriba",
                AccionControl.MoverAbajo => "Mover abajo",
                AccionControl.MoverIzquierda => "Mover izquierda",
                AccionControl.MoverDerecha => "Mover derecha",
                AccionControl.Correr => "Correr / Sprint",
                AccionControl.AtaquePrincipal => "Ataque básico",
                AccionControl.Habilidad1 => "Habilidad 1",
                AccionControl.Habilidad2 => "Habilidad 2",
                AccionControl.Ultimate => "Definitiva",
                AccionControl.Pausa => "Menú / Opciones",
                _ => accion.ToString()
            };
        }

        private void RestaurarPorDefecto()
        {
            GestorControles.RestaurarPorDefecto();
            FinalizarEsperaTecla();
            ActualizarTextos();
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        // =========================================================================
        // CANJEAR CÓDIGOS Y CHEATS
        // =========================================================================
        private void ProcesarCodigoInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            string textoLimpio = input.Trim();
            string clave = textoLimpio.ToLowerInvariant();

            // Desbloqueo secreto de modo administrador / desarrollador
            if (clave == "admin" || clave == "modo-admin" || clave == "modoadmin" || clave == "dev" ||
                clave == "soulknightadmin" || clave == "antigravity" || clave == "secretadmin")
            {
                _adminDesbloqueado = true;
                PlayerPrefs.SetInt("AdminDesbloqueado", 1);
                PlayerPrefs.Save();
                ActualizarVisibilidadAdmin();
                AbrirModalAdmin();
                if (_txtFeedbackComando != null)
                {
                    _txtFeedbackComando.text = "🔓 ¡MODO ADMIN DESBLOQUEADO! Has habilitado el panel de trucos y desarrollo.";
                    _txtFeedbackComando.color = new Color(0.40f, 1f, 0.55f, 1f);
                }
                if (_inputCodigoAdmin != null) _inputCodigoAdmin.text = "";
                GestorAudio.PlaySFX(TipoSFX.Mejora);
                return;
            }

            // Códigos de la comunidad / eventos
            if (clave == "oro" || clave == "gold" || clave == "gatooro")
            {
                foreach (var pj in Personaje.Todos)
                {
                    if (pj != null && (pj.IsOwner || !pj.IsSpawned))
                    {
                        pj.AgregarOro(500);
                    }
                }
                if (_txtFeedbackComando != null)
                {
                    _txtFeedbackComando.text = "💰 ¡CÓDIGO CANJEADO! +500 Monedas de Oro otorgadas a tu héroe.";
                    _txtFeedbackComando.color = new Color(1f, 0.88f, 0.2f, 1f);
                }
                if (_inputCodigoAdmin != null) _inputCodigoAdmin.text = "";
                GestorAudio.PlaySFX(TipoSFX.RecogerOro);
                return;
            }

            if (clave == "soulknight" || clave == "gatoepico")
            {
                foreach (var pj in Personaje.Todos)
                {
                    if (pj != null && (pj.IsOwner || !pj.IsSpawned))
                    {
                        pj.AgregarOro(300);
                        var nv = pj.GetComponent<NivelEnPartida>();
                        if (nv != null) nv.AgregarXP(150);
                    }
                }
                if (_txtFeedbackComando != null)
                {
                    _txtFeedbackComando.text = "🎁 ¡CÓDIGO CANJEADO! +300 Oro y +150 XP de run otorgados.";
                    _txtFeedbackComando.color = new Color(1f, 0.88f, 0.2f, 1f);
                }
                if (_inputCodigoAdmin != null) _inputCodigoAdmin.text = "";
                GestorAudio.PlaySFX(TipoSFX.Mejora);
                return;
            }

            // Si el modo admin ya está activo, permite ingresar cualquier comando directamente
            if (_adminDesbloqueado)
            {
                EjecutarCheat(textoLimpio);
            }
            else
            {
                if (_txtFeedbackComando != null)
                {
                    _txtFeedbackComando.text = "❌ Código desconocido o no válido. ¡Permanece atento a eventos futuros!";
                    _txtFeedbackComando.color = new Color(1f, 0.55f, 0.55f, 1f);
                }
                GestorAudio.PlaySFX(TipoSFX.ClickUI);
            }
        }

        private void EjecutarCheat(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;
            string resultado = ComandosAdmin.EjecutarComando(codigo);
            Color col = resultado.StartsWith("✓") 
                ? new Color(0.40f, 1f, 0.55f, 1f) 
                : new Color(1f, 0.55f, 0.55f, 1f);

            if (_txtFeedbackComando != null)
            {
                _txtFeedbackComando.text = resultado;
                _txtFeedbackComando.color = col;
            }
            if (_txtFeedbackModalAdmin != null)
            {
                _txtFeedbackModalAdmin.text = resultado;
                _txtFeedbackModalAdmin.color = col;
            }
            if (_inputCodigoAdmin != null)
            {
                _inputCodigoAdmin.text = "";
            }
            if (_inputComandoModal != null)
            {
                _inputComandoModal.text = "";
            }
            GestorAudio.PlaySFX(TipoSFX.Mejora, 0.8f);
        }

        private void BloquearAdmin()
        {
            _adminDesbloqueado = false;
            PlayerPrefs.SetInt("AdminDesbloqueado", 0);
            PlayerPrefs.Save();
            CerrarModalAdmin();
            ActualizarVisibilidadAdmin();
            if (_txtFeedbackComando != null)
            {
                _txtFeedbackComando.text = "🔒 Modo Admin bloqueado. Ingresa el código secreto para volver a habilitarlo.";
                _txtFeedbackComando.color = new Color(0.70f, 0.80f, 0.95f, 1f);
            }
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private void ActualizarVisibilidadAdmin()
        {
            if (_seccionAccesoAdmin != null)
            {
                _seccionAccesoAdmin.SetActive(_adminDesbloqueado);
            }
        }

        public void AbrirModalAdmin()
        {
            if (_modalAdmin != null)
            {
                _modalAdmin.transform.SetAsLastSibling();
                _modalAdmin.SetActive(true);
                if (_txtFeedbackModalAdmin != null)
                {
                    _txtFeedbackModalAdmin.text = "Selecciona una herramienta rápida o ingresa un comando:";
                    _txtFeedbackModalAdmin.color = new Color(0.70f, 0.85f, 1f, 1f);
                }
            }
        }

        public void CerrarModalAdmin()
        {
            if (_modalAdmin != null)
            {
                _modalAdmin.SetActive(false);
            }
        }

        private void ConstruirModalAdmin(Transform parent)
        {
            if (_modalAdmin != null) return;

            _modalAdmin = new GameObject("ModalAdmin");
            _modalAdmin.transform.SetParent(parent, false);
            var rtM = _modalAdmin.AddComponent<RectTransform>();
            rtM.anchorMin = Vector2.zero;
            rtM.anchorMax = Vector2.one;
            rtM.sizeDelta = Vector2.zero;

            var imgFondoOscuro = _modalAdmin.AddComponent<Image>();
            imgFondoOscuro.color = new Color(0.01f, 0.02f, 0.04f, 0.88f);

            var panelM = new GameObject("PanelAdmin");
            panelM.transform.SetParent(_modalAdmin.transform, false);
            var rtPanM = panelM.AddComponent<RectTransform>();
            rtPanM.anchorMin = new Vector2(0.5f, 0.5f);
            rtPanM.anchorMax = new Vector2(0.5f, 0.5f);
            rtPanM.pivot = new Vector2(0.5f, 0.5f);
            rtPanM.sizeDelta = new Vector2(900f, 620f);

            var imgPanM = panelM.AddComponent<Image>();
            imgPanM.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgPanM.type = Image.Type.Sliced;

            // Título
            var titMObj = new GameObject("TituloAdmin");
            titMObj.transform.SetParent(panelM.transform, false);
            var txtTit = titMObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 16;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.85f, 0.2f, 1f);
            txtTit.text = "👑 PANEL DE CONTROL DE ADMINISTRADOR Y PRUEBAS 👑";
            AgregarOutline(txtTit);
            var rtTit = titMObj.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0.5f, 1f);
            rtTit.anchorMax = new Vector2(0.5f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0f, -14f);
            rtTit.sizeDelta = new Vector2(850f, 26f);

            // Botón Cerrar X
            var btnXObj = new GameObject("BotonCerrarX");
            btnXObj.transform.SetParent(panelM.transform, false);
            var rtX = btnXObj.AddComponent<RectTransform>();
            rtX.anchorMin = new Vector2(1f, 1f);
            rtX.anchorMax = new Vector2(1f, 1f);
            rtX.pivot = new Vector2(1f, 1f);
            rtX.anchoredPosition = new Vector2(-15f, -10f);
            rtX.sizeDelta = new Vector2(34f, 34f);
            var imgX = btnXObj.AddComponent<Image>();
            imgX.color = new Color(0.4f, 0.12f, 0.15f, 1f);
            var btnX = btnXObj.AddComponent<Button>();
            var txtX = new GameObject("Txt").AddComponent<Text>();
            txtX.transform.SetParent(btnXObj.transform, false);
            txtX.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtX.fontSize = 14;
            txtX.fontStyle = FontStyle.Bold;
            txtX.alignment = TextAnchor.MiddleCenter;
            txtX.color = Color.white;
            txtX.text = "X";
            var rtTxtX = txtX.GetComponent<RectTransform>();
            rtTxtX.anchorMin = Vector2.zero;
            rtTxtX.anchorMax = Vector2.one;
            rtTxtX.sizeDelta = Vector2.zero;
            btnX.onClick.AddListener(CerrarModalAdmin);

            // Subtítulo
            var subMObj = new GameObject("SubtituloAdmin");
            subMObj.transform.SetParent(panelM.transform, false);
            var txtSub = subMObj.AddComponent<Text>();
            txtSub.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSub.fontSize = 10;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = new Color(0.70f, 0.85f, 1f, 0.9f);
            txtSub.text = "Usa estos botones de depuración durante partidas de prueba para acelerar el desarrollo.";
            var rtSub = subMObj.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 1f);
            rtSub.anchorMax = new Vector2(0.5f, 1f);
            rtSub.pivot = new Vector2(0.5f, 1f);
            rtSub.anchoredPosition = new Vector2(0f, -44f);
            rtSub.sizeDelta = new Vector2(850f, 18f);

            // Botones de Cheats Rápidos
            var gridCheats = new GameObject("GridCheats");
            gridCheats.transform.SetParent(panelM.transform, false);
            var rtGrid = gridCheats.AddComponent<RectTransform>();
            rtGrid.anchorMin = new Vector2(0.5f, 1f);
            rtGrid.anchorMax = new Vector2(0.5f, 1f);
            rtGrid.pivot = new Vector2(0.5f, 1f);
            rtGrid.anchoredPosition = new Vector2(0f, -70f);
            rtGrid.sizeDelta = new Vector2(840f, 260f);

            // Generación de botones de acción rápida
            CrearBotonCheat(gridCheats.transform, "BtnGod", "🛡️ MODO DIOS", new Vector2(-280, -20), new Color(0.85f, 0.65f, 0.15f, 1f), () => EjecutarCheat("god"));
            CrearBotonCheat(gridCheats.transform, "BtnOro1k", "💰 +1000 ORO", new Vector2(0, -20), new Color(0.85f, 0.70f, 0.12f, 1f), () => EjecutarCheat("oro 1000"));
            CrearBotonCheat(gridCheats.transform, "BtnOro10k", "💎 +10000 ORO", new Vector2(280, -20), new Color(0.95f, 0.80f, 0.10f, 1f), () => EjecutarCheat("oro 10000"));

            CrearBotonCheat(gridCheats.transform, "BtnCurar", "❤️ CURAR TOTAL", new Vector2(-280, -70), new Color(0.20f, 0.65f, 0.35f, 1f), () => EjecutarCheat("curar"));
            CrearBotonCheat(gridCheats.transform, "BtnLvlUp", "⭐ SUBIR 1 NIVEL", new Vector2(0, -70), new Color(0.25f, 0.50f, 0.80f, 1f), () => EjecutarCheat("lvlup"));
            CrearBotonCheat(gridCheats.transform, "BtnLvlMax", "🌟 NIVEL MÁXIMO", new Vector2(280, -70), new Color(0.40f, 0.35f, 0.85f, 1f), () => EjecutarCheat("nivel 20"));

            CrearBotonCheat(gridCheats.transform, "BtnKillAll", "💀 MATAR ENEMIGOS", new Vector2(-280, -120), new Color(0.70f, 0.18f, 0.22f, 1f), () => EjecutarCheat("killall"));
            CrearBotonCheat(gridCheats.transform, "BtnNoCooldown", "⚡ SIN COOLDOWN", new Vector2(0, -120), new Color(0.18f, 0.60f, 0.80f, 1f), () => EjecutarCheat("cd"));
            CrearBotonCheat(gridCheats.transform, "BtnSpeed2x", "💨 VELOCIDAD 2X", new Vector2(280, -120), new Color(0.15f, 0.65f, 0.55f, 1f), () => EjecutarCheat("speed 2"));

            CrearBotonCheat(gridCheats.transform, "BtnSpawnBoss", "👹 SPAWN BOSS", new Vector2(-280, -170), new Color(0.55f, 0.20f, 0.65f, 1f), () => EjecutarCheat("boss"));
            CrearBotonCheat(gridCheats.transform, "BtnNextLevel", "🚪 SIGUIENTE SALA", new Vector2(0, -170), new Color(0.45f, 0.40f, 0.70f, 1f), () => EjecutarCheat("siguiente"));
            CrearBotonCheat(gridCheats.transform, "BtnBloquearAdmin", "🔒 BLOQUEAR ADMIN", new Vector2(280, -170), new Color(0.45f, 0.15f, 0.18f, 1f), BloquearAdmin);

            // Consola de entrada libre
            var titConsolaObj = new GameObject("TitConsola");
            titConsolaObj.transform.SetParent(panelM.transform, false);
            var txtSubCmd = titConsolaObj.AddComponent<Text>();
            txtSubCmd.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSubCmd.fontSize = 11;
            txtSubCmd.fontStyle = FontStyle.Bold;
            txtSubCmd.color = new Color(1f, 0.85f, 0.3f, 1f);
            txtSubCmd.text = "⌨️ CONSOLA DE ENTRADA MANUAL:";
            var rtTitC = titConsolaObj.GetComponent<RectTransform>();
            rtTitC.anchorMin = new Vector2(0.5f, 1f);
            rtTitC.anchorMax = new Vector2(0.5f, 1f);
            rtTitC.pivot = new Vector2(0.5f, 1f);
            rtTitC.anchoredPosition = new Vector2(0f, -345f);
            rtTitC.sizeDelta = new Vector2(840f, 20f);

            var descConsolaObj = new GameObject("DescConsola");
            descConsolaObj.transform.SetParent(panelM.transform, false);
            var txtDescCmd = descConsolaObj.AddComponent<Text>();
            txtDescCmd.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtDescCmd.fontSize = 9;
            txtDescCmd.color = new Color(0.65f, 0.75f, 0.85f, 0.9f);
            txtDescCmd.text = "Ejemplos: help, oro [cant], exp [cant], nivel [1-20], god, curar, speed [mult], daño [mult], killall, noclip";
            var rtDescC = descConsolaObj.GetComponent<RectTransform>();
            rtDescC.anchorMin = new Vector2(0.5f, 1f);
            rtDescC.anchorMax = new Vector2(0.5f, 1f);
            rtDescC.pivot = new Vector2(0.5f, 1f);
            rtDescC.anchoredPosition = new Vector2(0f, -366f);
            rtDescC.sizeDelta = new Vector2(840f, 18f);

            // InputField en modal
            var inputBarM = new GameObject("BarraInputModal");
            inputBarM.transform.SetParent(panelM.transform, false);
            var rtInpBarM = inputBarM.AddComponent<RectTransform>();
            rtInpBarM.anchorMin = new Vector2(0.5f, 1f);
            rtInpBarM.anchorMax = new Vector2(0.5f, 1f);
            rtInpBarM.pivot = new Vector2(0.5f, 1f);
            rtInpBarM.anchoredPosition = new Vector2(0f, -390f);
            rtInpBarM.sizeDelta = new Vector2(840f, 40f);

            var inpMObj = new GameObject("InputField");
            inpMObj.transform.SetParent(inputBarM.transform, false);
            var rtInpM = inpMObj.AddComponent<RectTransform>();
            rtInpM.anchorMin = new Vector2(0f, 0.5f);
            rtInpM.anchorMax = new Vector2(0f, 0.5f);
            rtInpM.pivot = new Vector2(0f, 0.5f);
            rtInpM.anchoredPosition = Vector2.zero;
            rtInpM.sizeDelta = new Vector2(620f, 38f);
            var imgInpM = inpMObj.AddComponent<Image>();
            imgInpM.color = new Color(0.04f, 0.06f, 0.10f, 1f);

            _inputComandoModal = inpMObj.AddComponent<InputField>();

            var phMObj = new GameObject("Placeholder");
            phMObj.transform.SetParent(inpMObj.transform, false);
            var txtPhM = phMObj.AddComponent<Text>();
            txtPhM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPhM.fontSize = 11;
            txtPhM.fontStyle = FontStyle.Italic;
            txtPhM.color = new Color(0.40f, 0.48f, 0.60f, 0.75f);
            txtPhM.text = "Escribe un comando aquí (o 'help' para ver lista completa)...";
            txtPhM.alignment = TextAnchor.MiddleLeft;
            var rtPhM = phMObj.GetComponent<RectTransform>();
            rtPhM.anchorMin = Vector2.zero;
            rtPhM.anchorMax = Vector2.one;
            rtPhM.offsetMin = new Vector2(10f, 0f);
            rtPhM.offsetMax = new Vector2(-10f, 0f);

            var textMObj = new GameObject("Text");
            textMObj.transform.SetParent(inpMObj.transform, false);
            var txtInpM = textMObj.AddComponent<Text>();
            txtInpM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtInpM.fontSize = 12;
            txtInpM.color = Color.white;
            txtInpM.alignment = TextAnchor.MiddleLeft;
            txtInpM.supportRichText = false;
            AgregarOutline(txtInpM);
            var rtTextM = textMObj.GetComponent<RectTransform>();
            rtTextM.anchorMin = Vector2.zero;
            rtTextM.anchorMax = Vector2.one;
            rtTextM.offsetMin = new Vector2(10f, 0f);
            rtTextM.offsetMax = new Vector2(-10f, 0f);

            _inputComandoModal.textComponent = txtInpM;
            _inputComandoModal.placeholder = txtPhM;
            _inputComandoModal.onEndEdit.AddListener((val) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    EjecutarCheat(val);
                }
            });

            // Botón Ejecutar en Modal
            var btnEjMObj = new GameObject("BotonEjecutarModal");
            btnEjMObj.transform.SetParent(inputBarM.transform, false);
            var rtBtnEjM = btnEjMObj.AddComponent<RectTransform>();
            rtBtnEjM.anchorMin = new Vector2(1f, 0.5f);
            rtBtnEjM.anchorMax = new Vector2(1f, 0.5f);
            rtBtnEjM.pivot = new Vector2(1f, 0.5f);
            rtBtnEjM.anchoredPosition = Vector2.zero;
            rtBtnEjM.sizeDelta = new Vector2(210f, 38f);
            var imgBtnEjM = btnEjMObj.AddComponent<Image>();
            imgBtnEjM.color = new Color(0.12f, 0.55f, 0.32f, 1f);
            var btnEjM = btnEjMObj.AddComponent<Button>();

            var txtBtnEjMObj = new GameObject("Texto");
            txtBtnEjMObj.transform.SetParent(btnEjMObj.transform, false);
            var txtBtnEjM = txtBtnEjMObj.AddComponent<Text>();
            txtBtnEjM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtBtnEjM.fontSize = 11;
            txtBtnEjM.fontStyle = FontStyle.Bold;
            txtBtnEjM.alignment = TextAnchor.MiddleCenter;
            txtBtnEjM.color = Color.white;
            txtBtnEjM.text = "⚡ EJECUTAR";
            AgregarOutline(txtBtnEjM);
            var rtTxtEjM = txtBtnEjMObj.GetComponent<RectTransform>();
            rtTxtEjM.anchorMin = Vector2.zero;
            rtTxtEjM.anchorMax = Vector2.one;
            rtTxtEjM.sizeDelta = Vector2.zero;
            btnEjM.onClick.AddListener(() =>
            {
                if (_inputComandoModal != null)
                {
                    EjecutarCheat(_inputComandoModal.text);
                }
            });

            // Feedback en Modal
            var fbMObj = new GameObject("FeedbackModalAdmin");
            fbMObj.transform.SetParent(panelM.transform, false);
            _txtFeedbackModalAdmin = fbMObj.AddComponent<Text>();
            _txtFeedbackModalAdmin.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtFeedbackModalAdmin.fontSize = 11;
            _txtFeedbackModalAdmin.alignment = TextAnchor.MiddleCenter;
            _txtFeedbackModalAdmin.color = new Color(0.45f, 0.85f, 1f, 1f);
            _txtFeedbackModalAdmin.text = "Selecciona una herramienta rápida o ingresa un comando:";
            AgregarOutline(_txtFeedbackModalAdmin);
            var rtFbM = fbMObj.GetComponent<RectTransform>();
            rtFbM.anchorMin = new Vector2(0.5f, 1f);
            rtFbM.anchorMax = new Vector2(0.5f, 1f);
            rtFbM.pivot = new Vector2(0.5f, 1f);
            rtFbM.anchoredPosition = new Vector2(0f, -440f);
            rtFbM.sizeDelta = new Vector2(840f, 32f);

            // Botón Cerrar Modal Abajo
            var btnCerrarMObj = new GameObject("BotonCerrarModalAbajo");
            btnCerrarMObj.transform.SetParent(panelM.transform, false);
            var rtCM = btnCerrarMObj.AddComponent<RectTransform>();
            rtCM.anchorMin = new Vector2(0.5f, 0f);
            rtCM.anchorMax = new Vector2(0.5f, 0f);
            rtCM.pivot = new Vector2(0.5f, 0f);
            rtCM.anchoredPosition = new Vector2(0f, 25f);
            rtCM.sizeDelta = new Vector2(260f, 40f);
            var imgCM = btnCerrarMObj.AddComponent<Image>();
            imgCM.color = new Color(0.20f, 0.25f, 0.35f, 1f);
            var btnCM = btnCerrarMObj.AddComponent<Button>();
            var txtCMObj = new GameObject("Texto");
            txtCMObj.transform.SetParent(btnCerrarMObj.transform, false);
            var txtCerrarM = txtCMObj.AddComponent<Text>();
            txtCerrarM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtCerrarM.fontSize = 12;
            txtCerrarM.fontStyle = FontStyle.Bold;
            txtCerrarM.alignment = TextAnchor.MiddleCenter;
            txtCerrarM.color = Color.white;
            txtCerrarM.text = "CERRAR PANEL";
            AgregarOutline(txtCerrarM);
            var rtTxtCM = txtCMObj.GetComponent<RectTransform>();
            rtTxtCM.anchorMin = Vector2.zero;
            rtTxtCM.anchorMax = Vector2.one;
            rtTxtCM.sizeDelta = Vector2.zero;
            btnCM.onClick.AddListener(CerrarModalAdmin);

            _modalAdmin.SetActive(false);
        }

        private static void CrearBotonCheat(Transform parent, string nombre, string texto, Vector2 pos, Color colorBase, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 1f);
            rtBtn.anchorMax = new Vector2(0.5f, 1f);
            rtBtn.pivot = new Vector2(0.5f, 1f);
            rtBtn.anchoredPosition = pos;
            rtBtn.sizeDelta = new Vector2(260f, 40f);

            var img = btnObj.AddComponent<Image>();
            img.color = colorBase;

            var btn = btnObj.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = texto;
            AgregarOutline(txt);

            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;
        }

        private static void CrearRemache(Transform parent, string nombre, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos)
        {
            var rem = new GameObject(nombre);
            rem.transform.SetParent(parent, false);
            var rt = rem.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(8f, 8f);

            var img = rem.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteRemache();
            img.raycastTarget = false;
        }

        private static void CrearIconoCabecera(Transform parent, string nombre)
        {
            var ico = new GameObject(nombre);
            ico.transform.SetParent(parent, false);
            var rt = ico.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(45f, 40f);
            var img = ico.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            img.raycastTarget = false;
        }

        public static void ConfigurarBotonPixel(Button btn, Action onClick)
        {
            if (btn == null) return;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.10f, 1.10f, 1.10f, 1f);
            cb.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            if (onClick != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI, 1.0f);
                    onClick.Invoke();
                });
            }
        }

        private static Button CrearBotonTab(Transform parent, string nombre, string texto, string nombreIcono, Vector2 iconoSize, Vector2 pos, float ancho, out Image img, out Text txt)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
            rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
            rtBtn.pivot = new Vector2(0.5f, 0.5f);
            rtBtn.anchoredPosition = pos;
            rtBtn.sizeDelta = new Vector2(ancho, 68f);

            img = btnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Piedra);

            var btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            var contH = new GameObject("Contenido");
            contH.transform.SetParent(btnObj.transform, false);
            var rtContH = contH.AddComponent<RectTransform>();
            rtContH.anchorMin = Vector2.zero;
            rtContH.anchorMax = Vector2.one;
            rtContH.sizeDelta = Vector2.zero;

            var hlg = contH.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var icoObj = new GameObject("Icono");
            icoObj.transform.SetParent(contH.transform, false);
            var imgIco = icoObj.AddComponent<Image>();
            imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, EstiloUIUX.ColBtnPiedraTexto);
            imgIco.raycastTarget = false;
            var rtIco = icoObj.GetComponent<RectTransform>();
            rtIco.sizeDelta = iconoSize;

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(contH.transform, false);
            txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = EstiloUIUX.ColBtnPiedraTexto;
            txt.text = texto;
            txt.raycastTarget = false;
            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.sizeDelta = new Vector2(170f, 36f);

            return btn;
        }

        public void CambiarPestana(PestanaOpciones pestana)
        {
            _pestanaActual = pestana;

            if (_panelAudio != null) _panelAudio.SetActive(pestana == PestanaOpciones.Audio);
            if (_panelVideo != null)
            {
                _panelVideo.SetActive(pestana == PestanaOpciones.Video);
                if (pestana == PestanaOpciones.Video) ActualizarTodosVisualesVideo();
            }
            if (_panelControles != null)
            {
                _panelControles.SetActive(pestana == PestanaOpciones.Controles);
                if (pestana == PestanaOpciones.Controles) ActualizarTextos();
            }

            // El botón Restaurar solo se muestra en Controles
            if (botonRestaurar != null)
            {
                botonRestaurar.gameObject.SetActive(pestana == PestanaOpciones.Controles);
                var rtR = botonRestaurar.GetComponent<RectTransform>();
                if (rtR != null)
                {
                    rtR.anchorMin = new Vector2(0f, 0.5f);
                    rtR.anchorMax = new Vector2(0f, 0.5f);
                    rtR.pivot = new Vector2(0f, 0.5f);
                    rtR.anchoredPosition = Vector2.zero;
                    rtR.sizeDelta = new Vector2(440f, 72f);
                }
            }

            // El botón Cerrar se adapta: derecha en Controles, centrado en Audio y Video
            if (botonCerrar != null)
            {
                var rtC = botonCerrar.GetComponent<RectTransform>();
                if (rtC != null)
                {
                    if (pestana == PestanaOpciones.Controles)
                    {
                        rtC.anchorMin = new Vector2(1f, 0.5f);
                        rtC.anchorMax = new Vector2(1f, 0.5f);
                        rtC.pivot = new Vector2(1f, 0.5f);
                        rtC.anchoredPosition = Vector2.zero;
                        rtC.sizeDelta = new Vector2(440f, 72f);
                    }
                    else
                    {
                        rtC.anchorMin = new Vector2(0.5f, 0.5f);
                        rtC.anchorMax = new Vector2(0.5f, 0.5f);
                        rtC.pivot = new Vector2(0.5f, 0.5f);
                        rtC.anchoredPosition = Vector2.zero;
                        rtC.sizeDelta = new Vector2(460f, 72f);
                    }
                }
            }

            ActualizarEstiloBotonTab(_imgTabAudio, _txtTabAudio, pestana == PestanaOpciones.Audio, "audio");
            ActualizarEstiloBotonTab(_imgTabVideo, _txtTabVideo, pestana == PestanaOpciones.Video, "monitor");
            ActualizarEstiloBotonTab(_imgTabControles, _txtTabControles, pestana == PestanaOpciones.Controles, "controles");
        }

        private static void ActualizarEstiloBotonTab(Image imgTab, Text txtTab, bool activa, string nombreIcono)
        {
            if (imgTab == null) return;
            imgTab.sprite = EstiloUIUX.ObtenerSpriteBoton(activa ? TipoBotonUI.Dorado : TipoBotonUI.Piedra);
            imgTab.color = Color.white;
            Color colorContenido = activa ? EstiloUIUX.ColBtnDoradoTexto : EstiloUIUX.ColBtnPiedraTexto;
            if (txtTab != null) txtTab.color = colorContenido;
            var ico = imgTab.transform.Find("Contenido/Icono") ?? imgTab.transform.Find("Icono");
            if (ico != null)
            {
                var imgIco = ico.GetComponent<Image>();
                if (imgIco != null)
                {
                    imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, colorContenido);
                }
            }
        }

        void Start()
        {
            AsegurarInterfazCompleta();
        }

        void Update()
        {
            if (_accionEsperandoTecla.HasValue)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    AsignarNuevaTecla(_accionEsperandoTecla.Value, KeyCode.Mouse0);
                    return;
                }
                if (Input.GetMouseButtonDown(1))
                {
                    AsignarNuevaTecla(_accionEsperandoTecla.Value, KeyCode.Mouse1);
                    return;
                }
                if (Input.GetMouseButtonDown(2))
                {
                    AsignarNuevaTecla(_accionEsperandoTecla.Value, KeyCode.Mouse2);
                    return;
                }

                foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc == KeyCode.Escape)
                        {
                            FinalizarEsperaTecla();
                        }
                        else
                        {
                            AsignarNuevaTecla(_accionEsperandoTecla.Value, kc);
                        }
                        break;
                    }
                }
            }
            else if (_modalAdmin != null && _modalAdmin.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CerrarModalAdmin();
            }
            else if (panelContenedor != null && panelContenedor.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
            }
        }

        public void Abrir(PestanaOpciones pestanaInicial = PestanaOpciones.Audio)
        {
            AsegurarInterfazCompleta();
            if (panelContenedor != null)
            {
                panelContenedor.SetActive(true);
                panelContenedor.transform.SetAsLastSibling();
            }
            CambiarPestana(pestanaInicial);

            if (_sliderMaster != null && GestorAudio.Instancia != null) _sliderMaster.value = GestorAudio.Instancia.VolumenMaster;
            if (_sliderMusica != null && GestorAudio.Instancia != null) _sliderMusica.value = GestorAudio.Instancia.VolumenMusica;
            if (_sliderSFX != null && GestorAudio.Instancia != null) _sliderSFX.value = GestorAudio.Instancia.VolumenSFX;

            ActualizarVisibilidadAdmin();
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        public void Cerrar()
        {
            FinalizarEsperaTecla();
            if (_modalAdmin != null) _modalAdmin.SetActive(false);
            if (panelContenedor != null) panelContenedor.SetActive(false);
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private GameObject CrearFilaCard(Transform parent, string nombre, string titulo, string descripcion)
        {
            var cardObj = new GameObject(nombre);
            cardObj.transform.SetParent(parent, false);
            var rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.sizeDelta = new Vector2(1040f, 88f);

            var imgCard = cardObj.AddComponent<Image>();
            imgCard.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
            imgCard.type = Image.Type.Sliced;

            // Textos Izquierda
            var txtCont = new GameObject("ContenedorTextos");
            txtCont.transform.SetParent(cardObj.transform, false);
            var rtTxtCont = txtCont.AddComponent<RectTransform>();
            rtTxtCont.anchorMin = new Vector2(0f, 0.5f);
            rtTxtCont.anchorMax = new Vector2(0f, 0.5f);
            rtTxtCont.pivot = new Vector2(0f, 0.5f);
            rtTxtCont.anchoredPosition = new Vector2(32f, 0f);
            rtTxtCont.sizeDelta = new Vector2(540f, 64f);

            var vlgTxt = txtCont.AddComponent<VerticalLayoutGroup>();
            vlgTxt.childAlignment = TextAnchor.MiddleLeft;
            vlgTxt.spacing = 6f;
            vlgTxt.childControlWidth = true;
            vlgTxt.childControlHeight = false;

            // Etiqueta 28px
            var titObj = new GameObject("Etiqueta");
            titObj.transform.SetParent(txtCont.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelify();
            txtTit.fontSize = 28;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.color = EstiloUIUX.ColTextoCrema;
            txtTit.text = titulo;
            txtTit.lineSpacing = 1.1f;
            txtTit.raycastTarget = false;
            titObj.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 32f);

            // Subtítulo 20px
            var descObj = new GameObject("Descripcion");
            descObj.transform.SetParent(txtCont.transform, false);
            var txtDesc = descObj.AddComponent<Text>();
            txtDesc.font = FuenteUtil.ObtenerFuentePixelify();
            txtDesc.fontSize = 20;
            txtDesc.color = EstiloUIUX.ColTextoTenue;
            txtDesc.text = descripcion;
            txtDesc.lineSpacing = 1.35f;
            txtDesc.raycastTarget = false;
            descObj.GetComponent<RectTransform>().sizeDelta = new Vector2(540f, 26f);

            return cardObj;
        }

        private static void ConfigurarPosicionControlEnCard(RectTransform rtControl)
        {
            if (rtControl == null) return;
            rtControl.anchorMin = new Vector2(1f, 0.5f);
            rtControl.anchorMax = new Vector2(1f, 0.5f);
            rtControl.pivot = new Vector2(1f, 0.5f);
            rtControl.anchoredPosition = new Vector2(-28f, 0f);
        }

        private void AlternarModoPantalla()
        {
            bool nuevaPantallaCompleta = !Screen.fullScreen;
            Screen.fullScreen = nuevaPantallaCompleta;
            PlayerPrefs.SetInt("PantallaCompleta", nuevaPantallaCompleta ? 1 : 0);
            PlayerPrefs.Save();
            ActualizarVisualPantallaCompleta(nuevaPantallaCompleta);
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private void CambiarCalidadGrafica(int direccion)
        {
            int actual = QualitySettings.GetQualityLevel();
            int nuevo = Mathf.Clamp(actual + direccion, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(nuevo, true);
            PlayerPrefs.SetInt("CalidadGrafica", nuevo);
            PlayerPrefs.Save();
            ActualizarVisualCalidad();
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private void CambiarResolucionPantalla(int direccion)
        {
            SincronizarResolucionActual();
            int count = _resolucionesDisponibles.Length;
            _indiceResolucion = (_indiceResolucion + direccion + count) % count;

            var r = _resolucionesDisponibles[_indiceResolucion];
            Screen.SetResolution(r.width, r.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolucionAncho", r.width);
            PlayerPrefs.SetInt("ResolucionAlto", r.height);
            PlayerPrefs.Save();

            ActualizarVisualResolucion(r.width, r.height);
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        private void SincronizarResolucionActual()
        {
            int ancho = PlayerPrefs.GetInt("ResolucionAncho", Screen.width);
            int alto = PlayerPrefs.GetInt("ResolucionAlto", Screen.height);

            for (int i = 0; i < _resolucionesDisponibles.Length; i++)
            {
                if (_resolucionesDisponibles[i].width == ancho && _resolucionesDisponibles[i].height == alto)
                {
                    _indiceResolucion = i;
                    return;
                }
            }
            _indiceResolucion = 0;
        }

        private void ActualizarTodosVisualesVideo()
        {
            SincronizarResolucionActual();
            ActualizarVisualDanoAcumulativo();
            ActualizarVisualPantallaCompleta();
            ActualizarVisualVSync();
            ActualizarVisualCalidad();
            ActualizarVisualResolucion();
        }

        private void ActualizarVisualDanoAcumulativo()
        {
            if (_toggleDanoAcumulativo != null)
            {
                _toggleDanoAcumulativo.SetValor(AjustesVideo.DanoAcumulativo, false);
            }
        }

        private void ActualizarVisualPantallaCompleta(bool? esCompleta = null)
        {
            if (_selectorPantallaCompleta != null)
            {
                bool completa = esCompleta ?? Screen.fullScreen;
                string texto = completa ? "PANTALLA COMPLETA" : "MODO VENTANA";
                _selectorPantallaCompleta.EstablecerValor(texto, EstiloUIUX.ColCyan);
            }
        }

        private void ActualizarVisualVSync()
        {
            if (_toggleVSync != null)
            {
                _toggleVSync.SetValor(QualitySettings.vSyncCount > 0, false);
            }
        }

        private void ActualizarVisualCalidad()
        {
            if (_selectorCalidad != null)
            {
                string nombreCalidad = QualitySettings.names[QualitySettings.GetQualityLevel()].ToUpperInvariant();
                _selectorCalidad.EstablecerValor(nombreCalidad, EstiloUIUX.ColDorado, EstiloUIUX.ObtenerSpriteIcono("estrella", EstiloUIUX.ColDorado));
            }
        }

        private void ActualizarVisualResolucion(int? ancho = null, int? alto = null)
        {
            if (_selectorResolucion != null)
            {
                int w = ancho ?? (_resolucionesDisponibles != null && _resolucionesDisponibles.Length > 0 ? _resolucionesDisponibles[_indiceResolucion].width : Screen.width);
                int h = alto ?? (_resolucionesDisponibles != null && _resolucionesDisponibles.Length > 0 ? _resolucionesDisponibles[_indiceResolucion].height : Screen.height);
                _selectorResolucion.EstablecerValor($"{w} x {h}", EstiloUIUX.ColTextoCrema);
            }
        }
    }
}
