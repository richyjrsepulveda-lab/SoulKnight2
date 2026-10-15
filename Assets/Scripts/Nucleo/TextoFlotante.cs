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
        const float Duracion = 1.0f;
        const float Gravedad = 5.2f;

        TextMesh _texto;
        Color _color;
        float _tiempo;
        float _vx;
        float _vy;

        static Font _fuente;

        public static void Crear(Vector3 posicion, string texto, Color color)
        {
            if (_fuente == null) _fuente = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Desfase aleatorio para evitar que los textos se encimen directamente unos sobre otros
            Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.15f, 0.25f), 0f);
            var go = new GameObject("TextoFlotante");
            go.transform.position = posicion + offset;

            var tm = go.AddComponent<TextMesh>();
            tm.font = _fuente;
            tm.text = texto;
            tm.fontSize = 44;
            tm.characterSize = 0.055f; // Texto visiblemente más pequeño y compacto
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _fuente.material;
            mr.sortingOrder = 100; // Por encima de sprites y entidades

            var tf = go.AddComponent<TextoFlotante>();
            tf._texto = tm;
            tf._color = color;
            // Físicas: arco con velocidad horizontal suave y subida con caída/rebote
            tf._vx = Random.Range(-0.75f, 0.75f);
            tf._vy = Random.Range(2.2f, 2.7f);
        }

        void Update()
        {
            _tiempo += Time.deltaTime;

            // Físicas de arco parabólico (sube y luego baja por gravedad)
            _vy -= Gravedad * Time.deltaTime;
            _vx = Mathf.MoveTowards(_vx, 0f, 0.35f * Time.deltaTime);
            transform.position += new Vector3(_vx, _vy, 0f) * Time.deltaTime;

            // Desvanecimiento suave
            Color c = _color;
            c.a = Mathf.Clamp01(1f - (_tiempo / Duracion));
            _texto.color = c;

            if (_tiempo >= Duracion) Destroy(gameObject);
        }
    }
}
