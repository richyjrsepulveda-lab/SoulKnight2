using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Puestito físico de tienda secreta dentro de habitaciones de relleno.
    /// Al acercarse o entrar en su puerta, la pantalla se oscurece y se abre
    /// el menú de compras en el HUD para adquirir mejoras de la run con oro.
    /// </summary>
    public class PuestitoTienda : MonoBehaviour
    {
        public static readonly List<PuestitoTienda> Todos = new List<PuestitoTienda>();

        public struct ArticuloTienda
        {
            public TipoMejoraRun tipo;
            public int costoOro;
            public string nombre;
            public string descripcion;
            public bool comprado;
        }

        public readonly List<ArticuloTienda> articulos = new List<ArticuloTienda>();

        private TextMesh _textoIndicador;
        private static Sprite _spritePuestito;
        private bool _jugadorEnPuerta = false;

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
            if (HUDJugador.Instancia != null && HUDJugador.Instancia.PuestitoActivo == this)
            {
                HUDJugador.Instancia.CerrarTienda();
            }
        }

        void Awake()
        {
            InicializarArticulos();
            ConstruirVisualYPuestito();
        }

        private void InicializarArticulos()
        {
            articulos.Clear();
            articulos.Add(new ArticuloTienda
            {
                tipo = TipoMejoraRun.AumentoDano,
                costoOro = 30,
                nombre = "ESPADA ÍGNEA",
                descripcion = "+15% Daño Permanente en la Run",
                comprado = false
            });

            articulos.Add(new ArticuloTienda
            {
                tipo = TipoMejoraRun.AumentoDefensa,
                costoOro = 25,
                nombre = "ESCUDO RUNA",
                descripcion = "+10% Armadura Permanente en la Run",
                comprado = false
            });

            articulos.Add(new ArticuloTienda
            {
                tipo = TipoMejoraRun.AumentoVidaMaxima,
                costoOro = 35,
                nombre = "CORAZÓN VITAL",
                descripcion = "+30 Vida Máxima Permanente en la Run",
                comprado = false
            });

            articulos.Add(new ArticuloTienda
            {
                tipo = TipoMejoraRun.PescadoCompleto,
                costoOro = 20,
                nombre = "PESCADO ASADO",
                descripcion = "Restaura 60 Puntos de Vida al Instante",
                comprado = false
            });
        }

        private void ConstruirVisualYPuestito()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ObtenerSpritePuestito();
            sr.sortingOrder = 3;

            // Trigger de entrada a la tiendita (puerta / mostrador)
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.6f, 2.0f);
            col.offset = new Vector2(0f, -0.2f);

            // Letrero flotante encima del puestito
            var txtObj = new GameObject("TextoLetreroTienda");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.6f, -0.2f);

            _textoIndicador = txtObj.AddComponent<TextMesh>();
            _textoIndicador.text = "🏪 TIENDA [F]";
            _textoIndicador.fontSize = 26;
            _textoIndicador.characterSize = 0.08f;
            _textoIndicador.fontStyle = FontStyle.Bold;
            _textoIndicador.alignment = TextAlignment.Center;
            _textoIndicador.anchor = TextAnchor.MiddleCenter;
            _textoIndicador.color = new Color(1f, 0.88f, 0.25f);

            var mr = txtObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 14;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var pj = other.GetComponent<Personaje>();
            if (pj != null && pj.EsControlLocal)
            {
                _jugadorEnPuerta = true;
                // No abrir automáticamente: la tienda se abre ÚNICAMENTE pulsando [F]
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var pj = other.GetComponent<Personaje>();
            if (pj != null && pj.EsControlLocal)
            {
                _jugadorEnPuerta = false;
            }
        }

        void Update()
        {
            // Permitir abrir con [F] ÚNICAMENTE si está cerca
            Personaje local = null;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                if (Personaje.Todos[i] != null && Personaje.Todos[i].EsControlLocal)
                {
                    local = Personaje.Todos[i];
                    break;
                }
            }

            if (local != null)
            {
                float dist = Vector2.Distance(transform.position, local.transform.position);
                bool cerca = dist <= 2.8f;
                if (_textoIndicador != null)
                {
                    _textoIndicador.gameObject.SetActive(cerca);
                }

                // Abrir o alternar tienda ÚNICAMENTE con tecla [F]
                if (cerca && Input.GetKeyDown(KeyCode.F))
                {
                    if (HUDJugador.Instancia != null)
                    {
                        if (HUDJugador.Instancia.TiendaAbierta)
                            HUDJugador.Instancia.CerrarTienda();
                        else
                            HUDJugador.Instancia.AbrirTienda(this);
                    }
                }
                else if (dist > 3.6f)
                {
                    // Si el jugador se aleja físicamente del puesto, cerrar la tienda
                    if (HUDJugador.Instancia != null && HUDJugador.Instancia.TiendaAbierta && HUDJugador.Instancia.PuestitoActivo == this)
                    {
                        HUDJugador.Instancia.CerrarTienda();
                    }
                }
            }
        }

        public void MarcarArticuloComprado(int index)
        {
            if (index >= 0 && index < articulos.Count)
            {
                var art = articulos[index];
                art.comprado = true;
                articulos[index] = art;
            }
        }

        public static PuestitoTienda CrearPuestito(Vector2 posicion, Transform parent = null)
        {
            var go = new GameObject("PuestitoTienda");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            go.transform.position = (Vector3)posicion;
            return go.AddComponent<PuestitoTienda>();
        }

        public static void DestruirTodos()
        {
            for (int i = Todos.Count - 1; i >= 0; i--)
            {
                if (Todos[i] != null)
                {
                    if (Application.isPlaying) Destroy(Todos[i].gameObject);
                    else DestroyImmediate(Todos[i].gameObject);
                }
            }
            Todos.Clear();

            var huerfanos = Object.FindObjectsOfType<PuestitoTienda>();
            for (int i = 0; i < huerfanos.Length; i++)
            {
                if (huerfanos[i] != null)
                {
                    if (Application.isPlaying) Destroy(huerfanos[i].gameObject);
                    else DestroyImmediate(huerfanos[i].gameObject);
                }
            }
        }

        private static Sprite ObtenerSpritePuestito()
        {
            if (_spritePuestito != null) return _spritePuestito;

            const int W = 48;
            const int H = 40;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color cTrans = new Color(0, 0, 0, 0);
            Color cMuroFondo = new Color(0.10f, 0.08f, 0.12f, 0.95f);
            Color cMadera = new Color(0.48f, 0.28f, 0.14f, 1f);
            Color cMaderaOscura = new Color(0.32f, 0.18f, 0.08f, 1f);
            Color cToldoRojo = new Color(0.85f, 0.20f, 0.20f, 1f);
            Color cToldoBlanco = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color cDorado = new Color(1f, 0.85f, 0.25f, 1f);
            Color cBorde = new Color(0.06f, 0.04f, 0.06f, 1f);

            Color[] cols = new Color[W * H];
            for (int i = 0; i < cols.Length; i++) cols[i] = cTrans;

            // Relleno interior de la tienda (interior sombrío)
            for (int y = 6; y <= 28; y++)
            {
                for (int x = 8; x <= 40; x++)
                {
                    cols[y * W + x] = cMuroFondo;
                }
            }

            // Toldo a rayas rojas y blancas en la parte superior (Y = 24 a 38)
            for (int y = 24; y <= 38; y++)
            {
                for (int x = 4; x <= 44; x++)
                {
                    bool borde = (x == 4 || x == 44 || y == 38 || y == 24);
                    if (borde)
                    {
                        cols[y * W + x] = cBorde;
                    }
                    else
                    {
                        int franja = (x / 5) % 2;
                        cols[y * W + x] = (franja == 0) ? cToldoRojo : cToldoBlanco;
                    }
                }
            }

            // Mostrador de madera inferior (Y = 6 a 16)
            for (int y = 6; y <= 16; y++)
            {
                for (int x = 6; x <= 42; x++)
                {
                    bool borde = (x == 6 || x == 42 || y == 6 || y == 16);
                    if (borde)
                    {
                        cols[y * W + x] = cBorde;
                    }
                    else
                    {
                        cols[y * W + x] = (y >= 14) ? cMadera : cMaderaOscura;
                    }
                }
            }

            // Adorno de moneda dorada en el mostrador central
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (dx * dx + dy * dy <= 4)
                    {
                        cols[(11 + dy) * W + (24 + dx)] = cDorado;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();

            _spritePuestito = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.4f), 16f);
            return _spritePuestito;
        }
    }
}
