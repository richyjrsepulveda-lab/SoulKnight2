using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// En Godot cada personaje traía su propio Camera2D. Con varios jugadores en la misma
    /// escena eso no sirve (habría 4 cámaras peleándose), así que hay UNA sola cámara en la
    /// escena de juego y sigue al personaje de ESTA computadora (o a otro si estás espectando).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraSeguir : MonoBehaviour
    {
        public static CamaraSeguir Instancia { get; private set; }

        public Transform objetivo;
        [Tooltip("0 = pegada al objetivo. Valores mayores = sigue con suavidad.")]
        public float suavizado = 0f;

        void Awake()
        {
            Instancia = this;
            var cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void LateUpdate()
        {
            if (objetivo == null) return;
            Vector3 destino = new Vector3(objetivo.position.x, objetivo.position.y, transform.position.z);
            transform.position = suavizado <= 0f
                ? destino
                : Vector3.Lerp(transform.position, destino, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        }
    }
}
