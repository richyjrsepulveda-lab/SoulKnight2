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

        [Header("Explosión en Área")]
        public bool tieneExplosionArea = false;
        public float radioArea = 3.2f;
        public int danoArea = 22; // Menor daño a los enemigos secundarios en el área

        public NetworkVariable<Vector2> DireccionSincronizada = new NetworkVariable<Vector2>(
            Vector2.right,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

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
                sr.sortingOrder = 8;
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
                // Detección continua para evitar traspasar muros o enemigos a alta velocidad
                RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.28f, dir, paso);
                foreach (var hit in hits)
                {
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
                if (aplicaCongelacion || tieneExplosionArea)
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
                    // Daño completo de impacto directo
                    enemigo.RecibirDano(dano);

                    if (aplicaQuemadura)
                    {
                        enemigo.AplicarQuemadura();
                    }

                    if (aplicaCongelacion)
                    {
                        enemigo.Freeze(); // 100% de probabilidad garantizada al impactar
                    }

                    // Detonación en área para dañar y congelar a los enemigos cercanos
                    if (aplicaCongelacion || tieneExplosionArea)
                    {
                        DetonarExplosionArea(enemigo);
                    }

                    DestruirProyectil();
                    return true;
                }
            }

            return false;
        }

        private void DetonarExplosionArea(EnemigoBase enemigoDirecto)
        {
            Vector2 centro = transform.position;
            int danoCercanos = danoArea > 0 ? danoArea : Mathf.RoundToInt(dano * 0.55f);

            Collider2D[] cols = Physics2D.OverlapCircleAll(centro, radioArea);
            foreach (var col in cols)
            {
                if (col == null || col.gameObject == gameObject) continue;
                var enCercano = col.GetComponent<EnemigoBase>();
                if (enCercano != null && enCercano != enemigoDirecto)
                {
                    enCercano.RecibirDano(danoCercanos);

                    if (aplicaCongelacion)
                    {
                        enCercano.Freeze(); // 100% garantizado para todos en el área
                    }

                    if (aplicaQuemadura)
                    {
                        enCercano.AplicarQuemadura();
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
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 12;
            lr.startWidth = 0.22f;
            lr.endWidth = 0.02f;
            lr.startColor = esHielo ? new Color(0.4f, 0.95f, 1f, 0.95f) : new Color(1f, 0.6f, 0.1f, 0.95f);
            lr.endColor = esHielo ? new Color(0.8f, 1f, 1f, 0f) : new Color(1f, 0.2f, 0f, 0f);

            const int puntos = 16;
            lr.positionCount = puntos;

            efectoObj.AddComponent<AnimacionAnilloExplosion>().Inicializar(radioArea, esHielo);
        }

        private void DestruirProyectil()
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
