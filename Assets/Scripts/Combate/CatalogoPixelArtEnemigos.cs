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
    /// CatÃ¡logo central de Sprites Pixel-Art de alta fidelidad:
    /// - DiseÃ±os artesanales pixel a pixel con siluetas orgÃ¡nicas (no bloques matemÃ¡ticos).
    /// - Contorno oscuro de 1px, paletas con sombreado de 3 niveles y ojos expresivos a 32 PPU.
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

        private static Sprite CrearSpriteDeMapa(string[] mapa, Dictionary<char, Color> paleta, float ppu = 32f, float pivotY = 0.2f)
        {
            int origH = mapa.Length;
            int origW = mapa[0].Length;

            // Margen de 1px a cada lado para el borde blanco brillante exterior
            int w = origW + 2;
            int h = origH + 2;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixeles = new Color[w * h];
            for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.clear;

            for (int y = 0; y < origH; y++)
            {
                int texY = (origH - 1 - y) + 1;
                string fila = mapa[y];
                for (int x = 0; x < origW; x++)
                {
                    char c = x < fila.Length ? fila[x] : ' ';
                    if (paleta.TryGetValue(c, out Color col))
                    {
                        pixeles[texY * w + (x + 1)] = col;
                    }
                }
            }

            // Detección de silueta exterior para aplicar el borde blanco de alto contraste sobre fondo oscuro
            int[] dx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy8 = { -1, -1, -1, 0, 0, 1, 1, 1 };
            Color[] pixelesConBorde = (Color[])pixeles.Clone();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixeles[y * w + x].a < 0.1f)
                    {
                        bool tieneVecinoOpaco = false;
                        for (int k = 0; k < 8; k++)
                        {
                            int nx = x + dx8[k];
                            int ny = y + dy8[k];
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h && pixeles[ny * w + nx].a > 0.5f)
                            {
                                tieneVecinoOpaco = true;
                                break;
                            }
                        }
                        if (tieneVecinoOpaco)
                        {
                            pixelesConBorde[y * w + x] = new Color(0.96f, 0.15f, 0.15f, 1f); // Trazo rojo de alto contraste y peligro
                        }
                    }
                }
            }

            tex.SetPixels(pixelesConBorde);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, pivotY), ppu);
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

        private static Sprite GenerarSpriteJefe(TipoBioma bioma)
        {
            switch (bioma)
            {
                case TipoBioma.Piedra: return DibujarColosoPiedra();
                case TipoBioma.Hielo: return DibujarYetiVentiscas();
                case TipoBioma.Fuego: return DibujarSenorMagma();
                case TipoBioma.Planta: return DibujarReinaEspora();
                case TipoBioma.Arena: return DibujarFaraonEterno();
            }
            return DibujarColosoPiedra();
        }

        // ========================================================
        // 1. BIOMA PIEDRA (Mazmorra Medieval)
        // ========================================================

        // 1.1 Caballero de Hierro (Tanque) - Yelmo con cuernos, escudo redondo, maza
        private static Sprite DibujarCaballeroHierro()
        {
            string[] mapa = new string[]
            {
                "         ....           ",
                "        .++++.          ",
                "       ..++++..         ",
                "      ..XXXXXX..        ",
                "     .XXOOOOOOXX.       ",
                "    .#.XOOOOOOX.#.      ",
                "   .##.X@@..@@X.##.     ",
                "   .#$.X@.!!.@@X.#$.    ",
                "   .#$.X@.!!.@@X.#$.    ",
                "    .#.XOOOOOOX.#.      ",
                "     ..XXXXXXXX..       ",
                "  ....##XOOOOX##....    ",
                " .####.#XOOOOX#.####.   ",
                ".#$$$#.#XOOOOX#.####.   ",
                ".#$$$#.XXXXXXXX.####.   ",
                ".#$$$#..XOOOOX..#$$#.   ",
                ".#$$$# .XOOOOX. .##.    ",
                " .###. .XOOOOX.  ..     ",
                "  ...  .XXXXXX.         ",
                "       .XX..XX.         ",
                "       .XX..XX.         ",
                "      .XXX..XXX.        ",
                "      ....  ....        "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.08f, 0.12f) }, // Contorno negro azulado
                { 'X', new Color(0.24f, 0.28f, 0.36f) }, // Acero sombra
                { 'O', new Color(0.46f, 0.52f, 0.62f) }, // Acero medio
                { '@', new Color(0.72f, 0.78f, 0.88f) }, // Acero brillo
                { '+', new Color(0.85f, 0.18f, 0.18f) }, // Pluma yelmo carmesÃ­
                { '!', new Color(1f, 0.15f, 0.15f) },    // Ojo visor rojo brillante
                { '#', new Color(0.65f, 0.50f, 0.22f) }, // Bronce / borde escudo
                { '$', new Color(0.92f, 0.78f, 0.36f) }  // Oro brillo
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 1.2 GÃ¡rgola de Piedra (Melee RÃ¡pido) - Alas demonÃ­acas de piedra, cuernos, ojos amarillos
        private static Sprite DibujarGargola()
        {
            string[] mapa = new string[]
            {
                " ..              ..     ",
                ".##.            .##.    ",
                ".###..        ..###.    ",
                " .####.  ....  .####.   ",
                "  .###..XOOOOX..###.    ",
                " ..##.XXO@@@@OXX.##..   ",
                ".---.XOO@.!..@OOX.---.  ",
                ".---.XOO@.!..@OOX.---.  ",
                " .--.XOOOOOOOOOOX.--.   ",
                "  .. .XXXXXXXXXX. ..    ",
                "    .XXXXXXXXXXXX.      ",
                "   .XXOOOOOOOOOOXX.     ",
                "  .XXOOOOOOOOOOOOXX.    ",
                "  .XXOOOOOOOOOOOOXX.    ",
                "   .XXXXXXXXXXXXXX.     ",
                "    .XX.XXXXXX.XX.      ",
                "    .XX. .XX. .XX.      ",
                "   .XXX. .XX. .XXX.     ",
                "   ....  ....  ....     "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.10f, 0.14f) },
                { 'X', new Color(0.25f, 0.28f, 0.34f) },
                { 'O', new Color(0.45f, 0.50f, 0.58f) },
                { '@', new Color(0.68f, 0.74f, 0.82f) },
                { '!', new Color(1f, 0.90f, 0.15f) },    // Ojos amarillos brillantes
                { '#', new Color(0.32f, 0.35f, 0.42f) }, // Cuernos de piedra
                { '-', new Color(0.16f, 0.18f, 0.22f) }  // Membrana oscura de alas
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 1.3 Ballestero Esqueleto (Rango) - Capucha rota, crÃ¡neo blanco, ballesta
        private static Sprite DibujarBallestero()
        {
            string[] mapa = new string[]
            {
                "        ......          ",
                "       .XXXXXX.         ",
                "      .X@@@@@@X.        ",
                "      .X@....@X.        ",
                "     .X@.OOOO.@X.       ",
                "     .X@.O!!O.@X.       ",
                "     .X@.OOOO.@X.       ",
                "      .X@.OO.@X.        ",
                "    ...X@....@X...      ",
                "   .###.XXXXXX.###.     ",
                "  .##$#.XOOOOX.#$##.    ",
                " ..###..XOOOOX..###..   ",
                ".++++++.XXXXXX.++++++.  ",
                "  ....  .OOOO.  ....    ",
                "        .X..X.          ",
                "        .O..O.          ",
                "       .XX..XX.         ",
                "       ...  ...         "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.08f, 0.10f) },
                { 'X', new Color(0.20f, 0.18f, 0.24f) }, // Capucha oscura sombra
                { '@', new Color(0.38f, 0.34f, 0.44f) }, // Capucha media
                { 'O', new Color(0.82f, 0.80f, 0.74f) }, // Hueso crÃ¡neo
                { '!', new Color(1f, 0.65f, 0.10f) },    // Ojos Ã¡mbar
                { '#', new Color(0.48f, 0.30f, 0.18f) }, // Madera ballesta
                { '$', new Color(0.70f, 0.48f, 0.28f) },
                { '+', new Color(0.75f, 0.80f, 0.88f) }  // Arco de acero / cuerda
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 1.4 Fantasma RÃºnico (Especial) - Espectro flotante con manto andrajoso y nÃºcleo cian
        private static Sprite DibujarFantasma()
        {
            string[] mapa = new string[]
            {
                "        ......          ",
                "       .XXXXXX.         ",
                "      .XXOOOOXX.        ",
                "     .XXO@@@@OXX.       ",
                "     .XO@....@OX.       ",
                "  #  .XO@.!!.@@X.  #    ",
                " #$# .XO@.!!.@@X. #$#   ",
                "  #  .XO@....@OX.  #    ",
                "     .XXOOOOOOXX.       ",
                "      .XXXXXXXX.        ",
                "     .XXOOOOOOXX.       ",
                "    .XXOOOOOOOOXX.      ",
                "    .XXOOOOOOOOXX.      ",
                "    .XXO.OOOO.OXX.      ",
                "     .XX.XXXX.XX.       ",
                "      .X. .. .X.        ",
                "       ..    ..         "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.05f, 0.10f, 0.16f) },
                { 'X', new Color(0.12f, 0.26f, 0.38f) },
                { 'O', new Color(0.22f, 0.50f, 0.70f) },
                { '@', new Color(0.48f, 0.82f, 0.96f) },
                { '!', new Color(0.85f, 1f, 1f) },       // Ojos de alma fulgurantes
                { '#', new Color(0.28f, 0.32f, 0.38f) }, // Piedras rÃºnicas flotantes
                { '$', new Color(0.10f, 0.95f, 0.90f) }  // Glifo mÃ¡gico en la piedra
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.22f);
        }

        // 1.5 Coloso de Piedra (Jefe - 36x40) - TitÃ¡n monolÃ­tico de granito con runas y ojo ciclÃ³peo
        private static Sprite DibujarColosoPiedra()
        {
            string[] mapa = new string[]
            {
                "            ............            ",
                "           .############.           ",
                "          .##XXXXXXXXXX##.          ",
                "         .##XXOOOOOOOOXX##.         ",
                "        .##XXO@@@@@@@@OXX##.        ",
                "        .##XO@...$$...@OX##.        ",
                "        .##XO@..$$$$..@OX##.        ",
                "        .##XO@..$!!$..@OX##.        ",
                "        .##XO@..$$$$..@OX##.        ",
                "        .##XO@...$$...@OX##.        ",
                "        .##XXO@@@@@@@@OXX##.        ",
                "         .##XXXXXXXXXXXX##.         ",
                "       ...################...       ",
                "     ..XXX.##############.XXX..     ",
                "    .XXXXXX.XXXXXXXXXXXX.XXXXXX.    ",
                "   .XXXXOOOO.XXOOOOOOXX.OOOOXXXX.   ",
                "  .XXXXO@@@@O.XOOOOOOX.O@@@@OXXXX.  ",
                "  .XXXO@$$$$@O.XOOOOX.O@$$$$@OXXX.  ",
                "  .XXXO@$$$$@O.XXXXXX.O@$$$$@OXXX.  ",
                "  .XXXXO@@@@O.XXOOOOXX.O@@@@OXXXX.  ",
                "   .XXXXOOOO.XXOOOOOOXX.OOOOXXXX.   ",
                "    .XXXXXX.XXOOOOOOOOXX.XXXXXX.    ",
                "     ..XX..XXOOOOOOOOOOXX..XX..     ",
                "         .XXOOOOOOOOOOOOXX.         ",
                "        .XXOOOOO$$$$OOOOOXX.        ",
                "        .XXOOOO$$!!$$OOOOXX.        ",
                "        .XXOOOOO$$$$OOOOOXX.        ",
                "        .XXXXXXXXXXXXXXXXXX.        ",
                "        .XXXXXX.    .XXXXXX.        ",
                "        .XXXXXX.    .XXXXXX.        ",
                "        .XXXXXX.    .XXXXXX.        ",
                "       .XXXXXXXX.  .XXXXXXXX.       ",
                "       ..........  ..........       "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.08f, 0.12f) },
                { 'X', new Color(0.22f, 0.26f, 0.32f) }, // Granito sombra
                { 'O', new Color(0.40f, 0.46f, 0.55f) }, // Granito medio
                { '@', new Color(0.62f, 0.70f, 0.80f) }, // Granito claro
                { '#', new Color(0.18f, 0.32f, 0.22f) }, // Musgo antiguo en la corona
                { '$', new Color(0.10f, 0.85f, 0.90f) }, // Grietas rÃºnicas cian
                { '!', new Color(0.80f, 1f, 1f) }        // NÃºcleo rÃºnico brillante
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }

        // ========================================================
        // 2. BIOMA HIELO (Cripta Glacial)
        // ========================================================

        // 2.1 GÃ³lem de Escarcha (Tanque) - Hombros de cristal y carÃ¡mbanos
        private static Sprite DibujarGolemHielo()
        {
            string[] mapa = new string[]
            {
                "         ..      ..         ",
                "        .##.    .##.        ",
                "       .####.  .####.       ",
                "      ..####....####..      ",
                "     .XXOOOOOOOOOOOOXX.     ",
                "    .XXO@@@@@@@@@@@@OXX.    ",
                "   ..XO@...@@@@@@...@@OX..  ",
                "  .##XO@.!!.@@@@.!!.@@OX##. ",
                "  .#$XO@.!!.@@@@.!!.@@OX$#. ",
                "  .##XO@...@@@@@@...@@OX##. ",
                "   ..XXOOOOOOOOOOOOOOXX..   ",
                "  ...XXXXXXXXXXXXXXXXXX...  ",
                " .###.XXOOOOOOOOOOOOXX.###. ",
                ".#$$#.XXOOOOOOOOOOOOXX.#$$#.",
                ".#$$#.XXXXXXXXXXXXXXXX.#$$#.",
                " .##. .XXOOOOOOOOOOXX. .##. ",
                "  ..  .XXOOOOOOOOOOXX.  ..  ",
                "       .XXXXXXXXXXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "      .XXXXXX..XXXXXX.      ",
                "      .......  .......      "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.04f, 0.10f, 0.18f) },
                { 'X', new Color(0.12f, 0.25f, 0.40f) }, // Hielo sombra
                { 'O', new Color(0.24f, 0.50f, 0.72f) }, // Hielo medio
                { '@', new Color(0.50f, 0.80f, 0.95f) }, // Hielo brillo
                { '#', new Color(0.35f, 0.65f, 0.85f) }, // Puntas de carÃ¡mbano
                { '$', new Color(0.85f, 0.98f, 1f) },    // Escarcha blanca
                { '!', new Color(0.80f, 1f, 1f) }        // Ojos glaciales resplandecientes
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 2.2 Lobo Polar (Melee RÃ¡pido) - Lobo Ã¡rtico blanco con ojos cian
        private static Sprite DibujarLoboPolar()
        {
            string[] mapa = new string[]
            {
                " ..          ..         ",
                ".XX.        .XX.        ",
                ".XOX.      .XOX.        ",
                ".XOOX......XOOX.        ",
                ".XOOOXXXXXXOOOX.        ",
                ".XOOOOOOOOOOOOX.        ",
                ".XOO@.OOOO.@OOX.        ",
                ".XOO@.!!OO.!!OX.        ",
                ".XOO@.!!OO.!!OX.        ",
                " .XOOOO#..#OOX.         ",
                "  .XOOO####OX.          ",
                "   .XXXXXXXX.   ...     ",
                "  .XXOOOOOOXX. .XX.     ",
                " .XXOOOOOOOOXX.XOX.     ",
                ".XXOOOOOOOOOOXXXOX.     ",
                ".XXOOOOOOOOOOOOXOX.     ",
                " .XXOOOOOOOOOOXXOX.     ",
                "  .XXXXXXXXXXXXXOX.     ",
                "   .XX.    .XX.XOX.     ",
                "   .XX.    .XX. .X.     ",
                "  .XXX.   .XXX.  ..     ",
                "  ....    ....          "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.12f, 0.20f) },
                { 'X', new Color(0.40f, 0.55f, 0.72f) }, // Pelaje sombra azulada
                { 'O', new Color(0.75f, 0.88f, 0.98f) }, // Pelaje blanco Ã¡rtico
                { '@', new Color(1f, 1f, 1f) },          // Blanco nieve puro
                { '!', new Color(0.10f, 0.85f, 1f) },    // Ojos cian penetrantes
                { '#', new Color(0.15f, 0.20f, 0.30f) }  // Hocico oscuro
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 2.3 Mago Glacial (Rango) - Mago encapuchado azul con bÃ¡culo de diamante de hielo
        private static Sprite DibujarMagoGlacial()
        {
            string[] mapa = new string[]
            {
                "       $$$              ",
                "      $***$             ",
                "       $*$              ",
                "        #      ...      ",
                "       #+#    .XXX.     ",
                "        #    .X@@@X.    ",
                "        #   .X@...X.    ",
                "        #   .X.!!!.X.   ",
                "        #   .X.!!!.X.   ",
                "       #+#  .X@...X.    ",
                "        #   ..X@@@X..   ",
                "        #  .XXOOOOOXX.  ",
                "        # .XXOOOOOOOXX. ",
                "       #+#.XXOOOOOOOXX. ",
                "        #.XXOOOOOOOOOXX.",
                "        #.XXOOOOOOOOOXX.",
                "        #.XXOOOOOOOOOXX.",
                "        # .XXOOOOOOOXX. ",
                "        #  .XXXXXXXXX.  ",
                "        #   .XX...XX.   ",
                "        #   .XX. .XX.   ",
                "            .XX. .XX.   "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.10f, 0.18f) },
                { 'X', new Color(0.12f, 0.22f, 0.35f) }, // TÃºnica azul oscuro
                { 'O', new Color(0.20f, 0.40f, 0.62f) }, // TÃºnica azul medio
                { '@', new Color(0.40f, 0.68f, 0.88f) }, // Bordado cian
                { '!', new Color(0.20f, 0.95f, 1f) },    // Ojos de hielo brillante
                { '#', new Color(0.35f, 0.25f, 0.18f) }, // Madera del bÃ¡culo
                { '+', new Color(0.60f, 0.75f, 0.90f) }, // Anillos de plata
                { '*', new Color(0.60f, 0.92f, 1f) },    // Cristal de hielo flotante
                { '$', new Color(1f, 1f, 1f) }           // Destello cristalino
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 2.4 MurciÃ©lago Glacial (Especial) - Alas de cristal translÃºcido y colmillos de hielo
        private static Sprite DibujarMurcielagoHielo()
        {
            string[] mapa = new string[]
            {
                " ..              ..     ",
                ".##.            .##.    ",
                ".#$#.   ....   .#$#.    ",
                ".#$$#..XXXXXX..#$$#.    ",
                " .#$$#XOOOOOOX#$$#.     ",
                "  .#$XOO@..@OOX$#.      ",
                "   .XXOO.!!.OOXX.       ",
                "  .XXXXO.!!.OXXXX.      ",
                " .XXXXXXOOOOXXXXXX.     ",
                ".XXXXXXXX..XXXXXXXX.    ",
                ".XXXXXXX.  .XXXXXXX.    ",
                " .XXXXX.    .XXXXX.     ",
                "  .XXX.      .XXX.      ",
                "   ...        ...       "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.04f, 0.08f, 0.16f) },
                { 'X', new Color(0.12f, 0.24f, 0.38f) }, // Ala azul profundo
                { 'O', new Color(0.25f, 0.50f, 0.72f) }, // Membrana cristalina
                { '@', new Color(0.55f, 0.82f, 0.95f) },
                { '!', new Color(0.85f, 1f, 1f) },       // Ojos helados
                { '#', new Color(0.40f, 0.70f, 0.90f) }, // Puntas de hielo
                { '$', new Color(0.90f, 0.98f, 1f) }     // Brillo blanco
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.25f);
        }

        // 2.5 Yeti de las Ventiscas (Jefe - 36x40) - Bestia colosal con cuernos de carnero de hielo
        private static Sprite DibujarYetiVentiscas()
        {
            string[] mapa = new string[]
            {
                "    ....                    ....    ",
                "   .####.                  .####.   ",
                "  .######.                .######.  ",
                "  .##$$##.   ..........   .##$$##.  ",
                "  .##$$##...XXXXXXXXXXXX...##$$##.  ",
                "   .####.XXXXXXXXXXXXXXXXXX.####.   ",
                "    ....XXOOOOOOOOOOOOOOOOXX....    ",
                "       .XXOO@@@@@@@@@@@@OOXX.       ",
                "      .XXOO@...@@@@@@...@@OOXX.     ",
                "     .XXOOO@.!!.@@@@.!!.@@OOOXX.    ",
                "     .XXOOO@.!!.@@@@.!!.@@OOOXX.    ",
                "     .XXOOO@...@@@@@@...@@OOOXX.    ",
                "      .XXOOOOOOOOOOOOOOOOOOOOXX.    ",
                "     ..XXOOOO--#----#--OOOOOOXX..   ",
                "   ..XXXXOOOO--#----#--OOOOOOXXXX.. ",
                "  .XXXXXXOOOO----------OOOOOOXXXXXX.",
                " .XXXXOOOOOOO##########OOOOOOOXXXXX.",
                ".XXXXO@@@@OOOOOOOOOOOOOOOO@@@@OXXXX.",
                ".XXXO@$$$$@OOOOOOOOOOOOOO@$$$$@OXXX.",
                ".XXXO@$$$$@OOOOOOOOOOOOOO@$$$$@OXXX.",
                ".XXXXO@@@@OOOOOOOOOOOOOOOO@@@@OXXXX.",
                " .XXXXOOOOOOOOOOOOOOOOOOOOOOOOXXXX. ",
                "  .XXXXXXOOOOOOOOOOOOOOOOOOOOXXXX.  ",
                "   ..XXXXXXOOOOOOOOOOOOOOOOXXXX..   ",
                "     .XXXXXOOOOOOOOOOOOOOOOXXXXX.   ",
                "     .XXXXXOOOOOOOOOOOOOOOOXXXXX.   ",
                "     .XXXXXXXXXXXXXXXXXXXXXXXXXX.   ",
                "     .XXXXXXXX.      .XXXXXXXXXX.   ",
                "     .XXXXXXXX.      .XXXXXXXXXX.   ",
                "    .XXXXXXXXXX.    .XXXXXXXXXXXX.  ",
                "    ............    ..............  "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.04f, 0.08f, 0.16f) },
                { 'X', new Color(0.24f, 0.35f, 0.50f) }, // Pelaje sombra
                { 'O', new Color(0.60f, 0.75f, 0.88f) }, // Pelaje medio
                { '@', new Color(0.85f, 0.94f, 1f) },    // Pelaje blanco nieve
                { '#', new Color(0.18f, 0.40f, 0.65f) }, // Cuernos de hielo oscuro / boca
                { '$', new Color(0.55f, 0.85f, 1f) },    // Cuernos brillo
                { '-', new Color(0.10f, 0.14f, 0.22f) }, // Boca rugiente oscura
                { '!', new Color(0.20f, 0.95f, 1f) }     // Ojos cian ardientes
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }

        // ========================================================
        // 3. BIOMA FUEGO (Fortaleza VolcÃ¡nica)
        // ========================================================

        // 3.1 Engendro de Magma (Tanque) - Armadura de basalto negro con grietas de lava incandescente
        private static Sprite DibujarEngendroMagma()
        {
            string[] mapa = new string[]
            {
                "         ..      ..         ",
                "        .##.    .##.        ",
                "       .####....####.       ",
                "      ..XXXXXXXXXXXX..      ",
                "     .XXOOOOOOOOOOOOXX.     ",
                "    .XXO@@@@@@@@@@@@OXX.    ",
                "   ..XO@...@@@@@@...@@OX..  ",
                "  .##XO@.!!.@@@@.!!.@@OX##. ",
                "  .#$XO@.!!.@@@@.!!.@@OX$#. ",
                "  .##XO@...@@@@@@...@@OX##. ",
                "   ..XXOOOOOOOOOOOOOOXX..   ",
                "  ...XXXXXXXXXXXXXXXXXX...  ",
                " .###.XXOOOO$$$$OOOOXX.###. ",
                ".#$$#.XXOOO$$!!$$OOOXX.#$$#.",
                ".#$$#.XXOOOO$$$$OOOOXX.#$$#.",
                " .##. .XXOOOOOOOOOOXX. .##. ",
                "  ..  .XXOOOOOOOOOOXX.  ..  ",
                "       .XXXXXXXXXXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "      .XXXXXX..XXXXXX.      ",
                "      .......  .......      "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.04f, 0.04f) },
                { 'X', new Color(0.18f, 0.12f, 0.14f) }, // Basalto sombra
                { 'O', new Color(0.32f, 0.22f, 0.26f) }, // Basalto medio
                { '@', new Color(0.50f, 0.38f, 0.42f) }, // Basalto brillo
                { '#', new Color(0.85f, 0.20f, 0.05f) }, // Magma rojo
                { '$', new Color(1f, 0.55f, 0.10f) },    // Lava naranja
                { '!', new Color(1f, 0.90f, 0.20f) }     // NÃºcleo amarillo brillante
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 3.2 Sabueso Ãgneo (Melee RÃ¡pido) - Perro demonÃ­aco con crin en llamas
        private static Sprite DibujarSabuesoIgneo()
        {
            string[] mapa = new string[]
            {
                "    $$        $$$       ",
                "   $**$      $***$      ",
                "  $****$....$*****$     ",
                " ..#**#XXXXXX#***#..    ",
                ".XX#*#OOOOOOOO#*#OXX.   ",
                ".XOOOOOOOOOOOOOOOOOOX.  ",
                ".XOO@.OOOOOOOOOO.@OOX.  ",
                ".XOO@.!!OOOOOO!!.@@OX.  ",
                ".XOO@.!!OOOOOO!!.@@OX.  ",
                " .XOOOO--####--OOOOOX.  ",
                "  .XOOO--------OOOOX.   ",
                "   .XXXXXXXXXXXXXXX.    ",
                "  .XXOOOOOOOOOOOOOXX.   ",
                " .XXOOOOOOOOOOOOOOOXX.  ",
                ".XXOOOOOOOOOOOOOOOOOXX. ",
                " .XXOOOOOOOOOOOOOOOXX.  ",
                "  .XXXXXXXXXXXXXXXXX.   ",
                "   .XX.  .XX.  .XX.     ",
                "  .XXX. .XXX. .XXX.     ",
                "  ....  ....  ....      "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.04f, 0.04f) },
                { 'X', new Color(0.18f, 0.10f, 0.12f) }, // Piel chamuscada sombra
                { 'O', new Color(0.32f, 0.18f, 0.22f) }, // Piel chamuscada media
                { '@', new Color(0.52f, 0.30f, 0.36f) },
                { '#', new Color(0.85f, 0.15f, 0.05f) }, // Fuego base rojo
                { '*', new Color(1f, 0.50f, 0.08f) },    // Fuego naranja
                { '$', new Color(1f, 0.88f, 0.25f) },    // Fuego amarillo
                { '-', new Color(0.10f, 0.05f, 0.05f) }, // Fauces oscuras
                { '!', new Color(1f, 0.20f, 0.05f) }     // Ojos de fuego
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 3.3 ChamÃ¡n de Fuego (Rango) - MÃ¡scara ceremonial, tÃºnica roja y bÃ¡culo ardiente
        private static Sprite DibujarChamanFuego()
        {
            string[] mapa = new string[]
            {
                "       $$$              ",
                "      $***$             ",
                "       $*$              ",
                "        #      ...      ",
                "       #+#    .XXX.     ",
                "        #    .X@@@X.    ",
                "        #   .X@...X.    ",
                "        #   .X.!!!.X.   ",
                "        #   .X.!!!.X.   ",
                "       #+#  .X@...X.    ",
                "        #   ..X@@@X..   ",
                "        #  .XXOOOOOXX.  ",
                "        # .XXOOOOOOOXX. ",
                "       #+#.XXOOOOOOOXX. ",
                "        #.XXOOOOOOOOOXX.",
                "        #.XXOOOOOOOOOXX.",
                "        #.XXOOOOOOOOOXX.",
                "        # .XXOOOOOOOXX. ",
                "        #  .XXXXXXXXX.  ",
                "        #   .XX...XX.   ",
                "        #   .XX. .XX.   ",
                "            .XX. .XX.   "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.04f, 0.04f) },
                { 'X', new Color(0.35f, 0.08f, 0.08f) }, // TÃºnica carmesÃ­ sombra
                { 'O', new Color(0.65f, 0.15f, 0.12f) }, // TÃºnica carmesÃ­ media
                { '@', new Color(0.90f, 0.35f, 0.20f) }, // Borde fuego claro
                { '!', new Color(1f, 0.85f, 0.15f) },    // Ojos de fuego brillante
                { '#', new Color(0.28f, 0.20f, 0.18f) }, // Hierro negro del bÃ¡culo
                { '+', new Color(0.85f, 0.65f, 0.20f) }, // Anillos de bronce
                { '*', new Color(1f, 0.45f, 0.08f) },    // Bola de fuego naranja
                { '$', new Color(1f, 0.90f, 0.30f) }     // Fuego amarillo brillante
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 3.4 Fatuo VolcÃ¡nico (Especial) - Remolino de llamas vivas con nÃºcleo ardiente
        private static Sprite DibujarFatuoVolcanico()
        {
            string[] mapa = new string[]
            {
                "       $$$              ",
                "      $***$    $$       ",
                "     $*****$  $**$      ",
                "    ..#***#....#*$      ",
                "   .##X***X####..       ",
                "  .##XX***XX####.       ",
                "  .#XXO***OXX###.       ",
                "  .XOO@.!.@OOX##.       ",
                "  .XOO@.!.@OOX#.        ",
                "   .XXOOOOOXX.          ",
                "    .XXXXXXX.           ",
                "     .XX.XX.            ",
                "      .. ..             "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.10f, 0.02f, 0.02f) },
                { 'X', new Color(0.55f, 0.08f, 0.05f) }, // Rojo oscuro
                { 'O', new Color(0.85f, 0.25f, 0.05f) }, // Fuego medio
                { '@', new Color(1f, 0.60f, 0.12f) },    // Fuego claro
                { '!', new Color(1f, 1f, 0.70f) },       // Ojos blancos incandescentes
                { '#', new Color(0.70f, 0.12f, 0.05f) }, // Llamas exteriores
                { '*', new Color(1f, 0.50f, 0.08f) },    // Lenguas de fuego
                { '$', new Color(1f, 0.85f, 0.25f) }     // Puntas de chispa
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.25f);
        }

        // 3.5 SeÃ±or del Magma (Jefe - 36x40) - Archidemonio Ã­gneo con cuernos colosales y pecho de lava
        private static Sprite DibujarSenorMagma()
        {
            string[] mapa = new string[]
            {
                "  $$                            $$  ",
                " $**$                          $**$ ",
                ".#***#                        #***#.",
                ".##***#                      #***##.",
                " .##***#    ............    #***##. ",
                "  .##***#..##############..#***##.  ",
                "   .##**#.XXXXXXXXXXXXXXXX.#**##.   ",
                "    .##..XXOOOOOOOOOOOOOOXX..##.    ",
                "      ..XXOO@@@@@@@@@@@@OOXX..      ",
                "     .XXOO@...@@@@@@...@@OOXX.      ",
                "    .XXOOO@.!!.@@@@.!!.@@OOOXX.     ",
                "    .XXOOO@.!!.@@@@.!!.@@OOOXX.     ",
                "    .XXOOO@...@@@@@@...@@OOOXX.     ",
                "     .XXOOOOOOOOOOOOOOOOOOOOXX.     ",
                "    ..XXOOOO$$$$$$$$$$OOOOOOXX..    ",
                "  ..XXXXOOOO$$!!$$!!$$OOOOOOXXXX..  ",
                " .XXXXXXOOOO$$$$$$$$$$OOOOOOXXXXXX. ",
                ".XXXXOOOOOOO##########OOOOOOOXXXXXX.",
                ".XXXO@$$$$@OOOOOOOOOOOOOOOO@@@@OXXX.",
                ".XXXO@$$$$@OOOOOOOOOOOOOOOO@@@@OXXX.",
                ".XXXXO@@@@OOOOOOOOOOOOOOOO@@@@OXXXX.",
                " .XXXXOOOOOOOOOOOOOOOOOOOOOOOOXXXX. ",
                "  .XXXXXXOOOOOOOOOOOOOOOOOOOOXXXX.  ",
                "   ..XXXXXXOOOOOOOOOOOOOOOOXXXX..   ",
                "     .XXXXXOOOOOOOOOOOOOOOOXXXXX.   ",
                "     .XXXXXOOOOOOOOOOOOOOOOXXXXX.   ",
                "     .XXXXXXXXXXXXXXXXXXXXXXXXXX.   ",
                "     .XXXXXXXX.      .XXXXXXXXXX.   ",
                "     .XXXXXXXX.      .XXXXXXXXXX.   ",
                "    .XXXXXXXXXX.    .XXXXXXXXXXXX.  ",
                "    ............    ..............  "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.04f, 0.04f) },
                { 'X', new Color(0.20f, 0.12f, 0.14f) }, // Basalto obsidiana sombra
                { 'O', new Color(0.36f, 0.22f, 0.26f) }, // Basalto medio
                { '@', new Color(0.58f, 0.38f, 0.42f) }, // Basalto claro
                { '#', new Color(0.85f, 0.18f, 0.05f) }, // Cuernos de lava roja
                { '*', new Color(1f, 0.55f, 0.10f) },    // Cuernos lava naranja
                { '$', new Color(1f, 0.85f, 0.25f) },    // CorazÃ³n ardiente de sol lÃ­quido
                { '!', new Color(1f, 1f, 0.80f) }        // NÃºcleo blanco incandescente
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }

        // ========================================================
        // 4. BIOMA PLANTA (Santuario de Esporas)
        // ========================================================

        // 4.1 Treant Ancestral (Tanque) - Roble viviente con musgo y hojas
        private static Sprite DibujarTreant()
        {
            string[] mapa = new string[]
            {
                "         $$      $$         ",
                "        $**$    $**$        ",
                "       $****$..$****$       ",
                "      ..####XXXX####..      ",
                "     .XXOOOOOOOOOOOOXX.     ",
                "    .XXO@@@@@@@@@@@@OXX.    ",
                "   ..XO@...@@@@@@...@@OX..  ",
                "  .##XO@.!!.@@@@.!!.@@OX##. ",
                "  .#$XO@.!!.@@@@.!!.@@OX$#. ",
                "  .##XO@...@@@@@@...@@OX##. ",
                "   ..XXOOOOOOOOOOOOOOXX..   ",
                "  ...XXXXXXXXXXXXXXXXXX...  ",
                " .###.XXOOOOOOOOOOOOXX.###. ",
                ".#$$#.XXOOOOOOOOOOOOXX.#$$#.",
                ".#$$#.XXXXXXXXXXXXXXXX.#$$#.",
                " .##. .XXOOOOOOOOOOXX. .##. ",
                "  ..  .XXOOOOOOOOOOXX.  ..  ",
                "       .XXXXXXXXXXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "       .XXXX.  .XXXX.       ",
                "      .XXXXXX..XXXXXX.      ",
                "      .......  .......      "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.08f, 0.04f) },
                { 'X', new Color(0.24f, 0.16f, 0.10f) }, // Corteza roble sombra
                { 'O', new Color(0.40f, 0.28f, 0.18f) }, // Corteza roble media
                { '@', new Color(0.58f, 0.42f, 0.28f) }, // Corteza roble clara
                { '#', new Color(0.20f, 0.32f, 0.16f) }, // Musgo sereno
                { '$', new Color(0.32f, 0.52f, 0.26f) }, // Hojas verde bosque calmado
                { '*', new Color(0.24f, 0.40f, 0.20f) }, // Follaje medio
                { '!', new Color(0.95f, 0.75f, 0.20f) }  // Ojos de espíritu ámbar
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 4.2 Mandrágora Rabiosa (Melee Rápido) - Raíz viviente veloz con brote de hojas
        private static Sprite DibujarMandragora()
        {
            string[] mapa = new string[]
            {
                "         $$$            ",
                "        $***$           ",
                "         $*$            ",
                "        ......          ",
                "       .XXXXXX.         ",
                "      .XXOOOOXX.        ",
                "     .XXO@@@@OXX.       ",
                "     .XO@....@OX.       ",
                "     .XO@.!!.@@X.       ",
                "     .XO@.!!.@@X.       ",
                "     .XO@....@OX.       ",
                "      .X--##--X.        ",
                "      .X------X.        ",
                "       .XXXXXX.         ",
                "      .XXOOOOXX.        ",
                "     .XXOOOOOOXX.       ",
                "      .XXXXXXXX.        ",
                "      .XX.  .XX.        ",
                "     .XXX.  .XXX.       ",
                "     ....    ....       "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { 'X', new Color(0.32f, 0.22f, 0.14f) }, // Raíz sombra
                { 'O', new Color(0.52f, 0.38f, 0.25f) }, // Raíz media
                { '@', new Color(0.72f, 0.55f, 0.38f) }, // Raíz clara
                { '$', new Color(0.30f, 0.52f, 0.24f) }, // Brote verde calmado
                { '*', new Color(0.22f, 0.40f, 0.18f) },
                { '#', new Color(0.88f, 0.82f, 0.68f) }, // Dientes de madera
                { '-', new Color(0.12f, 0.08f, 0.06f) }, // Boca abierta
                { '!', new Color(0.88f, 0.18f, 0.18f) }  // Ojos rojos
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 4.3 Flor CarnÃ­vora (Rango) - Fauces abiertas de planta carnÃ­vora con espinas
        private static Sprite DibujarFlorCarnivora()
        {
            string[] mapa = new string[]
            {
                "      ........          ",
                "     .########.         ",
                "    .##--##--##.        ",
                "   .##--------##.       ",
                "  .##----------##.      ",
                "  .#------------#.      ",
                "  .#--!!----!!--#.      ",
                "  .#--!!----!!--#.      ",
                "  .##----------##.      ",
                "   .##--####--##.       ",
                "    .##########.        ",
                "     ...XXXX...         ",
                "       .XOOX.           ",
                "       .XOOX.   ..      ",
                "   ..  .XOOX.  .##.     ",
                "  .##. .XOOX. .####.    ",
                " .####..XOOX..######.   ",
                " .##################.   ",
                "  .################.    ",
                "   ................     "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.08f, 0.04f) },
                { 'X', new Color(0.16f, 0.28f, 0.14f) }, // Tallo verde oscuro sereno
                { 'O', new Color(0.26f, 0.44f, 0.22f) }, // Tallo verde medio
                { '#', new Color(0.44f, 0.18f, 0.46f) }, // Pétalos violeta sobrio
                { '-', new Color(0.18f, 0.07f, 0.20f) }, // Interior fauces oscuro
                { '!', new Color(0.62f, 0.78f, 0.35f) }  // Esporas polen tenue
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 4.4 Hongo de Esporas (Especial) - Sombrero de hongo morado con motas venenosas verdes
        private static Sprite DibujarHongoEsporas()
        {
            string[] mapa = new string[]
            {
                "        ......          ",
                "      ..######..        ",
                "     .##$$##$$##.       ",
                "    .###$$##$$###.      ",
                "   .##############.     ",
                "  .##$$########$$##.    ",
                "  .##$$########$$##.    ",
                "  .################.    ",
                "   ...XXXXXXXXXX...     ",
                "     .XXOOOOOOXX.       ",
                "     .XO@....@OX.       ",
                "     .XO@.!!.@@X.       ",
                "     .XO@.!!.@@X.       ",
                "     .XO@....@OX.       ",
                "     .XXOOOOOOXX.       ",
                "      .XXXXXXXX.        ",
                "      ..........        "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.06f, 0.08f) },
                { 'X', new Color(0.24f, 0.22f, 0.28f) }, // Tallo sombra
                { 'O', new Color(0.44f, 0.42f, 0.50f) }, // Tallo medio
                { '@', new Color(0.66f, 0.64f, 0.74f) }, // Tallo claro
                { '#', new Color(0.45f, 0.18f, 0.55f) }, // Sombrero morado profundo
                { '$', new Color(0.40f, 0.72f, 0.45f) }, // Motas jade suave
                { '!', new Color(0.95f, 0.80f, 0.20f) }  // Ojos ámbar
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.20f);
        }

        // 4.5 La Reina Espora (Jefe - 36x40) - Emperatriz floral con pétalos gigantes y látigos de espinas
        private static Sprite DibujarReinaEspora()
        {
            string[] mapa = new string[]
            {
                "     $$$                  $$$       ",
                "    $***$                $***$      ",
                "   $*****$  ..........  $*****$     ",
                "  .##***##..##########..##***##.    ",
                " .###***##################***###.   ",
                ".####***######$$$$######***####.    ",
                ".#####*######$$!!$$######*#####.    ",
                " .##########$$$!!$$$##########.     ",
                "  .#########$$$!!$$$#########.      ",
                "   .#########$$$$$$#########.       ",
                "    ..####################..        ",
                "   .XXXX.##############.XXXX.       ",
                "  .XXXXXX.XXXXXXXXXXXX.XXXXXX.      ",
                " .XXOOOOXX.XXOOOOOOXX.XXOOOOXX.     ",
                ".XXO@@@@OXX.XOOOOOOX.XXO@@@@OXX.    ",
                ".XXO@..@OXX.XOOOOOOX.XXO@..@OXX.    ",
                ".XXO@!!@OXX.XOOOOOOX.XXO@!!@OXX.    ",
                ".XXO@..@OXX.XOOOOOOX.XXO@..@OXX.    ",
                ".XXO@@@@OXX.XXXXXXXX.XXO@@@@OXX.    ",
                " .XXOOOOXX.XXOOOOOOXX.XXOOOOXX.     ",
                "  .XXXXXX.XXOOOOOOOOXX.XXXXXX.      ",
                "   ..XX..XXOOOOOOOOOOXX..XX..       ",
                "        .XXOOOOOOOOOOXX.            ",
                "       .XXOOOOOOOOOOOOXX.           ",
                "       .XXXXXXXXXXXXXXXX.           ",
                "       .XXXXXX.  .XXXXXX.           ",
                "       .XXXXXX.  .XXXXXX.           ",
                "      .XXXXXXXX..XXXXXXXX.          ",
                "      .........  .........          "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.06f, 0.08f, 0.04f) },
                { 'X', new Color(0.16f, 0.26f, 0.14f) }, // Látigos de enredadera sombra
                { 'O', new Color(0.26f, 0.45f, 0.24f) }, // Enredadera media serena
                { '@', new Color(0.40f, 0.62f, 0.36f) }, // Espinas claras suaves
                { '#', new Color(0.54f, 0.18f, 0.48f) }, // Pétalos violeta profundo
                { '$', new Color(0.72f, 0.32f, 0.62f) }, // Borde pétalo orquídea suave
                { '*', new Color(0.28f, 0.55f, 0.32f) }, // Hojas corona verde bosque
                { '!', new Color(0.95f, 0.88f, 0.30f) }  // Polen dorado
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }

        // ========================================================
        // 5. BIOMA ARENA (NecrÃ³polis del Desierto)
        // ========================================================

        // 5.1 SarcÃ³fago Andante (Tanque) - SarcÃ³fago de arenisca con mÃ¡scara dorada de faraÃ³n
        private static Sprite DibujarSarcofagoAndante()
        {
            string[] mapa = new string[]
            {
                "         ........           ",
                "        .########.          ",
                "       .##########.         ",
                "      .##$$$$$$$$##.        ",
                "     .##$$--$$--$$##.       ",
                "     .##$$-!!$$-!!$##.      ",
                "     .##$$-!!$$-!!$##.      ",
                "      .##$$$$$$$$##.        ",
                "       .##--##--##.         ",
                "      .###++++####.         ",
                "     .##XXXXXXXXXX##.       ",
                "    .##XXOOOOOOOOXX##.      ",
                "   .##XXO@@@@@@@@OXX##.     ",
                "   .##XXO@$$$$$$@OXX##.     ",
                "   .##XXO@$++++$@OXX##.     ",
                "   .##XXO@$$$$$$@OXX##.     ",
                "   .##XXO@@@@@@@@OXX##.     ",
                "    .##XXOOOOOOOOXX##.      ",
                "     .##XXXXXXXXXX##.       ",
                "      .XXXXXXXXXXXX.        ",
                "      .XXXX.  .XXXX.        ",
                "     .XXXXXX..XXXXXX.       ",
                "     .......  .......       "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { 'X', new Color(0.42f, 0.32f, 0.18f) }, // Arenisca sombra
                { 'O', new Color(0.65f, 0.50f, 0.30f) }, // Arenisca media
                { '@', new Color(0.85f, 0.70f, 0.45f) }, // Arenisca clara
                { '#', new Color(0.70f, 0.50f, 0.15f) }, // Oro faraÃ³nico sombra
                { '$', new Color(0.95f, 0.75f, 0.20f) }, // Oro brillante
                { '-', new Color(0.15f, 0.30f, 0.65f) }, // Franjas de lapislÃ¡zuli azul
                { '+', new Color(0.85f, 0.15f, 0.20f) }, // RubÃ­ sagrado
                { '!', new Color(1f, 0.10f, 0.15f) }     // Ojos de rubÃ­ brillante
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 5.2 AtaÃºd Ritual (Spawner) - Tumba erguida con vÃ³rtice de almas oscuras y cobra de oro
        private static Sprite DibujarAtaudRitual()
        {
            string[] mapa = new string[]
            {
                "         ..#$#..            ",
                "        .#######.           ",
                "       .#########.          ",
                "      .##.......##.         ",
                "     .##.-------.##.        ",
                "     .##.-------.##.        ",
                "     .##.--!!!!--.##.       ",
                "     .##.--!*!--.##.        ",
                "     .##.--!!!!--.##.       ",
                "     .##.-------.##.        ",
                "     .##.-------.##.        ",
                "     .##.--!!!!--.##.       ",
                "     .##.--!*!--.##.        ",
                "     .##.--!!!!--.##.       ",
                "     .##.-------.##.        ",
                "     .##.-------.##.        ",
                "     .##.-------.##.        ",
                "    .####.......####.       ",
                "   .#################.      ",
                "   ...................      "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { '#', new Color(0.55f, 0.42f, 0.25f) }, // Piedra de tumba arenisca
                { '$', new Color(0.95f, 0.75f, 0.20f) }, // Cobra de oro superior
                { '-', new Color(0.06f, 0.04f, 0.08f) }, // VÃ³rtice negro abisal interior
                { '!', new Color(0.10f, 0.85f, 0.45f) }, // Glifos rÃºnicos verdes de invocaciÃ³n
                { '*', new Color(0.80f, 1f, 0.80f) }     // Resplandor de invocaciÃ³n
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }

        // 5.3 Rata del Desierto (Melee RÃ¡pido) - Rata gigante salvaje de arena con incisivos
        private static Sprite DibujarRataDesierto()
        {
            string[] mapa = new string[]
            {
                " ..          ..         ",
                ".##.        .##.        ",
                ".#$#.      .#$#.        ",
                ".#$#X......X#$#.        ",
                ".XOOOXXXXXXOOOX.        ",
                ".XOOOOOOOOOOOOX.        ",
                ".XOO@.OOOO.@OOX.        ",
                ".XOO@.!!OO.!!OX.        ",
                ".XOO@.!!OO.!!OX.        ",
                " .XOOOO--++--OX.        ",
                "  .XOOO------X. ...     ",
                "   .XXXXXXXXX. .---.    ",
                "  .XXOOOOOOXX. .---.    ",
                " .XXOOOOOOOOXX..---.    ",
                ".XXOOOOOOOOOOX.---.     ",
                ".XXOOOOOOOOOOX.---.     ",
                " .XXOOOOOOOOXX.---.     ",
                "  .XXXXXXXXXX. .-.      ",
                "   .XX.  .XX.   .       ",
                "  .XXX. .XXX.           ",
                "  ....  ....            "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { 'X', new Color(0.35f, 0.25f, 0.15f) }, // Pelaje sombra arena
                { 'O', new Color(0.55f, 0.42f, 0.26f) }, // Pelaje medio arena
                { '@', new Color(0.75f, 0.60f, 0.40f) }, // Pelaje claro
                { '#', new Color(0.48f, 0.32f, 0.25f) }, // Orejas
                { '$', new Color(0.75f, 0.45f, 0.50f) }, // Interior oreja rosado
                { '+', new Color(0.95f, 0.90f, 0.60f) }, // Dientes incisivos afilados
                { '-', new Color(0.65f, 0.40f, 0.45f) }, // Cola escamada rosada
                { '!', new Color(1f, 0.12f, 0.15f) }     // Ojos rojos rubÃ­
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 5.4 Momia Maldita (Rango) - Momia con vendas desgarradas y maldiciÃ³n en las manos
        private static Sprite DibujarMomia()
        {
            string[] mapa = new string[]
            {
                "        ......          ",
                "       .XXXXXX.         ",
                "      .X@@@@@@X.        ",
                "      .X@....@X.        ",
                "     .X@.OOOO.@X.       ",
                "     .X@.O!!O.@X.       ",
                "     .X@.OOOO.@X.       ",
                "      .X@.OO.@X.        ",
                "    ...X@....@X...      ",
                "   .---.XXXXXX.---.     ",
                "  .----.XOOOOX.----.    ",
                " ..----.XOOOOX.----..   ",
                ".++++++.XXXXXX.++++++.  ",
                "  ....  .OOOO.  ....    ",
                "        .X..X.          ",
                "        .O..O.          ",
                "       .XX..XX.         ",
                "       ...  ...         "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { 'X', new Color(0.38f, 0.32f, 0.24f) }, // Vendajes sombra
                { 'O', new Color(0.60f, 0.52f, 0.40f) }, // Vendajes medios
                { '@', new Color(0.82f, 0.74f, 0.60f) }, // Vendajes claros
                { '-', new Color(0.70f, 0.62f, 0.50f) }, // Tiras de tela suelta
                { '+', new Color(0.10f, 0.85f, 0.70f) }, // MaldiciÃ³n mÃ¡gica turquesa en las manos
                { '!', new Color(0.10f, 0.95f, 0.75f) }  // Ojos turquesa malditos
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.18f);
        }

        // 5.5 FaraÃ³n Eterno (Jefe - 36x40) - Dios-Rey no muerto con tocado Nemes, mÃ¡scara de oro y cetros
        private static Sprite DibujarFaraonEterno()
        {
            string[] mapa = new string[]
            {
                "            ............            ",
                "           .############.           ",
                "          .##--$$--$$--##.          ",
                "         .##--$$--$$--$$##.         ",
                "        .##--$$--$$--$$--##.        ",
                "        .##--$$--$$--$$--##.        ",
                "        .##--$$..$$..$$--##.        ",
                "        .##--$.!!$$.!!$--##.        ",
                "        .##--$.!!$$.!!$--##.        ",
                "        .##--$$..$$..$$--##.        ",
                "        .##--$$$$$$$$$$--##.        ",
                "         .##--$$++++$$--##.         ",
                "          .##--$$$$$$--##.          ",
                "       ....############....         ",
                "     ..XXX.############.XXX..       ",
                "    .XXXXXX.XXXXXXXXXX.XXXXXX.      ",
                "   .XXXXOOOO.XOOOOOOX.OOOOXXXX.     ",
                "  .XXXXO@@@@O.XOOOOX.O@@@@OXXXX.    ",
                "  .XXXO@$$$$@O.XXXX.O@$$$$@OXXX.    ",
                "  .XXXO@$++$@O.OOOO.O@$++$@OXXX.    ",
                "  .XXXXO@@@@O.XOOOOX.O@@@@OXXXX.    ",
                "   .XXXXOOOO.XXOOOOXX.OOOOXXXX.     ",
                "    .XXXXXX.XXOOOOOOXX.XXXXXX.      ",
                "     ..XX..XXOOOOOOOOXX..XX..       ",
                "         .XXOOOOOOOOOOXX.           ",
                "        .XXOOOOOOOOOOOOXX.          ",
                "        .XXXXXXXXXXXXXXXX.          ",
                "        .XXXXXX.  .XXXXXX.          ",
                "        .XXXXXX.  .XXXXXX.          ",
                "       .XXXXXXXX..XXXXXXXX.         ",
                "       .........  .........         "
            };

            var pal = new Dictionary<char, Color>
            {
                { '.', new Color(0.08f, 0.06f, 0.04f) },
                { 'X', new Color(0.40f, 0.30f, 0.18f) }, // Manto sombra
                { 'O', new Color(0.65f, 0.50f, 0.28f) }, // Manto medio
                { '@', new Color(0.85f, 0.70f, 0.42f) }, // Manto claro
                { '#', new Color(0.75f, 0.55f, 0.15f) }, // Oro real
                { '$', new Color(1f, 0.82f, 0.25f) },    // Oro brillante de la mÃ¡scara
                { '-', new Color(0.12f, 0.28f, 0.65f) }, // LapislÃ¡zuli azul real del tocado Nemes
                { '+', new Color(0.85f, 0.15f, 0.20f) }, // Barba postiza y rubÃ­es
                { '!', new Color(1f, 0.10f, 0.15f) }     // Ojos de rubÃ­ resplandeciente
            };

            return CrearSpriteDeMapa(mapa, pal, 32f, 0.15f);
        }
    }
}