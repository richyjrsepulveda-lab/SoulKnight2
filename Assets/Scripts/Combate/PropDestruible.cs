using System.Collections;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoPropDestruible
    {
        Caja = 0,
        Barril = 1,
        BarrilExplosivo = 2,
        Jarron = 3,
        MuroDestructible = 4
    }

    /// <summary>
    /// Obstáculo destruible en la mazmorra (caja, barril, barril rojo explosivo o jarrón).
    /// </summary>
    public class PropDestruible : MonoBehaviour
    {
        public TipoPropDestruible tipoProp = TipoPropDestruible.Caja;
        public int vidaMax = 28;
        public bool esExplosivo = false;
        public int danoExplosion = 25;
        public float radioExplosion = 2.4f;

        private int _vidaActual;
        private SpriteRenderer _sr;
        private Color _colorOriginal;
        private bool _destruido = false;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _colorOriginal = _sr.color;
            _vidaActual = vidaMax;
        }

        public void RecibirDano(int cantidad)
        {
            if (_destruido) return;

            _vidaActual -= cantidad;
            StartCoroutine(RutinaFlashDano());

            if (_vidaActual <= 0)
            {
                if (esExplosivo)
                {
                    StartCoroutine(RutinaDetonarBomba());
                }
                else
                {
                    DestruirProp();
                }
            }
        }

        private IEnumerator RutinaFlashDano()
        {
            if (_sr != null)
            {
                _sr.color = Color.white * 1.8f;
                yield return new WaitForSeconds(0.08f);
                if (_sr != null) _sr.color = _colorOriginal;
            }
        }

        private IEnumerator RutinaDetonarBomba()
        {
            _destruido = true;

            // Desactivar colisionador para que el jugador o proyectiles no tropiecen durante la mecha
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 1. Fase de mecha e ignicion violenta (0.32 segundos de aviso cinematografico)
            float tTotal = 0.32f;
            float t = 0f;
            Vector3 escalaBase = transform.localScale;
            GestorAudio.PlaySFX(TipoSFX.FuegoDisparo, 0.9f);

            while (t < tTotal)
            {
                t += Time.deltaTime;
                float pct = t / tTotal;

                if (_sr != null)
                {
                    bool parpadeo = (t % 0.08f < 0.04f);
                    _sr.color = parpadeo ? (Color.white * 2.2f) : Color.Lerp(new Color(1f, 0.35f, 0.1f), Color.red, pct);
                }

                float pulso = 1.0f + 0.32f * pct;
                Vector3 temblor = (Vector3)Random.insideUnitCircle * (0.07f * pct);
                transform.localScale = escalaBase * pulso + temblor;

                yield return null;
            }

            // 2. Ejecutar la gran explosion animada
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.MostrarExplosionBombaClientRpc(transform.position, radioExplosion);
            }
            else
            {
                EfectoExplosionBomba.Crear(transform.position, radioExplosion);
            }

            // 3. Aplicar dano radial en servidor
            DetonarBarril();

            Destroy(gameObject);
        }

        private void DestruirProp()
        {
            if (_destruido) return;
            _destruido = true;

            if (esExplosivo)
            {
                StartCoroutine(RutinaDetonarBomba());
                return;
            }
            else if (tipoProp == TipoPropDestruible.MuroDestructible)
            {
                // Efecto de destrucción de muro de piedra: sonido y escombros en el suelo
                GestorAudio.PlaySFX(TipoSFX.ImpactoEnemigo, 1.25f);
                TextoFlotante.Crear(transform.position + Vector3.up * 0.4f, "💥 ¡ROTO!", new Color(0.9f, 0.85f, 0.7f));

                var escombrosObj = new GameObject("EscombrosPiedra");
                escombrosObj.transform.position = transform.position;
                var srEsc = escombrosObj.AddComponent<SpriteRenderer>();
                srEsc.sortingOrder = 0;
                var cat = CatalogoBiomas.Obtener(TipoBioma.Piedra);
                if (cat != null && cat.spritesDecals != null && cat.spritesDecals.Length > 1 && cat.spritesDecals[1] != null)
                {
                    srEsc.sprite = cat.spritesDecals[1]; // stone_decal_pebbles
                    srEsc.color = new Color(0.9f, 0.9f, 0.95f, 0.9f);
                }

                if (Random.value < 0.45f)
                {
                    MonedaOro.Crear(transform.position, Random.Range(1, 3));
                }
            }
            else
            {
                // Posibilidad de spawnear una moneda de oro al romperse (mayor en jarrones)
                float chance = (tipoProp == TipoPropDestruible.Jarron) ? 0.65f : 0.40f;
                if (Random.value < chance)
                {
                    MonedaOro.Crear(transform.position, Random.Range(1, 3));
                }
            }

            Destroy(gameObject);
        }

        private void DetonarBarril()
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, radioExplosion);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;

                var en = col.GetComponent<EnemigoBase>();
                if (en != null && en.VidaActual != null && en.VidaActual.Value > 0)
                {
                    en.RecibirDano(danoExplosion, new Color(1f, 0.4f, 0.1f));
                }

                var pj = col.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(10);
                }

                var otroProp = col.GetComponent<PropDestruible>();
                if (otroProp != null && !otroProp._destruido)
                {
                    otroProp.RecibirDano(danoExplosion);
                }
            }
        }
    }

    /// <summary>
    /// Efecto visual completo de explosion de bomba:
    /// - Esfera de fuego central en expansion con cambio de color blanco -> amarillo -> naranja -> humo
    /// - Onda de choque dorada en anillo
    /// - Fragmentos / chispas incandescentes expulsadas radialmente
    /// - Marca de quemadura / hollin en el piso
    /// - Sacudida de camara y sonido contundente
    /// </summary>
    public class EfectoExplosionBomba : MonoBehaviour
    {
        private static Sprite _spriteBolaFuego;
        private static Sprite _spriteChispa;
        private static Sprite _spriteHollin;

        public static void Crear(Vector2 posicion, float radio)
        {
            var go = new GameObject("EfectoExplosionBomba");
            go.transform.position = new Vector3(posicion.x, posicion.y, -0.6f);
            var ef = go.AddComponent<EfectoExplosionBomba>();
            ef.Inicializar(radio);
        }

        public void Inicializar(float radio)
        {
            GestorAudio.PlaySFX(TipoSFX.Explosion, 1.35f);
            TextoFlotante.Crear(transform.position + Vector3.up * 0.9f, "💥 ¡BOOM!", new Color(1f, 0.65f, 0.15f));
            CamaraSeguir.Instancia?.Sacudir(0.32f, 0.28f);

            AsegurarSprites();

            // 1. Bola de fuego central animada
            var fuegoObj = new GameObject("BolaFuegoBlast");
            fuegoObj.transform.SetParent(transform, false);
            var srFuego = fuegoObj.AddComponent<SpriteRenderer>();
            srFuego.sprite = _spriteBolaFuego;
            srFuego.sortingOrder = 15;

            // 2. Anillo de onda expansiva
            var anilloObj = new GameObject("OndaChoque");
            anilloObj.transform.SetParent(transform, false);
            var lr = anilloObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 16;
            lr.startWidth = 0.26f;
            lr.endWidth = 0.04f;
            lr.startColor = new Color(1f, 0.88f, 0.35f, 0.95f);
            lr.endColor = new Color(1f, 0.30f, 0.05f, 0f);
            const int seg = 20;
            lr.positionCount = seg;
            lr.useWorldSpace = false;
            lr.loop = true;

            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.3f, 0f));
            }

            // 3. Marca de quemadura en el suelo
            var marcaObj = new GameObject("HollinPiso");
            marcaObj.transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
            var srMarca = marcaObj.AddComponent<SpriteRenderer>();
            srMarca.sprite = _spriteHollin;
            srMarca.sortingOrder = 0;
            srMarca.color = new Color(0.12f, 0.10f, 0.08f, 0.70f);
            marcaObj.transform.localScale = Vector3.one * (radio * 1.6f);
            Destroy(marcaObj, 12f); // Permanece como huella de batalla y luego se retira

            // 4. Fragmentos y chispas radiales
            int cantChispas = 12;
            for (int i = 0; i < cantChispas; i++)
            {
                var chispaObj = new GameObject($"Chispa_{i}");
                chispaObj.transform.SetParent(transform, false);
                var srChispa = chispaObj.AddComponent<SpriteRenderer>();
                srChispa.sprite = _spriteChispa;
                srChispa.sortingOrder = 17;
                srChispa.color = (i % 2 == 0) ? new Color(1f, 0.90f, 0.30f) : new Color(1f, 0.40f, 0.10f);

                float ang = (float)i / cantChispas * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                float vel = Random.Range(3.5f, 6.5f);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                chispaObj.AddComponent<ParticulaChispaBomba>().Inicializar(dir, vel);
            }

            StartCoroutine(RutinaAnimacionExplosion(fuegoObj.transform, srFuego, lr, radio));
            Destroy(gameObject, 0.75f);
        }

        private IEnumerator RutinaAnimacionExplosion(Transform tFuego, SpriteRenderer srFuego, LineRenderer lrOnda, float radioMax)
        {
            float dur = 0.45f;
            float t = 0f;

            while (t < dur)
            {
                t += Time.deltaTime;
                float pct = Mathf.Clamp01(t / dur);

                // Bola de fuego: explosion rapida y luego humo
                float escalaFuego = (pct < 0.25f)
                    ? Mathf.Lerp(0.5f, radioMax * 2.3f, pct / 0.25f)
                    : Mathf.Lerp(radioMax * 2.3f, radioMax * 2.8f, (pct - 0.25f) / 0.75f);
                tFuego.localScale = Vector3.one * escalaFuego;

                Color cFuego = (pct < 0.25f)
                    ? Color.Lerp(Color.white, new Color(1f, 0.75f, 0.2f), pct / 0.25f)
                    : Color.Lerp(new Color(1f, 0.40f, 0.1f), new Color(0.25f, 0.20f, 0.18f, 0f), (pct - 0.25f) / 0.75f);
                srFuego.color = cFuego;

                // Onda expansiva: expansion hacia radioMax
                float radioOnda = Mathf.Lerp(0.4f, radioMax * 1.1f, pct);
                const int seg = 20;
                for (int i = 0; i < seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2f;
                    lrOnda.SetPosition(i, new Vector3(Mathf.Cos(a) * radioOnda, Mathf.Sin(a) * radioOnda, 0f));
                }

                float alfaOnda = (1f - pct) * 0.95f;
                lrOnda.startColor = new Color(1f, 0.85f, 0.35f, alfaOnda);
                lrOnda.endColor = new Color(1f, 0.25f, 0.05f, 0f);

                yield return null;
            }
        }

        private static void AsegurarSprites()
        {
            if (_spriteBolaFuego != null) return;

            // 1. Sprite Bola de Fuego (48x48 radial)
            {
                const int sz = 48;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                Vector2 c = new Vector2(sz * 0.5f - 0.5f, sz * 0.5f - 0.5f);
                float rMax = sz * 0.47f;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), c);
                        if (d > rMax)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            float t = d / rMax;
                            Color col;
                            if (t < 0.25f) col = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.55f), t / 0.25f);
                            else if (t < 0.65f) col = Color.Lerp(new Color(1f, 0.92f, 0.55f), new Color(1f, 0.45f, 0.08f), (t - 0.25f) / 0.40f);
                            else col = Color.Lerp(new Color(1f, 0.45f, 0.08f), new Color(0.60f, 0.12f, 0.05f, 0f), (t - 0.65f) / 0.35f);

                            tex.SetPixel(x, y, col);
                        }
                    }
                }
                tex.Apply();
                _spriteBolaFuego = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
            }

            // 2. Sprite Chispa (8x8)
            {
                const int sz = 8;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        bool c = (x >= 2 && x <= 5 && y >= 2 && y <= 5);
                        tex.SetPixel(x, y, c ? Color.white : Color.clear);
                    }
                }
                tex.Apply();
                _spriteChispa = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 16f);
            }

            // 3. Sprite Marca de Hollin (32x32)
            {
                const int sz = 32;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                Vector2 c = new Vector2(sz * 0.5f - 0.5f, sz * 0.5f - 0.5f);
                float rMax = sz * 0.46f;
                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), c);
                        if (d > rMax) tex.SetPixel(x, y, Color.clear);
                        else
                        {
                            float a = Mathf.Clamp01((rMax - d) / rMax);
                            tex.SetPixel(x, y, new Color(0.08f, 0.08f, 0.08f, a * 0.85f));
                        }
                    }
                }
                tex.Apply();
                _spriteHollin = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 32f);
            }
        }
    }

    /// <summary>
    /// Animacion individual de chispa expulsada por la explosion de la bomba.
    /// </summary>
    public class ParticulaChispaBomba : MonoBehaviour
    {
        private Vector2 _dir;
        private float _vel;
        private float _tiempo = 0f;
        private float _vida = 0.42f;

        public void Inicializar(Vector2 dir, float vel)
        {
            _dir = dir;
            _vel = vel;
            Destroy(gameObject, _vida);
        }

        void Update()
        {
            _tiempo += Time.deltaTime;
            _vel = Mathf.Max(0f, _vel - Time.deltaTime * 12f);
            transform.position += (Vector3)(_dir * (_vel * Time.deltaTime));
            float s = Mathf.Lerp(1.2f, 0.2f, _tiempo / _vida);
            transform.localScale = Vector3.one * s;
        }
    }

    /// <summary>
    /// Mecha animada en la parte superior del barril/bomba para advertir al jugador de que es explosiva.
    /// </summary>
    public class AnimadorMechaBomba : MonoBehaviour
    {
        private SpriteRenderer _sr;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            var tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    bool c = (x >= 1 && x <= 4 && y >= 1 && y <= 4);
                    tex.SetPixel(x, y, c ? new Color(1f, 0.85f, 0.25f) : Color.clear);
                }
            }
            tex.Apply();
            _sr.sprite = Sprite.Create(tex, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f), 16f);
        }

        void Update()
        {
            if (_sr != null)
            {
                float t = Time.time * 18f;
                float pulso = 0.8f + Mathf.Sin(t) * 0.35f;
                transform.localScale = Vector3.one * pulso;
                _sr.color = (Mathf.Sin(t * 1.5f) > 0f) ? new Color(1f, 0.95f, 0.3f, 0.95f) : new Color(1f, 0.40f, 0.1f, 0.95f);
            }
        }
    }
}
