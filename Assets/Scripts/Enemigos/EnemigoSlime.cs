using System;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Enemigo Slime:
    /// - Enemigo rápido y saltarín / ondulante.
    /// - Vida baja (50 HP) pero ágil.
    /// - Realiza pequeños impulsos o saltos hacia el jugador más cercano.
    /// </summary>
    public class EnemigoSlime : EnemigoBase
    {
        private Vector3 _escalaBase = Vector3.one;
        private float _tiempoSalto = 0f;

        protected override void Awake()
        {
            vidaMax = 65;
            velocidadBase = 4.6f;
            dano = 6;
            rangoAtaque = 1.4f;
            tiempoEntreAtaques = 0.95f;
            base.Awake();
            _escalaBase = transform.localScale;
        }

        protected void LateUpdate()
        {
            // Squash and stretch visual
            if (EstaCongelado.Value) return;

            float velocidadSq = _rb != null ? _rb.velocity.sqrMagnitude : 0f;
            if (velocidadSq > 0.1f)
            {
                float t = Mathf.PingPong(Time.time * 8f, 1f);
                float sx = Mathf.Lerp(0.85f, 1.18f, t);
                float sy = Mathf.Lerp(1.18f, 0.85f, t);
                transform.localScale = new Vector3(_escalaBase.x * sx, _escalaBase.y * sy, _escalaBase.z);
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _escalaBase, Time.deltaTime * 6f);
            }
        }
    }
}
