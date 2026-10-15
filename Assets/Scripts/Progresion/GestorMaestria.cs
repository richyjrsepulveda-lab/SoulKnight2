using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Gestiona la maestría persistente por personaje (guardada en disco local mediante PlayerPrefs / JSON).
    /// - Cada personaje tiene su propia maestría independiente.
    /// - Al alcanzar umbrales de puntos se desbloquean habilidades permanentemente.
    /// - Se gana maestría al derrotar enemigos, completar niveles o ganar la partida.
    /// </summary>
    public static class GestorMaestria
    {
        private const string PrefijoMaestria = "maestria_xp_";
        public const int XP_POR_NIVEL_MAESTRIA = 200;
        public const int NIVEL_MAXIMO_MAESTRIA = 20;
        public const int XP_MAXIMA_MAESTRIA = (NIVEL_MAXIMO_MAESTRIA - 1) * XP_POR_NIVEL_MAESTRIA;

        public static event Action<TipoPersonaje, int> AlGanarMaestria;

        public static int ObtenerPuntos(TipoPersonaje pj)
        {
            if (pj == TipoPersonaje.Ninguno) return 0;
            int guardado = PlayerPrefs.GetInt(PrefijoMaestria + pj.ToString(), 0);
            return Mathf.Clamp(guardado, 0, XP_MAXIMA_MAESTRIA);
        }

        public static int ObtenerNivelMaestria(TipoPersonaje pj)
        {
            if (pj == TipoPersonaje.Ninguno) return 1;
            int xp = ObtenerPuntos(pj);
            // Nivel 1 a Nivel 20 máximo por ahora (estrictamente topado a NIVEL_MAXIMO_MAESTRIA)
            int nivel = 1 + (xp / XP_POR_NIVEL_MAESTRIA);
            return Mathf.Clamp(nivel, 1, NIVEL_MAXIMO_MAESTRIA);
        }

        public static void AgregarPuntos(TipoPersonaje pj, int puntos)
        {
            if (pj == TipoPersonaje.Ninguno || puntos <= 0) return;
            int actual = ObtenerPuntos(pj);
            if (actual >= XP_MAXIMA_MAESTRIA) return; // Si ya alcanzó el nivel 20 máximo, no seguir acumulando ni disparar eventos innecesarios

            int nuevo = Mathf.Min(XP_MAXIMA_MAESTRIA, actual + puntos);
            PlayerPrefs.SetInt(PrefijoMaestria + pj.ToString(), nuevo);
            PlayerPrefs.Save();
            AlGanarMaestria?.Invoke(pj, nuevo);
        }

        /// <summary>
        /// Comprueba si la habilidad está desbloqueada por el NIVEL de maestría del personaje.
        /// Nivel 1: Habilidad básica (Click)
        /// Nivel 2: Habilidad 1 (E)
        /// Nivel 3: Habilidad 2 (Q)
        /// Nivel 4: Ultimate (R)
        /// </summary>
        public static bool EstaDesbloqueada(TipoPersonaje pj, IdHabilidad id)
        {
            var def = CatalogoHabilidades.Obtener(id);
            if (def == null) return false;
            int nivelActual = ObtenerNivelMaestria(pj);
            return nivelActual >= def.nivelMaestriaRequerido;
        }

        public static List<IdHabilidad> ObtenerHabilidadesDesbloqueadas(TipoPersonaje pj)
        {
            var lista = new List<IdHabilidad>();
            var todas = CatalogoHabilidades.ObtenerHabilidadesPersonaje(pj);
            int nivelActual = ObtenerNivelMaestria(pj);

            foreach (var h in todas)
            {
                if (nivelActual >= h.nivelMaestriaRequerido)
                {
                    lista.Add(h.id);
                }
            }
            return lista;
        }

        /// <summary>
        /// Calcula puntos de maestría obtenidos al concluir un run o nivel:
        /// enemigos derrotados * 5 + pisos superados * 50 + (victoria ? 150 : 0)
        /// </summary>
        public static int CalcularXPFinPartida(int enemigosEliminados, int pisosSuperados, bool victoria)
        {
            int basePuntos = (enemigosEliminados * 5) + (pisosSuperados * 50);
            if (victoria) basePuntos += 150;
            return Mathf.Max(10, basePuntos);
        }
    }
}
