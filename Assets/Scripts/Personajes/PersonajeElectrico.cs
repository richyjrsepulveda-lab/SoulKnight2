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
        public float tiempoCargaRayo = 2.0f;
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

        public float cooldownEspada = 0.25f;

        // Timers locales UI
        private float _cdEspadaRestante = 0f;
        private float _cdDashRestante = 0f;
        private float _cdTeleportRestante = 0f;
        private float _cdRayoRestante = 0f;

        // Variables de ejecución en servidor
        private Vector2 _dirDash;
        private float _distanciaRecorridaDash = 0f;
        private float _tiempoCargaActual = 0f;
        private float _longitudRayoActual = 0f;
        private float _timerTickRayo = 0f;
        private float _tiempoRayoActivo = 0f;

        protected override float ObtenerVelocidadCorrer() => velocidadBase * 2.0f; // Correr = 800 px/s

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            RayoCargando.OnValueChanged += (prev, act) => { if (visualCargaRayo) visualCargaRayo.enabled = act; };
            RayoLanzado.OnValueChanged += (prev, act) => { if (visualRayoElectrico) visualRayoElectrico.enabled = act; };
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
                const int pts = 12;
                if (visualCargaRayo.positionCount != pts) visualCargaRayo.positionCount = pts;

                float progreso = Mathf.Clamp01(_tiempoCargaActual / tiempoCargaRayo);
                float radio = 1.35f + Mathf.Sin(Time.time * 26f) * 0.25f;

                for (int i = 0; i < pts; i++)
                {
                    float ang = (float)i / pts * Mathf.PI * 2f + Time.time * (18f + progreso * 12f);
                    float r = radio + (Mathf.PerlinNoise(i * 1.7f, Time.time * 24f) - 0.5f) * 0.45f;
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
                    if (_tiempoCargaActual >= tiempoCargaRayo)
                    {
                        LanzarRayoServer();
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
                Vector2 origen = transform.position;
                Vector2 dir = IsOwner ? ((Vector2)Controles.PosicionMouseMundo() - origen).normalized : DireccionApuntado.Value;
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

                const int segs = 16;
                if (visualRayoElectrico.positionCount != segs) visualRayoElectrico.positionCount = segs;

                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 destino = origen + dir * _longitudRayoActual;

                for (int i = 0; i < segs; i++)
                {
                    float t = (float)i / (segs - 1);
                    Vector2 pt = Vector2.Lerp(origen, destino, t);

                    if (i > 0 && i < segs - 1)
                    {
                        float jitter = (UnityEngine.Random.value - 0.5f) * 0.75f;
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

                // Raycast para detectar muros del mapa antes de avanzar (evita salir del mapa)
                RaycastHit2D[] rayHits = Physics2D.RaycastAll(_rb.position, _dirDash, pasoDist + 0.45f);
                foreach (var rh in rayHits)
                {
                    if (rh.collider != null && rh.collider.gameObject != gameObject && EsMuroDelMapa(rh.collider))
                    {
                        // Detenerse exactamente frente al muro del mapa
                        _rb.position = rh.point - _dirDash * 0.45f;
                        DashActivo.Value = false;
                        _col.enabled = true;
                        _rb.velocity = Vector2.zero;
                        return;
                    }
                }

                Vector2 avance = _dirDash * pasoDist;
                _rb.position += avance;
                _distanciaRecorridaDash += avance.magnitude;

                // Daño en el camino del Dash (atraviesa enemigos y estructuras comunes)
                Collider2D[] hits = Physics2D.OverlapCircleAll(_rb.position, 0.8f);
                foreach (var hit in hits)
                {
                    var enemigo = hit.GetComponent<EnemigoBase>();
                    if (enemigo != null)
                    {
                        int danoEscalado = Mathf.RoundToInt(danoDash * MultiplicadorDanoTotal);
                        enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    }
                }

                if (_distanciaRecorridaDash >= distanciaMaximaDash)
                {
                    DashActivo.Value = false;
                    _col.enabled = true;
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
                    _cdEspadaRestante = cooldownEspada;
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
                    _cdDashRestante = cooldownDash;
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
                    _cdTeleportRestante = cooldownTeleport;
                    TeleportServerRpc(Controles.PosicionMouseMundo());
                }
                else
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, Controles.Presionada(Controles.Habilidad2));
                }
            }

            // Rayo Eléctrico (R) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Ultimate) && _cdRayoRestante <= 0 && !RayoCargando.Value && !RayoLanzado.Value)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Elec_Rayo, out string motivo))
                {
                    _cdRayoRestante = cooldownRayo;
                    ComenzarCargaRayoServerRpc();
                }
                else
                {
                    NotificarHabilidadBloqueada(motivo, Color.yellow, Controles.Presionada(Controles.Ultimate));
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void UsarEspadaServerRpc(Vector2 dir, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            Vector2 centroCorte = (Vector2)transform.position + dir * 1.5f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(centroCorte, 1.8f);
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == gameObject) continue;
                var enemigo = col.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    int danoEscalado = Mathf.RoundToInt(danoEspada * MultiplicadorDanoTotal);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    enemigo.AplicarKnockback(dir, 350f);
                }
            }

            MostrarEspadaClientRpc(dir);
        }

        [ClientRpc]
        private void MostrarEspadaClientRpc(Vector2 dir)
        {
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
            _col.enabled = false;
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
                // Raycast a lo largo del trayecto de teleportación para no atravesar muros del mapa
                RaycastHit2D[] hitsMuro = Physics2D.RaycastAll(origen, dir, dist);
                foreach (var hm in hitsMuro)
                {
                    if (hm.collider != null && hm.collider.gameObject != gameObject && EsMuroDelMapa(hm.collider))
                    {
                        // Detener la teleportación justo antes del muro del mapa
                        targetPos = hm.point - dir * 0.55f;
                        break;
                    }
                }
            }

            // Validar que el destino no quede empotrado dentro de un muro del mapa
            Collider2D[] puntoHits = Physics2D.OverlapCircleAll(targetPos, 0.4f);
            foreach (var ph in puntoHits)
            {
                if (ph != null && ph.gameObject != gameObject && EsMuroDelMapa(ph))
                {
                    // Si el destino quedó dentro de la pared, cancelar teleport
                    return;
                }
            }

            AplicarImpactoTeleport(origen);
            transform.position = targetPos;
            _rb.position = targetPos;
            AplicarImpactoTeleport(targetPos);

            MostrarTeleportEfectoClientRpc(origen, targetPos);
        }

        private bool EsMuroDelMapa(Collider2D col)
        {
            if (col == null || col.isTrigger) return false;

            // Estructuras comunes (columnas, props interiores) SÍ se pueden atravesar
            if (col.name.IndexOf("columna", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (col.name.IndexOf("portal", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;

            // Muros y paredes perimetrales del mapa NO se pueden atravesar
            bool esPared = false;
            try { esPared = col.CompareTag("Pared"); } catch { esPared = false; }
            if (esPared ||
                col.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                col.name.IndexOf("pared", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return false;
        }

        private void AplicarImpactoTeleport(Vector2 pos)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radioTeleport);
            foreach (var hit in hits)
            {
                var enemigo = hit.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    int danoEscalado = Mathf.RoundToInt(danoTeleport * MultiplicadorDanoTotal);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
                    Vector2 dirKnock = ((Vector2)enemigo.transform.position - pos).normalized;
                    enemigo.AplicarKnockback(dirKnock, 1000f);
                }
            }
        }

        [ClientRpc]
        private void MostrarTeleportEfectoClientRpc(Vector2 origen, Vector2 destino)
        {
            if (prefabEfectoTeleport != null)
            {
                Instantiate(prefabEfectoTeleport, origen, Quaternion.identity);
                Instantiate(prefabEfectoTeleport, destino, Quaternion.identity);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ComenzarCargaRayoServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            RayoCargando.Value = true;
            _tiempoCargaActual = 0f;
        }

        private void LanzarRayoServer()
        {
            RayoCargando.Value = false;
            RayoLanzado.Value = true;
            _longitudRayoActual = 0f;
            _tiempoRayoActivo = 0f;
            _timerTickRayo = 0f;
        }

        private void AplicarDanoRayoServer()
        {
            Vector2 dir = DireccionApuntado.Value;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            // Atraviesa todo (muros, obstáculos y enemigos) hasta la distancia máxima
            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 1.1f, dir, _longitudRayoActual);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                var enemigo = hit.collider.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    int danoEscalado = Mathf.RoundToInt(danoRayo * MultiplicadorDanoTotal);
                    enemigo.RecibirDano(danoEscalado, new Color(1f, 0.96f, 0.22f, 1f)); // Amarillo rayo
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
                _col.enabled = true;
                _rb.velocity = Vector2.zero;
            }

            _tiempoCargaActual = 0f;
            _longitudRayoActual = 0f;
            _distanciaRecorridaDash = 0f;

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

