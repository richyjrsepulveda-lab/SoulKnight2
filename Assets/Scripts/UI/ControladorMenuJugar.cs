using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Hub con 2 opciones: Crear Sala o Unirse a Sala (MenuJugar.gd en Godot).
    /// </summary>
    public class ControladorMenuJugar : MonoBehaviour
    {
        public Button botonCrearSala;
        public Button botonUnirseSala;
        public Button botonVolver;

        void Start()
        {
            if (botonCrearSala) botonCrearSala.onClick.AddListener(() => SceneManager.LoadScene(Escenas.CrearSala));
            if (botonUnirseSala) botonUnirseSala.onClick.AddListener(() => SceneManager.LoadScene(Escenas.UnirseSala));
            if (botonVolver) botonVolver.onClick.AddListener(() => SceneManager.LoadScene(Escenas.MenuPrincipal));
        }
    }
}
