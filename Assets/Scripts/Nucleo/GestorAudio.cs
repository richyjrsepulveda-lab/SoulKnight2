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
        RayoTP,
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
        GatoDano3,
        EraDeHielo,
        EraHieloHit,
        DashElectrico,
        RecogerOro,
        PasarNivel,
        SubirNivel,
        SubirMaestria,
        CargandoUltiRayo,
        LanzandoUltiRayo,
        ComerPescado,
        UltiExplosionFuego,
        UltiMaullidoFuego,
        UltiSiceoFuego,
        Lanzallamas,
        CachinCompra,
        GatoRonroneo,
        GatoMaullido1,
        GatoMaullido2
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
        private AudioSource _fuenteLoopSFX;
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
            DetenerTodosLosSFX();
            if (scene.name == Escenas.MenuPrincipal || scene.name == Escenas.Lobby)
            {
                ComandosAdmin.ResetearAjustesAdmin();
            }
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
            // Usar MusicaJefe1 para los combates contra jefes
            RegistrarMusica(TipoMusica.Jefe, "Audio/Musica/MusicaJefe1");

            // SFX
            RegistrarSFX(TipoSFX.Disparo, "Audio/SFX/Disparo");
            RegistrarSFX(TipoSFX.Laser, "Audio/SFX/Laser");
            RegistrarSFX(TipoSFX.Espada, "Audio/SFX/Espada");
            RegistrarSFX(TipoSFX.Dash, "Audio/SFX/Dash");
            RegistrarSFX(TipoSFX.Teleport, "Audio/SFX/RayoTP");
            RegistrarSFX(TipoSFX.RayoTP, "Audio/SFX/RayoTP");
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
            RegistrarSFX(TipoSFX.HieloDisparo, "Audio/SFX/HieloDisparo");
            RegistrarSFX(TipoSFX.ImpactoHielo, "Audio/SFX/ImpactoHielo");
            RegistrarSFX(TipoSFX.GatoDano1, "Audio/SFX/GatoDano1");
            RegistrarSFX(TipoSFX.GatoDano2, "Audio/SFX/GatoDano2");
            RegistrarSFX(TipoSFX.GatoDano3, "Audio/SFX/GatoDano3");
            RegistrarSFX(TipoSFX.EraDeHielo, "Audio/SFX/EraDeHielo");
            RegistrarSFX(TipoSFX.EraHieloHit, "Audio/SFX/EraHieloHit1");
            RegistrarSFX(TipoSFX.DashElectrico, "Audio/SFX/DashElectrico");
            RegistrarSFX(TipoSFX.RecogerOro, "Audio/SFX/RecogerOro");
            RegistrarSFX(TipoSFX.PasarNivel, "Audio/SFX/PasarNivel");
            RegistrarSFX(TipoSFX.SubirNivel, "Audio/SFX/SubirNivel");
            RegistrarSFX(TipoSFX.SubirMaestria, "Audio/SFX/SubirMaestria");
            RegistrarSFX(TipoSFX.CargandoUltiRayo, "Audio/SFX/CargandoUltiRayo");
            RegistrarSFX(TipoSFX.LanzandoUltiRayo, "Audio/SFX/LanzandoUltiRayo");
            RegistrarSFX(TipoSFX.ComerPescado, "Audio/SFX/ComerPescado");
            RegistrarSFX(TipoSFX.FuegoLlamas, "Audio/SFX/Lanzallamas");
            RegistrarSFX(TipoSFX.Lanzallamas, "Audio/SFX/Lanzallamas");
            RegistrarSFX(TipoSFX.UltiExplosionFuego, "Audio/SFX/UltiExplosionFuego");
            RegistrarSFX(TipoSFX.UltiMaullidoFuego, "Audio/SFX/UltiMaullidoFuego");
            RegistrarSFX(TipoSFX.UltiSiceoFuego, "Audio/SFX/UltiSiceoFuego");
            RegistrarSFX(TipoSFX.CachinCompra, "Audio/SFX/CachinCompra");
            RegistrarSFX(TipoSFX.GatoRonroneo, "Audio/SFX/GatoRonroneo");
            RegistrarSFX(TipoSFX.GatoMaullido1, "Audio/SFX/GatoMaullido1");
            RegistrarSFX(TipoSFX.GatoMaullido2, "Audio/SFX/GatoMaullido2");
        }

        private void RegistrarMusica(TipoMusica tipo, string rutaResources)
        {
            var clip = Resources.Load<AudioClip>(rutaResources);
            if (clip != null) _clipsMusica[tipo] = clip;
        }

        private void RegistrarSFX(TipoSFX tipo, string rutaResources, string rutaAlternativa = null)
        {
            var clip = Resources.Load<AudioClip>(rutaResources);
            if (clip == null && !string.IsNullOrEmpty(rutaAlternativa))
            {
                clip = Resources.Load<AudioClip>(rutaAlternativa);
            }
            if (clip != null) _clipsSFX[tipo] = clip;
        }

        public AudioClip ObtenerClipSFX(TipoSFX tipo)
        {
            if (_clipsSFX.TryGetValue(tipo, out var c)) return c;
            return null;
        }

        public static AudioClip GetClipSFX(TipoSFX tipo) => Instancia != null ? Instancia.ObtenerClipSFX(tipo) : null;

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
            if (_fuenteLoopSFX != null && _fuenteLoopSFX.isPlaying)
            {
                _fuenteLoopSFX.volume = VolumenMaster * VolumenSFX;
            }
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
            if (_fuenteLoopSFX != null && _fuenteLoopSFX.isPlaying)
            {
                _fuenteLoopSFX.volume = VolumenMaster * VolumenSFX;
            }
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

        public static void PlayUltiFuegoCompleta(float volumenMultiplicador = 1.0f)
        {
            PlaySFX(TipoSFX.UltiExplosionFuego, volumenMultiplicador * 1.15f, 0.02f);
            PlaySFX(TipoSFX.UltiMaullidoFuego, volumenMultiplicador * 1.05f, 0.02f);
            PlaySFX(TipoSFX.UltiSiceoFuego, volumenMultiplicador * 1.0f, 0.02f);
        }

        public static void PlayCachinCompra(float volumenMultiplicador = 1.0f)
        {
            PlaySFX(TipoSFX.CachinCompra, volumenMultiplicador, 0.03f);
        }

        public static void PlayRonroneoGato(float volumenMultiplicador = 0.5f)
        {
            PlaySFX(TipoSFX.GatoRonroneo, volumenMultiplicador, 0.02f);
        }

        public static void PlaySonidoGatoTienda(float volumenMultiplicador = 0.85f)
        {
            float r = UnityEngine.Random.value;
            if (r < 0.40f)
            {
                PlaySFX(TipoSFX.GatoRonroneo, volumenMultiplicador * 0.90f, 0.02f);
            }
            else if (r < 0.70f)
            {
                PlaySFX(TipoSFX.GatoMaullido1, volumenMultiplicador, 0.04f);
            }
            else
            {
                PlaySFX(TipoSFX.GatoMaullido2, volumenMultiplicador, 0.04f);
            }
        }

        public static void PlaySFX(TipoSFX tipo, float volumenMultiplicador = 1.0f, float pitchRandom = 0.06f)
        {
            Instancia.ReproducirSFXInterno(tipo, volumenMultiplicador, pitchRandom);
        }

        public static void PlaySFXLoop(TipoSFX tipo, float volumenMultiplicador = 1.0f)
        {
            Instancia.ReproducirSFXLoopInterno(tipo, volumenMultiplicador);
        }

        public static void DetenerSFXLoop()
        {
            Instancia.DetenerSFXLoopInterno();
        }

        public static void DetenerTodosLosSFX()
        {
            if (_instancia != null)
            {
                _instancia.DetenerTodosLosSFXInterno();
            }
        }

        private void DetenerTodosLosSFXInterno()
        {
            DetenerSFXLoopInterno();
            for (int i = 0; i < _poolSFX.Count; i++)
            {
                if (_poolSFX[i] != null && _poolSFX[i].isPlaying)
                {
                    _poolSFX[i].Stop();
                    _poolSFX[i].clip = null;
                }
            }
        }

        public static void PlayBGM(TipoMusica tipo, float tiempoFade = 0.6f)
        {
            Instancia.ReproducirMusicaInterno(tipo, tiempoFade);
        }

        private void ReproducirSFXLoopInterno(TipoSFX tipo, float volMult)
        {
            if (!_clipsSFX.TryGetValue(tipo, out AudioClip clip) || clip == null) return;
            if (_fuenteLoopSFX == null)
            {
                _fuenteLoopSFX = gameObject.AddComponent<AudioSource>();
                _fuenteLoopSFX.loop = true;
                _fuenteLoopSFX.playOnAwake = false;
                _fuenteLoopSFX.spatialBlend = 0f;
            }

            _fuenteLoopSFX.clip = clip;
            _fuenteLoopSFX.volume = VolumenMaster * VolumenSFX * volMult;
            _fuenteLoopSFX.pitch = 1.0f;
            _fuenteLoopSFX.Play();
        }

        private void DetenerSFXLoopInterno()
        {
            if (_fuenteLoopSFX != null && _fuenteLoopSFX.isPlaying)
            {
                _fuenteLoopSFX.Stop();
                _fuenteLoopSFX.clip = null;
            }
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
