using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Personaje de Hielo (PersonajeHielo.gd en Godot).
    /// Habilidades:
    /// 1. Rayo de Hielo: Click izquierdo mantenido, raycast continuo, ralentiza enemigos.
    /// 2. Bola de Hielo: Tecla E, proyectil recto con 80% prob congelar, cooldown 7s.
    /// 3. Armadura de Hielo: Tecla Q, 80% reducciÃ³n de daÃ±o por 10s, cooldown 12s.
    /// 4. Era de Hielo (Ultimate): Tecla R, zona AoE que congela y daÃ±a periÃ³dicamente por 10s, cooldown 40s.
    /// </summary>
    public class PersonajeHielo : Personaje
    {
        public override TipoPersonaje TipoPj => TipoPersonaje.Hielo;
        [Header("Prefabs y Efectos")]
        public GameObject prefabBolaHielo;
        public GameObject prefabEraHielo;
        public GameObject visualArmaduraHielo;
        public LineRenderer rayoHielo;
        public LineRenderer rayoHieloHalo;

        [Header("Estadísticas")]
        public int danoRayo = 9;
        public float rangoRayo = 7.5f;           // Alcance moderado balanceado (no infinito)
        public float intervaloTickRayo = 0.14f;  // Ticks continuos fluidos y sincronizados al instante sin saturar RPCs
        public float cooldownBolaHielo = 7.0f;
        public int danoBolaHielo = 100;

        public float cooldownArmadura = 12.0f;
        public float duracionArmadura = 10.0f;
        public float reduccionArmadura = 0.8f;

        public float cooldownEraHielo = 40.0f;
        public float duracionEraHielo = 10.0f;
        public int danoTickEraHielo = 25;

        // Estados sincronizados
        public NetworkVariable<bool> DisparandoRayo = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> ArmaduraActiva = new NetworkVariable<bool>(false);
        public NetworkVariable<float> AlcanceEfectivoRayoSincronizado = new NetworkVariable<float>(7.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Cooldowns locales UI
        private float _cdBolaHieloRestante = 0f;
        private float _cdArmaduraRestante = 0f;
        private float _cdEraHieloRestante = 0f;
        private float _tickTimerRayo = 0f;

        [Header("Animaciones Gato Hielo - Caminata")]
        public Sprite[] spritesAbajo;
        public Sprite[] spritesArriba;
        public Sprite[] spritesLado; // Sprites apuntando a la izquierda (West)
        public float fpsAnimacion = 8f;

        [Header("Animaciones Gato Hielo - Reposo")]
        public Sprite[] spritesSeSienta;    // Transición de pie a sentado (SeSienta_0006 a 0001)
        public Sprite[] spritesEstaSentado; // Ciclo continuo mientras está sentado (EstaSentado_0001 a 0007)
        public float fpsSentarse = 7f;
        public float fpsSentado = 5f;
        public float tiempoParaSentarse = 0.8f; // Segundos quieto antes de empezar a sentarse

        // Hitboxes dinámicas según dirección (calibradas para PPU 18, tamaño destacado y pixel-art puro):
        // Abajo/Arriba: gato visto de frente/espalda (ancho ~1.0m, alto ~2.2m)
        // Lado: gato visto de perfil (ancho ~2.9m, alto ~1.6m)
        // Sentado: gato sentado descansando (ancho ~1.5m, alto ~1.7m)
        private static readonly Vector2 HitboxVerticalSize = new Vector2(1.0f, 2.2f);
        private static readonly Vector2 HitboxVerticalOffset = new Vector2(0f, -0.1f);
        private static readonly Vector2 HitboxLateralSize = new Vector2(2.9f, 1.6f);
        private static readonly Vector2 HitboxLateralOffset = new Vector2(0f, -0.1f);
        private static readonly Vector2 HitboxSentadoSize = new Vector2(1.5f, 1.7f);
        private static readonly Vector2 HitboxSentadoOffset = new Vector2(0f, -0.15f);

        private BoxCollider2D _boxCol;
        private int _frameAnimActual = 0;
        private float _timerAnim = 0f;
        private int _direccionVisualActual = 0; // 0: Abajo, 1: Arriba, 2: Lado
        private float _tiempoMirarAtaque = 0f;  // Duración en segundos que se mantiene mirando hacia el cursor tras atacar

        // Estados del reposo del gato
        private float _tiempoQuieto = 0f;
        private bool _animandoSentarse = false;
        private bool _estaSentadoTotalmente = false;
        private int _frameSentarse = 0;
        private int _frameEstaSentado = 0;
        private float _timerSentarse = 0f;
        private float _timerEstaSentado = 0f;
        private bool _mirandoDerecha = false;

        protected override void Awake()
        {
            base.Awake();
            _boxCol = GetComponent<BoxCollider2D>();
        }

        public void NotificarAtaqueLocal(float duracion = 0.5f)
        {
            _tiempoMirarAtaque = duracion;
        }

        // Visuales mejorados del Rayo de Hielo (hélice de escarcha cristalina + estrella de impacto glacial)
        private LineRenderer _heliceEscarcha;
        private SpriteRenderer _estrellaImpactoHielo;
        private readonly List<SpriteRenderer> _cristalesViajeros = new List<SpriteRenderer>();
        private static Sprite _spriteCristalHielo;

        private void AsegurarVisualesRayoHielo()
        {
            if (_spriteCristalHielo == null)
            {
                // Rombo/estrella de hielo pixel-art (16x16)
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        float dx = Mathf.Abs(x - 7.5f);
                        float dy = Mathf.Abs(y - 7.5f);
                        float rombo = dx + dy;
                        bool eje = (dx < 1.2f && dy < 7f) || (dy < 1.2f && dx < 7f);
                        if (rombo <= 3.2f)
                            tex.SetPixel(x, y, new Color(0.95f, 1f, 1f, 1f)); // Blanco diamante
                        else if (rombo <= 5.8f || eje)
                            tex.SetPixel(x, y, new Color(0.35f, 0.92f, 1f, 0.92f)); // Cian cristalino
                        else if (rombo <= 7.2f)
                            tex.SetPixel(x, y, new Color(0.15f, 0.58f, 0.95f, 0.65f)); // Borde azul hielo
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                _spriteCristalHielo = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            }

            if (_heliceEscarcha == null && rayoHielo != null)
            {
                var goHelice = new GameObject("HeliceEscarcha");
                goHelice.transform.SetParent(transform, false);
                _heliceEscarcha = goHelice.AddComponent<LineRenderer>();
                _heliceEscarcha.material = rayoHielo.sharedMaterial;
                _heliceEscarcha.sortingOrder = rayoHielo.sortingOrder + 1;
                _heliceEscarcha.startWidth = 0.14f;
                _heliceEscarcha.endWidth = 0.14f;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.85f, 0.98f, 1f), 0f),
                        new GradientColorKey(new Color(0.28f, 0.88f, 1f), 0.5f),
                        new GradientColorKey(new Color(0.65f, 0.95f, 1f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.9f, 0f),
                        new GradientAlphaKey(0.85f, 0.8f),
                        new GradientAlphaKey(0.95f, 1f)
                    }
                );
                _heliceEscarcha.colorGradient = grad;
                _heliceEscarcha.enabled = false;
            }

            if (_estrellaImpactoHielo == null)
            {
                var goImp = new GameObject("EstrellaImpactoHielo");
                goImp.transform.SetParent(transform, false);
                _estrellaImpactoHielo = goImp.AddComponent<SpriteRenderer>();
                _estrellaImpactoHielo.sprite = _spriteCristalHielo;
                _estrellaImpactoHielo.sortingOrder = (rayoHielo != null ? rayoHielo.sortingOrder : 7) + 2;
                goImp.SetActive(false);
            }

            if (_cristalesViajeros.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var goC = new GameObject($"CristalRayo_{i}");
                    goC.transform.SetParent(transform, false);
                    var sr = goC.AddComponent<SpriteRenderer>();
                    sr.sprite = _spriteCristalHielo;
                    sr.sortingOrder = (rayoHielo != null ? rayoHielo.sortingOrder : 7) + 2;
                    goC.SetActive(false);
                    _cristalesViajeros.Add(sr);
                }
            }
        }

        private void OcultarVisualesExtraRayoHielo()
        {
            if (_heliceEscarcha != null && _heliceEscarcha.enabled) _heliceEscarcha.enabled = false;
            if (_estrellaImpactoHielo != null && _estrellaImpactoHielo.gameObject.activeSelf)
                _estrellaImpactoHielo.gameObject.SetActive(false);
            for (int i = 0; i < _cristalesViajeros.Count; i++)
            {
                if (_cristalesViajeros[i] != null && _cristalesViajeros[i].gameObject.activeSelf)
                    _cristalesViajeros[i].gameObject.SetActive(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ArmaduraActiva.OnValueChanged += (prev, act) => { if (visualArmaduraHielo) visualArmaduraHielo.SetActive(act); };
            DisparandoRayo.OnValueChanged += (prev, act) => 
            { 
                if (rayoHielo) rayoHielo.enabled = act; 
                if (rayoHieloHalo) rayoHieloHalo.enabled = act;
                if (!act) OcultarVisualesExtraRayoHielo();
            };
        }

        protected override void Update()
        {
            if (EstaMuerto.Value)
            {
                if (IsServer && DisparandoRayo.Value) DisparandoRayo.Value = false;
                if (rayoHielo != null && rayoHielo.enabled) rayoHielo.enabled = false;
                if (rayoHieloHalo != null && rayoHieloHalo.enabled) rayoHieloHalo.enabled = false;
                OcultarVisualesExtraRayoHielo();
                return;
            }

            base.Update();

            if (EsControlLocal)
            {
                if (_cdBolaHieloRestante > 0) _cdBolaHieloRestante -= Time.deltaTime;
                if (_cdArmaduraRestante > 0) _cdArmaduraRestante -= Time.deltaTime;
                if (_cdEraHieloRestante > 0) _cdEraHieloRestante -= Time.deltaTime;

                if (HUDJugador.Instancia != null)
                {
                    HUDJugador.Instancia.ActualizarCooldown(0, 0f, 1f); // LMB (Rayo continuo)
                    HUDJugador.Instancia.ActualizarCooldown(1, _cdBolaHieloRestante, cooldownBolaHielo); // E
                    HUDJugador.Instancia.ActualizarCooldown(2, _cdArmaduraRestante, cooldownArmadura);   // Q
                    HUDJugador.Instancia.ActualizarCooldown(3, _cdEraHieloRestante, cooldownEraHielo);   // R
                }
            }

            // Rayo de hielo visual (haz láser criogénico recto + hélice zig-zag de escarcha + cristal giratorio en la punta)
            bool disparandoVisual = (IsOwner ? Controles.PrincipalMantenido : DisparandoRayo.Value) && !EstaMuerto.Value;
            if (rayoHielo != null)
            {
                rayoHielo.enabled = disparandoVisual;
                if (rayoHieloHalo != null) rayoHieloHalo.enabled = disparandoVisual;

                if (disparandoVisual)
                {
                    AsegurarVisualesRayoHielo();
                    if (_heliceEscarcha != null) _heliceEscarcha.enabled = true;

                    Vector2 origen = transform.position;
                    Vector2 mouseMundo = Controles.PosicionMouseMundo();
                    float distMouse = Vector2.Distance(origen, mouseMundo);
                    // Si el cursor está por debajo del límite, el rayo se recorta al cursor; si está por encima, llega hasta el límite
                    float alcanceDinamico = IsOwner ? Mathf.Min(distMouse, rangoRayo) : Mathf.Min(AlcanceEfectivoRayoSincronizado.Value, rangoRayo);

                    if (IsOwner && IsSpawned && NetworkManager != null && NetworkManager.IsListening)
                    {
                        if (Mathf.Abs(AlcanceEfectivoRayoSincronizado.Value - alcanceDinamico) > 0.05f)
                        {
                            AlcanceEfectivoRayoSincronizado.Value = alcanceDinamico;
                        }
                    }

                    Vector2 dir = IsOwner ? (mouseMundo - origen).normalized : DireccionApuntado.Value;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

                    // El rayo penetra enemigos y se frena en su alcance dinámico o si choca con una pared sólida
                    Vector2 destino = origen + dir * alcanceDinamico;
                    RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir, alcanceDinamico);
                    foreach (var h in hits)
                    {
                        if (h.collider != null && h.collider.gameObject != gameObject)
                        {
                            bool esPared = false;
                            try { esPared = h.collider.CompareTag("Pared"); } catch { esPared = false; }
                            if (esPared || h.collider.name.IndexOf("pared", System.StringComparison.OrdinalIgnoreCase) >= 0 || h.collider.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                destino = h.point;
                                break;
                            }
                        }
                    }

                    const int segmentos = 14;
                    if (rayoHielo.positionCount != segmentos) rayoHielo.positionCount = segmentos;
                    if (rayoHieloHalo != null && rayoHieloHalo.positionCount != segmentos) rayoHieloHalo.positionCount = segmentos;
                    if (_heliceEscarcha != null && _heliceEscarcha.positionCount != segmentos) _heliceEscarcha.positionCount = segmentos;

                    rayoHielo.startWidth = 0.20f;
                    rayoHielo.endWidth = 0.24f;
                    if (rayoHieloHalo != null)
                    {
                        float pulsoHalo = 0.52f + Mathf.Sin(Time.time * 24f) * 0.08f;
                        rayoHieloHalo.startWidth = pulsoHalo;
                        rayoHieloHalo.endWidth = pulsoHalo * 1.15f;
                    }

                    Vector2 normal = new Vector2(-dir.y, dir.x);
                    for (int i = 0; i < segmentos; i++)
                    {
                        float t = (float)i / (segmentos - 1);
                        Vector2 ptBase = Vector2.Lerp(origen, destino, t);

                        // El haz central de hielo es tenso y cristalino
                        Vector3 pCentro = new Vector3(ptBase.x, ptBase.y, -0.5f);
                        rayoHielo.SetPosition(i, pCentro);
                        if (rayoHieloHalo != null) rayoHieloHalo.SetPosition(i, pCentro);

                        // La hélice de escarcha forma picos cristalinos en zig-zag alrededor del haz
                        if (_heliceEscarcha != null)
                        {
                            float signo = (i % 2 == 0) ? 1f : -1f;
                            float amp = (i == 0 || i == segmentos - 1) ? 0f : 0.28f * Mathf.Sin(Time.time * 22f + i * 0.9f) * signo;
                            Vector2 ptHelice = ptBase + normal * amp;
                            _heliceEscarcha.SetPosition(i, new Vector3(ptHelice.x, ptHelice.y, -0.55f));
                        }
                    }

                    // Estrella de cristal giratoria en el punto final de impacto
                    if (_estrellaImpactoHielo != null)
                    {
                        _estrellaImpactoHielo.gameObject.SetActive(true);
                        _estrellaImpactoHielo.transform.position = new Vector3(destino.x, destino.y, -0.6f);
                        _estrellaImpactoHielo.transform.rotation = Quaternion.Euler(0f, 0f, Time.time * 260f);
                        float escImp = 1.25f + Mathf.Sin(Time.time * 28f) * 0.22f;
                        _estrellaImpactoHielo.transform.localScale = new Vector3(escImp, escImp, 1f);
                    }

                    // Cristales viajeros a lo largo del haz
                    for (int c = 0; c < _cristalesViajeros.Count; c++)
                    {
                        var srC = _cristalesViajeros[c];
                        if (srC == null) continue;
                        srC.gameObject.SetActive(true);
                        float fase = Mathf.Repeat(Time.time * 3.2f + c * 0.25f, 1f);
                        Vector2 posC = Vector2.Lerp(origen, destino, fase);
                        srC.transform.position = new Vector3(posC.x, posC.y, -0.58f);
                        srC.transform.rotation = Quaternion.Euler(0f, 0f, -Time.time * 180f + c * 45f);
                        srC.transform.localScale = Vector3.one * 0.72f;
                    }
                }
                else
                {
                    OcultarVisualesExtraRayoHielo();
                }
            }

            // Daño continuo rayo de hielo en servidor (pega más rápido)
            if (IsServer && DisparandoRayo.Value)
            {
                _tickTimerRayo -= Time.deltaTime;
                if (_tickTimerRayo <= 0f)
                {
                    _tickTimerRayo = intervaloTickRayo;
                    EjecutarRaycastRayoHielo();
                }
            }

            // Decrementar temporizador de mirada por ataque
            if (_tiempoMirarAtaque > 0f)
            {
                _tiempoMirarAtaque -= Time.deltaTime;
            }

            ActualizarAnimacionGato();
        }

        protected override void ActualizarVisualMirada(Vector2 dirCursor)
        {
            // OJO: El gato debe mirar hacia donde está CAMINANDO.
            // Solo mirará hacia el cursor cuando vaya a atacar (disparando rayo o tras usar una habilidad).
            bool disparandoVisual = IsOwner ? Controles.PrincipalMantenido : DisparandoRayo.Value;
            bool estaAtacando = disparandoVisual || (_tiempoMirarAtaque > 0f);

            Vector2 dirMirada = dirCursor;

            if (!estaAtacando)
            {
                // Si se está moviendo (local o remoto sincronizado), mirar en la dirección del movimiento
                Vector2 mov = ObtenerMovimientoEfectivo();

                if (mov.sqrMagnitude > 0.01f)
                {
                    dirMirada = mov.normalized;
                }
                else
                {
                    // Si está quieto y no ataca, mantiene su dirección visual actual sin girar al mouse
                    return;
                }
            }

            // Determinar si la dirección predominante es vertical u horizontal
            // Si |x| >= |y| * 0.8f se considera lateral (Izquierda o Derecha)
            bool esLateral = Mathf.Abs(dirMirada.x) >= Mathf.Abs(dirMirada.y) * 0.8f;

            if (esLateral)
            {
                _direccionVisualActual = 2; // Lado (West)
                // Los sprites West miran naturalmente a la izquierda.
                // Izquierda: dirMirada.x < 0 -> flipX = false
                // Derecha:   dirMirada.x > 0 -> flipX = true
                _mirandoDerecha = dirMirada.x > 0;
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = _mirandoDerecha;
                }

                // Ajustar hitbox horizontal alargada (gato visto de perfil)
                ActualizarHitbox(HitboxLateralSize, HitboxLateralOffset);
            }
            else
            {
                _mirandoDerecha = false;
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = false;
                }

                if (dirMirada.y >= 0)
                {
                    _direccionVisualActual = 1; // Arriba (North)
                }
                else
                {
                    _direccionVisualActual = 0; // Abajo (South)
                }

                // Ajustar hitbox vertical estrecha (gato visto de frente / espalda)
                ActualizarHitbox(HitboxVerticalSize, HitboxVerticalOffset);
            }
        }

        private void ActualizarHitbox(Vector2 size, Vector2 offset)
        {
            if (_boxCol != null)
            {
                if (_boxCol.size != size) _boxCol.size = size;
                if (_boxCol.offset != offset) _boxCol.offset = offset;
            }
        }

        private void ActualizarAnimacionGato()
        {
            if (_spriteRenderer == null) return;

            // Comprobar si el personaje se está moviendo o atacando (tanto para jugador local como remoto)
            bool disparandoVisual = IsOwner ? Controles.PrincipalMantenido : DisparandoRayo.Value;
            bool estaAtacando = disparandoVisual || (_tiempoMirarAtaque > 0f);
            Vector2 movEfectivo = ObtenerMovimientoEfectivo();
            bool seMueve = movEfectivo.sqrMagnitude > 0.01f;

            if (seMueve || estaAtacando)
            {
                // Si se mueve o ataca, se levanta inmediatamente
                _tiempoQuieto = 0f;
                _animandoSentarse = false;
                _estaSentadoTotalmente = false;
                _frameSentarse = 0;
                _frameEstaSentado = 0;
                _timerSentarse = 0f;
                _timerEstaSentado = 0f;

                // Restaurar flipX según la dirección lateral
                if (_direccionVisualActual == 2)
                {
                    _spriteRenderer.flipX = _mirandoDerecha;
                }
                else
                {
                    _spriteRenderer.flipX = false;
                }

                Sprite[] listaSprites = _direccionVisualActual switch
                {
                    0 => spritesAbajo,
                    1 => spritesArriba,
                    2 => spritesLado,
                    _ => spritesAbajo
                };

                if (listaSprites == null || listaSprites.Length == 0) return;

                if (seMueve)
                {
                    _timerAnim += Time.deltaTime;
                    float tiempoPorFrame = 1f / Mathf.Max(1f, fpsAnimacion);
                    if (_timerAnim >= tiempoPorFrame)
                    {
                        _timerAnim -= tiempoPorFrame;
                        _frameAnimActual = (_frameAnimActual + 1) % listaSprites.Length;
                    }
                }
                else
                {
                    _timerAnim = 0f;
                    _frameAnimActual = 0;
                }

                int index = Mathf.Clamp(_frameAnimActual, 0, listaSprites.Length - 1);
                if (listaSprites[index] != null)
                {
                    _spriteRenderer.sprite = listaSprites[index];
                }
            }
            else
            {
                // El gato está quieto:
                _tiempoQuieto += Time.deltaTime;

                if (!_animandoSentarse && !_estaSentadoTotalmente)
                {
                    // Primeros momentos quieto: frame base de pie (idle)
                    if (_tiempoQuieto >= tiempoParaSentarse)
                    {
                        if (spritesSeSienta != null && spritesSeSienta.Length > 0)
                        {
                            _animandoSentarse = true;
                            _frameSentarse = 0;
                            _timerSentarse = 0f;
                        }
                        else
                        {
                            _estaSentadoTotalmente = true;
                        }
                    }
                    else
                    {
                        // Frame de pie según dirección
                        if (_direccionVisualActual == 2)
                        {
                            _spriteRenderer.flipX = _mirandoDerecha;
                        }
                        else
                        {
                            _spriteRenderer.flipX = false;
                        }

                        Sprite[] listaSprites = _direccionVisualActual switch
                        {
                            0 => spritesAbajo,
                            1 => spritesArriba,
                            2 => spritesLado,
                            _ => spritesAbajo
                        };
                        if (listaSprites != null && listaSprites.Length > 0 && listaSprites[0] != null)
                        {
                            _spriteRenderer.sprite = listaSprites[0];
                        }
                        return;
                    }
                }

                // Al sentarse: Los sprites originales (SeSienta y EstaSentado) miran hacia la DERECHA de forma nativa.
                // Si el gato estaba mirando a la derecha, flipX = false para mirar a la derecha.
                // Si estaba mirando a la izquierda, flipX = true para mirar a la izquierda.
                _spriteRenderer.flipX = !_mirandoDerecha;

                if (_animandoSentarse)
                {
                    // Reproducir transición "SeSienta" una sola vez
                    _timerSentarse += Time.deltaTime;
                    float tFrame = 1f / Mathf.Max(1f, fpsSentarse);
                    if (_timerSentarse >= tFrame)
                    {
                        _timerSentarse -= tFrame;
                        _frameSentarse++;
                        if (_frameSentarse >= spritesSeSienta.Length)
                        {
                            // Terminó de sentarse, pasar a ciclo EstaSentado
                            _animandoSentarse = false;
                            _estaSentadoTotalmente = true;
                            _frameEstaSentado = 0;
                            _timerEstaSentado = 0f;
                        }
                    }

                    if (_animandoSentarse && _frameSentarse < spritesSeSienta.Length)
                    {
                        if (spritesSeSienta[_frameSentarse] != null)
                        {
                            _spriteRenderer.sprite = spritesSeSienta[_frameSentarse];
                        }
                        ActualizarHitbox(HitboxSentadoSize, HitboxSentadoOffset);
                        return;
                    }
                }

                if (_estaSentadoTotalmente)
                {
                    // Ciclo continuo "EstaSentado" en reposo
                    if (spritesEstaSentado != null && spritesEstaSentado.Length > 0)
                    {
                        _timerEstaSentado += Time.deltaTime;
                        float tFrame = 1f / Mathf.Max(1f, fpsSentado);
                        if (_timerEstaSentado >= tFrame)
                        {
                            _timerEstaSentado -= tFrame;
                            _frameEstaSentado = (_frameEstaSentado + 1) % spritesEstaSentado.Length;
                        }

                        int idx = Mathf.Clamp(_frameEstaSentado, 0, spritesEstaSentado.Length - 1);
                        if (spritesEstaSentado[idx] != null)
                        {
                            _spriteRenderer.sprite = spritesEstaSentado[idx];
                        }
                    }

                    ActualizarHitbox(HitboxSentadoSize, HitboxSentadoOffset);
                }
            }
        }

        protected override void ProcesarInputsHabilidadesLocales()
        {
            // Rayo de Hielo (Click Izquierdo sostenido)
            bool disparando = Controles.PrincipalMantenido && PuedeUsarHabilidad(IdHabilidad.Hielo_RayoHielo, out _);
            if (disparando != DisparandoRayo.Value)
            {
                SetDisparandoRayoServerRpc(disparando);
            }

            // Bola de Hielo (E) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Habilidad1) && _cdBolaHieloRestante <= 0)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Hielo_BolaHielo, out string motivo))
                {
                    _cdBolaHieloRestante = cooldownBolaHielo;
                    Vector2 dir = ((Vector2)Controles.PosicionMouseMundo() - (Vector2)transform.position).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
                    NotificarAtaqueLocal(0.45f);
                    LanzarBolaHieloServerRpc(dir);
                }
                else
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 1.2f, motivo, Color.cyan);
                }
            }

            // Armadura de Hielo (Q) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Habilidad2) && _cdArmaduraRestante <= 0 && !ArmaduraActiva.Value)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Hielo_Armadura, out string motivo))
                {
                    _cdArmaduraRestante = cooldownArmadura;
                    ActivarArmaduraServerRpc();
                }
                else
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 1.2f, motivo, Color.cyan);
                }
            }

            // Era de Hielo (R) - Auto-casteo continuo al mantener presionado
            if (Controles.AutoDisparo(Controles.Ultimate) && _cdEraHieloRestante <= 0)
            {
                if (PuedeUsarHabilidad(IdHabilidad.Hielo_EraHielo, out string motivo))
                {
                    _cdEraHieloRestante = cooldownEraHielo;
                    NotificarAtaqueLocal(0.5f);
                    ActivarEraHieloServerRpc();
                }
                else
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 1.2f, motivo, Color.cyan);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetDisparandoRayoServerRpc(bool disparando, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            DisparandoRayo.Value = disparando;
        }

        private void EjecutarRaycastRayoHielo()
        {
            Vector2 origen = transform.position;
            Vector2 dir = DireccionApuntado.Value;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            // Daña y ralentiza a los enemigos hasta el alcance dinámico recortado al cursor
            float alcanceEfectivo = Mathf.Min(AlcanceEfectivoRayoSincronizado.Value, rangoRayo);
            RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir, alcanceEfectivo);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

                var enemigo = hit.collider.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    int danoEscalado = Mathf.RoundToInt(danoRayo * MultiplicadorDanoTotal);
                    enemigo.RecibirDano(danoEscalado);
                    enemigo.AplicarRalentizacion();
                    // NO rompemos el bucle: atraviesa a todos los enemigos en el haz
                }
                else
                {
                    bool esPared = false;
                    try { esPared = hit.collider.CompareTag("Pared"); } catch { esPared = false; }
                    if (esPared || hit.collider.name.IndexOf("pared", System.StringComparison.OrdinalIgnoreCase) >= 0 || hit.collider.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        break; // Solo las paredes sólidas detienen el rayo
                    }
                }
            }
        }

        [ClientRpc]
        private void NotificarAtaqueClientRpc(float duracion)
        {
            _tiempoMirarAtaque = duracion;
        }

        [ServerRpc(RequireOwnership = false)]
        private void LanzarBolaHieloServerRpc(Vector2 dir, ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (prefabBolaHielo == null) return;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            NotificarAtaqueClientRpc(0.45f);

            GameObject bola = Instantiate(prefabBolaHielo, transform.position, Quaternion.identity);
            var proj = bola.GetComponent<Proyectil>();
            if (proj != null)
            {
                int danoEscalado = Mathf.RoundToInt(danoBolaHielo * MultiplicadorDanoTotal);
                proj.Configurar(dir, danoEscalado, OwnerClientId, congelacion: true, probCong: 1.0f);
            }
            bola.GetComponent<NetworkObject>().Spawn();
            if (proj != null)
            {
                proj.DireccionSincronizada.Value = dir;
            }
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

        [ServerRpc(RequireOwnership = false)]
        private void ActivarEraHieloServerRpc(ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (prefabEraHielo == null) return;
            NotificarAtaqueClientRpc(0.5f);
            GameObject era = Instantiate(prefabEraHielo, transform.position, Quaternion.identity);
            var eraComponente = era.GetComponent<ZonaEraHielo>();
            if (eraComponente != null)
            {
                eraComponente.Configurar(duracionEraHielo, danoTickEraHielo);
            }
            era.GetComponent<NetworkObject>().Spawn();
        }

        public override void ReiniciarCooldowns()
        {
            _cdBolaHieloRestante = 0f;
            _cdArmaduraRestante = 0f;
            _cdEraHieloRestante = 0f;

            if (EsControlLocal && HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.ActualizarCooldown(0, 0f, 1f);
                HUDJugador.Instancia.ActualizarCooldown(1, 0f, cooldownBolaHielo);
                HUDJugador.Instancia.ActualizarCooldown(2, 0f, cooldownArmadura);
                HUDJugador.Instancia.ActualizarCooldown(3, 0f, cooldownEraHielo);
            }
        }

        public override void LimpiarEstadosYHabilidades()
        {
            base.LimpiarEstadosYHabilidades();

            if (IsServer)
            {
                StopAllCoroutines();
                ArmaduraActiva.Value = false;
                DisparandoRayo.Value = false;
                ReduccionDano.Value = 0f;
            }

            if (visualArmaduraHielo != null)
            {
                visualArmaduraHielo.SetActive(false);
            }
            if (rayoHielo != null)
            {
                rayoHielo.enabled = false;
            }
            if (rayoHieloHalo != null)
            {
                rayoHieloHalo.enabled = false;
            }
            OcultarVisualesExtraRayoHielo();
        }
    }
}

