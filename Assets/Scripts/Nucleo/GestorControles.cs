using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum AccionControl
    {
        MoverArriba,
        MoverAbajo,
        MoverIzquierda,
        MoverDerecha,
        Correr,
        AtaquePrincipal,
        Habilidad1,
        Habilidad2,
        Ultimate,
        Pausa
    }

    /// <summary>
    /// Sistema de controles remapeables con persistencia en PlayerPrefs.
    /// Funciona de forma totalmente desacoplada e independiente.
    /// </summary>
    public static class GestorControles
    {
        private const string PrefijoTecla = "control_tecla_";

        private static readonly Dictionary<AccionControl, KeyCode> TeclasPorDefecto = new Dictionary<AccionControl, KeyCode>
        {
            { AccionControl.MoverArriba, KeyCode.W },
            { AccionControl.MoverAbajo, KeyCode.S },
            { AccionControl.MoverIzquierda, KeyCode.A },
            { AccionControl.MoverDerecha, KeyCode.D },
            { AccionControl.Correr, KeyCode.LeftShift },
            { AccionControl.AtaquePrincipal, KeyCode.Mouse0 },
            { AccionControl.Habilidad1, KeyCode.E },
            { AccionControl.Habilidad2, KeyCode.Q },
            { AccionControl.Ultimate, KeyCode.R },
            { AccionControl.Pausa, KeyCode.Escape }
        };

        private static readonly Dictionary<AccionControl, KeyCode> TeclasActuales = new Dictionary<AccionControl, KeyCode>();

        static GestorControles()
        {
            CargarControles();
        }

        public static void CargarControles()
        {
            foreach (var kvp in TeclasPorDefecto)
            {
                string guardado = PlayerPrefs.GetString(PrefijoTecla + kvp.Key.ToString(), "");
                if (!string.IsNullOrEmpty(guardado) && Enum.TryParse(guardado, out KeyCode tecla))
                {
                    TeclasActuales[kvp.Key] = tecla;
                }
                else
                {
                    TeclasActuales[kvp.Key] = kvp.Value;
                }
            }
        }

        public static KeyCode ObtenerTecla(AccionControl accion)
        {
            return TeclasActuales.TryGetValue(accion, out KeyCode tecla) ? tecla : TeclasPorDefecto[accion];
        }

        public static void AsignarTecla(AccionControl accion, KeyCode nuevaTecla)
        {
            TeclasActuales[accion] = nuevaTecla;
            PlayerPrefs.SetString(PrefijoTecla + accion.ToString(), nuevaTecla.ToString());
            PlayerPrefs.Save();
        }

        public static void RestaurarPorDefecto()
        {
            foreach (var kvp in TeclasPorDefecto)
            {
                TeclasActuales[kvp.Key] = kvp.Value;
                PlayerPrefs.DeleteKey(PrefijoTecla + kvp.Key.ToString());
            }
            PlayerPrefs.Save();
        }
    }
}
