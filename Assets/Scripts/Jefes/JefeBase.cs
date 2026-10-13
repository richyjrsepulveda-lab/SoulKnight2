using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoJefe : byte
    {
        SlimeRey = 0,        // Slime gigante con saltos aplastantes y división de esbirros
        GranBrujo = 1,       // Mago nigromante con proyectiles en espiral y rayos
        GolemColosal = 2     // Guardián de roca con ondas sísmicas y carga pesada
    }

    /// <summary>
    /// Base extensible para jefes con dificultad escalable según la cantidad real de jugadores en la partida:
    /// - Al aparecer en la sala final, realiza una animación de caída desde el cielo e impacta el suelo.
    /// - Conecta su nombre y barra de vida superior estilo Dark Souls en el HUD de todos los jugadores.
    /// - Al morir, oculta la barra del HUD y desbloquea el pasillo final con el hoyo para avanzar de nivel.
    /// </summary>
    public class JefeBase : EnemigoBase
    {
        [Header("Configuración de Jefe")]
        public TipoJefe tipoJefe;
        public string nombreJefe = "Jefe Desconocido";

        [Header("Escalado por Jugadores")]
        public float multiplicadorVidaPorJugador = 0.75f; // +75% de vida por cada jugador adicional
        public float multiplicadorDanoPorJugador = 0.20f; // +20% de daño por cada jugador adicional

        [Header("Habilidades Especiales del Jefe")]
        public float tiempoEntreHabilidadesEspeciales = 5.0f;
        public GameObject prefabProyectilJefe;

        public NetworkVariable<int> VidaMaxSincronizada = new NetworkVariable<int>(500);

        protected int _numJugadoresAlSpawn = 1;
        protected float _timerHabilidadEspecial = 0f;

        protected override void Awake()
        {
            base.Awake();
            rangoMaximoDeteccion = 22f; // Rango dentro de su propia sala de jefe
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CalcularEscaladoPorJugadores();
            }

            base.OnNetworkSpawn();

            // Ocultar la barrita pequeña sobre la cabeza porque el Jefe usa la barra gigante arriba estilo Dark Souls
            if (barraVida != null)
            {
                barraVida.gameObject.SetActive(false);
            }

            VidaActual.OnValueChanged += (prev, act) =>
            {
                if (HUDJugador.Instancia != null)
                {
                    int maxHP = VidaMaxSincronizada.Value > 0 ? VidaMaxSincronizada.Value : vidaMax;
                    HUDJugador.Instancia.ActualizarBarraJefeDarkSouls(act, maxHP);
                }
            };

            VidaMaxSincronizada.OnValueChanged += (prev, act) =>
            {
                if (HUDJugador.Instancia != null && act > 0)
                {
                    HUDJugador.Instancia.ActualizarBarraJefeDarkSouls(VidaActual.Value, act);
                }
            };

            // Mostrar de inmediato la barra de vida del Jefe arriba al centro para que esté sincronizada desde el primer instante
            if (HUDJugador.Instancia != null)
            {
                int maxHP = VidaMaxSincronizada.Value > 0 ? VidaMaxSincronizada.Value : vidaMax;
                HUDJugador.Instancia.MostrarBarraJefeDarkSouls(nombreJefe, VidaActual.Value, maxHP);
            }

            // Iniciar cinemática de caída desde arriba en todos los clientes
            StartCoroutine(RutinaCaidaEntradaJefe());
        }

        public override void OnNetworkDespawn()
        {
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.OcultarBarraJefeDarkSouls();
            }
            base.OnNetworkDespawn();
        }

        private IEnumerator RutinaCaidaEntradaJefe()
        {
            enAnimacionEntrada = true;
            if (_col != null) _col.enabled = false;

            Vector3 posDestino = transform.position;
            Vector3 escalaBase = transform.localScale;
            Vector3 posCielo = posDestino + new Vector3(0f, 13f, 0f);

            // Crear sombra en el suelo donde va a caer el jefe
            GameObject sombraObj = new GameObject("SombraCaidaJefe");
            sombraObj.transform.position = posDestino + new Vector3(0f, -0.4f, 0f);
            var srSombra = sombraObj.AddComponent<SpriteRenderer>();
            Texture2D tex = Texture2D.whiteTexture;
            srSombra.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            srSombra.color = new Color(0f, 0f, 0f, 0.45f);
            srSombra.sortingOrder = 1;

            float duracionCaida = 0.95f;
            float t = 0f;
            while (t < duracionCaida)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duracionCaida);
                float curva = p * p; // Aceleración gravitatoria
                transform.position = Vector3.Lerp(posCielo, posDestino, curva);

                if (sombraObj != null)
                {
                    float escSombra = Mathf.Lerp(0.4f, 2.8f, curva);
                    sombraObj.transform.localScale = new Vector3(escSombra, escSombra * 0.45f, 1f);
                }
                yield return null;
            }

            if (sombraObj != null) Destroy(sombraObj);
            transform.position = posDestino;

            // Impacto aplastante (squash & stretch)
            transform.localScale = new Vector3(escalaBase.x * 1.35f, escalaBase.y * 0.65f, escalaBase.z);
            TextoFlotante.Crear(posDestino + new Vector3(0f, 1.8f, 0f), $"¡{nombreJefe.ToUpper()}!", new Color(1f, 0.25f, 0.2f));

            // Mostrar/actualizar Barra de Vida y Nombre arriba al centro estilo Dark Souls
            int maxHP = VidaMaxSincronizada.Value > 0 ? VidaMaxSincronizada.Value : vidaMax;
            if (HUDJugador.Instancia != null)
            {
                HUDJugador.Instancia.MostrarBarraJefeDarkSouls(nombreJefe, VidaActual.Value, maxHP);
            }

            float tRebote = 0f;
            while (tRebote < 0.25f)
            {
                tRebote += Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, escalaBase, tRebote / 0.25f);
                yield return null;
            }

            transform.localScale = escalaBase;
            if (_col != null) _col.enabled = true;
            enAnimacionEntrada = false;
        }

        public void CalcularEscaladoPorJugadores()
        {
            if (!IsServer) return;

            int jugadores = 1;
            if (SalaRed.Instancia != null && SalaRed.Instancia.Jugadores.Count > 0)
            {
                jugadores = Mathf.Max(1, SalaRed.Instancia.Jugadores.Count);
            }
            else if (GestorPartida.Instancia != null)
            {
                var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
                if (vivos.Count > 0) jugadores = vivos.Count;
            }

            _numJugadoresAlSpawn = jugadores;

            int pisoActual = (GestorPartida.Instancia != null) ? Mathf.Max(1, GestorPartida.Instancia.NivelActual) : 1;
            float factorPiso = 1.0f + (pisoActual - 1) * 0.25f;

            // Estadísticas de jefe desafiantes pero sin one-shots injustos
            switch (tipoJefe)
            {
                case TipoJefe.SlimeRey:
                    vidaMax = 850;
                    velocidadBase = 3.6f;
                    dano = 14;
                    tiempoEntreHabilidadesEspeciales = 3.0f;
                    tiempoEntreAtaques = 1.4f;
                    break;
                case TipoJefe.GranBrujo:
                    vidaMax = 950;
                    velocidadBase = 3.3f;
                    dano = 15;
                    tiempoEntreHabilidadesEspeciales = 2.7f;
                    tiempoEntreAtaques = 1.5f;
                    break;
                case TipoJefe.GolemColosal:
                    vidaMax = 1150;
                    velocidadBase = 3.0f;
                    dano = 18;
                    tiempoEntreHabilidadesEspeciales = 3.1f;
                    tiempoEntreAtaques = 1.6f;
                    break;
            }

            float factorVida = (1.0f + ((jugadores - 1) * multiplicadorVidaPorJugador)) * factorPiso;
            int nuevaVidaMax = Mathf.RoundToInt(vidaMax * factorVida);
            vidaMax = nuevaVidaMax;
            VidaMaxSincronizada.Value = nuevaVidaMax;
            VidaActual.Value = nuevaVidaMax;

            float factorDano = (1.0f + ((jugadores - 1) * 0.12f)) * (1.0f + (pisoActual - 1) * 0.12f);
            dano = Mathf.Clamp(Mathf.RoundToInt(dano * factorDano), 12, 26);
        }

        protected override void Update()
        {
            // Sincronización continua y en tiempo real de la barra de vida del Jefe en todos los clientes
            if (HUDJugador.Instancia != null && VidaActual.Value > 0)
            {
                int maxHP = VidaMaxSincronizada.Value > 0 ? VidaMaxSincronizada.Value : vidaMax;
                HUDJugador.Instancia.ActualizarBarraJefeDarkSouls(VidaActual.Value, maxHP);
            }

            base.Update();
            if (!IsServer || enAnimacionEntrada || EstaCongelado.Value) return;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0)) return;

            Personaje objetivo = ObtenerJugadorMasCercano();
            if (objetivo == null) return;

            // En Fase 2 (< 50% de vida) el jefe entra en furia y usa habilidades 25% más rápido
            bool enFuria = VidaMaxSincronizada.Value > 0 && VidaActual.Value <= VidaMaxSincronizada.Value * 0.5f;
            float multFuria = enFuria ? 1.25f : 1.0f;

            _timerHabilidadEspecial += Time.deltaTime * multFuria;
            if (_timerHabilidadEspecial >= tiempoEntreHabilidadesEspeciales)
            {
                _timerHabilidadEspecial = 0f;
                EjecutarPatronAtaqueJefe();
            }
        }

        protected virtual void EjecutarPatronAtaqueJefe()
        {
            switch (tipoJefe)
            {
                case TipoJefe.SlimeRey:
                    StartCoroutine(PatronSlimeRey());
                    break;
                case TipoJefe.GranBrujo:
                    PatronGranBrujo();
                    break;
                case TipoJefe.GolemColosal:
                    StartCoroutine(PatronGolemColosal());
                    break;
            }
        }

        private IEnumerator PatronSlimeRey()
        {
            int balas = 10 + (_numJugadoresAlSpawn * 2);
            float anguloPaso = 360f / balas;
            int danoBala = Mathf.Max(10, Mathf.RoundToInt(dano * 0.85f));

            // Primera nova radial
            for (int i = 0; i < balas; i++)
            {
                float rad = (i * anguloPaso) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                DispararProyectilJefe(dir, danoBala);
            }

            yield return new WaitForSeconds(0.42f);
            if (EstaCongelado.Value || VidaActual.Value <= 0) yield break;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0)) yield break;

            // Salto moderado y segunda nova desfasada
            Personaje obj = ObtenerJugadorMasCercano();
            if (obj != null)
            {
                Vector2 dirSalto = ((Vector2)obj.transform.position - (Vector2)transform.position).normalized;
                AplicarKnockback(dirSalto, 320f);
            }

            for (int i = 0; i < balas; i++)
            {
                float rad = ((i + 0.5f) * anguloPaso) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                DispararProyectilJefe(dir, danoBala);
            }
        }

        private void PatronGranBrujo()
        {
            Personaje objetivo = ObtenerJugadorMasCercano();
            if (objetivo == null) return;

            Vector2 dirPrincipal = ((Vector2)objetivo.transform.position - (Vector2)transform.position).normalized;
            StartCoroutine(RafagaEspiral(dirPrincipal));
        }

        private IEnumerator RafagaEspiral(Vector2 dirBase)
        {
            int rafagas = 5 + (_numJugadoresAlSpawn * 2);
            int danoBala = Mathf.Max(10, Mathf.RoundToInt(dano * 0.80f));
            for (int r = 0; r < rafagas; r++)
            {
                if (EstaCongelado.Value || VidaActual.Value <= 0) yield break;
                if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0)) yield break;

                Personaje obj = ObtenerJugadorMasCercano();
                if (obj != null)
                {
                    dirBase = ((Vector2)obj.transform.position - (Vector2)transform.position).normalized;
                }

                // Abanico triple con oscilación en espiral
                float anguloCentro = Mathf.Sin(r * 0.9f) * 28f;
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 dir = Quaternion.Euler(0, 0, anguloCentro + k * 20f) * dirBase;
                    DispararProyectilJefe(dir, danoBala);
                }
                yield return new WaitForSeconds(0.18f);
            }
        }

        private IEnumerator PatronGolemColosal()
        {
            // Embestida sísmica moderada hacia el objetivo antes del pisotón
            Personaje obj = ObtenerJugadorMasCercano();
            if (obj != null)
            {
                Vector2 dirCarga = ((Vector2)obj.transform.position - (Vector2)transform.position).normalized;
                AplicarKnockback(dirCarga, 360f);
            }

            yield return new WaitForSeconds(0.32f);
            if (EstaCongelado.Value || VidaActual.Value <= 0) yield break;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0)) yield break;

            float radioTerremoto = 5.4f + (_numJugadoresAlSpawn * 0.5f);
            int danoSismo = Mathf.Clamp(Mathf.RoundToInt(dano * 1.1f), 14, 24);
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radioTerremoto);
            foreach (var hit in hits)
            {
                var pj = hit.GetComponent<Personaje>();
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.RecibirDano(danoSismo);
                    Vector2 dirKnock = ((Vector2)pj.transform.position - (Vector2)transform.position).normalized;
                    pj.AplicarKnockback(dirKnock, 420f);

                    if (pj is PersonajeFuego pjFuego && pjFuego.ArmaduraActiva.Value)
                    {
                        AplicarQuemadura();
                        RecibirDano(18);
                    }
                }
            }

            // Onda de rocas sísmicas
            int rocas = 8 + (_numJugadoresAlSpawn * 2);
            float paso = 360f / rocas;
            int danoRoca = Mathf.Max(10, Mathf.RoundToInt(dano * 0.75f));
            for (int i = 0; i < rocas; i++)
            {
                float rad = (i * paso) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                DispararProyectilJefe(dir, danoRoca);
            }
        }

        protected void DispararProyectilJefe(Vector2 dir, int danoProj)
        {
            if (prefabProyectilJefe == null) return;
            if (GestorPartida.Instancia != null && (GestorPartida.Instancia.PartidaTerminada || GestorPartida.Instancia.ObtenerJugadoresVivos().Count == 0)) return;

            Vector3 posOrigen = transform.position + (Vector3)(dir.normalized * 0.85f);
            GameObject p = Instantiate(prefabProyectilJefe, posOrigen, Quaternion.identity);
            var proj = p.GetComponent<Proyectil>();
            if (proj != null)
            {
                proj.Configurar(dir, danoProj, 999999);
            }
            var no = p.GetComponent<NetworkObject>();
            if (no != null) no.Spawn();
            if (proj != null) proj.DireccionSincronizada.Value = dir;
        }

        protected override void Morir()
        {
            if (IsServer)
            {
                var jugadores = FindObjectsOfType<Personaje>();
                foreach (var pj in jugadores)
                {
                    if (pj != null && pj.nivelProgreso != null)
                    {
                        pj.nivelProgreso.AgregarXP(350);
                    }
                }

                if (SalaRed.Instancia != null)
                {
                    SalaRed.Instancia.EnviarAvisoTodos($"🏆 ¡{nombreJefe} derrotado! Se abrió el pasillo final hacia el hoyo.");
                }
            }

            base.Morir();
        }
    }
}
