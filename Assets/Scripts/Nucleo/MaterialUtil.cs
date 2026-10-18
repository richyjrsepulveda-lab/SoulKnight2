using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Utilidades de alto rendimiento para evitar asignaciones de memoria (GC allocations)
    /// y llamadas costosas de la API de Unity en los bucles principales (Update/LateUpdate).
    /// </summary>
    public static class MaterialUtil
    {
        private static Material _spritesDefault;

        /// <summary>
        /// Material compartido con shader Sprites/Default.
        /// Evita crear instancias huérfanas con 'new Material(Shader.Find(...))' en cada ataque o efecto.
        /// </summary>
        public static Material SpritesDefault
        {
            get
            {
                if (_spritesDefault == null)
                {
                    Shader s = Shader.Find("Sprites/Default");
                    if (s != null)
                    {
                        _spritesDefault = new Material(s)
                        {
                            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                        };
                    }
                }
                return _spritesDefault;
            }
        }
    }

    /// <summary>
    /// Acceso en caché a la cámara principal para evitar que 'Camera.main' recorra la escena con FindWithTag cada frame.
    /// </summary>
    public static class CamaraUtil
    {
        private static Camera _camaraCacheadas;

        public static Camera Principal
        {
            get
            {
                if (_camaraCacheadas == null)
                {
                    if (CamaraSeguir.Instancia != null)
                    {
                        _camaraCacheadas = CamaraSeguir.Instancia.GetComponent<Camera>();
                    }
                    if (_camaraCacheadas == null)
                    {
                        _camaraCacheadas = Camera.main;
                    }
                }
                return _camaraCacheadas;
            }
        }

        public static void InvalidarCache()
        {
            _camaraCacheadas = null;
        }
    }
}
