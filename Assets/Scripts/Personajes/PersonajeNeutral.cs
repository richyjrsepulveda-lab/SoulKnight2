using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Personaje neutral o recluta para el Lobby:
    /// - Es el personaje inicial cuando no se ha seleccionado ningún elemento (TipoPersonaje.Ninguno).
    /// - Permite caminar libremente por el Lobby y entrar a los cuartos físicos de personajes.
    /// - No tiene habilidades de combate asignadas.
    /// </summary>
    public class PersonajeNeutral : Personaje
    {
        public override TipoPersonaje TipoPj => TipoPersonaje.Ninguno;

        protected override void ProcesarInputsHabilidadesLocales()
        {
            // Sin habilidades de combate en estado neutral en el Lobby
        }
    }
}
