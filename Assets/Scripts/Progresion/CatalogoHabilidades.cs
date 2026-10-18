using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Identificador de habilidades por personaje (hasta 4 por personaje).
    /// </summary>
    public enum IdHabilidad : byte
    {
        Ninguna = 0,

        // FUEGO
        Fuego_BolaFuego = 1,      // Básica (Click)
        Fuego_Lanzallamas = 2,    // Habilidad 1 (E)
        Fuego_Armadura = 3,       // Habilidad 2 (Q)
        Fuego_Meteorito = 4,      // Ultimate (R) - Nueva habilidad para completar el kit de 4

        // HIELO
        Hielo_RayoHielo = 11,     // Básica (Click)
        Hielo_BolaHielo = 12,     // Habilidad 1 (E)
        Hielo_Armadura = 13,      // Habilidad 2 (Q)
        Hielo_EraHielo = 14,      // Ultimate (R)

        // ELÉCTRICO
        Elec_Espada = 21,         // Básica (Click)
        Elec_Dash = 22,           // Habilidad 1 (E)
        Elec_Teleport = 23,       // Habilidad 2 (Q)
        Elec_Rayo = 24            // Ultimate (R)
    }

    [Serializable]
    public class DefinicionHabilidad
    {
        public IdHabilidad id;
        public TipoPersonaje personaje;
        public string nombre;
        public string descripcion;
        public string descripcionTecnica;      // Descripción técnica detallada (daño, ralentización, duración, etc.)
        public int slotPorDefecto;             // 0: LMB, 1: E, 2: Q, 3: R
        public int nivelMaestriaRequerido;     // Nivel de maestría permanente en la cuenta (1: Básica, 2: Hab1, 3: Hab2, 4: Ultimate)
        public int nivelRequeridoRun;          // Nivel en partida (1-10) en que se destraba durante la run
    }

    /// <summary>
    /// Base de datos centralizada de información de habilidades por personaje.
    /// </summary>
    public static class CatalogoHabilidades
    {
        public static readonly List<TipoPersonaje> PersonajesJugables = new List<TipoPersonaje>
        {
            TipoPersonaje.Fuego,
            TipoPersonaje.Hielo,
            TipoPersonaje.Electrico
        };

        public static int MaxPersonajesJugables => PersonajesJugables.Count;

        private static readonly Dictionary<IdHabilidad, Sprite> _cacheSprites = new Dictionary<IdHabilidad, Sprite>();

        public static Sprite ObtenerSpriteHabilidad(IdHabilidad id)
        {
            if (id == IdHabilidad.Ninguna) return null;
            if (_cacheSprites.TryGetValue(id, out var cached) && cached != null)
            {
                return cached;
            }

            Sprite sp = Resources.Load<Sprite>($"IconosHabilidades/{id}");
            if (sp != null)
            {
                _cacheSprites[id] = sp;
            }
            return sp;
        }

        private static readonly Dictionary<IdHabilidad, DefinicionHabilidad> _habilidades = new Dictionary<IdHabilidad, DefinicionHabilidad>
        {
            // FUEGO
            {
                IdHabilidad.Fuego_BolaFuego,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Fuego_BolaFuego,
                    personaje = TipoPersonaje.Fuego,
                    nombre = "Bola de Fuego",
                    descripcion = "Disparo ígneo estándar con quemadura continua.",
                    descripcionTecnica = "Disparo de fuego que inflige daño continuo y quema a los enemigos durante 3.5 segundos.",
                    slotPorDefecto = 0,
                    nivelMaestriaRequerido = 1,
                    nivelRequeridoRun = 1
                }
            },
            {
                IdHabilidad.Fuego_Lanzallamas,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Fuego_Lanzallamas,
                    personaje = TipoPersonaje.Fuego,
                    nombre = "Lanzallamas Fluido",
                    descripcion = "Chorro continuo que calcina enemigos en cono frontal.",
                    descripcionTecnica = "Chorro continuo de fuego en cono frontal que calcina y daña a corta distancia.",
                    slotPorDefecto = 1,
                    nivelMaestriaRequerido = 2,
                    nivelRequeridoRun = 3
                }
            },
            {
                IdHabilidad.Fuego_Armadura,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Fuego_Armadura,
                    personaje = TipoPersonaje.Fuego,
                    nombre = "Armadura de Fuego",
                    descripcion = "Coraza de magma que reduce masivamente el daño recibido.",
                    descripcionTecnica = "Coraza de magma que reduce un 80% del daño recibido durante 10 segundos.",
                    slotPorDefecto = 2,
                    nivelMaestriaRequerido = 3,
                    nivelRequeridoRun = 5
                }
            },
            {
                IdHabilidad.Fuego_Meteorito,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Fuego_Meteorito,
                    personaje = TipoPersonaje.Fuego,
                    nombre = "Supernova Felina",
                    descripcion = "Estallido masivo de 360° y furor ígneo definitivo.",
                    descripcionTecnica = "Detona una explosión de 360°. Por 8s: velocidad aumentada, rastro de fuego, armadura y lanzallamas ilimitados, y escopeta triple de bolas de fuego.",
                    slotPorDefecto = 3,
                    nivelMaestriaRequerido = 4,
                    nivelRequeridoRun = 7
                }
            },

            // HIELO
            {
                IdHabilidad.Hielo_RayoHielo,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Hielo_RayoHielo,
                    personaje = TipoPersonaje.Hielo,
                    nombre = "Rayo Glacial",
                    descripcion = "Haz congelante continuo que ralentiza y erosiona a los enemigos.",
                    descripcionTecnica = "Rayo de hielo continuo hacia el cursor que daña y ralentiza a los enemigos un 50%.",
                    slotPorDefecto = 0,
                    nivelMaestriaRequerido = 1,
                    nivelRequeridoRun = 1
                }
            },
            {
                IdHabilidad.Hielo_BolaHielo,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Hielo_BolaHielo,
                    personaje = TipoPersonaje.Hielo,
                    nombre = "Orbe Gélido",
                    descripcion = "Detona en área congelando al 100% a todos los enemigos cercanos.",
                    descripcionTecnica = "Orbe de hielo que detona en área congelando por completo a los enemigos durante 2.5 segundos.",
                    slotPorDefecto = 1,
                    nivelMaestriaRequerido = 2,
                    nivelRequeridoRun = 3
                }
            },
            {
                IdHabilidad.Hielo_Armadura,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Hielo_Armadura,
                    personaje = TipoPersonaje.Hielo,
                    nombre = "Armadura Helada",
                    descripcion = "80% de reducción de daño con espinas de escarcha.",
                    descripcionTecnica = "Escudo glacial que reduce un 80% del daño recibido y congela a los atacantes cercanos.",
                    slotPorDefecto = 2,
                    nivelMaestriaRequerido = 3,
                    nivelRequeridoRun = 5
                }
            },
            {
                IdHabilidad.Hielo_EraHielo,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Hielo_EraHielo,
                    personaje = TipoPersonaje.Hielo,
                    nombre = "Era de Hielo",
                    descripcion = "Aura glacial gigante que congela y castiga toda el área.",
                    descripcionTecnica = "Ventisca bajo cero que congela e inflige daño continuo a todos los enemigos de la sala por 6 segundos.",
                    slotPorDefecto = 3,
                    nivelMaestriaRequerido = 4,
                    nivelRequeridoRun = 7
                }
            },

            // ELÉCTRICO
            {
                IdHabilidad.Elec_Espada,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Elec_Espada,
                    personaje = TipoPersonaje.Electrico,
                    nombre = "Corte Voltaico",
                    descripcion = "Ataque veloz cuerpo a cuerpo que empuja a los rivales.",
                    descripcionTecnica = "Tajo rápido con espada eléctrica que inflige daño cuerpo a cuerpo y empuja a los enemigos.",
                    slotPorDefecto = 0,
                    nivelMaestriaRequerido = 1,
                    nivelRequeridoRun = 1
                }
            },
            {
                IdHabilidad.Elec_Dash,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Elec_Dash,
                    personaje = TipoPersonaje.Electrico,
                    nombre = "Ráfaga Relámpago",
                    descripcion = "Desplazamiento supersónico que atraviesa y electrocuta.",
                    descripcionTecnica = "Embestida relámpago con invulnerabilidad total que atraviesa a los enemigos y los aturde.",
                    slotPorDefecto = 1,
                    nivelMaestriaRequerido = 2,
                    nivelRequeridoRun = 3
                }
            },
            {
                IdHabilidad.Elec_Teleport,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Elec_Teleport,
                    personaje = TipoPersonaje.Electrico,
                    nombre = "Salto Cuántico",
                    descripcion = "Teletransporte instantáneo con ondas expansivas de choque.",
                    descripcionTecnica = "Teletransporte instantáneo hacia el cursor desatando una onda de choque expansiva.",
                    slotPorDefecto = 2,
                    nivelMaestriaRequerido = 3,
                    nivelRequeridoRun = 5
                }
            },
            {
                IdHabilidad.Elec_Rayo,
                new DefinicionHabilidad
                {
                    id = IdHabilidad.Elec_Rayo,
                    personaje = TipoPersonaje.Electrico,
                    nombre = "Rayo Devastador",
                    descripcion = "Carga de 2s seguida de un láser eléctrico destructor de largo alcance.",
                    descripcionTecnica = "Láser voltaico masivo que atraviesa muros y electrocuta a todos los enemigos a su paso.",
                    slotPorDefecto = 3,
                    nivelMaestriaRequerido = 4,
                    nivelRequeridoRun = 7
                }
            }
        };

        public static DefinicionHabilidad Obtener(IdHabilidad id)
        {
            return _habilidades.TryGetValue(id, out var def) ? def : null;
        }

        public static List<DefinicionHabilidad> ObtenerHabilidadesPersonaje(TipoPersonaje pj)
        {
            var res = new List<DefinicionHabilidad>();
            foreach (var kvp in _habilidades)
            {
                if (kvp.Value.personaje == pj)
                {
                    res.Add(kvp.Value);
                }
            }
            return res;
        }

        public static IdHabilidad ObtenerHabilidadBasica(TipoPersonaje pj)
        {
            return pj switch
            {
                TipoPersonaje.Fuego => IdHabilidad.Fuego_BolaFuego,
                TipoPersonaje.Hielo => IdHabilidad.Hielo_RayoHielo,
                TipoPersonaje.Electrico => IdHabilidad.Elec_Espada,
                _ => IdHabilidad.Ninguna
            };
        }
    }
}
