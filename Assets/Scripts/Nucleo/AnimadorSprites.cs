using System;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Equivalente directo de AnimatedSprite2D + SpriteFrames de Godot:
    /// una lista de animaciones (cada una con sus cuadros, fps y si se repite).
    /// Se usa así: GetComponent&lt;AnimadorSprites&gt;().Reproducir("BolaFuego");
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimadorSprites : MonoBehaviour
    {
        [Serializable]
        public class Animacion
        {
            public string nombre = "default";
            public Sprite[] cuadros = Array.Empty<Sprite>();
            public float fps = 5f;
            public bool bucle = true;
        }

        public Animacion[] animaciones = Array.Empty<Animacion>();
        [Tooltip("Animación que se reproduce sola al activarse el objeto (vacío = ninguna).")]
        public string animacionInicial = "";

        SpriteRenderer _sr;
        Animacion _actual;
        float _tiempo;
        int _cuadro;

        public bool Reproduciendo { get; private set; }
        public string Actual => _actual != null ? _actual.nombre : "";

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
            if (!string.IsNullOrEmpty(animacionInicial))
                Reproducir(animacionInicial, true);
        }

        public void Reproducir(string nombre, bool reiniciar = false)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (!reiniciar && Reproduciendo && _actual != null && _actual.nombre == nombre) return;

            _actual = Array.Find(animaciones, a => a.nombre == nombre);
            if (_actual == null || _actual.cuadros.Length == 0)
            {
                Reproduciendo = false;
                return;
            }
            _tiempo = 0f;
            _cuadro = 0;
            _sr.sprite = _actual.cuadros[0];
            Reproduciendo = true;
        }

        public void Detener()
        {
            Reproduciendo = false;
        }

        void Update()
        {
            if (!Reproduciendo || _actual == null || _actual.fps <= 0f) return;

            _tiempo += Time.deltaTime;
            float duracionCuadro = 1f / _actual.fps;
            while (_tiempo >= duracionCuadro)
            {
                _tiempo -= duracionCuadro;
                _cuadro++;
                if (_cuadro >= _actual.cuadros.Length)
                {
                    if (_actual.bucle) _cuadro = 0;
                    else
                    {
                        _cuadro = _actual.cuadros.Length - 1;
                        Reproduciendo = false;
                        break;
                    }
                }
            }
            _sr.sprite = _actual.cuadros[_cuadro];
        }
    }
}
