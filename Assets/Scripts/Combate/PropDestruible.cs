using System.Collections;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoPropDestruible
    {
        Caja = 0,
        Barril = 1,
        BarrilExplosivo = 2,
        Jarron = 3
    }

    /// <summary>
    /// Obstáculo destruible en la mazmorra (caja, barril, barril rojo explosivo o jarrón).
    /// </summary>
    public class PropDestruible : MonoBehaviour
    {
        public TipoPropDestruible tipoProp = TipoPropDestruible.Caja;
        public int vidaMax = 15;
        public bool esExplosivo = false;
        public int danoExplosion = 25;
        public float radioExplosion = 2.4f;

        private int _vidaActual;
        private SpriteRenderer _sr;
        private Color _colorOriginal;
        private bool _destruido = false;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _colorOriginal = _sr.color;
            _vidaActual = vidaMax;
        }

        public void RecibirDano(int cantidad)
        {
            if (_destruido) return;

            _vidaActual -= cantidad;
            StartCoroutine(RutinaFlashDano());

            if (_vidaActual <= 0)
            {
                DestruirProp();
            }
        }

        private IEnumerator RutinaFlashDano()
        {
            if (_sr != null)
            {
                _sr.color = Color.white * 1.8f;
                yield return new WaitForSeconds(0.08f);
                if (_sr != null) _sr.color = _colorOriginal;
            }
        }

        private void DestruirProp()
        {
            if (_destruido) return;
            _destruido = true;

            if (esExplosivo)
            {
                DetonarBarril();
            }
            else
            {
                // Posibilidad de spawnear una moneda de oro al romperse (mayor en jarrones)
                float chance = (tipoProp == TipoPropDestruible.Jarron) ? 0.65f : 0.40f;
                if (Random.value < chance)
                {
                    MonedaOro.Crear(transform.position, Random.Range(1, 3));
                }
            }

            Destroy(gameObject);
        }

        private void DetonarBarril()
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, radioExplosion);
            foreach (var col in cols)
            {
                if (col.gameObject == gameObject) continue;

                var en = col.GetComponent<EnemigoBase>();
                if (en != null && en.VidaActual != null && en.VidaActual.Value > 0)
                {
                    en.RecibirDano(danoExplosion, new Color(1f, 0.4f, 0.1f));
                }

                var pj = col.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(10);
                }

                var otroProp = col.GetComponent<PropDestruible>();
                if (otroProp != null && !otroProp._destruido)
                {
                    otroProp.RecibirDano(danoExplosion);
                }
            }
        }
    }
}
