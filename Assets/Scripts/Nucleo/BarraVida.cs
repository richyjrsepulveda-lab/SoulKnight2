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

        public void Establecer(float actual, float maximo)
        {
            float ratio = maximo > 0f ? Mathf.Clamp01(actual / maximo) : 0f;
            if (relleno != null)
            {
                relleno.fillAmount = ratio;

                // Ajuste físico del RectTransform para recortar el relleno visual de 0 a 100%
                var rt = relleno.rectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(ratio, 1f);
                    rt.sizeDelta = Vector2.zero;
                }
            }
            if (textoVida != null)
            {
                textoVida.text = $"{Mathf.Max(0, Mathf.RoundToInt(actual))} / {Mathf.RoundToInt(maximo)}";
            }
        }
    }
}
