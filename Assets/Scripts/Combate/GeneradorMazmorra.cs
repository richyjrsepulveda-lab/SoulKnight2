using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoBioma
    {
        Piedra = 0,
        Hielo = 1,
        Fuego = 2,
        Magma = 2, // Alias de compatibilidad
        Planta = 3,
        Arena = 4
    }

    [System.Serializable]
    public class DatosBioma
    {
        public TipoBioma tipo;
        public string nombreBioma;
        public string prefijo;
        public Sprite spriteSuelo;
        public Sprite spriteMuro;
        public Sprite spriteColumna;
        public Color colorFondoCamara;
        public Color colorLuzAntorcha = new Color(1f, 0.72f, 0.28f, 1f);
        public Sprite spriteLlamaAntorcha;

        // Sprites oficiales de mazmorra (16x16, 16 PPU)
        public Sprite[] spritesSuelo;          // floor_00 .. 07
        public Sprite spriteFloorShadow00;     // floor_shadow_00
        public Sprite spriteFloorShadow01;     // floor_shadow_01
        public Sprite spriteVoid;              // void
        public Sprite[] spritesWallTop;        // wall_top_00 .. 15 (autotile bitmask)
        public Sprite spriteWallFaceA;         // wall_face_a
        public Sprite spriteWallFaceB;         // wall_face_b
        public Sprite spriteWallFaceCracked;   // wall_face_cracked
        public Sprite spriteWallFaceDecor;     // wall_face_decor / grate
        public Sprite spriteWallFaceBanner;    // wall_face_banner
        public Sprite[] framesWallFaceTorch;   // wall_face_torch (4 frames a 8 fps)
        public Sprite spriteTorchGlow;         // halo ambiental torch_glow
        public Sprite[] spritesDecals;         // stone_decal_crack, pebbles, bones, puddle
        public Sprite[] spritesBlock;          // stone_block_00 .. 15 (autotile bitmask)

        // Props y Obstáculos del bioma
        public Sprite spritePillar;            // pillar (16x32)
        public Sprite spriteBoulderS;          // boulder_s (16x16)
        public Sprite spriteBoulderL;          // boulder_l (32x32)
        public Sprite spriteCrate;             // crate (16x16)
        public Sprite spriteBarrel;            // barrel (16x16)
        public Sprite spriteBarrelRed;         // barrel_red (16x16 explosivo)
        public Sprite spritePot;               // pot (16x16 rompible con drop de oro)
        public Sprite spriteSpikesOff;         // spikes_off
        public Sprite spriteSpikesOn;          // spikes_on
        public Sprite[] framesSpikesStrip;     // spikes_strip (2 o 4 frames)
        public Sprite[] framesWaterStrip;      // water_strip (2 frames a 2 fps)
        public Sprite spritePit;               // pit (16x16 foso intransitable, balas pasan)
        public Sprite[] framesHazardStrip;     // pit (1 frame) o poison/icewater/lava/quicksand (4 frames, 5 fps)
        public Sprite spriteSpecialProp;       // bones, thornbush, crystal, brazier, cactus
        public Sprite[] framesSpecialAnim;     // animación brazier si aplica
    }

    /// <summary>
    /// Generador procedural de mazmorras top-down 2D.
    /// Utiliza el conjunto de sprites oficiales de 5 biomas (stone, ice, fire, plant, desert)
    /// con autotile para muros, cara frontal, sombras, vacío, props y trampas según LEEME.md.
    /// </summary>
    public class GeneradorMazmorra : MonoBehaviour
    {
        public static GeneradorMazmorra Instancia { get; private set; }

        [Header("Configuración de Biomas")]
        public TipoBioma biomaActual = TipoBioma.Piedra;
        public bool biomaAleatorio = true;
        public bool biomasPorSala = false; // Mantiene el bioma unificado según la recámara del nivel
        public DatosBioma[] biomas;

        [Header("Sprites Especiales")]
        public Sprite spritePortal;
        public Sprite spriteHoyitoPared;
        public Sprite spriteHoyitoParedCerrado;

        [Header("Contenedor")]
        public Transform contenedorMapa;

        [HideInInspector]
        public List<Vector2> puntosSpawnJugadores = new List<Vector2>();
        [HideInInspector]
        public List<Vector2> puntosSpawnEnemigos = new List<Vector2>();
        [HideInInspector]
        public Vector2 puntoSpawnJefe = Vector2.zero;
        [HideInInspector]
        public Vector2 posicionPortal = Vector2.zero;

        [HideInInspector]
        public List<ControladorSala> salasGeneradas = new List<ControladorSala>();
        [HideInInspector]
        public List<Rect> rectPasillos = new List<Rect>();

        private readonly List<SpriteRenderer> _murosVisuales = new List<SpriteRenderer>();

        [HideInInspector]
        public int gridMinX, gridMaxX, gridMinY, gridMaxY;
        [HideInInspector]
        public byte[,] gridActual;

        public event Action<DatosBioma> AlGenerarMazmorra;

        private int _ultimaSemilla = -1;
        private TipoBioma _ultimoBioma = (TipoBioma)(-1);

        private static Sprite _spriteCirculoLuz;
        private static Sprite _spriteAntorchaBase;
        private static Sprite _spriteHaloLuzCalida;
        private static Sprite _spritePortalInvocacion;

        void Awake()
        {
            Instancia = this;
            SistemaVision.AsegurarInstancia();
        }

        void Start()
        {
            SistemaVision.AsegurarInstancia();
            if (SalaRed.Instancia != null && SalaRed.Instancia.SemillaMapaActual.Value != 0)
            {
                bool tieneJefe = SalaRed.Instancia.TieneJefeActual.Value;
                GenerarConSemilla(SalaRed.Instancia.SemillaMapaActual.Value, (TipoBioma)SalaRed.Instancia.BiomaMapaActual.Value, tieneJefe);
            }
        }

        public void Generar()
        {
            int semilla = UnityEngine.Random.Range(1000, 999999);
            TipoBioma bioma = biomaActual;
            if (biomaAleatorio && biomas != null && biomas.Length > 0)
            {
                bioma = biomas[UnityEngine.Random.Range(0, biomas.Length)].tipo;
            }
            GenerarConSemilla(semilla, bioma);
        }

        public void GenerarConSemilla(int semilla, TipoBioma bioma, bool tieneJefe = true, bool esNivelDecisionLoop = false)
        {
            SistemaVision.AsegurarInstancia();
            _ultimaSemilla = semilla;
            _ultimoBioma = bioma;

            UnityEngine.Random.InitState(semilla);
            biomaActual = bioma;
            DatosBioma datos = ObtenerDatosBioma(biomaActual);

            var cam = CamaraUtil.Principal;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            LimpiarMapaExistente();

            if (esNivelDecisionLoop)
            {
                ConstruirSantuarioDecisionLoop(datos);
            }
            else
            {
                ConstruirMazmorra(datos, tieneJefe);
            }

            AlGenerarMazmorra?.Invoke(datos);
        }

        public DatosBioma ObtenerDatosBioma(TipoBioma tipo)
        {
            var cat = CatalogoBiomas.Obtener(tipo);
            if (cat != null && cat.spriteSuelo != null) return cat;

            if (biomas == null) return null;
            foreach (var b in biomas)
            {
                if (b.tipo == tipo) return b;
            }
            return biomas.Length > 0 ? biomas[0] : null;
        }

        private void LimpiarMapaExistente()
        {
            PuestitoTienda.DestruirTodos();
            CofreOro.DestruirTodos();
            MiniCofre.DestruirTodos();
            MonedaOro.DestruirTodas();
            puntosSpawnJugadores.Clear();
            puntosSpawnEnemigos.Clear();
            salasGeneradas.Clear();
            rectPasillos.Clear();
            _murosVisuales.Clear();
            puntoSpawnJefe = Vector2.zero;

            if (contenedorMapa != null)
            {
                for (int i = contenedorMapa.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(contenedorMapa.GetChild(i).gameObject);
                }
            }
            else
            {
                var go = new GameObject("ContenedorMapaProcedural");
                contenedorMapa = go.transform;
            }
        }

        public struct SalaInfo
        {
            public int xMin, xMax, yMin, yMax;
            public int cx, cy;
            public int numColumnas;
            public int numEnemigos;
            public bool esSalaInicio;
            public bool esSalaJefe;
            public TipoBioma bioma;
        }

        private struct PasilloInfo
        {
            public int x1, y1, x2, y2;
            public TipoBioma bioma;
        }

        private void ConstruirMazmorra(DatosBioma datosNivel, bool tieneJefe = true)
        {
            if (datosNivel == null) return;

            List<SalaInfo> salas = new List<SalaInfo>();
            List<PasilloInfo> pasillos = new List<PasilloInfo>();

            TipoBioma[] biomasDisponibles = new TipoBioma[]
            {
                TipoBioma.Piedra,
                TipoBioma.Planta,
                TipoBioma.Hielo,
                TipoBioma.Fuego,
                TipoBioma.Arena
            };

            // Sala 0: Entrada segura
            TipoBioma biomaInicio = biomasPorSala ? biomaActual : datosNivel.tipo;
            salas.Add(CrearSala(0, 0, 26, 20, 0, 0, esInicio: true, esJefe: false, bioma: biomaInicio));

            puntosSpawnJugadores.Add(new Vector2(-4.0f, 2.0f));
            puntosSpawnJugadores.Add(new Vector2(-4.0f, -2.0f));
            puntosSpawnJugadores.Add(new Vector2(-7.0f, 2.0f));
            puntosSpawnJugadores.Add(new Vector2(-7.0f, -2.0f));

            int totalSalasCaminoPrincipal = 5;
            int currX = 0;
            int currY = 0;

            for (int k = 1; k < totalSalasCaminoPrincipal; k++)
            {
                bool esJefe = tieneJefe && (k == totalSalasCaminoPrincipal - 1);
                int ancho = esJefe ? 42 : UnityEngine.Random.Range(32, 38);
                int alto = esJefe ? 32 : UnityEngine.Random.Range(24, 30);
                ancho = (ancho / 2) * 2;
                alto = (alto / 2) * 2;
                int numCols = esJefe ? 4 : (UnityEngine.Random.Range(2, 4) * 2);
                int numEnems = esJefe ? 0 : UnityEngine.Random.Range(5, 8);

                int chance = UnityEngine.Random.Range(0, 100);
                int stepX = 52;
                int stepY = 0;

                if (esJefe)
                {
                    stepX = 54;
                    stepY = 0;
                }
                else if (chance < 45)
                {
                    stepX = 52;
                    stepY = 0;
                }
                else if (chance < 72)
                {
                    stepX = 0;
                    stepY = 44;
                }
                else
                {
                    stepX = 0;
                    stepY = -44;
                }

                int candX = currX + stepX;
                int candY = currY + stepY;

                bool colisiona = false;
                foreach (var s in salas)
                {
                    if (Mathf.Abs(candX - s.cx) < (ancho + s.xMax - s.xMin) * 0.5f + 10 &&
                        Mathf.Abs(candY - s.cy) < (alto + s.yMax - s.yMin) * 0.5f + 10)
                    {
                        colisiona = true;
                        break;
                    }
                }

                if (colisiona)
                {
                    candX = currX + 54;
                    candY = currY;
                }

                currX = candX;
                currY = candY;

                TipoBioma biomaSala = biomasPorSala ? biomasDisponibles[k % biomasDisponibles.Length] : datosNivel.tipo;
                salas.Add(CrearSala(currX, currY, ancho, alto, numCols, numEnems, esInicio: false, esJefe: esJefe, bioma: biomaSala));

                pasillos.Add(new PasilloInfo
                {
                    x1 = salas[k - 1].cx,
                    y1 = salas[k - 1].cy,
                    x2 = salas[k].cx,
                    y2 = salas[k].cy,
                    bioma = salas[k - 1].bioma
                });
            }

            // Sala Final (Sala del Jefe) y Pasillito Posterior con el Hoyo
            var salaFinal = salas[totalSalasCaminoPrincipal - 1];
            puntoSpawnJefe = new Vector2(salaFinal.cx, salaFinal.cy);

            int pasilloJefeXMin = salaFinal.xMax + 1;
            int pasilloJefeXMax = salaFinal.xMax + 12;
            int pasilloJefeY = salaFinal.cy;
            posicionPortal = new Vector2(pasilloJefeXMax - 1.2f, pasilloJefeY + 0.5f);

            // Cuartos Extra sin salida (tienda y tesoro)
            Vector2Int[] dirsRamificacion = new Vector2Int[]
            {
                new Vector2Int(0, 44),
                new Vector2Int(0, -44),
                new Vector2Int(48, 0),
                new Vector2Int(-48, 0)
            };

            for (int idxPadre = 1; idxPadre <= 3; idxPadre++)
            {
                var padre = salas[idxPadre];
                int anchoExtra = UnityEngine.Random.Range(28, 34);
                int altoExtra = UnityEngine.Random.Range(22, 28);
                anchoExtra = (anchoExtra / 2) * 2;
                altoExtra = (altoExtra / 2) * 2;
                int numColsExtra = (UnityEngine.Random.Range(1, 3) * 2);

                int offsetInicial = UnityEngine.Random.Range(0, dirsRamificacion.Length);
                for (int d = 0; d < dirsRamificacion.Length; d++)
                {
                    Vector2Int offset = dirsRamificacion[(offsetInicial + d) % dirsRamificacion.Length];
                    int candX = padre.cx + offset.x;
                    int candY = padre.cy + offset.y;

                    if (candX + anchoExtra / 2 > salaFinal.xMin - 6 && Mathf.Abs(candY - salaFinal.cy) < 24)
                        continue;

                    bool colisiona = false;
                    foreach (var s in salas)
                    {
                        if (Mathf.Abs(candX - s.cx) < (anchoExtra + s.xMax - s.xMin) * 0.5f + 10 &&
                            Mathf.Abs(candY - s.cy) < (altoExtra + s.yMax - s.yMin) * 0.5f + 10)
                        {
                            colisiona = true;
                            break;
                        }
                    }

                    if (!colisiona)
                    {
                        TipoBioma biomaExtra = biomasPorSala ? biomasDisponibles[(idxPadre + 2) % biomasDisponibles.Length] : datosNivel.tipo;
                        var cuartoExtra = CrearSala(candX, candY, anchoExtra, altoExtra, numColsExtra, 7, esInicio: false, esJefe: false, bioma: biomaExtra);
                        salas.Add(cuartoExtra);
                        pasillos.Add(new PasilloInfo
                        {
                            x1 = padre.cx,
                            y1 = padre.cy,
                            x2 = cuartoExtra.cx,
                            y2 = cuartoExtra.cy,
                            bioma = padre.bioma
                        });
                        break;
                    }
                }
            }

            // Dimensiones dinámicas de la cuadrícula
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var s in salas)
            {
                if (s.xMin < minX) minX = s.xMin;
                if (s.xMax > maxX) maxX = s.xMax;
                if (s.yMin < minY) minY = s.yMin;
                if (s.yMax > maxY) maxY = s.yMax;
            }
            if (pasilloJefeXMax > maxX) maxX = pasilloJefeXMax;

            minX -= 6;
            maxX += 6;
            minY -= 6;
            maxY += 6;

            gridMinX = minX;
            gridMaxX = maxX;
            gridMinY = minY;
            gridMaxY = maxY;

            int gridW = maxX - minX + 1;
            int gridH = maxY - minY + 1;

            byte[,] grid = new byte[gridW, gridH]; // 0 = Nada/Void, 1 = Suelo, 2 = Muro Top, 3 = Muro Cara Frontal
            gridActual = grid;

            // Matriz para saber a qué bioma pertenece cada celda
            TipoBioma[,] gridBioma = new TipoBioma[gridW, gridH];

            // 1. Marcar Suelos de Salas
            foreach (var s in salas)
            {
                for (int x = s.xMin; x <= s.xMax; x++)
                {
                    for (int y = s.yMin; y <= s.yMax; y++)
                    {
                        int gx = x - minX;
                        int gy = y - minY;
                        if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                        {
                            grid[gx, gy] = 1;
                            gridBioma[gx, gy] = s.bioma;
                        }
                    }
                }
            }

            // 2. Marcar Suelos de Pasillos (ancho de 4 tiles)
            const int radioPasillo = 2;
            foreach (var p in pasillos)
            {
                int xDesde = Mathf.Min(p.x1, p.x2);
                int xHasta = Mathf.Max(p.x1, p.x2);
                for (int x = xDesde; x <= xHasta; x++)
                {
                    for (int dy = -radioPasillo + 1; dy <= radioPasillo; dy++)
                    {
                        int y = p.y1 + dy;
                        int gx = x - minX;
                        int gy = y - minY;
                        if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                        {
                            grid[gx, gy] = 1;
                            gridBioma[gx, gy] = p.bioma;
                        }
                    }
                }

                int yDesde = Mathf.Min(p.y1, p.y2);
                int yHasta = Mathf.Max(p.y1, p.y2);
                for (int y = yDesde; y <= yHasta; y++)
                {
                    for (int dx = -radioPasillo + 1; dx <= radioPasillo; dx++)
                    {
                        int x = p.x2 + dx;
                        int gx = x - minX;
                        int gy = y - minY;
                        if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                        {
                            grid[gx, gy] = 1;
                            gridBioma[gx, gy] = p.bioma;
                        }
                    }
                }
            }

            // 2b. Marcar Suelo del Pasillito Final tras el Jefe
            for (int x = pasilloJefeXMin; x <= pasilloJefeXMax; x++)
            {
                for (int dy = -radioPasillo + 1; dy <= radioPasillo; dy++)
                {
                    int y = pasilloJefeY + dy;
                    int gx = x - minX;
                    int gy = y - minY;
                    if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                    {
                        grid[gx, gy] = 1;
                        gridBioma[gx, gy] = salaFinal.bioma;
                    }
                }
            }

            // 3. Determinar Muros Perimetrales (grid = 2)
            int[] dx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy8 = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int gx = 0; gx < gridW; gx++)
            {
                for (int gy = 0; gy < gridH; gy++)
                {
                    if (grid[gx, gy] == 0)
                    {
                        bool tieneVecinoSuelo = false;
                        TipoBioma biomaVecino = datosNivel.tipo;
                        for (int k = 0; k < 8; k++)
                        {
                            int nx = gx + dx8[k];
                            int ny = gy + dy8[k];
                            if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH && grid[nx, ny] == 1)
                            {
                                tieneVecinoSuelo = true;
                                biomaVecino = gridBioma[nx, ny];
                                break;
                            }
                        }
                        if (tieneVecinoSuelo)
                        {
                            grid[gx, gy] = 2; // Muro perimetral
                            gridBioma[gx, gy] = biomaVecino;
                        }
                    }
                }
            }

            // 4. Determinar Cara Frontal de Muro (grid = 3):
            // Según LEEME.md regla 3: se coloca en la celda de suelo justo debajo de una celda de muro (se ve la pared de frente).
            // Es sólida y bloquea movimiento.
            for (int gx = 0; gx < gridW; gx++)
            {
                for (int gy = 0; gy < gridH; gy++)
                {
                    if (grid[gx, gy] == 1) // Es celda de suelo
                    {
                        // Si la celda justo encima (gy + 1) es muro top
                        if (gy + 1 < gridH && grid[gx, gy + 1] == 2)
                        {
                            grid[gx, gy] = 3; // Cara frontal de muro (wall_face)
                        }
                    }
                }
            }

            // 5. Instanciar Suelos, Sombras y Caras Frontales
            for (int gy = 0; gy < gridH; gy++)
            {
                for (int gx = 0; gx < gridW; gx++)
                {
                    byte tipoCelda = grid[gx, gy];
                    if (tipoCelda == 0) continue;

                    TipoBioma biomaCelda = gridBioma[gx, gy];
                    DatosBioma d = ObtenerDatosBioma(biomaCelda);
                    if (d == null) d = datosNivel;

                    float wx = gx + minX;
                    float wy = gy + minY;
                    Vector2 pos = new Vector2(wx, wy);

                    if (tipoCelda == 1)
                    {
                        // Suelo transitable: verificar si está bajo la sombra de un wall_face
                        // Sombra 00: justo debajo del wall_face (gy + 1 es wall_face)
                        // Sombra 01: en la siguiente fila (gy + 2 es wall_face)
                        bool bajoSombra00 = (gy + 1 < gridH && grid[gx, gy + 1] == 3);
                        bool bajoSombra01 = (!bajoSombra00 && gy + 2 < gridH && grid[gx, gy + 2] == 3);

                        Sprite spSuelo;
                        if (bajoSombra00 && d.spriteFloorShadow00 != null)
                        {
                            spSuelo = d.spriteFloorShadow00;
                        }
                        else if (bajoSombra01 && d.spriteFloorShadow01 != null)
                        {
                            spSuelo = d.spriteFloorShadow01;
                        }
                        else
                        {
                            // Regla LEEME.md: floor_00..03 base (~85%), floor_04..07 detalles (~15%)
                            if (d.spritesSuelo != null && d.spritesSuelo.Length >= 8)
                            {
                                if (UnityEngine.Random.value < 0.85f)
                                    spSuelo = d.spritesSuelo[UnityEngine.Random.Range(0, 4)];
                                else
                                    spSuelo = d.spritesSuelo[UnityEngine.Random.Range(4, 8)];
                            }
                            else
                            {
                                spSuelo = d.spriteSuelo;
                            }
                        }

                        InstanciarTileSuelo(pos, spSuelo, d);
                    }
                    else if (tipoCelda == 3)
                    {
                        // Cara Frontal del Muro (wall_face)
                        // Regla LEEME.md: sólida, mezcla a/b, cracked/decor/banner ocasional y antorcha cada 4 tiles
                        bool esAntorcha = ((gx % 4) == 0);
                        InstanciarTileWallFace(pos, d, esAntorcha);
                    }
                }
            }

            // 6. Instanciar Muros Top con Autotile (16 variantes bitmask)
            for (int gy = 0; gy < gridH; gy++)
            {
                for (int gx = 0; gx < gridW; gx++)
                {
                    if (grid[gx, gy] == 2)
                    {
                        TipoBioma biomaCelda = gridBioma[gx, gy];
                        DatosBioma d = ObtenerDatosBioma(biomaCelda);
                        if (d == null) d = datosNivel;

                        // Bitmask autotile LEEME.md: mask = N*1 + E*2 + S*4 + W*8
                        int mask = 0;
                        if (gy + 1 < gridH && grid[gx, gy + 1] == 2) mask |= 1; // N
                        if (gx + 1 < gridW && grid[gx + 1, gy] == 2) mask |= 2; // E
                        if (gy - 1 >= 0 && grid[gx, gy - 1] == 2) mask |= 4;    // S
                        if (gx - 1 >= 0 && grid[gx - 1, gy] == 2) mask |= 8;    // W

                        Sprite spWallTop = null;
                        if (d.spritesWallTop != null && mask >= 0 && mask < d.spritesWallTop.Length)
                        {
                            spWallTop = d.spritesWallTop[mask];
                        }
                        if (spWallTop == null) spWallTop = d.spriteMuro;

                        float wx = gx + minX;
                        float wy = gy + minY;
                        InstanciarTileWallTop(new Vector2(wx, wy), spWallTop);
                    }
                }
            }

            // 7. Instanciar Fondo Vacío (void) en los alrededores
            InstanciarFondoVoid(grid, gridW, gridH, minX, minY, datosNivel);

            // Registrar rectángulos de pasillos para consultas lógicas
            foreach (var p in pasillos)
            {
                int xDesde = Mathf.Min(p.x1, p.x2);
                int xHasta = Mathf.Max(p.x1, p.x2);
                int yDesde = Mathf.Min(p.y1, p.y2);
                int yHasta = Mathf.Max(p.y1, p.y2);

                if (xDesde != xHasta)
                {
                    rectPasillos.Add(new Rect(xDesde, p.y1 - radioPasillo + 1, xHasta - xDesde + 1, radioPasillo * 2));
                }
                if (yDesde != yHasta)
                {
                    rectPasillos.Add(new Rect(p.x2 - radioPasillo + 1, yDesde, radioPasillo * 2, yHasta - yDesde + 1));
                }
            }
            rectPasillos.Add(new Rect(pasilloJefeXMin, pasilloJefeY - radioPasillo + 1, pasilloJefeXMax - pasilloJefeXMin + 1, radioPasillo * 2));

            // 8. Instanciar Controladores de Sala, Props, Puertas y Encuentros
            AsegurarSpritesAntorcha();

            int indiceSalaTienda = -1;
            if (salas.Count > totalSalasCaminoPrincipal)
            {
                indiceSalaTienda = totalSalasCaminoPrincipal;
            }

            for (int idx = 0; idx < salas.Count; idx++)
            {
                var s = salas[idx];
                DatosBioma dSala = ObtenerDatosBioma(s.bioma) ?? datosNivel;

                bool esInicio = s.esSalaInicio;
                bool esSalaFinal = (idx == totalSalasCaminoPrincipal - 1);
                bool esJefe = esSalaFinal && tieneJefe;
                bool esTienda = (idx == indiceSalaTienda && !esInicio && !esSalaFinal);
                bool esCofre = (idx > totalSalasCaminoPrincipal && idx != indiceSalaTienda && !esInicio && !esSalaFinal);

                var salaObj = new GameObject($"Sala_{idx}_{dSala.prefijo}");
                salaObj.transform.SetParent(contenedorMapa);
                salaObj.transform.position = new Vector3(s.cx, s.cy, 0f);

                var ctrl = salaObj.AddComponent<ControladorSala>();
                ctrl.Inicializar(idx, s.xMin, s.xMax, s.yMin, s.yMax, esInicio, esSalaFinal, esTienda, esCofre, s.bioma);
                salasGeneradas.Add(ctrl);

                if (esTienda)
                {
                    PuestitoTienda.CrearPuestito(new Vector2(s.cx, s.cy), salaObj.transform);
                }
                else if (esCofre)
                {
                    CofreOro.CrearCofre(new Vector2(s.cx, s.cy), salaObj.transform);
                }

                // Antorchas con halos de luz en la sala
                var posAntorchas = new List<Vector2>
                {
                    new Vector2(s.xMin + 2.5f, s.yMax - 2.5f),
                    new Vector2(s.xMax - 2.5f, s.yMax - 2.5f),
                    new Vector2(s.xMin + 2.5f, s.yMin + 2.5f),
                    new Vector2(s.xMax - 2.5f, s.yMin + 2.5f)
                };

                if (s.xMax - s.xMin >= 28)
                {
                    posAntorchas.Add(new Vector2(s.cx, s.yMax - 2.5f));
                    posAntorchas.Add(new Vector2(s.cx, s.yMin + 2.5f));
                }

                if (dSala.tipo != TipoBioma.Piedra)
                {
                    foreach (var pa in posAntorchas)
                    {
                        CrearAntorchaEnSala(ctrl, pa, dSala);
                    }
                }

                // Detectar puertas de bloqueo en el perímetro
                CrearPuertasDeBloqueoSala(ctrl, s, grid, minX, minY, gridW, gridH, dSala.spriteMuro);

                // Muro de salida de la sala final
                if (idx == totalSalasCaminoPrincipal - 1)
                {
                    var muroSalida = CrearPuertaBloqueoVisual(
                        new Vector2(salaFinal.xMax + 0.5f, pasilloJefeY + 0.5f),
                        new Vector2(1.1f, radioPasillo * 2f + 0.5f),
                        dSala.spriteMuro,
                        new Color(0.75f, 0.25f, 0.25f, 1f)
                    );
                    ctrl.AsignarMuroSalidaJefe(muroSalida);
                }

                // Props y Obstáculos oficiales del bioma
                if (!esTienda)
                {
                    InstanciarPropsSala(salaObj.transform, s, dSala, esCofre);
                }

                // Portales y oleadas de enemigos
                if (!esInicio && !esTienda)
                {
                    ConfigurarOleadasSala(ctrl, s, esSalaFinal, esJefe);
                }
            }
        }

        private void InstanciarTileSuelo(Vector2 pos, Sprite spriteSuelo, DatosBioma d = null)
        {
            var go = new GameObject("Suelo");
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteSuelo;
            sr.sortingOrder = -1;

            // Decoración viva: calcomanías esparcidas orgánicamente (grietas, guijarros, huesos, charcos)
            if (d != null && d.spritesDecals != null && d.spritesDecals.Length > 0)
            {
                // No sobreponer calcomanías en las sombras proyectadas por la pared norte
                bool esFilaSombra = (spriteSuelo == d.spriteFloorShadow00 || spriteSuelo == d.spriteFloorShadow01);
                if (!esFilaSombra && UnityEngine.Random.value < 0.15f)
                {
                    int decalIdx = UnityEngine.Random.Range(0, d.spritesDecals.Length);
                    var decalObj = new GameObject("DecalSuelo");
                    decalObj.transform.SetParent(go.transform, false);
                    var srDecal = decalObj.AddComponent<SpriteRenderer>();
                    srDecal.sprite = d.spritesDecals[decalIdx];
                    srDecal.sortingOrder = 0;
                    srDecal.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.70f, 0.95f));
                }
            }
        }

        private void InstanciarTileWallTop(Vector2 pos, Sprite spriteWallTop)
        {
            var go = new GameObject("WallTop");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteWallTop;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            _murosVisuales.Add(sr);
        }

        private void InstanciarTileWallFace(Vector2 pos, DatosBioma d, bool esAntorcha)
        {
            var go = new GameObject("WallFace");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            _murosVisuales.Add(sr);

            if (esAntorcha && d.framesWallFaceTorch != null && d.framesWallFaceTorch.Length > 0 && d.framesWallFaceTorch[0] != null)
            {
                var anim = go.AddComponent<AnimadorSpriteTile>();
                anim.Configurar(sr, d.framesWallFaceTorch, 8f);

                // Halo de iluminación suave (usa stone_torch_glow centrado en la llama a escala 1:1 fija)
                var haloObj = new GameObject("HaloLuzPared");
                haloObj.transform.SetParent(go.transform, false);
                haloObj.transform.localPosition = new Vector3(0f, 0.16f, 0f); // Centrado en la llama según manifest
                haloObj.transform.localScale = Vector3.one;
                var srHalo = haloObj.AddComponent<SpriteRenderer>();
                srHalo.sprite = d.spriteTorchGlow ?? _spriteHaloLuzCalida;
                srHalo.color = Color.white; // El sprite ya contiene su gradiente suave y color cálido
                srHalo.sortingOrder = 3;
            }
            else
            {
                // Mezcla a/b, cracked, grate y banner
                float r = UnityEngine.Random.value;
                if (r < 0.08f && d.spriteWallFaceBanner != null)
                    sr.sprite = d.spriteWallFaceBanner;
                else if (r < 0.16f && d.spriteWallFaceCracked != null)
                    sr.sprite = d.spriteWallFaceCracked;
                else if (r < 0.26f && d.spriteWallFaceDecor != null)
                    sr.sprite = d.spriteWallFaceDecor;
                else if (r < 0.62f && d.spriteWallFaceB != null)
                    sr.sprite = d.spriteWallFaceB;
                else
                    sr.sprite = d.spriteWallFaceA ?? d.spriteMuro;
            }
        }

        private void InstanciarFondoVoid(byte[,] grid, int gridW, int gridH, int minX, int minY, DatosBioma dDefecto)
        {
            // Omitido: sin borde exterior de void para mantener la pared limpia contra el fondo de cámara
            return;
        }

        private void InstanciarFondoVoidAntiguo(byte[,] grid, int gridW, int gridH, int minX, int minY, DatosBioma dDefecto)
        {
            if (dDefecto == null || dDefecto.spriteVoid == null) return;

            var contenedorVoid = new GameObject("FondoVoid");
            contenedorVoid.transform.SetParent(contenedorMapa);

            for (int gx = 0; gx < gridW; gx++)
            {
                for (int gy = 0; gy < gridH; gy++)
                {
                    if (grid[gx, gy] == 0)
                    {
                        // Solo instanciar void en un borde cercano a los muros o suelos (distancia <= 3)
                        bool cerca = false;
                        for (int dy = -3; dy <= 3 && !cerca; dy++)
                        {
                            for (int dx = -3; dx <= 3 && !cerca; dx++)
                            {
                                int nx = gx + dx;
                                int ny = gy + dy;
                                if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH && grid[nx, ny] != 0)
                                {
                                    cerca = true;
                                }
                            }
                        }

                        if (cerca)
                        {
                            var vObj = new GameObject("Void");
                            vObj.transform.SetParent(contenedorVoid.transform);
                            vObj.transform.position = new Vector3(gx + minX, gy + minY, 0f);
                            var sr = vObj.AddComponent<SpriteRenderer>();
                            sr.sprite = dDefecto.spriteVoid;
                            sr.sortingOrder = -2;
                        }
                    }
                }
            }
        }

        public int layoutPiedraForzado = -1;

        public static readonly string[][] PlantillasLayoutsStone = new string[][]
        {
            // 0: La Bóveda Acorazada (Muros de 1 solo tile, mini cofre m y pinchos en diamante)
            new string[] {
                "...P..........P...",
                "...s..........s...",
                "..sss...D....sss..",
                "...s....c.....s...",
                ".......m..c.......",
                ".......c..p.......",
                "...s.....c....s...",
                "..sss....D...sss..",
                "...s..........s...",
                "...P..........P..."
            },
            // 1: Los Cuatro Bastiones (1 columna en cada esquina, núcleo explosivo protegido por muro destructible D)
            new string[] {
                ".P...s......s...P.",
                "...sss......sss...",
                ".....s......s.....",
                ".s..............s.",
                "sss....Dr.rD...sss",
                "sss....Dr.rD...sss",
                ".s..............s.",
                ".....s......s.....",
                "...sss......sss...",
                ".P...s......s...P."
            },
            // 2: El Tablero de Ajedrez Rítmico (Franjas seguras, muros de 1 tile de grosor y mini cofre)
            new string[] {
                ".#..............#.",
                "......s.ss.s......",
                ".....s.s..s.s.....",
                "..D...s.ss.s...D..",
                "..D....cp.pm...D..",
                "..D....pp.pc...D..",
                "..D...s.ss.s...D..",
                ".....s.s..s.s.....",
                "......s.ss.s......",
                ".#..............#."
            },
            // 3: El Laberinto en Cruz (Paredes de 1 solo tile # y D con aspas de trampas)
            new string[] {
                "....#...ss...#....",
                "....#...ss...#....",
                "....#........#....",
                ".ss.D........D.ss.",
                ".ss.D...cc...D.ss.",
                ".ss.D...cc...D.ss.",
                ".ss.D........D.ss.",
                "....#........#....",
                "....#...ss...#....",
                "....#...ss...#...."
            },
            // 4: La Arena de Foso y Columnas (Foso central, columnas P de 1 tile, alas de picos y barriles)
            new string[] {
                "...P..........P...",
                ".#..............#.",
                ".......ssss.......",
                ".......s..s.......",
                ".Db....oooo....bD.",
                ".Db....oooo....bD.",
                ".......s..s.......",
                ".......ssss.......",
                ".#..............#.",
                "...P..........P..."
            },
            // 5: El Estanque Sagrado (Agua poco profunda transitable con islote central de jarrones y mini cofre)
            new string[] {
                ".....wwwwwwww.....",
                "...wwwwwwwwwwww...",
                "..ww....ss....ww..",
                ".ww.....ss.....ww.",
                ".w....s.pm.s....w.",
                ".w....s.pp.s....w.",
                ".ww.....ss.....ww.",
                "..ww....ss....ww..",
                "...wwwwwwwwwwww...",
                ".....wwwwwwww....."
            },
            // 6: La Cámara del Búnker Central (Muros de 1 solo tile de grosor # con puertas D y foso de pinchos)
            new string[] {
                ".......ssss.......",
                "...s...ssss...s...",
                "...s..######..s...",
                "..ss..#....#..ss..",
                ".sss..D.rr.D..sss.",
                ".sss..D.rr.D..sss.",
                "..ss..#....#..ss..",
                "...s..######..s...",
                "...s...ssss...s...",
                ".......ssss......."
            },
            // 7: Las Alas del Fénix (Simetría en diamantes de pinchos, columnas P y muros rompibles D de 1 tile)
            new string[] {
                ".P..............P.",
                ".....ss....ss.....",
                "....ssss..ssss....",
                ".....ss....ss.....",
                ".D..............D.",
                ".D.....PPPP.....D.",
                ".....ss....ss.....",
                "....ssss..ssss....",
                ".....ss....ss.....",
                ".P..............P."
            },
            // 8: El Pasadizo Secreto de Cajas (Guantelete de 1 solo tile de grosor con nichos D y mini cofre m)
            new string[] {
                ".P..............P.",
                "..#.sss....sss.#..",
                "...#.s......s.#...",
                ".D.#.s..cc..s.#.D.",
                ".D......cm......D.",
                ".D......cc......D.",
                ".D.#.s..cc..s.#.D.",
                "...#.s......s.#...",
                "..#.sss....sss.#..",
                ".P..............P."
            },
            // 9: El Coliseo de Piedra (Paredes de 1 tile de grosor #, atajos destructibles D y cruz de pinchos)
            new string[] {
                ".#......ss......#.",
                ".......ssss.......",
                "........ss........",
                ".ss.D........D.ss.",
                ".ss.D...rr...D.ss.",
                ".ss.D...rr...D.ss.",
                ".ss.D........D.ss.",
                "........ss........",
                ".......ssss.......",
                ".#......ss......#."
            }
        };

        private void InstanciarPropsSala(Transform padre, SalaInfo s, DatosBioma d, bool esCofre = false)
        {
            if (d == null) return;

            // En mazmorra de piedra (excepto la sala inicial), instanciar layouts de room_layouts.json
            if (d.tipo == TipoBioma.Piedra && !s.esSalaInicio)
            {
                InstanciarLayoutSalaPiedra(padre, s, d, -1, esCofre);
                return;
            }

            float w = s.xMax - s.xMin + 1;
            float h = s.yMax - s.yMin + 1;
            float sepX = w * 0.28f;
            float sepY = h * 0.24f;

            // 1. Columnas / Pilares (16x32)
            if (d.spritePillar != null && s.numColumnas > 0)
            {
                CrearPillar(padre, new Vector2(s.cx - sepX, s.cy + sepY), d.spritePillar);
                CrearPillar(padre, new Vector2(s.cx + sepX, s.cy + sepY), d.spritePillar);
                if (s.numColumnas >= 4)
                {
                    CrearPillar(padre, new Vector2(s.cx - sepX, s.cy - sepY), d.spritePillar);
                    CrearPillar(padre, new Vector2(s.cx + sepX, s.cy - sepY), d.spritePillar);
                }
            }

            // 2. Rocas / Boulders
            if (d.spriteBoulderL != null)
            {
                CrearBoulder(padre, new Vector2(s.cx - sepX * 0.4f, s.cy - sepY * 0.8f), d.spriteBoulderL, grande: true);
            }
            if (d.spriteBoulderS != null)
            {
                CrearBoulder(padre, new Vector2(s.cx + sepX * 0.6f, s.cy + sepY * 0.8f), d.spriteBoulderS, grande: false);
            }

            // 3. Cajas y Barriles destruibles
            if (d.spriteCrate != null)
            {
                CrearCaja(padre, new Vector2(s.xMin + 3.5f, s.yMin + 3.5f), d.spriteCrate);
                CrearCaja(padre, new Vector2(s.xMin + 4.5f, s.yMin + 3.5f), d.spriteCrate);
            }
            if (d.spriteBarrel != null)
            {
                bool esExplosivo = (d.tipo == TipoBioma.Fuego);
                CrearBarril(padre, new Vector2(s.xMin + 3.5f, s.yMin + 4.5f), d.spriteBarrel, esExplosivo);
            }

            // 4. Trampas de Pinchos (Spikes)
            if (d.spriteSpikesOff != null && d.spriteSpikesOn != null && !s.esSalaInicio)
            {
                CrearTrampaPinchos(padre, new Vector2(s.cx - 1.5f, s.cy - sepY * 0.5f), d.spriteSpikesOff, d.spriteSpikesOn);
                CrearTrampaPinchos(padre, new Vector2(s.cx - 0.5f, s.cy - sepY * 0.5f), d.spriteSpikesOff, d.spriteSpikesOn);
                CrearTrampaPinchos(padre, new Vector2(s.cx + 0.5f, s.cy - sepY * 0.5f), d.spriteSpikesOff, d.spriteSpikesOn);
                CrearTrampaPinchos(padre, new Vector2(s.cx + 1.5f, s.cy - sepY * 0.5f), d.spriteSpikesOff, d.spriteSpikesOn);
            }

            // 5. Zona de Peligro / Hazard Continuo en el Suelo
            if (d.framesHazardStrip != null && d.framesHazardStrip.Length > 0 && d.framesHazardStrip[0] != null && !s.esSalaInicio)
            {
                TipoPeligroTile tipoP = TipoPeligroTile.Pozo;
                switch (d.tipo)
                {
                    case TipoBioma.Planta: tipoP = TipoPeligroTile.Veneno; break;
                    case TipoBioma.Hielo: tipoP = TipoPeligroTile.AguaHelada; break;
                    case TipoBioma.Fuego: tipoP = TipoPeligroTile.Lava; break;
                    case TipoBioma.Arena: tipoP = TipoPeligroTile.ArenasVivas; break;
                }

                Vector2 posHazard = new Vector2(s.cx + sepX * 0.7f, s.cy - sepY * 0.4f);
                for (int hx = 0; hx < 2; hx++)
                {
                    for (int hy = 0; hy < 2; hy++)
                    {
                        CrearTilePeligro(padre, posHazard + new Vector2(hx, hy), d.framesHazardStrip, tipoP);
                    }
                }
            }

            // 6. Prop Especial por Bioma
            if (d.spriteSpecialProp != null && !s.esSalaInicio)
            {
                Vector2 posSpec = new Vector2(s.xMin + 3.0f, s.cy);
                CrearPropEspecial(padre, posSpec, d);
            }
        }

        private void CrearPillar(Transform padre, Vector2 pos, Sprite spritePillar)
        {
            var go = new GameObject("Pillar");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spritePillar;
            sr.sortingOrder = 3;

            // Collider manifest: x=2, y=20, w=12, h=10
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.75f, 0.625f);
            col.offset = new Vector2(0f, -0.4f);

            _murosVisuales.Add(sr);
        }

        private void CrearBoulder(Transform padre, Vector2 pos, Sprite spriteBoulder, bool grande)
        {
            var go = new GameObject(grande ? "BoulderLarge" : "BoulderSmall");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteBoulder;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            if (grande)
            {
                // Manifest boulder_l: w=26, h=18
                col.size = new Vector2(1.625f, 1.125f);
                col.offset = new Vector2(0f, 0f);
            }
            else
            {
                // Manifest boulder_s: w=12, h=8
                col.size = new Vector2(0.75f, 0.5f);
                col.offset = new Vector2(0f, -0.1f);
            }

            _murosVisuales.Add(sr);
        }

        private void CrearCaja(Transform padre, Vector2 pos, Sprite spriteCrate)
        {
            var go = new GameObject("CajaDestruible");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteCrate;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.875f, 0.8125f);

            var prop = go.AddComponent<PropDestruible>();
            prop.vidaMax = 15;
            prop.esExplosivo = false;
        }

        private void CrearBarril(Transform padre, Vector2 pos, Sprite spriteBarrel, bool esExplosivo)
        {
            var go = new GameObject(esExplosivo ? "BarrilExplosivo" : "BarrilDestruible");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteBarrel;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.625f, 0.8125f);

            var prop = go.AddComponent<PropDestruible>();
            prop.tipoProp = esExplosivo ? TipoPropDestruible.BarrilExplosivo : TipoPropDestruible.Barril;
            prop.vidaMax = 12;
            prop.esExplosivo = esExplosivo;
        }

        private void CrearJarron(Transform padre, Vector2 pos, Sprite spJarron)
        {
            var go = new GameObject("JarronDestruible");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spJarron;
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.625f, 0.75f);

            var prop = go.AddComponent<PropDestruible>();
            prop.tipoProp = TipoPropDestruible.Jarron;
            prop.vidaMax = 8;
            prop.esExplosivo = false;
        }

        private void CrearMiniCofre(Transform padre, Vector2 pos)
        {
            var go = new GameObject("MiniCofre");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;
            go.AddComponent<MiniCofre>();
        }

        private void CrearMuroDestructible(Transform padre, Vector2 pos, DatosBioma d)
        {
            var go = new GameObject("MuroDestructible");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            Sprite spBase = (d.spritesBlock != null && d.spritesBlock.Length > 0 && d.spritesBlock[0] != null)
                ? d.spritesBlock[0] : d.spriteMuro;
            sr.sprite = spBase;
            sr.sortingOrder = 2;

            // Grieta visible decorativa en el centro del muro destructible para que el jugador sepa que es rompible
            if (d.spritesDecals != null && d.spritesDecals.Length > 0 && d.spritesDecals[0] != null)
            {
                var crackObj = new GameObject("GrietaMuro");
                crackObj.transform.SetParent(go.transform, false);
                var srCrack = crackObj.AddComponent<SpriteRenderer>();
                srCrack.sprite = d.spritesDecals[0]; // stone_decal_crack
                srCrack.sortingOrder = 3;
                srCrack.color = new Color(0.95f, 0.85f, 0.70f, 0.95f);
            }
            else if (d.spriteWallFaceCracked != null)
            {
                sr.sprite = d.spriteWallFaceCracked;
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var prop = go.AddComponent<PropDestruible>();
            prop.tipoProp = TipoPropDestruible.MuroDestructible;
            prop.vidaMax = 25;
            prop.esExplosivo = false;

            _murosVisuales.Add(sr);
        }

        private void CrearBloqueAutotile(Transform padre, Vector2 pos, Sprite sp)
        {
            var go = new GameObject("BloquePiedra");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            _murosVisuales.Add(sr);
        }

        private void CrearTrampaPinchosAnimada(Transform padre, Vector2 pos, Sprite[] frames)
        {
            var go = new GameObject("TrampaPinchos");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.85f, 0.85f);

            var trampa = go.AddComponent<TrampaPinchos>();
            trampa.Configurar(frames, 5);
        }

        private void CrearTileAgua(Transform padre, Vector2 pos, Sprite[] framesWater)
        {
            var go = new GameObject("Agua");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;

            if (framesWater != null && framesWater.Length > 0)
            {
                sr.sprite = framesWater[0];
                if (framesWater.Length > 1)
                {
                    var anim = go.AddComponent<AnimadorSpriteTile>();
                    anim.Configurar(sr, framesWater, 2f); // 2 fps según especificación
                }
            }

            // Agua poco profunda transitable: los personajes pueden vadearla sin ser bloqueados
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
        }

        private void CrearTileFoso(Transform padre, Vector2 pos, Sprite spPit)
        {
            var go = new GameObject("Foso");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spPit;
            sr.sortingOrder = 0;

            // Bloquea el paso a personajes, balas pasan por encima (no tag Pared, nombre Foso)
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        public void InstanciarLayoutSalaPiedra(Transform padre, SalaInfo s, DatosBioma d, int indiceLayout = -1, bool esSalaCofre = false)
        {
            if (PlantillasLayoutsStone == null || PlantillasLayoutsStone.Length == 0) return;

            int idx = indiceLayout >= 0 ? (indiceLayout % PlantillasLayoutsStone.Length)
                     : (layoutPiedraForzado >= 0 ? (layoutPiedraForzado % PlantillasLayoutsStone.Length)
                     : UnityEngine.Random.Range(0, PlantillasLayoutsStone.Length));

            string[] plantilla = PlantillasLayoutsStone[idx];
            bool flipH = (indiceLayout < 0 && layoutPiedraForzado < 0) ? (UnityEngine.Random.value > 0.5f) : false;
            bool flipV = (indiceLayout < 0 && layoutPiedraForzado < 0) ? (UnityEngine.Random.value > 0.5f) : false;

            int cols = 18;
            int rows = 10;
            char[,] gridMatriz = new char[cols, rows];

            for (int r = 0; r < rows; r++)
            {
                int rSrc = flipV ? (rows - 1 - r) : r;
                string fila = plantilla[rSrc];
                for (int c = 0; c < cols; c++)
                {
                    int cSrc = flipH ? (cols - 1 - c) : c;
                    gridMatriz[c, r] = (cSrc < fila.Length) ? fila[cSrc] : '.';
                }
            }

            float startX = s.cx - 8.5f;
            float startY = s.cy + 4.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = gridMatriz[c, r];
                    if (ch == '.') continue;

                    Vector2 pos = new Vector2(startX + c, startY - r);

                    // Si es la sala del super cofre, NUNCA generar mini cofres ni obstaculos que tapen el cofre central
                    if (esSalaCofre)
                    {
                        if (ch == 'm') continue;
                        if (Mathf.Abs(pos.x - s.cx) <= 1.9f && Mathf.Abs(pos.y - s.cy) <= 1.9f) continue;
                    }

                    switch (ch)
                    {
                        case 'm':
                            CrearMiniCofre(padre, pos);
                            break;

                        case 'D':
                        case 'x':
                            CrearMuroDestructible(padre, pos, d);
                            break;

                        case '#':
                            // Bloque autotile bitmask: N*1 + E*2 + S*4 + W*8
                            int mask = 0;
                            if (r > 0 && gridMatriz[c, r - 1] == '#') mask |= 1; // N
                            if (c + 1 < cols && gridMatriz[c + 1, r] == '#') mask |= 2; // E
                            if (r + 1 < rows && gridMatriz[c, r + 1] == '#') mask |= 4; // S
                            if (c > 0 && gridMatriz[c - 1, r] == '#') mask |= 8; // W

                            Sprite spBloque = (d.spritesBlock != null && mask >= 0 && mask < d.spritesBlock.Length && d.spritesBlock[mask] != null)
                                ? d.spritesBlock[mask] : d.spriteMuro;
                            CrearBloqueAutotile(padre, pos, spBloque);
                            break;

                        case 'P':
                            CrearPillar(padre, pos, d.spritePillar);
                            break;

                        case 'c':
                            CrearCaja(padre, pos, d.spriteCrate);
                            break;

                        case 'b':
                            CrearBarril(padre, pos, d.spriteBarrel, esExplosivo: false);
                            break;

                        case 'r':
                            CrearBarril(padre, pos, d.spriteBarrelRed ?? d.spriteBarrel, esExplosivo: true);
                            break;

                        case 'p':
                            CrearJarron(padre, pos, d.spritePot ?? d.spriteCrate);
                            break;

                        case 's':
                            if (d.framesSpikesStrip != null && d.framesSpikesStrip.Length > 0 && d.framesSpikesStrip[0] != null)
                            {
                                CrearTrampaPinchosAnimada(padre, pos, d.framesSpikesStrip);
                            }
                            else if (d.spriteSpikesOff != null && d.spriteSpikesOn != null)
                            {
                                CrearTrampaPinchos(padre, pos, d.spriteSpikesOff, d.spriteSpikesOn);
                            }
                            break;

                        case 'w':
                            CrearTileAgua(padre, pos, d.framesWaterStrip);
                            break;

                        case 'o':
                            CrearTileFoso(padre, pos, d.spritePit);
                            break;
                    }
                }
            }
        }

        private void CrearTrampaPinchos(Transform padre, Vector2 pos, Sprite spOff, Sprite spOn)
        {
            var go = new GameObject("TrampaPinchos");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spOff;
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.85f, 0.85f);

            var trampa = go.AddComponent<TrampaPinchos>();
            trampa.Configurar(spOff, spOn, 5);
        }

        private void CrearTilePeligro(Transform padre, Vector2 pos, Sprite[] frames, TipoPeligroTile tipo)
        {
            var go = new GameObject($"Hazard_{tipo}");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 0;

            if (frames.Length > 1)
            {
                var anim = go.AddComponent<AnimadorSpriteTile>();
                anim.Configurar(sr, frames, 5f);
            }

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);

            var zona = go.AddComponent<ZonaPeligroTile>();
            zona.Configurar(tipo, 4);
        }

        private void CrearPropEspecial(Transform padre, Vector2 pos, DatosBioma d)
        {
            var go = new GameObject($"Prop_{d.prefijo}");
            go.transform.SetParent(padre);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = d.spriteSpecialProp;
            sr.sortingOrder = 1;

            switch (d.tipo)
            {
                case TipoBioma.Piedra:
                    // bones: decorativo sin colisión
                    break;

                case TipoBioma.Planta:
                    // thornbush: sólido, daña al tocar
                    try { go.tag = "Pared"; } catch { }
                    var colPlant = go.AddComponent<BoxCollider2D>();
                    colPlant.size = new Vector2(0.8f, 0.8f);
                    go.AddComponent<PropDanoContacto>().danoContacto = 4;
                    _murosVisuales.Add(sr);
                    break;

                case TipoBioma.Hielo:
                    // crystal: sólido
                    try { go.tag = "Pared"; } catch { }
                    var colIce = go.AddComponent<BoxCollider2D>();
                    colIce.size = new Vector2(0.8f, 0.8f);
                    _murosVisuales.Add(sr);
                    break;

                case TipoBioma.Fuego:
                    // brazier: sólido, fuego animado a 8 fps
                    try { go.tag = "Pared"; } catch { }
                    if (d.framesSpecialAnim != null && d.framesSpecialAnim.Length > 1)
                    {
                        var anim = go.AddComponent<AnimadorSpriteTile>();
                        anim.Configurar(sr, d.framesSpecialAnim, 8f);
                    }
                    var colFire = go.AddComponent<BoxCollider2D>();
                    colFire.size = new Vector2(0.8f, 0.8f);
                    _murosVisuales.Add(sr);
                    break;

                case TipoBioma.Arena:
                    // cactus: sólido, daña al tocar
                    try { go.tag = "Pared"; } catch { }
                    var colCactus = go.AddComponent<BoxCollider2D>();
                    colCactus.size = new Vector2(0.8f, 0.8f);
                    go.AddComponent<PropDanoContacto>().danoContacto = 4;
                    _murosVisuales.Add(sr);
                    break;
            }
        }

        private void ConfigurarOleadasSala(ControladorSala ctrl, SalaInfo s, bool esSalaFinal, bool esJefe)
        {
            if (esSalaFinal)
            {
                float wSala = s.xMax - s.xMin;
                float hSala = s.yMax - s.yMin;
                float pxOff = Mathf.Max(4.5f, wSala * 0.32f);
                float pyOff = Mathf.Max(3.8f, hSala * 0.30f);

                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy + pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy + pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy - pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy - pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx, s.cy + pyOff * 0.85f));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx, s.cy - pyOff * 0.85f));

                for (int i = 0; i < 16; i++)
                {
                    Vector2 pPortal = ctrl.posicionesPortales[i % ctrl.posicionesPortales.Count];
                    Vector2 jitter = new Vector2(((i % 3) - 1) * 0.65f, ((i % 2 == 0) ? 0.5f : -0.5f));
                    ctrl.puntosSpawnOleada1.Add(pPortal + jitter);
                }

                for (int i = 0; i < 18; i++)
                {
                    Vector2 pPortal = ctrl.posicionesPortales[(i + 1) % ctrl.posicionesPortales.Count];
                    Vector2 jitter = new Vector2(((i % 3) - 1) * -0.65f, ((i % 2 == 0) ? -0.5f : 0.5f));
                    ctrl.puntosSpawnOleada2.Add(pPortal + jitter);
                }

                for (int i = 0; i < 22; i++)
                {
                    Vector2 pPortal = ctrl.posicionesPortales[(i + 2) % ctrl.posicionesPortales.Count];
                    Vector2 jitter = new Vector2(((i % 4) - 1.5f) * 0.55f, ((i % 2 == 0) ? 0.6f : -0.6f));
                    ctrl.puntosSpawnOleada3.Add(pPortal + jitter);
                }
            }
            else if (!esJefe)
            {
                float wSala = s.xMax - s.xMin;
                float hSala = s.yMax - s.yMin;
                float pxOff = Mathf.Max(3.0f, wSala * 0.24f);
                float pyOff = Mathf.Max(2.5f, hSala * 0.24f);

                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy + pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy + pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy - pyOff));
                CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy - pyOff));

                int cantOleada1 = UnityEngine.Random.Range(6, 8);
                int cantOleada2 = UnityEngine.Random.Range(8, 11);

                for (int i = 0; i < cantOleada1; i++)
                {
                    Vector2 pPortal = ctrl.posicionesPortales[i % ctrl.posicionesPortales.Count];
                    Vector2 jitter = new Vector2(((i % 3) - 1) * 0.5f, ((i % 2 == 0) ? 0.4f : -0.4f));
                    ctrl.puntosSpawnOleada1.Add(pPortal + jitter);
                }

                for (int i = 0; i < cantOleada2; i++)
                {
                    Vector2 pPortal = ctrl.posicionesPortales[(i + 1) % ctrl.posicionesPortales.Count];
                    Vector2 jitter = new Vector2(((i % 3) - 1) * -0.5f, ((i % 2 == 0) ? -0.4f : 0.4f));
                    ctrl.puntosSpawnOleada2.Add(pPortal + jitter);
                }
            }
        }

        private void ConstruirSantuarioDecisionLoop(DatosBioma datos)
        {
            if (datos == null) return;

            int ancho = 30;
            int alto = 22;
            int hx = ancho / 2;
            int hy = alto / 2;

            int xMin = -hx;
            int xMax = hx;
            int yMin = -hy;
            int yMax = hy;

            gridMinX = xMin - 4;
            gridMaxX = xMax + 4;
            gridMinY = yMin - 4;
            gridMaxY = yMax + 4;
            int gridW = gridMaxX - gridMinX + 1;
            int gridH = gridMaxY - gridMinY + 1;
            gridActual = new byte[gridW, gridH];

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    gridActual[x - gridMinX, y - gridMinY] = 1;
                }
            }

            // Muros perimetrales
            for (int x = xMin - 1; x <= xMax + 1; x++)
            {
                gridActual[x - gridMinX, (yMin - 1) - gridMinY] = 2;
                gridActual[x - gridMinX, (yMax + 1) - gridMinY] = 2;
            }
            for (int y = yMin; y <= yMax; y++)
            {
                gridActual[(xMin - 1) - gridMinX, y - gridMinY] = 2;
                gridActual[(xMax + 1) - gridMinX, y - gridMinY] = 2;
            }

            // Cara frontal de muro
            for (int x = xMin; x <= xMax; x++)
            {
                gridActual[x - gridMinX, yMax - gridMinY] = 3;
            }

            // Instanciar suelos y muros
            for (int gy = 0; gy < gridH; gy++)
            {
                for (int gx = 0; gx < gridW; gx++)
                {
                    byte t = gridActual[gx, gy];
                    if (t == 0) continue;

                    Vector2 pos = new Vector2(gx + gridMinX, gy + gridMinY);
                    if (t == 1)
                    {
                        bool sombra0 = (gy + 1 < gridH && gridActual[gx, gy + 1] == 3);
                        bool sombra1 = (!sombra0 && gy + 2 < gridH && gridActual[gx, gy + 2] == 3);
                        Sprite sp = sombra0 ? (datos.spriteFloorShadow00 ?? datos.spriteSuelo)
                                  : sombra1 ? (datos.spriteFloorShadow01 ?? datos.spriteSuelo)
                                  : datos.spriteSuelo;
                        InstanciarTileSuelo(pos, sp, datos);
                    }
                    else if (t == 2)
                    {
                        int mask = 0;
                        if (gy + 1 < gridH && gridActual[gx, gy + 1] == 2) mask |= 1;
                        if (gx + 1 < gridW && gridActual[gx + 1, gy] == 2) mask |= 2;
                        if (gy - 1 >= 0 && gridActual[gx, gy - 1] == 2) mask |= 4;
                        if (gx - 1 >= 0 && gridActual[gx - 1, gy] == 2) mask |= 8;
                        Sprite sp = (datos.spritesWallTop != null && mask < datos.spritesWallTop.Length) ? datos.spritesWallTop[mask] : datos.spriteMuro;
                        InstanciarTileWallTop(pos, sp);
                    }
                    else if (t == 3)
                    {
                        InstanciarTileWallFace(pos, datos, esAntorcha: (gx % 4 == 0));
                    }
                }
            }

            InstanciarFondoVoid(gridActual, gridW, gridH, gridMinX, gridMinY, datos);

            // Spawns de jugadores
            puntosSpawnJugadores.Add(new Vector2(-3.0f, 0f));
            puntosSpawnJugadores.Add(new Vector2(3.0f, 0f));
            puntosSpawnJugadores.Add(new Vector2(-3.0f, -2.0f));
            puntosSpawnJugadores.Add(new Vector2(3.0f, -2.0f));

            var salaObj = new GameObject("Sala_SantuarioTriunfo");
            salaObj.transform.SetParent(contenedorMapa);
            salaObj.transform.position = Vector3.zero;
            var ctrl = salaObj.AddComponent<ControladorSala>();
            ctrl.Inicializar(0, xMin, xMax, yMin, yMax, true, false, false, bioma: datos.tipo);
            salasGeneradas.Add(ctrl);

            if (datos.spritePillar != null)
            {
                CrearPillar(salaObj.transform, new Vector2(-9f, 5f), datos.spritePillar);
                CrearPillar(salaObj.transform, new Vector2(9f, 5f), datos.spritePillar);
                CrearPillar(salaObj.transform, new Vector2(-9f, -5f), datos.spritePillar);
                CrearPillar(salaObj.transform, new Vector2(9f, -5f), datos.spritePillar);
            }

            CrearAntorchaEnSala(ctrl, new Vector2(xMin + 2.5f, yMax - 2.5f), datos);
            CrearAntorchaEnSala(ctrl, new Vector2(xMax - 2.5f, yMax - 2.5f), datos);
            CrearAntorchaEnSala(ctrl, new Vector2(xMin + 2.5f, yMin + 2.5f), datos);
            CrearAntorchaEnSala(ctrl, new Vector2(xMax - 2.5f, yMin + 2.5f), datos);

            TextoFlotante.Crear(new Vector3(0, 3.5f, 0), "🏆 ¡MAZMORRA CONQUISTADA! 🏆", new Color(1f, 0.85f, 0.25f));
            TextoFlotante.Crear(new Vector3(0, 2.2f, 0), "Elige tu destino: Sal al Lobby o desafía el Bucle Infinito", Color.white);

            var pBucleObj = new GameObject("PortalBucleInfinito");
            pBucleObj.transform.SetParent(contenedorMapa);
            pBucleObj.transform.position = new Vector3(0f, 9.5f, 0f);
            var srBucle = pBucleObj.AddComponent<SpriteRenderer>();
            srBucle.sprite = _spritePortalInvocacion != null ? _spritePortalInvocacion : spritePortal;
            srBucle.sortingOrder = 3;
            pBucleObj.transform.localScale = Vector3.one * 1.8f;
            var colBucle = pBucleObj.AddComponent<CircleCollider2D>();
            colBucle.isTrigger = true;
            colBucle.radius = 1.2f;
            var pdBucle = pBucleObj.AddComponent<PortalDecisionLoop>();
            pdBucle.tipoDecision = TipoDecisionPortal.ContinuarLoop;
            pdBucle.tituloPortal = "🔼 ENTRAR AL BUCLE (NIVEL 11+)";
            pdBucle.subtituloPortal = "Peligro Extremo - Dificultad Aumentada";
            pdBucle.colorPortal = new Color(0.95f, 0.35f, 0.95f);

            var pLobbyObj = new GameObject("PortalSalirLobby");
            pLobbyObj.transform.SetParent(contenedorMapa);
            pLobbyObj.transform.position = new Vector3(0f, -9.5f, 0f);
            var srLobby = pLobbyObj.AddComponent<SpriteRenderer>();
            srLobby.sprite = spriteHoyitoPared != null ? spriteHoyitoPared : spritePortal;
            srLobby.sortingOrder = 3;
            pLobbyObj.transform.localScale = Vector3.one * 1.8f;
            var colLobby = pLobbyObj.AddComponent<CircleCollider2D>();
            colLobby.isTrigger = true;
            colLobby.radius = 1.2f;
            var pdLobby = pLobbyObj.AddComponent<PortalDecisionLoop>();
            pdLobby.tipoDecision = TipoDecisionPortal.SalirLobby;
            pdLobby.tituloPortal = "🔽 SALIR AL LOBBY";
            pdLobby.subtituloPortal = "Reclamar Victoria y Guardar Oro";
            pdLobby.colorPortal = new Color(0.35f, 0.95f, 0.45f);
        }

        private void CrearPuertasDeBloqueoSala(ControladorSala ctrl, SalaInfo s, byte[,] grid, int minX, int minY, int gridW, int gridH, Sprite spriteMuro)
        {
            CrearSegmentosPuertaVertical(ctrl, s.xMin - 1, s.yMin, s.yMax, grid, minX, minY, gridW, gridH, spriteMuro);
            CrearSegmentosPuertaVertical(ctrl, s.xMax + 1, s.yMin, s.yMax, grid, minX, minY, gridW, gridH, spriteMuro);
            CrearSegmentosPuertaHorizontal(ctrl, s.yMin - 1, s.xMin, s.xMax, grid, minX, minY, gridW, gridH, spriteMuro);
            CrearSegmentosPuertaHorizontal(ctrl, s.yMax + 1, s.xMin, s.xMax, grid, minX, minY, gridW, gridH, spriteMuro);
        }

        private void CrearSegmentosPuertaVertical(ControladorSala ctrl, int xBorde, int yMin, int yMax, byte[,] grid, int minX, int minY, int gridW, int gridH, Sprite spriteMuro)
        {
            int gx = xBorde - minX;
            if (gx < 0 || gx >= gridW) return;

            int yIni = -9999;
            for (int y = yMin; y <= yMax; y++)
            {
                int gy = y - minY;
                bool esSueloPasillo = (gy >= 0 && gy < gridH && grid[gx, gy] == 1);
                if (esSueloPasillo)
                {
                    if (yIni == -9999) yIni = y;
                }
                else
                {
                    if (yIni != -9999)
                    {
                        float alto = (y - 1) - yIni + 1f;
                        Vector2 pos = new Vector2(xBorde, (yIni + (y - 1)) * 0.5f);
                        var p = CrearPuertaBloqueoVisual(pos, new Vector2(1f, alto), spriteMuro, new Color(0.9f, 0.35f, 0.25f, 1f));
                        ctrl.RegistrarPuertaBloqueo(p);
                        yIni = -9999;
                    }
                }
            }
            if (yIni != -9999)
            {
                float alto = yMax - yIni + 1f;
                Vector2 pos = new Vector2(xBorde, (yIni + yMax) * 0.5f);
                var p = CrearPuertaBloqueoVisual(pos, new Vector2(1f, alto), spriteMuro, new Color(0.9f, 0.35f, 0.25f, 1f));
                ctrl.RegistrarPuertaBloqueo(p);
            }
        }

        private void CrearSegmentosPuertaHorizontal(ControladorSala ctrl, int yBorde, int xMin, int xMax, byte[,] grid, int minX, int minY, int gridW, int gridH, Sprite spriteMuro)
        {
            int gy = yBorde - minY;
            if (gy < 0 || gy >= gridH) return;

            int xIni = -9999;
            for (int x = xMin; x <= xMax; x++)
            {
                int gx = x - minX;
                bool esSueloPasillo = (gx >= 0 && gx < gridW && grid[gx, gy] == 1);
                if (esSueloPasillo)
                {
                    if (xIni == -9999) xIni = x;
                }
                else
                {
                    if (xIni != -9999)
                    {
                        float ancho = (x - 1) - xIni + 1f;
                        Vector2 pos = new Vector2((xIni + (x - 1)) * 0.5f, yBorde);
                        var p = CrearPuertaBloqueoVisual(pos, new Vector2(ancho, 1f), spriteMuro, new Color(0.9f, 0.35f, 0.25f, 1f));
                        ctrl.RegistrarPuertaBloqueo(p);
                        xIni = -9999;
                    }
                }
            }
            if (xIni != -9999)
            {
                float ancho = xMax - xIni + 1f;
                Vector2 pos = new Vector2((xIni + xMax) * 0.5f, yBorde);
                var p = CrearPuertaBloqueoVisual(pos, new Vector2(ancho, 1f), spriteMuro, new Color(0.9f, 0.35f, 0.25f, 1f));
                ctrl.RegistrarPuertaBloqueo(p);
            }
        }

        private GameObject CrearPuertaBloqueoVisual(Vector2 pos, Vector2 tam, Sprite spriteMuro, Color tinte)
        {
            var go = new GameObject("PuertaBloqueoSala");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteMuro;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tam;
            sr.color = tinte;
            sr.sortingOrder = 4;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = tam;
            return go;
        }

        private void AsegurarSpritesAntorcha()
        {
            if (_spriteCirculoLuz == null)
            {
                Texture2D texLlama = new Texture2D(12, 16, TextureFormat.RGBA32, false);
                texLlama.filterMode = FilterMode.Point;
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 12; x++)
                    {
                        float dx = Mathf.Abs(x - 5.5f);
                        float radioMax = (y < 6) ? 3.5f : Mathf.Lerp(3.5f, 0.5f, (y - 6f) / 9f);
                        if (y >= 2 && y <= 14 && dx <= radioMax)
                        {
                            if (dx <= radioMax * 0.45f && y <= 9)
                                texLlama.SetPixel(x, y, new Color(1f, 0.96f, 0.65f, 1f));
                            else if (dx <= radioMax * 0.75f)
                                texLlama.SetPixel(x, y, new Color(1f, 0.65f, 0.12f, 1f));
                            else
                                texLlama.SetPixel(x, y, new Color(0.95f, 0.28f, 0.08f, 0.95f));
                        }
                        else
                        {
                            texLlama.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                texLlama.Apply();
                _spriteCirculoLuz = Sprite.Create(texLlama, new Rect(0, 0, 12, 16), new Vector2(0.5f, 0.2f), 16f);
            }

            if (_spriteHaloLuzCalida == null)
            {
                int tam = 48;
                Texture2D texHalo = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
                texHalo.filterMode = FilterMode.Bilinear;
                texHalo.wrapMode = TextureWrapMode.Clamp;
                Vector2 centro = new Vector2((tam - 1) * 0.5f, (tam - 1) * 0.5f);
                float maxR = tam * 0.5f;
                for (int y = 0; y < tam; y++)
                {
                    for (int x = 0; x < tam; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), centro) / maxR;
                        if (d >= 1f)
                        {
                            texHalo.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            float falloff = (1f - d) * (1f - d);
                            Color col = Color.Lerp(new Color(1f, 0.55f, 0.12f, 0f), new Color(1f, 0.88f, 0.42f, 0.55f), falloff);
                            col.a = falloff * 0.58f;
                            texHalo.SetPixel(x, y, col);
                        }
                    }
                }
                texHalo.Apply();
                _spriteHaloLuzCalida = Sprite.Create(texHalo, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 24f);
            }

            if (_spritePortalInvocacion == null)
            {
                int tam = 28;
                Texture2D texP = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
                texP.filterMode = FilterMode.Point;
                Vector2 c = new Vector2(13.5f, 13.5f);
                for (int y = 0; y < tam; y++)
                {
                    for (int x = 0; x < tam; x++)
                    {
                        Vector2 diff = new Vector2(x, y) - c;
                        float dist = diff.magnitude;
                        if (dist <= 13f && dist >= 2.5f)
                        {
                            float angle = Mathf.Atan2(diff.y, diff.x);
                            float spiral = Mathf.Sin(angle * 3f + (13f - dist) * 0.8f);
                            if (spiral > 0.05f)
                            {
                                Color colP = (dist < 7.5f)
                                    ? Color.Lerp(new Color(1f, 0.95f, 1f, 1f), new Color(0.9f, 0.35f, 0.95f, 1f), dist / 7.5f)
                                    : Color.Lerp(new Color(0.9f, 0.35f, 0.95f, 1f), new Color(0.4f, 0.1f, 0.85f, 0f), (dist - 7.5f) / 5.5f);
                                texP.SetPixel(x, y, colP);
                            }
                            else
                            {
                                texP.SetPixel(x, y, Color.clear);
                            }
                        }
                        else
                        {
                            texP.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                texP.Apply();
                _spritePortalInvocacion = Sprite.Create(texP, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 16f);
            }

            if (_spriteAntorchaBase == null)
            {
                Texture2D texB = new Texture2D(6, 12, TextureFormat.RGBA32, false);
                texB.filterMode = FilterMode.Point;
                for (int y = 0; y < 12; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        if (y >= 8) texB.SetPixel(x, y, new Color(0.35f, 0.28f, 0.22f));
                        else if (x >= 2 && x <= 3) texB.SetPixel(x, y, new Color(0.5f, 0.38f, 0.25f));
                        else texB.SetPixel(x, y, Color.clear);
                    }
                }
                texB.Apply();
                _spriteAntorchaBase = Sprite.Create(texB, new Rect(0, 0, 6, 12), new Vector2(0.5f, 0.1f), 16f);
            }
        }

        private void CrearPortalSpawnEnSala(ControladorSala ctrl, Vector2 pos)
        {
            var pObj = new GameObject("PortalInvocacion");
            pObj.transform.SetParent(contenedorMapa);
            pObj.transform.position = (Vector3)pos;

            var sr = pObj.AddComponent<SpriteRenderer>();
            sr.sprite = _spritePortalInvocacion;
            sr.color = new Color(0.95f, 0.45f, 1f, 0.88f);
            sr.sortingOrder = 1;

            ctrl.RegistrarPortalSpawn(pos, pObj);
        }

        private void CrearAntorchaEnSala(ControladorSala ctrl, Vector2 pos, DatosBioma dSala = null)
        {
            var antObj = new GameObject("Antorcha");
            antObj.transform.SetParent(contenedorMapa);
            antObj.transform.position = (Vector3)pos;

            var srBase = antObj.AddComponent<SpriteRenderer>();
            srBase.sprite = _spriteAntorchaBase;
            srBase.sortingOrder = 0;

            DatosBioma datos = dSala ?? ObtenerDatosBioma(biomaActual);

            var haloObj = new GameObject("HaloLuzCalida");
            haloObj.transform.SetParent(antObj.transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            haloObj.transform.localScale = Vector3.one * 9.5f;
            var srHalo = haloObj.AddComponent<SpriteRenderer>();
            srHalo.sprite = _spriteHaloLuzCalida;
            srHalo.sortingOrder = 0;
            if (datos != null && datos.colorLuzAntorcha != default)
            {
                srHalo.color = datos.colorLuzAntorcha;
            }

            var llamaObj = new GameObject("Llama");
            llamaObj.transform.SetParent(antObj.transform, false);
            llamaObj.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            llamaObj.transform.localScale = Vector3.one;
            var srLlama = llamaObj.AddComponent<SpriteRenderer>();

            if (datos != null && datos.framesWallFaceTorch != null && datos.framesWallFaceTorch.Length > 0 && datos.framesWallFaceTorch[0] != null)
            {
                var anim = llamaObj.AddComponent<AnimadorSpriteTile>();
                anim.Configurar(srLlama, datos.framesWallFaceTorch, 8f);
            }
            else
            {
                srLlama.sprite = (datos != null && datos.spriteLlamaAntorcha != null) ? datos.spriteLlamaAntorcha : _spriteCirculoLuz;
            }

            srLlama.color = Color.white;
            srLlama.sortingOrder = 3;

            ctrl.RegistrarAntorcha(pos, srLlama, srHalo);
        }

        public void ActualizarIluminacionMuros(SistemaVision vision)
        {
            // Los muros ya quedan cubiertos directamente por el overlay de oscuridad en SistemaVision (sortingOrder = 4)
        }

        private SalaInfo CrearSala(int cx, int cy, int ancho, int alto, int numCols, int numEnems, bool esInicio, bool esJefe, TipoBioma bioma = TipoBioma.Piedra)
        {
            int hx = ancho / 2;
            int hy = alto / 2;
            return new SalaInfo
            {
                cx = cx,
                cy = cy,
                xMin = cx - hx,
                xMax = cx + hx,
                yMin = cy - hy,
                yMax = cy + hy,
                numColumnas = numCols,
                numEnemigos = numEnems,
                esSalaInicio = esInicio,
                esSalaJefe = esJefe,
                bioma = bioma
            };
        }
    }
}
