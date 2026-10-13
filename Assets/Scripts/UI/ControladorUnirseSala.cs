using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla para buscar sala por nombre en la LAN sin requerir IP (UnirseSala.gd en Godot).
    /// </summary>
    public class ControladorUnirseSala : MonoBehaviour
    {
        public InputField inputNombreSala;
        public InputField inputPassword;
        public Button botonBuscar;
        public Button botonVolver;
        public Text textoEstado;

        public Button botonConectarIP;

        void Start()
        {
            if (textoEstado) textoEstado.text = "";
            if (botonBuscar) botonBuscar.onClick.AddListener(AlBuscar);
            if (botonConectarIP) botonConectarIP.onClick.AddListener(AlBuscar);
            if (botonVolver) botonVolver.onClick.AddListener(() => SceneManager.LoadScene(Escenas.MenuJugar));

            if (Global.Instancia != null)
            {
                Global.Instancia.BusquedaIniciada += AlIniciarBusqueda;
                Global.Instancia.SalaNoEncontrada += AlNoEncontrarSala;
                Global.Instancia.ConexionFallida += AlFallarConexion;
            }
        }

        void OnDestroy()
        {
            if (Global.Instancia != null)
            {
                Global.Instancia.BusquedaIniciada -= AlIniciarBusqueda;
                Global.Instancia.SalaNoEncontrada -= AlNoEncontrarSala;
                Global.Instancia.ConexionFallida -= AlFallarConexion;
            }
        }

        private void AlBuscar()
        {
            string texto = inputNombreSala != null ? inputNombreSala.text.Trim() : "";
            if (string.IsNullOrEmpty(texto))
            {
                if (textoEstado) textoEstado.text = "Introduce el Código Secreto de la Sala (ej. SK-XXXX-XXXX-XX).";
                return;
            }

            if (botonBuscar) botonBuscar.interactable = false;
            if (botonConectarIP) botonConectarIP.interactable = false;
            string pass = inputPassword != null ? inputPassword.text : "";

            // Verificar si es un Código Secreto
            if (CodigoSala.DesencriptarCodigo(texto, out string ipDesencriptada, out ushort puerto))
            {
                if (textoEstado) textoEstado.text = "¡Código Secreto verificado! Conectando a la sala...";
                if (Global.Instancia != null)
                {
                    Global.Instancia.ConectarPorIP(ipDesencriptada, puerto, pass);
                }
                return;
            }

            // Si es un nombre de sala LAN
            if (textoEstado) textoEstado.text = "Buscando la sala en red local...";
            if (Global.Instancia != null)
            {
                Global.Instancia.BuscarSala(texto, pass);
            }
        }

        private void AlIniciarBusqueda()
        {
            if (textoEstado && !textoEstado.text.StartsWith("¡Código Secreto"))
            {
                textoEstado.text = "Buscando sala o conectando...";
            }
        }

        private void AlNoEncontrarSala()
        {
            if (botonBuscar) botonBuscar.interactable = true;
            if (botonConectarIP) botonConectarIP.interactable = true;
            if (textoEstado) textoEstado.text = "No se encontró sala con ese código o nombre. Pídele al creador su Código de Sala.";
        }

        private void AlFallarConexion(string motivo)
        {
            if (botonBuscar) botonBuscar.interactable = true;
            if (botonConectarIP) botonConectarIP.interactable = true;
            if (textoEstado) textoEstado.text = motivo;
        }
    }
}
