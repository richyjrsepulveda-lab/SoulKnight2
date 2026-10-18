using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    public enum PestanaOpciones
    {
        Audio,
        Controles
    }

    /// <summary>
    /// Panel de Opciones unificado con pestañas:
    /// - Pestaña 1: Audio (Volumen General, Música, Efectos de sonido con sliders interactivos y prueba).
    /// - Pestaña 2: Controles (Reasignación dinámica de teclas y restauración por defecto).
    /// Accesible y configurable en la Sala (menú principal), en el Lobby y durante la Run (menú de pausa).
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
        private GameObject _panelControles;
        private Button _btnTabAudio;
        private Button _btnTabControles;
        private Image _imgTabAudio;
        private Image _imgTabControles;
        private Text _txtTabAudio;
        private Text _txtTabControles;

        // Sliders Audio
        private Slider _sliderMaster;
        private Slider _sliderMusica;
        private Slider _sliderSFX;
        private Text _txtPctMaster;
        private Text _txtPctMusica;
        private Text _txtPctSFX;

        private PestanaOpciones _pestanaActual = PestanaOpciones.Audio;

        private AccionControl? _accionEsperandoTecla = null;
        private Text _textoBotonEsperando = null;

        private readonly Dictionary<AccionControl, Button> _botonesAccion = new Dictionary<AccionControl, Button>();
        private readonly Dictionary<AccionControl, Text> _textosAccion = new Dictionary<AccionControl, Text>();

        void Awake()
        {
            Instancia = this;
        }

        public static PanelOpcionesControles ObtenerOCrear(Transform parent)
        {
            if (Instancia != null)
            {
                if (parent != null && Instancia.transform.parent != parent)
                {
                    Instancia.transform.SetParent(parent, false);
                }
                return Instancia;
            }

            var go = new GameObject("PanelOpcionesControles");
            if (parent != null) go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var imgFondo = go.AddComponent<Image>();
            imgFondo.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);

            var comp = go.AddComponent<PanelOpcionesControles>();
            comp.panelContenedor = go;

            // Ventana Principal
            var ventanaObj = new GameObject("VentanaModal");
            ventanaObj.transform.SetParent(go.transform, false);
            var rtVentana = ventanaObj.AddComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.sizeDelta = new Vector2(580f, 520f);

            var imgVentana = ventanaObj.AddComponent<Image>();
            imgVentana.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

            // Borde superior
            var barraTop = new GameObject("BarraSuperior");
            barraTop.transform.SetParent(ventanaObj.transform, false);
            var rtBarra = barraTop.AddComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 1f);
            rtBarra.anchorMax = new Vector2(1f, 1f);
            rtBarra.pivot = new Vector2(0.5f, 1f);
            rtBarra.sizeDelta = new Vector2(0f, 44f);
            var imgBarra = barraTop.AddComponent<Image>();
            imgBarra.color = new Color(0.14f, 0.18f, 0.28f, 1f);

            // Título
            var titObj = new GameObject("Titulo");
            titObj.transform.SetParent(barraTop.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 18;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTit.text = "⚙️ CONFIGURACIÓN Y OPCIONES";
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
            rtTabs.anchoredPosition = new Vector2(0f, -54f);
            rtTabs.sizeDelta = new Vector2(520f, 40f);

            // Botón Tab Audio
            comp._btnTabAudio = CrearBotonTab(barraTabs.transform, "TabAudio", "🔊 AUDIO", new Vector2(-135f, 0f), out comp._imgTabAudio, out comp._txtTabAudio);
            comp._btnTabControles = CrearBotonTab(barraTabs.transform, "TabControles", "🎮 CONTROLES", new Vector2(135f, 0f), out comp._imgTabControles, out comp._txtTabControles);

            // ==================== PANEL AUDIO ====================
            comp._panelAudio = new GameObject("PanelAudio");
            comp._panelAudio.transform.SetParent(ventanaObj.transform, false);
            var rtAudio = comp._panelAudio.AddComponent<RectTransform>();
            rtAudio.anchorMin = new Vector2(0.5f, 0.5f);
            rtAudio.anchorMax = new Vector2(0.5f, 0.5f);
            rtAudio.pivot = new Vector2(0.5f, 0.5f);
            rtAudio.anchoredPosition = new Vector2(0f, -5f);
            rtAudio.sizeDelta = new Vector2(520f, 320f);

            // Subtítulo Audio
            var subAudio = new GameObject("SubtituloAudio");
            subAudio.transform.SetParent(comp._panelAudio.transform, false);
            var txtSubAudio = subAudio.AddComponent<Text>();
            txtSubAudio.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSubAudio.fontSize = 11;
            txtSubAudio.alignment = TextAnchor.MiddleCenter;
            txtSubAudio.color = new Color(0.70f, 0.80f, 0.95f, 1f);
            txtSubAudio.text = "Ajusta los niveles de volumen. Se guardan y aplican al instante.";
            var rtSubAudio = subAudio.GetComponent<RectTransform>();
            rtSubAudio.anchorMin = new Vector2(0.5f, 1f);
            rtSubAudio.anchorMax = new Vector2(0.5f, 1f);
            rtSubAudio.pivot = new Vector2(0.5f, 1f);
            rtSubAudio.anchoredPosition = new Vector2(0f, -10f);
            rtSubAudio.sizeDelta = new Vector2(500f, 24f);

            // Contenedor Sliders
            var contSliders = new GameObject("ContenedorSliders");
            contSliders.transform.SetParent(comp._panelAudio.transform, false);
            var rtContSliders = contSliders.AddComponent<RectTransform>();
            rtContSliders.anchorMin = new Vector2(0.5f, 0.5f);
            rtContSliders.anchorMax = new Vector2(0.5f, 0.5f);
            rtContSliders.pivot = new Vector2(0.5f, 0.5f);
            rtContSliders.anchoredPosition = new Vector2(0f, 15f);
            rtContSliders.sizeDelta = new Vector2(500f, 190f);

            var vlgSliders = contSliders.AddComponent<VerticalLayoutGroup>();
            vlgSliders.childAlignment = TextAnchor.UpperCenter;
            vlgSliders.spacing = 16f;
            vlgSliders.childControlWidth = false;
            vlgSliders.childControlHeight = false;

            comp._sliderMaster = CrearSliderFila(contSliders.transform, "SliderMaster", "Volumen General", GestorAudio.Instancia.VolumenMaster,
                (val) => GestorAudio.Instancia.SetVolumenMaster(val), out comp._txtPctMaster);

            comp._sliderMusica = CrearSliderFila(contSliders.transform, "SliderMusica", "Música de Fondo", GestorAudio.Instancia.VolumenMusica,
                (val) => GestorAudio.Instancia.SetVolumenMusica(val), out comp._txtPctMusica);

            comp._sliderSFX = CrearSliderFila(contSliders.transform, "SliderSFX", "Efectos (SFX)", GestorAudio.Instancia.VolumenSFX,
                (val) => GestorAudio.Instancia.SetVolumenSFX(val), out comp._txtPctSFX);

            // Botón Probar Sonido
            var btnProbarObj = new GameObject("BotonProbarSonido");
            btnProbarObj.transform.SetParent(comp._panelAudio.transform, false);
            var imgProbar = btnProbarObj.AddComponent<Image>();
            imgProbar.color = new Color(0.18f, 0.35f, 0.55f, 1f);
            var rtProbar = btnProbarObj.GetComponent<RectTransform>();
            rtProbar.anchorMin = new Vector2(0.5f, 0f);
            rtProbar.anchorMax = new Vector2(0.5f, 0f);
            rtProbar.pivot = new Vector2(0.5f, 0f);
            rtProbar.anchoredPosition = new Vector2(0f, 18f);
            rtProbar.sizeDelta = new Vector2(240f, 36f);
            var btnProbar = btnProbarObj.AddComponent<Button>();

            var txtProbarObj = new GameObject("Texto");
            txtProbarObj.transform.SetParent(btnProbarObj.transform, false);
            var txtProbar = txtProbarObj.AddComponent<Text>();
            txtProbar.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtProbar.fontSize = 11;
            txtProbar.fontStyle = FontStyle.Bold;
            txtProbar.alignment = TextAnchor.MiddleCenter;
            txtProbar.color = Color.white;
            txtProbar.text = "🎯 PROBAR EFECTO DE SONIDO";
            var rtTxtProbar = txtProbarObj.GetComponent<RectTransform>();
            rtTxtProbar.anchorMin = Vector2.zero;
            rtTxtProbar.anchorMax = Vector2.one;
            rtTxtProbar.sizeDelta = Vector2.zero;

            btnProbar.onClick.AddListener(() =>
            {
                GestorAudio.PlaySFX(TipoSFX.Mejora, 1.0f);
            });

            // ==================== PANEL CONTROLES ====================
            comp._panelControles = new GameObject("PanelControles");
            comp._panelControles.transform.SetParent(ventanaObj.transform, false);
            var rtContr = comp._panelControles.AddComponent<RectTransform>();
            rtContr.anchorMin = new Vector2(0.5f, 0.5f);
            rtContr.anchorMax = new Vector2(0.5f, 0.5f);
            rtContr.pivot = new Vector2(0.5f, 0.5f);
            rtContr.anchoredPosition = new Vector2(0f, -5f);
            rtContr.sizeDelta = new Vector2(520f, 320f);

            // Subtítulo Controles
            var estObj = new GameObject("TextoEstado");
            estObj.transform.SetParent(comp._panelControles.transform, false);
            var txtEst = estObj.AddComponent<Text>();
            txtEst.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtEst.fontSize = 11;
            txtEst.alignment = TextAnchor.MiddleCenter;
            txtEst.color = new Color(0.75f, 0.85f, 1f, 1f);
            txtEst.text = "Haz clic en una tecla para cambiarla. Presiona ESC para cancelar.";
            var rtEst = estObj.GetComponent<RectTransform>();
            rtEst.anchorMin = new Vector2(0.5f, 1f);
            rtEst.anchorMax = new Vector2(0.5f, 1f);
            rtEst.pivot = new Vector2(0.5f, 1f);
            rtEst.anchoredPosition = new Vector2(0f, -6f);
            rtEst.sizeDelta = new Vector2(500f, 24f);
            comp.textoEstado = txtEst;

            // Scroll / Contenedor Filas Controles
            var contFilasObj = new GameObject("ContenedorFilas");
            contFilasObj.transform.SetParent(comp._panelControles.transform, false);
            var vlg = contFilasObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 4f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            var rtFilas = contFilasObj.GetComponent<RectTransform>();
            rtFilas.anchorMin = new Vector2(0.5f, 0.5f);
            rtFilas.anchorMax = new Vector2(0.5f, 0.5f);
            rtFilas.pivot = new Vector2(0.5f, 0.5f);
            rtFilas.anchoredPosition = new Vector2(0f, 10f);
            rtFilas.sizeDelta = new Vector2(480f, 220f);
            comp.contenedorFilas = contFilasObj.transform;

            // Botón Restaurar Controles
            var btnRestObj = new GameObject("BotonRestaurar");
            btnRestObj.transform.SetParent(comp._panelControles.transform, false);
            var imgRest = btnRestObj.AddComponent<Image>();
            imgRest.color = new Color(0.45f, 0.22f, 0.22f, 1f);
            var rtRest = btnRestObj.GetComponent<RectTransform>();
            rtRest.anchorMin = new Vector2(0.5f, 0f);
            rtRest.anchorMax = new Vector2(0.5f, 0f);
            rtRest.pivot = new Vector2(0.5f, 0f);
            rtRest.anchoredPosition = new Vector2(0f, 14f);
            rtRest.sizeDelta = new Vector2(230f, 32f);
            comp.botonRestaurar = btnRestObj.AddComponent<Button>();

            var txtRestObj = new GameObject("Texto");
            txtRestObj.transform.SetParent(btnRestObj.transform, false);
            var txtRest = txtRestObj.AddComponent<Text>();
            txtRest.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtRest.fontSize = 11;
            txtRest.fontStyle = FontStyle.Bold;
            txtRest.alignment = TextAnchor.MiddleCenter;
            txtRest.color = Color.white;
            txtRest.text = "↺ RESTAURAR CONTROLES";
            var rtTxtRest = txtRestObj.GetComponent<RectTransform>();
            rtTxtRest.anchorMin = Vector2.zero;
            rtTxtRest.anchorMax = Vector2.one;
            rtTxtRest.sizeDelta = Vector2.zero;

            // ==================== BOTÓN CERRAR GLOBAL ====================
            var btnCerrarObj = new GameObject("BotonCerrar");
            btnCerrarObj.transform.SetParent(ventanaObj.transform, false);
            var imgCerrar = btnCerrarObj.AddComponent<Image>();
            imgCerrar.color = new Color(0.18f, 0.48f, 0.28f, 1f);
            var rtCerrar = btnCerrarObj.GetComponent<RectTransform>();
            rtCerrar.anchorMin = new Vector2(0.5f, 0f);
            rtCerrar.anchorMax = new Vector2(0.5f, 0f);
            rtCerrar.pivot = new Vector2(0.5f, 0f);
            rtCerrar.anchoredPosition = new Vector2(0f, 16f);
            rtCerrar.sizeDelta = new Vector2(260f, 38f);
            comp.botonCerrar = btnCerrarObj.AddComponent<Button>();

            var txtCerrarObj = new GameObject("Texto");
            txtCerrarObj.transform.SetParent(btnCerrarObj.transform, false);
            var txtCerrar = txtCerrarObj.AddComponent<Text>();
            txtCerrar.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtCerrar.fontSize = 12;
            txtCerrar.fontStyle = FontStyle.Bold;
            txtCerrar.alignment = TextAnchor.MiddleCenter;
            txtCerrar.color = Color.white;
            txtCerrar.text = "✔️ CERRAR Y GUARDAR";
            var rtTxtCerrar = txtCerrarObj.GetComponent<RectTransform>();
            rtTxtCerrar.anchorMin = Vector2.zero;
            rtTxtCerrar.anchorMax = Vector2.one;
            rtTxtCerrar.sizeDelta = Vector2.zero;

            // Listeners
            comp._btnTabAudio.onClick.AddListener(() => comp.CambiarPestana(PestanaOpciones.Audio));
            comp._btnTabControles.onClick.AddListener(() => comp.CambiarPestana(PestanaOpciones.Controles));
            comp.botonCerrar.onClick.AddListener(comp.Cerrar);
            comp.botonRestaurar.onClick.AddListener(comp.RestaurarPorDefecto);

            comp.ConstruirFilas();
            comp.CambiarPestana(PestanaOpciones.Audio);

            go.SetActive(false);
            return comp;
        }

        private static Button CrearBotonTab(Transform parent, string nombre, string texto, Vector2 pos, out Image img, out Text txt)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 36f);

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
            rtFila.sizeDelta = new Vector2(480f, 38f);

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
            var rtLbl = lblObj.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 0.5f);
            rtLbl.anchorMax = new Vector2(0f, 0.5f);
            rtLbl.pivot = new Vector2(0f, 0.5f);
            rtLbl.anchoredPosition = new Vector2(8f, 0f);
            rtLbl.sizeDelta = new Vector2(160f, 34f);

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
            var rtPct = pctObj.GetComponent<RectTransform>();
            rtPct.anchorMin = new Vector2(1f, 0.5f);
            rtPct.anchorMax = new Vector2(1f, 0.5f);
            rtPct.pivot = new Vector2(1f, 0.5f);
            rtPct.anchoredPosition = new Vector2(-8f, 0f);
            rtPct.sizeDelta = new Vector2(55f, 34f);
            txtPorcentaje = txtPct;

            // Slider Component
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(filaObj.transform, false);
            var rtSlider = sliderObj.AddComponent<RectTransform>();
            rtSlider.anchorMin = new Vector2(0f, 0.5f);
            rtSlider.anchorMax = new Vector2(0f, 0.5f);
            rtSlider.pivot = new Vector2(0f, 0.5f);
            rtSlider.anchoredPosition = new Vector2(175f, 0f);
            rtSlider.sizeDelta = new Vector2(235f, 20f);

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
            if (_panelControles != null) _panelControles.SetActive(pestana == PestanaOpciones.Controles);

            if (_imgTabAudio != null)
            {
                _imgTabAudio.color = pestana == PestanaOpciones.Audio ? new Color(0.25f, 0.55f, 0.85f, 1f) : new Color(0.14f, 0.18f, 0.26f, 1f);
            }
            if (_imgTabControles != null)
            {
                _imgTabControles.color = pestana == PestanaOpciones.Controles ? new Color(0.25f, 0.55f, 0.85f, 1f) : new Color(0.14f, 0.18f, 0.26f, 1f);
            }
        }

        void Start()
        {
            if (botonCerrar) botonCerrar.onClick.AddListener(Cerrar);
            if (botonRestaurar) botonRestaurar.onClick.AddListener(RestaurarPorDefecto);
            ConstruirFilas();
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
            else if (panelContenedor != null && panelContenedor.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
            }
        }

        public void Abrir()
        {
            if (panelContenedor) panelContenedor.SetActive(true);

            // Siempre abrir con Audio como primera opción seleccionada
            CambiarPestana(PestanaOpciones.Audio);

            // Sincronizar sliders con volúmenes reales
            if (_sliderMaster != null) _sliderMaster.value = GestorAudio.Instancia.VolumenMaster;
            if (_sliderMusica != null) _sliderMusica.value = GestorAudio.Instancia.VolumenMusica;
            if (_sliderSFX != null) _sliderSFX.value = GestorAudio.Instancia.VolumenSFX;

            if (_txtPctMaster != null) _txtPctMaster.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenMaster * 100f)}%";
            if (_txtPctMusica != null) _txtPctMusica.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenMusica * 100f)}%";
            if (_txtPctSFX != null) _txtPctSFX.text = $"{Mathf.RoundToInt(GestorAudio.Instancia.VolumenSFX * 100f)}%";

            ActualizarTextos();
        }

        public void Cerrar()
        {
            FinalizarEsperaTecla();
            if (panelContenedor) panelContenedor.SetActive(false);
        }

        private void RestaurarPorDefecto()
        {
            GestorControles.RestaurarPorDefecto();
            ActualizarTextos();
            if (textoEstado) textoEstado.text = "¡Controles restaurados por defecto!";
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
                rtFila.sizeDelta = new Vector2(460, 32);

                // Nombre de la acción
                var txtNomObj = new GameObject("NombreAccion");
                txtNomObj.transform.SetParent(fila.transform, false);
                var txtNom = txtNomObj.AddComponent<Text>();
                txtNom.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtNom.fontSize = 11;
                txtNom.alignment = TextAnchor.MiddleLeft;
                txtNom.color = Color.white;
                txtNom.rectTransform.anchoredPosition = new Vector2(-110, 0);
                txtNom.rectTransform.sizeDelta = new Vector2(220, 30);
                txtNom.text = ObtenerNombreLegible(accion);

                // Botón con la tecla actual
                var btnObj = new GameObject("BotonTecla");
                btnObj.transform.SetParent(fila.transform, false);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
                var rtBtn = btnObj.GetComponent<RectTransform>();
                rtBtn.anchoredPosition = new Vector2(130, 0);
                rtBtn.sizeDelta = new Vector2(140, 28);

                var btn = btnObj.AddComponent<Button>();

                var txtTeclaObj = new GameObject("TextoTecla");
                txtTeclaObj.transform.SetParent(btnObj.transform, false);
                var txtTecla = txtTeclaObj.AddComponent<Text>();
                txtTecla.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtTecla.fontSize = 11;
                txtTecla.alignment = TextAnchor.MiddleCenter;
                txtTecla.color = Color.yellow;
                txtTecla.rectTransform.sizeDelta = new Vector2(140, 28);

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
                AccionControl.MoverArriba => "Mover Arriba",
                AccionControl.MoverAbajo => "Mover Abajo",
                AccionControl.MoverIzquierda => "Mover Izquierda",
                AccionControl.MoverDerecha => "Mover Derecha",
                AccionControl.Correr => "Correr (Sprint)",
                AccionControl.AtaquePrincipal => "Ataque Principal",
                AccionControl.Habilidad1 => "Habilidad 1 (E)",
                AccionControl.Habilidad2 => "Habilidad 2 (Q)",
                AccionControl.Ultimate => "Ultimate (R)",
                AccionControl.Pausa => "Pausa (Esc)",
                _ => accion.ToString()
            };
        }
    }
}
