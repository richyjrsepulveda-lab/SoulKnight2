using Unity.Netcode.Components;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// NetworkTransform con autoridad del cliente propietario (Client/Owner Authoritative).
    /// Permite que el dueño del personaje se mueva de manera instantánea y con 0ms de delay en su pantalla.
    /// La posición se replica de manera suave y fluida a los demás jugadores.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
