using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Número de daño que sube y se desvanece (era danoflotante.tscn en Godot).
    /// Cuenta con contorno blanco brillante de alto contraste para máxima legibilidad sobre fondos oscuros.
    /// </summary>
    public class TextoFlotante : MonoBehaviour
    {
        const float Duracion = 1.0f;
        const float Gravedad = 5.2f;

        TextMesh _texto;
        List<TextMesh> _bordesBlanco = new List<TextMesh>();
        Color _color;
        float _tiempo;
        float _vx;
        float _vy;

        static Font _fuente;

        public static void Crear(Vector3 posicion, string texto, Color color)
        {
            if (_fuente == null) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Desfase aleatorio para evitar que los textos se encimen directamente unos sobre otros
            Vector3 offset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.2f), 0f);
            var go = new GameObject("TextoFlotante");
            go.transform.position = posicion + offset;

            // Contorno blanco de alto contraste en 4 direcciones cardinales
            float bOff = 0.024f;
            Vector3[] offsetsBorde = new Vector3[]
            {
                new Vector3(-bOff, 0f, 0f),
                new Vector3(bOff, 0f, 0f),
                new Vector3(0f, -bOff, 0f),
                new Vector3(0f, bOff, 0f),
                new Vector3(-bOff * 0.7f, -bOff * 0.7f, 0f),
                new Vector3(bOff * 0.7f, bOff * 0.7f, 0f)
            };

            var listaBordes = new List<TextMesh>();
            foreach (var off in offsetsBorde)
            {
                var subGo = new GameObject("BordeBlanco");
                subGo.transform.SetParent(go.transform, false);
                subGo.transform.localPosition = off;
                var subTm = subGo.AddComponent<TextMesh>();
                subTm.font = _fuente;
                subTm.text = texto;
                subTm.fontSize = 52;
                subTm.characterSize = 0.092f;
                subTm.fontStyle = FontStyle.Bold;
                subTm.anchor = TextAnchor.MiddleCenter;
                subTm.alignment = TextAlignment.Center;
                subTm.color = Color.white;
                var subMr = subGo.GetComponent<MeshRenderer>();
                subMr.sharedMaterial = _fuente.material;
                subMr.sortingOrder = 99; // Detrás del texto coloreado
                listaBordes.Add(subTm);
            }

            var tm = go.AddComponent<TextMesh>();
            tm.font = _fuente;
            tm.text = texto;
            tm.fontSize = 52;
            tm.characterSize = 0.092f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _fuente.material;
            mr.sortingOrder = 100; // Por encima de sprites y entidades

            var tf = go.AddComponent<TextoFlotante>();
            tf._texto = tm;
            tf._bordesBlanco = listaBordes;
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

            // Desvanecimiento suave tanto del texto central como de su borde blanco
            float alfa = Mathf.Clamp01(1f - (_tiempo / Duracion));
            Color c = _color;
            c.a = alfa;
            _texto.color = c;

            Color cBorde = Color.white;
            cBorde.a = alfa;
            for (int i = 0; i < _bordesBlanco.Count; i++)
            {
                if (_bordesBlanco[i] != null) _bordesBlanco[i].color = cBorde;
            }

            if (_tiempo >= Duracion) Destroy(gameObject);
        }
    }
}
