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

        public static void ProbarFlujoCompleto()
        {
            Debug.Log("=== VERIFICANDO ESCENAS Y PREFABS ===");
            var sceneLobby = EditorSceneManager.OpenScene("Assets/Escenas/Lobby.unity");
            var ctrlLobby = Object.FindObjectOfType<ControladorLobby>();
            if (ctrlLobby == null) throw new System.Exception("Falta ControladorLobby en Lobby.unity");
            var pjsEnLobbyEscena = Object.FindObjectsOfType<Personaje>();
            Debug.Log($"[Verificación] Personajes pre-instanciados en escena Lobby: {pjsEnLobbyEscena.Length} (Esperado: 0)");
            if (pjsEnLobbyEscena.Length != 0) throw new System.Exception("Hay personajes sueltos en Lobby.unity");

            var sceneJuego = EditorSceneManager.OpenScene("Assets/Escenas/Juego.unity");
            var gestor = Object.FindObjectOfType<GestorPartida>();
            var hud = Object.FindObjectOfType<HUDJugador>();
            if (gestor == null) throw new System.Exception("Falta GestorPartida en Juego.unity");
            if (hud == null) throw new System.Exception("Falta HUDJugador en Juego.unity");
            if (hud.textoMiNombreHUD == null) throw new System.Exception("Falta textoMiNombreHUD en HUDJugador");
            if (hud.barraVida == null) throw new System.Exception("Falta barraVida en HUDJugador");
            var pjsEnJuegoEscena = Object.FindObjectsOfType<Personaje>();
            Debug.Log($"[Verificación] Personajes pre-instanciados en escena Juego: {pjsEnJuegoEscena.Length} (Esperado: 0)");
            if (pjsEnJuegoEscena.Length != 0) throw new System.Exception("Hay personajes sueltos en Juego.unity");

            Debug.Log("=== VERIFICACIÓN EXITOSA: CERO ERRORES ===");
        }
    }
}
