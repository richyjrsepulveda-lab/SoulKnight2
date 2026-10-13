using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Controlador para PedirNombre (PedirNombre.gd en Godot).
    /// Guarda el nombre del jugador en disco y vuelve al Menú Principal.
    /// </summary>
    public class ControladorPedirNombre : MonoBehaviour
    {
        public InputField inputNombre;
        public Button botonContinuar;

        void Start()
        {
            if (inputNombre != null && Global.Instancia != null && Global.Instancia.HayNombreGuardado && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
            {
                inputNombre.text = Global.Instancia.NombreLocal.Trim();
                inputNombre.ActivateInputField();
            }

            if (botonContinuar) botonContinuar.onClick.AddListener(AlContinuar);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                AlContinuar();
            }
        }

        private void AlContinuar()
        {
            string nombre = inputNombre != null ? inputNombre.text.Trim() : "";
            if (string.IsNullOrEmpty(nombre))
            {
                nombre = (Global.Instancia != null && !string.IsNullOrWhiteSpace(Global.Instancia.NombreLocal))
                    ? Global.Instancia.NombreLocal.Trim()
                    : "Jugador";
            }

            if (Global.Instancia != null)
            {
                Global.Instancia.GuardarNombre(nombre);
            }

            SceneManager.LoadScene(Escenas.MenuPrincipal);
        }
    }
}
