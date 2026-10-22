using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Gestor de Object Pooling para proyectiles locales simulados.
    /// Evita Instantiate/Destroy en runtime eliminando picos de Garbage Collector.
    /// </summary>
    public class PoolProyectiles : MonoBehaviour
    {
        public static PoolProyectiles Instancia { get; private set; }

        private const int TAMANO_INICIAL = 48;
        private const int LIMITE_MAXIMO = 120;

        private readonly Queue<ProyectilLocal> _poolDisponibles = new Queue<ProyectilLocal>(TAMANO_INICIAL);
        private Transform _contenedorPool;
        private static Sprite _spriteBolaFuego;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            _contenedorPool = new GameObject("ContenedorPoolProyectiles").transform;
            _contenedorPool.SetParent(transform, false);

            Precalentar();
        }

        public static PoolProyectiles ObtenerOCrear()
        {
            if (Instancia != null) return Instancia;

            var go = new GameObject("PoolProyectiles");
            DontDestroyOnLoad(go);
            Instancia = go.AddComponent<PoolProyectiles>();
            return Instancia;
        }

        private void Precalentar()
        {
            for (int i = 0; i < TAMANO_INICIAL; i++)
            {
                var p = CrearNuevoProyectil();
                p.gameObject.SetActive(false);
                _poolDisponibles.Enqueue(p);
            }
        }

        private ProyectilLocal CrearNuevoProyectil()
        {
            var go = new GameObject("ProyectilLocal");
            go.transform.SetParent(_contenedorPool, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ObtenerSpriteBolaFuego();
            sr.sortingOrder = 9;

            var proj = go.AddComponent<ProyectilLocal>();
            return proj;
        }

        public static Sprite ObtenerSpriteBolaFuego()
        {
            if (_spriteBolaFuego != null) return _spriteBolaFuego;

            // Intentar cargar sprite existente del proyecto
            var spriteCargado = Resources.Load<Sprite>("Arte/Personajes/Fuego/BolaFuego");
            if (spriteCargado != null)
            {
                _spriteBolaFuego = spriteCargado;
                return _spriteBolaFuego;
            }

            // Generar sprite procedural pixel art 16x16 de alta fidelidad
            int s = 16;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color cBorde = new Color(0.85f, 0.15f, 0.05f, 1f);
            Color cCuerpo = new Color(1f, 0.48f, 0.05f, 1f);
            Color cMedio = new Color(1f, 0.80f, 0.15f, 1f);
            Color cNucleo = new Color(1f, 0.98f, 0.75f, 1f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    if (d <= 2.2f) tex.SetPixel(x, y, cNucleo);
                    else if (d <= 4.2f) tex.SetPixel(x, y, cMedio);
                    else if (d <= 6.2f) tex.SetPixel(x, y, cCuerpo);
                    else if (d <= 7.2f) tex.SetPixel(x, y, cBorde);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            _spriteBolaFuego = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            return _spriteBolaFuego;
        }

        public ProyectilLocal DispararBolaFuego(
            Personaje lanzador,
            Vector2 origen,
            Vector2 dir,
            float vel,
            int dano,
            bool esPropietarioLocal,
            bool quemadura = true,
            int bonoQuem = 0,
            float multArea = 1.0f)
        {
            ProyectilLocal p = null;
            while (_poolDisponibles.Count > 0 && p == null)
            {
                p = _poolDisponibles.Dequeue();
            }

            if (p == null)
            {
                p = CrearNuevoProyectil();
            }

            p.ConfigurarSprite(ObtenerSpriteBolaFuego(), Color.white, 0.65f);
            p.Inicializar(lanzador, dir, origen, vel, dano, esPropietarioLocal, quemadura, bonoQuem, multArea, DevolverAlPool);
            return p;
        }

        private void DevolverAlPool(ProyectilLocal p)
        {
            if (p == null) return;

            if (_poolDisponibles.Count < LIMITE_MAXIMO)
            {
                p.transform.SetParent(_contenedorPool, false);
                _poolDisponibles.Enqueue(p);
            }
            else
            {
                Destroy(p.gameObject);
            }
        }
    }
}
