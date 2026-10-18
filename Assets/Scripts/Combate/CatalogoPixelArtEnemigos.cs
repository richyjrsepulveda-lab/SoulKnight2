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
    /// Generador procedural de Sprites Pixel-Art de alta fidelidad con siluetas definidas,
    /// contorno oscuro (1px outline), sombreado volumétrico y ojos expresivos a 32 PPU.
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
                    if (rol == RolEnemigo.Tanque) return DibujarCaballeroHierro();
                    if (rol == RolEnemigo.MeleeRapido) return DibujarGargola();
                    if (rol == RolEnemigo.Rango) return DibujarBallestero();
                    return DibujarFantasma();

                case TipoBioma.Hielo:
                    if (rol == RolEnemigo.Tanque) return DibujarGolemHielo();
                    if (rol == RolEnemigo.MeleeRapido) return DibujarLoboPolar();
                    if (rol == RolEnemigo.Rango) return DibujarMagoGlacial();
                    return DibujarMurcielagoHielo();

                case TipoBioma.Fuego:
                    if (rol == RolEnemigo.Tanque) return DibujarEngendroMagma();
                    if (rol == RolEnemigo.MeleeRapido) return DibujarSabuesoIgneo();
                    if (rol == RolEnemigo.Rango) return DibujarChamanFuego();
                    return DibujarFatuoVolcanico();

                case TipoBioma.Planta:
                    if (rol == RolEnemigo.Tanque) return DibujarTreant();
                    if (rol == RolEnemigo.MeleeRapido) return DibujarMandragora();
                    if (rol == RolEnemigo.Rango) return DibujarFlorCarnivora();
                    return DibujarHongoEsporas();

                case TipoBioma.Arena:
                    if (rol == RolEnemigo.Tanque) return DibujarSarcofagoAndante();
                    if (rol == RolEnemigo.Spawner) return DibujarAtaudRitual();
                    if (rol == RolEnemigo.MeleeRapido) return DibujarRataDesierto();
                    return DibujarMomia();
            }

            return DibujarCaballeroHierro();
        }

        // ========================================================
        // 1. BIOMA PIEDRA
        // ========================================================
        private static Sprite DibujarCaballeroHierro()
        {
            int w = 28, h = 32;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cAcero = new Color(0.55f, 0.58f, 0.65f);
            Color cLuz = new Color(0.85f, 0.88f, 0.95f);
            Color cSombra = new Color(0.28f, 0.30f, 0.38f);
            Color cRojo = new Color(0.95f, 0.20f, 0.20f);
            Color cOro = new Color(1f, 0.80f, 0.20f);

            // Silueta / Escudo a la izquierda, Caballero al centro
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Pluma roja del yelmo
                    if (y >= 26 && y <= 30 && x >= 12 && x <= 16)
                    {
                        if (x == 12 || x == 16 || y == 30) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 14) ? cRojo : new Color(0.65f, 0.12f, 0.12f));
                        continue;
                    }

                    // Yelmo
                    if (y >= 19 && y <= 26 && x >= 10 && x <= 20)
                    {
                        if (x == 10 || x == 20 || y == 19 || y == 26) t.SetPixel(x, y, cOut);
                        else if (y == 22 && x >= 12 && x <= 18) t.SetPixel(x, y, (x == 14 || x == 17) ? cRojo : cOut); // Visor con ojos rojos
                        else t.SetPixel(x, y, (x <= 14) ? cLuz : (x <= 17 ? cAcero : cSombra));
                        continue;
                    }

                    // Escudo grande a la izquierda (x: 2 a 12, y: 7 a 20)
                    if (x >= 2 && x <= 12 && y >= 7 && y <= 20)
                    {
                        if (x == 2 || x == 12 || y == 7 || y == 20) t.SetPixel(x, y, cOut);
                        else if (x == 7 || y == 14) t.SetPixel(x, y, cOro); // Cruz dorada en el escudo
                        else t.SetPixel(x, y, (x <= 6) ? cLuz : cAcero);
                        continue;
                    }

                    // Pecho y brazo con espada a la derecha (x: 13 a 24, y: 8 a 19)
                    if (x >= 13 && x <= 24 && y >= 8 && y <= 19)
                    {
                        if (x == 24 || y == 8 || y == 19) t.SetPixel(x, y, cOut);
                        else if (x >= 20 && y >= 11 && y <= 17) t.SetPixel(x, y, (y == 11) ? cOro : cLuz); // Espada de acero
                        else t.SetPixel(x, y, (x <= 17) ? cAcero : cSombra);
                        continue;
                    }

                    // Piernas grebas
                    if (y >= 1 && y <= 7 && ((x >= 11 && x <= 15) || (x >= 17 && x <= 21)))
                    {
                        if (y == 1 || x == 11 || x == 15 || x == 17 || x == 21) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (y >= 5) ? cAcero : cSombra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarGargola()
        {
            int w = 28, h = 28;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cPiedra = new Color(0.44f, 0.46f, 0.52f);
            Color cLuz = new Color(0.65f, 0.68f, 0.75f);
            Color cSombra = new Color(0.24f, 0.25f, 0.30f);
            Color cOjos = new Color(1f, 0.90f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Alas puntiagudas
                    bool alaIzq = (x >= 1 && x <= 10 && y >= 12 && y <= 24 && (y - x >= 4));
                    bool alaDer = (x >= 17 && x <= 26 && y >= 12 && y <= 24 && (x + y >= 30));
                    if (alaIzq || alaDer)
                    {
                        if (x == 1 || x == 26 || y == 24 || y == 12) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (y > 18) ? cLuz : cPiedra);
                        continue;
                    }

                    // Cabeza con cuernos
                    if (y >= 14 && y <= 23 && x >= 10 && x <= 17)
                    {
                        if (x == 10 || x == 17 || y == 14 || y == 23) t.SetPixel(x, y, cOut);
                        else if (y == 17 && (x == 12 || x == 15)) t.SetPixel(x, y, cOjos);
                        else t.SetPixel(x, y, (x <= 13) ? cLuz : cPiedra);
                        continue;
                    }

                    // Torso y garras
                    if (y >= 3 && y <= 13 && x >= 9 && x <= 18)
                    {
                        if (x == 9 || x == 18 || y == 3) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 13) ? cPiedra : cSombra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarBallestero()
        {
            int w = 28, h = 30;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cCuero = new Color(0.48f, 0.32f, 0.18f);
            Color cCueroLuz = new Color(0.68f, 0.48f, 0.28f);
            Color cMetal = new Color(0.62f, 0.65f, 0.72f);
            Color cOjo = new Color(0.20f, 0.95f, 0.35f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Ballesta horizontal frontal apuntando (x: 2 a 16, y: 11 a 15)
                    if (x >= 2 && x <= 16 && y >= 11 && y <= 15)
                    {
                        if (x == 2 || x == 16 || y == 11 || y == 15) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 6) ? cMetal : cCueroLuz);
                        continue;
                    }

                    // Cabeza / Capucha
                    if (y >= 18 && y <= 27 && x >= 11 && x <= 20)
                    {
                        if (x == 11 || x == 20 || y == 18 || y == 27) t.SetPixel(x, y, cOut);
                        else if (y == 22 && x == 14) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (x <= 15) ? cCueroLuz : cCuero);
                        continue;
                    }

                    // Torso y Carcaj a la espalda
                    if (y >= 6 && y <= 18 && x >= 10 && x <= 22)
                    {
                        if (x == 10 || x == 22 || y == 6) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x > 18) ? cMetal : cCuero);
                        continue;
                    }

                    // Piernas
                    if (y >= 1 && y <= 5 && ((x >= 12 && x <= 15) || (x >= 17 && x <= 20)))
                    {
                        if (y == 1 || x == 12 || x == 15 || x == 17 || x == 20) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cCuero);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarFantasma()
        {
            int w = 26, h = 30;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.10f, 0.18f, 0.32f);
            Color cCuerpo = new Color(0.55f, 0.78f, 0.95f, 0.88f);
            Color cLuz = new Color(0.85f, 0.96f, 1f, 0.95f);
            Color cOjo = new Color(0.12f, 0.95f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Silueta espectral flotante ondulada
                    if (y >= 4 && y <= 26 && x >= 4 && x <= 21)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(12.5f, 18f));
                        if (dist <= 9.5f)
                        {
                            if (dist >= 8.5f || (y <= 6 && (x % 4 == 0))) t.SetPixel(x, y, cOut);
                            else if (y == 18 && (x == 9 || x == 15)) t.SetPixel(x, y, cOjo);
                            else t.SetPixel(x, y, (x <= 12) ? cLuz : cCuerpo);
                            continue;
                        }
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        // ========================================================
        // 2. BIOMA HIELO
        // ========================================================
        private static Sprite DibujarGolemHielo()
        {
            int w = 32, h = 34;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.06f, 0.12f, 0.25f);
            Color cCristal = new Color(0.38f, 0.68f, 0.88f);
            Color cBrillo = new Color(0.82f, 0.96f, 1f);
            Color cSombra = new Color(0.16f, 0.35f, 0.58f);
            Color cOjo = new Color(0.20f, 1f, 0.90f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Hombros gigantes con carámbanos (x: 3 a 28, y: 14 a 28)
                    if (x >= 3 && x <= 28 && y >= 14 && y <= 28)
                    {
                        if (x == 3 || x == 28 || y == 28 || (y == 14 && (x < 11 || x > 20))) t.SetPixel(x, y, cOut);
                        else if (y == 22 && (x == 13 || x == 18)) t.SetPixel(x, y, cOjo); // Ojos helados
                        else t.SetPixel(x, y, (x <= 15) ? cBrillo : (x <= 22 ? cCristal : cSombra));
                        continue;
                    }

                    // Piernas de hielo sólido
                    if (y >= 1 && y <= 13 && x >= 8 && x <= 23)
                    {
                        if (x == 8 || x == 23 || y == 1 || x == 15) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 15) ? cCristal : cSombra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarLoboPolar()
        {
            int w = 30, h = 26;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.12f, 0.20f);
            Color cPelo = new Color(0.88f, 0.94f, 1f);
            Color cSombra = new Color(0.55f, 0.68f, 0.82f);
            Color cOjo = new Color(0.15f, 0.75f, 1f);
            Color cNariz = new Color(0.12f, 0.18f, 0.28f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Orejas y hocico
                    if (x >= 2 && x <= 14 && y >= 10 && y <= 23)
                    {
                        if (x == 2 || y == 23 || (y == 10 && x < 8)) t.SetPixel(x, y, cOut);
                        else if (x == 3 && y == 13) t.SetPixel(x, y, cNariz);
                        else if (x == 8 && y == 16) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (y >= 16) ? cPelo : cSombra);
                        continue;
                    }

                    // Lomo y patas
                    if (x >= 12 && x <= 27 && y >= 2 && y <= 18)
                    {
                        if (x == 27 || y == 2 || y == 18) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (y >= 10) ? cPelo : cSombra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarMagoGlacial()
        {
            int w = 26, h = 32;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.06f, 0.10f, 0.22f);
            Color cTunica = new Color(0.24f, 0.48f, 0.75f);
            Color cTunicaLuz = new Color(0.55f, 0.78f, 0.95f);
            Color cBaculo = new Color(0.65f, 0.42f, 0.22f);
            Color cGema = new Color(0.35f, 0.95f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Báculo mágico a la derecha con gema de cristal
                    if (x >= 21 && x <= 24 && y >= 3 && y <= 30)
                    {
                        if (y >= 26) t.SetPixel(x, y, cGema);
                        else t.SetPixel(x, y, (x == 21 || x == 24) ? cOut : cBaculo);
                        continue;
                    }

                    // Capucha puntiaguda
                    if (y >= 20 && y <= 30 && x >= 8 && x <= 18)
                    {
                        if (x == 8 || x == 18 || y == 30) t.SetPixel(x, y, cOut);
                        else if (y == 23 && (x == 11 || x == 15)) t.SetPixel(x, y, cGema); // Ojos arcanos
                        else t.SetPixel(x, y, (x <= 13) ? cTunicaLuz : cTunica);
                        continue;
                    }

                    // Túnica ondeante
                    if (y >= 2 && y <= 20 && x >= 6 && x <= 20)
                    {
                        if (x == 6 || x == 20 || y == 2) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 12) ? cTunicaLuz : cTunica);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarMurcielagoHielo()
        {
            int w = 26, h = 24;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.06f, 0.10f, 0.20f);
            Color cAzul = new Color(0.45f, 0.75f, 0.92f);
            Color cLuz = new Color(0.80f, 0.95f, 1f);
            Color cOjo = new Color(1f, 0.25f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Alas puntiagudas
                    bool ala = (y >= 10 && y <= 20 && ((x >= 1 && x <= 9) || (x >= 16 && x <= 24)));
                    if (ala)
                    {
                        if (x == 1 || x == 24 || y == 20) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (y > 15) ? cLuz : cAzul);
                        continue;
                    }

                    // Cuerpo central y orejas
                    if (x >= 9 && x <= 16 && y >= 4 && y <= 21)
                    {
                        if (x == 9 || x == 16 || y == 4 || y == 21) t.SetPixel(x, y, cOut);
                        else if (y == 13 && (x == 11 || x == 14)) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, cAzul);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        // ========================================================
        // 3. BIOMA FUEGO
        // ========================================================
        private static Sprite DibujarEngendroMagma()
        {
            int w = 30, h = 32;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.05f, 0.05f);
            Color cObsidiana = new Color(0.22f, 0.18f, 0.24f);
            Color cLavaIntensa = new Color(1f, 0.85f, 0.20f);
            Color cFuego = new Color(0.95f, 0.35f, 0.08f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15f, 15f));
                    if (dist <= 12f)
                    {
                        if (dist >= 11f) t.SetPixel(x, y, cOut);
                        else if (dist <= 5.5f) t.SetPixel(x, y, (dist <= 2.5f) ? cLavaIntensa : cFuego); // Núcleo de lava brillante
                        else if ((x + y) % 5 == 0) t.SetPixel(x, y, cFuego); // Grietas de magma
                        else t.SetPixel(x, y, cObsidiana);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarSabuesoIgneo()
        {
            int w = 30, h = 26;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.05f, 0.05f);
            Color cCarbon = new Color(0.25f, 0.18f, 0.18f);
            Color cFuego = new Color(0.95f, 0.40f, 0.08f);
            Color cOjo = new Color(1f, 0.90f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 2 && x <= 14 && y >= 8 && y <= 22)
                    {
                        if (x == 2 || y == 22 || (y == 8 && x < 8)) t.SetPixel(x, y, cOut);
                        else if (x == 8 && y == 15) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (y >= 16) ? cFuego : cCarbon);
                        continue;
                    }

                    if (x >= 12 && x <= 27 && y >= 2 && y <= 18)
                    {
                        if (x == 27 || y == 2 || y == 18) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, ((x + y) % 4 == 0) ? cFuego : cCarbon);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarChamanFuego()
        {
            int w = 26, h = 32;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.10f, 0.06f, 0.08f);
            Color cManto = new Color(0.65f, 0.22f, 0.12f);
            Color cLuz = new Color(0.95f, 0.45f, 0.15f);
            Color cMascara = new Color(0.92f, 0.88f, 0.80f);
            Color cFuego = new Color(1f, 0.85f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Báculo con llama
                    if (x >= 21 && x <= 24 && y >= 3 && y <= 30)
                    {
                        if (y >= 25) t.SetPixel(x, y, cFuego);
                        else t.SetPixel(x, y, (x == 21 || x == 24) ? cOut : new Color(0.4f, 0.2f, 0.1f));
                        continue;
                    }

                    // Máscara tribal
                    if (y >= 20 && y <= 28 && x >= 8 && x <= 18)
                    {
                        if (x == 8 || x == 18 || y == 28) t.SetPixel(x, y, cOut);
                        else if (y == 23 && (x == 11 || x == 15)) t.SetPixel(x, y, cFuego); // Ojos ígneos
                        else t.SetPixel(x, y, cMascara);
                        continue;
                    }

                    // Manto ardiente
                    if (y >= 2 && y <= 20 && x >= 6 && x <= 19)
                    {
                        if (x == 6 || x == 19 || y == 2) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x <= 12) ? cLuz : cManto);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarFatuoVolcanico()
        {
            int w = 24, h = 26;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.05f, 0.05f);
            Color cLlamaExt = new Color(0.95f, 0.28f, 0.08f);
            Color cLlamaMed = new Color(1f, 0.65f, 0.12f);
            Color cNucleo = new Color(1f, 0.98f, 0.65f);

            Vector2 c = new Vector2(11.5f, 11.5f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= 9.5f)
                    {
                        if (d >= 8.5f) t.SetPixel(x, y, cOut);
                        else if (d <= 3.5f) t.SetPixel(x, y, cNucleo);
                        else if (d <= 6.5f) t.SetPixel(x, y, cLlamaMed);
                        else t.SetPixel(x, y, cLlamaExt);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        // ========================================================
        // 4. BIOMA PLANTA
        // ========================================================
        private static Sprite DibujarTreant()
        {
            int w = 32, h = 34;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.12f, 0.06f);
            Color cCorteza = new Color(0.38f, 0.26f, 0.16f);
            Color cCortezaLuz = new Color(0.55f, 0.38f, 0.24f);
            Color cHojas = new Color(0.24f, 0.68f, 0.28f);
            Color cOjo = new Color(0.40f, 1f, 0.35f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Corona de hojas y ramas arriba
                    if (y >= 26 && x >= 4 && x <= 27)
                    {
                        if (y == 33 || x == 4 || x == 27) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, (x % 3 == 0) ? new Color(0.45f, 0.85f, 0.35f) : cHojas);
                        continue;
                    }

                    // Tronco ancestral con ojos sabios
                    if (y >= 8 && y <= 26 && x >= 7 && x <= 24)
                    {
                        if (x == 7 || x == 24) t.SetPixel(x, y, cOut);
                        else if (y == 20 && (x == 12 || x == 19)) t.SetPixel(x, y, cOjo); // Ojos verdes benevolentes
                        else t.SetPixel(x, y, (x <= 15) ? cCortezaLuz : cCorteza);
                        continue;
                    }

                    // Raíces gruesas
                    if (y >= 1 && y <= 8 && x >= 5 && x <= 26)
                    {
                        if (y == 1 || x == 5 || x == 26) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cCorteza);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarMandragora()
        {
            int w = 24, h = 26;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.12f, 0.06f);
            Color cRaiz = new Color(0.68f, 0.52f, 0.32f);
            Color cRaizLuz = new Color(0.85f, 0.70f, 0.45f);
            Color cHoja = new Color(0.28f, 0.75f, 0.32f);
            Color cOjo = new Color(0.12f, 0.12f, 0.12f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Hojas superiores
                    if (y >= 18 && y <= 25 && x >= 8 && x <= 16)
                    {
                        if (y == 25 || x == 8 || x == 16) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cHoja);
                        continue;
                    }

                    // Raíz bulbosa
                    if (y >= 3 && y <= 18 && x >= 5 && x <= 19)
                    {
                        if (x == 5 || x == 19 || y == 3) t.SetPixel(x, y, cOut);
                        else if (y == 13 && (x == 9 || x == 15)) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (x <= 12) ? cRaizLuz : cRaiz);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarFlorCarnivora()
        {
            int w = 28, h = 30;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.12f, 0.06f);
            Color cPetalo = new Color(0.85f, 0.18f, 0.35f);
            Color cPetaloLuz = new Color(1f, 0.35f, 0.55f);
            Color cDientes = new Color(0.95f, 0.95f, 0.95f);
            Color cTallo = new Color(0.22f, 0.58f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Fauces carnívoras abiertas (x: 4 a 24, y: 12 a 28)
                    if (x >= 4 && x <= 24 && y >= 12 && y <= 28)
                    {
                        if (x == 4 || x == 24 || y == 28 || y == 12) t.SetPixel(x, y, cOut);
                        else if (y >= 18 && y <= 21 && (x % 3 == 0)) t.SetPixel(x, y, cDientes); // Dientes afilados
                        else t.SetPixel(x, y, (y >= 20) ? cPetaloLuz : cPetalo);
                        continue;
                    }

                    // Tallo con hojas espinosas
                    if (y >= 1 && y <= 12 && x >= 11 && x <= 17)
                    {
                        if (x == 11 || x == 17 || y == 1) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cTallo);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarHongoEsporas()
        {
            int w = 26, h = 28;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cSombrero = new Color(0.58f, 0.22f, 0.75f);
            Color cMota = new Color(0.35f, 0.95f, 0.85f);
            Color cTallo = new Color(0.85f, 0.82f, 0.75f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Sombrero del hongo
                    if (y >= 12 && y <= 26 && x >= 3 && x <= 23)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(13f, 15f));
                        if (dist <= 10f)
                        {
                            if (dist >= 9f) t.SetPixel(x, y, cOut);
                            else if ((x == 8 && y == 21) || (x == 17 && y == 20) || (x == 13 && y == 16)) t.SetPixel(x, y, cMota); // Esporas luminiscentes
                            else t.SetPixel(x, y, cSombrero);
                            continue;
                        }
                    }

                    // Pie del hongo
                    if (y >= 1 && y <= 12 && x >= 9 && x <= 17)
                    {
                        if (x == 9 || x == 17 || y == 1) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cTallo);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        // ========================================================
        // 5. BIOMA ARENA
        // ========================================================
        private static Sprite DibujarSarcofagoAndante()
        {
            int w = 28, h = 34;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.10f, 0.05f);
            Color cOro = new Color(0.95f, 0.78f, 0.22f);
            Color cOroSombra = new Color(0.65f, 0.50f, 0.12f);
            Color cLapis = new Color(0.15f, 0.35f, 0.75f);
            Color cOjo = new Color(1f, 0.25f, 0.25f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Silueta de sarcófago imperial
                    if (y >= 3 && y <= 31 && x >= 5 && x <= 22)
                    {
                        if (x == 5 || x == 22 || y == 3 || y == 31) t.SetPixel(x, y, cOut);
                        else if (y >= 21 && y <= 27 && (x + y) % 3 == 0) t.SetPixel(x, y, cLapis); // Franjas de lapislázuli en el nemes
                        else if (y == 23 && (x == 10 || x == 17)) t.SetPixel(x, y, cOjo); // Ojos de maldición
                        else t.SetPixel(x, y, (x <= 13) ? cOro : cOroSombra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarAtaudRitual()
        {
            int w = 32, h = 36;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.10f, 0.08f, 0.05f);
            Color cPiedra = new Color(0.60f, 0.48f, 0.32f);
            Color cCadena = new Color(0.75f, 0.78f, 0.85f);
            Color cAura = new Color(0.35f, 0.95f, 0.45f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 6 && x <= 25 && y >= 3 && y <= 33)
                    {
                        if (x == 6 || x == 25 || y == 3 || y == 33) t.SetPixel(x, y, cOut);
                        else if (x == y || x + y == 31) t.SetPixel(x, y, cCadena); // Cadenas cruzadas que atan el ataúd
                        else if (y >= 14 && y <= 18 && x >= 12 && x <= 19) t.SetPixel(x, y, cAura); // Sello arcano de invocación
                        else t.SetPixel(x, y, cPiedra);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarRataDesierto()
        {
            int w = 26, h = 20;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.10f, 0.05f);
            Color cArena = new Color(0.78f, 0.62f, 0.40f);
            Color cLuz = new Color(0.92f, 0.78f, 0.55f);
            Color cOjo = new Color(1f, 0.15f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Cola a la derecha
                    if (x >= 18 && y >= 10 && (y - x == -10 || y - x == -9)) { t.SetPixel(x, y, cOut); continue; }

                    // Cuerpo y cabeza afilada
                    if (x >= 2 && x <= 18 && y >= 3 && y <= 15)
                    {
                        if (x == 2 || x == 18 || y == 3 || y == 15) t.SetPixel(x, y, cOut);
                        else if (x == 6 && y == 11) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (y >= 9) ? cLuz : cArena);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        private static Sprite DibujarMomia()
        {
            int w = 26, h = 30;
            var t = CrearTextura(w, h);
            Color cOut = new Color(0.12f, 0.10f, 0.05f);
            Color cVendaje = new Color(0.85f, 0.80f, 0.68f);
            Color cVendajeSombra = new Color(0.55f, 0.50f, 0.40f);
            Color cOjo = new Color(1f, 0.90f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x >= 7 && x <= 19 && y >= 2 && y <= 27)
                    {
                        if (x == 7 || x == 19 || y == 2 || y == 27) t.SetPixel(x, y, cOut);
                        else if (y == 21 && (x == 10 || x == 16)) t.SetPixel(x, y, cOjo);
                        else if ((x + y) % 4 == 0) t.SetPixel(x, y, cVendajeSombra); // Rayas de los vendajes
                        else t.SetPixel(x, y, cVendaje);
                        continue;
                    }
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
        }

        // ========================================================
        // LOS 5 JEFES MONUMENTALES (64x64 at 32 PPU = 2.0 unidades)
        // ========================================================
        private static Sprite GenerarSpriteJefe(TipoBioma bioma)
        {
            int s = 64;
            var t = CrearTextura(s, s);

            switch (bioma)
            {
                case TipoBioma.Piedra: // El Coloso de Piedra
                    DibujarColosoPiedra(t);
                    break;
                case TipoBioma.Hielo: // Yeti de las Ventiscas
                    DibujarYetiVentiscas(t);
                    break;
                case TipoBioma.Fuego: // Señor del Magma
                    DibujarSenorMagma(t);
                    break;
                case TipoBioma.Planta: // La Reina Espora
                    DibujarReinaEspora(t);
                    break;
                case TipoBioma.Arena: // Faraón Eterno
                    DibujarFaraonEterno(t);
                    break;
            }

            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.22f), 32f);
        }

        private static void DibujarColosoPiedra(Texture2D t)
        {
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cPiedra = new Color(0.42f, 0.44f, 0.50f);
            Color cLuz = new Color(0.68f, 0.70f, 0.78f);
            Color cSombra = new Color(0.22f, 0.24f, 0.28f);
            Color cRuna = new Color(1f, 0.75f, 0.25f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Silueta monumental
                    if (x >= 12 && x <= 52 && y >= 6 && y <= 56)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 34f));
                        if (dist <= 23f)
                        {
                            if (dist >= 21.5f || x == 12 || x == 52 || y == 6) t.SetPixel(x, y, cOut);
                            else if (y >= 44 && y <= 54 && x >= 24 && x <= 40)
                            {
                                // Yelmo de titan con ojo rúnico central
                                if (y == 48 && x >= 29 && x <= 35) t.SetPixel(x, y, cRuna);
                                else t.SetPixel(x, y, (x <= 32) ? cLuz : cPiedra);
                            }
                            else if (y >= 26 && y <= 38 && x >= 26 && x <= 38 && (x == 32 || y == 32))
                            {
                                t.SetPixel(x, y, cRuna); // Cruz rúnica de poder en el pecho
                            }
                            else
                            {
                                t.SetPixel(x, y, (x <= 32) ? cLuz : (x <= 42 ? cPiedra : cSombra));
                            }
                        }
                    }
                }
            }
        }

        private static void DibujarYetiVentiscas(Texture2D t)
        {
            Color cOut = new Color(0.06f, 0.10f, 0.22f);
            Color cPelaje = new Color(0.85f, 0.94f, 1f);
            Color cSombra = new Color(0.45f, 0.62f, 0.78f);
            Color cCuerno = new Color(0.20f, 0.45f, 0.70f);
            Color cOjo = new Color(0.20f, 1f, 0.90f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Cuernos de hielo colosales (curvados a los lados)
                    bool cuernoIzq = (x >= 6 && x <= 22 && y >= 44 && y <= 58 && (x + y <= 72));
                    bool cuernoDer = (x >= 42 && x <= 58 && y >= 44 && y <= 58 && (y - x <= 8));
                    if (cuernoIzq || cuernoDer)
                    {
                        if (x == 6 || x == 58 || y == 58) t.SetPixel(x, y, cOut);
                        else t.SetPixel(x, y, cCuerno);
                        continue;
                    }

                    // Cuerpo y cabeza masiva de la bestia polar
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 30f));
                    if (dist <= 23f)
                    {
                        if (dist >= 21.5f || y == 6) t.SetPixel(x, y, cOut);
                        else if (y >= 38 && y <= 48 && (x == 26 || x == 38)) t.SetPixel(x, y, cOjo); // Ojos celestes furiosos
                        else if (y >= 32 && y <= 35 && x >= 28 && x <= 36) t.SetPixel(x, y, Color.white); // Colmillos
                        else t.SetPixel(x, y, (x <= 32) ? cPelaje : cSombra);
                    }
                }
            }
        }

        private static void DibujarSenorMagma(Texture2D t)
        {
            Color cOut = new Color(0.12f, 0.05f, 0.05f);
            Color cObsidiana = new Color(0.20f, 0.16f, 0.22f);
            Color cLavaIntensa = new Color(1f, 0.90f, 0.25f);
            Color cFuego = new Color(1f, 0.42f, 0.08f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Cuernos de fuego demoníacos
                    bool cuerno = (y >= 46 && y <= 60 && ((x >= 12 && x <= 22 && x + y >= 64) || (x >= 42 && x <= 52 && y - x >= 6)));
                    if (cuerno)
                    {
                        t.SetPixel(x, y, cFuego);
                        continue;
                    }

                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 30f));
                    if (dist <= 23f)
                    {
                        if (dist >= 21.5f || y == 6) t.SetPixel(x, y, cOut);
                        else if (dist <= 8f) t.SetPixel(x, y, (dist <= 4f) ? cLavaIntensa : cFuego); // Horno ardiente en el pecho
                        else if (y >= 40 && y <= 44 && (x == 27 || x == 37)) t.SetPixel(x, y, cLavaIntensa); // Ojos de fuego
                        else t.SetPixel(x, y, ((x + y) % 6 == 0) ? cFuego : cObsidiana);
                    }
                }
            }
        }

        private static void DibujarReinaEspora(Texture2D t)
        {
            Color cOut = new Color(0.08f, 0.08f, 0.12f);
            Color cSombrero = new Color(0.62f, 0.22f, 0.78f);
            Color cEspora = new Color(0.35f, 1f, 0.75f);
            Color cVestido = new Color(0.24f, 0.58f, 0.32f);
            Color cRostro = new Color(0.92f, 0.85f, 0.78f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Sombrero fúngico real coronando a la Reina
                    if (y >= 42 && y <= 60 && x >= 10 && x <= 54)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 44f));
                        if (d <= 21f)
                        {
                            if (d >= 19.5f) t.SetPixel(x, y, cOut);
                            else if ((x == 20 && y == 52) || (x == 44 && y == 51) || (x == 32 && y == 55)) t.SetPixel(x, y, cEspora);
                            else t.SetPixel(x, y, cSombrero);
                            continue;
                        }
                    }

                    // Rostro de dríade y túnica floral
                    if (x >= 18 && x <= 46 && y >= 6 && y <= 44)
                    {
                        if (x == 18 || x == 46 || y == 6) t.SetPixel(x, y, cOut);
                        else if (y >= 34 && y <= 42)
                        {
                            if (y == 38 && (x == 28 || x == 36)) t.SetPixel(x, y, cEspora); // Ojos esmeralda
                            else t.SetPixel(x, y, cRostro);
                        }
                        else
                        {
                            t.SetPixel(x, y, ((x * 3 + y) % 5 == 0) ? cEspora : cVestido);
                        }
                    }
                }
            }
        }

        private static void DibujarFaraonEterno(Texture2D t)
        {
            Color cOut = new Color(0.12f, 0.08f, 0.05f);
            Color cOro = new Color(0.98f, 0.80f, 0.20f);
            Color cOroBrillo = new Color(1f, 0.95f, 0.55f);
            Color cLapis = new Color(0.15f, 0.35f, 0.80f);
            Color cOjo = new Color(1f, 0.20f, 0.20f);
            Color cMascara = new Color(0.88f, 0.72f, 0.25f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Nemes real imperial (tocado de rayas doradas y azules)
                    if (y >= 36 && y <= 60 && x >= 12 && x <= 52)
                    {
                        if (x == 12 || x == 52 || y == 60) t.SetPixel(x, y, cOut);
                        else if (x == 32 && y >= 52) t.SetPixel(x, y, Color.red); // Cobra Uraeus real
                        else t.SetPixel(x, y, ((x + y / 2) % 6 < 3) ? cOro : cLapis);
                        continue;
                    }

                    // Máscara funeraria de oro puro con ojos carmesí
                    if (x >= 22 && x <= 42 && y >= 24 && y <= 40)
                    {
                        if (x == 22 || x == 42) t.SetPixel(x, y, cOut);
                        else if (y == 32 && (x == 28 || x == 36)) t.SetPixel(x, y, cOjo);
                        else t.SetPixel(x, y, (x <= 32) ? cOroBrillo : cMascara);
                        continue;
                    }

                    // Torso y collar usekh sagrado
                    if (x >= 16 && x <= 48 && y >= 6 && y <= 26)
                    {
                        if (x == 16 || x == 48 || y == 6) t.SetPixel(x, y, cOut);
                        else if (y >= 20 && y <= 24) t.SetPixel(x, y, (x % 4 == 0) ? cLapis : cOro);
                        else t.SetPixel(x, y, (x <= 32) ? cOro : cMascara);
                    }
                }
            }
        }

        private static Texture2D CrearTextura(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, Color.clear);
            return t;
        }
    }
}
