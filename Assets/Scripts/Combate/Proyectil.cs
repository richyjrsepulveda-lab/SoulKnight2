using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Prefab reutilizable para todos los proyectiles (Bola de Fuego, Bola de Hielo, etc.).
    /// - Sorting Order alto (8) y Z adelantada (-0.4f) para garantizar máxima visibilidad por encima del suelo y muros.
    /// - Detección continua (CircleCastAll) para evitar túneles.
    /// - Daño directo y explosión en área opcional (Bola de Hielo: 100% congelación y daño en área menor a enemigos cercanos).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Proyectil : NetworkBehaviour
    {
        [Header("Configuración")]
        public float velocidad = 18f;
        public int dano = 20;
        public float tiempoVida = 5f;
        public bool destruirConPared = true;

        [Header("Efectos al impactar")]
        public bool aplicaQuemadura = false;
        public bool aplicaCongelacion = false;
        public float probCongelacion = 1f;
        public bool aplicaRalentizacion = false;
        public bool aplicaEsporas = false;

        [Header("Explosión en Área")]
        public bool tieneExplosionArea = false;
        public float radioArea = 3.2f;
        public int danoArea = 22; // Menor daño a los enemigos secundarios en el área

        public NetworkVariable<Vector2> DireccionSincronizada = new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkVariable<int> TipoElemento = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public static readonly System.Collections.Generic.List<Proyectil> Todos = new System.Collections.Generic.List<Proyectil>(64);
        private static readonly RaycastHit2D[] _circleCastBuffer = new RaycastHit2D[16];
        private static readonly Collider2D[] _overlapBuffer = new Collider2D[24];

        protected virtual void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        protected virtual void OnDisable()
        {
            Todos.Remove(this);
        }

        private Vector2 _direccion = Vector2.right;
        private ulong _lanzadorId;
        private float _tiempoVivo = 0f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Garantizar que el sprite sea 100% visible por encima del suelo y paredes
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 9;
            }

            // Adelantar Z para estar al frente de la cámara ortográfica
            transform.position = new Vector3(transform.position.x, transform.position.y, -0.4f);

            if (IsServer)
            {
                DireccionSincronizada.Value = _direccion;
            }
            else
            {
                if (DireccionSincronizada.Value.sqrMagnitude > 0.001f)
                {
                    _direccion = DireccionSincronizada.Value;
                }
            }

            ActualizarRotacion(_direccion);
            DireccionSincronizada.OnValueChanged += (prev, act) =>
            {
                _direccion = act;
                ActualizarRotacion(act);
            };

            TipoElemento.OnValueChanged += (prev, act) => ActualizarVisualElemento(act);
            ActualizarVisualElemento(TipoElemento.Value);
        }

        public void Configurar(Vector2 direccion, int danoBase, ulong lanzadorId, bool quemadura = false, bool congelacion = false, float probCong = 1f)
        {
            _direccion = direccion.normalized;
            if (IsSpawned && IsServer)
            {
                DireccionSincronizada.Value = _direccion;
            }
            dano = danoBase;
            _lanzadorId = lanzadorId;
            aplicaQuemadura = quemadura;
            aplicaCongelacion = congelacion;
            probCongelacion = probCong;

            if (congelacion)
            {
                tieneExplosionArea = true;
                danoArea = Mathf.RoundToInt(danoBase * 0.55f);
                probCongelacion = 1f; // 100% garantizado
            }

            ActualizarRotacion(_direccion);
        }

        private void ActualizarRotacion(Vector2 dir)
        {
            if (dir.sqrMagnitude > 0.001f)
            {
                float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angulo);
            }
        }

        void Update()
        {
            Vector2 dir = _direccion;
            if (!IsServer && DireccionSincronizada != null && DireccionSincronizada.Value.sqrMagnitude > 0.001f)
            {
                dir = DireccionSincronizada.Value;
            }

            float paso = velocidad * Time.deltaTime;

            if (IsServer)
            {
                // Detección continua optimizada sin asignaciones de memoria
                int hitsCount = Physics2D.CircleCastNonAlloc(transform.position, 0.28f, dir, _circleCastBuffer, paso);
                for (int i = 0; i < hitsCount; i++)
                {
                    var hit = _circleCastBuffer[i];
                    if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                    if (ProcesarImpacto(hit.collider))
                    {
                        return;
                    }
                }
            }

            // Movimiento manteniendo Z al frente
            Vector3 nuevaPos = transform.position + (Vector3)(dir * paso);
            nuevaPos.z = -0.4f;
            transform.position = nuevaPos;

            if (IsServer)
            {
                _tiempoVivo += Time.deltaTime;
                if (_tiempoVivo >= tiempoVida)
                {
                    if (proyectilEnemigo)
                    {
                        DetonarImpactoEnemigo();
                    }
                    DestruirProyectil();
                }
            }
        }

        [Header("Tipo de Proyectil")]
        public bool proyectilEnemigo = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;
            ProcesarImpacto(other);
        }

        private bool ProcesarImpacto(Collider2D other)
        {
            if (other == null || other.gameObject == gameObject) return false;

            // Impacto con pared u obstáculo
            bool esPared = false;
            try
            {
                esPared = other.CompareTag("Pared");
            }
            catch
            {
                esPared = false;
            }

            if (!esPared && (other.name.IndexOf("pared", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                             other.name.IndexOf("muro", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                             other.name.IndexOf("columna", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                             other.name.IndexOf("obstaculo", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                esPared = true;
            }

            if (destruirConPared && esPared)
            {
                if (proyectilEnemigo)
                {
                    DetonarImpactoEnemigo();
                }
                else if (aplicaCongelacion || tieneExplosionArea)
                {
                    DetonarExplosionArea(null);
                }
                DestruirProyectil();
                return true;
            }

            if (proyectilEnemigo)
            {
                // Impacta a jugadores
                var personaje = other.GetComponent<Personaje>();
                if (personaje != null && !personaje.EstaMuerto.Value)
                {
                    personaje.RecibirDano(dano);
                    personaje.AplicarKnockback(_direccion, 220f);
                    if (aplicaCongelacion)
                    {
                        personaje.AplicarCongelacion(2.0f);
                    }
                    else if (aplicaRalentizacion)
                    {
                        personaje.AplicarRalentizacion(3.0f);
                    }
                    if (aplicaQuemadura)
                    {
                        personaje.AplicarQuemadura(4.0f, Mathf.Max(3, Mathf.RoundToInt(dano * 0.35f)));
                    }
                    if (aplicaEsporas)
                    {
                        personaje.AplicarEsporasVeneno(4.0f, Mathf.Max(3, Mathf.RoundToInt(dano * 0.3f)));
                    }
                    DetonarImpactoEnemigo();
                    DestruirProyectil();
                    return true;
                }
            }
            else
            {
                // Ignorar a TODOS los jugadores aliados
                var personaje = other.GetComponent<Personaje>();
                if (personaje != null)
                {
                    return false;
                }

                // Impacto con enemigo
                var enemigo = other.GetComponent<EnemigoBase>();
                if (enemigo != null)
                {
                    Color colorImpacto = Color.white;
                    if (aplicaCongelacion)
                    {
                        colorImpacto = new Color(0.42f, 0.88f, 1f, 1f); // Azul clarito helado
                    }
                    else if (aplicaQuemadura)
                    {
                        colorImpacto = new Color(0.96f, 0.40f, 0.05f, 1f); // Naranja oscuro fuego
                    }
                    else
                    {
                        for (int i = 0; i < Personaje.Todos.Count; i++)
                        {
                            var pj = Personaje.Todos[i];
                            if (pj != null && pj.OwnerClientId == _lanzadorId)
                            {
                                if (pj.TipoPj == TipoPersonaje.Hielo) colorImpacto = new Color(0.42f, 0.88f, 1f, 1f);
                                else if (pj.TipoPj == TipoPersonaje.Electrico) colorImpacto = new Color(1f, 0.96f, 0.22f, 1f);
                                else if (pj.TipoPj == TipoPersonaje.Fuego) colorImpacto = new Color(0.96f, 0.40f, 0.05f, 1f);
                                break;
                            }
                        }
                    }

                    // Daño completo de impacto directo
                    enemigo.RecibirDano(dano, colorImpacto);

                    if (aplicaQuemadura)
                    {
                        int danoTickQuemadura = Mathf.Max(12, Mathf.RoundToInt(dano * 0.45f));
                        enemigo.AplicarQuemadura(danoTickQuemadura);
                    }

                    if (aplicaCongelacion)
                    {
                        enemigo.Freeze(); // 100% de probabilidad garantizada al impactar
                    }

                    // Detonación en área para dañar y congelar a los enemigos cercanos
                    if (aplicaCongelacion || tieneExplosionArea)
                    {
                        DetonarExplosionArea(enemigo, colorImpacto);
                    }

                    DestruirProyectil();
                    return true;
                }
            }

            return false;
        }

        private void DetonarExplosionArea(EnemigoBase enemigoDirecto, Color colorDanoArea = default)
        {
            if (colorDanoArea == default)
            {
                colorDanoArea = aplicaCongelacion ? new Color(0.42f, 0.88f, 1f, 1f) : (aplicaQuemadura ? new Color(0.96f, 0.40f, 0.05f, 1f) : Color.white);
            }

            Vector2 centro = transform.position;
            int danoCercanos = danoArea > 0 ? danoArea : Mathf.RoundToInt(dano * 0.55f);

            int count = Physics2D.OverlapCircleNonAlloc(centro, radioArea, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null || col.gameObject == gameObject) continue;
                var enCercano = col.GetComponent<EnemigoBase>();
                if (enCercano != null && enCercano != enemigoDirecto)
                {
                    enCercano.RecibirDano(danoCercanos, colorDanoArea);

                    if (aplicaCongelacion)
                    {
                        enCercano.Freeze(); // 100% garantizado para todos en el área
                    }

                    if (aplicaQuemadura)
                    {
                        int danoTickQuemadura = Mathf.Max(10, Mathf.RoundToInt(danoCercanos * 0.45f));
                        enCercano.AplicarQuemadura(danoTickQuemadura);
                    }

                    Vector2 dirKnock = ((Vector2)enCercano.transform.position - centro).normalized;
                    if (dirKnock.sqrMagnitude < 0.001f) dirKnock = Vector2.up;
                    enCercano.AplicarKnockback(dirKnock, 240f);
                }
            }

            MostrarEfectoExplosionClientRpc(centro, aplicaCongelacion);
        }

        [ClientRpc]
        private void MostrarEfectoExplosionClientRpc(Vector2 pos, bool esHielo)
        {
            var efectoObj = new GameObject("EfectoExplosionProyectil");
            efectoObj.transform.position = new Vector3(pos.x, pos.y, -0.6f);

            var lr = efectoObj.AddComponent<LineRenderer>();
            lr.material = MaterialUtil.SpritesDefault;
            lr.sortingOrder = 12;
            lr.startWidth = 0.22f;
            lr.endWidth = 0.02f;
            lr.startColor = esHielo ? new Color(0.4f, 0.95f, 1f, 0.95f) : new Color(1f, 0.6f, 0.1f, 0.95f);
            lr.endColor = esHielo ? new Color(0.8f, 1f, 1f, 0f) : new Color(1f, 0.2f, 0f, 0f);

            const int puntos = 16;
            lr.positionCount = puntos;

            efectoObj.AddComponent<AnimacionAnilloExplosion>().Inicializar(radioArea, esHielo);
        }

        private void DetonarImpactoEnemigo()
        {
            if (!IsServer || !proyectilEnemigo) return;

            Vector2 pos = transform.position;
            if (aplicaEsporas || TipoElemento.Value == 3)
            {
                ZonaPeligroPiso.Instanciar(pos, 2.0f, TipoZonaPeligro.Esporas, 6.5f, esServidor: true, danoTick: 5);
                SpawnearZonaPeligroClientRpc(pos, 2.0f, (int)TipoZonaPeligro.Esporas, 6.5f);
            }
            else if (aplicaQuemadura || TipoElemento.Value == 1)
            {
                ZonaPeligroPiso.Instanciar(pos, 1.5f, TipoZonaPeligro.Fuego, 5.0f, esServidor: true, danoTick: 5);
                SpawnearZonaPeligroClientRpc(pos, 1.5f, (int)TipoZonaPeligro.Fuego, 5.0f);
            }
            else if (aplicaCongelacion || aplicaRalentizacion || TipoElemento.Value == 2)
            {
                ZonaPeligroPiso.Instanciar(pos, 1.6f, TipoZonaPeligro.Hielo, 5.5f, esServidor: true, danoTick: 3);
                SpawnearZonaPeligroClientRpc(pos, 1.6f, (int)TipoZonaPeligro.Hielo, 5.5f);
            }
        }

        [ClientRpc]
        private void SpawnearZonaPeligroClientRpc(Vector2 pos, float radio, int tipoZona, float duracion)
        {
            if (IsServer) return;
            ZonaPeligroPiso.Instanciar(pos, radio, (TipoZonaPeligro)tipoZona, duracion, esServidor: false);
        }

        private static Sprite _spriteProjEsporas;
        private static Sprite _spriteProjFuego;
        private static Sprite _spriteProjHielo;
        private static Sprite _spriteProjArena;
        private static Sprite _spriteProjArcano;
        private static Sprite _spriteHaloBase;
        private GameObject _haloBrillo;

        public void ActualizarVisualElemento(int elem)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sortingOrder = 9;

            if (proyectilEnemigo)
            {
                // Escala generosa visible de alto impacto
                transform.localScale = Vector3.one * 1.35f;

                Color colorHalo;
                switch (elem)
                {
                    case 3: // Planta / Esporas
                        if (_spriteProjEsporas == null) _spriteProjEsporas = GenerarSpriteBalaEsporas();
                        sr.sprite = _spriteProjEsporas;
                        colorHalo = new Color(0.48f, 1f, 0.22f, 0.85f);
                        aplicaEsporas = true;
                        break;
                    case 1: // Fuego
                        if (_spriteProjFuego == null) _spriteProjFuego = GenerarSpriteBalaFuego();
                        sr.sprite = _spriteProjFuego;
                        colorHalo = new Color(1f, 0.45f, 0.10f, 0.85f);
                        aplicaQuemadura = true;
                        break;
                    case 2: // Hielo
                        if (_spriteProjHielo == null) _spriteProjHielo = GenerarSpriteBalaHielo();
                        sr.sprite = _spriteProjHielo;
                        colorHalo = new Color(0.35f, 0.85f, 1f, 0.85f);
                        aplicaRalentizacion = true;
                        break;
                    case 4: // Arena
                        if (_spriteProjArena == null) _spriteProjArena = GenerarSpriteBalaArena();
                        sr.sprite = _spriteProjArena;
                        colorHalo = new Color(1f, 0.80f, 0.25f, 0.85f);
                        break;
                    default: // Normal / Piedra
                        if (_spriteProjArcano == null) _spriteProjArcano = GenerarSpriteBalaArcana();
                        sr.sprite = _spriteProjArcano;
                        colorHalo = new Color(1f, 0.20f, 0.20f, 0.85f);
                        break;
                }

                if (_haloBrillo == null)
                {
                    _haloBrillo = new GameObject("HaloBrilloProyectil");
                    _haloBrillo.transform.SetParent(transform, false);
                    _haloBrillo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
                    _haloBrillo.transform.localScale = Vector3.one * 1.45f;
                    var srHalo = _haloBrillo.AddComponent<SpriteRenderer>();
                    if (_spriteHaloBase == null) _spriteHaloBase = GenerarSpriteHalo();
                    srHalo.sprite = _spriteHaloBase;
                    srHalo.sortingOrder = 8;
                    srHalo.color = colorHalo;
                }
            }
        }

        private static Sprite GenerarSpriteBalaEsporas()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBordeRojo = new Color(0.96f, 0.15f, 0.15f, 1f); // Trazo rojo de peligro
            Color cVioleta = new Color(0.55f, 0.15f, 0.65f, 1f);
            Color cLima = new Color(0.50f, 1f, 0.25f, 1f);
            Color cNucleo = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    int dy = Mathf.Abs(y - 11);
                    int dist = dx + dy;
                    if (dist == 8) tex.SetPixel(x, y, cBordeRojo);
                    else if (dist < 8 && dist >= 6) tex.SetPixel(x, y, cVioleta);
                    else if (dist < 6 && dist >= 3) tex.SetPixel(x, y, cLima);
                    else if (dist < 3) tex.SetPixel(x, y, cNucleo);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteBalaFuego()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBordeRojo = new Color(0.96f, 0.15f, 0.15f, 1f);
            Color cFuego = new Color(1f, 0.40f, 0.05f, 1f);
            Color cOro = new Color(1f, 0.90f, 0.20f, 1f);
            Color cNucleo = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    int dy = Mathf.Abs(y - 11);
                    int dist = dx + dy;
                    if (dist == 8) tex.SetPixel(x, y, cBordeRojo);
                    else if (dist < 8 && dist >= 5) tex.SetPixel(x, y, cFuego);
                    else if (dist < 5 && dist >= 2) tex.SetPixel(x, y, cOro);
                    else if (dist < 2) tex.SetPixel(x, y, cNucleo);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteBalaHielo()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBordeRojo = new Color(0.96f, 0.15f, 0.15f, 1f);
            Color cCianOscuro = new Color(0.15f, 0.45f, 0.85f, 1f);
            Color cCianClaro = new Color(0.45f, 0.92f, 1f, 1f);
            Color cNucleo = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    int dy = Mathf.Abs(y - 11);
                    int dist = dx + dy;
                    if (dist == 8) tex.SetPixel(x, y, cBordeRojo);
                    else if (dist < 8 && dist >= 5) tex.SetPixel(x, y, cCianOscuro);
                    else if (dist < 5 && dist >= 2) tex.SetPixel(x, y, cCianClaro);
                    else if (dist < 2) tex.SetPixel(x, y, cNucleo);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteBalaArena()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBordeRojo = new Color(0.96f, 0.15f, 0.15f, 1f);
            Color cArena = new Color(0.85f, 0.55f, 0.15f, 1f);
            Color cOro = new Color(1f, 0.85f, 0.35f, 1f);
            Color cNucleo = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    int dy = Mathf.Abs(y - 11);
                    int dist = dx + dy;
                    if (dist == 8) tex.SetPixel(x, y, cBordeRojo);
                    else if (dist < 8 && dist >= 5) tex.SetPixel(x, y, cArena);
                    else if (dist < 5 && dist >= 2) tex.SetPixel(x, y, cOro);
                    else if (dist < 2) tex.SetPixel(x, y, cNucleo);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteBalaArcana()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBordeRojo = new Color(0.96f, 0.15f, 0.15f, 1f);
            Color cPurpura = new Color(0.40f, 0.10f, 0.55f, 1f);
            Color cRojoVivo = new Color(0.90f, 0.20f, 0.35f, 1f);
            Color cNucleo = Color.white;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    int dy = Mathf.Abs(y - 11);
                    int dist = dx + dy;
                    if (dist == 8) tex.SetPixel(x, y, cBordeRojo);
                    else if (dist < 8 && dist >= 5) tex.SetPixel(x, y, cPurpura);
                    else if (dist < 5 && dist >= 2) tex.SetPixel(x, y, cRojoVivo);
                    else if (dist < 2) tex.SetPixel(x, y, cNucleo);
                    else tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite GenerarSpriteHalo()
        {
            int s = 16;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float rMax = s * 0.48f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - 7.5f;
                    float dy = y - 7.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= rMax)
                    {
                        float a = Mathf.Clamp01(1f - (d / rMax));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * 0.65f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private void DestruirProyectil()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                if (IsServer)
                {
                    NetworkObject.Despawn();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Componente de apoyo visual para expandir el anillo de la explosión y autodestruirse.
    /// </summary>
    public class AnimacionAnilloExplosion : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _radioMax;
        private float _duracion = 0.28f;
        private float _tiempo = 0f;

        public void Inicializar(float radioMax, bool esHielo)
        {
            _lr = GetComponent<LineRenderer>();
            _radioMax = radioMax;
            Destroy(gameObject, _duracion + 0.05f);
        }

        void Update()
        {
            if (_lr == null) return;
            _tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(_tiempo / _duracion);
            float radioActual = Mathf.Lerp(0.3f, _radioMax, t);

            const int puntos = 16;
            for (int i = 0; i < puntos; i++)
            {
                float ang = (float)i / (puntos - 1) * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(ang) * radioActual, Mathf.Sin(ang) * radioActual, 0);
                _lr.SetPosition(i, p);
            }
        }
    }
}
