using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Efecto visual de altísimo impacto ("toda god") para el teleport eléctrico:
    /// Rayo celestial descendente con núcleo blanco brillante y halo cian/dorado,
    /// ramificaciones bifurcadas, pilar de destello vertical, onda de choque expansiva
    /// en el suelo, chispas de plasma radiales y micro-arcos crepitantes.
    /// </summary>
    public class EfectoRayoTeleport : MonoBehaviour
    {
        private LineRenderer _lrGlow;
        private LineRenderer _lrCore;
        private LineRenderer _lrRama1;
        private LineRenderer _lrRama2;
        private LineRenderer _lrAnilloSuelo;
        private LineRenderer _lrPilarDestello;

        private Vector3[] _ptsGlow;
        private Vector3[] _ptsCore;
        private Vector3[] _ptsRama1;
        private Vector3[] _ptsRama2;

        private float _tiempoTotal = 0f;
        private const float DURACION_TOTAL = 0.55f;
        private const float ALTURA_CIELO = 15f;
        private const float RADIO_MAX_ANILLO = 3.6f;

        private struct ChispaPlasma
        {
            public GameObject go;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float rotSpeed;
            public float escalaInicial;
        }

        private readonly List<ChispaPlasma> _chispas = new List<ChispaPlasma>();
        private static Sprite _spriteChispa;

        public static void Crear(Vector2 posicion)
        {
            var go = new GameObject("EfectoRayoTeleport");
            go.transform.position = new Vector3(posicion.x, posicion.y, -1.2f);
            var efecto = go.AddComponent<EfectoRayoTeleport>();
            efecto.Inicializar(posicion);
        }

        public void Inicializar(Vector2 centro)
        {
            transform.position = new Vector3(centro.x, centro.y, -1.2f);
            Material mat = MaterialUtil.SpritesDefault;

            // 1. Rayo Principal - Halo Glow exterior (Cian eléctrico / Oro)
            _lrGlow = gameObject.AddComponent<LineRenderer>();
            _lrGlow.material = mat;
            _lrGlow.useWorldSpace = false;
            _lrGlow.sortingOrder = 55;
            _lrGlow.startWidth = 0.70f;
            _lrGlow.endWidth = 0.40f;
            _lrGlow.startColor = new Color(0.1f, 0.88f, 1f, 0.95f);
            _lrGlow.endColor = new Color(1f, 0.95f, 0.25f, 0.95f);

            // 2. Rayo Principal - Núcleo Core interior (Blanco puro cegador)
            var coreObj = new GameObject("CoreRayo");
            coreObj.transform.SetParent(transform, false);
            _lrCore = coreObj.AddComponent<LineRenderer>();
            _lrCore.material = mat;
            _lrCore.useWorldSpace = false;
            _lrCore.sortingOrder = 56;
            _lrCore.startWidth = 0.28f;
            _lrCore.endWidth = 0.16f;
            _lrCore.startColor = Color.white;
            _lrCore.endColor = new Color(1f, 1f, 0.9f, 1f);

            // 3. Ramificación lateral 1
            var rama1Obj = new GameObject("Rama1");
            rama1Obj.transform.SetParent(transform, false);
            _lrRama1 = rama1Obj.AddComponent<LineRenderer>();
            _lrRama1.material = mat;
            _lrRama1.useWorldSpace = false;
            _lrRama1.sortingOrder = 54;
            _lrRama1.startWidth = 0.22f;
            _lrRama1.endWidth = 0.05f;
            _lrRama1.startColor = new Color(0.3f, 0.95f, 1f, 0.9f);
            _lrRama1.endColor = new Color(1f, 1f, 1f, 0.3f);

            // 4. Ramificación lateral 2
            var rama2Obj = new GameObject("Rama2");
            rama2Obj.transform.SetParent(transform, false);
            _lrRama2 = rama2Obj.AddComponent<LineRenderer>();
            _lrRama2.material = mat;
            _lrRama2.useWorldSpace = false;
            _lrRama2.sortingOrder = 54;
            _lrRama2.startWidth = 0.20f;
            _lrRama2.endWidth = 0.04f;
            _lrRama2.startColor = new Color(1f, 0.92f, 0.3f, 0.9f);
            _lrRama2.endColor = new Color(1f, 1f, 1f, 0.3f);

            // 5. Pilar de destello vertical (Flash de impacto atmosférico)
            var pilarObj = new GameObject("PilarDestello");
            pilarObj.transform.SetParent(transform, false);
            _lrPilarDestello = pilarObj.AddComponent<LineRenderer>();
            _lrPilarDestello.material = mat;
            _lrPilarDestello.useWorldSpace = false;
            _lrPilarDestello.sortingOrder = 52;
            _lrPilarDestello.startWidth = 1.45f;
            _lrPilarDestello.endWidth = 0.80f;
            _lrPilarDestello.startColor = new Color(0.7f, 0.95f, 1f, 0.85f);
            _lrPilarDestello.endColor = new Color(1f, 1f, 0.8f, 0.55f);
            _lrPilarDestello.positionCount = 2;
            _lrPilarDestello.SetPosition(0, new Vector3(0f, ALTURA_CIELO, 0f));
            _lrPilarDestello.SetPosition(1, Vector3.zero);

            // 6. Onda de choque expansiva en el suelo
            var anilloObj = new GameObject("AnilloSuelo");
            anilloObj.transform.SetParent(transform, false);
            _lrAnilloSuelo = anilloObj.AddComponent<LineRenderer>();
            _lrAnilloSuelo.material = mat;
            _lrAnilloSuelo.useWorldSpace = false;
            _lrAnilloSuelo.sortingOrder = 51;
            _lrAnilloSuelo.startWidth = 0.35f;
            _lrAnilloSuelo.endWidth = 0.08f;
            _lrAnilloSuelo.startColor = new Color(0.2f, 0.9f, 1f, 0.95f);
            _lrAnilloSuelo.endColor = new Color(1f, 0.95f, 0.3f, 0.75f);
            _lrAnilloSuelo.positionCount = 24;

            // 7. Chispas de plasma radiales
            AsegurarSpriteChispa();
            CrearChispasRadiales(14);

            // Generar geometría del rayo principal
            const int segs = 18;
            _ptsGlow = new Vector3[segs];
            _ptsCore = new Vector3[segs];
            _lrGlow.positionCount = segs;
            _lrCore.positionCount = segs;

            _ptsRama1 = new Vector3[8];
            _ptsRama2 = new Vector3[8];
            _lrRama1.positionCount = 8;
            _lrRama2.positionCount = 8;

            GenerarGeometriaRayo();

            Destroy(gameObject, DURACION_TOTAL);
        }

        private void GenerarGeometriaRayo()
        {
            int segs = _ptsGlow.Length;
            float inicioX = Random.Range(-0.7f, 0.7f);
            Vector3 origenCielo = new Vector3(inicioX, ALTURA_CIELO, 0f);
            Vector3 destinoSuelo = Vector3.zero;

            Vector3 rama1Inicio = Vector3.zero;
            Vector3 rama2Inicio = Vector3.zero;

            for (int i = 0; i < segs; i++)
            {
                float t = (float)i / (segs - 1);
                // Invertimos t para que 0 sea arriba y 1 sea el suelo
                Vector3 pt = Vector3.Lerp(origenCielo, destinoSuelo, t);

                if (i > 0 && i < segs - 1)
                {
                    // Desplazamiento lateral violento y orgánico
                    float factorCentro = Mathf.Sin(t * Mathf.PI);
                    float jitterX = (Random.value - 0.5f) * 1.55f * factorCentro;
                    float jitterY = (Random.value - 0.5f) * 0.45f * factorCentro;
                    pt += new Vector3(jitterX, jitterY, 0f);
                }

                _ptsGlow[i] = pt;
                _ptsCore[i] = pt;

                if (i == segs / 3) rama1Inicio = pt;
                if (i == (segs * 2) / 3) rama2Inicio = pt;
            }

            _lrGlow.SetPositions(_ptsGlow);
            _lrCore.SetPositions(_ptsCore);

            // Generar Rama 1 (hacia la izquierda y abajo)
            Vector3 actualRama1 = rama1Inicio;
            for (int r = 0; r < _ptsRama1.Length; r++)
            {
                _ptsRama1[r] = actualRama1;
                actualRama1 += new Vector3(
                    Random.Range(-0.7f, -0.2f),
                    Random.Range(-0.8f, -0.2f),
                    0f
                );
            }
            _lrRama1.SetPositions(_ptsRama1);

            // Generar Rama 2 (hacia la derecha y abajo)
            Vector3 actualRama2 = rama2Inicio;
            for (int r = 0; r < _ptsRama2.Length; r++)
            {
                _ptsRama2[r] = actualRama2;
                actualRama2 += new Vector3(
                    Random.Range(0.2f, 0.7f),
                    Random.Range(-0.8f, -0.2f),
                    0f
                );
            }
            _lrRama2.SetPositions(_ptsRama2);
        }

        private static void AsegurarSpriteChispa()
        {
            if (_spriteChispa != null) return;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dx = (x - 3.5f) / 3.5f;
                    float dy = (y - 3.5f) / 3.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            tex.Apply();
            _spriteChispa = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
        }

        private void CrearChispasRadiales(int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                var sparkObj = new GameObject($"Chispa_{i}");
                sparkObj.transform.SetParent(transform, false);
                var sr = sparkObj.AddComponent<SpriteRenderer>();
                sr.sprite = _spriteChispa;
                sr.material = MaterialUtil.SpritesDefault;
                sr.sortingOrder = 57;
                sr.color = (i % 2 == 0)
                    ? new Color(0.2f, 0.95f, 1f, 1f)
                    : new Color(1f, 0.95f, 0.35f, 1f);

                float ang = (float)i / cantidad * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float speed = Random.Range(5.5f, 12f);
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;
                float escala = Random.Range(0.45f, 0.95f);
                sparkObj.transform.localScale = Vector3.one * escala;

                _chispas.Add(new ChispaPlasma
                {
                    go = sparkObj,
                    sr = sr,
                    vel = vel,
                    rotSpeed = Random.Range(-720f, 720f),
                    escalaInicial = escala
                });
            }
        }

        private float _tiempoJitter = 0f;

        void Update()
        {
            float dt = Time.deltaTime;
            _tiempoTotal += dt;
            float t = Mathf.Clamp01(_tiempoTotal / DURACION_TOTAL);

            // 1. Efecto crepitante vivo del rayo durante los primeros 0.18s
            if (_tiempoTotal < 0.18f)
            {
                _tiempoJitter += dt;
                if (_tiempoJitter >= 0.035f)
                {
                    _tiempoJitter = 0f;
                    GenerarGeometriaRayo();
                }
            }

            // 2. Desvanecimiento suave del rayo principal
            float alphaRayo = Mathf.Clamp01(1f - (_tiempoTotal / 0.38f));
            if (_lrGlow != null)
            {
                _lrGlow.startColor = new Color(0.1f, 0.88f, 1f, alphaRayo * 0.95f);
                _lrGlow.endColor = new Color(1f, 0.95f, 0.25f, alphaRayo * 0.95f);
            }
            if (_lrCore != null)
            {
                _lrCore.startColor = new Color(1f, 1f, 1f, alphaRayo);
                _lrCore.endColor = new Color(1f, 1f, 0.9f, alphaRayo);
            }

            // 3. Ramas laterales se desvanecen más rápido
            float alphaRamas = Mathf.Clamp01(1f - (_tiempoTotal / 0.24f));
            if (_lrRama1 != null)
            {
                _lrRama1.startColor = new Color(0.3f, 0.95f, 1f, alphaRamas * 0.9f);
                _lrRama1.endColor = new Color(1f, 1f, 1f, alphaRamas * 0.3f);
            }
            if (_lrRama2 != null)
            {
                _lrRama2.startColor = new Color(1f, 0.92f, 0.3f, alphaRamas * 0.9f);
                _lrRama2.endColor = new Color(1f, 1f, 1f, alphaRamas * 0.3f);
            }

            // 4. Pilar de destello vertical se desvanece en 0.09s
            if (_lrPilarDestello != null)
            {
                float alphaPilar = Mathf.Clamp01(1f - (_tiempoTotal / 0.09f));
                _lrPilarDestello.startColor = new Color(0.7f, 0.95f, 1f, alphaPilar * 0.8f);
                _lrPilarDestello.endColor = new Color(1f, 1f, 0.8f, alphaPilar * 0.5f);
            }

            // 5. Onda de choque expansiva en el suelo
            if (_lrAnilloSuelo != null)
            {
                float tAnillo = Mathf.Clamp01(_tiempoTotal / 0.35f);
                float radioActual = Mathf.Lerp(0.35f, RADIO_MAX_ANILLO, Mathf.Sin(tAnillo * Mathf.PI * 0.5f));
                float alphaAnillo = Mathf.Clamp01(1f - tAnillo);

                _lrAnilloSuelo.startColor = new Color(0.2f, 0.92f, 1f, alphaAnillo * 0.95f);
                _lrAnilloSuelo.endColor = new Color(1f, 0.95f, 0.35f, alphaAnillo * 0.75f);
                _lrAnilloSuelo.startWidth = Mathf.Lerp(0.32f, 0.04f, tAnillo);

                const int puntos = 24;
                for (int i = 0; i < puntos; i++)
                {
                    float ang = (float)i / (puntos - 1) * Mathf.PI * 2f;
                    float jitterRadio = (tAnillo < 0.6f) ? (Random.value - 0.5f) * 0.22f : 0f;
                    float r = radioActual + jitterRadio;
                    Vector3 posCirculo = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                    _lrAnilloSuelo.SetPosition(i, posCirculo);
                }
            }

            // 6. Actualizar y proyectar chispas de plasma
            for (int i = 0; i < _chispas.Count; i++)
            {
                var ch = _chispas[i];
                if (ch.go == null) continue;

                ch.go.transform.localPosition += (Vector3)(ch.vel * dt);
                ch.go.transform.Rotate(0f, 0f, ch.rotSpeed * dt);
                ch.vel *= Mathf.Exp(-5.5f * dt); // Fricción atmosférica

                float tChispa = Mathf.Clamp01(_tiempoTotal / 0.42f);
                float escalaActual = Mathf.Lerp(ch.escalaInicial, 0f, tChispa);
                ch.go.transform.localScale = Vector3.one * escalaActual;

                if (ch.sr != null)
                {
                    Color c = ch.sr.color;
                    c.a = Mathf.Clamp01(1f - tChispa);
                    ch.sr.color = c;
                }

                _chispas[i] = ch;
            }
        }
    }
}
