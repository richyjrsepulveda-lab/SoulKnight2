using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Mini cofre secundario que aparece en ciertas salas de la mazmorra.
    /// Otorga entre 5 y 15 de oro y requiere MANTENER PULSADO [F] durante 1.0 segundo para abrirse.
    /// Al segundo de abrirse, se oscurece y se vuelve completamente traspasable.
    /// </summary>
    public class MiniCofre : MonoBehaviour
    {
        public static readonly List<MiniCofre> Todos = new List<MiniCofre>();

        public bool EstaAbiertoLocal { get; private set; } = false;
        public float distanciaInteraccion = 2.4f;
        public float tiempoSostenerRequerido = 1.0f;

        private float _tiempoSostenidoActual = 0f;
        private SpriteRenderer _sr;
        private TextMesh _textoIndicador;
        private static Sprite _spriteMiniCerrado;
        private static Sprite _spriteMiniAbierto;

        void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        void OnDisable()
        {
            Todos.Remove(this);
        }

        void OnDestroy()
        {
            Todos.Remove(this);
        }

        public static void DestruirTodos()
        {
            for (int i = Todos.Count - 1; i >= 0; i--)
            {
                if (Todos[i] != null)
                {
                    Destroy(Todos[i].gameObject);
                }
            }
            Todos.Clear();
        }

        public static Sprite ObtenerSpriteMiniCerrado()
        {
            AsegurarSpritesProcedurales();
            return _spriteMiniCerrado;
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 3;

            AsegurarSpritesProcedurales();
            _sr.sprite = _spriteMiniCerrado;

            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.9f);
            col.offset = new Vector2(0f, 0f);

            CrearTextoIndicador();
        }

        public static MiniCofre ObtenerCofreMasCercano(Vector2 posJugador, float radioMax)
        {
            MiniCofre mejor = null;
            float menorDist = radioMax;

            for (int i = 0; i < Todos.Count; i++)
            {
                var c = Todos[i];
                if (c == null || c.EstaAbiertoLocal || !c.gameObject.activeInHierarchy) continue;

                float d = Vector2.Distance(posJugador, c.transform.position);
                if (d <= menorDist)
                {
                    menorDist = d;
                    mejor = c;
                }
            }
            return mejor;
        }

        void Update()
        {
            if (EstaAbiertoLocal)
            {
                if (_textoIndicador != null && _textoIndicador.gameObject.activeSelf)
                {
                    _textoIndicador.gameObject.SetActive(false);
                }
                return;
            }

            Personaje local = null;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                if (Personaje.Todos[i] != null && Personaje.Todos[i].EsControlLocal)
                {
                    local = Personaje.Todos[i];
                    break;
                }
            }
            if (local == null) return;

            // Sistema estilo Warzone / Fortnite: SOLO el cofre interactuable más cercano está enfocado y recibe [F]
            MiniCofre enfocado = ObtenerCofreMasCercano(local.transform.position, distanciaInteraccion);
            bool soyElEnfocado = (enfocado == this);

            if (!soyElEnfocado)
            {
                if (_textoIndicador != null && _textoIndicador.gameObject.activeSelf)
                {
                    _textoIndicador.gameObject.SetActive(false);
                }
                _tiempoSostenidoActual = 0f;
                return;
            }

            if (_textoIndicador != null && !_textoIndicador.gameObject.activeSelf)
            {
                _textoIndicador.gameObject.SetActive(true);
            }

            bool sosteniendo = Input.GetKey(KeyCode.F) || Controles.Mantenida(KeyCode.F);
            if (sosteniendo)
            {
                _tiempoSostenidoActual += Time.deltaTime;
                float pct = Mathf.Clamp01(_tiempoSostenidoActual / tiempoSostenerRequerido);

                int totalBarras = 8;
                int barrasLlenas = Mathf.RoundToInt(pct * totalBarras);
                string barraVisual = new string('█', barrasLlenas) + new string('░', totalBarras - barrasLlenas);

                if (_textoIndicador != null)
                {
                    _textoIndicador.text = $"[MANTÉN F]\n[{barraVisual}] {Mathf.RoundToInt(pct * 100)}%";
                    _textoIndicador.color = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(0.3f, 1f, 0.4f), pct);
                }

                if (_tiempoSostenidoActual >= tiempoSostenerRequerido)
                {
                    Abrir(local);
                }
            }
            else
            {
                _tiempoSostenidoActual = Mathf.Max(0f, _tiempoSostenidoActual - Time.deltaTime * 3f);
                if (_textoIndicador != null)
                {
                    _textoIndicador.text = "[MANTÉN F] MINI COFRE\n(5-15 ORO)";
                    _textoIndicador.color = new Color(1f, 0.90f, 0.35f);
                }
            }
        }

        public void Abrir(Personaje local)
        {
            if (EstaAbiertoLocal) return;
            EstaAbiertoLocal = true;

            int cantidadOro = Random.Range(5, 16); // 5 a 15 de oro

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SpawnearMonedasEnServidor(transform.position, cantidadOro, valorPorMoneda: 1);
            }
            else
            {
                for (int i = 0; i < cantidadOro; i++)
                {
                    var go = new GameObject("MonedaOro_MiniCofre");
                    go.transform.position = transform.position;
                    var mon = go.AddComponent<MonedaOro>();
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    mon.IniciarSalto(Random.Range(1000, 99999), 1, dir, Random.Range(2.0f, 4.0f), Random.Range(4.5f, 7.5f));
                }
            }

            AplicarVisualAbierto();
            GestorAudio.PlaySFX(TipoSFX.Mejora, 1.0f);
            GestorAudio.PlaySFX(TipoSFX.RecogerOro, 1.2f);
            TextoFlotante.Crear(transform.position + Vector3.up * 1.3f, $"¡MINI COFRE! +{cantidadOro} 💰", new Color(1f, 0.92f, 0.35f));
        }

        public void AplicarVisualAbierto()
        {
            EstaAbiertoLocal = true;
            if (_sr != null && _spriteMiniAbierto != null)
            {
                _sr.sprite = _spriteMiniAbierto;
            }
            if (_textoIndicador != null)
            {
                _textoIndicador.gameObject.SetActive(false);
            }

            StartCoroutine(RutinaOscurecerYTraspasable());
        }

        private IEnumerator RutinaOscurecerYTraspasable()
        {
            yield return new WaitForSeconds(1.0f);

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }

            if (_sr != null)
            {
                Color cIni = _sr.color;
                Color cFin = new Color(0.38f, 0.38f, 0.42f, 0.75f);
                float dur = 0.35f;
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    if (_sr != null) _sr.color = Color.Lerp(cIni, cFin, t / dur);
                    yield return null;
                }
                if (_sr != null) _sr.color = cFin;
            }
        }

        private void CrearTextoIndicador()
        {
            var txtObj = new GameObject("TextoIndicadorMini");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            _textoIndicador = txtObj.AddComponent<TextMesh>();
            _textoIndicador.text = "[MANTÉN F] MINI COFRE";
            _textoIndicador.fontSize = 22;
            _textoIndicador.characterSize = 0.07f;
            _textoIndicador.alignment = TextAlignment.Center;
            _textoIndicador.anchor = TextAnchor.MiddleCenter;
            _textoIndicador.color = new Color(1f, 0.90f, 0.35f);

            var mr = txtObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

            txtObj.SetActive(false);
        }

        private static void AsegurarSpritesProcedurales()
        {
            if (_spriteMiniCerrado != null && _spriteMiniAbierto != null) return;

            const int w = 18;
            const int h = 14;

            Color cMaderaOsc = new Color(0.42f, 0.22f, 0.08f);
            Color cMaderaClara = new Color(0.62f, 0.36f, 0.15f);
            Color cBronceBorde = new Color(0.82f, 0.55f, 0.18f);
            Color cBronceBrillo = new Color(0.96f, 0.72f, 0.30f);
            Color cCerradura = new Color(0.18f, 0.18f, 0.18f);
            Color cInteriorOscuro = new Color(0.12f, 0.06f, 0.03f);

            // Sprite Mini Cerrado
            Texture2D texCerrado = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texCerrado.filterMode = FilterMode.Point;
            texCerrado.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool bordeExt = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    bool franjaBronce = (x <= 1 || x >= w - 2 || y == 5 || y == 6);

                    if (bordeExt)
                        texCerrado.SetPixel(x, y, new Color(0.12f, 0.06f, 0.02f));
                    else if (franjaBronce)
                        texCerrado.SetPixel(x, y, (x == 1 || y == 6) ? cBronceBrillo : cBronceBorde);
                    else if (x >= 8 && x <= 9 && y >= 4 && y <= 6)
                        texCerrado.SetPixel(x, y, cCerradura);
                    else
                        texCerrado.SetPixel(x, y, (y > 6) ? cMaderaClara : cMaderaOsc);
                }
            }
            texCerrado.Apply();
            _spriteMiniCerrado = Sprite.Create(texCerrado, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);

            // Sprite Mini Abierto
            Texture2D texAbierto = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texAbierto.filterMode = FilterMode.Point;
            texAbierto.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool bordeExt = (x == 0 || x == w - 1 || y == 0);
                    bool baseCofre = y <= 5;

                    if (bordeExt)
                    {
                        texAbierto.SetPixel(x, y, new Color(0.12f, 0.06f, 0.02f));
                    }
                    else if (baseCofre)
                    {
                        bool franja = (x <= 1 || x >= w - 2 || y == 1);
                        texAbierto.SetPixel(x, y, franja ? cBronceBorde : cMaderaOsc);
                    }
                    else if (y <= 8)
                    {
                        // Monedas refulgentes en el interior
                        bool brillo = (x % 3 == 0 || y == 7);
                        texAbierto.SetPixel(x, y, brillo ? cBronceBrillo : cInteriorOscuro);
                    }
                    else
                    {
                        // Tapa levantada
                        bool bordeTapa = (y == h - 1 || x <= 1 || x >= w - 2);
                        texAbierto.SetPixel(x, y, bordeTapa ? cBronceBorde : cMaderaClara);
                    }
                }
            }
            texAbierto.Apply();
            _spriteMiniAbierto = Sprite.Create(texAbierto, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
