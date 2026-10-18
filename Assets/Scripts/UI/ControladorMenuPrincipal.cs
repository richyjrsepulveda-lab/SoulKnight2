using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Controlador para la pantalla de Menú Principal (Menu_Principal.gd en Godot).
    /// Si no hay nombre guardado en disco, redirige a PedirNombre.
    /// Botones: Jugar -> MenuJugar, Salir -> Application.Quit.
    /// </summary>
    public class ControladorMenuPrincipal : MonoBehaviour
    {
        public Button botonJugar;
        public Button botonOpciones;
        public Button botonSalir;
        public Button botonCambiarNombre;
        public Text textoMensaje;

        void Start()
        {
            if (Global.Instancia != null && !Global.Instancia.HayNombreGuardado)
            {
                SceneManager.LoadScene(Escenas.PedirNombre);
                return;
            }

            if (textoMensaje != null && !string.IsNullOrEmpty(Global.MensajeParaMenu))
            {
                textoMensaje.text = Global.MensajeParaMenu;
                Global.MensajeParaMenu = null;
            }

            if (botonJugar) botonJugar.onClick.AddListener(AlPresionarJugar);
            if (botonOpciones) botonOpciones.onClick.AddListener(AlPresionarOpciones);
            if (botonSalir) botonSalir.onClick.AddListener(AlPresionarSalir);

            GestorAudio.PlayBGM(TipoMusica.Sala);
            AsegurarBotonCambiarNombre();

            if (panelOpciones == null)
            {
                panelOpciones = GetComponentInChildren<PanelOpcionesControles>(true) ?? FindObjectOfType<PanelOpcionesControles>(true);
            }
            if (panelOpciones != null)
            {
                panelOpciones.AsegurarInterfazCompleta();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (panelOpciones != null && panelOpciones.gameObject.activeSelf)
                {
                    panelOpciones.Cerrar();
                }
                else
                {
                    AlPresionarOpciones();
                }
            }
        }

        private void AsegurarBotonCambiarNombre()
        {
            if (botonCambiarNombre == null)
            {
                var existente = transform.Find("Panel/Boton_CAMBIAR NOMBRE") ?? transform.Find("BotonCambiarNombre");
                if (existente != null)
                {
                    botonCambiarNombre = existente.GetComponent<Button>();
                }
            }

            if (botonCambiarNombre == null)
            {
                Transform contenedor = transform.Find("Panel") != null ? transform.Find("Panel") : transform;
                var go = new GameObject("BotonCambiarNombre");
                go.transform.SetParent(contenedor, false);

                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(24f, 24f);
                rt.sizeDelta = new Vector2(290f, 44f);

                var img = go.AddComponent<Image>();
                img.color = new Color(0.22f, 0.25f, 0.34f, 0.95f);

                botonCambiarNombre = go.AddComponent<Button>();
                var colors = botonCambiarNombre.colors;
                colors.highlightedColor = new Color(0.32f, 0.36f, 0.48f, 1f);
                colors.pressedColor = new Color(0.16f, 0.18f, 0.25f, 1f);
                botonCambiarNombre.colors = colors;

                var txtGo = new GameObject("Texto");
                txtGo.transform.SetParent(go.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                var txt = txtGo.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 16;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                var rt = botonCambiarNombre.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(24f, 24f);
                    rt.sizeDelta = new Vector2(290f, 44f);
                }
            }

            string nombreActual = (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
                ? Global.Instancia.NombreLocal.Trim()
                : "Jugador";

            var lbl = botonCambiarNombre.GetComponentInChildren<Text>();
            if (lbl != null)
            {
                lbl.fontSize = 15;
                lbl.text = $"✎ CAMBIAR NOMBRE ({nombreActual})";
            }

            botonCambiarNombre.onClick.RemoveListener(AlPresionarCambiarNombre);
            botonCambiarNombre.onClick.AddListener(AlPresionarCambiarNombre);
        }

        private void AlPresionarCambiarNombre()
        {
            SceneManager.LoadScene(Escenas.PedirNombre);
        }

        private void AlPresionarJugar()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            SceneManager.LoadScene(Escenas.MenuJugar);
        }

        public PanelOpcionesControles panelOpciones;

        private void AlPresionarOpciones()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            if (panelOpciones == null)
            {
                panelOpciones = GetComponentInChildren<PanelOpcionesControles>(true) ?? FindObjectOfType<PanelOpcionesControles>(true);
            }
            if (panelOpciones != null)
            {
                panelOpciones.Abrir();
            }
            else
            {
                Canvas canvas = GetComponentInChildren<Canvas>() ?? FindObjectOfType<Canvas>();
                panelOpciones = PanelOpcionesControles.ObtenerOCrear(canvas != null ? canvas.transform : transform);
                panelOpciones.Abrir();
            }
        }

        private void AlPresionarSalir()
        {
            GestorAudio.PlaySFX(TipoSFX.ClickUI);
            Application.Quit();
        }
    }
}
