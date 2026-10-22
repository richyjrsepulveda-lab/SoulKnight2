using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Ajustes de video cacheados en memoria.
    /// PlayerPrefs en Windows lee del registro: es lento para consultarlo en cada impacto,
    /// así que se lee una sola vez y se actualiza solo cuando el jugador cambia la opción.
    /// </summary>
    public static class AjustesVideo
    {
        private const string ClaveDanoAcumulativo = "DanoAcumulativo";

        private static bool _cargado;
        private static bool _danoAcumulativo;

        public static bool DanoAcumulativo
        {
            get
            {
                if (!_cargado)
                {
                    _danoAcumulativo = PlayerPrefs.GetInt(ClaveDanoAcumulativo, 0) == 1;
                    _cargado = true;
                }
                return _danoAcumulativo;
            }
            set
            {
                _danoAcumulativo = value;
                _cargado = true;
                PlayerPrefs.SetInt(ClaveDanoAcumulativo, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
