using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    [Serializable]
    public class SlotHabilidadHUD
    {
        public GameObject contenedor;
        public Image icono;
        public Image sombraCooldown;
        public Text textoTiempo;
        public Text textoTecla;
    }

    /// <summary>
    /// HUD en pantalla durante el juego:
    /// - Barra de vida del personaje local abajo al centro sujeta a la cámara.
    /// - Iconos de habilidades con enfriamientos radiales (cooldowns) abajo al centro.
    /// - Menús emergentes: Menú de Pausa (tecla ESC) y Fin de Partida (Victoria / Derrota).
    /// </summary>
    public class HUDJugador : MonoBehaviour
    {
        public static HUDJugador Instancia { get; private set; }

        [Header("Vida")]
        public Text textoMiNombreHUD;
        public BarraVida barraVida;

        [Header("Barras de Progresión")]
        public BarraVida barraMaestria; // Barra de nivel de maestría abajo de la barra de vida
        public BarraVida barraXPRun;    // Barra de experiencia sobre la barra de habilidades (run actual)
        public Text textoOroRun;        // Contador de oro actual en la run

        [Header("Slots de Habilidades")]
        public SlotHabilidadHUD[] slotsHabilidades;
        public Image[] iconosHabilidades; // Compatibilidad con referencias directas

        [Header("Sprites de Habilidades - Fuego")]
        public Sprite iconoFuegoLMB;
        public Sprite iconoFuegoE;
        public Sprite iconoFuegoQ;

        [Header("Sprites de Habilidades - Hielo")]
        public Sprite iconoHieloLMB;
        public Sprite iconoHieloE;
        public Sprite iconoHieloQ;
        public Sprite iconoHieloR;

        [Header("Sprites de Habilidades - Eléctrico")]
        public Sprite iconoElecLMB;
        public Sprite iconoElecE;
        public Sprite iconoElecQ;
        public Sprite iconoElecR;

        [Header("Botón de Pausa HUD")]
        public Button botonPausaHUD;

        [Header("Menú de Pausa")]
        public GameObject panelPausa;
        public Button botonReanudar;
        public Button botonControlesPausa;
        public Button botonVolverLobbyPausa;
        public Button botonSalirPartida;

        [Header("Menú Fin de Partida")]
        public GameObject panelFinPartida;
        public Text textoResultado; // "¡Victoria!" o "Has muerto..."
        public Button botonVolverLobby;

        [Header("Votación Volver al Lobby (Estilo LoL)")]
        public GameObject panelVotacion;
        public Text textoTituloVotacion;
        public Text textoEstadoVotos;
        public Text textoTemporizadorVotacion;
        public Button botonVotarSi;
        public Button botonVotarNo;
        private bool _yaVoteLocalmente = false;

        [Header("Transición de Nivel (Fade a Negro)")]
        public Image panelFadeNegro;
        public Text textoBannerNivel;
        public Text textoAvisoPasaje;

        [Header("Barra de Jefe (Estilo Dark Souls) y Minimapa")]
        public GameObject panelJefeDarkSouls;
        public Text textoNombreJefeDS;
        public Image imagenRellenoJefeDS;
        public Text textoVidaJefeDS;
        public MinimapaHUD minimapaHUD;

        private Coroutine _rutinaFade;
        private float _tiempoAvisoPasaje = 0f;

        private Personaje _personajeLocal;

        void Awake()
        {
            Instancia = this;
            if (panelPausa) panelPausa.SetActive(false);
            if (panelFinPartida) panelFinPartida.SetActive(false);

            if (botonPausaHUD)
            {
                botonPausaHUD.onClick.AddListener(AlternarPausa);
                // Asegurar que el botón de pausa esté arriba del minimapa (y = -12)
                var rtContPausa = botonPausaHUD.transform.parent as RectTransform;
                if (rtContPausa != null && rtContPausa.name == "ContenedorHUD_ArribaDerecha")
                {
                    rtContPausa.anchorMin = new Vector2(1f, 1f);
                    rtContPausa.anchorMax = new Vector2(1f, 1f);
                    rtContPausa.pivot = new Vector2(1f, 1f);
                    rtContPausa.anchoredPosition = new Vector2(-16f, -12f);
                }
            }
            if (botonReanudar) botonReanudar.onClick.AddListener(CerrarPausa);
            if (botonVolverLobbyPausa) botonVolverLobbyPausa.onClick.AddListener(VolverAlLobbyDesdePausa);
            if (botonSalirPartida) botonSalirPartida.onClick.AddListener(SalirAlMenuPrincipal);
            if (botonVolverLobby) botonVolverLobby.onClick.AddListener(VolverAlLobbyFinPartidaIndividual);

            AsegurarTextoMiNombreHUD();
            AsegurarLayoutInferior();
            AsegurarBotonControlesPausa();

            // Iniciar las barras de progresión en 0 con su texto de nivel correspondiente
            if (barraVida != null) barraVida.Establecer(100, 100, true);
            if (barraMaestria != null)
            {
                barraMaestria.Establecer(0, GestorMaestria.XP_POR_NIVEL_MAESTRIA, false);
                if (barraMaestria.textoVida != null)
                {
                    barraMaestria.textoVida.text = $"Maestría Nv.1 (0/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP)";
                }
            }
            if (barraXPRun != null)
            {
                barraXPRun.Establecer(0, 100, false);
                if (barraXPRun.textoVida != null)
                {
                    barraXPRun.textoVida.text = "Nv.1 (0/100 XP)";
                }
            }

            GestorMaestria.AlGanarMaestria += OnMaestriaActualizada;

            // Crear dinámicamente el panel de fade a negro si no está asignado
            if (panelFadeNegro == null)
            {
                var fadeObj = new GameObject("PanelFadeNegro");
                fadeObj.transform.SetParent(transform, false);
                fadeObj.transform.SetAsLastSibling(); // Por encima de todo
                panelFadeNegro = fadeObj.AddComponent<Image>();
                panelFadeNegro.color = new Color(0f, 0f, 0f, 0f);
                panelFadeNegro.raycastTarget = false;
                fadeObj.SetActive(false);

                var rt = fadeObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            // Crear dinámicamente texto de aviso de pasaje si no existe
            if (textoAvisoPasaje == null)
            {
                var avisoObj = new GameObject("TextoAvisoPasaje");
                avisoObj.transform.SetParent(transform, false);
                textoAvisoPasaje = avisoObj.AddComponent<Text>();
                textoAvisoPasaje.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoAvisoPasaje.fontSize = 24;
                textoAvisoPasaje.alignment = TextAnchor.MiddleCenter;
                textoAvisoPasaje.color = Color.yellow;
                textoAvisoPasaje.raycastTarget = false;
                var rt = avisoObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 160);
                rt.sizeDelta = new Vector2(600, 50);
                avisoObj.SetActive(false);
            }

            // Crear dinámicamente banner de nuevo nivel
            if (textoBannerNivel == null)
            {
                var banObj = new GameObject("TextoBannerNivel");
                banObj.transform.SetParent(transform, false);
                textoBannerNivel = banObj.AddComponent<Text>();
                textoBannerNivel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoBannerNivel.fontSize = 42;
                textoBannerNivel.alignment = TextAnchor.MiddleCenter;
                textoBannerNivel.color = Color.cyan;
                textoBannerNivel.raycastTarget = false;
                var rt = banObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 40);
                rt.sizeDelta = new Vector2(800, 100);
                banObj.SetActive(false);
            }

            // Crear sprite blanco plano para las sombras radiales
            Texture2D texBlanca = Texture2D.whiteTexture;
            Sprite spriteBlanco = Sprite.Create(texBlanca, new Rect(0, 0, texBlanca.width, texBlanca.height), new Vector2(0.5f, 0.5f));

            if (slotsHabilidades != null)
            {
                foreach (var s in slotsHabilidades)
                {
                    if (s == null) continue;
                    if (s.icono != null)
                    {
                        s.icono.color = Color.white; // 100% brillante y crudo
                    }
                    if (s.sombraCooldown != null)
                    {
                        if (s.sombraCooldown.sprite == null)
                        {
                            s.sombraCooldown.sprite = spriteBlanco;
                        }
                        s.sombraCooldown.color = new Color(0f, 0f, 0f, 0.75f);
                        s.sombraCooldown.fillAmount = 0f;
                        s.sombraCooldown.gameObject.SetActive(false); // Iniciar apagada para no oscurecer
                    }
                    if (s.textoTiempo != null)
                    {
                        s.textoTiempo.text = "";
                    }
                }
            }
            // Crear dinámicamente panel de votación estilo League of Legends si no existe
            CrearPanelVotacionSiNoExiste();

            // Crear Minimapa arriba a la derecha
            if (minimapaHUD == null)
            {
                minimapaHUD = MinimapaHUD.CrearEnHUD(transform);
            }

            // Crear Barra de Jefe estilo Dark Souls arriba al centro
            CrearBarraJefeDarkSoulsSiNoExiste(spriteBlanco);

            // Ocultar botón de volver al lobby en pausa si no es servidor
            ActualizarVisibilidadBotonLobbyPausa();
        }

        private void CrearBarraJefeDarkSoulsSiNoExiste(Sprite spriteBlanco)
        {
            if (panelJefeDarkSouls != null) return;

            var objJefe = new GameObject("PanelJefeDarkSouls");
            objJefe.transform.SetParent(transform, false);

            var rtJefe = objJefe.GetComponent<RectTransform>();
            if (rtJefe == null) rtJefe = objJefe.AddComponent<RectTransform>();
            rtJefe.anchorMin = new Vector2(0.5f, 1f);
            rtJefe.anchorMax = new Vector2(0.5f, 1f);
            rtJefe.pivot = new Vector2(0.5f, 1f);
            rtJefe.anchoredPosition = new Vector2(0f, -18f);
            rtJefe.sizeDelta = new Vector2(640f, 68f);

            // Nombre del Jefe arriba de la barra (Estilo Dark Souls)
            var nomObj = new GameObject("NombreJefeDS");
            nomObj.transform.SetParent(objJefe.transform, false);
            textoNombreJefeDS = nomObj.AddComponent<Text>();
            textoNombreJefeDS.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoNombreJefeDS.fontSize = 20;
            textoNombreJefeDS.fontStyle = FontStyle.Bold;
            textoNombreJefeDS.color = new Color(0.95f, 0.88f, 0.72f, 1f);
            textoNombreJefeDS.alignment = TextAnchor.MiddleCenter;
            textoNombreJefeDS.text = "JEFE";
            var rtNom = nomObj.GetComponent<RectTransform>();
            rtNom.anchorMin = new Vector2(0f, 1f);
            rtNom.anchorMax = new Vector2(1f, 1f);
            rtNom.pivot = new Vector2(0.5f, 1f);
            rtNom.anchoredPosition = new Vector2(0f, 0f);
            rtNom.sizeDelta = new Vector2(0f, 28f);

            // Marco dorado oscuro de la barra
            var marcoBarra = new GameObject("MarcoBarraJefe");
            marcoBarra.transform.SetParent(objJefe.transform, false);
            var imgMarco = marcoBarra.AddComponent<Image>();
            imgMarco.color = new Color(0.35f, 0.28f, 0.15f, 0.95f);
            var rtMarco = marcoBarra.GetComponent<RectTransform>();
            rtMarco.anchorMin = new Vector2(0f, 0f);
            rtMarco.anchorMax = new Vector2(1f, 0f);
            rtMarco.pivot = new Vector2(0.5f, 0f);
            rtMarco.anchoredPosition = new Vector2(0f, 6f);
            rtMarco.sizeDelta = new Vector2(0f, 24f);

            // Fondo negro interior
            var fondoBarra = new GameObject("FondoBarraJefe");
            fondoBarra.transform.SetParent(marcoBarra.transform, false);
            var imgFondo = fondoBarra.AddComponent<Image>();
            imgFondo.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);
            var rtFondo = fondoBarra.GetComponent<RectTransform>();
            rtFondo.anchorMin = Vector2.zero;
            rtFondo.anchorMax = Vector2.one;
            rtFondo.offsetMin = new Vector2(3f, 3f);
            rtFondo.offsetMax = new Vector2(-3f, -3f);

            // Relleno carmesí Dark Souls
            var rellenoObj = new GameObject("RellenoBarraJefe");
            rellenoObj.transform.SetParent(fondoBarra.transform, false);
            imagenRellenoJefeDS = rellenoObj.AddComponent<Image>();
            imagenRellenoJefeDS.sprite = spriteBlanco;
            imagenRellenoJefeDS.type = Image.Type.Filled;
            imagenRellenoJefeDS.fillMethod = Image.FillMethod.Horizontal;
            imagenRellenoJefeDS.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenRellenoJefeDS.color = new Color(0.68f, 0.10f, 0.12f, 1f);
            imagenRellenoJefeDS.fillAmount = 1f;
            var rtRelleno = rellenoObj.GetComponent<RectTransform>();
            rtRelleno.anchorMin = Vector2.zero;
            rtRelleno.anchorMax = Vector2.one;
            rtRelleno.sizeDelta = Vector2.zero;

            // Texto numérico de HP sobre la barra
            var txtHpObj = new GameObject("TextoHPJefe");
            txtHpObj.transform.SetParent(fondoBarra.transform, false);
            textoVidaJefeDS = txtHpObj.AddComponent<Text>();
            textoVidaJefeDS.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoVidaJefeDS.fontSize = 12;
            textoVidaJefeDS.fontStyle = FontStyle.Bold;
            textoVidaJefeDS.color = new Color(1f, 0.95f, 0.9f, 0.9f);
            textoVidaJefeDS.alignment = TextAnchor.MiddleCenter;
            var rtTxtHp = txtHpObj.GetComponent<RectTransform>();
            rtTxtHp.anchorMin = Vector2.zero;
            rtTxtHp.anchorMax = Vector2.one;
            rtTxtHp.sizeDelta = Vector2.zero;

            panelJefeDarkSouls = objJefe;
            panelJefeDarkSouls.SetActive(false);
        }

        public void MostrarBarraJefeDarkSouls(string nombre, int actual, int max)
        {
            if (panelJefeDarkSouls == null) return;
            panelJefeDarkSouls.SetActive(true);
            if (textoNombreJefeDS != null)
            {
                textoNombreJefeDS.text = nombre.ToUpper();
            }
            ActualizarBarraJefeDarkSouls(actual, max);
        }

        public void ActualizarBarraJefeDarkSouls(int actual, int max)
        {
            if (panelJefeDarkSouls == null) return;
            if (!panelJefeDarkSouls.activeSelf && actual > 0)
            {
                panelJefeDarkSouls.SetActive(true);
            }
            int m = Mathf.Max(1, max);
            int a = Mathf.Clamp(actual, 0, m);
            float ratio = Mathf.Clamp01((float)a / m);
            if (imagenRellenoJefeDS != null)
            {
                imagenRellenoJefeDS.fillAmount = ratio;
                var rt = imagenRellenoJefeDS.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(ratio, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
            if (textoVidaJefeDS != null)
            {
                textoVidaJefeDS.text = $"{a} / {m}";
            }
            if (a <= 0)
            {
                OcultarBarraJefeDarkSouls();
            }
        }

        public void OcultarBarraJefeDarkSouls()
        {
            if (panelJefeDarkSouls != null)
            {
                panelJefeDarkSouls.SetActive(false);
            }
        }

        private void ActualizarVisibilidadBotonLobbyPausa()
        {
            if (botonVolverLobbyPausa != null)
            {
                botonVolverLobbyPausa.gameObject.SetActive(true);
            }
        }

        private void CrearPanelVotacionSiNoExiste()
        {
            if (panelVotacion != null) return;

            var votObj = new GameObject("PanelVotacionLobby");
            votObj.transform.SetParent(transform, false);
            var imgFondo = votObj.AddComponent<Image>();
            imgFondo.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            var rt = votObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 180);
            rt.sizeDelta = new Vector2(460, 150);

            // Título
            var objTit = new GameObject("TextoTitulo");
            objTit.transform.SetParent(votObj.transform, false);
            textoTituloVotacion = objTit.AddComponent<Text>();
            textoTituloVotacion.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoTituloVotacion.fontSize = 17;
            textoTituloVotacion.fontStyle = FontStyle.Bold;
            textoTituloVotacion.color = new Color(1f, 0.85f, 0.3f);
            textoTituloVotacion.alignment = TextAnchor.MiddleCenter;
            textoTituloVotacion.text = "🗳️ ¿REGRESAR AL LOBBY?";
            var rtTit = objTit.GetComponent<RectTransform>();
            rtTit.anchoredPosition = new Vector2(0, 48);
            rtTit.sizeDelta = new Vector2(440, 32);

            // Estado de votos (Verdes / Rojos)
            var objVotos = new GameObject("TextoEstadoVotos");
            objVotos.transform.SetParent(votObj.transform, false);
            textoEstadoVotos = objVotos.AddComponent<Text>();
            textoEstadoVotos.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoEstadoVotos.fontSize = 15;
            textoEstadoVotos.color = Color.white;
            textoEstadoVotos.alignment = TextAnchor.MiddleCenter;
            textoEstadoVotos.text = "Votos: [0 SÍ] vs [0 NO]";
            var rtVotos = objVotos.GetComponent<RectTransform>();
            rtVotos.anchoredPosition = new Vector2(0, 14);
            rtVotos.sizeDelta = new Vector2(440, 28);

            // Temporizador
            var objTiempo = new GameObject("TextoTiempo");
            objTiempo.transform.SetParent(votObj.transform, false);
            textoTemporizadorVotacion = objTiempo.AddComponent<Text>();
            textoTemporizadorVotacion.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textoTemporizadorVotacion.fontSize = 13;
            textoTemporizadorVotacion.color = new Color(0.7f, 0.7f, 0.7f);
            textoTemporizadorVotacion.alignment = TextAnchor.MiddleCenter;
            textoTemporizadorVotacion.text = "Tiempo restante: 15s";
            var rtTiempo = objTiempo.GetComponent<RectTransform>();
            rtTiempo.anchoredPosition = new Vector2(0, -14);
            rtTiempo.sizeDelta = new Vector2(440, 24);

            // Botón SÍ [F1]
            var objBtnSi = new GameObject("BotonSi");
            objBtnSi.transform.SetParent(votObj.transform, false);
            var imgSi = objBtnSi.AddComponent<Image>();
            imgSi.color = new Color(0.18f, 0.58f, 0.28f);
            botonVotarSi = objBtnSi.AddComponent<Button>();
            var rtSi = objBtnSi.GetComponent<RectTransform>();
            rtSi.anchoredPosition = new Vector2(-110, -48);
            rtSi.sizeDelta = new Vector2(170, 34);

            var objTxtSi = new GameObject("Txt");
            objTxtSi.transform.SetParent(objBtnSi.transform, false);
            var txtSi = objTxtSi.AddComponent<Text>();
            txtSi.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSi.fontSize = 13;
            txtSi.fontStyle = FontStyle.Bold;
            txtSi.color = Color.white;
            txtSi.alignment = TextAnchor.MiddleCenter;
            txtSi.text = "SÍ  [F1]";
            objTxtSi.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 34);

            botonVotarSi.onClick.AddListener(() => Votar(true));

            // Botón NO [F2]
            var objBtnNo = new GameObject("BotonNo");
            objBtnNo.transform.SetParent(votObj.transform, false);
            var imgNo = objBtnNo.AddComponent<Image>();
            imgNo.color = new Color(0.68f, 0.22f, 0.22f);
            botonVotarNo = objBtnNo.AddComponent<Button>();
            var rtNo = objBtnNo.GetComponent<RectTransform>();
            rtNo.anchoredPosition = new Vector2(110, -48);
            rtNo.sizeDelta = new Vector2(170, 34);

            var objTxtNo = new GameObject("Txt");
            objTxtNo.transform.SetParent(objBtnNo.transform, false);
            var txtNo = objTxtNo.AddComponent<Text>();
            txtNo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtNo.fontSize = 13;
            txtNo.fontStyle = FontStyle.Bold;
            txtNo.color = Color.white;
            txtNo.alignment = TextAnchor.MiddleCenter;
            txtNo.text = "NO  [F2]";
            objTxtNo.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 34);

            botonVotarNo.onClick.AddListener(() => Votar(false));

            panelVotacion = votObj;
            panelVotacion.SetActive(false);
        }

        void Start()
        {
            SalaRed.PartidaTerminada += AlTerminarPartida;
            ActualizarVisibilidadBotonLobbyPausa();
            AplicarTipografiasPixelArt();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instancia == this) Instancia = null;
            SalaRed.PartidaTerminada -= AlTerminarPartida;
            GestorMaestria.AlGanarMaestria -= OnMaestriaActualizada;
        }

        void Update()
        {
            if (Controles.Presionada(Controles.Pausa))
            {
                AlternarPausa();
            }

            // Atajos de votación F1 (SÍ) y F2 (NO)
            if (panelVotacion != null && panelVotacion.activeSelf && !_yaVoteLocalmente)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    Votar(true);
                }
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    Votar(false);
                }
            }

            // Actualizar tiempo y estado de votación si está activa
            if (panelVotacion != null && panelVotacion.activeSelf && SalaRed.Instancia != null)
            {
                if (SalaRed.Instancia.VotacionLobbyActiva.Value)
                {
                    float t = Mathf.Max(0f, SalaRed.Instancia.TiempoVotacionRestante.Value);
                    if (textoTemporizadorVotacion != null)
                    {
                        textoTemporizadorVotacion.text = $"Tiempo restante: {Mathf.CeilToInt(t)}s (Atajos: F1=SÍ, F2=NO)";
                    }
                    if (textoEstadoVotos != null)
                    {
                        int si = SalaRed.Instancia.VotosSiLobby.Value;
                        int no = SalaRed.Instancia.VotosNoLobby.Value;
                        int total = SalaRed.Instancia.TotalVotantesLobby.Value;
                        textoEstadoVotos.text = $"<color=#55ff55><b>{si} SÍ</b></color>  —  <color=#ff5555><b>{no} NO</b></color>  (Total: {total})";
                    }
                }
            }

            if (_tiempoAvisoPasaje > 0f)
            {
                _tiempoAvisoPasaje -= Time.deltaTime;
                if (_tiempoAvisoPasaje <= 0f && textoAvisoPasaje != null)
                {
                    textoAvisoPasaje.gameObject.SetActive(false);
                }
            }

            if (_personajeLocal == null || !_personajeLocal.EsControlLocal)
            {
                _personajeLocal = null;
                for (int i = 0; i < Personaje.Todos.Count; i++)
                {
                    var p = Personaje.Todos[i];
                    if (p != null && p.EsControlLocal)
                    {
                        ConectarPersonaje(p);
                        break;
                    }
                }
            }

            ActualizarHUDJugadorLocal();
            ActualizarEfectosConsumiblesDerecha(Time.deltaTime);
            ActualizarCompanerosArribaIzquierda();
        }

        private void AsegurarTextoMiNombreHUD()
        {
            var contArribaIzq = transform.Find("ContenedorHUD_ArribaIzquierda") as RectTransform;
            if (contArribaIzq == null && barraVida != null)
            {
                var p = barraVida.transform.parent as RectTransform;
                if (p != null && p.name == "ContenedorHUD_ArribaIzquierda") contArribaIzq = p;
            }

            if (contArribaIzq != null)
            {
                contArribaIzq.anchorMin = new Vector2(0f, 1f);
                contArribaIzq.anchorMax = new Vector2(0f, 1f);
                contArribaIzq.pivot = new Vector2(0f, 1f);
                contArribaIzq.anchoredPosition = new Vector2(25f, -10f);
                contArribaIzq.sizeDelta = new Vector2(400f, 60f);

                if (textoMiNombreHUD == null)
                {
                    var existente = contArribaIzq.Find("TextoMiNombreHUD");
                    if (existente != null)
                    {
                        textoMiNombreHUD = existente.GetComponent<Text>();
                    }
                }

                if (textoMiNombreHUD == null)
                {
                    var nomObj = new GameObject("TextoMiNombreHUD");
                    nomObj.transform.SetParent(contArribaIzq, false);
                    var rtNom = nomObj.AddComponent<RectTransform>();
                    rtNom.anchorMin = new Vector2(0f, 1f);
                    rtNom.anchorMax = new Vector2(0f, 1f);
                    rtNom.pivot = new Vector2(0f, 1f);
                    rtNom.anchoredPosition = new Vector2(2f, 0f);
                    rtNom.sizeDelta = new Vector2(380f, 22f);

                    textoMiNombreHUD = nomObj.AddComponent<Text>();
                    textoMiNombreHUD.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    textoMiNombreHUD.fontSize = 13;
                    textoMiNombreHUD.fontStyle = FontStyle.Bold;
                    textoMiNombreHUD.alignment = TextAnchor.MiddleLeft;
                    textoMiNombreHUD.color = new Color(0.95f, 0.98f, 1f, 1f);
                    textoMiNombreHUD.raycastTarget = false;

                    var sombra = nomObj.AddComponent<Shadow>();
                    sombra.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    sombra.effectDistance = new Vector2(1.5f, -1.5f);
                }

                if (barraMaestria != null && barraMaestria.transform.parent == contArribaIzq)
                {
                    var rtBm = barraMaestria.GetComponent<RectTransform>();
                    if (rtBm != null)
                    {
                        rtBm.anchoredPosition = new Vector2(0f, -24f);
                    }
                }
            }
        }

        private void AsegurarLayoutInferior()
        {
            var contAbajo = transform.Find("ContenedorHUD_AbajoCentro") as RectTransform;
            if (contAbajo == null) return;

            contAbajo.anchorMin = new Vector2(0.5f, 0f);
            contAbajo.anchorMax = new Vector2(0.5f, 0f);
            contAbajo.pivot = new Vector2(0.5f, 0f);
            contAbajo.anchoredPosition = new Vector2(0f, 8f);
            contAbajo.sizeDelta = new Vector2(400f, 155f);

            // 1. Fila de Habilidades en la base
            var filaHab = contAbajo.Find("FilaHabilidades") as RectTransform;
            if (filaHab != null)
            {
                filaHab.anchorMin = new Vector2(0.5f, 0f);
                filaHab.anchorMax = new Vector2(0.5f, 0f);
                filaHab.pivot = new Vector2(0.5f, 0f);
                filaHab.anchoredPosition = new Vector2(0f, 8f);
                filaHab.sizeDelta = new Vector2(340f, 66f);
            }

            // 2. Barra de Vida colocada SOBRE las habilidades
            if (barraVida != null)
            {
                if (barraVida.transform.parent != contAbajo)
                {
                    barraVida.transform.SetParent(contAbajo, false);
                }
                var rtBv = barraVida.GetComponent<RectTransform>();
                if (rtBv != null)
                {
                    rtBv.anchorMin = new Vector2(0.5f, 0f);
                    rtBv.anchorMax = new Vector2(0.5f, 0f);
                    rtBv.pivot = new Vector2(0.5f, 0f);
                    rtBv.anchoredPosition = new Vector2(0f, 78f);
                    rtBv.sizeDelta = new Vector2(360f, 22f);
                }
            }

            // 3. Barra de Experiencia de Run colocada SOBRE la Barra de Vida
            if (barraXPRun != null)
            {
                if (barraXPRun.transform.parent != contAbajo)
                {
                    barraXPRun.transform.SetParent(contAbajo, false);
                }
                var rtXp = barraXPRun.GetComponent<RectTransform>();
                if (rtXp != null)
                {
                    rtXp.anchorMin = new Vector2(0.5f, 0f);
                    rtXp.anchorMax = new Vector2(0.5f, 0f);
                    rtXp.pivot = new Vector2(0.5f, 0f);
                    rtXp.anchoredPosition = new Vector2(0f, 104f);
                    rtXp.sizeDelta = new Vector2(360f, 18f);
                }
            }

            // 4. Contador de Oro en la Run sobre la Barra de Experiencia
            if (textoOroRun == null)
            {
                var existOro = contAbajo.Find("TextoOroRun");
                if (existOro != null)
                {
                    textoOroRun = existOro.GetComponent<Text>();
                }
            }
            if (textoOroRun == null)
            {
                var oroObj = new GameObject("TextoOroRun");
                oroObj.transform.SetParent(contAbajo, false);
                textoOroRun = oroObj.AddComponent<Text>();
                textoOroRun.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoOroRun.fontSize = 12;
                textoOroRun.fontStyle = FontStyle.Bold;
                textoOroRun.alignment = TextAnchor.MiddleCenter;
                textoOroRun.color = new Color(1f, 0.88f, 0.2f, 1f);
                var rtOro = oroObj.GetComponent<RectTransform>();
                rtOro.anchorMin = new Vector2(0.5f, 0f);
                rtOro.anchorMax = new Vector2(0.5f, 0f);
                rtOro.pivot = new Vector2(0.5f, 0f);
                rtOro.anchoredPosition = new Vector2(0f, 126f);
                rtOro.sizeDelta = new Vector2(360f, 20f);
            }
        }

        private void AsegurarBotonControlesPausa()
        {
            if (panelPausa == null) return;

            if (botonControlesPausa == null)
            {
                var exist = panelPausa.transform.Find("BotonControlesPausa");
                if (exist != null)
                {
                    botonControlesPausa = exist.GetComponent<Button>();
                }
            }

            if (botonControlesPausa == null)
            {
                var btnObj = new GameObject("BotonControlesPausa");
                btnObj.transform.SetParent(panelPausa.transform, false);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.18f, 0.22f, 0.32f, 0.95f);
                botonControlesPausa = btnObj.AddComponent<Button>();

                var rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(230f, 40f);

                var txtObj = new GameObject("Texto");
                txtObj.transform.SetParent(btnObj.transform, false);
                var txt = txtObj.AddComponent<Text>();
                txt.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 11;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.text = "⚙️ CONTROLES";
                var rtTxt = txtObj.GetComponent<RectTransform>();
                rtTxt.anchorMin = Vector2.zero;
                rtTxt.anchorMax = Vector2.one;
                rtTxt.sizeDelta = Vector2.zero;
            }

            botonControlesPausa.onClick.RemoveAllListeners();
            botonControlesPausa.onClick.AddListener(() =>
            {
                PanelOpcionesControles.ObtenerOCrear(transform).Abrir();
            });

            // Reajustar posiciones verticales de los 4 botones en el Menú de Pausa:
            if (botonReanudar != null)
            {
                var rtReanudar = botonReanudar.transform as RectTransform;
                if (rtReanudar != null)
                {
                    rtReanudar.anchoredPosition = new Vector2(0, 50);
                    rtReanudar.sizeDelta = new Vector2(230, 40);
                }
            }
            if (botonControlesPausa != null)
            {
                var rtCtrl = botonControlesPausa.transform as RectTransform;
                if (rtCtrl != null)
                {
                    rtCtrl.anchoredPosition = new Vector2(0, 2);
                }
            }
            if (botonVolverLobbyPausa != null)
            {
                var rtLobby = botonVolverLobbyPausa.transform as RectTransform;
                if (rtLobby != null)
                {
                    rtLobby.anchoredPosition = new Vector2(0, -46);
                    rtLobby.sizeDelta = new Vector2(230, 40);
                }
            }
            if (botonSalirPartida != null)
            {
                var rtSalir = botonSalirPartida.transform as RectTransform;
                if (rtSalir != null)
                {
                    rtSalir.anchoredPosition = new Vector2(0, -94);
                    rtSalir.sizeDelta = new Vector2(230, 40);
                }
            }
        }

        private void AplicarTipografiasPixelArt()
        {
            Font pixelFont = FuenteUtil.ObtenerFuentePixelArt();
            if (pixelFont == null) return;

            foreach (var txt in GetComponentsInChildren<Text>(true))
            {
                if (txt != null)
                {
                    txt.font = pixelFont;
                }
            }
        }

        private void ActualizarHUDJugadorLocal()
        {
            if (textoMiNombreHUD == null)
            {
                AsegurarTextoMiNombreHUD();
            }

            string miNombre = "Jugador";
            if (_personajeLocal != null)
            {
                miNombre = _personajeLocal.ObtenerNombreJugador();
            }
            else if (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
            {
                miNombre = Global.Instancia.NombreLocal.Trim();
            }

            bool caidoLocal = _personajeLocal != null && _personajeLocal.EstaMuerto.Value;

            if (textoMiNombreHUD != null)
            {
                textoMiNombreHUD.text = caidoLocal ? $"💀 {miNombre} (CAÍDO)" : $"👤 {miNombre}";
                textoMiNombreHUD.color = caidoLocal
                    ? new Color(1f, 0.42f, 0.42f, 1f)
                    : new Color(0.95f, 0.98f, 1f, 1f);
            }

            if (_personajeLocal != null)
            {
                if (barraVida != null)
                {
                    int hpMax = (_personajeLocal.VidaMaxSincronizada.Value > 0)
                        ? _personajeLocal.VidaMaxSincronizada.Value
                        : Mathf.Max(1, _personajeLocal.vidaMax);
                    int hpActual = caidoLocal ? 0 : Mathf.Clamp(_personajeLocal.VidaActual.Value, 0, hpMax);
                    barraVida.Establecer(hpActual, hpMax, true);
                    if (barraVida.textoVida != null)
                    {
                        barraVida.textoVida.text = $"❤ {hpActual} / {hpMax} HP";
                    }
                }

                if (_personajeLocal.nivelProgreso != null && barraXPRun != null)
                {
                    ActualizarProgresoRun(
                        _personajeLocal.nivelProgreso.NivelActual.Value,
                        _personajeLocal.nivelProgreso.XPActual.Value,
                        _personajeLocal.nivelProgreso.XPSiguienteNivel.Value);
                }

                if (barraMaestria != null && _personajeLocal.TipoPj != TipoPersonaje.Ninguno)
                {
                    ActualizarBarraMaestria(_personajeLocal.TipoPj);
                }

                if (textoOroRun != null)
                {
                    textoOroRun.text = $"💰 {_personajeLocal.OroRun.Value} ORO";
                }
            }
        }

        private class SlotCompaneroHUD
        {
            public ulong idCliente;
            public GameObject contenedor;
            public RectTransform rtContenedor;
            public Text textoNombre;
            public Image rellenoVida;
            public RectTransform rtRellenoVida;
            public Text textoVidaNumerica;
        }

        private readonly System.Collections.Generic.List<SlotCompaneroHUD> _slotsCompaneros = new System.Collections.Generic.List<SlotCompaneroHUD>();

        private void ActualizarCompanerosArribaIzquierda()
        {
            bool redActiva = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening;

            // Recolectar todos los compañeros (personajes que no sean el jugador local)
            System.Collections.Generic.List<Personaje> companeros = new System.Collections.Generic.List<Personaje>();
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                var p = Personaje.Todos[i];
                if (p == null) continue;
                if (redActiva && !p.IsSpawned) continue;
                if (p == _personajeLocal || p.EsControlLocal) continue;
                companeros.Add(p);
            }

            companeros.Sort((a, b) =>
            {
                int cmp = a.OwnerClientId.CompareTo(b.OwnerClientId);
                return cmp != 0 ? cmp : a.NetworkObjectId.CompareTo(b.NetworkObjectId);
            });

            // Eliminar slots de compañeros que ya no existen (asociados por NetworkObjectId exacto del Personaje)
            for (int i = _slotsCompaneros.Count - 1; i >= 0; i--)
            {
                bool sigueActivo = false;
                for (int j = 0; j < companeros.Count; j++)
                {
                    ulong keyComp = companeros[j].IsSpawned ? companeros[j].NetworkObjectId : (ulong)companeros[j].GetInstanceID();
                    if (keyComp == _slotsCompaneros[i].idCliente)
                    {
                        sigueActivo = true;
                        break;
                    }
                }
                if (!sigueActivo)
                {
                    if (_slotsCompaneros[i].contenedor != null)
                    {
                        Destroy(_slotsCompaneros[i].contenedor);
                    }
                    _slotsCompaneros.RemoveAt(i);
                }
            }

            // Crear o actualizar cada compañero arriba a la izquierda (debajo de las barras del jugador local)
            for (int idx = 0; idx < companeros.Count; idx++)
            {
                var comp = companeros[idx];
                ulong keyComp = comp.IsSpawned ? comp.NetworkObjectId : (ulong)comp.GetInstanceID();

                SlotCompaneroHUD slot = null;
                for (int s = 0; s < _slotsCompaneros.Count; s++)
                {
                    if (_slotsCompaneros[s].idCliente == keyComp)
                    {
                        slot = _slotsCompaneros[s];
                        break;
                    }
                }

                if (slot == null || slot.contenedor == null)
                {
                    slot = CrearSlotCompaneroArribaIzquierda(keyComp);
                    _slotsCompaneros.Add(slot);
                }

                // Posicionar en lista vertical arriba a la izquierda (debajo de BarraMaestriaHUD)
                if (slot.rtContenedor != null)
                {
                    slot.rtContenedor.anchoredPosition = new Vector2(25f, -68f - (idx * 50f));
                }

                string nombreComp = comp.ObtenerNombreJugador();
                bool caido = comp.EstaMuerto.Value;
                int hpMax = (comp.VidaMaxSincronizada.Value > 0) ? comp.VidaMaxSincronizada.Value : Mathf.Max(1, comp.vidaMax);
                int hpActual = caido ? 0 : Mathf.Clamp(comp.VidaActual.Value, 0, hpMax);
                float ratio = Mathf.Clamp01((float)hpActual / Mathf.Max(1, hpMax));

                if (slot.textoNombre != null)
                {
                    slot.textoNombre.text = caido ? $"💀 {nombreComp} (CAÍDO)" : $"👤 {nombreComp}";
                    slot.textoNombre.color = caido ? new Color(1f, 0.42f, 0.42f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
                }

                if (slot.rellenoVida != null)
                {
                    slot.rellenoVida.fillAmount = ratio;
                    slot.rellenoVida.color = caido
                        ? new Color(0.45f, 0.12f, 0.12f, 1f)
                        : (ratio > 0.35f ? new Color(0.22f, 0.85f, 0.38f, 1f) : new Color(0.95f, 0.55f, 0.15f, 1f));
                }

                if (slot.rtRellenoVida != null)
                {
                    slot.rtRellenoVida.anchorMin = Vector2.zero;
                    slot.rtRellenoVida.anchorMax = new Vector2(ratio, 1f);
                    slot.rtRellenoVida.offsetMin = new Vector2(2f, 2f);
                    slot.rtRellenoVida.offsetMax = new Vector2(-2f, -2f);
                }

                if (slot.textoVidaNumerica != null)
                {
                    slot.textoVidaNumerica.text = caido ? "CAÍDO - [Mantén F cerca]" : $"{hpActual} / {hpMax}";
                }
            }
        }

        private SlotCompaneroHUD CrearSlotCompaneroArribaIzquierda(ulong idCliente)
        {
            var contObj = new GameObject($"CompaneroHUD_{idCliente}");
            contObj.transform.SetParent(transform, false);

            var rtCont = contObj.AddComponent<RectTransform>();
            rtCont.anchorMin = new Vector2(0f, 1f);
            rtCont.anchorMax = new Vector2(0f, 1f);
            rtCont.pivot = new Vector2(0f, 1f);
            rtCont.sizeDelta = new Vector2(270f, 44f);

            var imgPanel = contObj.AddComponent<Image>();
            imgPanel.color = new Color(0.05f, 0.07f, 0.11f, 0.86f);
            imgPanel.raycastTarget = false;

            // Nombre del compañero arriba de su barra
            var nomObj = new GameObject("NombreCompanero");
            nomObj.transform.SetParent(contObj.transform, false);
            var rtNom = nomObj.AddComponent<RectTransform>();
            rtNom.anchorMin = new Vector2(0f, 1f);
            rtNom.anchorMax = new Vector2(1f, 1f);
            rtNom.pivot = new Vector2(0.5f, 1f);
            rtNom.anchoredPosition = new Vector2(0f, -2f);
            rtNom.sizeDelta = new Vector2(-12f, 18f);

            var txtNom = nomObj.AddComponent<Text>();
            txtNom.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtNom.fontSize = 13;
            txtNom.fontStyle = FontStyle.Bold;
            txtNom.alignment = TextAnchor.MiddleLeft;
            txtNom.color = new Color(0.85f, 0.95f, 1f, 1f);
            txtNom.raycastTarget = false;

            // Fondo de la barra de vida del compañero
            var barraFondoObj = new GameObject("FondoBarraCompanero");
            barraFondoObj.transform.SetParent(contObj.transform, false);
            var rtBarra = barraFondoObj.AddComponent<RectTransform>();
            rtBarra.anchorMin = new Vector2(0f, 0f);
            rtBarra.anchorMax = new Vector2(1f, 0f);
            rtBarra.pivot = new Vector2(0.5f, 0f);
            rtBarra.anchoredPosition = new Vector2(0f, 4f);
            rtBarra.sizeDelta = new Vector2(-12f, 18f);

            var imgFondoBarra = barraFondoObj.AddComponent<Image>();
            imgFondoBarra.color = new Color(0.18f, 0.05f, 0.05f, 0.95f);
            imgFondoBarra.raycastTarget = false;

            // Relleno de vida en tiempo real
            var rellenoObj = new GameObject("RellenoVidaCompanero");
            rellenoObj.transform.SetParent(barraFondoObj.transform, false);
            var rtRelleno = rellenoObj.AddComponent<RectTransform>();
            rtRelleno.anchorMin = Vector2.zero;
            rtRelleno.anchorMax = Vector2.one;
            rtRelleno.offsetMin = new Vector2(2f, 2f);
            rtRelleno.offsetMax = new Vector2(-2f, -2f);

            var imgRelleno = rellenoObj.AddComponent<Image>();
            imgRelleno.color = new Color(0.22f, 0.85f, 0.38f, 1f);
            imgRelleno.raycastTarget = false;

            // Texto numérico de vida sobre la barra
            var txtHpObj = new GameObject("TextoVidaCompanero");
            txtHpObj.transform.SetParent(barraFondoObj.transform, false);
            var rtTxtHp = txtHpObj.AddComponent<RectTransform>();
            rtTxtHp.anchorMin = Vector2.zero;
            rtTxtHp.anchorMax = Vector2.one;
            rtTxtHp.sizeDelta = Vector2.zero;

            var txtHp = txtHpObj.AddComponent<Text>();
            txtHp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtHp.fontSize = 11;
            txtHp.fontStyle = FontStyle.Bold;
            txtHp.alignment = TextAnchor.MiddleCenter;
            txtHp.color = Color.white;
            txtHp.raycastTarget = false;

            return new SlotCompaneroHUD
            {
                idCliente = idCliente,
                contenedor = contObj,
                rtContenedor = rtCont,
                textoNombre = txtNom,
                rellenoVida = imgRelleno,
                rtRellenoVida = rtRelleno,
                textoVidaNumerica = txtHp
            };
        }

        private class SlotEfectoConsumibleHUD
        {
            public TipoMejoraRun tipo;
            public GameObject contenedor;
            public RectTransform rtContenedor;
            public Image icono;
            public Image veloOscuro;
            public RectTransform rtVeloOscuro;
            public float duracionTotal;
            public float tiempoRestante;
        }

        private readonly System.Collections.Generic.List<SlotEfectoConsumibleHUD> _efectosActivosDerecha = new System.Collections.Generic.List<SlotEfectoConsumibleHUD>();

        public void ActivarEfectoConsumibleDerecha(TipoMejoraRun tipo, float duracion)
        {
            if (duracion <= 0f) return;

            // Si ya existe el efecto activo a la derecha, refrescar su tiempo al 100%
            for (int i = 0; i < _efectosActivosDerecha.Count; i++)
            {
                if (_efectosActivosDerecha[i].tipo == tipo && _efectosActivosDerecha[i].contenedor != null)
                {
                    _efectosActivosDerecha[i].duracionTotal = duracion;
                    _efectosActivosDerecha[i].tiempoRestante = duracion;
                    return;
                }
            }

            // Crear recuadro de efecto estilo Minecraft en el borde derecho de la pantalla
            var slotObj = new GameObject($"EfectoHUD_{tipo}");
            slotObj.transform.SetParent(transform, false);

            var rtSlot = slotObj.AddComponent<RectTransform>();
            rtSlot.anchorMin = new Vector2(1f, 1f);
            rtSlot.anchorMax = new Vector2(1f, 1f);
            rtSlot.pivot = new Vector2(1f, 1f);
            rtSlot.sizeDelta = new Vector2(54f, 54f);

            var imgBorde = slotObj.AddComponent<Image>();
            imgBorde.color = new Color(0.24f, 0.27f, 0.34f, 0.94f);
            imgBorde.raycastTarget = false;

            var fondoIntObj = new GameObject("FondoInterior");
            fondoIntObj.transform.SetParent(slotObj.transform, false);
            var rtFondoInt = fondoIntObj.AddComponent<RectTransform>();
            rtFondoInt.anchorMin = Vector2.zero;
            rtFondoInt.anchorMax = Vector2.one;
            rtFondoInt.offsetMin = new Vector2(3f, 3f);
            rtFondoInt.offsetMax = new Vector2(-3f, -3f);
            var imgFondoInt = fondoIntObj.AddComponent<Image>();
            imgFondoInt.color = new Color(0.07f, 0.08f, 0.12f, 0.94f);
            imgFondoInt.raycastTarget = false;

            // Dibujito del efecto consumible
            var iconoObj = new GameObject("DibujitoEfecto");
            iconoObj.transform.SetParent(fondoIntObj.transform, false);
            var rtIcono = iconoObj.AddComponent<RectTransform>();
            rtIcono.anchorMin = Vector2.zero;
            rtIcono.anchorMax = Vector2.one;
            rtIcono.offsetMin = new Vector2(4f, 4f);
            rtIcono.offsetMax = new Vector2(-4f, -4f);
            var imgIcono = iconoObj.AddComponent<Image>();
            imgIcono.sprite = ItemMejoraRun.ObtenerSpriteMejora(tipo);
            imgIcono.preserveAspect = true;
            imgIcono.color = Color.white;
            imgIcono.raycastTarget = false;

            // Velo de oscurecimiento progresivo que va cubriendo y oscureciendo el dibujito hasta desaparecer
            var veloObj = new GameObject("VeloOscurecimiento");
            veloObj.transform.SetParent(fondoIntObj.transform, false);
            var rtVelo = veloObj.AddComponent<RectTransform>();
            rtVelo.anchorMin = new Vector2(0f, 0f);
            rtVelo.anchorMax = new Vector2(1f, 0f);
            rtVelo.offsetMin = Vector2.zero;
            rtVelo.offsetMax = Vector2.zero;
            var imgVelo = veloObj.AddComponent<Image>();
            imgVelo.color = new Color(0f, 0f, 0f, 0.72f);
            imgVelo.raycastTarget = false;

            var nuevoSlot = new SlotEfectoConsumibleHUD
            {
                tipo = tipo,
                contenedor = slotObj,
                rtContenedor = rtSlot,
                icono = imgIcono,
                veloOscuro = imgVelo,
                rtVeloOscuro = rtVelo,
                duracionTotal = duracion,
                tiempoRestante = duracion
            };

            _efectosActivosDerecha.Add(nuevoSlot);
            ReposicionarEfectosDerecha();
        }

        public void LimpiarEfectosConsumiblesDerecha()
        {
            for (int i = _efectosActivosDerecha.Count - 1; i >= 0; i--)
            {
                if (_efectosActivosDerecha[i].contenedor != null)
                {
                    Destroy(_efectosActivosDerecha[i].contenedor);
                }
            }
            _efectosActivosDerecha.Clear();
        }

        private void ReposicionarEfectosDerecha()
        {
            for (int i = 0; i < _efectosActivosDerecha.Count; i++)
            {
                var s = _efectosActivosDerecha[i];
                if (s.rtContenedor != null)
                {
                    // Ubicados a la derecha de la pantalla, justo debajo del botón de pausa/minimapa
                    s.rtContenedor.anchoredPosition = new Vector2(-18f, -242f - (i * 62f));
                }
            }
        }

        private void ActualizarEfectosConsumiblesDerecha(float dt)
        {
            if (_efectosActivosDerecha.Count == 0) return;

            bool huboEliminado = false;
            for (int i = _efectosActivosDerecha.Count - 1; i >= 0; i--)
            {
                var s = _efectosActivosDerecha[i];
                if (s == null || s.contenedor == null)
                {
                    _efectosActivosDerecha.RemoveAt(i);
                    huboEliminado = true;
                    continue;
                }

                s.tiempoRestante -= dt;
                if (s.tiempoRestante <= 0f)
                {
                    Destroy(s.contenedor);
                    _efectosActivosDerecha.RemoveAt(i);
                    huboEliminado = true;
                    continue;
                }

                // Fracción de tiempo restante (1 = recién obtenido, 0 = a punto de desaparecer)
                float ratio = Mathf.Clamp01(s.tiempoRestante / Mathf.Max(0.1f, s.duracionTotal));
                float fraccionGastada = 1f - ratio;

                // 1) El dibujito se va oscureciendo gradualmente conforme pasa el tiempo
                if (s.icono != null)
                {
                    float brillo = Mathf.Lerp(0.22f, 1.0f, ratio);
                    float alpha = 1.0f;
                    // En los últimos 3.5 segundos parpadea suavemente como en Minecraft antes de desaparecer
                    if (s.tiempoRestante < 3.5f)
                    {
                        alpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 7.5f));
                    }
                    s.icono.color = new Color(brillo, brillo, brillo, alpha);
                }

                // 2) El velo oscuro va subiendo/cubriendo el recuadro a medida que se agota
                if (s.rtVeloOscuro != null)
                {
                    s.rtVeloOscuro.anchorMin = new Vector2(0f, 1f - fraccionGastada);
                    s.rtVeloOscuro.anchorMax = new Vector2(1f, 1f);
                    s.rtVeloOscuro.offsetMin = Vector2.zero;
                    s.rtVeloOscuro.offsetMax = Vector2.zero;
                }
            }

            if (huboEliminado)
            {
                ReposicionarEfectosDerecha();
            }
        }

        public void ConectarPersonaje(Personaje personaje)
        {
            _personajeLocal = personaje;
            ConfigurarIconosPersonaje(personaje);
            ActualizarVida(personaje.VidaActual.Value, personaje.vidaMax);

            if (personaje.nivelProgreso != null)
            {
                ActualizarProgresoRun(personaje.nivelProgreso.NivelActual.Value, personaje.nivelProgreso.XPActual.Value, personaje.nivelProgreso.XPSiguienteNivel.Value);
                personaje.nivelProgreso.NivelActual.OnValueChanged += (ant, act) =>
                {
                    ActualizarProgresoRun(act, personaje.nivelProgreso.XPActual.Value, personaje.nivelProgreso.XPSiguienteNivel.Value);
                    ActualizarDesbloqueoHabilidadesVisual();
                };
                personaje.nivelProgreso.XPActual.OnValueChanged += (ant, act) =>
                {
                    ActualizarProgresoRun(personaje.nivelProgreso.NivelActual.Value, act, personaje.nivelProgreso.XPSiguienteNivel.Value);
                };
            }

            ActualizarBarraMaestria(personaje.TipoPj);
            ActualizarDesbloqueoHabilidadesVisual();
        }

        public void ActualizarBarraMaestria(TipoPersonaje pj)
        {
            if (barraMaestria == null || pj == TipoPersonaje.Ninguno) return;
            int xpTotal = GestorMaestria.ObtenerPuntos(pj);
            int nivelMaestria = GestorMaestria.ObtenerNivelMaestria(pj);
            bool esMax = nivelMaestria >= GestorMaestria.NIVEL_MAXIMO_MAESTRIA;
            int xpEnNivel = esMax ? GestorMaestria.XP_POR_NIVEL_MAESTRIA : (xpTotal % GestorMaestria.XP_POR_NIVEL_MAESTRIA);
            barraMaestria.Establecer(xpEnNivel, GestorMaestria.XP_POR_NIVEL_MAESTRIA, false);
            if (barraMaestria.textoVida != null)
            {
                barraMaestria.textoVida.text = esMax
                    ? $"Maestría Nv.{nivelMaestria} (MÁXIMO)"
                    : $"Maestría Nv.{nivelMaestria} ({xpEnNivel}/{GestorMaestria.XP_POR_NIVEL_MAESTRIA} XP)";
            }
        }

        public void ActualizarProgresoRun(int nivel, int xpActual, int xpMax)
        {
            if (barraXPRun == null) return;
            bool esMax = nivel >= NivelEnPartida.NivelMaximo;
            int xpMostrar = esMax ? Mathf.Max(1, xpMax) : xpActual;
            barraXPRun.Establecer(xpMostrar, Mathf.Max(1, xpMax), false);
            if (barraXPRun.textoVida != null)
            {
                barraXPRun.textoVida.text = esMax 
                    ? $"⭐ NIVEL RUN {NivelEnPartida.NivelMaximo} (MÁXIMO)" 
                    : $"Nv.{nivel} ({xpActual}/{xpMax} XP)";
            }
        }



        private void OnMaestriaActualizada(TipoPersonaje tipo, int totalXP)
        {
            if (_personajeLocal != null && _personajeLocal.TipoPj == tipo)
            {
                ActualizarBarraMaestria(tipo);
                ActualizarDesbloqueoHabilidadesVisual();
            }
        }

        public void ActualizarDesbloqueoHabilidadesVisual()
        {
            if (_personajeLocal == null || slotsHabilidades == null) return;
            int nivelRun = _personajeLocal.nivelProgreso != null ? _personajeLocal.nivelProgreso.NivelActual.Value : 1;

            for (int i = 0; i < slotsHabilidades.Length; i++)
            {
                var slot = slotsHabilidades[i];
                if (slot == null || slot.icono == null) continue;

                IdHabilidad habId = ObtenerIdHabilidadDeSlot(_personajeLocal, i);
                if (habId == IdHabilidad.Ninguna) continue;

                var def = CatalogoHabilidades.Obtener(habId);
                if (def == null) continue;

                // 1. Condición Absoluta: Desbloqueada por Maestría permanente
                bool desbloqueadaPorMaestria = GestorMaestria.EstaDesbloqueada(_personajeLocal.TipoPj, habId);
                if (!desbloqueadaPorMaestria)
                {
                    // Totalmente oscurecida porque aún no se alcanza la maestría necesaria
                    slot.icono.color = new Color(0.12f, 0.12f, 0.16f, 0.38f);
                    if (slot.textoTiempo != null)
                    {
                        slot.textoTiempo.fontSize = 10;
                        slot.textoTiempo.text = "🔒 BLOQ";
                        slot.textoTiempo.color = new Color(1f, 0.70f, 0.25f);
                    }
                    continue;
                }

                // 2. Condición Temporal: Nivel dentro de la run actual
                bool bloqueadaPorRun = nivelRun < def.nivelRequeridoRun;
                if (bloqueadaPorRun)
                {
                    // Oscurecida según el nivel de la run
                    slot.icono.color = new Color(0.25f, 0.25f, 0.30f, 0.65f);
                    if (slot.textoTiempo != null)
                    {
                        slot.textoTiempo.fontSize = 11;
                        slot.textoTiempo.text = $"Nv.{def.nivelRequeridoRun}";
                        slot.textoTiempo.color = new Color(0.85f, 0.85f, 0.85f);
                    }
                    continue;
                }

                // 3. Desbloqueada y disponible para usar
                slot.icono.color = Color.white;
                if (slot.textoTiempo != null && (slot.textoTiempo.text.StartsWith("Nv.") || slot.textoTiempo.text.Contains("BLOQ") || slot.textoTiempo.text.Contains("MAESTRÍA")))
                {
                    slot.textoTiempo.text = "";
                }
            }
        }

        public IdHabilidad ObtenerIdHabilidadDeSlot(Personaje personaje, int index)
        {
            if (personaje == null) return IdHabilidad.Ninguna;
            return personaje.TipoPj switch
            {
                TipoPersonaje.Fuego => index switch
                {
                    0 => IdHabilidad.Fuego_BolaFuego,
                    1 => IdHabilidad.Fuego_Lanzallamas,
                    2 => IdHabilidad.Fuego_Armadura,
                    _ => IdHabilidad.Ninguna
                },
                TipoPersonaje.Hielo => index switch
                {
                    0 => IdHabilidad.Hielo_RayoHielo,
                    1 => IdHabilidad.Hielo_BolaHielo,
                    2 => IdHabilidad.Hielo_Armadura,
                    3 => IdHabilidad.Hielo_EraHielo,
                    _ => IdHabilidad.Ninguna
                },
                TipoPersonaje.Electrico => index switch
                {
                    0 => IdHabilidad.Elec_Espada,
                    1 => IdHabilidad.Elec_Dash,
                    2 => IdHabilidad.Elec_Teleport,
                    3 => IdHabilidad.Elec_Rayo,
                    _ => IdHabilidad.Ninguna
                },
                _ => IdHabilidad.Ninguna
            };
        }

        public void ConfigurarIconosPersonaje(Personaje personaje)
        {
            if (personaje == null || slotsHabilidades == null) return;

            if (personaje is PersonajeFuego)
            {
                ConfigurarSlot(0, iconoFuegoLMB, "LMB", true);
                ConfigurarSlot(1, iconoFuegoE, "E", true);
                ConfigurarSlot(2, iconoFuegoQ, "Q", true);
                ConfigurarSlot(3, null, "", false); // Fuego no tiene R
            }
            else if (personaje is PersonajeHielo)
            {
                ConfigurarSlot(0, iconoHieloLMB, "LMB", true);
                ConfigurarSlot(1, iconoHieloE, "E", true);
                ConfigurarSlot(2, iconoHieloQ, "Q", true);
                ConfigurarSlot(3, iconoHieloR, "R", true);
            }
            else if (personaje is PersonajeElectrico)
            {
                ConfigurarSlot(0, iconoElecLMB, "LMB", true);
                ConfigurarSlot(1, iconoElecE, "E", true);
                ConfigurarSlot(2, iconoElecQ, "Q", true);
                ConfigurarSlot(3, iconoElecR, "R", true);
            }
        }

        private void ConfigurarSlot(int index, Sprite sprite, string tecla, bool activo)
        {
            if (slotsHabilidades == null || index < 0 || index >= slotsHabilidades.Length) return;

            var slot = slotsHabilidades[index];
            if (slot == null) return;

            if (slot.contenedor != null)
            {
                slot.contenedor.SetActive(activo);
            }

            if (!activo) return;

            if (slot.icono != null)
            {
                if (sprite != null)
                {
                    slot.icono.sprite = sprite;
                    slot.icono.enabled = true;
                }
                slot.icono.color = Color.white;
            }

            if (slot.textoTecla != null)
            {
                slot.textoTecla.text = tecla;
            }

            if (slot.sombraCooldown != null)
            {
                slot.sombraCooldown.fillAmount = 0f;
                slot.sombraCooldown.gameObject.SetActive(false);
            }

            if (slot.textoTiempo != null)
            {
                slot.textoTiempo.text = "";
            }
        }

        public void ActualizarVida(int actual, int max)
        {
            if (barraVida != null)
            {
                barraVida.Establecer(actual, max);
            }
        }

        public void ActualizarCooldown(int index, float tiempoRestante, float tiempoTotal)
        {
            // Actualización de slots avanzados
            if (slotsHabilidades != null && index >= 0 && index < slotsHabilidades.Length)
            {
                var slot = slotsHabilidades[index];
                if (slot != null && _personajeLocal != null)
                {
                    IdHabilidad habId = ObtenerIdHabilidadDeSlot(_personajeLocal, index);
                    if (habId != IdHabilidad.Ninguna)
                    {
                        var def = CatalogoHabilidades.Obtener(habId);
                        if (def != null)
                        {
                            // 1. Maestría
                            if (!GestorMaestria.EstaDesbloqueada(_personajeLocal.TipoPj, habId))
                            {
                                if (slot.icono != null) slot.icono.color = new Color(0.12f, 0.12f, 0.16f, 0.38f);
                                if (slot.textoTiempo != null)
                                {
                                    slot.textoTiempo.fontSize = 10;
                                    slot.textoTiempo.text = "🔒 BLOQ";
                                    slot.textoTiempo.color = new Color(1f, 0.70f, 0.25f);
                                }
                                return;
                            }

                            // 2. Nivel en run
                            int nivelRun = _personajeLocal.nivelProgreso != null ? _personajeLocal.nivelProgreso.NivelActual.Value : 1;
                            if (nivelRun < def.nivelRequeridoRun)
                            {
                                if (slot.icono != null) slot.icono.color = new Color(0.25f, 0.25f, 0.30f, 0.65f);
                                if (slot.textoTiempo != null)
                                {
                                    slot.textoTiempo.fontSize = 11;
                                    slot.textoTiempo.text = $"Nv.{def.nivelRequeridoRun}";
                                    slot.textoTiempo.color = new Color(0.85f, 0.85f, 0.85f);
                                }
                                return;
                            }
                        }
                    }

                    bool enCooldown = tiempoRestante > 0.05f;

                    // Oscurecer el sprite claramente al usar la habilidad, y dejarlo 100% brillante al estar listo
                    if (slot.icono != null)
                    {
                        slot.icono.color = enCooldown ? new Color(0.32f, 0.32f, 0.32f, 1f) : Color.white;
                    }

                    // Sombra de barrido radial
                    if (slot.sombraCooldown != null)
                    {
                        if (enCooldown)
                        {
                            slot.sombraCooldown.gameObject.SetActive(true);
                            float factor = (tiempoTotal > 0f) ? Mathf.Clamp01(tiempoRestante / tiempoTotal) : 0f;
                            slot.sombraCooldown.fillAmount = factor;
                        }
                        else
                        {
                            slot.sombraCooldown.fillAmount = 0f;
                            slot.sombraCooldown.gameObject.SetActive(false);
                        }
                    }

                    // Contador numérico de segundos
                    if (slot.textoTiempo != null)
                    {
                        if (enCooldown)
                        {
                            slot.textoTiempo.fontSize = 13;
                            slot.textoTiempo.color = Color.white;
                            slot.textoTiempo.text = tiempoRestante >= 10f 
                                ? Mathf.CeilToInt(tiempoRestante).ToString() 
                                : tiempoRestante.ToString("F1");
                        }
                        else
                        {
                            slot.textoTiempo.text = "";
                        }
                    }
                }
            }

            // Compatibilidad hacia atrás si hay iconos enlazados directamente
            if (iconosHabilidades != null && index >= 0 && index < iconosHabilidades.Length)
            {
                Image icono = iconosHabilidades[index];
                if (icono != null && tiempoTotal > 0f)
                {
                    icono.fillAmount = Mathf.Clamp01(tiempoRestante / tiempoTotal);
                }
            }
        }

        public void AlternarPausa()
        {
            if (panelFinPartida != null && panelFinPartida.activeSelf) return;

            bool estado = !panelPausa.activeSelf;
            panelPausa.SetActive(estado);
            Controles.Bloqueado = estado;

            // Ocultar el minimapa mientras el menú de pausa esté abierto
            if (minimapaHUD != null)
            {
                minimapaHUD.gameObject.SetActive(!estado);
            }

            // Modo solitario: pausar el tiempo del juego si solo hay 1 jugador
            bool esSolitario = Unity.Netcode.NetworkManager.Singleton == null 
                || !Unity.Netcode.NetworkManager.Singleton.IsListening 
                || Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds.Count <= 1;

            if (esSolitario)
            {
                Time.timeScale = estado ? 0f : 1f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (estado)
            {
                ActualizarVisibilidadBotonLobbyPausa();
            }
            else
            {
                if (PanelOpcionesControles.Instancia != null)
                {
                    PanelOpcionesControles.Instancia.Cerrar();
                }
            }
        }

        public void CerrarPausa()
        {
            if (panelPausa) panelPausa.SetActive(false);
            Controles.Bloqueado = false;
            Time.timeScale = 1f;

            if (minimapaHUD != null)
            {
                minimapaHUD.gameObject.SetActive(true);
            }

            if (PanelOpcionesControles.Instancia != null)
            {
                PanelOpcionesControles.Instancia.Cerrar();
            }
        }

        public void SalirAlMenuPrincipal()
        {
            Controles.Bloqueado = false;
            Time.timeScale = 1f;
            if (Global.Instancia != null)
            {
                Global.Instancia.SalirAlMenu("Has salido de la partida.");
            }
        }

        /// <summary>
        /// Llamado al pulsar "Volver al Lobby" desde el Menú de Pausa a media run (inicia votación si hay varios jugadores).
        /// </summary>
        public void VolverAlLobbyDesdePausa()
        {
            CerrarPausa();

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SolicitarVolverAlLobbyServerRpc();
            }
            else
            {
                SalirAlMenuPrincipal();
            }
        }

        /// <summary>
        /// Llamado al pulsar "Volver al Lobby" en la pantalla de Fin de Partida (cuando mueren todos).
        /// Manda a cada jugador individualmente al Lobby según lo vaya pulsando, sin votación.
        /// </summary>
        public void VolverAlLobbyFinPartidaIndividual()
        {
            SalaRed.MostrarFinPartidaPendiente = false;
            Controles.Bloqueado = false;

            if (panelFinPartida != null)
            {
                panelFinPartida.SetActive(false);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SolicitarVolverAlLobbyFinPartidaServerRpc();
            }
            else
            {
                SalirAlMenuPrincipal();
            }
        }

        public void VolverAlLobby()
        {
            if (panelFinPartida != null && panelFinPartida.activeSelf)
            {
                VolverAlLobbyFinPartidaIndividual();
            }
            else
            {
                VolverAlLobbyDesdePausa();
            }
        }

        public void MostrarPanelVotacion(ulong idIniciador = ulong.MaxValue)
        {
            CrearPanelVotacionSiNoExiste();
            _yaVoteLocalmente = false;

            if (panelVotacion != null)
            {
                panelVotacion.SetActive(true);
            }

            if (botonVotarSi != null) botonVotarSi.interactable = true;
            if (botonVotarNo != null) botonVotarNo.interactable = true;

            // Si soy quien inició la votación, ya voté que SÍ automáticamente
            ulong miId = Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
            if (idIniciador == miId)
            {
                _yaVoteLocalmente = true;
                if (botonVotarSi != null) botonVotarSi.interactable = false;
                if (botonVotarNo != null) botonVotarNo.interactable = false;
            }
        }

        public void Votar(bool votoSi)
        {
            if (_yaVoteLocalmente) return;
            _yaVoteLocalmente = true;

            if (botonVotarSi != null) botonVotarSi.interactable = false;
            if (botonVotarNo != null) botonVotarNo.interactable = false;

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.RegistrarVotoLobbyServerRpc(votoSi);
            }
        }

        public void CerrarPanelVotacion(bool aprobada)
        {
            if (panelVotacion != null)
            {
                panelVotacion.SetActive(false);
            }
        }

        public void MostrarAvisoPasaje(string mensaje)
        {
            if (textoAvisoPasaje != null)
            {
                textoAvisoPasaje.text = mensaje;
                textoAvisoPasaje.gameObject.SetActive(true);
                _tiempoAvisoPasaje = 3.5f;
            }
        }

        public void IniciarFadeOscurecer(float duracion = 0.8f, Action alTerminar = null)
        {
            if (_rutinaFade != null) StopCoroutine(_rutinaFade);
            _rutinaFade = StartCoroutine(RutinaFade(1f, duracion, alTerminar));
        }

        public void IniciarFadeAclarar(float duracion = 0.8f, string textoBanner = "", Action alTerminar = null)
        {
            if (_rutinaFade != null) StopCoroutine(_rutinaFade);
            if (textoBannerNivel != null && !string.IsNullOrEmpty(textoBanner))
            {
                textoBannerNivel.text = textoBanner;
                textoBannerNivel.gameObject.SetActive(true);
            }
            _rutinaFade = StartCoroutine(RutinaFade(0f, duracion, () =>
            {
                if (textoBannerNivel != null)
                {
                    StartCoroutine(RutinaOcultarBanner());
                }
                alTerminar?.Invoke();
            }));
        }

        private System.Collections.IEnumerator RutinaFade(float alfaObjetivo, float duracion, Action alTerminar)
        {
            if (panelFadeNegro == null)
            {
                alTerminar?.Invoke();
                yield break;
            }

            panelFadeNegro.gameObject.SetActive(true);
            panelFadeNegro.raycastTarget = alfaObjetivo > 0.01f;

            Color c = panelFadeNegro.color;
            float alfaInicial = c.a;
            float t = 0f;

            while (t < duracion)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(alfaInicial, alfaObjetivo, t / duracion);
                panelFadeNegro.color = c;
                yield return null;
            }

            c.a = alfaObjetivo;
            panelFadeNegro.color = c;
            if (alfaObjetivo <= 0.01f)
            {
                panelFadeNegro.gameObject.SetActive(false);
            }

            alTerminar?.Invoke();
        }

        private System.Collections.IEnumerator RutinaOcultarBanner()
        {
            yield return new WaitForSeconds(2.8f);
            if (textoBannerNivel != null)
            {
                textoBannerNivel.gameObject.SetActive(false);
            }
        }

        private void AlTerminarPartida(bool victoria)
        {
            Controles.Bloqueado = true;
            Time.timeScale = 1f;
            if (minimapaHUD != null) minimapaHUD.gameObject.SetActive(false);
            if (panelPausa) panelPausa.SetActive(false);
            if (panelFinPartida)
            {
                panelFinPartida.SetActive(true);
                if (textoResultado)
                {
                    textoResultado.text = victoria ? "¡VICTORIA!" : "TODO EL EQUIPO HA CAÍDO";
                }
            }
        }
    }
}
