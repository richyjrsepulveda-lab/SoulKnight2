using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Representa una sala física de la mazmorra:
    /// - Detecta cuando el primer jugador entra en la sala para activar el combate.
    /// - Bloquea las puertas/entradas con muros físicos mientras el encuentro está activo.
    /// - Enciende las antorchas de la sala al entrar, otorgando visión permanente en esa sala.
    /// </summary>
    public class ControladorSala : MonoBehaviour
    {
        public int indiceSala;
        public bool esSalaInicio;
        public bool esSalaJefe;
        public bool esSalaTienda;
        public bool esSalaCofreTesoro;
        public TipoBioma biomaSala = TipoBioma.Piedra;
        public DatosBioma datosBioma;

        public Rect limitesSala;       // xMin, yMin, width, height
        public Rect limitesInteriores; // un poco más adentro para que el jugador ya haya cruzado el umbral

        public bool descubierta = false;
        public bool enCombate = false;
        public bool despejada = false;

        public List<Vector2> puntosSpawnOleada1 = new List<Vector2>();
        public List<Vector2> puntosSpawnOleada2 = new List<Vector2>();
        public List<Vector2> puntosSpawnOleada3 = new List<Vector2>();
        public List<Vector2> posicionesAntorchas = new List<Vector2>();
        public List<Vector2> posicionesPortales = new List<Vector2>();

        private readonly List<GameObject> _puertasBloqueo = new List<GameObject>();
        private readonly List<SpriteRenderer> _lucesAntorchas = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _llamasAntorchas = new List<SpriteRenderer>();
        private readonly List<Transform> _portalesVisuales = new List<Transform>();
        private GameObject _muroSalidaJefe;
        private bool _portalesActivos = false;
        private float _escalaPortales = 0f;

        public void Inicializar(int idx, int xMin, int xMax, int yMin, int yMax, bool inicio, bool jefe, bool tienda = false, bool cofreTesoro = false, TipoBioma bioma = TipoBioma.Piedra)
        {
            indiceSala = idx;
            esSalaInicio = inicio;
            esSalaJefe = jefe;
            esSalaTienda = tienda;
            esSalaCofreTesoro = cofreTesoro;
            biomaSala = bioma;
            datosBioma = CatalogoBiomas.Obtener(bioma);

            float w = xMax - xMin + 1;
            float h = yMax - yMin + 1;
            limitesSala = new Rect(xMin - 0.5f, yMin - 0.5f, w, h);

            // Margen interior de 2.2 unidades para asegurar que el jugador ya entró a la sala antes de cerrar puertas
            limitesInteriores = new Rect(xMin + 1.8f, yMin + 1.8f, Mathf.Max(2f, w - 3.6f), Mathf.Max(2f, h - 3.6f));

            if (esSalaInicio)
            {
                descubierta = true;
                despejada = true;
                EncenderAntorchas(true);
            }
            else if (esSalaTienda || esSalaCofreTesoro)
            {
                descubierta = false;
                despejada = true; // Despejada para que no haya combate ni puertas cerradas
            }
        }

        public void RegistrarPuertaBloqueo(GameObject puerta)
        {
            if (puerta != null)
            {
                puerta.SetActive(false);
                _puertasBloqueo.Add(puerta);
            }
        }

        public void RegistrarPortalSpawn(Vector2 pos, GameObject portalObj)
        {
            posicionesPortales.Add(pos);
            if (portalObj != null)
            {
                portalObj.SetActive(false);
                _portalesVisuales.Add(portalObj.transform);
            }
        }

        public void MostrarPortalesSpawn(bool mostrar)
        {
            _portalesActivos = mostrar;
            if (mostrar)
            {
                _escalaPortales = 0.1f;
            }
            foreach (var p in _portalesVisuales)
            {
                if (p != null)
                {
                    p.gameObject.SetActive(mostrar);
                    if (mostrar) p.localScale = Vector3.one * 0.1f;
                }
            }
        }

        public void AsignarMuroSalidaJefe(GameObject muroSalida)
        {
            _muroSalidaJefe = muroSalida;
            if (_muroSalidaJefe != null)
            {
                _muroSalidaJefe.SetActive(true); // Cerrado hasta derrotar al jefe
            }
        }

        public void AbrirMuroSalidaJefe()
        {
            if (_muroSalidaJefe != null)
            {
                _muroSalidaJefe.SetActive(false);
            }
        }

        public void RegistrarAntorcha(Vector2 pos, SpriteRenderer llama, SpriteRenderer haloLuz)
        {
            posicionesAntorchas.Add(pos);
            if (llama != null) _llamasAntorchas.Add(llama);
            if (haloLuz != null) _lucesAntorchas.Add(haloLuz);

            bool encendida = descubierta;
            if (llama != null) llama.gameObject.SetActive(encendida);
            if (haloLuz != null) haloLuz.gameObject.SetActive(encendida);
        }

        private float _progresoEncendido = 0f;
        public float ProgresoEncendido => _progresoEncendido;

        public void EncenderAntorchas(bool encender)
        {
            descubierta = encender;
            _progresoEncendido = encender ? 1f : 0f;

            foreach (var l in _llamasAntorchas)
            {
                if (l != null) l.gameObject.SetActive(encender);
            }
            foreach (var h in _lucesAntorchas)
            {
                if (h != null) h.gameObject.SetActive(encender);
            }
        }

        public void BloquearSala(bool bloquear)
        {
            enCombate = bloquear;
            foreach (var p in _puertasBloqueo)
            {
                if (p != null) p.SetActive(bloquear);
            }
            if (!bloquear)
            {
                MostrarPortalesSpawn(false);
            }
        }

        void Update()
        {
            // Antorchas con escala 1:1 fija sin bombeo; animadas pixel a pixel a 8 fps
            if (descubierta && _progresoEncendido < 1f)
            {
                _progresoEncendido = Mathf.MoveTowards(_progresoEncendido, 1f, Time.deltaTime * 2.8f);
            }

            // Animación de vórtice giratorio de los portales de invocación mientras están activos
            if (_portalesActivos)
            {
                _escalaPortales = Mathf.MoveTowards(_escalaPortales, 1.45f, Time.deltaTime * 3.8f);
                for (int i = 0; i < _portalesVisuales.Count; i++)
                {
                    var p = _portalesVisuales[i];
                    if (p != null && p.gameObject.activeSelf)
                    {
                        float pulso = _escalaPortales * (1f + Mathf.Sin(Time.time * 7f + i * 1.5f) * 0.08f);
                        p.localScale = new Vector3(pulso, pulso, 1f);
                        p.rotation = Quaternion.Euler(0f, 0f, -Time.time * 140f + i * 90f);
                    }
                }
            }

            // Encender e iluminar por completo la sala al entrar cualquier jugador
            if (!descubierta)
            {
                for (int i = 0; i < Personaje.Todos.Count; i++)
                {
                    var pj = Personaje.Todos[i];
                    if (pj != null && limitesSala.Contains(pj.transform.position))
                    {
                        EncenderAntorchas(true);
                        break;
                    }
                }
            }

            // En el servidor: detectar si algún jugador vivo entró a una sala aún no despejada ni en combate
            if (!esSalaInicio && !esSalaTienda && !esSalaCofreTesoro && !despejada && !enCombate)
            {
                if (ComandosAdmin.ModoSinEnemigosEnSalas)
                {
                    despejada = true;
                    EncenderAntorchas(true);
                    BloquearSala(false);
                    AbrirMuroSalidaJefe();
                    return;
                }

                if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
                {
                    if (GestorPartida.Instancia != null && !GestorPartida.Instancia.PartidaTerminada)
                    {
                        var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
                        foreach (var pj in vivos)
                        {
                            if (pj != null && limitesInteriores.Contains(pj.transform.position))
                            {
                                GestorPartida.Instancia.ActivarEncuentroSalaEnServidor(indiceSala, pj);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
