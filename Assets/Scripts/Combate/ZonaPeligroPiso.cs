using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoZonaPeligro
    {
        Esporas = 0,
        Fuego = 1,
        Hielo = 2
    }

    /// <summary>
    /// Zona de peligro visual y táctica en el suelo (Nubes de esporas, parches de fuego, escarcha).
    /// - Altamente visible: perímetro brillante de peligro, nubes pixel-art arremolinadas y motas flotantes.
    /// - sortingOrder = 2: siempre encima del suelo y de la niebla de exploración.
    /// - Daño periódico en área sobre el servidor a cualquier jugador que la pise.
    /// </summary>
    public class ZonaPeligroPiso : MonoBehaviour
    {
        public static readonly List<ZonaPeligroPiso> Todas = new List<ZonaPeligroPiso>();

        public TipoZonaPeligro tipoZona;
        public float radio = 1.9f;
        public float duracion = 6.5f;
        public int danoPorTick = 1;
        public bool esServidor = false;

        private float _tiempoVivo = 0f;
        private float _timerTick = 0f;
        private SpriteRenderer _srNube;
        private LineRenderer _lrAnillo;
        private readonly List<Transform> _particulas = new List<Transform>();
        private readonly List<Vector2> _particulasOffset = new List<Vector2>();

        private static Sprite _spriteNubeEsporas;
        private static Sprite _spriteNubeFuego;
        private static Sprite _spriteNubeHielo;
        private static Sprite _spriteMota;

        void OnEnable()
        {
            if (!Todas.Contains(this)) Todas.Add(this);
        }

        void OnDisable()
        {
            Todas.Remove(this);
        }

        void OnDestroy()
        {
            Todas.Remove(this);
        }

        public static void LimpiarTodas()
        {
            for (int i = Todas.Count - 1; i >= 0; i--)
            {
                if (Todas[i] != null)
                {
                    Destroy(Todas[i].gameObject);
                }
            }
            Todas.Clear();
        }

        public static ZonaPeligroPiso Instanciar(Vector2 posicion, float radio, TipoZonaPeligro tipo, float duracion, bool esServidor, int danoTick = 1)
        {
            var go = new GameObject($"ZonaPeligro_{tipo}");
            go.transform.position = new Vector3(posicion.x, posicion.y, -0.15f);

            var zona = go.AddComponent<ZonaPeligroPiso>();
            zona.radio = radio;
            zona.tipoZona = tipo;
            zona.duracion = duracion;
            zona.esServidor = esServidor;
            zona.danoPorTick = danoTick;
            zona.InicializarVisuales();

            // Garantia infalible del motor de Unity para autodestruccion segura
            Destroy(go, duracion + 0.05f);

            return zona;
        }

        public void InicializarVisuales()
        {
            // 1. Cuerpo central de la nube / zona peligrosa
            var nubeObj = new GameObject("NubeVisual");
            nubeObj.transform.SetParent(transform, false);
            nubeObj.transform.localPosition = Vector3.zero;

            _srNube = nubeObj.AddComponent<SpriteRenderer>();
            _srNube.sortingOrder = 2; // Por encima del suelo y la niebla (1)

            switch (tipoZona)
            {
                case TipoZonaPeligro.Esporas:
                    if (_spriteNubeEsporas == null) _spriteNubeEsporas = GenerarSpriteNubeEsporas();
                    _srNube.sprite = _spriteNubeEsporas;
                    _srNube.color = new Color(1f, 1f, 1f, 0.78f);
                    break;
                case TipoZonaPeligro.Fuego:
                    if (_spriteNubeFuego == null) _spriteNubeFuego = GenerarSpriteNubeFuego();
                    _srNube.sprite = _spriteNubeFuego;
                    _srNube.color = new Color(1f, 1f, 1f, 0.82f);
                    break;
                case TipoZonaPeligro.Hielo:
                    if (_spriteNubeHielo == null) _spriteNubeHielo = GenerarSpriteNubeHielo();
                    _srNube.sprite = _spriteNubeHielo;
                    _srNube.color = new Color(1f, 1f, 1f, 0.80f);
                    break;
            }

            // Escalar para cubrir el radio correspondiente
            float diametro = radio * 2.2f;
            nubeObj.transform.localScale = Vector3.one * diametro;

            // 2. Anillo de advertencia perimetral de alto contraste
            var anilloObj = new GameObject("AnilloAdvertencia");
            anilloObj.transform.SetParent(transform, false);
            anilloObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            _lrAnillo = anilloObj.AddComponent<LineRenderer>();
            _lrAnillo.material = MaterialUtil.SpritesDefault;
            _lrAnillo.sortingOrder = 3;
            _lrAnillo.startWidth = 0.08f;
            _lrAnillo.endWidth = 0.08f;
            _lrAnillo.useWorldSpace = false;
            _lrAnillo.loop = true;

            Color cAnillo;
            switch (tipoZona)
            {
                case TipoZonaPeligro.Esporas:
                    cAnillo = new Color(0.50f, 1f, 0.20f, 0.90f); // Verde lima tóxico brillante
                    break;
                case TipoZonaPeligro.Fuego:
                    cAnillo = new Color(1f, 0.35f, 0.10f, 0.90f); // Naranja fuego vivo
                    break;
                case TipoZonaPeligro.Hielo:
                default:
                    cAnillo = new Color(0.35f, 0.88f, 1f, 0.90f); // Cian glaciar
                    break;
            }

            _lrAnillo.startColor = cAnillo;
            _lrAnillo.endColor = cAnillo;

            const int segmentos = 28;
            _lrAnillo.positionCount = segmentos;
            for (int i = 0; i < segmentos; i++)
            {
                float rad = ((float)i / segmentos) * Mathf.PI * 2f;
                _lrAnillo.SetPosition(i, new Vector3(Mathf.Cos(rad) * radio, Mathf.Sin(rad) * radio, 0f));
            }

            // 3. Motas / esporas flotantes orbitantes dentro de la nube
            if (_spriteMota == null) _spriteMota = GenerarSpriteMota();
            int cantMotas = 6;
            for (int i = 0; i < cantMotas; i++)
            {
                var motaObj = new GameObject($"Mota_{i}");
                motaObj.transform.SetParent(transform, false);

                float ang = (float)i / cantMotas * Mathf.PI * 2f;
                float dist = Random.Range(radio * 0.25f, radio * 0.78f);
                Vector2 off = new Vector2(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist);
                motaObj.transform.localPosition = new Vector3(off.x, off.y, -0.06f);
                motaObj.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);

                var srMota = motaObj.AddComponent<SpriteRenderer>();
                srMota.sprite = _spriteMota;
                srMota.sortingOrder = 3;
                srMota.color = (tipoZona == TipoZonaPeligro.Esporas) 
                    ? (i % 2 == 0 ? new Color(0.55f, 1f, 0.25f, 0.95f) : new Color(0.85f, 0.35f, 0.95f, 0.90f))
                    : (tipoZona == TipoZonaPeligro.Fuego ? new Color(1f, 0.85f, 0.20f, 0.95f) : new Color(0.80f, 0.96f, 1f, 0.95f));

                _particulas.Add(motaObj.transform);
                _particulasOffset.Add(off);
            }
        }

        void Update()
        {
            _tiempoVivo += Time.deltaTime;

            // Animación continua de rotación suave y pulsación de la nube
            if (_srNube != null)
            {
                _srNube.transform.Rotate(0f, 0f, 15f * Time.deltaTime);
                float pulso = 1.0f + Mathf.Sin(Time.time * 3.5f) * 0.05f;
                _srNube.transform.localScale = Vector3.one * (radio * 2.2f * pulso);
            }

            // Animación de las motas flotantes (burbujeo y órbita sutil)
            for (int i = 0; i < _particulas.Count; i++)
            {
                if (_particulas[i] != null)
                {
                    float t = Time.time * 2.0f + i;
                    Vector2 baseOff = _particulasOffset[i];
                    Vector2 bob = new Vector2(Mathf.Sin(t) * 0.15f, Mathf.Cos(t * 1.3f) * 0.15f);
                    _particulas[i].localPosition = new Vector3(baseOff.x + bob.x, baseOff.y + bob.y, -0.06f);
                }
            }

            // Desvanecimiento y disolucion visible en los ultimos 0.8 segundos (se derrite / evapora)
            float tiempoRestante = duracion - _tiempoVivo;
            if (tiempoRestante <= 0.8f)
            {
                float alfa = Mathf.Clamp01(tiempoRestante / 0.8f);
                if (_srNube != null)
                {
                    Color c = _srNube.color;
                    c.a = alfa * 0.78f;
                    _srNube.color = c;
                    _srNube.transform.localScale = Vector3.one * (radio * 2.2f * alfa);
                }
                if (_lrAnillo != null)
                {
                    Color c = _lrAnillo.startColor;
                    c.a = alfa * 0.90f;
                    _lrAnillo.startColor = c;
                    _lrAnillo.endColor = c;
                    _lrAnillo.transform.localScale = Vector3.one * alfa;
                }
            }

            if (_tiempoVivo >= duracion)
            {
                Destroy(gameObject);
                return;
            }

            // Lógica de daño periódica (solo ejecutada por el Servidor)
            if (esServidor)
            {
                _timerTick += Time.deltaTime;
                if (_timerTick >= 0.55f)
                {
                    _timerTick = 0f;
                    AplicarEfectosJugadoresEnArea();
                }
            }
        }

        private static readonly Collider2D[] _colsBuffer = new Collider2D[16];

        private void AplicarEfectosJugadoresEnArea()
        {
            int hits = Physics2D.OverlapCircleNonAlloc(transform.position, radio, _colsBuffer);
            for (int i = 0; i < hits; i++)
            {
                var col = _colsBuffer[i];
                if (col == null) continue;

                var pj = col.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(danoPorTick);
                    switch (tipoZona)
                    {
                        case TipoZonaPeligro.Esporas:
                            pj.AplicarEsporasVeneno(3.5f, 2);
                            break;
                        case TipoZonaPeligro.Fuego:
                            pj.AplicarQuemadura(3.5f, 2);
                            break;
                        case TipoZonaPeligro.Hielo:
                            pj.AplicarRalentizacion(3.0f);
                            break;
                    }
                }
            }
        }

        // ========================================================
        // GENERADORES PROCEDURALES DE SPRITES DE PELIGRO (32 PPU)
        // ========================================================
        private static Sprite GenerarSpriteNubeEsporas()
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cCentro = new Color(0.48f, 1f, 0.28f, 0.85f);  // Núcleo verde espora brillante
            Color cMedio = new Color(0.68f, 0.22f, 0.78f, 0.70f);  // Velo violeta tóxico
            Color cBorde = new Color(0.20f, 0.75f, 0.30f, 0.40f);  // Borde exterior difuso
            float rMax = s * 0.46f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - (s * 0.5f);
                    float dy = y - (s * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Forma de nube orgánica con lóbulos
                    float ang = Mathf.Atan2(dy, dx);
                    float distMod = dist + Mathf.Sin(ang * 5f) * 2.8f + Mathf.Cos(ang * 3f) * 2.2f;

                    if (distMod <= rMax)
                    {
                        float norm = distMod / rMax;
                        Color c;
                        if (norm < 0.40f) c = Color.Lerp(cCentro, cMedio, norm / 0.40f);
                        else c = Color.Lerp(cMedio, cBorde, (norm - 0.40f) / 0.60f);

                        // Ruido sutil para textura de humo
                        if ((x * 3 + y * 5) % 7 == 0) c.a *= 0.75f;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteNubeFuego()
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cCentro = new Color(1f, 0.95f, 0.40f, 0.90f);  // Magma amarillo brillante
            Color cMedio = new Color(1f, 0.45f, 0.08f, 0.75f);   // Llamas naranja vivo
            Color cBorde = new Color(0.85f, 0.12f, 0.05f, 0.40f); // Borde brasa rojo
            float rMax = s * 0.46f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - (s * 0.5f);
                    float dy = y - (s * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float distMod = dist + Mathf.Sin(ang * 6f) * 3.2f;

                    if (distMod <= rMax)
                    {
                        float norm = distMod / rMax;
                        Color c = (norm < 0.35f) 
                            ? Color.Lerp(cCentro, cMedio, norm / 0.35f) 
                            : Color.Lerp(cMedio, cBorde, (norm - 0.35f) / 0.65f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteNubeHielo()
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cCentro = new Color(0.88f, 0.98f, 1f, 0.90f);   // Escarcha blanca
            Color cMedio = new Color(0.35f, 0.82f, 1f, 0.75f);    // Cian cristalino
            Color cBorde = new Color(0.12f, 0.45f, 0.88f, 0.35f); // Azul glacial difuso
            float rMax = s * 0.46f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - (s * 0.5f);
                    float dy = y - (s * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float distMod = dist + Mathf.Cos(ang * 8f) * 2.5f;

                    if (distMod <= rMax)
                    {
                        float norm = distMod / rMax;
                        Color c = (norm < 0.40f) 
                            ? Color.Lerp(cCentro, cMedio, norm / 0.40f) 
                            : Color.Lerp(cMedio, cBorde, (norm - 0.40f) / 0.60f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteMota()
        {
            int s = 8;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 3);
                    int dy = Mathf.Abs(y - 3);
                    if (dx + dy <= 3) tex.SetPixel(x, y, Color.white);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
