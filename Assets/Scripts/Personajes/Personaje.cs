using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight
{
    /// <summary>
    /// Base compartida por todos los personajes jugables (Fuego, Hielo, ElÃ©ctrico).
    /// Replica fielmente Personaje.gd de Godot:
    /// - Vida, recibir daÃ±o, knockback y movimiento.
    /// - Host-autoritativo: el servidor mueve y calcula colisiones, el cliente envÃ­a inputs.
    /// - NetworkVariables para sincronizar vida y estados.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class Personaje : NetworkBehaviour
    {
        [Header("Estadísticas Base")]
        public int vidaMax = 100;
        public float velocidadBase = 12.5f; // ~400 px/s en Godot
        public float friccionKnockback = 25f; // ~800 px/s²

        public NetworkVariable<int> VidaActual = new NetworkVariable<int>(100);
        public NetworkVariable<int> VidaMaxSincronizada = new NetworkVariable<int>(100);
        public NetworkVariable<float> ReduccionDano = new NetworkVariable<float>(0f); // 0.0 a 1.0 (inmune)
        public NetworkVariable<bool> EstaMuerto = new NetworkVariable<bool>(false);
        public NetworkVariable<FixedString64Bytes> NombreJugadorSincronizado = new NetworkVariable<FixedString64Bytes>();

        public abstract TipoPersonaje TipoPj { get; }

        [Header("Sistemas de Progresión")]
        public NivelEnPartida nivelProgreso;
        public NetworkVariable<float> MultiplicadorDanoLoot = new NetworkVariable<float>(1.0f);
        public NetworkVariable<float> ReduccionDanoLoot = new NetworkVariable<float>(0f);
        public NetworkVariable<float> MultiplicadorVelocidadLoot = new NetworkVariable<float>(1.0f);
        public NetworkVariable<int> NivelMaestriaSincronizado = new NetworkVariable<int>(1);
        public NetworkVariable<int> OroRun = new NetworkVariable<int>(0);

        [Header("Efectos de Estado de Biomas")]
        public NetworkVariable<bool> JugadorCongelado = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> JugadorRalentizado = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> JugadorQuemado = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> JugadorEnvenenado = new NetworkVariable<bool>(false);

        private float _tiempoCongeladoPj = 0f;
        private float _tiempoRalentizadoPj = 0f;
        private float _tiempoQuemadoPj = 0f;
        private float _tiempoVenenoPj = 0f;
        private float _tickDanoEstadoPj = 0f;
        private int _danoTickQuemadoPj = 2;
        private int _danoTickVenenoPj = 2;

        protected Rigidbody2D _rb;
        protected Collider2D _col;
        protected Vector2 _knockback = Vector2.zero;
        protected float _velocidadActual;

        // Entradas enviadas por el dueño al servidor
        protected Vector2 _inputMovimiento = Vector2.zero;
        protected Vector2 _posicionMouse = Vector2.zero;
        protected bool _corriendo = false;

        public NetworkVariable<Vector2> DireccionApuntado = new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        public NetworkVariable<Vector2> MovimientoSincronizado = new NetworkVariable<Vector2>(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Vector3 _ultimaPosRemota;
        protected Vector2 _velocidadRemotaEstimada = Vector2.zero;
        private float _tiempoInmovilGato = 0f;
        private float _timerProximoRonroneo = 6.0f;

        public Vector2 ObtenerMovimientoEfectivo()
        {
            if (EsControlLocal)
            {
                if (_rb != null && _rb.velocity.sqrMagnitude > 0.05f) return _rb.velocity;
                return _inputMovimiento;
            }
            if (MovimientoSincronizado.Value.sqrMagnitude > 0.01f)
            {
                return MovimientoSincronizado.Value;
            }
            return _velocidadRemotaEstimada;
        }

        protected SpriteRenderer _spriteRenderer;

        public event Action AlMorir;
        public bool EsControlLocal => (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            ? (IsSpawned && IsOwner)
            : true;

        public string ObtenerNombreJugador()
        {
            if (EsControlLocal && Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
            {
                return Global.Instancia.NombreLocal.Trim();
            }
            if (!NombreJugadorSincronizado.Value.IsEmpty)
            {
                string syncNom = NombreJugadorSincronizado.Value.ToString();
                if (!string.IsNullOrWhiteSpace(syncNom)) return syncNom;
            }
            if (SalaRed.Instancia != null && IsSpawned)
            {
                string salaNom = SalaRed.Instancia.NombreDe(OwnerClientId);
                if (!string.IsNullOrWhiteSpace(salaNom)) return salaNom;
            }
            return "Jugador";
        }

        /// <summary>
        /// Multiplicador de Daño escalado por la Maestría permanente de la cuenta:
        /// - Empieza pegando menos a Nivel 1 (0.65x, -35% daño).
        /// - Escala +5% por nivel hasta 1.60x (+60% de daño permanente a Nivel 20).
        /// Aumenta el daño de TODOS los ataques básicos y habilidades de combate.
        /// </summary>
        public float MultiplicadorDanoMaestria
        {
            get
            {
                int nivelM = IsOwner ? GestorMaestria.ObtenerNivelMaestria(TipoPj) : NivelMaestriaSincronizado.Value;
                return Mathf.Clamp(0.65f + ((nivelM - 1) * 0.05f), 0.65f, 2.0f);
            }
        }

        public float MultiplicadorDanoTotal => MultiplicadorDanoMaestria * (nivelProgreso != null ? nivelProgreso.MultiplicadorDano : 1f) * MultiplicadorDanoLoot.Value;

        /// <summary>
        /// Comprueba las 3 capas estrictas de progresión:
        /// 1. Maestría (Permanente): Si no está desbloqueada por maestría, NO PUEDO USARLA EN LA RUN AUNQUE YA TENGA EL NIVEL DE RUN SUFICIENTE.
        /// 2. Loadout (Lobby): Necesito equiparla en el lobby para poder usarla en las runs.
        /// 3. Nivel Run (1-10): Una vez desbloqueada por maestría y equipada en el lobby, requiere el nivel de la partida.
        /// </summary>
        public bool PuedeUsarHabilidad(IdHabilidad id, out string motivoBloqueo)
        {
            motivoBloqueo = "";
            if (id == IdHabilidad.Ninguna) return false;

            var def = CatalogoHabilidades.Obtener(id);
            if (def == null) return true;

            // 1. Maestría (Requisito absoluto y permanente de cuenta)
            int nivelMaestriaActual = IsOwner ? GestorMaestria.ObtenerNivelMaestria(TipoPj) : NivelMaestriaSincronizado.Value;
            if (nivelMaestriaActual < def.nivelMaestriaRequerido)
            {
                motivoBloqueo = $"🔒 Bloqueada: Requiere Maestría Nivel {def.nivelMaestriaRequerido}";
                return false;
            }

            // 2. Loadout elegido y equipado en el Lobby
            var loadout = GestorLoadout.ObtenerLoadout(TipoPj);
            if (!loadout.Contains(id))
            {
                motivoBloqueo = "⚠️ No equipada (Debes equiparla en el Lobby)";
                return false;
            }

            // 3. Nivel de la run actual
            int nivelRun = nivelProgreso != null ? nivelProgreso.NivelActual.Value : 1;
            if (nivelRun < def.nivelRequeridoRun)
            {
                motivoBloqueo = $"¡Requiere Nivel {def.nivelRequeridoRun} en la run! (Actual: {nivelRun})";
                return false;
            }

            return true;
        }

        public bool PuedeUsarHabilidad(IdHabilidad id) => PuedeUsarHabilidad(id, out _);

        private float _tiempoUltimoAvisoBloqueo;

        /// <summary>
        /// Emite un aviso cuando una habilidad no puede usarse, exactamente 1 vez por pulsación (evita spam al mantener presionada la tecla).
        /// </summary>
        public void NotificarHabilidadBloqueada(string motivo, Color color, bool fuePresionada)
        {
            if (string.IsNullOrEmpty(motivo)) return;
            if (fuePresionada && Time.time - _tiempoUltimoAvisoBloqueo > 0.5f)
            {
                _tiempoUltimoAvisoBloqueo = Time.time;
                TextoFlotante.Crear(transform.position + Vector3.up * 1.2f, motivo, color);
            }
        }

        public static readonly System.Collections.Generic.List<Personaje> Todos = new System.Collections.Generic.List<Personaje>();

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
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _velocidadActual = velocidadBase;
            nivelProgreso = GetComponent<NivelEnPartida>() ?? gameObject.AddComponent<NivelEnPartida>();

            // Asignar material sin fricción y detección continua para evitar atascarse en paredes o atravesarlas
            PhysicsMaterial2D matSinFriccion = new PhysicsMaterial2D("SinFriccionJugador") { friction = 0f, bounciness = 0f };
            if (_rb != null)
            {
                _rb.sharedMaterial = matSinFriccion;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            if (_col != null)
            {
                _col.sharedMaterial = matSinFriccion;
            }

            // Calibrar hitbox precisa y compacta en los pies para evitar atascos con paredes, esquinas y puertas
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(0.60f, 0.65f);
                box.offset = new Vector2(0f, -0.22f);
                box.sharedMaterial = matSinFriccion;
            }
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = 0.32f;
                circle.offset = new Vector2(0f, -0.22f);
                circle.sharedMaterial = matSinFriccion;
            }
        }

        private readonly Dictionary<TipoMejoraTienda, int> _nivelesMejorasTienda = new Dictionary<TipoMejoraTienda, int>();

        public int ObtenerNivelMejora(TipoMejoraTienda tipo)
        {
            return _nivelesMejorasTienda.TryGetValue(tipo, out int nv) ? nv : 0;
        }

        public void EstablecerNivelMejora(TipoMejoraTienda tipo, int nivel)
        {
            _nivelesMejorasTienda[tipo] = nivel;
        }

        public float FactorReduccionCooldown => Mathf.Clamp(1f - (ObtenerNivelMejora(TipoMejoraTienda.CooldownUniversal) * 0.07f), 0.50f, 1.0f);
        public float FactorReduccionCooldownUlti => Mathf.Clamp(FactorReduccionCooldown - (ObtenerNivelMejora(TipoMejoraTienda.CooldownDefinitiva) * 0.10f), 0.35f, 1.0f);
        public float MultiplicadorAreaHabilidades => 1.0f + (ObtenerNivelMejora(TipoMejoraTienda.AreaHabilidades) * 0.12f);
        public float ProbabilidadCritico => ObtenerNivelMejora(TipoMejoraTienda.GolpeCritico) * 0.08f;

        public int AplicarCriticoSiProcede(int danoBase, out bool esCritico)
        {
            if (UnityEngine.Random.value < ProbabilidadCritico)
            {
                esCritico = true;
                return Mathf.RoundToInt(danoBase * 1.8f);
            }
            esCritico = false;
            return danoBase;
        }

        protected virtual void Start()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Controles.Bloqueado = false;
                if (CamaraSeguir.Instancia != null)
                {
                    CamaraSeguir.Instancia.objetivo = transform;
                }
                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ConectarPersonaje(this);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            _ultimaPosRemota = transform.position;

            if (IsServer)
            {
                VidaActual.Value = vidaMax;
                VidaMaxSincronizada.Value = vidaMax;
                EstaMuerto.Value = false;
                MultiplicadorDanoLoot.Value = 1.0f;
                ReduccionDanoLoot.Value = 0f;
                MultiplicadorVelocidadLoot.Value = 1.0f;

                if (NombreJugadorSincronizado.Value.IsEmpty && SalaRed.Instancia != null)
                {
                    string nomSala = SalaRed.Instancia.NombreDe(OwnerClientId);
                    if (!string.IsNullOrWhiteSpace(nomSala))
                    {
                        NombreJugadorSincronizado.Value = new FixedString64Bytes(nomSala);
                    }
                }
            }

            VidaActual.OnValueChanged += AlCambiarVida;
            VidaMaxSincronizada.OnValueChanged += (anterior, actual) =>
            {
                vidaMax = actual;
                if (EsControlLocal && HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ActualizarVida(VidaActual.Value, actual);
                }
            };
            EstaMuerto.OnValueChanged += AlCambiarEstadoMuerte;
            OroRun.OnValueChanged += (anterior, actual) =>
            {
                if (actual > anterior && (IsOwner || !NetworkManager.IsListening))
                {
                    GestorAudio.PlaySFX(TipoSFX.RecogerOro, 1.0f);
                }
            };

            if (!IsOwner && NetworkManager != null && NetworkManager.IsListening)
            {
                if (_rb != null) _rb.isKinematic = true;
            }
            else
            {
                if (_rb != null) _rb.isKinematic = false;
            }

            if (IsOwner)
            {
                Controles.Bloqueado = false;

                // Asignar cámara local al spawnear
                if (CamaraSeguir.Instancia != null)
                {
                    CamaraSeguir.Instancia.objetivo = transform;
                }

                // Configurar HUD local
                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ConectarPersonaje(this);
                }

                // Sincronizar nombre local y maestría persistente de la cuenta local con el servidor y la sala
                string miNombreLocal = (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
                    ? Global.Instancia.NombreLocal.Trim()
                    : "Jugador";
                int miMaestria = GestorMaestria.ObtenerNivelMaestria(TipoPj);
                if (NetworkManager != null && NetworkManager.IsListening)
                {
                    SincronizarNombrePersonajeServerRpc(miNombreLocal);
                    SincronizarMaestriaServerRpc(miMaestria);
                }
                else
                {
                    NombreJugadorSincronizado.Value = new FixedString64Bytes(miNombreLocal);
                    NivelMaestriaSincronizado.Value = miMaestria;
                }

                GestorMaestria.AlGanarMaestria += AlCambiarMaestriaLocal;
            }
        }

        [ServerRpc]
        private void SincronizarNombrePersonajeServerRpc(string nombreJugador)
        {
            string limpio = string.IsNullOrWhiteSpace(nombreJugador) ? "Jugador" : nombreJugador.Trim();
            if (limpio.Length > 30) limpio = limpio.Substring(0, 30);
            var fs = new FixedString64Bytes(limpio);
            if (NombreJugadorSincronizado.Value != fs)
            {
                NombreJugadorSincronizado.Value = fs;
            }
        }

        public override void OnNetworkDespawn()
        {
            VidaActual.OnValueChanged -= AlCambiarVida;
            EstaMuerto.OnValueChanged -= AlCambiarEstadoMuerte;
            GestorMaestria.AlGanarMaestria -= AlCambiarMaestriaLocal;
        }

        private void AlCambiarMaestriaLocal(TipoPersonaje tipo, int totalXP)
        {
            if (tipo == TipoPj && IsOwner && IsSpawned)
            {
                int nuevoNivel = GestorMaestria.ObtenerNivelMaestria(tipo);
                if (NetworkManager != null && NetworkManager.IsListening)
                {
                    SincronizarMaestriaServerRpc(nuevoNivel);
                }
            }
        }

        [ServerRpc]
        private void SincronizarMaestriaServerRpc(int nivelMaestria)
        {
            NivelMaestriaSincronizado.Value = nivelMaestria;
        }

        protected virtual void Update()
        {
            if (EstaMuerto.Value) return;

            if (IsServer)
            {
                ActualizarEfectosConsumiblesServidor(Time.deltaTime);
                ActualizarRegeneracionPasivaServidor(Time.deltaTime);
            }

            if (ComandosAdmin.ModoCeroCooldowns)
            {
                ReiniciarCooldowns();
            }

            bool esControlLocal = EsControlLocal;
            if (esControlLocal)
            {
                // Leer inputs locales y procesar movimiento instantáneamente
                Vector2 mov = Controles.LeerMovimiento();
                Vector2 mouse = Controles.PosicionMouseMundo();
                bool run = Controles.Correr;

                _inputMovimiento = mov;
                _posicionMouse = mouse;
                _corriendo = run;

                if (NetworkManager != null && NetworkManager.IsListening && IsSpawned)
                {
                    if ((MovimientoSincronizado.Value - mov).sqrMagnitude > 0.001f)
                    {
                        MovimientoSincronizado.Value = mov;
                    }
                }

                Vector2 dirLocal = (mouse - (Vector2)transform.position).normalized;
                if (dirLocal.sqrMagnitude > 0.001f)
                {
                    ActualizarVisualMirada(dirLocal);
                    if (NetworkManager != null && NetworkManager.IsListening && IsSpawned)
                    {
                        DireccionApuntado.Value = dirLocal;
                    }
                }

                // En el Lobby pacífico o pantallas no se procesan inputs de combate ni habilidades
                bool enMazmorraCombate = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Escenas.Juego && ControladorLobby.Instancia == null;
                if (enMazmorraCombate)
                {
                    ProcesarInputsHabilidadesLocales();
                }
                ProcesarRevivirAliadosLocales();

                // Ronroneo periódico del gato del jugador mientras esté quieto
                bool estaInmovil = mov.sqrMagnitude < 0.001f && (_rb == null || _rb.velocity.sqrMagnitude < 0.05f);
                if (estaInmovil)
                {
                    _tiempoInmovilGato += Time.deltaTime;
                    if (_tiempoInmovilGato >= 2.0f) // Tras 2 segundos quieto
                    {
                        _timerProximoRonroneo -= Time.deltaTime;
                        if (_timerProximoRonroneo <= 0f)
                        {
                            _timerProximoRonroneo = UnityEngine.Random.Range(7f, 13f);
                            GestorAudio.PlayRonroneoGato(0.40f);
                        }
                    }
                }
                else
                {
                    _tiempoInmovilGato = 0f;
                    _timerProximoRonroneo = UnityEngine.Random.Range(5f, 9f);
                }
            }
            else
            {
                // Calcular velocidad remota real a partir del cambio de posición por si hay interpolación de red
                if (Time.deltaTime > 0.0001f)
                {
                    Vector2 deltaPos = (Vector2)(transform.position - _ultimaPosRemota);
                    Vector2 velInst = deltaPos / Time.deltaTime;
                    _velocidadRemotaEstimada = Vector2.Lerp(_velocidadRemotaEstimada, velInst, Time.deltaTime * 18f);
                    if (_velocidadRemotaEstimada.sqrMagnitude < 0.08f) _velocidadRemotaEstimada = Vector2.zero;
                }
                _ultimaPosRemota = transform.position;

                // En clientes remotos, actualizar orientación visual según movimiento y DireccionApuntado sincronizada
                Vector2 dirRemoto = DireccionApuntado.Value;
                if (dirRemoto.sqrMagnitude < 0.001f) dirRemoto = Vector2.down;
                ActualizarVisualMirada(dirRemoto);
            }
        }

        protected virtual void ActualizarVisualMirada(Vector2 dir)
        {
            bool mirarIzquierda = dir.x < 0;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = mirarIzquierda;
            }
            else
            {
                Vector3 s = transform.localScale;
                s.x = mirarIzquierda ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
                transform.localScale = s;
            }
        }

        protected virtual void FixedUpdate()
        {
            bool esControlLocal = IsOwner || NetworkManager == null || !NetworkManager.IsListening;
            if (!esControlLocal) return;
            if (EstaMuerto.Value)
            {
                if (_rb != null)
                {
                    _rb.velocity = Vector2.zero;
                    _rb.angularVelocity = 0f;
                }
                _knockback = Vector2.zero;
                return;
            }

            // Manejo físico del movimiento y knockback inmediatamente en la máquina local (0ms delay)
            float vel = (_corriendo ? ObtenerVelocidadCorrer() : _velocidadActual) * MultiplicadorVelocidadLoot.Value;
            if (JugadorCongelado.Value)
            {
                vel = 0f;
            }
            else if (JugadorRalentizado.Value)
            {
                vel *= 0.50f;
            }
            Vector2 movimiento = _inputMovimiento.normalized * vel;

            if (_knockback.sqrMagnitude > 0.01f)
            {
                _knockback = Vector2.MoveTowards(_knockback, Vector2.zero, friccionKnockback * Time.fixedDeltaTime);
            }
            else
            {
                _knockback = Vector2.zero;
            }

            _rb.velocity = movimiento + _knockback;
        }

        protected virtual float ObtenerVelocidadCorrer() => velocidadBase * 1.5f;

        [ServerRpc]
        private void EnviarInputServerRpc(Vector2 mov, Vector2 mouse, bool correr)
        {
        }

        private float _ultimoTiempoGolpeRecibido = -10f;
        public float ventanaInvulnerabilidadGolpe = 0.42f;

        public virtual void RecibirDano(int cantidad)
        {
            if (!IsServer || EstaMuerto.Value) return;
            if (GestorPartida.Instancia != null && GestorPartida.Instancia.PartidaTerminada) return;

            // Ventana breve de invulnerabilidad tras recibir golpe para evitar one-shots por múltiples proyectiles superpuestos
            if (Time.time - _ultimoTiempoGolpeRecibido < ventanaInvulnerabilidadGolpe) return;
            _ultimoTiempoGolpeRecibido = Time.time;

            float reduccionTotal = Mathf.Clamp01(ReduccionDano.Value + ReduccionDanoLoot.Value);
            int danoFinal = Mathf.Max(1, Mathf.RoundToInt(cantidad * (1.0f - reduccionTotal)));
            // Tope de seguridad por golpe individual (máx 35% de la vida máxima) para que ningún golpe pueda oneshotear
            danoFinal = Mathf.Min(danoFinal, Mathf.Max(15, Mathf.RoundToInt(vidaMax * 0.35f)));

            VidaActual.Value = Mathf.Max(0, VidaActual.Value - danoFinal);

            MostrarDanoClientRpc(danoFinal, new Color(1f, 0.22f, 0.22f, 1f)); // Daño de enemigos siempre rojo

            if (VidaActual.Value <= 0)
            {
                Morir();
            }
        }

        /// <summary>
        /// Pipeline de impacto reportado por el cliente dueño con validación autoritativa en el host.
        /// El cliente ve el impacto en su pantalla con 0ms de retraso y el host valida plausibilidad
        /// (distancia, línea de visión y daño máximo) antes de aplicar la vida y estado.
        /// </summary>
        public void NotificarImpactoProyectil(EnemigoBase enemigo, Vector2 puntoImpacto, int dano, bool quemadura, int bonoQuem)
        {
            if (enemigo == null) return;

            if (IsServer)
            {
                // Host atacando: aplica directamente
                ProcesarImpactoEnemigoServidor(enemigo, puntoImpacto, dano, quemadura, bonoQuem);
            }
            else if (IsOwner)
            {
                // Cliente dueño: reporta al host mediante ServerRpc confiable
                ReportarImpactoProyectilServerRpc(enemigo.NetworkObjectId, puntoImpacto, dano, quemadura, bonoQuem);
            }
        }

        [ServerRpc]
        private void ReportarImpactoProyectilServerRpc(ulong enemigoNetId, Vector2 puntoImpacto, int dano, bool quemadura, int bonoQuem, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

            // 1. Validar que el enemigo exista y esté vivo en el servidor
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemigoNetId, out var netObj)) return;
            var enemigo = netObj.GetComponent<EnemigoBase>();
            if (enemigo == null || enemigo.VidaActual.Value <= 0) return;

            // 2. Validación de plausibilidad por latencia (tolerancia de 3.2 unidades por interpolación)
            float distEnemigo = Vector2.Distance(enemigo.transform.position, puntoImpacto);
            if (distEnemigo > 3.2f)
            {
                puntoImpacto = enemigo.transform.position;
            }

            // 3. Validación de límite de daño plausible para evitar inyección de daño
            int maxDanoPlausible = Mathf.RoundToInt(300 * MultiplicadorDanoTotal);
            int danoSeguro = Mathf.Clamp(dano, 1, maxDanoPlausible);

            // 4. Aplicar daño autoritativo en el host
            ProcesarImpactoEnemigoServidor(enemigo, puntoImpacto, danoSeguro, quemadura, bonoQuem);
        }

        private void ProcesarImpactoEnemigoServidor(EnemigoBase enemigo, Vector2 puntoImpacto, int dano, bool quemadura, int bonoQuem)
        {
            if (enemigo == null || enemigo.VidaActual.Value <= 0) return;

            Color colImpacto = quemadura ? new Color(0.96f, 0.40f, 0.05f) : Color.white;
            enemigo.RecibirDano(dano, colImpacto);

            if (quemadura)
            {
                int danoTickQuemadura = Mathf.Max(12 + bonoQuem, Mathf.RoundToInt(dano * 0.45f) + bonoQuem);
                enemigo.AplicarQuemadura(danoTickQuemadura);
            }

            Vector2 dirEmpuje = ((Vector2)enemigo.transform.position - (Vector2)transform.position).normalized;
            if (dirEmpuje.sqrMagnitude > 0.001f)
            {
                enemigo.AplicarKnockback(dirEmpuje, 260f);
            }
        }

        public const float DURACION_EFECTO_CONSUMIBLE = 22f;
        private float _timerEfectoDano = 0f;
        private float _timerEfectoDefensa = 0f;
        private float _timerEfectoVelocidad = 0f;
        private float _timerEfectoVidaMax = 0f;
        private int _bonoVidaMaxActivo = 0;

        private void ActualizarEfectosConsumiblesServidor(float dt)
        {
            if (_timerEfectoDano > 0f)
            {
                _timerEfectoDano -= dt;
                if (_timerEfectoDano <= 0f)
                {
                    _timerEfectoDano = 0f;
                    MultiplicadorDanoLoot.Value = 1.0f;
                }
            }

            if (_timerEfectoDefensa > 0f)
            {
                _timerEfectoDefensa -= dt;
                if (_timerEfectoDefensa <= 0f)
                {
                    _timerEfectoDefensa = 0f;
                    ReduccionDanoLoot.Value = 0f;
                }
            }

            if (_timerEfectoVelocidad > 0f)
            {
                _timerEfectoVelocidad -= dt;
                if (_timerEfectoVelocidad <= 0f)
                {
                    _timerEfectoVelocidad = 0f;
                    MultiplicadorVelocidadLoot.Value = 1.0f;
                }
            }

            if (_timerEfectoVidaMax > 0f)
            {
                _timerEfectoVidaMax -= dt;
                if (_timerEfectoVidaMax <= 0f)
                {
                    _timerEfectoVidaMax = 0f;
                    if (_bonoVidaMaxActivo > 0)
                    {
                        vidaMax = Mathf.Max(100, vidaMax - _bonoVidaMaxActivo);
                        VidaMaxSincronizada.Value = vidaMax;
                        _bonoVidaMaxActivo = 0;
                        if (VidaActual.Value > vidaMax)
                        {
                            VidaActual.Value = vidaMax;
                        }
                    }
                }
            }

            // Actualizar estados alterados de biomas en el servidor
            if (_tiempoCongeladoPj > 0f)
            {
                _tiempoCongeladoPj -= dt;
                if (_tiempoCongeladoPj <= 0f)
                {
                    _tiempoCongeladoPj = 0f;
                    JugadorCongelado.Value = false;
                }
            }

            if (_tiempoRalentizadoPj > 0f)
            {
                _tiempoRalentizadoPj -= dt;
                if (_tiempoRalentizadoPj <= 0f)
                {
                    _tiempoRalentizadoPj = 0f;
                    JugadorRalentizado.Value = false;
                }
            }

            if (_tiempoQuemadoPj > 0f)
            {
                _tiempoQuemadoPj -= dt;
                _tickDanoEstadoPj += dt;
                if (_tickDanoEstadoPj >= 0.5f)
                {
                    _tickDanoEstadoPj = 0f;
                    RecibirDanoPorEstado(_danoTickQuemadoPj, new Color(1f, 0.45f, 0.1f));
                }
                if (_tiempoQuemadoPj <= 0f)
                {
                    _tiempoQuemadoPj = 0f;
                    JugadorQuemado.Value = false;
                }
            }

            if (_tiempoVenenoPj > 0f)
            {
                _tiempoVenenoPj -= dt;
                _tickDanoEstadoPj += dt;
                if (_tickDanoEstadoPj >= 0.5f)
                {
                    _tickDanoEstadoPj = 0f;
                    RecibirDanoPorEstado(_danoTickVenenoPj, new Color(0.35f, 0.95f, 0.35f));
                }
                if (_tiempoVenenoPj <= 0f)
                {
                    _tiempoVenenoPj = 0f;
                    JugadorEnvenenado.Value = false;
                }
            }
        }

        private void RecibirDanoPorEstado(int cantidad, Color color)
        {
            if (!IsServer || EstaMuerto.Value) return;
            VidaActual.Value = Mathf.Max(0, VidaActual.Value - cantidad);
            MostrarDanoClientRpc(cantidad, color);
            if (VidaActual.Value <= 0)
            {
                Morir();
            }
        }

        public void AplicarCongelacion(float duracion)
        {
            if (!IsServer || EstaMuerto.Value) return;
            _tiempoCongeladoPj = Mathf.Max(_tiempoCongeladoPj, duracion);
            JugadorCongelado.Value = true;
            MostrarTextoFlotanteClientRpc("¡CONGELADO!", new Color(0.4f, 0.8f, 1f));
        }

        public void AplicarRalentizacion(float duracion)
        {
            if (!IsServer || EstaMuerto.Value || JugadorCongelado.Value) return;
            _tiempoRalentizadoPj = Mathf.Max(_tiempoRalentizadoPj, duracion);
            JugadorRalentizado.Value = true;
            MostrarTextoFlotanteClientRpc("¡Ralentizado!", new Color(0.6f, 0.9f, 1f));
        }

        private float _timerRegenPasiva = 0f;

        private void ActualizarRegeneracionPasivaServidor(float dt)
        {
            if (!IsServer || EstaMuerto.Value) return;
            if (SceneManager.GetActiveScene().name != "Juego") return;

            if (VidaActual.Value < VidaMaxSincronizada.Value)
            {
                _timerRegenPasiva += dt;
                if (_timerRegenPasiva >= 4.0f)
                {
                    _timerRegenPasiva = 0f;
                    Curar(1);
                }
            }
            else
            {
                _timerRegenPasiva = 0f;
            }
        }

        public void AplicarQuemadura(float duracion, int danoPorTick = 2)
        {
            if (!IsServer || EstaMuerto.Value) return;
            _tiempoQuemadoPj = Mathf.Max(_tiempoQuemadoPj, duracion);
            _danoTickQuemadoPj = Mathf.Min(2, danoPorTick);
            JugadorQuemado.Value = true;
            MostrarTextoFlotanteClientRpc("¡En Llamas!", new Color(1f, 0.4f, 0.1f));
        }

        public void AplicarEsporasVeneno(float duracion, int danoPorTick = 2)
        {
            if (!IsServer || EstaMuerto.Value) return;
            _tiempoVenenoPj = Mathf.Max(_tiempoVenenoPj, duracion);
            _danoTickVenenoPj = Mathf.Min(2, danoPorTick);
            JugadorEnvenenado.Value = true;
            MostrarTextoFlotanteClientRpc("¡Esporas Tóxicas!", new Color(0.4f, 0.9f, 0.3f));
        }

        public void PerderMejorasTemporalesPorMuerteEnLoop()
        {
            if (!IsServer) return;
            MultiplicadorDanoLoot.Value = 1.0f;
            ReduccionDanoLoot.Value = 0f;
            MultiplicadorVelocidadLoot.Value = 1.0f;
            MostrarTextoFlotanteClientRpc("⚠️ Mejoras de la run perdidas por muerte en el Bucle", Color.red);
        }

        public void AgregarOro(int cantidad)
        {
            if (!IsServer || cantidad <= 0) return;
            OroRun.Value += cantidad;
            MostrarTextoFlotanteClientRpc($"+{cantidad} Oro 💰", new Color(1f, 0.88f, 0.2f));
        }

        public void ReclamarMonedaLocalmente(int idMoneda, int valor)
        {
            if (MonedaOro.Todas.TryGetValue(idMoneda, out var mon) && mon != null)
            {
                mon.DesaparecerPorRecoleccion();
            }

            if (NetworkManager != null && NetworkManager.IsListening)
            {
                ReclamarMonedaServerRpc(idMoneda, valor);
            }
            else
            {
                AgregarOro(valor);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReclamarMonedaServerRpc(int idMoneda, int valor, ServerRpcParams rpcParams = default)
        {
            if (EstaMuerto.Value) return;
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.ReclamarMonedaEnServidor(rpcParams.Receive.SenderClientId, idMoneda, valor);
            }
            else
            {
                AgregarOro(valor);
            }
        }

        public void InteractuarCofre(CofreOro cofre)
        {
            if (cofre == null || cofre.EstaAbiertoLocal) return;
            cofre.AbrirPuzzle(this);
        }

        public void InteractuarCofreDirecto(CofreOro cofre)
        {
            if (cofre == null || cofre.EstaAbiertoLocal) return;

            if (NetworkManager != null && NetworkManager.IsListening)
            {
                AbrirCofreCercanoServerRpc(cofre.transform.position);
            }
            else
            {
                cofre.AbrirLocalmente(this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbrirCofreCercanoServerRpc(Vector2 posCofre, ServerRpcParams rpcParams = default)
        {
            if (EstaMuerto.Value) return;

            float menorDist = 4.5f;
            CofreOro cofreCercano = null;
            for (int i = 0; i < CofreOro.Todos.Count; i++)
            {
                var c = CofreOro.Todos[i];
                if (c != null && !c.EstaAbiertoLocal)
                {
                    float d = Vector2.Distance(posCofre, c.transform.position);
                    if (d < menorDist)
                    {
                        menorDist = d;
                        cofreCercano = c;
                    }
                }
            }

            if (cofreCercano != null)
            {
                cofreCercano.AbrirPorServidor();
                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.NotificarAperturaCofreClientRpc(posCofre);
                }
            }
        }

        public bool GastarOro(int cantidad)
        {
            if (!IsServer || cantidad <= 0) return false;
            if (OroRun.Value >= cantidad)
            {
                OroRun.Value -= cantidad;
                MostrarTextoFlotanteClientRpc($"-{cantidad} Oro 💰", new Color(1f, 0.5f, 0.2f));
                return true;
            }
            return false;
        }

        [ServerRpc]
        public void ComprarMejoraTiendaServerRpc(byte tipoMejoraByte, int costo)
        {
            if (EstaMuerto.Value) return;
            TipoMejoraTienda tipo = (TipoMejoraTienda)tipoMejoraByte;
            int nvActual = ObtenerNivelMejora(tipo);
            if (tipo != TipoMejoraTienda.PescadoAsado && nvActual >= 5) return;

            if (GastarOro(costo))
            {
                int nuevoNv = nvActual + 1;
                EstablecerNivelMejora(tipo, nuevoNv);
                AplicarEfectoMejoraTienda(tipo, nuevoNv);
                SincronizarMejoraTiendaClientRpc(tipoMejoraByte, nuevoNv);
            }
        }

        [ClientRpc]
        private void SincronizarMejoraTiendaClientRpc(byte tipoMejoraByte, int nuevoNv)
        {
            TipoMejoraTienda tipo = (TipoMejoraTienda)tipoMejoraByte;
            EstablecerNivelMejora(tipo, nuevoNv);
            if (IsOwner)
            {
                if (tipo == TipoMejoraTienda.PescadoAsado)
                {
                    GestorAudio.PlaySFX(TipoSFX.ComerPescado);
                }
                else if (HUDJugador.Instancia == null || !HUDJugador.Instancia.TiendaAbierta)
                {
                    GestorAudio.PlayCachinCompra();
                }
            }
            if (HUDJugador.Instancia != null && HUDJugador.Instancia.TiendaAbierta)
            {
                HUDJugador.Instancia.ActualizarItemsTiendaUI();
            }
        }

        public bool SolicitarCompraTienda(TipoMejoraTienda tipo, int costo)
        {
            if (OroRun.Value < costo) return false;
            int nvActual = ObtenerNivelMejora(tipo);
            if (tipo != TipoMejoraTienda.PescadoAsado && nvActual >= 5) return false;

            if (NetworkManager != null && NetworkManager.IsListening)
            {
                ComprarMejoraTiendaServerRpc((byte)tipo, costo);
            }
            else
            {
                if (GastarOro(costo))
                {
                    int nuevoNv = nvActual + 1;
                    EstablecerNivelMejora(tipo, nuevoNv);
                    AplicarEfectoMejoraTienda(tipo, nuevoNv);
                    if (HUDJugador.Instancia != null && HUDJugador.Instancia.TiendaAbierta)
                    {
                        HUDJugador.Instancia.ActualizarItemsTiendaUI();
                    }
                }
            }
            return true;
        }

        public virtual void AplicarEfectoMejoraTienda(TipoMejoraTienda tipo, int nivel)
        {
            if (!IsServer) return;

            switch (tipo)
            {
                case TipoMejoraTienda.VidaMaxima:
                    const int TOPE_MAXIMO_VIDA = 300;
                    if (vidaMax >= TOPE_MAXIMO_VIDA)
                    {
                        MostrarTextoFlotanteClientRpc("¡Vida Máxima al Límite (300 HP)!", Color.yellow);
                        break;
                    }
                    int nuevaVidaMax = Mathf.Min(TOPE_MAXIMO_VIDA, vidaMax + 30);
                    int inc = nuevaVidaMax - vidaMax;
                    vidaMax = nuevaVidaMax;
                    VidaMaxSincronizada.Value = vidaMax;
                    Curar(inc);
                    MostrarTextoFlotanteClientRpc($"¡+{inc} Vida Máxima! ({vidaMax}/{TOPE_MAXIMO_VIDA} HP)", new Color(1f, 0.25f, 0.45f));
                    break;

                case TipoMejoraTienda.PescadoAsado:
                    Curar(60);
                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        NotificarComerPescadoClientRpc();
                    }
                    else
                    {
                        GestorAudio.PlaySFX(TipoSFX.ComerPescado);
                    }
                    break;

                case TipoMejoraTienda.DanoUniversal:
                    MultiplicadorDanoLoot.Value = Mathf.Clamp(MultiplicadorDanoLoot.Value + 0.12f, 1.0f, 2.0f);
                    MostrarTextoFlotanteClientRpc($"¡+12% Daño General! (Nv {nivel}/5)", new Color(1f, 0.45f, 0.2f));
                    break;

                case TipoMejoraTienda.CooldownUniversal:
                    MostrarTextoFlotanteClientRpc($"¡-7% Tiempos de Cooldown! (Nv {nivel}/5)", new Color(0.35f, 0.85f, 1f));
                    break;
                case TipoMejoraTienda.AreaHabilidades:
                    MostrarTextoFlotanteClientRpc($"¡+12% Tamaño/Área de Habilidades! (Nv {nivel}/5)", new Color(0.6f, 1f, 0.5f));
                    break;
                case TipoMejoraTienda.CooldownDefinitiva:
                    MostrarTextoFlotanteClientRpc($"¡-10% Recarga de Ulti! (Nv {nivel}/5)", new Color(1f, 0.85f, 0.3f));
                    break;
                case TipoMejoraTienda.GolpeCritico:
                    MostrarTextoFlotanteClientRpc($"¡+8% Probabilidad Crítica (1.8x)! (Nv {nivel}/5)", new Color(1f, 0.3f, 0.3f));
                    break;

                case TipoMejoraTienda.Fuego_DanoQuemadura:
                    MostrarTextoFlotanteClientRpc($"¡+6 Daño por Quemadura! (Nv {nivel}/5)", new Color(1f, 0.5f, 0.1f));
                    break;
                case TipoMejoraTienda.Fuego_Area:
                    MostrarTextoFlotanteClientRpc($"¡+15% Área Ígnea! (Nv {nivel}/5)", new Color(1f, 0.6f, 0.2f));
                    break;
                case TipoMejoraTienda.Fuego_Duracion:
                    MostrarTextoFlotanteClientRpc($"¡+1.0s Fuego Prolongado! (Nv {nivel}/5)", new Color(1f, 0.35f, 0.1f));
                    break;
                case TipoMejoraTienda.Fuego_SupernovaGigante:
                    MostrarTextoFlotanteClientRpc($"¡+25% Radio Supernova Titánica! (Nv {nivel}/5)", new Color(1f, 0.4f, 0.05f));
                    break;

                case TipoMejoraTienda.Hielo_DuracionCongelacion:
                    MostrarTextoFlotanteClientRpc($"¡+0.6s Cero Absoluto! (Nv {nivel}/5)", new Color(0.4f, 0.85f, 1f));
                    break;
                case TipoMejoraTienda.Hielo_AreaVentisca:
                    MostrarTextoFlotanteClientRpc($"¡+15% Ventisca Ampliada! (Nv {nivel}/5)", new Color(0.6f, 0.95f, 1f));
                    break;
                case TipoMejoraTienda.Hielo_PotenciaGlacial:
                    MostrarTextoFlotanteClientRpc($"¡+12% Fragmentación Glacial! (Nv {nivel}/5)", new Color(0.7f, 0.9f, 1f));
                    break;
                case TipoMejoraTienda.Hielo_TormentaEterna:
                    MostrarTextoFlotanteClientRpc($"¡+20% Ventisca Eterna (+1.5s)! (Nv {nivel}/5)", new Color(0.5f, 0.95f, 1f));
                    break;

                case TipoMejoraTienda.Elec_Voltaje:
                    MostrarTextoFlotanteClientRpc($"¡+15% Alto Voltaje! (Nv {nivel}/5)", new Color(1f, 0.95f, 0.25f));
                    break;
                case TipoMejoraTienda.Elec_AreaTeleport:
                    MostrarTextoFlotanteClientRpc($"¡+20% Área Teleport! (Nv {nivel}/5)", new Color(1f, 1f, 0.4f));
                    break;
                case TipoMejoraTienda.Elec_CooldownDash:
                    MostrarTextoFlotanteClientRpc($"¡-0.3s Recarga Dash! (Nv {nivel}/5)", new Color(1f, 0.9f, 0.3f));
                    break;
                case TipoMejoraTienda.Elec_RayoCataclismico:
                    MostrarTextoFlotanteClientRpc($"¡+30% Rayo Cataclísmico! (Nv {nivel}/5)", new Color(1f, 1f, 0.5f));
                    break;
            }
        }

        public virtual void AplicarMejoraRun(TipoMejoraRun mejora)
        {
            if (!IsServer) return;
            var def = CatalogoMejorasRun.Obtener(mejora);
            if (def == null) return;

            switch (mejora)
            {
                case TipoMejoraRun.PescadoEsqueleto:
                case TipoMejoraRun.PescadoMedio:
                case TipoMejoraRun.PescadoCompleto:
                    Curar(Mathf.RoundToInt(def.valor));
                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        NotificarComerPescadoClientRpc();
                    }
                    else
                    {
                        GestorAudio.PlaySFX(TipoSFX.ComerPescado);
                    }
                    break;
                case TipoMejoraRun.AumentoDano:
                    // Mejora permanente acotada en la run (tope máx +50% daño extra por tienda)
                    MultiplicadorDanoLoot.Value = Mathf.Clamp(MultiplicadorDanoLoot.Value + def.valor, 1.0f, 1.50f);
                    MostrarTextoFlotanteClientRpc($"¡+{Mathf.RoundToInt(def.valor * 100f)}% Daño Permanente!", new Color(1f, 0.45f, 0.2f));
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, 9999f);
                    break;
                case TipoMejoraRun.AumentoDefensa:
                    // Mejora permanente acotada en la run (tope máx 35% de reducción por tienda)
                    ReduccionDanoLoot.Value = Mathf.Clamp(ReduccionDanoLoot.Value + def.valor, 0f, 0.35f);
                    MostrarTextoFlotanteClientRpc($"¡+{Mathf.RoundToInt(def.valor * 100f)}% Armadura Permanente!", new Color(0.35f, 0.85f, 1f));
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, 9999f);
                    break;
                case TipoMejoraRun.AumentoVelocidad:
                    // Desactivadas las botas de velocidad para evitar entorpecer la run
                    break;
                case TipoMejoraRun.AumentoVidaMaxima:
                    // Mejora permanente con TOPE MÁXIMO ESTRICTO DE 300 HP
                    const int TOPE_MAXIMO_VIDA = 300;
                    if (vidaMax >= TOPE_MAXIMO_VIDA)
                    {
                        MostrarTextoFlotanteClientRpc("¡Vida Máxima al Límite (300 HP)!", Color.yellow);
                        break;
                    }
                    int bonoHp = Mathf.RoundToInt(def.valor);
                    int nuevaVidaMax = Mathf.Min(TOPE_MAXIMO_VIDA, vidaMax + bonoHp);
                    int incrementoEfectivo = nuevaVidaMax - vidaMax;
                    vidaMax = nuevaVidaMax;
                    VidaMaxSincronizada.Value = vidaMax;
                    Curar(incrementoEfectivo);
                    MostrarTextoFlotanteClientRpc($"¡+{incrementoEfectivo} Vida Máxima! ({vidaMax}/{TOPE_MAXIMO_VIDA} HP)", new Color(1f, 0.25f, 0.45f));
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, 9999f);
                    break;
            }
        }

        public void ResetearPoderesRun()
        {
            MultiplicadorDanoLoot.Value = 1f;
            ReduccionDanoLoot.Value = 0f;
            MultiplicadorVelocidadLoot.Value = 1f;
            vidaMax = 100;
            if (VidaMaxSincronizada != null) VidaMaxSincronizada.Value = 100;
            if (VidaActual != null) VidaActual.Value = 100;
            _nivelesMejorasTienda.Clear();

            if (NetworkManager != null && NetworkManager.IsListening)
            {
                LimpiarEfectosRunClientRpc();
            }
            else
            {
                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.LimpiarEfectosConsumibles();
                }
            }
        }

        [ClientRpc]
        private void LimpiarEfectosRunClientRpc()
        {
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.LimpiarEfectosConsumibles();
            }
        }

        [ClientRpc]
        private void MostrarEfectoConsumibleHUDClientRpc(byte tipoMejoraByte, float duracion)
        {
            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.ActivarEfectoConsumibleDerecha((TipoMejoraRun)tipoMejoraByte, duracion);
            }
        }

        [ClientRpc]
        public void MostrarTextoFlotanteClientRpc(string texto, Color color)
        {
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.6f, 0), texto, color);
        }

        [ClientRpc]
        private void MostrarDanoClientRpc(int cantidad, Color color)
        {
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.25f, 0), cantidad.ToString(), color);
            if (color != Color.green)
            {
                if (IsOwner)
                {
                    GestorAudio.PlayDanoGatoRotativo(1.0f);
                }
                else
                {
                    GestorAudio.PlayDanoGatoRotativo(0.6f);
                }
            }
        }

        public virtual void Curar(int cantidad)
        {
            if (!IsServer || EstaMuerto.Value) return;
            VidaActual.Value = Mathf.Min(vidaMax, VidaActual.Value + cantidad);
            MostrarDanoClientRpc(cantidad, Color.green);
        }

        public virtual void CurarTotalmente()
        {
            if (!IsServer || EstaMuerto.Value) return;
            int curacion = vidaMax - VidaActual.Value;
            VidaActual.Value = vidaMax;
            if (curacion > 0)
            {
                MostrarDanoClientRpc(curacion, Color.green);
            }
        }

        [ClientRpc]
        public void NotificarComerPescadoClientRpc()
        {
            GestorAudio.PlaySFX(TipoSFX.ComerPescado);
        }

        [ClientRpc]
        public void TeletransportarClientRpc(Vector2 nuevaPos)
        {
            transform.position = (Vector3)nuevaPos;
            if (_rb != null)
            {
                _rb.position = nuevaPos;
                _rb.velocity = Vector2.zero;
            }
        }

        public virtual void AplicarKnockback(Vector2 direccion, float fuerza)
        {
            if (EstaMuerto.Value) return;
            if (!IsServer && !IsOwner) return;
            if (IsOwner)
            {
                // ~600 px/s en Godot equivale a ~18 unidades en Unity
                _knockback = direccion.normalized * (fuerza / 32f);
            }
            else if (IsServer)
            {
                AplicarKnockbackClientRpc(direccion, fuerza);
            }
        }

        [ClientRpc]
        public void AplicarKnockbackClientRpc(Vector2 direccion, float fuerza)
        {
            if (EstaMuerto.Value) return;
            if (IsOwner)
            {
                _knockback = direccion.normalized * (fuerza / 32f);
            }
        }

        protected virtual void Morir()
        {
            if (!IsServer) return;
            EstaMuerto.Value = true;
            _knockback = Vector2.zero;
            _inputMovimiento = Vector2.zero;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.isKinematic = true;
            }
            if (_col != null) _col.enabled = false;

            // Apagar inmediatamente cualquier rayo, lanzallamas o habilidad activa al morir
            LimpiarEstadosYHabilidades();
            LimpiarEstadosYHabilidadesClientRpc();

            AlMorir?.Invoke();

            if (GestorPartida.Instancia != null)
            {
                GestorPartida.Instancia.RevisarFinDePartida();
            }
        }

        public void RevivirConMitadVida()
        {
            if (!IsServer) return;
            EstaMuerto.Value = false;
            VidaActual.Value = Mathf.Max(1, vidaMax / 2); // Siempre reviven con la mitad de vida
            _col.enabled = true;
            _rb.velocity = Vector2.zero;
            NotificarRevivirClientRpc();
        }

        [ClientRpc]
        private void NotificarRevivirClientRpc()
        {
            if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
            transform.rotation = Quaternion.identity;
            TextoFlotante.Crear(transform.position + Vector3.up * 1.5f, "¡REVIVIDO! (50% VIDA)", Color.green);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RevivirAliadoServerRpc(ulong targetNetId, ServerRpcParams rpcParams = default)
        {
            if (EstaMuerto.Value) return;
            if (NetworkManager != null && NetworkManager.SpawnManager != null &&
                NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var netObj))
            {
                var aliado = netObj.GetComponent<Personaje>();
                if (aliado != null && aliado.EstaMuerto.Value)
                {
                    float dist = Vector2.Distance(transform.position, aliado.transform.position);
                    if (dist <= 3.8f)
                    {
                        aliado.RevivirConMitadVida();
                    }
                }
            }
        }

        private float _tiempoManteniendoRevivir = 0f;
        private const float DURACION_MANTENER_REVIVIR = 1.8f;
        private TextMesh _textoIndicadorRevivir;

        private void ActualizarTextoIndicadorRevivir(Personaje aliado, string texto, Color color)
        {
            if (aliado == null)
            {
                if (_textoIndicadorRevivir != null) _textoIndicadorRevivir.gameObject.SetActive(false);
                return;
            }

            if (_textoIndicadorRevivir == null)
            {
                var go = new GameObject("IndicadorRevivirAliado");
                _textoIndicadorRevivir = go.AddComponent<TextMesh>();
                Font f = FuenteUtil.ObtenerFuentePixelify();
                _textoIndicadorRevivir.font = f;
                _textoIndicadorRevivir.fontSize = 28;
                _textoIndicadorRevivir.characterSize = 0.085f;
                _textoIndicadorRevivir.fontStyle = FontStyle.Bold;
                _textoIndicadorRevivir.alignment = TextAlignment.Center;
                _textoIndicadorRevivir.anchor = TextAnchor.MiddleCenter;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = f.material;
                    mr.sortingOrder = 15;
                }
            }

            _textoIndicadorRevivir.gameObject.SetActive(true);
            _textoIndicadorRevivir.transform.position = aliado.transform.position + new Vector3(0f, 1.55f, -0.5f);
            _textoIndicadorRevivir.text = texto;
            _textoIndicadorRevivir.color = color;
        }

        protected void ProcesarRevivirAliadosLocales()
        {
            if (!IsOwner || EstaMuerto.Value)
            {
                _tiempoManteniendoRevivir = 0f;
                ActualizarTextoIndicadorRevivir(null, "", Color.white);
                return;
            }

            // Buscar si hay algún compañero caído a corta distancia
            Personaje aliadoCaido = null;
            float menorDist = 3.2f;

            for (int i = 0; i < Todos.Count; i++)
            {
                var p = Todos[i];
                if (p != null && p != this && p.EstaMuerto.Value)
                {
                    float d = Vector2.Distance(transform.position, p.transform.position);
                    if (d < menorDist)
                    {
                        menorDist = d;
                        aliadoCaido = p;
                    }
                }
            }

            if (aliadoCaido != null)
            {
                // Mantener pulsada la tecla F al lado del compañero para revivirlo
                if (Input.GetKey(KeyCode.F))
                {
                    _tiempoManteniendoRevivir += Time.deltaTime;
                    float progreso = Mathf.Clamp01(_tiempoManteniendoRevivir / DURACION_MANTENER_REVIVIR);
                    int bloquesLlenos = Mathf.RoundToInt(progreso * 8);
                    string barra = new string('█', bloquesLlenos) + new string('░', 8 - bloquesLlenos);
                    ActualizarTextoIndicadorRevivir(aliadoCaido, $"➕ Reviviendo [{barra}] {Mathf.RoundToInt(progreso * 100f)}%", Color.green);

                    if (_tiempoManteniendoRevivir >= DURACION_MANTENER_REVIVIR)
                    {
                        _tiempoManteniendoRevivir = 0f;
                        ActualizarTextoIndicadorRevivir(null, "", Color.white);
                        RevivirAliadoServerRpc(aliadoCaido.NetworkObjectId);
                    }
                }
                else
                {
                    _tiempoManteniendoRevivir = Mathf.MoveTowards(_tiempoManteniendoRevivir, 0f, Time.deltaTime * 2f);
                    ActualizarTextoIndicadorRevivir(aliadoCaido, "Mantén [F] para revivir", new Color(1f, 0.92f, 0.35f));
                }
            }
            else
            {
                _tiempoManteniendoRevivir = 0f;
                ActualizarTextoIndicadorRevivir(null, "", Color.white);
            }
        }

        private void AlCambiarVida(int anterior, int actual)
        {
            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                int maxHp = VidaMaxSincronizada.Value > 0 ? VidaMaxSincronizada.Value : vidaMax;
                HUDJugador.Instancia.ActualizarVida(actual, maxHp);
            }
        }

        private void AlCambiarEstadoMuerte(bool anterior, bool muerto)
        {
            if (muerto)
            {
                if (_col != null) _col.enabled = false;
                _knockback = Vector2.zero;
                _inputMovimiento = Vector2.zero;
                if (_rb != null)
                {
                    _rb.velocity = Vector2.zero;
                    _rb.angularVelocity = 0f;
                    _rb.isKinematic = true;
                }
                if (_spriteRenderer != null) _spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.55f);
                transform.rotation = Quaternion.Euler(0, 0, 80f);
                if (IsOwner)
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 1.5f, "¡HAS CAÍDO! Espera a tu compañero", Color.red);
                    GestorAudio.PlaySFX(TipoSFX.MuerteJugador);
                }
            }
            else
            {
                if (_col != null) _col.enabled = true;
                _knockback = Vector2.zero;
                if (_rb != null)
                {
                    _rb.velocity = Vector2.zero;
                    _rb.angularVelocity = 0f;
                    if (IsOwner || NetworkManager == null || !NetworkManager.IsListening)
                    {
                        _rb.isKinematic = false;
                    }
                }
                if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
                transform.rotation = Quaternion.identity;
            }
        }

        [ClientRpc]
        public void ReiniciarCooldownsClientRpc()
        {
            ReiniciarCooldowns();
        }

        public virtual void ReiniciarCooldowns()
        {
            // Sobrescrito en subclases
        }

        [ClientRpc]
        public void LimpiarEstadosYHabilidadesClientRpc()
        {
            LimpiarEstadosYHabilidades();
        }

        public virtual void LimpiarEstadosYHabilidades()
        {
            // Solo reiniciamos cooldowns para que el jugador entre al nuevo piso con habilidades listas.
            ReiniciarCooldowns();

            if (IsServer)
            {
                JugadorCongelado.Value = false;
                JugadorRalentizado.Value = false;
                JugadorQuemado.Value = false;
                JugadorEnvenenado.Value = false;
            }
            _tiempoCongeladoPj = 0f;
            _tiempoRalentizadoPj = 0f;
            _tiempoQuemadoPj = 0f;
            _tiempoVenenoPj = 0f;

            // NOTA: Las mejoras acumulativas de la run (MultiplicadorDanoLoot, ReduccionDanoLoot,
            // MultiplicadorVelocidadLoot, vidaMax extra y OroRun) son permanentes durante TODA la run
            // y deben preservarse al pasar de piso / nivel.
        }

        protected abstract void ProcesarInputsHabilidadesLocales();
    }
}

