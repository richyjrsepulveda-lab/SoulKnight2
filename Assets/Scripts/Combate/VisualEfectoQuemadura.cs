using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Efecto visual auténtico de llamas y brasas ardientes para enemigos y personajes quemados.
    /// Reemplaza el simple tinte rojizo (que parecía estado de furia/enojo) por fuego real activo.
    /// </summary>
    public class VisualEfectoQuemadura : MonoBehaviour
    {
        private static Sprite _spriteLlamaPixel;
        private static Sprite _spriteBrasaPixel;

        private readonly List<Transform> _llamas = new List<Transform>();
        private readonly List<SpriteRenderer> _srLlamas = new List<SpriteRenderer>();

        private struct Brasa
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 velocidad;
            public float vida;
            public float maxVida;
        }

        private readonly List<Brasa> _brasas = new List<Brasa>();
        private bool _activo = false;

        void Awake()
        {
            AsegurarSprites();
            ConstruirLlamas();
        }

        private static void AsegurarSprites()
        {
            if (_spriteLlamaPixel != null) return;

            // 1. Textura de Llama Ardiente (16x20)
            const int W = 16;
            const int H = 20;
            Texture2D texLlama = new Texture2D(W, H, TextureFormat.RGBA32, false);
            texLlama.filterMode = FilterMode.Point;
            texLlama.wrapMode = TextureWrapMode.Clamp;

            Color cTrans = new Color(0, 0, 0, 0);
            Color cRojo = new Color(0.95f, 0.20f, 0.05f, 0.95f);
            Color cNaranja = new Color(1.0f, 0.55f, 0.10f, 0.98f);
            Color cAmarillo = new Color(1.0f, 0.92f, 0.35f, 1f);
            Color cBlanco = new Color(1f, 1f, 0.9f, 1f);

            for (int y = 0; y < H; y++)
            {
                float normY = (float)y / (H - 1);
                float anchoNivel = (1f - normY * 0.85f) * (W * 0.45f);

                for (int x = 0; x < W; x++)
                {
                    float distCentroX = Mathf.Abs(x - (W / 2f - 0.5f));
                    if (distCentroX <= anchoNivel)
                    {
                        float factorBorde = distCentroX / Mathf.Max(0.1f, anchoNivel);
                        if (normY < 0.35f && factorBorde < 0.35f)
                            texLlama.SetPixel(x, y, cBlanco);
                        else if (factorBorde < 0.45f)
                            texLlama.SetPixel(x, y, cAmarillo);
                        else if (factorBorde < 0.80f)
                            texLlama.SetPixel(x, y, cNaranja);
                        else
                            texLlama.SetPixel(x, y, cRojo);
                    }
                    else
                    {
                        texLlama.SetPixel(x, y, cTrans);
                    }
                }
            }
            texLlama.Apply();
            _spriteLlamaPixel = Sprite.Create(texLlama, new Rect(0, 0, W, H), new Vector2(0.5f, 0.1f), 16f);

            // 2. Textura de Brasa / Chispa (4x4)
            Texture2D texBrasa = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texBrasa.filterMode = FilterMode.Point;
            for (int by = 0; by < 4; by++)
            {
                for (int bx = 0; bx < 4; bx++)
                {
                    texBrasa.SetPixel(bx, by, cAmarillo);
                }
            }
            texBrasa.Apply();
            _spriteBrasaPixel = Sprite.Create(texBrasa, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
        }

        private void ConstruirLlamas()
        {
            // 3 Llamas danzantes en la base y centro del cuerpo
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-0.25f, -0.1f, -0.1f),
                new Vector3(0.0f, 0.15f, -0.15f),
                new Vector3(0.25f, -0.05f, -0.1f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var go = new GameObject($"LlamaFuego_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = offsets[i];
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spriteLlamaPixel;
                sr.sortingOrder = 7;
                _llamas.Add(go.transform);
                _srLlamas.Add(sr);
            }

            // 4 Brasas flotantes
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"Brasa_{i}");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spriteBrasaPixel;
                sr.sortingOrder = 8;
                _brasas.Add(new Brasa
                {
                    t = go.transform,
                    sr = sr,
                    velocidad = Vector2.up * Random.Range(1.2f, 2.4f),
                    vida = Random.Range(0f, 0.6f),
                    maxVida = Random.Range(0.45f, 0.75f)
                });
            }

            SetVisible(false);
        }

        public void SetActivo(bool activo)
        {
            _activo = activo;
            SetVisible(activo);
        }

        private void SetVisible(bool v)
        {
            for (int i = 0; i < _srLlamas.Count; i++)
            {
                if (_srLlamas[i] != null) _srLlamas[i].enabled = v;
            }
            for (int i = 0; i < _brasas.Count; i++)
            {
                if (_brasas[i].sr != null) _brasas[i].sr.enabled = v;
            }
        }

        void Update()
        {
            if (!_activo) return;

            // Animar el parpadeo y ondulación de las llamas
            for (int i = 0; i < _llamas.Count; i++)
            {
                var t = _llamas[i];
                if (t == null) continue;

                float desfase = i * 2.1f;
                float escalaY = 0.85f + Mathf.Sin(Time.time * 16f + desfase) * 0.25f;
                float escalaX = 0.90f + Mathf.Cos(Time.time * 12f + desfase) * 0.18f;
                t.localScale = new Vector3(escalaX, escalaY, 1f);

                float inclinacion = Mathf.Sin(Time.time * 14f + desfase) * 12f;
                t.localRotation = Quaternion.Euler(0, 0, inclinacion);
            }

            // Animar brasas ascendentes
            for (int i = 0; i < _brasas.Count; i++)
            {
                var b = _brasas[i];
                if (b.t == null) continue;

                b.vida += Time.deltaTime;
                if (b.vida >= b.maxVida)
                {
                    b.vida = 0f;
                    b.maxVida = Random.Range(0.45f, 0.75f);
                    b.t.localPosition = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.2f), -0.2f);
                    b.velocidad = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.4f, 2.5f));
                }
                else
                {
                    b.t.localPosition += (Vector3)(b.velocidad * Time.deltaTime);
                }

                if (b.sr != null)
                {
                    float alpha = 1f - (b.vida / b.maxVida);
                    Color col = Color.Lerp(Color.yellow, new Color(1f, 0.35f, 0.05f), b.vida / b.maxVida);
                    col.a = alpha;
                    b.sr.color = col;
                }

                _brasas[i] = b;
            }
        }
    }
}
