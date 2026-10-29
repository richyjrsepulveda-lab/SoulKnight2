using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoPeligroTile
    {
        Pozo = 0,      // stone_pit
        Veneno = 1,    // plant_poison
        AguaHelada = 2,// ice_icewater
        Lava = 3,      // fire_lava
        ArenasVivas = 4// desert_quicksand
    }

    /// <summary>
    /// Zona de peligro continua en el suelo de mazmorra (lava, charco de veneno, agua helada, arenas movedizas o fosa).
    /// </summary>
    public class ZonaPeligroTile : MonoBehaviour
    {
        public TipoPeligroTile tipoPeligro;
        public int danoTick = 3;
        public float intervaloTick = 0.6f;

        private float _timer;
        private readonly List<Collider2D> _personajesDentro = new List<Collider2D>();

        void Update()
        {
            if (_personajesDentro.Count == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= intervaloTick)
            {
                _timer = 0f;
                ProcesarEfectos();
            }
        }

        private void ProcesarEfectos()
        {
            for (int i = _personajesDentro.Count - 1; i >= 0; i--)
            {
                var col = _personajesDentro[i];
                if (col == null)
                {
                    _personajesDentro.RemoveAt(i);
                    continue;
                }

                var pj = col.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(danoTick);
                    switch (tipoPeligro)
                    {
                        case TipoPeligroTile.Lava:
                            pj.AplicarQuemadura(2.5f, 2);
                            break;
                        case TipoPeligroTile.Veneno:
                            pj.AplicarEsporasVeneno(2.5f, 2);
                            break;
                        case TipoPeligroTile.AguaHelada:
                            pj.AplicarRalentizacion(2.0f);
                            break;
                        case TipoPeligroTile.ArenasVivas:
                            pj.AplicarRalentizacion(2.0f);
                            break;
                    }
                    continue;
                }

                var en = col.GetComponent<EnemigoBase>();
                if (en != null && en.VidaActual != null && en.VidaActual.Value > 0)
                {
                    en.RecibirDano(danoTick, ObtenerColorImpacto());
                }
            }
        }

        private Color ObtenerColorImpacto()
        {
            switch (tipoPeligro)
            {
                case TipoPeligroTile.Lava: return new Color(1f, 0.45f, 0.1f);
                case TipoPeligroTile.Veneno: return new Color(0.2f, 0.9f, 0.3f);
                case TipoPeligroTile.AguaHelada: return new Color(0.4f, 0.8f, 1f);
                case TipoPeligroTile.ArenasVivas: return new Color(0.9f, 0.75f, 0.3f);
                default: return Color.red;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_personajesDentro.Contains(other))
            {
                _personajesDentro.Add(other);
                ProcesarEfectos();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            _personajesDentro.Remove(other);
        }

        public void Configurar(TipoPeligroTile tipo, int dano = 3)
        {
            tipoPeligro = tipo;
            danoTick = dano;
        }
    }
}
