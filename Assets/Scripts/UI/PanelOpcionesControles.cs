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

        // Comandos Admin
        private InputField _inputCodigoAdmin;
        private Text _txtFeedbackComando;

        private PestanaOpciones _pestanaActual = PestanaOpciones.Audio;

        private AccionControl? _accionEsperandoTecla = null;
        private Text _textoBotonEsperando = null;

        private readonly Dictionary<AccionControl, Button> _botonesAccion = new Dictionary<AccionControl, Button>();
        private readonly Dictionary<AccionControl, Text> _textosAccion = new Dictionary<AccionControl, Text>();

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
            if (_panelAudio != null && _panelControles != null) return;

            // Limpiar hijos residuales antiguos para garantizar diseño renovado espacioso
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            panelContenedor = gameObject;

            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var imgFondo = GetComponent<Image>();
            if (imgFondo == null) imgFondo = gameObject.AddComponent<Image>();
            imgFondo.color = new Color(0.03f, 0.04f, 0.07f, 0.95f); // Fondo translúcido oscuro

            // ==================== VENTANA PRINCIPAL AMPLIADA ====================
            var ventanaObj = new GameObject("VentanaModal");
            ventanaObj.transform.SetParent(transform, false);
            var rtVentana = ventanaObj.AddComponent<RectTransform>();
            rtVentana.anchorMin = new Vector2(0.5f, 0.5f);
            rtVentana.anchorMax = new Vector2(0.5f, 0.5f);
            rtVentana.pivot = new Vector2(0.5f, 0.5f);
            rtVentana.sizeDelta = new Vector2(860f, 660f); // Tamaño generoso y no apretado

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

            // Título
            var titObj = new GameObject("Titulo");
            titObj.transform.SetParent(barraTop.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 17;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTit.text = "⚙️ MENÚ DE OPCIONES & CONFIGURACIÓN";
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
            rtTabs.sizeDelta = new Vector2(800f, 42f);

            _btnTabAudio = CrearBotonTab(barraTabs.transform, "TabAudio", "🔊 AUDIO Y CÓDIGOS ADMIN", new Vector2(-205f, 0f), 390f, out _imgTabAudio, out _txtTabAudio);
            _btnTabControles = CrearBotonTab(barraTabs.transform, "TabControles", "🎮 CONTROLES DE JUEGO", new Vector2(205f, 0f), 390f, out _imgTabControles, out _txtTabControles);

            // ==================== PANEL AUDIO Y COMANDOS ADMIN ====================
            _panelAudio = new GameObject("PanelAudio");
            _panelAudio.transform.SetParent(ventanaObj.transform, false);
            var rtAudio = _panelAudio.AddComponent<RectTransform>();
            rtAudio.anchorMin = new Vector2(0.5f, 0.5f);
            rtAudio.anchorMax = new Vector2(0.5f, 0.5f);
            rtAudio.pivot = new Vector2(0.5f, 0.5f);
            rtAudio.anchoredPosition = new Vector2(0f, -12f);
            rtAudio.sizeDelta = new Vector2(800f, 490f);

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
            var rtSubAudio = subAudio.GetComponent<RectTransform>();
            rtSubAudio.anchorMin = new Vector2(0f, 1f);
            rtSubAudio.anchorMax = new Vector2(1f, 1f);
            rtSubAudio.pivot = new Vector2(0f, 1f);
            rtSubAudio.anchoredPosition = new Vector2(10f, -6f);
            rtSubAudio.sizeDelta = new Vector2(0f, 22f);

            // Contenedor Sliders
            var contSliders = new GameObject("ContenedorSliders");
            contSliders.transform.SetParent(_panelAudio.transform, false);
            var rtContSliders = contSliders.AddComponent<RectTransform>();
            rtContSliders.anchorMin = new Vector2(0.5f, 1f);
            rtContSliders.anchorMax = new Vector2(0.5f, 1f);
            rtContSliders.pivot = new Vector2(0.5f, 1f);
            rtContSliders.anchoredPosition = new Vector2(0f, -32f);
            rtContSliders.sizeDelta = new Vector2(780f, 125f);

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
            rtProbar.anchoredPosition = new Vector2(-10f, -162f);
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
            rtDiv.anchoredPosition = new Vector2(0f, -202f);
            rtDiv.sizeDelta = new Vector2(780f, 2f);
            var imgDiv = divisorObj.AddComponent<Image>();
            imgDiv.color = new Color(0.20f, 0.26f, 0.38f, 0.8f);

            // ==================== SECCIÓN DE COMANDOS / CÓDIGOS DE ADMIN ====================
            var titAdmin = new GameObject("TituloAdmin");
            titAdmin.transform.SetParent(_panelAudio.transform, false);
            var txtTitAdmin = titAdmin.AddComponent<Text>();
            txtTitAdmin.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTitAdmin.fontSize = 12;
            txtTitAdmin.fontStyle = FontStyle.Bold;
            txtTitAdmin.alignment = TextAnchor.MiddleLeft;
            txtTitAdmin.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTitAdmin.text = "⚡ CONSOLA DE CÓDIGOS & COMANDOS DE ADMIN / DEV";
            var rtTitAdmin = titAdmin.GetComponent<RectTransform>();
            rtTitAdmin.anchorMin = new Vector2(0f, 1f);
            rtTitAdmin.anchorMax = new Vector2(1f, 1f);
            rtTitAdmin.pivot = new Vector2(0f, 1f);
            rtTitAdmin.anchoredPosition = new Vector2(10f, -212f);
            rtTitAdmin.sizeDelta = new Vector2(0f, 22f);

            var descAdmin = new GameObject("DescAdmin");
            descAdmin.transform.SetParent(_panelAudio.transform, false);
            var txtDescAdmin = descAdmin.AddComponent<Text>();
            txtDescAdmin.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtDescAdmin.fontSize = 10;
            txtDescAdmin.alignment = TextAnchor.MiddleLeft;
            txtDescAdmin.color = new Color(0.70f, 0.78f, 0.90f, 1f);
            txtDescAdmin.text = "Haz clic en los accesos rápidos o escribe un código manual para testear instantáneamente:";
            var rtDescAdmin = descAdmin.GetComponent<RectTransform>();
            rtDescAdmin.anchorMin = new Vector2(0f, 1f);
            rtDescAdmin.anchorMax = new Vector2(1f, 1f);
            rtDescAdmin.pivot = new Vector2(0f, 1f);
            rtDescAdmin.anchoredPosition = new Vector2(10f, -234f);
            rtDescAdmin.sizeDelta = new Vector2(0f, 18f);

            // Botones de Acceso Rápido Fila 1
            CrearBotonCheat(_panelAudio.transform, "BtnCheatLvl5", "⭐ +5 NIVELES", new Vector2(-290f, -270f), new Color(0.18f, 0.40f, 0.65f, 1f), () => EjecutarCheat("lvl 5"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatMaxLvl", "👑 MAX NIVEL (10)", new Vector2(-95f, -270f), new Color(0.20f, 0.45f, 0.75f, 1f), () => EjecutarCheat("maxlvl"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatUnlock", "🔓 UNLOCK ALL (ALL SKILLS)", new Vector2(100f, -270f), new Color(0.65f, 0.45f, 0.15f, 1f), () => EjecutarCheat("unlockall"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatMaestria", "✨ +1000 MAESTRÍA", new Vector2(295f, -270f), new Color(0.55f, 0.35f, 0.65f, 1f), () => EjecutarCheat("maestria 1000"));

            // Botones de Acceso Rápido Fila 2
            CrearBotonCheat(_panelAudio.transform, "BtnCheatGod", "❤️ MODO DIOS (9999 HP)", new Vector2(-290f, -310f), new Color(0.75f, 0.20f, 0.25f, 1f), () => EjecutarCheat("god"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatKill", "💀 MATAR ENEMIGOS", new Vector2(-95f, -310f), new Color(0.45f, 0.18f, 0.22f, 1f), () => EjecutarCheat("killall"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatSkip", "🌀 SALTAR NIVEL (Skip)", new Vector2(100f, -310f), new Color(0.20f, 0.50f, 0.45f, 1f), () => EjecutarCheat("skip"));
            CrearBotonCheat(_panelAudio.transform, "BtnCheatNoCD", "⚡ RESET COOLDOWNS", new Vector2(295f, -310f), new Color(0.25f, 0.45f, 0.30f, 1f), () => EjecutarCheat("resetcd"));

            // Barra de Input de Texto
            var inputBarObj = new GameObject("BarraInputCodigo");
            inputBarObj.transform.SetParent(_panelAudio.transform, false);
            var rtInputBar = inputBarObj.AddComponent<RectTransform>();
            rtInputBar.anchorMin = new Vector2(0.5f, 1f);
            rtInputBar.anchorMax = new Vector2(0.5f, 1f);
            rtInputBar.pivot = new Vector2(0.5f, 1f);
            rtInputBar.anchoredPosition = new Vector2(0f, -355f);
            rtInputBar.sizeDelta = new Vector2(780f, 38f);

            // InputField
            var inputObj = new GameObject("InputFieldCodigo");
            inputObj.transform.SetParent(inputBarObj.transform, false);
            var rtInput = inputObj.AddComponent<RectTransform>();
            rtInput.anchorMin = new Vector2(0f, 0.5f);
            rtInput.anchorMax = new Vector2(0f, 0.5f);
            rtInput.pivot = new Vector2(0f, 0.5f);
            rtInput.anchoredPosition = new Vector2(0f, 0f);
            rtInput.sizeDelta = new Vector2(560f, 36f);

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
            txtPlaceholder.text = "Escribe un código o comando (ej: unlockall, nivel 10, god, killall, ayuda)...";
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
                    EjecutarCheat(val);
                }
            });

            // Botón Ejecutar Código
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
            txtBtnEj.text = "⚡ CANJEAR / EJECUTAR";
            var rtTxtBtnEj = txtBtnEjObj.GetComponent<RectTransform>();
            rtTxtBtnEj.anchorMin = Vector2.zero;
            rtTxtBtnEj.anchorMax = Vector2.one;
            rtTxtBtnEj.sizeDelta = Vector2.zero;

            btnEj.onClick.AddListener(() =>
            {
                if (_inputCodigoAdmin != null)
                {
                    EjecutarCheat(_inputCodigoAdmin.text);
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
            _txtFeedbackComando.text = "💡 Escribe 'ayuda' para ver la lista completa de comandos o pulsa los botones de arriba.";
            var rtFeedback = feedbackObj.GetComponent<RectTransform>();
            rtFeedback.anchorMin = new Vector2(0.5f, 1f);
            rtFeedback.anchorMax = new Vector2(0.5f, 1f);
            rtFeedback.pivot = new Vector2(0.5f, 1f);
            rtFeedback.anchoredPosition = new Vector2(0f, -405f);
            rtFeedback.sizeDelta = new Vector2(780f, 40f);

            // ==================== PANEL CONTROLES AMPLIADO ====================
            _panelControles = new GameObject("PanelControles");
            _panelControles.transform.SetParent(ventanaObj.transform, false);
            var rtContr = _panelControles.AddComponent<RectTransform>();
            rtContr.anchorMin = new Vector2(0.5f, 0.5f);
            rtContr.anchorMax = new Vector2(0.5f, 0.5f);
            rtContr.pivot = new Vector2(0.5f, 0.5f);
            rtContr.anchoredPosition = new Vector2(0f, -12f);
            rtContr.sizeDelta = new Vector2(800f, 490f);

            // Subtítulo Controles
            var estObj = new GameObject("TextoEstado");
            estObj.transform.SetParent(_panelControles.transform, false);
            var txtEst = estObj.AddComponent<Text>();
            txtEst.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtEst.fontSize = 12;
            txtEst.alignment = TextAnchor.MiddleCenter;
            txtEst.color = new Color(0.75f, 0.85f, 1f, 1f);
            txtEst.text = "Haz clic en una tecla para cambiarla. Presiona ESC para cancelar la espera.";
            var rtEst = estObj.GetComponent<RectTransform>();
            rtEst.anchorMin = new Vector2(0.5f, 1f);
            rtEst.anchorMax = new Vector2(0.5f, 1f);
            rtEst.pivot = new Vector2(0.5f, 1f);
            rtEst.anchoredPosition = new Vector2(0f, -8f);
            rtEst.sizeDelta = new Vector2(760f, 24f);
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
            rtFilas.sizeDelta = new Vector2(760f, 430f);
            contenedorFilas = contFilasObj.transform;

            // ==================== BARRA INFERIOR DE ACCIÓN GLOBAL ====================
            var barraInferior = new GameObject("BarraInferior");
            barraInferior.transform.SetParent(ventanaObj.transform, false);
            var rtBarraInf = barraInferior.AddComponent<RectTransform>();
            rtBarraInf.anchorMin = new Vector2(0.5f, 0f);
            rtBarraInf.anchorMax = new Vector2(0.5f, 0f);
            rtBarraInf.pivot = new Vector2(0.5f, 0f);
            rtBarraInf.anchoredPosition = new Vector2(0f, 18f);
            rtBarraInf.sizeDelta = new Vector2(800f, 44f);

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
            var rtTxtCerrar = txtCerrarObj.GetComponent<RectTransform>();
            rtTxtCerrar.anchorMin = Vector2.zero;
            rtTxtCerrar.anchorMax = Vector2.one;
            rtTxtCerrar.sizeDelta = Vector2.zero;

            // Listeners
            _btnTabAudio.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Audio));
            _btnTabControles.onClick.AddListener(() => CambiarPestana(PestanaOpciones.Controles));
            botonCerrar.onClick.AddListener(Cerrar);
            botonRestaurar.onClick.AddListener(RestaurarPorDefecto);

            ConstruirFilas();
            CambiarPestana(PestanaOpciones.Audio);
        }

        private void EjecutarCheat(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;
            string resultado = ComandosAdmin.EjecutarComando(codigo);
            if (_txtFeedbackComando != null)
            {
                _txtFeedbackComando.text = resultado;
                _txtFeedbackComando.color = resultado.StartsWith("✓") 
                    ? new Color(0.40f, 1f, 0.55f, 1f) 
                    : new Color(1f, 0.55f, 0.55f, 1f);
            }
            if (_inputCodigoAdmin != null)
            {
                _inputCodigoAdmin.text = "";
            }
            GestorAudio.PlaySFX(TipoSFX.Mejora, 0.8f);
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
            rt.sizeDelta = new Vector2(185f, 32f);

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
            rtFila.sizeDelta = new Vector2(760f, 34f);

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
            else if (panelContenedor != null && panelContenedor.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
            }
        }

        public void Abrir()
        {
            AsegurarInterfazCompleta();

            if (panelContenedor) panelContenedor.SetActive(true);
            else gameObject.SetActive(true);

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
            else gameObject.SetActive(false);
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
