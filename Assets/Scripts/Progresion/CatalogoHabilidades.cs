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
                    descripcionTecnica = "• Cooldown: 0.75s | Daño: 22 impacto\n• Quemadura: 18 daño/seg por 3.5s\n• Proyectil balístico veloz de largo alcance.",
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
                    descripcionTecnica = "• Cooldown: 7.0s | Duración: 4.0s\n• Daño: 14 por tick continuo cada 0.14s\n• Alcance: Cono frontal de 6.5m con quemadura.",
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
                    descripcionTecnica = "• Cooldown: 14.0s | Duración: 6.0s\n• Reducción de daño: 80%\n• Contraataque: Inflige 18 daño y quema a los agresores.",
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
                    descripcionTecnica = "• Cooldown: 28.0s | Duración furia: 8.0s\n• Onda expansiva: Radio 5.0m, 75 daño + empuje 500\n• 16 bolas en 360°, armadura activa y escopeta triple.",
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
                    descripcionTecnica = "• Cooldown: 0.15s | Daño: 12 continuo\n• Ralentización: 50% de velocidad rival\n• Alcance: Haz directo de 12.0m hacia el cursor.",
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
                    descripcionTecnica = "• Cooldown: 5.5s | Daño: 35 impacto\n• Congelación: 100% parálisis durante 2.5s\n• Radio de explosión: 3.8m.",
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
                    descripcionTecnica = "• Cooldown: 14.0s | Duración: 6.0s\n• Reducción de daño: 80%\n• Escarcha: Congela a los atacantes cercanos.",
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
                    descripcionTecnica = "• Cooldown: 30.0s | Duración: 10.0s\n• Radio: Ventisca de 10.0m de radio (20m diámetro)\n• Daño: 25 por tick (cada 0.5s) y congelación permanente.",
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
                    descripcionTecnica = "• Cooldown: 0.45s | Daño: 30 físico/eléctrico\n• Arco: Barrido de 110° con 2.5m de alcance\n• Empuje: Knockback de 350 a los enemigos alcanzados.",
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
                    descripcionTecnica = "• Cooldown: 3.5s | Distancia: 7.5m (Vel. 24m/s)\n• Invulnerabilidad total durante el desplazamiento\n• No atraviesa muros ni obstáculos sólidos.",
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
                    descripcionTecnica = "• Cooldown: 8.0s | Alcance: Hasta 12.0m hacia el cursor\n• Impacto doble (origen y destino): Radio 3.2m\n• Daño: 45 + knockback masivo de 1000.",
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
                    descripcionTecnica = "• Cooldown: 26.0s | Carga: Hasta 4.0s\n• Rayos de carga: Radio 4.4m, 15 daño por tick\n• Cañón voltaico: Alcance 22m, daño escalado (90 a 240).",
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
