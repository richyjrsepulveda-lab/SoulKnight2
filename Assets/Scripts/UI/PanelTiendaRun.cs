using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Interfaz visual completa de la Tienda de la Run ("Tienda Mística del Gato Mercader").
    /// Diseñada e implementada al 100% siguiendo las especificaciones del diseño de referencia
    /// (Shop.dc.html y Tienda de la run@1x.png), resolución 1920x1080:
    /// - Toldo a rayas teal/crema con festón ondulado repetido a 16 módulos.
    /// - Faroles colgantes con cadenas de hierro y halo cálido circular suave.
    /// - Letrero colgante de madera ("TIENDA - MEJORAS PARA ESTA RUN") con cadenas.
    /// - Gato tendero místico con ojos luminosos verdes, gema mágica en el pecho,
    ///   halo radial circular suave (sin bordes cuadrados), parpadeo animado y motas flotantes.
    /// - Bocadillo de diálogo reactivo ("Acércate… ¿qué buscas?" y respuestas tras comprar).
    /// - Mostrador con vela encendida, halo circular suave, contador de monedas y botón "CONTINUAR ➔".
    /// - 3 repisas de madera con las 6 tarjetas de mejoras:
    ///   - Iconos reales de habilidades de la run (IconosHabilidades).
    ///   - Signo de interrogación "?" para habilidades/mejoras universales.
    ///   - Precios claramente visibles sin recortes (horizontal y vertical overflow habilitados).
    ///   - Insignias de ranura (E, Q, R, CLIC, ★) y estados interactivos reactivos en 0ms.
    /// </summary>
    public class PanelTiendaRun : MonoBehaviour
    {
        public static PanelTiendaRun Instancia { get; private set; }

        public bool EstaAbierto => gameObject != null && gameObject.activeSelf;

        [Header("Sprites Pixel-Art")]
        private Sprite _spAwning;
        private Sprite _spCatBase;
        private Sprite _spCatEyes;
        private Sprite _spCatGem;
        private Sprite _spCandle;
        private Sprite _spLantern;
        private Sprite _spIconSnow;
        private Sprite _spIconBolt;
        private Sprite _spIconFlame;
        private Sprite _spIconPaw;
        private Sprite _spIconCoin;
        private Sprite _spIconCheck;
        private Sprite _spIconStar;
        private Sprite _spPointer;

        // Textura y sprite para halos circulares ultra suaves (sin recortes cuadrados)
        private static Sprite _spGlowCircular;

        // Referencias de elementos animados
        private RectTransform _rtCat;
        private RectTransform _rtCatEyes;
        private Image _imgCatGem;
        private Image _imgCatGlow;
        private RectTransform _rtVelaLlama;
        private Image _imgVelaHalo;
        private readonly List<MotaMagica> _motas = new List<MotaMagica>();

        // Textos y datos
        private Text _txtBocadillo;
        private Text _txtOro;
        private float _timerDialogoRestaurar = -1f;

        // Tarjetas
        private readonly TarjetaUI[] _tarjetas = new TarjetaUI[6];

        // Estado de juego
        private PuestitoTienda _puestitoActivo;
        private Personaje _personajeLocal;
        private int _oroVisual = 0;

        // Tiempos de animación
        private float _timerParpadeo = 3.5f;
        private bool _parpadeando = false;
        private float _tiempoParpadeoRestante = 0f;

        private class MotaMagica
        {
            public RectTransform rt;
            public Image img;
            public Vector2 posBase;
            public float velocidad;
            public float fase;
            public float distanciaMax;
        }

        private class TarjetaUI
        {
            public GameObject root;
            public CanvasGroup canvasGroup;
            public Image imgBordeAccent;
            public Image imgIcono;
            public Text txtInterrogacion;
            public Text txtSlot;
            public Image imgSlotStar;
            public Text txtTag;
            public Text txtNombre;
            public Text txtDesc;
            public Image imgCoin;
            public Text txtPrecio;
            public Button btnComprar;
            public Image imgBtnComprar;
            public Text txtBtnComprar;
            public GameObject objComprado;
            public int index;
        }

        public static PanelTiendaRun ObtenerOCrear(Transform parent)
        {
            if (Instancia != null) return Instancia;

            var compExistente = parent.GetComponentInChildren<PanelTiendaRun>(true);
            if (compExistente != null)
            {
                Instancia = compExistente;
                return compExistente;
            }

            var go = new GameObject("PanelTiendaRun", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var panel = go.AddComponent<PanelTiendaRun>();
            return panel;
        }

        void Awake()
        {
            Instancia = this;
            CargarSprites();
            ConstruirInterfaz();
            gameObject.SetActive(false);
        }

        private void CargarSprites()
        {
            _spAwning = Resources.Load<Sprite>("UI/Tienda/awning");
            _spCatBase = Resources.Load<Sprite>("UI/Tienda/cat_base");
            _spCatEyes = Resources.Load<Sprite>("UI/Tienda/cat_eyes");
            _spCatGem = Resources.Load<Sprite>("UI/Tienda/cat_gem");
            _spCandle = Resources.Load<Sprite>("UI/Tienda/candle");
            _spLantern = Resources.Load<Sprite>("UI/Tienda/lantern");
            _spIconSnow = Resources.Load<Sprite>("UI/Tienda/icon_snow");
            _spIconBolt = Resources.Load<Sprite>("UI/Tienda/icon_bolt");
            _spIconFlame = Resources.Load<Sprite>("UI/Tienda/icon_flame");
            _spIconPaw = Resources.Load<Sprite>("UI/Tienda/icon_paw");
            _spIconCoin = Resources.Load<Sprite>("UI/Tienda/icon_coin");
            _spIconCheck = Resources.Load<Sprite>("UI/Tienda/icon_check");
            _spIconStar = Resources.Load<Sprite>("UI/Tienda/icon_star");
            _spPointer = Resources.Load<Sprite>("UI/Tienda/pointer");
        }

        /// <summary>
        /// Genera proceduralmente un sprite de gradiente radial circular con decaimiento Hermite
        /// para que la iluminación sea 100% esférica, suave y se desvanezca por completo a 0 alfa
        /// mucho antes de alcanzar el borde del RectTransform (evitando cualquier corte cuadrado).
        /// </summary>
        public static Sprite ObtenerSpriteGlowCircular()
        {
            if (_spGlowCircular != null) return _spGlowCircular;

            const int S = 128;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] px = new Color32[S * S];
            float centro = (S - 1) * 0.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= 1.0f)
                    {
                        px[y * S + x] = new Color32(255, 255, 255, 0);
                    }
                    else
                    {
                        float t = 1.0f - dist;
                        float hermite = t * t * (3f - 2f * t);
                        byte a = (byte)(Mathf.Clamp01(hermite) * 255f);
                        px[y * S + x] = new Color32(255, 255, 255, a);
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            _spGlowCircular = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            return _spGlowCircular;
        }

        // =========================================================================
        // CONSTRUCCIÓN VISUAL EXACTA 1920x1080
        // =========================================================================
        private void ConstruirInterfaz()
        {
            Font fuentePixel = FuenteUtil.ObtenerFuentePixelify();
            Font fuenteSilkscreen = FuenteUtil.ObtenerFuenteSilkscreen() ?? fuentePixel;

            // 1. Fondo general de mazmorra (#1c1537) con oscurecimiento profundo
            var imgFondo = gameObject.AddComponent<Image>();
            imgFondo.color = new Color(0.04f, 0.03f, 0.08f, 0.94f);

            // Contenedor principal 1920x1080 centrado para visualización nítida
            var rootCanvas = new GameObject("CanvasTienda1920", typeof(RectTransform));
            rootCanvas.transform.SetParent(transform, false);
            var rtRoot = rootCanvas.GetComponent<RectTransform>();
            rtRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rtRoot.pivot = new Vector2(0.5f, 0.5f);
            rtRoot.sizeDelta = new Vector2(1920f, 1080f);
            rtRoot.anchoredPosition = Vector2.zero;

            // 2. Interior del puesto de madera (#2b1c14) [left: 72, top: 100, width: 1776, height: 922]
            var puestoInterior = CrearRect(rootCanvas.transform, "PuestoInterior", 72f, 100f, 1776f, 922f);
            var imgPuesto = puestoInterior.gameObject.AddComponent<Image>();
            imgPuesto.color = new Color32(0x2B, 0x1C, 0x14, 0xFF);

            // Sombra interior superior [left: 72, top: 142, width: 1776, height: 44]
            var sombraPuesto = CrearRect(puestoInterior.transform, "SombraSuperior", 0f, 42f, 1776f, 44f);
            var imgSombraPuesto = sombraPuesto.gameObject.AddComponent<Image>();
            imgSombraPuesto.color = new Color(0f, 0f, 0f, 0.38f);

            // 3. Postes de madera laterales [left: 24, top: 130, width: 48, height: 950]
            CrearPoste(rootCanvas.transform, "PosteIzquierdo", 24f, 130f, 48f, 950f);
            CrearPoste(rootCanvas.transform, "PosteDerecho", 1848f, 130f, 48f, 950f);

            // 4. Base del puesto de madera [left: 72, top: 1022, width: 1776, height: 58]
            var basePuesto = CrearRect(rootCanvas.transform, "BasePuesto", 72f, 1022f, 1776f, 58f);
            var imgBasePuesto = basePuesto.gameObject.AddComponent<Image>();
            imgBasePuesto.color = new Color32(0x8A, 0x5A, 0x3A, 0xFF);
            CrearBordeInterior(basePuesto.transform, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xA9, 0x72, 0x48, 0xFF));

            // 5. 3 Repisas de madera para las tarjetas [left: 596, width: 1228, height: 24] en top: 430, 714, 998
            CrearRepisa(rootCanvas.transform, "Repisa1", 596f, 430f, 1228f, 24f);
            CrearRepisa(rootCanvas.transform, "Repisa2", 596f, 714f, 1228f, 24f);
            CrearRepisa(rootCanvas.transform, "Repisa3", 596f, 998f, 1228f, 24f);

            // 6. Toldo superior festoneado [left: 0, top: 0, width: 1920, height: 142] con 16 módulos de 120px
            var toldoContenedor = CrearRect(rootCanvas.transform, "ToldoSuperior", 0f, 0f, 1920f, 142f);
            for (int i = 0; i < 16; i++)
            {
                var mod = CrearRect(toldoContenedor.transform, $"Modulo_{i}", i * 120f, 0f, 120f, 142f);
                var imgMod = mod.gameObject.AddComponent<Image>();
                imgMod.sprite = _spAwning;
                imgMod.color = Color.white;
            }
            // Sombra bajo el toldo
            var sombraToldo = CrearRect(rootCanvas.transform, "SombraToldo", 0f, 142f, 1920f, 6f);
            var imgSombraToldo = sombraToldo.gameObject.AddComponent<Image>();
            imgSombraToldo.color = new Color32(0x0C, 0x08, 0x18, 0xFF);

            // 7. Faroles colgantes y cadenas [left: 117 y 561] con halos circulares suaves
            CrearFarol(rootCanvas.transform, "FarolIzquierdo", 117f, 146f, 99f, 196f);
            CrearFarol(rootCanvas.transform, "FarolDerecho", 561f, 146f, 543f, 196f);

            // 8. Letrero colgante de la Tienda [left: 135, top: 222, width: 400, height: 120]
            CrearCadena(rootCanvas.transform, "CadenaLetreroIzq", 190f, 146f, 80f);
            CrearCadena(rootCanvas.transform, "CadenaLetreroDer", 480f, 146f, 80f);

            var letreroObj = CrearRect(rootCanvas.transform, "LetreroTienda", 135f, 222f, 400f, 120f);
            var imgLetrero = letreroObj.gameObject.AddComponent<Image>();
            imgLetrero.color = new Color32(0x8A, 0x5A, 0x3A, 0xFF);
            CrearBordeCaja(letreroObj.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xA9, 0x72, 0x48, 0xFF), new Color32(0x5A, 0x3A, 0x22, 0xFF));

            var titObj = CrearRectCentrado(letreroObj.transform, "TituloTienda", 0f, 14f, 380f, 60f);
            var txtTit = titObj.gameObject.AddComponent<Text>();
            txtTit.font = fuentePixel;
            txtTit.text = "TIENDA";
            txtTit.fontSize = 54;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtTit.verticalOverflow = VerticalWrapMode.Overflow;
            txtTit.color = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
            BordeTextoPixelArt.Aplicar(txtTit, 10, new Vector2(0f, -6f), new Color32(0x0C, 0x08, 0x18, 0xFF));

            var subTitObj = CrearRectCentrado(letreroObj.transform, "SubtituloTienda", 0f, -32f, 380f, 24f);
            var txtSubTit = subTitObj.gameObject.AddComponent<Text>();
            txtSubTit.font = fuenteSilkscreen;
            txtSubTit.text = "MEJORAS PARA ESTA RUN";
            txtSubTit.fontSize = 16;
            txtSubTit.fontStyle = FontStyle.Bold;
            txtSubTit.alignment = TextAnchor.MiddleCenter;
            txtSubTit.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtSubTit.verticalOverflow = VerticalWrapMode.Overflow;
            txtSubTit.color = new Color32(0xFF, 0xF3, 0xD6, 0xFF);

            // 9. Bocadillo de diálogo [left: 120, top: 430, width: 420, height: 100]
            var bocadilloObj = CrearRect(rootCanvas.transform, "BocadilloDialogo", 120f, 430f, 420f, 100f);
            var imgBocadillo = bocadilloObj.gameObject.AddComponent<Image>();
            imgBocadillo.color = new Color32(0x17, 0x11, 0x2E, 0xFF);
            CrearBordeCaja(bocadilloObj.transform, 4, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF));

            var txtBocObj = CrearRectCentrado(bocadilloObj.transform, "TextoDialogo", 0f, 0f, 390f, 80f);
            _txtBocadillo = txtBocObj.gameObject.AddComponent<Text>();
            _txtBocadillo.font = fuentePixel;
            _txtBocadillo.fontSize = 24;
            _txtBocadillo.fontStyle = FontStyle.Bold;
            _txtBocadillo.alignment = TextAnchor.MiddleCenter;
            _txtBocadillo.horizontalOverflow = HorizontalWrapMode.Overflow;
            _txtBocadillo.verticalOverflow = VerticalWrapMode.Overflow;
            _txtBocadillo.color = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
            _txtBocadillo.text = "Acércate… ¿qué buscas?";

            // Puntero del bocadillo [left: 306, top: 530, width: 48, height: 24]
            var ptrObj = CrearRect(rootCanvas.transform, "PunteroBocadillo", 306f, 530f, 48f, 24f);
            var imgPtr = ptrObj.gameObject.AddComponent<Image>();
            imgPtr.sprite = _spPointer;
            imgPtr.color = Color.white;

            // 10. Gato Tendero Misterioso [left: 170, top: 612, width: 320, height: 288]
            var gatoContenedor = CrearRect(rootCanvas.transform, "GatoTendero", 170f, 612f, 320f, 288f);
            _rtCat = gatoContenedor;

            // Halo cálido circular suave detrás del gato (amplio 520x480 con caída Hermite suave a 0)
            var glowObj = CrearRectCentrado(gatoContenedor.transform, "HaloGatoCircular", 0f, 0f, 520f, 480f);
            _imgCatGlow = glowObj.gameObject.AddComponent<Image>();
            _imgCatGlow.sprite = ObtenerSpriteGlowCircular();
            _imgCatGlow.color = new Color(1f, 0.55f, 0.20f, 0.22f);

            // Capa 1: Cuerpo base
            var cuerpoObj = CrearRectCentrado(gatoContenedor.transform, "CuerpoBase", 0f, 0f, 320f, 288f);
            var imgCuerpo = cuerpoObj.gameObject.AddComponent<Image>();
            imgCuerpo.sprite = _spCatBase;
            imgCuerpo.color = Color.white;

            // Capa 2: Ojos verdes luminosos
            var ojosObj = CrearRectCentrado(gatoContenedor.transform, "OjosGato", 0f, 0f, 320f, 288f);
            _rtCatEyes = ojosObj;
            _rtCatEyes.pivot = new Vector2(0.5f, 0.55f);
            var imgOjos = ojosObj.gameObject.AddComponent<Image>();
            imgOjos.sprite = _spCatEyes;
            imgOjos.color = Color.white;

            // Capa 3: Gema mística púrpura
            var gemaObj = CrearRectCentrado(gatoContenedor.transform, "GemaMistica", 0f, 0f, 320f, 288f);
            _imgCatGem = gemaObj.gameObject.AddComponent<Image>();
            _imgCatGem.sprite = _spCatGem;
            _imgCatGem.color = Color.white;

            // 11. Motas de polvo mágico flotantes
            CrearMota(rootCanvas.transform, new Vector2(150f, 820f), new Color32(0xC9, 0x9B, 0xFF, 0xFF), 18f, 0f);
            CrearMota(rootCanvas.transform, new Vector2(512f, 770f), new Color32(0x8D, 0xFF, 0xA8, 0xFF), 22f, 1.2f);
            CrearMota(rootCanvas.transform, new Vector2(132f, 720f), new Color32(0x8D, 0xFF, 0xA8, 0xFF), 16f, 2.5f);
            CrearMota(rootCanvas.transform, new Vector2(498f, 700f), new Color32(0xC9, 0x9B, 0xFF, 0xFF), 20f, 3.8f);
            CrearMota(rootCanvas.transform, new Vector2(330f, 600f), new Color32(0xC9, 0x9B, 0xFF, 0xFF), 15f, 4.4f);

            // 12. Mostrador de la tienda [left: 90, top: 900, width: 490, height: 180]
            var mostradorTop = CrearRect(rootCanvas.transform, "MostradorTop", 90f, 900f, 490f, 24f);
            var imgMosTop = mostradorTop.gameObject.AddComponent<Image>();
            imgMosTop.color = new Color32(0xC5, 0x8F, 0x5C, 0xFF);
            CrearBordeCaja(mostradorTop.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xE0, 0xAE, 0x78, 0xFF), new Color32(0x8A, 0x5A, 0x3A, 0xFF));

            var mostradorFrente = CrearRect(rootCanvas.transform, "MostradorFrente", 90f, 924f, 490f, 156f);
            var imgMosFrente = mostradorFrente.gameObject.AddComponent<Image>();
            imgMosFrente.color = new Color32(0x8A, 0x5A, 0x3A, 0xFF);
            CrearBordeCaja(mostradorFrente.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xA9, 0x72, 0x48, 0xFF), new Color32(0x5A, 0x3A, 0x22, 0xFF));

            // 13. Vela encendida sobre el mostrador [left: 508, top: 820, width: 40, height: 80]
            // Halo circular suave de la vela (180x180 centrado en la llama con Sprite de gradiente Hermite)
            var haloVela = CrearRectCentrado(rootCanvas.transform, "HaloVelaCircular", 528f, -810f, 180f, 180f);
            _imgVelaHalo = haloVela.gameObject.AddComponent<Image>();
            _imgVelaHalo.sprite = ObtenerSpriteGlowCircular();
            _imgVelaHalo.color = new Color(1f, 0.68f, 0.24f, 0.32f);

            var velaObj = CrearRect(rootCanvas.transform, "VelaMostrador", 508f, 820f, 40f, 80f);
            _rtVelaLlama = velaObj;
            var imgVela = velaObj.gameObject.AddComponent<Image>();
            imgVela.sprite = _spCandle;
            imgVela.color = Color.white;

            // 14. Contador de monedas sobre el mostrador [left: 104, top: 834, width: 140, height: 60]
            var monedasObj = CrearRect(rootCanvas.transform, "CajaMonedas", 104f, 834f, 140f, 60f);
            var imgMonedas = monedasObj.gameObject.AddComponent<Image>();
            imgMonedas.color = new Color32(0x17, 0x11, 0x2E, 0xFF);
            CrearBordeCaja(monedasObj.transform, 4, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF));

            var coinIcoObj = CrearRectCentrado(monedasObj.transform, "IconoMoneda", -40f, 0f, 32f, 32f);
            var imgCoinIco = coinIcoObj.gameObject.AddComponent<Image>();
            imgCoinIco.sprite = _spIconCoin;
            imgCoinIco.color = Color.white;

            var txtOroObj = CrearRectCentrado(monedasObj.transform, "TextoOro", 20f, 0f, 80f, 40f);
            _txtOro = txtOroObj.gameObject.AddComponent<Text>();
            _txtOro.font = fuentePixel;
            _txtOro.fontSize = 32;
            _txtOro.fontStyle = FontStyle.Bold;
            _txtOro.alignment = TextAnchor.MiddleLeft;
            _txtOro.horizontalOverflow = HorizontalWrapMode.Overflow;
            _txtOro.verticalOverflow = VerticalWrapMode.Overflow;
            _txtOro.color = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
            _txtOro.text = "0";

            // 15. Botón CONTINUAR [left: 140, top: 968, width: 390, height: 72]
            var btnCont = EstiloUIUX.CrearBoton(rootCanvas.transform, "BotonContinuar", "CONTINUAR ➔", TipoBotonUI.Dorado, new Vector2(390f, 72f), Cerrar);
            var rtBtnCont = btnCont.GetComponent<RectTransform>();
            rtBtnCont.anchorMin = new Vector2(0f, 1f);
            rtBtnCont.anchorMax = new Vector2(0f, 1f);
            rtBtnCont.pivot = new Vector2(0f, 1f);
            rtBtnCont.anchoredPosition = new Vector2(140f, -968f);
            var txtBtn = btnCont.GetComponentInChildren<Text>();
            if (txtBtn != null)
            {
                txtBtn.fontSize = 28;
                txtBtn.fontStyle = FontStyle.Bold;
                txtBtn.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtBtn.verticalOverflow = VerticalWrapMode.Overflow;
            }

            // 16. Grid de 6 Tarjetas de Mejoras (2 columnas x 3 filas) [left: 610, top: 190, width: 1200]
            float[] posX = { 610f, 1220f };
            float[] posY = { 190f, 474f, 758f };

            for (int i = 0; i < 6; i++)
            {
                int r = i / 2;
                int c = i % 2;
                _tarjetas[i] = ConstruirTarjeta(rootCanvas.transform, i, posX[c], posY[r]);
            }
        }

        private TarjetaUI ConstruirTarjeta(Transform parent, int index, float x, float y)
        {
            Font fuentePixel = FuenteUtil.ObtenerFuentePixelify();
            Font fuenteSilkscreen = FuenteUtil.ObtenerFuenteSilkscreen() ?? fuentePixel;

            var t = new TarjetaUI { index = index };

            // Tarjeta principal (580x240)
            var cardObj = CrearRect(parent, $"TarjetaItem_{index}", x, y, 580f, 240f);
            t.root = cardObj.gameObject;
            t.canvasGroup = cardObj.gameObject.AddComponent<CanvasGroup>();

            var imgFondo = cardObj.gameObject.AddComponent<Image>();
            imgFondo.color = new Color32(0x22, 0x1A, 0x44, 0xFF);

            // Borde oscuro exterior de 6px
            CrearBordeCaja(cardObj.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0x18, 0x10, 0x32, 0xFF), new Color32(0x18, 0x10, 0x32, 0xFF));

            // Barra superior de acento de categoría
            var accentBar = CrearRect(cardObj.transform, "BarraAcento", 0f, 0f, 580f, 6f);
            t.imgBordeAccent = accentBar.gameObject.AddComponent<Image>();
            t.imgBordeAccent.color = new Color32(0x58, 0xC4, 0xFF, 0xFF);

            // Caja de Icono 96x96 [left: 24, top: 20]
            var icoBox = CrearRect(cardObj.transform, "CajaIcono", 24f, 20f, 96f, 96f);
            var imgIcoBox = icoBox.gameObject.AddComponent<Image>();
            imgIcoBox.color = new Color32(0x17, 0x11, 0x2E, 0xFF);
            CrearBordeCaja(icoBox.transform, 4, new Color32(0x58, 0xC4, 0xFF, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF), new Color32(0x0A, 0x06, 0x16, 0xFF));

            // Icono de la habilidad real (64x64)
            var icoImgObj = CrearRectCentrado(icoBox.transform, "IconoSprite", 0f, 0f, 64f, 64f);
            t.imgIcono = icoImgObj.gameObject.AddComponent<Image>();
            t.imgIcono.sprite = _spIconSnow;
            t.imgIcono.color = Color.white;

            // Signo de interrogación "?" para habilidades universales
            var qObj = CrearRectCentrado(icoBox.transform, "InterrogacionUniversal", 0f, 0f, 80f, 80f);
            t.txtInterrogacion = qObj.gameObject.AddComponent<Text>();
            t.txtInterrogacion.font = fuentePixel;
            t.txtInterrogacion.fontSize = 52;
            t.txtInterrogacion.fontStyle = FontStyle.Bold;
            t.txtInterrogacion.alignment = TextAnchor.MiddleCenter;
            t.txtInterrogacion.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.txtInterrogacion.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtInterrogacion.color = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
            t.txtInterrogacion.text = "?";
            t.txtInterrogacion.gameObject.SetActive(false);

            // Insignia de Ranura [slot badge: E, Q, R, CLIC, ★] en esquina inferior derecha del icono
            var slotObj = CrearRect(icoBox.transform, "BadgeSlot", 68f, 72f, 38f, 30f);
            var imgSlot = slotObj.gameObject.AddComponent<Image>();
            imgSlot.color = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
            CrearBordeCaja(slotObj.transform, 3, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xFF, 0xFD, 0xF2, 0xFF), new Color32(0xCD, 0xBF, 0x9C, 0xFF));

            var txtSlotObj = CrearRectCentrado(slotObj.transform, "TextoSlot", 0f, 0f, 38f, 28f);
            t.txtSlot = txtSlotObj.gameObject.AddComponent<Text>();
            t.txtSlot.font = fuenteSilkscreen;
            t.txtSlot.fontSize = 15;
            t.txtSlot.fontStyle = FontStyle.Bold;
            t.txtSlot.alignment = TextAnchor.MiddleCenter;
            t.txtSlot.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.txtSlot.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtSlot.color = new Color32(0x1B, 0x14, 0x38, 0xFF);
            t.txtSlot.text = "E";

            var starObj = CrearRectCentrado(slotObj.transform, "EstrellaSlot", 0f, 0f, 16f, 16f);
            t.imgSlotStar = starObj.gameObject.AddComponent<Image>();
            t.imgSlotStar.sprite = _spIconStar;
            t.imgSlotStar.color = new Color32(0x1B, 0x14, 0x38, 0xFF);
            t.imgSlotStar.gameObject.SetActive(false);

            // Bloque de Texto: Categoría, Nombre y Descripción [left: 136]
            var tagObj = CrearRect(cardObj.transform, "TextoTag", 136f, 18f, 420f, 24f);
            t.txtTag = tagObj.gameObject.AddComponent<Text>();
            t.txtTag.font = fuentePixel;
            t.txtTag.fontSize = 17;
            t.txtTag.fontStyle = FontStyle.Bold;
            t.txtTag.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.txtTag.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtTag.color = new Color32(0x8F, 0xE0, 0xFF, 0xFF);
            t.txtTag.text = "HABILIDAD 1 • HIELO";

            var nmObj = CrearRect(cardObj.transform, "TextoNombre", 136f, 44f, 420f, 36f);
            t.txtNombre = nmObj.gameObject.AddComponent<Text>();
            t.txtNombre.font = fuentePixel;
            t.txtNombre.fontSize = 28;
            t.txtNombre.fontStyle = FontStyle.Bold;
            t.txtNombre.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.txtNombre.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtNombre.color = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
            t.txtNombre.text = "Aliento Helado";

            var dsObj = CrearRect(cardObj.transform, "TextoDescripcion", 136f, 84f, 420f, 72f);
            t.txtDesc = dsObj.gameObject.AddComponent<Text>();
            t.txtDesc.font = fuentePixel;
            t.txtDesc.fontSize = 19;
            t.txtDesc.lineSpacing = 1.15f;
            t.txtDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.txtDesc.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtDesc.color = new Color32(0xC9, 0xC1, 0xEE, 0xFF);
            t.txtDesc.text = "Tu ráfaga de hielo ralentiza a los enemigos durante más tiempo.";

            // Fila inferior directa de Precio: Icono de Moneda + Número de Precio
            var coinObj = CrearRect(cardObj.transform, "MonedaPrecio", 24f, 174f, 32f, 32f);
            t.imgCoin = coinObj.gameObject.AddComponent<Image>();
            t.imgCoin.sprite = _spIconCoin;
            t.imgCoin.color = Color.white;

            var txtPrObj = CrearRect(cardObj.transform, "TextoPrecio", 64f, 170f, 180f, 44f);
            t.txtPrecio = txtPrObj.gameObject.AddComponent<Text>();
            t.txtPrecio.font = fuentePixel;
            t.txtPrecio.fontSize = 34;
            t.txtPrecio.fontStyle = FontStyle.Bold;
            t.txtPrecio.alignment = TextAnchor.MiddleLeft;
            t.txtPrecio.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.txtPrecio.verticalOverflow = VerticalWrapMode.Overflow;
            t.txtPrecio.color = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
            t.txtPrecio.text = "120";

            // Botón Comprar interactivo (Verde o Piedra según oro)
            int capturado = index;
            t.btnComprar = EstiloUIUX.CrearBoton(cardObj.transform, $"BotonComprar_{index}", "COMPRAR", TipoBotonUI.Verde, new Vector2(210f, 52f), () => OnClickComprar(capturado));
            var rtBtn = t.btnComprar.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(1f, 1f);
            rtBtn.anchorMax = new Vector2(1f, 1f);
            rtBtn.pivot = new Vector2(1f, 1f);
            rtBtn.anchoredPosition = new Vector2(-24f, -168f);
            t.imgBtnComprar = t.btnComprar.GetComponent<Image>();
            t.txtBtnComprar = t.btnComprar.GetComponentInChildren<Text>();
            if (t.txtBtnComprar != null)
            {
                t.txtBtnComprar.fontSize = 22;
                t.txtBtnComprar.fontStyle = FontStyle.Bold;
                t.txtBtnComprar.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.txtBtnComprar.verticalOverflow = VerticalWrapMode.Overflow;
            }

            // Etiqueta ✔ COMPRADO cuando ya se adquirió el máximo
            var compObj = CrearRect(cardObj.transform, "EstadoComprado", 340f, 172f, 220f, 52f);
            t.objComprado = compObj.gameObject;
            var icoCheckObj = CrearRectCentrado(compObj.transform, "IconoCheck", -65f, 0f, 36f, 24f);
            var imgCheck = icoCheckObj.gameObject.AddComponent<Image>();
            imgCheck.sprite = _spIconCheck;
            imgCheck.color = new Color32(0x4E, 0xD1, 0x7E, 0xFF);

            var txtCompObj = CrearRectCentrado(compObj.transform, "TextoComprado", 20f, 0f, 150f, 40f);
            var txtComp = txtCompObj.gameObject.AddComponent<Text>();
            txtComp.font = fuentePixel;
            txtComp.fontSize = 26;
            txtComp.fontStyle = FontStyle.Bold;
            txtComp.alignment = TextAnchor.MiddleLeft;
            txtComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtComp.verticalOverflow = VerticalWrapMode.Overflow;
            txtComp.color = new Color32(0x4E, 0xD1, 0x7E, 0xFF);
            txtComp.text = "COMPRADO";
            t.objComprado.SetActive(false);

            return t;
        }

        // =========================================================================
        // APERTURA, CIERRE Y COMPRA REACTIVA
        // =========================================================================
        public void Abrir(PuestitoTienda puestito, Personaje personajeLocal, int oroInicial)
        {
            _puestitoActivo = puestito;
            _personajeLocal = personajeLocal;
            _oroVisual = oroInicial;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            SetDialogo("Acércate… ¿qué buscas?");
            ActualizarOro(_oroVisual);
            RefrescarUI();
        }

        public void Cerrar()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.CerrarTienda();
            }
        }

        public void ActualizarOro(int oro)
        {
            _oroVisual = oro;
            if (_txtOro != null)
            {
                _txtOro.text = oro.ToString();
            }
        }

        public void RefrescarUI()
        {
            if (_puestitoActivo == null) return;

            int totalItems = _puestitoActivo.itemsOfrecidos.Count;
            for (int i = 0; i < 6; i++)
            {
                if (i >= totalItems)
                {
                    _tarjetas[i].root.SetActive(false);
                    continue;
                }

                _tarjetas[i].root.SetActive(true);
                ConfigurarTarjeta(_tarjetas[i], _puestitoActivo.itemsOfrecidos[i]);
            }
        }

        private void ConfigurarTarjeta(TarjetaUI t, TipoMejoraTienda tipo)
        {
            int nvActual = (_personajeLocal != null) ? _personajeLocal.ObtenerNivelMejora(tipo) : 0;
            int costo = CatalogoMejorasTienda.ObtenerCostoNivel(tipo, nvActual);
            TipoPersonaje claseReq = CatalogoMejorasTienda.ObtenerClaseCompatible(tipo);
            bool compatible = (claseReq == TipoPersonaje.Ninguno) || (_personajeLocal != null && _personajeLocal.TipoPj == claseReq);
            bool esMaximo = (tipo != TipoMejoraTienda.PescadoAsado && nvActual >= 5) || (tipo == TipoMejoraTienda.VidaMaxima && _personajeLocal != null && _personajeLocal.vidaMax >= 300);
            bool puedePagar = _oroVisual >= costo;

            // Datos de texto y diseño por tipo
            ObtenerDetallesMejora(tipo, nvActual, out string nombre, out string desc, out string tag, out string slot, out Color32 colAccent, out Color32 colTag, out Sprite spIcono, out bool esUniversal);

            t.txtNombre.text = nombre;
            t.txtDesc.text = desc;
            t.txtTag.text = tag;
            t.txtTag.color = colTag;
            t.imgBordeAccent.color = colAccent;

            // Icono de habilidad real o signo de interrogación "?" para habilidades universales
            if (esUniversal)
            {
                t.imgIcono.gameObject.SetActive(false);
                t.txtInterrogacion.gameObject.SetActive(true);
            }
            else
            {
                t.txtInterrogacion.gameObject.SetActive(false);
                t.imgIcono.gameObject.SetActive(true);
                t.imgIcono.sprite = spIcono;
            }

            // Precio y Moneda
            t.txtPrecio.text = costo.ToString();
            t.imgCoin.gameObject.SetActive(true);
            t.txtPrecio.gameObject.SetActive(true);

            // Configurar badge de ranura
            if (slot == "★")
            {
                t.txtSlot.gameObject.SetActive(false);
                t.imgSlotStar.gameObject.SetActive(true);
            }
            else
            {
                t.txtSlot.gameObject.SetActive(true);
                t.imgSlotStar.gameObject.SetActive(false);
                t.txtSlot.text = slot;
            }

            // Estados visuales (Comprado / Incompatible / Falta Oro / Disponible)
            if (esMaximo)
            {
                t.canvasGroup.alpha = 0.55f;
                t.btnComprar.gameObject.SetActive(false);
                t.objComprado.SetActive(true);
                t.txtPrecio.color = new Color32(0x4E, 0xD1, 0x7E, 0xFF);
            }
            else
            {
                t.canvasGroup.alpha = 1f;
                t.objComprado.SetActive(false);
                t.btnComprar.gameObject.SetActive(true);

                if (!compatible)
                {
                    t.canvasGroup.alpha = 0.65f;
                    t.btnComprar.interactable = false;
                    t.txtBtnComprar.text = "INCOMPATIBLE";
                    t.txtBtnComprar.color = EstiloUIUX.ColBtnPiedraTexto;
                    t.imgBtnComprar.color = EstiloUIUX.ColBtnPiedraCara;
                    t.txtPrecio.color = EstiloUIUX.ColTextoTenue;
                }
                else if (!puedePagar)
                {
                    t.btnComprar.interactable = false;
                    int faltante = costo - _oroVisual;
                    t.txtBtnComprar.text = $"FALTAN {faltante}";
                    t.txtBtnComprar.color = EstiloUIUX.ColBtnPiedraTexto;
                    t.imgBtnComprar.color = EstiloUIUX.ColBtnPiedraCara;
                    t.txtPrecio.color = new Color32(0xFF, 0x8D, 0x96, 0xFF);
                }
                else
                {
                    t.btnComprar.interactable = true;
                    t.txtBtnComprar.text = "COMPRAR";
                    t.txtBtnComprar.color = EstiloUIUX.ColBtnVerdeTexto;
                    t.imgBtnComprar.color = EstiloUIUX.ColBtnVerdeCara;
                    t.txtPrecio.color = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
                }
            }
        }

        private Sprite ObtenerSpriteHabilidadReal(IdHabilidad id)
        {
            if (id == IdHabilidad.Ninguna) return _spIconPaw;

            // 1. Cargar desde HUDJugador si ya están instanciados
            if (HUDJugador.Instancia != null)
            {
                switch (id)
                {
                    case IdHabilidad.Fuego_BolaFuego: if (HUDJugador.Instancia.iconoFuegoLMB != null) return HUDJugador.Instancia.iconoFuegoLMB; break;
                    case IdHabilidad.Fuego_Lanzallamas: if (HUDJugador.Instancia.iconoFuegoE != null) return HUDJugador.Instancia.iconoFuegoE; break;
                    case IdHabilidad.Fuego_Armadura: if (HUDJugador.Instancia.iconoFuegoQ != null) return HUDJugador.Instancia.iconoFuegoQ; break;
                    case IdHabilidad.Fuego_Meteorito: if (HUDJugador.Instancia.iconoFuegoR != null) return HUDJugador.Instancia.iconoFuegoR; break;

                    case IdHabilidad.Hielo_RayoHielo: if (HUDJugador.Instancia.iconoHieloLMB != null) return HUDJugador.Instancia.iconoHieloLMB; break;
                    case IdHabilidad.Hielo_BolaHielo: if (HUDJugador.Instancia.iconoHieloE != null) return HUDJugador.Instancia.iconoHieloE; break;
                    case IdHabilidad.Hielo_Armadura: if (HUDJugador.Instancia.iconoHieloQ != null) return HUDJugador.Instancia.iconoHieloQ; break;
                    case IdHabilidad.Hielo_EraHielo: if (HUDJugador.Instancia.iconoHieloR != null) return HUDJugador.Instancia.iconoHieloR; break;

                    case IdHabilidad.Elec_Espada: if (HUDJugador.Instancia.iconoElecLMB != null) return HUDJugador.Instancia.iconoElecLMB; break;
                    case IdHabilidad.Elec_Dash: if (HUDJugador.Instancia.iconoElecE != null) return HUDJugador.Instancia.iconoElecE; break;
                    case IdHabilidad.Elec_Teleport: if (HUDJugador.Instancia.iconoElecQ != null) return HUDJugador.Instancia.iconoElecQ; break;
                    case IdHabilidad.Elec_Rayo: if (HUDJugador.Instancia.iconoElecR != null) return HUDJugador.Instancia.iconoElecR; break;
                }
            }

            // 2. Cargar directamente desde Resources/IconosHabilidades/
            var sp = Resources.Load<Sprite>("IconosHabilidades/" + id.ToString());
            if (sp != null) return sp;

            // 3. Fallbacks elementales temáticos
            string idStr = id.ToString();
            if (idStr.StartsWith("Fuego")) return _spIconFlame;
            if (idStr.StartsWith("Hielo")) return _spIconSnow;
            if (idStr.StartsWith("Elec")) return _spIconBolt;
            return _spIconPaw;
        }

        private void ObtenerDetallesMejora(TipoMejoraTienda tipo, int nivelActual, out string nombre, out string desc, out string tag, out string slot, out Color32 colAccent, out Color32 colTag, out Sprite spIcono, out bool esUniversal)
        {
            IdHabilidad idHab = CatalogoMejorasTienda.ObtenerIdHabilidadDeMejora(tipo);
            esUniversal = (idHab == IdHabilidad.Ninguna);

            if (!esUniversal)
            {
                spIcono = ObtenerSpriteHabilidadReal(idHab);
            }
            else
            {
                spIcono = _spIconPaw;
            }

            switch (tipo)
            {
                // Hielo
                case TipoMejoraTienda.Hielo_DuracionCongelacion:
                    nombre = "Garras de Escarcha";
                    desc = "Tus ataques básicos ralentizan y congelan por más tiempo.";
                    tag = "ATAQUE BÁSICO • HIELO";
                    slot = "CLIC";
                    colAccent = new Color32(0x58, 0xC4, 0xFF, 0xFF);
                    colTag = new Color32(0x8F, 0xE0, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.Hielo_AreaVentisca:
                    nombre = "Aliento Helado";
                    desc = "Tu ráfaga de hielo y esferas aumentan su área de impacto.";
                    tag = "HABILIDAD 1 • HIELO";
                    slot = "E";
                    colAccent = new Color32(0x58, 0xC4, 0xFF, 0xFF);
                    colTag = new Color32(0x8F, 0xE0, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.Hielo_PotenciaGlacial:
                    nombre = "Fragmentación Glacial";
                    desc = "Mayor daño crítico contra enemigos ralentizados o congelados.";
                    tag = "HABILIDAD 2 • HIELO";
                    slot = "Q";
                    colAccent = new Color32(0x58, 0xC4, 0xFF, 0xFF);
                    colTag = new Color32(0x8F, 0xE0, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.Hielo_TormentaEterna:
                    nombre = "Ventisca Eterna";
                    desc = "Tu definitiva Era de Hielo cubre un radio colosal.";
                    tag = "DEFINITIVA • HIELO";
                    slot = "R";
                    colAccent = new Color32(0x58, 0xC4, 0xFF, 0xFF);
                    colTag = new Color32(0x8F, 0xE0, 0xFF, 0xFF);
                    break;

                // Rayo / Eléctrico
                case TipoMejoraTienda.Elec_Voltaje:
                    nombre = "Filo Relámpago";
                    desc = "Tus ataques con espada eléctrica atraviesan más enemigos.";
                    tag = "ATAQUE BÁSICO • RAYO";
                    slot = "CLIC";
                    colAccent = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    colTag = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    break;
                case TipoMejoraTienda.Elec_CooldownDash:
                    nombre = "Chispa Veloz";
                    desc = "Reduce el enfriamiento del dash eléctrico para mayor movilidad.";
                    tag = "HABILIDAD 1 • RAYO";
                    slot = "E";
                    colAccent = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    colTag = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    break;
                case TipoMejoraTienda.Elec_AreaTeleport:
                    nombre = "Cadena Eléctrica";
                    desc = "El rayo salta entre más enemigos cercanos antes de apagarse.";
                    tag = "HABILIDAD 2 • RAYO";
                    slot = "Q";
                    colAccent = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    colTag = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    break;
                case TipoMejoraTienda.Elec_RayoCataclismico:
                    nombre = "Rayo Cataclísmico";
                    desc = "Tu definitiva desata una tormenta de rayos devastadora.";
                    tag = "DEFINITIVA • RAYO";
                    slot = "R";
                    colAccent = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    colTag = new Color32(0xFF, 0xE1, 0x4A, 0xFF);
                    break;

                // Fuego
                case TipoMejoraTienda.Fuego_DanoQuemadura:
                    nombre = "Garras de Fuego";
                    desc = "Tus bolas de fuego dejan quemaduras más ardientes y duraderas.";
                    tag = "ATAQUE BÁSICO • FUEGO";
                    slot = "CLIC";
                    colAccent = new Color32(0xFF, 0x7A, 0x2F, 0xFF);
                    colTag = new Color32(0xFF, 0xB3, 0x6B, 0xFF);
                    break;
                case TipoMejoraTienda.Fuego_Area:
                    nombre = "Llamarada Expansiva";
                    desc = "El lanzallamas cubre un cono mucho más ancho y letal.";
                    tag = "HABILIDAD 1 • FUEGO";
                    slot = "E";
                    colAccent = new Color32(0xFF, 0x7A, 0x2F, 0xFF);
                    colTag = new Color32(0xFF, 0xB3, 0x6B, 0xFF);
                    break;
                case TipoMejoraTienda.Fuego_Duracion:
                    nombre = "Ignición Prolongada";
                    desc = "La armadura de fuego resiste más golpes y quema todo.";
                    tag = "HABILIDAD 2 • FUEGO";
                    slot = "Q";
                    colAccent = new Color32(0xFF, 0x7A, 0x2F, 0xFF);
                    colTag = new Color32(0xFF, 0xB3, 0x6B, 0xFF);
                    break;
                case TipoMejoraTienda.Fuego_SupernovaGigante:
                    nombre = "Infierno Felino";
                    desc = "Tu definitiva dura más tiempo y quema todo lo que toque.";
                    tag = "DEFINITIVA • FUEGO";
                    slot = "R";
                    colAccent = new Color32(0xFF, 0x7A, 0x2F, 0xFF);
                    colTag = new Color32(0xFF, 0xB3, 0x6B, 0xFF);
                    break;

                // Universales / Globales (Icono de "?" e insignia de "★")
                case TipoMejoraTienda.VidaMaxima:
                    nombre = "Siete Vidas";
                    desc = "Aumenta tu vida máxima permanente para aguantar más golpes.";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.PescadoAsado:
                    nombre = "Pescado Asado";
                    desc = "Restaura 60 Puntos de Vida de inmediato al consumirlo.";
                    tag = "CONSUMIBLE";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.DanoUniversal:
                    nombre = "Poder Arcano";
                    desc = "+8% daño general adicional en todos tus ataques y habilidades.";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.CooldownUniversal:
                    nombre = "Reloj Arcano";
                    desc = "Reduce un 7% el tiempo de recarga de todas tus habilidades.";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.AreaHabilidades:
                    nombre = "Amplitud Mística";
                    desc = "+12% tamaño y radio de impacto de tus hechizos y proyectiles.";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.CooldownDefinitiva:
                    nombre = "Energía Celestial";
                    desc = "Recarga tu Habilidad Definitiva (R) un 10% más rápido.";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
                case TipoMejoraTienda.GolpeCritico:
                default:
                    nombre = "Zarpa Fulminante";
                    desc = "+8% probabilidad de asestar golpes críticos (1.8x daño).";
                    tag = "MEJORA GLOBAL";
                    slot = "★";
                    colAccent = new Color32(0xC9, 0xA8, 0xFF, 0xFF);
                    colTag = new Color32(0xD6, 0xC2, 0xFF, 0xFF);
                    break;
            }
        }

        private void OnClickComprar(int index)
        {
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.ComprarArticuloTiendaIndex(index);
            }
        }

        public void NotificarCompraExitosa(TipoMejoraTienda tipo)
        {
            string[] respuestas = {
                "¡Buena elección, aventurero!",
                "¡Ese poder te sentará de maravilla!",
                "¡Excelente compra! La run será épica.",
                "¡El poder místico fluye en ti!"
            };
            string frase = respuestas[UnityEngine.Random.Range(0, respuestas.Length)];
            SetDialogo(frase, 4f);
        }

        private void SetDialogo(string texto, float duracion = -1f)
        {
            if (_txtBocadillo != null)
            {
                _txtBocadillo.text = texto;
            }
            _timerDialogoRestaurar = duracion;
        }

        // =========================================================================
        // CICLO DE ANIMACIÓN EN TIEMPO REAL (PARPADEO, VELAS, MOTAS, HALOS)
        // =========================================================================
        void Update()
        {
            if (!gameObject.activeSelf) return;

            // Salir con tecla ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
                return;
            }

            float t = Time.unscaledTime;

            // 1. Restaurar diálogo del gato
            if (_timerDialogoRestaurar > 0f)
            {
                _timerDialogoRestaurar -= Time.unscaledDeltaTime;
                if (_timerDialogoRestaurar <= 0f)
                {
                    SetDialogo("Acércate… ¿qué buscas?");
                }
            }

            // 2. Parpadeo de ojos del gato tendero
            if (_rtCatEyes != null)
            {
                if (_parpadeando)
                {
                    _tiempoParpadeoRestante -= Time.unscaledDeltaTime;
                    if (_tiempoParpadeoRestante <= 0f)
                    {
                        _parpadeando = false;
                        _rtCatEyes.localScale = Vector3.one;
                        _timerParpadeo = UnityEngine.Random.Range(3.8f, 6.2f);
                    }
                }
                else
                {
                    _timerParpadeo -= Time.unscaledDeltaTime;
                    if (_timerParpadeo <= 0f)
                    {
                        _parpadeando = true;
                        _tiempoParpadeoRestante = 0.14f;
                        _rtCatEyes.localScale = new Vector3(1f, 0.15f, 1f);
                    }
                }
            }

            // 3. Cabeceo sutil del gato (1px paso)
            if (_rtCat != null)
            {
                float bob = (Mathf.Floor(t * 0.9f) % 2 == 0) ? 0f : -2f;
                _rtCat.anchoredPosition = new Vector2(170f, -612f + bob);
            }

            // 4. Pulso de la gema mística del pecho
            if (_imgCatGem != null)
            {
                float pulso = 0.65f + 0.35f * Mathf.Sin(t * 3.5f);
                _imgCatGem.color = new Color(1f, 1f, 1f, pulso);
            }

            // 5. Brillo ambiente circular del gato (sin cortes)
            if (_imgCatGlow != null)
            {
                float halo = 0.20f + 0.08f * Mathf.Sin(t * 2.2f);
                _imgCatGlow.color = new Color(1f, 0.55f, 0.20f, halo);
            }

            // 6. Parpadeo de la llama de la vela y su halo circular suave
            if (_rtVelaLlama != null)
            {
                float flick = Mathf.Sin(t * 16f) * 1.5f;
                _rtVelaLlama.anchoredPosition = new Vector2(508f + flick, -820f);
            }
            if (_imgVelaHalo != null)
            {
                float haloVela = 0.30f + 0.10f * Mathf.Sin(t * 14f);
                _imgVelaHalo.color = new Color(1f, 0.68f, 0.24f, haloVela);
            }

            // 7. Ascenso y desvanecimiento de motas mágicas
            for (int i = 0; i < _motas.Count; i++)
            {
                var m = _motas[i];
                float yOffset = Mathf.Repeat((t + m.fase) * m.velocidad, m.distanciaMax);
                float progreso = yOffset / m.distanciaMax;
                float alfa = Mathf.Sin(progreso * Mathf.PI);

                m.rt.anchoredPosition = new Vector2(m.posBase.x, -m.posBase.y + yOffset);
                Color c = m.img.color;
                c.a = alfa * 0.85f;
                m.img.color = c;
            }
        }

        // =========================================================================
        // MÉTODOS DE CREACIÓN DE ELEMENTOS UI
        // =========================================================================
        private RectTransform CrearRect(Transform parent, string nombre, float x, float y, float w, float h)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private RectTransform CrearRectCentrado(Transform parent, string nombre, float x, float y, float w, float h)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private void CrearPoste(Transform parent, string nombre, float x, float y, float w, float h)
        {
            var p = CrearRect(parent, nombre, x, y, w, h);
            var img = p.gameObject.AddComponent<Image>();
            img.color = new Color32(0x8A, 0x5A, 0x3A, 0xFF);
            CrearBordeCaja(p.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xA9, 0x72, 0x48, 0xFF), new Color32(0x5A, 0x3A, 0x22, 0xFF));
        }

        private void CrearRepisa(Transform parent, string nombre, float x, float y, float w, float h)
        {
            var r = CrearRect(parent, nombre, x, y, w, h);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color32(0xA9, 0x72, 0x48, 0xFF);
            CrearBordeCaja(r.transform, 6, new Color32(0x0C, 0x08, 0x18, 0xFF), new Color32(0xC5, 0x8F, 0x5C, 0xFF), new Color32(0x5A, 0x3A, 0x22, 0xFF));
        }

        private void CrearFarol(Transform parent, string nombre, float xCad, float yCad, float xFarol, float yFarol)
        {
            CrearCadena(parent, $"{nombre}_Cadena", xCad, yCad, 52f);

            // Halo circular suave alrededor del farol
            var haloFarol = CrearRectCentrado(parent, $"{nombre}_HaloCircular", xFarol + 18f, -(yFarol + 24f), 160f, 160f);
            var imgHalo = haloFarol.gameObject.AddComponent<Image>();
            imgHalo.sprite = ObtenerSpriteGlowCircular();
            imgHalo.color = new Color(1f, 0.72f, 0.30f, 0.25f);

            var farol = CrearRect(parent, nombre, xFarol, yFarol, 36f, 48f);
            var img = farol.gameObject.AddComponent<Image>();
            img.sprite = _spLantern;
            img.color = Color.white;
        }

        private void CrearCadena(Transform parent, string nombre, float x, float y, float h)
        {
            var c = CrearRect(parent, nombre, x, y, 6f, h);
            var img = c.gameObject.AddComponent<Image>();
            img.color = new Color32(0x8A, 0x90, 0xAA, 0xFF);
        }

        private void CrearMota(Transform parent, Vector2 pos, Color32 color, float velocidad, float fase)
        {
            var mObj = CrearRect(parent, "MotaMagica", pos.x, pos.y, 6f, 6f);
            var img = mObj.gameObject.AddComponent<Image>();
            img.color = color;
            _motas.Add(new MotaMagica
            {
                rt = mObj,
                img = img,
                posBase = pos,
                velocidad = velocidad,
                fase = fase,
                distanciaMax = 70f
            });
        }

        private void CrearBordeInterior(Transform parent, Color32 contorno, Color32 luz)
        {
            var bTop = CrearRect(parent, "BordeLuz", 0f, 0f, 1776f, 6f);
            var imgTop = bTop.gameObject.AddComponent<Image>();
            imgTop.color = luz;

            var bOut = CrearRect(parent, "BordeContorno", 0f, -6f, 1776f, 6f);
            var imgOut = bOut.gameObject.AddComponent<Image>();
            imgOut.color = contorno;
        }

        private void CrearBordeCaja(Transform parent, int grosor, Color32 contorno, Color32 luz, Color32 sombra)
        {
            var rtParent = parent.GetComponent<RectTransform>();
            float w = rtParent.sizeDelta.x;
            float h = rtParent.sizeDelta.y;

            var lTop = CrearRect(parent, "LineTop", 0f, 0f, w, grosor);
            var imgLTop = lTop.gameObject.AddComponent<Image>();
            imgLTop.color = luz;

            var lBot = CrearRect(parent, "LineBot", 0f, h - grosor, w, grosor);
            var imgLBot = lBot.gameObject.AddComponent<Image>();
            imgLBot.color = sombra;
        }
    }
}
