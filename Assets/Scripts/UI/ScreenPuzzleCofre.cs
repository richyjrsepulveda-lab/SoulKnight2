using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla minijuego cooperativa de combinación para el cofre grande de 50 de oro.
    /// Diseñada como una cerradura mecánica ancestral (vault lock / tumbler lock).
    /// Un laberinto de engranajes y tambores con 4 pernos elementales en las 4 esquinas.
    /// Los jugadores arrastran los pernos con el cursor sorteando los obstáculos mecánicos
    /// hasta introducirlos en los tambores de la cerradura central.
    /// Las esferas colisionan físicamente entre sí (estilo Among Us) y se deslizan suavemente por los muros.
    /// </summary>
    public class ScreenPuzzleCofre : MonoBehaviour
    {
        public static ScreenPuzzleCofre Instancia { get; private set; }

        private CofreOro _cofreActual;
        private CanvasGroup _canvasGroup;
        private RectTransform _panelVentana;
        private RectTransform _panelTablero;

        private const float RADIO_BOLITA = 14f;
        private const float DIAMETRO_COLISION = 28f;
        private const float TOLERANCIA_META = 24f;

        // 4 Pernos Elementales: 0=Fuego(Rojo), 1=Hielo(Cian), 2=Rayo(Amarillo), 3=Planta(Verde)
        private readonly Vector2[] _posIniciales = new Vector2[]
        {
            new Vector2(-220f, 220f),  // Top-Left (Fuego)
            new Vector2(220f, 220f),   // Top-Right (Hielo)
            new Vector2(-220f, -220f), // Bottom-Left (Rayo)
            new Vector2(220f, -220f)   // Bottom-Right (Planta)
        };

        private readonly Vector2[] _metas = new Vector2[]
        {
            new Vector2(-45f, 45f),    // Tambor Fuego
            new Vector2(45f, 45f),     // Tambor Hielo
            new Vector2(-45f, -45f),   // Tambor Rayo
            new Vector2(45f, -45f)     // Tambor Planta
        };

        private readonly Color[] _coloresBase = new Color[]
        {
            new Color(1f, 0.25f, 0.18f),  // Fuego
            new Color(0.18f, 0.85f, 1f),  // Hielo
            new Color(1f, 0.92f, 0.20f),  // Rayo
            new Color(0.22f, 0.95f, 0.42f) // Planta
        };

        private readonly Color[] _coloresBrillo = new Color[]
        {
            new Color(1f, 0.85f, 0.80f),
            new Color(0.85f, 0.98f, 1f),
            new Color(1f, 1f, 0.80f),
            new Color(0.85f, 1f, 0.88f)
        };

        private readonly string[] _nombresElementos = new string[]
        {
            "Fuego", "Hielo", "Rayo", "Planta"
        };

        // Posiciones locales en el tablero [-260..260]
        public readonly Vector2[] posicionesBolitas = new Vector2[4];
        public readonly bool[] enMeta = new bool[4];

        private RectTransform[] _visualBolitas = new RectTransform[4];
        private Image[] _imgBolitas = new Image[4];
        private Image[] _imgMetas = new Image[4];

        private int _bolitaAgarrada = -1;
        private bool _estaAbierto = false;
        private bool _resuelto = false;
        private float _tiempoUltimoEnvioRed = 0f;

        // Paredes mecánicas del laberinto (rectángulos locales)
        private readonly List<Rect> _murosLaberinto = new List<Rect>();

        private static Sprite[] _spritesPernos;
        private static Sprite[] _spritesTambores;
        private static Sprite _spritePlacaCerradura;
        private static Sprite _spriteMuroCerradura;
        private static Sprite _spriteOjoCerradura;

        void Awake()
        {
            if (Instancia == null)
            {
                Instancia = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            ConstruirMurosLaberinto();
            AsegurarTexturasProcedurales();
            ConstruirInterfaz();
            ResetearPosiciones();
            CerrarInstantaneo();
        }

        void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
                Controles.Bloqueado = false;
            }
        }

        private void ConstruirMurosLaberinto()
        {
            _murosLaberinto.Clear();

            // 1. Bordes exteriores del área jugable (recinto circular/cuadrado [-260..260], grosor 16)
            _murosLaberinto.Add(new Rect(-276, 260, 552, 16));   // Techo
            _murosLaberinto.Add(new Rect(-276, -276, 552, 16));  // Suelo
            _murosLaberinto.Add(new Rect(-276, -276, 16, 552));  // Pared Izq
            _murosLaberinto.Add(new Rect(260, -276, 16, 552));   // Pared Der

            // 2. Bastidores de acceso a la Cámara Central de la Cerradura (recinto central [-75..75])
            // Postes del portón norte
            _murosLaberinto.Add(new Rect(-75, 75, 45, 14));
            _murosLaberinto.Add(new Rect(30, 75, 45, 14));
            // Postes del portón sur
            _murosLaberinto.Add(new Rect(-75, -89, 45, 14));
            _murosLaberinto.Add(new Rect(30, -89, 45, 14));
            // Postes del portón oeste
            _murosLaberinto.Add(new Rect(-89, -75, 14, 45));
            _murosLaberinto.Add(new Rect(-89, 30, 14, 45));
            // Postes del portón este
            _murosLaberinto.Add(new Rect(75, -75, 14, 45));
            _murosLaberinto.Add(new Rect(75, 30, 14, 45));

            // 3. Wards y tambores mecánicos laberínticos (4 cuadrantes balanceados y 100% navegables):
            // Cuadrante II: Top-Left (Fuego)
            _murosLaberinto.Add(new Rect(-215, 175, 120, 14)); // Deflector superior TL
            _murosLaberinto.Add(new Rect(-170, 75, 14, 100));  // Tabique vertical TL
            _murosLaberinto.Add(new Rect(-170, 75, 60, 14));   // Repisa interna TL
            _murosLaberinto.Add(new Rect(-60, 125, 14, 90));   // Chicana vertical TL
            _murosLaberinto.Add(new Rect(-110, 125, 50, 14));  // Brazo deflector TL

            // Cuadrante I: Top-Right (Hielo)
            _murosLaberinto.Add(new Rect(95, 175, 120, 14));
            _murosLaberinto.Add(new Rect(156, 75, 14, 100));
            _murosLaberinto.Add(new Rect(110, 75, 60, 14));
            _murosLaberinto.Add(new Rect(46, 125, 14, 90));
            _murosLaberinto.Add(new Rect(60, 125, 50, 14));

            // Cuadrante III: Bottom-Left (Rayo)
            _murosLaberinto.Add(new Rect(-215, -189, 120, 14));
            _murosLaberinto.Add(new Rect(-170, -175, 14, 100));
            _murosLaberinto.Add(new Rect(-170, -89, 60, 14));
            _murosLaberinto.Add(new Rect(-60, -215, 14, 90));
            _murosLaberinto.Add(new Rect(-110, -139, 50, 14));

            // Cuadrante IV: Bottom-Right (Planta)
            _murosLaberinto.Add(new Rect(95, -189, 120, 14));
            _murosLaberinto.Add(new Rect(156, -175, 14, 100));
            _murosLaberinto.Add(new Rect(110, -89, 60, 14));
            _murosLaberinto.Add(new Rect(46, -215, 14, 90));
            _murosLaberinto.Add(new Rect(60, -139, 50, 14));
        }

        public void ResetearPosiciones()
        {
            for (int i = 0; i < 4; i++)
            {
                posicionesBolitas[i] = _posIniciales[i];
                enMeta[i] = false;
            }
            _bolitaAgarrada = -1;
            _resuelto = false;
            ActualizarVisuales();
        }

        private void ConstruirInterfaz()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;

            // Canvas en ScreenSpaceOverlay con resolución de referencia 1080p
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 25000;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Fondo translúcido oscuro cinematográfico
            var rtRoot = GetComponent<RectTransform>();
            if (rtRoot == null) rtRoot = gameObject.AddComponent<RectTransform>();
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.offsetMin = Vector2.zero;
            rtRoot.offsetMax = Vector2.zero;

            var imgFondo = GetComponent<Image>();
            if (imgFondo == null) imgFondo = gameObject.AddComponent<Image>();
            imgFondo.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            // Ventana modal de la cerradura (740 x 780 px - Grandecita y envolvente)
            var ventanaObj = new GameObject("PanelVentanaCerradura");
            ventanaObj.transform.SetParent(transform, false);
            _panelVentana = ventanaObj.AddComponent<RectTransform>();
            _panelVentana.anchorMin = new Vector2(0.5f, 0.5f);
            _panelVentana.anchorMax = new Vector2(0.5f, 0.5f);
            _panelVentana.pivot = new Vector2(0.5f, 0.5f);
            _panelVentana.sizeDelta = new Vector2(740f, 780f);
            _panelVentana.anchoredPosition = Vector2.zero;

            var imgVentana = ventanaObj.AddComponent<Image>();
            imgVentana.color = new Color(0.10f, 0.11f, 0.15f, 0.98f);

            // Borde biselado de bronce y remaches en las esquinas
            var bordeObj = new GameObject("BordeBronce");
            bordeObj.transform.SetParent(ventanaObj.transform, false);
            var rtBorde = bordeObj.AddComponent<RectTransform>();
            rtBorde.anchorMin = Vector2.zero;
            rtBorde.anchorMax = Vector2.one;
            rtBorde.sizeDelta = Vector2.zero;
            var outl = bordeObj.AddComponent<Outline>();
            outl.effectColor = new Color(0.82f, 0.62f, 0.22f, 0.85f);
            outl.effectDistance = new Vector2(4f, -4f);

            // Barra de Título Superior (Estilo Placa de Cerradura Antigua)
            var headerObj = new GameObject("HeaderCerradura");
            headerObj.transform.SetParent(ventanaObj.transform, false);
            var rtHeader = headerObj.AddComponent<RectTransform>();
            rtHeader.anchorMin = new Vector2(0f, 1f);
            rtHeader.anchorMax = new Vector2(1f, 1f);
            rtHeader.pivot = new Vector2(0.5f, 1f);
            rtHeader.anchoredPosition = new Vector2(0f, -10f);
            rtHeader.sizeDelta = new Vector2(-28f, 60f);

            var txtHeader = headerObj.AddComponent<Text>();
            txtHeader.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtHeader.fontSize = 23;
            txtHeader.alignment = TextAnchor.MiddleCenter;
            txtHeader.text = "🔐 CERRADURA MECÁNICA ANCESTRAL (50 ORO)\n<size=15><color=#f5c642>Guía los 4 pernos elementales por el laberinto de la cerradura hasta el tambor central</color></size>";
            txtHeader.color = Color.white;

            // Botón de Cierre [✕]
            var btnCerrarObj = new GameObject("BtnCerrar");
            btnCerrarObj.transform.SetParent(ventanaObj.transform, false);
            var rtBtn = btnCerrarObj.AddComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(1f, 1f);
            rtBtn.anchorMax = new Vector2(1f, 1f);
            rtBtn.pivot = new Vector2(1f, 1f);
            rtBtn.anchoredPosition = new Vector2(-14f, -14f);
            rtBtn.sizeDelta = new Vector2(38f, 38f);

            var imgBtn = btnCerrarObj.AddComponent<Image>();
            imgBtn.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            var btn = btnCerrarObj.AddComponent<Button>();
            btn.onClick.AddListener(Cerrar);

            var txtBtnObj = new GameObject("TxtX");
            txtBtnObj.transform.SetParent(btnCerrarObj.transform, false);
            var rtTxtX = txtBtnObj.AddComponent<RectTransform>();
            rtTxtX.anchorMin = Vector2.zero;
            rtTxtX.anchorMax = Vector2.one;
            rtTxtX.sizeDelta = Vector2.zero;
            var txtX = txtBtnObj.AddComponent<Text>();
            txtX.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtX.fontSize = 22;
            txtX.alignment = TextAnchor.MiddleCenter;
            txtX.text = "✕";
            txtX.color = Color.white;

            // Placa de la Cerradura / Laberinto (620 x 620 px - Tamaño ideal y nítido)
            var tableroObj = new GameObject("TableroCerradura");
            tableroObj.transform.SetParent(ventanaObj.transform, false);
            _panelTablero = tableroObj.AddComponent<RectTransform>();
            _panelTablero.anchorMin = new Vector2(0.5f, 0f);
            _panelTablero.anchorMax = new Vector2(0.5f, 0f);
            _panelTablero.pivot = new Vector2(0.5f, 0f);
            _panelTablero.sizeDelta = new Vector2(620f, 620f);
            _panelTablero.anchoredPosition = new Vector2(0f, 22f);

            var imgTablero = tableroObj.AddComponent<Image>();
            imgTablero.sprite = _spritePlacaCerradura;
            imgTablero.color = Color.white;

            // Emblema Central: Ojo de Cerradura Antiguo grabado en el fondo del tambor
            var ojoObj = new GameObject("OjoCerraduraCentro");
            ojoObj.transform.SetParent(_panelTablero, false);
            var rtOjo = ojoObj.AddComponent<RectTransform>();
            rtOjo.anchorMin = new Vector2(0.5f, 0.5f);
            rtOjo.anchorMax = new Vector2(0.5f, 0.5f);
            rtOjo.pivot = new Vector2(0.5f, 0.5f);
            rtOjo.sizeDelta = new Vector2(64f, 64f);
            rtOjo.anchoredPosition = Vector2.zero;
            var imgOjo = ojoObj.AddComponent<Image>();
            imgOjo.sprite = _spriteOjoCerradura;
            imgOjo.color = new Color(0.85f, 0.70f, 0.30f, 0.40f);

            // Muros y Deflectores Mecánicos del Laberinto
            for (int i = 0; i < _murosLaberinto.Count; i++)
            {
                Rect m = _murosLaberinto[i];
                var muroUI = new GameObject($"MuroCerradura_{i}");
                muroUI.transform.SetParent(_panelTablero, false);
                var rtMuro = muroUI.AddComponent<RectTransform>();
                rtMuro.anchorMin = new Vector2(0.5f, 0.5f);
                rtMuro.anchorMax = new Vector2(0.5f, 0.5f);
                rtMuro.pivot = new Vector2(0.5f, 0.5f);
                rtMuro.sizeDelta = new Vector2(m.width, m.height);
                rtMuro.anchoredPosition = new Vector2(m.x + m.width * 0.5f, m.y + m.height * 0.5f);

                var imgMuro = muroUI.AddComponent<Image>();
                imgMuro.sprite = _spriteMuroCerradura;
                imgMuro.color = Color.white;
            }

            // Tambores / Receptáculos en la Cámara Central
            for (int i = 0; i < 4; i++)
            {
                var metaObj = new GameObject($"Tambor_{i}_{_nombresElementos[i]}");
                metaObj.transform.SetParent(_panelTablero, false);
                var rtMeta = metaObj.AddComponent<RectTransform>();
                rtMeta.anchorMin = new Vector2(0.5f, 0.5f);
                rtMeta.anchorMax = new Vector2(0.5f, 0.5f);
                rtMeta.pivot = new Vector2(0.5f, 0.5f);
                rtMeta.sizeDelta = new Vector2(44f, 44f);
                rtMeta.anchoredPosition = _metas[i];

                var imgMeta = metaObj.AddComponent<Image>();
                imgMeta.sprite = _spritesTambores[i];
                imgMeta.color = new Color(1f, 1f, 1f, 0.55f);
                _imgMetas[i] = imgMeta;
            }

            // 4 Pernos Elementales Móviles
            for (int i = 0; i < 4; i++)
            {
                var pernoObj = new GameObject($"Perno_{i}_{_nombresElementos[i]}");
                pernoObj.transform.SetParent(_panelTablero, false);
                var rtP = pernoObj.AddComponent<RectTransform>();
                rtP.anchorMin = new Vector2(0.5f, 0.5f);
                rtP.anchorMax = new Vector2(0.5f, 0.5f);
                rtP.pivot = new Vector2(0.5f, 0.5f);
                rtP.sizeDelta = new Vector2(RADIO_BOLITA * 2f, RADIO_BOLITA * 2f);
                rtP.anchoredPosition = posicionesBolitas[i];

                var imgP = pernoObj.AddComponent<Image>();
                imgP.sprite = _spritesPernos[i];
                imgP.color = Color.white;

                _visualBolitas[i] = rtP;
                _imgBolitas[i] = imgP;
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

            Controles.Bloqueado = true;
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
        }

        public void Cerrar()
        {
            _estaAbierto = false;
            _bolitaAgarrada = -1;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            Controles.Bloqueado = false;
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
            Controles.Bloqueado = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_estaAbierto) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cerrar();
                return;
            }

            if (_resuelto) return;

            ProcesarInteraccionCursor();
            VerificarVictoria();
        }

        private void ProcesarInteraccionCursor()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panelTablero,
                Input.mousePosition,
                null,
                out Vector2 mouseLocal);

            // Al pulsar clic izquierdo, determinar si agarramos un perno
            if (Input.GetMouseButtonDown(0))
            {
                int mejorIndice = -1;
                float menorDist = 36f;
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

            // Al soltar el clic
            if (Input.GetMouseButtonUp(0))
            {
                _bolitaAgarrada = -1;
            }

            // Arrastrar perno
            if (_bolitaAgarrada >= 0 && Input.GetMouseButton(0))
            {
                MoverPernoConFisica(_bolitaAgarrada, mouseLocal);

                // Enviar sincronización de red (~25Hz)
                if (Time.time - _tiempoUltimoEnvioRed >= 0.04f)
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

        private void MoverPernoConFisica(int indice, Vector2 destinoDeseado)
        {
            Vector2 posActual = posicionesBolitas[indice];
            Vector2 paso = destinoDeseado - posActual;
            float dist = paso.magnitude;
            if (dist < 0.001f) return;

            float velocidadMax = 560f * Time.deltaTime;
            Vector2 delta = paso.normalized * Mathf.Min(dist, velocidadMax);
            Vector2 cand = posActual + delta;

            // 1. Deslizamiento suave contra los muros mecánicos de la cerradura
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

            // 2. Colisión física entre Pernos (se empujan entre sí estilo Among Us)
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

                    Vector2 nuevaOtra = posicionesBolitas[j] - dirSep * (penetracion * 0.5f);
                    if (!ColisionaConMuros(nuevaOtra))
                    {
                        posicionesBolitas[j] = nuevaOtra;
                    }

                    posActual += dirSep * (penetracion * 0.5f);
                }
            }

            // 3. Imán / Encastre en el tambor de la cerradura
            float distMeta = Vector2.Distance(posActual, _metas[indice]);
            if (distMeta <= TOLERANCIA_META)
            {
                posActual = Vector2.MoveTowards(posActual, _metas[indice], 150f * Time.deltaTime);
                if (distMeta <= 5f && !enMeta[indice])
                {
                    posActual = _metas[indice];
                    enMeta[indice] = true;
                    GestorAudio.PlaySFX(TipoSFX.Mejora, 1.25f);
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
            for (int i = 0; i < _murosLaberinto.Count; i++)
            {
                Rect r = _murosLaberinto[i];
                float cx = Mathf.Clamp(pos.x, r.xMin, r.xMax);
                float cy = Mathf.Clamp(pos.y, r.yMin, r.yMax);
                float distSq = (pos.x - cx) * (pos.x - cx) + (pos.y - cy) * (pos.y - cy);
                if (distSq < RADIO_BOLITA * RADIO_BOLITA)
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
                enMeta[bolitaIdx] = (Vector2.Distance(pos, _metas[bolitaIdx]) <= 6f);
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
                        _imgMetas[i].color = new Color(1f, 1f, 1f, 1f);
                        _visualBolitas[i].localScale = Vector3.one * (1.18f + Mathf.Sin(Time.time * 9f) * 0.08f);
                    }
                    else
                    {
                        _imgMetas[i].color = new Color(1f, 1f, 1f, 0.55f);
                        _visualBolitas[i].localScale = Vector3.one;
                    }
                }
            }
        }

        private void VerificarVictoria()
        {
            if (_resuelto) return;

            bool todasEnMeta = enMeta[0] && enMeta[1] && enMeta[2] && enMeta[3];
            if (todasEnMeta)
            {
                _resuelto = true;
                StartCoroutine(RutinaVictoria());
            }
        }

        private IEnumerator RutinaVictoria()
        {
            GestorAudio.PlaySFX(TipoSFX.Mejora, 1.4f);
            GestorAudio.PlaySFX(TipoSFX.RecogerOro, 1.25f);

            if (_cofreActual != null)
            {
                TextoFlotante.Crear(_cofreActual.transform.position + Vector3.up * 1.5f, "¡CERRADURA DESBLOQUEADA! +50 💰", new Color(1f, 0.95f, 0.25f));
                _cofreActual.AbrirPorServidor();
            }

            yield return new WaitForSeconds(0.75f);
            Cerrar();
        }

        private static void AsegurarTexturasProcedurales()
        {
            if (_spritesPernos != null && _spritesTambores != null && _spritePlacaCerradura != null && _spriteMuroCerradura != null) return;

            Color[] cBases = new Color[]
            {
                new Color(1f, 0.25f, 0.18f),  // Fuego
                new Color(0.18f, 0.85f, 1f),  // Hielo
                new Color(1f, 0.92f, 0.20f),  // Rayo
                new Color(0.22f, 0.95f, 0.42f) // Planta
            };

            Color[] cBrillos = new Color[]
            {
                new Color(1f, 0.85f, 0.80f),
                new Color(0.85f, 0.98f, 1f),
                new Color(1f, 1f, 0.80f),
                new Color(0.85f, 1f, 0.88f)
            };

            // 1. Textura de la Placa de Cerradura Circular (128x128 con estrías de engranaje y bisel de bronce)
            {
                const int sz = 128;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                Vector2 c = new Vector2(sz * 0.5f - 0.5f, sz * 0.5f - 0.5f);
                float rExt = sz * 0.48f;
                float rAnilloBronce = sz * 0.44f;
                float rInterior = sz * 0.40f;
                float rCamaraCentro = sz * 0.16f;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float d = Vector2.Distance(p, c);
                        float ang = Mathf.Atan2(p.y - c.y, p.x - c.x);

                        if (d > rExt)
                        {
                            tex.SetPixel(x, y, new Color(0.06f, 0.07f, 0.10f, 1f));
                        }
                        else if (d >= rAnilloBronce)
                        {
                            // Anillo exterior de bronce con marcas de dial/dientes
                            bool muesca = (Mathf.Abs(Mathf.Sin(ang * 16f)) > 0.7f);
                            Color cBronce = muesca ? new Color(0.92f, 0.72f, 0.30f) : new Color(0.55f, 0.40f, 0.18f);
                            tex.SetPixel(x, y, cBronce);
                        }
                        else if (d >= rInterior)
                        {
                            // Ranura de sombra metálica
                            tex.SetPixel(x, y, new Color(0.08f, 0.09f, 0.12f));
                        }
                        else if (d <= rCamaraCentro)
                        {
                            // Tambor central oscuro donde se insertan los pernos
                            tex.SetPixel(x, y, new Color(0.04f, 0.05f, 0.07f));
                        }
                        else
                        {
                            // Superficie de la cerradura con estrías circulares concéntricas
                            float estr = Mathf.Sin(d * 1.8f) * 0.04f;
                            Color cSlate = new Color(0.09f + estr, 0.11f + estr, 0.15f + estr);
                            tex.SetPixel(x, y, cSlate);
                        }
                    }
                }
                tex.Apply();
                _spritePlacaCerradura = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            }

            // 2. Emblema Ojo de Cerradura Central (32x32)
            {
                const int sz = 32;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                Vector2 cCirculo = new Vector2(15.5f, 19f);
                float rHead = 6.5f;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float dHead = Vector2.Distance(new Vector2(x, y), cCirculo);
                        bool enCabeza = (dHead <= rHead);
                        bool enRanura = (y <= 16 && y >= 6 && Mathf.Abs(x - 15.5f) <= (16f - y) * 0.35f + 1.8f);

                        if (enCabeza || enRanura)
                        {
                            tex.SetPixel(x, y, new Color(0.92f, 0.75f, 0.32f, 0.9f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                tex.Apply();
                _spriteOjoCerradura = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            }

            // 3. Muros Mecánicos de la Cerradura (16x16 bronce pulido con bisel)
            {
                const int sz = 16;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        bool bordeSupIzq = (x == 0 || y == sz - 1);
                        bool bordeInfDer = (x == sz - 1 || y == 0);
                        if (bordeSupIzq)
                            tex.SetPixel(x, y, new Color(0.90f, 0.74f, 0.38f)); // Brillo dorado
                        else if (bordeInfDer)
                            tex.SetPixel(x, y, new Color(0.25f, 0.18f, 0.08f)); // Sombra bronce
                        else
                            tex.SetPixel(x, y, new Color(0.58f, 0.44f, 0.22f)); // Metal bronce central
                    }
                }
                tex.Apply();
                _spriteMuroCerradura = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 16f);
            }

            // 4. Sprites de los 4 Pernos Elementales (32x32 gema esférica con aro de bronce)
            _spritesPernos = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                const int sz = 32;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                Vector2 centro = new Vector2(sz * 0.5f - 0.5f, sz * 0.5f - 0.5f);
                Vector2 centroLuz = new Vector2(sz * 0.36f, sz * 0.64f);
                float rExt = sz * 0.47f;
                float rAro = sz * 0.40f;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float dist = Vector2.Distance(p, centro);
                        if (dist > rExt)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist >= rAro)
                        {
                            // Aro metálico protector de bronce del perno
                            float alpha = Mathf.Clamp01((rExt - dist) * 2f);
                            Color cAro = new Color(0.85f, 0.65f, 0.25f, alpha);
                            tex.SetPixel(x, y, cAro);
                        }
                        else
                        {
                            // Núcleo esférico brillante de la gema elemental
                            float distLuz = Vector2.Distance(p, centroLuz);
                            float factorLuz = Mathf.Clamp01(1f - distLuz / (rAro * 1.35f));
                            float highlight = Mathf.Pow(factorLuz, 3.2f);

                            Color cBase = cBases[i];
                            Color cSombra = cBase * 0.40f;
                            Color cInterm = Color.Lerp(cSombra, cBase, Mathf.Clamp01((y + (sz - x)) / (sz * 1.8f)));
                            Color cFinal = Color.Lerp(cInterm, cBrillos[i], highlight);
                            cFinal.a = 1f;
                            tex.SetPixel(x, y, cFinal);
                        }
                    }
                }
                tex.Apply();
                _spritesPernos[i] = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            }

            // 5. Sprites de los 4 Tambores / Receptáculos (46x46 cilindro ranurado)
            _spritesTambores = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                const int sz = 46;
                Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;

                Vector2 centro = new Vector2(sz * 0.5f - 0.5f, sz * 0.5f - 0.5f);
                float rExt = sz * 0.47f;
                float rAnillo = sz * 0.36f;
                float rInt = sz * 0.24f;

                for (int y = 0; y < sz; y++)
                {
                    for (int x = 0; x < sz; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), centro);
                        if (dist > rExt)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else if (dist >= rAnillo)
                        {
                            // Aro exterior de bronce ranurado
                            float a = Mathf.Clamp01((rExt - dist) * 2f);
                            Color c = Color.Lerp(new Color(0.75f, 0.55f, 0.20f), cBases[i], 0.3f);
                            c.a = a * 0.95f;
                            tex.SetPixel(x, y, c);
                        }
                        else if (dist >= rInt)
                        {
                            // Anillo luminoso del elemento
                            Color c = Color.Lerp(cBases[i], cBrillos[i], 0.65f);
                            c.a = 0.90f;
                            tex.SetPixel(x, y, c);
                        }
                        else
                        {
                            // Cavidad oscura receptora del perno
                            Color c = new Color(0.04f, 0.05f, 0.07f, 0.95f);
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
                tex.Apply();
                _spritesTambores[i] = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
