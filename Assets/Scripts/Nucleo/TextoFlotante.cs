using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Número de daño que sube y se desvanece (era danoflotante.tscn en Godot).
    /// Es puramente visual: cada computadora lo crea localmente cuando el host avisa
    /// que hubo daño (ver Personaje/EnemigoBase -> MostrarDanoClientRpc).
    /// </summary>
    public class TextoFlotante : MonoBehaviour
    {
        // Godot: Vector2(0, -50) px/s hacia arriba. En Unity 1 unidad = 32 px y Y apunta arriba.
        static readonly Vector3 Velocidad = new Vector3(0f, 50f / 32f, 0f);
        const float Duracion = 1f;

        TextMesh _texto;
        Color _color;
        float _tiempo;

        static Font _fuente;

        public static void Crear(Vector3 posicion, string texto, Color color)
        {
            if (_fuente == null) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = new GameObject("TextoFlotante");
            go.transform.position = posicion;

            var tm = go.AddComponent<TextMesh>();
            tm.font = _fuente;
            tm.text = texto;
            tm.fontSize = 64;
            tm.characterSize = 0.09f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _fuente.material;
            mr.sortingOrder = 100; // z_index = 6 en Godot: por encima de todo

            var tf = go.AddComponent<TextoFlotante>();
            tf._texto = tm;
            tf._color = color;
        }

        void Update()
        {
            _tiempo += Time.deltaTime;
            transform.position += Velocidad * Time.deltaTime;
            Color c = _color;
            c.a = Mathf.Lerp(1f, 0f, _tiempo / Duracion);
            _texto.color = c;
            if (_tiempo >= Duracion) Destroy(gameObject);
        }
    }
}
