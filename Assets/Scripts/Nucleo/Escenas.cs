namespace SoulKnight
{
    /// <summary>
    /// Nombres de TODAS las escenas en un solo lugar.
    /// Lección de Godot: las rutas escritas a mano en cada script ("res://Juego/..." vs
    /// "res://juego/...") causaron bugs al exportar. Aquí nadie escribe el nombre de una
    /// escena "a mano": todos usan estas constantes.
    /// </summary>
    public static class Escenas
    {
        public const string MenuPrincipal = "MenuPrincipal";
        public const string PedirNombre = "PedirNombre";
        public const string MenuJugar = "MenuJugar";
        public const string CrearSala = "CrearSala";
        public const string UnirseSala = "UnirseSala";
        public const string Lobby = "Lobby";
        public const string Juego = "Juego";

        /// <summary>Orden en Build Settings (la primera es la que arranca el juego).</summary>
        public static readonly string[] Todas =
        {
            MenuPrincipal, PedirNombre, MenuJugar, CrearSala, UnirseSala, Lobby, Juego
        };
    }
}
