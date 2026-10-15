using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulKnight
{
    /// <summary>
    /// Cuarto físico perimetral en el Lobby:
    /// - Es un área física con collider trigger dentro del mapa del Lobby.
    /// - Al entrar el jugador caminando (o hacer click sobre el cuarto), se abre la vista del cuarto.
    /// - Permite ver la maestría, configurar el loadout, y presionar SELECCIONAR o VOLVER.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CuartoFisicoLobby : MonoBehaviour
    {
        public TipoPersonaje personaje = TipoPersonaje.Ninguno;
        public bool esProximamente = false;
        public string nombreCuarto = "Cuarto";

        private bool _yaSeleccionoEnEstaEstancia = false;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var pj = collision.GetComponentInParent<Personaje>();
            if (pj == null) return;
            if (pj.NetworkObject != null && pj.NetworkObject.IsSpawned && !pj.IsOwner) return;

            // Si ya seleccionó en esta estancia, no volver a abrir la pantalla hasta que salga del cuarto y vuelva a entrar
            if (_yaSeleccionoEnEstaEstancia) return;

            if (ControladorLobby.Instancia != null)
            {
                ControladorLobby.Instancia.AlEntrarCuartoFisico(this);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var pj = collision.GetComponentInParent<Personaje>();
            if (pj == null) return;
            if (pj.NetworkObject != null && pj.NetworkObject.IsSpawned && !pj.IsOwner) return;

            // Al salir del cuarto físicamente, se restablece el permiso para volver a abrirlo si regresa
            _yaSeleccionoEnEstaEstancia = false;
        }

        public void MarcarSeleccionado()
        {
            _yaSeleccionoEnEstaEstancia = true;
        }
    }
}
