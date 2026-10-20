using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SoulKnight
{
    /// <summary>
    /// Cofre de tesoro interactuable que aparece en salas secundarias de la mazmorra.
    /// Al abrirse con [F] o al interactuar con él, otorga 50 de oro a todos los jugadores de la run
    /// y muestra efectos visuales y auditivos de monedas de oro.
    /// </summary>
    public class CofreOro : NetworkBehaviour
    {
        public static readonly List<CofreOro> Todos = new List<CofreOro>();

        public NetworkVariable<bool> EstaAbierto = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private SpriteRenderer _sr;
        private TextMesh _textoIndicador;
        private static Sprite _spriteCerrado;
        private static Sprite _spriteAbierto;

        public float distanciaInteraccion = 2.4f;

        void OnEnable()
        {
            if (!Todos.Contains(this)) Todos.Add(this);
        }

        void OnDisable()
        {
            Todos.Remove(this);
        }

        public override void OnDestroy()
        {
            Todos.Remove(this);
            base.OnDestroy();
        }

        public static void DestruirTodos()
        {
            for (int i = Todos.Count - 1; i >= 0; i--)
            {
                if (Todos[i] != null)
                {
                    var no = Todos[i].GetComponent<NetworkObject>();
                    if (no != null && no.IsSpawned) no.Despawn();
                    else Destroy(Todos[i].gameObject);
                }
            }
            Todos.Clear();
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 3;

            AsegurarSpritesProcedurales();
            _sr.sprite = _spriteCerrado;

            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 1.2f);
            col.offset = new Vector2(0f, 0f);

            CrearTextoIndicador();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EstaAbierto.OnValueChanged += (anterior, actual) =>
            {
                if (actual)
                {
                    AplicarVisualAbierto();
                }
            };

            if (EstaAbierto.Value)
            {
                AplicarVisualAbierto();
            }
        }

        void Update()
        {
            if (EstaAbierto.Value)
            {
                if (_textoIndicador != null && _textoIndicador.gameObject.activeSelf)
                {
                    _textoIndicador.gameObject.SetActive(false);
                }
                return;
            }

            Personaje local = null;
            for (int i = 0; i < Personaje.Todos.Count; i++)
            {
                if (Personaje.Todos[i] != null && Personaje.Todos[i].EsControlLocal)
                {
                    local = Personaje.Todos[i];
                    break;
                }
            }
            if (local == null) return;

            float dist = Vector2.Distance(transform.position, local.transform.position);
            bool cerca = dist <= distanciaInteraccion;

            if (_textoIndicador != null)
            {
                _textoIndicador.gameObject.SetActive(cerca);
            }

            if (cerca && Input.GetKeyDown(KeyCode.F))
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    AbrirCofreServerRpc();
                }
                else
                {
                    AbrirCofreLocal();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AbrirCofreServerRpc(ServerRpcParams rpcParams = default)
        {
            if (EstaAbierto.Value) return;
            EstaAbierto.Value = true;

            // Otorgar 50 de oro a todos los héroes
            foreach (var pj in Personaje.Todos)
            {
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.AgregarOro(50);
                }
            }

            NotificarAperturaClientRpc();
        }

        private void AbrirCofreLocal()
        {
            if (EstaAbierto.Value) return;
            EstaAbierto.Value = true;

            foreach (var pj in Personaje.Todos)
            {
                if (pj != null && !pj.EstaMuerto.Value)
                {
                    pj.AgregarOro(50);
                }
            }

            AplicarVisualAbierto();
            EfectosApertura();
        }

        [ClientRpc]
        private void NotificarAperturaClientRpc()
        {
            AplicarVisualAbierto();
            EfectosApertura();
        }

        private void AplicarVisualAbierto()
        {
            if (_sr != null && _spriteAbierto != null)
            {
                _sr.sprite = _spriteAbierto;
            }
            if (_textoIndicador != null)
            {
                _textoIndicador.gameObject.SetActive(false);
            }
        }

        private void EfectosApertura()
        {
            GestorAudio.PlaySFX(TipoSFX.Mejora, 1.1f);
            GestorAudio.PlaySFX(TipoSFX.RecogerOro, 1.25f);
            TextoFlotante.Crear(transform.position + Vector3.up * 1.5f, "¡+50 ORO!", new Color(1f, 0.88f, 0.2f));
        }

        private void CrearTextoIndicador()
        {
            var txtObj = new GameObject("TextoIndicador");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            _textoIndicador = txtObj.AddComponent<TextMesh>();
            _textoIndicador.text = "[F] ABRIR COFRE DE ORO";
            _textoIndicador.fontSize = 24;
            _textoIndicador.characterSize = 0.08f;
            _textoIndicador.alignment = TextAlignment.Center;
            _textoIndicador.anchor = TextAnchor.MiddleCenter;
            _textoIndicador.color = new Color(1f, 0.90f, 0.25f);

            var mr = txtObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

            txtObj.SetActive(false);
        }

        private static void AsegurarSpritesProcedurales()
        {
            if (_spriteCerrado != null && _spriteAbierto != null) return;

            const int w = 24;
            const int h = 20;

            // 1. Sprite Cofre Cerrado
            Texture2D texCerrado = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texCerrado.filterMode = FilterMode.Point;
            Color maderaOscura = new Color(0.36f, 0.20f, 0.09f, 1f);
            Color maderaClara = new Color(0.55f, 0.32f, 0.15f, 1f);
            Color metalBorde = new Color(0.25f, 0.25f, 0.28f, 1f);
            Color oroPuro = new Color(1f, 0.82f, 0.18f, 1f);
            Color oroBrillo = new Color(1f, 0.95f, 0.5f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        texCerrado.SetPixel(x, y, metalBorde);
                    }
                    else if (x == 1 || x == 2 || x == w - 2 || x == w - 3 || y == 10 || y == 11)
                    {
                        texCerrado.SetPixel(x, y, metalBorde);
                    }
                    else if (x >= 10 && x <= 13 && y >= 8 && y <= 12)
                    {
                        texCerrado.SetPixel(x, y, oroPuro);
                    }
                    else if (x == 11 && y == 10)
                    {
                        texCerrado.SetPixel(x, y, oroBrillo);
                    }
                    else
                    {
                        Color cMadera = ((y % 3 == 0) ? maderaOscura : maderaClara);
                        texCerrado.SetPixel(x, y, cMadera);
                    }
                }
            }
            texCerrado.Apply();
            _spriteCerrado = Sprite.Create(texCerrado, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);

            // 2. Sprite Cofre Abierto con Oro reluciente
            Texture2D texAbierto = new Texture2D(w, h + 4, TextureFormat.RGBA32, false);
            texAbierto.filterMode = FilterMode.Point;
            int totalH = h + 4;

            for (int y = 0; y < totalH; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= 10 && y <= 16 && x >= 3 && x <= w - 4)
                    {
                        if ((x + y) % 3 == 0) texAbierto.SetPixel(x, y, oroBrillo);
                        else if ((x * y) % 7 == 0) texAbierto.SetPixel(x, y, new Color(1f, 0.2f, 0.3f));
                        else texAbierto.SetPixel(x, y, oroPuro);
                    }
                    else if (y < 10)
                    {
                        if (x == 0 || x == w - 1 || y == 0) texAbierto.SetPixel(x, y, metalBorde);
                        else if (x <= 2 || x >= w - 3) texAbierto.SetPixel(x, y, metalBorde);
                        else texAbierto.SetPixel(x, y, maderaClara);
                    }
                    else if (y > 16)
                    {
                        if (x == 1 || x == w - 2 || y == totalH - 1) texAbierto.SetPixel(x, y, metalBorde);
                        else texAbierto.SetPixel(x, y, maderaOscura);
                    }
                    else
                    {
                        texAbierto.SetPixel(x, y, Color.clear);
                    }
                }
            }
            texAbierto.Apply();
            _spriteAbierto = Sprite.Create(texAbierto, new Rect(0, 0, w, totalH), new Vector2(0.5f, 0.4f), 16f);
        }

        public static CofreOro CrearCofre(Vector2 pos, Transform padre)
        {
            var go = new GameObject("CofreDeOro");
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.position = (Vector3)pos;

            var cofre = go.AddComponent<CofreOro>();
            return cofre;
        }
    }
}
