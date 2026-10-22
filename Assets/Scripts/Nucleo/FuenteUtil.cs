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
        /// Fuente pixel art retro para el HUD, barra de vida, minimapa, textos flotantes y elementos de juego.
        /// Preserva el tamaño y métricas originales de PressStart2P / LegacyRuntime.
        /// </summary>
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

        /// <summary>
        /// Fuente principal moderna (Pixelify Sans) para los nuevos menús UI/UX (Principal, Opciones, Diálogos).
        /// </summary>
        public static Font ObtenerFuentePixelify()
        {
            if (_fuentePixelify == null)
            {
                _fuentePixelify = Resources.Load<Font>("Fuentes/PixelifySans");
                if (_fuentePixelify == null)
                {
                    _fuentePixelify = ObtenerFuentePixelArt();
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
