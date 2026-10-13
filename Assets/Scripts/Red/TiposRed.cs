using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>Los 3 personajes elegibles en el Lobby.</summary>
    public enum TipoPersonaje : byte
    {
        Ninguno = 0,
        Fuego = 1,
        Hielo = 2,
        Electrico = 3
    }

    /// <summary>
    /// Una fila de la lista de jugadores de la sala.
    /// En Godot era el diccionario { id: {"nombre": ..., "personaje": ...} } que el host
    /// retransmitía por RPC. Aquí vive dentro de un NetworkList (SalaRed.Jugadores) y
    /// Netcode lo sincroniza solo cada vez que el host lo cambia.
    /// </summary>
    public struct InfoJugador : INetworkSerializable, IEquatable<InfoJugador>
    {
        public ulong idCliente;
        public FixedString64Bytes nombre;
        public TipoPersonaje personaje;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref idCliente);
            serializer.SerializeValue(ref nombre);
            serializer.SerializeValue(ref personaje);
        }

        public bool Equals(InfoJugador otro) =>
            idCliente == otro.idCliente && nombre.Equals(otro.nombre) && personaje == otro.personaje;
    }

    /// <summary>
    /// Lo que el cliente le manda al host cada tick: SOLO su input, nunca su posición.
    /// El host es quien mueve a todos los personajes (host-autoritativo).
    /// </summary>
    public struct EntradaJugador : INetworkSerializable, IEquatable<EntradaJugador>
    {
        public Vector2 mover;     // WASD normalizado o no, el host lo normaliza
        public Vector2 apuntar;   // posición del mouse en el mundo
        public bool correr;       // Shift
        public bool principal;    // click izquierdo mantenido

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mover);
            serializer.SerializeValue(ref apuntar);
            serializer.SerializeValue(ref correr);
            serializer.SerializeValue(ref principal);
        }

        public bool Equals(EntradaJugador o) =>
            mover == o.mover && apuntar == o.apuntar && correr == o.correr && principal == o.principal;
    }

    /// <summary>Acciones de "un solo toque" (just_pressed en Godot). Se mandan por RPC confiable.</summary>
    public enum Accion : byte
    {
        Principal = 0,   // click izquierdo (al presionarlo)
        Habilidad1 = 1,  // E
        Habilidad2 = 2,  // Q
        Ultimate = 3     // R
    }
}
