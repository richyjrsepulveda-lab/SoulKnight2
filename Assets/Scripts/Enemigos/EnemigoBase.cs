using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Base para los enemigos del juego.
    /// Primer enemigo (Barril con Peces):
    /// - Estadísticas balanceadas: vida moderada (120), velocidad tranquila (3.5f) y daño no abusivo (10).
    /// - IA por probabilidad: alterna entre perseguir, rondar/flanquear, dudar/acechar y retroceder.
    /// - Efectos de estado: Congelado (inmóvil) y Quemadura (daño por tick).
    /// - Sincronizado por red: el servidor calcula la IA, vida y daño.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EnemigoBase : NetworkBehaviour
    {
        public enum ModoMovimiento { Perseguir, Flanquear, Dudar, Retroceder }

        [Header("Estadísticas Base (1er Enemigo Tranquilo)")]
        public int vidaMax = 120;
        public float velocidadBase = 3.5f; // Tranquilo comparado con el jugador (6.25f)
        public int dano = 10;
        public float rangoAtaque = 2.4f;
        public float tiempoEntreAtaques = 2.5f;
        public float rangoMaximoDeteccion = 14f; // No puede ver a jugadores si están lejos
        public bool enAnimacionEntrada = false;

        [Header("Efectos de Estado")]
        public float duracionRalentizacion = 4f;
        public float duracionCongelacion = 4f;
        public float duracionQuemado = 8f;
        public int danoQuemadoPorSegundo = 8;
        public float tickQuemado = 0.25f;

        [Header("Visuales")]
        public BarraVida barraVida;
        public GameObject spriteCongelado;

        public NetworkVariable<int> VidaActual = new NetworkVariable<int>(120);
        public NetworkVariable<bool> EstaCongelado = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> EstaRalentizado = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> EstaQuemado = new NetworkVariable<bool>(false);

        protected Rigidbody2D _rb;
        protected Collider2D _col;

        private float _velocidadActual;
        private float _tiempoAtaqueActual = 0f;
        private float _tiempoRalentizado = 0f;
        private float _tiempoCongelado = 0f;
        private float _tiempoQuemado = 0f;
        private float _tiempoTickQuemadoActual = 0f;
        private int _danoQuemadoPersonalizado = 0;

        // IA Probabilística
        private float _timerDecision = 0f;
        private ModoMovimiento _modoActual = ModoMovimiento.Perseguir;
        private Vector2 _direccionMovimiento = Vector2.zero;
        private float _factorVelocidad = 1f;

        private Vector2 _knockback = Vector2.zero;
        private float _knockbackFriccion = 25f;

        public static readonly List<EnemigoBase> Todos = new List<EnemigoBase>();
        private SpriteRenderer[] _renderersCacheados;

        public event Action AlMorir;

        protected virtual void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        protected virtual void OnDisable()
        {
            Todos.Remove(this);
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _velocidadActual = velocidadBase;
            _renderersCacheados = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void EstablecerVisibilidad(bool visible)
        {
            if (_renderersCacheados == null) _renderersCacheados = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < _renderersCacheados.Length; i++)
            {
                var sr = _renderersCacheados[i];
                if (sr != null && sr.gameObject != spriteCongelado)
                {
                    sr.enabled = visible;
                }
            }
            if (barraVida != null && !(this is JefeBase))
            {
                barraVida.gameObject.SetActive(visible);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                VidaActual.Value = vidaMax;
            }

            VidaActual.OnValueChanged += (prev, act) =>
            {
                if (barraVida != null) barraVida.Establecer(act, vidaMax);
            };

            EstaCongelado.OnValueChanged += (prev, act) =>
            {
                if (spriteCongelado != null) spriteCongelado.SetActive(act);
                ActualizarTinteVisual();
            };

            EstaRalentizado.OnValueChanged += (prev, act) =>
            {
                ActualizarTinteVisual();
            };

            EstaQuemado.OnValueChanged += (prev, act) =>
            {
                ActualizarTinteVisual();
            };

            if (barraVida != null) barraVida.Establecer(VidaActual.Value, vidaMax);
            if (spriteCongelado != null) spriteCongelado.SetActive(EstaCongelado.Value);
            ActualizarTinteVisual();
        }

        private void ActualizarTinteVisual()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (EstaCongelado.Value || EstaRalentizado.Value)
                {
                    // Filtro azul clarito helado para enemigos ralentizados o congelados
                    sr.color = new Color(0.48f, 0.85f, 1f, 1f);
                }
                else if (EstaQuemado.Value)
                {
                    // Filtro naranja para quemadura
                    sr.color = new Color(1f, 0.52f, 0.25f, 1f);
                }
                else
                {
                    sr.color = Color.white;
                }
            }
        }

        protected virtual void Update()
        {
            if (!IsServer) return;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0))
            {
                if (EstaQuemado.Value) EstaQuemado.Value = false;
                if (_rb != null) _rb.velocity = Vector2.zero;
                return;
            }

            // Procesar Efectos de Estado
            if (EstaCongelado.Value)
            {
                _tiempoCongelado -= Time.deltaTime;
                if (_tiempoCongelado <= 0f)
                {
                    EstaCongelado.Value = false;
                    _velocidadActual = velocidadBase;
                }
                return; // Bloquea acción y movimiento mientras está congelado
            }

            if (_tiempoRalentizado > 0f)
            {
                _tiempoRalentizado -= Time.deltaTime;
                if (_tiempoRalentizado <= 0f)
                {
                    _velocidadActual = velocidadBase;
                    EstaRalentizado.Value = false;
                }
            }

            if (EstaQuemado.Value)
            {
                _tiempoQuemado -= Time.deltaTime;
                _tiempoTickQuemadoActual -= Time.deltaTime;

                if (_tiempoTickQuemadoActual <= 0f)
                {
                    _tiempoTickQuemadoActual = tickQuemado;
                    int danoFinalQuemado = _danoQuemadoPersonalizado > 0 ? _danoQuemadoPersonalizado : danoQuemadoPorSegundo;
                    RecibirDano(danoFinalQuemado, new Color(1f, 0.72f, 0.20f, 1f)); // Quemadura: naranja clarito
                }

                if (_tiempoQuemado <= 0f)
                {
                    EstaQuemado.Value = false;
                    _danoQuemadoPersonalizado = 0;
                }
            }

            if (enAnimacionEntrada)
            {
                if (_rb != null) _rb.velocity = Vector2.zero;
                return;
            }

            // Ataque al jugador más cercano con cooldown
            if (_tiempoAtaqueActual > 0f)
            {
                _tiempoAtaqueActual -= Time.deltaTime;
            }

            Personaje jugadorCercano = ObtenerJugadorMasCercano();
            if (jugadorCercano != null && _tiempoAtaqueActual <= 0f)
            {
                float dist = Vector2.Distance(transform.position, jugadorCercano.transform.position);
                if (dist <= rangoAtaque)
                {
                    jugadorCercano.RecibirDano(dano);
                    Vector2 dirEmpuje = ((Vector2)jugadorCercano.transform.position - (Vector2)transform.position).normalized;
                    jugadorCercano.AplicarKnockback(dirEmpuje, 350f);

                    // Si el jugador tiene la Armadura de Fuego activa, quema al enemigo que lo golpeó a melee
                    if (jugadorCercano is PersonajeFuego pjFuego && pjFuego.ArmaduraActiva.Value)
                    {
                        AplicarQuemadura();
                        RecibirDano(18);
                        MostrarDanoClientRpc(18, new Color(1f, 0.45f, 0.1f));
                    }

                    _tiempoAtaqueActual = tiempoEntreAtaques;

                    // Al golpear, retrocede un momento para dar respiro
                    _modoActual = ModoMovimiento.Retroceder;
                    _timerDecision = 1.2f;
                    _direccionMovimiento = -dirEmpuje;
                    _factorVelocidad = 0.7f;
                }
            }
        }

        void FixedUpdate()
        {
            if (!IsServer || enAnimacionEntrada || EstaCongelado.Value || (GestorPartida.Instancia != null && GestorPartida.Instancia.PartidaTerminada))
            {
                if (_rb != null) _rb.velocity = Vector2.zero;
                return;
            }

            Personaje jugador = ObtenerJugadorMasCercano();
            if (jugador == null)
            {
                _direccionMovimiento = Vector2.zero;
                _factorVelocidad = 0f;
            }
            else
            {
                // Decisión probabilística de movimiento cada cierto tiempo
                _timerDecision -= Time.fixedDeltaTime;
                if (_timerDecision <= 0f)
                {
                    _timerDecision = UnityEngine.Random.Range(1.3f, 2.3f);
                    TomarDecisionProbabilistica(jugador);
                }
            }

            Vector2 movimiento = _direccionMovimiento * (_velocidadActual * _factorVelocidad);

            if (_knockback.sqrMagnitude > 0.01f)
            {
                _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, _knockbackFriccion * Time.fixedDeltaTime);
            }
            else
            {
                _knockback = Vector2.zero;
            }

            _rb.velocity = movimiento + _knockback;
        }

        private void TomarDecisionProbabilistica(Personaje jugador)
        {
            Vector2 posJugador = jugador.transform.position;
            Vector2 posPropia = transform.position;
            Vector2 dirHaciaJugador = (posJugador - posPropia).normalized;
            float distancia = Vector2.Distance(posPropia, posJugador);

            // Vector perpendicular para rodear / flanquear
            Vector2 dirPerpendicular = new Vector2(-dirHaciaJugador.y, dirHaciaJugador.x) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);

            float tirada = UnityEngine.Random.value;

            // Si está muy cerca (menos de 2.2m), tiende a retroceder o dudar más
            if (distancia < 2.2f)
            {
                if (tirada < 0.40f)
                {
                    _modoActual = ModoMovimiento.Retroceder;
                    _direccionMovimiento = -dirHaciaJugador;
                    _factorVelocidad = 0.75f;
                }
                else if (tirada < 0.70f)
                {
                    _modoActual = ModoMovimiento.Flanquear;
                    _direccionMovimiento = dirPerpendicular;
                    _factorVelocidad = 0.8f;
                }
                else
                {
                    _modoActual = ModoMovimiento.Dudar;
                    _direccionMovimiento = Vector2.zero;
                    _factorVelocidad = 0f;
                }
                return;
            }

            // A distancia normal:
            if (tirada < 0.50f)
            {
                // 50% Probabilidad: Acercarse al jugador tranquilamente
                _modoActual = ModoMovimiento.Perseguir;
                _direccionMovimiento = dirHaciaJugador;
                _factorVelocidad = 1.0f;
            }
            else if (tirada < 0.75f)
            {
                // 25% Probabilidad: Flanquear / Merodear en zigzag
                _modoActual = ModoMovimiento.Flanquear;
                _direccionMovimiento = (dirHaciaJugador * 0.4f + dirPerpendicular * 0.8f).normalized;
                _factorVelocidad = 0.85f;
            }
            else if (tirada < 0.90f)
            {
                // 15% Probabilidad: Dudar / Pausa (le da respiro y ventana de disparo al jugador)
                _modoActual = ModoMovimiento.Dudar;
                _direccionMovimiento = dirHaciaJugador * 0.2f;
                _factorVelocidad = 0.2f;
            }
            else
            {
                // 10% Probabilidad: Retroceder o cambiar ángulo
                _modoActual = ModoMovimiento.Retroceder;
                _direccionMovimiento = -dirHaciaJugador;
                _factorVelocidad = 0.6f;
            }
        }

        public virtual void RecibirDano(int cantidad)
        {
            RecibirDano(cantidad, Color.white);
        }

        public virtual void RecibirDano(int cantidad, Color colorDano)
        {
            if (!IsServer || VidaActual.Value <= 0 || enAnimacionEntrada) return;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0))
            {
                return;
            }

            VidaActual.Value = Mathf.Max(0, VidaActual.Value - cantidad);
            MostrarDanoClientRpc(cantidad, colorDano);

            if (VidaActual.Value <= 0)
            {
                Morir();
            }
        }

        [ClientRpc]
        private void MostrarDanoClientRpc(int cantidad, Color color)
        {
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.25f, 0), cantidad.ToString(), color);
        }

        public void AplicarKnockback(Vector2 direccion, float fuerza)
        {
            if (!IsServer) return;
            _knockback = direccion.normalized * (fuerza / 32f);
        }

        public void AplicarRalentizacion()
        {
            if (!IsServer || EstaCongelado.Value) return;
            _tiempoRalentizado = duracionRalentizacion;
            _velocidadActual = velocidadBase * 0.45f;
            EstaRalentizado.Value = true;
        }

        public void Freeze()
        {
            if (!IsServer) return;
            EstaCongelado.Value = true;
            _tiempoCongelado = duracionCongelacion;
            _rb.velocity = Vector2.zero;
        }

        public void AplicarQuemadura(int danoPorTick = -1)
        {
            if (!IsServer) return;
            EstaQuemado.Value = true;
            _tiempoQuemado = duracionQuemado;
            _tiempoTickQuemadoActual = tickQuemado;
            if (danoPorTick > 0)
            {
                _danoQuemadoPersonalizado = Mathf.Max(_danoQuemadoPersonalizado, danoPorTick);
            }
        }

        protected virtual void Morir()
        {
            if (!IsServer) return;
            AlMorir?.Invoke();

            // Otorgar oro de la run a los jugadores (reducido para no saturar de oro)
            int oroRecompensa = UnityEngine.Random.value < 0.65f ? 1 : 2;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var pj = Personaje.Todos[i];
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.AgregarOro(oroRecompensa);
                }
            }

            if (GestorPartida.Instancia != null)
            {
                GestorPartida.Instancia.EnemigoEliminado(this);
            }

            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }

        public void Detener()
        {
            enabled = false;
            if (_rb != null) _rb.velocity = Vector2.zero;
        }

        protected Personaje ObtenerJugadorMasCercano()
        {
            if (GestorPartida.Instancia != null && GestorPartida.Instancia.PartidaTerminada)
            {
                return null;
            }

            Personaje masCercano = null;
            float menorDist = rangoMaximoDeteccion; // No detecta jugadores fuera de su rango de visión

            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var j = Personaje.Todos[i];
                if (j == null || j.EstaMuerto.Value || !j.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(transform.position, j.transform.position);
                if (d <= menorDist)
                {
                    menorDist = d;
                    masCercano = j;
                }
            }

            return masCercano;
        }
    }
}
