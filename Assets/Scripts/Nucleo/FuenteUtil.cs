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
        private static Font _fuentePixel;
        private static Font _fuentePixelify;
        private static Font _fuenteSilkscreen;

        /// <summary>
        /// Fuente pixel art retro / principal para el HUD, barra de vida, minimapa, textos flotantes y elementos de juego.
        /// Unificada a Pixelify Sans en todo el juego.
        /// </summary>
        public static Font ObtenerFuentePixelArt()
        {
            return ObtenerFuentePixelify();
        }

        /// <summary>
        /// Fuente principal moderna (Pixelify Sans) para todos los menús y HUDs del juego.
        /// </summary>
        public static Font ObtenerFuentePixelify()
        {
            if (_fuentePixelify == null)
            {
                // Jersey 15: Tipografía pixel art nítida y retro con la letra 'C' perfectamente abierta y legible (evitando que parezca 'O')
                _fuentePixelify = Resources.Load<Font>("Fuentes/Jersey15");
                if (_fuentePixelify == null)
                {
                    _fuentePixelify = Resources.Load<Font>("Fuentes/Silkscreen-Bold");
                }
                if (_fuentePixelify == null)
                {
                    _fuentePixelify = Resources.Load<Font>("Fuentes/PixelifySans");
                }
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

        /// <summary>
        /// Fuente para teclas / keycaps (Silkscreen Bold).
        /// </summary>
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
