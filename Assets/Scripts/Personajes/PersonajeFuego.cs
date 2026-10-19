using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Personaje de Fuego (PersonajeFuego.gd en Godot).
    /// Habilidades:
    /// 1. Bola de Fuego: Click izquierdo / disparo estándar, proyectil recto con quemadura.
    /// 2. Cabeza de Fuego (Lanzallamas fluido): Modo alterno (tecla E), chorro continuo que se deforma y curva como cuerda/látigo al girar.
    /// 3. Armadura de Fuego: Tecla Q, 80% reducción de daño por 10s, cooldown 20s.
    /// </summary>
    public class PersonajeFuego : Personaje
    {
        public override TipoPersonaje TipoPj => TipoPersonaje.Fuego;
        [Header("Referencias Prefabs / Visuales")]
        public GameObject prefabBolaFuego;
        public GameObject visualArmaduraFuego;
        public LineRenderer rayoCabezaFuego;

        [Header("Tiempos y Cooldowns")]
        public float cooldownBolaFuego = 1.0f;
        public int danoBolaFuego = 40;

        public float cooldownCabezaFuego = 20.0f;
        public float duracionCabezaFuego = 5.0f;
        public int danoCabezaFuego = 10;

        public float cooldownArmadura = 20.0f;
        public float duracionArmadura = 10.0f;
        public float reduccionArmadura = 0.8f;

        [Header("Ultimate - Supernova")]
        public float cooldownSupernova = 35.0f;
        public float duracionSupernova = 8.0f;
        public int danoSupernovaExplosion = 85;

        // Estados sincronizados
        public NetworkVariable<bool> ArmaduraActiva = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> CabezaFuegoActiva = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> DisparandoCabezaFuego = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> SupernovaActiva = new NetworkVariable<bool>(false);

        // Cooldowns locales para la UI
        private float _cdBolaFuegoRestante = 0f;
        private float _cdCabezaFuegoRestante = 0f;
        private float _cdArmaduraRestante = 0f;
        private float _cdSupernovaRestante = 0f;

        // Control de efectos y rastro de fuego
        private float _timerRastroFuego = 0f;
        private Vector2 _ultimaPosRastroFuego = Vector2.zero;
        private GameObject _auraSupernova;

        // Lógica de daño continuo para cabeza de fuego
        private float _tickTimerCabeza = 0f;

        // Nodos para la simulación física de látigo/cuerda fluida del lanzallamas
        private const int NUM_NODOS_LLAMA = 16;
        private Vector2[] _posicionesLlamas = new Vector2[NUM_NODOS_LLAMA];
        private Vector2[] _velocidadesLlamas = new Vector2[NUM_NODOS_LLAMA];
        private bool _flamallamaIniciada = false;

        // Visuales mejorados del Lanzallamas (núcleo incandescente + lenguas de fuego animadas)
        private LineRenderer _nucleoLanzallamas;
        private readonly List<SpriteRenderer> _lenguasFuego = new List<SpriteRenderer>();
        private static Sprite _spriteLenguaFuego;
        private AudioSource _fuenteAudioLanzallamas;

        private void AsegurarVisualesLanzallamas()
        {
            if (_spriteLenguaFuego == null)
            {
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                        if (d <= 3.2f)
                            tex.SetPixel(x, y, new Color(1f, 0.96f, 0.65f, 0.95f));
                        else if (d <= 5.5f)
                            tex.SetPixel(x, y, new Color(1f, 0.58f, 0.12f, 0.85f));
                        else if (d <= 7.2f)
                            tex.SetPixel(x, y, new Color(0.95f, 0.22f, 0.05f, 0.55f));
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                _spriteLenguaFuego = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            }

            if (_nucleoLanzallamas == null && rayoCabezaFuego != null)
            {
                var goNucleo = new GameObject("NucleoLanzallamas");
                goNucleo.transform.SetParent(transform, false);
                _nucleoLanzallamas = goNucleo.AddComponent<LineRenderer>();
                Material matFuego = (rayoCabezaFuego != null && rayoCabezaFuego.sharedMaterial != null)
                    ? rayoCabezaFuego.sharedMaterial
                    : MaterialUtil.SpritesDefault;
                if (rayoCabezaFuego != null && rayoCabezaFuego.sharedMaterial == null) rayoCabezaFuego.sharedMaterial = matFuego;
                _nucleoLanzallamas.sharedMaterial = matFuego;
                _nucleoLanzallamas.sortingOrder = rayoCabezaFuego.sortingOrder + 1;
                _nucleoLanzallamas.numCapVertices = 4;
                _nucleoLanzallamas.numCornerVertices = 4;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                        new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0.55f),
                        new GradientColorKey(new Color(1f, 0.45f, 0.08f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.95f, 0f),
                        new GradientAlphaKey(0.85f, 0.6f),
                        new GradientAlphaKey(0.0f, 1f)
                    }
                );
                _nucleoLanzallamas.colorGradient = grad;
                _nucleoLanzallamas.enabled = false;
            }

            if (_lenguasFuego.Count == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    var goL = new GameObject($"LenguaFuego_{i}");
                    goL.transform.SetParent(transform, false);
                    var sr = goL.AddComponent<SpriteRenderer>();
                    sr.sprite = _spriteLenguaFuego;
                    sr.sortingOrder = (rayoCabezaFuego != null ? rayoCabezaFuego.sortingOrder : 7) + 2;
                    goL.SetActive(false);
                    _lenguasFuego.Add(sr);
                }
            }
        }

        private void OcultarVisualesExtraLanzallamas()
        {
            if (_nucleoLanzallamas != null && _nucleoLanzallamas.enabled)
                _nucleoLanzallamas.enabled = false;
            for (int i = 0; i < _lenguasFuego.Count; i++)
            {
                if (_lenguasFuego[i] != null && _lenguasFuego[i].gameObject.activeSelf)
                    _lenguasFuego[i].gameObject.SetActive(false);
            }
        }

        private void AsegurarAuraSupernova()
        {
            if (_auraSupernova == null)
            {
                _auraSupernova = new GameObject("AuraSupernova");
                _auraSupernova.transform.SetParent(transform, false);
                _auraSupernova.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                var sr = _auraSupernova.AddComponent<SpriteRenderer>();
                sr.sprite = _spriteLenguaFuego;
                sr.color = new Color(1f, 0.75f, 0.15f, 0.70f);
                sr.sortingOrder = (rayoCabezaFuego != null ? rayoCabezaFuego.sortingOrder : 7) - 1;
                _auraSupernova.transform.localScale = Vector3.one * 1.8f;
                _auraSupernova.SetActive(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ArmaduraActiva.OnValueChanged += (prev, act) => { if (visualArmaduraFuego) visualArmaduraFuego.SetActive(act); };
            DisparandoCabezaFuego.OnValueChanged += (prev, act) =>
            {
                if (rayoCabezaFuego) rayoCabezaFuego.enabled = act;
                if (!act) OcultarVisualesExtraLanzallamas();
            };
            ArmaduraActiva.OnValueChanged += (prev, act) =>
            {
                if (act) GestorAudio.PlaySFX(TipoSFX.Escudo);
            };
            SupernovaActiva.OnValueChanged += (prev, act) =>
            {
                AsegurarAuraSupernova();
                if (_auraSupernova != null) _auraSupernova.SetActive(act);
            };
        }

        private float _tiempoSonidoLlamas = 0f;

        protected override float ObtenerVelocidadCorrer()
        {
            if (SupernovaActiva.Value) return velocidadBase * 1.9f;
            return base.ObtenerVelocidadCorrer();
        }

        protected override void Update()
        {
            if (EstaMuerto.Value)
            {
                DetenerAudioLanzallamasLoop();
                if (IsServer && DisparandoCabezaFuego.Value) DisparandoCabezaFuego.Value = false;
                if (IsServer && CabezaFuegoActiva.Value) CabezaFuegoActiva.Value = false;
                if (IsServer && SupernovaActiva.Value) SupernovaActiva.Value = false;
                _flamallamaIniciada = false;
                if (rayoCabezaFuego != null && rayoCabezaFuego.enabled) rayoCabezaFuego.enabled = false;
                OcultarVisualesExtraLanzallamas();
                if (_auraSupernova != null && _auraSupernova.activeSelf) _auraSupernova.SetActive(false);
                return;
            }

            base.Update();

            // Animación de aura y velocidad durante Supernova
            if (SupernovaActiva.Value)
            {
                _velocidadActual = velocidadBase * 1.4f;
                if (_auraSupernova != null && _auraSupernova.activeSelf)
                {
                    _auraSupernova.transform.Rotate(0f, 0f, 200f * Time.deltaTime);
                    float s = 1.8f + Mathf.Sin(Time.time * 14f) * 0.18f;
                    _auraSupernova.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            // Actualizar timers locales para UI
            if (EsControlLocal)
            {
                if (_cdBolaFuegoRestante > 0) _cdBolaFuegoRestante -= Time.deltaTime;
                if (_cdCabezaFuegoRestante > 0) _cdCabezaFuegoRestante -= Time.deltaTime;
                if (_cdArmaduraRestante > 0) _cdArmaduraRestante -= Time.deltaTime;
                if (_cdSupernovaRestante > 0) _cdSupernovaRestante -= Time.deltaTime;

                if (SupernovaActiva.Value)
                {
                    // Durante Supernova las habilidades no tienen cooldown
                    _cdCabezaFuegoRestante = 0f;
                    _cdArmaduraRestante = 0f;
                }

                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ActualizarCooldown(0, _cdBolaFuegoRestante, cooldownBolaFuego);
                    HUDJugador.Instancia.ActualizarCooldown(1, _cdCabezaFuegoRestante, cooldownCabezaFuego);
                    HUDJugador.Instancia.ActualizarCooldown(2, _cdArmaduraRestante, cooldownArmadura);
                    HUDJugador.Instancia.ActualizarCooldown(3, _cdSupernovaRestante, cooldownSupernova);
                }
            }

            // Generación continua y densa de rastro de fuego ardiente al caminar en Supernova (Servidor)
            if (IsServer && SupernovaActiva.Value)
            {
                _timerRastroFuego -= Time.deltaTime;
                float distDesdeUltimo = Vector2.Distance(transform.position, _ultimaPosRastroFuego);
                if ((_timerRastroFuego <= 0f || distDesdeUltimo > 0.40f) && ObtenerMovimientoEfectivo().sqrMagnitude > 0.04f)
                {
                    _timerRastroFuego = 0.05f;
                    _ultimaPosRastroFuego = transform.position;
                    CrearRastroFuego(transform.position);
                    CrearRastroFuegoClientRpc(transform.position);
                }
            }

            // Actualización visual y sonora del lanzallamas de fuego fluido
            if (DisparandoCabezaFuego.Value && rayoCabezaFuego != null)
            {
                ReproducirAudioLanzallamasLoop();
                ActualizarLanzallamasCurvado();
            }
            else
            {
                DetenerAudioLanzallamasLoop();
                _flamallamaIniciada = false;
                if (rayoCabezaFuego != null && rayoCabezaFuego.enabled)
                {
                    rayoCabezaFuego.enabled = false;
                }
                OcultarVisualesExtraLanzallamas();
            }

            // Aplicación de daño continuo en servidor a lo largo del chorro curvado
            if (IsServer && CabezaFuegoActiva.Value && DisparandoCabezaFuego.Value)
            {
                _tickTimerCabeza -= Time.deltaTime;
                if (_tickTimerCabeza <= 0f)
                {
                    _tickTimerCabeza = 0.14f;
                    EjecutarLanzallamasFuego();
                }
            }
        }

        private void ActualizarLanzallamasCurvado()
        {
            if (rayoCabezaFuego == null) return;
            AsegurarVisualesLanzallamas();
            rayoCabezaFuego.enabled = true;
            if (_nucleoLanzallamas != null) _nucleoLanzallamas.enabled = true;

            Vector2 origen = transform.position;
            Vector2 dirActual = IsOwner ? ((Vector2)Controles.PosicionMouseMundo() - origen).normalized : DireccionApuntado.Value;
            if (dirActual.sqrMagnitude < 0.001f) dirActual = Vector2.right;

            float velocidadFlujo = 19f;
            float pasoDist = 0.54f;

            if (!_flamallamaIniciada)
            {
                _flamallamaIniciada = true;
                for (int i = 0; i < NUM_NODOS_LLAMA; i++)
                {
                    _posicionesLlamas[i] = origen + dirActual * (i * pasoDist);
                    _velocidadesLlamas[i] = dirActual * velocidadFlujo;
                }
            }

            // El nodo 0 siempre está en la boca / origen
            _posicionesLlamas[0] = origen;
            _velocidadesLlamas[0] = dirActual * velocidadFlujo;

            // Cada nodo siguiente avanza con su velocidad emitida y se arrastra con el anterior
            float dt = Time.deltaTime;
            for (int i = 1; i < NUM_NODOS_LLAMA; i++)
            {
                _posicionesLlamas[i] += _velocidadesLlamas[i] * dt;

                Vector2 delta = _posicionesLlamas[i] - _posicionesLlamas[i - 1];
                float dist = delta.magnitude;
                if (dist > pasoDist)
                {
                    _posicionesLlamas[i] = _posicionesLlamas[i - 1] + delta.normalized * pasoDist;
                }

                // Inercia de giro: sigue la curvatura como una cuerda o látigo
                _velocidadesLlamas[i] = Vector2.Lerp(_velocidadesLlamas[i], _velocidadesLlamas[i - 1], dt * 14f);
            }

            // Renderizado en LineRenderer con turbulencia de fuego y expansión en cono
            if (rayoCabezaFuego.positionCount != NUM_NODOS_LLAMA)
                rayoCabezaFuego.positionCount = NUM_NODOS_LLAMA;
            if (_nucleoLanzallamas != null && _nucleoLanzallamas.positionCount != NUM_NODOS_LLAMA)
                _nucleoLanzallamas.positionCount = NUM_NODOS_LLAMA;

            rayoCabezaFuego.startWidth = 0.42f + Mathf.Sin(Time.time * 26f) * 0.08f;
            rayoCabezaFuego.endWidth = 2.85f + Mathf.Sin(Time.time * 20f) * 0.42f;

            if (_nucleoLanzallamas != null)
            {
                _nucleoLanzallamas.startWidth = 0.22f + Mathf.Cos(Time.time * 32f) * 0.05f;
                _nucleoLanzallamas.endWidth = 1.25f + Mathf.Sin(Time.time * 24f) * 0.22f;
            }

            int puntosValidos = NUM_NODOS_LLAMA;
            for (int i = 0; i < NUM_NODOS_LLAMA; i++)
            {
                // Bloqueo por muros sólidos
                if (i > 0)
                {
                    RaycastHit2D hitMuro = Physics2D.Linecast(_posicionesLlamas[i - 1], _posicionesLlamas[i]);
                    if (hitMuro.collider != null && hitMuro.collider.gameObject != gameObject && EsMuro(hitMuro.collider))
                    {
                        puntosValidos = i;
                        break;
                    }
                }

                float t = (float)i / (NUM_NODOS_LLAMA - 1);
                Vector2 pos = _posicionesLlamas[i];

                if (i > 0)
                {
                    Vector2 dirSeg = (_posicionesLlamas[i] - _posicionesLlamas[i - 1]).normalized;
                    Vector2 normal = new Vector2(-dirSeg.y, dirSeg.x);
                    float turbulencia = Mathf.Sin(Time.time * 32f + i * 1.8f) * (t * 0.46f);
                    pos += normal * turbulencia;
                }

                Vector3 p3 = new Vector3(pos.x, pos.y, -0.4f);
                rayoCabezaFuego.SetPosition(i, p3);
                if (_nucleoLanzallamas != null) _nucleoLanzallamas.SetPosition(i, p3);
            }

            for (int i = puntosValidos; i < NUM_NODOS_LLAMA; i++)
            {
                Vector3 pLast = rayoCabezaFuego.GetPosition(Mathf.Max(0, puntosValidos - 1));
                rayoCabezaFuego.SetPosition(i, pLast);
                if (_nucleoLanzallamas != null) _nucleoLanzallamas.SetPosition(i, pLast);
            }

            // Animar lenguas/bolas de fuego viajando por el chorro del lanzallamas
            for (int k = 0; k < _lenguasFuego.Count; k++)
            {
                var srL = _lenguasFuego[k];
                if (srL == null) continue;
                float fase = Mathf.Repeat(Time.time * 2.6f + k * (1f / _lenguasFuego.Count), 1f);
                int idxNodo = Mathf.Clamp(Mathf.RoundToInt(fase * (puntosValidos - 1)), 0, Mathf.Max(0, puntosValidos - 1));
                srL.gameObject.SetActive(puntosValidos > 1);
                Vector3 posNodo = rayoCabezaFuego.GetPosition(idxNodo);
                srL.transform.position = posNodo;
                float escala = Mathf.Lerp(0.55f, 2.3f, fase);
                srL.transform.localScale = new Vector3(escala, escala, 1f);
                srL.color = Color.Lerp(new Color(1f, 0.95f, 0.5f, 0.95f), new Color(1f, 0.3f, 0.05f, 0.35f), fase);
            }
        }

        protected override void ProcesarInputsHabilidadesLocales()
        {
            // --- 0. ATAQUE PRINCIPAL: Click Izquierdo ---
            if (CabezaFuegoActiva.Value)
            {
                // En modo Lanzallamas: durante Supernova permanece continuo; fuera de ella responde al click
                bool disparando = SupernovaActiva.Value || Controles.PrincipalMantenido;
                if (disparando != DisparandoCabezaFuego.Value)
                {
                    SetDisparandoCabezaServerRpc(disparando);
                }
            }
            else
            {
                // En modo Bolas de Fuego:
                if (Controles.PrincipalMantenido && _cdBolaFuegoRestante <= 0)
                {
                    if (PuedeUsarHabilidad(IdHabilidad.Fuego_BolaFuego, out string motivo))
                    {
                        Vector2 dir = ((Vector2)Controles.PosicionMouseMundo() - (Vector2)transform.position).normalized;
                        if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

                        if (SupernovaActiva.Value)
                        {
                            // MODO ESCOPETA TRIPLE DE FUEGO (Cadencia rápida 0.28s)
                            _cdBolaFuegoRestante = 0.28f;
                            GestorAudio.PlaySFX(TipoSFX.FuegoDisparo, 1.15f);
                            LanzarEscopetaFuegoServerRpc(dir);
                        }
                        else
                        {
                            // Disparo estándar
                            _cdBolaFuegoRestante = cooldownBolaFuego;
                            GestorAudio.PlaySFX(TipoSFX.FuegoDisparo, 0.95f);
                            LanzarBolaFuegoServerRpc(dir);
                        }
                    }
                    else if (!string.IsNullOrEmpty(motivo))
                    {
                        NotificarHabilidadBloqueada(motivo, Color.red, Controles.PrincipalPresionado);
                    }
                }
            }

            // --- 1. HABILIDAD 1 (E): Lanzallamas / Cabeza de Fuego ---
            if (SupernovaActiva.Value)
            {
                // En Supernova: sin cooldown, presionar E alterna (toggle) entre Lanzallamas permanente y Escopeta de Bolas de Fuego
                if (Controles.Presionada(Controles.Habilidad1))
                {
                    if (PuedeUsarHabilidad(IdHabilidad.Fuego_Lanzallamas, out string motivoE))
                    {
                        bool activar = !CabezaFuegoActiva.Value;
                        if (activar) GestorAudio.PlaySFX(TipoSFX.FuegoLlamas, 0.8f);
                        ToggleCabezaFuegoSupernovaServerRpc(activar);
                    }
                    else
                    {
                        NotificarHabilidadBloqueada(motivoE, Color.red, true);
                    }
                }
            }
            else
            {
                // Modo normal: Auto-casteo al presionar con cooldown
                if (Controles.AutoDisparo(Controles.Habilidad1) && _cdCabezaFuegoRestante <= 0 && !CabezaFuegoActiva.Value)
                {
                    if (PuedeUsarHabilidad(IdHabilidad.Fuego_Lanzallamas, out string motivo))
                    {
                        _cdCabezaFuegoRestante = cooldownCabezaFuego;
                        GestorAudio.PlaySFX(TipoSFX.FuegoLlamas, 0.8f);
                        ActivarCabezaFuegoServerRpc();
                    }
                    else
                    {
                        NotificarHabilidadBloqueada(motivo, Color.red, Controles.Presionada(Controles.Habilidad1));
                    }
                }
            }

            // --- 2. HABILIDAD 2 (Q): Armadura de Fuego ---
            if (!SupernovaActiva.Value)
            {
                // Fuera de Supernova: comportamiento normal con cooldown
                if (Controles.AutoDisparo(Controles.Habilidad2) && _cdArmaduraRestante <= 0 && !ArmaduraActiva.Value)
                {
                    if (PuedeUsarHabilidad(IdHabilidad.Fuego_Armadura, out string motivo))
                    {
                        _cdArmaduraRestante = cooldownArmadura;
                        GestorAudio.PlaySFX(TipoSFX.Escudo);
                        ActivarArmaduraServerRpc();
                    }
                    else
                    {
                        NotificarHabilidadBloqueada(motivo, Color.red, Controles.Presionada(Controles.Habilidad2));
                    }
                }
            }
            else
            {
                // En Supernova, la armadura ya está activa permanentemente
                _cdArmaduraRestante = 0f;
            }

            // --- 3. ULTIMATE (R): Supernova Felina ---
            if (Controles.AutoDisparo(Controles.Ultimate) && _cdSupernovaRestante <= 0 && !SupernovaActiva.Value)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Fuego_Meteorito, out string motivoUlti))
                {
                    _cdSupernovaRestante = cooldownSupernova;
                    GestorAudio.PlayUltiFuegoCompleta(1.20f);
                    ActivarSupernovaServerRpc();
                }
                else
                {
                    NotificarHabilidadBloqueada(motivoUlti, Color.red, Controles.Presionada(Controles.Ultimate));
                }
            }
        }

        private void InstanciarBolaFuego(Vector2 dir, int dano)
        {
            if (prefabBolaFuego == null) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            GameObject bola = Instantiate(prefabBolaFuego, transform.position, Quaternion.identity);
            var proj = bola.GetComponent<Proyectil>();
            if (proj != null)
            {
                proj.Configurar(dir, dano, OwnerClientId, quemadura: true);
            }
            bola.GetComponent<NetworkObject>().Spawn();
            if (proj != null)
            {
                proj.DireccionSincronizada.Value = dir;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void LanzarBolaFuegoServerRpc(Vector2 dir, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            int danoEscalado = Mathf.RoundToInt(danoBolaFuego * MultiplicadorDanoTotal);
            InstanciarBolaFuego(dir, danoEscalado);
        }

        [ServerRpc(RequireOwnership = false)]
        private void LanzarEscopetaFuegoServerRpc(Vector2 dirPrincipal, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (prefabBolaFuego == null) return;
            if (dirPrincipal.sqrMagnitude < 0.001f) dirPrincipal = Vector2.right;

            int danoEscalado = Mathf.RoundToInt(danoBolaFuego * MultiplicadorDanoTotal);
            Vector2 dirIzq = Quaternion.Euler(0f, 0f, 16f) * dirPrincipal;
            Vector2 dirDer = Quaternion.Euler(0f, 0f, -16f) * dirPrincipal;

            InstanciarBolaFuego(dirPrincipal, danoEscalado);
            InstanciarBolaFuego(dirIzq, danoEscalado);
            InstanciarBolaFuego(dirDer, danoEscalado);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ToggleCabezaFuegoSupernovaServerRpc(bool activar, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            CabezaFuegoActiva.Value = activar;
            DisparandoCabezaFuego.Value = activar;
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivarSupernovaServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            DetonarSupernova();
            StartCoroutine(RutinaSupernova());
        }

        private static readonly Collider2D[] _fireOverlapBuffer = new Collider2D[32];
        private static readonly HashSet<EnemigoBase> _danadosCache = new HashSet<EnemigoBase>();

        private void DetonarSupernova()
        {
            // 1. Onda de choque 360° con daño y knockback
            int danoExp = Mathf.RoundToInt(danoSupernovaExplosion * MultiplicadorDanoTotal);
            int danoQuem = Mathf.RoundToInt(30 * MultiplicadorDanoTotal);
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, 5.0f, _fireOverlapBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                var h = _fireOverlapBuffer[i];
                if (h == null || h.isTrigger || h.gameObject == gameObject) continue;
                var enemigo = h.GetComponent<EnemigoBase>();
                if (enemigo != null && enemigo.VidaActual.Value > 0)
                {
                    enemigo.RecibirDano(danoExp, new Color(1f, 0.40f, 0.02f));
                    enemigo.AplicarQuemadura(danoQuem);
                    Vector2 dirEmpuje = ((Vector2)enemigo.transform.position - (Vector2)transform.position).normalized;
                    if (dirEmpuje.sqrMagnitude > 0.001f)
                    {
                        enemigo.AplicarKnockback(dirEmpuje, 500f);
                    }
                }
            }

            // 2. Disparar 16 bolas de fuego en abanico circular de 360°
            int danoRadial = Mathf.RoundToInt(danoBolaFuego * 0.9f * MultiplicadorDanoTotal);
            for (int i = 0; i < 16; i++)
            {
                float ang = i * (360f / 16f) * Mathf.Deg2Rad;
                Vector2 dirRadial = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                InstanciarBolaFuego(dirRadial, danoRadial);
            }

            // 3. Efectos y shake en todos los clientes
            DetonarSupernovaClientRpc(transform.position);
        }

        [ClientRpc]
        private void DetonarSupernovaClientRpc(Vector3 pos)
        {
            if (!IsOwner)
            {
                GestorAudio.PlayUltiFuegoCompleta(1.15f);
            }
            CamaraSeguir.Instancia?.Sacudir(0.45f, 0.32f);
            TextoFlotante.Crear(pos + Vector3.up * 1.5f, "¡SUPERNOVA!", new Color(1f, 0.82f, 0.15f));

            // Efecto visual expansivo
            var goOnda = new GameObject("OndaSupernovaVisual");
            goOnda.transform.position = pos;
            var sr = goOnda.AddComponent<SpriteRenderer>();
            sr.sprite = _spriteLenguaFuego;
            sr.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            sr.sortingOrder = 8;
            StartCoroutine(RutinaAnimarOnda(goOnda, sr));
        }

        private IEnumerator RutinaAnimarOnda(GameObject goOnda, SpriteRenderer sr)
        {
            float t = 0f;
            float dur = 0.45f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float escala = Mathf.Lerp(0.5f, 9.5f, p);
                if (goOnda != null) goOnda.transform.localScale = new Vector3(escala, escala, 1f);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(0.9f, 0f, p);
                    sr.color = c;
                }
                yield return null;
            }
            if (goOnda != null) Destroy(goOnda);
        }

        private IEnumerator RutinaSupernova()
        {
            SupernovaActiva.Value = true;
            ArmaduraActiva.Value = true;
            ReduccionDano.Value = reduccionArmadura;

            float tiempoRestante = duracionSupernova;
            while (tiempoRestante > 0f)
            {
                if (EstaMuerto.Value) break;
                tiempoRestante -= Time.deltaTime;
                yield return null;
            }

            SupernovaActiva.Value = false;
            CabezaFuegoActiva.Value = false;
            DisparandoCabezaFuego.Value = false;
            ArmaduraActiva.Value = false;
            ReduccionDano.Value = 0f;
            _velocidadActual = velocidadBase;
        }

        private void CrearRastroFuego(Vector3 pos)
        {
            var go = new GameObject("RastroFuego");
            go.transform.position = pos;
            var rf = go.AddComponent<RastroFuego>();
            int danoTick = Mathf.RoundToInt(18 * MultiplicadorDanoTotal);
            rf.Configurar(danoTick, 2.5f);
        }

        [ClientRpc]
        private void CrearRastroFuegoClientRpc(Vector3 pos)
        {
            if (IsServer) return; // Ya creado en servidor
            var go = new GameObject("RastroFuegoVisual");
            go.transform.position = pos;
            var rf = go.AddComponent<RastroFuego>();
            rf.Configurar(0, 2.5f);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivarCabezaFuegoServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            StartCoroutine(RutinaCabezaFuego());
        }

        private IEnumerator RutinaCabezaFuego()
        {
            CabezaFuegoActiva.Value = true;
            _velocidadActual = velocidadBase * 0.7f;
            yield return new WaitForSeconds(duracionCabezaFuego);
            CabezaFuegoActiva.Value = false;
            DisparandoCabezaFuego.Value = false;
            _velocidadActual = velocidadBase;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetDisparandoCabezaServerRpc(bool disparando, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            DisparandoCabezaFuego.Value = disparando;
        }

        private void EjecutarLanzallamasFuego()
        {
            _danadosCache.Clear();

            for (int i = 0; i < NUM_NODOS_LLAMA; i++)
            {
                Vector2 posNodo = _posicionesLlamas[i];
                float radioNodo = 0.5f + (float)i / NUM_NODOS_LLAMA * 0.95f;

                int hitCount = Physics2D.OverlapCircleNonAlloc(posNodo, radioNodo, _fireOverlapBuffer);
                for (int c = 0; c < hitCount; c++)
                {
                    var col = _fireOverlapBuffer[c];
                    if (col == null || col.gameObject == gameObject) continue;
                    var enemigo = col.GetComponent<EnemigoBase>();
                    if (enemigo != null && _danadosCache.Add(enemigo))
                    {
                        int danoEscalado = Mathf.RoundToInt(danoCabezaFuego * MultiplicadorDanoTotal);
                        enemigo.RecibirDano(danoEscalado, new Color(0.96f, 0.40f, 0.05f, 1f)); // Naranja oscuro
                        int danoQuemaduraEscalado = Mathf.RoundToInt(18 * MultiplicadorDanoTotal);
                        enemigo.AplicarQuemadura(danoQuemaduraEscalado);
                    }
                }
            }
        }

        private bool EsMuro(Collider2D col)
        {
            if (col == null || col.isTrigger) return false;
            bool esPared = false;
            try { esPared = col.CompareTag("Pared"); } catch { esPared = false; }
            return esPared ||
                   col.name.IndexOf("pared", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   col.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   col.name.IndexOf("columna", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivarArmaduraServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            StartCoroutine(RutinaArmadura());
        }

        private IEnumerator RutinaArmadura()
        {
            ArmaduraActiva.Value = true;
            ReduccionDano.Value = reduccionArmadura;
            yield return new WaitForSeconds(duracionArmadura);
            ArmaduraActiva.Value = false;
            ReduccionDano.Value = 0f;
        }

        public override void ReiniciarCooldowns()
        {
            _cdBolaFuegoRestante = 0f;
            _cdCabezaFuegoRestante = 0f;
            _cdArmaduraRestante = 0f;
            _cdSupernovaRestante = 0f;

            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.ActualizarCooldown(0, 0f, cooldownBolaFuego);
                HUDJugador.Instancia.ActualizarCooldown(1, 0f, cooldownCabezaFuego);
                HUDJugador.Instancia.ActualizarCooldown(2, 0f, cooldownArmadura);
                HUDJugador.Instancia.ActualizarCooldown(3, 0f, cooldownSupernova);
            }
        }

        public override void LimpiarEstadosYHabilidades()
        {
            base.LimpiarEstadosYHabilidades();
            DetenerAudioLanzallamasLoop();

            if (IsServer)
            {
                StopAllCoroutines();
                ArmaduraActiva.Value = false;
                CabezaFuegoActiva.Value = false;
                DisparandoCabezaFuego.Value = false;
                SupernovaActiva.Value = false;
                ReduccionDano.Value = 0f;
            }

            _velocidadActual = velocidadBase;
            _flamallamaIniciada = false;
            if (rayoCabezaFuego != null)
            {
                rayoCabezaFuego.enabled = false;
            }
            OcultarVisualesExtraLanzallamas();
            if (visualArmaduraFuego != null)
            {
                visualArmaduraFuego.SetActive(false);
            }
            if (_auraSupernova != null)
            {
                _auraSupernova.SetActive(false);
            }
        }

        private void ReproducirAudioLanzallamasLoop()
        {
            if (_fuenteAudioLanzallamas == null)
            {
                _fuenteAudioLanzallamas = gameObject.AddComponent<AudioSource>();
                _fuenteAudioLanzallamas.loop = true;
                _fuenteAudioLanzallamas.playOnAwake = false;
                _fuenteAudioLanzallamas.spatialBlend = 0f;
            }

            if (_fuenteAudioLanzallamas.clip == null)
            {
                _fuenteAudioLanzallamas.clip = GestorAudio.GetClipSFX(TipoSFX.Lanzallamas) 
                    ?? GestorAudio.GetClipSFX(TipoSFX.FuegoLlamas) 
                    ?? Resources.Load<AudioClip>("Audio/SFX/Lanzallamas");
            }

            if (_fuenteAudioLanzallamas.clip != null)
            {
                float vol = GestorAudio.Instancia != null 
                    ? GestorAudio.Instancia.VolumenMaster * GestorAudio.Instancia.VolumenSFX * 0.88f 
                    : 0.85f;
                _fuenteAudioLanzallamas.volume = vol;
                if (!_fuenteAudioLanzallamas.isPlaying)
                {
                    _fuenteAudioLanzallamas.Play();
                }
            }
        }

        private void DetenerAudioLanzallamasLoop()
        {
            if (_fuenteAudioLanzallamas != null && _fuenteAudioLanzallamas.isPlaying)
            {
                _fuenteAudioLanzallamas.Stop();
            }
        }

        protected override void OnDisable()
        {
            DetenerAudioLanzallamasLoop();
            base.OnDisable();
        }

        protected virtual void OnDestroy()
        {
            DetenerAudioLanzallamasLoop();
        }
    }
}
