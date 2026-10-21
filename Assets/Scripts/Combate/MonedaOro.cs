using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Moneda de oro física con animación fluida de brinco, rebotes en el piso,
    /// sombra dinámica, reposo flotante, y atracción magnética hacia el jugador.
    /// Sincronizada en multijugador a través de SalaRed y Personaje.
    /// </summary>
    public class MonedaOro : MonoBehaviour
    {
        public static readonly Dictionary<int, MonedaOro> Todas = new Dictionary<int, MonedaOro>();

        public int idMoneda = 0;
        public int valor = 1;

        private SpriteRenderer _srMoneda;
        private SpriteRenderer _srSombra;
        private Transform _visualMoneda;
        private Transform _visualSombra;

        // Físicas del brinco parabólico
        private Vector2 _posBase;
        private Vector2 _velHorizontal;
        private float _altura = 0f;
        private float _velVertical = 0f;
        private int _rebotes = 0;
        private bool _asentada = false;
        private bool _recogida = false;
        private float _tiempoVivo = 0f;

        private static Sprite[] _spritesMonedaAnim;
        private static Sprite _spriteSombra;

        void Awake()
        {
            AsegurarSpritesProcedurales();

            // Sombra en el piso
            var sombraObj = new GameObject("SombraMoneda");
            sombraObj.transform.SetParent(transform, false);
            _visualSombra = sombraObj.transform;
            _srSombra = sombraObj.AddComponent<SpriteRenderer>();
            _srSombra.sprite = _spriteSombra;
            _srSombra.sortingOrder = 2; // Sobre el piso
            _srSombra.color = new Color(0f, 0f, 0f, 0.45f);

            // Objeto visual de la moneda
            var monedaObj = new GameObject("VisualMoneda");
            monedaObj.transform.SetParent(transform, false);
            _visualMoneda = monedaObj.transform;
            _srMoneda = monedaObj.AddComponent<SpriteRenderer>();
            _srMoneda.sprite = _spritesMonedaAnim[0];
            _srMoneda.sortingOrder = 7; // Sobre el jugador y props

            _posBase = transform.position;
        }

        void OnDestroy()
        {
            Todas.Remove(idMoneda);
        }

        public void IniciarSalto(int id, int valorOro, Vector2 dirImpulso, float fuerzaHorizontal = 2.0f, float fuerzaVertical = 4.8f)
        {
            idMoneda = id;
            valor = valorOro;
            Todas[id] = this;

            _posBase = transform.position;
            _velHorizontal = dirImpulso.normalized * fuerzaHorizontal;
            _velVertical = fuerzaVertical;
            _altura = 0.1f;
            _rebotes = 0;
            _asentada = false;
            _recogida = false;
            _tiempoVivo = 0f;
        }

        void Update()
        {
            _tiempoVivo += Time.deltaTime;

            if (_recogida) return;

            // 1. Fase de Brinco y Rebotes iniciales
            if (!_asentada)
            {
                const float gravedad = 18.0f;
                _velVertical -= gravedad * Time.deltaTime;
                _altura += _velVertical * Time.deltaTime;
                _posBase += _velHorizontal * Time.deltaTime;

                if (_altura <= 0f)
                {
                    _altura = 0f;
                    _rebotes++;

                    if (_rebotes >= 2)
                    {
                        _asentada = true;
                        _velHorizontal = Vector2.zero;
                        _velVertical = 0f;
                    }
                    else
                    {
                        _velVertical = Mathf.Abs(_velVertical) * 0.42f;
                        _velHorizontal *= 0.55f;
                    }
                }

                // Rotación en el aire mientras salta
                if (_visualMoneda != null)
                {
                    int frameRot = Mathf.FloorToInt(_tiempoVivo * 14f) % _spritesMonedaAnim.Length;
                    _srMoneda.sprite = _spritesMonedaAnim[frameRot];
                }
            }
            else
            {
                // 2. Fase de Reposo en el Piso (Brillo y Flotación sutil)
                float bob = Mathf.Sin(_tiempoVivo * 5.0f) * 0.08f;
                _altura = Mathf.Max(0f, 0.08f + bob);

                // Animación de destello / giro lento cada 2.5s
                int frame = Mathf.FloorToInt((_tiempoVivo * 4f)) % _spritesMonedaAnim.Length;
                _srMoneda.sprite = _spritesMonedaAnim[frame];

                // 3. Atracción magnética hacia los jugadores vivos
                Personaje objetivoCercano = null;
                float menorDist = 2.4f;

                for (int i = 0; i < Personaje.Todos.Count; i++)
                {
                    var pj = Personaje.Todos[i];
                    if (pj != null && !pj.EstaMuerto.Value)
                    {
                        float d = Vector2.Distance(_posBase, pj.transform.position);
                        if (d < menorDist)
                        {
                            menorDist = d;
                            objetivoCercano = pj;
                        }
                    }
                }

                if (objetivoCercano != null)
                {
                    // Vuelo magnético acelerado hacia el jugador
                    Vector2 dir = ((Vector2)objetivoCercano.transform.position - _posBase).normalized;
                    float velocidadMagnetica = Mathf.Lerp(12f, 22f, 1f - (menorDist / 2.4f));
                    _posBase += dir * velocidadMagnetica * Time.deltaTime;

                    // Si está lo suficientemente cerca, recogerla
                    if (menorDist <= 0.65f)
                    {
                        ProcesarRecoleccion(objetivoCercano);
                    }
                }
            }

            // Actualizar transformaciones visuales desacopladas (moneda arriba, sombra en piso)
            transform.position = (Vector3)_posBase;
            if (_visualMoneda != null)
            {
                _visualMoneda.localPosition = new Vector3(0f, _altura, 0f);
            }
            if (_visualSombra != null)
            {
                float escalaSombra = Mathf.Clamp(1.0f - (_altura * 0.35f), 0.4f, 1.0f);
                _visualSombra.localScale = new Vector3(escalaSombra, escalaSombra * 0.6f, 1f);
                _srSombra.color = new Color(0f, 0f, 0f, 0.45f * escalaSombra);
            }
        }

        private void ProcesarRecoleccion(Personaje pj)
        {
            if (_recogida) return;
            _recogida = true;

            if (pj.EsControlLocal)
            {
                pj.ReclamarMonedaLocalmente(idMoneda, valor);
            }
            else if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                pj.AgregarOro(valor);
                EfectoRecogerVisual();
                Destroy(gameObject);
            }
        }

        public void DesaparecerPorRecoleccion()
        {
            if (_recogida) return;
            _recogida = true;
            EfectoRecogerVisual();
            Destroy(gameObject);
        }

        public void EfectoRecogerVisual()
        {
            GestorAudio.PlaySFX(TipoSFX.RecogerOro, Random.Range(0.98f, 1.15f));
            CrearBrilloMoneda(_posBase + Vector2.up * _altura);
        }

        private static void CrearBrilloMoneda(Vector2 pos)
        {
            var go = new GameObject("BrilloRecogerMoneda");
            go.transform.position = (Vector3)pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spritesMonedaAnim[0];
            sr.sortingOrder = 18;
            sr.color = new Color(1f, 0.95f, 0.4f, 1f);

            var mon = go.AddComponent<MonedaOroEfectoDesvanecer>();
            mon.Iniciar();
        }

        private static void AsegurarSpritesProcedurales()
        {
            if (_spritesMonedaAnim != null && _spriteSombra != null) return;

            // 4 Frames de rotación de la moneda dorada pixel-art (16x16)
            _spritesMonedaAnim = new Sprite[4];
            int[] anchos = new int[] { 14, 10, 5, 10 };

            for (int f = 0; f < 4; f++)
            {
                const int S = 16;
                Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                Color cBorde = new Color(0.28f, 0.16f, 0.02f, 1f);
                Color cOroOsc = new Color(0.82f, 0.58f, 0.08f, 1f);
                Color cOroMed = new Color(1f, 0.82f, 0.18f, 1f);
                Color cOroBrillo = new Color(1f, 0.96f, 0.55f, 1f);

                int wF = anchos[f];
                int xMin = (S - wF) / 2;
                int xMax = xMin + wF - 1;

                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }

                for (int y = 2; y <= 13; y++)
                {
                    int recorteY = (y == 2 || y == 13) ? 2 : ((y == 3 || y == 12) ? 1 : 0);
                    int xStart = xMin + recorteY;
                    int xEnd = xMax - recorteY;

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        bool esBorde = (x == xStart || x == xEnd || y == 2 || y == 13);
                        if (esBorde)
                        {
                            tex.SetPixel(x, y, cBorde);
                        }
                        else if (y >= 10 && x <= xStart + (xEnd - xStart) / 2)
                        {
                            tex.SetPixel(x, y, cOroBrillo);
                        }
                        else if (y >= 6)
                        {
                            tex.SetPixel(x, y, cOroMed);
                        }
                        else
                        {
                            tex.SetPixel(x, y, cOroOsc);
                        }
                    }
                }

                // Símbolo de estrella o línea interior si está de frente
                if (f == 0)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        tex.SetPixel(7, 7 + dy, cOroBrillo);
                        tex.SetPixel(8, 7 + dy, cOroOsc);
                    }
                }

                tex.Apply();
                _spritesMonedaAnim[f] = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 16f);
            }

            // Sombra ovalada suave
            {
                const int sw = 16;
                const int sh = 10;
                Texture2D texS = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
                texS.filterMode = FilterMode.Point;
                texS.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < sh; y++)
                {
                    for (int x = 0; x < sw; x++)
                    {
                        float nx = (x - 7.5f) / 6.5f;
                        float ny = (y - 4.5f) / 3.5f;
                        float d = nx * nx + ny * ny;
                        if (d <= 1.0f)
                        {
                            texS.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            texS.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                texS.Apply();
                _spriteSombra = Sprite.Create(texS, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 16f);
            }
        }

        public static void DestruirTodas()
        {
            var lista = new List<MonedaOro>(Todas.Values);
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i] != null) Destroy(lista[i].gameObject);
            }
            Todas.Clear();
        }
    }

    public class MonedaOroEfectoDesvanecer : MonoBehaviour
    {
        public void Iniciar()
        {
            StartCoroutine(RutinaFade());
        }

        private IEnumerator RutinaFade()
        {
            var sr = GetComponent<SpriteRenderer>();
            float t = 0f;
            float dur = 0.22f;
            Vector3 posIni = transform.position;

            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                transform.position = posIni + Vector3.up * (p * 0.6f);
                transform.localScale = Vector3.one * (1.0f + p * 0.4f);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, p);
                    sr.color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
