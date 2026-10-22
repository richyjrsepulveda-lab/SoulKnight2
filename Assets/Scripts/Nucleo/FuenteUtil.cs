using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Proveedor centralizado de tipografía Pixel Art retro para toda la UI del juego:
    /// - Fuente Principal: Pixelify Sans (Google Fonts)
    /// - Fuente de Teclado/Keycaps: Silkscreen Bold
    /// </summary>
    public static class FuenteUtil
    {
        private static Font _fuentePixelify;
        private static Font _fuenteSilkscreen;

        public static Font ObtenerFuentePixelify()
        {
            if (_fuentePixelify == null)
            {
                _fuentePixelify = Resources.Load<Font>("Fuentes/PixelifySans");
                if (_fuentePixelify == null)
                {
                    _fuentePixelify = Resources.Load<Font>("Fuentes/PressStart2P");
                }
                if (_fuentePixelify == null)
                {
                    _fuentePixelify = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            return _fuentePixelify;
        }

        public static Font ObtenerFuenteSilkscreen()
        {
            if (_fuenteSilkscreen == null)
            {
                _fuenteSilkscreen = Resources.Load<Font>("Fuentes/Silkscreen-Bold");
                if (_fuenteSilkscreen == null)
                {
                    _fuenteSilkscreen = ObtenerFuentePixelify();
                }
            }
            return _fuenteSilkscreen;
        }

        public static Font ObtenerFuentePixelArt()
        {
            return ObtenerFuentePixelify();
        }

        public static void AplicarFuentePixel(UnityEngine.UI.Text texto, int tamano = -1)
        {
            if (texto == null) return;
            texto.font = ObtenerFuentePixelify();
            if (tamano > 0)
            {
                texto.fontSize = tamano;
            }
        }
    }
}
