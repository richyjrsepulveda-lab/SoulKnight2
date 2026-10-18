using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Objeto interactuable en el mapa que otorga una mejora temporal de run a quien lo recoja.
    /// Sincronizado por red (servidor autoritativo).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class ItemMejoraRun : NetworkBehaviour
    {
        public static readonly System.Collections.Generic.List<ItemMejoraRun> Todos = new System.Collections.Generic.List<ItemMejoraRun>();

        public NetworkVariable<byte> TipoMejora = new NetworkVariable<byte>(0);
        public NetworkVariable<int> CostoOro = new NetworkVariable<int>(0);

        private SpriteRenderer _sr;
        private CircleCollider2D _col;
        private bool _recogido = false;
        private Sprite _spriteBase;
        private TextMesh _textoPrecio;

        private static Sprite _spritePescadoEsqueleto;
        private static Sprite _spritePescadoMedio;
        private static Sprite _spritePescadoCompleto;
        private static Sprite _spriteEspadaDano;
        private static Sprite _spriteEscudoDefensa;
        private static Sprite _spriteBotaVelocidad;
        private static Sprite _spriteCorazonVidaMax;

        private void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        private void OnDisable()
        {
            Todos.Remove(this);
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _sr.sortingOrder = 6; // Siempre visible por encima del filtro de oscuridad (1)
                _spriteBase = _sr.sprite;
            }
            _col = GetComponent<CircleCollider2D>();
            _col.isTrigger = true;
            _col.radius = 0.8f;
        }

        public override void OnNetworkSpawn()
        {
            ActualizarVisual();
            TipoMejora.OnValueChanged += (ant, act) => ActualizarVisual();
            CostoOro.OnValueChanged += (ant, act) => ActualizarVisual();
        }

        public void Configurar(TipoMejoraRun tipo, int costoOro = 0)
        {
            if (NetworkManager != null && NetworkManager.IsListening && IsSpawned && !IsServer) return;
            TipoMejora.Value = (byte)tipo;
            CostoOro.Value = Mathf.Max(0, costoOro);
            ActualizarVisual();
        }

        public static void AsegurarSpritesMejoras()
        {
            if (_spritePescadoEsqueleto == null)
                _spritePescadoEsqueleto = CrearSpritePescado(modo: 1); // 1: Solo cabeza bien + cuerpo de huesos (+20)
            if (_spritePescadoMedio == null)
                _spritePescadoMedio = CrearSpritePescado(modo: 2);     // 2: Mitad cuerpo bien + mitad esqueleto (+50)
            if (_spritePescadoCompleto == null)
                _spritePescadoCompleto = CrearSpritePescado(modo: 3);  // 3: Pescadito completo (+80)
            if (_spriteEspadaDano == null)
                _spriteEspadaDano = CrearSpriteIconoEfecto(TipoMejoraRun.AumentoDano);
            if (_spriteEscudoDefensa == null)
                _spriteEscudoDefensa = CrearSpriteIconoEfecto(TipoMejoraRun.AumentoDefensa);
            if (_spriteBotaVelocidad == null)
                _spriteBotaVelocidad = CrearSpriteIconoEfecto(TipoMejoraRun.AumentoVelocidad);
            if (_spriteCorazonVidaMax == null)
                _spriteCorazonVidaMax = CrearSpriteIconoEfecto(TipoMejoraRun.AumentoVidaMaxima);
        }

        public static Sprite ObtenerSpriteMejora(TipoMejoraRun tipo)
        {
            AsegurarSpritesMejoras();
            switch (tipo)
            {
                case TipoMejoraRun.PescadoEsqueleto: return _spritePescadoEsqueleto;
                case TipoMejoraRun.PescadoMedio:     return _spritePescadoMedio;
                case TipoMejoraRun.PescadoCompleto:  return _spritePescadoCompleto;
                case TipoMejoraRun.AumentoDano:      return _spriteEspadaDano;
                case TipoMejoraRun.AumentoDefensa:   return _spriteEscudoDefensa;
                case TipoMejoraRun.AumentoVelocidad: return _spriteBotaVelocidad;
                case TipoMejoraRun.AumentoVidaMaxima: return _spriteCorazonVidaMax;
                default: return _spriteEspadaDano;
            }
        }

        /// <summary>
        /// Genera un icono pixel-art (22x22) para cada consumible de efecto:
        /// - AumentoDano: Espada ígnea carmesí/dorada
        /// - AumentoDefensa: Escudo rúnico cian/plata
        /// - AumentoVelocidad: Bota alada amarilla/blanca
        /// - AumentoVidaMaxima: Corazón dorado/rubí
        /// </summary>
        private static Sprite CrearSpriteIconoEfecto(TipoMejoraRun tipo)
        {
            const int S = 22;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[,] px = new Color[S, S];
            Color cBorde = new Color(0.06f, 0.08f, 0.14f, 1f);

            if (tipo == TipoMejoraRun.AumentoDano)
            {
                // Espada diagonal brillante (Fuerza / Daño)
                Color cFiloLuz = new Color(1f, 0.92f, 0.55f, 1f);
                Color cFiloMed = new Color(1f, 0.48f, 0.15f, 1f);
                Color cFiloOsc = new Color(0.82f, 0.18f, 0.12f, 1f);
                Color cGuarda  = new Color(1f, 0.82f, 0.20f, 1f);
                Color cMango   = new Color(0.48f, 0.26f, 0.14f, 1f);

                // Hoja diagonal desde (7,7) hasta (18,18)
                for (int i = 7; i <= 18; i++)
                {
                    px[i, i] = cFiloLuz;
                    if (i < 18) px[i, i + 1] = cFiloMed;
                    if (i < 18) px[i + 1, i] = cFiloOsc;
                    if (i < 16) px[i - 1, i + 1] = cFiloMed;
                    if (i < 16) px[i + 1, i - 1] = cFiloOsc;
                }
                // Guarda cruzada
                for (int d = -3; d <= 3; d++)
                {
                    int gx = 7 - d;
                    int gy = 7 + d;
                    if (gx >= 2 && gx < S - 2 && gy >= 2 && gy < S - 2)
                    {
                        px[gx, gy] = cGuarda;
                        px[gx + 1, gy] = cGuarda;
                    }
                }
                // Empuñadura y pomo
                for (int i = 3; i <= 6; i++)
                {
                    px[i, i] = cMango;
                    px[i + 1, i] = cMango;
                }
                px[2, 2] = cGuarda; px[3, 2] = cGuarda; px[2, 3] = cGuarda;
            }
            else if (tipo == TipoMejoraRun.AumentoDefensa)
            {
                // Escudo rúnico cian/plata
                Color cMarco = new Color(0.78f, 0.92f, 1f, 1f);
                Color cCuerpo = new Color(0.16f, 0.62f, 0.88f, 1f);
                Color cCentro = new Color(0.38f, 0.92f, 1f, 1f);

                for (int y = 3; y <= 17; y++)
                {
                    int halfW = y >= 11 ? 6 : Mathf.Max(1, (y - 2) * 6 / 9);
                    for (int x = 11 - halfW; x <= 10 + halfW; x++)
                    {
                        bool esBordeEscudo = (x == 11 - halfW || x == 10 + halfW || y == 17 || y <= 4);
                        if (esBordeEscudo) px[x, y] = cMarco;
                        else if (x == 10 || x == 11 || y == 11) px[x, y] = cCentro;
                        else px[x, y] = cCuerpo;
                    }
                }
            }
            else if (tipo == TipoMejoraRun.AumentoVelocidad)
            {
                // Bota alada dorada/amarilla con ala blanca/cian
                Color cBotaLuz = new Color(1f, 0.92f, 0.25f, 1f);
                Color cBotaSomb = new Color(0.88f, 0.60f, 0.10f, 1f);
                Color cSuela = new Color(0.35f, 0.22f, 0.12f, 1f);
                Color cAla = new Color(0.92f, 0.98f, 1f, 1f);

                // Suela y pie (x = 5..16, y = 3..7)
                for (int x = 5; x <= 16; x++)
                {
                    px[x, 3] = cSuela;
                    for (int y = 4; y <= 7; y++)
                    {
                        if (x <= 14 || y <= 6)
                            px[x, y] = (y >= 6) ? cBotaLuz : cBotaSomb;
                    }
                }
                // Caña de la bota (x = 6..11, y = 8..15)
                for (int y = 8; y <= 15; y++)
                {
                    for (int x = 6; x <= 11; x++)
                    {
                        px[x, y] = (x >= 8) ? cBotaLuz : cBotaSomb;
                    }
                }
                // Ala de velocidad en el talón/lateral (x = 11..18, y = 10..17)
                for (int dy = 0; dy <= 6; dy++)
                {
                    int y = 10 + dy;
                    int xMin = 11 + dy / 2;
                    int xMax = Mathf.Min(18, 14 + dy);
                    for (int x = xMin; x <= xMax; x++)
                    {
                        px[x, y] = cAla;
                    }
                }
            }
            else
            {
                // Corazón de Titán (Rubí con borde dorado)
                Color cOro = new Color(1f, 0.88f, 0.30f, 1f);
                Color cRojoLuz = new Color(1f, 0.35f, 0.55f, 1f);
                Color cRojoMed = new Color(0.88f, 0.14f, 0.38f, 1f);
                Color cBrillo = Color.white;

                for (int y = 3; y <= 17; y++)
                {
                    for (int x = 3; x <= 18; x++)
                    {
                        float nx = (x - 10.5f) / 6.5f;
                        float ny = (y - 9.5f) / 6.5f;
                        // Ecuación clásica de corazón
                        float eq = Mathf.Pow(nx * nx + ny * ny - 0.65f, 3f) - nx * nx * Mathf.Pow(ny, 3f);
                        if (eq <= 0f)
                        {
                            if (eq > -0.16f) px[x, y] = cOro;
                            else if (x >= 6 && x <= 8 && y >= 12 && y <= 14) px[x, y] = cBrillo;
                            else if (y >= 10) px[x, y] = cRojoLuz;
                            else px[x, y] = cRojoMed;
                        }
                    }
                }
            }

            // Contorno oscuro pixel-art
            Color[,] finalPx = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    if (px[x, y].a > 0.5f)
                    {
                        finalPx[x, y] = px[x, y];
                    }
                    else
                    {
                        bool vecino =
                            (x > 0 && px[x - 1, y].a > 0.5f) ||
                            (x < S - 1 && px[x + 1, y].a > 0.5f) ||
                            (y > 0 && px[x, y - 1].a > 0.5f) ||
                            (y < S - 1 && px[x, y + 1].a > 0.5f);
                        finalPx[x, y] = vecino ? cBorde : Color.clear;
                    }
                }
            }

            for (int x = 0; x < S; x++)
                for (int y = 0; y < S; y++)
                    tex.SetPixel(x, y, finalPx[x, y]);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 20f);
        }

        /// <summary>
        /// Genera los 3 sprites pixel-art de pescaditos (28x20) con borde oscuro nítido:
        /// modo 1 = Cabeza intacta + espina dorsal y costillas de esqueleto (+20 HP)
        /// modo 2 = Mitad delantera del cuerpo intacta + mitad trasera esqueleto (+50 HP)
        /// modo 3 = Pescadito completo y rechoncho de cabeza a cola (+80 HP)
        /// </summary>
        private static Sprite CrearSpritePescado(int modo)
        {
            const int W = 28;
            const int H = 20;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[,] px = new Color[W, H];

            Color cCuerpoOsc = new Color(0.18f, 0.48f, 0.78f, 1f);
            Color cCuerpoMed = new Color(0.28f, 0.68f, 0.96f, 1f);
            Color cCuerpoLuz = new Color(0.58f, 0.88f, 1.00f, 1f);
            Color cPanza     = new Color(0.82f, 0.95f, 1.00f, 1f);
            Color cAleta     = new Color(0.15f, 0.40f, 0.68f, 1f);
            Color cCarneCorte = new Color(1.00f, 0.52f, 0.48f, 1f);
            Color cHueso     = new Color(0.97f, 0.98f, 0.99f, 1f);
            Color cHuesoSomb = new Color(0.78f, 0.83f, 0.88f, 1f);
            Color cBorde     = new Color(0.06f, 0.10f, 0.18f, 1f);

            // 1. CABEZA INTACTA (x = 2..9, presente en los 3 pescados)
            for (int x = 2; x <= 9; x++)
            {
                int halfH = x <= 3 ? 2 : (x <= 5 ? 4 : 5);
                int yMin = 10 - halfH;
                int yMax = 9 + halfH;
                for (int y = yMin; y <= yMax; y++)
                {
                    if (y >= yMax - 1) px[x, y] = cCuerpoOsc;
                    else if (y >= 9) px[x, y] = cCuerpoMed;
                    else if (y >= yMin + 2) px[x, y] = cCuerpoLuz;
                    else px[x, y] = cPanza;
                }
            }
            // Ojo tierno del pescado (x = 5..6, y = 10..11) y línea de branquia (x = 9)
            px[5, 11] = Color.white; px[6, 11] = Color.white;
            px[5, 10] = Color.black; px[6, 10] = Color.white;
            for (int y = 6; y <= 12; y++) px[9, y] = cAleta;

            // 2. CUERPO Y COLA SEGÚN EL MODO (1: Esqueleto, 2: Mitad/Mitad, 3: Completo)
            int xFinCarne = (modo == 1) ? 9 : (modo == 2 ? 15 : 21);

            // Parte con carne/escamas del cuerpo (si modo 2 o modo 3)
            for (int x = 10; x <= xFinCarne; x++)
            {
                int halfH = x <= 14 ? 5 : (x <= 18 ? 4 : (x <= 20 ? 3 : 2));
                int yMin = 10 - halfH;
                int yMax = 9 + halfH;
                for (int y = yMin; y <= yMax; y++)
                {
                    bool escamaBrillo = ((x + y) % 3 == 0) && y >= 8 && y <= 12;
                    if (y >= yMax - 1) px[x, y] = cCuerpoOsc;
                    else if (escamaBrillo) px[x, y] = cPanza;
                    else if (y >= 9) px[x, y] = cCuerpoMed;
                    else if (y >= yMin + 2) px[x, y] = cCuerpoLuz;
                    else px[x, y] = cPanza;
                }

                // Aleta dorsal arriba
                if (x >= 11 && x <= Mathf.Min(xFinCarne, 16))
                {
                    px[x, 15] = cAleta;
                    if (x >= 12 && x <= 14) px[x, 16] = cAleta;
                }
                // Aleta ventral abajo
                if (x >= 11 && x <= Mathf.Min(xFinCarne, 14))
                {
                    px[x, 4] = cAleta;
                }
            }

            // Si es el 2do pescado (mitad cuerpo, mitad esqueleto), marcar el borde de mordida en x = 15
            if (modo == 2)
            {
                for (int y = 6; y <= 13; y++)
                {
                    if (y != 9 && y != 10) px[15, y] = cCarneCorte;
                }
            }

            // Parte de esqueleto (para modo 1 desde x=10, y para modo 2 desde x=16)
            if (modo == 1 || modo == 2)
            {
                int xInicioHueso = (modo == 1) ? 10 : 16;
                // Espina dorsal central horizontal
                for (int x = xInicioHueso; x <= 22; x++)
                {
                    px[x, 9] = cHuesoSomb;
                    px[x, 10] = cHueso;
                }

                // Costillas verticales de hueso
                int[] costillasX = (modo == 1) ? new int[] { 12, 15, 18, 20 } : new int[] { 17, 20 };
                foreach (int cx in costillasX)
                {
                    int altoCostilla = (cx <= 15) ? 4 : (cx <= 18 ? 3 : 2);
                    for (int dy = -altoCostilla; dy <= altoCostilla + 1; dy++)
                    {
                        int y = 9 + dy;
                        if (y >= 2 && y < H - 2)
                        {
                            px[cx, y] = (dy == -altoCostilla || dy == altoCostilla + 1) ? cHuesoSomb : cHueso;
                        }
                    }
                }

                // Aleta caudal de hueso en V al final (x = 22..25)
                for (int dx = 0; dx <= 3; dx++)
                {
                    int x = 22 + dx;
                    px[x, 10 + dx] = cHueso;
                    px[x, 9 - dx] = cHuesoSomb;
                    if (dx == 3)
                    {
                        px[x, 10 + dx - 1] = cHuesoSomb;
                        px[x, 9 - dx + 1] = cHuesoSomb;
                    }
                }
            }
            else
            {
                // Modo 3 (Pescadito Completo): Aleta caudal completa azul/plata (x = 22..25)
                for (int dx = 0; dx <= 3; dx++)
                {
                    int x = 22 + dx;
                    int yTop = 10 + dx + 1;
                    int yBot = 9 - dx - 1;
                    for (int y = yBot; y <= yTop; y++)
                    {
                        if (dx == 3 && Mathf.Abs(y - 9.5f) < 1.2f) continue; // Muesca en V de la cola
                        px[x, y] = (y >= 10) ? cCuerpoMed : cCuerpoLuz;
                    }
                }
            }

            // 3. CONTORNO PIXEL-ART OSCURO ALREDEDOR DE TODA LA SILUETA PARA MÁXIMA NITIDEZ
            Color[,] finalPx = new Color[W, H];
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    if (px[x, y].a > 0.5f)
                    {
                        finalPx[x, y] = px[x, y];
                    }
                    else
                    {
                        bool vecinoSolido =
                            (x > 0 && px[x - 1, y].a > 0.5f) ||
                            (x < W - 1 && px[x + 1, y].a > 0.5f) ||
                            (y > 0 && px[x, y - 1].a > 0.5f) ||
                            (y < H - 1 && px[x, y + 1].a > 0.5f);
                        finalPx[x, y] = vecinoSolido ? cBorde : Color.clear;
                    }
                }
            }

            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    tex.SetPixel(x, y, finalPx[x, y]);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 20f);
        }

        private void CrearTextoPrecioSiNoExiste()
        {
            if (_textoPrecio != null) return;
            var tObj = new GameObject("TextoPrecio");
            tObj.transform.SetParent(transform, false);
            tObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);

            _textoPrecio = tObj.AddComponent<TextMesh>();
            _textoPrecio.font = FuenteUtil.ObtenerFuentePixelArt();
            if (_textoPrecio.font == null)
                _textoPrecio.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var mr = _textoPrecio.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = _textoPrecio.font.material;
                mr.sortingOrder = 10;
            }
            _textoPrecio.fontSize = 20;
            _textoPrecio.characterSize = 0.08f;
            _textoPrecio.alignment = TextAlignment.Center;
            _textoPrecio.anchor = TextAnchor.MiddleCenter;
            _textoPrecio.color = new Color(1f, 0.88f, 0.2f, 1f);
        }

        private void ActualizarVisual()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;
            _sr.sortingOrder = 6;

            TipoMejoraRun tipo = (TipoMejoraRun)TipoMejora.Value;
            _sr.sprite = ObtenerSpriteMejora(tipo);
            _sr.color = Color.white;
            transform.localScale = Vector3.one * 1.15f;

            int costo = CostoOro.Value;
            if (costo > 0)
            {
                CrearTextoPrecioSiNoExiste();
                if (_textoPrecio != null)
                {
                    _textoPrecio.text = $"💰 {costo}";
                    _textoPrecio.gameObject.SetActive(true);
                }
            }
            else if (_textoPrecio != null)
            {
                _textoPrecio.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Animación de flotación visual
            float offset = Mathf.Sin(Time.time * 4f) * 0.12f;
            transform.position = new Vector3(transform.position.x, transform.position.y + offset * Time.deltaTime, transform.position.z);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsServer || _recogido) return;

            var pj = collision.GetComponent<Personaje>();
            if (pj != null && !pj.EstaMuerto.Value)
            {
                int costo = CostoOro.Value;
                if (costo > 0)
                {
                    if (pj.OroRun.Value < costo)
                    {
                        pj.MostrarTextoFlotanteClientRpc($"¡Falta Oro! ({pj.OroRun.Value}/{costo} 💰)", new Color(1f, 0.4f, 0.4f));
                        return;
                    }
                    pj.GastarOro(costo);
                }

                _recogido = true;
                TipoMejoraRun tipo = (TipoMejoraRun)TipoMejora.Value;
                pj.AplicarMejoraRun(tipo);

                var def = CatalogoMejorasRun.Obtener(tipo);
                string nombreMejora = def != null ? def.nombre : "¡Mejora!";
                Color color = def != null ? def.colorTexto : Color.green;

                if (costo > 0)
                {
                    pj.MostrarTextoFlotanteClientRpc($"¡Compraste {nombreMejora}! (-{costo} 💰)", Color.yellow);
                }
                else
                {
                    pj.MostrarTextoFlotanteClientRpc($"¡{nombreMejora}!", color);
                }

                NotificarRecogidoClientRpc((byte)tipo);

                if (NetworkObject != null && NetworkObject.IsSpawned)
                {
                    NetworkObject.Despawn();
                }
                Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void NotificarRecogidoClientRpc(byte tipoByte)
        {
            var tipo = (TipoMejoraRun)tipoByte;
            if (tipo != TipoMejoraRun.PescadoEsqueleto && tipo != TipoMejoraRun.PescadoMedio && tipo != TipoMejoraRun.PescadoCompleto)
            {
                GestorAudio.PlaySFX(TipoSFX.Mejora);
            }
        }
    }
}
