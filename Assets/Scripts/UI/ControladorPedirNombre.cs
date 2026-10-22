using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Controlador para la pantalla de cambiar/pedir nombre (PedirNombre.unity):
    /// Rediseñado 1:1 con la especificación visual oficial de Name.dc.html:
    /// - Canvas 1920x1080 responsivo.
    /// - Mosaico de ladrillos + overlay oscuro.
    /// - Panel central 900x588 con remaches dorados.
    /// - Cabecera con huellas doradas y título CAMBIAR NOMBRE.
    /// - Input field de 88px con borde dorado y contador de caracteres (X/16).
    /// - Botones de acción CANCELAR (Piedra) y GUARDAR (Verde con palomita).
    /// </summary>
    public class ControladorPedirNombre : MonoBehaviour
    {
        public InputField inputNombre;
        public Button botonContinuar;

        private Text _txtContador;

        void Awake()
        {
            AsegurarInterfazCompleta();
        }

        void Start()
        {
            GestorAudio.PlayBGM(TipoMusica.Sala);

            string nombreActual = (Global.Instancia != null && Global.Instancia.HayNombreGuardado && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
                ? Global.Instancia.NombreLocal.Trim()
                : "RichardLap";

            if (inputNombre != null)
            {
                inputNombre.text = nombreActual;
                ActualizarContador(nombreActual);
                inputNombre.ActivateInputField();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GuardarYContinuar();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cancelar();
            }
        }

        private void ActualizarContador(string texto)
        {
            if (_txtContador != null)
            {
                int len = string.IsNullOrEmpty(texto) ? 0 : texto.Length;
                _txtContador.text = $"{len}/16";
            }
        }

        private void GuardarYContinuar()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            string nombre = inputNombre != null ? inputNombre.text.Trim() : "";
            if (string.IsNullOrEmpty(nombre))
            {
                nombre = (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
                    ? Global.Instancia.NombreLocal.Trim()
                    : "Jugador";
            }

            if (Global.Instancia != null)
            {
                Global.Instancia.GuardarNombre(nombre);
            }

            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }

        private void Cancelar()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }

        private void AsegurarInterfazCompleta()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                var cObj = new GameObject("CanvasNombre");
                canvas = cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cObj.AddComponent<CanvasScaler>();
                cObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(cObj.transform, false);
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Limpiar hijos existentes del transform del controlador
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var rtRoot = GetComponent<RectTransform>();
            if (rtRoot != null)
            {
                rtRoot.anchorMin = Vector2.zero;
                rtRoot.anchorMax = Vector2.one;
                rtRoot.sizeDelta = Vector2.zero;
            }

            // Fondo ladrillos
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

            // Overlay oscuro rgba(8, 4, 20, 0.62)
            var overObj = new GameObject("CapaOscuraFondo");
            overObj.transform.SetParent(transform, false);
            var rtOver = overObj.AddComponent<RectTransform>();
            rtOver.anchorMin = Vector2.zero;
            rtOver.anchorMax = Vector2.one;
            rtOver.sizeDelta = Vector2.zero;
            var imgOver = overObj.AddComponent<Image>();
            imgOver.color = new Color(8f / 255f, 4f / 255f, 20f / 255f, 0.62f);
            imgOver.raycastTarget = true;

            // Panel Modal 900 x 588 px
            var panelObj = new GameObject("PanelModal");
            panelObj.transform.SetParent(transform, false);
            var rtPan = panelObj.AddComponent<RectTransform>();
            rtPan.anchorMin = new Vector2(0.5f, 0.5f);
            rtPan.anchorMax = new Vector2(0.5f, 0.5f);
            rtPan.pivot = new Vector2(0.5f, 0.5f);
            rtPan.anchoredPosition = Vector2.zero;
            rtPan.sizeDelta = new Vector2(900f, 588f);

            var imgPan = panelObj.AddComponent<Image>();
            imgPan.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgPan.type = Image.Type.Sliced;

            // Remaches en esquinas inferiores a 16px
            CrearRemache(panelObj.transform, new Vector2(0f, 0f), new Vector2(16f, 16f));
            CrearRemache(panelObj.transform, new Vector2(1f, 0f), new Vector2(-16f, 16f));

            // Cabecera (Alto 124px)
            var cabObj = new GameObject("CabeceraModal");
            cabObj.transform.SetParent(panelObj.transform, false);
            var rtCab = cabObj.AddComponent<RectTransform>();
            rtCab.anchorMin = new Vector2(0f, 1f);
            rtCab.anchorMax = new Vector2(1f, 1f);
            rtCab.pivot = new Vector2(0.5f, 1f);
            rtCab.anchoredPosition = Vector2.zero;
            rtCab.sizeDelta = new Vector2(0f, 124f);

            var imgCab = cabObj.AddComponent<Image>();
            imgCab.color = EstiloUIUX.ColCabecera;

            // Borde inferior 6px
            var lineaBorde = new GameObject("BordeInferiorCabecera");
            lineaBorde.transform.SetParent(cabObj.transform, false);
            var rtLin = lineaBorde.AddComponent<RectTransform>();
            rtLin.anchorMin = new Vector2(0f, 0f);
            rtLin.anchorMax = new Vector2(1f, 0f);
            rtLin.pivot = new Vector2(0.5f, 0f);
            rtLin.sizeDelta = new Vector2(0f, 6f);
            var imgLin = lineaBorde.AddComponent<Image>();
            imgLin.color = EstiloUIUX.ColContornoUniversal;

            var contCab = new GameObject("ContenidoCabecera");
            contCab.transform.SetParent(cabObj.transform, false);
            var rtContCab = contCab.AddComponent<RectTransform>();
            rtContCab.anchorMin = Vector2.zero;
            rtContCab.anchorMax = Vector2.one;
            rtContCab.sizeDelta = Vector2.zero;

            var hlgCab = contCab.AddComponent<HorizontalLayoutGroup>();
            hlgCab.childAlignment = TextAnchor.MiddleCenter;
            hlgCab.spacing = 32f;
            hlgCab.childControlWidth = false;
            hlgCab.childControlHeight = false;

            CrearIconoHuella(contCab.transform, new Vector2(45f, 40f));

            var txtTitModal = new GameObject("Titulo").AddComponent<Text>();
            txtTitModal.transform.SetParent(contCab.transform, false);
            txtTitModal.font = FuenteUtil.ObtenerFuentePixelify();
            txtTitModal.fontSize = 56;
            txtTitModal.fontStyle = FontStyle.Bold;
            txtTitModal.alignment = TextAnchor.MiddleCenter;
            txtTitModal.color = EstiloUIUX.ColDorado;
            txtTitModal.text = "CAMBIAR NOMBRE";
            txtTitModal.raycastTarget = false;
            txtTitModal.rectTransform.sizeDelta = new Vector2(500f, 60f);
            PanelOpcionesControles.AgregarOutline(txtTitModal, EstiloUIUX.ColContornoUniversal, new Vector2(4f, -4f));

            CrearIconoHuella(contCab.transform, new Vector2(45f, 40f));

            // Cuerpo del Diálogo (padding 44px arriba, 64px lados)
            var cuerpoObj = new GameObject("CuerpoModal");
            cuerpoObj.transform.SetParent(panelObj.transform, false);
            var rtCuerpo = cuerpoObj.AddComponent<RectTransform>();
            rtCuerpo.anchorMin = new Vector2(0f, 0f);
            rtCuerpo.anchorMax = new Vector2(1f, 1f);
            rtCuerpo.offsetMin = new Vector2(64f, 130f);
            rtCuerpo.offsetMax = new Vector2(-64f, -168f);

            var vlgCuerpo = cuerpoObj.AddComponent<VerticalLayoutGroup>();
            vlgCuerpo.childAlignment = TextAnchor.UpperLeft;
            vlgCuerpo.spacing = 16f;
            vlgCuerpo.childControlWidth = true;
            vlgCuerpo.childControlHeight = false;

            // Etiqueta: Huella 27x24 + "TU NOMBRE DE AVENTURERO"
            var filaEtiqueta = new GameObject("FilaEtiqueta");
            filaEtiqueta.transform.SetParent(cuerpoObj.transform, false);
            var rtFilaE = filaEtiqueta.AddComponent<RectTransform>();
            rtFilaE.sizeDelta = new Vector2(772f, 32f);

            var hlgE = filaEtiqueta.AddComponent<HorizontalLayoutGroup>();
            hlgE.childAlignment = TextAnchor.MiddleLeft;
            hlgE.spacing = 14f;
            hlgE.childControlWidth = false;
            hlgE.childControlHeight = false;

            CrearIconoHuella(filaEtiqueta.transform, new Vector2(27f, 24f));

            var txtE = new GameObject("Etiqueta").AddComponent<Text>();
            txtE.transform.SetParent(filaEtiqueta.transform, false);
            txtE.font = FuenteUtil.ObtenerFuentePixelify();
            txtE.fontSize = 28;
            txtE.fontStyle = FontStyle.Bold;
            txtE.color = EstiloUIUX.ColDorado;
            txtE.text = "TU NOMBRE DE AVENTURERO";
            txtE.raycastTarget = false;
            txtE.rectTransform.sizeDelta = new Vector2(500f, 32f);

            // Campo de Entrada 88px de alto
            var campoObj = new GameObject("CampoTexto");
            campoObj.transform.SetParent(cuerpoObj.transform, false);
            var rtCampo = campoObj.AddComponent<RectTransform>();
            rtCampo.sizeDelta = new Vector2(772f, 88f);

            var imgCampo = campoObj.AddComponent<Image>();
            imgCampo.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputNombre", EstiloUIUX.ColValorFondo, EstiloUIUX.ColValorSombra, EstiloUIUX.ColValorFondo, 6, 6, 0);
            imgCampo.type = Image.Type.Sliced;

            inputNombre = campoObj.AddComponent<InputField>();
            inputNombre.characterLimit = 16;
            inputNombre.caretColor = EstiloUIUX.ColDorado;

            // Texto dentro del input
            var txtValorInp = new GameObject("Text").AddComponent<Text>();
            txtValorInp.transform.SetParent(campoObj.transform, false);
            txtValorInp.font = FuenteUtil.ObtenerFuentePixelify();
            txtValorInp.fontSize = 42;
            txtValorInp.fontStyle = FontStyle.Bold;
            txtValorInp.alignment = TextAnchor.MiddleLeft;
            txtValorInp.color = EstiloUIUX.ColTextoCrema;
            txtValorInp.supportRichText = false;
            var rtTxtVal = txtValorInp.GetComponent<RectTransform>();
            rtTxtVal.anchorMin = Vector2.zero;
            rtTxtVal.anchorMax = Vector2.one;
            rtTxtVal.offsetMin = new Vector2(28f, 0f);
            rtTxtVal.offsetMax = new Vector2(-140f, 0f);

            // Contador 10/16 a la derecha
            var contObj = new GameObject("Contador");
            contObj.transform.SetParent(campoObj.transform, false);
            _txtContador = contObj.AddComponent<Text>();
            _txtContador.font = FuenteUtil.ObtenerFuentePixelify();
            _txtContador.fontSize = 26;
            _txtContador.fontStyle = FontStyle.Bold;
            _txtContador.alignment = TextAnchor.MiddleRight;
            _txtContador.color = EstiloUIUX.ColTextoMuyTenue;
            _txtContador.raycastTarget = false;
            var rtCont = contObj.GetComponent<RectTransform>();
            rtCont.anchorMin = new Vector2(1f, 0f);
            rtCont.anchorMax = new Vector2(1f, 1f);
            rtCont.pivot = new Vector2(1f, 0.5f);
            rtCont.anchoredPosition = new Vector2(-28f, 0f);
            rtCont.sizeDelta = new Vector2(100f, 0f);

            inputNombre.textComponent = txtValorInp;
            inputNombre.onValueChanged.AddListener(ActualizarContador);

            // Subtítulo informativo
            var subM = new GameObject("SubtituloModal");
            subM.transform.SetParent(cuerpoObj.transform, false);
            var txtSubM = subM.AddComponent<Text>();
            txtSubM.font = FuenteUtil.ObtenerFuentePixelify();
            txtSubM.fontSize = 22;
            txtSubM.color = EstiloUIUX.ColTextoTenue;
            txtSubM.text = "Así te verán los demás gatos en la mazmorra.";
            txtSubM.raycastTarget = false;
            subM.GetComponent<RectTransform>().sizeDelta = new Vector2(772f, 26f);

            // Footer con botones CANCELAR y GUARDAR
            var footerObj = new GameObject("FooterModal");
            footerObj.transform.SetParent(panelObj.transform, false);
            var rtFoot = footerObj.AddComponent<RectTransform>();
            rtFoot.anchorMin = new Vector2(0.5f, 0f);
            rtFoot.anchorMax = new Vector2(0.5f, 0f);
            rtFoot.pivot = new Vector2(0.5f, 0f);
            rtFoot.anchoredPosition = new Vector2(0f, 52f);
            rtFoot.sizeDelta = new Vector2(772f, 76f);

            var hlgFoot = footerObj.AddComponent<HorizontalLayoutGroup>();
            hlgFoot.childAlignment = TextAnchor.MiddleCenter;
            hlgFoot.spacing = 48f;
            hlgFoot.childControlWidth = false;
            hlgFoot.childControlHeight = false;

            // Botón Cancelar (Piedra 300x76px)
            CrearBotonModal(footerObj.transform, "BotonCancelar", "CANCELAR", TipoBotonUI.Piedra, 300f, 76f, null, Cancelar);

            // Botón Guardar (Verde 300x76px con palomita)
            botonContinuar = CrearBotonModal(footerObj.transform, "BotonGuardar", "GUARDAR", TipoBotonUI.Verde, 300f, 76f, "palomita", GuardarYContinuar);
        }

        private static Button CrearBotonModal(Transform parent, string nombre, string texto, TipoBotonUI tipo, float ancho, float alto, string icono, System.Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ancho, alto);

            var img = btnObj.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(tipo);
            img.type = Image.Type.Sliced;

            var btn = btnObj.AddComponent<Button>();
            PanelOpcionesControles.ConfigurarBotonPixel(btn, onClick);

            var contH = new GameObject("Contenido");
            contH.transform.SetParent(btnObj.transform, false);
            var rtContH = contH.AddComponent<RectTransform>();
            rtContH.anchorMin = Vector2.zero;
            rtContH.anchorMax = Vector2.one;
            rtContH.sizeDelta = Vector2.zero;

            var hlg = contH.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 16f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Color32 colorTexto = tipo == TipoBotonUI.Verde ? EstiloUIUX.ColBtnVerdeTexto : EstiloUIUX.ColBtnPiedraTexto;

            if (!string.IsNullOrEmpty(icono))
            {
                var icoObj = new GameObject("Icono");
                icoObj.transform.SetParent(contH.transform, false);
                var imgIco = icoObj.AddComponent<Image>();
                imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono(icono, colorTexto);
                imgIco.raycastTarget = false;
                icoObj.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 20f);
            }

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(contH.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = colorTexto;
            txt.text = texto;
            txt.raycastTarget = false;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 40f);

            return btn;
        }

        private static void CrearIconoHuella(Transform parent, Vector2 tamano)
        {
            var ico = new GameObject("Huella");
            ico.transform.SetParent(parent, false);
            var rt = ico.AddComponent<RectTransform>();
            rt.sizeDelta = tamano;
            var img = ico.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            img.raycastTarget = false;
        }

        private static void CrearRemache(Transform parent, Vector2 anchor, Vector2 offset)
        {
            var rem = new GameObject("Remache");
            rem.transform.SetParent(parent, false);
            var rt = rem.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(8f, 8f);

            var img = rem.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteRemache();
            img.raycastTarget = false;
        }
    }
}
