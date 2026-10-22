using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Minimapa dinámico en tiempo real ubicado arriba a la derecha del HUD:
    /// - Cuadro amplio y claro en pantalla sin zoom (mantiene la escala de tiles natural).
    /// - Las salas y pasillos se descubren conforme se exploran.
    /// - Ofrece vista completa de toda la mazmorra sin cortes para el mapa grande desplegable [M].
    /// </summary>
    public class MinimapaHUD : MonoBehaviour
    {
        public static MinimapaHUD Instancia { get; private set; }

        private RawImage _rawImage;
        private Texture2D _texMapa;
        public Texture2D TextureMapa => _texMapa;
        private Color32[] _pixeles;

        // Minimapa HUD: cuadro más grande pero SIN ZOOM (misma escala de píxeles por tile)
        private const int MAP_W = 240;
        private const int MAP_H = 170;
        private const float ESCALA_PIXELS_POR_TILE = 1.35f;

        // Mapa Completo para modal de tecla M (toda la mazmorra ajustada en pantalla)
        private Texture2D _texMapaCompleto;
        public Texture2D TextureMapaCompleto => _texMapaCompleto;
        private Color32[] _pixelesMapaCompleto;
        private const int COMPLETO_W = 480;
        private const int COMPLETO_H = 320;

        private float _timerActualizacion = 0f;

        public static MinimapaHUD CrearEnHUD(Transform canvasParent)
        {
            var existente = canvasParent.Find("MarcoMinimapa");
            if (existente != null)
            {
                var compExist = existente.GetComponent<MinimapaHUD>();
                if (compExist != null)
                {
                    var rt = existente.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(360f, 260f);
                    return compExist;
                }
                Destroy(existente.gameObject);
            }

            var marcoObj = new GameObject("MarcoMinimapa");
            marcoObj.transform.SetParent(canvasParent, false);

            var imgMarco = marcoObj.AddComponent<Image>();
            imgMarco.sprite = EstiloUIUX.ObtenerSpritePanel();
            imgMarco.type = Image.Type.Sliced;
            imgMarco.color = Color.white;

            var rtMarco = marcoObj.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(1f, 1f);
            rtMarco.anchorMax = new Vector2(1f, 1f);
            rtMarco.pivot = new Vector2(1f, 1f);
            rtMarco.anchoredPosition = new Vector2(-16f, -54f);
            rtMarco.sizeDelta = new Vector2(360f, 260f); // Cuadrito más grande sin zoom

            EstiloUIUX.CrearRemaches(marcoObj.transform, 14f);

            var fondoInterno = new GameObject("FondoInternoMinimapa");
            fondoInterno.transform.SetParent(marcoObj.transform, false);
            var imgFondoInt = fondoInterno.AddComponent<Image>();
            imgFondoInt.sprite = EstiloUIUX.ObtenerSpriteTarjeta();
            imgFondoInt.type = Image.Type.Sliced;
            imgFondoInt.color = Color.white;
            var rtFondoInt = fondoInterno.GetComponent<RectTransform>();
            rtFondoInt.anchorMin = Vector2.zero;
            rtFondoInt.anchorMax = Vector2.one;
            rtFondoInt.offsetMin = new Vector2(8f, 8f);
            rtFondoInt.offsetMax = new Vector2(-8f, -8f);

            var rawObj = new GameObject("LienzoMinimapa");
            rawObj.transform.SetParent(fondoInterno.transform, false);
            var raw = rawObj.AddComponent<RawImage>();
            var rtRaw = rawObj.GetComponent<RectTransform>();
            rtRaw.anchorMin = Vector2.zero;
            rtRaw.anchorMax = Vector2.one;
            rtRaw.offsetMin = new Vector2(4f, 4f);
            rtRaw.offsetMax = new Vector2(-4f, -24f);

            var lblObj = new GameObject("TituloMinimapa");
            lblObj.transform.SetParent(fondoInterno.transform, false);
            var txt = lblObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = EstiloUIUX.ColDorado;
            txt.text = "✦ MINIMAPA ✦";
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var rtLbl = lblObj.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 1f);
            rtLbl.anchorMax = new Vector2(1f, 1f);
            rtLbl.pivot = new Vector2(0.5f, 1f);
            rtLbl.anchoredPosition = new Vector2(0f, -3f);
            rtLbl.sizeDelta = new Vector2(0f, 20f);

            var comp = marcoObj.AddComponent<MinimapaHUD>();
            comp.Inicializar(raw);
            return comp;
        }

        public void Inicializar(RawImage raw)
        {
            Instancia = this;
            _rawImage = raw;
            if (_texMapa != null) Destroy(_texMapa);
            _texMapa = new Texture2D(MAP_W, MAP_H, TextureFormat.RGBA32, false);
            _texMapa.filterMode = FilterMode.Point;
            _texMapa.wrapMode = TextureWrapMode.Clamp;
            _pixeles = new Color32[MAP_W * MAP_H];
            Color32 initColor = new Color32(8, 10, 16, 245);
            for (int i = 0; i < _pixeles.Length; i++) _pixeles[i] = initColor;
            _texMapa.SetPixels32(_pixeles);
            _texMapa.Apply(false);
            if (_rawImage != null) _rawImage.texture = _texMapa;
        }

        void Awake()
        {
            if (Instancia == null) Instancia = this;
            if (_texMapa == null || _texMapa.width != MAP_W || _texMapa.height != MAP_H)
            {
                if (_rawImage == null) _rawImage = GetComponentInChildren<RawImage>(true);
                if (_rawImage != null)
                {
                    Inicializar(_rawImage);
                }
            }
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            if (_texMapa != null) Destroy(_texMapa);
            if (_texMapaCompleto != null) Destroy(_texMapaCompleto);
        }

        void Update()
        {
            _timerActualizacion -= Time.deltaTime;
            if (_timerActualizacion <= 0f)
            {
                _timerActualizacion = 0.1f;
                RenderizarMinimapa();
            }
        }

        public void RenderizarMinimapa()
        {
            var gen = GeneradorMazmorra.Instancia;
            var vision = SistemaVision.Instancia;
            if (_texMapa == null || _pixeles == null || _texMapa.width != MAP_W || _texMapa.height != MAP_H)
            {
                if (_rawImage == null) _rawImage = GetComponentInChildren<RawImage>(true);
                if (_rawImage != null) Inicializar(_rawImage);
                if (_texMapa == null || _pixeles == null) return;
            }

            Color32 colFondo = new Color32(8, 10, 16, 245);
            for (int i = 0; i < _pixeles.Length; i++)
            {
                _pixeles[i] = colFondo;
            }

            if (gen == null || gen.gridActual == null)
            {
                _texMapa.SetPixels32(_pixeles);
                _texMapa.Apply(false);
                return;
            }

            Vector2 centroMundo = Vector2.zero;
            Personaje pjLocal = null;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var p = Personaje.Todos[i];
                if (p != null && p.EsControlLocal && p.gameObject.activeInHierarchy)
                {
                    pjLocal = p;
                    break;
                }
            }
            if (pjLocal != null)
            {
                centroMundo = pjLocal.transform.position;
            }
            else if (CamaraUtil.Principal != null)
            {
                centroMundo = CamaraUtil.Principal.transform.position;
            }

            int minX = gen.gridMinX;
            int minY = gen.gridMinY;
            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);

            Color32 colSueloNormal = new Color32(78, 104, 136, 255);
            Color32 colSueloCombate = new Color32(115, 52, 52, 255);
            Color32 colMuroNormal = new Color32(180, 200, 225, 255);

            ControladorSala salaCerrada = (vision != null) ? vision.ObtenerSalaBloqueadaActiva() : null;
            bool haySalaCerrada = (salaCerrada != null);
            Rect rectSalaCerrada = haySalaCerrada ? salaCerrada.limitesSala : default;

            float centroPX = (MAP_W - 1) * 0.5f;
            float centroPY = (MAP_H - 1) * 0.5f;

            int[] dx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy8 = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int py = 0; py < MAP_H; py++)
            {
                float wy = centroMundo.y + (py - centroPY) / ESCALA_PIXELS_POR_TILE;
                int gy = Mathf.RoundToInt(wy) - minY;
                if (gy < 0 || gy >= gridH) continue;

                int rowOffset = py * MAP_W;

                for (int px = 0; px < MAP_W; px++)
                {
                    float wx = centroMundo.x + (px - centroPX) / ESCALA_PIXELS_POR_TILE;
                    int gx = Mathf.RoundToInt(wx) - minX;
                    if (gx < 0 || gx >= gridW) continue;

                    byte celda = gen.gridActual[gx, gy];
                    if (celda == 0) continue;

                    if (celda == 1) // Suelo
                    {
                        bool explorada = (vision == null) || vision.EstaCeldaExplorada(gx, gy);
                        if (explorada)
                        {
                            Vector2 posMundo = new Vector2(wx, wy);
                            bool enSalaCombate = haySalaCerrada && rectSalaCerrada.Contains(posMundo);
                            _pixeles[rowOffset + px] = enSalaCombate ? colSueloCombate : colSueloNormal;
                        }
                    }
                    else if (celda == 2) // Muro
                    {
                        bool muroRevelado = false;
                        if (vision == null)
                        {
                            muroRevelado = true;
                        }
                        else if (vision.EstaCeldaExplorada(gx, gy))
                        {
                            muroRevelado = true;
                        }
                        else
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                int nx = gx + dx8[k];
                                int ny = gy + dy8[k];
                                if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                                {
                                    if (gen.gridActual[nx, ny] == 1 && vision.EstaCeldaExplorada(nx, ny))
                                    {
                                        muroRevelado = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (muroRevelado)
                        {
                            _pixeles[rowOffset + px] = colMuroNormal;
                        }
                    }
                }
            }

            // Pasadizo / Hoyo final
            if (PasajeSiguienteNivel.Instancia != null)
            {
                Vector2 posHoyo = PasajeSiguienteNivel.Instancia.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posHoyo))
                {
                    DibujarPuntoMundo(posHoyo, centroMundo, new Color32(235, 195, 95, 255), 2);
                }
            }

            // Puestos de Tienda
            for (int i = 0; i < PuestitoTienda.Todos.Count; i++)
            {
                var tienda = PuestitoTienda.Todos[i];
                if (tienda == null || !tienda.gameObject.activeInHierarchy) continue;
                Vector2 posT = tienda.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posT))
                {
                    DibujarPuntoMundo(posT, centroMundo, new Color32(255, 215, 0, 255), 3);
                }
            }

            // Cofres de oro
            for (int i = 0; i < CofreOro.Todos.Count; i++)
            {
                var cofre = CofreOro.Todos[i];
                if (cofre == null || !cofre.gameObject.activeInHierarchy || cofre.EstaAbiertoLocal) continue;
                Vector2 posC = cofre.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posC))
                {
                    DibujarPuntoMundo(posC, centroMundo, new Color32(255, 175, 40, 255), 3);
                }
            }

            // Pociones / Mejoras en el suelo
            for (int i = 0; i < ItemMejoraRun.Todos.Count; i++)
            {
                var item = ItemMejoraRun.Todos[i];
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                Vector2 posItem = item.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posItem))
                {
                    Color32 col = (item.CostoOro != null && item.CostoOro.Value > 0)
                        ? new Color32(255, 215, 0, 255)
                        : new Color32(80, 255, 220, 255);
                    DibujarPuntoMundo(posItem, centroMundo, col, 2);
                }
            }

            // Enemigos
            for (int i = 0; i < EnemigoBase.Todos.Count; i++)
            {
                var e = EnemigoBase.Todos[i];
                if (e == null || !e.gameObject.activeInHierarchy) continue;
                Vector2 posE = e.transform.position;

                bool esJefe = (e is JefeBase);
                Color32 colEnem = esJefe ? new Color32(255, 50, 220, 255) : new Color32(255, 55, 55, 255);
                int radio = esJefe ? 3 : 2;
                DibujarPuntoMundo(posE, centroMundo, colEnem, radio);
            }

            // Jugadores Aliados
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj == null || !pj.gameObject.activeInHierarchy || pj.EsControlLocal) continue;
                DibujarPuntoMundo(pj.transform.position, centroMundo, new Color32(70, 200, 255, 255), 2);
            }

            // Jugador Local
            if (pjLocal != null)
            {
                DibujarPuntoMundo(pjLocal.transform.position, centroMundo, new Color32(60, 255, 100, 255), 2);
            }

            _texMapa.SetPixels32(_pixeles);
            _texMapa.Apply(false);
        }

        /// <summary>
        /// Renderiza TODA la mazmorra completa y ajustada para ver el mapa entero al abrirlo con [M].
        /// Muestra las salas y pasillos descubiertos sin recortar el escenario.
        /// </summary>
        public void RenderizarMapaCompleto()
        {
            var gen = GeneradorMazmorra.Instancia;
            var vision = SistemaVision.Instancia;
            if (_texMapaCompleto == null || _pixelesMapaCompleto == null || _texMapaCompleto.width != COMPLETO_W || _texMapaCompleto.height != COMPLETO_H)
            {
                if (_texMapaCompleto != null) Destroy(_texMapaCompleto);
                _texMapaCompleto = new Texture2D(COMPLETO_W, COMPLETO_H, TextureFormat.RGBA32, false);
                _texMapaCompleto.filterMode = FilterMode.Point;
                _texMapaCompleto.wrapMode = TextureWrapMode.Clamp;
                _pixelesMapaCompleto = new Color32[COMPLETO_W * COMPLETO_H];
            }

            Color32 colFondo = new Color32(8, 10, 16, 245);
            for (int i = 0; i < _pixelesMapaCompleto.Length; i++)
            {
                _pixelesMapaCompleto[i] = colFondo;
            }

            if (gen == null || gen.gridActual == null)
            {
                _texMapaCompleto.SetPixels32(_pixelesMapaCompleto);
                _texMapaCompleto.Apply(false);
                return;
            }

            int minX = gen.gridMinX;
            int maxX = gen.gridMaxX;
            int minY = gen.gridMinY;
            int maxY = gen.gridMaxY;
            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);

            float spanX = Mathf.Max(12f, (maxX - minX + 6));
            float spanY = Mathf.Max(12f, (maxY - minY + 6));
            Vector2 centroDungeon = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            // Escala uniforme para que TODO el piso de la mazmorra entre completo sin recortes
            float escalaCompleta = Mathf.Min((COMPLETO_W - 20) / spanX, (COMPLETO_H - 20) / spanY);
            float cPX = COMPLETO_W * 0.5f;
            float cPY = COMPLETO_H * 0.5f;

            Color32 colSueloNormal = new Color32(84, 114, 150, 255);
            Color32 colSueloCombate = new Color32(145, 55, 55, 255);
            Color32 colMuroNormal = new Color32(195, 215, 240, 255);

            ControladorSala salaCerrada = (vision != null) ? vision.ObtenerSalaBloqueadaActiva() : null;
            bool haySalaCerrada = (salaCerrada != null);
            Rect rectSalaCerrada = haySalaCerrada ? salaCerrada.limitesSala : default;

            int[] dx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy8 = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int py = 0; py < COMPLETO_H; py++)
            {
                float wy = centroDungeon.y + (py - cPY) / escalaCompleta;
                int gy = Mathf.RoundToInt(wy) - minY;
                if (gy < 0 || gy >= gridH) continue;

                int rowOffset = py * COMPLETO_W;

                for (int px = 0; px < COMPLETO_W; px++)
                {
                    float wx = centroDungeon.x + (px - cPX) / escalaCompleta;
                    int gx = Mathf.RoundToInt(wx) - minX;
                    if (gx < 0 || gx >= gridW) continue;

                    byte celda = gen.gridActual[gx, gy];
                    if (celda == 0) continue;

                    if (celda == 1) // Suelo
                    {
                        bool explorada = (vision == null) || vision.EstaCeldaExplorada(gx, gy);
                        if (explorada)
                        {
                            Vector2 posMundo = new Vector2(wx, wy);
                            bool enSalaCombate = haySalaCerrada && rectSalaCerrada.Contains(posMundo);
                            _pixelesMapaCompleto[rowOffset + px] = enSalaCombate ? colSueloCombate : colSueloNormal;
                        }
                    }
                    else if (celda == 2) // Muro
                    {
                        bool muroRevelado = false;
                        if (vision == null)
                        {
                            muroRevelado = true;
                        }
                        else if (vision.EstaCeldaExplorada(gx, gy))
                        {
                            muroRevelado = true;
                        }
                        else
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                int nx = gx + dx8[k];
                                int ny = gy + dy8[k];
                                if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                                {
                                    if (gen.gridActual[nx, ny] == 1 && vision.EstaCeldaExplorada(nx, ny))
                                    {
                                        muroRevelado = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (muroRevelado)
                        {
                            _pixelesMapaCompleto[rowOffset + px] = colMuroNormal;
                        }
                    }
                }
            }

            // Puestos de Tienda en el mapa completo
            for (int i = 0; i < PuestitoTienda.Todos.Count; i++)
            {
                var tienda = PuestitoTienda.Todos[i];
                if (tienda == null || !tienda.gameObject.activeInHierarchy) continue;
                Vector2 posT = tienda.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posT))
                {
                    DibujarPuntoMapaCompleto(posT, centroDungeon, escalaCompleta, new Color32(255, 215, 0, 255), 4);
                }
            }

            // Cofres de Oro
            for (int i = 0; i < CofreOro.Todos.Count; i++)
            {
                var cofre = CofreOro.Todos[i];
                if (cofre == null || !cofre.gameObject.activeInHierarchy || cofre.EstaAbiertoLocal) continue;
                Vector2 posC = cofre.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posC))
                {
                    DibujarPuntoMapaCompleto(posC, centroDungeon, escalaCompleta, new Color32(255, 175, 40, 255), 4);
                }
            }

            // Pasaje Siguiente Nivel / Hoyo
            if (PasajeSiguienteNivel.Instancia != null)
            {
                Vector2 posHoyo = PasajeSiguienteNivel.Instancia.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posHoyo))
                {
                    DibujarPuntoMapaCompleto(posHoyo, centroDungeon, escalaCompleta, new Color32(255, 220, 80, 255), 4);
                }
            }

            // Jugadores Aliados en el mapa completo
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj == null || !pj.gameObject.activeInHierarchy || pj.EstaMuerto.Value || pj.EsControlLocal) continue;
                DibujarPuntoMapaCompleto(pj.transform.position, centroDungeon, escalaCompleta, new Color32(70, 200, 255, 255), 3);
            }

            // Jugador Local en el mapa completo
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj != null && pj.gameObject.activeInHierarchy && pj.EsControlLocal && !pj.EstaMuerto.Value)
                {
                    DibujarPuntoMapaCompleto(pj.transform.position, centroDungeon, escalaCompleta, new Color32(60, 255, 100, 255), 4);
                    break;
                }
            }

            _texMapaCompleto.SetPixels32(_pixelesMapaCompleto);
            _texMapaCompleto.Apply(false);
        }

        private void DibujarPuntoMundo(Vector2 posMundo, Vector2 centroMundo, Color32 color, int radio)
        {
            float centroPX = (MAP_W - 1) * 0.5f;
            float centroPY = (MAP_H - 1) * 0.5f;

            int cx = Mathf.RoundToInt(centroPX + (posMundo.x - centroMundo.x) * ESCALA_PIXELS_POR_TILE);
            int cy = Mathf.RoundToInt(centroPY + (posMundo.y - centroMundo.y) * ESCALA_PIXELS_POR_TILE);

            for (int dy = -radio; dy <= radio; dy++)
            {
                int py = cy + dy;
                if (py < 0 || py >= MAP_H) continue;
                int rowOffset = py * MAP_W;
                for (int dx = -radio; dx <= radio; dx++)
                {
                    if (dx * dx + dy * dy > radio * radio) continue;
                    int px = cx + dx;
                    if (px < 0 || px >= MAP_W) continue;
                    _pixeles[rowOffset + px] = color;
                }
            }
        }

        private void DibujarPuntoMapaCompleto(Vector2 posMundo, Vector2 centroMundo, float escala, Color32 color, int radio)
        {
            float cPX = COMPLETO_W * 0.5f;
            float cPY = COMPLETO_H * 0.5f;

            int cx = Mathf.RoundToInt(cPX + (posMundo.x - centroMundo.x) * escala);
            int cy = Mathf.RoundToInt(cPY + (posMundo.y - centroMundo.y) * escala);

            for (int dy = -radio; dy <= radio; dy++)
            {
                int py = cy + dy;
                if (py < 0 || py >= COMPLETO_H) continue;
                int rowOffset = py * COMPLETO_W;
                for (int dx = -radio; dx <= radio; dx++)
                {
                    if (dx * dx + dy * dy > radio * radio) continue;
                    int px = cx + dx;
                    if (px < 0 || px >= COMPLETO_W) continue;
                    _pixelesMapaCompleto[rowOffset + px] = color;
                }
            }
        }
    }
}
