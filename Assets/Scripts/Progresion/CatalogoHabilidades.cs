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
                    descripcionTecnica = "Lanza un proyectil ígneo que inflige 40 de daño base (+25% por nivel en run) y aplica Quemadura durante 3.5s (8 de daño/s). Cooldown: 1.0s. [Click Izq]",
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
                    descripcionTecnica = "Emite un torrente de fuego en abanico continuo durante 5s. Inflige 10 de daño por tick cada 0.15s y reinicia el estado de Quemadura a todos los alcanzados. Cooldown: 20s. [Tecla E]",
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
                    descripcionTecnica = "Genera una coraza ígnea sobre el gato que absorbe y reduce un 80% de todo el daño entrante durante 10 segundos. Cooldown: 20s. [Tecla Q]",
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
                    nombre = "Tormenta de Meteoros",
                    descripcion = "Impacto cósmico devastador en área con daño masivo de fuego.",
                    descripcionTecnica = "Invoca un colosal meteorito ígneo tras 1s sobre la posición del cursor, infligiendo 180 de daño masivo en área (+25% por nivel en run) y derribando enemigos. Cooldown: 25s. [Tecla R]",
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
                    descripcionTecnica = "Dispara un haz continuo de escarcha hasta 9.5m (siguiendo dinámicamente la posición del cursor). Atraviesa enemigos, inflige 16 de daño continuo cada 0.18s y ralentiza un 50% la velocidad enemiga. [Click Izq]",
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
                    descripcionTecnica = "Lanza un proyectil gélido que detona en área al chocar. Inflige 50 de daño e inmoviliza/congela por completo a los enemigos alcanzados durante 2.5 segundos. Cooldown: 6s. [Tecla E]",
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
                    descripcionTecnica = "Envuelve al gato en una coraza glacial que reduce un 80% todo el daño recibido durante 10s y congela a los enemigos que te ataquen cuerpo a cuerpo. Cooldown: 20s. [Tecla Q]",
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
                    descripcionTecnica = "Desata una ventisca absoluta que congela instantáneamente al 100% a todos los enemigos en pantalla durante 4s y causa 25 de daño continuo por segundo durante 8s. Cooldown: 35s. [Tecla R]",
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
                    descripcionTecnica = "Ejecuta un veloz tajo con espada relámpago de 55 de daño (+25% por nivel en run) que empuja a los rivales y encadena chispas eléctricas a objetivos secundarios. Cooldown: 0.5s. [Click Izq]",
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
                    descripcionTecnica = "Embestida relámpago con invulnerabilidad total. Atraviesa enemigos causándoles 35 de daño y aturdiéndolos durante 0.8s. Cooldown: 4s. [Tecla E]",
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
                    descripcionTecnica = "Teletransporte instantáneo hacia la dirección del cursor, emitiendo una onda de choque estática que causa 45 de daño y repele a los enemigos al llegar. Cooldown: 7s. [Tecla Q]",
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
                    descripcionTecnica = "Tras 1.2s de concentración, dispara un gigantesco rayo voltaico de 250 de daño en línea recta que atraviesa muros y electrocuta a todos los enemigos en la sala. Cooldown: 30s. [Tecla R]",
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
