using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Zona de área para la ultimate "Era de Hielo".
    /// Transforma el área en una feroz ventisca ártica (Blizzard):
    /// - Escala ampliada con radio de congelación masivo.
    /// - Ráfagas y vórtices de viento helado en espiral giratoria continua (sin iconos planos).
    /// - Enjambre de 24 copos de nieve y esquirlas de hielo en órbita turbulenta.
    /// - Ondas y pulsos de escarcha que dañan y congelan a todos los enemigos periódicamente.
    /// - El daño por tick escala con el nivel del jugador y multiplicadores de run.
    /// </summary>
    public class ZonaEraHielo : NetworkBehaviour
    {
        [Header("Efectos Visuales")]
        public Transform vorticeInterior;
        public Transform anilloExterior;
        public SpriteRenderer fondoAura;
        public SpriteRenderer destelloTick;

        private float _duracion = 10f;
        private int _danoPorTick = 25;
        private float _intervaloTick = 0.5f;

        private float _tiempoTranscurrido = 0f;
        private float _timerTick = 0f;
        private readonly List<EnemigoBase> _enemigosEnZona = new List<EnemigoBase>();

        private Vector3 _escalaObjetivo = Vector3.one * 1.45f;
        private float _tiempoAparicion = 0f;
        private Personaje _lanzador;

        public static readonly List<ZonaEraHielo> Todas = new List<ZonaEraHielo>();

        // Partículas orbitales de la ventisca
        private struct ParticulaVentisca
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public float distanciaOrbital;
            public float anguloActual;
            public float velocidadOrbital;
            public float desfaseOscilacion;
            public float escalaBase;
        }

        private readonly List<ParticulaVentisca> _particulasVentisca = new List<ParticulaVentisca>();

        // Sprites procedurales cacheados de la ventisca
        private static Sprite _spriteAuraBlizzard;
        private static Sprite _spriteVorticeBlizzard;
        private static Sprite _spriteCopoBlizzard;
        private static Sprite _spriteCristalBlizzard;
        private static Sprite _spriteAnilloBlizzard;

        public void Configurar(float duracion, int danoTick, Personaje lanzador = null)
        {
            _duracion = duracion;
            _danoPorTick = danoTick;
            _lanzador = lanzador;

            if (_lanzador != null)
            {
                float factorEterna = 1.0f + _lanzador.ObtenerNivelMejora(TipoMejoraTienda.Hielo_TormentaEterna) * 0.20f;
                radioZona = 10.0f * _lanzador.MultiplicadorAreaHabilidades * factorEterna;
                var col = GetComponent<CircleCollider2D>();
                if (col != null) col.radius = radioZona;
                ConfigurarVisualesVentisca();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!Todas.Contains(this)) Todas.Add(this);
        }

        public override void OnNetworkDespawn()
        {
            Todas.Remove(this);
            base.OnNetworkDespawn();
        }

        private AudioSource _fuenteAudioEra;

        [Header("Dimensiones")]
        public float radioZona = 10.0f;

        void Awake()
        {
            // Radio de la ulti: el collider y todas las capas visuales sincronizadas al mismo tamaño exacto
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = radioZona;
            }

            _escalaObjetivo = Vector3.one * 1.0f;
            transform.localScale = Vector3.one * 0.1f;
        }

        void Start()
        {
            ConfigurarVisualesVentisca();

            _fuenteAudioEra = gameObject.AddComponent<AudioSource>();
            _fuenteAudioEra.loop = true;
            _fuenteAudioEra.spatialBlend = 0f;
            _fuenteAudioEra.playOnAwake = false;

            var clip = Resources.Load<AudioClip>("Audio/SFX/EraDeHielo");
            if (clip != null)
            {
                _fuenteAudioEra.clip = clip;
                float vol = GestorAudio.Instancia != null 
                    ? GestorAudio.Instancia.VolumenMaster * GestorAudio.Instancia.VolumenSFX * 0.95f 
                    : 0.85f;
                _fuenteAudioEra.volume = vol;
                _fuenteAudioEra.Play();
            }
        }

        private void ConfigurarVisualesVentisca()
        {
            GenerarSpritesProcedurales();

            // Los sprites procedurales tienen textura 128x128 con PPU 32 -> diámetro base en unidades = 128 / 32 = 4.0 unidades
            // Para que el diámetro visual coincida exactamente con el diámetro del collider (2 * radioZona):
            float baseSpriteUnits = 128f / 32f;
            float escalaVisual = (radioZona * 2f) / baseSpriteUnits;

            // 1. Fondo de Escarcha y Suelo Congelado (Capa 1)
            if (fondoAura == null)
            {
                var fObj = transform.Find("FondoAura");
                if (fObj != null) fondoAura = fObj.GetComponent<SpriteRenderer>();
            }
            if (fondoAura != null)
            {
                fondoAura.sprite = _spriteAuraBlizzard;
                fondoAura.color = new Color(0.18f, 0.65f, 1f, 0.40f);
                fondoAura.sortingOrder = 1;
                fondoAura.transform.localScale = Vector3.one * escalaVisual;
            }

            // 2. Vórtice Interior de Viento Huracanado (Capa 2) - Se ajusta exactamente al área de la ulti
            if (vorticeInterior == null)
            {
                var vObj = transform.Find("VorticeInterior");
                if (vObj != null) vorticeInterior = vObj;
            }
            if (vorticeInterior != null)
            {
                var sr = vorticeInterior.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = _spriteVorticeBlizzard;
                    sr.color = new Color(0.65f, 0.92f, 1f, 0.75f);
                    sr.sortingOrder = 2;
                }
                vorticeInterior.localScale = Vector3.one * (escalaVisual * 0.98f);
            }

            // 3. Anillo Exterior de Tormenta de Nieve (Capa 3) - Marca el límite perimetral exacto
            if (anilloExterior == null)
            {
                var aObj = transform.Find("AnilloExterior");
                if (aObj != null) anilloExterior = aObj;
            }
            if (anilloExterior != null)
            {
                var sr = anilloExterior.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = _spriteAnilloBlizzard;
                    sr.color = new Color(0.85f, 0.98f, 1f, 0.80f);
                    sr.sortingOrder = 3;
                }
                anilloExterior.localScale = Vector3.one * escalaVisual;
            }

            // 4. Destello de Tick Radial (Capa 4)
            if (destelloTick == null)
            {
                var dObj = transform.Find("DestelloTick");
                if (dObj != null) destelloTick = dObj.GetComponent<SpriteRenderer>();
            }
            if (destelloTick != null)
            {
                destelloTick.sprite = _spriteAuraBlizzard;
                destelloTick.color = new Color(1f, 1f, 1f, 0f);
                destelloTick.sortingOrder = 4;
                destelloTick.transform.localScale = Vector3.one * escalaVisual;
            }

            // 5. Enjambre Dinámico de Copos y Esquirlas de Hielo
            CrearEnjambreCopos();
        }

        private void CrearEnjambreCopos()
        {
            _particulasVentisca.Clear();
            int totalParticulas = 24;

            for (int i = 0; i < totalParticulas; i++)
            {
                var pObj = new GameObject($"CopoVentisca_{i}");
                pObj.transform.SetParent(transform, false);

                var sr = pObj.AddComponent<SpriteRenderer>();
                bool esCristal = (i % 2 == 0);
                sr.sprite = esCristal ? _spriteCristalBlizzard : _spriteCopoBlizzard;
                sr.color = esCristal 
                    ? new Color(0.70f, 0.92f, 1f, UnityEngine.Random.Range(0.65f, 0.95f))
                    : new Color(0.95f, 0.98f, 1f, UnityEngine.Random.Range(0.70f, 0.98f));
                sr.sortingOrder = 5;

                float escala = UnityEngine.Random.Range(0.40f, 0.75f);
                pObj.transform.localScale = Vector3.one * escala;

                var part = new ParticulaVentisca
                {
                    transform = pObj.transform,
                    renderer = sr,
                    distanciaOrbital = UnityEngine.Random.Range(0.8f, radioZona * 0.92f),
                    anguloActual = (360f / totalParticulas) * i + UnityEngine.Random.Range(-12f, 12f),
                    velocidadOrbital = UnityEngine.Random.Range(100f, 190f),
                    desfaseOscilacion = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    escalaBase = escala
                };

                _particulasVentisca.Add(part);
            }
        }

        void OnDisable()
        {
            Todas.Remove(this);
            if (_fuenteAudioEra != null && _fuenteAudioEra.isPlaying)
            {
                _fuenteAudioEra.Stop();
            }
        }

        public override void OnDestroy()
        {
            Todas.Remove(this);
            if (_fuenteAudioEra != null && _fuenteAudioEra.isPlaying)
            {
                _fuenteAudioEra.Stop();
            }
            base.OnDestroy();
        }

        void Update()
        {
            // Animación de aparición rápida con rebote elástico (0.35s)
            if (_tiempoAparicion < 0.35f)
            {
                _tiempoAparicion += Time.deltaTime;
                float t = Mathf.Clamp01(_tiempoAparicion / 0.35f);
                float factor = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                transform.localScale = _escalaObjetivo * Mathf.Clamp(factor, 0.1f, 1.15f);
            }
            else
            {
                // Pulsación sutil de expansión helada
                float pulso = 1f + Mathf.Sin(Time.time * 2.5f) * 0.035f;
                transform.localScale = _escalaObjetivo * pulso;
            }

            // Rotación rápida del vórtice interior (ráfagas de viento en remolino)
            if (vorticeInterior != null)
            {
                vorticeInterior.Rotate(0f, 0f, -125f * Time.deltaTime);
            }

            // Rotación del anillo exterior en contra-viento
            if (anilloExterior != null)
            {
                anilloExterior.Rotate(0f, 0f, 75f * Time.deltaTime);
            }

            // Animar el enjambre de copos y cristales de la ventisca
            for (int i = 0; i < _particulasVentisca.Count; i++)
            {
                var p = _particulasVentisca[i];
                if (p.transform == null) continue;

                p.anguloActual += p.velocidadOrbital * Time.deltaTime;
                float rad = p.anguloActual * Mathf.Deg2Rad;

                // Oscilación radial turbulenta del viento (acerca y aleja los copos del ojo de la tormenta dentro de la zona de la ulti)
                float radioDinamico = Mathf.Clamp(p.distanciaOrbital + Mathf.Sin(Time.time * 3.5f + p.desfaseOscilacion) * 0.35f, 0.4f, radioZona * 0.96f);
                Vector3 nuevaPos = new Vector3(Mathf.Cos(rad) * radioDinamico, Mathf.Sin(rad) * radioDinamico, 0f);
                p.transform.localPosition = nuevaPos;

                // Rotación del copo sobre su eje
                p.transform.Rotate(0f, 0f, 180f * Time.deltaTime);

                // Pulso de brillo por viento
                if (p.renderer != null)
                {
                    Color c = p.renderer.color;
                    c.a = 0.55f + Mathf.Sin(Time.time * 5f + p.desfaseOscilacion) * 0.40f;
                    p.renderer.color = c;
                }

                _particulasVentisca[i] = p;
            }

            // Atenuar destello del tick
            if (destelloTick != null && destelloTick.color.a > 0.01f)
            {
                Color c = destelloTick.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 2.8f);
                destelloTick.color = c;
            }

            // Lógica de daño en Servidor
            if (!IsServer) return;

            _tiempoTranscurrido += Time.deltaTime;
            _timerTick -= Time.deltaTime;

            if (_timerTick <= 0f)
            {
                _timerTick = _intervaloTick;
                AplicarTicks();
                ActivarPulsoTickClientRpc();
            }

            if (_tiempoTranscurrido >= _duracion)
            {
                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        [ClientRpc]
        private void ActivarPulsoTickClientRpc()
        {
            GestorAudio.PlaySFX(TipoSFX.EraHieloHit, 1.0f);
            if (destelloTick != null)
            {
                destelloTick.color = new Color(0.88f, 0.98f, 1f, 0.65f);
            }
        }

        private void AplicarTicks()
        {
            int danoFinal = _danoPorTick;
            if (_lanzador != null)
            {
                // Si el lanzador sigue vivo, sincronizar con su multiplicador total de daño por nivel de run
                danoFinal = Mathf.Max(_danoPorTick, Mathf.RoundToInt(_danoPorTick * _lanzador.MultiplicadorDanoTotal));
            }

            for (int i = _enemigosEnZona.Count - 1; i >= 0; i--)
            {
                var enemigo = _enemigosEnZona[i];
                if (enemigo != null && enemigo.gameObject.activeInHierarchy)
                {
                    enemigo.RecibirDano(danoFinal, new Color(0.42f, 0.88f, 1f, 1f));
                    enemigo.Freeze();
                }
                else
                {
                    _enemigosEnZona.RemoveAt(i);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            var enemigo = other.GetComponent<EnemigoBase>();
            if (enemigo != null && !_enemigosEnZona.Contains(enemigo))
            {
                _enemigosEnZona.Add(enemigo);
                enemigo.Freeze();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;
            var enemigo = other.GetComponent<EnemigoBase>();
            if (enemigo != null)
            {
                _enemigosEnZona.Remove(enemigo);
            }
        }

        // ==================== GENERACIÓN PROCEDURAL DE SPRITES DE VENTISCA ====================
        private static void GenerarSpritesProcedurales()
        {
            if (_spriteAuraBlizzard != null) return;

            // 1. Aura circular de escarcha con degradado radial
            int dimAura = 128;
            var texAura = new Texture2D(dimAura, dimAura, TextureFormat.RGBA32, false);
            texAura.filterMode = FilterMode.Bilinear;
            float centroAura = dimAura / 2f;
            float radioAura = dimAura * 0.48f;

            for (int y = 0; y < dimAura; y++)
            {
                for (int x = 0; x < dimAura; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centroAura, centroAura));
                    if (dist > radioAura)
                    {
                        texAura.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float norm = dist / radioAura;
                        float alpha = Mathf.Pow(1f - norm, 1.8f) * 0.85f;
                        Color col = Color.Lerp(new Color(0.85f, 0.96f, 1f, alpha), new Color(0.18f, 0.55f, 0.95f, alpha * 0.6f), norm);
                        texAura.SetPixel(x, y, col);
                    }
                }
            }
            texAura.Apply();
            _spriteAuraBlizzard = Sprite.Create(texAura, new Rect(0, 0, dimAura, dimAura), new Vector2(0.5f, 0.5f), 32f);

            // 2. Vórtice de viento en espiral (racha huracanada)
            int dimVor = 128;
            var texVor = new Texture2D(dimVor, dimVor, TextureFormat.RGBA32, false);
            texVor.filterMode = FilterMode.Bilinear;
            float cVor = dimVor / 2f;

            for (int y = 0; y < dimVor; y++)
            {
                for (int x = 0; x < dimVor; x++)
                {
                    Vector2 d = new Vector2(x - cVor, y - cVor);
                    float dist = d.magnitude;
                    if (dist > cVor || dist < 4f)
                    {
                        texVor.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float angulo = Mathf.Atan2(d.y, d.x);
                        // Espiral de 3 brazos de viento
                        float espiral = Mathf.Sin(angulo * 3f + (dist * 0.22f));
                        float intensidad = Mathf.Clamp01((espiral - 0.1f) / 0.9f);
                        float bordeSuave = Mathf.Sin((dist / cVor) * Mathf.PI);
                        float a = intensidad * bordeSuave * 0.85f;
                        texVor.SetPixel(x, y, new Color(0.80f, 0.95f, 1f, a));
                    }
                }
            }
            texVor.Apply();
            _spriteVorticeBlizzard = Sprite.Create(texVor, new Rect(0, 0, dimVor, dimVor), new Vector2(0.5f, 0.5f), 32f);

            // 3. Anillo exterior de ráfagas periféricas
            int dimAnillo = 128;
            var texAnillo = new Texture2D(dimAnillo, dimAnillo, TextureFormat.RGBA32, false);
            texAnillo.filterMode = FilterMode.Bilinear;
            float cAnillo = dimAnillo / 2f;
            float rAnilloMin = dimAnillo * 0.32f;
            float rAnilloMax = dimAnillo * 0.48f;

            for (int y = 0; y < dimAnillo; y++)
            {
                for (int x = 0; x < dimAnillo; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cAnillo, cAnillo));
                    if (dist < rAnilloMin || dist > rAnilloMax)
                    {
                        texAnillo.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float angulo = Mathf.Atan2(y - cAnillo, x - cAnillo);
                        float patron = Mathf.Sin(angulo * 6f + dist * 0.3f);
                        float norm = (dist - rAnilloMin) / (rAnilloMax - rAnilloMin);
                        float curva = Mathf.Sin(norm * Mathf.PI);
                        float a = Mathf.Clamp01((patron + 1f) * 0.5f) * curva * 0.75f;
                        texAnillo.SetPixel(x, y, new Color(0.75f, 0.95f, 1f, a));
                    }
                }
            }
            texAnillo.Apply();
            _spriteAnilloBlizzard = Sprite.Create(texAnillo, new Rect(0, 0, dimAnillo, dimAnillo), new Vector2(0.5f, 0.5f), 32f);

            // 4. Copo de nieve de 6 puntas
            int dimCopo = 32;
            var texCopo = new Texture2D(dimCopo, dimCopo, TextureFormat.RGBA32, false);
            texCopo.filterMode = FilterMode.Point;
            float cCopo = dimCopo / 2f;

            for (int y = 0; y < dimCopo; y++)
            {
                for (int x = 0; x < dimCopo; x++)
                {
                    Vector2 d = new Vector2(x - cCopo, y - cCopo);
                    float dist = d.magnitude;
                    if (dist > cCopo)
                    {
                        texCopo.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float ang = Mathf.Atan2(d.y, d.x);
                        float puntas = Mathf.Abs(Mathf.Cos(ang * 3f)); // 6 puntas simétricas
                        bool rayo = puntas > 0.88f && dist < (cCopo * 0.9f);
                        bool centro = dist < 3.2f;
                        bool rama = puntas > 0.70f && dist > 5f && dist < 10f;

                        if (rayo || centro || rama)
                        {
                            texCopo.SetPixel(x, y, new Color(1f, 1f, 1f, 0.95f));
                        }
                        else
                        {
                            texCopo.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }
            texCopo.Apply();
            _spriteCopoBlizzard = Sprite.Create(texCopo, new Rect(0, 0, dimCopo, dimCopo), new Vector2(0.5f, 0.5f), 16f);

            // 5. Cristal de hielo en diamante
            int dimCris = 32;
            var texCris = new Texture2D(dimCris, dimCris, TextureFormat.RGBA32, false);
            texCris.filterMode = FilterMode.Point;
            float cCris = dimCris / 2f;

            for (int y = 0; y < dimCris; y++)
            {
                for (int x = 0; x < dimCris; x++)
                {
                    float dx = Mathf.Abs(x - cCris) / (cCris * 0.65f);
                    float dy = Mathf.Abs(y - cCris) / (cCris * 0.95f);
                    if (dx + dy <= 1f)
                    {
                        float brillo = 1f - (dx + dy) * 0.5f;
                        texCris.SetPixel(x, y, new Color(0.70f, 0.95f, 1f, brillo));
                    }
                    else
                    {
                        texCris.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texCris.Apply();
            _spriteCristalBlizzard = Sprite.Create(texCris, new Rect(0, 0, dimCris, dimCris), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
