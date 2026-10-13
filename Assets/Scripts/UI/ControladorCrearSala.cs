using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla para crear sala con nombre y contraseña opcional (CrearSala.gd en Godot).
    /// </summary>
    public class ControladorCrearSala : MonoBehaviour
    {
        public InputField inputNombreSala;
        public InputField inputPassword;
        public Button botonConfirmar;
        public Button botonVolver;
        public Text textoError;

        void Start()
        {
            if (textoError) textoError.text = "";
            if (botonConfirmar) botonConfirmar.onClick.AddListener(AlConfirmar);
            if (botonVolver) botonVolver.onClick.AddListener(() => SceneManager.LoadScene(Escenas.MenuJugar));

            if (Global.Instancia != null)
            {
                Global.Instancia.ConexionFallida += MostrarError;
            }
        }

        void OnDestroy()
        {
            if (Global.Instancia != null)
            {
                Global.Instancia.ConexionFallida -= MostrarError;
            }
        }

        private void AlConfirmar()
        {
            string nombreSala = inputNombreSala != null ? inputNombreSala.text.Trim() : "";
            if (string.IsNullOrEmpty(nombreSala))
            {
                MostrarError("Ponle un nombre a tu sala.");
                return;
            }

            string pass = inputPassword != null ? inputPassword.text : "";
            if (Global.Instancia != null)
            {
                Global.Instancia.CrearSala(nombreSala, pass);
            }
        }

        private void MostrarError(string mensaje)
        {
            if (textoError != null)
            {
                textoError.text = mensaje;
            }
        }
    }
}
