using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;

namespace SoulKnight.Editor
{
    public static class GeneradorProyecto
    {
        [MenuItem("SoulKnight/Reconstruir Todo el Proyecto")]
        public static void ReconstruirTodo()
        {
            Debug.Log("== Iniciando Reconstrucción Completa de SoulKnight2 en Unity ==");

            CrearDirectorios();
            ConfigurarTexturas();
            CrearPrefabsRed();
            CrearEscenas();
            ConfigurarBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("== Reconstrucción Completa Exitosa ==");
        }

        private static void CrearDirectorios()
        {
            string[] dirs = new string[]
            {
                "Assets/Prefabs",
                "Assets/Prefabs/Personajes",
                "Assets/Prefabs/Proyectiles",
                "Assets/Prefabs/Enemigos",
                "Assets/Prefabs/Red",
                "Assets/Escenas",
                "Assets/Resources"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        private static void ConfigurarTexturas()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Arte", "Assets/Resources" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool modificado = false;
                    float ppuDeseado = 32f;
                    if (path.IndexOf("dungeon_tiles", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        path.IndexOf("Mapas", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ppuDeseado = 16f;
                    }
                    else if (path.IndexOf("Gato", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ppuDeseado = 18f;
                    }
                    if (importer.textureType != TextureImporterType.Sprite || Mathf.Abs(importer.spritePixelsPerUnit - ppuDeseado) > 0.01f)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = ppuDeseado;
                        modificado = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point; // Pixel art estilo Soul Knight sin blur
                        modificado = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        modificado = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        modificado = true;
                    }

                    // Asegurar que en Standalone no se aplique compresión por plataforma
                    var standSettings = importer.GetPlatformTextureSettings("Standalone");
                    if (standSettings.overridden || standSettings.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        standSettings.overridden = true;
                        standSettings.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SetPlatformTextureSettings(standSettings);
                        modificado = true;
                    }

                    if (modificado)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static void CrearPrefabsRed()
        {
            // 1. Proyectil Bola de Fuego
            CrearPrefabProyectil("BolaFuego", "Assets/Arte/Personajes/Fuego/BolaFuego.png", true, false);

            // 2. Proyectil Bola de Hielo
            CrearPrefabProyectil("BolaHielo", "Assets/Arte/Personajes/Hielo/BolaHielo/BolaHielo_00.png", false, true);

            // 3. Zona Era de Hielo
            CrearPrefabZonaEraHielo();

            // 4. SalaRed
            CrearPrefabSalaRed();

            // 5. Personajes
            var neutralPrefab = CrearPrefabPersonajeNeutral();
            var fuegoPrefab = CrearPrefabPersonajeFuego();
            var hieloPrefab = CrearPrefabPersonajeHielo();
            var elecPrefab = CrearPrefabPersonajeElectrico();

            // 6. Enemigos y Proyectil Enemigo
            var proyectilMagoPrefab = CrearPrefabProyectilMago();
            var enemigoPrefab = CrearPrefabEnemigo();
            var enemigoMagoPrefab = CrearPrefabEnemigoMago(proyectilMagoPrefab);
            var enemigoSlimePrefab = CrearPrefabEnemigoSlime();

            // 7. Jefes de Mazmorra
            var jefeSlimePrefab = CrearPrefabJefeSlimeRey(proyectilMagoPrefab);
            var jefeMagoPrefab = CrearPrefabJefeGranBrujo(proyectilMagoPrefab);
            var jefeGolemPrefab = CrearPrefabJefeGolemColosal(proyectilMagoPrefab);

            // 8. Item Mejora Run (Loot)
            var itemLootPrefab = CrearPrefabItemMejoraRun();

            // 9. Global (NetworkManager + Prefab Registration)
            CrearPrefabGlobal(new List<GameObject>
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Proyectiles/BolaFuego.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Proyectiles/BolaHielo.prefab"),
                proyectilMagoPrefab,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combate/ZonaEraHielo.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Red/SalaRed.prefab"),
                neutralPrefab,
                fuegoPrefab,
                hieloPrefab,
                elecPrefab,
                enemigoPrefab,
                enemigoMagoPrefab,
                enemigoSlimePrefab,
                jefeSlimePrefab,
                jefeMagoPrefab,
                jefeGolemPrefab,
                itemLootPrefab
            });
        }

        private static GameObject CrearPrefabItemMejoraRun()
        {
            var go = new GameObject("ItemMejoraRun");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/UI/IconoCorazon.png") 
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoBolaHielo.png");
            sr.sortingOrder = 6;
            go.transform.localScale = Vector3.one * 0.75f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;

            go.AddComponent<NetworkObject>();
            go.AddComponent<ItemMejoraRun>();

            if (!Directory.Exists("Assets/Prefabs/Loot")) Directory.CreateDirectory("Assets/Prefabs/Loot");
            string path = "Assets/Prefabs/Loot/ItemMejoraRun.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static void CrearPrefabProyectil(string nombre, string rutaSprite, bool quemadura, bool congelacion)
        {
            var go = new GameObject(nombre);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(rutaSprite);
            sr.sortingOrder = 8;

            go.transform.localScale = Vector3.one * 0.65f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var netObj = go.AddComponent<NetworkObject>();
            var proj = go.AddComponent<Proyectil>();
            proj.aplicaQuemadura = quemadura;
            proj.aplicaCongelacion = congelacion;
            if (congelacion)
            {
                proj.tieneExplosionArea = true;
                proj.radioArea = 3.2f;
                proj.probCongelacion = 1f;
            }

            string path = $"Assets/Prefabs/Proyectiles/{nombre}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
        }

        private static void CrearPrefabZonaEraHielo()
        {
            var go = new GameObject("ZonaEraHielo");
            go.transform.localScale = Vector3.one * 1f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 5.8f; // Radio expandido de la ulti de hielo

            go.AddComponent<NetworkObject>();
            var zona = go.AddComponent<ZonaEraHielo>();

            var spriteHielo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoEraHielo.png");

            // Capa 1: Fondo de Aura Glacial
            var auraObj = new GameObject("FondoAura");
            auraObj.transform.SetParent(go.transform, false);
            var auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = spriteHielo;
            auraSr.color = new Color(0.12f, 0.55f, 1f, 0.4f);
            auraSr.sortingOrder = 1;
            auraObj.transform.localScale = Vector3.one * 10.5f;

            // Capa 2: Vórtice Interior Giratorio
            var vorticeObj = new GameObject("VorticeInterior");
            vorticeObj.transform.SetParent(go.transform, false);
            var vorticeSr = vorticeObj.AddComponent<SpriteRenderer>();
            vorticeSr.sprite = spriteHielo;
            vorticeSr.color = new Color(0.6f, 0.95f, 1f, 0.85f);
            vorticeSr.sortingOrder = 2;
            vorticeObj.transform.localScale = Vector3.one * 7.5f;

            // Capa 3: Anillo Exterior con Estacas y Cristales
            var anilloObj = new GameObject("AnilloExterior");
            anilloObj.transform.SetParent(go.transform, false);
            anilloObj.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            var anilloSr = anilloObj.AddComponent<SpriteRenderer>();
            anilloSr.sprite = spriteHielo;
            anilloSr.color = new Color(0.85f, 1f, 1f, 0.9f);
            anilloSr.sortingOrder = 3;
            anilloObj.transform.localScale = Vector3.one * 11.2f;

            // Capa 4: Destello de Pulso de Tick
            var destelloObj = new GameObject("DestelloTick");
            destelloObj.transform.SetParent(go.transform, false);
            var destelloSr = destelloObj.AddComponent<SpriteRenderer>();
            destelloSr.sprite = spriteHielo;
            destelloSr.color = new Color(1f, 1f, 1f, 0f);
            destelloSr.sortingOrder = 4;
            destelloObj.transform.localScale = Vector3.one * 12.0f;

            zona.fondoAura = auraSr;
            zona.vorticeInterior = vorticeObj.transform;
            zona.anilloExterior = anilloObj.transform;
            zona.destelloTick = destelloSr;

            if (!Directory.Exists("Assets/Prefabs/Combate")) Directory.CreateDirectory("Assets/Prefabs/Combate");
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Combate/ZonaEraHielo.prefab");
            GameObject.DestroyImmediate(go);
        }

        private static void CrearPrefabSalaRed()
        {
            var go = new GameObject("SalaRed");
            go.AddComponent<NetworkObject>();
            go.AddComponent<SalaRed>();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Red/SalaRed.prefab");
            GameObject.DestroyImmediate(go);
        }

        private static GameObject CrearPrefabPersonajeFuego()
        {
            var go = new GameObject("PersonajeFuego");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/PersonajeFuego.png");
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<ClientNetworkTransform>();
            go.AddComponent<NivelEnPartida>();
            var pf = go.AddComponent<PersonajeFuego>();
            pf.prefabBolaFuego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Proyectiles/BolaFuego.prefab");

            // LineRenderer para Cabeza de Fuego
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.red;
            lr.endColor = Color.yellow;
            lr.enabled = false;
            pf.rayoCabezaFuego = lr;

            string path = "Assets/Prefabs/Personajes/PersonajeFuego.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabPersonajeHielo()
        {
            var go = new GameObject("PersonajeHielo");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0001.png");
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.8f);
            col.offset = new Vector2(0f, -0.08f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<ClientNetworkTransform>();
            go.AddComponent<NivelEnPartida>();
            var ph = go.AddComponent<PersonajeHielo>();
            ph.prefabBolaHielo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Proyectiles/BolaHielo.prefab");
            ph.prefabEraHielo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combate/ZonaEraHielo.prefab");

            // Cargar sprites de animación del gato de hielo
            ph.spritesAbajo = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0001.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0002.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0003.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0004.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0005.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Abajo/South_0006.png")
            };
            ph.spritesArriba = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0001.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0002.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0003.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0004.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0005.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Arriba/North_0006.png")
            };
            ph.spritesLado = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0001.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0002.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0003.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0004.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0005.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Izquierda/West_0006.png")
            };

            // Animaciones de Reposo (sentado):
            // Transición de pie a sentado: SentadoEmpieza_0006 -> SentadoEmpieza_0001
            ph.spritesSeSienta = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0006.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0005.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0004.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0003.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0002.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoEmpieza_0001.png")
            };

            // Ciclo continuo sentado: SentadoInfinito_0001 -> SentadoInfinito_0006
            ph.spritesEstaSentado = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0001.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0002.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0003.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0004.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0005.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Gato/Reposo/SentadoInfinito_0006.png")
            };

            ph.fpsAnimacion = 8f;
            ph.fpsSentarse = 7f;
            ph.fpsSentado = 5f;
            ph.tiempoParaSentarse = 0.8f;

            // Rayo de hielo: Halo exterior cian glacial
            var haloObj = new GameObject("HaloRayoHielo");
            haloObj.transform.SetParent(go.transform, false);
            var lrHalo = haloObj.AddComponent<LineRenderer>();
            lrHalo.startWidth = 0.65f;
            lrHalo.endWidth = 0.65f;
            lrHalo.material = new Material(Shader.Find("Sprites/Default"));
            lrHalo.startColor = new Color(0.15f, 0.65f, 1f, 0.45f);
            lrHalo.endColor = new Color(0.35f, 0.85f, 1f, 0.45f);
            lrHalo.sortingOrder = 9;
            lrHalo.positionCount = 12;
            lrHalo.enabled = false;
            ph.rayoHieloHalo = lrHalo;

            // Rayo de hielo: Núcleo interior blanco diamante resplandeciente
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.22f;
            lr.endWidth = 0.22f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.95f, 1f, 1f, 0.98f);
            lr.endColor = new Color(0.85f, 0.98f, 1f, 0.98f);
            lr.sortingOrder = 10;
            lr.positionCount = 12;
            lr.enabled = false;
            ph.rayoHielo = lr;

            string path = "Assets/Prefabs/Personajes/PersonajeHielo.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabPersonajeElectrico()
        {
            var go = new GameObject("PersonajeElectrico");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/PersonajeElectrico.png");
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<ClientNetworkTransform>();
            go.AddComponent<NivelEnPartida>();
            var pe = go.AddComponent<PersonajeElectrico>();

            // LineRenderer para Rayo Eléctrico (Ultimate)
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.35f;
            lr.endWidth = 0.35f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.white;
            lr.sortingOrder = 10;
            lr.positionCount = 2;
            lr.enabled = false;
            pe.visualRayoElectrico = lr;

            // Visual Espada Eléctrica (LineRenderer animado en arco)
            var espadaObj = new GameObject("VisualEspada");
            espadaObj.transform.SetParent(go.transform, false);
            var lrEspada = espadaObj.AddComponent<LineRenderer>();
            lrEspada.startWidth = 0.30f;
            lrEspada.endWidth = 0.05f;
            lrEspada.material = new Material(Shader.Find("Sprites/Default"));
            lrEspada.startColor = new Color(0.3f, 0.9f, 1f, 1f);
            lrEspada.endColor = Color.white;
            lrEspada.sortingOrder = 11;
            espadaObj.SetActive(false);
            pe.visualEspada = espadaObj;

            // Visual Carga Energética del Rayo (Efecto crepitante de 2s)
            var cargaObj = new GameObject("VisualCargaRayo");
            cargaObj.transform.SetParent(go.transform, false);
            var lrCarga = cargaObj.AddComponent<LineRenderer>();
            lrCarga.startWidth = 0.14f;
            lrCarga.endWidth = 0.14f;
            lrCarga.material = new Material(Shader.Find("Sprites/Default"));
            lrCarga.startColor = Color.yellow;
            lrCarga.endColor = new Color(0.2f, 0.9f, 1f, 0.9f);
            lrCarga.sortingOrder = 11;
            lrCarga.enabled = false;
            pe.visualCargaRayo = lrCarga;

            string path = "Assets/Prefabs/Personajes/PersonajeElectrico.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabPersonajeNeutral()
        {
            var go = new GameObject("PersonajeNeutral");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Neutral/PersonajeNeutral.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/PersonajeFuego.png");
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<ClientNetworkTransform>();
            go.AddComponent<NivelEnPartida>();
            go.AddComponent<PersonajeNeutral>();

            string path = "Assets/Prefabs/Personajes/PersonajeNeutral.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabProyectilMago()
        {
            var go = new GameObject("ProyectilMago");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/ProyectilMago.png");
            sr.sortingOrder = 8;
            go.transform.localScale = Vector3.one * 0.70f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.32f;

            var netObj = go.AddComponent<NetworkObject>();
            var proj = go.AddComponent<Proyectil>();
            proj.velocidad = 10f;
            proj.dano = 12;
            proj.tiempoVida = 6f;
            proj.proyectilEnemigo = true;

            string path = "Assets/Prefabs/Proyectiles/ProyectilMago.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabEnemigo()
        {
            var go = new GameObject("Enemigo1");
            go.tag = "Enemigos";
            // Escala 0.30f para normalizar el sprite (160x160 px a 32 PPU = 5 unidades -> 1.5 unidades, proporcional al jugador)
            go.transform.localScale = new Vector3(0.30f, 0.30f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/BarrilPeces.png");
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.5f, 3.8f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var en = go.AddComponent<EnemigoBase>();

            // Sprite congelado encima
            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.2f, 0);
            congObj.transform.localScale = Vector3.one * 4.5f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            en.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/Enemigo1.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabEnemigoMago(GameObject proyectilPrefab)
        {
            var go = new GameObject("EnemigoMago");
            go.tag = "Enemigos";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/EnemigoMago.png");
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 1.35f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var em = go.AddComponent<EnemigoMago>();
            em.prefabProyectil = proyectilPrefab;

            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            congObj.transform.localScale = Vector3.one * 1.3f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            em.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/EnemigoMago.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabEnemigoSlime()
        {
            var go = new GameObject("EnemigoSlime");
            go.tag = "Enemigos";

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/EnemigoSlime.png");
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.48f;

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var es = go.AddComponent<EnemigoSlime>();

            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            congObj.transform.localScale = Vector3.one * 1.1f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            es.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/EnemigoSlime.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabJefeSlimeRey(GameObject projPrefab)
        {
            var go = new GameObject("JefeSlimeRey");
            go.tag = "Enemigos";
            go.transform.localScale = Vector3.one * 2.4f; // Jefe gigante

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/EnemigoSlime.png");
            sr.color = new Color(0.2f, 0.95f, 0.35f, 1f); // Verde esmeralda regio
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.52f;

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var jefe = go.AddComponent<JefeBase>();
            jefe.tipoJefe = TipoJefe.SlimeRey;
            jefe.nombreJefe = "Rey Slime de Ácido";
            jefe.vidaMax = 650;
            jefe.dano = 22;
            jefe.velocidadBase = 2.2f;
            jefe.prefabProyectilJefe = projPrefab;

            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            congObj.transform.localScale = Vector3.one * 1.3f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            jefe.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/JefeSlimeRey.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabJefeGranBrujo(GameObject projPrefab)
        {
            var go = new GameObject("JefeGranBrujo");
            go.tag = "Enemigos";
            go.transform.localScale = Vector3.one * 1.85f; // Mago imponente

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/EnemigoMago.png");
            sr.color = new Color(0.9f, 0.35f, 1f, 1f); // Púrpura nigromante oscuro
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.95f, 1.4f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var jefe = go.AddComponent<JefeBase>();
            jefe.tipoJefe = TipoJefe.GranBrujo;
            jefe.nombreJefe = "Gran Brujo del Vórtice";
            jefe.vidaMax = 550;
            jefe.dano = 18;
            jefe.velocidadBase = 1.9f;
            jefe.prefabProyectilJefe = projPrefab;

            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            congObj.transform.localScale = Vector3.one * 1.4f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            jefe.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/JefeGranBrujo.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CrearPrefabJefeGolemColosal(GameObject projPrefab)
        {
            var go = new GameObject("JefeGolemColosal");
            go.tag = "Enemigos";
            go.transform.localScale = new Vector3(0.65f, 0.65f, 1f); // Golem colosal masivo

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Enemigos/BarrilPeces.png");
            sr.color = new Color(0.85f, 0.3f, 0.2f, 1f); // Titán volcánico ígneo
            sr.sortingOrder = 3;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3.6f, 3.8f);

            go.AddComponent<NetworkObject>();
            go.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            var jefe = go.AddComponent<JefeBase>();
            jefe.tipoJefe = TipoJefe.GolemColosal;
            jefe.nombreJefe = "Golem Colosal de Roca";
            jefe.vidaMax = 800;
            jefe.dano = 28;
            jefe.velocidadBase = 1.5f;
            jefe.prefabProyectilJefe = projPrefab;

            var congObj = new GameObject("SpriteCongelado");
            congObj.transform.SetParent(go.transform);
            congObj.transform.localPosition = new Vector3(0, 0.2f, 0);
            congObj.transform.localScale = Vector3.one * 5.0f;
            var congSr = congObj.AddComponent<SpriteRenderer>();
            congSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Efectos/Congelado.png");
            congSr.sortingOrder = 4;
            congObj.SetActive(false);
            jefe.spriteCongelado = congObj;

            string path = "Assets/Prefabs/Enemigos/JefeGolemColosal.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            GameObject.DestroyImmediate(go);
            return prefab;
        }

        private static void CrearPrefabGlobal(List<GameObject> prefabsRed)
        {
            var go = new GameObject("Global");
            var nm = go.AddComponent<NetworkManager>();
            var utp = go.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport = utp;
            nm.NetworkConfig.EnableSceneManagement = true;
            nm.NetworkConfig.ConnectionApproval = true;

            var global = go.AddComponent<Global>();
            var serializedObj = new SerializedObject(global);
            serializedObj.FindProperty("prefabSalaRed").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Red/SalaRed.prefab");

            var prefabsProp = serializedObj.FindProperty("prefabsRed");
            prefabsProp.ClearArray();
            for (int i = 0; i < prefabsRed.Count; i++)
            {
                if (prefabsRed[i] == null) continue;
                prefabsProp.InsertArrayElementAtIndex(i);
                prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = prefabsRed[i];
            }
            serializedObj.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/Global.prefab");
            GameObject.DestroyImmediate(go);
        }

        private static void CrearEscenas()
        {
            CrearEscenaMenuPrincipal();
            CrearEscenaPedirNombre();
            CrearEscenaMenuJugar();
            CrearEscenaCrearSala();
            CrearEscenaUnirseSala();
            CrearEscenaLobby();
            CrearEscenaJuego();
        }

        private static void ConfigurarCanvas(GameObject root, out Canvas canvas, out GameObject panel)
        {
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private static Button CrearBoton(Transform parent, string texto, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Boton_" + texto);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.3f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();

            var txtGo = new GameObject("Texto");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.text = texto;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txtGo.GetComponent<RectTransform>().sizeDelta = size;

            return btn;
        }

        private static InputField CrearInput(Transform parent, string placeholder, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var inp = go.AddComponent<InputField>();

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txtGo.GetComponent<RectTransform>().sizeDelta = size - new Vector2(20, 0);

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<Text>();
            ph.font = FuenteUtil.ObtenerFuentePixelify();
            ph.fontSize = 20;
            ph.text = placeholder;
            ph.color = Color.gray;
            ph.alignment = TextAnchor.MiddleLeft;
            phGo.GetComponent<RectTransform>().sizeDelta = size - new Vector2(20, 0);

            inp.textComponent = txt;
            inp.placeholder = ph;

            return inp;
        }

        private static Text CrearTexto(Transform parent, string contenido, Vector2 anchoredPos, int tamano, Color color)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(600, 60);

            var txt = go.AddComponent<Text>();
            txt.font = FuenteUtil.ObtenerFuentePixelify();
            txt.text = contenido;
            txt.fontSize = tamano;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            return txt;
        }

        private static void CrearEscenaMenuPrincipal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("UI_MenuPrincipal");
            ConfigurarCanvas(root, out _, out var panel);

            CrearTexto(panel.transform, "SOUL KNIGHT 2", new Vector2(0, 160), 48, Color.yellow);
            var btnJugar = CrearBoton(panel.transform, "JUGAR", new Vector2(0, 40), new Vector2(260, 50));
            var btnOpciones = CrearBoton(panel.transform, "OPCIONES", new Vector2(0, -30), new Vector2(260, 50));
            var btnSalir = CrearBoton(panel.transform, "SALIR", new Vector2(0, -100), new Vector2(260, 50));
            var txtMsg = CrearTexto(panel.transform, "", new Vector2(0, -180), 20, Color.white);

            // Botón Cambiar Nombre abajo a la izquierda
            var btnCambiarNombre = CrearBoton(panel.transform, "CAMBIAR NOMBRE", Vector2.zero, new Vector2(290, 44));
            var rtCambiarNom = btnCambiarNombre.GetComponent<RectTransform>();
            rtCambiarNom.anchorMin = new Vector2(0f, 0f);
            rtCambiarNom.anchorMax = new Vector2(0f, 0f);
            rtCambiarNom.pivot = new Vector2(0f, 0f);
            rtCambiarNom.anchoredPosition = new Vector2(24f, 24f);
            var lblCambiarNom = btnCambiarNombre.GetComponentInChildren<Text>();
            if (lblCambiarNom != null) lblCambiarNom.fontSize = 15;

            // Panel de Opciones de Controles Remapeables
            var panelOpcionesObj = new GameObject("PanelOpcionesControles");
            panelOpcionesObj.transform.SetParent(root.transform, false);
            var imgOpc = panelOpcionesObj.AddComponent<Image>();
            imgOpc.color = new Color(0.04f, 0.04f, 0.08f, 0.95f);
            var rtOpc = panelOpcionesObj.GetComponent<RectTransform>();
            rtOpc.anchorMin = new Vector2(0.5f, 0.5f);
            rtOpc.anchorMax = new Vector2(0.5f, 0.5f);
            rtOpc.sizeDelta = new Vector2(580, 560);
            panelOpcionesObj.SetActive(false);

            CrearTexto(panelOpcionesObj.transform, "⚙️ CONTROLES Y TECLAS", new Vector2(0, 240), 24, Color.yellow);
            var txtEstOpc = CrearTexto(panelOpcionesObj.transform, "Haz click en un botón para cambiar la tecla.", new Vector2(0, 205), 14, Color.white);

            var contFilas = new GameObject("ContenedorFilas");
            contFilas.transform.SetParent(panelOpcionesObj.transform, false);
            var rtFilas = contFilas.AddComponent<RectTransform>();
            rtFilas.anchoredPosition = new Vector2(0, -10);
            rtFilas.sizeDelta = new Vector2(520, 360);
            var vlgFilas = contFilas.AddComponent<VerticalLayoutGroup>();
            vlgFilas.childControlWidth = true;
            vlgFilas.childForceExpandHeight = false;
            vlgFilas.spacing = 6;

            var btnRestaurar = CrearBoton(panelOpcionesObj.transform, "RESTAURAR DEFECTO", new Vector2(-120, -235), new Vector2(200, 38));
            var btnCerrar = CrearBoton(panelOpcionesObj.transform, "GUARDAR Y SALIR", new Vector2(120, -235), new Vector2(200, 38));

            var panelOpcComp = panelOpcionesObj.AddComponent<PanelOpcionesControles>();
            panelOpcComp.panelContenedor = panelOpcionesObj;
            panelOpcComp.contenedorFilas = contFilas.transform;
            panelOpcComp.botonCerrar = btnCerrar;
            panelOpcComp.botonRestaurar = btnRestaurar;
            panelOpcComp.textoEstado = txtEstOpc;

            var ctrl = root.AddComponent<ControladorMenuPrincipal>();
            ctrl.botonJugar = btnJugar;
            ctrl.botonOpciones = btnOpciones;
            ctrl.botonSalir = btnSalir;
            ctrl.botonCambiarNombre = btnCambiarNombre;
            ctrl.textoMensaje = txtMsg;
            ctrl.panelOpciones = panelOpcComp;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/MenuPrincipal.unity");
        }

        private static void CrearEscenaPedirNombre()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("UI_PedirNombre");
            ConfigurarCanvas(root, out _, out var panel);

            CrearTexto(panel.transform, "¿CÓMO TE LLAMAS?", new Vector2(0, 100), 32, Color.white);
            var input = CrearInput(panel.transform, "Introduce tu nombre...", new Vector2(0, 10), new Vector2(300, 45));
            var btnCont = CrearBoton(panel.transform, "CONTINUAR", new Vector2(0, -70), new Vector2(200, 45));

            var ctrl = root.AddComponent<ControladorPedirNombre>();
            ctrl.inputNombre = input;
            ctrl.botonContinuar = btnCont;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/PedirNombre.unity");
        }

        private static void CrearEscenaMenuJugar()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("UI_MenuJugar");
            ConfigurarCanvas(root, out _, out var panel);

            CrearTexto(panel.transform, "MODO MULTIJUGADOR LAN", new Vector2(0, 120), 36, Color.white);
            var btnCrear = CrearBoton(panel.transform, "CREAR SALA", new Vector2(0, 30), new Vector2(260, 50));
            var btnUnirse = CrearBoton(panel.transform, "UNIRSE A SALA", new Vector2(0, -40), new Vector2(260, 50));
            var btnVolver = CrearBoton(panel.transform, "VOLVER", new Vector2(0, -120), new Vector2(200, 40));

            var ctrl = root.AddComponent<ControladorMenuJugar>();
            ctrl.botonCrearSala = btnCrear;
            ctrl.botonUnirseSala = btnUnirse;
            ctrl.botonVolver = btnVolver;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/MenuJugar.unity");
        }

        private static void CrearEscenaCrearSala()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("UI_CrearSala");
            ConfigurarCanvas(root, out _, out var panel);

            CrearTexto(panel.transform, "CONFIGURAR SALA", new Vector2(0, 140), 36, Color.white);
            var inNombre = CrearInput(panel.transform, "Nombre de la sala...", new Vector2(0, 60), new Vector2(320, 45));
            var inPass = CrearInput(panel.transform, "Contraseña (opcional)...", new Vector2(0, 0), new Vector2(320, 45));
            inPass.contentType = InputField.ContentType.Password;

            var btnCrear = CrearBoton(panel.transform, "CREAR", new Vector2(0, -70), new Vector2(200, 45));
            var btnVolver = CrearBoton(panel.transform, "VOLVER", new Vector2(0, -130), new Vector2(160, 35));
            var txtErr = CrearTexto(panel.transform, "", new Vector2(0, -180), 18, Color.red);

            var ctrl = root.AddComponent<ControladorCrearSala>();
            ctrl.inputNombreSala = inNombre;
            ctrl.inputPassword = inPass;
            ctrl.botonConfirmar = btnCrear;
            ctrl.botonVolver = btnVolver;
            ctrl.textoError = txtErr;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/CrearSala.unity");
        }

        private static void CrearEscenaUnirseSala()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("UI_UnirseSala");
            ConfigurarCanvas(root, out _, out var panel);

            CrearTexto(panel.transform, "UNIRSE A SALA", new Vector2(0, 160), 28, Color.white);
            CrearTexto(panel.transform, "Ingresa el Código Secreto de la Sala (ej: SK-...) o Nombre de sala:", new Vector2(0, 115), 16, new Color(0.9f, 0.9f, 0.6f));
            var inNombre = CrearInput(panel.transform, "Código Secreto (SK-...) o Nombre LAN...", new Vector2(0, 60), new Vector2(400, 45));
            var inPass = CrearInput(panel.transform, "Contraseña (si tiene)...", new Vector2(0, 5), new Vector2(320, 45));
            inPass.contentType = InputField.ContentType.Password;

            var btnBuscar = CrearBoton(panel.transform, "CONECTARSE A SALA", new Vector2(0, -60), new Vector2(260, 45));
            var btnVolver = CrearBoton(panel.transform, "VOLVER", new Vector2(0, -120), new Vector2(160, 35));
            var txtEst = CrearTexto(panel.transform, "", new Vector2(0, -175), 18, Color.cyan);

            var ctrl = root.AddComponent<ControladorUnirseSala>();
            ctrl.inputNombreSala = inNombre;
            ctrl.inputPassword = inPass;
            ctrl.botonBuscar = btnBuscar;
            ctrl.botonVolver = btnVolver;
            ctrl.textoEstado = txtEst;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/UnirseSala.unity");
        }

        private static void CrearEscenaLobby()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Cámara principal con CamaraSeguir
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 7.8f; // Vista espaciosa equilibrada
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.gameObject.AddComponent<CamaraSeguir>();
            }

            // 2. Mapa fisico del Salon de Espera usando sprites oficiales de piedra
            var datosStone = CatalogoBiomas.Obtener(TipoBioma.Piedra);
            var spritePortal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/PortalSalida.png");

            var mapaLobby = new GameObject("MapaLobby");

            // Funcion auxiliar para colocar suelo estructurado
            void PonerSuelo(int x, int y, Color? tinte = null)
            {
                var suelo = new GameObject($"Suelo_{x}_{y}");
                suelo.transform.SetParent(mapaLobby.transform);
                suelo.transform.position = new Vector3(x, y, 0);
                var sr = suelo.AddComponent<SpriteRenderer>();

                // Orden estructurado 2x2 de losas de piedra
                int slabX = Mathf.FloorToInt((x + 20f) / 2f);
                int slabY = Mathf.FloorToInt((y + 20f) / 2f);
                bool alterno = ((slabX + slabY) % 2 == 1);
                Sprite sp = alterno ? (datosStone.spritesSuelo[3] ?? datosStone.spritesSuelo[0]) : datosStone.spritesSuelo[0];

                int mx = Mathf.Abs(x + 20) % 6;
                int my = Mathf.Abs(y + 20) % 6;
                if (mx == 2 && my == 3) sp = datosStone.spritesSuelo[1];
                else if (mx == 5 && my == 1) sp = datosStone.spritesSuelo[4];
                else if (mx == 0 && my == 4) sp = datosStone.spritesSuelo[6];

                sr.sprite = sp;
                if (tinte.HasValue) sr.color = tinte.Value;
                sr.sortingOrder = 0;

                // Calcomanias decorativas en orden estructurado
                if (!tinte.HasValue && datosStone.spritesDecals != null && datosStone.spritesDecals.Length > 0)
                {
                    if ((Mathf.Abs(x + 20) % 7 == 3) && (Mathf.Abs(y + 20) % 7 == 4))
                    {
                        int dIdx = Mathf.Abs(x * 3 + y) % datosStone.spritesDecals.Length;
                        var dObj = new GameObject("Decal");
                        dObj.transform.SetParent(suelo.transform, false);
                        var srD = dObj.AddComponent<SpriteRenderer>();
                        srD.sprite = datosStone.spritesDecals[dIdx];
                        srD.sortingOrder = 1;
                    }
                }
            }

            // Funcion auxiliar para colocar muro de 1 tile de grosor
            void CrearMuro(int x, int y, int mask = 10)
            {
                var muro = new GameObject($"Muro_{x}_{y}");
                muro.transform.SetParent(mapaLobby.transform);
                muro.transform.position = new Vector3(x, y, 0);
                var sr = muro.AddComponent<SpriteRenderer>();
                sr.sprite = ObtenerWallTop(datosStone, mask);
                sr.sortingOrder = 2;
                var col = muro.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                try { muro.tag = "Pared"; } catch {}
            }

            // Funcion para colocar cara frontal de muro norte con antorchas y halo fijo
            void CrearWallFaceNorte(int x, int y, bool ponerAntorcha = false)
            {
                var faceObj = new GameObject($"WallFace_{x}_{y}");
                faceObj.transform.SetParent(mapaLobby.transform);
                faceObj.transform.position = new Vector3(x, y, 0);
                var sr = faceObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                var col = faceObj.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
                try { faceObj.tag = "Pared"; } catch {}

                if (ponerAntorcha && datosStone.framesWallFaceTorch != null && datosStone.framesWallFaceTorch.Length > 0)
                {
                    var anim = faceObj.AddComponent<AnimadorSpriteTile>();
                    anim.Configurar(sr, datosStone.framesWallFaceTorch, 8f);

                    // Halo fijo sin deformacion
                    if (datosStone.spriteTorchGlow != null)
                    {
                        var glow = new GameObject("TorchGlow");
                        glow.transform.SetParent(faceObj.transform, false);
                        glow.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                        glow.transform.localScale = Vector3.one;
                        var srGlow = glow.AddComponent<SpriteRenderer>();
                        srGlow.sprite = datosStone.spriteTorchGlow;
                        srGlow.color = Color.white;
                        srGlow.sortingOrder = 3;
                    }
                }
                else
                {
                    int rx = Mathf.Abs(x) % 5;
                    if (rx == 0 && datosStone.spriteWallFaceBanner != null) sr.sprite = datosStone.spriteWallFaceBanner;
                    else if (rx == 2 && datosStone.spriteWallFaceDecor != null) sr.sprite = datosStone.spriteWallFaceDecor;
                    else if (rx == 3 && datosStone.spriteWallFaceCracked != null) sr.sprite = datosStone.spriteWallFaceCracked;
                    else sr.sprite = (x % 2 == 0) ? (datosStone.spriteWallFaceB ?? datosStone.spriteWallFaceA) : datosStone.spriteWallFaceA;
                }

                // Sombra bajo el muro norte
                var shObj = new GameObject($"Sombra_{x}_{y - 1}");
                shObj.transform.SetParent(mapaLobby.transform);
                shObj.transform.position = new Vector3(x, y - 1, 0);
                var srSh = shObj.AddComponent<SpriteRenderer>();
                srSh.sprite = datosStone.spriteFloorShadow00 ?? datosStone.spriteSuelo;
                srSh.sortingOrder = 1;
            }

            // Salon Central (X de -10 a 10, Y de -12 a 12)
            for (int x = -10; x <= 10; x++)
            {
                for (int y = -12; y <= 12; y++)
                {
                    PonerSuelo(x, y);
                }
            }

            // Muros Norte del Salon Central (fila 13 muro top, fila 12 cara frontal)
            for (int x = -11; x <= 11; x++)
            {
                if (x < -2 || x > 2)
                {
                    CrearMuro(x, 13, 10);
                    bool torch = (Mathf.Abs(x) % 4 == 0);
                    CrearWallFaceNorte(x, 12, torch);
                }
                CrearMuro(x, -13, 10); // Muro sur (1 tile)
            }

            // Alcoba del Portal al Norte: X de -2 a 2, Y de 13 a 16
            for (int x = -2; x <= 2; x++)
            {
                for (int y = 13; y <= 16; y++)
                {
                    PonerSuelo(x, y, new Color(0.85f, 0.95f, 1f));
                }
                CrearMuro(x, 17, 10);
                bool torchAlcoba = (x == -2 || x == 2);
                CrearWallFaceNorte(x, 16, torchAlcoba);
            }
            CrearMuro(-3, 14, 5); CrearMuro(-3, 15, 5); CrearMuro(-3, 16, 5);
            CrearMuro(3, 14, 5);  CrearMuro(3, 15, 5);  CrearMuro(3, 16, 5);

            // Columnas / Pilares de piedra oficiales (16x32)
            void CrearColumna(int x, int y)
            {
                var colObj = new GameObject($"Pilar_{x}_{y}");
                colObj.transform.SetParent(mapaLobby.transform);
                colObj.transform.position = new Vector3(x, y, 0);
                var sr = colObj.AddComponent<SpriteRenderer>();
                sr.sprite = datosStone.spritePillar;
                sr.sortingOrder = 4;
                var col = colObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.75f, 0.625f);
                col.offset = new Vector2(0, -0.4f);
                try { colObj.tag = "Pared"; } catch {}
            }
            CrearColumna(-7, 8);
            CrearColumna(7, 8);
            CrearColumna(-7, -8);
            CrearColumna(7, -8);

            // Props decorativos en el lobby
            void CrearPropLobby(int x, int y, Sprite sp)
            {
                var pObj = new GameObject($"Prop_{x}_{y}");
                pObj.transform.SetParent(mapaLobby.transform);
                pObj.transform.position = new Vector3(x, y, 0);
                var sr = pObj.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = 3;
                var col = pObj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.8f);
                try { pObj.tag = "Pared"; } catch {}
                var prop = pObj.AddComponent<PropDestruible>();
                prop.vidaMax = 10;
            }
            CrearPropLobby(-8, 11, datosStone.spriteCrate);
            CrearPropLobby(-9, 11, datosStone.spriteBarrel);
            CrearPropLobby(8, 11, datosStone.spritePot);
            CrearPropLobby(9, 11, datosStone.spriteCrate);

            // Funcion constructora de cuartos fisicos
            void ConstruirCuartoFisico(string nombre, TipoPersonaje tipo, bool esProx, int xMin, int xMax, int yMin, int yMax, int puertaY1, int puertaY2, bool esOeste, Color tinteSuelo, string textoCartel)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        PonerSuelo(x, y, tinteSuelo);
                    }
                }

                // Muros perimetrales del cuarto (1 tile exacto de grosor)
                int paredExtX = esOeste ? xMin - 1 : xMax + 1;
                for (int y = yMin - 1; y <= yMax + 1; y++)
                {
                    CrearMuro(paredExtX, y, 5);
                }
                for (int x = xMin; x <= xMax; x++)
                {
                    CrearMuro(x, yMax + 1, 10);
                    CrearWallFaceNorte(x, yMax, (x == xMin + 2 || x == xMax - 2));
                    CrearMuro(x, yMin - 1, 10);
                }

                int paredSalidaX = esOeste ? xMax + 1 : xMin - 1;
                for (int y = yMin; y <= yMax; y++)
                {
                    if (y < puertaY1 || y > puertaY2)
                    {
                        CrearMuro(paredSalidaX, y, 5);
                    }
                }

                for (int y = puertaY1; y <= puertaY2; y++)
                {
                    PonerSuelo(paredSalidaX, y, tinteSuelo);
                }

                var cuartoObj = new GameObject($"Trigger_{nombre}");
                cuartoObj.transform.SetParent(mapaLobby.transform);
                float ancho = (xMax - xMin + 1) * 0.75f;
                float alto = (yMax - yMin + 1) * 0.75f;
                float centroX = esOeste ? ((xMin + xMax) * 0.5f - 0.6f) : ((xMin + xMax) * 0.5f + 0.6f);
                float centroY = (yMin + yMax) * 0.5f;
                cuartoObj.transform.position = new Vector3(centroX, centroY, 0);

                var boxCol = cuartoObj.AddComponent<BoxCollider2D>();
                boxCol.isTrigger = true;
                boxCol.size = new Vector2(ancho, alto);

                var compCuarto = cuartoObj.AddComponent<CuartoFisicoLobby>();
                compCuarto.personaje = tipo;
                compCuarto.esProximamente = esProx;
                compCuarto.nombreCuarto = nombre;

                if (!esProx)
                {
                    var prevObj = new GameObject($"Estatua_{nombre}");
                    prevObj.transform.SetParent(mapaLobby.transform);
                    prevObj.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0);
                    var srPrev = prevObj.AddComponent<SpriteRenderer>();
                    srPrev.sprite = tipo switch
                    {
                        TipoPersonaje.Fuego => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/PersonajeFuego.png"),
                        TipoPersonaje.Hielo => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Caminar/HieloCaminar_00.png"),
                        TipoPersonaje.Electrico => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/PersonajeElectrico.png"),
                        _ => null
                    };
                    srPrev.sortingOrder = 3;
                }

                var cartelObj = new GameObject($"Cartel_{nombre}");
                cartelObj.transform.SetParent(mapaLobby.transform);
                float cartelX = (xMin + xMax) * 0.5f;
                float cartelY = yMax - 0.25f;
                cartelObj.transform.position = new Vector3(cartelX, cartelY, 0f);

                var colCartel = cartelObj.AddComponent<BoxCollider2D>();
                colCartel.size = new Vector2(4.8f, 0.9f);
                try { cartelObj.tag = "Pared"; } catch {}

                var bordeCartel = new GameObject("BordeCartel");
                bordeCartel.transform.SetParent(cartelObj.transform, false);
                bordeCartel.transform.localScale = new Vector3(5.1f, 1.25f, 1f);
                var srBorde = bordeCartel.AddComponent<SpriteRenderer>();
                srBorde.sprite = datosStone.spritesSuelo[0];
                Color colorBorde = esProx ? new Color(0.4f, 0.45f, 0.55f, 0.9f) : (tipo == TipoPersonaje.Fuego ? new Color(1f, 0.45f, 0.1f, 0.95f) : (tipo == TipoPersonaje.Hielo ? new Color(0.2f, 0.8f, 1f, 0.95f) : new Color(1f, 0.9f, 0.2f, 0.95f)));
                srBorde.color = colorBorde;
                srBorde.sortingOrder = 7;

                var fondoCartel = new GameObject("FondoCartel");
                fondoCartel.transform.SetParent(cartelObj.transform, false);
                fondoCartel.transform.localScale = new Vector3(4.8f, 1.05f, 1f);
                var srFondo = fondoCartel.AddComponent<SpriteRenderer>();
                srFondo.sprite = datosStone.spritesSuelo[0];
                srFondo.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
                srFondo.sortingOrder = 8;

                var textoObj = new GameObject("TextoCartel");
                textoObj.transform.SetParent(cartelObj.transform, false);
                textoObj.transform.localPosition = new Vector3(0, 0, -0.1f);
                var tm = textoObj.AddComponent<TextMesh>();
                tm.text = textoCartel;
                tm.fontSize = 26;
                tm.characterSize = 0.105f;
                tm.fontStyle = FontStyle.Bold;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = esProx ? new Color(0.85f, 0.88f, 0.92f) : (tipo == TipoPersonaje.Fuego ? new Color(1f, 0.72f, 0.35f) : (tipo == TipoPersonaje.Hielo ? new Color(0.55f, 0.96f, 1f) : new Color(1f, 0.98f, 0.45f)));
                var mr = textoObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 9;
            }

            // --- CUARTOS JUGABLES (LADO OESTE, X de -19 a -12) ---
            ConstruirCuartoFisico("Fuego", TipoPersonaje.Fuego, false, -19, -12, 6, 11, 8, 10, true, new Color(1f, 0.90f, 0.88f), "🔥 FUEGO");
            CrearColumna(-15, 8);

            ConstruirCuartoFisico("Hielo", TipoPersonaje.Hielo, false, -19, -12, -3, 2, -1, 1, true, new Color(0.88f, 0.95f, 1f), "❄️ HIELO");
            CrearColumna(-15, 0);

            ConstruirCuartoFisico("Electrico", TipoPersonaje.Electrico, false, -19, -12, -12, -7, -10, -8, true, new Color(1f, 1f, 0.90f), "⚡ ELÉCTRICO");
            CrearColumna(-15, -9);

            for (int y = -12; y <= 12; y++)
            {
                bool esPuerta = (y >= -10 && y <= -8) || (y >= -1 && y <= 1) || (y >= 8 && y <= 10);
                if (!esPuerta)
                {
                    CrearMuro(-11, y, 5);
                }
            }

            // --- CUARTOS PRÓXIMAMENTE (LADO ESTE, X de 12 a 19) ---
            ConstruirCuartoFisico("Proximamente_1", TipoPersonaje.Ninguno, true, 12, 19, 6, 11, 8, 10, false, new Color(0.88f, 0.98f, 0.88f), "🔒 PRÓXIMAMENTE");
            CrearColumna(15, 8);

            ConstruirCuartoFisico("Proximamente_2", TipoPersonaje.Ninguno, true, 12, 19, -3, 2, -1, 1, false, new Color(0.95f, 0.88f, 1f), "🔒 PRÓXIMAMENTE");
            CrearColumna(15, 0);

            ConstruirCuartoFisico("Proximamente_3", TipoPersonaje.Ninguno, true, 12, 19, -12, -7, -10, -8, false, new Color(0.92f, 0.90f, 0.88f), "🔒 PRÓXIMAMENTE");
            CrearColumna(15, -9);

            for (int y = -12; y <= 12; y++)
            {
                bool esPuerta = (y >= -10 && y <= -8) || (y >= -1 && y <= 1) || (y >= 8 && y <= 10);
                if (!esPuerta)
                {
                    CrearMuro(11, y, 5);
                }
            }

            // Portal a la Mazmorra (En la alcoba al norte del salón central)
            var portalObj = new GameObject("PortalMazmorra");
            portalObj.transform.SetParent(mapaLobby.transform);
            portalObj.transform.position = new Vector3(0, 15f, 0);
            portalObj.transform.localScale = Vector3.one * 1.5f;
            var srPortal = portalObj.AddComponent<SpriteRenderer>();
            srPortal.sprite = spritePortal;
            srPortal.sortingOrder = 3;
            var colPortal = portalObj.AddComponent<BoxCollider2D>();
            colPortal.isTrigger = true;
            colPortal.size = new Vector2(2f, 1.5f);
            var portalLobby = portalObj.AddComponent<PortalLobby>();

            // Spawns de los jugadores en el centro del Salón
            var spawnsLobby = new GameObject("SpawnsLobby");
            var tSpawn1 = new GameObject("Spawn1").transform; tSpawn1.position = new Vector3(-3, 0, 0); tSpawn1.SetParent(spawnsLobby.transform);
            var tSpawn2 = new GameObject("Spawn2").transform; tSpawn2.position = new Vector3(-1, 0, 0); tSpawn2.SetParent(spawnsLobby.transform);
            var tSpawn3 = new GameObject("Spawn3").transform; tSpawn3.position = new Vector3(1, 0, 0); tSpawn3.SetParent(spawnsLobby.transform);
            var tSpawn4 = new GameObject("Spawn4").transform; tSpawn4.position = new Vector3(3, 0, 0); tSpawn4.SetParent(spawnsLobby.transform);
            // 3. UI Overlay del Lobby (Limpio y despejado para ver los cuartos en el juego)
            var canvasObj = new GameObject("UI_Lobby");
            ConfigurarCanvas(canvasObj, out var canvas, out var panel);
            GameObject.DestroyImmediate(panel); // Sin panel de fondo opaco

            // Título superior
            var txtTitulo = CrearTexto(canvasObj.transform, "🏰 SALÓN LOBBY", new Vector2(0, 490), 28, Color.yellow);
            var txtSala = CrearTexto(canvasObj.transform, "Sala: ...", new Vector2(0, 455), 20, Color.white);
            var txtAvisoPortal = CrearTexto(canvasObj.transform, "▲ CAMINA A UN CUARTO PARA ELEGIR PERSONAJE | USA EL PORTAL PARA INICIAR ▲", new Vector2(0, 415), 15, new Color(0.6f, 1f, 0.6f));
            txtAvisoPortal.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 45);

            // Caja Arriba a la Derecha (Código Secreto)
            var cajaInfo = new GameObject("CajaInfo");
            cajaInfo.transform.SetParent(canvasObj.transform, false);
            var imgInfo = cajaInfo.AddComponent<Image>();
            imgInfo.color = new Color(0.06f, 0.06f, 0.12f, 0.85f);
            var rtInfo = cajaInfo.GetComponent<RectTransform>();
            rtInfo.anchorMin = new Vector2(1, 1);
            rtInfo.anchorMax = new Vector2(1, 1);
            rtInfo.pivot = new Vector2(1, 1);
            rtInfo.anchoredPosition = new Vector2(-25, -25);
            rtInfo.sizeDelta = new Vector2(340, 130);

            var txtInfo = CrearTexto(cajaInfo.transform, "🔑 Conectando sala...", new Vector2(0, 25), 15, Color.cyan);
            txtInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 50);
            var btnCopiarIP = CrearBoton(cajaInfo.transform, "📋 COPIAR CÓDIGO", new Vector2(0, -35), new Vector2(220, 36));

            // PANEL VISTA GENERAL (Contenedor vacío de interfaz limpia)
            var panelVistaGeneral = new GameObject("PanelVistaGeneral");
            panelVistaGeneral.transform.SetParent(canvasObj.transform, false);
            var rtVG = panelVistaGeneral.AddComponent<RectTransform>();
            rtVG.anchorMin = Vector2.zero;
            rtVG.anchorMax = Vector2.one;
            rtVG.sizeDelta = Vector2.zero;

            // PANEL VISTA DETALLADA DEL CUARTO (Loadout, Maestría y Selección)
            var panelVistaCuarto = new GameObject("PanelVistaCuarto");
            panelVistaCuarto.transform.SetParent(canvasObj.transform, false);
            var imgFondoCuarto = panelVistaCuarto.AddComponent<Image>();
            imgFondoCuarto.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);
            var rtVC = panelVistaCuarto.GetComponent<RectTransform>();
            rtVC.anchorMin = new Vector2(0.5f, 0.5f);
            rtVC.anchorMax = new Vector2(0.5f, 0.5f);
            rtVC.sizeDelta = new Vector2(740, 640);
            panelVistaCuarto.SetActive(false); // Oculto inicialmente

            var txtTitCuarto = CrearTexto(panelVistaCuarto.transform, "PERSONAJE", new Vector2(0, 275), 26, new Color(1f, 0.9f, 0.3f));
            var txtMaestria = CrearTexto(panelVistaCuarto.transform, "MAESTRÍA PERMANENTE", new Vector2(0, 238), 16, new Color(0.6f, 0.9f, 1f));
            var txtEstCuarto = CrearTexto(panelVistaCuarto.transform, "DISPONIBLE", new Vector2(0, 206), 15, Color.green);
            CrearTexto(panelVistaCuarto.transform, "KIT DE HABILIDADES Y PROGRESIÓN (DESBLOQUEO POR NIVEL DE MAESTRÍA):", new Vector2(0, 172), 12, new Color(0.75f, 0.8f, 0.9f));

            var contSlots = new GameObject("ContenedorSlots");
            contSlots.transform.SetParent(panelVistaCuarto.transform, false);
            var rtCS = contSlots.AddComponent<RectTransform>();
            rtCS.anchoredPosition = new Vector2(0, -20);
            rtCS.sizeDelta = new Vector2(640, 310);
            var vlgSlots = contSlots.AddComponent<VerticalLayoutGroup>();
            vlgSlots.childControlWidth = true;
            vlgSlots.childForceExpandHeight = false;
            vlgSlots.spacing = 8;

            // Fila de Botones de Acción del Cuarto
            var filaBotonesCuarto = new GameObject("FilaBotonesCuarto");
            filaBotonesCuarto.transform.SetParent(panelVistaCuarto.transform, false);
            var rtFBC = filaBotonesCuarto.AddComponent<RectTransform>();
            rtFBC.anchoredPosition = new Vector2(0, -255);
            rtFBC.sizeDelta = new Vector2(680, 50);

            var btnSeleccionarCuarto = CrearBoton(filaBotonesCuarto.transform, "✅ SELECCIONAR PERSONAJE", new Vector2(-180, 0), new Vector2(250, 48));
            var imgSel = btnSeleccionarCuarto.GetComponent<Image>();
            if (imgSel) imgSel.color = new Color(0.18f, 0.65f, 0.28f);

            var btnLiberarCuarto = CrearBoton(filaBotonesCuarto.transform, "❌ LIBERAR", new Vector2(90, 0), new Vector2(140, 48));
            var imgLib = btnLiberarCuarto.GetComponent<Image>();
            if (imgLib) imgLib.color = new Color(0.75f, 0.22f, 0.22f);

            var btnVolverCuarto = CrearBoton(filaBotonesCuarto.transform, "↩ VOLVER", new Vector2(245, 0), new Vector2(130, 48));
            var imgVolver = btnVolverCuarto.GetComponent<Image>();
            if (imgVolver) imgVolver.color = new Color(0.35f, 0.38f, 0.45f);

            // Botón Iniciar (Host) Abajo a la Derecha
            var btnIniciar = CrearBoton(canvasObj.transform, "⚔️ INICIAR MAZMORRA", new Vector2(680, -470), new Vector2(260, 52));
            var imgIni = btnIniciar.GetComponent<Image>();
            if (imgIni != null) imgIni.color = new Color(0.18f, 0.65f, 0.28f, 1f);

            var btnSalir = CrearBoton(canvasObj.transform, "SALIR", new Vector2(-750, -470), new Vector2(140, 40));
            var txtEstGeneral = CrearTexto(canvasObj.transform, "", new Vector2(0, -440), 18, Color.white);

            var ctrl = canvasObj.AddComponent<ControladorLobby>();
            ctrl.textoNombreSala = txtSala;
            ctrl.textoInfoConexion = txtInfo;
            ctrl.botonCopiarIP = btnCopiarIP;
            ctrl.textoEstado = txtEstGeneral;
            ctrl.botonIniciar = btnIniciar;
            ctrl.botonSalir = btnSalir;

            ctrl.panelVistaGeneral = panelVistaGeneral;
            ctrl.panelVistaCuarto = panelVistaCuarto;

            ctrl.textoTituloCuarto = txtTitCuarto;
            ctrl.textoMaestriaCuarto = txtMaestria;
            ctrl.textoEstadoCuarto = txtEstCuarto;
            ctrl.contenedorSlotsHabilidades = contSlots.transform;
            ctrl.botonSeleccionarCuarto = btnSeleccionarCuarto;
            ctrl.botonLiberarCuarto = btnLiberarCuarto;
            ctrl.botonVolverCuarto = btnVolverCuarto;

            ctrl.prefabNeutral = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeNeutral.prefab");
            ctrl.prefabFuego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeFuego.prefab");
            ctrl.prefabHielo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeHielo.prefab");
            ctrl.prefabElectrico = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeElectrico.prefab");
            ctrl.puntosSpawnLobby = new Transform[] { tSpawn1, tSpawn2, tSpawn3, tSpawn4 };

            portalLobby.controladorLobby = ctrl;

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/Lobby.unity");
        }

        private static void CrearEscenaJuego()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Cámara con CamaraSeguir
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 7.8f; // Vista espaciosa táctica para combate y mazmorra
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.gameObject.AddComponent<CamaraSeguir>();
            }

            // Mapa base procedural
            var mapa = new GameObject("Mapa");
            var genMaz = mapa.AddComponent<GeneradorMazmorra>();
            genMaz.biomasPorSala = false;
            mapa.AddComponent<SistemaVision>();
            genMaz.contenedorMapa = mapa.transform;
            genMaz.spritePortal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/PortalSalida.png");
            genMaz.spriteHoyitoPared = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/HoyitoPared.png");
            genMaz.spriteHoyitoParedCerrado = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/HoyitoPared_Cerrado.png");
            genMaz.biomas = new DatosBioma[]
            {
                new DatosBioma
                {
                    tipo = TipoBioma.Piedra,
                    nombreBioma = "Mazmorra de Piedra Ancestral",
                    spriteSuelo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/SueloPiedra.png"),
                    spriteMuro = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/MuroPiedra.png"),
                    spriteColumna = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/ColumnaPiedra.png"),
                    colorFondoCamara = Color.black
                },
                new DatosBioma
                {
                    tipo = TipoBioma.Hielo,
                    nombreBioma = "Cripta de Hielo Eterno",
                    spriteSuelo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/SueloHielo.png"),
                    spriteMuro = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/MuroHielo.png"),
                    spriteColumna = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/ColumnaHielo.png"),
                    colorFondoCamara = Color.black
                },
                new DatosBioma
                {
                    tipo = TipoBioma.Magma,
                    nombreBioma = "Fortaleza de Obsidiana y Magma",
                    spriteSuelo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/SueloMagma.png"),
                    spriteMuro = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/MuroObsidiana.png"),
                    spriteColumna = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Mapas/ColumnaMagma.png"),
                    colorFondoCamara = Color.black
                }
            };
            genMaz.Generar();

            // Gestor de Partida
            var gpObj = new GameObject("GestorPartida");
            var gp = gpObj.AddComponent<GestorPartida>();
            gp.generadorMazmorra = genMaz;
            gp.prefabFuego = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeFuego.prefab");
            gp.prefabHielo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeHielo.prefab");
            gp.prefabElectrico = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/PersonajeElectrico.prefab");
            gp.prefabEnemigo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/Enemigo1.prefab");
            gp.prefabEnemigoMago = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/EnemigoMago.prefab");
            gp.prefabEnemigoSlime = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/EnemigoSlime.prefab");
            gp.prefabJefeSlimeRey = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/JefeSlimeRey.prefab");
            gp.prefabJefeGranBrujo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/JefeGranBrujo.prefab");
            gp.prefabJefeGolemColosal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/JefeGolemColosal.prefab");
            gp.prefabItemMejoraRun = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Loot/ItemMejoraRun.prefab");

            // Spawns (Fallback)
            var spawnsJugador = new GameObject("SpawnsJugador");
            var tSpawn1 = new GameObject("Spawn1").transform; tSpawn1.position = new Vector2(-4, 1); tSpawn1.SetParent(spawnsJugador.transform);
            var tSpawn2 = new GameObject("Spawn2").transform; tSpawn2.position = new Vector2(-4, -1); tSpawn2.SetParent(spawnsJugador.transform);
            var tSpawn3 = new GameObject("Spawn3").transform; tSpawn3.position = new Vector2(-6, 1); tSpawn3.SetParent(spawnsJugador.transform);
            var tSpawn4 = new GameObject("Spawn4").transform; tSpawn4.position = new Vector2(-6, -1); tSpawn4.SetParent(spawnsJugador.transform);
            gp.puntosSpawnJugadores = new Transform[] { tSpawn1, tSpawn2, tSpawn3, tSpawn4 };

            // HUD (Screen Space Overlay: fijo a la pantalla/cámara del jugador, nunca en el fondo del mapa)
            var canvasObj = new GameObject("HUD");
            ConfigurarCanvas(canvasObj, out var canvas, out var panel);
            GameObject.DestroyImmediate(panel); // HUD no lleva panel opaco

            // Contenedor Arriba a la Izquierda (Nombre del jugador local + Barra de Vida HUD Grande y Destacada)
            var contArribaIzq = new GameObject("ContenedorHUD_ArribaIzquierda");
            contArribaIzq.transform.SetParent(canvasObj.transform, false);
            var izqRt = contArribaIzq.AddComponent<RectTransform>();
            izqRt.anchorMin = new Vector2(0f, 1f);
            izqRt.anchorMax = new Vector2(0f, 1f);
            izqRt.pivot = new Vector2(0f, 1f);
            izqRt.anchoredPosition = new Vector2(25f, -10f);
            izqRt.sizeDelta = new Vector2(400f, 92f);

            // Nombre del jugador local justo arriba de su barra de vida
            var nomLocalObj = new GameObject("TextoMiNombreHUD");
            nomLocalObj.transform.SetParent(contArribaIzq.transform, false);
            var nomLocalRt = nomLocalObj.AddComponent<RectTransform>();
            nomLocalRt.anchorMin = new Vector2(0f, 1f);
            nomLocalRt.anchorMax = new Vector2(0f, 1f);
            nomLocalRt.pivot = new Vector2(0f, 1f);
            nomLocalRt.anchoredPosition = new Vector2(2f, 0f);
            nomLocalRt.sizeDelta = new Vector2(380f, 22f);

            var txtMiNombreHUD = nomLocalObj.AddComponent<Text>();
            txtMiNombreHUD.font = FuenteUtil.ObtenerFuentePixelify();
            txtMiNombreHUD.text = "👤 Jugador";
            txtMiNombreHUD.fontSize = 15;
            txtMiNombreHUD.fontStyle = FontStyle.Bold;
            txtMiNombreHUD.alignment = TextAnchor.MiddleLeft;
            txtMiNombreHUD.color = new Color(0.95f, 0.98f, 1f, 1f);
            txtMiNombreHUD.raycastTarget = false;

            var sombraNomLocal = nomLocalObj.AddComponent<Shadow>();
            sombraNomLocal.effectColor = new Color(0f, 0f, 0f, 0.9f);
            sombraNomLocal.effectDistance = new Vector2(1.5f, -1.5f);

            // Barra de vida HUD (arriba a la izquierda, justo debajo del nombre del jugador local)
            var bvObj = new GameObject("BarraVidaHUD");
            bvObj.transform.SetParent(contArribaIzq.transform, false);
            var bvRt = bvObj.AddComponent<RectTransform>();
            bvRt.anchorMin = new Vector2(0f, 1f);
            bvRt.anchorMax = new Vector2(0f, 1f);
            bvRt.pivot = new Vector2(0f, 1f);
            bvRt.anchoredPosition = new Vector2(0f, -23f);
            bvRt.sizeDelta = new Vector2(380f, 32f);

            var bvFondo = bvObj.AddComponent<Image>();
            bvFondo.color = new Color(0.18f, 0.04f, 0.04f, 0.95f);

            var bvRellenoObj = new GameObject("Relleno");
            bvRellenoObj.transform.SetParent(bvObj.transform, false);
            var bvRellenoRt = bvRellenoObj.AddComponent<RectTransform>();
            bvRellenoRt.pivot = new Vector2(0f, 0.5f);
            bvRellenoRt.anchorMin = Vector2.zero;
            bvRellenoRt.anchorMax = Vector2.one;
            bvRellenoRt.offsetMin = new Vector2(2f, 2f);
            bvRellenoRt.offsetMax = new Vector2(-2f, -2f);

            var bvRelleno = bvRellenoObj.AddComponent<Image>();
            bvRelleno.color = new Color(0.92f, 0.18f, 0.18f, 1f); // Rojo carmesí brillante
            bvRelleno.type = Image.Type.Simple;

            // Texto numérico de vida ("100 / 100")
            var bvTxtObj = new GameObject("TextoVida");
            bvTxtObj.transform.SetParent(bvObj.transform, false);
            var bvTxtRt = bvTxtObj.AddComponent<RectTransform>();
            bvTxtRt.anchorMin = Vector2.zero;
            bvTxtRt.anchorMax = Vector2.one;
            bvTxtRt.sizeDelta = Vector2.zero;
            var bvTxt = bvTxtObj.AddComponent<Text>();
            bvTxt.font = FuenteUtil.ObtenerFuentePixelify();
            bvTxt.text = "100 / 100";
            bvTxt.fontSize = 16;
            bvTxt.fontStyle = FontStyle.Bold;
            bvTxt.alignment = TextAnchor.MiddleCenter;
            bvTxt.color = Color.white;

            var barraVidaComp = bvObj.AddComponent<BarraVida>();
            barraVidaComp.relleno = bvRelleno;
            barraVidaComp.textoVida = bvTxt;

            // Barra de Nivel de Maestría (justo abajo de la barra de vida, gruesa y llamativa)
            var bmObj = new GameObject("BarraMaestriaHUD");
            bmObj.transform.SetParent(contArribaIzq.transform, false);
            var bmRt = bmObj.AddComponent<RectTransform>();
            bmRt.anchorMin = new Vector2(0f, 1f);
            bmRt.anchorMax = new Vector2(0f, 1f);
            bmRt.pivot = new Vector2(0f, 1f);
            bmRt.anchoredPosition = new Vector2(0f, -60f);
            bmRt.sizeDelta = new Vector2(340f, 26f);

            var bmFondo = bmObj.AddComponent<Image>();
            bmFondo.color = new Color(0.12f, 0.06f, 0.18f, 0.95f);

            var bmRellenoObj = new GameObject("RellenoMaestria");
            bmRellenoObj.transform.SetParent(bmObj.transform, false);
            var bmRellenoRt = bmRellenoObj.AddComponent<RectTransform>();
            bmRellenoRt.pivot = new Vector2(0f, 0.5f);
            bmRellenoRt.anchorMin = Vector2.zero;
            bmRellenoRt.anchorMax = new Vector2(0f, 1f);
            bmRellenoRt.offsetMin = Vector2.zero;
            bmRellenoRt.offsetMax = Vector2.zero;

            var bmRelleno = bmRellenoObj.AddComponent<Image>();
            bmRelleno.color = new Color(0.78f, 0.38f, 1f, 1f); // Púrpura brillante
            bmRelleno.type = Image.Type.Simple;

            var bmTxtObj = new GameObject("TextoMaestria");
            bmTxtObj.transform.SetParent(bmObj.transform, false);
            var bmTxtRt = bmTxtObj.AddComponent<RectTransform>();
            bmTxtRt.anchorMin = Vector2.zero;
            bmTxtRt.anchorMax = Vector2.one;
            bmTxtRt.sizeDelta = Vector2.zero;
            var bmTxt = bmTxtObj.AddComponent<Text>();
            bmTxt.font = FuenteUtil.ObtenerFuentePixelify();
            bmTxt.text = "Maestría Nv.1 (0/200 XP)";
            bmTxt.fontSize = 12;
            bmTxt.fontStyle = FontStyle.Bold;
            bmTxt.alignment = TextAnchor.MiddleCenter;
            bmTxt.color = Color.white;

            var barraMaestriaComp = bmObj.AddComponent<BarraVida>();
            barraMaestriaComp.relleno = bmRelleno;
            barraMaestriaComp.textoVida = bmTxt;

            // Contenedor Arriba a la Derecha (Botón de Pausa arriba del Minimapa)
            var contArribaDer = new GameObject("ContenedorHUD_ArribaDerecha");
            contArribaDer.transform.SetParent(canvasObj.transform, false);
            var arRt = contArribaDer.AddComponent<RectTransform>();
            arRt.anchorMin = new Vector2(1f, 1f);
            arRt.anchorMax = new Vector2(1f, 1f);
            arRt.pivot = new Vector2(1f, 1f);
            arRt.anchoredPosition = new Vector2(-16f, -12f);
            arRt.sizeDelta = new Vector2(40f, 40f);

            var btnPausaHUD = CrearBoton(contArribaDer.transform, "☰", Vector2.zero, new Vector2(38f, 38f));
            var bRt = btnPausaHUD.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1f, 1f);
            bRt.anchorMax = new Vector2(1f, 1f);
            bRt.pivot = new Vector2(1f, 1f);
            bRt.anchoredPosition = Vector2.zero;
            var txtBP = btnPausaHUD.GetComponentInChildren<Text>();
            if (txtBP != null) txtBP.fontSize = 24;

            // Contenedor Abajo al Centro (Fila de Habilidades + Barra de XP de Run sobre ella)
            var contenedorAbajo = new GameObject("ContenedorHUD_AbajoCentro");
            contenedorAbajo.transform.SetParent(canvasObj.transform, false);
            var contRt = contenedorAbajo.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 0f);
            contRt.anchorMax = new Vector2(0.5f, 0f);
            contRt.pivot = new Vector2(0.5f, 0f);
            contRt.anchoredPosition = new Vector2(0f, 12f);
            contRt.sizeDelta = new Vector2(380f, 112f);

            // Fondo translúcido tipo marco para el HUD inferior
            var marcoImg = contenedorAbajo.AddComponent<Image>();
            marcoImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

            // Barra de Experiencia de Run (justo sobre la fila de habilidades, de 26px de alto y gruesa)
            var bxpObj = new GameObject("BarraXPRunHUD");
            bxpObj.transform.SetParent(contenedorAbajo.transform, false);
            var bxpRt = bxpObj.AddComponent<RectTransform>();
            bxpRt.anchorMin = new Vector2(0.5f, 1f);
            bxpRt.anchorMax = new Vector2(0.5f, 1f);
            bxpRt.pivot = new Vector2(0.5f, 1f);
            bxpRt.anchoredPosition = new Vector2(0f, -6f);
            bxpRt.sizeDelta = new Vector2(360f, 26f);

            var bxpFondo = bxpObj.AddComponent<Image>();
            bxpFondo.color = new Color(0.05f, 0.12f, 0.20f, 0.95f);

            var bxpRellenoObj = new GameObject("RellenoXP");
            bxpRellenoObj.transform.SetParent(bxpObj.transform, false);
            var bxpRellenoRt = bxpRellenoObj.AddComponent<RectTransform>();
            bxpRellenoRt.pivot = new Vector2(0f, 0.5f);
            bxpRellenoRt.anchorMin = Vector2.zero;
            bxpRellenoRt.anchorMax = new Vector2(0f, 1f);
            bxpRellenoRt.offsetMin = Vector2.zero;
            bxpRellenoRt.offsetMax = Vector2.zero;

            var bxpRelleno = bxpRellenoObj.AddComponent<Image>();
            bxpRelleno.color = new Color(0.12f, 0.88f, 1f, 1f); // Cian eléctrico brillante
            bxpRelleno.type = Image.Type.Simple;

            var bxpTxtObj = new GameObject("TextoXP");
            bxpTxtObj.transform.SetParent(bxpObj.transform, false);
            var bxpTxtRt = bxpTxtObj.AddComponent<RectTransform>();
            bxpTxtRt.anchorMin = Vector2.zero;
            bxpTxtRt.anchorMax = Vector2.one;
            bxpTxtRt.sizeDelta = Vector2.zero;
            var bxpTxt = bxpTxtObj.AddComponent<Text>();
            bxpTxt.font = FuenteUtil.ObtenerFuentePixelify();
            bxpTxt.text = "Nv.1 (0/100 XP)";
            bxpTxt.fontSize = 12;
            bxpTxt.fontStyle = FontStyle.Bold;
            bxpTxt.alignment = TextAnchor.MiddleCenter;
            bxpTxt.color = Color.white;

            var barraXPRunComp = bxpObj.AddComponent<BarraVida>();
            barraXPRunComp.relleno = bxpRelleno;
            barraXPRunComp.textoVida = bxpTxt;

            // Contenedor de Fila de Habilidades
            var filaHabilidades = new GameObject("FilaHabilidades");
            filaHabilidades.transform.SetParent(contenedorAbajo.transform, false);
            var filaRt = filaHabilidades.AddComponent<RectTransform>();
            filaRt.anchorMin = new Vector2(0.5f, 0.5f);
            filaRt.anchorMax = new Vector2(0.5f, 0.5f);
            filaRt.pivot = new Vector2(0.5f, 0.5f);
            filaRt.anchoredPosition = new Vector2(0f, -6f);
            filaRt.sizeDelta = new Vector2(380f, 84f);

            var slots = new SlotHabilidadHUD[4];
            var iconosHabilidades = new Image[4];
            float[] posX = new float[] { -141f, -47f, 47f, 141f };
            string[] teclas = new string[] { "LMB", "E", "Q", "R" };

            for (int i = 0; i < 4; i++)
            {
                var slotObj = new GameObject("Slot_" + teclas[i]);
                slotObj.transform.SetParent(filaHabilidades.transform, false);
                var sRt = slotObj.AddComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(posX[i], 0f);
                sRt.sizeDelta = new Vector2(80f, 80f);

                var sFondo = slotObj.AddComponent<Image>();
                sFondo.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);

                var iconoObj = new GameObject("Icono");
                iconoObj.transform.SetParent(slotObj.transform, false);
                var icRt = iconoObj.AddComponent<RectTransform>();
                icRt.anchorMin = Vector2.zero;
                icRt.anchorMax = Vector2.one;
                icRt.sizeDelta = new Vector2(-6f, -6f);
                var icImg = iconoObj.AddComponent<Image>();
                icImg.preserveAspect = true;
                icImg.color = Color.white; // 100% brillante y crudo

                var sombraObj = new GameObject("SombraCooldown");
                sombraObj.transform.SetParent(slotObj.transform, false);
                var somRt = sombraObj.AddComponent<RectTransform>();
                somRt.anchorMin = Vector2.zero;
                somRt.anchorMax = Vector2.one;
                somRt.sizeDelta = new Vector2(-6f, -6f);
                var somImg = sombraObj.AddComponent<Image>();
                somImg.color = new Color(0f, 0f, 0f, 0.75f);
                somImg.type = Image.Type.Filled;
                somImg.fillMethod = Image.FillMethod.Radial360;
                somImg.fillOrigin = (int)Image.Origin360.Top;
                somImg.fillClockwise = false;
                somImg.fillAmount = 0f;
                sombraObj.SetActive(false); // Iniciar apagada para que no oscurezca el icono en reposo

                var cdTxtObj = new GameObject("TextoCooldown");
                cdTxtObj.transform.SetParent(slotObj.transform, false);
                var cdTxtRt = cdTxtObj.AddComponent<RectTransform>();
                cdTxtRt.anchorMin = Vector2.zero;
                cdTxtRt.anchorMax = Vector2.one;
                cdTxtRt.sizeDelta = Vector2.zero;
                var cdTxt = cdTxtObj.AddComponent<Text>();
                cdTxt.font = FuenteUtil.ObtenerFuentePixelify();
                cdTxt.text = "";
                cdTxt.fontSize = 12;
                cdTxt.fontStyle = FontStyle.Bold;
                cdTxt.alignment = TextAnchor.MiddleCenter;
                cdTxt.color = new Color(1f, 0.95f, 0.3f, 1f);
                cdTxt.resizeTextForBestFit = false;

                var keyBadgeObj = new GameObject("BadgeTecla");
                keyBadgeObj.transform.SetParent(slotObj.transform, false);
                var kbRt = keyBadgeObj.AddComponent<RectTransform>();
                kbRt.anchorMin = new Vector2(0.5f, 0f);
                kbRt.anchorMax = new Vector2(0.5f, 0f);
                kbRt.pivot = new Vector2(0.5f, 0f);
                kbRt.anchoredPosition = new Vector2(0f, -2f);
                kbRt.sizeDelta = new Vector2(36f, 16f);
                var kbImg = keyBadgeObj.AddComponent<Image>();
                kbImg.color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

                var kbTxtObj = new GameObject("TextoTecla");
                kbTxtObj.transform.SetParent(keyBadgeObj.transform, false);
                var kbTxtRt = kbTxtObj.AddComponent<RectTransform>();
                kbTxtRt.anchorMin = Vector2.zero;
                kbTxtRt.anchorMax = Vector2.one;
                kbTxtRt.sizeDelta = Vector2.zero;
                var kbTxt = kbTxtObj.AddComponent<Text>();
                kbTxt.font = FuenteUtil.ObtenerFuenteSilkscreen();
                kbTxt.text = teclas[i];
                kbTxt.fontSize = 10;
                kbTxt.fontStyle = FontStyle.Bold;
                kbTxt.alignment = TextAnchor.MiddleCenter;
                kbTxt.color = Color.white;

                slots[i] = new SlotHabilidadHUD
                {
                    contenedor = slotObj,
                    icono = icImg,
                    sombraCooldown = somImg,
                    textoTiempo = cdTxt,
                    textoTecla = kbTxt
                };
                iconosHabilidades[i] = somImg;
            }

            // Panel Pausa
            var panelPausa = new GameObject("PanelPausa");
            panelPausa.transform.SetParent(canvasObj.transform, false);
            var pImg = panelPausa.AddComponent<Image>();
            pImg.color = new Color(0, 0, 0, 0.82f);
            var pRt = panelPausa.GetComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one; pRt.sizeDelta = Vector2.zero;
            CrearTexto(panelPausa.transform, "PAUSA", new Vector2(0, 100), 36, Color.white);
            var btnReanudar = CrearBoton(panelPausa.transform, "REANUDAR", new Vector2(0, 40), new Vector2(230, 45));
            var btnVolverLobbyPausa = CrearBoton(panelPausa.transform, "VOLVER AL LOBBY", new Vector2(0, -15), new Vector2(230, 45));
            var btnSalirPartida = CrearBoton(panelPausa.transform, "SALIR AL MENÚ", new Vector2(0, -70), new Vector2(230, 45));

            // Panel Fin Partida
            var panelFin = new GameObject("PanelFinPartida");
            panelFin.transform.SetParent(canvasObj.transform, false);
            var finImg = panelFin.AddComponent<Image>();
            finImg.color = new Color(0, 0, 0, 0.85f);
            var finRt = panelFin.GetComponent<RectTransform>();
            finRt.anchorMin = Vector2.zero; finRt.anchorMax = Vector2.one; finRt.sizeDelta = Vector2.zero;
            var txtFin = CrearTexto(panelFin.transform, "FIN DE PARTIDA", new Vector2(0, 60), 36, Color.yellow);
            var btnVolverLobby = CrearBoton(panelFin.transform, "VOLVER AL LOBBY", new Vector2(0, -20), new Vector2(240, 50));

            var hud = canvasObj.AddComponent<HUDJugador>();
            hud.textoMiNombreHUD = txtMiNombreHUD;
            hud.barraVida = barraVidaComp;
            hud.barraMaestria = barraMaestriaComp;
            hud.barraXPRun = barraXPRunComp;
            hud.slotsHabilidades = slots;
            hud.iconosHabilidades = iconosHabilidades;
            hud.botonPausaHUD = btnPausaHUD;
            hud.panelPausa = panelPausa;
            hud.botonReanudar = btnReanudar;
            hud.botonVolverLobbyPausa = btnVolverLobbyPausa;
            hud.botonSalirPartida = btnSalirPartida;
            hud.panelFinPartida = panelFin;
            hud.textoResultado = txtFin;
            hud.botonVolverLobby = btnVolverLobby;

            hud.iconoFuegoLMB = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/BolaFuego.png");
            hud.iconoFuegoE = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/CabezaFuegoAtaque.png");
            hud.iconoFuegoQ = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Fuego/ArmaduraFuego.png");
            hud.iconoFuegoR = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/IconosHabilidades/Fuego_Meteorito.png");

            hud.iconoHieloLMB = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoRayoHielo.png");
            hud.iconoHieloE = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoBolaHielo.png");
            hud.iconoHieloQ = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoArmaduraHielo.png");
            hud.iconoHieloR = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Hielo/Iconos/IconoEraHielo.png");

            hud.iconoElecLMB = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/EspadaElectrica/EspadaElectrica_00.png");
            hud.iconoElecE = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/Iconos/IconoDashElectrico.png");
            hud.iconoElecQ = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/Iconos/IconoTeleportElectrico.png");
            hud.iconoElecR = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Personajes/Electrico/Iconos/IconoRayoElectrico.png");

            EditorSceneManager.SaveScene(scene, "Assets/Escenas/Juego.unity");
        }

        private static void CrearPared(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Pared");
            go.tag = "Pared";
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)pos;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void ConfigurarBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Escenas/MenuPrincipal.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/PedirNombre.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/MenuJugar.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/CrearSala.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/UnirseSala.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/Lobby.unity", true),
                new EditorBuildSettingsScene("Assets/Escenas/Juego.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("SoulKnight/Compilar Build Standalone")]
        public static void CompilarBuildStandalone()
        {
            ReconstruirTodo();

            string buildDir = Directory.Exists("C:/Users/Usuario/Desktop/soulknight2Test")
                ? "C:/Users/Usuario/Desktop/soulknight2Test"
                : "C:/Users/Ricardo/Desktop/unityJuegos/SoulKnight2_Build";
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

            string exePath = Path.Combine(buildDir, "SoulKnight2.exe");
            string[] escenas = new string[]
            {
                "Assets/Escenas/MenuPrincipal.unity",
                "Assets/Escenas/PedirNombre.unity",
                "Assets/Escenas/MenuJugar.unity",
                "Assets/Escenas/CrearSala.unity",
                "Assets/Escenas/UnirseSala.unity",
                "Assets/Escenas/Lobby.unity",
                "Assets/Escenas/Juego.unity"
            };

            var options = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build exitoso: {exePath}");
            }
            else
            {
                Debug.LogError($"Error en build: {report.summary.result}");
            }
    }

        [MenuItem("SoulKnight/Generar Capturas de Salas por Bioma")]
        public static void GenerarCapturasSalasBiomas()
        {
            Debug.Log("== Iniciando Generación de Capturas de Salas por Bioma ==");
            ConfigurarTexturas();
            CatalogoBiomas.LimpiarCache();

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "CapturasBiomas");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            string artifactDir = @"C:\Users\Ricardo\.gemini\antigravity\brain\5c974bf4-1ac1-41f8-8beb-7c1da4ef20fb";

            TipoBioma[] biomas = new TipoBioma[]
            {
                TipoBioma.Piedra,
                TipoBioma.Planta,
                TipoBioma.Hielo,
                TipoBioma.Fuego,
                TipoBioma.Arena
            };

            foreach (var bioma in biomas)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("TestRoom_" + bioma);

                var datos = CatalogoBiomas.Obtener(bioma);
                string prefijo = datos.prefijo;

                // Dimensiones exactas 22x13 como los previews
                // x de -10 a 11 (22 tiles)
                // y de -6 a 6 (13 tiles)
                int xMin = -11;
                int xMax = 10;
                int yMin = -6;
                int yMax = 6;

                // Suelos y sombras
                for (int y = yMin + 1; y <= yMax - 2; y++)
                {
                    for (int x = xMin + 1; x <= xMax - 1; x++)
                    {
                        var sObj = new GameObject("Suelo");
                        sObj.transform.SetParent(root.transform);
                        sObj.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                        var sr = sObj.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = -1;

                        if (y == yMax - 2)
                        {
                            sr.sprite = datos.spriteFloorShadow00 ?? datos.spriteSuelo;
                        }
                        else if (y == yMax - 3)
                        {
                            sr.sprite = datos.spriteFloorShadow01 ?? datos.spriteSuelo;
                        }
                        else
                        {
                            if (Random.value < 0.85f && datos.spritesSuelo != null && datos.spritesSuelo.Length >= 4)
                                sr.sprite = datos.spritesSuelo[Random.Range(0, 4)];
                            else if (datos.spritesSuelo != null && datos.spritesSuelo.Length >= 8)
                                sr.sprite = datos.spritesSuelo[Random.Range(4, 8)];
                            else
                                sr.sprite = datos.spriteSuelo;
                        }
                    }
                }

                // Wall Top perimetral
                for (int x = xMin; x <= xMax; x++)
                {
                    CrearTileCaptura(root.transform, x + 0.5f, yMax + 0.5f, ObtenerWallTop(datos, x == xMin ? 6 : (x == xMax ? 12 : 10)), 2);
                    CrearTileCaptura(root.transform, x + 0.5f, yMin + 0.5f, ObtenerWallTop(datos, x == xMin ? 3 : (x == xMax ? 9 : 10)), 2);
                }
                for (int y = yMin + 1; y <= yMax - 1; y++)
                {
                    CrearTileCaptura(root.transform, xMin + 0.5f, y + 0.5f, ObtenerWallTop(datos, 5), 2);
                    CrearTileCaptura(root.transform, xMax + 0.5f, y + 0.5f, ObtenerWallTop(datos, 5), 2);
                }

                // Wall Face norte (fila yMax - 1)
                for (int x = xMin + 1; x <= xMax - 1; x++)
                {
                    bool torch = ((x - xMin) % 4 == 0);
                    Sprite spFace;
                    if (torch && datos.framesWallFaceTorch != null && datos.framesWallFaceTorch.Length > 0)
                    {
                        spFace = datos.framesWallFaceTorch[0];
                    }
                    else
                    {
                        float r = Random.value;
                        if (r < 0.12f && datos.spriteWallFaceCracked != null) spFace = datos.spriteWallFaceCracked;
                        else if (r < 0.24f && datos.spriteWallFaceDecor != null) spFace = datos.spriteWallFaceDecor;
                        else if (r < 0.62f && datos.spriteWallFaceB != null) spFace = datos.spriteWallFaceB;
                        else spFace = datos.spriteWallFaceA ?? datos.spriteMuro;
                    }
                    CrearTileCaptura(root.transform, x + 0.5f, yMax - 0.5f, spFace, 2);
                }

                // Obstáculo de muro central interior
                for (int ox = -2; ox <= 2; ox++)
                {
                    CrearTileCaptura(root.transform, ox + 0.5f, 1.5f, ObtenerWallTop(datos, ox == -2 ? 6 : (ox == 2 ? 12 : 10)), 2);
                    Sprite spObstFace = (ox == 0 && datos.framesWallFaceTorch != null) ? datos.framesWallFaceTorch[0] : (datos.spriteWallFaceA ?? datos.spriteMuro);
                    CrearTileCaptura(root.transform, ox + 0.5f, 0.5f, spObstFace, 2);

                    // Sombra bajo el obstáculo central
                    CrearTileCaptura(root.transform, ox + 0.5f, -0.5f, datos.spriteFloorShadow00 ?? datos.spriteSuelo, -1);
                }

                // Props oficiales del bioma
                if (datos.spritePillar != null)
                {
                    CrearTileCaptura(root.transform, -6.5f, 3.5f, datos.spritePillar, 3);
                    CrearTileCaptura(root.transform, 6.5f, 3.5f, datos.spritePillar, 3);
                }
                if (datos.spriteBoulderS != null)
                {
                    CrearTileCaptura(root.transform, 2.5f, 2.5f, datos.spriteBoulderS, 1);
                }
                if (datos.spriteBoulderL != null)
                {
                    CrearTileCaptura(root.transform, -3.5f, -2.5f, datos.spriteBoulderL, 1);
                }
                if (datos.spriteCrate != null)
                {
                    CrearTileCaptura(root.transform, -6.5f, -1.5f, datos.spriteCrate, 1);
                    CrearTileCaptura(root.transform, -5.5f, -1.5f, datos.spriteCrate, 1);
                }
                if (datos.spriteBarrel != null)
                {
                    CrearTileCaptura(root.transform, -5.5f, -3.5f, datos.spriteBarrel, 1);
                }
                if (datos.spriteSpikesOff != null && datos.spriteSpikesOn != null)
                {
                    CrearTileCaptura(root.transform, -1.5f, -2.5f, datos.spriteSpikesOn, 0);
                    CrearTileCaptura(root.transform, -0.5f, -2.5f, datos.spriteSpikesOff, 0);
                    CrearTileCaptura(root.transform, 0.5f, -2.5f, datos.spriteSpikesOn, 0);
                    CrearTileCaptura(root.transform, 1.5f, -2.5f, datos.spriteSpikesOff, 0);
                }
                if (datos.framesHazardStrip != null && datos.framesHazardStrip.Length > 0 && datos.framesHazardStrip[0] != null)
                {
                    for (int hx = 4; hx <= 5; hx++)
                    {
                        for (int hy = -3; hy <= -2; hy++)
                        {
                            CrearTileCaptura(root.transform, hx + 0.5f, hy + 0.5f, datos.framesHazardStrip[0], 0);
                        }
                    }
                }
                if (datos.spriteSpecialProp != null)
                {
                    CrearTileCaptura(root.transform, -7.5f, -3.5f, datos.spriteSpecialProp, 1);
                    CrearTileCaptura(root.transform, 3.5f, -3.5f, datos.spriteSpecialProp, 1);
                }

                // Cámara y Render
                var camObj = new GameObject("CamaraCaptura");
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.75f;
                cam.transform.position = new Vector3(0f, 0.5f, -10f);
                cam.backgroundColor = Color.black;
                cam.clearFlags = CameraClearFlags.SolidColor;

                int wPx = 1408;
                int hPx = 832;
                var rt = new RenderTexture(wPx, hPx, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(wPx, hPx, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, wPx, hPx), 0, 0);
                tex.Apply();

                byte[] pngBytes = tex.EncodeToPNG();
                string fileName = $"Captura_Sala_{prefijo}.png";
                string fullPath = Path.Combine(outputDir, fileName);
                File.WriteAllBytes(fullPath, pngBytes);

                if (Directory.Exists(artifactDir))
                {
                    string artPath = Path.Combine(artifactDir, fileName);
                    File.WriteAllBytes(artPath, pngBytes);
                }

                Debug.Log($"[Captura Generada] Bioma {bioma} ({prefijo}) -> {fullPath}");

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(root);
            }

            Debug.Log("== Generación de Capturas de Biomas Completada Exitosamente ==");
        }

        [MenuItem("SoulKnight/Generar Capturas de 3 Cuartos Piedra")]
        public static void GenerarCapturasTresCuartosPiedra()
        {
            Debug.Log("== Iniciando Generacion de Capturas de 3 Salas de Mazmorra de Piedra (Fiel a Preview 20x13) ==");
            ConfigurarTexturas();
            CatalogoBiomas.LimpiarCache();

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "CapturasBiomas");
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            string docsScreenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "docs", "screenshots");
            if (!Directory.Exists(docsScreenshotsDir)) Directory.CreateDirectory(docsScreenshotsDir);

            string artifactDir = @"C:\Users\Ricardo\.gemini\antigravity\brain\5c974bf4-1ac1-41f8-8beb-7c1da4ef20fb";

            var datos = CatalogoBiomas.Obtener(TipoBioma.Piedra);

            // 3 layouts de 18x10: 1 ("bastiones"), 5 ("estanque"), 6 ("bunker")
            int[] layouts = new int[] { 1, 5, 6 };
            string[] nombresSalas = new string[] { "bastiones", "estanque", "bunker" };

            for (int i = 0; i < layouts.Length; i++)
            {
                int layoutIdx = layouts[i];
                string layoutNombre = nombresSalas[i];

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Room_Piedra_" + layoutNombre);

                // Cuarto de exactamente 20 columnas x 13 filas (escala 1:1 fiel a stone_room_arena.png)
                // X: de -9.5f a 9.5f (20 tiles: pared izq=-9.5, interior=-8.5..8.5, pared der=9.5)
                // Y: de -6.5f a 5.5f (13 tiles: pared sup=5.5, wall_face=4.5, interior 10 filas=3.5..-5.5, pared inf=-6.5)

                // 1. Vacio exterior (stone_void)
                if (false && datos.spriteVoid != null)
                {
                    for (int vy = -8; vy <= 7; vy++)
                    {
                        for (int vx = -11; vx <= 11; vx++)
                        {
                            if (vx < -9 || vx > 9 || vy < -6 || vy > 5)
                            {
                                CrearTileCaptura(root.transform, vx - 0.5f, vy + 0.5f, datos.spriteVoid, -2);
                            }
                        }
                    }
                }

                // 2. Fila 0 (Superior): Muro Top Perimetral (1 tile de grosor exacto)
                CrearTileCaptura(root.transform, -9.5f, 5.5f, ObtenerWallTop(datos, 6), 2);
                for (int c = 0; c < 18; c++)
                {
                    CrearTileCaptura(root.transform, -8.5f + c, 5.5f, ObtenerWallTop(datos, 10), 2);
                }
                CrearTileCaptura(root.transform, 9.5f, 5.5f, ObtenerWallTop(datos, 12), 2);

                // 3. Fila 1: Cara Frontal de Muro Norte (wall_face) con antorchas cada 4 tiles y halo torch_glow fijo
                CrearTileCaptura(root.transform, -9.5f, 4.5f, ObtenerWallTop(datos, 5), 2); // Lateral izq
                for (int c = 0; c < 18; c++)
                {
                    float wx = -8.5f + c;
                    bool esAntorcha = (c == 1 || c == 5 || c == 9 || c == 13 || c == 17);

                    Sprite spFace;
                    if (esAntorcha && datos.framesWallFaceTorch != null && datos.framesWallFaceTorch.Length > 0)
                    {
                        spFace = datos.framesWallFaceTorch[0];
                        CrearTileCaptura(root.transform, wx, 4.5f, spFace, 2);

                        // Halo Glow centrado en la llama a escala 1:1 sin deformacion
                        if (datos.spriteTorchGlow != null)
                        {
                            var glowObj = new GameObject("TorchGlow");
                            glowObj.transform.SetParent(root.transform);
                            glowObj.transform.position = new Vector3(wx, 4.5f + 0.16f, 0);
                            glowObj.transform.localScale = Vector3.one;
                            var srGlow = glowObj.AddComponent<SpriteRenderer>();
                            srGlow.sprite = datos.spriteTorchGlow;
                            srGlow.color = Color.white; // Ya contiene gradiente y alfa suave
                            srGlow.sortingOrder = 3;
                        }
                    }
                    else
                    {
                        if (c == 8 && datos.spriteWallFaceBanner != null) spFace = datos.spriteWallFaceBanner;
                        else if ((c == 3 || c == 15) && datos.spriteWallFaceDecor != null) spFace = datos.spriteWallFaceDecor;
                        else if ((c == 4 || c == 12) && datos.spriteWallFaceCracked != null) spFace = datos.spriteWallFaceCracked;
                        else if (c % 2 == 0 && datos.spriteWallFaceB != null) spFace = datos.spriteWallFaceB;
                        else spFace = datos.spriteWallFaceA ?? datos.spriteMuro;

                        CrearTileCaptura(root.transform, wx, 4.5f, spFace, 2);
                    }
                }
                CrearTileCaptura(root.transform, 9.5f, 4.5f, ObtenerWallTop(datos, 5), 2); // Lateral der

                // 4. Filas 2 a 11: Interior del cuarto (18 cols x 10 rows)
                string[] plantilla = GeneradorMazmorra.PlantillasLayoutsStone[layoutIdx];
                int cols = 18;
                int rows = 10;
                char[,] gridMatriz = new char[cols, rows];
                for (int r = 0; r < rows; r++)
                {
                    string fila = plantilla[r];
                    for (int c = 0; c < cols; c++)
                    {
                        gridMatriz[c, r] = (c < fila.Length) ? fila[c] : '.';
                    }
                }

                for (int r = 0; r < 10; r++)
                {
                    float wy = 3.5f - r;

                    // Muros laterales izquierdo y derecho (1 tile grosor exacto, mask 5)
                    CrearTileCaptura(root.transform, -9.5f, wy, ObtenerWallTop(datos, 5), 2);
                    CrearTileCaptura(root.transform, 9.5f, wy, ObtenerWallTop(datos, 5), 2);

                    for (int c = 0; c < 18; c++)
                    {
                        float wx = -8.5f + c;

                        // Suelo continuo vivo según LEEME.md con sombras drop en filas 0 y 1
                        Sprite spSuelo;
                        if (r == 0)
                        {
                            spSuelo = datos.spriteFloorShadow00 ?? datos.spriteSuelo;
                        }
                        else if (r == 1)
                        {
                            spSuelo = datos.spriteFloorShadow01 ?? datos.spriteSuelo;
                        }
                        else
                        {
                            // Distribución continua plana: base floor_00..03 (~85%), detalles floor_04..07 (~15%)
                            int hash = Mathf.Abs((c * 37 + r * 19) ^ (i * 103));
                            if (hash % 100 < 85 && datos.spritesSuelo != null && datos.spritesSuelo.Length >= 4)
                            {
                                spSuelo = datos.spritesSuelo[hash % 4];
                            }
                            else if (datos.spritesSuelo != null && datos.spritesSuelo.Length >= 8)
                            {
                                spSuelo = datos.spritesSuelo[4 + (hash % 4)];
                            }
                            else
                            {
                                spSuelo = datos.spriteSuelo;
                            }
                        }
                        CrearTileCaptura(root.transform, wx, wy, spSuelo, -1);

                        // Calcomanías decorativas orgánicas sobre el suelo (huesos, grietas, charcos, piedras)
                        if (r >= 2 && gridMatriz[c, r] == '.' && datos.spritesDecals != null && datos.spritesDecals.Length > 0)
                        {
                            int roll = Mathf.Abs((c * 13 + r * 29) ^ (i * 71)) % 100;
                            if (roll < 14)
                            {
                                int decalIdx = Mathf.Abs(c * 7 + r * 11) % datos.spritesDecals.Length;
                                CrearTileCaptura(root.transform, wx, wy, datos.spritesDecals[decalIdx], 0);
                            }
                        }

                        // Props / Elementos del layout
                        char ch = gridMatriz[c, r];
                        if (ch == '.') continue;

                        switch (ch)
                        {
                            case 'D':
                            case 'x':
                                // Muro Destructible: bloque de piedra con grieta superpuesta
                                Sprite spDestr = (datos.spritesBlock != null && datos.spritesBlock.Length > 0 && datos.spritesBlock[0] != null)
                                    ? datos.spritesBlock[0] : datos.spriteMuro;
                                CrearTileCaptura(root.transform, wx, wy, spDestr, 2);
                                if (datos.spritesDecals != null && datos.spritesDecals.Length > 0 && datos.spritesDecals[0] != null)
                                {
                                    CrearTileCaptura(root.transform, wx, wy, datos.spritesDecals[0], 3);
                                }
                                break;

                            case '#':
                                // Bloque autotile 1 tile exacto de grosor
                                int mask = 0;
                                if (r > 0 && gridMatriz[c, r - 1] == '#') mask |= 1;
                                if (c + 1 < cols && gridMatriz[c + 1, r] == '#') mask |= 2;
                                if (r + 1 < rows && gridMatriz[c, r + 1] == '#') mask |= 4;
                                if (c > 0 && gridMatriz[c - 1, r] == '#') mask |= 8;
                                Sprite spB = (datos.spritesBlock != null && mask >= 0 && mask < datos.spritesBlock.Length)
                                    ? datos.spritesBlock[mask] : datos.spriteMuro;
                                CrearTileCaptura(root.transform, wx, wy, spB, 2);
                                break;

                            case 'P':
                                CrearTileCaptura(root.transform, wx, wy, datos.spritePillar, 3);
                                break;

                            case 'c':
                                CrearTileCaptura(root.transform, wx, wy, datos.spriteCrate, 1);
                                break;

                            case 'b':
                                CrearTileCaptura(root.transform, wx, wy, datos.spriteBarrel, 1);
                                break;

                            case 'r':
                                CrearTileCaptura(root.transform, wx, wy, datos.spriteBarrelRed ?? datos.spriteBarrel, 1);
                                break;

                            case 'p':
                                CrearTileCaptura(root.transform, wx, wy, datos.spritePot ?? datos.spriteCrate, 1);
                                break;

                            case 'm':
                                Sprite spM = MiniCofre.ObtenerSpriteMiniCerrado();
                                CrearTileCaptura(root.transform, wx, wy, spM, 2);
                                break;

                            case 's':
                                Sprite spS = (datos.framesSpikesStrip != null && datos.framesSpikesStrip.Length > 2)
                                    ? datos.framesSpikesStrip[2] : (datos.spriteSpikesOn ?? datos.spriteSpikesOff);
                                CrearTileCaptura(root.transform, wx, wy, spS, 0);
                                break;

                            case 'w':
                                Sprite spW = (datos.framesWaterStrip != null && datos.framesWaterStrip.Length > 0)
                                    ? datos.framesWaterStrip[0] : null;
                                CrearTileCaptura(root.transform, wx, wy, spW, 0);
                                break;

                            case 'o':
                                CrearTileCaptura(root.transform, wx, wy, datos.spritePit, 0);
                                break;
                        }
                    }
                }

                // 5. Fila 12 (Inferior): Muro Top Inferior Perimetral (1 tile exacto de grosor)
                CrearTileCaptura(root.transform, -9.5f, -6.5f, ObtenerWallTop(datos, 3), 2);
                for (int c = 0; c < 18; c++)
                {
                    CrearTileCaptura(root.transform, -8.5f + c, -6.5f, ObtenerWallTop(datos, 10), 2);
                }
                CrearTileCaptura(root.transform, 9.5f, -6.5f, ObtenerWallTop(datos, 9), 2);

                // 6. Camara y Render (encuadre exacto 20x13 a 1280x832 px)
                var camObj = new GameObject("CamaraCaptura");
                var cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                cam.transform.position = new Vector3(0f, -0.5f, -10f);
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;

                int wPx = 1280;
                int hPx = 832;
                var rt = new RenderTexture(wPx, hPx, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(wPx, hPx, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, wPx, hPx), 0, 0);
                tex.Apply();

                byte[] pngBytes = tex.EncodeToPNG();

                string fileNameNum = $"Captura_Sala_Piedra_{i + 1}.png";
                string fullPathNum = Path.Combine(outputDir, fileNameNum);
                File.WriteAllBytes(fullPathNum, pngBytes);
                File.WriteAllBytes(Path.Combine(docsScreenshotsDir, fileNameNum), pngBytes);

                if (Directory.Exists(artifactDir))
                {
                    File.WriteAllBytes(Path.Combine(artifactDir, fileNameNum), pngBytes);
                }

                if (i == 0)
                {
                    string stoneName = "Captura_Sala_stone.png";
                    File.WriteAllBytes(Path.Combine(outputDir, stoneName), pngBytes);
                    File.WriteAllBytes(Path.Combine(docsScreenshotsDir, stoneName), pngBytes);
                    if (Directory.Exists(artifactDir))
                    {
                        File.WriteAllBytes(Path.Combine(artifactDir, stoneName), pngBytes);
                    }
                }

                Debug.Log($"[Captura Piedra {i + 1}] Layout {layoutNombre} -> {fullPathNum}");

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(root);
            }

            Debug.Log("== Generacion de Capturas de 3 Cuartos Piedra Completada Exitosamente ==");
        }

        private static Sprite ObtenerWallTop(DatosBioma d, int mask)
        {
            if (d.spritesWallTop != null && mask >= 0 && mask < d.spritesWallTop.Length && d.spritesWallTop[mask] != null)
                return d.spritesWallTop[mask];
            return d.spriteMuro;
        }

        private static void CrearTileCaptura(Transform parent, float x, float y, Sprite sprite, int sortingOrder)
        {
            if (sprite == null) return;
            var go = new GameObject("Tile");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }
}
}
