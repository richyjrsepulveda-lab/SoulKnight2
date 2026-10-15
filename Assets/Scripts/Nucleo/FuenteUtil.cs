using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Proveedor centralizado de tipografía Pixel Art retro para toda la UI del juego.
    /// </summary>
    public static class FuenteUtil
    {
        private static Font _fuentePixel;

        public static Font ObtenerFuentePixelArt()
        {
            if (_fuentePixel == null)
            {
                _fuentePixel = Resources.Load<Font>("Fuentes/PressStart2P");
                if (_fuentePixel == null)
                {
                    _fuentePixel = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            return _fuentePixel;
        }

        public static void AplicarFuentePixel(UnityEngine.UI.Text texto, int tamano = -1)
        {
            if (texto == null) return;
            texto.font = ObtenerFuentePixelArt();
            if (tamano > 0)
            {
                texto.fontSize = tamano;
            }
        }
    }
}
