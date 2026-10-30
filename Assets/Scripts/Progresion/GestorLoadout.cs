using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    [Serializable]
    public class DatosLoadout
    {
        public byte[] habilidades = new byte[4]; // IdHabilidad convertidos a byte
    }

    /// <summary>
    /// Guarda y carga la selección de loadout de hasta 4 habilidades por personaje.
    /// Persiste localmente por jugador y personaje en el dispositivo (PlayerPrefs).
    /// </summary>
    public static class GestorLoadout
    {
        private const string PrefijoLoadout = "loadout_pj_";

        public static List<IdHabilidad> ObtenerLoadout(TipoPersonaje pj)
        {
            var res = new List<IdHabilidad>();
            if (pj == TipoPersonaje.Ninguno) return res;

            string clave = PrefijoLoadout + pj.ToString();
            string json = PlayerPrefs.GetString(clave, "");

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    DatosLoadout datos = JsonUtility.FromJson<DatosLoadout>(json);
                    if (datos != null && datos.habilidades != null)
                    {
                        for (int i = 0; i < datos.habilidades.Length; i++)
                        {
                            IdHabilidad id = (IdHabilidad)datos.habilidades[i];
                            // Validar que la habilidad pertenezca al personaje y esté desbloqueada
                            if (id != IdHabilidad.Ninguna && GestorMaestria.EstaDesbloqueada(pj, id))
                            {
                                res.Add(id);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error al cargar loadout para {pj}: {e.Message}");
                }
            }

            // Si no hay loadout guardado, asignar sólo la habilidad básica inicial
            if (res.Count == 0)
            {
                res = GenerarLoadoutPorDefecto(pj);
                GuardarLoadout(pj, res);
            }

            return res;
        }

        public static void GuardarLoadout(TipoPersonaje pj, List<IdHabilidad> habilidades)
        {
            if (pj == TipoPersonaje.Ninguno) return;

            DatosLoadout datos = new DatosLoadout();
            for (int i = 0; i < 4; i++)
            {
                datos.habilidades[i] = (i < habilidades.Count) ? (byte)habilidades[i] : (byte)IdHabilidad.Ninguna;
            }

            string json = JsonUtility.ToJson(datos);
            PlayerPrefs.SetString(PrefijoLoadout + pj.ToString(), json);
            PlayerPrefs.Save();
        }

        public static List<IdHabilidad> GenerarLoadoutPorDefecto(TipoPersonaje pj)
        {
            var res = new List<IdHabilidad>();
            // Por defecto, sólo la habilidad básica (Click izquierdo) viene equipada al iniciar
            IdHabilidad basica = CatalogoHabilidades.ObtenerHabilidadBasica(pj);
            if (basica != IdHabilidad.Ninguna)
            {
                res.Add(basica);
            }
            return res;
        }
    }
}
