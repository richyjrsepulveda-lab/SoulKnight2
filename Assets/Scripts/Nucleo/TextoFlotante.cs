using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Número de daño que sube y se desvanece (era danoflotante.tscn en Godot).
    /// Optimizado con Object Pooling para evitar asignaciones de memoria y destrucciones en combate.
    /// Cuenta con contorno negro nítido de alto contraste para máxima legibilidad.
    /// </summary>
    public class TextoFlotante : MonoBehaviour
    {
        private const float Duracion = 0.95f;
        private const float Gravedad = 5.2f;

        private static readonly Queue<TextoFlotante> _pool = new Queue<TextoFlotante>(64);
        private const int MAX_POOL = 80;

        private TextMesh _texto;
        private TextMesh[] _bordes;
        private Color _color;
        private float _tiempo;
        private float _vx;
        private float _vy;

        private static Font _fuente;

        public static TextoFlotante Crear(Vector3 posicion, string texto, Color color)
        {
            if (_fuente == null) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Vector3 offset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.2f), 0f);
            Vector3 spawnPos = posicion + offset;

            TextoFlotante tf = null;
            while (_pool.Count > 0 && tf == null)
            {
                tf = _pool.Dequeue();
            }

            if (tf == null)
            {
                tf = InstanciarNuevo();
            }

            tf.transform.position = spawnPos;
            tf.gameObject.SetActive(true);
            tf.Reiniciar(texto, color);
            return tf;
        }

        public void ActualizarAcumulado(int nuevoTotal, Color nuevoColor, Vector3 posicionObjetivo)
        {
            _color = nuevoColor;
            _tiempo = 0f; // Reiniciar tiempo de desvanecimiento para combos continuos
            _vy = 1.5f;   // Pequeño impulso ascendente
            transform.position = posicionObjetivo + new Vector3(0f, 0.25f, 0f);

            string texto = nuevoTotal.ToString();
            if (_texto != null)
            {
                _texto.text = texto;
                _texto.color = nuevoColor;
            }

            if (_bordes != null)
            {
                for (int i = 0; i < _bordes.Length; i++)
                {
                    if (_bordes[i] != null)
                    {
                        _bordes[i].text = texto;
                        _bordes[i].color = Color.black;
                    }
                }
            }
        }

        private static TextoFlotante InstanciarNuevo()
        {
            var go = new GameObject("TextoFlotante");

            // Contorno negro de alto contraste en 4 puntos cardinales (50% menos drawcalls y sub-objetos)
            float bOff = 0.026f;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-bOff, 0f, 0f),
                new Vector3(bOff, 0f, 0f),
                new Vector3(0f, -bOff, 0f),
                new Vector3(0f, bOff, 0f)
            };

            var bordes = new TextMesh[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                var subGo = new GameObject("BordeNegro");
                subGo.transform.SetParent(go.transform, false);
                subGo.transform.localPosition = offsets[i];
                var subTm = subGo.AddComponent<TextMesh>();
                subTm.font = _fuente;
                subTm.fontSize = 52;
                subTm.characterSize = 0.092f;
                subTm.fontStyle = FontStyle.Bold;
                subTm.anchor = TextAnchor.MiddleCenter;
                subTm.alignment = TextAlignment.Center;
                subTm.color = Color.black;

                var subMr = subGo.GetComponent<MeshRenderer>();
                subMr.sharedMaterial = _fuente.material;
                subMr.sortingOrder = 99;
                bordes[i] = subTm;
            }

            var tm = go.AddComponent<TextMesh>();
            tm.font = _fuente;
            tm.fontSize = 52;
            tm.characterSize = 0.092f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _fuente.material;
            mr.sortingOrder = 100;

            var tf = go.AddComponent<TextoFlotante>();
            tf._texto = tm;
            tf._bordes = bordes;

            return tf;
        }

        private void Reiniciar(string texto, Color color)
        {
            _color = color;
            _tiempo = 0f;
            _vx = Random.Range(-0.75f, 0.75f);
            _vy = Random.Range(2.2f, 2.7f);

            if (_texto != null)
            {
                _texto.text = texto;
                _texto.color = color;
            }

            if (_bordes != null)
            {
                for (int i = 0; i < _bordes.Length; i++)
                {
                    if (_bordes[i] != null)
                    {
                        _bordes[i].text = texto;
                        _bordes[i].color = Color.black;
                    }
                }
            }
        }

        void Update()
        {
            _tiempo += Time.deltaTime;

            _vy -= Gravedad * Time.deltaTime;
            _vx = Mathf.MoveTowards(_vx, 0f, 0.35f * Time.deltaTime);
            transform.position += new Vector3(_vx, _vy, 0f) * Time.deltaTime;

            float alfa = Mathf.Clamp01(1f - (_tiempo / Duracion));
            if (_texto != null)
            {
                Color c = _color;
                c.a = alfa;
                _texto.color = c;
            }

            if (_bordes != null)
            {
                Color cBorde = Color.black;
                cBorde.a = alfa;
                for (int i = 0; i < _bordes.Length; i++)
                {
                    if (_bordes[i] != null) _bordes[i].color = cBorde;
                }
            }

            if (_tiempo >= Duracion)
            {
                if (_pool.Count < MAX_POOL)
                {
                    gameObject.SetActive(false);
                    _pool.Enqueue(this);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
