using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Pasaje / Hoyito en la pared para avanzar al siguiente nivel:
    /// - Se ubica en la pared de la sala final de la mazmorra.
    /// - Permanece bloqueado/cerrado hasta que todos los enemigos son derrotados.
    /// - Al despejar la sala, se abre e ilumina con partículas y brillo místico.
    /// - Requiere que TODOS los jugadores vivos entren al hoyito para iniciar la transición.
    /// </summary>
    public class PasajeSiguienteNivel : MonoBehaviour
    {
        public static PasajeSiguienteNivel Instancia { get; private set; }

        [Header("Sprites")]
        public SpriteRenderer sr;
        public Sprite spriteCerrado;
        public Sprite spriteAbierto;

        [Header("Configuración")]
        public bool estaAbierto = false;

        private readonly HashSet<Personaje> _jugadoresDentro = new HashSet<Personaje>();
        private Vector3 _escalaBase = Vector3.one * 1.6f;
        private float _tiempoFeedback = 0f;
        private TextMesh _indicadorTexto;

        private static Sprite _spriteProceduralCerrado;
        private static Sprite _spriteProceduralAbierto;

        void Awake()
        {
            Instancia = this;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            AsegurarSprites();
            CrearIndicadorTexto();
        }

        void Start()
        {
            ActualizarVisual();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Update()
        {
            if (_tiempoFeedback > 0f) _tiempoFeedback -= Time.deltaTime;

            if (estaAbierto && sr != null)
            {
                float pulso = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                transform.localScale = _escalaBase * pulso;
                if (_indicadorTexto != null)
                {
                    _indicadorTexto.gameObject.SetActive(true);
                }
            }
            else
            {
                if (_indicadorTexto != null)
                {
                    _indicadorTexto.gameObject.SetActive(false);
                }
            }
        }

        public void Abrir()
        {
            estaAbierto = true;
            ActualizarVisual();
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.5f, 0), "¡PASADIZO SECRETO ABIERTO!", new Color(1f, 0.85f, 0.4f));
        }

        public void Cerrar()
        {
            estaAbierto = false;
            _jugadoresDentro.Clear();
            ActualizarVisual();
        }

        private void AsegurarSprites()
        {
            if (spriteCerrado == null || spriteAbierto == null)
            {
                if (_spriteProceduralCerrado == null || _spriteProceduralAbierto == null)
                {
                    const int sz = 32;
                    Texture2D texC = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                    texC.filterMode = FilterMode.Point;
                    for (int y = 0; y < sz; y++)
                    {
                        for (int x = 0; x < sz; x++)
                        {
                            bool marco = (x == 4 || x == 27 || y == 28 || y == 2);
                            bool reja = (x == 10 || x == 16 || x == 21) && y <= 26 && y >= 2;
                            bool interior = (x > 4 && x < 27 && y > 2 && y < 28);
                            if (marco) texC.SetPixel(x, y, new Color(0.45f, 0.45f, 0.50f, 1f));
                            else if (reja) texC.SetPixel(x, y, new Color(0.30f, 0.30f, 0.35f, 1f));
                            else if (interior) texC.SetPixel(x, y, new Color(0.10f, 0.10f, 0.14f, 1f));
                            else texC.SetPixel(x, y, Color.clear);
                        }
                    }
                    texC.Apply();
                    _spriteProceduralCerrado = Sprite.Create(texC, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 16f);

                    Texture2D texA = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                    texA.filterMode = FilterMode.Point;
                    for (int y = 0; y < sz; y++)
                    {
                        for (int x = 0; x < sz; x++)
                        {
                            bool marco = (x == 4 || x == 27 || y == 28 || y == 2);
                            bool interior = (x > 4 && x < 27 && y > 2 && y < 28);
                            float distC = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 14f));
                            if (marco) texA.SetPixel(x, y, new Color(0.95f, 0.82f, 0.35f, 1f));
                            else if (interior)
                            {
                                float glow = Mathf.Clamp01(1f - distC / 12f);
                                Color cVortice = Color.Lerp(new Color(0.15f, 0.35f, 0.65f, 1f), new Color(0.55f, 0.95f, 1f, 1f), glow);
                                texA.SetPixel(x, y, cVortice);
                            }
                            else texA.SetPixel(x, y, Color.clear);
                        }
                    }
                    texA.Apply();
                    _spriteProceduralAbierto = Sprite.Create(texA, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 16f);
                }

                if (spriteCerrado == null) spriteCerrado = _spriteProceduralCerrado;
                if (spriteAbierto == null) spriteAbierto = _spriteProceduralAbierto;
            }
        }

        private void CrearIndicadorTexto()
        {
            var txtObj = new GameObject("TextoPasajeIndicador");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            _indicadorTexto = txtObj.AddComponent<TextMesh>();
            _indicadorTexto.text = "🌀 PASADIZO AL SIGUIENTE NIVEL";
            _indicadorTexto.fontSize = 22;
            _indicadorTexto.characterSize = 0.08f;
            _indicadorTexto.alignment = TextAlignment.Center;
            _indicadorTexto.anchor = TextAnchor.MiddleCenter;
            _indicadorTexto.color = new Color(0.4f, 0.95f, 1f);
            var mr = txtObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;
            txtObj.SetActive(false);
        }

        private void ActualizarVisual()
        {
            AsegurarSprites();
            if (sr == null) return;
            if (estaAbierto)
            {
                if (spriteAbierto != null) sr.sprite = spriteAbierto;
                sr.color = Color.white;
            }
            else
            {
                if (spriteCerrado != null) sr.sprite = spriteCerrado;
                sr.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers == null || pers.EstaMuerto.Value) return;

            if (!estaAbierto)
            {
                if (_tiempoFeedback <= 0f)
                {
                    _tiempoFeedback = 2f;
                    TextoFlotante.Crear(transform.position + new Vector3(0, 1.2f, 0), "¡Sellado! Eliminen a todos los enemigos.", Color.red);
                }
                return;
            }

            _jugadoresDentro.Add(pers);
            VerificarJugadores();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers != null)
            {
                _jugadoresDentro.Remove(pers);
                ActualizarAvisoHUD();
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!estaAbierto) return;
            var pers = collision.GetComponent<Personaje>();
            if (pers != null && !pers.EstaMuerto.Value && !_jugadoresDentro.Contains(pers))
            {
                _jugadoresDentro.Add(pers);
                VerificarJugadores();
            }
        }

        public void VerificarJugadores()
        {
            if (!estaAbierto || GestorPartida.Instancia == null) return;

            // Limpiar referencias muertas o nulas
            _jugadoresDentro.RemoveWhere(p => p == null || p.EstaMuerto.Value);

            var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
            int totalVivos = vivos.Count;
            int dentro = _jugadoresDentro.Count;

            ActualizarAvisoHUD();

            // Si todos los jugadores vivos están dentro del pasaje
            if (totalVivos > 0 && dentro >= totalVivos)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    GestorPartida.Instancia.IniciarTransicionSiguienteNivel();
                }
            }
        }

        private void ActualizarAvisoHUD()
        {
            if (GestorPartida.Instancia == null) return;
            var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
            int totalVivos = vivos.Count;
            int dentro = _jugadoresDentro.Count;

            if (HUDJugador.Instancia != null && estaAbierto)
            {
                if (dentro < totalVivos)
                {
                    HUDJugador.Instancia.MostrarAvisoPasaje($"En el pasaje: {dentro}/{totalVivos}. ¡Entren todos para avanzar!");
                }
                else
                {
                    HUDJugador.Instancia.MostrarAvisoPasaje("¡Todos dentro! Avanzando al siguiente nivel...");
                }
            }
        }
    }
}
