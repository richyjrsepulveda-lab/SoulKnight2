using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    public enum TipoBotonUI
    {
        Dorado,
        Piedra,
        Verde,
        Hielo,
        Rojo
    }

    /// <summary>
    /// Fábrica y sistema de diseño pixel-art estricto para "Just a Cat Adventure":
    /// - Escala de pixel = 6px (y 4px en selectores/detalles).
    /// - Esquinas mordidas (sin píxel de esquina).
    /// - Paleta de colores oficial 1:1 con las especificaciones y archivos HTML de diseño.
    /// - Filtro Point sin suavizado, anti-aliasing ni redondeos.
    /// - Fuentes Pixelify Sans y Silkscreen Bold.
    /// </summary>
    public static class EstiloUIUX
    {
        // Constante de Pixels Per Unit para que las cajas sliced se dibujen a escala exacta 1:1 en el canvas
        public const float PPU = 100f;

        // =========================================================================
        // PALETA OFICIAL
        // =========================================================================
        public static readonly Color32 ColContornoUniversal = new Color32(0x0C, 0x08, 0x18, 0xFF); // #0c0818
        public static readonly Color32 ColFondoBase = new Color32(0x1C, 0x15, 0x37, 0xFF);          // #1c1537
        public static readonly Color32 ColLadrilloResalte = new Color32(0x27, 0x1F, 0x4D, 0xFF);    // #271f4d
        public static readonly Color32 ColMortero = new Color32(0x11, 0x0B, 0x24, 0xFF);            // #110b24

        public static readonly Color32 ColPanel = new Color32(0x2A, 0x21, 0x50, 0xFF);              // #2a2150
        public static readonly Color32 ColPanelLuz = new Color32(0x43, 0x38, 0x8A, 0xFF);           // #43388a
        public static readonly Color32 ColPanelSombra = new Color32(0x1B, 0x14, 0x38, 0xFF);        // #1b1438
        public static readonly Color32 ColCabecera = new Color32(0x1B, 0x14, 0x38, 0xFF);           // #1b1438

        public static readonly Color32 ColPozo = new Color32(0x17, 0x11, 0x2E, 0xFF);               // #17112e
        public static readonly Color32 ColPozoSombra = new Color32(0x0A, 0x06, 0x16, 0xFF);         // #0a0616

        public static readonly Color32 ColTarjeta = new Color32(0x22, 0x1A, 0x44, 0xFF);            // #221a44
        public static readonly Color32 ColTarjetaLuz = new Color32(0x2F, 0x25, 0x61, 0xFF);         // #2f2561
        public static readonly Color32 ColTarjetaSombra = new Color32(0x18, 0x10, 0x32, 0xFF);      // #181032

        public static readonly Color32 ColValorFondo = new Color32(0x0F, 0x0A, 0x22, 0xFF);         // #0f0a22
        public static readonly Color32 ColValorSombra = new Color32(0x05, 0x03, 0x0D, 0xFF);        // #05030d

        public static readonly Color32 ColTextoCrema = new Color32(0xFF, 0xF3, 0xD6, 0xFF);         // #fff3d6
        public static readonly Color32 ColTextoTenue = new Color32(0xB9, 0xB0, 0xE6, 0xFF);         // #b9b0e6
        public static readonly Color32 ColTextoMuyTenue = new Color32(0x8A, 0x7F, 0xD0, 0xFF);      // #8a7fd0
        public static readonly Color32 ColDorado = new Color32(0xFF, 0xCF, 0x4A, 0xFF);              // #ffcf4a
        public static readonly Color32 ColCyan = new Color32(0x8F, 0xE0, 0xFF, 0xFF);                // #8fe0ff
        public static readonly Color32 ColVerde = new Color32(0x4E, 0xD1, 0x7E, 0xFF);               // #4ed17e
        public static readonly Color32 ColRojo = new Color32(0xD4, 0x40, 0x4F, 0xFF);                // #d4404f

        // Botones: (Cara, Luz, Sombra, Texto)
        public static readonly Color32 ColBtnDoradoCara = new Color32(0xFF, 0xB9, 0x38, 0xFF);
        public static readonly Color32 ColBtnDoradoLuz = new Color32(0xFF, 0xE0, 0x8A, 0xFF);
        public static readonly Color32 ColBtnDoradoSombra = new Color32(0xD9, 0x79, 0x1A, 0xFF);
        public static readonly Color32 ColBtnDoradoTexto = new Color32(0x2A, 0x14, 0x05, 0xFF);

        public static readonly Color32 ColBtnPiedraCara = new Color32(0x4A, 0x3F, 0x86, 0xFF);
        public static readonly Color32 ColBtnPiedraLuz = new Color32(0x6C, 0x5F, 0xB5, 0xFF);
        public static readonly Color32 ColBtnPiedraSombra = new Color32(0x2D, 0x25, 0x59, 0xFF);
        public static readonly Color32 ColBtnPiedraTexto = new Color32(0xE4, 0xDE, 0xFA, 0xFF);

        public static readonly Color32 ColBtnVerdeCara = new Color32(0x4E, 0xD1, 0x7E, 0xFF);
        public static readonly Color32 ColBtnVerdeLuz = new Color32(0xA4, 0xF2, 0xC0, 0xFF);
        public static readonly Color32 ColBtnVerdeSombra = new Color32(0x23, 0x92, 0x4F, 0xFF);
        public static readonly Color32 ColBtnVerdeTexto = new Color32(0x04, 0x21, 0x0F, 0xFF);

        public static readonly Color32 ColBtnHieloCara = new Color32(0x58, 0xC4, 0xFF, 0xFF);
        public static readonly Color32 ColBtnHieloLuz = new Color32(0xB4, 0xE6, 0xFF, 0xFF);
        public static readonly Color32 ColBtnHieloSombra = new Color32(0x2A, 0x87, 0xC9, 0xFF);
        public static readonly Color32 ColBtnHieloTexto = new Color32(0x07, 0x1A, 0x33, 0xFF);

        public static readonly Color32 ColBtnRojoCara = new Color32(0xD4, 0x40, 0x4F, 0xFF);
        public static readonly Color32 ColBtnRojoLuz = new Color32(0xFF, 0x8D, 0x96, 0xFF);
        public static readonly Color32 ColBtnRojoSombra = new Color32(0x8F, 0x1F, 0x31, 0xFF);
        public static readonly Color32 ColBtnRojoTexto = new Color32(0xFF, 0xF3, 0xD6, 0xFF);

        // Knob perilla
        public static readonly Color32 ColKnobFondo = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
        public static readonly Color32 ColKnobSombra = new Color32(0xB9, 0xAD, 0x8C, 0xFF);

        // Remache
        public static readonly Color32 ColRemache = new Color32(0xFF, 0xCF, 0x4A, 0xFF);
        public static readonly Color32 ColRemacheSombra = new Color32(0xD9, 0x8A, 0x1A, 0xFF);

        // =========================================================================
        // CACHÉ DE SPRITES Y TEXTURAS PROCEDURALES
        // =========================================================================
        private static readonly Dictionary<string, Sprite> _cacheSprites = new Dictionary<string, Sprite>();
        private static Texture2D _texLadrillos;
        private static Texture2D _texVineta;
        private static Texture2D _texGlowAntorcha;

        public static Texture2D ObtenerTexturaVineta()
        {
            if (_texVineta != null) return _texVineta;

            const int S = 128;
            _texVineta = new Texture2D(S, S, TextureFormat.RGBA32, false);
            _texVineta.filterMode = FilterMode.Bilinear;
            _texVineta.wrapMode = TextureWrapMode.Clamp;

            Color32 cOscuro = new Color32(8, 4, 20, 200); // 0.78 alpha
            Color32 cTrans = new Color32(8, 4, 20, 0);

            Color32[] px = new Color32[S * S];
            float centro = (S - 1) * 0.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy); // 0 en centro, 1 en bordes, 1.41 en esquinas

                    if (dist <= 0.40f)
                    {
                        px[y * S + x] = cTrans;
                    }
                    else
                    {
                        float t = Mathf.Clamp01((dist - 0.40f) / 0.60f);
                        byte a = (byte)(t * 200);
                        px[y * S + x] = new Color32(8, 4, 20, a);
                    }
                }
            }
            _texVineta.SetPixels32(px);
            _texVineta.Apply(false);
            return _texVineta;
        }

        public static Texture2D ObtenerTexturaGlowAntorcha()
        {
            if (_texGlowAntorcha != null) return _texGlowAntorcha;

            const int S = 64;
            _texGlowAntorcha = new Texture2D(S, S, TextureFormat.RGBA32, false);
            _texGlowAntorcha.filterMode = FilterMode.Bilinear;
            _texGlowAntorcha.wrapMode = TextureWrapMode.Clamp;

            Color32[] px = new Color32[S * S];
            float centro = (S - 1) * 0.5f;

            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= 1.0f)
                    {
                        px[y * S + x] = new Color32(255, 138, 60, 0);
                    }
                    else
                    {
                        float t = 1.0f - dist;
                        byte a = (byte)(t * t * 76); // ~0.30 alpha máx en centro
                        px[y * S + x] = new Color32(255, 138, 60, a);
                    }
                }
            }
            _texGlowAntorcha.SetPixels32(px);
            _texGlowAntorcha.Apply(false);
            return _texGlowAntorcha;
        }

        public static Texture2D ObtenerTexturaLadrillos()
        {
            if (_texLadrillos != null) return _texLadrillos;

            const int W = 128;
            const int H = 64;
            _texLadrillos = new Texture2D(W, H, TextureFormat.RGBA32, false);
            _texLadrillos.filterMode = FilterMode.Point;
            _texLadrillos.wrapMode = TextureWrapMode.Repeat;

            Color32[] pixeles = new Color32[W * H];
            for (int i = 0; i < pixeles.Length; i++) pixeles[i] = ColFondoBase;

            void FillRect(int rx, int ry, int rw, int rh, Color32 col)
            {
                // En SVG ry=0 está arriba; invertimos para Texture2D donde y=0 está abajo
                int texY = H - (ry + rh);
                for (int y = 0; y < rh; y++)
                {
                    int py = texY + y;
                    if (py < 0 || py >= H) continue;
                    for (int x = 0; x < rw; x++)
                    {
                        int px = rx + x;
                        if (px < 0 || px >= W) continue;
                        pixeles[py * W + px] = col;
                    }
                }
            }

            // Resalte de ladrillos superiores (4px #271f4d)
            FillRect(4, 4, 60, 4, ColLadrilloResalte);
            FillRect(68, 4, 60, 4, ColLadrilloResalte);
            FillRect(36, 36, 60, 4, ColLadrilloResalte);
            FillRect(100, 36, 28, 4, ColLadrilloResalte);
            FillRect(0, 36, 32, 4, ColLadrilloResalte);

            // Mortero horizontal (4px #110b24)
            FillRect(0, 0, 128, 4, ColMortero);
            FillRect(0, 32, 128, 4, ColMortero);

            // Mortero vertical (4px #110b24)
            FillRect(0, 4, 4, 28, ColMortero);
            FillRect(64, 4, 4, 28, ColMortero);
            FillRect(32, 36, 4, 28, ColMortero);
            FillRect(96, 36, 4, 28, ColMortero);

            _texLadrillos.SetPixels32(pixeles);
            _texLadrillos.Apply(false);
            return _texLadrillos;
        }

        /// <summary>
        /// Genera un Sprite 9-slice con bordes escalonados de 6px (esquinas mordidas transparentes)
        /// y luz/sombra interior.
        /// </summary>
        public static Sprite ObtenerSpriteCajaEscalonada(string clave, Color32 cara, Color32 luz, Color32 sombra, int bordeContorno = 6, int luzInner = 6, int sombraInner = 6)
        {
            if (_cacheSprites.TryGetValue(clave, out var spExistente) && spExistente != null)
            {
                return spExistente;
            }

            const int S = 36;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32 trans = new Color32(0, 0, 0, 0);
            Color32[] pixeles = new Color32[S * S];

            int c = bordeContorno;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    int idx = y * S + x;

                    // Esquinas exteriores mordidas (transparentes)
                    bool esEsquinaTL = (x < c && y >= S - c);
                    bool esEsquinaTR = (x >= S - c && y >= S - c);
                    bool esEsquinaBL = (x < c && y < c);
                    bool esEsquinaBR = (x >= S - c && y < c);

                    if (esEsquinaTL || esEsquinaTR || esEsquinaBL || esEsquinaBR)
                    {
                        pixeles[idx] = trans;
                        continue;
                    }

                    // Borde exterior contorno #0c0818
                    bool enBorde = (x < c || x >= S - c || y < c || y >= S - c);
                    if (enBorde)
                    {
                        pixeles[idx] = ColContornoUniversal;
                        continue;
                    }

                    // Dentro de la caja:
                    // Luz superior (y cerca de S - c)
                    if (y >= S - c - luzInner)
                    {
                        pixeles[idx] = luz;
                    }
                    // Sombra inferior (y cerca de c)
                    else if (y < c + sombraInner)
                    {
                        pixeles[idx] = sombra;
                    }
                    else
                    {
                        pixeles[idx] = cara;
                    }
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply(false);

            Vector4 sliceBorder = new Vector4(12, 12, 12, 12);
            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, sliceBorder);
            _cacheSprites[clave] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpritePanel()
        {
            return ObtenerSpriteCajaEscalonada("PanelBase", ColPanel, ColPanelLuz, ColPanelSombra, 6, 6, 6);
        }

        public static Sprite ObtenerSpritePozo()
        {
            return ObtenerSpriteCajaEscalonada("PozoBase", ColPozo, ColPozoSombra, ColPozo, 6, 6, 0);
        }

        public static Sprite ObtenerSpriteTarjeta()
        {
            if (_cacheSprites.TryGetValue("TarjetaBase", out var sp) && sp != null) return sp;

            const int S = 24;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixeles = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    int idx = y * S + x;
                    if (y >= S - 4) pixeles[idx] = ColTarjetaLuz;
                    else if (y < 4) pixeles[idx] = ColTarjetaSombra;
                    else pixeles[idx] = ColTarjeta;
                }
            }
            tex.SetPixels32(pixeles);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
            _cacheSprites["TarjetaBase"] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpriteValorSelector()
        {
            return ObtenerSpriteCajaEscalonada("ValorSelector", ColValorFondo, ColValorSombra, ColValorFondo, 4, 4, 0);
        }

        public static Sprite ObtenerSpriteBoton(TipoBotonUI tipo, bool presionado = false)
        {
            string clave = $"Btn_{tipo}_{presionado}";
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            Color32 cara, luz, sombra;
            switch (tipo)
            {
                case TipoBotonUI.Dorado:
                    cara = ColBtnDoradoCara; luz = ColBtnDoradoLuz; sombra = ColBtnDoradoSombra; break;
                case TipoBotonUI.Verde:
                    cara = ColBtnVerdeCara; luz = ColBtnVerdeLuz; sombra = ColBtnVerdeSombra; break;
                case TipoBotonUI.Hielo:
                    cara = ColBtnHieloCara; luz = ColBtnHieloLuz; sombra = ColBtnHieloSombra; break;
                case TipoBotonUI.Rojo:
                    cara = ColBtnRojoCara; luz = ColBtnRojoLuz; sombra = ColBtnRojoSombra; break;
                default:
                    cara = ColBtnPiedraCara; luz = ColBtnPiedraLuz; sombra = ColBtnPiedraSombra; break;
            }

            if (presionado)
            {
                // Invertir luz y sombra cuando está presionado
                var temp = luz;
                luz = sombra;
                sombra = temp;
            }

            return ObtenerSpriteCajaEscalonada(clave, cara, luz, sombra, 6, 6, 6);
        }

        public static Sprite ObtenerSpriteKnob()
        {
            if (_cacheSprites.TryGetValue("KnobSwitch", out var sp) && sp != null) return sp;

            const int S = 36;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixeles = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    int idx = y * S + x;
                    // Borde exterior 4px #0c0818
                    if (x < 4 || x >= S - 4 || y < 4 || y >= S - 4)
                    {
                        pixeles[idx] = ColContornoUniversal;
                    }
                    // Sombra interior abajo 6px #b9ad8c
                    else if (y < 4 + 6)
                    {
                        pixeles[idx] = ColKnobSombra;
                    }
                    else
                    {
                        pixeles[idx] = ColKnobFondo;
                    }
                }
            }
            tex.SetPixels32(pixeles);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, Vector4.zero);
            _cacheSprites["KnobSwitch"] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpriteRemache()
        {
            if (_cacheSprites.TryGetValue("Remache", out var sp) && sp != null) return sp;

            const int S = 8;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixeles = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    int idx = y * S + x;
                    // Sombra interior de 2px en esquina inferior derecha
                    if (x >= S - 2 || y < 2)
                    {
                        pixeles[idx] = ColRemacheSombra;
                    }
                    else
                    {
                        pixeles[idx] = ColRemache;
                    }
                }
            }
            tex.SetPixels32(pixeles);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, Vector4.zero);
            _cacheSprites["Remache"] = sprite;
            return sprite;
        }

        // =========================================================================
        // SLIDERS Y KEYCAPS
        // =========================================================================
        public static Sprite ObtenerSpritePistaSlider()
        {
            if (_cacheSprites.TryGetValue("PistaSlider", out var sp) && sp != null) return sp;

            const int W = 24, H = 24;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32 cFondo = new Color32(0x0C, 0x08, 0x18, 0xFF);
            Color32 cSombra = new Color32(0x05, 0x03, 0x0D, 0xFF);
            Color32[] px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    px[y * W + x] = (y >= H - 6) ? cSombra : cFondo;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
            _cacheSprites["PistaSlider"] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpriteFillSlider(string tipo)
        {
            string clave = "FillSlider_" + tipo;
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            Color32 cara, luz, sombra;
            switch (tipo.ToLowerInvariant())
            {
                case "pink":
                    cara = new Color32(0xFF, 0x8F, 0xB1, 0xFF);
                    luz = new Color32(0xFF, 0xC7, 0xD9, 0xFF);
                    sombra = new Color32(0xC2, 0x45, 0x6F, 0xFF);
                    break;
                case "fire":
                    cara = new Color32(0xFF, 0x9A, 0x3D, 0xFF);
                    luz = new Color32(0xFF, 0xD0, 0x8A, 0xFF);
                    sombra = new Color32(0xC9, 0x56, 0x0F, 0xFF);
                    break;
                default: // ice
                    cara = new Color32(0x58, 0xC4, 0xFF, 0xFF);
                    luz = new Color32(0xB4, 0xE6, 0xFF, 0xFF);
                    sombra = new Color32(0x2A, 0x87, 0xC9, 0xFF);
                    break;
            }

            const int W = 24, H = 24;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (y >= H - 6) px[y * W + x] = luz;
                    else if (y < 6) px[y * W + x] = sombra;
                    else px[y * W + x] = cara;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
            _cacheSprites[clave] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpriteThumbSlider()
        {
            if (_cacheSprites.TryGetValue("ThumbSlider", out var sp) && sp != null) return sp;

            const int W = 24, H = 48;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32 cBorde = ColContornoUniversal;
            Color32 cCara = ColDorado;
            Color32 cLuz = new Color32(0xFF, 0xF0, 0xB0, 0xFF);
            Color32 cSombra = new Color32(0xD9, 0x8A, 0x1A, 0xFF);

            Color32[] px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    if (x < 4 || x >= W - 4 || y < 4 || y >= H - 4)
                        px[idx] = cBorde;
                    else if (y >= H - 4 - 4)
                        px[idx] = cLuz;
                    else if (y < 4 + 6)
                        px[idx] = cSombra;
                    else
                        px[idx] = cCara;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
            _cacheSprites["ThumbSlider"] = sprite;
            return sprite;
        }

        public static Sprite ObtenerSpriteKeycap(bool esperando = false)
        {
            string clave = esperando ? "KeycapEsperando" : "KeycapNormal";
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            Color32 cBorde = esperando ? ColDorado : ColContornoUniversal;
            Color32 cCara = esperando ? ColBtnDoradoCara : ColTextoCrema;
            Color32 cLuz = esperando ? ColBtnDoradoLuz : new Color32(0xFF, 0xFD, 0xF2, 0xFF);
            Color32 cSombra = esperando ? ColBtnDoradoSombra : new Color32(0xCD, 0xBF, 0x9C, 0xFF);

            return ObtenerSpriteCajaEscalonada(clave, cCara, cLuz, cSombra, 4, 4, 6);
        }

        // =========================================================================
        // ÍCONOS PROCEDURALES EXACTOS (DESDE COORDENADAS SVG DE DISEÑO)
        // =========================================================================
        public static Sprite ObtenerSpriteIcono(string nombreIcono, Color32? colorTinte = null)
        {
            Color32 color = colorTinte ?? ColDorado;
            string clave = $"Icono_{nombreIcono}_{color.r}_{color.g}_{color.b}_{color.a}";
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            Texture2D tex = null;
            Color32 trans = new Color32(0, 0, 0, 0);

            switch (nombreIcono.ToLowerInvariant())
            {
                case "huella": // Paw 9x8
                    tex = CrearTexturaIcono(9, 8, color, new RectInt[]
                    {
                        new RectInt(0, 2, 2, 2),
                        new RectInt(2, 0, 2, 2),
                        new RectInt(5, 0, 2, 2),
                        new RectInt(7, 2, 2, 2),
                        new RectInt(2, 4, 5, 1),
                        new RectInt(1, 5, 7, 2),
                        new RectInt(2, 7, 5, 1)
                    });
                    break;

                case "flecha_izq": // arl 5x8
                    tex = CrearTexturaIcono(5, 8, color, new RectInt[]
                    {
                        new RectInt(3, 0, 1, 8),
                        new RectInt(2, 1, 1, 6),
                        new RectInt(1, 2, 1, 4),
                        new RectInt(0, 3, 1, 2)
                    });
                    break;

                case "flecha_der": // arr 5x8
                    tex = CrearTexturaIcono(5, 8, color, new RectInt[]
                    {
                        new RectInt(1, 0, 1, 8),
                        new RectInt(2, 1, 1, 6),
                        new RectInt(3, 2, 1, 4),
                        new RectInt(4, 3, 1, 2)
                    });
                    break;

                case "monitor": // Monitor 10x8
                    tex = CrearTexturaHollow(10, 8, color,
                        new RectInt(0, 0, 10, 6), new RectInt(1, 1, 8, 4),
                        new RectInt[]
                        {
                            new RectInt(4, 6, 2, 1),
                            new RectInt(3, 7, 4, 1)
                        });
                    break;

                case "audio": // Bocina 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(0, 3, 2, 2),
                        new RectInt(2, 2, 1, 4),
                        new RectInt(3, 1, 1, 6),
                        new RectInt(5, 2, 1, 1),
                        new RectInt(6, 3, 1, 2),
                        new RectInt(5, 5, 1, 1)
                    });
                    break;

                case "controles": // Gamepad 10x7
                    tex = CrearTexturaHollow(10, 7, color,
                        new RectInt(0, 0, 10, 7), new RectInt(1, 1, 8, 5),
                        new RectInt[]
                        {
                            new RectInt(2, 2, 1, 1),
                            new RectInt(4, 2, 1, 1),
                            new RectInt(6, 2, 1, 1),
                            new RectInt(3, 4, 4, 1)
                        });
                    break;

                case "estrella": // Star 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(3, 0, 2, 2),
                        new RectInt(0, 2, 8, 1),
                        new RectInt(1, 3, 6, 1),
                        new RectInt(2, 4, 4, 1),
                        new RectInt(1, 5, 2, 1),
                        new RectInt(5, 5, 2, 1),
                        new RectInt(0, 6, 2, 1),
                        new RectInt(6, 6, 2, 1)
                    });
                    break;

                case "palomita": // Checkmark 9x6
                    tex = CrearTexturaIcono(9, 6, color, new RectInt[]
                    {
                        new RectInt(7, 0, 2, 1),
                        new RectInt(6, 1, 2, 1),
                        new RectInt(5, 2, 2, 1),
                        new RectInt(0, 3, 2, 1),
                        new RectInt(4, 3, 2, 1),
                        new RectInt(1, 4, 4, 1),
                        new RectInt(2, 5, 2, 1)
                    });
                    break;

                case "play": // Play triángulo 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(1, 0, 2, 8),
                        new RectInt(3, 1, 2, 6),
                        new RectInt(5, 2, 2, 4),
                        new RectInt(7, 3, 1, 2)
                    });
                    break;

                case "engrane": // Engrane / Opciones 9x9
                    tex = CrearTexturaHollow(9, 9, color,
                        new RectInt(2, 2, 5, 5), new RectInt(3, 3, 3, 3),
                        new RectInt[]
                        {
                            new RectInt(4, 0, 1, 2),
                            new RectInt(4, 7, 1, 2),
                            new RectInt(0, 4, 2, 1),
                            new RectInt(7, 4, 2, 1),
                            new RectInt(1, 1, 1, 1),
                            new RectInt(7, 1, 1, 1),
                            new RectInt(1, 7, 1, 1),
                            new RectInt(7, 7, 1, 1)
                        });
                    break;

                case "puerta": // Salir 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(0, 0, 4, 1),
                        new RectInt(0, 1, 1, 6),
                        new RectInt(0, 7, 4, 1),
                        new RectInt(3, 3, 4, 2),
                        new RectInt(5, 2, 1, 4),
                        new RectInt(6, 1, 1, 6)
                    });
                    break;

                case "lapiz": // Editar nombre 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(6, 0, 2, 2),
                        new RectInt(5, 1, 1, 2),
                        new RectInt(4, 2, 1, 2),
                        new RectInt(3, 3, 1, 2),
                        new RectInt(2, 4, 1, 2),
                        new RectInt(1, 5, 1, 2),
                        new RectInt(0, 6, 2, 2)
                    });
                    break;

                case "musica": // BGM Nota musical 8x7
                    tex = CrearTexturaIcono(8, 7, color, new RectInt[]
                    {
                        new RectInt(3, 0, 4, 1),
                        new RectInt(3, 1, 1, 4),
                        new RectInt(6, 1, 1, 3),
                        new RectInt(1, 4, 3, 2),
                        new RectInt(4, 3, 3, 2)
                    });
                    break;

                case "destello": // SFX Chispa 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(3, 0, 1, 7),
                        new RectInt(0, 3, 7, 1),
                        new RectInt(2, 2, 3, 3)
                    });
                    break;

                case "restaurar": // Flecha circular 8x8
                    tex = CrearTexturaIcono(8, 8, color, new RectInt[]
                    {
                        new RectInt(2, 0, 4, 1),
                        new RectInt(1, 1, 1, 1),
                        new RectInt(6, 1, 1, 1),
                        new RectInt(0, 2, 1, 3),
                        new RectInt(7, 2, 1, 3),
                        new RectInt(1, 5, 1, 1),
                        new RectInt(3, 2, 1, 2),
                        new RectInt(4, 3, 1, 2),
                        new RectInt(2, 6, 4, 1),
                        new RectInt(6, 5, 1, 1)
                    });
                    break;
            }

            if (tex == null) return null;

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect, Vector4.zero);
            _cacheSprites[clave] = sprite;
            return sprite;
        }

        private static Texture2D CrearTexturaIcono(int w, int h, Color32 col, RectInt[] rects)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32 trans = new Color32(0, 0, 0, 0);
            Color32[] pixeles = new Color32[w * h];
            for (int i = 0; i < pixeles.Length; i++) pixeles[i] = trans;

            foreach (var r in rects)
            {
                int texY = h - (r.y + r.height);
                for (int y = 0; y < r.height; y++)
                {
                    int py = texY + y;
                    if (py < 0 || py >= h) continue;
                    for (int x = 0; x < r.width; x++)
                    {
                        int px = r.x + x;
                        if (px < 0 || px >= w) continue;
                        pixeles[py * w + px] = col;
                    }
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D CrearTexturaHollow(int w, int h, Color32 col, RectInt outer, RectInt innerHole, RectInt[] extraRects)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32 trans = new Color32(0, 0, 0, 0);
            Color32[] pixeles = new Color32[w * h];
            for (int i = 0; i < pixeles.Length; i++) pixeles[i] = trans;

            // Dibujar outer
            int outerTexY = h - (outer.y + outer.height);
            for (int y = 0; y < outer.height; y++)
            {
                int py = outerTexY + y;
                for (int x = 0; x < outer.width; x++)
                {
                    pixeles[py * w + (outer.x + x)] = col;
                }
            }

            // Vaciar hole
            int holeTexY = h - (innerHole.y + innerHole.height);
            for (int y = 0; y < innerHole.height; y++)
            {
                int py = holeTexY + y;
                for (int x = 0; x < innerHole.width; x++)
                {
                    pixeles[py * w + (innerHole.x + x)] = trans;
                }
            }

            // Dibujar extra rects
            if (extraRects != null)
            {
                foreach (var r in extraRects)
                {
                    int rTexY = h - (r.y + r.height);
                    for (int y = 0; y < r.height; y++)
                    {
                        int py = rTexY + y;
                        for (int x = 0; x < r.width; x++)
                        {
                            pixeles[py * w + (r.x + x)] = col;
                        }
                    }
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply(false);
            return tex;
        }
    }
}
