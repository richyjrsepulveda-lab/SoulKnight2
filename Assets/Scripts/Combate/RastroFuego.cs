using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Rastro de fuego ardiente en el suelo dejado durante el estado de Supernova del Personaje de Fuego.
    /// Quema y daña a los enemigos que lo pisen mientras permanezca encendido.
    /// </summary>
    public class RastroFuego : MonoBehaviour
    {
        private float _duracion = 2.5f;
        private float _tiempoVida = 0f;
        private int _danoPorTick = 16;
        private float _timerTick = 0f;
        private SpriteRenderer _sr;

        private static Sprite _spriteRastro;

        public void Configurar(int danoTick, float duracion = 2.5f)
        {
            _danoPorTick = danoTick;
            _duracion = duracion;
        }

        private void Awake()
        {
            AsegurarSprite();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = _spriteRastro;
            _sr.sortingOrder = 2; // Por detrás de entidades pero sobre el suelo
            transform.localScale = Vector3.one * 0.9f;
        }

        private static void AsegurarSprite()
        {
            if (_spriteRastro != null) return;
            int size = 20;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Vector2 centro = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float radio = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), centro);
                    if (dist <= radio)
                    {
                        float t = dist / radio;
                        if (t < 0.35f)
                            tex.SetPixel(x, y, new Color(1f, 1f, 0.72f, 0.95f)); // Núcleo incandescente
                        else if (t < 0.72f)
                            tex.SetPixel(x, y, new Color(1f, 0.50f, 0.08f, 0.88f)); // Llamas naranja
                        else
                            tex.SetPixel(x, y, new Color(0.85f, 0.16f, 0.02f, 0.65f)); // Borde ardiente
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            _spriteRastro = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private void Update()
        {
            _tiempoVida += Time.deltaTime;

            // Crepitación y pulsación visual
            float pulso = 0.88f + Mathf.Sin(_tiempoVida * 16f) * 0.09f;

            // Desvanecimiento suave en los últimos instantes
            if (_tiempoVida > _duracion - 0.6f)
            {
                float tFade = Mathf.Clamp01((_duracion - _tiempoVida) / 0.6f);
                pulso *= tFade;
                if (_sr != null)
                {
                    Color c = _sr.color;
                    c.a = tFade;
                    _sr.color = c;
                }
            }
            transform.localScale = new Vector3(pulso, pulso, 1f);

            // Verificación y aplicación de daño en Servidor
            _timerTick -= Time.deltaTime;
            if (_timerTick <= 0f)
            {
                _timerTick = 0.28f;
                AplicarDanoEnArea();
            }

            if (_tiempoVida >= _duracion)
            {
                Destroy(gameObject);
            }
        }

        private void AplicarDanoEnArea()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
                return;

            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 0.75f);
            foreach (var col in cols)
            {
                if (col == null || col.isTrigger) continue;
                var enemigo = col.GetComponent<EnemigoBase>();
                if (enemigo != null && enemigo.VidaActual.Value > 0)
                {
                    enemigo.RecibirDano(_danoPorTick, new Color(0.96f, 0.40f, 0.05f)); // Naranja fuego
                    enemigo.AplicarQuemadura(Mathf.RoundToInt(_danoPorTick * 0.75f));
                }
            }
        }
    }
}
