using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoulKnight
{
    public enum TipoMusica
    {
        Ninguna,
        Sala,       // Menú principal y pantallas de crear/unirse a sala
        Lobby,      // Lobby de espera y selección de héroes
        Mazmorra,   // Recámaras normales de la run
        Jefe        // Jefe y oleadas pesadas
    }

    public enum TipoSFX
    {
        Disparo,
        Laser,
        Espada,
        Dash,
        Teleport,
        RayoElectrico,
        Explosion,
        Congelar,
        Zarpazo,
        ImpactoEnemigo,
        MuerteEnemigo,
        DanoJugador,
        MuerteJugador,
        PuertaCerrar,
        PuertaAbrir,
        Portal,
        SalaDespejada,
        Victoria,
        Mejora,
        ClickUI,
        RugidoJefe,
        Escudo,
        FuegoDisparo,
        FuegoLlamas,
        HieloDisparo,
        ImpactoHielo,
        GatoDano1,
        GatoDano2,
        GatoDano3
    }

    public class MarcadorAudioBoton : MonoBehaviour { }

    /// <summary>
    /// Gestor centralizado de audio con soporte para música de fondo (BGM) en bucle con fade,
    /// pool dinámico de canales de efectos de sonido (SFX) y ajuste de volúmenes persistentes.
    /// </summary>
    public class GestorAudio : MonoBehaviour
    {
        private static GestorAudio _instancia;
        public static GestorAudio Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = FindObjectOfType<GestorAudio>();
                    if (_instancia == null)
                    {
                        var go = new GameObject("GestorAudio");
                        _instancia = go.AddComponent<GestorAudio>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instancia;
            }
        }

        private AudioSource _fuenteMusicaPrincipal;
        private AudioSource _fuenteMusicaSecundaria;
        private readonly List<AudioSource> _poolSFX = new List<AudioSource>();
        private const int CANTIDAD_FUENTES_SFX = 16;

        private readonly Dictionary<TipoSFX, AudioClip> _clipsSFX = new Dictionary<TipoSFX, AudioClip>();
        private readonly Dictionary<TipoMusica, AudioClip> _clipsMusica = new Dictionary<TipoMusica, AudioClip>();

        public float VolumenMaster { get; private set; } = 1.0f;
        public float VolumenMusica { get; private set; } = 0.70f;
        public float VolumenSFX { get; private set; } = 0.85f;

        private TipoMusica _musicaActual = TipoMusica.Ninguna;
        private Coroutine _rutinaFadeMusica;

        void Awake()
        {
            if (_instancia != null && _instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            InicializarFuentes();
            CargarClipsAudio();
            CargarPreferenciasVolumen();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
        }

        private void AlCargarEscena(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(RutinaAsignarClicksBotones());
        }

        private IEnumerator RutinaAsignarClicksBotones()
        {
            yield return null; // Esperar un frame a que los canvases despierten
            VincularClicksEnTodosLosBotones();
        }

        public static void VincularClicksEnTodosLosBotones(GameObject raiz = null)
        {
            Button[] botones = raiz != null ? raiz.GetComponentsInChildren<Button>(true) : FindObjectsOfType<Button>(true);
            foreach (var btn in botones)
            {
                if (btn == null) continue;
                if (btn.GetComponent<MarcadorAudioBoton>() == null)
                {
                    btn.gameObject.AddComponent<MarcadorAudioBoton>();
                    btn.onClick.AddListener(() => PlaySFX(TipoSFX.ClickUI));
                }
            }
        }

        private void InicializarFuentes()
        {
            _fuenteMusicaPrincipal = gameObject.AddComponent<AudioSource>();
            _fuenteMusicaPrincipal.loop = true;
            _fuenteMusicaPrincipal.playOnAwake = false;
            _fuenteMusicaPrincipal.spatialBlend = 0f; // 2D

            _fuenteMusicaSecundaria = gameObject.AddComponent<AudioSource>();
            _fuenteMusicaSecundaria.loop = true;
            _fuenteMusicaSecundaria.playOnAwake = false;
            _fuenteMusicaSecundaria.spatialBlend = 0f;

            for (int i = 0; i < CANTIDAD_FUENTES_SFX; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _poolSFX.Add(src);
            }
        }

        private void CargarClipsAudio()
        {
            // BGM
            RegistrarMusica(TipoMusica.Sala, "Audio/Musica/MusicaSala");
            RegistrarMusica(TipoMusica.Lobby, "Audio/Musica/MusicaLobby");
            RegistrarMusica(TipoMusica.Mazmorra, "Audio/Musica/MusicaDungeon");
            RegistrarMusica(TipoMusica.Jefe, "Audio/Musica/MusicaBoss");

            // SFX
            RegistrarSFX(TipoSFX.Disparo, "Audio/SFX/Disparo");
            RegistrarSFX(TipoSFX.Laser, "Audio/SFX/Laser");
            RegistrarSFX(TipoSFX.Espada, "Audio/SFX/Espada");
            RegistrarSFX(TipoSFX.Dash, "Audio/SFX/Dash");
            RegistrarSFX(TipoSFX.Teleport, "Audio/SFX/Teleport");
            RegistrarSFX(TipoSFX.RayoElectrico, "Audio/SFX/RayoElectrico");
            RegistrarSFX(TipoSFX.Explosion, "Audio/SFX/Explosion");
            RegistrarSFX(TipoSFX.Congelar, "Audio/SFX/Congelar");
            RegistrarSFX(TipoSFX.Zarpazo, "Audio/SFX/Zarpazo");
            RegistrarSFX(TipoSFX.ImpactoEnemigo, "Audio/SFX/ImpactoEnemigo");
            RegistrarSFX(TipoSFX.MuerteEnemigo, "Audio/SFX/MuerteEnemigo");
            RegistrarSFX(TipoSFX.DanoJugador, "Audio/SFX/DanoJugador");
            RegistrarSFX(TipoSFX.MuerteJugador, "Audio/SFX/MuerteJugador");
            RegistrarSFX(TipoSFX.PuertaCerrar, "Audio/SFX/PuertaCerrar");
            RegistrarSFX(TipoSFX.PuertaAbrir, "Audio/SFX/PuertaAbrir");
            RegistrarSFX(TipoSFX.Portal, "Audio/SFX/Portal");
            RegistrarSFX(TipoSFX.SalaDespejada, "Audio/SFX/SalaDespejada");
            RegistrarSFX(TipoSFX.Victoria, "Audio/SFX/Victoria");
            RegistrarSFX(TipoSFX.Mejora, "Audio/SFX/Mejora");
            RegistrarSFX(TipoSFX.ClickUI, "Audio/SFX/ClickUI");
            RegistrarSFX(TipoSFX.RugidoJefe, "Audio/SFX/RugidoJefe");
            RegistrarSFX(TipoSFX.Escudo, "Audio/SFX/Escudo");
            RegistrarSFX(TipoSFX.FuegoDisparo, "Audio/SFX/FuegoDisparo");
            RegistrarSFX(TipoSFX.FuegoLlamas, "Audio/SFX/FuegoLlamas");
            RegistrarSFX(TipoSFX.HieloDisparo, "Audio/SFX/HieloDisparo");
            RegistrarSFX(TipoSFX.ImpactoHielo, "Audio/SFX/ImpactoHielo");
            RegistrarSFX(TipoSFX.GatoDano1, "Audio/SFX/GatoDano1");
            RegistrarSFX(TipoSFX.GatoDano2, "Audio/SFX/GatoDano2");
            RegistrarSFX(TipoSFX.GatoDano3, "Audio/SFX/GatoDano3");
        }

        private void RegistrarMusica(TipoMusica tipo, string rutaResources)
        {
            var clip = Resources.Load<AudioClip>(rutaResources);
            if (clip != null) _clipsMusica[tipo] = clip;
        }

        private void RegistrarSFX(TipoSFX tipo, string rutaResources)
        {
            var clip = Resources.Load<AudioClip>(rutaResources);
            if (clip != null) _clipsSFX[tipo] = clip;
        }

        private void CargarPreferenciasVolumen()
        {
            VolumenMaster = PlayerPrefs.GetFloat("VolumenMaster", 1.0f);
            VolumenMusica = PlayerPrefs.GetFloat("VolumenMusica", 0.70f);
            VolumenSFX = PlayerPrefs.GetFloat("VolumenSFX", 0.85f);
        }

        public void SetVolumenMaster(float v)
        {
            VolumenMaster = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("VolumenMaster", VolumenMaster);
            PlayerPrefs.Save();
            ActualizarVolumenMusica();
        }

        public void SetVolumenMusica(float v)
        {
            VolumenMusica = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("VolumenMusica", VolumenMusica);
            PlayerPrefs.Save();
            ActualizarVolumenMusica();
        }

        public void SetVolumenSFX(float v)
        {
            VolumenSFX = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("VolumenSFX", VolumenSFX);
            PlayerPrefs.Save();
        }

        public void AjustarVolumen(float master, float musica, float sfx)
        {
            VolumenMaster = Mathf.Clamp01(master);
            VolumenMusica = Mathf.Clamp01(musica);
            VolumenSFX = Mathf.Clamp01(sfx);

            PlayerPrefs.SetFloat("VolumenMaster", VolumenMaster);
            PlayerPrefs.SetFloat("VolumenMusica", VolumenMusica);
            PlayerPrefs.SetFloat("VolumenSFX", VolumenSFX);
            PlayerPrefs.Save();

            ActualizarVolumenMusica();
        }

        private void ActualizarVolumenMusica()
        {
            if (_fuenteMusicaPrincipal != null && _rutinaFadeMusica == null)
            {
                _fuenteMusicaPrincipal.volume = VolumenMaster * VolumenMusica;
            }
        }

        private static int _indiceDanoGato = 0;

        public static void PlayDanoGatoRotativo(float volumenMultiplicador = 1.0f)
        {
            _indiceDanoGato = (_indiceDanoGato % 3) + 1;
            TipoSFX sfx = _indiceDanoGato switch
            {
                1 => TipoSFX.GatoDano1,
                2 => TipoSFX.GatoDano2,
                _ => TipoSFX.GatoDano3
            };
            PlaySFX(sfx, volumenMultiplicador, 0.03f);
        }

        public static void PlaySFX(TipoSFX tipo, float volumenMultiplicador = 1.0f, float pitchRandom = 0.06f)
        {
            Instancia.ReproducirSFXInterno(tipo, volumenMultiplicador, pitchRandom);
        }

        public static void PlayBGM(TipoMusica tipo, float tiempoFade = 0.6f)
        {
            Instancia.ReproducirMusicaInterno(tipo, tiempoFade);
        }

        private void ReproducirSFXInterno(TipoSFX tipo, float volMult, float pitchRnd)
        {
            // Rotación automática si se invoca TipoSFX.DanoJugador
            if (tipo == TipoSFX.DanoJugador)
            {
                _indiceDanoGato = (_indiceDanoGato % 3) + 1;
                tipo = _indiceDanoGato switch
                {
                    1 => TipoSFX.GatoDano1,
                    2 => TipoSFX.GatoDano2,
                    _ => TipoSFX.GatoDano3
                };
            }

            // Realce de impacto para golpes contundentes a enemigos
            if (tipo == TipoSFX.ImpactoEnemigo)
            {
                volMult *= 1.35f;
            }
            else if (tipo == TipoSFX.ImpactoHielo)
            {
                volMult *= 1.25f;
            }

            if (!_clipsSFX.TryGetValue(tipo, out AudioClip clip) || clip == null) return;

            AudioSource fuenteDisponible = null;
            for (int i = 0; i < _poolSFX.Count; i++)
            {
                if (!_poolSFX[i].isPlaying)
                {
                    fuenteDisponible = _poolSFX[i];
                    break;
                }
            }

            if (fuenteDisponible == null)
            {
                fuenteDisponible = _poolSFX[0];
            }

            fuenteDisponible.clip = clip;
            fuenteDisponible.volume = VolumenMaster * VolumenSFX * volMult;
            fuenteDisponible.pitch = 1.0f + UnityEngine.Random.Range(-pitchRnd, pitchRnd);
            fuenteDisponible.Play();
        }

        private void ReproducirMusicaInterno(TipoMusica tipo, float tiempoFade)
        {
            if (_musicaActual == tipo) return;
            _musicaActual = tipo;

            if (_rutinaFadeMusica != null)
            {
                StopCoroutine(_rutinaFadeMusica);
            }

            AudioClip nuevoClip = null;
            if (tipo != TipoMusica.Ninguna && _clipsMusica.TryGetValue(tipo, out AudioClip c))
            {
                nuevoClip = c;
            }

            _rutinaFadeMusica = StartCoroutine(RutinaTransicionMusica(nuevoClip, tiempoFade));
        }

        private IEnumerator RutinaTransicionMusica(AudioClip nuevoClip, float tiempoFade)
        {
            float volDestino = VolumenMaster * VolumenMusica;
            float inicioVol = _fuenteMusicaPrincipal.volume;

            // Fade Out
            if (_fuenteMusicaPrincipal.isPlaying && tiempoFade > 0f)
            {
                float t = 0f;
                while (t < tiempoFade)
                {
                    t += Time.deltaTime;
                    _fuenteMusicaPrincipal.volume = Mathf.Lerp(inicioVol, 0f, t / tiempoFade);
                    yield return null;
                }
            }

            _fuenteMusicaPrincipal.Stop();

            // Fade In de la nueva música
            if (nuevoClip != null)
            {
                _fuenteMusicaPrincipal.clip = nuevoClip;
                _fuenteMusicaPrincipal.volume = 0f;
                _fuenteMusicaPrincipal.Play();

                float t = 0f;
                while (t < tiempoFade)
                {
                    t += Time.deltaTime;
                    _fuenteMusicaPrincipal.volume = Mathf.Lerp(0f, volDestino, t / tiempoFade);
                    yield return null;
                }
                _fuenteMusicaPrincipal.volume = volDestino;
            }

            _rutinaFadeMusica = null;
        }
    }
}
