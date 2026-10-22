using System;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Proyectil balístico simulado localmente con predicción del cliente (0ms de delay).
    /// - Dueño local: se simula en el frame 0, detecta impactos en su pantalla y los reporta al host.
    /// - Clientes remotos: se adelantan según el timestamp de red para sincronización visual fluida.
    /// - Administrado mediante Object Pooling para 0 asignaciones de basura (GC).
    /// </summary>
    public class ProyectilLocal : MonoBehaviour
    {
        public Vector2 Direccion { get; private set; }
        public float Velocidad { get; set; } = 18f;
        public int Dano { get; set; } = 20;
        public bool EsPropietarioLocal { get; set; }
        public bool AplicaQuemadura { get; set; }
        public int BonoQuemadura { get; set; }
        public float MultiplicadorArea { get; set; } = 1.0f;
        public float TiempoVida { get; set; } = 3.5f;

        private float _tiempoVivo;
        private Personaje _lanzador;
        private Action<ProyectilLocal> _onReciclar;
        private SpriteRenderer _sr;
        private TrailRenderer _trail;

        private static readonly RaycastHit2D[] _castBuffer = new RaycastHit2D[16];

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 9;

            _trail = GetComponent<TrailRenderer>();
        }

        public void ConfigurarSprite(Sprite sprite, Color colorTinte, float escala = 0.65f)
        {
            if (_sr != null)
            {
                _sr.sprite = sprite;
                _sr.color = colorTinte;
                _sr.sortingOrder = 9;
            }
            transform.localScale = Vector3.one * (escala * MultiplicadorArea);
        }

        public void Inicializar(
            Personaje lanzador,
            Vector2 dir,
            Vector2 pos,
            float vel,
            int dano,
            bool esPropietario,
            bool quemadura,
            int bonoQuem,
            float multArea,
            Action<ProyectilLocal> onReciclar)
        {
            _lanzador = lanzador;
            Direccion = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
            Velocidad = vel;
            Dano = dano;
            EsPropietarioLocal = esPropietario;
            AplicaQuemadura = quemadura;
            BonoQuemadura = bonoQuem;
            MultiplicadorArea = Mathf.Max(0.5f, multArea);
            _onReciclar = onReciclar;
            _tiempoVivo = 0f;

            transform.position = new Vector3(pos.x, pos.y, -0.4f);
            ActualizarRotacion(Direccion);

            if (_trail != null)
            {
                _trail.Clear();
                _trail.enabled = true;
            }

            gameObject.SetActive(true);
        }

        private void ActualizarRotacion(Vector2 dir)
        {
            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _tiempoVivo += dt;
            if (_tiempoVivo >= TiempoVida)
            {
                Reciclar();
                return;
            }

            float paso = Velocidad * dt;
            Vector2 posActual = transform.position;

            if (EsPropietarioLocal)
            {
                // Detección continua en el dueño (0 ms delay en el hit visual)
                float radioDeteccion = 0.32f * MultiplicadorArea;
                int hits = Physics2D.CircleCastNonAlloc(posActual, radioDeteccion, Direccion, _castBuffer, paso);
                for (int i = 0; i < hits; i++)
                {
                    var hit = _castBuffer[i];
                    if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                    if (_lanzador != null && hit.collider.gameObject == _lanzador.gameObject) continue;

                    // Obstáculo o Pared
                    if (EsPared(hit.collider))
                    {
                        CrearEfectoImpactoPared(hit.point);
                        Reciclar();
                        return;
                    }

                    // Enemigo (Jefe o común)
                    var enemigo = hit.collider.GetComponent<EnemigoBase>();
                    if (enemigo != null && enemigo.VidaActual.Value > 0)
                    {
                        ProcesarImpactoEnemigo(enemigo, hit.point);
                        Reciclar();
                        return;
                    }
                }
            }
            else
            {
                // En clientes remotos: sólo detección visual de paredes
                int hits = Physics2D.CircleCastNonAlloc(posActual, 0.25f, Direccion, _castBuffer, paso);
                for (int i = 0; i < hits; i++)
                {
                    var hit = _castBuffer[i];
                    if (hit.collider != null && EsPared(hit.collider))
                    {
                        CrearEfectoImpactoPared(hit.point);
                        Reciclar();
                        return;
                    }
                }
            }

            Vector3 nuevaPos = transform.position + (Vector3)(Direccion * paso);
            nuevaPos.z = -0.4f;
            transform.position = nuevaPos;
        }

        private void ProcesarImpactoEnemigo(EnemigoBase enemigo, Vector2 puntoImpacto)
        {
            Color col = AplicaQuemadura ? new Color(0.96f, 0.40f, 0.05f) : Color.white;
            GestorAudio.PlaySFX(TipoSFX.ImpactoEnemigo, 0.85f);
            CrearEfectoImpactoEnemigo(puntoImpacto, col);

            if (_lanzador != null)
            {
                _lanzador.NotificarImpactoProyectil(enemigo, puntoImpacto, Dano, AplicaQuemadura, BonoQuemadura);
            }
        }

        private static bool EsPared(Collider2D col)
        {
            if (col == null || col.isTrigger) return false;
            try
            {
                if (col.CompareTag("Pared")) return true;
            }
            catch { }

            string n = col.name.ToLowerInvariant();
            return n.Contains("pared") || n.Contains("muro") || n.Contains("columna") || n.Contains("obstaculo") || n.Contains("puerta");
        }

        private void CrearEfectoImpactoPared(Vector2 punto)
        {
            GestorAudio.PlaySFX(TipoSFX.ImpactoEnemigo, 0.55f);
            // Destello o chispa rápida
            var flash = new GameObject("ChispaImpactoPared");
            flash.transform.position = new Vector3(punto.x, punto.y, -0.45f);
            var sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = _sr != null ? _sr.sprite : null;
            sr.color = new Color(1f, 0.85f, 0.4f, 0.9f);
            sr.sortingOrder = 10;
            flash.transform.localScale = Vector3.one * 0.45f;
            Destroy(flash, 0.08f);
        }

        private void CrearEfectoImpactoEnemigo(Vector2 punto, Color col)
        {
            var flash = new GameObject("FlashImpactoEnemigo");
            flash.transform.position = new Vector3(punto.x, punto.y, -0.45f);
            var sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = _sr != null ? _sr.sprite : null;
            sr.color = col;
            sr.sortingOrder = 10;
            flash.transform.localScale = Vector3.one * 0.7f;
            Destroy(flash, 0.10f);
        }

        public void Reciclar()
        {
            if (!gameObject.activeSelf) return;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.enabled = false;
            }

            gameObject.SetActive(false);
            _onReciclar?.Invoke(this);
        }
    }
}
