using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Personaje ElÃ©ctrico (PersonajeElectrico.gd en Godot).
    /// Habilidades:
    /// 1. Espada ElÃ©ctrica: Click izquierdo, ataque cuerpo a cuerpo (raycast corto), daÃ±o 70.
    /// 2. Dash: Tecla E, traslaciÃ³n rÃ¡pida en lÃ­nea recta, atraviesa e inflige 200 de daÃ±o, cooldown 1s.
    /// 3. Teleport: Tecla Q, teletransporte instantÃ¡neo al mouse, daÃ±o 400 + knockback en origen y destino, cooldown 5s.
    /// 4. Rayo ElÃ©ctrico (Ultimate): Tecla R, se carga por 2s, luego dispara rayo extendido daÃ±ando por tick, cooldown 30s.
    /// </summary>
    public class PersonajeElectrico : Personaje
    {
        public override TipoPersonaje TipoPj => TipoPersonaje.Electrico;
        [Header("Espada ElÃ©ctrica")]
        public GameObject visualEspada;
        public float duracionEspada = 0.2f;
        public int danoEspada = 70;
        public float rangoEspada = 2.5f;

        [Header("Dash")]
        public float velocidadDash = 75f; // ~2500 px/s en Godot
        public float distanciaMaximaDash = 9.5f; // ~300 px
        public int danoDash = 200;
        public float cooldownDash = 1.0f;

        [Header("Teleport")]
        public int danoTeleport = 400;
        public float radioTeleport = 3.5f;
        public float cooldownTeleport = 5.0f;
        public GameObject prefabEfectoTeleport;

        [Header("Rayo Eléctrico (Ultimate)")]
        public float tiempoCargaMaximo = 4.0f;
        public float cooldownRayo = 30.0f;
        public int danoRayo = 15;
        public float distanciaMaximaRayo = 18.0f; // Alcance medio-largo limitado para no atravesar infinito
        public float duracionRayoActivo = 2.2f;
        public LineRenderer visualRayoElectrico;
        public LineRenderer visualCargaRayo;

        // Estados sincronizados
        public NetworkVariable<bool> DashActivo = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> RayoCargando = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> RayoLanzado = new NetworkVariable<bool>(false);
        public NetworkVariable<float> EscalaCargaRayo = new NetworkVariable<float>(1.0f);

        public float cooldownEspada = 0.25f;

        // Timers locales UI
        private float _cdEspadaRestante = 0f;
        private float _cdDashRestante = 0f;
        private float _cdTeleportRestante = 0f;
        private float _cdRayoRestante = 0f;
        private bool _cargandoLocal = false;
        private float _tiempoCargaLocal = 0f;

        // Variables de ejecución en servidor
        private Vector2 _dirDash;
        private float _distanciaRecorridaDash = 0f;
        private float _tiempoCargaActual = 0f;
        private float _timerRayoEntorno = 0f;
        private float _longitudRayoActual = 0f;
        private float _timerTickRayo = 0f;
        private float _tiempoRayoActivo = 0f;

        // Efectos audiovisuales sincronizados
        private AudioSource _fuenteAudioCarga;
        private AudioSource _fuenteAudioDisparo;
        private float _timerAfterImageDash = 0f;

        private static readonly RaycastHit2D[] _rayHitsBuffer = new RaycastHit2D[32];
        private static readonly Collider2D[] _colHitsBuffer = new Collider2D[32];

        protected override float ObtenerVelocidadCorrer() => velocidadBase * 2.0f; // Correr = 800 px/s

        private void AsegurarFuentesAudio()
        {
            if (_fuenteAudioCarga == null)
            {
                _fuenteAudioCarga = gameObject.AddComponent<AudioSource>();
                _fuenteAudioCarga.loop = true;
                _fuenteAudioCarga.playOnAwake = false;
                _fuenteAudioCarga.spatialBlend = 0f;
            }
            if (_fuenteAudioDisparo == null)
            {
                _fuenteAudioDisparo = gameObject.AddComponent<AudioSource>();
                _fuenteAudioDisparo.loop = true;
                _fuenteAudioDisparo.playOnAwake = false;
                _fuenteAudioDisparo.spatialBlend = 0f;
            }
        }

        private void IniciarAudioCarga()
        {
            AsegurarFuentesAudio();
            var clip = GestorAudio.GetClipSFX(TipoSFX.CargandoUltiRayo);
            if (clip != null)
            {
                _fuenteAudioCarga.clip = clip;
                _fuenteAudioCarga.volume = (GestorAudio.Instancia != null ? GestorAudio.Instancia.VolumenMaster * GestorAudio.Instancia.VolumenSFX : 0.85f);
                if (!_fuenteAudioCarga.isPlaying) _fuenteAudioCarga.Play();
            }
        }

        private void DetenerAudioCarga()
        {
            if (_fuenteAudioCarga != null && _fuenteAudioCarga.isPlaying)
            {
                _fuenteAudioCarga.Stop();
            }
            GestorAudio.DetenerSFXLoop();
        }

        private void IniciarAudioDisparo()
        {
            AsegurarFuentesAudio();
            DetenerAudioCarga();
            var clip = GestorAudio.GetClipSFX(TipoSFX.LanzandoUltiRayo);
            if (clip != null)
            {
                _fuenteAudioDisparo.clip = clip;
                _fuenteAudioDisparo.volume = (GestorAudio.Instancia != null ? GestorAudio.Instancia.VolumenMaster * GestorAudio.Instancia.VolumenSFX : 0.85f) * 1.35f;
                _fuenteAudioDisparo.Play();
            }
        }

        private void DetenerAudioDisparo()
        {
            if (_fuenteAudioDisparo != null && _fuenteAudioDisparo.isPlaying)
            {
                _fuenteAudioDisparo.Stop();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            AsegurarFuentesAudio();

            RayoCargando.OnValueChanged += (prev, act) => {
                if (visualCargaRayo) visualCargaRayo.enabled = act;
                if (act)
                {
                    IniciarAudioCarga();
                }
                else
                {
                    DetenerAudioCarga();
                }
            };
            RayoLanzado.OnValueChanged += (prev, act) => {
                if (visualRayoElectrico) visualRayoElectrico.enabled = act;
                if (act)
                {
                    IniciarAudioDisparo();
                    if (CamaraSeguir.Instancia != null)
                    {
                        CamaraSeguir.Instancia.Sacudir(0.45f + EscalaCargaRayo.Value * 0.12f, 0.45f);
                    }
                }
                else
                {
                    DetenerAudioDisparo();
                }
            };
            DashActivo.OnValueChanged += (prev, act) => {
                if (act)
                {
                    GestorAudio.PlaySFX(TipoSFX.DashElectrico, 1.25f);
                    CrearArcoRayoDash(_dirDash);
                }
            };
        }

        protected override void Update()
        {
            if (EstaMuerto.Value)
            {
                if (IsServer)
                {
                    if (RayoCargando.Value) RayoCargando.Value = false;
                    if (RayoLanzado.Value) RayoLanzado.Value = false;
                    if (DashActivo.Value) DashActivo.Value = false;
                }
                if (visualCargaRayo != null && visualCargaRayo.enabled) visualCargaRayo.enabled = false;
                if (visualRayoElectrico != null && visualRayoElectrico.enabled) visualRayoElectrico.enabled = false;
                return;
            }

            base.Update();

            // Animación cinematográfica de velocidad para el Dash Eléctrico (After-images + Arcos Zenitsu)
            if (DashActivo.Value)
            {
                _timerAfterImageDash -= Time.deltaTime;
                if (_timerAfterImageDash <= 0f)
                {
                    _timerAfterImageDash = 0.032f;
                    CrearAfterImageDash();
                    CrearArcoRayoDash(_dirDash);
                }
            }

            if (EsControlLocal)
            {
                if (_cdEspadaRestante > 0) _cdEspadaRestante -= Time.deltaTime;
                if (_cdDashRestante > 0) _cdDashRestante -= Time.deltaTime;
                if (_cdTeleportRestante > 0) _cdTeleportRestante -= Time.deltaTime;
                if (_cdRayoRestante > 0) _cdRayoRestante -= Time.deltaTime;

                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ActualizarCooldown(0, _cdEspadaRestante, cooldownEspada); // LMB (Espada)
                    HUDJugador.Instancia.ActualizarCooldown(1, _cdDashRestante, cooldownDash);       // E
                    HUDJugador.Instancia.ActualizarCooldown(2, _cdTeleportRestante, cooldownTeleport); // Q
                    HUDJugador.Instancia.ActualizarCooldown(3, _cdRayoRestante, cooldownRayo);       // R
                }
            }

            // Animación visual de Carga Energética (rayos eléctricos orbitando al personaje)
            if (RayoCargando.Value && visualCargaRayo != null)
            {
                visualCargaRayo.enabled = true;
                const int pts = 14;
                if (visualCargaRayo.positionCount != pts) visualCargaRayo.positionCount = pts;

                float tiempoActual = IsOwner ? _tiempoCargaLocal : _tiempoCargaActual;
                float progreso = Mathf.Clamp01(tiempoActual / tiempoCargaMaximo);
                float radio = 1.35f + progreso * 0.9f + Mathf.Sin(Time.time * 26f) * 0.25f;

                for (int i = 0; i < pts; i++)
                {
                    float ang = (float)i / pts * Mathf.PI * 2f + Time.time * (18f + progreso * 20f);
                    float r = radio + (Mathf.PerlinNoise(i * 1.7f, Time.time * 24f) - 0.5f) * (0.45f + progreso * 0.35f);
                    Vector3 p = transform.position + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, -0.3f);
                    visualCargaRayo.SetPosition(i, p);
                }
            }
            else if (!RayoCargando.Value && visualCargaRayo != null)
            {
                visualCargaRayo.enabled = false;
            }

            // Lógica de carga y disparo del rayo en servidor
            if (IsServer)
            {
                if (RayoCargando.Value)
                {
                    _tiempoCargaActual += Time.deltaTime;
                    _timerRayoEntorno -= Time.deltaTime;
                    if (_timerRayoEntorno <= 0f)
                    {
                        _timerRayoEntorno = 0.28f;
                        Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.4f, 4.4f);
                        Vector2 strikePos = (Vector2)transform.position + offset;
                        SpawnRayoEntornoClientRpc(strikePos);

                        int count = Physics2D.OverlapCircleNonAlloc(strikePos, 2.2f, _colHitsBuffer);
                        for (int i = 0; i < count; i++)
                        {
                            var col = _colHitsBuffer[i];
                            if (col == null || col.gameObject == gameObject) continue;
                            var enemigo = col.GetComponent<EnemigoBase>();
                            if (enemigo != null)
                            {
                                int danoEscalado = Mathf.RoundToInt(75 * MultiplicadorDanoTotal);
                                enemigo.RecibirDano(danoEscalado, new Color(1f, 0.95f, 0.2f));
                                enemigo.AplicarKnockback(((Vector2)enemigo.transform.position - strikePos).normalized, 320f);
                            }
                        }
                    }

                    if (_tiempoCargaActual >= tiempoCargaMaximo)
                    {
                        LanzarRayoServer(tiempoCargaMaximo);
                    }
                }

                if (RayoLanzado.Value)
                {
                    _longitudRayoActual = Mathf.Min(distanciaMaximaRayo, _longitudRayoActual + 35f * Time.deltaTime);
                    _tiempoRayoActivo += Time.deltaTime;
                    _timerTickRayo -= Time.deltaTime;

                    if (_timerTickRayo <= 0f)
                    {
                        _timerTickRayo = 0.05f;
                        AplicarDanoRayoServer();
                    }

                    if (_tiempoRayoActivo >= duracionRayoActivo)
                    {
                        DetenerRayoServer();
                    }
                }
            }

            // Animación visual del rayo eléctrico (rayo crepitante con relámpagos vivos que atraviesa todo)
            if (RayoLanzado.Value && visualRayoElectrico != null)
            {
                visualRayoElectrico.enabled = true;
                float escala = Mathf.Max(1.0f, EscalaCargaRayo.Value);
                visualRayoElectrico.startWidth = 0.50f * escala;
                visualRayoElectrico.endWidth = 0.85f * escala;

                Vector2 origen = transform.position;
                Vector2 dir = IsOwner ? ((Vector2)Controles.PosicionMouseMundo() - origen).normalized : DireccionApuntado.Value;
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

                const int segs = 18;
                if (visualRayoElectrico.positionCount != segs) visualRayoElectrico.positionCount = segs;

                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 destino = origen + dir * _longitudRayoActual;

                for (int i = 0; i < segs; i++)
                {
                    float t = (float)i / (segs - 1);
                    Vector2 pt = Vector2.Lerp(origen, destino, t);

                    if (i > 0 && i < segs - 1)
                    {
                        float jitter = (UnityEngine.Random.value - 0.5f) * (0.80f * escala);
                        pt += normal * jitter;
                    }

                    visualRayoElectrico.SetPosition(i, new Vector3(pt.x, pt.y, -0.4f));
                }
            }
            else if (!RayoLanzado.Value && visualRayoElectrico != null)
            {
                visualRayoElectrico.enabled = false;
            }
        }

        protected override void FixedUpdate()
        {
            if (IsServer && DashActivo.Value)
            {
                float pasoDist = velocidadDash * Time.fixedDeltaTime;

                // CircleCast en la dirección del dash para detectar CUALQUIER obstáculo sólido (muros, columnas, props)
                int rayCount = Physics2D.CircleCastNonAlloc(_rb.position, 0.35f, _dirDash, _rayHitsBuffer, pasoDist + 0.35f);
                for (int i = 0; i < rayCount; i++)
                {
                    var rh = _rayHitsBuffer[i];
                    if (rh.collider != null && rh.collider.gameObject != gameObject && EsObstaculoSolido(rh.collider))
                    {
                        // Detener el dash inmediatamente frente al obstáculo sin atravesarlo
                        _rb.position = rh.point - _dirDash * 0.40f;
                        DashActivo.Value = false;
                        _rb.velocity = Vector2.zero;
                        return;
                    }
                }

                Vector2 avance = _dirDash * pasoDist;
                _rb.position += avance;
                _distanciaRecorridaDash += avance.magnitude;

                // Daño en el camino del Dash a los enemigos
                int colCount = Physics2D.OverlapCircleNonAlloc(_rb.position, 0.8f, _colHitsBuffer);
                for (int i = 0; i < colCount; i++)
                {
                    var hit = _colHitsBuffer[i];
                    if (hit == null || hit.gameObject == gameObject) continue;
                    var enemigo = hit.GetComponent<EnemigoBase>();
                    if (enemigo != null)
                    {
                        float multVolt = 1f + (ObtenerNivelMejora(TipoMejoraTienda.Elec_Voltaje) * 0.15f);
                        int danoEscalado = Mathf.RoundToInt(danoDash * MultiplicadorDanoTotal * multVolt);
                        enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    }
                }

                if (_distanciaRecorridaDash >= distanciaMaximaDash)
                {
                    DashActivo.Value = false;
                    _rb.velocity = Vector2.zero;
                }
                return;
            }

            base.FixedUpdate();
        }

        protected override void ProcesarInputsHabilidadesLocales()
        {
            // Espada Eléctrica (Click Izquierdo continuo al mantener presionado)
            if (Controles.PrincipalMantenido && _cdEspadaRestante <= 0)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Elec_Espada, out string motivo))
                {
                    _cdEspadaRestante = cooldownEspada * FactorReduccionCooldown;
                    Vector2 dir = ((Vector2)Controles.PosicionMouseMundo() - (Vector2)transform.position).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                    UsarEspadaServerRpc(dir);
                }
                else if (!string.IsNullOrEmpty(motivo))
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, Controles.PrincipalPresionado);
                }
            }

            // Dash (E) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Habilidad1) && _cdDashRestante <= 0 && !DashActivo.Value)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Elec_Dash, out string motivo))
                {
                    float cdDashEfectivo = Mathf.Max(0.35f, (cooldownDash - (ObtenerNivelMejora(TipoMejoraTienda.Elec_CooldownDash) * 0.15f)) * FactorReduccionCooldown);
                    _cdDashRestante = cdDashEfectivo;
                    Vector2 dir = ((Vector2)Controles.PosicionMouseMundo() - (Vector2)transform.position).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                    IniciarDashServerRpc(dir);
                }
                else
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, Controles.Presionada(Controles.Habilidad1));
                }
            }

            // Teleport (Q) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Habilidad2) && _cdTeleportRestante <= 0)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Elec_Teleport, out string motivo))
                {
                    _cdTeleportRestante = cooldownTeleport * FactorReduccionCooldown;
                    Vector2 origen = transform.position;
                    Vector2 destino = Controles.PosicionMouseMundo();

                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        TeleportServerRpc(destino);
                    }
                    else
                    {
                        EjecutarTeleportLocal(destino);
                    }
                }
                else
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, Controles.Presionada(Controles.Habilidad2));
                }
            }

            // Rayo Eléctrico (R) - Carga hasta 4 segundos manteniendo presionada la tecla R, se dispara al soltar
            if (_cargandoLocal)
            {
                _tiempoCargaLocal += Time.deltaTime;
                // Si el jugador suelta la tecla R o si se alcanza el tiempo máximo de carga (4.0s)
                if (!Controles.Mantenida(Controles.Ultimate) || _tiempoCargaLocal >= tiempoCargaMaximo)
                {
                    _cargandoLocal = false;
                    DetenerAudioCarga();
                    _cdRayoRestante = cooldownRayo * FactorReduccionCooldown;
                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        DispararRayoServerRpc(_tiempoCargaLocal);
                    }
                    else
                    {
                        LanzarRayoServer(_tiempoCargaLocal);
                    }
                }
            }
            else if (Controles.Presionada(Controles.Ultimate) && _cdRayoRestante <= 0 && !RayoCargando.Value && !RayoLanzado.Value)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Elec_Rayo, out string motivo))
                {
                    _cargandoLocal = true;
                    _tiempoCargaLocal = 0f;
                    IniciarAudioCarga();
                    if (NetworkManager != null && NetworkManager.IsListening)
                    {
                        ComenzarCargaRayoServerRpc();
                    }
                    else
                    {
                        RayoCargando.Value = true;
                        _tiempoCargaActual = 0f;
                        _timerRayoEntorno = 0.08f;
                    }
                }
                else
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, true);
                }
            }
        }

        private void CrearAfterImageDash()
        {
            var srOriginal = GetComponent<SpriteRenderer>();
            if (srOriginal == null || srOriginal.sprite == null) return;

            var ghostObj = new GameObject("AfterImageDash");
            ghostObj.transform.position = transform.position;
            ghostObj.transform.rotation = transform.rotation;
            ghostObj.transform.localScale = transform.localScale;

            var sr = ghostObj.AddComponent<SpriteRenderer>();
            sr.sprite = srOriginal.sprite;
            sr.flipX = srOriginal.flipX;
            sr.flipY = srOriginal.flipY;
            sr.material = MaterialUtil.SpritesDefault;
            sr.sortingOrder = Mathf.Max(1, srOriginal.sortingOrder - 1);
            sr.color = new Color(0.25f, 0.90f, 1f, 0.75f); // Azul eléctrico / Cian brillante

            StartCoroutine(RutinaFadeAfterImage(sr, ghostObj));
        }

        private IEnumerator RutinaFadeAfterImage(SpriteRenderer sr, GameObject go)
        {
            float dur = 0.28f;
            float t = 0f;
            Color baseCol = sr != null ? sr.color : Color.white;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                if (sr != null)
                {
                    sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(baseCol.a, 0f, p));
                }
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private void CrearArcoRayoDash(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
            var boltObj = new GameObject("ArcoRayoDash");
            boltObj.transform.position = transform.position;
            var lr = boltObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.useWorldSpace = true;
            lr.sortingOrder = 35;
            lr.startWidth = 0.26f;
            lr.endWidth = 0.08f;
            lr.startColor = Color.white;
            lr.endColor = new Color(1f, 0.95f, 0.25f, 0.85f);

            const int pts = 6;
            lr.positionCount = pts;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector3 inicio = transform.position - (Vector3)(dir * 0.9f);
            Vector3 fin = transform.position + (Vector3)(dir * 0.7f);

            for (int i = 0; i < pts; i++)
            {
                float frac = (float)i / (pts - 1);
                Vector3 pt = Vector3.Lerp(inicio, fin, frac);
                if (i > 0 && i < pts - 1)
                {
                    float jitter = (UnityEngine.Random.value - 0.5f) * 0.75f;
                    pt += (Vector3)(normal * jitter);
                }
                lr.SetPosition(i, new Vector3(pt.x, pt.y, -0.2f));
            }

            StartCoroutine(RutinaFadeArcoDash(lr, boltObj));
        }

        private IEnumerator RutinaFadeArcoDash(LineRenderer lr, GameObject go)
        {
            float dur = 0.18f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (t / dur));
                if (lr != null)
                {
                    lr.startColor = new Color(1f, 1f, 1f, alpha);
                    lr.endColor = new Color(1f, 0.95f, 0.25f, alpha * 0.85f);
                }
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        [ServerRpc(RequireOwnership = false)]
        private void UsarEspadaServerRpc(Vector2 dir, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            Vector2 centroCorte = (Vector2)transform.position + dir * 1.5f;
            int count = Physics2D.OverlapCircleNonAlloc(centroCorte, 1.8f, _colHitsBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = _colHitsBuffer[i];
                if (col == null || col.gameObject == gameObject) continue;
                var enemigo = col.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    float multVolt = 1f + (ObtenerNivelMejora(TipoMejoraTienda.Elec_Voltaje) * 0.15f);
                    int danoEscalado = Mathf.RoundToInt(danoEspada * MultiplicadorDanoTotal * multVolt);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    enemigo.AplicarKnockback(dir, 350f);
                }
            }

            MostrarEspadaClientRpc(dir);
        }

        [ClientRpc]
        private void MostrarEspadaClientRpc(Vector2 dir)
        {
            GestorAudio.PlaySFX(TipoSFX.Espada);
            if (visualEspada != null)
            {
                StopCoroutine("RutinaAnimacionEspada");
                StartCoroutine(RutinaAnimacionEspada(dir));
            }
        }

        private IEnumerator RutinaAnimacionEspada(Vector2 dir)
        {
            visualEspada.SetActive(true);
            var lr = visualEspada.GetComponent<LineRenderer>();
            float anguloBase = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float tiempoTotal = duracionEspada;
            float transcurrido = 0f;

            while (transcurrido < tiempoTotal)
            {
                transcurrido += Time.deltaTime;
                float t = transcurrido / tiempoTotal;

                // Animación de barrido en arco de 110 grados (-55° a +55°)
                float anguloBarrido = Mathf.Lerp(-55f, 55f, t);
                visualEspada.transform.rotation = Quaternion.Euler(0, 0, anguloBase + anguloBarrido);

                // Dibujar arco de hoja eléctrica si tiene LineRenderer
                if (lr != null)
                {
                    const int pts = 8;
                    if (lr.positionCount != pts) lr.positionCount = pts;
                    for (int i = 0; i < pts; i++)
                    {
                        float frac = (float)i / (pts - 1);
                        float distHoja = Mathf.Lerp(0.35f, rangoEspada, frac);
                        float curvatura = Mathf.Sin(frac * Mathf.PI) * 0.45f;
                        Vector3 localPt = new Vector3(distHoja, curvatura, -0.3f);
                        lr.SetPosition(i, visualEspada.transform.position + visualEspada.transform.rotation * localPt);
                    }
                }

                yield return null;
            }

            visualEspada.SetActive(false);
        }

        [ServerRpc(RequireOwnership = false)]
        private void IniciarDashServerRpc(Vector2 dir, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
            _dirDash = dir;
            DashActivo.Value = true;
            _distanciaRecorridaDash = 0f;
            // No deshabilitamos _col para evitar atravesar o incrustarse en obstáculos sólidos
        }

        [ServerRpc(RequireOwnership = false)]
        private void TeleportServerRpc(Vector2 targetPos, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            Vector2 origen = transform.position;
            Vector2 delta = targetPos - origen;
            float dist = delta.magnitude;

            if (dist > 0.05f)
            {
                Vector2 dir = delta.normalized;
                // Raycast a lo largo del trayecto de teleportación para no atravesar muros u obstáculos
                int rayCount = Physics2D.RaycastNonAlloc(origen, dir, _rayHitsBuffer, dist);
                for (int i = 0; i < rayCount; i++)
                {
                    var hm = _rayHitsBuffer[i];
                    if (hm.collider != null && hm.collider.gameObject != gameObject && EsObstaculoSolido(hm.collider))
                    {
                        // Detener la teleportación justo antes del muro u obstáculo
                        targetPos = hm.point - dir * 0.55f;
                        break;
                    }
                }
            }

            // Validar que el destino no quede empotrado dentro de un obstáculo del mapa
            int puntoCount = Physics2D.OverlapCircleNonAlloc(targetPos, 0.4f, _colHitsBuffer);
            for (int i = 0; i < puntoCount; i++)
            {
                var ph = _colHitsBuffer[i];
                if (ph != null && ph.gameObject != gameObject && EsObstaculoSolido(ph))
                {
                    // Si el destino quedó dentro del obstáculo, cancelar teleport
                    return;
                }
            }

            AplicarImpactoTeleport(origen);
            transform.position = targetPos;
            _rb.position = targetPos;
            AplicarImpactoTeleport(targetPos);

            MostrarTeleportEfectoClientRpc(origen, targetPos);
        }

        private bool EsObstaculoSolido(Collider2D col)
        {
            if (col == null || col.isTrigger) return false;
            if (col.gameObject == gameObject) return false;
            if (col.GetComponent<Personaje>() != null) return false;
            if (col.GetComponent<EnemigoBase>() != null) return false;
            if (col.GetComponent<ItemMejoraRun>() != null) return false;
            if (col.name.IndexOf("portal", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;

            // Bloquear ante cualquier obstáculo del mapa: muros, columnas, props y paredes
            return true;
        }

        private void AplicarImpactoTeleport(Vector2 pos)
        {
            float radioEfectivo = radioTeleport * (1f + (ObtenerNivelMejora(TipoMejoraTienda.Elec_AreaTeleport) * 0.20f));
            int count = Physics2D.OverlapCircleNonAlloc(pos, radioEfectivo, _colHitsBuffer);
            for (int i = 0; i < count; i++)
            {
                var hit = _colHitsBuffer[i];
                if (hit == null || hit.gameObject == gameObject) continue;
                var enemigo = hit.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    float multVolt = 1f + (ObtenerNivelMejora(TipoMejoraTienda.Elec_Voltaje) * 0.15f);
                    int danoEscalado = Mathf.RoundToInt(danoTeleport * MultiplicadorDanoTotal * multVolt);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    Vector2 dirKnock = ((Vector2)enemigo.transform.position - pos).normalized;
                    enemigo.AplicarKnockback(dirKnock, 1000f);
                }
            }
        }

        [ClientRpc]
        private void MostrarTeleportEfectoClientRpc(Vector2 origen, Vector2 destino)
        {
            GestorAudio.PlaySFX(TipoSFX.Teleport, 1.2f);

            // 2 Rayos celestiales de alto impacto ("toda god"):
            // 1 donde estaba el jugador (origen) y 1 donde apunta el cursor/reaparece (destino)
            EfectoRayoTeleport.Crear(origen);
            EfectoRayoTeleport.Crear(destino);

            if (prefabEfectoTeleport != null)
            {
                Instantiate(prefabEfectoTeleport, origen, Quaternion.identity);
                Instantiate(prefabEfectoTeleport, destino, Quaternion.identity);
            }

            if (CamaraSeguir.Instancia != null)
            {
                CamaraSeguir.Instancia.Sacudir(0.35f, 0.42f);
            }
        }

        private void EjecutarTeleportLocal(Vector2 targetPos)
        {
            Vector2 origen = transform.position;
            Vector2 delta = targetPos - origen;
            float dist = delta.magnitude;

            if (dist > 0.05f)
            {
                Vector2 dir = delta.normalized;
                int rayCount = Physics2D.RaycastNonAlloc(origen, dir, _rayHitsBuffer, dist);
                for (int i = 0; i < rayCount; i++)
                {
                    var hm = _rayHitsBuffer[i];
                    if (hm.collider != null && hm.collider.gameObject != gameObject && EsObstaculoSolido(hm.collider))
                    {
                        targetPos = hm.point - dir * 0.55f;
                        break;
                    }
                }
            }

            int puntoCount = Physics2D.OverlapCircleNonAlloc(targetPos, 0.4f, _colHitsBuffer);
            for (int i = 0; i < puntoCount; i++)
            {
                var ph = _colHitsBuffer[i];
                if (ph != null && ph.gameObject != gameObject && EsObstaculoSolido(ph))
                {
                    return;
                }
            }

            AplicarImpactoTeleport(origen);
            transform.position = targetPos;
            if (_rb != null) _rb.position = targetPos;
            AplicarImpactoTeleport(targetPos);

            GestorAudio.PlaySFX(TipoSFX.Teleport, 1.2f);
            EfectoRayoTeleport.Crear(origen);
            EfectoRayoTeleport.Crear(targetPos);

            if (CamaraSeguir.Instancia != null)
            {
                CamaraSeguir.Instancia.Sacudir(0.35f, 0.42f);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ComenzarCargaRayoServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            RayoCargando.Value = true;
            _tiempoCargaActual = 0f;
            _timerRayoEntorno = 0.08f;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DispararRayoServerRpc(float tiempoCarga, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            LanzarRayoServer(tiempoCarga);
        }

        private void LanzarRayoServer(float tiempoCarga)
        {
            RayoCargando.Value = false;
            RayoLanzado.Value = true;
            float ratio = Mathf.Clamp01(tiempoCarga / tiempoCargaMaximo);
            EscalaCargaRayo.Value = Mathf.Lerp(1.0f, 3.5f, ratio);
            _longitudRayoActual = 0f;
            _tiempoRayoActivo = 0f;
            _timerTickRayo = 0f;
        }

        [ClientRpc]
        private void SpawnRayoEntornoClientRpc(Vector2 pos)
        {
            EfectoRayoTeleport.Crear(pos);
            GestorAudio.PlaySFX(TipoSFX.RayoTP, 0.75f, 0.12f);
            if (CamaraSeguir.Instancia != null)
            {
                CamaraSeguir.Instancia.Sacudir(0.12f, 0.15f);
            }
        }

        private void AplicarDanoRayoServer()
        {
            Vector2 dir = DireccionApuntado.Value;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            float escala = Mathf.Max(1.0f, EscalaCargaRayo.Value);
            float radioGolpe = 1.1f * escala;

            // Atraviesa todo (muros, obstáculos y enemigos) hasta la distancia máxima
            int count = Physics2D.CircleCastNonAlloc(transform.position, radioGolpe, dir, _rayHitsBuffer, _longitudRayoActual);
            for (int i = 0; i < count; i++)
            {
                var hit = _rayHitsBuffer[i];
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                var enemigo = hit.collider.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    float multiCarga = Mathf.Lerp(1.0f, 3.8f, (escala - 1f) / 2.5f);
                    int danoEscalado = Mathf.RoundToInt(danoRayo * MultiplicadorDanoTotal * multiCarga);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    enemigo.AplicarKnockback(dir, 380f * escala);
                }
            }
        }

        private void DetenerRayoServer()
        {
            RayoLanzado.Value = false;
            _longitudRayoActual = 0f;
        }

        public override void ReiniciarCooldowns()
        {
            _cdDashRestante = 0f;
            _cdTeleportRestante = 0f;
            _cdRayoRestante = 0f;
            _cargandoLocal = false;
            _tiempoCargaLocal = 0f;
            DetenerAudioCarga();
            DetenerAudioDisparo();

            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.ActualizarCooldown(0, 0f, 1f);
                HUDJugador.Instancia.ActualizarCooldown(1, 0f, cooldownDash);
                HUDJugador.Instancia.ActualizarCooldown(2, 0f, cooldownTeleport);
                HUDJugador.Instancia.ActualizarCooldown(3, 0f, cooldownRayo);
            }
        }

        public override void LimpiarEstadosYHabilidades()
        {
            base.LimpiarEstadosYHabilidades();

            if (IsServer)
            {
                StopAllCoroutines();
                DashActivo.Value = false;
                RayoCargando.Value = false;
                RayoLanzado.Value = false;
                EscalaCargaRayo.Value = 1.0f;
                _col.enabled = true;
                _rb.velocity = Vector2.zero;
            }

            _cargandoLocal = false;
            _tiempoCargaLocal = 0f;
            _tiempoCargaActual = 0f;
            _longitudRayoActual = 0f;
            _distanciaRecorridaDash = 0f;

            DetenerAudioCarga();
            DetenerAudioDisparo();

            if (visualCargaRayo != null)
            {
                visualCargaRayo.enabled = false;
            }
            if (visualRayoElectrico != null)
            {
                visualRayoElectrico.enabled = false;
            }
            if (visualEspada != null)
            {
                visualEspada.SetActive(false);
            }
        }
    }
}

