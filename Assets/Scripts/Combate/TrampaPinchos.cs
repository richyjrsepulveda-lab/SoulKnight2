using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Trampa de pinchos interactiva de mazmorra.
    /// Soporta 2 frames (off/on) o 4 frames (0 apagado -> 1 aviso -> 2/3 activos con daño -> 0).
    /// Solo causa daño cuando los pinchos están alzados.
    /// </summary>
    public class TrampaPinchos : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float intervaloOculto = 1.8f;
        public float duracionAviso = 0.4f;
        public float duracionActivo = 1.2f;
        public int danoTick = 5;

        private int _estado = 0; // 0 = Oculto, 1 = Aviso, 2 = Activo
        private float _timer = 0f;
        private float _timerDano = 0f;
        private readonly List<Collider2D> _personajesDentro = new List<Collider2D>();

        void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            _timer = Random.Range(0f, intervaloOculto);
            ActualizarEstado(0);
        }

        void Update()
        {
            _timer += Time.deltaTime;

            if (_estado == 0 && _timer >= intervaloOculto)
            {
                _timer = 0f;
                ActualizarEstado((frames != null && frames.Length >= 4) ? 1 : 2);
            }
            else if (_estado == 1 && _timer >= duracionAviso)
            {
                _timer = 0f;
                ActualizarEstado(2);
            }
            else if (_estado == 2 && _timer >= duracionActivo)
            {
                _timer = 0f;
                ActualizarEstado(0);
            }

            if (_estado == 2 && _personajesDentro.Count > 0)
            {
                _timerDano += Time.deltaTime;
                if (_timerDano >= 0.45f)
                {
                    _timerDano = 0f;
                    AplicarDanoPersonajes();
                }
            }
        }

        private void ActualizarEstado(int nuevoEstado)
        {
            _estado = nuevoEstado;
            if (sr != null && frames != null && frames.Length > 0)
            {
                if (frames.Length >= 4)
                {
                    if (_estado == 0) sr.sprite = frames[0];
                    else if (_estado == 1) sr.sprite = frames[1];
                    else sr.sprite = frames[2];
                }
                else if (frames.Length >= 2)
                {
                    sr.sprite = (_estado == 2) ? frames[1] : frames[0];
                }
                else
                {
                    sr.sprite = frames[0];
                }
            }

            if (_estado == 2)
            {
                AplicarDanoPersonajes();
            }
        }

        private void AplicarDanoPersonajes()
        {
            for (int i = _personajesDentro.Count - 1; i >= 0; i--)
            {
                var col = _personajesDentro[i];
                if (col == null)
                {
                    _personajesDentro.RemoveAt(i);
                    continue;
                }

                var pj = col.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(danoTick);
                    continue;
                }

                var en = col.GetComponent<EnemigoBase>();
                if (en != null && en.VidaActual != null && en.VidaActual.Value > 0)
                {
                    en.RecibirDano(danoTick, new Color(0.9f, 0.2f, 0.2f));
                }
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_personajesDentro.Contains(other))
            {
                _personajesDentro.Add(other);
                if (_estado == 2)
                {
                    AplicarDanoPersonajes();
                }
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            _personajesDentro.Remove(other);
        }

        public void Configurar(Sprite[] framesAnim, int dano = 5)
        {
            frames = framesAnim;
            danoTick = dano;
            ActualizarEstado(0);
        }

        public void Configurar(Sprite off, Sprite on, int dano = 5)
        {
            frames = new Sprite[] { off, on };
            danoTick = dano;
            ActualizarEstado(0);
        }
    }
}
