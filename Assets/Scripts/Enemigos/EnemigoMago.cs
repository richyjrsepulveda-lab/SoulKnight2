using System;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Enemigo Mago / Hechicero Arcano:
    /// - Mantiene la distancia con los jugadores.
    /// - Dispara orbes mágicos periódicamente.
    /// - Si un jugador se acerca demasiado, retrocede de inmediato.
    /// </summary>
    public class EnemigoMago : EnemigoBase
    {
        [Header("Hechizos y Disparo")]
        public GameObject prefabProyectil;
        public float tiempoEntreDisparos = 2.8f;
        public int danoProyectil = 12;
        public float rangoDisparo = 12f;
        public float distanciaOptima = 5.5f;

        private float _timerDisparo = 1.5f;
        private float _tiempoCasteo = 0f;

        protected override void Awake()
        {
            vidaMax = 75;
            velocidadBase = 2.8f;
            dano = 8;
            rangoAtaque = 1.8f;
            tiempoEntreAtaques = 2.5f;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsServer) return;
            if (GestorPartida.Instancia != null && GestorPartida.Instancia.PartidaTerminada) return;

            // Si está congelado, no castea hechizos
            if (EstaCongelado.Value) return;

            Personaje jugador = ObtenerJugadorMasCercano();
            if (jugador == null || jugador.EstaMuerto.Value) return;

            float distancia = Vector2.Distance(transform.position, jugador.transform.position);

            // Flotación / Levadizo visual sutil
            transform.position += new Vector3(0f, Mathf.Sin(Time.time * 3f) * 0.0015f, 0f);

            // Temporizador de disparo
            _timerDisparo -= Time.deltaTime;
            if (_tiempoCasteo > 0f)
            {
                _tiempoCasteo -= Time.deltaTime;
                if (_tiempoCasteo <= 0f)
                {
                    DispararAlJugador(jugador);
                }
            }
            else if (_timerDisparo <= 0f && distancia <= rangoDisparo)
            {
                // Iniciar casteo
                _tiempoCasteo = 0.35f;
                _timerDisparo = tiempoEntreDisparos + UnityEngine.Random.Range(-0.4f, 0.5f);
            }
        }

        private void DispararAlJugador(Personaje jugador)
        {
            if (prefabProyectil == null || jugador == null) return;

            Vector2 dir = ((Vector2)jugador.transform.position - (Vector2)transform.position).normalized;
            GameObject bola = Instantiate(prefabProyectil, transform.position, Quaternion.identity);

            var proj = bola.GetComponent<Proyectil>();
            if (proj != null)
            {
                proj.proyectilEnemigo = true;
                proj.Configurar(dir, danoProyectil, 999999);
            }

            var netObj = bola.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
    }
}
