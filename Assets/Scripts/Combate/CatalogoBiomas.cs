using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Catálogo central de Biomas y generador procedural de arte Pixel-Art de alta fidelidad:
    /// - Piedra: Mazmorra medieval ancestral, cantería gris labrada y antorchas ámbar.
    /// - Hielo: Cripta glacial con baldosas de escarcha, muros de cristal azur y fuego frío.
    /// - Fuego: Fortaleza volcánica con losas de basalto, venas de magma incandescente y antorchas ardientes.
    /// - Planta: Ruinas de la selva cubiertas de musgo esmeralda, enredaderas trepadoras y flores luminiscentes.
    /// - Arena: Necrópolis desértica con losas doradas de arenisca, jeroglíficos y pebeteros sagrados.
    /// </summary>
    public static class CatalogoBiomas
    {
        private static readonly Dictionary<TipoBioma, DatosBioma> _cacheBiomas = new Dictionary<TipoBioma, DatosBioma>();

        public static DatosBioma Obtener(TipoBioma tipo)
        {
            if (_cacheBiomas.TryGetValue(tipo, out var datos) && datos != null && datos.spriteSuelo != null)
            {
                return datos;
            }

            datos = GenerarDatosBioma(tipo);
            _cacheBiomas[tipo] = datos;
            return datos;
        }

        private static DatosBioma GenerarDatosBioma(TipoBioma tipo)
        {
            DatosBioma b = new DatosBioma();
            b.tipo = tipo;
            b.colorFondoCamara = Color.black;

            switch (tipo)
            {
                case TipoBioma.Piedra:
                    b.nombreBioma = "Mazmorra de Piedra Ancestral";
                    b.spriteSuelo = CrearSueloPiedra();
                    b.spriteMuro = CrearMuroPiedra();
                    b.spriteColumna = CrearColumnaPiedra();
                    b.colorLuzAntorcha = new Color(1f, 0.72f, 0.28f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.95f, 0.65f), new Color(1f, 0.60f, 0.12f), new Color(0.95f, 0.25f, 0.08f));
                    break;

                case TipoBioma.Hielo:
                    b.nombreBioma = "Cripta de Hielo Eterno";
                    b.spriteSuelo = CrearSueloHielo();
                    b.spriteMuro = CrearMuroHielo();
                    b.spriteColumna = CrearColumnaHielo();
                    b.colorLuzAntorcha = new Color(0.35f, 0.85f, 1f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(0.85f, 0.98f, 1f), new Color(0.28f, 0.78f, 1f), new Color(0.12f, 0.45f, 0.92f));
                    break;

                case TipoBioma.Fuego:
                    b.nombreBioma = "Fortaleza Volcánica de Magma";
                    b.spriteSuelo = CrearSueloFuego();
                    b.spriteMuro = CrearMuroFuego();
                    b.spriteColumna = CrearColumnaFuego();
                    b.colorLuzAntorcha = new Color(1f, 0.40f, 0.15f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.92f, 0.45f), new Color(1f, 0.42f, 0.08f), new Color(0.90f, 0.12f, 0.08f));
                    break;

                case TipoBioma.Planta:
                    b.nombreBioma = "Santuario Selvático de Esporas";
                    b.spriteSuelo = CrearSueloPlanta();
                    b.spriteMuro = CrearMuroPlanta();
                    b.spriteColumna = CrearColumnaPlanta();
                    b.colorLuzAntorcha = new Color(0.40f, 1f, 0.45f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(0.88f, 1f, 0.75f), new Color(0.35f, 0.95f, 0.42f), new Color(0.12f, 0.68f, 0.28f));
                    break;

                case TipoBioma.Arena:
                    b.nombreBioma = "Necrópolis de Arenas Doradas";
                    b.spriteSuelo = CrearSueloArena();
                    b.spriteMuro = CrearMuroArena();
                    b.spriteColumna = CrearColumnaArena();
                    b.colorLuzAntorcha = new Color(1f, 0.82f, 0.35f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.98f, 0.75f), new Color(1f, 0.75f, 0.18f), new Color(0.85f, 0.48f, 0.10f));
                    break;
            }

            return b;
        }

        // ==========================================
        // 1. BIOMA: PIEDRA (Mazmorra Medieval)
        // ==========================================
        private static Sprite CrearSueloPiedra()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cBase = new Color(0.32f, 0.33f, 0.36f);
            Color cClaro = new Color(0.40f, 0.41f, 0.44f);
            Color cOscuro = new Color(0.24f, 0.25f, 0.27f);
            Color cMortero = new Color(0.14f, 0.15f, 0.16f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Cuadrícula de 4 grandes losas (16x16 cada una) con biselado
                    bool juntaH = (y == 0 || y == 15 || y == 16 || y == 31);
                    bool juntaV = (x == 0 || x == 15 || x == 16 || x == 31);

                    if (juntaH || juntaV)
                    {
                        tex.SetPixel(x, y, cMortero);
                    }
                    else
                    {
                        int lx = x % 16;
                        int ly = y % 16;
                        // Biselado 3D
                        if (ly == 14 || lx == 1)
                        {
                            tex.SetPixel(x, y, cClaro);
                        }
                        else if (ly == 1 || lx == 14)
                        {
                            tex.SetPixel(x, y, cOscuro);
                        }
                        else
                        {
                            // Texturizado con ruido pixel art
                            float n = Mathf.PerlinNoise(x * 0.45f, y * 0.45f);
                            Color cPiedra = (n > 0.65f) ? cClaro : (n < 0.35f ? cOscuro : cBase);
                            // Grieta ocasional
                            if ((lx == 7 && ly > 4 && ly < 11) || (lx == 8 && ly == 8))
                            {
                                cPiedra = cMortero;
                            }
                            tex.SetPixel(x, y, cPiedra);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearMuroPiedra()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cLadrillo = new Color(0.42f, 0.44f, 0.48f);
            Color cLadrilloClaro = new Color(0.52f, 0.54f, 0.58f);
            Color cLadrilloOscuro = new Color(0.28f, 0.30f, 0.33f);
            Color cJunta = new Color(0.12f, 0.13f, 0.15f);

            for (int y = 0; y < s; y++)
            {
                int fila = y / 8; // 4 filas de ladrillos
                int ly = y % 8;
                int desfaseX = (fila % 2 == 1) ? 8 : 0;

                for (int x = 0; x < s; x++)
                {
                    int lx = (x + desfaseX) % 16; // Ladrillos de 16x8

                    if (ly == 0 || lx == 0)
                    {
                        tex.SetPixel(x, y, cJunta);
                    }
                    else if (ly == 7 || lx == 1)
                    {
                        tex.SetPixel(x, y, cLadrilloClaro);
                    }
                    else if (ly == 1 || lx == 15)
                    {
                        tex.SetPixel(x, y, cLadrilloOscuro);
                    }
                    else
                    {
                        float ruido = Mathf.Sin(x * 12.3f + y * 7.7f);
                        Color c = (ruido > 0.4f) ? cLadrilloClaro : (ruido < -0.4f ? cLadrilloOscuro : cLadrillo);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearColumnaPiedra()
        {
            int w = 24, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cPiedra = new Color(0.44f, 0.46f, 0.50f);
            Color cClaro = new Color(0.60f, 0.62f, 0.66f);
            Color cOscuro = new Color(0.26f, 0.28f, 0.31f);
            Color cSombra = new Color(0.14f, 0.15f, 0.17f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool enCapitel = (y >= 30);
                    bool enBase = (y <= 6);
                    int margen = (enCapitel || enBase) ? 2 : 4;

                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float nx = (float)(x - margen) / (w - margen * 2 - 1);
                        if (nx < 0.22f) tex.SetPixel(x, y, cClaro);
                        else if (nx > 0.78f) tex.SetPixel(x, y, cSombra);
                        else if (nx > 0.58f) tex.SetPixel(x, y, cOscuro);
                        else tex.SetPixel(x, y, cPiedra);

                        // Molduras horizontales en base y capitel
                        if (y == 31 || y == 5) tex.SetPixel(x, y, cClaro);
                        if (y == 29 || y == 1) tex.SetPixel(x, y, cSombra);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ==========================================
        // 2. BIOMA: HIELO (Cripta Glacial)
        // ==========================================
        private static Sprite CrearSueloHielo()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cGlaciar = new Color(0.38f, 0.68f, 0.88f);
            Color cBrillo = new Color(0.85f, 0.96f, 1f);
            Color cProfundo = new Color(0.20f, 0.42f, 0.65f);
            Color cGrieta = new Color(0.12f, 0.25f, 0.45f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool bordeH = (y == 0 || y == 31);
                    bool bordeV = (x == 0 || x == 31);
                    if (bordeH || bordeV)
                    {
                        tex.SetPixel(x, y, cGrieta);
                        continue;
                    }

                    // Cristales angulares de hielo
                    float d1 = Mathf.Abs(x - y);
                    float d2 = Mathf.Abs(x + y - 31);
                    if (d1 == 0 || d2 == 0)
                    {
                        tex.SetPixel(x, y, cProfundo);
                    }
                    else if (d1 == 1 || d2 == 1)
                    {
                        tex.SetPixel(x, y, cBrillo);
                    }
                    else
                    {
                        float n = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                        Color c = (n > 0.6f) ? cBrillo : (n < 0.3f ? cProfundo : cGlaciar);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearMuroHielo()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cHielo = new Color(0.28f, 0.58f, 0.82f);
            Color cEscarcha = new Color(0.82f, 0.95f, 1f);
            Color cSombraAzul = new Color(0.15f, 0.32f, 0.52f);
            Color cBordeOscuro = new Color(0.08f, 0.18f, 0.35f);

            for (int y = 0; y < s; y++)
            {
                int fila = y / 8;
                int ly = y % 8;
                int desf = (fila % 2 == 1) ? 8 : 0;

                for (int x = 0; x < s; x++)
                {
                    int lx = (x + desf) % 16;
                    if (ly == 0 || lx == 0)
                    {
                        tex.SetPixel(x, y, cBordeOscuro);
                    }
                    else if (ly == 7 || lx == 1)
                    {
                        // Brillo de escarcha superior
                        tex.SetPixel(x, y, cEscarcha);
                    }
                    else if (ly == 1 || lx == 15)
                    {
                        tex.SetPixel(x, y, cSombraAzul);
                    }
                    else
                    {
                        float ruido = Mathf.Sin(x * 8.5f + y * 14.1f);
                        tex.SetPixel(x, y, ruido > 0.35f ? cEscarcha : (ruido < -0.3f ? cSombraAzul : cHielo));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearColumnaHielo()
        {
            int w = 24, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cPuro = new Color(0.42f, 0.75f, 0.95f);
            Color cReflejo = new Color(0.92f, 0.98f, 1f);
            Color cOscuro = new Color(0.18f, 0.38f, 0.62f);

            for (int y = 0; y < h; y++)
            {
                // Forma de estalagmita afilada
                int anchoCuerpo = (y > 28) ? Mathf.RoundToInt(Mathf.Lerp(6f, 1f, (y - 28f) / 8f)) : Mathf.RoundToInt(Mathf.Lerp(9f, 6f, y / 28f));
                int cx = w / 2;

                for (int x = 0; x < w; x++)
                {
                    int dist = Mathf.Abs(x - cx);
                    if (dist > anchoCuerpo)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        if (x < cx - 1) tex.SetPixel(x, y, cReflejo);
                        else if (x > cx + 1) tex.SetPixel(x, y, cOscuro);
                        else tex.SetPixel(x, y, cPuro);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ==========================================
        // 3. BIOMA: FUEGO (Fortaleza de Magma)
        // ==========================================
        private static Sprite CrearSueloFuego()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cBasalto = new Color(0.18f, 0.16f, 0.18f);
            Color cBasaltoClaro = new Color(0.28f, 0.24f, 0.26f);
            Color cLavaViva = new Color(1f, 0.88f, 0.25f);
            Color cLavaNaranja = new Color(1f, 0.42f, 0.05f);
            Color cLavaRoja = new Color(0.78f, 0.12f, 0.05f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Ríos de magma en las juntas de basalto
                    bool lavaH = (y == 0 || y == 16);
                    bool lavaV = (x == 0 || x == 16);

                    if (lavaH || lavaV)
                    {
                        float pulso = Mathf.Sin(x * 0.8f + y * 0.8f);
                        tex.SetPixel(x, y, pulso > 0.4f ? cLavaViva : (pulso < -0.3f ? cLavaRoja : cLavaNaranja));
                    }
                    else
                    {
                        float n = Mathf.PerlinNoise(x * 0.4f, y * 0.4f);
                        // Grietas volcánicas internas
                        if ((x == 6 && y > 3 && y < 13) || (x == 22 && y > 18 && y < 29))
                        {
                            tex.SetPixel(x, y, cLavaNaranja);
                        }
                        else
                        {
                            tex.SetPixel(x, y, n > 0.55f ? cBasaltoClaro : cBasalto);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearMuroFuego()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cRoca = new Color(0.22f, 0.18f, 0.20f);
            Color cBordeRojo = new Color(0.85f, 0.22f, 0.08f);
            Color cBordeNaranja = new Color(1f, 0.55f, 0.12f);
            Color cCeniza = new Color(0.10f, 0.08f, 0.10f);

            for (int y = 0; y < s; y++)
            {
                int fila = y / 8;
                int ly = y % 8;
                int desf = (fila % 2 == 1) ? 8 : 0;

                for (int x = 0; x < s; x++)
                {
                    int lx = (x + desf) % 16;
                    if (ly == 0 || lx == 0)
                    {
                        tex.SetPixel(x, y, cBordeRojo);
                    }
                    else if (ly == 1 || lx == 1)
                    {
                        tex.SetPixel(x, y, cBordeNaranja);
                    }
                    else if (ly == 7 || lx == 15)
                    {
                        tex.SetPixel(x, y, cCeniza);
                    }
                    else
                    {
                        float r = Mathf.Sin(x * 9f + y * 13f);
                        tex.SetPixel(x, y, r > 0.6f ? new Color(0.45f, 0.25f, 0.18f) : cRoca);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearColumnaFuego()
        {
            int w = 24, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cBasalto = new Color(0.20f, 0.16f, 0.18f);
            Color cLava = new Color(1f, 0.55f, 0.08f);
            Color cLavaNucleo = new Color(1f, 0.92f, 0.35f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y <= 4 || y >= 32) ? 2 : 4;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        // Grieta de magma vertical en el centro
                        if (x == 11 || x == 12)
                        {
                            tex.SetPixel(x, y, (x == 11 && y % 3 == 0) ? cLavaNucleo : cLava);
                        }
                        else if (x == 10 || x == 13)
                        {
                            tex.SetPixel(x, y, new Color(0.75f, 0.18f, 0.05f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, x < 10 ? new Color(0.35f, 0.28f, 0.30f) : cBasalto);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ==========================================
        // 4. BIOMA: PLANTA (Santuario de Selva)
        // ==========================================
        private static Sprite CrearSueloPlanta()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cTierra = new Color(0.25f, 0.18f, 0.12f);
            Color cMusgo = new Color(0.22f, 0.52f, 0.20f);
            Color cMusgoClaro = new Color(0.35f, 0.72f, 0.28f);
            Color cFlor1 = new Color(1f, 0.45f, 0.65f); // Rosa
            Color cFlor2 = new Color(0.95f, 0.88f, 0.25f); // Amarilla

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float ruido = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    Color c = (ruido > 0.45f) ? cMusgoClaro : ((ruido > 0.25f) ? cMusgo : cTierra);

                    // Flores esporádicas pixel art
                    if ((x == 7 && y == 9) || (x == 23 && y == 19)) c = cFlor1;
                    if ((x == 15 && y == 27) || (x == 28 && y == 6)) c = cFlor2;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearMuroPlanta()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cPiedraVerde = new Color(0.32f, 0.40f, 0.30f);
            Color cPiedraClara = new Color(0.42f, 0.52f, 0.38f);
            Color cEnredadera = new Color(0.14f, 0.45f, 0.15f);
            Color cHoja = new Color(0.28f, 0.75f, 0.25f);
            Color cSombra = new Color(0.12f, 0.18f, 0.12f);

            for (int y = 0; y < s; y++)
            {
                int fila = y / 8;
                int ly = y % 8;
                int desf = (fila % 2 == 1) ? 8 : 0;

                for (int x = 0; x < s; x++)
                {
                    int lx = (x + desf) % 16;
                    if (ly == 0 || lx == 0)
                    {
                        tex.SetPixel(x, y, cSombra);
                    }
                    else
                    {
                        // Enredaderas trepadoras orgánicas
                        bool esRama = (Mathf.Sin(y * 0.45f) * 4f + 8f == lx) || (Mathf.Cos(x * 0.45f) * 3f + 4f == ly);
                        if (esRama)
                        {
                            tex.SetPixel(x, y, (x % 2 == 0) ? cHoja : cEnredadera);
                        }
                        else if (ly == 7 || lx == 1)
                        {
                            tex.SetPixel(x, y, cPiedraClara);
                        }
                        else
                        {
                            tex.SetPixel(x, y, cPiedraVerde);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearColumnaPlanta()
        {
            int w = 24, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cMadera = new Color(0.38f, 0.25f, 0.14f);
            Color cCortezaClara = new Color(0.52f, 0.36f, 0.20f);
            Color cSombra = new Color(0.22f, 0.14f, 0.08f);
            Color cFollaje = new Color(0.25f, 0.68f, 0.22f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Corona de hojas arriba
                    if (y >= 30)
                    {
                        int distH = Mathf.Abs(x - 12);
                        if (distH <= 9)
                        {
                            tex.SetPixel(x, y, (x + y) % 2 == 0 ? cFollaje : new Color(0.38f, 0.82f, 0.30f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        continue;
                    }

                    // Raíces que se ensanchan en la base
                    int ancho = (y < 8) ? Mathf.RoundToInt(Mathf.Lerp(9f, 6f, y / 8f)) : 6;
                    int dist = Mathf.Abs(x - 12);

                    if (dist > ancho)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        if (x < 10) tex.SetPixel(x, y, cCortezaClara);
                        else if (x > 14) tex.SetPixel(x, y, cSombra);
                        else tex.SetPixel(x, y, cMadera);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ==========================================
        // 5. BIOMA: ARENA (Necrópolis del Desierto)
        // ==========================================
        private static Sprite CrearSueloArena()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cArenisca = new Color(0.78f, 0.65f, 0.42f);
            Color cAreniscaClara = new Color(0.88f, 0.76f, 0.52f);
            Color cAreniscaOscura = new Color(0.62f, 0.48f, 0.28f);
            Color cMorteroArena = new Color(0.48f, 0.36f, 0.20f);
            Color cJeroglifico = new Color(0.52f, 0.40f, 0.22f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool juntaH = (y == 0 || y == 16);
                    bool juntaV = (x == 0 || x == 16);

                    if (juntaH || juntaV)
                    {
                        tex.SetPixel(x, y, cMorteroArena);
                    }
                    else
                    {
                        int lx = x % 16;
                        int ly = y % 16;

                        if (ly == 15 || lx == 1)
                        {
                            tex.SetPixel(x, y, cAreniscaClara);
                        }
                        else if (ly == 1 || lx == 15)
                        {
                            tex.SetPixel(x, y, cAreniscaOscura);
                        }
                        else
                        {
                            // Grabado de escarabajo / ojo egipcio estilizado en el centro de la losa
                            bool esGrabado = (lx >= 6 && lx <= 9 && ly == 8) || (lx == 8 && ly >= 6 && ly <= 10);
                            if (esGrabado)
                            {
                                tex.SetPixel(x, y, cJeroglifico);
                            }
                            else
                            {
                                float n = Mathf.PerlinNoise(x * 0.4f, y * 0.4f);
                                tex.SetPixel(x, y, n > 0.6f ? cAreniscaClara : (n < 0.35f ? cAreniscaOscura : cArenisca));
                            }
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearMuroArena()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cBloque = new Color(0.82f, 0.68f, 0.45f);
            Color cBordeOro = new Color(0.95f, 0.82f, 0.55f);
            Color cSombra = new Color(0.52f, 0.38f, 0.22f);
            Color cFranjaTurquesa = new Color(0.22f, 0.65f, 0.68f);

            for (int y = 0; y < s; y++)
            {
                int fila = y / 8;
                int ly = y % 8;
                int desf = (fila % 2 == 1) ? 8 : 0;

                for (int x = 0; x < s; x++)
                {
                    int lx = (x + desf) % 16;
                    if (ly == 0 || lx == 0)
                    {
                        tex.SetPixel(x, y, cSombra);
                    }
                    else if (fila == 2 && (ly == 3 || ly == 4))
                    {
                        // Cenefa ceremonial dorada y turquesa
                        tex.SetPixel(x, y, (lx % 4 == 0) ? cBordeOro : cFranjaTurquesa);
                    }
                    else if (ly == 7 || lx == 1)
                    {
                        tex.SetPixel(x, y, cBordeOro);
                    }
                    else
                    {
                        float r = Mathf.Sin(x * 11f + y * 9f);
                        tex.SetPixel(x, y, r > 0.4f ? cBordeOro : (r < -0.4f ? cSombra : cBloque));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearColumnaArena()
        {
            int w = 24, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cArenisca = new Color(0.80f, 0.66f, 0.42f);
            Color cOro = new Color(0.95f, 0.82f, 0.35f);
            Color cSombra = new Color(0.52f, 0.38f, 0.22f);
            Color cTurquesa = new Color(0.18f, 0.65f, 0.68f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y <= 5 || y >= 29) ? 2 : 4;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        // Franjas de oro y turquesa en el capitel y base
                        if (y == 28 || y == 6)
                        {
                            tex.SetPixel(x, y, cOro);
                        }
                        else if (y == 27 || y == 7)
                        {
                            tex.SetPixel(x, y, cTurquesa);
                        }
                        else
                        {
                            if (x < 8) tex.SetPixel(x, y, cOro);
                            else if (x > 15) tex.SetPixel(x, y, cSombra);
                            else tex.SetPixel(x, y, cArenisca);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ==========================================
        // FLAMAS DE ANTORCHAS / PEBETEROS PIXEL ART
        // ==========================================
        private static Sprite CrearLlamaPixel(Color cNucleo, Color cCuerpo, Color cBorde)
        {
            int w = 12, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - 5.5f);
                    float radioMax = (y < 6) ? 3.5f : Mathf.Lerp(3.5f, 0.5f, (y - 6f) / 9f);
                    if (y >= 2 && y <= 14 && dx <= radioMax)
                    {
                        if (dx <= radioMax * 0.45f && y <= 9)
                            tex.SetPixel(x, y, cNucleo);
                        else if (dx <= radioMax * 0.75f)
                            tex.SetPixel(x, y, cCuerpo);
                        else
                            tex.SetPixel(x, y, cBorde);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 16f);
        }
    }
}
