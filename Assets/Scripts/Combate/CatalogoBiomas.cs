using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Catálogo central de Biomas y generador procedural de arte Pixel-Art de alta fidelidad:
    /// - Texturas de 32x32 a 32 PPU para que cada celda de la cuadrícula coincida exactamente con 1 unidad de Unity.
    /// - Bordes y biselados diseñados para repetición limpia (tiling) sin cortes, desniveles ni costuras visibles.
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
                    b.colorLuzAntorcha = new Color(1f, 0.75f, 0.32f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.96f, 0.70f), new Color(1f, 0.62f, 0.14f), new Color(0.92f, 0.22f, 0.08f));
                    break;

                case TipoBioma.Hielo:
                    b.nombreBioma = "Cripta de Hielo Eterno";
                    b.spriteSuelo = CrearSueloHielo();
                    b.spriteMuro = CrearMuroHielo();
                    b.spriteColumna = CrearColumnaHielo();
                    b.colorLuzAntorcha = new Color(0.38f, 0.88f, 1f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(0.88f, 0.98f, 1f), new Color(0.32f, 0.82f, 1f), new Color(0.14f, 0.48f, 0.94f));
                    break;

                case TipoBioma.Fuego:
                    b.nombreBioma = "Fortaleza Volcánica de Magma";
                    b.spriteSuelo = CrearSueloFuego();
                    b.spriteMuro = CrearMuroFuego();
                    b.spriteColumna = CrearColumnaFuego();
                    b.colorLuzAntorcha = new Color(1f, 0.42f, 0.16f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.94f, 0.48f), new Color(1f, 0.44f, 0.09f), new Color(0.88f, 0.12f, 0.08f));
                    break;

                case TipoBioma.Planta:
                    b.nombreBioma = "Santuario Selvático de Esporas";
                    b.spriteSuelo = CrearSueloPlanta();
                    b.spriteMuro = CrearMuroPlanta();
                    b.spriteColumna = CrearColumnaPlanta();
                    b.colorLuzAntorcha = new Color(0.42f, 1f, 0.48f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(0.90f, 1f, 0.78f), new Color(0.38f, 0.96f, 0.45f), new Color(0.14f, 0.70f, 0.30f));
                    break;

                case TipoBioma.Arena:
                    b.nombreBioma = "Necrópolis de Arenas Doradas";
                    b.spriteSuelo = CrearSueloArena();
                    b.spriteMuro = CrearMuroArena();
                    b.spriteColumna = CrearColumnaArena();
                    b.colorLuzAntorcha = new Color(1f, 0.84f, 0.38f, 1f);
                    b.spriteLlamaAntorcha = CrearLlamaPixel(new Color(1f, 0.98f, 0.78f), new Color(1f, 0.76f, 0.20f), new Color(0.86f, 0.50f, 0.12f));
                    break;
            }

            return b;
        }

        // ========================================================
        // 1. BIOMA: PIEDRA (Mazmorra Medieval Clásica)
        // ========================================================
        private static Sprite CrearSueloPiedra()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cNegro = new Color(0.12f, 0.13f, 0.16f, 1f);
            Color cSombra = new Color(0.18f, 0.20f, 0.24f, 1f);
            Color cBase = new Color(0.24f, 0.26f, 0.31f, 1f);
            Color cLuz = new Color(0.32f, 0.35f, 0.42f, 1f);
            Color cGrava = new Color(0.40f, 0.43f, 0.50f, 1f);

            // Generador continuo toroidal (sin bordes de bloque como Minecraft)
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float angX = (x * 2.0f * Mathf.PI) / s;
                    float angY = (y * 2.0f * Mathf.PI) / s;
                    float u = Mathf.Cos(angX), v = Mathf.Sin(angX);
                    float sy = Mathf.Cos(angY), ty = Mathf.Sin(angY);

                    float val = Mathf.Sin(u * 4.0f + sy * 3.5f) + 
                                Mathf.Cos(v * 3.5f - ty * 4.0f) + 
                                0.5f * Mathf.Sin(angX * 4f + angY * 3f);

                    Color c;
                    if (val < -0.7f) c = cNegro;
                    else if (val < -0.1f) c = cSombra;
                    else if (val < 0.7f) c = cBase;
                    else if (val < 1.4f) c = cLuz;
                    else c = cGrava;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearMuroPiedra()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cPiedra = new Color(0.34f, 0.36f, 0.42f);
            Color cPiedraClara = new Color(0.46f, 0.48f, 0.56f);
            Color cPiedraOscura = new Color(0.22f, 0.23f, 0.28f);
            Color cMortero = new Color(0.10f, 0.11f, 0.14f);
            Color cBordeCap = new Color(0.54f, 0.56f, 0.64f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Cornisa superior del muro (cap 3D)
                    if (y >= 28)
                    {
                        if (y == 31) tex.SetPixel(x, y, cBordeCap);
                        else if (y == 28) tex.SetPixel(x, y, cMortero);
                        else tex.SetPixel(x, y, cPiedraClara);
                        continue;
                    }

                    // 2 filas de bloques de 16x14 con desfase
                    int fila = (y < 14) ? 0 : 1;
                    int ly = (y < 14) ? y : (y - 14);
                    int desfase = (fila == 1) ? 8 : 0;
                    int lx = (x + desfase) % 16;

                    if (lx == 0 || ly == 0)
                    {
                        tex.SetPixel(x, y, cMortero);
                    }
                    else if (ly == 13 || lx == 1)
                    {
                        tex.SetPixel(x, y, cPiedraClara);
                    }
                    else if (ly == 1 || lx == 15)
                    {
                        tex.SetPixel(x, y, cPiedraOscura);
                    }
                    else
                    {
                        Color c = cPiedra;
                        if ((x * 7 + y * 3) % 13 == 0) c = cPiedraClara;
                        else if ((x * 5 + y * 11) % 17 == 0) c = cPiedraOscura;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearColumnaPiedra()
        {
            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cPiedra = new Color(0.40f, 0.42f, 0.48f);
            Color cClaro = new Color(0.56f, 0.58f, 0.66f);
            Color cOscuro = new Color(0.24f, 0.26f, 0.30f);
            Color cSombra = new Color(0.12f, 0.13f, 0.16f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool capitel = (y >= 40);
                    bool baseCol = (y <= 8);
                    int margen = (capitel || baseCol) ? 3 : 6;

                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float nx = (float)(x - margen) / (w - margen * 2 - 1);
                        if (nx < 0.25f) tex.SetPixel(x, y, cClaro);
                        else if (nx > 0.75f) tex.SetPixel(x, y, cSombra);
                        else if (nx > 0.55f) tex.SetPixel(x, y, cOscuro);
                        else tex.SetPixel(x, y, cPiedra);

                        if (y == 47 || y == 7) tex.SetPixel(x, y, cClaro);
                        if (y == 40 || y == 1) tex.SetPixel(x, y, cSombra);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 32f);
        }

        // ========================================================
        // 2. BIOMA: HIELO (Cripta Glacial)
        // ========================================================
        private static Sprite CrearSueloHielo()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            // Paleta de hielo glaciar oscuro y descansado a la vista (sin brillo excesivo)
            Color cAbismo = new Color(0.07f, 0.13f, 0.20f, 1f);   // #122133 Base muy oscura
            Color cHielo = new Color(0.10f, 0.18f, 0.28f, 1f);    // #1a2e47 Tono medio hielo profundo
            Color cVeta = new Color(0.15f, 0.26f, 0.38f, 1f);     // #264261 Fracturas cristalinas internas
            Color cEscarcha = new Color(0.20f, 0.34f, 0.48f, 1f); // #33577a Escarcha sutil en grieta
            Color cGlint = new Color(0.28f, 0.44f, 0.60f, 1f);    // #477099 Destellos mínimos dispersos

            // Generador de textura continua toroidal (100% seamless, sin bordes de bloque como Minecraft)
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float angX = (x * 2.0f * Mathf.PI) / s;
                    float angY = (y * 2.0f * Mathf.PI) / s;
                    float u = Mathf.Cos(angX), v = Mathf.Sin(angX);
                    float sy = Mathf.Cos(angY), ty = Mathf.Sin(angY);

                    // Ondas armónicas toroidales con fracturas diagonales de permafrost
                    float val = Mathf.Sin(u * 3.2f + sy * 2.8f) + 
                                Mathf.Cos(v * 2.8f - ty * 3.2f) + 
                                0.6f * Mathf.Sin(angX * 3f + angY * 2f) +
                                0.3f * Mathf.Cos(angX * 5f - angY * 4f);

                    Color c;
                    if (val < -0.8f) c = cAbismo;
                    else if (val < 0.1f) c = cHielo;
                    else if (val < 0.9f) c = cVeta;
                    else if (val < 1.6f) c = cEscarcha;
                    else c = cGlint;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearMuroHielo()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cCristal = new Color(0.24f, 0.50f, 0.76f);
            Color cEscarcha = new Color(0.85f, 0.96f, 1f);
            Color cAbismo = new Color(0.08f, 0.20f, 0.38f);
            Color cJunta = new Color(0.05f, 0.12f, 0.25f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Témpanos y capa de escarcha superior
                    if (y >= 28)
                    {
                        if (y == 31) tex.SetPixel(x, y, Color.white);
                        else if (y == 28) tex.SetPixel(x, y, cJunta);
                        else tex.SetPixel(x, y, cEscarcha);
                        continue;
                    }

                    int fila = (y < 14) ? 0 : 1;
                    int ly = (y < 14) ? y : (y - 14);
                    int desfase = (fila == 1) ? 8 : 0;
                    int lx = (x + desfase) % 16;

                    if (lx == 0 || ly == 0)
                    {
                        tex.SetPixel(x, y, cJunta);
                    }
                    else if (ly == 13 || lx == 1)
                    {
                        tex.SetPixel(x, y, cEscarcha);
                    }
                    else if (ly == 1 || lx == 15)
                    {
                        tex.SetPixel(x, y, cAbismo);
                    }
                    else
                    {
                        Color c = cCristal;
                        if ((x + y * 2) % 7 == 0) c = cEscarcha;
                        else if ((x * 3 + y) % 11 == 0) c = cAbismo;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearColumnaHielo()
        {
            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cCristal = new Color(0.30f, 0.60f, 0.85f);
            Color cBrillo = new Color(0.85f, 0.98f, 1f);
            Color cSombra = new Color(0.10f, 0.24f, 0.44f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y >= 38 || y <= 8) ? 3 : 5;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float nx = (float)(x - margen) / (w - margen * 2 - 1);
                        Color c = Color.Lerp(cBrillo, cSombra, nx);
                        if (y >= 44 || y == 7) c = Color.white;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 32f);
        }

        // ========================================================
        // 3. BIOMA: FUEGO (Fortaleza de Magma)
        // ========================================================
        private static Sprite CrearSueloFuego()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cObsidiana = new Color(0.10f, 0.08f, 0.11f, 1f);
            Color cBasalto = new Color(0.16f, 0.13f, 0.18f, 1f);
            Color cCeniza = new Color(0.24f, 0.18f, 0.22f, 1f);
            Color cLavaOscura = new Color(0.45f, 0.10f, 0.05f, 1f);
            Color cMagma = new Color(0.75f, 0.22f, 0.05f, 1f);

            // Generador continuo toroidal (sin bordes de bloque como Minecraft)
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float angX = (x * 2.0f * Mathf.PI) / s;
                    float angY = (y * 2.0f * Mathf.PI) / s;
                    float u = Mathf.Cos(angX), v = Mathf.Sin(angX);
                    float sy = Mathf.Cos(angY), ty = Mathf.Sin(angY);

                    float val = Mathf.Sin(u * 3.5f + sy * 3.0f) + 
                                Mathf.Cos(v * 3.0f - ty * 3.5f) + 
                                0.6f * Mathf.Sin(angX * 3f - angY * 3f);

                    Color c;
                    if (val < -0.6f) c = cObsidiana;
                    else if (val < 0.2f) c = cBasalto;
                    else if (val < 0.9f) c = cCeniza;
                    else if (val < 1.5f) c = cLavaOscura;
                    else c = cMagma;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearMuroFuego()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cObsidiana = new Color(0.16f, 0.14f, 0.18f);
            Color cObsidianaBrillo = new Color(0.32f, 0.28f, 0.35f);
            Color cLava = new Color(1f, 0.50f, 0.08f);
            Color cMortero = new Color(0.08f, 0.06f, 0.09f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    if (y >= 28)
                    {
                        if (y == 31) tex.SetPixel(x, y, cObsidianaBrillo);
                        else if (y == 28) tex.SetPixel(x, y, cLava);
                        else tex.SetPixel(x, y, cObsidiana);
                        continue;
                    }

                    int fila = (y < 14) ? 0 : 1;
                    int ly = (y < 14) ? y : (y - 14);
                    int desfase = (fila == 1) ? 8 : 0;
                    int lx = (x + desfase) % 16;

                    if (lx == 0 || ly == 0)
                    {
                        tex.SetPixel(x, y, cMortero);
                    }
                    else if ((lx == 7 && ly > 3 && ly < 10) || (ly == 6 && lx > 2 && lx < 8))
                    {
                        tex.SetPixel(x, y, cLava); // Grieta de lava en el muro
                    }
                    else if (ly == 13 || lx == 1)
                    {
                        tex.SetPixel(x, y, cObsidianaBrillo);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cObsidiana);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearColumnaFuego()
        {
            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cObs = new Color(0.20f, 0.18f, 0.22f);
            Color cLava = new Color(1f, 0.55f, 0.10f);
            Color cSombra = new Color(0.08f, 0.07f, 0.10f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y >= 38 || y <= 8) ? 3 : 5;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float nx = (float)(x - margen) / (w - margen * 2 - 1);
                        Color c = Color.Lerp(cObs, cSombra, nx);
                        if (x == w / 2 && y >= 10 && y <= 36) c = cLava; // Vena central de magma
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 32f);
        }

        // ========================================================
        // 4. BIOMA: PLANTA (Santuario de Esporas)
        // ========================================================
        private static Sprite CrearSueloPlanta()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cTierra = new Color(0.12f, 0.10f, 0.08f, 1f);
            Color cHumus = new Color(0.16f, 0.14f, 0.10f, 1f);
            Color cMusgoOscuro = new Color(0.16f, 0.24f, 0.12f, 1f);
            Color cMusgo = new Color(0.22f, 0.35f, 0.16f, 1f);
            Color cTrebol = new Color(0.30f, 0.46f, 0.20f, 1f);

            // Generador continuo toroidal (sin bordes de bloque como Minecraft)
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float angX = (x * 2.0f * Mathf.PI) / s;
                    float angY = (y * 2.0f * Mathf.PI) / s;
                    float u = Mathf.Cos(angX), v = Mathf.Sin(angX);
                    float sy = Mathf.Cos(angY), ty = Mathf.Sin(angY);

                    float val = Mathf.Sin(u * 4.0f + sy * 3.2f) + 
                                Mathf.Cos(v * 3.2f - ty * 4.0f) + 
                                0.5f * Mathf.Sin(angX * 4f + angY * 2f);

                    Color c;
                    if (val < -0.6f) c = cTierra;
                    else if (val < 0.1f) c = cHumus;
                    else if (val < 0.8f) c = cMusgoOscuro;
                    else if (val < 1.4f) c = cMusgo;
                    else c = cTrebol;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearMuroPlanta()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cPiedra = new Color(0.26f, 0.30f, 0.26f);
            Color cHiedra = new Color(0.18f, 0.52f, 0.22f);
            Color cHoja = new Color(0.36f, 0.78f, 0.38f);
            Color cJunta = new Color(0.10f, 0.12f, 0.10f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    if (y >= 28)
                    {
                        if (y == 31) tex.SetPixel(x, y, cHoja);
                        else if (y == 28) tex.SetPixel(x, y, cJunta);
                        else tex.SetPixel(x, y, cHiedra);
                        continue;
                    }

                    int fila = (y < 14) ? 0 : 1;
                    int ly = (y < 14) ? y : (y - 14);
                    int desfase = (fila == 1) ? 8 : 0;
                    int lx = (x + desfase) % 16;

                    if (lx == 0 || ly == 0)
                    {
                        tex.SetPixel(x, y, cJunta);
                    }
                    else if ((lx == ly) || (lx + ly == 14))
                    {
                        // Enredaderas trepando por el muro
                        tex.SetPixel(x, y, (x % 2 == 0) ? cHoja : cHiedra);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cPiedra);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearColumnaPlanta()
        {
            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cTronco = new Color(0.28f, 0.32f, 0.26f);
            Color cVerde = new Color(0.25f, 0.65f, 0.28f);
            Color cFlor = new Color(0.95f, 0.40f, 0.75f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y >= 38 || y <= 8) ? 3 : 5;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        Color c = cTronco;
                        if ((x + y) % 6 == 0) c = cVerde;
                        if (x == 16 && (y == 20 || y == 35)) c = cFlor;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 32f);
        }

        // ========================================================
        // 5. BIOMA: ARENA (Necrópolis del Desierto)
        // ========================================================
        private static Sprite CrearSueloArena()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cSombra = new Color(0.35f, 0.26f, 0.15f, 1f);
            Color cArena = new Color(0.48f, 0.36f, 0.21f, 1f);
            Color cDuna = new Color(0.58f, 0.44f, 0.26f, 1f);
            Color cCresta = new Color(0.68f, 0.52f, 0.32f, 1f);

            // Generador continuo toroidal (sin bordes de bloque como Minecraft)
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float angX = (x * 2.0f * Mathf.PI) / s;
                    float angY = (y * 2.0f * Mathf.PI) / s;
                    float u = Mathf.Cos(angX), v = Mathf.Sin(angX);
                    float sy = Mathf.Cos(angY), ty = Mathf.Sin(angY);

                    // Ondas de viento diagonales continuas
                    float val = Mathf.Sin(angX * 3.0f + angY * 2.0f + u * 1.5f) + 0.3f * Mathf.Cos(v * 2f - sy * 2f);

                    Color c;
                    if (val < -0.4f) c = cSombra;
                    else if (val < 0.2f) c = cArena;
                    else if (val < 0.7f) c = cDuna;
                    else c = cCresta;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearMuroArena()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color cArenisca = new Color(0.68f, 0.52f, 0.30f);
            Color cOro = new Color(0.95f, 0.78f, 0.22f);
            Color cSombra = new Color(0.45f, 0.32f, 0.16f);
            Color cJunta = new Color(0.28f, 0.20f, 0.10f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    if (y >= 28)
                    {
                        if (y == 31) tex.SetPixel(x, y, cOro); // Friso dorado superior
                        else if (y == 28) tex.SetPixel(x, y, cJunta);
                        else tex.SetPixel(x, y, cArenisca);
                        continue;
                    }

                    int fila = (y < 14) ? 0 : 1;
                    int ly = (y < 14) ? y : (y - 14);
                    int desfase = (fila == 1) ? 8 : 0;
                    int lx = (x + desfase) % 16;

                    if (lx == 0 || ly == 0)
                    {
                        tex.SetPixel(x, y, cJunta);
                    }
                    else if (ly == 7 && lx >= 5 && lx <= 11)
                    {
                        tex.SetPixel(x, y, cOro); // Jeroglífico dorado grabado
                    }
                    else if (ly == 13 || lx == 1)
                    {
                        tex.SetPixel(x, y, cOro);
                    }
                    else
                    {
                        tex.SetPixel(x, y, (ly == 1 || lx == 15) ? cSombra : cArenisca);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CrearColumnaArena()
        {
            int w = 32, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color cArenisca = new Color(0.70f, 0.54f, 0.32f);
            Color cOro = new Color(0.95f, 0.80f, 0.25f);
            Color cLapis = new Color(0.18f, 0.38f, 0.80f);
            Color cSombra = new Color(0.42f, 0.30f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int margen = (y >= 38 || y <= 8) ? 3 : 5;
                    if (x < margen || x >= w - margen)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        Color c = cArenisca;
                        if (y >= 42 || y == 6) c = cOro; // Capitel de flor de loto dorada
                        if (y == 24 || y == 25) c = cLapis; // Banda sagrada de lapislázuli
                        if (x == w - margen - 1) c = cSombra;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.35f), 32f);
        }

        // ========================================================
        // ANTORCHA Y LLAMA PIXEL ART
        // ========================================================
        public static Sprite CrearLlamaPixel(Color cNucleo, Color cCuerpo, Color cBorde)
        {
            int w = 16, h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Vector2 basePos = new Vector2(7.5f, 4f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - basePos.x);
                    float dy = y - basePos.y;

                    if (dy < 0f || dy > 18f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Forma de llama ascendente cónica
                    float anchoLlama = (dy < 6f) ? (3.5f + dy * 0.4f) : Mathf.Max(0.5f, 6.0f - (dy - 6f) * 0.45f);

                    if (dx <= anchoLlama)
                    {
                        if (dx <= anchoLlama * 0.35f && dy <= 10f)
                            tex.SetPixel(x, y, cNucleo);
                        else if (dx <= anchoLlama * 0.72f && dy <= 14f)
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 32f);
        }
    }
}
