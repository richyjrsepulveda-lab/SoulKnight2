using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Obstáculo sólido que inflige daño al contacto (zarzas espinosas de planta o cactus del desierto).
    /// </summary>
    public class PropDanoContacto : MonoBehaviour
    {
        public int danoContacto = 3;
        public float intervaloDano = 0.6f;
        private float _timer;

        void OnCollisionStay2D(Collision2D collision)
        {
            _timer += Time.deltaTime;
            if (_timer >= intervaloDano)
            {
                _timer = 0f;
                var pj = collision.gameObject.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(danoContacto);
                    return;
                }

                var en = collision.gameObject.GetComponent<EnemigoBase>();
                if (en != null && en.VidaActual != null && en.VidaActual.Value > 0)
                {
                    en.RecibirDano(danoContacto, new Color(0.8f, 0.3f, 0.2f));
                }
            }
        }
    }
}
