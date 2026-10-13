using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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

        public float MultiplicadorDanoTotal => (nivelProgreso != null ? nivelProgreso.MultiplicadorDano : 1f) * MultiplicadorDanoLoot.Value;

        /// <summary>
        /// Comprueba las 3 capas estrictas de progresión:
        /// 1. Maestría (Permanente): Si no está desbloqueada por maestría, NUNCA se puede usar en la run.
        /// 2. Loadout (Lobby): Debe estar elegida en el equipamiento del personaje.
        /// 3. Nivel Run (1-10): Debe haberse alcanzado el nivel requerido en la partida.
        /// </summary>
        public bool PuedeUsarHabilidad(IdHabilidad id, out string motivoBloqueo)
        {
            motivoBloqueo = "";
            if (id == IdHabilidad.Ninguna) return false;

            var def = CatalogoHabilidades.Obtener(id);
            if (def == null) return true;

            // 1. Maestría (Requisito absoluto y permanente de NIVEL de cuenta)
            int nivelMaestriaActual = IsOwner ? GestorMaestria.ObtenerNivelMaestria(TipoPj) : NivelMaestriaSincronizado.Value;
            if (nivelMaestriaActual < def.nivelMaestriaRequerido)
            {
                motivoBloqueo = $"🔒 Requiere Nivel {def.nivelMaestriaRequerido} de Maestría (Actual: Nv.{nivelMaestriaActual})";
                return false;
            }

            // 2. Loadout elegido en el lobby
            if (IsOwner)
            {
                var loadout = GestorLoadout.ObtenerLoadout(TipoPj);
                if (!loadout.Contains(id))
                {
                    motivoBloqueo = "No está equipada en tu Loadout";
                    return false;
                }
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
            EstaMuerto.OnValueChanged += AlCambiarEstadoMuerte;

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

                ProcesarInputsHabilidadesLocales();
                ProcesarRevivirAliadosLocales();
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
            if (!esControlLocal || EstaMuerto.Value) return;

            // Manejo físico del movimiento y knockback inmediatamente en la máquina local (0ms delay)
            float vel = (_corriendo ? ObtenerVelocidadCorrer() : _velocidadActual) * MultiplicadorVelocidadLoot.Value;
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

            MostrarDanoClientRpc(danoFinal, Color.white);

            if (VidaActual.Value <= 0)
            {
                Morir();
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
                    break;
                case TipoMejoraRun.AumentoDano:
                    _timerEfectoDano = DURACION_EFECTO_CONSUMIBLE;
                    MultiplicadorDanoLoot.Value = 1.0f + def.valor;
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, DURACION_EFECTO_CONSUMIBLE);
                    break;
                case TipoMejoraRun.AumentoDefensa:
                    _timerEfectoDefensa = DURACION_EFECTO_CONSUMIBLE;
                    ReduccionDanoLoot.Value = Mathf.Clamp(def.valor, 0f, 0.6f);
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, DURACION_EFECTO_CONSUMIBLE);
                    break;
                case TipoMejoraRun.AumentoVelocidad:
                    _timerEfectoVelocidad = DURACION_EFECTO_CONSUMIBLE;
                    MultiplicadorVelocidadLoot.Value = 1.0f + def.valor;
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, DURACION_EFECTO_CONSUMIBLE);
                    break;
                case TipoMejoraRun.AumentoVidaMaxima:
                    if (_bonoVidaMaxActivo == 0)
                    {
                        _bonoVidaMaxActivo = Mathf.RoundToInt(def.valor);
                        vidaMax += _bonoVidaMaxActivo;
                        VidaMaxSincronizada.Value = vidaMax;
                    }
                    _timerEfectoVidaMax = DURACION_EFECTO_CONSUMIBLE;
                    Curar(Mathf.RoundToInt(def.valor));
                    MostrarEfectoConsumibleHUDClientRpc((byte)mejora, DURACION_EFECTO_CONSUMIBLE);
                    break;
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
            if (IsOwner)
            {
                _knockback = direccion.normalized * (fuerza / 32f);
            }
        }

        protected virtual void Morir()
        {
            if (!IsServer) return;
            EstaMuerto.Value = true;
            _rb.velocity = Vector2.zero;
            _col.enabled = false;

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
                _textoIndicadorRevivir.fontSize = 28;
                _textoIndicadorRevivir.characterSize = 0.085f;
                _textoIndicadorRevivir.fontStyle = FontStyle.Bold;
                _textoIndicadorRevivir.alignment = TextAlignment.Center;
                _textoIndicadorRevivir.anchor = TextAnchor.MiddleCenter;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 15;
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
                _col.enabled = false;
                if (_spriteRenderer != null) _spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.55f);
                transform.rotation = Quaternion.Euler(0, 0, 80f);
                if (IsOwner)
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 1.5f, "¡HAS CAÍDO! Espera a tu compañero", Color.red);
                }
            }
            else
            {
                _col.enabled = true;
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
            ReiniciarCooldowns();
            if (IsServer)
            {
                _timerEfectoDano = 0f;
                _timerEfectoDefensa = 0f;
                _timerEfectoVelocidad = 0f;
                _timerEfectoVidaMax = 0f;
                if (_bonoVidaMaxActivo > 0)
                {
                    vidaMax = Mathf.Max(100, vidaMax - _bonoVidaMaxActivo);
                    VidaMaxSincronizada.Value = vidaMax;
                    _bonoVidaMaxActivo = 0;
                }
                MultiplicadorDanoLoot.Value = 1.0f;
                ReduccionDanoLoot.Value = 0f;
                MultiplicadorVelocidadLoot.Value = 1.0f;
            }
            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.LimpiarEfectosConsumiblesDerecha();
            }
        }

        protected abstract void ProcesarInputsHabilidadesLocales();
    }
}

