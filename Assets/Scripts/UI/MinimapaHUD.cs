using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Minimapa dinámico en tiempo real ubicado arriba a la derecha del HUD (0 lag):
    /// - Muestra TODO el mapa completo por defecto con un filtro oscuro (suelos y muros oscuros).
    /// - Ilumina con claridad el radio donde tenemos visión activa (jugadores + antorchas encendidas).
    /// - Si una puerta se cierra (combate en sala), oculta por completo el exterior en el minimapa.
    /// - Muestra a los enemigos (rojo) SOLO si están dentro de nuestro rango de visión.
    /// </summary>
    public class MinimapaHUD : MonoBehaviour
    {
        public static MinimapaHUD Instancia { get; private set; }

        private RawImage _rawImage;
        private Texture2D _texMapa;
        public Texture2D TextureMapa => _texMapa;
        private Color32[] _pixeles;

        private const int MAP_W = 160;
        private const int MAP_H = 112;
        private const float ESCALA_PIXELS_POR_TILE = 1.35f;

        private float _timerActualizacion = 0f;

        public static MinimapaHUD CrearEnHUD(Transform canvasParent)
        {
            var existente = canvasParent.Find("MarcoMinimapa");
            if (existente != null)
            {
                var compExist = existente.GetComponent<MinimapaHUD>();
                if (compExist != null) return compExist;
                Destroy(existente.gameObject);
            }

            var marcoObj = new GameObject("MarcoMinimapa");
            marcoObj.transform.SetParent(canvasParent, false);

            var imgMarco = marcoObj.AddComponent<Image>();
            imgMarco.color = new Color(0.04f, 0.06f, 0.09f, 0.96f);

            var rtMarco = marcoObj.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(1f, 1f);
            rtMarco.anchorMax = new Vector2(1f, 1f);
            rtMarco.pivot = new Vector2(1f, 1f);
            rtMarco.anchoredPosition = new Vector2(-16f, -54f);
            rtMarco.sizeDelta = new Vector2(285f, 214f); // Más grande y amplio para navegación clara

            // Borde exterior definido y nítido de estilo pixel-art
            var bordeObj = new GameObject("BordeMinimapa");
            bordeObj.transform.SetParent(marcoObj.transform, false);
            var imgBorde = bordeObj.AddComponent<Image>();
            imgBorde.color = new Color(0.32f, 0.65f, 0.95f, 0.95f); // Borde azul cian bien definido
            var rtBorde = bordeObj.GetComponent<RectTransform>();
            rtBorde.anchorMin = Vector2.zero;
            rtBorde.anchorMax = Vector2.one;
            rtBorde.sizeDelta = Vector2.zero;

            // Capa interna que deja 3px de borde marcado
            var fondoInterno = new GameObject("FondoInternoMinimapa");
            fondoInterno.transform.SetParent(bordeObj.transform, false);
            var imgFondoInt = fondoInterno.AddComponent<Image>();
            imgFondoInt.color = new Color(0.05f, 0.07f, 0.11f, 0.98f);
            var rtFondoInt = fondoInterno.GetComponent<RectTransform>();
            rtFondoInt.anchorMin = Vector2.zero;
            rtFondoInt.anchorMax = Vector2.one;
            rtFondoInt.offsetMin = new Vector2(3f, 3f);
            rtFondoInt.offsetMax = new Vector2(-3f, -3f);

            var rawObj = new GameObject("LienzoMinimapa");
            rawObj.transform.SetParent(fondoInterno.transform, false);
            var raw = rawObj.AddComponent<RawImage>();
            var rtRaw = rawObj.GetComponent<RectTransform>();
            rtRaw.anchorMin = Vector2.zero;
            rtRaw.anchorMax = Vector2.one;
            rtRaw.offsetMin = new Vector2(4f, 4f);
            rtRaw.offsetMax = new Vector2(-4f, -22f);

            var lblObj = new GameObject("TituloMinimapa");
            lblObj.transform.SetParent(fondoInterno.transform, false);
            var txt = lblObj.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 10;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            txt.text = "✦ MINIMAPA ✦";
            var rtLbl = lblObj.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 1f);
            rtLbl.anchorMax = new Vector2(1f, 1f);
            rtLbl.pivot = new Vector2(0.5f, 1f);
            rtLbl.anchoredPosition = new Vector2(0f, -2f);
            rtLbl.sizeDelta = new Vector2(0f, 18f);

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
            _rawImage.texture = _texMapa;
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
        }

        void Update()
        {
            _timerActualizacion -= Time.deltaTime;
            if (_timerActualizacion <= 0f)
            {
                _timerActualizacion = 0.1f; // 10 actualizaciones por segundo: súper ligero
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

            // Centro dinámico centrado en el jugador local (navegación fluida según avanza por el mapa)
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
            else if (Camera.main != null)
            {
                centroMundo = Camera.main.transform.position;
            }

            int minX = gen.gridMinX;
            int minY = gen.gridMinY;
            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);

            // Colores limpios y estéticos
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

            // Renderizado centrado 1:1 sin distorsión por relación de aspecto
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

                    // El mapa NO se desbloquea de golpe: se revela estrictamente conforme el jugador avanza
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
                    else if (celda == 2) // Muro: Solo visible si el muro o suelo colindante fue explorado
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

            // Pasadizo / Hoyo final (aparece al haber explorado su posición)
            if (PasajeSiguienteNivel.Instancia != null)
            {
                Vector2 posHoyo = PasajeSiguienteNivel.Instancia.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posHoyo))
                {
                    DibujarPuntoMundo(posHoyo, centroMundo, new Color32(235, 195, 95, 255), 2);
                }
            }

            // Pociones / Mejoras en el suelo (visibles si el punto ha sido explorado)
            for (int i = 0; i < ItemMejoraRun.Todos.Count; i++)
            {
                var item = ItemMejoraRun.Todos[i];
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                Vector2 posItem = item.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posItem))
                {
                    Color32 col = (item.CostoOro != null && item.CostoOro.Value > 0)
                        ? new Color32(255, 215, 0, 255)   // Dorado para tienda
                        : new Color32(80, 255, 220, 255);  // Cian para mejora libre
                    DibujarPuntoMundo(posItem, centroMundo, col, 2);
                }
            }

            // Enemigos (SOLO visibles si están dentro de nuestro radio de visión activa en el momento)
            for (int i = 0; i < EnemigoBase.Todos.Count; i++)
            {
                var e = EnemigoBase.Todos[i];
                if (e == null || !e.gameObject.activeInHierarchy) continue;
                Vector2 posE = e.transform.position;

                if (vision == null || vision.TieneVisionEnPunto(posE))
                {
                    bool esJefe = (e is JefeBase);
                    Color32 colEnem = esJefe ? new Color32(255, 50, 220, 255) : new Color32(255, 55, 55, 255);
                    int radio = esJefe ? 3 : 2;
                    DibujarPuntoMundo(posE, centroMundo, colEnem, radio);
                }
            }

            // Jugadores Aliados (si hay otros jugadores en red)
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj == null || !pj.gameObject.activeInHierarchy || pj.EsControlLocal) continue;
                DibujarPuntoMundo(pj.transform.position, centroMundo, new Color32(70, 200, 255, 255), 2);
            }

            // Jugador Local: Punto verde brillante en el centro del minimapa
            if (pjLocal != null)
            {
                DibujarPuntoMundo(pjLocal.transform.position, centroMundo, new Color32(60, 255, 100, 255), 2);
            }

            _texMapa.SetPixels32(_pixeles);
            _texMapa.Apply(false);
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
                    int px = cx + dx;
                    if (px < 0 || px >= MAP_W) continue;
                    _pixeles[rowOffset + px] = color;
                }
            }
        }
    }
}
