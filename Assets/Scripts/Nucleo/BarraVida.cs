using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Barra de vida (equivale a TextureProgressBar de Godot).
    /// Funciona igual en el HUD del jugador y encima de los enemigos:
    /// solo cambia el fillAmount de la imagen de relleno.
    /// </summary>
    public class BarraVida : MonoBehaviour
    {
        public Image relleno;
        public Text textoVida;

        private static Sprite _spriteBlancoRuntime;

        private static Sprite ObtenerSpriteBlanco()
        {
            if (_spriteBlancoRuntime == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _spriteBlancoRuntime = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return _spriteBlancoRuntime;
        }

        private void AsegurarSpritesValidos()
        {
            var imgFondo = GetComponent<Image>();
            if (imgFondo != null && (imgFondo.sprite == null || imgFondo.sprite.texture == null))
            {
                imgFondo.sprite = ObtenerSpriteBlanco();
                imgFondo.type = Image.Type.Simple;
            }

            if (relleno != null && (relleno.sprite == null || relleno.sprite.texture == null))
            {
                relleno.sprite = ObtenerSpriteBlanco();
            }

            if (textoVida != null)
            {
                textoVida.transform.SetAsLastSibling();
                if (textoVida.GetComponent<Shadow>() == null)
                {
                    var sombra = textoVida.gameObject.AddComponent<Shadow>();
                    sombra.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    sombra.effectDistance = new Vector2(1f, -1f);
                }
            }
        }

        void Awake()
        {
            AsegurarSpritesValidos();
        }

        public void Establecer(float actual, float maximo, bool actualizarTextoNumerico = true)
        {
            AsegurarSpritesValidos();

            float ratio = maximo > 0f ? Mathf.Clamp01(actual / maximo) : 0f;
            if (relleno != null)
            {
                // Usar Simple + anclas normalizadas con offsetMin/offsetMax explícitos para evitar desplazamientos y doble recorte
                relleno.type = Image.Type.Simple;
                relleno.fillAmount = 1f;

                var rt = relleno.rectTransform;
                if (rt != null)
                {
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(ratio, 1f);

                    if (ratio <= 0.001f)
                    {
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        relleno.enabled = false;
                    }
                    else
                    {
                        relleno.enabled = true;
                        rt.offsetMin = new Vector2(2f, 2f);
                        rt.offsetMax = new Vector2(-2f, -2f);
                        if (rt.rect.width < 0f)
                        {
                            rt.offsetMin = new Vector2(0f, 2f);
                            rt.offsetMax = new Vector2(0f, -2f);
                        }
                    }
                }
            }
            if (actualizarTextoNumerico && textoVida != null)
            {
                textoVida.text = $"{Mathf.Max(0, Mathf.RoundToInt(actual))} / {Mathf.RoundToInt(maximo)}";
            }
        }
    }
}
