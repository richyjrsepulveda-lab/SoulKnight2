using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Puestito físico de tienda mística dentro de habitaciones de relleno.
    /// Al acercarse o entrar en su puerta, se interactúa con [F] para abrir
    /// el menú de compras en el HUD con hasta 6 mejoras de características
    /// basadas en los personajes activos en la run y universales.
    /// </summary>
    public class PuestitoTienda : MonoBehaviour
    {
        public static readonly List<PuestitoTienda> Todos = new List<PuestitoTienda>();

        public readonly List<TipoMejoraTienda> itemsOfrecidos = new List<TipoMejoraTienda>();

        private TextMesh _textoIndicador;
        private static Sprite _spritePuestito;
        private bool _articulosInicializados = false;

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
            ConstruirVisualYPuestito();
            LimpiarMonedasCercanas();
        }

        void Start()
        {
            AsegurarArticulos();
            LimpiarMonedasCercanas();
        }

        private void LimpiarMonedasCercanas()
        {
            var monedas = Object.FindObjectsOfType<MonedaOro>();
            for (int i = 0; i < monedas.Length; i++)
            {
                if (monedas[i] != null && Vector2.Distance(monedas[i].transform.position, transform.position) < 14f)
                {
                    Destroy(monedas[i].gameObject);
                }
            }
        }

        public void AsegurarArticulos()
        {
            if (_articulosInicializados && itemsOfrecidos.Count >= 6) return;
            InicializarArticulos();
            _articulosInicializados = true;
        }

        private void InicializarArticulos()
        {
            itemsOfrecidos.Clear();

            // Detectar qué personajes están activos en la run
            bool hayFuego = false;
            bool hayHielo = false;
            bool hayElec = false;

            if (SalaRed.Instancia != null && SalaRed.Instancia.Jugadores.Count > 0)
            {
                foreach (var j in SalaRed.Instancia.Jugadores)
                {
                    if (j.personaje == TipoPersonaje.Fuego) hayFuego = true;
                    if (j.personaje == TipoPersonaje.Hielo) hayHielo = true;
                    if (j.personaje == TipoPersonaje.Electrico) hayElec = true;
                }
            }

            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj != null)
                {
                    if (pj.TipoPj == TipoPersonaje.Fuego) hayFuego = true;
                    if (pj.TipoPj == TipoPersonaje.Hielo) hayHielo = true;
                    if (pj.TipoPj == TipoPersonaje.Electrico) hayElec = true;
                }
            }

            // Si por timing de inicio no se detectó ninguno, asumir todos los personajes posibles
            if (!hayFuego && !hayHielo && !hayElec)
            {
                hayFuego = true;
                hayHielo = true;
                hayElec = true;
            }

            // Construir el pool candidato con universales + los activos en la partida
            var pool = new List<TipoMejoraTienda>();

            // Universales (siempre presentes en la baraja de opciones)
            pool.Add(TipoMejoraTienda.VidaMaxima);
            pool.Add(TipoMejoraTienda.DanoUniversal);
            pool.Add(TipoMejoraTienda.CooldownUniversal);
            pool.Add(TipoMejoraTienda.PescadoAsado);
            pool.Add(TipoMejoraTienda.AreaHabilidades);
            pool.Add(TipoMejoraTienda.CooldownDefinitiva);
            pool.Add(TipoMejoraTienda.GolpeCritico);

            if (hayFuego)
            {
                pool.Add(TipoMejoraTienda.Fuego_DanoQuemadura);
                pool.Add(TipoMejoraTienda.Fuego_Area);
                pool.Add(TipoMejoraTienda.Fuego_Duracion);
                pool.Add(TipoMejoraTienda.Fuego_SupernovaGigante);
            }

            if (hayHielo)
            {
                pool.Add(TipoMejoraTienda.Hielo_DuracionCongelacion);
                pool.Add(TipoMejoraTienda.Hielo_AreaVentisca);
                pool.Add(TipoMejoraTienda.Hielo_PotenciaGlacial);
                pool.Add(TipoMejoraTienda.Hielo_TormentaEterna);
            }

            if (hayElec)
            {
                pool.Add(TipoMejoraTienda.Elec_Voltaje);
                pool.Add(TipoMejoraTienda.Elec_AreaTeleport);
                pool.Add(TipoMejoraTienda.Elec_CooldownDash);
                pool.Add(TipoMejoraTienda.Elec_RayoCataclismico);
            }

            // Barajar usando semilla sincronizada de la mazmorra para que a TODOS les salga exactamente lo mismo
            int semilla = (SalaRed.Instancia != null && SalaRed.Instancia.SemillaMapaActual.Value != 0)
                ? SalaRed.Instancia.SemillaMapaActual.Value + 33881
                : 987654;

            if (GestorPartida.Instancia != null)
            {
                semilla += GestorPartida.Instancia.NivelActual * 1237;
            }

            var rng = new System.Random(semilla);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                var temp = pool[i];
                pool[i] = pool[k];
                pool[k] = temp;
            }

            // Seleccionar un máximo de 6 cosas comprables en la tienda
            for (int i = 0; i < 6 && i < pool.Count; i++)
            {
                itemsOfrecidos.Add(pool[i]);
            }
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
                        {
                            HUDJugador.Instancia.CerrarTienda();
                        }
                        else
                        {
                            AsegurarArticulos();
                            HUDJugador.Instancia.AbrirTienda(this);
                        }
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

            tex.SetPixels(cols);
            tex.Apply();

            _spritePuestito = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.4f), 16f);
            return _spritePuestito;
        }
    }
}
