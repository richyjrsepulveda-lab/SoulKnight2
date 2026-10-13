using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Portal ubicado al norte del Lobby pacífico que conduce a la mazmorra de combate.
    /// Exige que TODOS los jugadores de la sala entren al portal y tengan personaje asignado.
    /// </summary>
    public class PortalLobby : MonoBehaviour
    {
        public ControladorLobby controladorLobby;
        private readonly HashSet<ulong> _jugadoresEnPortal = new HashSet<ulong>();

        public HashSet<ulong> JugadoresEnPortal => _jugadoresEnPortal;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var personaje = collision.GetComponent<Personaje>();
            if (personaje != null)
            {
                _jugadoresEnPortal.Add(personaje.OwnerClientId);

                if (controladorLobby != null)
                {
                    controladorLobby.RevisarPortalMazmorra(_jugadoresEnPortal, mostrarAviso: true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var personaje = collision.GetComponent<Personaje>();
            if (personaje != null)
            {
                _jugadoresEnPortal.Remove(personaje.OwnerClientId);

                if (controladorLobby != null)
                {
                    controladorLobby.RevisarPortalMazmorra(_jugadoresEnPortal, mostrarAviso: false);
                }
            }
        }
    }
}

