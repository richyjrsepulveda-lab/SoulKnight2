using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoBioma
    {
        Piedra,
        Hielo,
        Magma
    }

    [System.Serializable]
    public class DatosBioma
    {
        public TipoBioma tipo;
        public string nombreBioma;
        public Sprite spriteSuelo;
        public Sprite spriteMuro;
        public Sprite spriteColumna;
        public Color colorFondoCamara;
    }

    /// <summary>
    /// Generador procedural de mazmorras con salas conectadas mediante pasillos transitables,
    /// puertas de bloqueo por encuentro, antorchas que iluminan las salas al acceder,
    /// y pasillo final tras el jefe que lleva al hoyo para avanzar de nivel.
    /// </summary>
    public class GeneradorMazmorra : MonoBehaviour
    {
        public static GeneradorMazmorra Instancia { get; private set; }

        [Header("Configuración de Biomas")]
        public TipoBioma biomaActual = TipoBioma.Piedra;
        public bool biomaAleatorio = true;
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

        // Muros registrados para iluminación dinámica por rango de visión
        private readonly List<SpriteRenderer> _murosVisuales = new List<SpriteRenderer>();

        // Límites globales de la mazmorra para el minimapa
        [HideInInspector]
        public int gridMinX, gridMaxX, gridMinY, gridMaxY;
        [HideInInspector]
        public byte[,] gridActual;

        public event Action<DatosBioma> AlGenerarMazmorra;

        private int _ultimaSemilla = -1;
        private TipoBioma _ultimoBioma = (TipoBioma)(-1);

        private static Sprite _spriteCirculoLuz;
        private static Sprite _spriteAntorchaBase;

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
                GenerarConSemilla(SalaRed.Instancia.SemillaMapaActual.Value, (TipoBioma)SalaRed.Instancia.BiomaMapaActual.Value);
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

        public void GenerarConSemilla(int semilla, TipoBioma bioma)
        {
            SistemaVision.AsegurarInstancia();
            if (_ultimaSemilla == semilla && _ultimoBioma == bioma) return;
            _ultimaSemilla = semilla;
            _ultimoBioma = bioma;

            UnityEngine.Random.InitState(semilla);
            biomaActual = bioma;
            DatosBioma datos = ObtenerDatosBioma(biomaActual);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            LimpiarMapaExistente();
            ConstruirMazmorra(datos);

            AlGenerarMazmorra?.Invoke(datos);
        }

        private DatosBioma ObtenerDatosBioma(TipoBioma tipo)
        {
            if (biomas == null) return null;
            foreach (var b in biomas)
            {
                if (b.tipo == tipo) return b;
            }
            return biomas.Length > 0 ? biomas[0] : null;
        }

        private void LimpiarMapaExistente()
        {
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
        }

        private struct PasilloInfo
        {
            public int x1, y1, x2, y2;
        }

        private void ConstruirMazmorra(DatosBioma datos)
        {
            if (datos == null) return;

            List<SalaInfo> salas = new List<SalaInfo>();
            List<PasilloInfo> pasillos = new List<PasilloInfo>();

            // Sala 0: Entrada segura (siempre al inicio / izquierda)
            salas.Add(CrearSala(0, 0, 14, 12, 0, 0, esInicio: true, esJefe: false));

            puntosSpawnJugadores.Add(new Vector2(-2.5f, 1.5f));
            puntosSpawnJugadores.Add(new Vector2(-2.5f, -1.5f));
            puntosSpawnJugadores.Add(new Vector2(-4.5f, 1.5f));
            puntosSpawnJugadores.Add(new Vector2(-4.5f, -1.5f));

            int totalSalasCaminoPrincipal = 5;
            int currX = 0;
            int currY = 0;

            for (int k = 1; k < totalSalasCaminoPrincipal; k++)
            {
                bool esJefe = (k == totalSalasCaminoPrincipal - 1);
                int ancho = esJefe ? 20 : UnityEngine.Random.Range(16, 22);
                int alto = esJefe ? 16 : UnityEngine.Random.Range(14, 18);
                int numCols = esJefe ? 2 : (UnityEngine.Random.Range(1, 3) * 2);
                int numEnems = esJefe ? 0 : UnityEngine.Random.Range(4, 7);

                int chance = UnityEngine.Random.Range(0, 100);
                int stepX = 28;
                int stepY = 0;

                if (esJefe)
                {
                    stepX = 30;
                    stepY = 0;
                }
                else if (chance < 50)
                {
                    stepX = UnityEngine.Random.Range(26, 30);
                    stepY = UnityEngine.Random.Range(-4, 5);
                }
                else if (chance < 75)
                {
                    stepX = UnityEngine.Random.Range(8, 16);
                    stepY = UnityEngine.Random.Range(20, 25);
                }
                else
                {
                    stepX = UnityEngine.Random.Range(8, 16);
                    stepY = -UnityEngine.Random.Range(20, 25);
                }

                int candX = currX + stepX;
                int candY = currY + stepY;

                bool colisiona = false;
                foreach (var s in salas)
                {
                    if (Mathf.Abs(candX - s.cx) < (ancho + s.xMax - s.xMin) * 0.5f + 6 &&
                        Mathf.Abs(candY - s.cy) < (alto + s.yMax - s.yMin) * 0.5f + 6)
                    {
                        colisiona = true;
                        break;
                    }
                }

                if (colisiona)
                {
                    candX = currX + 30;
                    candY = currY;
                }

                currX = candX;
                currY = candY;
                salas.Add(CrearSala(currX, currY, ancho, alto, numCols, numEnems, esInicio: false, esJefe: esJefe));

                // Conectar con la sala anterior del camino principal
                pasillos.Add(new PasilloInfo
                {
                    x1 = salas[k - 1].cx,
                    y1 = salas[k - 1].cy,
                    x2 = salas[k].cx,
                    y2 = salas[k].cy
                });
            }

            // Sala Final (Sala del Jefe) y Pasillito Posterior con el Hoyo
            var salaFinal = salas[totalSalasCaminoPrincipal - 1];
            puntoSpawnJefe = new Vector2(salaFinal.cx, salaFinal.cy);

            // Pasillito de salida después de la sala del jefe (8 bloques hacia la derecha)
            int pasilloJefeXMin = salaFinal.xMax + 1;
            int pasilloJefeXMax = salaFinal.xMax + 9;
            int pasilloJefeY = salaFinal.cy;
            posicionPortal = new Vector2(pasilloJefeXMax - 1.2f, pasilloJefeY + 0.5f);

            // Cuartos Extra sin salida (ramificaciones laterales que no llevan a ningún lado, solo tienen más enemigos para pelear)
            Vector2Int[] dirsRamificacion = new Vector2Int[]
            {
                new Vector2Int(0, 25),
                new Vector2Int(0, -25),
                new Vector2Int(-25, 22),
                new Vector2Int(-25, -22),
                new Vector2Int(24, 24),
                new Vector2Int(24, -24)
            };

            for (int idxPadre = 1; idxPadre <= 3; idxPadre++)
            {
                var padre = salas[idxPadre];
                int anchoExtra = UnityEngine.Random.Range(16, 20);
                int altoExtra = UnityEngine.Random.Range(14, 18);
                int numColsExtra = (UnityEngine.Random.Range(1, 3) * 2);

                int offsetInicial = UnityEngine.Random.Range(0, dirsRamificacion.Length);
                for (int d = 0; d < dirsRamificacion.Length; d++)
                {
                    Vector2Int offset = dirsRamificacion[(offsetInicial + d) % dirsRamificacion.Length];
                    int candX = padre.cx + offset.x;
                    int candY = padre.cy + offset.y;

                    // Evitar invadir la zona del pasillo de salida del jefe
                    if (candX + anchoExtra / 2 > salaFinal.xMin - 4 && Mathf.Abs(candY - salaFinal.cy) < 18)
                        continue;

                    bool colisiona = false;
                    foreach (var s in salas)
                    {
                        if (Mathf.Abs(candX - s.cx) < (anchoExtra + s.xMax - s.xMin) * 0.5f + 6 &&
                            Mathf.Abs(candY - s.cy) < (altoExtra + s.yMax - s.yMin) * 0.5f + 6)
                        {
                            colisiona = true;
                            break;
                        }
                    }

                    if (!colisiona)
                    {
                        var cuartoExtra = CrearSala(candX, candY, anchoExtra, altoExtra, numColsExtra, 6, esInicio: false, esJefe: false);
                        salas.Add(cuartoExtra);
                        pasillos.Add(new PasilloInfo
                        {
                            x1 = padre.cx,
                            y1 = padre.cy,
                            x2 = cuartoExtra.cx,
                            y2 = cuartoExtra.cy
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

            minX -= 8;
            maxX += 8;
            minY -= 8;
            maxY += 8;

            gridMinX = minX;
            gridMaxX = maxX;
            gridMinY = minY;
            gridMaxY = maxY;

            int gridW = maxX - minX + 1;
            int gridH = maxY - minY + 1;

            byte[,] grid = new byte[gridW, gridH]; // 0 = Nada, 1 = Suelo, 2 = Muro
            gridActual = grid;

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
                            grid[gx, gy] = 1;
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
                            grid[gx, gy] = 1;
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
                        grid[gx, gy] = 1;
                }
            }

            // 3. Determinar Muros Perimetrales
            int[] dx8 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy8 = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int gx = 0; gx < gridW; gx++)
            {
                for (int gy = 0; gy < gridH; gy++)
                {
                    if (grid[gx, gy] == 0)
                    {
                        bool tieneVecinoSuelo = false;
                        for (int k = 0; k < 8; k++)
                        {
                            int nx = gx + dx8[k];
                            int ny = gy + dy8[k];
                            if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH && grid[nx, ny] == 1)
                            {
                                tieneVecinoSuelo = true;
                                break;
                            }
                        }
                        if (tieneVecinoSuelo)
                        {
                            grid[gx, gy] = 2;
                        }
                    }
                }
            }

            // 4. Instanciar Suelos
            foreach (var s in salas)
            {
                Vector2 tam = new Vector2(s.xMax - s.xMin + 1, s.yMax - s.yMin + 1);
                Vector2 pos = new Vector2((s.xMin + s.xMax) * 0.5f, (s.yMin + s.yMax) * 0.5f);
                CrearSueloVisual(pos, tam, datos.spriteSuelo);
            }

            foreach (var p in pasillos)
            {
                float anchoH = Mathf.Abs(p.x2 - p.x1) + radioPasillo * 2f;
                float altoH = radioPasillo * 2f;
                Vector2 posH = new Vector2((p.x1 + p.x2) * 0.5f, p.y1 + 0.5f);
                CrearSueloVisual(posH, new Vector2(anchoH, altoH), datos.spriteSuelo);
                rectPasillos.Add(new Rect(posH.x - anchoH * 0.5f, posH.y - altoH * 0.5f, anchoH, altoH));

                float anchoV = radioPasillo * 2f;
                float altoV = Mathf.Abs(p.y2 - p.y1) + radioPasillo * 2f;
                Vector2 posV = new Vector2(p.x2 + 0.5f, (p.y1 + p.y2) * 0.5f);
                CrearSueloVisual(posV, new Vector2(anchoV, altoV), datos.spriteSuelo);
                rectPasillos.Add(new Rect(posV.x - anchoV * 0.5f, posV.y - altoV * 0.5f, anchoV, altoV));
            }

            // Suelo visual del pasillito final del jefe
            float anchoPasilloJefe = (pasilloJefeXMax - pasilloJefeXMin + 1);
            float altoPasilloJefe = radioPasillo * 2f;
            Vector2 posPasilloJefe = new Vector2((pasilloJefeXMin + pasilloJefeXMax) * 0.5f, pasilloJefeY + 0.5f);
            CrearSueloVisual(posPasilloJefe, new Vector2(anchoPasilloJefe, altoPasilloJefe), datos.spriteSuelo);
            rectPasillos.Add(new Rect(posPasilloJefe.x - anchoPasilloJefe * 0.5f, posPasilloJefe.y - altoPasilloJefe * 0.5f, anchoPasilloJefe, altoPasilloJefe));

            // 5. Instanciar Muros Perimetrales
            for (int gy = 0; gy < gridH; gy++)
            {
                int gxInicio = -1;
                for (int gx = 0; gx < gridW; gx++)
                {
                    if (grid[gx, gy] == 2)
                    {
                        if (gxInicio == -1) gxInicio = gx;
                    }
                    else
                    {
                        if (gxInicio != -1)
                        {
                            CrearMuroSegmento(gxInicio, gx - 1, gy, minX, minY, datos.spriteMuro);
                            gxInicio = -1;
                        }
                    }
                }
                if (gxInicio != -1)
                {
                    CrearMuroSegmento(gxInicio, gridW - 1, gy, minX, minY, datos.spriteMuro);
                }
            }

            // 6. Instanciar Controladores de Sala, Antorchas, Puertas de Bloqueo y Spawns por Oleadas
            AsegurarSpritesAntorcha();

            // La sala de tienda es SECRETA:
            // 1. NO aparece en todos los niveles (aprox 50% de probabilidad).
            // 2. NUNCA en el camino principal hacia la salida (debe ser una de las habitaciones de relleno / ramificaciones sin salida).
            // 3. En el minimapa se ve exactamente como cualquier otra sala normal (sin marcas ni íconos dorados).
            int indiceSalaTienda = -1;
            if (salas.Count > totalSalasCaminoPrincipal && UnityEngine.Random.value < 0.52f)
            {
                // Elegir aleatoriamente una de las habitaciones de relleno laterales sin salida
                indiceSalaTienda = UnityEngine.Random.Range(totalSalasCaminoPrincipal, salas.Count);
            }

            for (int idx = 0; idx < salas.Count; idx++)
            {
                var s = salas[idx];
                bool esInicio = s.esSalaInicio;
                bool esJefe = s.esSalaJefe;
                bool esTienda = (idx == indiceSalaTienda && !esInicio && !esJefe);

                var salaObj = new GameObject($"Sala_{idx}");
                salaObj.transform.SetParent(contenedorMapa);
                salaObj.transform.position = new Vector3(s.cx, s.cy, 0f);

                var ctrl = salaObj.AddComponent<ControladorSala>();
                ctrl.Inicializar(idx, s.xMin, s.xMax, s.yMin, s.yMax, esInicio, esJefe, esTienda);

                // Colocar 4 Antorchas en las esquinas interiores de la sala
                Vector2[] posAntorchas = new Vector2[]
                {
                    new Vector2(s.xMin + 2.2f, s.yMax - 2.2f),
                    new Vector2(s.xMax - 2.2f, s.yMax - 2.2f),
                    new Vector2(s.xMin + 2.2f, s.yMin + 2.2f),
                    new Vector2(s.xMax - 2.2f, s.yMin + 2.2f)
                };
                foreach (var pa in posAntorchas)
                {
                    CrearAntorchaEnSala(ctrl, pa);
                }

                // Detectar aberturas en el perímetro de la sala para crear puertas de bloqueo cuando se active el combate
                CrearPuertasDeBloqueoSala(ctrl, s, grid, minX, minY, gridW, gridH, datos.spriteMuro);

                // Si es la sala del jefe, colocar también el muro que sella el pasillito final hasta derrotar al jefe
                if (esJefe)
                {
                    var muroSalida = CrearPuertaBloqueoVisual(
                        new Vector2(salaFinal.xMax + 0.5f, pasilloJefeY + 0.5f),
                        new Vector2(1.1f, radioPasillo * 2f + 0.5f),
                        datos.spriteMuro,
                        new Color(0.75f, 0.25f, 0.25f, 1f)
                    );
                    ctrl.AsignarMuroSalidaJefe(muroSalida);
                }

                // Columnas Tácticas
                if (s.numColumnas > 0 && datos.spriteColumna != null)
                {
                    float w = s.xMax - s.xMin + 1;
                    float h = s.yMax - s.yMin + 1;
                    float sepX = w * 0.28f;
                    float sepY = h * 0.25f;

                    if (s.numColumnas == 2)
                    {
                        CrearColumna(new Vector2(s.cx - sepX, s.cy), datos.spriteColumna);
                        CrearColumna(new Vector2(s.cx + sepX, s.cy), datos.spriteColumna);
                    }
                    else if (s.numColumnas >= 4)
                    {
                        CrearColumna(new Vector2(s.cx - sepX, s.cy + sepY), datos.spriteColumna);
                        CrearColumna(new Vector2(s.cx + sepX, s.cy + sepY), datos.spriteColumna);
                        CrearColumna(new Vector2(s.cx - sepX, s.cy - sepY), datos.spriteColumna);
                        CrearColumna(new Vector2(s.cx + sepX, s.cy - sepY), datos.spriteColumna);
                    }
                }

                // Portales de Invocación (2 o 4 portales por sala) y Oleadas que salen de ellos
                if (!esInicio && !esJefe && !esTienda)
                {
                    int numPortales = (UnityEngine.Random.value < 0.5f) ? 2 : 4;
                    float wSala = s.xMax - s.xMin;
                    float hSala = s.yMax - s.yMin;
                    float pxOff = Mathf.Max(3.0f, wSala * 0.22f);
                    float pyOff = Mathf.Max(2.5f, hSala * 0.22f);

                    if (numPortales == 2)
                    {
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy));
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy));
                    }
                    else
                    {
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy + pyOff));
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy + pyOff));
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx - pxOff, s.cy - pyOff));
                        CrearPortalSpawnEnSala(ctrl, new Vector2(s.cx + pxOff, s.cy - pyOff));
                    }

                    // Los enemigos de cada oleada emergen directamente de los portales de la sala
                    int cantOleada1 = (numPortales == 4) ? 4 : UnityEngine.Random.Range(3, 5);
                    int cantOleada2 = (numPortales == 4) ? 4 : UnityEngine.Random.Range(3, 5);

                    for (int i = 0; i < cantOleada1; i++)
                    {
                        Vector2 pPortal = ctrl.posicionesPortales[i % ctrl.posicionesPortales.Count];
                        Vector2 jitter = new Vector2((i % 2 == 0 ? 0.25f : -0.25f), (i >= 2 ? 0.2f : -0.2f));
                        ctrl.puntosSpawnOleada1.Add(pPortal + jitter);
                    }
                    for (int i = 0; i < cantOleada2; i++)
                    {
                        Vector2 pPortal = ctrl.posicionesPortales[(i + 1) % ctrl.posicionesPortales.Count];
                        Vector2 jitter = new Vector2((i % 2 == 0 ? -0.25f : 0.25f), (i >= 2 ? -0.2f : 0.2f));
                        ctrl.puntosSpawnOleada2.Add(pPortal + jitter);
                    }
                }

                salasGeneradas.Add(ctrl);
            }

            // 7. Instanciar el Agujero / Pasadizo Secreto en la pared final del pasillito detrás de la Sala del Jefe
            var hoyitoObj = new GameObject("HoyitoSiguienteNivel");
            hoyitoObj.transform.SetParent(contenedorMapa);
            hoyitoObj.transform.position = new Vector3(pasilloJefeXMax - 0.2f, pasilloJefeY + 0.5f, 0f);

            var sr = hoyitoObj.AddComponent<SpriteRenderer>();
            sr.sprite = (spriteHoyitoParedCerrado != null) ? spriteHoyitoParedCerrado : spriteHoyitoPared;
            sr.sortingOrder = 3; // Encima del muro del borde (2) y del filtro de oscuridad (1)
            hoyitoObj.transform.localScale = Vector3.one * 2.1f;

            var col = hoyitoObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.4f;

            var pasaje = hoyitoObj.AddComponent<PasajeSiguienteNivel>();
            pasaje.sr = sr;
            pasaje.spriteCerrado = spriteHoyitoParedCerrado;
            pasaje.spriteAbierto = spriteHoyitoPared;
            pasaje.estaAbierto = false;
        }

        private void CrearPuertasDeBloqueoSala(ControladorSala ctrl, SalaInfo s, byte[,] grid, int minX, int minY, int gridW, int gridH, Sprite spriteMuro)
        {
            // Borde Izquierdo (x = s.xMin - 1) y Derecho (x = s.xMax + 1)
            CrearSegmentosPuertaVertical(ctrl, s.xMin - 1, s.yMin, s.yMax, grid, minX, minY, gridW, gridH, spriteMuro);
            CrearSegmentosPuertaVertical(ctrl, s.xMax + 1, s.yMin, s.yMax, grid, minX, minY, gridW, gridH, spriteMuro);

            // Borde Inferior (y = s.yMin - 1) y Superior (y = s.yMax + 1)
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
            sr.sortingOrder = 4; // Bloques de borde siempre visibles por encima del filtro de oscuridad (1)

            var col = go.AddComponent<BoxCollider2D>();
            col.size = tam;
            return go;
        }

        private static Sprite _spriteHaloLuzCalida;
        private static Sprite _spritePortalInvocacion;

        private void AsegurarSpritesAntorcha()
        {
            if (_spriteCirculoLuz == null)
            {
                // Sprite pixel-art de llama definida (12x16)
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
                                texLlama.SetPixel(x, y, new Color(1f, 0.96f, 0.65f, 1f)); // Núcleo blanco/amarillo
                            else if (dx <= radioMax * 0.75f)
                                texLlama.SetPixel(x, y, new Color(1f, 0.65f, 0.12f, 1f)); // Fuego naranja
                            else
                                texLlama.SetPixel(x, y, new Color(0.95f, 0.28f, 0.08f, 0.95f)); // Borde rojo cálido
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
                // Halo radial cálido (48x48) que hace evidente que la luz emana de cada antorcha
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
                // Vórtice arcano púrpura/magenta (28x28) para anunciar la aparición de enemigos
                int tam = 28;
                Texture2D texP = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
                texP.filterMode = FilterMode.Point;
                Vector2 c = new Vector2(13.5f, 13.5f);
                for (int y = 0; y < tam; y++)
                {
                    for (int x = 0; x < tam; x++)
                    {
                        Vector2 diff = new Vector2(x, y) - c;
                        float r = diff.magnitude;
                        float ang = Mathf.Atan2(diff.y, diff.x);
                        float espiral = Mathf.Sin(ang * 3f + r * 0.85f);
                        if (r <= 3.2f)
                            texP.SetPixel(x, y, new Color(0.95f, 0.75f, 1f, 1f)); // Núcleo brillante
                        else if (r <= 8.5f)
                            texP.SetPixel(x, y, espiral > 0f ? new Color(0.75f, 0.18f, 0.98f, 0.95f) : new Color(0.32f, 0.06f, 0.55f, 0.92f));
                        else if (r <= 12.2f)
                            texP.SetPixel(x, y, espiral > -0.2f ? new Color(0.92f, 0.25f, 0.85f, 0.85f) : Color.clear);
                        else
                            texP.SetPixel(x, y, Color.clear);
                    }
                }
                texP.Apply();
                _spritePortalInvocacion = Sprite.Create(texP, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 16f);
            }

            if (_spriteAntorchaBase == null)
            {
                // Soporte de hierro y madera pixel-art bien definido (12x20)
                Texture2D texA = new Texture2D(12, 20, TextureFormat.RGBA32, false);
                texA.filterMode = FilterMode.Point;
                for (int y = 0; y < 20; y++)
                {
                    for (int x = 0; x < 12; x++)
                    {
                        if (x >= 5 && x <= 6 && y >= 2 && y <= 12)
                            texA.SetPixel(x, y, (x == 5) ? new Color(0.52f, 0.32f, 0.16f, 1f) : new Color(0.36f, 0.20f, 0.09f, 1f));
                        else if (x >= 4 && x <= 7 && y >= 6 && y <= 7)
                            texA.SetPixel(x, y, new Color(0.25f, 0.27f, 0.32f, 1f)); // Argolla de hierro
                        else if (x >= 3 && x <= 8 && y >= 13 && y <= 15)
                            texA.SetPixel(x, y, new Color(0.28f, 0.30f, 0.35f, 1f)); // Cazoleta de hierro
                        else
                            texA.SetPixel(x, y, Color.clear);
                    }
                }
                texA.Apply();
                _spriteAntorchaBase = Sprite.Create(texA, new Rect(0, 0, 12, 20), new Vector2(0.5f, 0.5f), 16f);
            }
        }

        private void CrearPortalSpawnEnSala(ControladorSala ctrl, Vector2 pos)
        {
            var portalObj = new GameObject("PortalSpawnEnemigos");
            portalObj.transform.SetParent(ctrl.transform);
            portalObj.transform.position = (Vector3)pos;

            var sr = portalObj.AddComponent<SpriteRenderer>();
            sr.sprite = _spritePortalInvocacion;
            sr.sortingOrder = 2; // Sobre el suelo y visible al activarse la sala

            ctrl.RegistrarPortalSpawn(pos, portalObj);
        }

        private void CrearAntorchaEnSala(ControladorSala ctrl, Vector2 pos)
        {
            var antObj = new GameObject("Antorcha");
            antObj.transform.SetParent(ctrl.transform);
            antObj.transform.position = (Vector3)pos;

            var srBase = antObj.AddComponent<SpriteRenderer>();
            srBase.sprite = _spriteAntorchaBase;
            srBase.sortingOrder = 0;

            // Resplandor cálido radial sobre el suelo (sortingOrder = 0, bajo el overlay de oscuridad para no traspasar paredes)
            var haloObj = new GameObject("HaloLuzCalida");
            haloObj.transform.SetParent(antObj.transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            haloObj.transform.localScale = Vector3.one * 9.5f;
            var srHalo = haloObj.AddComponent<SpriteRenderer>();
            srHalo.sprite = _spriteHaloLuzCalida;
            srHalo.sortingOrder = 0;

            // Llama pixel-art definida
            var llamaObj = new GameObject("Llama");
            llamaObj.transform.SetParent(antObj.transform, false);
            llamaObj.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            llamaObj.transform.localScale = Vector3.one;
            var srLlama = llamaObj.AddComponent<SpriteRenderer>();
            srLlama.sprite = _spriteCirculoLuz;
            srLlama.color = Color.white;
            srLlama.sortingOrder = 3;

            ctrl.RegistrarAntorcha(pos, srLlama, srHalo);
        }

        public void ActualizarIluminacionMuros(SistemaVision vision)
        {
            // Los muros ya quedan cubiertos directamente por el overlay de oscuridad en SistemaVision (sortingOrder = 4)
        }

        private SalaInfo CrearSala(int cx, int cy, int ancho, int alto, int numCols, int numEnems, bool esInicio, bool esJefe)
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
                esSalaJefe = esJefe
            };
        }

        private void CrearSueloVisual(Vector2 pos, Vector2 size, Sprite spriteSuelo)
        {
            var go = new GameObject("Suelo");
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteSuelo;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = -1;
        }

        private void CrearMuroSegmento(int gx1, int gx2, int gy, int minX, int minY, Sprite spriteMuro)
        {
            float wx1 = gx1 + minX;
            float wx2 = gx2 + minX;
            float wy = gy + minY;

            float longitud = wx2 - wx1 + 1f;
            Vector2 centro = new Vector2((wx1 + wx2) * 0.5f, wy);
            Vector2 tam = new Vector2(longitud, 1f);

            var go = new GameObject("Muro");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)centro;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteMuro;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tam;
            sr.sortingOrder = 2; // Todos los bloques de los bordes siempre visibles por encima de la oscuridad (1)

            var col = go.AddComponent<BoxCollider2D>();
            col.size = tam;

            _murosVisuales.Add(sr);
        }

        private void CrearColumna(Vector2 pos, Sprite spriteColumna)
        {
            var go = new GameObject("Columna");
            try { go.tag = "Pared"; } catch { }
            go.transform.SetParent(contenedorMapa);
            go.transform.position = (Vector3)pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteColumna;
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 1.3f);

            _murosVisuales.Add(sr);
        }
    }
}
