using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla minijuego cooperativa de combinación para el cofre grande de 50 de oro.
    /// Un laberinto con 4 bolitas elementales (empiezan en cada esquina).
    /// Los jugadores mueven las bolitas arrastrándolas con el cursor hasta su receptáculo central correspondiente.
    /// Las bolitas colisionan con los muros del laberinto Y colisionan entre sí, por lo que los jugadores
    /// interactúan en tiempo real y se afectan mutuamente (como el puzzle de switch de Among Us).
    /// </summary>
    public class ScreenPuzzleCofre : MonoBehaviour
    {
        public static ScreenPuzzleCofre Instancia { get; private set; }

        private CofreOro _cofreActual;
        private CanvasGroup _canvasGroup;
        private RectTransform _panelTablero;
        private Image _fondoOscuro;

        private const float RADIO_BOLITA = 18f;
        private const float DIAMETRO_COLISION = 36f;
        private const float TOLERANCIA_META = 22f;

        // 4 bolitas: 0=Fuego(Roja), 1=Hielo(Cian), 2=Rayo(Amarilla), 3=Planta(Verde)
        private readonly Vector2[] _posIniciales = new Vector2[]
        {
            new Vector2(-170f, 170f),  // Top-Left
            new Vector2(170f, 170f),   // Top-Right
            new Vector2(-170f, -170f), // Bottom-Left
            new Vector2(170f, -170f)   // Bottom-Right
        };

        private readonly Vector2[] _metas = new Vector2[]
        {
            new Vector2(-50f, 50f),    // Receptáculo Fuego
            new Vector2(50f, 50f),     // Receptáculo Hielo
            new Vector2(-50f, -50f),   // Receptáculo Rayo
            new Vector2(50f, -50f)     // Receptáculo Planta
        };

        private readonly Color[] _colores = new Color[]
        {
            new Color(1f, 0.32f, 0.22f),  // Fuego
            new Color(0.25f, 0.85f, 1f),  // Hielo
            new Color(1f, 0.95f, 0.25f),  // Rayo
            new Color(0.35f, 0.95f, 0.45f) // Planta
        };

        private readonly string[] _nombresElementos = new string[]
        {
            "Fuego", "Hielo", "Rayo", "Naturaleza"
        };

        // Posiciones actuales de las bolitas en espacio local del tablero
        public readonly Vector2[] posicionesBolitas = new Vector2[4];
        public readonly bool[] enMeta = new bool[4];

        private RectTransform[] _visualBolitas = new RectTransform[4];
        private Image[] _imgBolitas = new Image[4];
        private Image[] _imgMetas = new Image[4];

        private int _bolitaAgarrada = -1;
        private bool _estaAbierto = false;
        private bool _resuelto = false;
        private float _tiempoUltimoEnvioRed = 0f;

        // Paredes del laberinto (rectángulos en coordenadas locales del tablero [-220..220, -220..220])
        private readonly List<Rect> _murosLaberinto = new List<Rect>();

        void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
            }
            else if (Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            ConstruirMurosLaberinto();
            ConstruirInterfaz();
            ResetearPosiciones();
            CerrarInstantaneo();
        }

        private void ConstruirMurosLaberinto()
        {
            _murosLaberinto.Clear();

            // Bordes exteriores (grosor 20)
            _murosLaberinto.Add(new Rect(-240, 220, 480, 20));  // Techo
            _murosLaberinto.Add(new Rect(-240, -240, 480, 20)); // Suelo
            _murosLaberinto.Add(new Rect(-240, -240, 20, 480)); // Pared Izq
            _murosLaberinto.Add(new Rect(220, -240, 20, 480));  // Pared Der

            // Tabiques internos formando pasadizos de ~44px de ancho (1 bolita de ancho exacto)
            // Cuadrante Superior Izquierdo
            _murosLaberinto.Add(new Rect(-130, 80, 16, 140));
            _murosLaberinto.Add(new Rect(-220, 80, 90, 16));

            // Cuadrante Superior Derecho
            _murosLaberinto.Add(new Rect(114, 80, 16, 140));
            _murosLaberinto.Add(new Rect(130, 80, 90, 16));

            // Cuadrante Inferior Izquierdo
            _murosLaberinto.Add(new Rect(-130, -220, 16, 140));
            _murosLaberinto.Add(new Rect(-220, -96, 90, 16));

            // Cuadrante Inferior Derecho
            _murosLaberinto.Add(new Rect(114, -220, 16, 140));
            _murosLaberinto.Add(new Rect(130, -96, 90, 16));

            // Chokes de acceso al recinto central
            _murosLaberinto.Add(new Rect(-90, 0, 40, 16));
            _murosLaberinto.Add(new Rect(50, 0, 40, 16));
            _murosLaberinto.Add(new Rect(-8, 50, 16, 40));
            _murosLaberinto.Add(new Rect(-8, -90, 16, 40));
        }

        public void ResetearPosiciones()
        {
            for (int i = 0; i < 4; i++)
            {
                posicionesBolitas[i] = _posIniciales[i];
                enMeta[i] = false;
            }
            _resuelto = false;
            ActualizarVisuales();
        }

        private void ConstruirInterfaz()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 25000;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var rtFondo = gameObject.GetComponent<RectTransform>();
            if (rtFondo == null) rtFondo = gameObject.AddComponent<RectTransform>();
            rtFondo.anchorMin = Vector2.zero;
            rtFondo.anchorMax = Vector2.one;
            rtFondo.sizeDelta = Vector2.zero;

            _fondoOscuro = gameObject.GetComponent<Image>();
            if (_fondoOscuro == null) _fondoOscuro = gameObject.AddComponent<Image>();
            _fondoOscuro.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            // Tablero Central (520 x 520 px)
            var tableroObj = new GameObject("TableroLaberinto");
            tableroObj.transform.SetParent(transform, false);
            _panelTablero = tableroObj.AddComponent<RectTransform>();
            _panelTablero.sizeDelta = new Vector2(520f, 520f);
            _panelTablero.anchoredPosition = new Vector2(0f, -15f);

            var imgTablero = tableroObj.AddComponent<Image>();
            imgTablero.color = new Color(0.10f, 0.12f, 0.18f, 1f);

            // Título y Cabecera
            var headerObj = new GameObject("HeaderPuzzle");
            headerObj.transform.SetParent(transform, false);
            var rtHeader = headerObj.AddComponent<RectTransform>();
            rtHeader.anchoredPosition = new Vector2(0f, 320f);
            rtHeader.sizeDelta = new Vector2(700f, 80f);
            var txtHeader = headerObj.AddComponent<Text>();
            txtHeader.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtHeader.fontSize = 28;
            txtHeader.alignment = TextAnchor.MiddleCenter;
            txtHeader.text = "🔐 MECANISMO DE COMBINACIÓN ANCESTRAL (50 ORO)\n<size=18><color=#ffd700>Arrastra las 4 gemas a sus receptáculos centrales. ¡Pueden cooperar!</color></size>";
            txtHeader.color = Color.white;

            // Botón Cerrar
            var btnCerrarObj = new GameObject("BtnCerrar");
            btnCerrarObj.transform.SetParent(transform, false);
            var rtBtn = btnCerrarObj.AddComponent<RectTransform>();
            rtBtn.anchoredPosition = new Vector2(280f, 260f);
            rtBtn.sizeDelta = new Vector2(44f, 44f);
            var imgBtn = btnCerrarObj.AddComponent<Image>();
            imgBtn.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var btn = btnCerrarObj.AddComponent<Button>();
            btn.onClick.AddListener(Cerrar);

            var txtBtnObj = new GameObject("TxtX");
            txtBtnObj.transform.SetParent(btnCerrarObj.transform, false);
            var txtX = txtBtnObj.AddComponent<Text>();
            txtX.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtX.fontSize = 24;
            txtX.alignment = TextAnchor.MiddleCenter;
            txtX.text = "X";
            txtX.color = Color.white;

            // Renderizar Muros del Laberinto en el Tablero
            foreach (var m in _murosLaberinto)
            {
                var muroUI = new GameObject("MuroUI");
                muroUI.transform.SetParent(_panelTablero, false);
                var rtMuro = muroUI.AddComponent<RectTransform>();
                rtMuro.sizeDelta = new Vector2(m.width, m.height);
                rtMuro.anchoredPosition = new Vector2(m.x + m.width * 0.5f, m.y + m.height * 0.5f);
                var imgMuro = muroUI.AddComponent<Image>();
                imgMuro.color = new Color(0.28f, 0.32f, 0.44f, 1f);
            }

            // Crear Receptáculos (Metas) en el centro
            for (int i = 0; i < 4; i++)
            {
                var metaObj = new GameObject($"Meta_{i}_{_nombresElementos[i]}");
                metaObj.transform.SetParent(_panelTablero, false);
                var rtMeta = metaObj.AddComponent<RectTransform>();
                rtMeta.sizeDelta = new Vector2(44f, 44f);
                rtMeta.anchoredPosition = _metas[i];
                var imgMeta = metaObj.AddComponent<Image>();
                imgMeta.color = new Color(_colores[i].r, _colores[i].g, _colores[i].b, 0.35f);
                _imgMetas[i] = imgMeta;

                // Anillo exterior decorativo
                var bordeObj = new GameObject("Borde");
                bordeObj.transform.SetParent(metaObj.transform, false);
                var rtBorde = bordeObj.AddComponent<RectTransform>();
                rtBorde.sizeDelta = new Vector2(40f, 40f);
                var imgBorde = bordeObj.AddComponent<Image>();
                imgBorde.color = new Color(_colores[i].r, _colores[i].g, _colores[i].b, 0.85f);
            }

            // Crear las 4 Bolitas
            for (int i = 0; i < 4; i++)
            {
                var bolitaObj = new GameObject($"Bolita_{i}_{_nombresElementos[i]}");
                bolitaObj.transform.SetParent(_panelTablero, false);
                var rtB = bolitaObj.AddComponent<RectTransform>();
                rtB.sizeDelta = new Vector2(RADIO_BOLITA * 2f, RADIO_BOLITA * 2f);
                rtB.anchoredPosition = posicionesBolitas[i];
                var imgB = bolitaObj.AddComponent<Image>();
                imgB.color = _colores[i];

                _visualBolitas[i] = rtB;
                _imgBolitas[i] = imgB;

                // Destello interior brillante
                var brilloObj = new GameObject("Brillo");
                brilloObj.transform.SetParent(bolitaObj.transform, false);
                var rtBrillo = brilloObj.AddComponent<RectTransform>();
                rtBrillo.sizeDelta = new Vector2(12f, 12f);
                rtBrillo.anchoredPosition = new Vector2(-4f, 4f);
                var imgBrillo = brilloObj.AddComponent<Image>();
                imgBrillo.color = new Color(1f, 1f, 1f, 0.85f);
            }
        }

        public void Abrir(CofreOro cofre)
        {
            if (cofre == null || cofre.EstaAbiertoLocal) return;
            _cofreActual = cofre;
            _estaAbierto = true;

            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        public void Cerrar()
        {
            _estaAbierto = false;
            _bolitaAgarrada = -1;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private void CerrarInstantaneo()
        {
            _estaAbierto = false;
            _bolitaAgarrada = -1;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_estaAbierto || _resuelto) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
                return;
            }

            ProcesarInteraccionCursor();
            VerificarVictoria();
        }

        private void ProcesarInteraccionCursor()
        {
            Vector2 mousePantalla = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panelTablero,
                mousePantalla,
                null,
                out Vector2 mouseLocal);

            // Al hacer clic, buscar si agarramos alguna bolita
            if (Input.GetMouseButtonDown(0))
            {
                int mejorIndice = -1;
                float menorDist = 32f;
                for (int i = 0; i < 4; i++)
                {
                    float d = Vector2.Distance(mouseLocal, posicionesBolitas[i]);
                    if (d < menorDist)
                    {
                        menorDist = d;
                        mejorIndice = i;
                    }
                }
                _bolitaAgarrada = mejorIndice;
            }

            // Soltar
            if (Input.GetMouseButtonUp(0))
            {
                _bolitaAgarrada = -1;
            }

            // Arrastrar bolita
            if (_bolitaAgarrada >= 0 && Input.GetMouseButton(0))
            {
                MoverBolitaConFisica(_bolitaAgarrada, mouseLocal);

                // Sincronizar en red si está activo
                if (Time.time - _tiempoUltimoEnvioRed >= 0.04f) // ~25Hz
                {
                    _tiempoUltimoEnvioRed = Time.time;
                    if (SalaRed.Instancia != null)
                    {
                        SalaRed.Instancia.ActualizarPosicionBolitaServerRpc(_bolitaAgarrada, posicionesBolitas[_bolitaAgarrada]);
                    }
                }
            }

            ActualizarVisuales();
        }

        private void MoverBolitaConFisica(int indice, Vector2 destinoDeseado)
        {
            Vector2 posActual = posicionesBolitas[indice];
            Vector2 paso = destinoDeseado - posActual;
            float dist = paso.magnitude;
            if (dist < 0.001f) return;

            // Movimiento suave y acotado
            float velocidadMax = 450f * Time.deltaTime;
            Vector2 delta = paso.normalized * Mathf.Min(dist, velocidadMax);
            Vector2 cand = posActual + delta;

            // 1. Colisión con los Muros del Laberinto (deslizamiento por ejes)
            Vector2 candX = new Vector2(cand.x, posActual.y);
            if (!ColisionaConMuros(candX))
            {
                posActual.x = candX.x;
            }
            Vector2 candY = new Vector2(posActual.x, cand.y);
            if (!ColisionaConMuros(candY))
            {
                posActual.y = candY.y;
            }

            // 2. Colisión física entre Bolitas (se empujan y bloquean entre sí)
            for (int j = 0; j < 4; j++)
            {
                if (j == indice) continue;
                Vector2 diff = posActual - posicionesBolitas[j];
                float dBol = diff.magnitude;
                if (dBol < DIAMETRO_COLISION)
                {
                    if (dBol < 0.001f) diff = Vector2.right * 0.1f;
                    Vector2 dirSep = diff.normalized;
                    float penetracion = DIAMETRO_COLISION - dBol;

                    // Empujar ambas bolitas (o la otra si una está agarrada)
                    Vector2 nuevaOtra = posicionesBolitas[j] - dirSep * (penetracion * 0.5f);
                    if (!ColisionaConMuros(nuevaOtra))
                    {
                        posicionesBolitas[j] = nuevaOtra;
                    }

                    posActual += dirSep * (penetracion * 0.5f);
                }
            }

            // 3. Imán/Snap hacia su receptáculo meta si está muy cerca
            float distMeta = Vector2.Distance(posActual, _metas[indice]);
            if (distMeta <= TOLERANCIA_META)
            {
                posActual = Vector2.MoveTowards(posActual, _metas[indice], 120f * Time.deltaTime);
                if (distMeta <= 6f && !enMeta[indice])
                {
                    posActual = _metas[indice];
                    enMeta[indice] = true;
                    GestorAudio.PlaySFX(TipoSFX.Mejora, 1.15f);
                }
            }
            else
            {
                enMeta[indice] = false;
            }

            posicionesBolitas[indice] = posActual;
        }

        private bool ColisionaConMuros(Vector2 pos)
        {
            // Bounding box circular aproximado vs rectángulos de muros
            for (int i = 0; i < _murosLaberinto.Count; i++)
            {
                Rect r = _murosLaberinto[i];
                // Expandir muro por el radio de la bolita
                if (pos.x >= r.x - RADIO_BOLITA && pos.x <= r.xMax + RADIO_BOLITA &&
                    pos.y >= r.y - RADIO_BOLITA && pos.y <= r.yMax + RADIO_BOLITA)
                {
                    return true;
                }
            }
            return false;
        }

        public void AplicarPosicionRemota(int bolitaIdx, Vector2 pos)
        {
            if (bolitaIdx >= 0 && bolitaIdx < 4)
            {
                posicionesBolitas[bolitaIdx] = pos;
                enMeta[bolitaIdx] = (Vector2.Distance(pos, _metas[bolitaIdx]) <= 8f);
                ActualizarVisuales();
            }
        }

        private void ActualizarVisuales()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_visualBolitas[i] != null)
                {
                    _visualBolitas[i].anchoredPosition = posicionesBolitas[i];
                    if (enMeta[i])
                    {
                        _imgMetas[i].color = new Color(_colores[i].r, _colores[i].g, _colores[i].b, 0.90f);
                        _visualBolitas[i].localScale = Vector3.one * (1.15f + Mathf.Sin(Time.time * 8f) * 0.08f);
                    }
                    else
                    {
                        _imgMetas[i].color = new Color(_colores[i].r, _colores[i].g, _colores[i].b, 0.35f);
                        _visualBolitas[i].localScale = Vector3.one;
                    }
                }
            }
        }

        private void VerificarVictoria()
        {
            bool todasEnMeta = true;
            for (int i = 0; i < 4; i++)
            {
                if (Vector2.Distance(posicionesBolitas[i], _metas[i]) > 10f)
                {
                    todasEnMeta = false;
                    break;
                }
            }

            if (todasEnMeta && !_resuelto)
            {
                _resuelto = true;
                StartCoroutine(RutinaVictoria());
            }
        }

        private IEnumerator RutinaVictoria()
        {
            GestorAudio.PlaySFX(TipoSFX.Victoria, 1.2f);
            TextoFlotante.Crear(Vector3.zero, "✨ ¡COMBINACIÓN RESUELTA! ✨", new Color(1f, 0.92f, 0.35f));

            yield return new WaitForSeconds(0.7f);

            if (_cofreActual != null)
            {
                Personaje local = null;
                for (int i = 0; i < Personaje.Todos.Count; i++)
                {
                    if (Personaje.Todos[i] != null && Personaje.Todos[i].EsControlLocal)
                    {
                        local = Personaje.Todos[i];
                        break;
                    }
                }

                if (local != null)
                {
                    local.InteractuarCofreDirecto(_cofreActual);
                }
                else
                {
                    _cofreActual.AbrirPorServidor();
                }
            }

            Cerrar();
        }
    }
}
