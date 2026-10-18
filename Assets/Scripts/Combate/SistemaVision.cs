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
        public float radioLuzJugador = 7.4f;      // Círculo de visión más amplio alrededor del personaje
        public float radioLuzAntorcha = 10.8f;    // Radio amplio y potente de cada antorcha (sin atravesar paredes)

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
        /// Devuelve true si el punto es visible (dentro de una sala descubierta o en el círculo de luz del jugador/antorchas en pasillos).
        /// </summary>
        public bool TieneVisionEnPunto(Vector2 pos)
        {
            var gen = GeneradorMazmorra.Instancia;
            ControladorSala salaCerrada = ObtenerSalaBloqueadaActiva();

            // 1. Visión total del 100% en cualquier sala que haya sido descubierta/ingresada
            if (gen != null && gen.salasGeneradas != null)
            {
                for (int i = 0; i < gen.salasGeneradas.Count; i++)
                {
                    var s = gen.salasGeneradas[i];
                    if (s == null || !s.descubierta) continue;
                    if (salaCerrada != null && salaCerrada != s) continue;
                    if (s.limitesSala.Contains(pos))
                    {
                        return true;
                    }
                }
            }

            // 2. En pasillos: visión circular del jugador (no atraviesa paredes)
            float rJugSq = radioLuzJugador * radioLuzJugador;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj == null || !pj.gameObject.activeInHierarchy) continue;
                Vector2 posPj = pj.transform.position;
                if ((posPj - pos).sqrMagnitude <= rJugSq)
                {
                    if (HayLineaDeVisionSinAtravesarPared(posPj, pos, gen, salaCerrada))
                    {
                        return true;
                    }
                }
            }

            // 3. Antorchas
            if (gen != null && gen.salasGeneradas != null)
            {
                float rAntSq = radioLuzAntorcha * radioLuzAntorcha;
                for (int i = 0; i < gen.salasGeneradas.Count; i++)
                {
                    var s = gen.salasGeneradas[i];
                    if (s == null || !s.descubierta) continue;
                    if (salaCerrada != null && salaCerrada != s) continue;

                    for (int a = 0; a < s.posicionesAntorchas.Count; a++)
                    {
                        Vector2 posAnt = s.posicionesAntorchas[a];
                        if ((posAnt - pos).sqrMagnitude <= rAntSq)
                        {
                            if (HayLineaDeVisionSinAtravesarPared(posAnt, pos, gen, salaCerrada))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        void LateUpdate()
        {
            InicializarOverlayNiebla();

            var cam = Camera.main;
            if (cam == null || _overlayNiebla == null) return;

            var gen = GeneradorMazmorra.Instancia;

            // 1. Cubrir el 100% de la cámara con margen de sobra
            float altoCam = cam.orthographicSize * 2f + 2f;
            float anchoCam = altoCam * cam.aspect + 2f;
            Vector3 posCam = cam.transform.position;
            _overlayNiebla.transform.position = new Vector3(posCam.x, posCam.y, 0f);

            float spriteW = TEX_W / 10f;
            float spriteH = TEX_H / 10f;
            _overlayNiebla.transform.localScale = new Vector3(anchoCam / spriteW, altoCam / spriteH, 1f);

            // 2. Recolectar posición de los jugadores
            _fuentesJugadores.Clear();
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj != null && pj.gameObject.activeInHierarchy)
                {
                    _fuentesJugadores.Add(pj.transform.position);
                }
            }

            // 3. Recolectar salas descubiertas y antorchas encendidas cercanas a la cámara
            _salasDescubiertasEnCamara.Clear();
            _fuentesAntorchas.Clear();
            ControladorSala salaCerrada = ObtenerSalaBloqueadaActiva();
            float xMinCam = posCam.x - anchoCam * 0.5f;
            float yMinCam = posCam.y - altoCam * 0.5f;
            Rect rectCam = new Rect(xMinCam, yMinCam, anchoCam, altoCam);
            Rect rectCamLuz = new Rect(
                xMinCam - radioLuzAntorcha - 1f,
                yMinCam - radioLuzAntorcha - 1f,
                anchoCam + (radioLuzAntorcha + 1f) * 2f,
                altoCam + (radioLuzAntorcha + 1f) * 2f
            );

            ForzarActualizacionExploracion();

            if (gen != null && gen.gridActual != null)
            {
                for (int i = 0; i < gen.salasGeneradas.Count; i++)
                {
                    var s = gen.salasGeneradas[i];
                    if (s == null || !s.descubierta) continue;

                    // Si hay una sala en combate bloqueada, solo iluminar la sala del encuentro
                    if (salaCerrada != null && salaCerrada != s) continue;

                    // Iluminar toda la sala por completo (con margen para cubrir la cara de las paredes internas)
                    Rect rSala = new Rect(
                        s.limitesSala.xMin - 0.45f,
                        s.limitesSala.yMin - 0.45f,
                        s.limitesSala.width + 0.90f,
                        s.limitesSala.height + 0.90f
                    );
                    if (rSala.Overlaps(rectCamLuz))
                    {
                        _salasDescubiertasEnCamara.Add(rSala);
                    }

                    for (int a = 0; a < s.posicionesAntorchas.Count; a++)
                    {
                        Vector2 posAnt = s.posicionesAntorchas[a];
                        if (rectCamLuz.Contains(posAnt))
                        {
                            float pulso = 0.97f + 0.03f * Mathf.Sin(Time.time * 9f + a * 1.7f + i);
                            _fuentesAntorchas.Add(new FuenteAntorcha
                            {
                                pos = posAnt,
                                radioEfectivo = radioLuzAntorcha * pulso
                            });
                        }
                    }
                }
            }

            // 4. Pintar la máscara de niebla/oscuridad
            float rJug = radioLuzJugador;
            float rJugInterno = rJug * 0.82f;
            float rJugSq = rJug * rJug;

            for (int py = 0; py < TEX_H; py++)
            {
                float wy = yMinCam + ((py + 0.5f) / TEX_H) * altoCam;
                int rowOffset = py * TEX_W;

                for (int px = 0; px < TEX_W; px++)
                {
                    float wx = xMinCam + ((px + 0.5f) / TEX_W) * anchoCam;
                    Vector2 pt = new Vector2(wx, wy);

                    float luzMax = 0f;

                    // 1. Si el punto pertenece a una sala descubierta: ILUMINAR EL 100% DE LA SALA COMPLETA
                    for (int s = 0; s < _salasDescubiertasEnCamara.Count; s++)
                    {
                        if (_salasDescubiertasEnCamara[s].Contains(pt))
                        {
                            luzMax = 1f;
                            break;
                        }
                    }

                    // 2. Si no está en una sala descubierta (pasillos de conexión): visión circular del jugador
                    if (luzMax < 1f)
                    {
                        for (int j = 0; j < _fuentesJugadores.Count; j++)
                        {
                            Vector2 orig = _fuentesJugadores[j];
                            float dSq = (orig - pt).sqrMagnitude;
                            if (dSq <= rJugSq)
                            {
                                if (HayLineaDeVisionSinAtravesarPared(orig, pt, gen, salaCerrada))
                                {
                                    float d = Mathf.Sqrt(dSq);
                                    float l = (d <= rJugInterno) ? 1f : 1f - ((d - rJugInterno) / (rJug - rJugInterno));
                                    if (l > luzMax) luzMax = l;
                                }
                            }
                        }
                    }

                    // 3. Antorchas en pasillos o entradas
                    if (luzMax < 1f)
                    {
                        for (int a = 0; a < _fuentesAntorchas.Count; a++)
                        {
                            FuenteAntorcha ant = _fuentesAntorchas[a];
                            float rAnt = ant.radioEfectivo;
                            float rAntSq = rAnt * rAnt;
                            float dSq = (ant.pos - pt).sqrMagnitude;
                            if (dSq <= rAntSq)
                            {
                                if (HayLineaDeVisionSinAtravesarPared(ant.pos, pt, gen, salaCerrada))
                                {
                                    float rAntInterno = rAnt * 0.78f;
                                    float d = Mathf.Sqrt(dSq);
                                    float l = (d <= rAntInterno) ? 1f : 1f - ((d - rAntInterno) / (rAnt - rAntInterno));
                                    if (l > luzMax) luzMax = l;
                                }
                            }
                        }
                    }

                    // Fuera del área iluminada: 100% NEGRO ABSOLUTO (alpha = 255)
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 0f, Mathf.Clamp01(luzMax)));
                    _pixelesNiebla[rowOffset + px] = new Color32(0, 0, 0, alpha);
                }
            }

            _texNiebla.SetPixels32(_pixelesNiebla);
            _texNiebla.Apply(false);

            // 5. Ocultar enemigos que estén fuera del área iluminada
            for (int i = 0; i < EnemigoBase.Todos.Count; i++)
            {
                var e = EnemigoBase.Todos[i];
                if (e != null)
                {
                    e.EstablecerVisibilidad(TieneVisionEnPunto(e.transform.position));
                }
            }
        }
    }
}
