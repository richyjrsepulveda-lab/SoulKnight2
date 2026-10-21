using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Cofre de tesoro interactuable que aparece en salas secundarias de la mazmorra.
    /// Al abrirse con [F], otorga oro y dropea monedas físicas saltando para todos los jugadores.
    /// La interacción en multijugador se sincroniza a través de Personaje y SalaRed.
    /// </summary>
    public class CofreOro : MonoBehaviour
    {
        public static readonly List<CofreOro> Todos = new List<CofreOro>();

        public bool EstaAbiertoLocal { get; private set; } = false;

        private SpriteRenderer _sr;
        private TextMesh _textoIndicador;
        private static Sprite _spriteCerrado;
        private static Sprite _spriteAbierto;

        public float distanciaInteraccion = 2.6f;

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

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 3;

            AsegurarSpritesProcedurales();
            _sr.sprite = _spriteCerrado;

            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 1.2f);
            col.offset = new Vector2(0f, 0f);

            CrearTextoIndicador();
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

            float dist = Vector2.Distance(transform.position, local.transform.position);
            bool cerca = dist <= distanciaInteraccion;

            if (_textoIndicador != null)
            {
                _textoIndicador.gameObject.SetActive(cerca);
            }

            if (cerca && Input.GetKeyDown(KeyCode.F))
            {
                local.InteractuarCofre(this);
            }
        }

        public void AbrirPorServidor()
        {
            if (EstaAbiertoLocal) return;
            EstaAbiertoLocal = true;

            // Dropear monedas de oro físicas saltando del cofre en erupción vistosa (25 monedas de valor 2)
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SpawnearMonedasEnServidor(transform.position, 25, valorPorMoneda: 2); // 50 de oro
            }
            else
            {
                for (int i = 0; i < 25; i++)
                {
                    var go = new GameObject("MonedaOro_Cofre");
                    go.transform.position = transform.position;
                    var mon = go.AddComponent<MonedaOro>();
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    mon.IniciarSalto(Random.Range(1000, 99999), 2, dir, Random.Range(2.5f, 5.0f), Random.Range(5.5f, 9.0f));
                }
            }

            AplicarVisualAbierto();
            EfectosApertura();
        }

        public void AbrirLocalmente(Personaje local)
        {
            if (EstaAbiertoLocal) return;
            EstaAbiertoLocal = true;

            for (int i = 0; i < 25; i++)
            {
                var go = new GameObject("MonedaOro_Cofre");
                go.transform.position = transform.position;
                var mon = go.AddComponent<MonedaOro>();
                Vector2 dir = Random.insideUnitCircle.normalized;
                mon.IniciarSalto(Random.Range(1000, 99999), 2, dir, Random.Range(2.5f, 5.0f), Random.Range(5.5f, 9.0f));
            }

            AplicarVisualAbierto();
            EfectosApertura();
        }

        public void AplicarVisualAbierto()
        {
            EstaAbiertoLocal = true;
            if (_sr != null && _spriteAbierto != null)
            {
                _sr.sprite = _spriteAbierto;
            }
            if (_textoIndicador != null)
            {
                _textoIndicador.gameObject.SetActive(false);
            }
        }

        public void EfectosApertura()
        {
            GestorAudio.PlaySFX(TipoSFX.Mejora, 1.1f);
            GestorAudio.PlaySFX(TipoSFX.RecogerOro, 1.25f);
            TextoFlotante.Crear(transform.position + Vector3.up * 1.5f, "¡COFRE ABIERTO! 💰", new Color(1f, 0.88f, 0.2f));
        }

        private void CrearTextoIndicador()
        {
            var txtObj = new GameObject("TextoIndicador");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            _textoIndicador = txtObj.AddComponent<TextMesh>();
            _textoIndicador.text = "[F] ABRIR COFRE DE ORO";
            _textoIndicador.fontSize = 24;
            _textoIndicador.characterSize = 0.08f;
            _textoIndicador.alignment = TextAlignment.Center;
            _textoIndicador.anchor = TextAnchor.MiddleCenter;
            _textoIndicador.color = new Color(1f, 0.90f, 0.25f);

            var mr = txtObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

            txtObj.SetActive(false);
        }

        private static void AsegurarSpritesProcedurales()
        {
            if (_spriteCerrado != null && _spriteAbierto != null) return;

            const int w = 24;
            const int h = 18;

            Color cMaderaOsc = new Color(0.35f, 0.18f, 0.05f);
            Color cMaderaClara = new Color(0.55f, 0.32f, 0.12f);
            Color cOroBorde = new Color(0.95f, 0.75f, 0.15f);
            Color cOroBrillo = new Color(1f, 0.92f, 0.40f);
            Color cCerradura = new Color(0.20f, 0.20f, 0.20f);
            Color cInteriorOscuro = new Color(0.12f, 0.05f, 0.02f);
            Color cBrilloGemas = new Color(1f, 0.85f, 0.2f);

            // Sprite Cerrado
            Texture2D texCerrado = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texCerrado.filterMode = FilterMode.Point;
            texCerrado.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool bordeExt = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    bool franjaOro = (x <= 2 || x >= w - 3 || y == 7 || y == 8);

                    if (bordeExt)
                    {
                        texCerrado.SetPixel(x, y, new Color(0.1f, 0.05f, 0.02f));
                    }
                    else if (franjaOro)
                    {
                        texCerrado.SetPixel(x, y, (x == 1 || y == 8) ? cOroBrillo : cOroBorde);
                    }
                    else if (x >= 10 && x <= 13 && y >= 6 && y <= 9)
                    {
                        texCerrado.SetPixel(x, y, (x == 11 || x == 12) && y == 7 ? cCerradura : cOroBrillo);
                    }
                    else
                    {
                        texCerrado.SetPixel(x, y, (y > 8) ? cMaderaClara : cMaderaOsc);
                    }
                }
            }
            texCerrado.Apply();
            _spriteCerrado = Sprite.Create(texCerrado, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);

            // Sprite Abierto (Tapa levantada mostrando interior y monedas refulgentes)
            Texture2D texAbierto = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texAbierto.filterMode = FilterMode.Point;
            texAbierto.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool bordeExt = (x == 0 || x == w - 1 || y == 0);
                    bool baseCofre = y <= 7;

                    if (bordeExt)
                    {
                        texAbierto.SetPixel(x, y, new Color(0.1f, 0.05f, 0.02f));
                    }
                    else if (baseCofre)
                    {
                        bool franjaOro = (x <= 2 || x >= w - 3 || y == 1);
                        if (franjaOro) texAbierto.SetPixel(x, y, cOroBorde);
                        else texAbierto.SetPixel(x, y, (y >= 6) ? cInteriorOscuro : cMaderaOsc);
                    }
                    else if (y >= 8 && y <= 11)
                    {
                        // Montón de monedas brillantes y oro rebosando
                        bool brilloMoneda = ((x + y) % 3 == 0);
                        texAbierto.SetPixel(x, y, brilloMoneda ? cOroBrillo : cBrilloGemas);
                    }
                    else if (y >= 12 && y <= 16 && x >= 2 && x <= w - 3)
                    {
                        // Tapa levantada hacia arriba
                        bool bordeTapa = (y == 16 || x == 2 || x == w - 3);
                        texAbierto.SetPixel(x, y, bordeTapa ? cOroBorde : cMaderaClara);
                    }
                    else
                    {
                        texAbierto.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texAbierto.Apply();
            _spriteAbierto = Sprite.Create(texAbierto, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        public static CofreOro CrearCofre(Vector2 pos, Transform padre)
        {
            var go = new GameObject("CofreDeOro");
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.position = (Vector3)pos;

            var cofre = go.AddComponent<CofreOro>();
            return cofre;
        }
    }
}
