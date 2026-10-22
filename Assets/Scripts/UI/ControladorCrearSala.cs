using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla para crear sala con nombre y contraseña opcional (CrearSala.gd en Godot).
    /// Implementa la arquitectura visual 1:1 Pixel Art oficial de "Just a Cat Adventure":
    /// - Canvas responsivo 1920x1080 con CanvasScaler.
    /// - Fondo mosaico de ladrillos, overlay oscuro y partículas ambientales.
    /// - Panel modal con esquinas mordidas, remaches dorados y cabecera temática.
    /// - Campos de texto con cajas escalonadas, contador y validación inmediata.
    /// - Botones de acción con feedback táctil (4px down) y tipografía Pixelify Sans sin truncamiento.
    /// </summary>
    public class ControladorCrearSala : MonoBehaviour
    {
        public InputField inputNombreSala;
        public InputField inputPassword;
        public Button botonConfirmar;
        public Button botonVolver;
        public Text textoError;

        private Text _txtContadorNombre;
        private bool _interfazConstruida;

        void Awake()
        {
            AsegurarInterfazCompleta();
        }

        void Start()
        {
            GestorAudio.PlayBGM(TipoMusica.Sala);

            if (textoError != null) textoError.text = "";

            if (botonConfirmar != null)
            {
                botonConfirmar.onClick.RemoveAllListeners();
                botonConfirmar.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI);
                    AlConfirmar();
                });
            }

            if (botonVolver != null)
            {
                botonVolver.onClick.RemoveAllListeners();
                botonVolver.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI);
                    Volver();
                });
            }

            if (Global.Instancia != null)
            {
                Global.Instancia.ConexionFallida += MostrarError;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Volver();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                AlConfirmar();
            }
        }

        void OnDestroy()
        {
            if (Global.Instancia != null)
            {
                Global.Instancia.ConexionFallida -= MostrarError;
            }
        }

        private void Volver()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            SceneManager.LoadScene(Escenas.MenuJugar);
        }

        public void AsegurarInterfazCompleta()
        {
            if (_interfazConstruida) return;

            // 1. Canvas en la escena (1920x1080 responsivo)
            Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CanvasCrearSala");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Ocultar elementos heredados antiguos para no superponer
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var hijo = canvas.transform.GetChild(i);
                if (hijo.name == "RootCrearSala") continue;
                hijo.gameObject.SetActive(false);
            }

            // 2. Contenedor Raíz 1920x1080
            var rootObj = new GameObject("RootCrearSala");
            rootObj.transform.SetParent(canvas.transform, false);
            var rtRoot = rootObj.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            // Fondo con patrón de ladrillos
            var bgObj = new GameObject("FondoLadrillosMosaico");
            bgObj.transform.SetParent(rootObj.transform, false);
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
            overObj.transform.SetParent(rootObj.transform, false);
            var rtOver = overObj.AddComponent<RectTransform>();
            rtOver.anchorMin = Vector2.zero;
            rtOver.anchorMax = Vector2.one;
            rtOver.sizeDelta = Vector2.zero;
            var imgOver = overObj.AddComponent<Image>();
            imgOver.color = new Color(8f / 255f, 4f / 255f, 20f / 255f, 0.62f);
            imgOver.raycastTarget = true;

            // 3. Ventana Modal 960 x 740 px
            var panelObj = new GameObject("VentanaModal");
            panelObj.transform.SetParent(rootObj.transform, false);
            var rtPan = panelObj.AddComponent<RectTransform>();
            rtPan.anchorMin = new Vector2(0.5f, 0.5f);
            rtPan.anchorMax = new Vector2(0.5f, 0.5f);
            rtPan.pivot = new Vector2(0.5f, 0.5f);
            rtPan.anchoredPosition = Vector2.zero;
            rtPan.sizeDelta = new Vector2(960f, 740f);

            var imgPan = panelObj.AddComponent<Image>();
            imgPan.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgPan.type = Image.Type.Sliced;

            // Remaches dorados en esquinas inferiores
            CrearRemache(panelObj.transform, new Vector2(16f, 16f), new Vector2(0f, 0f));
            CrearRemache(panelObj.transform, new Vector2(-16f, 16f), new Vector2(1f, 0f));

            // 4. Cabecera (Alto 124px, #1b1438, borde inferior 6px)
            var cabObj = new GameObject("Cabecera");
            cabObj.transform.SetParent(panelObj.transform, false);
            var rtCab = cabObj.AddComponent<RectTransform>();
            rtCab.anchorMin = new Vector2(0f, 1f);
            rtCab.anchorMax = new Vector2(1f, 1f);
            rtCab.pivot = new Vector2(0.5f, 1f);
            rtCab.anchoredPosition = Vector2.zero;
            rtCab.sizeDelta = new Vector2(0f, 124f);

            var imgCab = cabObj.AddComponent<Image>();
            imgCab.color = EstiloUIUX.ColCabecera;

            var lineaBorde = new GameObject("BordeInferiorCabecera");
            lineaBorde.transform.SetParent(cabObj.transform, false);
            var rtLin = lineaBorde.AddComponent<RectTransform>();
            rtLin.anchorMin = new Vector2(0f, 0f);
            rtLin.anchorMax = new Vector2(1f, 0f);
            rtLin.pivot = new Vector2(0.5f, 0f);
            rtLin.sizeDelta = new Vector2(0f, 6f);
            lineaBorde.AddComponent<Image>().color = EstiloUIUX.ColContornoUniversal;

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

            var bloqueTit = new GameObject("BloqueTitulos");
            bloqueTit.transform.SetParent(contCab.transform, false);
            var rtBloque = bloqueTit.AddComponent<RectTransform>();
            rtBloque.sizeDelta = new Vector2(560f, 96f);
            var vlgCab = bloqueTit.AddComponent<VerticalLayoutGroup>();
            vlgCab.childAlignment = TextAnchor.MiddleCenter;
            vlgCab.spacing = 2f;
            vlgCab.childControlWidth = true;
            vlgCab.childControlHeight = false;

            var txtTit = new GameObject("Titulo").AddComponent<Text>();
            txtTit.transform.SetParent(bloqueTit.transform, false);
            txtTit.font = FuenteUtil.ObtenerFuentePixelify();
            txtTit.fontSize = 52;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = EstiloUIUX.ColDorado;
            txtTit.text = "CREAR SALA";
            txtTit.raycastTarget = false;
            txtTit.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtTit.verticalOverflow = VerticalWrapMode.Overflow;
            txtTit.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 56f);
            BordeTextoPixelArt.Aplicar(txtTit, 8, new Vector2(0f, -6f));

            var txtSub = new GameObject("Subtitulo").AddComponent<Text>();
            txtSub.transform.SetParent(bloqueTit.transform, false);
            txtSub.font = FuenteUtil.ObtenerFuentePixelify();
            txtSub.fontSize = 20;
            txtSub.fontStyle = FontStyle.Bold;
            txtSub.alignment = TextAnchor.MiddleCenter;
            txtSub.color = EstiloUIUX.ColTextoTenue;
            txtSub.text = "CONFIGURACIÓN DE LA PARTIDA EN RED";
            txtSub.raycastTarget = false;
            txtSub.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtSub.verticalOverflow = VerticalWrapMode.Overflow;
            txtSub.GetComponent<RectTransform>().sizeDelta = new Vector2(560f, 26f);

            CrearIconoHuella(contCab.transform, new Vector2(45f, 40f));

            // 5. Cuerpo del Diálogo (Padding 32px arriba, 64px lados)
            var cuerpoObj = new GameObject("CuerpoModal");
            cuerpoObj.transform.SetParent(panelObj.transform, false);
            var rtCuerpo = cuerpoObj.AddComponent<RectTransform>();
            rtCuerpo.anchorMin = new Vector2(0f, 0f);
            rtCuerpo.anchorMax = new Vector2(1f, 1f);
            rtCuerpo.offsetMin = new Vector2(64f, 126f);
            rtCuerpo.offsetMax = new Vector2(-64f, -140f);

            var vlgCuerpo = cuerpoObj.AddComponent<VerticalLayoutGroup>();
            vlgCuerpo.childAlignment = TextAnchor.UpperLeft;
            vlgCuerpo.spacing = 18f;
            vlgCuerpo.childControlWidth = true;
            vlgCuerpo.childControlHeight = false;

            // ==================== CAMPO 1: NOMBRE DE LA SALA ====================
            var bloqueNombre = new GameObject("BloqueNombre");
            bloqueNombre.transform.SetParent(cuerpoObj.transform, false);
            var vlgBN = bloqueNombre.AddComponent<VerticalLayoutGroup>();
            vlgBN.childAlignment = TextAnchor.UpperLeft;
            vlgBN.spacing = 6f;
            vlgBN.childControlWidth = true;
            vlgBN.childControlHeight = false;

            var filaE1 = new GameObject("FilaEtiqueta1");
            filaE1.transform.SetParent(bloqueNombre.transform, false);
            var hlgE1 = filaE1.AddComponent<HorizontalLayoutGroup>();
            hlgE1.childAlignment = TextAnchor.MiddleLeft;
            hlgE1.spacing = 14f;
            hlgE1.childControlWidth = false;
            hlgE1.childControlHeight = false;
            filaE1.GetComponent<RectTransform>().sizeDelta = new Vector2(832f, 32f);

            CrearIconoHuella(filaE1.transform, new Vector2(27f, 24f));

            var txtE1 = new GameObject("Etiqueta").AddComponent<Text>();
            txtE1.transform.SetParent(filaE1.transform, false);
            txtE1.font = FuenteUtil.ObtenerFuentePixelify();
            txtE1.fontSize = 26;
            txtE1.fontStyle = FontStyle.Bold;
            txtE1.color = EstiloUIUX.ColDorado;
            txtE1.text = "NOMBRE DE LA SALA";
            txtE1.raycastTarget = false;
            txtE1.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtE1.verticalOverflow = VerticalWrapMode.Overflow;
            txtE1.rectTransform.sizeDelta = new Vector2(400f, 32f);

            // Subtítulo explicativo
            var txtDesc1 = new GameObject("DescNombre").AddComponent<Text>();
            txtDesc1.transform.SetParent(bloqueNombre.transform, false);
            txtDesc1.font = FuenteUtil.ObtenerFuentePixelify();
            txtDesc1.fontSize = 18;
            txtDesc1.color = EstiloUIUX.ColTextoTenue;
            txtDesc1.text = "Elige el nombre con el que tus amigos identificarán la sala en la red.";
            txtDesc1.raycastTarget = false;
            txtDesc1.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtDesc1.verticalOverflow = VerticalWrapMode.Overflow;
            txtDesc1.rectTransform.sizeDelta = new Vector2(832f, 22f);

            // InputField Nombre
            var campoNombre = new GameObject("CampoNombre");
            campoNombre.transform.SetParent(bloqueNombre.transform, false);
            campoNombre.AddComponent<RectTransform>().sizeDelta = new Vector2(832f, 76f);
            var imgCN = campoNombre.AddComponent<Image>();
            imgCN.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputNombreSala", EstiloUIUX.ColValorFondo, EstiloUIUX.ColValorSombra, EstiloUIUX.ColValorFondo, 6, 6, 0);
            imgCN.type = Image.Type.Sliced;

            inputNombreSala = campoNombre.AddComponent<InputField>();
            inputNombreSala.characterLimit = 24;
            inputNombreSala.caretColor = EstiloUIUX.ColDorado;

            var txtValorNom = new GameObject("Text").AddComponent<Text>();
            txtValorNom.transform.SetParent(campoNombre.transform, false);
            txtValorNom.font = FuenteUtil.ObtenerFuentePixelify();
            txtValorNom.fontSize = 36;
            txtValorNom.fontStyle = FontStyle.Bold;
            txtValorNom.alignment = TextAnchor.MiddleLeft;
            txtValorNom.color = EstiloUIUX.ColTextoCrema;
            txtValorNom.supportRichText = false;
            txtValorNom.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtValorNom.verticalOverflow = VerticalWrapMode.Overflow;
            var rtTVN = txtValorNom.GetComponent<RectTransform>();
            rtTVN.anchorMin = Vector2.zero;
            rtTVN.anchorMax = Vector2.one;
            rtTVN.offsetMin = new Vector2(24f, 0f);
            rtTVN.offsetMax = new Vector2(-130f, 0f);

            var plNom = new GameObject("Placeholder").AddComponent<Text>();
            plNom.transform.SetParent(campoNombre.transform, false);
            plNom.font = FuenteUtil.ObtenerFuentePixelify();
            plNom.fontSize = 24;
            plNom.color = EstiloUIUX.ColTextoMuyTenue;
            plNom.text = "Ej: Mazmorra de Michis...";
            plNom.alignment = TextAnchor.MiddleLeft;
            plNom.horizontalOverflow = HorizontalWrapMode.Overflow;
            plNom.verticalOverflow = VerticalWrapMode.Overflow;
            var rtPlNom = plNom.GetComponent<RectTransform>();
            rtPlNom.anchorMin = Vector2.zero;
            rtPlNom.anchorMax = Vector2.one;
            rtPlNom.offsetMin = new Vector2(24f, 0f);
            rtPlNom.offsetMax = new Vector2(-130f, 0f);

            // Contador de caracteres
            var contNomObj = new GameObject("Contador").AddComponent<Text>();
            contNomObj.transform.SetParent(campoNombre.transform, false);
            _txtContadorNombre = contNomObj;
            _txtContadorNombre.font = FuenteUtil.ObtenerFuentePixelify();
            _txtContadorNombre.fontSize = 24;
            _txtContadorNombre.fontStyle = FontStyle.Bold;
            _txtContadorNombre.alignment = TextAnchor.MiddleRight;
            _txtContadorNombre.color = EstiloUIUX.ColTextoMuyTenue;
            _txtContadorNombre.text = "0/24";
            _txtContadorNombre.raycastTarget = false;
            _txtContadorNombre.horizontalOverflow = HorizontalWrapMode.Overflow;
            _txtContadorNombre.verticalOverflow = VerticalWrapMode.Overflow;
            var rtCNom = contNomObj.GetComponent<RectTransform>();
            rtCNom.anchorMin = new Vector2(1f, 0f);
            rtCNom.anchorMax = new Vector2(1f, 1f);
            rtCNom.pivot = new Vector2(1f, 0.5f);
            rtCNom.anchoredPosition = new Vector2(-24f, 0f);
            rtCNom.sizeDelta = new Vector2(90f, 0f);

            inputNombreSala.textComponent = txtValorNom;
            inputNombreSala.placeholder = plNom;
            inputNombreSala.onValueChanged.AddListener((val) =>
            {
                if (_txtContadorNombre != null) _txtContadorNombre.text = $"{val.Length}/24";
            });

            // ==================== CAMPO 2: CONTRASEÑA (OPCIONAL) ====================
            var bloquePass = new GameObject("BloquePass");
            bloquePass.transform.SetParent(cuerpoObj.transform, false);
            var vlgBP = bloquePass.AddComponent<VerticalLayoutGroup>();
            vlgBP.childAlignment = TextAnchor.UpperLeft;
            vlgBP.spacing = 6f;
            vlgBP.childControlWidth = true;
            vlgBP.childControlHeight = false;

            var filaE2 = new GameObject("FilaEtiqueta2");
            filaE2.transform.SetParent(bloquePass.transform, false);
            var hlgE2 = filaE2.AddComponent<HorizontalLayoutGroup>();
            hlgE2.childAlignment = TextAnchor.MiddleLeft;
            hlgE2.spacing = 14f;
            hlgE2.childControlWidth = false;
            hlgE2.childControlHeight = false;
            filaE2.GetComponent<RectTransform>().sizeDelta = new Vector2(832f, 32f);

            CrearIcono(filaE2.transform, "candado", EstiloUIUX.ColDorado, new Vector2(24f, 24f));

            var txtE2 = new GameObject("Etiqueta").AddComponent<Text>();
            txtE2.transform.SetParent(filaE2.transform, false);
            txtE2.font = FuenteUtil.ObtenerFuentePixelify();
            txtE2.fontSize = 26;
            txtE2.fontStyle = FontStyle.Bold;
            txtE2.color = EstiloUIUX.ColDorado;
            txtE2.text = "CONTRASEÑA (OPCIONAL)";
            txtE2.raycastTarget = false;
            txtE2.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtE2.verticalOverflow = VerticalWrapMode.Overflow;
            txtE2.rectTransform.sizeDelta = new Vector2(400f, 32f);

            var txtDesc2 = new GameObject("DescPass").AddComponent<Text>();
            txtDesc2.transform.SetParent(bloquePass.transform, false);
            txtDesc2.font = FuenteUtil.ObtenerFuentePixelify();
            txtDesc2.fontSize = 18;
            txtDesc2.color = EstiloUIUX.ColTextoTenue;
            txtDesc2.text = "Déjalo vacío para que cualquier jugador pueda unirse libremente.";
            txtDesc2.raycastTarget = false;
            txtDesc2.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtDesc2.verticalOverflow = VerticalWrapMode.Overflow;
            txtDesc2.rectTransform.sizeDelta = new Vector2(832f, 22f);

            // InputField Password
            var campoPass = new GameObject("CampoPass");
            campoPass.transform.SetParent(bloquePass.transform, false);
            campoPass.AddComponent<RectTransform>().sizeDelta = new Vector2(832f, 76f);
            var imgCP = campoPass.AddComponent<Image>();
            imgCP.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputPassSala", EstiloUIUX.ColValorFondo, EstiloUIUX.ColValorSombra, EstiloUIUX.ColValorFondo, 6, 6, 0);
            imgCP.type = Image.Type.Sliced;

            inputPassword = campoPass.AddComponent<InputField>();
            inputPassword.contentType = InputField.ContentType.Password;
            inputPassword.characterLimit = 16;
            inputPassword.caretColor = EstiloUIUX.ColDorado;

            var txtValorPass = new GameObject("Text").AddComponent<Text>();
            txtValorPass.transform.SetParent(campoPass.transform, false);
            txtValorPass.font = FuenteUtil.ObtenerFuentePixelify();
            txtValorPass.fontSize = 36;
            txtValorPass.fontStyle = FontStyle.Bold;
            txtValorPass.alignment = TextAnchor.MiddleLeft;
            txtValorPass.color = EstiloUIUX.ColTextoCrema;
            txtValorPass.supportRichText = false;
            txtValorPass.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtValorPass.verticalOverflow = VerticalWrapMode.Overflow;
            var rtTVP = txtValorPass.GetComponent<RectTransform>();
            rtTVP.anchorMin = Vector2.zero;
            rtTVP.anchorMax = Vector2.one;
            rtTVP.offsetMin = new Vector2(24f, 0f);
            rtTVP.offsetMax = new Vector2(-24f, 0f);

            var plPass = new GameObject("Placeholder").AddComponent<Text>();
            plPass.transform.SetParent(campoPass.transform, false);
            plPass.font = FuenteUtil.ObtenerFuentePixelify();
            plPass.fontSize = 24;
            plPass.color = EstiloUIUX.ColTextoMuyTenue;
            plPass.text = "Sin contraseña (sala pública)...";
            plPass.alignment = TextAnchor.MiddleLeft;
            plPass.horizontalOverflow = HorizontalWrapMode.Overflow;
            plPass.verticalOverflow = VerticalWrapMode.Overflow;
            var rtPlPass = plPass.GetComponent<RectTransform>();
            rtPlPass.anchorMin = Vector2.zero;
            rtPlPass.anchorMax = Vector2.one;
            rtPlPass.offsetMin = new Vector2(24f, 0f);
            rtPlPass.offsetMax = new Vector2(-24f, 0f);

            inputPassword.textComponent = txtValorPass;
            inputPassword.placeholder = plPass;

            // ==================== MENSAJE DE ERROR / ESTADO ====================
            var errObj = new GameObject("TextoError");
            errObj.transform.SetParent(cuerpoObj.transform, false);
            textoError = errObj.AddComponent<Text>();
            textoError.font = FuenteUtil.ObtenerFuentePixelify();
            textoError.fontSize = 22;
            textoError.fontStyle = FontStyle.Bold;
            textoError.alignment = TextAnchor.MiddleCenter;
            textoError.color = new Color32(0xFF, 0x6B, 0x6B, 0xFF);
            textoError.text = "";
            textoError.raycastTarget = false;
            textoError.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoError.verticalOverflow = VerticalWrapMode.Overflow;
            errObj.GetComponent<RectTransform>().sizeDelta = new Vector2(832f, 28f);

            // ==================== FOOTER: BOTONES VOLVER Y CREAR ====================
            var footerObj = new GameObject("FooterModal");
            footerObj.transform.SetParent(panelObj.transform, false);
            var rtFoot = footerObj.AddComponent<RectTransform>();
            rtFoot.anchorMin = new Vector2(0.5f, 0f);
            rtFoot.anchorMax = new Vector2(0.5f, 0f);
            rtFoot.pivot = new Vector2(0.5f, 0f);
            rtFoot.anchoredPosition = new Vector2(0f, 44f);
            rtFoot.sizeDelta = new Vector2(832f, 76f);

            var hlgFoot = footerObj.AddComponent<HorizontalLayoutGroup>();
            hlgFoot.childAlignment = TextAnchor.MiddleCenter;
            hlgFoot.spacing = 40f;
            hlgFoot.childControlWidth = false;
            hlgFoot.childControlHeight = false;

            // Botón Volver
            botonVolver = CrearBotonModal(footerObj.transform, "BotonVolver", "VOLVER", TipoBotonUI.Piedra, 340f, 76f, "puerta", new Vector2(28f, 28f), Volver);

            // Botón Crear Sala
            botonConfirmar = CrearBotonModal(footerObj.transform, "BotonConfirmar", "CREAR SALA", TipoBotonUI.Verde, 380f, 76f, "palomita", new Vector2(30f, 20f), AlConfirmar);

            _interfazConstruida = true;
        }

        private static Button CrearBotonModal(Transform parent, string nombre, string texto, TipoBotonUI tipo, float ancho, float alto, string icono, Vector2 iconoSize, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ancho, alto);

            var img = btnObj.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(tipo);
            img.type = Image.Type.Sliced;

            var btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Color cTexto = tipo == TipoBotonUI.Verde ? EstiloUIUX.ColBtnVerdeTexto :
                           (tipo == TipoBotonUI.Hielo ? EstiloUIUX.ColBtnHieloTexto : EstiloUIUX.ColBtnPiedraTexto);

            if (!string.IsNullOrEmpty(icono))
            {
                var ico = new GameObject("Icono").AddComponent<Image>();
                ico.transform.SetParent(btnObj.transform, false);
                ico.sprite = EstiloUIUX.ObtenerSpriteIcono(icono, cTexto);
                ico.raycastTarget = false;
                ico.GetComponent<RectTransform>().sizeDelta = iconoSize;
            }

            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = cTexto;
            txt.text = texto;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(ancho - 70f, 44f);

            var trigger = btnObj.AddComponent<EventTrigger>();
            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { rt.anchoredPosition += new Vector2(0f, -4f); });
            trigger.triggers.Add(entryDown);

            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { rt.anchoredPosition -= new Vector2(0f, -4f); });
            trigger.triggers.Add(entryUp);

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI);
                    onClick.Invoke();
                });
            }

            return btn;
        }

        private static void CrearIconoHuella(Transform parent, Vector2 tamano)
        {
            var go = new GameObject("IconoHuella");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = tamano;
        }

        private static void CrearIcono(Transform parent, string nombreIcono, Color32 color, Vector2 tamano)
        {
            var go = new GameObject($"Icono_{nombreIcono}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, color);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = tamano;
        }

        private static void CrearRemache(Transform parent, Vector2 pos, Vector2 anchor)
        {
            var go = new GameObject("Remache");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(8f, 8f);
            var img = go.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteRemache();
            img.raycastTarget = false;
        }

        private void AlConfirmar()
        {
            string nombreSala = inputNombreSala != null ? inputNombreSala.text.Trim() : "";
            if (string.IsNullOrEmpty(nombreSala))
            {
                MostrarError("Ponle un nombre a tu sala para continuar.");
                return;
            }

            string pass = inputPassword != null ? inputPassword.text : "";
            if (Global.Instancia != null)
            {
                MostrarError("");
                Global.Instancia.CrearSala(nombreSala, pass);
            }
        }

        private void MostrarError(string mensaje)
        {
            if (textoError != null)
            {
                textoError.text = mensaje;
            }
        }
    }
}
