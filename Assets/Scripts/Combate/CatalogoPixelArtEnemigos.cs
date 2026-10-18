using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum RolEnemigo
    {
        Tanque,
        MeleeRapido,
        Rango,
        Especial,
        Spawner
    }

    /// <summary>
    /// Generador y catálogo de arte Pixel-Art de alta fidelidad para todos los enemigos y jefes
    /// de los 5 biomas de la mazmorra.
    /// </summary>
    public static class CatalogoPixelArtEnemigos
    {
        private static readonly Dictionary<string, Sprite> _cacheSprites = new Dictionary<string, Sprite>();

        public static Sprite ObtenerSpriteEnemigo(TipoBioma bioma, RolEnemigo rol)
        {
            string clave = $"{bioma}_{rol}";
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            sp = GenerarSpriteEnemigo(bioma, rol);
            _cacheSprites[clave] = sp;
            return sp;
        }

        public static Sprite ObtenerSpriteJefe(TipoBioma bioma)
        {
            string clave = $"Jefe_{bioma}";
            if (_cacheSprites.TryGetValue(clave, out var sp) && sp != null) return sp;

            sp = GenerarSpriteJefe(bioma);
            _cacheSprites[clave] = sp;
            return sp;
        }

        private static Sprite GenerarSpriteEnemigo(TipoBioma bioma, RolEnemigo rol)
        {
            switch (bioma)
            {
                case TipoBioma.Piedra:
                    if (rol == RolEnemigo.Tanque) return CrearPixelCaballeroHierro();
                    if (rol == RolEnemigo.MeleeRapido) return CrearPixelGargola();
                    if (rol == RolEnemigo.Rango) return CrearPixelBallestero();
                    return CrearPixelFantasma();

                case TipoBioma.Hielo:
                    if (rol == RolEnemigo.Tanque) return CrearPixelGolemHielo();
                    if (rol == RolEnemigo.MeleeRapido) return CrearPixelLoboPolar();
                    if (rol == RolEnemigo.Rango) return CrearPixelMagoGlacial();
                    return CrearPixelMurcielagoHielo();

                case TipoBioma.Fuego:
                    if (rol == RolEnemigo.Tanque) return CrearPixelEngendroMagma();
                    if (rol == RolEnemigo.MeleeRapido) return CrearPixelSabuesoIgneo();
                    if (rol == RolEnemigo.Rango) return CrearPixelChamanFuego();
                    return CrearPixelFatuoVolcanico();

                case TipoBioma.Planta:
                    if (rol == RolEnemigo.Tanque) return CrearPixelTreant();
                    if (rol == RolEnemigo.MeleeRapido) return CrearPixelMandragora();
                    if (rol == RolEnemigo.Rango) return CrearPixelFlorCarnivora();
                    return CrearPixelHongoEsporas();

                case TipoBioma.Arena:
                    if (rol == RolEnemigo.Tanque) return CrearPixelSarcofagoAndante();
                    if (rol == RolEnemigo.Spawner) return CrearPixelAtaudRitual();
                    if (rol == RolEnemigo.MeleeRapido) return CrearPixelRataDesierto();
                    return CrearPixelMomia();
            }

            return CrearPixelCaballeroHierro();
        }

        // ========================================================
        // 1. PIEDRA
        // ========================================================
        private static Sprite CrearPixelCaballeroHierro()
        {
            // 24x28 Caballero acorazado con escudo y yelmo
            int w = 24, h = 28;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cAcero = new Color(0.55f, 0.58f, 0.65f);
            Color cAceroBrillo = new Color(0.85f, 0.88f, 0.95f);
            Color cSombra = new Color(0.25f, 0.28f, 0.35f);
            Color cVisor = new Color(0.1f, 0.12f, 0.15f);
            Color cOjoRojo = new Color(1f, 0.2f, 0.2f);
            Color cPenacho = new Color(0.85f, 0.15f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Penacho rojo arriba
                    if (y >= 24 && x >= 10 && x <= 13) { t.SetPixel(x, y, cPenacho); continue; }

                    // Yelmo
                    if (y >= 18 && y <= 23 && x >= 8 && x <= 15)
                    {
                        if (y == 20 && (x == 10 || x == 13)) t.SetPixel(x, y, cOjoRojo);
                        else if (y == 20) t.SetPixel(x, y, cVisor);
                        else t.SetPixel(x, y, (x <= 11) ? cAceroBrillo : cAcero);
                        continue;
                    }

                    // Pecho y Escudo grande a la izquierda (x: 3 a 10, y: 7 a 18)
                    if (x >= 3 && x <= 10 && y >= 7 && y <= 18)
                    {
                        if (x == 3 || y == 7 || y == 18) t.SetPixel(x, y, cSombra);
                        else if (x == 4 || y == 17) t.SetPixel(x, y, cAceroBrillo);
                        else if (x == 6 && y == 12) t.SetPixel(x, y, cOjoRojo); // Cruz/Gema central
                        else t.SetPixel(x, y, cAcero);
                        continue;
                    }

                    // Torso y Espada a la derecha
                    if (x >= 11 && x <= 18 && y >= 8 && y <= 17)
                    {
                        t.SetPixel(x, y, (x > 15) ? cSombra : cAcero);
                        continue;
                    }

                    // Piernas
                    if (y <= 7 && ((x >= 9 && x <= 12) || (x >= 14 && x <= 17)))
                    {
                        t.SetPixel(x, y, (y <= 2) ? cSombra : cAcero);
                        continue;
                    }

                    t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelGargola()
        {
            // 24x24 Gárgola alada
            int w = 24, h = 24;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cGris = new Color(0.42f, 0.44f, 0.48f);
            Color cOscuro = new Color(0.24f, 0.25f, 0.28f);
            Color cOjo = new Color(1f, 0.85f, 0.2f); // Ojos amarillos brillantes

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Alas desplegadas
                    bool alaIzq = (x >= 2 && x <= 8 && y >= 12 && y <= 20 && x + y >= 16);
                    bool alaDer = (x >= 15 && x <= 21 && y >= 12 && y <= 20 && y - x <= 2);
                    if (alaIzq || alaDer) { t.SetPixel(x, y, (x == 2 || x == 21) ? cOscuro : cGris); continue; }

                    // Cabeza cornuda
                    if (y >= 14 && y <= 19 && x >= 9 && x <= 14)
                    {
                        if (y == 16 && (x == 10 || x == 13)) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, cGris);
                        continue;
                    }

                    // Cuerpo agazapado
                    if (y >= 4 && y <= 13 && x >= 8 && x <= 15)
                    {
                        t.SetPixel(x, y, (x < 11) ? cGris : cOscuro);
                        continue;
                    }

                    t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelBallestero()
        {
            int w = 22, h = 26;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cHueso = new Color(0.88f, 0.88f, 0.82f);
            Color cMadera = new Color(0.48f, 0.28f, 0.14f);
            Color cHierro = new Color(0.6f, 0.65f, 0.7f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Calavera
                    if (y >= 18 && y <= 24 && x >= 8 && x <= 14)
                    {
                        if (y == 20 && (x == 10 || x == 12)) t.SetPixel(x, y, Color.black);
                        else t.SetPixel(x, y, cHueso);
                        continue;
                    }
                    // Ballesta horizontal en brazos
                    if (y == 13 && x >= 3 && x <= 19) { t.SetPixel(x, y, cHierro); continue; }
                    if (x == 11 && y >= 10 && y <= 15) { t.SetPixel(x, y, cMadera); continue; }
                    // Costillas y piernas de esqueleto
                    if (y >= 2 && y <= 17 && x >= 9 && x <= 13)
                    {
                        t.SetPixel(x, y, (y % 3 == 0) ? Color.black : cHueso);
                        continue;
                    }
                    t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelFantasma()
        {
            int w = 20, h = 26;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cEtereo = new Color(0.65f, 0.85f, 0.95f, 0.85f);
            Color cBrillo = new Color(0.9f, 0.98f, 1f, 0.95f);
            Color cOjo = new Color(0.1f, 0.35f, 0.65f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 10);
                    int maxDx = (y > 18) ? 6 : (y > 6 ? 7 : Mathf.RoundToInt(6 - (6 - y) * 0.8f));
                    if (dx <= maxDx && y >= 2 && y <= 23)
                    {
                        if (y == 17 && (x == 8 || x == 12)) t.SetPixel(x, y, cOjo);
                        else if (y == 14 && x == 10) t.SetPixel(x, y, cOjo); // Boca
                        else t.SetPixel(x, y, (x <= 9) ? cBrillo : cEtereo);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ========================================================
        // 2. HIELO
        // ========================================================
        private static Sprite CrearPixelGolemHielo()
        {
            int w = 28, h = 30;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cHielo = new Color(0.38f, 0.68f, 0.88f);
            Color cBrillo = new Color(0.85f, 0.96f, 1f);
            Color cSombra = new Color(0.18f, 0.38f, 0.58f);
            Color cOjoCian = new Color(0.1f, 1f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 14);
                    if (dx <= 11 && y >= 4 && y <= 26)
                    {
                        if (y >= 20 && y <= 21 && (x == 11 || x == 16)) t.SetPixel(x, y, cOjoCian);
                        else if (x <= 13) t.SetPixel(x, y, cBrillo);
                        else if (x >= 20) t.SetPixel(x, y, cSombra);
                        else t.SetPixel(x, y, cHielo);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelLoboPolar()
        {
            int w = 26, h = 20;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cPelaje = new Color(0.88f, 0.92f, 0.98f);
            Color cSombra = new Color(0.55f, 0.65f, 0.78f);
            Color cOjoAzul = new Color(0.2f, 0.75f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Hocico y cabeza hacia la izquierda
                    bool cabeza = (x >= 3 && x <= 11 && y >= 9 && y <= 16);
                    bool cuerpo = (x >= 9 && x <= 22 && y >= 6 && y <= 15);
                    bool patas = (y <= 5 && (x == 7 || x == 8 || x == 19 || x == 20));

                    if (cabeza || cuerpo || patas)
                    {
                        if (x == 6 && y == 13) t.SetPixel(x, y, cOjoAzul);
                        else if (x == 3 && y == 11) t.SetPixel(x, y, Color.black); // Nariz
                        else t.SetPixel(x, y, (y >= 11) ? cPelaje : cSombra);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelMagoGlacial()
        {
            int w = 22, h = 28;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cTunica = new Color(0.22f, 0.42f, 0.72f);
            Color cBordeHielo = new Color(0.75f, 0.92f, 1f);
            Color cCetro = new Color(0.35f, 0.88f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Cetro de hielo a la derecha
                    if (x >= 18 && x <= 20 && y >= 4 && y <= 24)
                    {
                        t.SetPixel(x, y, (y >= 21) ? cBordeHielo : cCetro);
                        continue;
                    }
                    // Túnica con capucha
                    int dx = Mathf.Abs(x - 10);
                    int maxDx = (y > 20) ? 5 : Mathf.RoundToInt(Mathf.Lerp(8f, 4f, (y - 4f) / 16f));
                    if (dx <= maxDx && y >= 4 && y <= 25)
                    {
                        if (y == 20 && (x == 9 || x == 12)) t.SetPixel(x, y, Color.cyan);
                        else t.SetPixel(x, y, (x <= 9) ? cBordeHielo : cTunica);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelMurcielagoHielo()
        {
            int w = 22, h = 18;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cAla = new Color(0.35f, 0.75f, 0.95f, 0.9f);
            Color cCuerpo = new Color(0.18f, 0.38f, 0.62f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    if (dx <= 3 && y >= 5 && y <= 13)
                    {
                        if (y == 10 && (x == 10 || x == 12)) t.SetPixel(x, y, Color.white);
                        else t.SetPixel(x, y, cCuerpo);
                    }
                    else if (y >= 7 && y <= 15 && dx <= 10)
                    {
                        t.SetPixel(x, y, cAla);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ========================================================
        // 3. FUEGO
        // ========================================================
        private static Sprite CrearPixelEngendroMagma()
        {
            int w = 28, h = 28;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cBasalto = new Color(0.18f, 0.14f, 0.16f);
            Color cLava = new Color(1f, 0.52f, 0.08f);
            Color cLavaViva = new Color(1f, 0.92f, 0.35f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 14);
                    int dy = Mathf.Abs(y - 14);
                    if (dx * dx + dy * dy <= 125)
                    {
                        // Grietas incandescentes
                        if (dx == 0 || (x + y == 28 && dy < 8)) t.SetPixel(x, y, cLavaViva);
                        else if (dx <= 3 || dy <= 2) t.SetPixel(x, y, cLava);
                        else t.SetPixel(x, y, cBasalto);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelSabuesoIgneo()
        {
            int w = 26, h = 20;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cCarneFuego = new Color(0.85f, 0.22f, 0.08f);
            Color cLlama = new Color(1f, 0.75f, 0.12f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool cabeza = (x >= 3 && x <= 10 && y >= 8 && y <= 15);
                    bool cuerpo = (x >= 9 && x <= 22 && y >= 6 && y <= 14);
                    bool espinasFuego = (y >= 15 && y <= 18 && x >= 11 && x <= 19 && (x + y) % 2 == 0);
                    bool patas = (y <= 5 && (x == 7 || x == 8 || x == 19 || x == 20));

                    if (espinasFuego) t.SetPixel(x, y, cLlama);
                    else if (cabeza || cuerpo || patas)
                    {
                        if (x == 6 && y == 12) t.SetPixel(x, y, cLlama);
                        else t.SetPixel(x, y, cCarneFuego);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelChamanFuego()
        {
            int w = 22, h = 28;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cTunica = new Color(0.55f, 0.15f, 0.08f);
            Color cMascara = new Color(0.92f, 0.85f, 0.65f);
            Color cFuego = new Color(1f, 0.65f, 0.12f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Orbe de fuego en mano derecha
                    if (x >= 17 && x <= 21 && y >= 16 && y <= 20) { t.SetPixel(x, y, cFuego); continue; }
                    // Máscara
                    if (y >= 18 && y <= 24 && x >= 8 && x <= 14)
                    {
                        if (y == 20 && (x == 10 || x == 12)) t.SetPixel(x, y, Color.red);
                        else t.SetPixel(x, y, cMascara);
                        continue;
                    }
                    // Túnica
                    int dx = Mathf.Abs(x - 11);
                    if (dx <= 6 && y >= 4 && y <= 18) t.SetPixel(x, y, cTunica);
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelFatuoVolcanico()
        {
            int w = 18, h = 22;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cLlama = new Color(1f, 0.35f, 0.05f, 0.9f);
            Color cCentro = new Color(1f, 0.92f, 0.45f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 9);
                    int dy = Mathf.Abs(y - 10);
                    if (dx * dx + dy * dy <= 50)
                    {
                        if (y == 11 && (x == 7 || x == 11)) t.SetPixel(x, y, Color.black); // Ojos
                        else t.SetPixel(x, y, (dx <= 2 && dy <= 2) ? cCentro : cLlama);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ========================================================
        // 4. PLANTA
        // ========================================================
        private static Sprite CrearPixelTreant()
        {
            int w = 28, h = 32;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cCorteza = new Color(0.42f, 0.28f, 0.15f);
            Color cMusgo = new Color(0.28f, 0.72f, 0.22f);
            Color cOjoVerde = new Color(0.65f, 1f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 14);
                    // Ramas arriba
                    if (y >= 25 && dx <= 11)
                    {
                        t.SetPixel(x, y, (x + y) % 3 == 0 ? cMusgo : cCorteza);
                        continue;
                    }
                    // Tronco
                    if (dx <= 9 && y >= 4 && y <= 25)
                    {
                        if (y == 19 && (x == 11 || x == 16)) t.SetPixel(x, y, cOjoVerde);
                        else if (y >= 12 && y <= 16 && dx <= 5) t.SetPixel(x, y, cMusgo); // Barba de musgo
                        else t.SetPixel(x, y, cCorteza);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelMandragora()
        {
            int w = 22, h = 24;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cRaiz = new Color(0.58f, 0.48f, 0.28f);
            Color cHoja = new Color(0.32f, 0.82f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Hojas arriba
                    if (y >= 17 && Mathf.Abs(x - 11) <= 7 && (x + y) % 2 == 0) { t.SetPixel(x, y, cHoja); continue; }
                    // Raíz chillona
                    int dx = Mathf.Abs(x - 11);
                    if (dx <= 6 && y >= 3 && y <= 17)
                    {
                        if (y == 12 && (x == 9 || x == 13)) t.SetPixel(x, y, Color.black);
                        else if (y == 8 && dx <= 2) t.SetPixel(x, y, Color.black); // Boca abierta
                        else t.SetPixel(x, y, cRaiz);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelFlorCarnivora()
        {
            int w = 24, h = 26;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cPetalo = new Color(0.92f, 0.25f, 0.35f);
            Color cTallo = new Color(0.25f, 0.65f, 0.20f);
            Color cDientes = new Color(1f, 0.95f, 0.85f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Tallo
                    if (y <= 12 && x >= 10 && x <= 13) { t.SetPixel(x, y, cTallo); continue; }
                    // Cabeza abierta estilo Venus atrapamoscas
                    int dx = Mathf.Abs(x - 12);
                    if (dx <= 8 && y >= 12 && y <= 23)
                    {
                        if (y == 17 && dx <= 6) t.SetPixel(x, y, Color.black); // Fauces
                        else if ((y == 18 || y == 16) && dx % 2 == 1) t.SetPixel(x, y, cDientes);
                        else t.SetPixel(x, y, cPetalo);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelHongoEsporas()
        {
            int w = 22, h = 24;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cSombrero = new Color(0.35f, 0.85f, 0.42f);
            Color cMota = new Color(0.85f, 1f, 0.65f);
            Color cTallo = new Color(0.88f, 0.88f, 0.82f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Tallo
                    if (y <= 10 && x >= 8 && x <= 13)
                    {
                        if (y == 6 && (x == 9 || x == 12)) t.SetPixel(x, y, Color.black); // Ojos
                        else t.SetPixel(x, y, cTallo);
                        continue;
                    }
                    // Sombrero abovedado
                    int dx = Mathf.Abs(x - 11);
                    if (y >= 10 && y <= 21 && dx <= 9)
                    {
                        if ((dx == 4 && y == 16) || (dx == 1 && y == 19)) t.SetPixel(x, y, cMota);
                        else t.SetPixel(x, y, cSombrero);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ========================================================
        // 5. ARENA
        // ========================================================
        private static Sprite CrearPixelSarcofagoAndante()
        {
            // 24x30 Sarcófago vertical con patas
            int w = 24, h = 30;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cOro = new Color(0.95f, 0.82f, 0.35f);
            Color cTurquesa = new Color(0.20f, 0.68f, 0.72f);
            Color cSombra = new Color(0.55f, 0.42f, 0.18f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Patas abajo
                    if (y <= 5 && ((x >= 6 && x <= 9) || (x >= 14 && x <= 17))) { t.SetPixel(x, y, cSombra); continue; }

                    int dx = Mathf.Abs(x - 12);
                    int maxDx = (y > 22) ? 7 : (y > 10 ? 8 : 6);
                    if (dx <= maxDx && y >= 6 && y <= 28)
                    {
                        // Máscara funeraria
                        if (y >= 20 && y <= 25 && dx <= 4)
                        {
                            if (y == 22 && (x == 10 || x == 13)) t.SetPixel(x, y, Color.black);
                            else t.SetPixel(x, y, cOro);
                        }
                        else if (y % 4 == 0) t.SetPixel(x, y, cTurquesa);
                        else t.SetPixel(x, y, (x <= 11) ? cOro : cSombra);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelAtaudRitual()
        {
            // 26x32 Sarcófago ceremonial fijo (Spawner)
            int w = 26, h = 32;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cArenisca = new Color(0.75f, 0.62f, 0.40f);
            Color cOro = new Color(0.95f, 0.82f, 0.35f);
            Color cGlifo = new Color(0.25f, 0.75f, 0.85f); // Glifos mágicos cian

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 13);
                    if (dx <= 10 && y >= 3 && y <= 29)
                    {
                        if (dx == 10 || y == 3 || y == 29) t.SetPixel(x, y, cOro);
                        else if ((dx == 0 && y >= 8 && y <= 22) || (y == 16 && dx <= 5)) t.SetPixel(x, y, cGlifo); // Cruz/Anj
                        else t.SetPixel(x, y, cArenisca);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Sprite CrearPixelRataDesierto()
        {
            int w = 20, h = 14;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cPelaje = new Color(0.68f, 0.52f, 0.35f);
            Color cOjoRojo = new Color(0.95f, 0.15f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Cola larga a la derecha
                    if (x >= 14 && y == 7 && x % 2 == 0) { t.SetPixel(x, y, new Color(0.85f, 0.65f, 0.65f)); continue; }
                    // Cuerpo agachado
                    if (x >= 3 && x <= 14 && y >= 3 && y <= 9)
                    {
                        if (x == 4 && y == 7) t.SetPixel(x, y, cOjoRojo);
                        else t.SetPixel(x, y, cPelaje);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 16f);
        }

        private static Sprite CrearPixelMomia()
        {
            int w = 22, h = 28;
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            Color cVendas = new Color(0.85f, 0.80f, 0.68f);
            Color cSombra = new Color(0.55f, 0.48f, 0.38f);
            Color cOjoRojo = new Color(1f, 0.25f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - 11);
                    if (dx <= 6 && y >= 3 && y <= 25)
                    {
                        if (y == 20 && (x == 9 || x == 13)) t.SetPixel(x, y, cOjoRojo);
                        else if (y % 3 == 0) t.SetPixel(x, y, cSombra); // Vendas cruzadas
                        else t.SetPixel(x, y, cVendas);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        // ========================================================
        // 👑 JEFES MONUMENTALES (48x48)
        // ========================================================
        private static Sprite GenerarSpriteJefe(TipoBioma bioma)
        {
            int s = 48;
            Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;

            Color c1, c2, cOjos;

            switch (bioma)
            {
                case TipoBioma.Piedra: // Coloso de Piedra
                    c1 = new Color(0.48f, 0.50f, 0.55f);
                    c2 = new Color(0.28f, 0.30f, 0.35f);
                    cOjos = new Color(1f, 0.85f, 0.2f);
                    break;
                case TipoBioma.Hielo: // Yeti de las Ventiscas
                    c1 = new Color(0.85f, 0.95f, 1f);
                    c2 = new Color(0.35f, 0.65f, 0.88f);
                    cOjos = new Color(0.1f, 0.95f, 1f);
                    break;
                case TipoBioma.Fuego: // Señor del Magma
                    c1 = new Color(0.22f, 0.16f, 0.18f);
                    c2 = new Color(1f, 0.45f, 0.08f);
                    cOjos = new Color(1f, 0.95f, 0.3f);
                    break;
                case TipoBioma.Planta: // Reina Espora
                    c1 = new Color(0.25f, 0.65f, 0.25f);
                    c2 = new Color(0.85f, 0.35f, 0.65f);
                    cOjos = new Color(0.7f, 1f, 0.2f);
                    break;
                default: // Faraón Eterno
                    c1 = new Color(0.95f, 0.82f, 0.35f);
                    c2 = new Color(0.22f, 0.65f, 0.72f);
                    cOjos = new Color(1f, 0.25f, 0.15f);
                    break;
            }

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - 24);
                    int dy = Mathf.Abs(y - 24);
                    if (dx * dx + dy * dy <= 380)
                    {
                        if (y >= 26 && y <= 29 && (x == 18 || x == 29)) t.SetPixel(x, y, cOjos);
                        else if (y % 5 == 0 || dx >= 16) t.SetPixel(x, y, c2);
                        else t.SetPixel(x, y, c1);
                    }
                    else t.SetPixel(x, y, Color.clear);
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
