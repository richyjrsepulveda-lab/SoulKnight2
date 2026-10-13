using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SoulKnight;
using Unity.Netcode;

namespace SoulKnight.Editor
{
    public static class TestLobby
    {
        public static void ProbarSeleccionPersonaje()
        {
            Debug.Log("=== INICIANDO TEST DE SELECCIÓN DE PERSONAJE ===");

            var scene = EditorSceneManager.OpenScene("Assets/Escenas/Lobby.unity");
            var ctrl = Object.FindObjectOfType<ControladorLobby>();
            if (ctrl == null)
            {
                Debug.LogError("ERROR: No se encontró ControladorLobby en la escena.");
                return;
            }

            Debug.Log($"ControladorLobby encontrado: {ctrl.name}");
            Debug.Log($"botonSeleccionarCuarto asignado: {ctrl.botonSeleccionarCuarto != null}");
            Debug.Log($"botonLiberarCuarto asignado: {ctrl.botonLiberarCuarto != null}");
            Debug.Log($"botonVolverCuarto asignado: {ctrl.botonVolverCuarto != null}");
            Debug.Log($"panelVistaCuarto asignado: {ctrl.panelVistaCuarto != null}");
            Debug.Log($"prefabNeutral asignado: {ctrl.prefabNeutral != null}");
            Debug.Log($"prefabFuego asignado: {ctrl.prefabFuego != null}");

            var triggers = Object.FindObjectsOfType<CuartoFisicoLobby>();
            Debug.Log($"Cantidad de CuartoFisicoLobby encontrados: {triggers.Length}");
            foreach (var t in triggers)
            {
                Debug.Log($" - Cuarto: {t.nombreCuarto}, Personaje: {t.personaje}, EsProx: {t.esProximamente}, Pos: {t.transform.position}");
            }

            Debug.Log("=== FIN DEL TEST ===");
        }
    }
}
