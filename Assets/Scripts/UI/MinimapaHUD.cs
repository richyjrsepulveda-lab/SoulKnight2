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
        private RawImage _rawImage;
        private Texture2D _texMapa;
        private Color32[] _pixeles;

        private const int MAP_W = 120;
        private const int MAP_H = 84;

        private float _timerActualizacion = 0f;

        public static MinimapaHUD CrearEnHUD(Transform canvasParent)
        {
            var marcoObj = new GameObject("MarcoMinimapa");
            marcoObj.transform.SetParent(canvasParent, false);

            var imgMarco = marcoObj.AddComponent<Image>();
            imgMarco.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            var rtMarco = marcoObj.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(1f, 1f);
            rtMarco.anchorMax = new Vector2(1f, 1f);
            rtMarco.pivot = new Vector2(1f, 1f);
            rtMarco.anchoredPosition = new Vector2(-16f, -54f);
            rtMarco.sizeDelta = new Vector2(224f, 168f);

            var bordeObj = new GameObject("BordeMinimapa");
            bordeObj.transform.SetParent(marcoObj.transform, false);
            var imgBorde = bordeObj.AddComponent<Image>();
            imgBorde.color = new Color(0.28f, 0.36f, 0.48f, 0.65f);
            var rtBorde = bordeObj.GetComponent<RectTransform>();
            rtBorde.anchorMin = Vector2.zero;
            rtBorde.anchorMax = Vector2.one;
            rtBorde.sizeDelta = new Vector2(-4f, -4f);

            var rawObj = new GameObject("LienzoMinimapa");
            rawObj.transform.SetParent(marcoObj.transform, false);
            var raw = rawObj.AddComponent<RawImage>();
            var rtRaw = rawObj.GetComponent<RectTransform>();
            rtRaw.anchorMin = Vector2.zero;
            rtRaw.anchorMax = Vector2.one;
            rtRaw.offsetMin = new Vector2(6f, 6f);
            rtRaw.offsetMax = new Vector2(-6f, -24f);

            var lblObj = new GameObject("TituloMinimapa");
            lblObj.transform.SetParent(marcoObj.transform, false);
            var txt = lblObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.75f, 0.88f, 1f, 0.9f);
            txt.text = "MINIMAPA";
            var rtLbl = lblObj.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0f, 1f);
            rtLbl.anchorMax = new Vector2(1f, 1f);
            rtLbl.pivot = new Vector2(0.5f, 1f);
            rtLbl.anchoredPosition = new Vector2(0f, -3f);
            rtLbl.sizeDelta = new Vector2(0f, 18f);

            var comp = marcoObj.AddComponent<MinimapaHUD>();
            comp.Inicializar(raw);
            return comp;
        }

        public void Inicializar(RawImage raw)
        {
            _rawImage = raw;
            _texMapa = new Texture2D(MAP_W, MAP_H, TextureFormat.RGBA32, false);
            _texMapa.filterMode = FilterMode.Point;
            _texMapa.wrapMode = TextureWrapMode.Clamp;
            _pixeles = new Color32[MAP_W * MAP_H];
            _rawImage.texture = _texMapa;
        }

        void OnDestroy()
        {
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

        private void RenderizarMinimapa()
        {
            var gen = GeneradorMazmorra.Instancia;
            var vision = SistemaVision.Instancia;
            if (gen == null || gen.gridActual == null) return;

            Color32 colFondo = new Color32(8, 10, 16, 245);
            for (int i = 0; i < _pixeles.Length; i++)
            {
                _pixeles[i] = colFondo;
            }

            int minX = gen.gridMinX;
            int maxX = gen.gridMaxX;
            int minY = gen.gridMinY;
            int maxY = gen.gridMaxY;
            float spanX = Mathf.Max(1f, maxX - minX);
            float spanY = Mathf.Max(1f, maxY - minY);

            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);

            // Colores: Bloques de los bordes siempre visibles; suelo explorado y objetos con total normalidad
            Color32 colSueloNormal = new Color32(78, 104, 136, 255);
            Color32 colSueloCombate = new Color32(115, 52, 52, 255);
            Color32 colMuroNormal = new Color32(180, 200, 225, 255);

            ControladorSala salaCerrada = vision != null ? vision.ObtenerSalaBloqueadaActiva() : null;
            bool haySalaCerrada = (salaCerrada != null);
            Rect rectSalaCerrada = haySalaCerrada ? salaCerrada.limitesSala : default;

            for (int py = 0; py < MAP_H; py++)
            {
                float wy = minY + ((py + 0.5f) / MAP_H) * spanY;
                int gy = Mathf.RoundToInt(wy) - minY;
                if (gy < 0 || gy >= gridH) continue;

                int rowOffset = py * MAP_W;

                for (int px = 0; px < MAP_W; px++)
                {
                    float wx = minX + ((px + 0.5f) / MAP_W) * spanX;
                    int gx = Mathf.RoundToInt(wx) - minX;
                    if (gx < 0 || gx >= gridW) continue;

                    byte celda = gen.gridActual[gx, gy];
                    if (celda == 0) continue;

                    if (celda == 2) // Todos los bloques de los bordes siempre visibles sin importar la luz
                    {
                        _pixeles[rowOffset + px] = colMuroNormal;
                    }
                    else if (celda == 1) // El mapa se va revelando con total normalidad según vas explorando
                    {
                        bool explorada = (vision == null) || vision.EstaCeldaExplorada(gx, gy);
                        if (explorada)
                        {
                            Vector2 posMundo = new Vector2(wx, wy);
                            bool enSalaCombate = haySalaCerrada && rectSalaCerrada.Contains(posMundo);
                            _pixeles[rowOffset + px] = enSalaCombate ? colSueloCombate : colSueloNormal;
                        }
                    }
                }
            }

            // Pasadizo / Hoyo final (aparece con normalidad al explorar)
            if (PasajeSiguienteNivel.Instancia != null)
            {
                Vector2 posHoyo = PasajeSiguienteNivel.Instancia.transform.position;
                if (vision == null || vision.EstaPuntoExplorado(posHoyo))
                {
                    DibujarPuntoMundo(posHoyo, minX, minY, spanX, spanY, new Color32(235, 195, 95, 255), 2);
                }
            }

            // Pociones / Mejoras en el suelo (aparecen con total normalidad)
            for (int i = 0; i < ItemMejoraRun.Todos.Count; i++)
            {
                var item = ItemMejoraRun.Todos[i];
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                DibujarPuntoMundo(item.transform.position, minX, minY, spanX, spanY, new Color32(80, 255, 220, 255), 2);
            }

            // Enemigos (en el mapa la visión SOLO aplica para mostrar u ocultar a los enemigos)
            for (int i = 0; i < EnemigoBase.Todos.Count; i++)
            {
                var e = EnemigoBase.Todos[i];
                if (e == null) continue;
                Vector2 posE = e.transform.position;

                if (vision == null || vision.TieneVisionEnPunto(posE))
                {
                    bool esJefe = (e is JefeBase);
                    Color32 colEnem = esJefe ? new Color32(255, 50, 220, 255) : new Color32(255, 55, 55, 255);
                    int radio = esJefe ? 3 : 2;
                    DibujarPuntoMundo(posE, minX, minY, spanX, spanY, colEnem, radio);
                }
            }

            // Jugadores Aliados
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj == null || !pj.gameObject.activeInHierarchy) continue;

                Color32 colPj = pj.EsControlLocal
                    ? new Color32(60, 255, 100, 255)
                    : new Color32(70, 200, 255, 255);

                DibujarPuntoMundo(pj.transform.position, minX, minY, spanX, spanY, colPj, 2);
            }

            _texMapa.SetPixels32(_pixeles);
            _texMapa.Apply(false);
        }

        private void DibujarPuntoMundo(Vector2 posMundo, int minX, int minY, float spanX, float spanY, Color32 color, int radio)
        {
            int cx = Mathf.RoundToInt(((posMundo.x - minX) / spanX) * (MAP_W - 1));
            int cy = Mathf.RoundToInt(((posMundo.y - minY) / spanY) * (MAP_H - 1));

            for (int dy = -radio; dy <= radio; dy++)
            {
                int py = cy + dy;
                if (py < 0 || py >= MAP_H) continue;
                for (int dx = -radio; dx <= radio; dx++)
                {
                    int px = cx + dx;
                    if (px < 0 || px >= MAP_W) continue;
                    _pixeles[py * MAP_W + px] = color;
                }
            }
        }
    }
}
