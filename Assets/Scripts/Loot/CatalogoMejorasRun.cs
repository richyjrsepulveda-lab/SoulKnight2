using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoMejoraRun : byte
    {
        PescadoEsqueleto = 0,   // 1er Pescado: Cabeza bien y resto huesos -> Cura 20 de vida
        PocionVida = 0,         // Alias de compatibilidad -> PescadoEsqueleto
        AumentoDano = 1,        // +15% daño en el run
        AumentoDefensa = 2,     // +10% reducción de daño en el run
        AumentoVelocidad = 3,   // +15% velocidad de movimiento en el run
        AumentoVidaMaxima = 4,  // +30 de vida máxima temporal en el run
        PescadoMedio = 5,       // 2do Pescado: Mitad cuerpo bien y mitad esqueleto -> Cura 50 de vida
        PescadoCompleto = 6     // 3er Pescado: Pescadito completo -> Cura 80 de vida
    }

    [Serializable]
    public class DefinicionMejora
    {
        public TipoMejoraRun tipo;
        public string nombre;
        public string descripcion;
        public Color colorTexto;
        public float valor; // Valor numérico del efecto
    }

    /// <summary>
    /// Catálogo extensible de tipos de mejoras de loot de run y los 3 pescaditos curativos.
    /// </summary>
    public static class CatalogoMejorasRun
    {
        private static readonly Dictionary<TipoMejoraRun, DefinicionMejora> _mejoras = new Dictionary<TipoMejoraRun, DefinicionMejora>
        {
            {
                TipoMejoraRun.PescadoEsqueleto,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.PescadoEsqueleto,
                    nombre = "Poco Pescado",
                    descripcion = "Pescado con la cabeza intacta y cuerpo de espinas. Cura 20 de vida.",
                    colorTexto = new Color(0.55f, 1f, 0.65f),
                    valor = 20f
                }
            },
            {
                TipoMejoraRun.PescadoMedio,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.PescadoMedio,
                    nombre = "Medio Pescado",
                    descripcion = "Mitad cuerpo fresco y mitad esqueleto. Cura 50 de vida.",
                    colorTexto = new Color(0.35f, 0.95f, 1f),
                    valor = 50f
                }
            },
            {
                TipoMejoraRun.PescadoCompleto,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.PescadoCompleto,
                    nombre = "Pescado Completo",
                    descripcion = "Pescadito entero y delicioso. Cura 80 de vida.",
                    colorTexto = new Color(0.2f, 1f, 0.45f),
                    valor = 80f
                }
            },
            {
                TipoMejoraRun.AumentoDano,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.AumentoDano,
                    nombre = "Poder Arcano",
                    descripcion = "+15% de daño a todos tus ataques.",
                    colorTexto = new Color(1f, 0.4f, 0.2f),
                    valor = 0.15f
                }
            },
            {
                TipoMejoraRun.AumentoDefensa,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.AumentoDefensa,
                    nombre = "Escudo Rúnico",
                    descripcion = "+10% de reducción pasiva de daño recibido.",
                    colorTexto = Color.cyan,
                    valor = 0.10f
                }
            },
            {
                TipoMejoraRun.AumentoVelocidad,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.AumentoVelocidad,
                    nombre = "Botas de Viento",
                    descripcion = "+15% de velocidad de movimiento.",
                    colorTexto = Color.yellow,
                    valor = 0.15f
                }
            },
            {
                TipoMejoraRun.AumentoVidaMaxima,
                new DefinicionMejora
                {
                    tipo = TipoMejoraRun.AumentoVidaMaxima,
                    nombre = "Corazón de Titán",
                    descripcion = "+30 de vida máxima para este run.",
                    colorTexto = new Color(0.9f, 0.2f, 0.5f),
                    valor = 30f
                }
            }
        };

        public static DefinicionMejora Obtener(TipoMejoraRun tipo)
        {
            return _mejoras.TryGetValue(tipo, out var def) ? def : null;
        }

        public static TipoMejoraRun ObtenerAleatoria()
        {
            // Las mejoras infinitas de estadísticas (daño, vida y escudo) están desactivadas como drops.
            // En el suelo y salas solo caen los 3 pescaditos de curación (esqueleto, medio y completo).
            float r = UnityEngine.Random.value;
            if (r < 0.50f) return TipoMejoraRun.PescadoEsqueleto; // 50% Poco pescado (cura 20)
            if (r < 0.82f) return TipoMejoraRun.PescadoMedio;     // 32% Medio pescado (cura 50)
            return TipoMejoraRun.PescadoCompleto;                 // 18% Pescado completo (cura 80)
        }
    }
}
