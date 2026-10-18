using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    public enum TipoDecisionPortal
    {
        ContinuarLoop,
        SalirLobby
    }

    /// <summary>
    /// Maneja los 2 portales del Nivel 10.1 (Santuario del Triunfo):
    /// - Arriba: Desafío del Bucle (Niveles 11+ con enemigos más difíciles, escalado infinito y advertencia).
    /// - Abajo: Salir al Lobby (Reclamar victoria, conservar oro y gemas acumulados).
    /// </summary>
    public class PortalDecisionLoop : MonoBehaviour
    {
        public TipoDecisionPortal tipoDecision;
        public string tituloPortal = "";
        public string subtituloPortal = "";
        public Color colorPortal = Color.yellow;

        private readonly HashSet<Personaje> _jugadoresDentro = new HashSet<Personaje>();
        private float _tiempoCooldownMensaje = 0f;

        void Start()
        {
            // Crear cartel flotante estático sobre el portal
            TextoFlotante.Crear(transform.position + new Vector3(0, 1.8f, 0), tituloPortal, colorPortal);
            if (!string.IsNullOrEmpty(subtituloPortal))
            {
                TextoFlotante.Crear(transform.position + new Vector3(0, 1.1f, 0), subtituloPortal, new Color(0.85f, 0.85f, 0.85f));
            }
        }

        void Update()
        {
            if (_tiempoCooldownMensaje > 0f) _tiempoCooldownMensaje -= Time.deltaTime;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers == null || pers.EstaMuerto.Value) return;

            _jugadoresDentro.Add(pers);

            if (tipoDecision == TipoDecisionPortal.ContinuarLoop)
            {
                if (pers.IsOwner || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                {
                    if (HUDJugador.Instancia != null)
                    {
                        HUDJugador.Instancia.MostrarAdvertenciaBucle(() =>
                        {
                            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                            {
                                if (GestorPartida.Instancia != null)
                                {
                                    GestorPartida.Instancia.AvanzarAlBucleInfinito();
                                }
                            }
                            else if (GestorPartida.Instancia != null)
                            {
                                GestorPartida.Instancia.SolicitarAvanzarBucleServerRpc();
                            }
                        });
                    }
                }
            }
            else if (tipoDecision == TipoDecisionPortal.SalirLobby)
            {
                if (_tiempoCooldownMensaje <= 0f)
                {
                    _tiempoCooldownMensaje = 2f;
                    TextoFlotante.Crear(transform.position + new Vector3(0, 2.5f, 0), "¡TODOS LOS JUGADORES DEBEN ENTRAR PARA SALIR!", Color.green);
                }

                VerificarJugadoresSalirLobby();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var pers = collision.GetComponent<Personaje>();
            if (pers != null)
            {
                _jugadoresDentro.Remove(pers);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (tipoDecision == TipoDecisionPortal.SalirLobby)
            {
                var pers = collision.GetComponent<Personaje>();
                if (pers != null && !pers.EstaMuerto.Value && !_jugadoresDentro.Contains(pers))
                {
                    _jugadoresDentro.Add(pers);
                    VerificarJugadoresSalirLobby();
                }
            }
        }

        private void VerificarJugadoresSalirLobby()
        {
            if (GestorPartida.Instancia == null) return;
            _jugadoresDentro.RemoveWhere(p => p == null || p.EstaMuerto.Value);

            var vivos = GestorPartida.Instancia.ObtenerJugadoresVivos();
            if (vivos.Count > 0 && _jugadoresDentro.Count >= vivos.Count)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    GestorPartida.Instancia.FinalizarRunConVictoria();
                }
            }
        }
    }
}
