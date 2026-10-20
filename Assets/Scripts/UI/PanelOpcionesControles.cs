using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    /// - Pestaña 1: Audio (Sliders Master, Música, SFX) + Consola de Códigos de Admin/Dev para testeo rápido.
    /// - Pestaña 2: Controles (Reasignación dinámica de teclas con nombres legibles).
    /// - Diseño ampliado, moderno, márgenes limpios y botones cómodos para Sala, Lobby y Run.
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

        // Opciones de Video
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

        private AccionControl? _accionEsperandoTecla = null;
        private Text _textoBotonEsperando = null;

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

            // Asegurar Canvas propio de alta prioridad para nunca quedar oculto detrás de otros elementos del HUD
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
            imgFondo.color = new Color(0.03f, 0.04f, 0.07f, 0.95f); // Fondo translúcido oscuro

            // ==================== VENTANA PRINCIPAL AMPLIADA Y ESPACIOSA ====================
            var ventanaObj = new GameObject("VentanaModal");
            ventanaObj.transform.SetParent(transform, false);
            var rtVentana = ventanaObj.AddComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.sizeDelta = new Vector2(920f, 680f); // Tamaño generoso y con amplios márgenes

            var imgVentana = ventanaObj.AddComponent<Image>();
            imgVentana.color = new Color(0.08f, 0.10f, 0.16f, 0.99f);

            // Borde superior / Cabecera
            var barraTop = new GameObject("BarraSuperior");
            barraTop.transform.SetParent(ventanaObj.transform, false);
            var rtBarra = barraTop.AddComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 1f);
            rtBarra.anchorMax = new Vector2(1f, 1f);
            rtBarra.pivot = new Vector2(0.5f, 1f);
            rtBarra.sizeDelta = new Vector2(0f, 48f);
            var imgBarra = barraTop.AddComponent<Image>();
            imgBarra.color = new Color(0.13f, 0.17f, 0.27f, 1f);

            // Título con Trazo (Outline)
            var titObj = new GameObject("Titulo");
            titObj.transform.SetParent(barraTop.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 17;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTit.text = "⚙️ MENÚ DE OPCIONES & CONFIGURACIÓN";
            AgregarOutline(txtTit, new Color(0.01f, 0.01f, 0.03f, 0.98f), new Vector2(1.8f, -1.8f));
            var rtTit = titObj.GetComponent<RectTransform>();
            rtTit.anchorMin = Vector2.zero;
            rtTit.anchorMax = Vector2.one;
            rtTit.sizeDelta = Vector2.zero;

            // Barra de Pestañas
            var barraTabs = new GameObject("BarraPestanas");
            barraTabs.transform.SetParent(ventanaObj.transform, false);
            var rtTabs = barraTabs.AddComponent<RectTransform>();
            rtTabs.anchorMin = new Vector2(0.5f, 1f);
            rtTabs.anchorMax = new Vector2(0.5f, 1f);
            rtTabs.pivot = new Vector2(0.5f, 1f);
            rtTabs.anchoredPosition = new Vector2(0f, -56f);
            rtTabs.sizeDelta = new Vector2(850f, 44f);

            _btnTabAudio = CrearBotonTab(barraTabs.transform, "TabAudio", "🔊 AUDIO", new Vector2(-280f, 0f), 270f, out _imgTabAudio, out _txtTabAudio);
            _btnTabVideo = CrearBotonTab(barraTabs.transform, "TabVideo", "🖥️ VIDEO", new Vector2(0f, 0f), 270f, out _imgTabVideo, out _txtTabVideo);
            _btnTabControles = CrearBotonTab(barraTabs.transform, "TabControles", "🎮 CONTROLES", new Vector2(280f, 0f), 270f, out _imgTabControles, out _txtTabControles);

            // ==================== PANEL AUDIO Y COMANDOS / CÓDIGOS ====================
            _panelAudio = new GameObject("PanelAudio");
            _panelAudio.transform.SetParent(ventanaObj.transform, false);
            var rtAudio = _panelAudio.AddComponent<RectTransform>();
            rtAudio.anchorMin = new Vector2(0.5f, 0.5f);
            rtAudio.anchorMax = new Vector2(0.5f, 0.5f);
            rtAudio.pivot = new Vector2(0.5f, 0.5f);
            rtAudio.anchoredPosition = new Vector2(0f, -12f);
            rtAudio.sizeDelta = new Vector2(850f, 500f);

            // Subtítulo Audio
            var subAudio = new GameObject("SubtituloAudio");
            subAudio.transform.SetParent(_panelAudio.transform, false);
            var txtSubAudio = subAudio.AddComponent<Text>();
            txtSubAudio.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSubAudio.fontSize = 12;
            txtSubAudio.fontStyle = FontStyle.Bold;
            txtSubAudio.alignment = TextAnchor.MiddleLeft;
            txtSubAudio.color = new Color(0.40f, 0.85f, 1f, 1f);
            txtSubAudio.text = "🔊 NIVELES DE VOLUMEN (SE GUARDAN AUTOMÁTICAMENTE)";
            AgregarOutline(txtSubAudio);
            var rtSubAudio = subAudio.GetComponent<RectTransform>();
            rtSubAudio.anchorMin = new Vector2(0f, 1f);
            rtSubAudio.anchorMax = new Vector2(1f, 1f);
            rtSubAudio.pivot = new Vector2(0f, 1f);
            rtSubAudio.anchoredPosition = new Vector2(15f, -6f);
            rtSubAudio.sizeDelta = new Vector2(0f, 22f);

            // Contenedor Sliders con margen generoso
            var contSliders = new GameObject("ContenedorSliders");
            contSliders.transform.SetParent(_panelAudio.transform, false);
            var rtContSliders = contSliders.AddComponent<RectTransform>();
            rtContSliders.anchorMin = new Vector2(0.5f, 1f);
            rtContSliders.anchorMax = new Vector2(0.5f, 1f);
            rtContSliders.pivot = new Vector2(0.5f, 1f);
            rtContSliders.anchoredPosition = new Vector2(0f, -32f);
            rtContSliders.sizeDelta = new Vector2(820f, 120f);

            var vlgSliders = contSliders.AddComponent<VerticalLayoutGroup>();
            vlgSliders.childAlignment = TextAnchor.UpperCenter;
            vlgSliders.spacing = 8f;
            vlgSliders.childControlWidth = false;
            vlgSliders.childControlHeight = false;

            _sliderMaster = CrearSliderFila(contSliders.transform, "SliderMaster", "Volumen General (Master)", GestorAudio.Instancia.VolumenMaster,
                (val) => GestorAudio.Instancia.SetVolumenMaster(val), out _txtPctMaster);

            _sliderMusica = CrearSliderFila(contSliders.transform, "SliderMusica", "Música de Fondo (BGM)", GestorAudio.Instancia.VolumenMusica,
                (val) => GestorAudio.Instancia.SetVolumenMusica(val), out _txtPctMusica);

            _sliderSFX = CrearSliderFila(contSliders.transform, "SliderSFX", "Efectos de Sonido (SFX)", GestorAudio.Instancia.VolumenSFX,
                (val) => GestorAudio.Instancia.SetVolumenSFX(val), out _txtPctSFX);

            // Botón Probar Sonido
            var btnProbarObj = new GameObject("BotonProbarSonido");
            btnProbarObj.transform.SetParent(_panelAudio.transform, false);
            var imgProbar = btnProbarObj.AddComponent<Image>();
            imgProbar.color = new Color(0.18f, 0.35f, 0.55f, 1f);
            var rtProbar = btnProbarObj.GetComponent<RectTransform>();
            rtProbar.anchorMin = new Vector2(1f, 1f);
            rtProbar.anchorMax = new Vector2(1f, 1f);
            rtProbar.pivot = new Vector2(1f, 1f);
            rtProbar.anchoredPosition = new Vector2(-15f, -156f);
            rtProbar.sizeDelta = new Vector2(230f, 32f);
            var btnProbar = btnProbarObj.AddComponent<Button>();

            var txtProbarObj = new GameObject("Texto");
            txtProbarObj.transform.SetParent(btnProbarObj.transform, false);
            var txtProbar = txtProbarObj.AddComponent<Text>();
            txtProbar.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtProbar.fontSize = 11;
            txtProbar.fontStyle = FontStyle.Bold;
            txtProbar.alignment = TextAnchor.MiddleCenter;
            txtProbar.color = Color.white;
            txtProbar.text = "🎯 PROBAR EFECTO (SFX)";
            AgregarOutline(txtProbar);
            var rtTxtProbar = txtProbarObj.GetComponent<RectTransform>();
            rtTxtProbar.anchorMin = Vector2.zero;
            rtTxtProbar.anchorMax = Vector2.one;
            rtTxtProbar.sizeDelta = Vector2.zero;
            btnProbar.onClick.AddListener(() => GestorAudio.PlaySFX(TipoSFX.Mejora, 1.0f));

            // Divisor Visual
            var divisorObj = new GameObject("Divisor");
            divisorObj.transform.SetParent(_panelAudio.transform, false);
            var rtDiv = divisorObj.AddComponent<RectTransform>();
            rtDiv.anchorMin = new Vector2(0.5f, 1f);
            rtDiv.anchorMax = new Vector2(0.5f, 1f);
            rtDiv.pivot = new Vector2(0.5f, 1f);
            rtDiv.anchoredPosition = new Vector2(0f, -196f);
            rtDiv.sizeDelta = new Vector2(820f, 2f);
            var imgDiv = divisorObj.AddComponent<Image>();
            imgDiv.color = new Color(0.20f, 0.26f, 0.38f, 0.8f);

            // ==================== SECCIÓN CANJEAR CÓDIGO (SIEMPRE VISIBLE) ====================
            var titCanje = new GameObject("TituloCanje");
            titCanje.transform.SetParent(_panelAudio.transform, false);
            var txtTitCanje = titCanje.AddComponent<Text>();
            txtTitCanje.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTitCanje.fontSize = 12;
            txtTitCanje.fontStyle = FontStyle.Bold;
            txtTitCanje.alignment = TextAnchor.MiddleLeft;
            txtTitCanje.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTitCanje.text = "🎟️ CANJEAR CÓDIGO (EVENTOS / COMUNIDAD)";
            AgregarOutline(txtTitCanje);
            var rtTitCanje = titCanje.GetComponent<RectTransform>();
            rtTitCanje.anchorMin = new Vector2(0f, 1f);
            rtTitCanje.anchorMax = new Vector2(1f, 1f);
            rtTitCanje.pivot = new Vector2(0f, 1f);
            rtTitCanje.anchoredPosition = new Vector2(15f, -206f);
            rtTitCanje.sizeDelta = new Vector2(0f, 22f);

            var descCanje = new GameObject("DescCanje");
            descCanje.transform.SetParent(_panelAudio.transform, false);
            var txtDescCanje = descCanje.AddComponent<Text>();
            txtDescCanje.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtDescCanje.fontSize = 10;
            txtDescCanje.alignment = TextAnchor.MiddleLeft;
            txtDescCanje.color = new Color(0.70f, 0.78f, 0.90f, 1f);
            txtDescCanje.text = "Introduce un código promocional de evento o clave para canjear recompensas:";
            AgregarOutline(txtDescCanje);
            var rtDescCanje = descCanje.GetComponent<RectTransform>();
            rtDescCanje.anchorMin = new Vector2(0f, 1f);
            rtDescCanje.anchorMax = new Vector2(1f, 1f);
            rtDescCanje.pivot = new Vector2(0f, 1f);
            rtDescCanje.anchoredPosition = new Vector2(15f, -228f);
            rtDescCanje.sizeDelta = new Vector2(0f, 18f);

            // Barra de Input de Texto y Botón Canjear
            var inputBarObj = new GameObject("BarraInputCodigo");
            inputBarObj.transform.SetParent(_panelAudio.transform, false);
            var rtInputBar = inputBarObj.AddComponent<RectTransform>();
            rtInputBar.anchorMin = new Vector2(0.5f, 1f);
            rtInputBar.anchorMax = new Vector2(0.5f, 1f);
            rtInputBar.pivot = new Vector2(0.5f, 1f);
            rtInputBar.anchoredPosition = new Vector2(0f, -252f);
            rtInputBar.sizeDelta = new Vector2(820f, 38f);

            // InputField
            var inputObj = new GameObject("InputFieldCodigo");
            inputObj.transform.SetParent(inputBarObj.transform, false);
            var rtInput = inputObj.AddComponent<RectTransform>();
            rtInput.anchorMin = new Vector2(0f, 0.5f);
            rtInput.anchorMax = new Vector2(0f, 0.5f);
            rtInput.pivot = new Vector2(0f, 0.5f);
            rtInput.anchoredPosition = new Vector2(0f, 0f);
            rtInput.sizeDelta = new Vector2(600f, 36f);

            var imgInput = inputObj.AddComponent<Image>();
            imgInput.color = new Color(0.05f, 0.07f, 0.11f, 1f);

            _inputCodigoAdmin = inputObj.AddComponent<InputField>();

            // Placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var txtPlaceholder = placeholderObj.AddComponent<Text>();
            txtPlaceholder.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPlaceholder.fontSize = 11;
            txtPlaceholder.fontStyle = FontStyle.Italic;
            txtPlaceholder.color = new Color(0.48f, 0.55f, 0.65f, 0.75f);
            txtPlaceholder.text = "Introduce un código promocional o clave aquí...";
            txtPlaceholder.alignment = TextAnchor.MiddleLeft;
            var rtPlace = placeholderObj.GetComponent<RectTransform>();
            rtPlace.anchorMin = Vector2.zero;
            rtPlace.anchorMax = Vector2.one;
            rtPlace.offsetMin = new Vector2(10f, 0f);
            rtPlace.offsetMax = new Vector2(-10f, 0f);

            // Texto Input
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var txtInput = textObj.AddComponent<Text>();
            txtInput.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtInput.fontSize = 12;
            txtInput.color = Color.white;
            txtInput.alignment = TextAnchor.MiddleLeft;
            txtInput.supportRichText = false;
            AgregarOutline(txtInput);
            var rtText = textObj.GetComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = new Vector2(10f, 0f);
            rtText.offsetMax = new Vector2(-10f, 0f);

            _inputCodigoAdmin.textComponent = txtInput;
            _inputCodigoAdmin.placeholder = txtPlaceholder;
            _inputCodigoAdmin.onEndEdit.AddListener((val) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ProcesarCodigoInput(val);
                }
            });

            // Botón Canjear
            var btnEjecutarObj = new GameObject("BotonEjecutar");
            btnEjecutarObj.transform.SetParent(inputBarObj.transform, false);
            var rtBtnEj = btnEjecutarObj.AddComponent<RectTransform>();
            rtBtnEj.anchorMin = new Vector2(1f, 0.5f);
            rtBtnEj.anchorMax = new Vector2(1f, 0.5f);
            rtBtnEj.pivot = new Vector2(1f, 0.5f);
            rtBtnEj.anchoredPosition = new Vector2(0f, 0f);
            rtBtnEj.sizeDelta = new Vector2(210f, 36f);

            var imgEj = btnEjecutarObj.AddComponent<Image>();
            imgEj.color = new Color(0.12f, 0.55f, 0.32f, 1f);
            var btnEj = btnEjecutarObj.AddComponent<Button>();

            var txtBtnEjObj = new GameObject("Texto");
            txtBtnEjObj.transform.SetParent(btnEjecutarObj.transform, false);
            var txtBtnEj = txtBtnEjObj.AddComponent<Text>();
            txtBtnEj.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtBtnEj.fontSize = 11;
            txtBtnEj.fontStyle = FontStyle.Bold;
            txtBtnEj.alignment = TextAnchor.MiddleCenter;
            txtBtnEj.color = Color.white;
            txtBtnEj.text = "⚡ CANJEAR CÓDIGO";
            AgregarOutline(txtBtnEj);
            var rtTxtBtnEj = txtBtnEjObj.GetComponent<RectTransform>();
            rtTxtBtnEj.anchorMin = Vector2.zero;
            rtTxtBtnEj.anchorMax = Vector2.one;
            rtTxtBtnEj.sizeDelta = Vector2.zero;

            btnEj.onClick.AddListener(() =>
            {
                if (_inputCodigoAdmin != null)
                {
                    ProcesarCodigoInput(_inputCodigoAdmin.text);
                }
            });

            // Feedback de Ejecución
            var feedbackObj = new GameObject("FeedbackComando");
            feedbackObj.transform.SetParent(_panelAudio.transform, false);
            _txtFeedbackComando = feedbackObj.AddComponent<Text>();
            _txtFeedbackComando.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtFeedbackComando.fontSize = 11;
            _txtFeedbackComando.alignment = TextAnchor.MiddleCenter;
            _txtFeedbackComando.color = new Color(0.45f, 0.85f, 1f, 1f);
            _txtFeedbackComando.text = "💡 Escribe tu código y pulsa 'Canjear' para activar recompensas o modos especiales.";
            AgregarOutline(_txtFeedbackComando);
            var rtFeedback = feedbackObj.GetComponent<RectTransform>();
            rtFeedback.anchorMin = new Vector2(0.5f, 1f);
            rtFeedback.anchorMax = new Vector2(0.5f, 1f);
            rtFeedback.pivot = new Vector2(0.5f, 1f);
            rtFeedback.anchoredPosition = new Vector2(0f, -294f);
            rtFeedback.sizeDelta = new Vector2(820f, 26f);

            // ==================== BOTÓN ACCESO ADMIN (SOLO VISIBLE SI ESTÁ DESBLOQUEADO) ====================
            _seccionAccesoAdmin = new GameObject("SeccionAccesoAdmin");
            _seccionAccesoAdmin.transform.SetParent(_panelAudio.transform, false);
            var rtAccesoAdmin = _seccionAccesoAdmin.AddComponent<RectTransform>();
            rtAccesoAdmin.anchorMin = new Vector2(0.5f, 1f);
            rtAccesoAdmin.anchorMax = new Vector2(0.5f, 1f);
            rtAccesoAdmin.pivot = new Vector2(0.5f, 1f);
            rtAccesoAdmin.anchoredPosition = new Vector2(0f, -345f);
            rtAccesoAdmin.sizeDelta = new Vector2(820f, 60f);

            // Botón Abrir Panel Admin
            var btnAbrirModalObj = new GameObject("BotonAbrirModalAdmin");
            btnAbrirModalObj.transform.SetParent(_seccionAccesoAdmin.transform, false);
            var rtAbrirM = btnAbrirModalObj.AddComponent<RectTransform>();
            rtAbrirM.anchorMin = new Vector2(0.5f, 0.5f);
            rtAbrirM.anchorMax = new Vector2(0.5f, 0.5f);
            rtAbrirM.pivot = new Vector2(0.5f, 0.5f);
            rtAbrirM.anchoredPosition = new Vector2(-155f, 0f);
            rtAbrirM.sizeDelta = new Vector2(360f, 42f);
            var imgAbrirM = btnAbrirModalObj.AddComponent<Image>();
            imgAbrirM.color = new Color(0.85f, 0.55f, 0.12f, 1f);
            var btnAbrirM = btnAbrirModalObj.AddComponent<Button>();

            var txtAbrirMObj = new GameObject("Texto");
            txtAbrirMObj.transform.SetParent(btnAbrirModalObj.transform, false);
            var txtAbrirM = txtAbrirMObj.AddComponent<Text>();
            txtAbrirM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtAbrirM.fontSize = 12;
            txtAbrirM.fontStyle = FontStyle.Bold;
            txtAbrirM.alignment = TextAnchor.MiddleCenter;
            txtAbrirM.color = Color.white;
            txtAbrirM.text = "👑 ABRIR PANEL DE ADMIN ▾";
            AgregarOutline(txtAbrirM);
            var rtTxtAM = txtAbrirMObj.GetComponent<RectTransform>();
            rtTxtAM.anchorMin = Vector2.zero;
            rtTxtAM.anchorMax = Vector2.one;
            rtTxtAM.sizeDelta = Vector2.zero;
            btnAbrirM.onClick.AddListener(AbrirModalAdmin);

            // Botón Bloquear Modo Admin
            var btnBloquearObj = new GameObject("BotonBloquearAdmin");
            btnBloquearObj.transform.SetParent(_seccionAccesoAdmin.transform, false);
            var rtBloquear = btnBloquearObj.AddComponent<RectTransform>();
            rtBloquear.anchorMin = new Vector2(0.5f, 0.5f);
            rtBloquear.anchorMax = new Vector2(0.5f, 0.5f);
            rtBloquear.pivot = new Vector2(0.5f, 0.5f);
            rtBloquear.anchoredPosition = new Vector2(215f, 0f);
            rtBloquear.sizeDelta = new Vector2(260f, 42f);
            var imgBloquear = btnBloquearObj.AddComponent<Image>();
            imgBloquear.color = new Color(0.48f, 0.20f, 0.22f, 1f);
            var btnBloquear = btnBloquearObj.AddComponent<Button>();

            var txtBloquearObj = new GameObject("Texto");
            txtBloquearObj.transform.SetParent(btnBloquearObj.transform, false);
            var txtBloquear = txtBloquearObj.AddComponent<Text>();
            txtBloquear.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtBloquear.fontSize = 11;
            txtBloquear.fontStyle = FontStyle.Bold;
            txtBloquear.alignment = TextAnchor.MiddleCenter;
            txtBloquear.color = Color.white;
            txtBloquear.text = "🔒 BLOQUEAR MODO ADMIN";
            AgregarOutline(txtBloquear);
            var rtTxtBloq = txtBloquearObj.GetComponent<RectTransform>();
            rtTxtBloq.anchorMin = Vector2.zero;
            rtTxtBloq.anchorMax = Vector2.one;
            rtTxtBloq.sizeDelta = Vector2.zero;
            btnBloquear.onClick.AddListener(BloquearAdmin);

            ActualizarVisibilidadAdmin();

            // ==================== PANEL VIDEO ====================
            ConstruirPanelVideo(ventanaObj.transform);

            // ==================== PANEL CONTROLES AMPLIADO ====================
            _panelControles = new GameObject("PanelControles");
            _panelControles.transform.SetParent(ventanaObj.transform, false);
            var rtContr = _panelControles.AddComponent<RectTransform>();
            rtContr.anchorMin = new Vector2(0.5f, 0.5f);
            rtContr.anchorMax = new Vector2(0.5f, 0.5f);
            rtContr.pivot = new Vector2(0.5f, 0.5f);
            rtContr.anchoredPosition = new Vector2(0f, -12f);
            rtContr.sizeDelta = new Vector2(850f, 500f);

            // Subtítulo Controles
            var estObj = new GameObject("TextoEstado");
            estObj.transform.SetParent(_panelControles.transform, false);
            var txtEst = estObj.AddComponent<Text>();
            txtEst.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtEst.fontSize = 12;
            txtEst.alignment = TextAnchor.MiddleCenter;
            txtEst.color = new Color(0.75f, 0.85f, 1f, 1f);
            txtEst.text = "Haz clic en una tecla para cambiarla. Presiona ESC para cancelar la espera.";
            AgregarOutline(txtEst);
            var rtEst = estObj.GetComponent<RectTransform>();
            rtEst.anchorMin = new Vector2(0.5f, 1f);
            rtEst.anchorMax = new Vector2(0.5f, 1f);
            rtEst.pivot = new Vector2(0.5f, 1f);
            rtEst.anchoredPosition = new Vector2(0f, -8f);
            rtEst.sizeDelta = new Vector2(820f, 24f);
            textoEstado = txtEst;

            // Scroll / Contenedor Filas Controles Espacioso
            var contFilasObj = new GameObject("ContenedorFilas");
            contFilasObj.transform.SetParent(_panelControles.transform, false);
            var vlg = contFilasObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 6f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            var rtFilas = contFilasObj.GetComponent<RectTransform>();
            rtFilas.anchorMin = new Vector2(0.5f, 0.5f);
            rtFilas.anchorMax = new Vector2(0.5f, 0.5f);
            rtFilas.pivot = new Vector2(0.5f, 0.5f);
            rtFilas.anchoredPosition = new Vector2(0f, -10f);
            rtFilas.sizeDelta = new Vector2(820f, 430f);
            contenedorFilas = contFilasObj.transform;

            // ==================== BARRA INFERIOR DE ACCIÓN GLOBAL ====================
            var barraInferior = new GameObject("BarraInferior");
            barraInferior.transform.SetParent(ventanaObj.transform, false);
            var rtBarraInf = barraInferior.AddComponent<RectTransform>();
            rtBarraInf.anchorMin = new Vector2(0.5f, 0f);
            rtBarraInf.anchorMax = new Vector2(0.5f, 0f);
            rtBarraInf.pivot = new Vector2(0.5f, 0f);
            rtBarraInf.anchoredPosition = new Vector2(0f, 18f);
            rtBarraInf.sizeDelta = new Vector2(850f, 44f);

            // Botón Restaurar Controles
            var btnRestObj = new GameObject("BotonRestaurar");
            btnRestObj.transform.SetParent(barraInferior.transform, false);
            var imgRest = btnRestObj.AddComponent<Image>();
            imgRest.color = new Color(0.48f, 0.22f, 0.22f, 1f);
            var rtRest = btnRestObj.GetComponent<RectTransform>();
            rtRest.anchorMin = new Vector2(0f, 0.5f);
            rtRest.anchorMax = new Vector2(0f, 0.5f);
            rtRest.pivot = new Vector2(0f, 0.5f);
            rtRest.anchoredPosition = new Vector2(0f, 0f);
            rtRest.sizeDelta = new Vector2(250f, 40f);
            botonRestaurar = btnRestObj.AddComponent<Button>();

            var txtRestObj = new GameObject("Texto");
            txtRestObj.transform.SetParent(btnRestObj.transform, false);
            var txtRest = txtRestObj.AddComponent<Text>();
            txtRest.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtRest.fontSize = 11;
            txtRest.fontStyle = FontStyle.Bold;
            txtRest.alignment = TextAnchor.MiddleCenter;
            txtRest.color = Color.white;
            txtRest.text = "↺ RESTAURAR CONTROLES";
            AgregarOutline(txtRest);
            var rtTxtRest = txtRestObj.GetComponent<RectTransform>();
            rtTxtRest.anchorMin = Vector2.zero;
            rtTxtRest.anchorMax = Vector2.one;
            rtTxtRest.sizeDelta = Vector2.zero;

            // Botón Cerrar Global
            var btnCerrarObj = new GameObject("BotonCerrar");
            btnCerrarObj.transform.SetParent(barraInferior.transform, false);
            var imgCerrar = btnCerrarObj.AddComponent<Image>();
            imgCerrar.color = new Color(0.18f, 0.52f, 0.32f, 1f);
            var rtCerrar = btnCerrarObj.GetComponent<RectTransform>();
            rtCerrar.anchorMin = new Vector2(1f, 0.5f);
            rtCerrar.anchorMax = new Vector2(1f, 0.5f);
            rtCerrar.pivot = new Vector2(1f, 0.5f);
            rtCerrar.anchoredPosition = new Vector2(0f, 0f);
            rtCerrar.sizeDelta = new Vector2(270f, 40f);
            botonCerrar = btnCerrarObj.AddComponent<Button>();

            var txtCerrarObj = new GameObject("Texto");
            txtCerrarObj.transform.SetParent(btnCerrarObj.transform, false);
            var txtCerrar = txtCerrarObj.AddComponent<Text>();
            txtCerrar.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtCerrar.fontSize = 12;
            txtCerrar.fontStyle = FontStyle.Bold;
            txtCerrar.alignment = TextAnchor.MiddleCenter;
            txtCerrar.color = Color.white;
            txtCerrar.text = "✔️ CERRAR Y GUARDAR";
            AgregarOutline(txtCerrar);
            var rtTxtCerrar = txtCerrarObj.GetComponent<RectTransform>();
            rtTxtCerrar.anchorMin = Vector2.zero;
            rtTxtCerrar.anchorMax = Vector2.one;
            rtTxtCerrar.sizeDelta = Vector2.zero;

            // Listeners
            _btnTabAudio.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Audio));
            if (_btnTabVideo != null) _btnTabVideo.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Video));
            _btnTabControles.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Controles));
            botonCerrar.onClick.AddListener(Cerrar);
            botonRestaurar.onClick.AddListener(RestaurarPorDefecto);

            ConstruirModalAdmin(transform);
            ConstruirFilas();
            CambiarPestana(PestanaOpciones.Audio);
        }

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

            var rtModal = _modalAdmin.AddComponent<RectTransform>();
            rtModal.anchorMin = Vector2.zero;
            rtModal.anchorMax = Vector2.one;
            rtModal.sizeDelta = Vector2.zero;
            rtModal.anchoredPosition = Vector2.zero;

            var imgFondoOscuro = _modalAdmin.AddComponent<Image>();
            imgFondoOscuro.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            // Ventana flotante de herramientas de Admin
            var dialogObj = new GameObject("DialogoAdmin");
            dialogObj.transform.SetParent(_modalAdmin.transform, false);
            var rtDialog = dialogObj.AddComponent<RectTransform>();
            rtDialog.anchorMin = new Vector2(0.5f, 0.5f);
            rtDialog.anchorMax = new Vector2(0.5f, 0.5f);
            rtDialog.pivot = new Vector2(0.5f, 0.5f);
            rtDialog.sizeDelta = new Vector2(840f, 575f);

            var imgDialog = dialogObj.AddComponent<Image>();
            imgDialog.color = new Color(0.08f, 0.11f, 0.18f, 0.99f);

            // Cabecera Modal
            var barraTopModal = new GameObject("BarraTopModal");
            barraTopModal.transform.SetParent(dialogObj.transform, false);
            var rtBarraTop = barraTopModal.AddComponent<RectTransform>();
            rtBarraTop.anchorMin = new Vector2(0f, 1f);
            rtBarraTop.anchorMax = new Vector2(1f, 1f);
            rtBarraTop.pivot = new Vector2(0.5f, 1f);
            rtBarraTop.sizeDelta = new Vector2(0f, 46f);
            var imgBarra = barraTopModal.AddComponent<Image>();
            imgBarra.color = new Color(0.14f, 0.18f, 0.28f, 1f);

            // Título
            var titModal = new GameObject("Titulo");
            titModal.transform.SetParent(barraTopModal.transform, false);
            var txtTit = titModal.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 14;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleLeft;
            txtTit.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTit.text = "  👑 PANEL DE CONTROL DE ADMINISTRADOR & TESTEO";
            AgregarOutline(txtTit);
            var rtTit = titModal.GetComponent<RectTransform>();
            rtTit.anchorMin = Vector2.zero;
            rtTit.anchorMax = Vector2.one;
            rtTit.offsetMax = new Vector2(-50f, 0f);

            // Botón X cerrar en esquina
            var btnXObj = new GameObject("BtnCerrarX");
            btnXObj.transform.SetParent(barraTopModal.transform, false);
            var rtX = btnXObj.AddComponent<RectTransform>();
            rtX.anchorMin = new Vector2(1f, 0.5f);
            rtX.anchorMax = new Vector2(1f, 0.5f);
            rtX.pivot = new Vector2(1f, 0.5f);
            rtX.anchoredPosition = new Vector2(-8f, 0f);
            rtX.sizeDelta = new Vector2(34f, 34f);
            var imgX = btnXObj.AddComponent<Image>();
            imgX.color = new Color(0.55f, 0.22f, 0.22f, 1f);
            var btnX = btnXObj.AddComponent<Button>();

            var txtXObj = new GameObject("Texto");
            txtXObj.transform.SetParent(btnXObj.transform, false);
            var txtX = txtXObj.AddComponent<Text>();
            txtX.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtX.fontSize = 15;
            txtX.fontStyle = FontStyle.Bold;
            txtX.alignment = TextAnchor.MiddleCenter;
            txtX.color = Color.white;
            txtX.text = "✕";
            AgregarOutline(txtX);
            var rtTxtX = txtXObj.GetComponent<RectTransform>();
            rtTxtX.anchorMin = Vector2.zero;
            rtTxtX.anchorMax = Vector2.one;
            rtTxtX.sizeDelta = Vector2.zero;
            btnX.onClick.AddListener(CerrarModalAdmin);

            // Subtítulo
            var subModal = new GameObject("Subtitulo");
            subModal.transform.SetParent(dialogObj.transform, false);
            var txtSub = subModal.AddComponent<Text>();
            txtSub.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSub.fontSize = 11;
            txtSub.fontStyle = FontStyle.Bold;
            txtSub.alignment = TextAnchor.MiddleLeft;
            txtSub.color = new Color(0.40f, 0.85f, 1f, 1f);
            txtSub.text = "⚡ ACCESOS RÁPIDOS DE DESARROLLO:";
            AgregarOutline(txtSub);
            var rtSub = subModal.GetComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 1f);
            rtSub.anchorMax = new Vector2(1f, 1f);
            rtSub.pivot = new Vector2(0f, 1f);
            rtSub.anchoredPosition = new Vector2(20f, -54f);
            rtSub.sizeDelta = new Vector2(0f, 22f);

            // Botones de Acceso Rápido Fila 1 (y = -80f)
            CrearBotonCheat(dialogObj.transform, "BtnCheatLvl5", "⭐ +5 NIVELES", new Vector2(-295f, -80f), new Color(0.18f, 0.40f, 0.65f, 1f), () => EjecutarCheat("lvl 5"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatMaxLvl", "👑 MAX NIVEL (10)", new Vector2(-98f, -80f), new Color(0.20f, 0.45f, 0.75f, 1f), () => EjecutarCheat("maxlvl"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatUnlock", "🔓 UNLOCK ALL SKILLS", new Vector2(98f, -80f), new Color(0.65f, 0.45f, 0.15f, 1f), () => EjecutarCheat("unlockall"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatMaestria", "✨ +1000 MAESTRÍA", new Vector2(295f, -80f), new Color(0.55f, 0.35f, 0.65f, 1f), () => EjecutarCheat("maestria 1000"));

            // Botones de Acceso Rápido Fila 2 (y = -122f)
            CrearBotonCheat(dialogObj.transform, "BtnCheatGod", "❤️ MODO DIOS (9999 HP)", new Vector2(-295f, -122f), new Color(0.75f, 0.20f, 0.25f, 1f), () => EjecutarCheat("god"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatKill", "💀 MATAR ENEMIGOS", new Vector2(-98f, -122f), new Color(0.45f, 0.18f, 0.22f, 1f), () => EjecutarCheat("killall"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatSkip", "🌀 SALTAR NIVEL (Skip)", new Vector2(98f, -122f), new Color(0.20f, 0.50f, 0.45f, 1f), () => EjecutarCheat("skip"));
            CrearBotonCheat(dialogObj.transform, "BtnCheat0CD", "⚡ 0 COOLDOWNS", new Vector2(295f, -122f), new Color(0.20f, 0.55f, 0.35f, 1f), () => EjecutarCheat("0cd"));

            // Botones de Acceso Rápido Fila 3 (y = -164f)
            CrearBotonCheat(dialogObj.transform, "BtnCheatSkipSalas", "🌀 SALTAR TODAS SALAS", new Vector2(-295f, -164f), new Color(0.28f, 0.40f, 0.60f, 1f), () => EjecutarCheat("skipsalas"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatResetLvl", "↺ RESET NIVEL (1)", new Vector2(-98f, -164f), new Color(0.50f, 0.30f, 0.25f, 1f), () => EjecutarCheat("resetlvl"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatResetMaestria", "↺ RESET MAESTRÍA", new Vector2(98f, -164f), new Color(0.55f, 0.25f, 0.35f, 1f), () => EjecutarCheat("resetmaestria"));
            CrearBotonCheat(dialogObj.transform, "BtnCheatResetPoderes", "↺ RESET PODERES RUN", new Vector2(295f, -164f), new Color(0.60f, 0.20f, 0.20f, 1f), () => EjecutarCheat("resetpoderes"));

            // Divisor Modal
            var divModal = new GameObject("DivisorModal");
            divModal.transform.SetParent(dialogObj.transform, false);
            var rtDivM = divModal.AddComponent<RectTransform>();
            rtDivM.anchorMin = new Vector2(0.5f, 1f);
            rtDivM.anchorMax = new Vector2(0.5f, 1f);
            rtDivM.pivot = new Vector2(0.5f, 1f);
            rtDivM.anchoredPosition = new Vector2(0f, -208f);
            rtDivM.sizeDelta = new Vector2(790f, 2f);
            var imgDivM = divModal.AddComponent<Image>();
            imgDivM.color = new Color(0.20f, 0.26f, 0.38f, 0.8f);

            // Subtítulo Comandos Manuales
            var subCmd = new GameObject("SubtituloComandos");
            subCmd.transform.SetParent(dialogObj.transform, false);
            var txtSubCmd = subCmd.AddComponent<Text>();
            txtSubCmd.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSubCmd.fontSize = 11;
            txtSubCmd.fontStyle = FontStyle.Bold;
            txtSubCmd.alignment = TextAnchor.MiddleLeft;
            txtSubCmd.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtSubCmd.text = "💻 CONSOLA DE COMANDOS MANUALES:";
            AgregarOutline(txtSubCmd);
            var rtSubCmd = subCmd.GetComponent<RectTransform>();
            rtSubCmd.anchorMin = new Vector2(0f, 1f);
            rtSubCmd.anchorMax = new Vector2(1f, 1f);
            rtSubCmd.pivot = new Vector2(0f, 1f);
            rtSubCmd.anchoredPosition = new Vector2(20f, -222f);
            rtSubCmd.sizeDelta = new Vector2(0f, 20f);

            var descCmd = new GameObject("DescComandos");
            descCmd.transform.SetParent(dialogObj.transform, false);
            var txtDescCmd = descCmd.AddComponent<Text>();
            txtDescCmd.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtDescCmd.fontSize = 10;
            txtDescCmd.alignment = TextAnchor.MiddleLeft;
            txtDescCmd.color = new Color(0.70f, 0.78f, 0.90f, 1f);
            txtDescCmd.text = "Ejemplos: '0cd', 'skipsalas', 'resetrun', 'resetlvl', 'lvl 8', 'ouro 999', 'vida 500', 'dano 100'";
            AgregarOutline(txtDescCmd);
            var rtDescCmd = descCmd.GetComponent<RectTransform>();
            rtDescCmd.anchorMin = new Vector2(0f, 1f);
            rtDescCmd.anchorMax = new Vector2(1f, 1f);
            rtDescCmd.pivot = new Vector2(0f, 1f);
            rtDescCmd.anchoredPosition = new Vector2(20f, -244f);
            rtDescCmd.sizeDelta = new Vector2(0f, 16f);

            // Barra Input Modal
            var barInputModal = new GameObject("BarraInputModal");
            barInputModal.transform.SetParent(dialogObj.transform, false);
            var rtBarInputM = barInputModal.AddComponent<RectTransform>();
            rtBarInputM.anchorMin = new Vector2(0.5f, 1f);
            rtBarInputM.anchorMax = new Vector2(0.5f, 1f);
            rtBarInputM.pivot = new Vector2(0.5f, 1f);
            rtBarInputM.anchoredPosition = new Vector2(0f, -270f);
            rtBarInputM.sizeDelta = new Vector2(790f, 38f);

            var inpModalObj = new GameObject("InputFieldModal");
            inpModalObj.transform.SetParent(barInputModal.transform, false);
            var rtInpM = inpModalObj.AddComponent<RectTransform>();
            rtInpM.anchorMin = new Vector2(0f, 0.5f);
            rtInpM.anchorMax = new Vector2(0f, 0.5f);
            rtInpM.pivot = new Vector2(0f, 0.5f);
            rtInpM.anchoredPosition = new Vector2(0f, 0f);
            rtInpM.sizeDelta = new Vector2(570f, 36f);

            var imgInpM = inpModalObj.AddComponent<Image>();
            imgInpM.color = new Color(0.05f, 0.07f, 0.11f, 1f);

            _inputComandoModal = inpModalObj.AddComponent<InputField>();

            var phMObj = new GameObject("Placeholder");
            phMObj.transform.SetParent(inpModalObj.transform, false);
            var txtPhM = phMObj.AddComponent<Text>();
            txtPhM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPhM.fontSize = 11;
            txtPhM.fontStyle = FontStyle.Italic;
            txtPhM.color = new Color(0.48f, 0.55f, 0.65f, 0.75f);
            txtPhM.text = "Escribe comando y pulsa Enter o Ejecutar...";
            txtPhM.alignment = TextAnchor.MiddleLeft;
            var rtPhM = phMObj.GetComponent<RectTransform>();
            rtPhM.anchorMin = Vector2.zero;
            rtPhM.anchorMax = Vector2.one;
            rtPhM.offsetMin = new Vector2(10f, 0f);
            rtPhM.offsetMax = new Vector2(-10f, 0f);

            var txtInpMObj = new GameObject("Text");
            txtInpMObj.transform.SetParent(inpModalObj.transform, false);
            var txtInpM = txtInpMObj.AddComponent<Text>();
            txtInpM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtInpM.fontSize = 12;
            txtInpM.color = Color.white;
            txtInpM.alignment = TextAnchor.MiddleLeft;
            txtInpM.supportRichText = false;
            AgregarOutline(txtInpM);
            var rtTxtInpM = txtInpMObj.GetComponent<RectTransform>();
            rtTxtInpM.anchorMin = Vector2.zero;
            rtTxtInpM.anchorMax = Vector2.one;
            rtTxtInpM.offsetMin = new Vector2(10f, 0f);
            rtTxtInpM.offsetMax = new Vector2(-10f, 0f);

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
            var btnEjMObj = new GameObject("BtnEjecutarModal");
            btnEjMObj.transform.SetParent(barInputModal.transform, false);
            var rtBtnEjM = btnEjMObj.AddComponent<RectTransform>();
            rtBtnEjM.anchorMin = new Vector2(1f, 0.5f);
            rtBtnEjM.anchorMax = new Vector2(1f, 0.5f);
            rtBtnEjM.pivot = new Vector2(1f, 0.5f);
            rtBtnEjM.anchoredPosition = new Vector2(0f, 0f);
            rtBtnEjM.sizeDelta = new Vector2(200f, 36f);
            var imgEjM = btnEjMObj.AddComponent<Image>();
            imgEjM.color = new Color(0.12f, 0.55f, 0.32f, 1f);
            var btnEjM = btnEjMObj.AddComponent<Button>();

            var txtBtnEjMObj = new GameObject("Texto");
            txtBtnEjMObj.transform.SetParent(btnEjMObj.transform, false);
            var txtBtnEjM = txtBtnEjMObj.AddComponent<Text>();
            txtBtnEjM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtBtnEjM.fontSize = 11;
            txtBtnEjM.fontStyle = FontStyle.Bold;
            txtBtnEjM.alignment = TextAnchor.MiddleCenter;
            txtBtnEjM.color = Color.white;
            txtBtnEjM.text = "▶ EJECUTAR";
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
            var fbMObj = new GameObject("FeedbackModal");
            fbMObj.transform.SetParent(dialogObj.transform, false);
            _txtFeedbackModalAdmin = fbMObj.AddComponent<Text>();
            _txtFeedbackModalAdmin.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtFeedbackModalAdmin.fontSize = 11;
            _txtFeedbackModalAdmin.alignment = TextAnchor.MiddleCenter;
            _txtFeedbackModalAdmin.color = new Color(0.45f, 0.85f, 1f, 1f);
            _txtFeedbackModalAdmin.text = "Selecciona una herramienta rápida o escribe un comando personalizado:";
            AgregarOutline(_txtFeedbackModalAdmin);
            var rtFbM = fbMObj.GetComponent<RectTransform>();
            rtFbM.anchorMin = new Vector2(0.5f, 1f);
            rtFbM.anchorMax = new Vector2(0.5f, 1f);
            rtFbM.pivot = new Vector2(0.5f, 1f);
            rtFbM.anchoredPosition = new Vector2(0f, -318f);
            rtFbM.sizeDelta = new Vector2(790f, 26f);

            // Botón Cerrar en Modal
            var btnCerrarMObj = new GameObject("BtnCerrarModal");
            btnCerrarMObj.transform.SetParent(dialogObj.transform, false);
            var rtBtnCerrarM = btnCerrarMObj.AddComponent<RectTransform>();
            rtBtnCerrarM.anchorMin = new Vector2(0.5f, 0f);
            rtBtnCerrarM.anchorMax = new Vector2(0.5f, 0f);
            rtBtnCerrarM.pivot = new Vector2(0.5f, 0f);
            rtBtnCerrarM.anchoredPosition = new Vector2(0f, 20f);
            rtBtnCerrarM.sizeDelta = new Vector2(300f, 40f);
            var imgCerrarM = btnCerrarMObj.AddComponent<Image>();
            imgCerrarM.color = new Color(0.35f, 0.40f, 0.50f, 1f);
            var btnCerrarM = btnCerrarMObj.AddComponent<Button>();

            var txtCerrarMObj = new GameObject("Texto");
            txtCerrarMObj.transform.SetParent(btnCerrarMObj.transform, false);
            var txtCerrarM = txtCerrarMObj.AddComponent<Text>();
            txtCerrarM.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtCerrarM.fontSize = 12;
            txtCerrarM.fontStyle = FontStyle.Bold;
            txtCerrarM.alignment = TextAnchor.MiddleCenter;
            txtCerrarM.color = Color.white;
            txtCerrarM.text = "✕ CERRAR PANEL ADMIN";
            AgregarOutline(txtCerrarM);
            var rtTxtCM = txtCerrarMObj.GetComponent<RectTransform>();
            rtTxtCM.anchorMin = Vector2.zero;
            rtTxtCM.anchorMax = Vector2.one;
            rtTxtCM.sizeDelta = Vector2.zero;
            btnCerrarM.onClick.AddListener(CerrarModalAdmin);

            _modalAdmin.SetActive(false);
        }

        private static void CrearBotonCheat(Transform parent, string nombre, string texto, Vector2 pos, Color colorBase, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(190f, 32f);

            var img = btnObj.AddComponent<Image>();
            img.color = colorBase;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = colorBase * 1.25f;
            colors.pressedColor = colorBase * 0.75f;
            btn.colors = colors;

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 10;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = texto;
            AgregarOutline(txt);

            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private static Button CrearBotonTab(Transform parent, string nombre, string texto, Vector2 pos, float ancho, out Image img, out Text txt)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(ancho, 38f);

            img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f, 1f);

            var btn = btnObj.AddComponent<Button>();

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = texto;
            AgregarOutline(txt);
            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static Slider CrearSliderFila(Transform parent, string nombre, string etiqueta, float valorInicial, Action<float> onCambio, out Text txtPorcentaje)
        {
            var filaObj = new GameObject(nombre);
            filaObj.transform.SetParent(parent, false);
            var rtFila = filaObj.AddComponent<RectTransform>();
            rtFila.sizeDelta = new Vector2(800f, 34f);

            // Label
            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(filaObj.transform, false);
            var txtLbl = lblObj.AddComponent<Text>();
            txtLbl.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtLbl.fontSize = 12;
            txtLbl.fontStyle = FontStyle.Bold;
            txtLbl.alignment = TextAnchor.MiddleLeft;
            txtLbl.color = Color.white;
            txtLbl.text = etiqueta;
            AgregarOutline(txtLbl);
            var rtLbl = lblObj.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 0.5f);
            rtLbl.anchorMax = new Vector2(0f, 0.5f);
            rtLbl.pivot = new Vector2(0f, 0.5f);
            rtLbl.anchoredPosition = new Vector2(10f, 0f);
            rtLbl.sizeDelta = new Vector2(250f, 32f);

            // Porcentaje
            var pctObj = new GameObject("Porcentaje");
            pctObj.transform.SetParent(filaObj.transform, false);
            var txtPct = pctObj.AddComponent<Text>();
            txtPct.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPct.fontSize = 12;
            txtPct.fontStyle = FontStyle.Bold;
            txtPct.alignment = TextAnchor.MiddleRight;
            txtPct.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtPct.text = $"{Mathf.RoundToInt(valorInicial * 100f)}%";
            AgregarOutline(txtPct);
            var rtPct = pctObj.GetComponent<RectTransform>();
            rtPct.anchorMin = new Vector2(1f, 0.5f);
            rtPct.anchorMax = new Vector2(1f, 0.5f);
            rtPct.pivot = new Vector2(1f, 0.5f);
            rtPct.anchoredPosition = new Vector2(-10f, 0f);
            rtPct.sizeDelta = new Vector2(60f, 32f);
            txtPorcentaje = txtPct;

            // Slider Component
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(filaObj.transform, false);
            var rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchorMin = new Vector2(0f, 0.5f);
            rtSlider.anchorMax = new Vector2(0f, 0.5f);
            rtSlider.pivot = new Vector2(0f, 0.5f);
            rtSlider.anchoredPosition = new Vector2(270f, 0f);
            rtSlider.sizeDelta = new Vector2(400f, 20f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = valorInicial;

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var imgBg = bgObj.AddComponent<Image>();
            imgBg.color = new Color(0.12f, 0.15f, 0.22f, 1f);
            var rtBg = bgObj.GetComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;

            // Fill Area
            var fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var rtFillArea = fillAreaObj.AddComponent<RectTransform>();
            rtFillArea.anchorMin = Vector2.zero;
            rtFillArea.anchorMax = Vector2.one;
            rtFillArea.sizeDelta = new Vector2(-12f, 0f);
            rtFillArea.anchoredPosition = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var imgFill = fillObj.AddComponent<Image>();
            imgFill.color = new Color(0.24f, 0.68f, 0.95f, 1f);
            var rtFill = fillObj.GetComponent<RectTransform>();
            rtFill.sizeDelta = Vector2.zero;
            slider.fillRect = rtFill;

            // Handle Area
            var handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            var rtHandleArea = handleAreaObj.AddComponent<RectTransform>();
            rtHandleArea.anchorMin = Vector2.zero;
            rtHandleArea.anchorMax = Vector2.one;
            rtHandleArea.sizeDelta = new Vector2(-12f, 0f);

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            var imgHandle = handleObj.AddComponent<Image>();
            imgHandle.color = new Color(1f, 0.85f, 0.25f, 1f);
            var rtHandle = handleObj.GetComponent<RectTransform>();
            rtHandle.sizeDelta = new Vector2(16f, 26f);
            slider.handleRect = rtHandle;
            slider.targetGraphic = imgHandle;

            var txtRef = txtPct;
            slider.onValueChanged.AddListener((v) =>
            {
                if (txtRef != null) txtRef.text = $"{Mathf.RoundToInt(v * 100f)}%";
                onCambio?.Invoke(v);
            });

            return slider;
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
            if (_panelControles != null) _panelControles.SetActive(pestana == PestanaOpciones.Controles);

            // La opción de resetear controles solo debe de salir en el apartado de controles
            if (botonRestaurar != null)
            {
                botonRestaurar.gameObject.SetActive(pestana == PestanaOpciones.Controles);
            }

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
                        rtC.anchoredPosition = new Vector2(0f, 0f);
                        rtC.sizeDelta = new Vector2(270f, 40f);
                    }
                    else
                    {
                        rtC.anchorMin = new Vector2(0.5f, 0.5f);
                        rtC.anchorMax = new Vector2(0.5f, 0.5f);
                        rtC.pivot = new Vector2(0.5f, 0.5f);
                        rtC.anchoredPosition = new Vector2(0f, 0f);
                        rtC.sizeDelta = new Vector2(340f, 40f);
                    }
                }
            }

            Color cActivo = new Color(0.25f, 0.55f, 0.85f, 1f);
            Color cInactivo = new Color(0.14f, 0.18f, 0.26f, 1f);
            if (_imgTabAudio != null) _imgTabAudio.color = (pestana == PestanaOpciones.Audio) ? cActivo : cInactivo;
            if (_imgTabVideo != null) _imgTabVideo.color = (pestana == PestanaOpciones.Video) ? cActivo : cInactivo;
            if (_imgTabControles != null) _imgTabControles.color = (pestana == PestanaOpciones.Controles) ? cActivo : cInactivo;
        }

        void Start()
        {
            AsegurarInterfazCompleta();
        }

        void Update()
        {
            if (_accionEsperandoTecla.HasValue)
            {
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

            transform.SetAsLastSibling();
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 30000;
            }

            ActualizarVisibilidadAdmin();

            if (panelContenedor) panelContenedor.SetActive(true);
            else gameObject.SetActive(true);

            CambiarPestana(pestanaInicial);

            // Sincronizar sliders con volúmenes reales
            if (_sliderMaster != null) _sliderMaster.value = GestorAudio.Instancia.VolumenMaster;
            if (_sliderMusica != null) _sliderMusica.value = GestorAudio.Instancia.VolumenMusica;
            if (_sliderSFX != null) _sliderSFX.value = GestorAudio.Instancia.VolumenSFX;

            if (_txtPctMaster != null) _txtPctMaster.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenMaster * 100f)}%";
            if (_txtPctMusica != null) _txtPctMusica.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenMusica * 100f)}%";
            if (_txtPctSFX != null) _txtPctSFX.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenSFX * 100f)}%";

            ActualizarTodosVisualesVideo();
            ActualizarTextos();
        }

        public void Cerrar()
        {
            FinalizarEsperaTecla();
            CerrarModalAdmin();
            if (panelContenedor) panelContenedor.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void RestaurarPorDefecto()
        {
            GestorControles.RestaurarPorDefecto();
            ActualizarTextos();
            if (textoEstado) textoEstado.text = "¡Controles restaurados por defecto!";
        }

        private void ConstruirPanelVideo(Transform ventanaTransform)
        {
            _panelVideo = new GameObject("PanelVideo");
            _panelVideo.transform.SetParent(ventanaTransform, false);
            var rtVideo = _panelVideo.AddComponent<RectTransform>();
            rtVideo.anchorMin = new Vector2(0.5f, 0.5f);
            rtVideo.anchorMax = new Vector2(0.5f, 0.5f);
            rtVideo.pivot = new Vector2(0.5f, 0.5f);
            rtVideo.anchoredPosition = new Vector2(0f, -12f);
            rtVideo.sizeDelta = new Vector2(850f, 500f);

            // Subtítulo Video
            var subVideo = new GameObject("SubtituloVideo");
            subVideo.transform.SetParent(_panelVideo.transform, false);
            var txtSubVideo = subVideo.AddComponent<Text>();
            txtSubVideo.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSubVideo.fontSize = 12;
            txtSubVideo.fontStyle = FontStyle.Bold;
            txtSubVideo.alignment = TextAnchor.MiddleLeft;
            txtSubVideo.color = new Color(0.40f, 0.85f, 1f, 1f);
            txtSubVideo.text = "🖥️ AJUSTES DE VIDEO Y VISUALIZACIÓN EN PANTALLA";
            AgregarOutline(txtSubVideo);
            var rtSubVideo = subVideo.GetComponent<RectTransform>();
            rtSubVideo.anchorMin = new Vector2(0f, 1f);
            rtSubVideo.anchorMax = new Vector2(1f, 1f);
            rtSubVideo.pivot = new Vector2(0f, 1f);
            rtSubVideo.anchoredPosition = new Vector2(15f, -6f);
            rtSubVideo.sizeDelta = new Vector2(0f, 22f);

            // Contenedor Filas Video
            var contFilasVideo = new GameObject("ContenedorFilasVideo");
            contFilasVideo.transform.SetParent(_panelVideo.transform, false);
            var rtContVideo = contFilasVideo.AddComponent<RectTransform>();
            rtContVideo.anchorMin = new Vector2(0.5f, 1f);
            rtContVideo.anchorMax = new Vector2(0.5f, 1f);
            rtContVideo.pivot = new Vector2(0.5f, 1f);
            rtContVideo.anchoredPosition = new Vector2(0f, -34f);
            rtContVideo.sizeDelta = new Vector2(820f, 440f);

            var vlgVideo = contFilasVideo.AddComponent<VerticalLayoutGroup>();
            vlgVideo.childAlignment = TextAnchor.UpperCenter;
            vlgVideo.spacing = 8f;
            vlgVideo.childControlWidth = false;
            vlgVideo.childControlHeight = false;

            // 1. Daño Acumulativo (Suma de números por tick)
            _btnDanoAcumulativo = CrearFilaOpcionBoton(contFilasVideo.transform, "FilaDanoAcumulativo",
                "Daño Acumulativo en Pantalla",
                "Agrupa y va sumando los impactos continuos del jugador en una sola cifra que sube.",
                "",
                () =>
                {
                    bool actual = PlayerPrefs.GetInt("DanoAcumulativo", 0) == 1;
                    PlayerPrefs.SetInt("DanoAcumulativo", actual ? 0 : 1);
                    PlayerPrefs.Save();
                    ActualizarVisualDanoAcumulativo();
                },
                out _txtDanoAcumulativo);

            // 2. Modo de Pantalla (Fullscreen / Ventana)
            _btnPantallaCompleta = CrearFilaOpcionBoton(contFilasVideo.transform, "FilaPantallaCompleta",
                "Modo de Pantalla",
                "Alterna entre modo ventana y pantalla completa sin bordes.",
                "",
                () =>
                {
                    bool nuevaCompleta = !Screen.fullScreen;
                    FullScreenMode modo = nuevaCompleta ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                    var resActual = _resolucionesDisponibles[_indiceResolucion];
                    Screen.SetResolution(resActual.width, resActual.height, modo);
                    PlayerPrefs.SetInt("PantallaCompleta", nuevaCompleta ? 1 : 0);
                    PlayerPrefs.Save();
                    ActualizarVisualPantallaCompleta(nuevaCompleta);
                },
                out _txtPantallaCompleta);

            // 3. Sincronización Vertical (V-Sync)
            _btnVSync = CrearFilaOpcionBoton(contFilasVideo.transform, "FilaVSync",
                "Sincronización Vertical (V-Sync)",
                "Limita los FPS a la tasa de refresco del monitor para una fluidez óptima.",
                "",
                () =>
                {
                    QualitySettings.vSyncCount = (QualitySettings.vSyncCount > 0) ? 0 : 1;
                    PlayerPrefs.SetInt("VSyncSetting", QualitySettings.vSyncCount);
                    PlayerPrefs.Save();
                    ActualizarVisualVSync();
                },
                out _txtVSync);

            // 4. Calidad Gráfica
            _btnCalidad = CrearFilaOpcionBoton(contFilasVideo.transform, "FilaCalidad",
                "Nivel de Calidad Gráfica",
                "Ajusta sombras, suavizado y nivel de renderizado gráfico.",
                "",
                () =>
                {
                    int totalNiveles = QualitySettings.names.Length;
                    int actual = QualitySettings.GetQualityLevel();
                    int nuevo = (actual + 1) % totalNiveles;
                    QualitySettings.SetQualityLevel(nuevo, true);
                    PlayerPrefs.SetInt("CalidadGrafica", nuevo);
                    PlayerPrefs.Save();
                    ActualizarVisualCalidad();
                },
                out _txtCalidad);

            // 5. Resolución de Pantalla
            _btnResolucion = CrearFilaOpcionBoton(contFilasVideo.transform, "FilaResolucion",
                "Resolución de Pantalla",
                "Cambia las dimensiones nativas de la ventana.",
                "",
                () =>
                {
                    _indiceResolucion = (_indiceResolucion + 1) % _resolucionesDisponibles.Length;
                    var res = _resolucionesDisponibles[_indiceResolucion];
                    FullScreenMode modo = Screen.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                    Screen.SetResolution(res.width, res.height, modo);
                    PlayerPrefs.SetInt("ResWidth", res.width);
                    PlayerPrefs.SetInt("ResHeight", res.height);
                    PlayerPrefs.Save();
                    ActualizarVisualResolucion(res.width, res.height);
                },
                out _txtResolucion);

            ActualizarTodosVisualesVideo();
        }

        private Button CrearFilaOpcionBoton(Transform parent, string nombre, string titulo, string descripcion, string textoBotonInicial, Action onBotonClick, out Text outTxtBoton)
        {
            var filaObj = new GameObject(nombre);
            filaObj.transform.SetParent(parent, false);
            var rtFila = filaObj.AddComponent<RectTransform>();
            rtFila.sizeDelta = new Vector2(810f, 54f);

            var imgFila = filaObj.AddComponent<Image>();
            imgFila.color = new Color(0.10f, 0.13f, 0.20f, 0.95f);

            // Textos: Titulo y Descripcion a la izquierda
            var textCont = new GameObject("ContenedorTextos");
            textCont.transform.SetParent(filaObj.transform, false);
            var rtTextCont = textCont.AddComponent<RectTransform>();
            rtTextCont.anchorMin = new Vector2(0f, 0f);
            rtTextCont.anchorMax = new Vector2(0.68f, 1f);
            rtTextCont.offsetMin = new Vector2(16f, 4f);
            rtTextCont.offsetMax = new Vector2(-8f, -4f);

            var txtTit = textCont.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 13;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.UpperLeft;
            txtTit.color = Color.white;
            txtTit.text = $"{titulo}\n<size=10><color=#A0B2C6>{descripcion}</color></size>";
            txtTit.supportRichText = true;
            AgregarOutline(txtTit);

            // Botón a la derecha
            var btnObj = new GameObject("BotonAccion");
            btnObj.transform.SetParent(filaObj.transform, false);
            var rtBtn = btnObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(1f, 0.5f);
            rtBtn.anchorMax = new Vector2(1f, 0.5f);
            rtBtn.pivot = new Vector2(1f, 0.5f);
            rtBtn.anchoredPosition = new Vector2(-16f, 0f);
            rtBtn.sizeDelta = new Vector2(250f, 38f);

            var imgBtn = btnObj.AddComponent<Image>();
            imgBtn.color = new Color(0.18f, 0.25f, 0.38f, 1f);

            var btn = btnObj.AddComponent<Button>();
            if (onBotonClick != null) btn.onClick.AddListener(() => onBotonClick());

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = textoBotonInicial;
            AgregarOutline(txt);

            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.sizeDelta = Vector2.zero;

            outTxtBoton = txt;
            return btn;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AplicarAjustesVideoGuardados()
        {
            if (PlayerPrefs.HasKey("ResWidth") && PlayerPrefs.HasKey("ResHeight"))
            {
                int w = PlayerPrefs.GetInt("ResWidth");
                int h = PlayerPrefs.GetInt("ResHeight");
                bool fs = PlayerPrefs.GetInt("PantallaCompleta", Screen.fullScreen ? 1 : 0) == 1;
                FullScreenMode modo = fs ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.SetResolution(w, h, modo);
            }
            if (PlayerPrefs.HasKey("VSyncSetting"))
            {
                QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSyncSetting");
            }
        }

        private void SincronizarResolucionActual()
        {
            int wActual = PlayerPrefs.GetInt("ResWidth", Screen.width);
            int hActual = PlayerPrefs.GetInt("ResHeight", Screen.height);

            int mejorIndice = 0;
            int menorDiferencia = int.MaxValue;

            for (int i = 0; i < _resolucionesDisponibles.Length; i++)
            {
                var r = _resolucionesDisponibles[i];
                if (r.width == wActual && r.height == hActual)
                {
                    mejorIndice = i;
                    break;
                }
                int diff = Mathf.Abs(r.width - wActual) + Mathf.Abs(r.height - hActual);
                if (diff < menorDiferencia)
                {
                    menorDiferencia = diff;
                    mejorIndice = i;
                }
            }

            _indiceResolucion = mejorIndice;
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
            bool activo = PlayerPrefs.GetInt("DanoAcumulativo", 0) == 1;
            if (_txtDanoAcumulativo != null)
            {
                _txtDanoAcumulativo.text = activo ? "✔ ACTIVADO (ACUMULATIVO)" : "✖ DESACTIVADO (POR TICK)";
                _txtDanoAcumulativo.color = activo ? new Color(0.35f, 0.95f, 0.45f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }

        private void ActualizarVisualPantallaCompleta(bool? esCompleta = null)
        {
            if (_txtPantallaCompleta != null)
            {
                bool fs = esCompleta ?? Screen.fullScreen;
                _txtPantallaCompleta.text = fs ? "🖥️ PANTALLA COMPLETA" : "🪟 MODO VENTANA";
                _txtPantallaCompleta.color = fs ? new Color(0.40f, 0.85f, 1f, 1f) : new Color(1f, 0.85f, 0.35f, 1f);
            }
        }

        private void ActualizarVisualVSync()
        {
            if (_txtVSync != null)
            {
                bool vSync = QualitySettings.vSyncCount > 0;
                _txtVSync.text = vSync ? "✔ V-SYNC (ACTIVADO)" : "✖ V-SYNC (DESACTIVADO)";
                _txtVSync.color = vSync ? new Color(0.35f, 0.95f, 0.45f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }

        private void ActualizarVisualCalidad()
        {
            if (_txtCalidad != null)
            {
                string nombreCalidad = QualitySettings.names[QualitySettings.GetQualityLevel()];
                _txtCalidad.text = $"★ {nombreCalidad.ToUpperInvariant()}";
                _txtCalidad.color = new Color(1f, 0.85f, 0.35f, 1f);
            }
        }

        private void ActualizarVisualResolucion(int? ancho = null, int? alto = null)
        {
            if (_txtResolucion != null)
            {
                int w = ancho ?? (_resolucionesDisponibles != null && _resolucionesDisponibles.Length > 0 ? _resolucionesDisponibles[_indiceResolucion].width : Screen.width);
                int h = alto ?? (_resolucionesDisponibles != null && _resolucionesDisponibles.Length > 0 ? _resolucionesDisponibles[_indiceResolucion].height : Screen.height);
                _txtResolucion.text = $"📐 {w} x {h}";
                _txtResolucion.color = Color.white;
            }
        }

        private void ConstruirFilas()
        {
            if (contenedorFilas == null) return;

            foreach (Transform t in contenedorFilas) Destroy(t.gameObject);
            _botonesAccion.Clear();
            _textosAccion.Clear();

            var acciones = (AccionControl[])Enum.GetValues(typeof(AccionControl));
            foreach (var accion in acciones)
            {
                var fila = new GameObject($"Fila_{accion}");
                fila.transform.SetParent(contenedorFilas, false);
                var rtFila = fila.AddComponent<RectTransform>();
                rtFila.sizeDelta = new Vector2(740, 34);

                // Nombre de la acción
                var txtNomObj = new GameObject("NombreAccion");
                txtNomObj.transform.SetParent(fila.transform, false);
                var txtNom = txtNomObj.AddComponent<Text>();
                txtNom.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtNom.fontSize = 12;
                txtNom.fontStyle = FontStyle.Bold;
                txtNom.alignment = TextAnchor.MiddleLeft;
                txtNom.color = Color.white;
                txtNom.rectTransform.anchoredPosition = new Vector2(-150, 0);
                txtNom.rectTransform.sizeDelta = new Vector2(360, 32);
                txtNom.text = ObtenerNombreLegible(accion);
                AgregarOutline(txtNom);

                // Botón con la tecla actual
                var btnObj = new GameObject("BotonTecla");
                btnObj.transform.SetParent(fila.transform, false);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
                var rtBtn = btnObj.GetComponent<RectTransform>();
                rtBtn.anchoredPosition = new Vector2(190, 0);
                rtBtn.sizeDelta = new Vector2(220, 30);

                var btn = btnObj.AddComponent<Button>();

                var txtTeclaObj = new GameObject("TextoTecla");
                txtTeclaObj.transform.SetParent(btnObj.transform, false);
                var txtTecla = txtTeclaObj.AddComponent<Text>();
                txtTecla.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtTecla.fontSize = 12;
                txtTecla.alignment = TextAnchor.MiddleCenter;
                txtTecla.color = Color.yellow;
                txtTecla.rectTransform.sizeDelta = new Vector2(220, 30);
                AgregarOutline(txtTecla);

                AccionControl act = accion;
                btn.onClick.AddListener(() => IniciarEsperaTecla(act, txtTecla));

                _botonesAccion[accion] = btn;
                _textosAccion[accion] = txtTecla;
            }

            ActualizarTextos();
        }

        private void IniciarEsperaTecla(AccionControl accion, Text txtBoton)
        {
            _accionEsperandoTecla = accion;
            _textoBotonEsperando = txtBoton;
            txtBoton.text = "[Presiona tecla...]";
            if (textoEstado) textoEstado.text = $"Presiona cualquier tecla para '{ObtenerNombreLegible(accion)}' (ESC para cancelar)";
        }

        private void AsignarNuevaTecla(AccionControl accion, KeyCode nuevaTecla)
        {
            GestorControles.AsignarTecla(accion, nuevaTecla);
            FinalizarEsperaTecla();
            ActualizarTextos();
            if (textoEstado) textoEstado.text = $"Asignado: {ObtenerNombreLegible(accion)} -> {nuevaTecla}";
        }

        private void FinalizarEsperaTecla()
        {
            _accionEsperandoTecla = null;
            _textoBotonEsperando = null;
        }

        private void ActualizarTextos()
        {
            foreach (var kvp in _textosAccion)
            {
                KeyCode tecla = GestorControles.ObtenerTecla(kvp.Key);
                kvp.Value.text = tecla.ToString();
            }
        }

        private string ObtenerNombreLegible(AccionControl accion)
        {
            return accion switch
            {
                AccionControl.MoverArriba => "Mover Arriba (W)",
                AccionControl.MoverAbajo => "Mover Abajo (S)",
                AccionControl.MoverIzquierda => "Mover Izquierda (A)",
                AccionControl.MoverDerecha => "Mover Derecha (D)",
                AccionControl.Correr => "Correr / Sprint (Shift)",
                AccionControl.AtaquePrincipal => "Ataque Básico (Click Izquierdo)",
                AccionControl.Habilidad1 => "Habilidad 1 (Tecla E)",
                AccionControl.Habilidad2 => "Habilidad 2 (Tecla Q)",
                AccionControl.Ultimate => "Definitiva / Ultimate (Tecla R)",
                AccionControl.Pausa => "Menú / Opciones (Escape)",
                _ => accion.ToString()
            };
        }
    }
}
