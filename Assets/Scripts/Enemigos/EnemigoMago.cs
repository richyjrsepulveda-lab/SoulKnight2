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
        public float tiempoEntreDisparos = 1.35f;
        public int danoProyectil = 12;
        public float rangoDisparo = 12f;
        public float distanciaOptima = 5.5f;

        private float _timerDisparo = 0.6f;
        private float _tiempoCasteo = 0f;

        protected override void Awake()
        {
            vidaMax = 95;
            velocidadBase = 3.0f;
            dano = 8;
            rangoAtaque = 1.8f;
            tiempoEntreAtaques = 1.10f;
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
                // Iniciar casteo rápido
                _tiempoCasteo = 0.22f;
                _timerDisparo = tiempoEntreDisparos + UnityEngine.Random.Range(-0.15f, 0.20f);
                Vector2 dirCasteo = ((Vector2)jugador.transform.position - (Vector2)transform.position).normalized;
                MostrarCasteoClientRpc((Vector2)transform.position + dirCasteo * 0.60f, BiomaActualNet.Value);
            }
        }

        [ClientRpc]
        private void MostrarCasteoClientRpc(Vector2 pos, int bioma)
        {
            var sparkObj = new GameObject("CasteoMagico");
            sparkObj.transform.position = new Vector3(pos.x, pos.y, -0.4f);

            var lr = sparkObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 10;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.02f;
            lr.loop = true;

            Color c;
            if (bioma == (int)TipoBioma.Fuego) c = new Color(1f, 0.55f, 0.10f, 0.95f);
            else if (bioma == (int)TipoBioma.Hielo) c = new Color(0.35f, 0.88f, 1f, 0.95f);
            else if (bioma == (int)TipoBioma.Planta) c = new Color(0.45f, 1f, 0.25f, 0.95f);
            else c = new Color(1f, 0.85f, 0.30f, 0.95f);

            lr.startColor = c;
            lr.endColor = c;

            const int seg = 12;
            lr.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.35f, 0f));
            }

            sparkObj.AddComponent<AnimacionZarpazo>().Inicializar(0.35f);
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

                int elem = 0;
                if (BiomaActualNet.Value == (int)TipoBioma.Planta)
                {
                    proj.aplicaEsporas = true;
                    elem = 3;
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Fuego)
                {
                    proj.aplicaQuemadura = true;
                    elem = 1;
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Hielo)
                {
                    proj.aplicaRalentizacion = true;
                    elem = 2;
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Arena)
                {
                    elem = 4;
                }
                proj.TipoElemento.Value = elem;
            }

            var netObj = bola.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
    }
}
