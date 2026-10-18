using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Sistema de Oscuridad Absoluta con Círculo de Visión del Personaje:
    /// - Se asegura en Start/LateUpdate de estar siempre activo.
    /// - SOLO puedes ver lo que está dentro del círculo de visión de tu personaje (y tus aliados)
    ///   y un pequeño radio alrededor de las antorchas encendidas.
    /// - Todo lo que esté fuera de ese círculo o detrás de una pared/puerta cerrada es 100% NEGRO.
    /// </summary>
    public class SistemaVision : MonoBehaviour
    {
        public static SistemaVision Instancia { get; private set; }

        [Header("Configuración de Luz")]
        public float radioLuzJugador = 26f;      // Visión amplia total que cubre toda la pantalla sin visión de túnel
        public float radioLuzAntorcha = 25f;    // Iluminación expansiva y potente de cada antorcha
        public float oscuridadMaxima = 75f;     // Sombra ambiental suave en vez de oscuridad negra impenetrable (255)

        private SpriteRenderer _overlayNiebla;
        private Texture2D _texNiebla;
        private Color32[] _pixelesNiebla;

        private const int TEX_W = 120;
        private const int TEX_H = 75;

        private struct FuenteAntorcha
        {
            public Vector2 pos;
            public float radioEfectivo;
        }

        private readonly List<Vector2> _fuentesJugadores = new List<Vector2>(8);
        private readonly List<FuenteAntorcha> _fuentesAntorchas = new List<FuenteAntorcha>(24);
        private readonly List<Rect> _salasDescubiertasEnCamara = new List<Rect>(16);

        private byte[,] _gridReferencia;
        private bool[,] _celdasExploradas;

        public bool EstaCeldaExplorada(int gx, int gy)
        {
            if (_celdasExploradas == null) return false;
            if (gx < 0 || gx >= _celdasExploradas.GetLength(0) || gy < 0 || gy >= _celdasExploradas.GetLength(1))
                return false;
            return _celdasExploradas[gx, gy];
        }

        public bool EstaPuntoExplorado(Vector2 posMundo)
        {
            var gen = GeneradorMazmorra.Instancia;
            if (gen == null || _celdasExploradas == null) return false;
            int gx = Mathf.RoundToInt(posMundo.x) - gen.gridMinX;
            int gy = Mathf.RoundToInt(posMundo.y) - gen.gridMinY;
            return EstaCeldaExplorada(gx, gy);
        }

        public void ForzarActualizacionExploracion()
        {
            var gen = GeneradorMazmorra.Instancia;
            if (gen == null || gen.gridActual == null) return;

            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);
            if (_gridReferencia != gen.gridActual || _celdasExploradas == null || 
                _celdasExploradas.GetLength(0) != gridW || _celdasExploradas.GetLength(1) != gridH)
            {
                _gridReferencia = gen.gridActual;
                _celdasExploradas = new bool[gridW, gridH];
            }

            for (int i = 0; i < gen.salasGeneradas.Count; i++)
            {
                var s = gen.salasGeneradas[i];
                if (s == null || !s.descubierta) continue;

                int sxMin = Mathf.Max(0, Mathf.FloorToInt(s.limitesSala.xMin) - 1 - gen.gridMinX);
                int sxMax = Mathf.Min(gridW - 1, Mathf.CeilToInt(s.limitesSala.xMax) + 1 - gen.gridMinX);
                int syMin = Mathf.Max(0, Mathf.FloorToInt(s.limitesSala.yMin) - 1 - gen.gridMinY);
                int syMax = Mathf.Min(gridH - 1, Mathf.CeilToInt(s.limitesSala.yMax) + 1 - gen.gridMinY);
                for (int gx = sxMin; gx <= sxMax; gx++)
                {
                    for (int gy = syMin; gy <= syMax; gy++)
                    {
                        if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                        {
                            _celdasExploradas[gx, gy] = true;
                        }
                    }
                }
            }

            // Marcar celdas del entorno inmediato de los personajes presentes
            int rInt = Mathf.CeilToInt(radioLuzJugador) + 1;
            float rExpSq = (radioLuzJugador + 0.8f) * (radioLuzJugador + 0.8f);
            ControladorSala salaCerrada = ObtenerSalaBloqueadaActiva();

            for (int j = 0; j < Personaje.Todos.Count; j++)
            {
                var pj = Personaje.Todos[j];
                if (pj == null || !pj.gameObject.activeInHierarchy) continue;
                Vector2 orig = pj.transform.position;
                int cx = Mathf.RoundToInt(orig.x) - gen.gridMinX;
                int cy = Mathf.RoundToInt(orig.y) - gen.gridMinY;
                int x0 = Mathf.Max(0, cx - rInt);
                int x1 = Mathf.Min(gridW - 1, cx + rInt);
                int y0 = Mathf.Max(0, cy - rInt);
                int y1 = Mathf.Min(gridH - 1, cy + rInt);

                for (int gx = x0; gx <= x1; gx++)
                {
                    for (int gy = y0; gy <= y1; gy++)
                    {
                        if (gx < 0 || gx >= gridW || gy < 0 || gy >= gridH) continue;
                        if (_celdasExploradas[gx, gy]) continue;
                        if (gen.gridActual[gx, gy] == 0) continue;

                        Vector2 posCelda = new Vector2(gx + gen.gridMinX, gy + gen.gridMinY);
                        if ((orig - posCelda).sqrMagnitude <= rExpSq)
                        {
                            if (HayLineaDeVisionSinAtravesarPared(orig, posCelda, gen, salaCerrada))
                            {
                                _celdasExploradas[gx, gy] = true;
                            }
                        }
                    }
                }
            }
        }

        public static void AsegurarInstancia()
        {
            if (Instancia != null) return;
            var existente = FindObjectOfType<SistemaVision>();
            if (existente != null)
            {
                Instancia = existente;
                return;
            }
            var go = new GameObject("SistemaVisionGlobal");
            Instancia = go.AddComponent<SistemaVision>();
        }

        void Awake()
        {
            Instancia = this;
            radioLuzJugador = Mathf.Max(26f, radioLuzJugador);
            radioLuzAntorcha = Mathf.Max(25f, radioLuzAntorcha);
            InicializarOverlayNiebla();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            if (_texNiebla != null) Destroy(_texNiebla);
        }

        private void InicializarOverlayNiebla()
        {
            if (_overlayNiebla != null) return;

            _texNiebla = new Texture2D(TEX_W, TEX_H, TextureFormat.RGBA32, false);
            _texNiebla.filterMode = FilterMode.Bilinear;
            _texNiebla.wrapMode = TextureWrapMode.Clamp;
            _pixelesNiebla = new Color32[TEX_W * TEX_H];

            // Inicializar completamente en negro absoluto
            for (int i = 0; i < _pixelesNiebla.Length; i++)
            {
                _pixelesNiebla[i] = new Color32(0, 0, 0, 255);
            }
            _texNiebla.SetPixels32(_pixelesNiebla);
            _texNiebla.Apply(false);

            var go = new GameObject("OverlayOscuridadAbsoluta");
            go.transform.SetParent(transform);
            _overlayNiebla = go.AddComponent<SpriteRenderer>();
            _overlayNiebla.sprite = Sprite.Create(_texNiebla, new Rect(0, 0, TEX_W, TEX_H), new Vector2(0.5f, 0.5f), 10f);
            _overlayNiebla.sortingOrder = 1; // Cubre el suelo (0), dejando visibles los bloques de borde (2..4) y pociones (6)
        }

        /// <summary>
        /// Verifica que no haya paredes ni puertas cerradas entre el foco de luz y el punto.
        /// </summary>
        public bool HayLineaDeVisionSinAtravesarPared(Vector2 origen, Vector2 destino, GeneradorMazmorra gen, ControladorSala salaCerrada)
        {
            if (gen == null || gen.gridActual == null) return true;

            if (salaCerrada != null)
            {
                bool origenDentro = salaCerrada.limitesSala.Contains(origen);
                bool destinoDentro = salaCerrada.limitesSala.Contains(destino);
                if (origenDentro != destinoDentro) return false;
            }

            Vector2 diff = destino - origen;
            float dist = diff.magnitude;
            if (dist <= 0.6f) return true;

            int minX = gen.gridMinX;
            int minY = gen.gridMinY;
            int gridW = gen.gridActual.GetLength(0);
            int gridH = gen.gridActual.GetLength(1);

            int pasos = Mathf.CeilToInt(dist / 0.5f);
            Vector2 pasoVec = diff / pasos;

            // Recorremos hasta pasos - 1 para iluminar la cara interior de la pared, pero bloquear todo detrás de ella
            for (int i = 1; i < pasos; i++)
            {
                Vector2 p = origen + pasoVec * i;
                int gx = Mathf.RoundToInt(p.x) - minX;
                int gy = Mathf.RoundToInt(p.y) - minY;
                if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                {
                    if (gen.gridActual[gx, gy] != 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public ControladorSala ObtenerSalaBloqueadaActiva()
        {
            var gen = GeneradorMazmorra.Instancia;
            if (gen == null) return null;
            for (int i = 0; i < gen.salasGeneradas.Count; i++)
            {
                var s = gen.salasGeneradas[i];
                if (s != null && s.enCombate) return s;
            }
            return null;
        }

        /// <summary>
        /// Devuelve true si el punto es visible (visibilidad completa de todas las entidades).
        /// </summary>
        public bool TieneVisionEnPunto(Vector2 pos)
        {
            return true;
        }

        void LateUpdate()
        {
            InicializarOverlayNiebla();
            if (_overlayNiebla != null)
            {
                _overlayNiebla.enabled = false; // Iluminación total del 100% en todas las salas y pasillos del mapa sin recortar nada
            }

            ForzarActualizacionExploracion();

            for (int i = 0; i < EnemigoBase.Todos.Count; i++)
            {
                var e = EnemigoBase.Todos[i];
                if (e != null) e.EstablecerVisibilidad(true);
            }
        }
    }
}
