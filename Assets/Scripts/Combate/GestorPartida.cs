using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Gestiona la sesión de juego en la escena de batalla con combates por salas:
    /// - Al inicio, el mapa no tiene enemigos sueltos.
    /// - Cuando un jugador entra a una sala no despejada:
    ///     1. La sala se bloquea (cierra puertas) y enciende sus antorchas.
    ///     2. Todos los jugadores vivos son teletransportados dentro de esa sala.
    ///     3. En salas normales aparecen 2 mini-oleadas de enemigos.
    ///     4. En la sala final cae el Jefe desde el cielo con su barra estilo Dark Souls.
    /// - Al matar a los enemigos de la sala, las puertas se abren.
    /// - Al matar al Jefe, se abre el muro hacia el pasillito final con el hoyo al siguiente nivel.
    /// </summary>
    public class GestorPartida : MonoBehaviour
    {
        public static GestorPartida Instancia { get; private set; }

        [Header("Prefabs de Personajes")]
        public GameObject prefabFuego;
        public GameObject prefabHielo;
        public GameObject prefabElectrico;

        [Header("Generador de Mazmorra Procedural")]
        public GeneradorMazmorra generadorMazmorra;

        [Header("Prefabs y Spawns")]
        public GameObject prefabEnemigo;
        public GameObject prefabEnemigoMago;
        public GameObject prefabEnemigoSlime;

        [Header("Prefabs de Jefes")]
        public GameObject prefabJefeSlimeRey;
        public GameObject prefabJefeGranBrujo;
        public GameObject prefabJefeGolemColosal;

        public Transform[] puntosSpawnJugadores;
        public Transform[] puntosSpawnEnemigos;

        public GameObject prefabItemMejoraRun;

        private readonly List<Personaje> _personajesActivos = new List<Personaje>();
        private readonly List<EnemigoBase> _enemigosActivos = new List<EnemigoBase>();

        public bool PartidaTerminada { get; private set; } = false;
        private bool _partidaIniciada = false;

        // Estado del encuentro de sala actual en el servidor
        private ControladorSala _salaActivaEnCombate = null;
        private int _oleadaActualSala = 0;
        private bool _spawneandoOleada = false;

        public int NivelActual { get; private set; } = 1;
        private bool _transicionEnProgreso = false;

        void Awake()
        {
            Instancia = this;
        }

        void Start()
        {
            SalaRed.PartidaTerminada += AlTerminarPartida;

            if (!_partidaIniciada && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && SalaRed.Instancia == null)
            {
                IniciarEnServidor();
            }
        }

        void OnDestroy()
        {
            SalaRed.PartidaTerminada -= AlTerminarPartida;
            if (Instancia == this) Instancia = null;
        }

        private void AlTerminarPartida(bool victoria)
        {
            DetenerPartida();
        }

        public void IniciarEnServidor()
        {
            if (_partidaIniciada || !NetworkManager.Singleton.IsServer) return;
            _partidaIniciada = true;

            int semilla = UnityEngine.Random.Range(1000, 999999);
            TipoBioma bioma = TipoBioma.Piedra;
            if (generadorMazmorra != null && generadorMazmorra.biomas != null && generadorMazmorra.biomas.Length > 0)
            {
                bioma = generadorMazmorra.biomas[UnityEngine.Random.Range(0, generadorMazmorra.biomas.Length)].tipo;
            }

            if (generadorMazmorra != null)
            {
                generadorMazmorra.GenerarConSemilla(semilla, bioma);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SemillaMapaActual.Value = semilla;
                SalaRed.Instancia.BiomaMapaActual.Value = (int)bioma;
                SalaRed.Instancia.SincronizarMapaClientRpc(semilla, bioma);
            }

            SpawnearJugadores();
            SpawnearTiendasEnSalas();
        }

        private void SpawnearJugadores()
        {
            if (SalaRed.Instancia == null) return;

            var clientesSpawneados = new HashSet<ulong>();
            int index = 0;
            for (int i = 0; i < SalaRed.Instancia.Jugadores.Count; i++)
            {
                var info = SalaRed.Instancia.Jugadores[i];
                if (info.personaje == TipoPersonaje.Ninguno) continue;
                if (!clientesSpawneados.Add(info.idCliente)) continue;

                GameObject prefab = null;
                switch (info.personaje)
                {
                    case TipoPersonaje.Fuego: prefab = prefabFuego; break;
                    case TipoPersonaje.Hielo: prefab = prefabHielo; break;
                    case TipoPersonaje.Electrico: prefab = prefabElectrico; break;
                    default: continue;
                }

                if (prefab == null) continue;

                Vector3 spawnPos = Vector3.zero;
                if (generadorMazmorra != null && generadorMazmorra.puntosSpawnJugadores.Count > 0)
                {
                    spawnPos = (Vector3)generadorMazmorra.puntosSpawnJugadores[index % generadorMazmorra.puntosSpawnJugadores.Count];
                }
                else if (puntosSpawnJugadores != null && puntosSpawnJugadores.Length > 0)
                {
                    spawnPos = puntosSpawnJugadores[index % puntosSpawnJugadores.Length].position;
                }

                GameObject jugadorObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                var pers = jugadorObj.GetComponent<Personaje>();
                var netObj = jugadorObj.GetComponent<NetworkObject>();
                netObj.SpawnWithOwnership(info.idCliente, destroyWithScene: true);

                if (pers != null)
                {
                    if (!info.nombre.IsEmpty)
                    {
                        pers.NombreJugadorSincronizado.Value = info.nombre;
                    }
                    _personajesActivos.Add(pers);
                }

                index++;
            }
        }

        /// <summary>
        /// Llamado en el servidor cuando el primer jugador vivo entra en una sala aún no despejada.
        /// </summary>
        public void ActivarEncuentroSalaEnServidor(int indiceSala, Personaje jugadorQueEntro)
        {
            if (!NetworkManager.Singleton.IsServer || PartidaTerminada || _transicionEnProgreso) return;
            if (_salaActivaEnCombate != null) return; // Ya hay una sala activa
            if (generadorMazmorra == null || indiceSala < 0 || indiceSala >= generadorMazmorra.salasGeneradas.Count) return;

            var sala = generadorMazmorra.salasGeneradas[indiceSala];
            if (sala == null || sala.despejada || sala.enCombate) return;

            _salaActivaEnCombate = sala;
            sala.EncenderAntorchas(true);
            sala.BloquearSala(true);

            // Sincronizar con todos los clientes (encender antorchas y cerrar puertas de la sala)
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SincronizarEstadoSalaClientRpc(indiceSala, descubierta: true, bloqueada: true, despejada: false);
            }

            // Teletransportar a TODOS los jugadores vivos a esta sala que se activó
            Vector2 puntoReunion = jugadorQueEntro != null
                ? (Vector2)jugadorQueEntro.transform.position
                : sala.limitesSala.center;

            // Asegurar que el punto de reunión esté bien adentro de la sala (lejos de las puertas cerradas)
            puntoReunion.x = Mathf.Clamp(puntoReunion.x, sala.limitesInteriores.xMin + 1f, sala.limitesInteriores.xMax - 1f);
            puntoReunion.y = Mathf.Clamp(puntoReunion.y, sala.limitesInteriores.yMin + 1f, sala.limitesInteriores.yMax - 1f);

            var vivos = ObtenerJugadoresVivos();
            int offsetIdx = 0;
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(0.8f, 0.8f),
                new Vector2(-0.8f, 0.8f),
                new Vector2(0.8f, -0.8f),
                new Vector2(-0.8f, -0.8f)
            };

            foreach (var pj in vivos)
            {
                if (pj == null) continue;
                // Si el jugador no es el que acaba de activar o está fuera de los límites interiores, teletransportarlo adentro
                if (pj != jugadorQueEntro || !sala.limitesInteriores.Contains(pj.transform.position))
                {
                    Vector2 destino = puntoReunion + offsets[offsetIdx % offsets.Length];
                    destino.x = Mathf.Clamp(destino.x, sala.limitesInteriores.xMin + 0.5f, sala.limitesInteriores.xMax - 0.5f);
                    destino.y = Mathf.Clamp(destino.y, sala.limitesInteriores.yMin + 0.5f, sala.limitesInteriores.yMax - 0.5f);
                    pj.TeletransportarClientRpc(destino);
                    offsetIdx++;
                }
            }

            if (sala.esSalaJefe)
            {
                // Sala del Jefe: Iniciar encuentro con el Jefe
                _oleadaActualSala = 1;
                StartCoroutine(RutinaSpawnearJefeEnSala(sala));
            }
            else
            {
                // Sala normal: Al entrar no hay portales; aparecen 1s después de cerrarse y enemigos 1s después del portal
                sala.MostrarPortalesSpawn(false);
                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.MostrarPortalesSalaClientRpc(indiceSala, false);
                    SalaRed.Instancia.EnviarAvisoTodos($"⚔️ ¡Sala bloqueada! Prepárense...");
                }
                _oleadaActualSala = 1;
                StartCoroutine(RutinaSpawnearOleada(sala, sala.puntosSpawnOleada1, esPrimeraOleada: true));
            }
        }

        private IEnumerator RutinaSpawnearOleada(ControladorSala sala, List<Vector2> puntosSpawn, bool esPrimeraOleada)
        {
            _spawneandoOleada = true;

            if (esPrimeraOleada)
            {
                // 1 segundo después de cerrarse la sala, aparecen los 2 o 4 portales
                yield return new WaitForSeconds(1.0f);
                if (PartidaTerminada || sala == null) { _spawneandoOleada = false; yield break; }

                sala.MostrarPortalesSpawn(true);
                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.MostrarPortalesSalaClientRpc(sala.indiceSala, true);
                    SalaRed.Instancia.EnviarAvisoTodos("🌀 ¡Portales abiertos! Oleada 1/2");
                }

                // 1 segundo después de aparecer el portal, salen los enemigos
                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                // Los portales siguen abiertos; 1 segundo después emerge la siguiente oleada
                yield return new WaitForSeconds(1.0f);
            }

            List<GameObject> catalogo = new List<GameObject>();
            if (prefabEnemigoSlime != null) catalogo.Add(prefabEnemigoSlime);
            if (prefabEnemigoMago != null) catalogo.Add(prefabEnemigoMago);
            if (prefabEnemigo != null) catalogo.Add(prefabEnemigo);

            if (catalogo.Count > 0 && puntosSpawn != null)
            {
                for (int i = 0; i < puntosSpawn.Count; i++)
                {
                    if (PartidaTerminada) break;

                    GameObject prefabSeleccionado = catalogo[(i + _oleadaActualSala) % catalogo.Count];
                    GameObject enemigoObj = Instantiate(prefabSeleccionado, (Vector3)puntosSpawn[i], Quaternion.identity);
                    var netObj = enemigoObj.GetComponent<NetworkObject>();
                    netObj.Spawn(destroyWithScene: true);

                    var enemigo = enemigoObj.GetComponent<EnemigoBase>();
                    if (enemigo != null)
                    {
                        _enemigosActivos.Add(enemigo);
                    }

                    yield return new WaitForSeconds(0.15f);
                }
            }

            _spawneandoOleada = false;
            RevisarFinDePartida();
        }

        private IEnumerator RutinaSpawnearJefeEnSala(ControladorSala salaJefe)
        {
            _spawneandoOleada = true;
            yield return new WaitForSeconds(0.35f);

            Vector2 posJefe = salaJefe.limitesSala.center;

            GameObject prefabJefeElegido = null;
            int jefeIndice = (NivelActual - 1) % 3;
            if (jefeIndice == 0 && prefabJefeSlimeRey != null) prefabJefeElegido = prefabJefeSlimeRey;
            else if (jefeIndice == 1 && prefabJefeGranBrujo != null) prefabJefeElegido = prefabJefeGranBrujo;
            else if (prefabJefeGolemColosal != null) prefabJefeElegido = prefabJefeGolemColosal;
            else if (prefabJefeSlimeRey != null) prefabJefeElegido = prefabJefeSlimeRey;

            if (prefabJefeElegido != null && !PartidaTerminada)
            {
                GameObject jefeObj = Instantiate(prefabJefeElegido, (Vector3)posJefe, Quaternion.identity);
                var noJefe = jefeObj.GetComponent<NetworkObject>();
                noJefe.Spawn(destroyWithScene: true);

                var jefe = jefeObj.GetComponent<JefeBase>();
                if (jefe != null)
                {
                    _enemigosActivos.Add(jefe);
                }
            }

            _spawneandoOleada = false;
            RevisarFinDePartida();
        }

        public void DetenerPartida()
        {
            PartidaTerminada = true;
            StopAllCoroutines();

            for (int i = _personajesActivos.Count - 1; i >= 0; i--)
            {
                var pj = _personajesActivos[i];
                if (pj != null)
                {
                    pj.LimpiarEstadosYHabilidades();
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && pj.IsSpawned)
                    {
                        pj.LimpiarEstadosYHabilidadesClientRpc();
                    }
                }
            }

            for (int i = _enemigosActivos.Count - 1; i >= 0; i--)
            {
                var enemigo = _enemigosActivos[i];
                if (enemigo != null)
                {
                    enemigo.Detener();
                }
            }

            var proyectiles = FindObjectsOfType<Proyectil>();
            foreach (var p in proyectiles)
            {
                if (p != null)
                {
                    Destroy(p.gameObject);
                }
            }

            var zonas = FindObjectsOfType<ZonaEraHielo>();
            foreach (var z in zonas)
            {
                if (z != null)
                {
                    Destroy(z.gameObject);
                }
            }
        }

        public void EnemigoEliminado(EnemigoBase enemigo)
        {
            _enemigosActivos.Remove(enemigo);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !PartidaTerminada)
            {
                foreach (var p in _personajesActivos)
                {
                    if (p != null && p.nivelProgreso != null)
                    {
                        p.nivelProgreso.AgregarXP(35);
                    }
                }

                if (prefabItemMejoraRun != null && UnityEngine.Random.value < 0.32f && enemigo != null)
                {
                    GameObject itemObj = Instantiate(prefabItemMejoraRun, enemigo.transform.position, Quaternion.identity);
                    var item = itemObj.GetComponent<ItemMejoraRun>();
                    var tipoElegido = CatalogoMejorasRun.ObtenerAleatoria();
                    if (item != null)
                    {
                        item.Configurar(tipoElegido);
                    }
                    var no = itemObj.GetComponent<NetworkObject>();
                    if (no != null) no.Spawn(destroyWithScene: true);
                    if (item != null)
                    {
                        item.Configurar(tipoElegido);
                    }
                }
            }

            RevisarFinDePartida();
        }

        public void RevisarFinDePartida()
        {
            if (!NetworkManager.Singleton.IsServer || SalaRed.Instancia == null || PartidaTerminada) return;

            // 1. Verificar si todos los jugadores murieron
            bool todosMuertos = true;
            foreach (var p in _personajesActivos)
            {
                if (p != null && !p.EstaMuerto.Value)
                {
                    todosMuertos = false;
                    break;
                }
            }

            if (todosMuertos && _personajesActivos.Count > 0)
            {
                DetenerPartida();
                SalaRed.Instancia.FinPartidaClientRpc(victoria: false);
                return;
            }

            // 2. Gestionar progreso de la sala activa
            if (_salaActivaEnCombate != null && !_spawneandoOleada && _enemigosActivos.Count == 0)
            {
                if (!_salaActivaEnCombate.esSalaJefe && _oleadaActualSala == 1)
                {
                    // Pasar a la segunda mini-oleada
                    _oleadaActualSala = 2;
                    if (SalaRed.Instancia != null)
                    {
                        SalaRed.Instancia.EnviarAvisoTodos("🌀 ¡Oleada 2/2 saliendo de los portales!");
                    }
                    StartCoroutine(RutinaSpawnearOleada(_salaActivaEnCombate, _salaActivaEnCombate.puntosSpawnOleada2, esPrimeraOleada: false));
                }
                else
                {
                    // ¡Sala despejada! Los portales desaparecen y las puertas se abren
                    var salaCompletada = _salaActivaEnCombate;
                    _salaActivaEnCombate = null;
                    _oleadaActualSala = 0;

                    salaCompletada.despejada = true;
                    salaCompletada.MostrarPortalesSpawn(false);
                    salaCompletada.BloquearSala(false);

                    if (salaCompletada.esSalaJefe)
                    {
                        // Se mató al jefe: abrir el pasillo final y activar el hoyo
                        salaCompletada.AbrirMuroSalidaJefe();
                        ActivarPasajeSiguienteNivel();
                    }
                    else
                    {
                        // Recompensas de mejoras permanentes y compras al despejar la sala
                        SpawnearRecompensaFinSala(salaCompletada);
                    }

                    if (SalaRed.Instancia != null)
                    {
                        SalaRed.Instancia.MostrarPortalesSalaClientRpc(salaCompletada.indiceSala, false);
                        SalaRed.Instancia.SincronizarEstadoSalaClientRpc(
                            salaCompletada.indiceSala,
                            descubierta: true,
                            bloqueada: false,
                            despejada: true
                        );

                        if (!salaCompletada.esSalaJefe)
                        {
                            SalaRed.Instancia.EnviarAvisoTodos("✅ ¡Sala despejada! Portales cerrados y puertas abiertas.");
                        }
                    }
                }
            }
        }

        public List<Personaje> ObtenerJugadoresVivos()
        {
            List<Personaje> vivos = new List<Personaje>();
            foreach (var p in _personajesActivos)
            {
                if (p != null && !p.EstaMuerto.Value && p.gameObject.activeInHierarchy)
                {
                    vivos.Add(p);
                }
            }
            return vivos;
        }

        public void ActivarPasajeSiguienteNivel()
        {
            if (PasajeSiguienteNivel.Instancia != null)
            {
                PasajeSiguienteNivel.Instancia.Abrir();
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.ActivarPasajeClientRpc();
                SalaRed.Instancia.EnviarAvisoTodos("🕳️ ¡Pasillo final abierto! Avancen al hoyo al final del pasillo.");
            }
        }

        public void IniciarTransicionSiguienteNivel()
        {
            if (!NetworkManager.Singleton.IsServer || _transicionEnProgreso || PartidaTerminada) return;
            StartCoroutine(RutinaTransicionNivel());
        }

        private IEnumerator RutinaTransicionNivel()
        {
            _transicionEnProgreso = true;

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.IniciarTransicionNivelClientRpc();
            }

            yield return new WaitForSeconds(0.9f);

            NivelActual++;

            var proyectiles = FindObjectsOfType<Proyectil>();
            foreach (var p in proyectiles)
            {
                if (p != null) Destroy(p.gameObject);
            }

            var itemsLoot = FindObjectsOfType<ItemMejoraRun>();
            foreach (var item in itemsLoot)
            {
                if (item != null)
                {
                    var no = item.GetComponent<NetworkObject>();
                    if (no != null && no.IsSpawned) no.Despawn();
                    else Destroy(item.gameObject);
                }
            }

            var zonasHielo = FindObjectsOfType<ZonaEraHielo>();
            foreach (var zh in zonasHielo)
            {
                if (zh != null)
                {
                    var no = zh.GetComponent<NetworkObject>();
                    if (no != null && no.IsSpawned) no.Despawn();
                    else Destroy(zh.gameObject);
                }
            }

            _enemigosActivos.Clear();
            _salaActivaEnCombate = null;
            _oleadaActualSala = 0;
            _spawneandoOleada = false;

            int semilla = UnityEngine.Random.Range(1000, 999999);
            TipoBioma bioma = TipoBioma.Piedra;
            if (generadorMazmorra != null && generadorMazmorra.biomas != null && generadorMazmorra.biomas.Length > 0)
            {
                bioma = generadorMazmorra.biomas[UnityEngine.Random.Range(0, generadorMazmorra.biomas.Length)].tipo;
            }

            if (generadorMazmorra != null)
            {
                generadorMazmorra.GenerarConSemilla(semilla, bioma);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SemillaMapaActual.Value = semilla;
                SalaRed.Instancia.BiomaMapaActual.Value = (int)bioma;
                SalaRed.Instancia.SincronizarMapaClientRpc(semilla, bioma);
            }

            SpawnearTiendasEnSalas();

            int index = 0;
            foreach (var p in _personajesActivos)
            {
                if (p != null)
                {
                    Vector2 spawnPos = Vector2.zero;
                    if (generadorMazmorra != null && generadorMazmorra.puntosSpawnJugadores.Count > 0)
                    {
                        spawnPos = generadorMazmorra.puntosSpawnJugadores[index % generadorMazmorra.puntosSpawnJugadores.Count];
                    }
                    else if (puntosSpawnJugadores != null && puntosSpawnJugadores.Length > 0)
                    {
                        spawnPos = puntosSpawnJugadores[index % puntosSpawnJugadores.Length].position;
                    }

                    if (p.EstaMuerto.Value)
                    {
                        p.RevivirConMitadVida();
                    }
                    else
                    {
                        p.CurarTotalmente();
                    }

                    p.LimpiarEstadosYHabilidades();
                    p.LimpiarEstadosYHabilidadesClientRpc();
                    p.TeletransportarClientRpc(spawnPos);
                    index++;
                }
            }

            yield return new WaitForSeconds(2.0f);

            string nombreBioma = (generadorMazmorra != null) ? generadorMazmorra.biomaActual.ToString().ToUpper() : "MAZMORRA";
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.FinalizarTransicionNivelClientRpc(NivelActual, new Unity.Collections.FixedString64Bytes(nombreBioma));
            }

            _transicionEnProgreso = false;
        }

        public void SpawnearTiendasEnSalas()
        {
            if (prefabItemMejoraRun == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            var salas = FindObjectsOfType<ControladorSala>();
            foreach (var sala in salas)
            {
                if (sala != null && sala.esSalaTienda)
                {
                    Vector2 centro = sala.limitesSala.center;
                    PuestitoTienda.CrearPuestito(centro);
                }
            }
        }

        private void SpawnearRecompensaFinSala(ControladorSala sala)
        {
            if (prefabItemMejoraRun == null || sala == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            Vector2 centro = sala.limitesSala.center;

            TipoMejoraRun[] posibles = new TipoMejoraRun[]
            {
                TipoMejoraRun.AumentoDano,
                TipoMejoraRun.AumentoDefensa,
                TipoMejoraRun.AumentoVidaMaxima,
                TipoMejoraRun.AumentoVelocidad,
                TipoMejoraRun.PescadoMedio,
                TipoMejoraRun.PescadoCompleto
            };

            TipoMejoraRun elegida1 = posibles[UnityEngine.Random.Range(0, posibles.Length)];
            GameObject itemObj = Instantiate(prefabItemMejoraRun, centro + new Vector2(-1.2f, 0f), Quaternion.identity);
            var item = itemObj.GetComponent<ItemMejoraRun>();
            if (item != null)
            {
                item.Configurar(elegida1, 0); // Recompensa de sala garantizada y gratuita
            }
            var no1 = itemObj.GetComponent<NetworkObject>();
            if (no1 != null) no1.Spawn();

            // 65% de probabilidad de generar una segunda opción gratuita en salas comunes
            if (UnityEngine.Random.value < 0.65f)
            {
                TipoMejoraRun elegida2 = posibles[UnityEngine.Random.Range(0, posibles.Length)];
                GameObject itemObj2 = Instantiate(prefabItemMejoraRun, centro + new Vector2(1.2f, 0f), Quaternion.identity);
                var item2 = itemObj2.GetComponent<ItemMejoraRun>();
                if (item2 != null)
                {
                    item2.Configurar(elegida2, 0);
                }
                var no2 = itemObj2.GetComponent<NetworkObject>();
                if (no2 != null) no2.Spawn();
            }
        }
    }
}
