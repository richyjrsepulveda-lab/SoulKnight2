using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Gestor de visibilidad y descubrimiento del mapa:
    /// - Sistema de sombras dinámicas completamente desactivado (sin oscurecimiento de cámara ni niebla pesada).
    /// - Mantiene el registro de celdas exploradas por el jugador para el Minimapa y Mapa Grande.
    /// - Las salas y pasillos se descubren estrictamente cuando el jugador accede o camina por ellos.
    /// </summary>
    public class SistemaVision : MonoBehaviour
    {
        public static SistemaVision Instancia { get; private set; }

        [Header("Configuración de Visión y Exploración")]
        public float radioLuzJugador = 7.4f;
        public float radioLuzAntorcha = 10.8f;

        private byte[,] _gridReferencia;
        private bool[,] _celdasExploradas;
        private float _timerExploracion = 0f;
        private const float INTERVALO_EXPLORACION = 0.05f;

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

            // 1. Revelar celdas de las salas que han sido descubiertas por el jugador
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

            // 2. Revelar celdas por donde caminan los personajes vivos (revelando pasillos paso a paso)
            int rInt = Mathf.CeilToInt(radioLuzJugador) + 1;
            float rExpSq = (radioLuzJugador + 0.8f) * (radioLuzJugador + 0.8f);

            for (int j = 0; j < Personaje.Todos.Count; j++)
            {
                var pj = Personaje.Todos[j];
                if (pj == null || !pj.gameObject.activeInHierarchy || pj.EstaMuerto.Value) continue;
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
                            _celdasExploradas[gx, gy] = true;
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
            DestruirOverlayOscuridadSiExiste();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        private void DestruirOverlayOscuridadSiExiste()
        {
            var huerfanos = GameObject.FindObjectsOfType<Transform>();
            for (int i = 0; i < huerfanos.Length; i++)
            {
                if (huerfanos[i] != null && huerfanos[i].name == "OverlayOscuridadAbsoluta")
                {
                    Destroy(huerfanos[i].gameObject);
                }
            }
        }

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
        /// Siempre true: las sombras dinámicas han sido completamente removidas.
        /// </summary>
        public bool TieneVisionEnPunto(Vector2 pos)
        {
            return true;
        }

        void LateUpdate()
        {
            _timerExploracion += Time.deltaTime;
            if (_timerExploracion >= INTERVALO_EXPLORACION)
            {
                _timerExploracion = 0f;
                ForzarActualizacionExploracion();

                // Asegurar que todos los enemigos permanezcan siempre visibles sin niebla artificial
                for (int i = 0; i < EnemigoBase.Todos.Count; i++)
                {
                    var e = EnemigoBase.Todos[i];
                    if (e != null)
                    {
                        e.EstablecerVisibilidad(true);
                    }
                }
            }
        }
    }
}
