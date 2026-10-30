using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Catálogo central de Biomas de Mazmorra. Carga y organiza los sprites oficiales (16x16, 16 PPU, Point filter)
    /// para los 5 biomas: Piedra (stone), Hielo (ice), Fuego (fire), Planta (plant) y Arena (desert).
    /// </summary>
    public static class CatalogoBiomas
    {
        private static readonly Dictionary<TipoBioma, DatosBioma> _cacheBiomas = new Dictionary<TipoBioma, DatosBioma>();

        public static DatosBioma Obtener(TipoBioma tipo)
        {
            if (_cacheBiomas.TryGetValue(tipo, out var datos) && datos != null && datos.spriteSuelo != null)
            {
                return datos;
            }

            datos = GenerarDatosBioma(tipo);
            _cacheBiomas[tipo] = datos;
            return datos;
        }

        public static void LimpiarCache()
        {
            _cacheBiomas.Clear();
        }

        private static DatosBioma GenerarDatosBioma(TipoBioma tipo)
        {
            switch (tipo)
            {
                case TipoBioma.Piedra:
                    return CargarBiomaOficial(TipoBioma.Piedra, "stone", "Mazmorra de Piedra Ancestral", new Color(1f, 0.75f, 0.32f, 1f));

                case TipoBioma.Hielo:
                    return CargarBiomaOficial(TipoBioma.Hielo, "ice", "Cripta de Hielo Eterno", new Color(0.38f, 0.88f, 1f, 1f));

                case TipoBioma.Fuego:
                    return CargarBiomaOficial(TipoBioma.Fuego, "fire", "Fortaleza Volcánica de Basalto", new Color(1f, 0.65f, 0.20f, 1f));

                case TipoBioma.Planta:
                    return CargarBiomaOficial(TipoBioma.Planta, "plant", "Santuario Cubierto de Musgo", new Color(0.55f, 0.95f, 0.40f, 1f));

                case TipoBioma.Arena:
                    return CargarBiomaOficial(TipoBioma.Arena, "desert", "Necrópolis de Arenas Doradas", new Color(1f, 0.84f, 0.38f, 1f));

                default:
                    return CargarBiomaOficial(TipoBioma.Piedra, "stone", "Mazmorra de Piedra Ancestral", new Color(1f, 0.75f, 0.32f, 1f));
            }
        }

        private static DatosBioma CargarBiomaOficial(TipoBioma tipo, string prefijo, string nombre, Color colorLuz)
        {
            DatosBioma b = new DatosBioma();
            b.tipo = tipo;
            b.prefijo = prefijo;
            b.nombreBioma = nombre;
            b.colorFondoCamara = Color.black;
            b.colorLuzAntorcha = colorLuz;

            // 1. Suelos (floor_00..07)
            b.spritesSuelo = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                b.spritesSuelo[i] = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_floor_{i:D2}");
            }
            b.spriteSuelo = b.spritesSuelo[0];

            // 2. Sombras y Vacío
            b.spriteFloorShadow00 = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_floor_shadow_00");
            b.spriteFloorShadow01 = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_floor_shadow_01");
            b.spriteVoid = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_void");

            // 3. Muros Top (16 variantes autotile por bitmask N*1 + E*2 + S*4 + W*8)
            b.spritesWallTop = new Sprite[16];
            for (int i = 0; i < 16; i++)
            {
                b.spritesWallTop[i] = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_top_{i:D2}");
            }
            b.spriteMuro = b.spritesWallTop[15] ?? b.spritesWallTop[0];

            // 4. Muro Cara Frontal (wall_face_*) y Antorcha Animada
            b.spriteWallFaceA = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_a");
            b.spriteWallFaceB = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_b");
            b.spriteWallFaceCracked = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_cracked");
            b.spriteWallFaceDecor = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_grate") ?? CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_decor");
            b.spriteWallFaceBanner = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_banner");

            b.framesWallFaceTorch = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                b.framesWallFaceTorch[i] = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_wall_face_torch_f{i}");
            }
            b.spriteLlamaAntorcha = b.framesWallFaceTorch[0];
            b.spriteTorchGlow = CargarSprite($"Mapas/{prefijo}/walls/{prefijo}_torch_glow");

            // 5. Calcomanías de Suelo (en stone y biomas que aplique)
            var decalCrack = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_decal_crack");
            var decalPebbles = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_decal_pebbles");
            var decalBones = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_decal_bones");
            var decalPuddle = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_decal_puddle");
            var listDecals = new List<Sprite>();
            if (decalCrack != null) listDecals.Add(decalCrack);
            if (decalPebbles != null) listDecals.Add(decalPebbles);
            if (decalBones != null) listDecals.Add(decalBones);
            if (decalPuddle != null) listDecals.Add(decalPuddle);
            b.spritesDecals = listDecals.Count > 0 ? listDecals.ToArray() : null;

            // 6. Bloques Autotile (16 variantes bitmask N*1 + E*2 + S*4 + W*8)
            b.spritesBlock = new Sprite[16];
            for (int i = 0; i < 16; i++)
            {
                b.spritesBlock[i] = CargarSprite($"Mapas/{prefijo}/blocks/{prefijo}_block_{i:D2}");
            }

            // 7. Props Generales
            b.spritePillar = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_pillar");
            b.spriteColumna = b.spritePillar;
            b.spriteBoulderS = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_boulder_s");
            b.spriteBoulderL = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_boulder_l");
            b.spriteCrate = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_crate");
            b.spriteBarrel = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_barrel");
            b.spriteBarrelRed = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_barrel_red");
            b.spritePot = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_pot");
            b.spritePit = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_pit");

            // Trampa de pinchos: 4 frames (stone) o 2 frames (otros biomas)
            var spF0 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_f0");
            var spF1 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_f1");
            var spF2 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_f2");
            var spF3 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_f3");
            if (spF0 != null && spF1 != null && spF2 != null && spF3 != null)
            {
                b.framesSpikesStrip = new Sprite[] { spF0, spF1, spF2, spF3 };
                b.spriteSpikesOff = spF0;
                b.spriteSpikesOn = spF2;
            }
            else
            {
                b.spriteSpikesOff = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_off");
                b.spriteSpikesOn = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_spikes_on");
                if (b.spriteSpikesOff != null && b.spriteSpikesOn != null)
                {
                    b.framesSpikesStrip = new Sprite[] { b.spriteSpikesOff, b.spriteSpikesOn };
                }
            }

            // Agua animada (stone)
            var wF0 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_water_f0");
            var wF1 = CargarSprite($"Mapas/{prefijo}/props/{prefijo}_water_f1");
            if (wF0 != null && wF1 != null)
            {
                b.framesWaterStrip = new Sprite[] { wF0, wF1 };
            }

            // Hazard por bioma
            switch (prefijo)
            {
                case "stone":
                    if (b.spritePit != null)
                    {
                        b.framesHazardStrip = new Sprite[] { b.spritePit };
                    }
                    b.spriteSpecialProp = CargarSprite($"Mapas/{prefijo}/tiles/{prefijo}_decal_bones") ?? CargarSprite($"Mapas/{prefijo}/props/{prefijo}_bones");
                    break;
                case "plant":
                    b.framesHazardStrip = new Sprite[4];
                    for (int i = 0; i < 4; i++) b.framesHazardStrip[i] = CargarSprite($"Mapas/plant/props/plant_poison_f{i}");
                    b.spriteSpecialProp = CargarSprite("Mapas/plant/props/plant_thornbush");
                    break;
                case "ice":
                    b.framesHazardStrip = new Sprite[4];
                    for (int i = 0; i < 4; i++) b.framesHazardStrip[i] = CargarSprite($"Mapas/ice/props/ice_icewater_f{i}");
                    b.spriteSpecialProp = CargarSprite("Mapas/ice/props/ice_crystal");
                    break;
                case "fire":
                    b.framesHazardStrip = new Sprite[4];
                    for (int i = 0; i < 4; i++) b.framesHazardStrip[i] = CargarSprite($"Mapas/fire/props/fire_lava_f{i}");
                    b.framesSpecialAnim = new Sprite[4];
                    for (int i = 0; i < 4; i++) b.framesSpecialAnim[i] = CargarSprite($"Mapas/fire/props/fire_brazier_f{i}");
                    b.spriteSpecialProp = b.framesSpecialAnim[0];
                    break;
                case "desert":
                    b.framesHazardStrip = new Sprite[4];
                    for (int i = 0; i < 4; i++) b.framesHazardStrip[i] = CargarSprite($"Mapas/desert/props/desert_quicksand_f{i}");
                    b.spriteSpecialProp = CargarSprite("Mapas/desert/props/desert_cactus");
                    break;
            }

            AsegurarFallbacks(b, prefijo);

            return b;
        }

        private static Sprite CargarSprite(string rutaRelativa)
        {
            var sp = Resources.Load<Sprite>(rutaRelativa);
            if (sp != null) return sp;

#if UNITY_EDITOR
            string rutaEditor = "Assets/Resources/" + rutaRelativa + ".png";
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(rutaEditor);
            if (sp != null) return sp;

            string rutaArte = "Assets/Arte/Mapas/dungeon_tiles/" + (rutaRelativa.StartsWith("Mapas/") ? rutaRelativa.Substring(6) : rutaRelativa) + ".png";
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(rutaArte);
            if (sp != null) return sp;
#endif

            try
            {
                string pathDisco = System.IO.Path.Combine(Application.dataPath, "Resources", rutaRelativa.Replace('/', System.IO.Path.DirectorySeparatorChar) + ".png");
                if (System.IO.File.Exists(pathDisco))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(pathDisco);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    if (tex.LoadImage(bytes))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
                    }
                }
            }
            catch { }

            return null;
        }

        private static void AsegurarFallbacks(DatosBioma b, string prefijo)
        {
            Sprite fallbackSuelo = b.spriteSuelo ?? CrearSpriteSolido(ObtenerColorBaseBioma(prefijo));
            for (int i = 0; i < 8; i++)
            {
                if (b.spritesSuelo[i] == null) b.spritesSuelo[i] = fallbackSuelo;
            }
            if (b.spriteSuelo == null) b.spriteSuelo = fallbackSuelo;

            Sprite fallbackMuro = b.spriteMuro ?? CrearSpriteSolido(ObtenerColorMuroBioma(prefijo));
            for (int i = 0; i < 16; i++)
            {
                if (b.spritesWallTop[i] == null) b.spritesWallTop[i] = fallbackMuro;
            }
            if (b.spriteMuro == null) b.spriteMuro = fallbackMuro;

            if (b.spriteWallFaceA == null) b.spriteWallFaceA = fallbackMuro;
            if (b.spriteWallFaceB == null) b.spriteWallFaceB = b.spriteWallFaceA;
            if (b.spriteWallFaceCracked == null) b.spriteWallFaceCracked = b.spriteWallFaceA;
            if (b.spriteWallFaceDecor == null) b.spriteWallFaceDecor = b.spriteWallFaceA;

            if (b.framesWallFaceTorch == null || b.framesWallFaceTorch.Length == 0 || b.framesWallFaceTorch[0] == null)
            {
                b.framesWallFaceTorch = new Sprite[] { fallbackMuro };
            }
            if (b.spriteLlamaAntorcha == null) b.spriteLlamaAntorcha = b.framesWallFaceTorch[0];
            if (b.spritePillar == null) b.spritePillar = fallbackMuro;
            if (b.spriteColumna == null) b.spriteColumna = b.spritePillar;
        }

        private static Sprite CrearSpriteSolido(Color c)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Color ObtenerColorBaseBioma(string prefijo)
        {
            switch (prefijo)
            {
                case "stone": return new Color(0.25f, 0.26f, 0.32f);
                case "plant": return new Color(0.18f, 0.35f, 0.22f);
                case "ice": return new Color(0.24f, 0.42f, 0.62f);
                case "fire": return new Color(0.36f, 0.18f, 0.14f);
                case "desert": return new Color(0.58f, 0.46f, 0.28f);
                default: return Color.gray;
            }
        }

        private static Color ObtenerColorMuroBioma(string prefijo)
        {
            switch (prefijo)
            {
                case "stone": return new Color(0.16f, 0.17f, 0.22f);
                case "plant": return new Color(0.12f, 0.24f, 0.14f);
                case "ice": return new Color(0.16f, 0.28f, 0.45f);
                case "fire": return new Color(0.24f, 0.10f, 0.08f);
                case "desert": return new Color(0.42f, 0.32f, 0.18f);
                default: return new Color(0.25f, 0.25f, 0.25f);
            }
        }
    }
}
