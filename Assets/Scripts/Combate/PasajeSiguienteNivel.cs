using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Pasaje / Hoyito en la pared para avanzar al siguiente nivel:
    /// - Se ubica en la pared de la sala final de la mazmorra.
    /// - Permanece bloqueado/cerrado hasta que todos los enemigos son derrotados.
    /// - Al despejar la sala, se abre e ilumina con partículas y brillo místico.
    /// - Requiere que TODOS los jugadores vivos entren al hoyito para iniciar la transición.
    /// </summary>
    public class PasajeSiguienteNivel : MonoBehaviour
    {
        public static PasajeSiguienteNivel Instancia { get; private set; }

        [Header("Sprites")]
        public SpriteRenderer sr;
        public Sprite spriteCerrado;
        public Sprite spriteAbierto;

        [Header("Configuración")]
        public bool estaAbierto = false;

        private readonly HashSet<Personaje> _jugadoresDentro = new HashSet<Personaje>();
        private Vector3 _escalaBase = Vector3.one * 1.6f;
        private float _tiempoFeedback = 0f;

        void Awake()
        {
            Instancia = this;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            ActualizarVisual();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Update()
        {
            if (_tiempoFeedback > 0f) _tiempoFeedback -= Time.deltaTime;
        }

        public void Abrir()
        {
            estaAbierto = true;
            ActualizarVisual();
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.5f, 0), "¡PASADIZO SECRETO ABIERTO!", new Color(1f, 0.85f, 0.4f));
        }

        public void Cerrar()
        {
            estaAbierto = false;
            _jugadoresDentro.Clear();
            ActualizarVisual();
        }

        private void ActualizarVisual()
        {
            if (sr == null) return;
            if (estaAbierto)
            {
                if (spriteAbierto != null) sr.sprite = spriteAbierto;
                sr.color = Color.white;
            }
            else
            {
                if (spriteCerrado != null) sr.sprite = spriteCerrado;
                sr.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers == null || pers.EstaMuerto.Value) return;

            if (!estaAbierto)
            {
                if (_tiempoFeedback <= 0f)
                {
                    _tiempoFeedback = 2f;
                    TextoFlotante.Crear(transform.position + new Vector3(0, 1.2f, 0), "¡Sellado! Eliminen a todos los enemigos.", Color.red);
                }
                return;
            }

            _jugadoresDentro.Add(pers);
            VerificarJugadores();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers != null)
            {
                _jugadoresDentro.Remove(pers);
                ActualizarAvisoHUD();
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!estaAbierto) return;
            var pers = collision.GetComponent<Personaje>();
            if (pers != null && !pers.EstaMuerto.Value && !_jugadoresDentro.Contains(pers))
            {
                _jugadoresDentro.Add(pers);
                VerificarJugadores();
            }
        }

        public void VerificarJugadores()
        {
            if (!estaAbierto || GestorPartida.Instancia == null) return;

            // Limpiar referencias muertas o nulas
            _jugadoresDentro.RemoveWhere(p => p == null || p.EstaMuerto.Value);

            var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
            int totalVivos = vivos.Count;
            int dentro = _jugadoresDentro.Count;

            ActualizarAvisoHUD();

            // Si todos los jugadores vivos están dentro del pasaje
            if (totalVivos > 0 && dentro >= totalVivos)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    GestorPartida.Instancia.IniciarTransicionSiguienteNivel();
                }
            }
        }

        private void ActualizarAvisoHUD()
        {
            if (GestorPartida.Instancia == null) return;
            var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
            int totalVivos = vivos.Count;
            int dentro = _jugadoresDentro.Count;

            if (HUDJugador.Instancia != null && estaAbierto)
            {
                if (dentro < totalVivos)
                {
                    HUDJugador.Instancia.MostrarAvisoPasaje($"En el pasaje: {dentro}/{totalVivos}. ¡Entren todos para avanzar!");
                }
                else
                {
                    HUDJugador.Instancia.MostrarAvisoPasaje("¡Todos dentro! Avanzando al siguiente nivel...");
                }
            }
        }
    }
}
