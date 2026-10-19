using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Sistema de nivel en partida (temporal 1-10, estilo LoL / MOBAs).
    /// - Se reinicia en cada run (nivel 1 al arrancar).
    /// - Gana XP derrotando enemigos y completando niveles.
    /// - Al subir de nivel:
    ///   * Se destraban progresivamente las habilidades del loadout.
    ///   * El daño y poder de las habilidades escala con el nivel.
    /// - Tope máximo: nivel 10.
    /// </summary>
    public class NivelEnPartida : NetworkBehaviour
    {
        public const int NivelMaximo = 10;

        // XP requerida por nivel: nivel 1->2 (100), 2->3 (180), etc.
        private static readonly int[] XPRequeridaNivel = new int[]
        {
            0,    // Nivel 0 (no usado)
            100,  // Nivel 1 -> 2
            180,  // Nivel 2 -> 3
            280,  // Nivel 3 -> 4
            400,  // Nivel 4 -> 5
            540,  // Nivel 5 -> 6
            700,  // Nivel 6 -> 7
            880,  // Nivel 7 -> 8
            1080, // Nivel 8 -> 9
            1300  // Nivel 9 -> 10
        };

        public NetworkVariable<int> NivelActual = new NetworkVariable<int>(1);
        public NetworkVariable<int> XPActual = new NetworkVariable<int>(0);
        public NetworkVariable<int> XPSiguienteNivel = new NetworkVariable<int>(100);

        // Factor multiplicador de daño que escala moderadamente con el nivel: (1.0 + (nivel - 1) * 0.08)
        public float MultiplicadorDano => 1.0f + ((NivelActual.Value - 1) * 0.08f);

        public event Action<int> AlSubirNivel;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NivelActual.Value = 1;
                XPActual.Value = 0;
                XPSiguienteNivel.Value = XPRequeridaNivel[1];
            }

            NivelActual.OnValueChanged += (anterior, actual) =>
            {
                AlSubirNivel?.Invoke(actual);
                if (actual > anterior && (IsOwner || NetworkManager == null || !NetworkManager.IsListening))
                {
                    TextoFlotante.Crear(transform.position + Vector3.up * 2f, $"¡NIVEL {actual}!", Color.yellow);
                    GestorAudio.PlaySFX(TipoSFX.SubirNivel);
                }
            };
        }

        public void AgregarXP(int cantidad)
        {
            bool esAutoridad = IsServer || NetworkManager == null || !NetworkManager.IsListening;
            if (!esAutoridad || cantidad <= 0) return;

            // 1. La maestría sube de nivel junto con el nivel de la run, pero escalando más lento (~35% de la XP de la run)
            var pj = GetComponent<Personaje>();
            if (pj != null && pj.TipoPj != TipoPersonaje.Ninguno)
            {
                int xpMaestria = Mathf.Max(1, Mathf.RoundToInt(cantidad * 0.35f));
                if (NetworkManager != null && NetworkManager.IsListening)
                {
                    OtorgarXPMaestriaClientRpc((byte)pj.TipoPj, xpMaestria);
                }
                else
                {
                    GestorMaestria.AgregarPuntos(pj.TipoPj, xpMaestria);
                }
            }

            // 2. XP y subida de nivel de la run actual (1-10)
            if (NivelActual.Value >= NivelMaximo) return;

            int xpTotal = XPActual.Value + cantidad;

            while (NivelActual.Value < NivelMaximo && xpTotal >= XPSiguienteNivel.Value)
            {
                xpTotal -= XPSiguienteNivel.Value;
                NivelActual.Value++;

                if (NivelActual.Value < NivelMaximo)
                {
                    XPSiguienteNivel.Value = XPRequeridaNivel[NivelActual.Value];
                }
                else
                {
                    XPSiguienteNivel.Value = 0;
                    xpTotal = 0;
                }
            }

            XPActual.Value = xpTotal;
        }

        /// <summary>
        /// Determina si una habilidad está activa en el run según el nivel actual del personaje.
        /// </summary>
        public bool HabilidadDesbloqueadaEnRun(IdHabilidad id)
        {
            var def = CatalogoHabilidades.Obtener(id);
            if (def == null) return false;
            return NivelActual.Value >= def.nivelRequeridoRun;
        }

        [ClientRpc]
        private void OtorgarXPMaestriaClientRpc(byte tipoPjByte, int xp)
        {
            if (IsOwner)
            {
                TipoPersonaje tipo = (TipoPersonaje)tipoPjByte;
                GestorMaestria.AgregarPuntos(tipo, xp);
            }
        }
    }
}
