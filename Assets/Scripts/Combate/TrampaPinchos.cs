using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Trampa de pinchos interactiva de mazmorra. Alterna periódicamente entre oculta (spikes_off)
    /// y activa (spikes_on). Solo causa daño cuando los pinchos están alzados.
    /// </summary>
    public class TrampaPinchos : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite spriteOff;
        public Sprite spriteOn;
        public float intervaloOculto = 1.8f;
        public float intervaloActivo = 1.4f;
        public int danoTick = 5;

        private bool _estaActiva = false;
        private float _timer = 0f;
        private float _timerDano = 0f;
        private readonly List<Collider2D> _personajesDentro = new List<Collider2D>();

        void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            // Desfasar el inicio para que no todas las trampas de la sala se activen en sincronía perfecta
            _timer = Random.Range(0f, intervaloOculto);
            ActualizarEstado(false);
        }

        void Update()
        {
            _timer += Time.deltaTime;
            float tiempoObjetivo = _estaActiva ? intervaloActivo : intervaloOculto;

            if (_timer >= tiempoObjetivo)
            {
                _timer = 0f;
                ActualizarEstado(!_estaActiva);
            }

            if (_estaActiva && _personajesDentro.Count > 0)
            {
                _timerDano += Time.deltaTime;
                if (_timerDano >= 0.5f)
                {
                    _timerDano = 0f;
                    AplicarDanoPersonajes();
                }
            }
        }

        private void ActualizarEstado(bool activa)
        {
            _estaActiva = activa;
            if (sr != null)
            {
                sr.sprite = _estaActiva ? spriteOn : spriteOff;
            }
            if (_estaActiva)
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
                if (_estaActiva)
                {
                    AplicarDanoPersonajes();
                }
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            _personajesDentro.Remove(other);
        }

        public void Configurar(Sprite off, Sprite on, int dano = 5)
        {
            spriteOff = off;
            spriteOn = on;
            danoTick = dano;
            ActualizarEstado(false);
        }
    }
}
