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
        public int LoopActual { get; private set; } = 0;
        public TipoBioma[] BiomasRun { get; private set; } = new TipoBioma[3];
        public bool EnNivelDecisionLoop { get; private set; } = false;
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

        public void InicializarRecamarasRun()
        {
            List<TipoBioma> disponibles = new List<TipoBioma>()
            {
                TipoBioma.Piedra,
                TipoBioma.Hielo,
                TipoBioma.Fuego,
                TipoBioma.Planta,
                TipoBioma.Arena
            };

            // Barajar aleatoriamente sin repetir
            for (int i = 0; i < disponibles.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, disponibles.Count);
                TipoBioma tmp = disponibles[i];
                disponibles[i] = disponibles[r];
                disponibles[r] = tmp;
            }

            BiomasRun = new TipoBioma[3];
            BiomasRun[0] = disponibles[0];
            BiomasRun[1] = disponibles[1];
            BiomasRun[2] = disponibles[2];
        }

        public TipoBioma ObtenerBiomaPorNivel(int nivel)
        {
            if (BiomasRun == null || BiomasRun.Length < 3 || (BiomasRun[0] == BiomasRun[1] && BiomasRun[1] == BiomasRun[2]))
            {
                InicializarRecamarasRun();
            }

            int nivelEnCiclo = ((nivel - 1) % 10) + 1; // 1 a 10
            if (nivelEnCiclo <= 3) return BiomasRun[0];      // Recámara 1: Niveles 1-3
            if (nivelEnCiclo <= 6) return BiomasRun[1];      // Recámara 2: Niveles 4-6
            return BiomasRun[2];                             // Recámara 3: Niveles 7-10
        }

        public int ObtenerNumeroRecamara(int nivel)
        {
            int nivelEnCiclo = ((nivel - 1) % 10) + 1;
            if (nivelEnCiclo <= 3) return 1;
            if (nivelEnCiclo <= 6) return 2;
            return 3;
        }

        public bool EsNivelDeJefe(int nivel)
        {
            int nivelEnCiclo = ((nivel - 1) % 10) + 1;
            return (nivelEnCiclo == 3 || nivelEnCiclo == 6 || nivelEnCiclo == 10);
        }

        public void IniciarEnServidor()
        {
            if (_partidaIniciada || !NetworkManager.Singleton.IsServer) return;
            _partidaIniciada = true;

            NivelActual = 1;
            LoopActual = 0;
            EnNivelDecisionLoop = false;
            InicializarRecamarasRun();

            int semilla = UnityEngine.Random.Range(1000, 999999);
            TipoBioma bioma = ObtenerBiomaPorNivel(NivelActual);
            bool tieneJefe = EsNivelDeJefe(NivelActual);

            if (generadorMazmorra != null)
            {
                generadorMazmorra.GenerarConSemilla(semilla, bioma, tieneJefe, esNivelDecisionLoop: false);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SemillaMapaActual.Value = semilla;
                SalaRed.Instancia.BiomaMapaActual.Value = (int)bioma;
                SalaRed.Instancia.TieneJefeActual.Value = tieneJefe;
                SalaRed.Instancia.SincronizarMapaClientRpc(semilla, bioma, tieneJefe, esNivelDecisionLoop: false);
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
                if (EsNivelDeJefe(NivelActual))
                {
                    // Nivel con Jefe (3, 6, 10): Iniciar encuentro con el Jefe correspondiente al bioma
                    _oleadaActualSala = 1;
                    StartCoroutine(RutinaSpawnearJefeEnSala(sala));
                }
                else
                {
                    // Nivel normal (1, 2, 4, 5, 7, 8, 9): ¡Cámara Final con Oleada Heavy de Guardianes!
                    // Muchas oleadas con los enemigos más fuertes de cada bioma
                    sala.MostrarPortalesSpawn(false);
                    if (SalaRed.Instancia != null)
                    {
                        SalaRed.Instancia.MostrarPortalesSalaClientRpc(indiceSala, false);
                        SalaRed.Instancia.EnviarAvisoTodos($"💀 ¡SALA FINAL! Despejen la oleada HEAVY de guardianes élite para abrir el pasadizo.");
                    }
                    _oleadaActualSala = 1;
                    StartCoroutine(RutinaSpawnearOleada(sala, sala.puntosSpawnOleada1, esPrimeraOleada: true, esGuardianes: true));
                }
            }
            else
            {
                // Sala normal
                sala.MostrarPortalesSpawn(false);
                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.MostrarPortalesSalaClientRpc(indiceSala, false);
                    SalaRed.Instancia.EnviarAvisoTodos($"⚔️ ¡Sala bloqueada! Prepárense...");
                }
                _oleadaActualSala = 1;
                StartCoroutine(RutinaSpawnearOleada(sala, sala.puntosSpawnOleada1, esPrimeraOleada: true, esGuardianes: false));
            }
        }

        private List<RolEnemigo> ObtenerRolesOleada(TipoBioma bioma, int oleada, int cantidad, bool esGuardianes = false)
        {
            List<RolEnemigo> resultado = new List<RolEnemigo>();
            RolEnemigo[] catalogo = null;

            if (esGuardianes)
            {
                // En la cámara final de guardianes se seleccionan los enemigos más duros e impactantes de cada bioma
                if (oleada == 1)
                {
                    // Oleada 1: Vanguardia de choque con tanques pesados, magos y hostigadores
                    catalogo = new RolEnemigo[] { RolEnemigo.Tanque, RolEnemigo.Tanque, RolEnemigo.Especial, RolEnemigo.MeleeRapido, RolEnemigo.Rango };
                }
                else if (oleada == 2)
                {
                    // Oleada 2: Múltiples Generadores (Spawners) que llenan la sala de esbirros + Magos de artillería + Tanques
                    catalogo = new RolEnemigo[] { RolEnemigo.Spawner, RolEnemigo.Spawner, RolEnemigo.Rango, RolEnemigo.Tanque, RolEnemigo.Especial };
                }
                else
                {
                    // Oleada 3 (Asalto Élite Definitivo Heavy): Combinación total máxima
                    catalogo = new RolEnemigo[] { RolEnemigo.Spawner, RolEnemigo.Tanque, RolEnemigo.Tanque, RolEnemigo.Especial, RolEnemigo.Rango, RolEnemigo.MeleeRapido };
                }
            }
            else
            {
                if (oleada == 1)
                {
                    // Oleada 1 (Vanguardia): Tanques, combate melé y hostigadores frontales
                    catalogo = new RolEnemigo[] { RolEnemigo.Tanque, RolEnemigo.MeleeRapido, RolEnemigo.Especial };
                }
                else
                {
                    // Oleada 2 (Retaguardia & Apoyo): Daño a distancia, invocadores y apoyo táctico
                    if (bioma == TipoBioma.Arena)
                    {
                        catalogo = new RolEnemigo[] { RolEnemigo.Spawner, RolEnemigo.Especial, RolEnemigo.MeleeRapido };
                    }
                    else
                    {
                        catalogo = new RolEnemigo[] { RolEnemigo.Rango, RolEnemigo.Especial, RolEnemigo.Tanque };
                    }
                }
            }

            if (catalogo == null || catalogo.Length == 0)
            {
                catalogo = new RolEnemigo[] { RolEnemigo.Tanque };
            }

            for (int i = 0; i < cantidad; i++)
            {
                resultado.Add(catalogo[i % catalogo.Length]);
            }
            return resultado;
        }

        private IEnumerator RutinaSpawnearOleada(ControladorSala sala, List<Vector2> puntosSpawn, bool esPrimeraOleada, bool esGuardianes = false)
        {
            _spawneandoOleada = true;

            if (esPrimeraOleada)
            {
                yield return new WaitForSeconds(0.2f);
                if (PartidaTerminada || sala == null) { _spawneandoOleada = false; yield break; }

                sala.MostrarPortalesSpawn(true);
                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.MostrarPortalesSalaClientRpc(sala.indiceSala, true);
                    string msgOleada = esGuardianes ? "💀 ¡OLEADA HEAVY DE GUARDIANES! (Oleada 1/3)" : "🌀 ¡Portales abiertos! Oleada 1/2";
                    SalaRed.Instancia.EnviarAvisoTodos(msgOleada);
                }

                yield return new WaitForSeconds(0.35f);
            }
            else
            {
                yield return new WaitForSeconds(0.45f);
            }

            TipoBioma biomaActual = ObtenerBiomaPorNivel(NivelActual);

            // Escalamiento continuo según nivel actual del run (1 a 10) y loops
            int nivelEnCiclo = ((NivelActual - 1) % 10) + 1;
            float escalaNivel = 1.0f + (nivelEnCiclo - 1) * 0.14f;
            float escalaLoop = 1.0f + (LoopActual * 0.65f);
            float multDificultad = escalaNivel * escalaLoop * (esGuardianes ? 1.35f : 1.0f);

            // Refuerzos adicionales según nivel actual de la run (+1 enemigo por nivel)
            List<Vector2> spawnsFinales = new List<Vector2>(puntosSpawn != null ? puntosSpawn : new List<Vector2>());
            if (sala != null && sala.posicionesPortales != null && sala.posicionesPortales.Count > 0)
            {
                int enemigosExtraNivel = Mathf.Clamp(nivelEnCiclo - 1, 0, 8);
                for (int ex = 0; ex < enemigosExtraNivel; ex++)
                {
                    Vector2 pPortal = sala.posicionesPortales[ex % sala.posicionesPortales.Count];
                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * 0.65f;
                    spawnsFinales.Add(pPortal + jitter);
                }
            }

            // Fallback infalible: si por alguna razón la lista de puntos quedó vacía, generar de inmediato una oleada pesada en la sala
            if (spawnsFinales.Count == 0 && sala != null)
            {
                Vector2 centro = sala.limitesSala.center;
                int cantidadFallback = esGuardianes ? 16 : 8;
                for (int f = 0; f < cantidadFallback; f++)
                {
                    float ang = (float)f / cantidadFallback * Mathf.PI * 2f;
                    Vector2 pos = centro + new Vector2(Mathf.Cos(ang) * 5.5f, Mathf.Sin(ang) * 4.2f);
                    spawnsFinales.Add(pos);
                }
            }

            if (spawnsFinales.Count > 0)
            {
                var roles = ObtenerRolesOleada(biomaActual, _oleadaActualSala, spawnsFinales.Count, esGuardianes);
                for (int i = 0; i < spawnsFinales.Count; i++)
                {
                    if (PartidaTerminada) break;

                    RolEnemigo rol = roles[i];
                    GameObject prefab = (rol == RolEnemigo.Rango || rol == RolEnemigo.Spawner)
                        ? (prefabEnemigoMago != null ? prefabEnemigoMago : prefabEnemigo)
                        : (prefabEnemigo != null ? prefabEnemigo : prefabEnemigoSlime);

                    if (prefab != null)
                    {
                        GameObject enemigoObj = Instantiate(prefab, (Vector3)spawnsFinales[i], Quaternion.identity);
                        var netObj = enemigoObj.GetComponent<NetworkObject>();
                        netObj.Spawn(destroyWithScene: true);

                        var enemigo = enemigoObj.GetComponent<EnemigoBase>();
                        if (enemigo != null)
                        {
                            enemigo.ConfigurarRolYBioma(biomaActual, rol, multDificultad, NivelActual);
                            _enemigosActivos.Add(enemigo);
                        }
                    }

                    yield return new WaitForSeconds(0.04f);
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
            TipoBioma biomaActual = ObtenerBiomaPorNivel(NivelActual);
            int nivelEnCiclo = ((NivelActual - 1) % 10) + 1;
            float escalaNivel = 1.0f + (nivelEnCiclo - 1) * 0.12f;
            float escalaLoop = 1.0f + (LoopActual * 0.65f);
            float multDificultad = escalaNivel * escalaLoop;

            GameObject prefabJefeElegido = prefabJefeGolemColosal != null ? prefabJefeGolemColosal : (prefabJefeSlimeRey != null ? prefabJefeSlimeRey : prefabEnemigo);

            if (prefabJefeElegido != null && !PartidaTerminada)
            {
                GameObject jefeObj = Instantiate(prefabJefeElegido, (Vector3)posJefe, Quaternion.identity);
                var noJefe = jefeObj.GetComponent<NetworkObject>();
                noJefe.Spawn(destroyWithScene: true);

                var jefe = jefeObj.GetComponent<JefeBase>();
                if (jefe != null)
                {
                    jefe.ConfigurarJefeBioma(biomaActual, multDificultad);
                    _enemigosActivos.Add(jefe);
                }
            }

            _spawneandoOleada = false;
            RevisarFinDePartida();
        }

        public void SpawnearEsbirroEmergente(Vector2 posicion, TipoBioma bioma, RolEnemigo rol)
        {
            if (!NetworkManager.Singleton.IsServer || PartidaTerminada) return;

            GameObject prefab = (rol == RolEnemigo.Rango || rol == RolEnemigo.Spawner)
                ? (prefabEnemigoMago != null ? prefabEnemigoMago : prefabEnemigo)
                : (prefabEnemigo != null ? prefabEnemigo : prefabEnemigoSlime);

            if (prefab == null) return;

            GameObject go = Instantiate(prefab, (Vector3)posicion, Quaternion.identity);
            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn(destroyWithScene: true);

            var enemigo = go.GetComponent<EnemigoBase>();
            if (enemigo != null)
            {
                enemigo.EsInvocado = true; // Minion invocado: no da XP
                int nivelEnCiclo = ((NivelActual - 1) % 10) + 1;
                float mult = (1.0f + (nivelEnCiclo - 1) * 0.12f) * (1.0f + (LoopActual * 0.65f));
                enemigo.ConfigurarRolYBioma(bioma, rol, mult, NivelActual);
                _enemigosActivos.Add(enemigo);
            }
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

            for (int i = Proyectil.Todos.Count - 1; i >= 0; i--)
            {
                var p = Proyectil.Todos[i];
                if (p != null) Destroy(p.gameObject);
            }

            for (int i = ZonaEraHielo.Todas.Count - 1; i >= 0; i--)
            {
                var z = ZonaEraHielo.Todas[i];
                if (z != null) Destroy(z.gameObject);
            }

            PuestitoTienda.DestruirTodos();
        }

        public void EnemigoEliminado(EnemigoBase enemigo)
        {
            _enemigosActivos.Remove(enemigo);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !PartidaTerminada)
            {
                // ÚNICAMENTE los enemigos naturales/invocadores dan XP; los esbirros invocados NO dan XP
                if (enemigo != null && !enemigo.EsInvocado)
                {
                    // Experiencia moderada y controlada (16 XP por normal, 28 XP por tanque o invocador)
                    int rolVal = enemigo.RolActualNet.Value;
                    int xpOtorgada = (rolVal == (int)RolEnemigo.Tanque || rolVal == (int)RolEnemigo.Especial || rolVal == (int)RolEnemigo.Spawner) ? 28 : 16;

                    foreach (var p in _personajesActivos)
                    {
                        if (p != null && p.nivelProgreso != null)
                        {
                            p.nivelProgreso.AgregarXP(xpOtorgada);
                        }
                    }
                }

                if (prefabItemMejoraRun != null && enemigo != null && !enemigo.EsInvocado && UnityEngine.Random.value < 0.22f)
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
                bool esCamaraGuardianes = _salaActivaEnCombate.esSalaJefe && !EsNivelDeJefe(NivelActual);
                int maxOleadas = esCamaraGuardianes ? 3 : 2;
                bool esJefeReal = _salaActivaEnCombate.esSalaJefe && EsNivelDeJefe(NivelActual);

                if (!esJefeReal && _oleadaActualSala < maxOleadas)
                {
                    _oleadaActualSala++;
                    List<Vector2> siguientesSpawns = null;
                    if (_oleadaActualSala == 2)
                    {
                        siguientesSpawns = _salaActivaEnCombate.puntosSpawnOleada2;
                        if (SalaRed.Instancia != null)
                        {
                            if (esCamaraGuardianes)
                                SalaRed.Instancia.EnviarAvisoTodos("💀 ¡OLEADA HEAVY 2/3: Invocadores masivos y artillería élite!");
                            else
                                SalaRed.Instancia.EnviarAvisoTodos("🌀 ¡Oleada 2/2 saliendo de los portales!");
                        }
                    }
                    else if (_oleadaActualSala == 3)
                    {
                        siguientesSpawns = _salaActivaEnCombate.puntosSpawnOleada3;
                        if (siguientesSpawns == null || siguientesSpawns.Count == 0)
                            siguientesSpawns = _salaActivaEnCombate.puntosSpawnOleada2;

                        if (SalaRed.Instancia != null)
                        {
                            SalaRed.Instancia.EnviarAvisoTodos("🔥 ¡OLEADA HEAVY FINAL 3/3: Asalto destructor de élite!");
                        }
                    }

                    if (siguientesSpawns == null || siguientesSpawns.Count == 0)
                    {
                        siguientesSpawns = _salaActivaEnCombate.puntosSpawnOleada1;
                    }

                    StartCoroutine(RutinaSpawnearOleada(_salaActivaEnCombate, siguientesSpawns, esPrimeraOleada: false, esGuardianes: esCamaraGuardianes));
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
                        // Se mató al jefe o se completó la cámara de guardianes: abrir el pasillo final y activar el hoyo
                        salaCompletada.AbrirMuroSalidaJefe();
                        ActivarPasajeSiguienteNivel();

                        // En la cámara de guardianes también otorgamos cofre de recompensas por superar el desafío
                        SpawnearRecompensaFinSala(salaCompletada);

                        if (SalaRed.Instancia != null)
                        {
                            if (esCamaraGuardianes)
                                SalaRed.Instancia.EnviarAvisoTodos("🏆 ¡OLEADA HEAVY SUPERADA! Pasillo final abierto.");
                            else
                                SalaRed.Instancia.EnviarAvisoTodos("👑 ¡Jefe derrotado! Pasillo final abierto.");
                        }
                    }
                    else
                    {
                        // Sala normal
                        SpawnearRecompensaFinSala(salaCompletada);
                        if (SalaRed.Instancia != null)
                        {
                            SalaRed.Instancia.EnviarAvisoTodos("✅ ¡Sala despejada! Portales cerrados y puertas abiertas.");
                        }
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

            if (NivelActual == 10 && !EnNivelDecisionLoop)
            {
                // ¡Fin de los 10 niveles! Entrar al Nivel 10.1 (Santuario del Triunfo)
                EnNivelDecisionLoop = true;
            }
            else
            {
                NivelActual++;
            }

            for (int i = Proyectil.Todos.Count - 1; i >= 0; i--)
            {
                var p = Proyectil.Todos[i];
                if (p != null) Destroy(p.gameObject);
            }

            for (int i = ItemMejoraRun.Todos.Count - 1; i >= 0; i--)
            {
                var item = ItemMejoraRun.Todos[i];
                if (item != null)
                {
                    var no = item.GetComponent<NetworkObject>();
                    if (no != null && no.IsSpawned) no.Despawn();
                    else Destroy(item.gameObject);
                }
            }

            for (int i = ZonaEraHielo.Todas.Count - 1; i >= 0; i--)
            {
                var zh = ZonaEraHielo.Todas[i];
                if (zh != null)
                {
                    var no = zh.GetComponent<NetworkObject>();
                    if (no != null && no.IsSpawned) no.Despawn();
                    else Destroy(zh.gameObject);
                }
            }

            PuestitoTienda.DestruirTodos();

            _enemigosActivos.Clear();
            _salaActivaEnCombate = null;
            _oleadaActualSala = 0;
            _spawneandoOleada = false;

            int semilla = UnityEngine.Random.Range(1000, 999999);
            TipoBioma bioma = EnNivelDecisionLoop ? TipoBioma.Piedra : ObtenerBiomaPorNivel(NivelActual);
            bool tieneJefe = !EnNivelDecisionLoop && EsNivelDeJefe(NivelActual);

            if (generadorMazmorra != null)
            {
                generadorMazmorra.GenerarConSemilla(semilla, bioma, tieneJefe, esNivelDecisionLoop: EnNivelDecisionLoop);
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SemillaMapaActual.Value = semilla;
                SalaRed.Instancia.BiomaMapaActual.Value = (int)bioma;
                SalaRed.Instancia.TieneJefeActual.Value = tieneJefe;
                SalaRed.Instancia.SincronizarMapaClientRpc(semilla, bioma, tieneJefe, esNivelDecisionLoop: EnNivelDecisionLoop);
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

            string textoBannerFinal = "";
            if (EnNivelDecisionLoop)
            {
                textoBannerFinal = "SANTUARIO DEL TRIUNFO\n(NIVEL 10.1)";
            }
            else
            {
                string extraLoop = LoopActual > 0 ? $" [BUCLE +{LoopActual}]" : "";
                textoBannerFinal = $"NIVEL {NivelActual}{extraLoop}";
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.FinalizarTransicionNivelClientRpc(NivelActual, new Unity.Collections.FixedString64Bytes(textoBannerFinal));
            }

            _transicionEnProgreso = false;
        }

        public void SolicitarAvanzarBucleServerRpc()
        {
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.SolicitarAvanzarBucleServerRpc();
            }
            else
            {
                AvanzarAlBucleInfinito();
            }
        }

        public void AvanzarAlBucleInfinito()
        {
            if (!NetworkManager.Singleton.IsServer || PartidaTerminada || _transicionEnProgreso) return;

            LoopActual++;
            EnNivelDecisionLoop = false;
            NivelActual = 10; // Para que al hacer la transición avance al nivel 11
            InicializarRecamarasRun();

            // Conservan el oro pero pierden las mejoras temporales adquiridas durante la run
            foreach (var p in _personajesActivos)
            {
                if (p != null)
                {
                    p.PerderMejorasTemporalesPorMuerteEnLoop();
                }
            }

            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.EnviarAvisoTodos($"💀 ¡HA COMENZADO EL BUCLE +{LoopActual}! (Niveles {LoopActual * 10 + 1}-{LoopActual * 10 + 10}). Los enemigos serán mucho más peligrosos.");
            }

            StartCoroutine(RutinaTransicionNivel());
        }

        public void FinalizarRunConVictoria()
        {
            if (!NetworkManager.Singleton.IsServer || PartidaTerminada) return;

            DetenerPartida();
            if (SalaRed.Instancia != null)
            {
                SalaRed.Instancia.FinPartidaClientRpc(victoria: true);
                SalaRed.Instancia.EnviarAvisoTodos("🏆 ¡VICTORIA TOTAL! Han completado la mazmorra y reclamado su gloria.");
            }
        }

        public void SpawnearTiendasEnSalas()
        {
            // La tienda se instancia limpiamente en GeneradorMazmorra dentro de su sala asignada
            // y queda emparentada a la sala para asegurar destrucción y sincronización determinista.
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
