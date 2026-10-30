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
        public int danoProyectil = 8;
        public float rangoDisparo = 7.5f; // Rango acotado para evitar disparos fuera de pantalla
        public float distanciaOptima = 4.8f;

        private float _timerDisparo = 0.6f;
        private float _tiempoCasteo = 0f;
        private bool _sentidoOrbital = true;
        private float _cooldownTeleportDodge = 0f;
        private static readonly Collider2D[] _amenazasBufferMago = new Collider2D[16];

        protected override void Awake()
        {
            vidaMax = 95;
            velocidadBase = 3.0f;
            dano = 6;
            rangoAtaque = 1.8f;
            tiempoEntreAtaques = 1.10f;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsServer) return;
            if (GestorPartida.Instancia != null && GestorPartida.Instancia.PartidaTerminada) return;

            // Si está congelado, no castea hechizos ni esquiva
            if (EstaCongelado.Value) return;

            Personaje jugador = ObtenerJugadorMasCercano();
            if (jugador == null || jugador.EstaMuerto.Value) return;

            // Cooldown de esquiva mágica (teletransporte / finta)
            if (_cooldownTeleportDodge > 0f)
            {
                _cooldownTeleportDodge -= Time.deltaTime;
            }
            else
            {
                VerificarTeleportDodge(jugador);
            }

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
                _tiempoCasteo = 0.20f;
                _timerDisparo = tiempoEntreDisparos + UnityEngine.Random.Range(-0.10f, 0.20f);
                Vector2 dirCasteo = ((Vector2)jugador.transform.position - (Vector2)transform.position).normalized;
                MostrarCasteoClientRpc((Vector2)transform.position + dirCasteo * 0.60f, BiomaActualNet.Value);
            }
        }

        protected override void TomarDecisionProbabilistica(Personaje jugador)
        {
            Vector2 posJugador = jugador.transform.position;
            Vector2 posPropia = transform.position;
            Vector2 dirHaciaJugador = (posJugador - posPropia).normalized;
            float distancia = Vector2.Distance(posPropia, posJugador);

            Vector2 dirPerpendicular = new Vector2(-dirHaciaJugador.y, dirHaciaJugador.x) * (_sentidoOrbital ? 1f : -1f);

            if (distancia < 4.2f)
            {
                // El jugador se acercó mucho: kiting activo retrocediendo en ángulo
                _modoActual = ModoMovimiento.Retroceder;
                _direccionMovimiento = (-dirHaciaJugador * 0.85f + dirPerpendicular * 0.45f).normalized;
                _factorVelocidad = 1.30f;
            }
            else if (distancia <= 7.2f)
            {
                // Rango óptimo de disparo (4.2m a 7.2m): orbitar lateralmente para ametrallar al jugador
                _modoActual = ModoMovimiento.Flanquear;
                if (UnityEngine.Random.value < 0.20f) _sentidoOrbital = !_sentidoOrbital;
                _direccionMovimiento = (dirPerpendicular * 0.88f + dirHaciaJugador * 0.12f).normalized;
                _factorVelocidad = 0.95f;
            }
            else
            {
                // Acercarse hasta entrar en rango de disparo (7.5m)
                _modoActual = ModoMovimiento.Perseguir;
                _direccionMovimiento = (dirHaciaJugador * 0.85f + dirPerpendicular * 0.25f).normalized;
                _factorVelocidad = 1.05f;
            }
        }

        private void VerificarTeleportDodge(Personaje jugador)
        {
            if (_cooldownTeleportDodge > 0f || !IsServer) return;

            bool amenazaDetectada = false;
            Vector2 dirAmenaza = Vector2.zero;

            // 1. Detectar proyectiles de jugadores acercándose
            int n = Physics2D.OverlapCircleNonAlloc(transform.position, 2.6f, _amenazasBufferMago);
            for (int i = 0; i < n; i++)
            {
                var col = _amenazasBufferMago[i];
                if (col == null) continue;
                var proj = col.GetComponent<Proyectil>();
                if (proj != null && !proj.proyectilEnemigo)
                {
                    Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                    Vector2 velProj = rbProj != null ? rbProj.velocity : Vector2.zero;
                    Vector2 haciaMi = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
                    if (velProj.sqrMagnitude > 0.5f && Vector2.Dot(velProj.normalized, haciaMi) > 0.35f)
                    {
                        amenazaDetectada = true;
                        dirAmenaza = velProj.normalized;
                        break;
                    }
                }
            }

            // 2. O si el jugador se abalanza agresivamente
            if (!amenazaDetectada && jugador != null)
            {
                float d = Vector2.Distance(transform.position, jugador.transform.position);
                var rbJ = jugador.GetComponent<Rigidbody2D>();
                if (d < 2.2f || (d < 3.2f && rbJ != null && rbJ.velocity.magnitude > 7.0f))
                {
                    amenazaDetectada = true;
                    dirAmenaza = ((Vector2)transform.position - (Vector2)jugador.transform.position).normalized;
                }
            }

            if (amenazaDetectada)
            {
                // Mini-teletransporte lateral o de retroceso evasivo (2.2m)
                Vector2 dirEvasiva = Vector2.Perpendicular(dirAmenaza) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
                dirEvasiva = EvitarObstaculos(transform.position, dirEvasiva);

                Vector2 posDestino = (Vector2)transform.position + dirEvasiva * 2.2f;

                // Comprobar que el destino no esté dentro de una pared
                Collider2D colDestino = Physics2D.OverlapCircle(posDestino, 0.38f);
                if (colDestino == null || colDestino.isTrigger || colDestino.GetComponent<Personaje>() != null)
                {
                    Vector2 posOrigen = transform.position;
                    transform.position = posDestino;
                    if (_rb != null) _rb.position = posDestino;
                    _cooldownTeleportDodge = 3.2f;
                    MostrarTeleportDodgeClientRpc(posOrigen, posDestino);
                }
            }
        }

        [ClientRpc]
        private void MostrarTeleportDodgeClientRpc(Vector2 origen, Vector2 destino)
        {
            GestorAudio.PlaySFX(TipoSFX.Teleport, 0.85f);
            CrearChispaArcana(origen);
            CrearChispaArcana(destino);
        }

        private void CrearChispaArcana(Vector2 pos)
        {
            var spark = new GameObject("ChispaArcanaMago");
            spark.transform.position = new Vector3(pos.x, pos.y, -0.4f);
            var lr = spark.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 14;
            lr.startWidth = 0.20f;
            lr.endWidth = 0.02f;
            lr.startColor = new Color(0.80f, 0.40f, 1f, 0.95f);
            lr.endColor = new Color(0.40f, 0.90f, 1f, 0.1f);
            lr.positionCount = 8;
            for (int i = 0; i < 8; i++)
            {
                float a = (i / 8f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a) * 0.45f, 0f));
            }
            Destroy(spark, 0.25f);
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

            // Puntería predictiva inteligente: anticipar la posición futura del jugador según su inercia
            Vector2 posJugador = jugador.transform.position;
            Vector2 velJugador = Vector2.zero;
            var rbJugador = jugador.GetComponent<Rigidbody2D>();
            if (rbJugador != null)
            {
                velJugador = rbJugador.velocity;
            }

            float distancia = Vector2.Distance(transform.position, posJugador);
            float velocidadProyectil = 9.0f;
            float tiempoVuelo = distancia / velocidadProyectil;

            Vector2 posPredicha = posJugador + (velJugador * Mathf.Clamp(tiempoVuelo, 0f, 0.45f) * 0.85f);
            Vector2 dir = (posPredicha - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

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
