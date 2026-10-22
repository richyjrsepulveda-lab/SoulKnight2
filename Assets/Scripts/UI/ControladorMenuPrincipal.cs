using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Controlador para la pantalla de Menú Principal (Menú principal 1920x1080 1:1).
    /// - Fondo con patrón de ladrillos pixel art + viñeta radial + antorchas encendidas con chispas flotando.
    /// - Título gigante con outline grueso: "JUST A CAT ADVENTURE".
    /// - 3 Botones de acción principales (560x84px): JUGAR (Dorado), OPCIONES (Hielo), SALIR (Piedra).
    /// - Acceso a Cambiar Nombre con previsualización del nombre actual en la esquina inferior izquierda.
    /// - Modal integrado de Cambiar Nombre (900x588px) con contador de caracteres y validación.
    /// </summary>
    public class ControladorMenuPrincipal : MonoBehaviour
    {
        public Button botonJugar;
        public Button botonOpciones;
        public Button botonSalir;
        public Button botonCambiarNombre;
        public Text textoMensaje;
        public PanelOpcionesControles panelOpciones;

        private GameObject _modalCambiarNombre;
        private InputField _inputNombreModal;
        private Text _txtContadorModal;
        private Text _txtNombreBotonMenu;
        private RectTransform _rtLlamaIzq;
        private RectTransform _rtLlamaDer;
        private readonly List<RectTransform> _embers = new List<RectTransform>();
        private readonly List<Vector2> _posBaseEmbers = new List<Vector2>();

        void Start()
        {
            ComandosAdmin.ResetearAjustesAdmin();
            GestorAudio.DetenerTodosLosSFX();

            if (Global.Instancia != null && !Global.Instancia.HayNombreGuardado)
            {
                // Si no hay nombre guardado, abrir el modal de nombre de inmediato en lugar de pantalla en blanco
                AsegurarInterfazCompleta();
                AbrirModalCambiarNombre();
                return;
            }

            AsegurarInterfazCompleta();

            if (textoMensaje != null && !string.IsNullOrEmpty(Global.MensajeParaMenu))
            {
                textoMensaje.text = Global.MensajeParaMenu;
                Global.MensajeParaMenu = null;
            }

            GestorAudio.PlayBGM(TipoMusica.Sala);
        }

        void Update()
        {
            // Animación por pasos de llamas de antorcha (parpadeo de 4px cada 0.7s sin interpolar)
            float t = Time.time % 0.7f;
            float dy = (t < 0.35f) ? 0f : -4f;
            if (_rtLlamaIzq != null) _rtLlamaIzq.anchoredPosition = new Vector2(0f, dy);
            if (_rtLlamaDer != null) _rtLlamaDer.anchoredPosition = new Vector2(0f, dy);

            // Flotación sutil de chispas
            for (int i = 0; i < _embers.Count; i++)
            {
                if (_embers[i] != null && i < _posBaseEmbers.Count)
                {
                    float fase = (Time.time * 1.5f + i * 0.45f) % 2.0f;
                    float offsetY = (fase < 1.0f) ? 0f : -6f;
                    _embers[i].anchoredPosition = _posBaseEmbers[i] + new Vector2(0f, offsetY);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_modalCambiarNombre != null && _modalCambiarNombre.activeSelf)
                {
                    CerrarModalCambiarNombre();
                }
                else if (panelOpciones != null && panelOpciones.gameObject.activeSelf)
                {
                    panelOpciones.Cerrar();
                }
                else
                {
                    AlPresionarOpciones();
                }
            }
        }

        public void AsegurarInterfazCompleta()
        {
            // Buscar Canvas en la escena o crear uno responsivo
            Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CanvasMenuPrincipal");
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

            // Asegurar EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Ocultar elementos heredados antiguos para construir la interfaz 1:1 limpia
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var hijo = canvas.transform.GetChild(i);
                if (hijo.name == "PanelOpcionesControles") continue;
                hijo.gameObject.SetActive(false);
            }

            // Contenedor Raíz 1920x1080
            var rootObj = new GameObject("RootMenuPrincipal");
            rootObj.transform.SetParent(canvas.transform, false);
            var rtRoot = rootObj.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            // ==================== 1. FONDO: Ladrillos + Viñeta ====================
            var bgObj = new GameObject("FondoLadrillos");
            bgObj.transform.SetParent(rootObj.transform, false);
            var rtBg = bgObj.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.sizeDelta = Vector2.zero;
            var rawBg = bgObj.AddComponent<RawImage>();
            rawBg.texture = EstiloUIUX.ObtenerTexturaLadrillos();
            rawBg.uvRect = new Rect(0, 0, 1920f / 128f, 1080f / 64f);
            rawBg.raycastTarget = false;

            // Viñeta radial oscura
            var vinetaObj = new GameObject("Vineta");
            vinetaObj.transform.SetParent(rootObj.transform, false);
            var rtVin = vinetaObj.AddComponent<RectTransform>();
            rtVin.anchorMin = Vector2.zero;
            rtVin.anchorMax = Vector2.one;
            rtVin.sizeDelta = Vector2.zero;
            var rawVin = vinetaObj.AddComponent<RawImage>();
            rawVin.texture = EstiloUIUX.ObtenerTexturaVineta();
            rawVin.raycastTarget = false;

            // ==================== 2. ANTORCHAS Y RESPLANDOR ====================
            // Glows cálidos (640x640px)
            CrearGlowAntorcha(rootObj.transform, new Vector2(190f + 32f, 1080f - (330f + 72f)));
            CrearGlowAntorcha(rootObj.transform, new Vector2(1666f + 32f, 1080f - (330f + 72f)));

            // Antorchas (64x144px en x=190, y=330 y x=1666, y=330)
            _rtLlamaIzq = CrearAntorcha(rootObj.transform, new Vector2(190f, 1080f - 330f));
            _rtLlamaDer = CrearAntorcha(rootObj.transform, new Vector2(1666f, 1080f - 330f));

            // Chispas flotando en el ambiente
            CrearChispa(rootObj.transform, new Vector2(318f, 1080f - 420f), 9, new Color32(0xFF, 0x9A, 0x3D, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(402f, 1080f - 310f), 6, new Color32(0xFF, 0xD8, 0x4D, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(262f, 1080f - 520f), 6, new Color32(0x58, 0xC4, 0xFF, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(1580f, 1080f - 430f), 9, new Color32(0xFF, 0x9A, 0x3D, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(1510f, 1080f - 318f), 6, new Color32(0xFF, 0xD8, 0x4D, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(1650f, 1080f - 540f), 6, new Color32(0x58, 0xC4, 0xFF, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(560f, 1080f - 130f), 6, new Color32(0xFF, 0xD8, 0x4D, 0xFF));
            CrearChispa(rootObj.transform, new Vector2(1350f, 1080f - 150f), 6, new Color32(0xFF, 0x9A, 0x3D, 0xFF));

            // ==================== 3. TÍTULO GIGANTE ====================
            var titCont = new GameObject("ContenedorTitulo");
            titCont.transform.SetParent(rootObj.transform, false);
            var rtTitCont = titCont.AddComponent<RectTransform>();
            rtTitCont.anchorMin = new Vector2(0.5f, 1f);
            rtTitCont.anchorMax = new Vector2(0.5f, 1f);
            rtTitCont.pivot = new Vector2(0.5f, 1f);
            rtTitCont.anchoredPosition = new Vector2(0f, -240f);
            rtTitCont.sizeDelta = new Vector2(1800f, 240f);

            var vlgTit = titCont.AddComponent<VerticalLayoutGroup>();
            vlgTit.childAlignment = TextAnchor.UpperCenter;
            vlgTit.spacing = 12f;
            vlgTit.childControlWidth = false;
            vlgTit.childControlHeight = false;

            // "JUST A"
            var txtJustAObj = new GameObject("TxtJustA");
            txtJustAObj.transform.SetParent(titCont.transform, false);
            var txtJustA = txtJustAObj.AddComponent<Text>();
            txtJustA.font = FuenteUtil.ObtenerFuentePixelify();
            txtJustA.fontSize = 44;
            txtJustA.fontStyle = FontStyle.Bold;
            txtJustA.alignment = TextAnchor.MiddleCenter;
            txtJustA.color = EstiloUIUX.ColTextoCrema;
            txtJustA.text = "J  U  S  T   A";
            txtJustA.raycastTarget = false;
            txtJustAObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 54f);
            BordeTextoPixelArt.Aplicar(txtJustA, 8, new Vector2(0f, -4f));

            // "CAT ADVENTURE"
            var txtCatObj = new GameObject("TxtCatAdventure");
            txtCatObj.transform.SetParent(titCont.transform, false);
            var txtCat = txtCatObj.AddComponent<Text>();
            txtCat.font = FuenteUtil.ObtenerFuentePixelify();
            txtCat.fontSize = 136;
            txtCat.fontStyle = FontStyle.Bold;
            txtCat.alignment = TextAnchor.MiddleCenter;
            txtCat.supportRichText = true;
            txtCat.text = "<color=#ff7a2f>CAT</color>  <color=#ffcf4a>ADVENTURE</color>";
            txtCat.raycastTarget = false;
            txtCatObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f, 150f);
            BordeTextoPixelArt.Aplicar(txtCat, 12, new Vector2(0f, -8f));

            // ==================== 4. BOTONES PRINCIPALES (560 x 84 px, Gap 28 px) ====================
            var contBotones = new GameObject("ContenedorBotones");
            contBotones.transform.SetParent(rootObj.transform, false);
            var rtBtns = contBotones.AddComponent<RectTransform>();
            rtBtns.anchorMin = new Vector2(0.5f, 1f);
            rtBtns.anchorMax = new Vector2(0.5f, 1f);
            rtBtns.pivot = new Vector2(0.5f, 1f);
            rtBtns.anchoredPosition = new Vector2(0f, -510f);
            rtBtns.sizeDelta = new Vector2(560f, 320f);

            var vlgBtns = contBotones.AddComponent<VerticalLayoutGroup>();
            vlgBtns.childAlignment = TextAnchor.UpperCenter;
            vlgBtns.spacing = 28f;
            vlgBtns.childControlWidth = true;
            vlgBtns.childControlHeight = false;

            // Botón JUGAR (Dorado)
            botonJugar = CrearBotonAccion(contBotones.transform, "BotonJugar", "JUGAR", "play", new Vector2(32f, 32f), TipoBotonUI.Dorado, AlPresionarJugar);

            // Botón OPCIONES (Hielo)
            botonOpciones = CrearBotonAccion(contBotones.transform, "BotonOpciones", "OPCIONES", "engrane", new Vector2(36f, 36f), TipoBotonUI.Hielo, AlPresionarOpciones);

            // Botón SALIR (Piedra)
            botonSalir = CrearBotonAccion(contBotones.transform, "BotonSalir", "SALIR", "puerta", new Vector2(32f, 32f), TipoBotonUI.Piedra, AlPresionarSalir);

            // ==================== 5. BOTÓN CAMBIAR NOMBRE (Inferior Izquierda) ====================
            CrearBotonCambiarNombreMenu(rootObj.transform);

            // ==================== 6. MODAL CAMBIAR NOMBRE (1:1 Name.dc.html) ====================
            ConstruirModalCambiarNombre(canvas.transform);

            // Asegurar panel de opciones
            if (panelOpciones == null)
            {
                panelOpciones = GetComponentInChildren<PanelOpcionesControles>(true) ?? FindObjectOfType<PanelOpcionesControles>(true);
            }
            if (panelOpciones == null)
            {
                panelOpciones = PanelOpcionesControles.ObtenerOCrear(canvas.transform);
            }
        }

        private static void AgregarOutline(Text txt, Color col, Vector2 dist)
        {
            var o1 = txt.gameObject.AddComponent<Outline>();
            o1.effectColor = col;
            o1.effectDistance = dist;
            var o2 = txt.gameObject.AddComponent<Outline>();
            o2.effectColor = col;
            o2.effectDistance = new Vector2(-dist.x, dist.y);
            var o3 = txt.gameObject.AddComponent<Outline>();
            o3.effectColor = col;
            o3.effectDistance = new Vector2(dist.x, -dist.y);
            var o4 = txt.gameObject.AddComponent<Outline>();
            o4.effectColor = col;
            o4.effectDistance = new Vector2(-dist.x, -dist.y);
        }

        private static void CrearGlowAntorcha(Transform parent, Vector2 pos)
        {
            var glowObj = new GameObject("GlowAntorcha");
            glowObj.transform.SetParent(parent, false);
            var rt = glowObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(640f, 640f);
            var raw = glowObj.AddComponent<RawImage>();
            raw.texture = EstiloUIUX.ObtenerTexturaGlowAntorcha();
            raw.raycastTarget = false;
        }

        private static RectTransform CrearAntorcha(Transform parent, Vector2 posTopLeft)
        {
            var torchObj = new GameObject("Antorcha");
            torchObj.transform.SetParent(parent, false);
            var rt = torchObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = posTopLeft;
            rt.sizeDelta = new Vector2(64f, 144f);

            // Mango de madera
            var mangoObj = new GameObject("MangoMadera");
            mangoObj.transform.SetParent(torchObj.transform, false);
            var rtM = mangoObj.AddComponent<RectTransform>();
            rtM.anchorMin = new Vector2(0.5f, 0f);
            rtM.anchorMax = new Vector2(0.5f, 0f);
            rtM.pivot = new Vector2(0.5f, 0f);
            rtM.anchoredPosition = Vector2.zero;
            rtM.sizeDelta = new Vector2(16f, 48f);
            mangoObj.AddComponent<Image>().color = new Color32(0x5A, 0x3A, 0x22, 0xFF);

            // Copa metálica
            var copaObj = new GameObject("CopaMetalica");
            copaObj.transform.SetParent(torchObj.transform, false);
            var rtC = copaObj.AddComponent<RectTransform>();
            rtC.anchorMin = new Vector2(0.5f, 0f);
            rtC.anchorMax = new Vector2(0.5f, 0f);
            rtC.pivot = new Vector2(0.5f, 0f);
            rtC.anchoredPosition = new Vector2(0f, 48f);
            rtC.sizeDelta = new Vector2(48f, 16f);
            copaObj.AddComponent<Image>().color = new Color32(0x8D, 0x7A, 0x5A, 0xFF);

            // Llama animada
            var llamaObj = new GameObject("LlamaFuego");
            llamaObj.transform.SetParent(torchObj.transform, false);
            var rtLl = llamaObj.AddComponent<RectTransform>();
            rtLl.anchorMin = new Vector2(0.5f, 0f);
            rtLl.anchorMax = new Vector2(0.5f, 0f);
            rtLl.pivot = new Vector2(0.5f, 0f);
            rtLl.anchoredPosition = new Vector2(0f, 64f);
            rtLl.sizeDelta = new Vector2(56f, 72f);

            // Fuego naranja
            var imgLl = llamaObj.AddComponent<Image>();
            imgLl.color = new Color32(0xFF, 0x6A, 0x1F, 0xFF);

            // Núcleo amarillo
            var nucleoObj = new GameObject("NucleoLlama");
            nucleoObj.transform.SetParent(llamaObj.transform, false);
            var rtN = nucleoObj.AddComponent<RectTransform>();
            rtN.anchorMin = new Vector2(0.5f, 0.25f);
            rtN.anchorMax = new Vector2(0.5f, 0.25f);
            rtN.pivot = new Vector2(0.5f, 0.5f);
            rtN.sizeDelta = new Vector2(28f, 32f);
            nucleoObj.AddComponent<Image>().color = new Color32(0xFF, 0xD8, 0x4D, 0xFF);

            return rtLl;
        }

        private void CrearChispa(Transform parent, Vector2 pos, float size, Color32 col)
        {
            var emberObj = new GameObject("Ember");
            emberObj.transform.SetParent(parent, false);
            var rt = emberObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = emberObj.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            emberObj.tag = "Untagged";
            _embers.Add(rt);
            _posBaseEmbers.Add(pos);
        }

        private static Button CrearBotonAccion(Transform parent, string nombre, string texto, string nombreIcono, Vector2 iconoSize, TipoBotonUI tipo, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(560f, 84f);

            var img = btnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(tipo);

            var btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            var hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 16f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Color colorContenido = tipo switch
            {
                TipoBotonUI.Dorado => EstiloUIUX.ColBtnDoradoTexto,
                TipoBotonUI.Hielo => EstiloUIUX.ColBtnHieloTexto,
                TipoBotonUI.Verde => EstiloUIUX.ColBtnVerdeTexto,
                TipoBotonUI.Rojo => EstiloUIUX.ColBtnRojoTexto,
                _ => EstiloUIUX.ColBtnPiedraTexto
            };

            // Ícono
            var icoObj = new GameObject("Icono");
            icoObj.transform.SetParent(btnObj.transform, false);
            var imgIco = icoObj.AddComponent<Image>();
            imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, colorContenido);
            imgIco.raycastTarget = false;
            imgIco.preserveAspect = true;
            icoObj.GetComponent<RectTransform>().sizeDelta = iconoSize;

            // Texto
            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 40;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = colorContenido;
            txt.text = texto;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var csf = txtObj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Pointer feedback 4px down
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
                    onClick();
                });
            }

            return btn;
        }

        private void CrearBotonCambiarNombreMenu(Transform parent)
        {
            var btnObj = new GameObject("BotonCambiarNombre");
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(54f, 54f);
            rt.sizeDelta = new Vector2(340f, 72f);

            var img = btnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            img.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("ChipCambiarNombre", EstiloUIUX.ColTarjeta, EstiloUIUX.ColTarjetaLuz, EstiloUIUX.ColTarjetaSombra, 4, 4, 4);

            botonCambiarNombre = btnObj.AddComponent<Button>();
            var cb = botonCambiarNombre.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.selectedColor = Color.white;
            botonCambiarNombre.colors = cb;

            var hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 16f;
            hlg.padding = new RectOffset(16, 24, 6, 6);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Ícono lápiz
            var icoObj = new GameObject("IconoLápiz");
            icoObj.transform.SetParent(btnObj.transform, false);
            var imgIco = icoObj.AddComponent<Image>();
            imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono("lapiz", EstiloUIUX.ColTextoMuyTenue);
            imgIco.raycastTarget = false;
            imgIco.preserveAspect = true;
            icoObj.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

            // Bloque de textos
            var bloqueObj = new GameObject("BloqueTextos");
            bloqueObj.transform.SetParent(btnObj.transform, false);
            var rtBloque = bloqueObj.AddComponent<RectTransform>();
            rtBloque.sizeDelta = new Vector2(240f, 56f);

            var vlg = bloqueObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 2f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Etiqueta tenue
            var lblObj = new GameObject("Etiqueta");
            lblObj.transform.SetParent(bloqueObj.transform, false);
            var txtLbl = lblObj.AddComponent<Text>();
            txtLbl.font = FuenteUtil.ObtenerFuentePixelify();
            txtLbl.fontSize = 16;
            txtLbl.fontStyle = FontStyle.Bold;
            txtLbl.alignment = TextAnchor.MiddleLeft;
            txtLbl.color = EstiloUIUX.ColTextoMuyTenue;
            txtLbl.text = "CAMBIAR NOMBRE";
            txtLbl.raycastTarget = false;
            lblObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 20f);

            // Nombre actual
            var nomObj = new GameObject("NombreJugador");
            nomObj.transform.SetParent(bloqueObj.transform, false);
            _txtNombreBotonMenu = nomObj.AddComponent<Text>();
            _txtNombreBotonMenu.font = FuenteUtil.ObtenerFuentePixelify();
            _txtNombreBotonMenu.fontSize = 26;
            _txtNombreBotonMenu.fontStyle = FontStyle.Bold;
            _txtNombreBotonMenu.alignment = TextAnchor.MiddleLeft;
            _txtNombreBotonMenu.color = EstiloUIUX.ColTextoCrema;
            _txtNombreBotonMenu.text = ObtenerNombreActual();
            _txtNombreBotonMenu.raycastTarget = false;
            nomObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 28f);

            // Pointer feedback 4px down
            var trigger = btnObj.AddComponent<EventTrigger>();
            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { rt.anchoredPosition += new Vector2(0f, -4f); });
            trigger.triggers.Add(entryDown);

            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { rt.anchoredPosition -= new Vector2(0f, -4f); });
            trigger.triggers.Add(entryUp);

            botonCambiarNombre.onClick.AddListener(() =>
            {
                GestorAudio.PlaySFX(TipoSFX.ClickUI);
                AbrirModalCambiarNombre();
            });
        }

        private string ObtenerNombreActual()
        {
            if (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
            {
                return Global.Instancia.NombreLocal.Trim();
            }
            return PlayerPrefs.GetString("NombreJugador", "RichardLap");
        }

        // =========================================================================
        // MODAL CAMBIAR NOMBRE (1:1 Name.dc.html)
        // =========================================================================
        private void ConstruirModalCambiarNombre(Transform parent)
        {
            _modalCambiarNombre = new GameObject("ModalCambiarNombre");
            _modalCambiarNombre.transform.SetParent(parent, false);
            var rtModal = _modalCambiarNombre.AddComponent<RectTransform>();
            rtModal.anchorMin = Vector2.zero;
            rtModal.anchorMax = Vector2.one;
            rtModal.sizeDelta = Vector2.zero;

            // Capa oscura translúcida rgba(8, 4, 20, 0.62)
            var overObj = new GameObject("OverlayOscuro");
            overObj.transform.SetParent(_modalCambiarNombre.transform, false);
            var rtOver = overObj.AddComponent<RectTransform>();
            rtOver.anchorMin = Vector2.zero;
            rtOver.anchorMax = Vector2.one;
            rtOver.sizeDelta = Vector2.zero;
            var imgOver = overObj.AddComponent<Image>();
            imgOver.color = new Color(8f / 255f, 4f / 255f, 20f / 255f, 0.62f);
            imgOver.raycastTarget = true;

            // Panel Principal 900 x 588 px
            var panelObj = new GameObject("PanelModal");
            panelObj.transform.SetParent(_modalCambiarNombre.transform, false);
            var rtPan = panelObj.AddComponent<RectTransform>();
            rtPan.anchorMin = new Vector2(0.5f, 0.5f);
            rtPan.anchorMax = new Vector2(0.5f, 0.5f);
            rtPan.pivot = new Vector2(0.5f, 0.5f);
            rtPan.anchoredPosition = Vector2.zero;
            rtPan.sizeDelta = new Vector2(900f, 588f);

            var imgPan = panelObj.AddComponent<Image>();
            imgPan.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgPan.type = Image.Type.Sliced;

            // Remaches dorados en esquinas inferiores
            CrearRemache(panelObj.transform, new Vector2(16f, 16f), new Vector2(0f, 0f));
            CrearRemache(panelObj.transform, new Vector2(-16f, 16f), new Vector2(1f, 0f));

            // Cabecera (124px height, fondo #1b1438, borde inferior 6px)
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
            rtLin.sizeDelta = new Vector2(0f, 6f);
            lineaBorde.AddComponent<Image>().color = EstiloUIUX.ColContornoUniversal;

            // Contenido Cabecera: Huella dorada 45x40 | Titulo | Huella dorada 45x40
            var hlgCab = cabObj.AddComponent<HorizontalLayoutGroup>();
            hlgCab.childAlignment = TextAnchor.MiddleCenter;
            hlgCab.spacing = 32f;
            hlgCab.childControlWidth = false;
            hlgCab.childControlHeight = false;

            CrearIconoCabeceraModal(cabObj.transform, "HuellaIzq");

            var txtTitModal = new GameObject("Titulo").AddComponent<Text>();
            txtTitModal.transform.SetParent(cabObj.transform, false);
            txtTitModal.font = FuenteUtil.ObtenerFuentePixelify();
            txtTitModal.fontSize = 56;
            txtTitModal.fontStyle = FontStyle.Bold;
            txtTitModal.alignment = TextAnchor.MiddleCenter;
            txtTitModal.color = EstiloUIUX.ColDorado;
            txtTitModal.text = "CAMBIAR NOMBRE";
            txtTitModal.raycastTarget = false;
            txtTitModal.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 70f);
            BordeTextoPixelArt.Aplicar(txtTitModal, 8, new Vector2(0f, -6f));

            CrearIconoCabeceraModal(cabObj.transform, "HuellaDer");

            // Cuerpo del Modal (Padding 44px 64px 0)
            var cuerpoObj = new GameObject("CuerpoModal");
            cuerpoObj.transform.SetParent(panelObj.transform, false);
            var rtCuerpo = cuerpoObj.AddComponent<RectTransform>();
            rtCuerpo.anchorMin = new Vector2(0.5f, 1f);
            rtCuerpo.anchorMax = new Vector2(0.5f, 1f);
            rtCuerpo.pivot = new Vector2(0.5f, 1f);
            rtCuerpo.anchoredPosition = new Vector2(0f, -168f);
            rtCuerpo.sizeDelta = new Vector2(772f, 260f);

            var vlgCuerpo = cuerpoObj.AddComponent<VerticalLayoutGroup>();
            vlgCuerpo.childAlignment = TextAnchor.UpperLeft;
            vlgCuerpo.spacing = 14f;
            vlgCuerpo.childControlWidth = true;
            vlgCuerpo.childControlHeight = false;

            // Etiqueta "TU NOMBRE DE AVENTURERO" con huella
            var filaEtiqueta = new GameObject("FilaEtiqueta");
            filaEtiqueta.transform.SetParent(cuerpoObj.transform, false);
            var rtFilaE = filaEtiqueta.AddComponent<RectTransform>();
            rtFilaE.sizeDelta = new Vector2(772f, 32f);

            var hlgE = filaEtiqueta.AddComponent<HorizontalLayoutGroup>();
            hlgE.childAlignment = TextAnchor.MiddleLeft;
            hlgE.spacing = 14f;
            hlgE.childControlWidth = false;
            hlgE.childControlHeight = false;

            var icoP = new GameObject("IconoPata").AddComponent<Image>();
            icoP.transform.SetParent(filaEtiqueta.transform, false);
            icoP.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            icoP.raycastTarget = false;
            icoP.GetComponent<RectTransform>().sizeDelta = new Vector2(27f, 24f);

            var txtE = new GameObject("TextoE").AddComponent<Text>();
            txtE.transform.SetParent(filaEtiqueta.transform, false);
            txtE.font = FuenteUtil.ObtenerFuentePixelify();
            txtE.fontSize = 28;
            txtE.fontStyle = FontStyle.Bold;
            txtE.alignment = TextAnchor.MiddleLeft;
            txtE.color = EstiloUIUX.ColDorado;
            txtE.text = "TU NOMBRE DE AVENTURERO";
            txtE.raycastTarget = false;
            txtE.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 32f);

            // Contenedor InputField de 88px de alto
            var contInputObj = new GameObject("ContenedorInput");
            contInputObj.transform.SetParent(cuerpoObj.transform, false);
            var rtContInp = contInputObj.AddComponent<RectTransform>();
            rtContInp.sizeDelta = new Vector2(772f, 88f);

            var imgInpBg = contInputObj.AddComponent<Image>();
            imgInpBg.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputNombreGold", EstiloUIUX.ColValorFondo, EstiloUIUX.ColValorSombra, EstiloUIUX.ColValorSombra, 6, 6, 0);
            imgInpBg.type = Image.Type.Sliced;

            // Borde dorado 6px
            var bordeInp = new GameObject("BordeDorado");
            bordeInp.transform.SetParent(contInputObj.transform, false);
            var rtBrd = bordeInp.AddComponent<RectTransform>();
            rtBrd.anchorMin = Vector2.zero;
            rtBrd.anchorMax = Vector2.one;
            rtBrd.sizeDelta = Vector2.zero;
            var imgBrd = bordeInp.AddComponent<Image>();
            imgBrd.sprite = EstiloUIUX.ObtenerSpriteCajaEscalonada("InputNombreOutline", Color.clear, EstiloUIUX.ColDorado, EstiloUIUX.ColDorado, 6, 0, 0);
            imgBrd.type = Image.Type.Sliced;
            imgBrd.raycastTarget = false;

            // InputField
            _inputNombreModal = contInputObj.AddComponent<InputField>();
            _inputNombreModal.characterLimit = 16;

            var txtValorInp = new GameObject("TextoValor").AddComponent<Text>();
            txtValorInp.transform.SetParent(contInputObj.transform, false);
            var rtTxtVal = txtValorInp.GetComponent<RectTransform>();
            rtTxtVal.anchorMin = Vector2.zero;
            rtTxtVal.anchorMax = Vector2.one;
            rtTxtVal.offsetMin = new Vector2(28f, 0f);
            rtTxtVal.offsetMax = new Vector2(-120f, 0f);

            txtValorInp.font = FuenteUtil.ObtenerFuentePixelify();
            txtValorInp.fontSize = 42;
            txtValorInp.fontStyle = FontStyle.Bold;
            txtValorInp.alignment = TextAnchor.MiddleLeft;
            txtValorInp.color = EstiloUIUX.ColTextoCrema;

            _inputNombreModal.textComponent = txtValorInp;

            // Contador "10/16" en la derecha del campo
            var contNumObj = new GameObject("ContadorCaracteres").AddComponent<Text>();
            contNumObj.transform.SetParent(contInputObj.transform, false);
            var rtNum = contNumObj.GetComponent<RectTransform>();
            rtNum.anchorMin = new Vector2(1f, 0.5f);
            rtNum.anchorMax = new Vector2(1f, 0.5f);
            rtNum.pivot = new Vector2(1f, 0.5f);
            rtNum.anchoredPosition = new Vector2(-28f, 0f);
            rtNum.sizeDelta = new Vector2(100f, 40f);

            _txtContadorModal = contNumObj;
            _txtContadorModal.font = FuenteUtil.ObtenerFuentePixelify();
            _txtContadorModal.fontSize = 26;
            _txtContadorModal.fontStyle = FontStyle.Bold;
            _txtContadorModal.alignment = TextAnchor.MiddleRight;
            _txtContadorModal.color = EstiloUIUX.ColTextoMuyTenue;
            _txtContadorModal.raycastTarget = false;

            _inputNombreModal.onValueChanged.AddListener((val) =>
            {
                if (_txtContadorModal != null) _txtContadorModal.text = $"{val.Length}/16";
            });

            // Subtítulo explicativo
            var txtSubM = new GameObject("Subtitulo").AddComponent<Text>();
            txtSubM.transform.SetParent(cuerpoObj.transform, false);
            txtSubM.font = FuenteUtil.ObtenerFuentePixelify();
            txtSubM.fontSize = 22;
            txtSubM.fontStyle = FontStyle.Bold;
            txtSubM.alignment = TextAnchor.MiddleLeft;
            txtSubM.color = EstiloUIUX.ColTextoTenue;
            txtSubM.text = "Así te verán los demás gatos en la mazmorra.";
            txtSubM.raycastTarget = false;
            txtSubM.GetComponent<RectTransform>().sizeDelta = new Vector2(772f, 32f);

            // Footer con botones CANCELAR y GUARDAR (300 x 76 px, Gap 48 px)
            var footerObj = new GameObject("FooterModal");
            footerObj.transform.SetParent(panelObj.transform, false);
            var rtFoot = footerObj.AddComponent<RectTransform>();
            rtFoot.anchorMin = new Vector2(0.5f, 0f);
            rtFoot.anchorMax = new Vector2(0.5f, 0f);
            rtFoot.pivot = new Vector2(0.5f, 0f);
            rtFoot.anchoredPosition = new Vector2(0f, 52f);
            rtFoot.sizeDelta = new Vector2(648f, 76f);

            var hlgFoot = footerObj.AddComponent<HorizontalLayoutGroup>();
            hlgFoot.childAlignment = TextAnchor.MiddleCenter;
            hlgFoot.spacing = 48f;
            hlgFoot.childControlWidth = false;
            hlgFoot.childControlHeight = false;

            CrearBotonModal(footerObj.transform, "BotonCancelar", "CANCELAR", null, Vector2.zero, TipoBotonUI.Piedra, 300f, 76f, CerrarModalCambiarNombre);
            CrearBotonModal(footerObj.transform, "BotonGuardar", "GUARDAR", "palomita", new Vector2(30f, 20f), TipoBotonUI.Verde, 300f, 76f, GuardarNombreDesdeModal);

            _modalCambiarNombre.SetActive(false);
        }

        private static void CrearIconoCabeceraModal(Transform parent, string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteIcono("huella", EstiloUIUX.ColDorado);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(45f, 40f);
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

        private static Button CrearBotonModal(Transform parent, string nombre, string texto, string nombreIcono, Vector2 iconoSize, TipoBotonUI tipo, float w, float h, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var img = btnObj.AddComponent<Image>();
            img.type = Image.Type.Sliced;
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(tipo);

            var btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Color cTexto = tipo == TipoBotonUI.Verde ? EstiloUIUX.ColBtnVerdeTexto : EstiloUIUX.ColBtnPiedraTexto;

            if (!string.IsNullOrEmpty(nombreIcono))
            {
                var ico = new GameObject("Icono").AddComponent<Image>();
                ico.transform.SetParent(btnObj.transform, false);
                ico.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, cTexto);
                ico.raycastTarget = false;
                ico.GetComponent<RectTransform>().sizeDelta = iconoSize;
            }

            var txt = new GameObject("Texto").AddComponent<Text>();
            txt.transform.SetParent(btnObj.transform, false);
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = cTexto;
            txt.text = texto;
            txt.raycastTarget = false;
            txt.GetComponent<RectTransform>().sizeDelta = new Vector2(w - 60f, 40f);

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
                    onClick();
                });
            }
            return btn;
        }

        public void AbrirModalCambiarNombre()
        {
            if (_modalCambiarNombre == null) return;
            string actual = ObtenerNombreActual();
            if (_inputNombreModal != null)
            {
                _inputNombreModal.text = actual;
                _inputNombreModal.ActivateInputField();
            }
            if (_txtContadorModal != null)
            {
                _txtContadorModal.text = $"{actual.Length}/16";
            }
            _modalCambiarNombre.SetActive(true);
            _modalCambiarNombre.transform.SetAsLastSibling();
        }

        public void CerrarModalCambiarNombre()
        {
            if (_modalCambiarNombre != null) _modalCambiarNombre.SetActive(false);
        }

        private void GuardarNombreDesdeModal()
        {
            string nuevoNombre = _inputNombreModal != null ? _inputNombreModal.text.Trim() : "";
            if (string.IsNullOrEmpty(nuevoNombre))
            {
                nuevoNombre = "RichardLap";
            }

            if (Global.Instancia != null)
            {
                Global.Instancia.GuardarNombre(nuevoNombre);
            }
            else
            {
                PlayerPrefs.SetString("NombreJugador", nuevoNombre);
                PlayerPrefs.Save();
            }

            if (_txtNombreBotonMenu != null)
            {
                _txtNombreBotonMenu.text = nuevoNombre;
            }

            CerrarModalCambiarNombre();
        }

        private void AlPresionarJugar()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            SceneManager.LoadScene(Escenas.MenuJugar);
        }

        private void AlPresionarOpciones()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            if (panelOpciones == null)
            {
                panelOpciones = GetComponentInChildren<PanelOpcionesControles>(true) ?? FindObjectOfType<PanelOpcionesControles>(true);
            }
            if (panelOpciones != null)
            {
                panelOpciones.Abrir();
            }
            else
            {
                Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
                panelOpciones = PanelOpcionesControles.ObtenerOCrear(canvas != null ? canvas.transform : transform);
                panelOpciones.Abrir();
            }
        }

        private void AlPresionarSalir()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            Application.Quit();
        }
    }
}
