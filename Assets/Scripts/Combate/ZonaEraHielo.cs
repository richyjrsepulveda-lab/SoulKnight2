using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Zona de área para la ultimate "Era de Hielo".
    /// Daña y congela periódicamente a todos los enemigos dentro durante su duración.
    /// Visuales mejorados: vórtice de hielo giratorio, copos de nieve orbitales,
    /// pulsaciones de escarcha y anillo exterior rúnico.
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

        private Vector3 _escalaObjetivo;
        private float _tiempoAparicion = 0f;

        public static readonly List<ZonaEraHielo> Todas = new List<ZonaEraHielo>();

        public void Configurar(float duracion, int danoTick)
        {
            _duracion = duracion;
            _danoPorTick = danoTick;
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

        void OnDisable()
        {
            Todas.Remove(this);
        }

        void Awake()
        {
            _escalaObjetivo = transform.localScale;
            transform.localScale = Vector3.one * 0.1f; // Comienza pequeño para expandirse
        }

        void Update()
        {
            // Animación de aparición rápida con rebote elástico (0.3s)
            if (_tiempoAparicion < 0.35f)
            {
                _tiempoAparicion += Time.deltaTime;
                float t = Mathf.Clamp01(_tiempoAparicion / 0.35f);
                float factor = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                transform.localScale = _escalaObjetivo * Mathf.Clamp(factor, 0.1f, 1.15f);
            }
            else
            {
                // Pulsación suave de respiración helada
                float pulso = 1f + Mathf.Sin(Time.time * 3f) * 0.04f;
                transform.localScale = _escalaObjetivo * pulso;
            }

            // Rotación del vórtice interior en sentido horario
            if (vorticeInterior != null)
            {
                vorticeInterior.Rotate(0f, 0f, -50f * Time.deltaTime);
            }

            // Rotación del anillo exterior en sentido antihorario
            if (anilloExterior != null)
            {
                anilloExterior.Rotate(0f, 0f, 30f * Time.deltaTime);
            }

            // Atenuar destello del tick
            if (destelloTick != null && destelloTick.color.a > 0.01f)
            {
                Color c = destelloTick.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 2.5f);
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
            if (destelloTick != null)
            {
                destelloTick.color = new Color(0.85f, 0.98f, 1f, 0.55f);
            }
        }

        private void AplicarTicks()
        {
            for (int i = _enemigosEnZona.Count - 1; i >= 0; i--)
            {
                var enemigo = _enemigosEnZona[i];
                if (enemigo != null && enemigo.gameObject.activeInHierarchy)
                {
                    enemigo.RecibirDano(_danoPorTick, new Color(0.42f, 0.88f, 1f, 1f));
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
    }
}
