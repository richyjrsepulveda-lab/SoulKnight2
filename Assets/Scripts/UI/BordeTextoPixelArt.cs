using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Componente de efecto de malla para uGUI que dibuja un trazo (stroke) denso y continuo
    /// en 360 grados y una sombra inferior pura, reproduciendo con exactitud matemática el efecto CSS:
    /// -webkit-text-stroke: 12px #0c0818; paint-order: stroke fill; filter: drop-shadow(0 8px 0 #0c0818);
    /// Elimina completamente los huecos diagonales y artefactos del Outline nativo de Unity.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public class BordeTextoPixelArt : BaseMeshEffect
    {
        public Color colorContorno = new Color32(0x0C, 0x08, 0x18, 0xFF);
        [Range(1, 24)] public int grosor = 10;
        public Vector2 offsetSombra = new Vector2(0f, -8f);

        private readonly List<UIVertex> _vertsOriginales = new List<UIVertex>();
        private readonly List<UIVertex> _vertsResultado = new List<UIVertex>();

        public static BordeTextoPixelArt Aplicar(Text txt, int grosorTrazo = 10, Vector2? sombra = null, Color? color = null)
        {
            if (txt == null) return null;
            // Remover posibles Outlines o Shadows previos que generen artefactos o conflictos
            var outlines = txt.GetComponents<Outline>();
            for (int i = outlines.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(outlines[i]);
                else DestroyImmediate(outlines[i]);
            }
            var shadows = txt.GetComponents<Shadow>();
            for (int i = shadows.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(shadows[i]);
                else DestroyImmediate(shadows[i]);
            }

            var comp = txt.GetComponent<BordeTextoPixelArt>();
            if (comp == null) comp = txt.gameObject.AddComponent<BordeTextoPixelArt>();

            comp.grosor = grosorTrazo;
            comp.offsetSombra = sombra ?? new Vector2(0f, -8f);
            comp.colorContorno = color ?? new Color32(0x0C, 0x08, 0x18, 0xFF);
            comp.graphic?.SetVerticesDirty();
            return comp;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            _vertsOriginales.Clear();
            vh.GetUIVertexStream(_vertsOriginales);
            int count = _vertsOriginales.Count;
            if (count == 0) return;

            _vertsResultado.Clear();

            // 1. Sombra inferior proyectada pura (0, -8px por defecto)
            if (offsetSombra != Vector2.zero)
            {
                for (int i = 0; i < count; i++)
                {
                    var v = _vertsOriginales[i];
                    v.position += new Vector3(offsetSombra.x, offsetSombra.y, 0f);
                    v.color = colorContorno;
                    _vertsResultado.Add(v);
                }
            }

            // 2. Trazo circular continuo sólido de paso fino (2 a 3 px) para evitar huecos en cualquier ángulo
            int paso = Mathf.Clamp(grosor / 4, 2, 3);
            int g2 = grosor * grosor;

            for (int x = -grosor; x <= grosor; x += paso)
            {
                for (int y = -grosor; y <= grosor; y += paso)
                {
                    if (x == 0 && y == 0) continue;
                    if (x * x + y * y > g2 + (paso * grosor)) continue;

                    for (int i = 0; i < count; i++)
                    {
                        var v = _vertsOriginales[i];
                        v.position += new Vector3(x, y, 0f);
                        v.color = colorContorno;
                        _vertsResultado.Add(v);
                    }
                }
            }

            // 3. Texto frontal encima con sus colores originales de relleno (soporta tags de rich text <color=...>)
            _vertsResultado.AddRange(_vertsOriginales);

            vh.Clear();
            vh.AddUIVertexTriangleStream(_vertsResultado);
        }
    }
}
