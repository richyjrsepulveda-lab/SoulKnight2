using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulKnight
{
    /// <summary>
    /// Equivalente al "Input Map" de Godot: TODAS las teclas del juego viven aquí.
    /// Si quieres cambiar una tecla, solo la cambias en este archivo.
    ///
    /// Mapeo (igual que en el proyecto de Godot):
    ///   WASD / Flechas  -> moverse
    ///   Shift           -> correr
    ///   Click izquierdo -> ataque principal (bola de fuego / rayo de hielo / espada)
    ///   E               -> habilidad 1 (cabeza de fuego / bola de hielo / dash)
    ///   Q               -> habilidad 2 (armaduras / teleport)
    ///   R               -> ultimate (era de hielo / rayo eléctrico)
    ///   ESC             -> menú de pausa
    /// </summary>
    public static class Controles
    {
        public static KeyCode Habilidad1 => GestorControles.ObtenerTecla(AccionControl.Habilidad1);
        public static KeyCode Habilidad2 => GestorControles.ObtenerTecla(AccionControl.Habilidad2);
        public static KeyCode Ultimate => GestorControles.ObtenerTecla(AccionControl.Ultimate);
        public static KeyCode Pausa => GestorControles.ObtenerTecla(AccionControl.Pausa);

        /// <summary>
        /// Cuando hay un menú abierto (pausa, muerte, fin de partida) el personaje
        /// deja de leer el teclado/mouse.
        /// </summary>
        public static bool Bloqueado;

        /// <summary>Las 4 direcciones. OJO: en Unity el eje Y apunta hacia ARRIBA (en Godot, hacia abajo).</summary>
        public static Vector2 LeerMovimiento()
        {
            if (Bloqueado) return Vector2.zero;
            Vector2 v = Vector2.zero;

            KeyCode arriba = GestorControles.ObtenerTecla(AccionControl.MoverArriba);
            KeyCode abajo = GestorControles.ObtenerTecla(AccionControl.MoverAbajo);
            KeyCode izquierda = GestorControles.ObtenerTecla(AccionControl.MoverIzquierda);
            KeyCode derecha = GestorControles.ObtenerTecla(AccionControl.MoverDerecha);

            if (Input.GetKey(derecha) || Input.GetKey(KeyCode.RightArrow)) v.x += 1;
            if (Input.GetKey(izquierda) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1;
            if (Input.GetKey(arriba) || Input.GetKey(KeyCode.UpArrow)) v.y += 1;
            if (Input.GetKey(abajo) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1;
            return v;
        }

        public static bool Correr
        {
            get
            {
                if (Bloqueado) return false;
                KeyCode teclaCorrer = GestorControles.ObtenerTecla(AccionControl.Correr);
                return Input.GetKey(teclaCorrer) || Input.GetKey(KeyCode.RightShift);
            }
        }

        public static bool PrincipalMantenido => !Bloqueado && (Input.GetKey(GestorControles.ObtenerTecla(AccionControl.AtaquePrincipal)) || Input.GetMouseButton(0)) && !MouseSobreUI();
        public static bool PrincipalPresionado => !Bloqueado && (Input.GetKeyDown(GestorControles.ObtenerTecla(AccionControl.AtaquePrincipal)) || Input.GetMouseButtonDown(0)) && !MouseSobreUI();

        public static bool Presionada(KeyCode tecla) => !Bloqueado && Input.GetKeyDown(tecla);
        public static bool Mantenida(KeyCode tecla) => !Bloqueado && Input.GetKey(tecla);
        public static bool AutoDisparo(KeyCode tecla) => !Bloqueado && (Input.GetKey(tecla) || Input.GetKeyDown(tecla));

        private static PointerEventData _pointerDataCache;
        private static readonly System.Collections.Generic.List<RaycastResult> _uiRaycastResultsCache = new System.Collections.Generic.List<RaycastResult>(16);

        /// <summary>Posición del mouse en coordenadas del mundo (equivale a get_global_mouse_position()).</summary>
        public static Vector2 PosicionMouseMundo()
        {
            Camera cam = CamaraUtil.Principal;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(Input.mousePosition);
        }

        static bool MouseSobreUI()
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) return false;

            if (_pointerDataCache == null)
            {
                _pointerDataCache = new PointerEventData(EventSystem.current);
            }
            _pointerDataCache.position = Input.mousePosition;

            _uiRaycastResultsCache.Clear();
            EventSystem.current.RaycastAll(_pointerDataCache, _uiRaycastResultsCache);
            for (int i = 0; i < _uiRaycastResultsCache.Count; i++)
            {
                var r = _uiRaycastResultsCache[i];
                if (r.gameObject != null && r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
