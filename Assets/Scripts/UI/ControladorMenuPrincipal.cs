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
        }

        private void AlPresionarJugar()
        {
            SceneManager.LoadScene(Escenas.MenuJugar);
        }

        public PanelOpcionesControles panelOpciones;

        private void AlPresionarOpciones()
        {
            if (panelOpciones != null)
            {
                panelOpciones.Abrir();
            }
            else if (PanelOpcionesControles.Instancia != null)
            {
                PanelOpcionesControles.Instancia.Abrir();
            }
        }

        private void AlPresionarSalir()
        {
            Application.Quit();
        }
    }
}
