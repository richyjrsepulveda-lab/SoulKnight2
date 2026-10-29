using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Componente ligero para animar sprites de baldosas y props (antorchas, trampas, lava, pebeteros)
    /// utilizando tiras de frames o arrays de sprites a los FPS especificados en el manifest.
    /// </summary>
    public class AnimadorSpriteTile : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float fps = 8f;
        public bool aleatorizarInicio = true;

        private float _timer;
        private int _frameActual;

        void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            if (frames != null && frames.Length > 0)
            {
                if (aleatorizarInicio && frames.Length > 1)
                {
                    _frameActual = Random.Range(0, frames.Length);
                }
                if (sr != null) sr.sprite = frames[_frameActual];
            }
        }

        void Update()
        {
            if (frames == null || frames.Length <= 1 || fps <= 0f) return;

            _timer += Time.deltaTime;
            float tiempoPorFrame = 1f / fps;
            if (_timer >= tiempoPorFrame)
            {
                _timer -= tiempoPorFrame;
                _frameActual = (_frameActual + 1) % frames.Length;
                if (sr != null)
                {
                    sr.sprite = frames[_frameActual];
                }
            }
        }

        public void Configurar(SpriteRenderer renderer, Sprite[] framesAnim, float framesPorSegundo)
        {
            sr = renderer;
            frames = framesAnim;
            fps = framesPorSegundo;
            if (frames != null && frames.Length > 0 && sr != null)
            {
                _frameActual = Random.Range(0, frames.Length);
                sr.sprite = frames[_frameActual];
            }
        }
    }
}
