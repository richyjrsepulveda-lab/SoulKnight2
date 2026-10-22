using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Hub con opciones: Crear Sala, Buscar Sala o Volver (MenuJugar.gd en Godot).
    /// Implementa la arquitectura visual 1:1 Pixel Art oficial de "Just a Cat Adventure":
    /// - Canvas responsivo 1920x1080 con CanvasScaler.
    /// - Fondo mosaico de ladrillos, viñeta oscura y partículas de antorchas.
    /// - Título temático con contorno y sombra pixel art sin huecos.
    /// - Botones de acción 560x84 px con feedback táctil (4px down) y espaciado exacto.
    /// </summary>
    public class ControladorMenuJugar : MonoBehaviour
    {
        public Button botonCrearSala;
        public Button botonUnirseSala;
        public Button botonVolver;

        private bool _interfazConstruida;

        void Awake()
        {
            AsegurarInterfazCompleta();
        }

        void Start()
        {
            GestorAudio.PlayBGM(TipoMusica.Sala);

            if (botonCrearSala != null)
            {
                botonCrearSala.onClick.RemoveAllListeners();
                botonCrearSala.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI);
                    SceneManager.LoadScene(Escenas.CrearSala);
                });
            }

            if (botonUnirseSala != null)
            {
                botonUnirseSala.onClick.RemoveAllListeners();
                botonUnirseSala.onClick.AddListener(() =>
                {
                    GestorAudio.PlaySFX(TipoSFX.ClickUI);
                    SceneManager.LoadScene(Escenas.UnirseSala);
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
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Volver();
            }
        }

        private void Volver()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }

        public void AsegurarInterfazCompleta()
        {
            if (_interfazConstruida) return;

            // 1. Canvas en la escena (1920x1080 responsivo)
            Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CanvasMenuJugar");
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

            // Ocultar elementos heredados antiguos
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var hijo = canvas.transform.GetChild(i);
                if (hijo.name == "RootMenuJugar") continue;
                hijo.gameObject.SetActive(false);
            }

            // 2. Contenedor Raíz 1920x1080
            var rootObj = new GameObject("RootMenuJugar");
            rootObj.transform.SetParent(canvas.transform, false);
            var rtRoot = rootObj.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.sizeDelta = Vector2.zero;

            // Fondo Ladrillos
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

            // Viñeta oscura
            var vinetaObj = new GameObject("Vineta");
            vinetaObj.transform.SetParent(rootObj.transform, false);
            var rtVin = vinetaObj.AddComponent<RectTransform>();
            rtVin.anchorMin = Vector2.zero;
            rtVin.anchorMax = Vector2.one;
            rtVin.sizeDelta = Vector2.zero;
            var rawVin = vinetaObj.AddComponent<RawImage>();
            rawVin.texture = EstiloUIUX.ObtenerTexturaVineta();
            rawVin.raycastTarget = false;

            // Overlay oscuro rgba(8, 4, 20, 0.45)
            var overObj = new GameObject("CapaOscuraFondo");
            overObj.transform.SetParent(rootObj.transform, false);
            var rtOver = overObj.AddComponent<RectTransform>();
            rtOver.anchorMin = Vector2.zero;
            rtOver.anchorMax = Vector2.one;
            rtOver.sizeDelta = Vector2.zero;
            var imgOver = overObj.AddComponent<Image>();
            imgOver.color = new Color(8f / 255f, 4f / 255f, 20f / 255f, 0.45f);
            imgOver.raycastTarget = false;

            // Antorchas en laterales
            CrearAntorcha(rootObj.transform, new Vector2(190f, 1080f - 330f - 144f));
            CrearAntorcha(rootObj.transform, new Vector2(1666f, 1080f - 330f - 144f));

            // ==================== 3. TÍTULO GIGANTE ====================
            var titCont = new GameObject("ContenedorTitulo");
            titCont.transform.SetParent(rootObj.transform, false);
            var rtTitCont = titCont.AddComponent<RectTransform>();
            rtTitCont.anchorMin = new Vector2(0.5f, 1f);
            rtTitCont.anchorMax = new Vector2(0.5f, 1f);
            rtTitCont.pivot = new Vector2(0.5f, 1f);
            rtTitCont.anchoredPosition = new Vector2(0f, -220f);
            rtTitCont.sizeDelta = new Vector2(1800f, 220f);

            var vlgTit = titCont.AddComponent<VerticalLayoutGroup>();
            vlgTit.childAlignment = TextAnchor.UpperCenter;
            vlgTit.spacing = 10f;
            vlgTit.childControlWidth = false;
            vlgTit.childControlHeight = false;

            // "MODO DE JUEGO"
            var txtJustAObj = new GameObject("TxtModo");
            txtJustAObj.transform.SetParent(titCont.transform, false);
            var txtModo = txtJustAObj.AddComponent<Text>();
            txtModo.font = FuenteUtil.ObtenerFuentePixelify();
            txtModo.fontSize = 40;
            txtModo.fontStyle = FontStyle.Bold;
            txtModo.alignment = TextAnchor.MiddleCenter;
            txtModo.color = EstiloUIUX.ColTextoCrema;
            txtModo.text = "M O D O   D E   J U E G O";
            txtModo.raycastTarget = false;
            txtModo.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtModo.verticalOverflow = VerticalWrapMode.Overflow;
            txtJustAObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 54f);
            BordeTextoPixelArt.Aplicar(txtModo, 8, new Vector2(0f, -4f));

            // "MULTIJUGADOR"
            var txtMultiObj = new GameObject("TxtMultijugador");
            txtMultiObj.transform.SetParent(titCont.transform, false);
            var txtMulti = txtMultiObj.AddComponent<Text>();
            txtMulti.font = FuenteUtil.ObtenerFuentePixelify();
            txtMulti.fontSize = 110;
            txtMulti.fontStyle = FontStyle.Bold;
            txtMulti.alignment = TextAnchor.MiddleCenter;
            txtMulti.supportRichText = true;
            txtMulti.text = "<color=#ff7a2f>MULTI</color>  <color=#ffcf4a>JUGADOR</color>";
            txtMulti.raycastTarget = false;
            txtMulti.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtMulti.verticalOverflow = VerticalWrapMode.Overflow;
            txtMultiObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f, 140f);
            BordeTextoPixelArt.Aplicar(txtMulti, 12, new Vector2(0f, -8f));

            // ==================== 4. BOTONES PRINCIPALES (380 x 74 px, Gap 24 px) ====================
            var contBotones = new GameObject("ContenedorBotones");
            contBotones.transform.SetParent(rootObj.transform, false);
            var rtBtns = contBotones.AddComponent<RectTransform>();
            rtBtns.anchorMin = new Vector2(0.5f, 1f);
            rtBtns.anchorMax = new Vector2(0.5f, 1f);
            rtBtns.pivot = new Vector2(0.5f, 1f);
            rtBtns.anchoredPosition = new Vector2(0f, -490f);
            rtBtns.sizeDelta = new Vector2(380f, 320f);

            var vlgBtns = contBotones.AddComponent<VerticalLayoutGroup>();
            vlgBtns.childAlignment = TextAnchor.UpperCenter;
            vlgBtns.spacing = 22f;
            vlgBtns.childControlWidth = true;
            vlgBtns.childControlHeight = false;

            // 1. Crear Sala (Dorado)
            botonCrearSala = CrearBotonAccion(contBotones.transform, "BotonCrearSala", "CREAR SALA", "play", new Vector2(30f, 30f), TipoBotonUI.Dorado, () =>
            {
                SceneManager.LoadScene(Escenas.CrearSala);
            });

            // 2. Buscar Sala (Hielo)
            botonUnirseSala = CrearBotonAccion(contBotones.transform, "BotonUnirseSala", "BUSCAR SALA", "lupa", new Vector2(32f, 32f), TipoBotonUI.Hielo, () =>
            {
                SceneManager.LoadScene(Escenas.UnirseSala);
            });

            // 3. Volver (Piedra)
            botonVolver = CrearBotonAccion(contBotones.transform, "BotonVolver", "VOLVER", "puerta", new Vector2(32f, 32f), TipoBotonUI.Piedra, Volver);

            _interfazConstruida = true;
        }

        private static Button CrearBotonAccion(Transform parent, string nombre, string texto, string nombreIcono, Vector2 iconoSize, TipoBotonUI tipo, Action onClick)
        {
            var btnObj = new GameObject(nombre);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 74f);

            var img = btnObj.AddComponent<Image>();
            img.sprite = EstiloUIUX.ObtenerSpriteBoton(tipo);
            img.type = Image.Type.Sliced;

            var btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            var hlg = btnObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Color colorContenido = tipo == TipoBotonUI.Dorado ? EstiloUIUX.ColBtnDoradoTexto :
                                  (tipo == TipoBotonUI.Hielo ? EstiloUIUX.ColBtnHieloTexto : EstiloUIUX.ColBtnPiedraTexto);

            // Icono
            var icoObj = new GameObject("Icono");
            icoObj.transform.SetParent(btnObj.transform, false);
            var imgIco = icoObj.AddComponent<Image>();
            imgIco.sprite = EstiloUIUX.ObtenerSpriteIcono(nombreIcono, colorContenido);
            imgIco.raycastTarget = false;
            imgIco.preserveAspect = true;
            icoObj.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

            // Texto
            var txtObj = new GameObject("Texto");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 30;
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

            // Feedback táctil al presionar (4px abajo)
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

        private static void CrearAntorcha(Transform parent, Vector2 posTopLeft)
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

            // Llama
            var llamaObj = new GameObject("LlamaFuego");
            llamaObj.transform.SetParent(torchObj.transform, false);
            var rtLl = llamaObj.AddComponent<RectTransform>();
            rtLl.anchorMin = new Vector2(0.5f, 0f);
            rtLl.anchorMax = new Vector2(0.5f, 0f);
            rtLl.pivot = new Vector2(0.5f, 0f);
            rtLl.anchoredPosition = new Vector2(0f, 64f);
            rtLl.sizeDelta = new Vector2(56f, 72f);
            llamaObj.AddComponent<Image>().color = new Color32(0xFF, 0x6A, 0x1F, 0xFF);

            var nucleoObj = new GameObject("NucleoLlama");
            nucleoObj.transform.SetParent(llamaObj.transform, false);
            var rtN = nucleoObj.AddComponent<RectTransform>();
            rtN.anchorMin = new Vector2(0.5f, 0.25f);
            rtN.anchorMax = new Vector2(0.5f, 0.25f);
            rtN.pivot = new Vector2(0.5f, 0.5f);
            rtN.sizeDelta = new Vector2(28f, 32f);
            nucleoObj.AddComponent<Image>().color = new Color32(0xFF, 0xD8, 0x4D, 0xFF);
        }
    }
}
