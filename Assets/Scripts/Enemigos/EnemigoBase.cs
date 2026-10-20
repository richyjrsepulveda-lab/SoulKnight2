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

        [Header("Bioma y Rol")]
        public NetworkVariable<int> BiomaActualNet = new NetworkVariable<int>(0);
        public NetworkVariable<int> RolActualNet = new NetworkVariable<int>(0);
        public NetworkVariable<float> EscalaVisualNet = new NetworkVariable<float>(1.0f);
        public bool EsInvocado { get; set; } = false;

        private float _timerHabilidadRol = 0f;

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

        private VisualEfectoQuemadura _visualQuemadura;
        private static readonly Collider2D[] _murosCheckBuffer = new Collider2D[8];

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _velocidadActual = velocidadBase;
            _renderersCacheados = GetComponentsInChildren<SpriteRenderer>(true);

            _visualQuemadura = GetComponent<VisualEfectoQuemadura>();
            if (_visualQuemadura == null) _visualQuemadura = gameObject.AddComponent<VisualEfectoQuemadura>();

            PhysicsMaterial2D matSinFriccion = new PhysicsMaterial2D("SinFriccionEnemigo") { friction = 0f, bounciness = 0f };
            if (_rb != null)
            {
                _rb.sharedMaterial = matSinFriccion;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            if (_col != null)
            {
                _col.sharedMaterial = matSinFriccion;
            }
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
                if (spriteCongelado != null)
                {
                    spriteCongelado.SetActive(act);
                    if (act) AjustarVisualCongelado();
                }
                if (act) GestorAudio.PlaySFX(TipoSFX.Congelar);
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

            BiomaActualNet.OnValueChanged += (prev, act) => ActualizarVisualPixelArt();
            RolActualNet.OnValueChanged += (prev, act) => ActualizarVisualPixelArt();
            EscalaVisualNet.OnValueChanged += (prev, act) => ActualizarVisualPixelArt();

            if (barraVida != null) barraVida.Establecer(VidaActual.Value, vidaMax);
            if (spriteCongelado != null)
            {
                spriteCongelado.SetActive(EstaCongelado.Value);
                if (EstaCongelado.Value) AjustarVisualCongelado();
            }
            ActualizarTinteVisual();
            ActualizarVisualPixelArt();
        }

        public void ConfigurarRolYBioma(TipoBioma bioma, RolEnemigo rol, float multDificultad = 1.0f, int nivelRun = 1)
        {
            if (!IsServer) return;
            BiomaActualNet.Value = (int)bioma;
            RolActualNet.Value = (int)rol;

            int nivelCiclo = Mathf.Clamp(((nivelRun - 1) % 10) + 1, 1, 10);
            float bonusVelocidadNivel = 1.0f + (nivelCiclo - 1) * 0.035f;
            float reduccionCooldownNivel = Mathf.Max(0.68f, 1.0f - (nivelCiclo - 1) * 0.035f);

            switch (rol)
            {
                case RolEnemigo.Tanque:
                    vidaMax = Mathf.RoundToInt(250 * multDificultad);
                    velocidadBase = 2.6f * bonusVelocidadNivel;
                    dano = Mathf.RoundToInt(16 * multDificultad);
                    rangoAtaque = 1.40f;
                    tiempoEntreAtaques = 1.35f * reduccionCooldownNivel;
                    EscalaVisualNet.Value = 1.95f;
                    break;
                case RolEnemigo.MeleeRapido:
                    vidaMax = Mathf.RoundToInt(95 * multDificultad);
                    velocidadBase = 4.8f * bonusVelocidadNivel;
                    dano = Mathf.RoundToInt(10 * multDificultad);
                    rangoAtaque = 1.25f;
                    tiempoEntreAtaques = 0.75f * reduccionCooldownNivel;
                    EscalaVisualNet.Value = 1.45f;
                    break;
                case RolEnemigo.Rango:
                    vidaMax = Mathf.RoundToInt(120 * multDificultad);
                    velocidadBase = 2.9f * bonusVelocidadNivel;
                    dano = Mathf.RoundToInt(13 * multDificultad);
                    rangoAtaque = 1.05f;
                    tiempoEntreAtaques = 1.10f * reduccionCooldownNivel;
                    EscalaVisualNet.Value = 1.55f;
                    break;
                case RolEnemigo.Especial:
                    vidaMax = Mathf.RoundToInt(145 * multDificultad);
                    velocidadBase = 3.5f * bonusVelocidadNivel;
                    dano = Mathf.RoundToInt(14 * multDificultad);
                    rangoAtaque = 1.30f;
                    tiempoEntreAtaques = 1.05f * reduccionCooldownNivel;
                    EscalaVisualNet.Value = 1.60f;
                    break;
                case RolEnemigo.Spawner:
                    vidaMax = Mathf.RoundToInt(340 * multDificultad);
                    velocidadBase = 0f;
                    dano = 0;
                    rangoAtaque = 0f;
                    tiempoEntreAtaques = 2.0f;
                    rangoMaximoDeteccion = 9.5f; // Rango de detección para despertar e invocar
                    EscalaVisualNet.Value = 2.05f;
                    if (_rb != null) _rb.isKinematic = true;
                    break;
            }

            var mago = GetComponent<EnemigoMago>();
            if (mago != null)
            {
                mago.tiempoEntreDisparos = Mathf.Max(0.85f, 1.35f * reduccionCooldownNivel);
                mago.danoProyectil = Mathf.RoundToInt(12 * multDificultad);
            }

            var slime = GetComponent<EnemigoSlime>();
            if (slime != null)
            {
                slime.tiempoEntreAtaques = Mathf.Max(0.65f, 0.95f * reduccionCooldownNivel);
                slime.velocidadBase = 4.4f * bonusVelocidadNivel;
            }

            _velocidadActual = velocidadBase;
            VidaActual.Value = vidaMax;
            ActualizarVisualPixelArt();
        }

        public void ActualizarVisualPixelArt()
        {
            if (this is JefeBase) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = CatalogoPixelArtEnemigos.ObtenerSpriteEnemigo((TipoBioma)BiomaActualNet.Value, (RolEnemigo)RolActualNet.Value);
            }
            float esc = EscalaVisualNet.Value > 0.1f ? EscalaVisualNet.Value : 1.45f;
            transform.localScale = new Vector3(esc, esc, 1f);
            AjustarHitboxPrecisa((RolEnemigo)RolActualNet.Value);
            if (EstaCongelado.Value && spriteCongelado != null)
            {
                AjustarVisualCongelado();
            }
        }

        public void AjustarVisualCongelado()
        {
            if (spriteCongelado == null) return;

            var sr = GetComponent<SpriteRenderer>();
            var srCong = spriteCongelado.GetComponent<SpriteRenderer>();
            if (srCong == null) srCong = spriteCongelado.AddComponent<SpriteRenderer>();

            float w = 1f;
            float h = 1f;
            float offsetX = 0f;
            float offsetY = 0.12f;

            if (sr != null && sr.sprite != null)
            {
                Sprite sp = sr.sprite;
                float ppu = sp.pixelsPerUnit > 0 ? sp.pixelsPerUnit : 32f;
                w = sp.rect.width / ppu;
                h = sp.rect.height / ppu;
                offsetX = (sp.rect.width * 0.5f - sp.pivot.x) / ppu;
                offsetY = (sp.rect.height * 0.5f - sp.pivot.y) / ppu;
            }
            else
            {
                var box = GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    w = box.size.x;
                    h = box.size.y;
                    offsetX = box.offset.x;
                    offsetY = box.offset.y;
                }
            }

            float congW = 2.0f;
            float congH = 2.0f;
            if (srCong.sprite != null)
            {
                float ppuCong = srCong.sprite.pixelsPerUnit > 0 ? srCong.sprite.pixelsPerUnit : 32f;
                congW = srCong.sprite.rect.width / ppuCong;
                congH = srCong.sprite.rect.height / ppuCong;
            }

            // Escalar el bloque de hielo para que envuelva al enemigo con un 16% de holgura exacta
            float escalaX = (w * 1.16f) / Mathf.Max(0.1f, congW);
            float escalaY = (h * 1.16f) / Mathf.Max(0.1f, congH);
            float escalaBase = Mathf.Max(escalaX, escalaY);

            spriteCongelado.transform.localScale = new Vector3(Mathf.Max(escalaBase * 0.85f, escalaX), Mathf.Max(escalaBase * 0.85f, escalaY), 1f);
            spriteCongelado.transform.localPosition = new Vector3(offsetX, offsetY, -0.15f);

            if (sr != null)
            {
                srCong.sortingOrder = sr.sortingOrder + 1;
            }
            srCong.color = new Color(0.85f, 0.95f, 1f, 0.88f);
        }

        private void AjustarHitboxPrecisa(RolEnemigo rol)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                switch (rol)
                {
                    case RolEnemigo.Tanque:
                        box.size = new Vector2(0.70f, 0.75f);
                        box.offset = new Vector2(0f, -0.05f);
                        break;
                    case RolEnemigo.MeleeRapido:
                        box.size = new Vector2(0.55f, 0.60f);
                        box.offset = new Vector2(0f, -0.05f);
                        break;
                    case RolEnemigo.Rango:
                        box.size = new Vector2(0.55f, 0.65f);
                        box.offset = new Vector2(0f, -0.05f);
                        break;
                    case RolEnemigo.Especial:
                        box.size = new Vector2(0.55f, 0.65f);
                        box.offset = new Vector2(0f, -0.05f);
                        break;
                    case RolEnemigo.Spawner:
                        box.size = new Vector2(0.75f, 0.90f);
                        box.offset = new Vector2(0f, 0f);
                        break;
                    default:
                        box.size = new Vector2(0.60f, 0.70f);
                        box.offset = new Vector2(0f, -0.05f);
                        break;
                }
            }
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = 0.35f;
                circle.offset = new Vector2(0f, -0.05f);
            }
        }

        private static readonly Collider2D[] _aliadosBuffer = new Collider2D[24];

        private void PulsoCuracionPlanta()
        {
            if (!IsServer || VidaActual.Value <= 0) return;
            MostrarOndaCuracionClientRpc(transform.position, 6.5f);
            // Curar a aliados cercanos sin asignaciones de memoria
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, 6.5f, _aliadosBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = _aliadosBuffer[i];
                if (col == null) continue;
                var e = col.GetComponent<EnemigoBase>();
                if (e != null && e.gameObject.activeInHierarchy && e.VidaActual.Value > 0)
                {
                    int heal = 25;
                    e.VidaActual.Value = Mathf.Min(e.vidaMax, e.VidaActual.Value + heal);
                    TextoFlotante.Crear(e.transform.position + new Vector3(0, 1.2f, 0), $"+{heal} HP", Color.green);
                }
            }
        }

        private void SoltarEsporasEnPiso()
        {
            if (!IsServer || VidaActual.Value <= 0) return;
            Vector2 pos = transform.position;
            ZonaPeligroPiso.Instanciar(pos, 2.1f, TipoZonaPeligro.Esporas, 6.5f, esServidor: true, danoTick: 5);
            CrearZonaPeligroClientRpc(pos, 2.1f, (int)TipoZonaPeligro.Esporas, 6.5f);
        }

        private void SoltarFuegoEnPiso()
        {
            if (!IsServer || VidaActual.Value <= 0) return;
            Vector2 pos = transform.position;
            ZonaPeligroPiso.Instanciar(pos, 1.6f, TipoZonaPeligro.Fuego, 5.0f, esServidor: true, danoTick: 5);
            CrearZonaPeligroClientRpc(pos, 1.6f, (int)TipoZonaPeligro.Fuego, 5.0f);
        }

        private void SoltarHieloEnPiso()
        {
            if (!IsServer || VidaActual.Value <= 0) return;
            Vector2 pos = transform.position;
            ZonaPeligroPiso.Instanciar(pos, 1.8f, TipoZonaPeligro.Hielo, 5.5f, esServidor: true, danoTick: 3);
            CrearZonaPeligroClientRpc(pos, 1.8f, (int)TipoZonaPeligro.Hielo, 5.5f);
        }

        [ClientRpc]
        public void CrearZonaPeligroClientRpc(Vector2 pos, float radio, int tipoZona, float duracion)
        {
            if (IsServer) return;
            ZonaPeligroPiso.Instanciar(pos, radio, (TipoZonaPeligro)tipoZona, duracion, esServidor: false);
        }

        [ClientRpc]
        private void MostrarOndaCuracionClientRpc(Vector2 pos, float radio)
        {
            var ondaObj = new GameObject("OndaCuracion");
            ondaObj.transform.position = new Vector3(pos.x, pos.y, -0.2f);
            var lr = ondaObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 4;
            lr.startWidth = 0.12f;
            lr.endWidth = 0.02f;
            lr.startColor = new Color(0.35f, 1f, 0.45f, 0.95f);
            lr.endColor = new Color(0.85f, 1f, 0.5f, 0f);
            lr.loop = true;
            const int seg = 24;
            lr.positionCount = seg;
            ondaObj.AddComponent<AnimacionAnilloExplosion>().Inicializar(radio, false);
        }

        private void InvocarEsbirroAtaud()
        {
            if (!IsServer || VidaActual.Value <= 0) return;
            if (GestorPartida.Instancia != null)
            {
                RolEnemigo subrol = (UnityEngine.Random.value < 0.65f) ? RolEnemigo.MeleeRapido : RolEnemigo.Rango;
                Vector2 posSpawn = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle.normalized * 1.6f;
                TipoBioma biomaActual = (TipoBioma)BiomaActualNet.Value;
                GestorPartida.Instancia.SpawnearEsbirroEmergente(posSpawn, biomaActual, subrol);
            }
        }

        private int ContarEsbirrosCercanos(float radio)
        {
            if (GestorPartida.Instancia != null)
            {
                return GestorPartida.Instancia.ContarEsbirrosCercanos((Vector2)transform.position, radio);
            }
            return 0;
        }

        private void ActualizarTinteVisual()
        {
            if (_visualQuemadura != null)
            {
                _visualQuemadura.SetActivo(EstaQuemado.Value);
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (EstaCongelado.Value || EstaRalentizado.Value)
                {
                    // Filtro azul clarito helado para enemigos ralentizados o congelados
                    sr.color = new Color(0.48f, 0.85f, 1f, 1f);
                }
                else
                {
                    // Se mantiene el color blanco limpio original del pixel art para que no parezca que están enojados
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

            if (IsServer)
            {
                _timerHabilidadRol += Time.deltaTime;
                if (BiomaActualNet.Value == (int)TipoBioma.Planta && RolActualNet.Value == (int)RolEnemigo.Tanque)
                {
                    if (_timerHabilidadRol >= 3.8f)
                    {
                        _timerHabilidadRol = 0f;
                        PulsoCuracionPlanta();
                    }
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Planta && RolActualNet.Value == (int)RolEnemigo.Especial)
                {
                    if (_timerHabilidadRol >= 2.6f)
                    {
                        _timerHabilidadRol = 0f;
                        SoltarEsporasEnPiso();
                    }
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Fuego && RolActualNet.Value == (int)RolEnemigo.Especial)
                {
                    if (_timerHabilidadRol >= 2.5f)
                    {
                        _timerHabilidadRol = 0f;
                        SoltarFuegoEnPiso();
                    }
                }
                else if (BiomaActualNet.Value == (int)TipoBioma.Hielo && RolActualNet.Value == (int)RolEnemigo.Especial)
                {
                    if (_timerHabilidadRol >= 2.6f)
                    {
                        _timerHabilidadRol = 0f;
                        SoltarHieloEnPiso();
                    }
                }
                else if (RolActualNet.Value == (int)RolEnemigo.Spawner)
                {
                    // Los ataúdes solo invocan si detectan activamente a un jugador cercano (optimización de rendimiento y antispam)
                    Personaje jugadorDetectado = ObtenerJugadorMasCercano();
                    if (jugadorDetectado != null)
                    {
                        if (_timerHabilidadRol >= 3.5f)
                        {
                            _timerHabilidadRol = 0f;
                            // Límite de esbirros activos cercanos a este ataúd para evitar sobrecarga
                            if (ContarEsbirrosCercanos(9f) < 3)
                            {
                                InvocarEsbirroAtaud();
                            }
                        }
                    }
                    else
                    {
                        // Si no hay jugador cerca, se resetea el timer de invocación
                        _timerHabilidadRol = 0f;
                    }
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

            // Si el enemigo es de Rango o Spawner, NO hace ataques melee cuerpo a cuerpo en Update()
            if (RolActualNet.Value == (int)RolEnemigo.Rango || RolActualNet.Value == (int)RolEnemigo.Spawner)
            {
                return;
            }

            Personaje jugadorCercano = ObtenerJugadorMasCercano();
            if (jugadorCercano != null && _tiempoAtaqueActual <= 0f)
            {
                float dist = Vector2.Distance(transform.position, jugadorCercano.transform.position);
                if (dist <= rangoAtaque)
                {
                    Vector2 dirAtaque = ((Vector2)jugadorCercano.transform.position - (Vector2)transform.position).normalized;
                    Vector2 posZarpazo = (Vector2)transform.position + dirAtaque * 0.65f;

                    // Mostrar zarpazo / ataque melee visible en todos los clientes
                    MostrarZarpazoClientRpc(posZarpazo, dirAtaque, BiomaActualNet.Value);

                    jugadorCercano.RecibirDano(dano);
                    jugadorCercano.AplicarKnockback(dirAtaque, 350f);

                    // Efectos de estado de bioma al golpear a un jugador
                    if (BiomaActualNet.Value == (int)TipoBioma.Hielo)
                    {
                        jugadorCercano.AplicarRalentizacion(3.5f);
                    }
                    else if (BiomaActualNet.Value == (int)TipoBioma.Fuego)
                    {
                        jugadorCercano.AplicarQuemadura(4.0f, 6);
                    }
                    else if (BiomaActualNet.Value == (int)TipoBioma.Planta)
                    {
                        jugadorCercano.AplicarEsporasVeneno(4.0f, 5);
                    }

                    // Si el jugador tiene la Armadura de Fuego activa, quema al enemigo que lo golpeó a melee
                    if (jugadorCercano is PersonajeFuego pjFuego && pjFuego.ArmaduraActiva.Value)
                    {
                        AplicarQuemadura();
                        RecibirDano(18);
                        MostrarDanoClientRpc(18, new Color(1f, 0.45f, 0.1f));
                    }

                    _tiempoAtaqueActual = tiempoEntreAtaques;

                    // Al golpear, mantiene la presión sobre el jugador
                    _modoActual = ModoMovimiento.Perseguir;
                    _timerDecision = 0.35f;
                    _direccionMovimiento = dirAtaque;
                    _factorVelocidad = 0.95f;
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
                // Decisión de movimiento dinámica y rápida (entre 0.6s y 1.2s)
                _timerDecision -= Time.fixedDeltaTime;
                if (_timerDecision <= 0f)
                {
                    _timerDecision = UnityEngine.Random.Range(0.6f, 1.2f);
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

            // Seguridad anti-atasco en muros: si el enemigo overlap con un muro o columna, expulsarlo suavemente hacia afuera
            int hitsMuro = Physics2D.OverlapCircleNonAlloc(_rb.position, 0.35f, _murosCheckBuffer);
            for (int i = 0; i < hitsMuro; i++)
            {
                var m = _murosCheckBuffer[i];
                if (m != null && !m.isTrigger && (m.CompareTag("Pared") || m.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0 || m.name.IndexOf("columna", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Vector2 posMuro = m.transform.position;
                    Vector2 dirEscape = ((Vector2)transform.position - posMuro);
                    if (dirEscape.sqrMagnitude < 0.001f) dirEscape = Vector2.down;
                    _rb.position += dirEscape.normalized * (1.2f * Time.fixedDeltaTime);
                    _knockback = Vector2.zero;
                    break;
                }
            }
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

            // Si está muy cerca (< 2.5m), ataca y presiona agresivamente al jugador
            if (distancia < 2.5f)
            {
                if (tirada < 0.75f)
                {
                    // 75% Probabilidad: Embestir directo
                    _modoActual = ModoMovimiento.Perseguir;
                    _direccionMovimiento = dirHaciaJugador;
                    _factorVelocidad = 1.05f;
                }
                else if (tirada < 0.90f)
                {
                    // 15% Probabilidad: Flanqueo cerrado
                    _modoActual = ModoMovimiento.Flanquear;
                    _direccionMovimiento = (dirHaciaJugador * 0.4f + dirPerpendicular * 0.8f).normalized;
                    _factorVelocidad = 0.95f;
                }
                else
                {
                    // 10% Probabilidad: Breve finta
                    _modoActual = ModoMovimiento.Retroceder;
                    _direccionMovimiento = -dirHaciaJugador;
                    _factorVelocidad = 0.65f;
                }
                return;
            }

            // A distancia normal:
            if (tirada < 0.70f)
            {
                // 70% Probabilidad: Perseguir directamente
                _modoActual = ModoMovimiento.Perseguir;
                _direccionMovimiento = dirHaciaJugador;
                _factorVelocidad = 1.0f;
            }
            else if (tirada < 0.90f)
            {
                // 20% Probabilidad: Flanquear agresivo
                _modoActual = ModoMovimiento.Flanquear;
                _direccionMovimiento = (dirHaciaJugador * 0.45f + dirPerpendicular * 0.75f).normalized;
                _factorVelocidad = 0.95f;
            }
            else
            {
                // 10% Probabilidad: Ajuste de ángulo
                _modoActual = ModoMovimiento.Retroceder;
                _direccionMovimiento = -dirHaciaJugador;
                _factorVelocidad = 0.65f;
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
            bool esDanoHielo = color.b > 0.7f && color.r < 0.6f;
            if (esDanoHielo)
            {
                GestorAudio.PlaySFX(TipoSFX.ImpactoHielo, 1.0f);
            }
            else
            {
                GestorAudio.PlaySFX(TipoSFX.ImpactoEnemigo, 0.95f);
            }
        }

        public void AplicarKnockback(Vector2 direccion, float fuerza)
        {
            if (!IsServer) return;
            // Prevenir empujar al enemigo dentro de una pared sólida
            RaycastHit2D hitCercano = Physics2D.Raycast(transform.position, direccion, 0.6f);
            if (hitCercano.collider != null && !hitCercano.collider.isTrigger && (hitCercano.collider.CompareTag("Pared") || hitCercano.collider.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0 || hitCercano.collider.name.IndexOf("columna", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _knockback = Vector2.zero;
                return;
            }
            _knockback = direccion.normalized * Mathf.Min(fuerza / 32f, 10f);
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
                // Si era un Sarcófago Andante en el Desierto, al morir emerge una Momia
                if (BiomaActualNet.Value == (int)TipoBioma.Arena && RolActualNet.Value == (int)RolEnemigo.Tanque)
                {
                    GestorPartida.Instancia.SpawnearEsbirroEmergente(transform.position, TipoBioma.Arena, RolEnemigo.MeleeRapido);
                }
                // Si era Hongo de Esporas, al morir estalla en una nube de esporas
                if (BiomaActualNet.Value == (int)TipoBioma.Planta && RolActualNet.Value == (int)RolEnemigo.Especial)
                {
                    SoltarEsporasEnPiso();
                }
                // Si era Fatuo Volcánico, al morir estalla en llamas
                if (BiomaActualNet.Value == (int)TipoBioma.Fuego && RolActualNet.Value == (int)RolEnemigo.Especial)
                {
                    SoltarFuegoEnPiso();
                }

                GestorPartida.Instancia.EnemigoEliminado(this);
            }

            MostrarMuerteClientRpc();
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }

        [ClientRpc]
        private void MostrarMuerteClientRpc()
        {
            GestorAudio.PlaySFX(TipoSFX.MuerteEnemigo, 0.75f, 0.1f);
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
            float menorDistSq = rangoMaximoDeteccion * rangoMaximoDeteccion;
            Vector2 miPos = transform.position;

            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var j = Personaje.Todos[i];
                if (j == null || j.EstaMuerto.Value || !j.gameObject.activeInHierarchy) continue;
                float dSq = ((Vector2)j.transform.position - miPos).sqrMagnitude;
                if (dSq <= menorDistSq)
                {
                    menorDistSq = dSq;
                    masCercano = j;
                }
            }

            return masCercano;
        }

        [ClientRpc]
        private void MostrarZarpazoClientRpc(Vector2 pos, Vector2 dir, int bioma)
        {
            GestorAudio.PlaySFX(TipoSFX.Zarpazo);
            var swipeObj = new GameObject("ZarpazoEnemigo");
            swipeObj.transform.position = new Vector3(pos.x, pos.y, -0.45f);

            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            swipeObj.transform.rotation = Quaternion.Euler(0, 0, angulo);

            var lr = swipeObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 10;
            lr.startWidth = 0.18f;
            lr.endWidth = 0.05f;

            Color cInicio, cFin;
            if (bioma == (int)TipoBioma.Fuego)
            {
                cInicio = new Color(1f, 0.45f, 0.08f, 0.95f);
                cFin = new Color(1f, 0.90f, 0.20f, 0.20f);
            }
            else if (bioma == (int)TipoBioma.Hielo)
            {
                cInicio = new Color(0.35f, 0.88f, 1f, 0.95f);
                cFin = new Color(0.90f, 0.98f, 1f, 0.20f);
            }
            else if (bioma == (int)TipoBioma.Planta)
            {
                cInicio = new Color(0.40f, 0.95f, 0.25f, 0.95f);
                cFin = new Color(0.85f, 0.35f, 0.95f, 0.20f);
            }
            else
            {
                cInicio = new Color(1f, 0.20f, 0.20f, 0.95f);
                cFin = new Color(1f, 0.85f, 0.40f, 0.20f);
            }

            lr.startColor = cInicio;
            lr.endColor = cFin;

            const int seg = 8;
            lr.positionCount = seg;
            float radioArco = 0.75f;
            float apertura = 95f * Mathf.Deg2Rad;
            for (int i = 0; i < seg; i++)
            {
                float t = ((float)i / (seg - 1)) - 0.5f;
                float a = t * apertura;
                Vector3 p = new Vector3(Mathf.Cos(a) * radioArco, Mathf.Sin(a) * radioArco, 0f);
                lr.SetPosition(i, p);
            }

            swipeObj.AddComponent<AnimacionZarpazo>().Inicializar(0.24f);
        }
    }

    public class AnimacionZarpazo : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _duracion = 0.24f;
        private float _tiempo = 0f;

        public void Inicializar(float duracion)
        {
            _lr = GetComponent<LineRenderer>();
            _duracion = duracion;
            Destroy(gameObject, _duracion + 0.02f);
        }

        void Update()
        {
            if (_lr == null) return;
            _tiempo += Time.deltaTime;
            float alfa = Mathf.Clamp01(1f - (_tiempo / _duracion));
            Color c1 = _lr.startColor;
            c1.a = alfa * 0.95f;
            _lr.startColor = c1;

            Color c2 = _lr.endColor;
            c2.a = alfa * 0.20f;
            _lr.endColor = c2;

            transform.localScale = Vector3.one * (1.0f + (_tiempo / _duracion) * 0.35f);
        }
    }
}
