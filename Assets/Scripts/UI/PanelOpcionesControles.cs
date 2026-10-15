using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Pantalla de Opciones independiente para reasignar controles (teclas de movimiento, correr, habilidades, etc.)
    /// </summary>
    public class PanelOpcionesControles : MonoBehaviour
    {
        public static PanelOpcionesControles Instancia { get; private set; }

        public GameObject panelContenedor;
        public Transform contenedorFilas;
        public Button botonCerrar;
        public Button botonRestaurar;
        public Text textoEstado;

        private AccionControl? _accionEsperandoTecla = null;
        private Text _textoBotonEsperando = null;

        private readonly Dictionary<AccionControl, Button> _botonesAccion = new Dictionary<AccionControl, Button>();
        private readonly Dictionary<AccionControl, Text> _textosAccion = new Dictionary<AccionControl, Text>();

        void Awake()
        {
            Instancia = this;
        }

        public static PanelOpcionesControles ObtenerOCrear(Transform parent)
        {
            if (Instancia != null) return Instancia;

            var go = new GameObject("PanelOpcionesControles");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var imgFondo = go.AddComponent<Image>();
            imgFondo.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);

            var comp = go.AddComponent<PanelOpcionesControles>();
            comp.panelContenedor = go;

            // Titulo
            var titObj = new GameObject("Titulo");
            titObj.transform.SetParent(go.transform, false);
            var txtTit = titObj.AddComponent<Text>();
            txtTit.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtTit.fontSize = 20;
            txtTit.fontStyle = FontStyle.Bold;
            txtTit.alignment = TextAnchor.MiddleCenter;
            txtTit.color = new Color(1f, 0.85f, 0.25f, 1f);
            txtTit.text = "⚙️ REASIGNAR CONTROLES";
            var rtTit = titObj.GetComponent<RectTransform>();
            rtTit.anchorMin = new Vector2(0.5f, 1f);
            rtTit.anchorMax = new Vector2(0.5f, 1f);
            rtTit.pivot = new Vector2(0.5f, 1f);
            rtTit.anchoredPosition = new Vector2(0f, -25f);
            rtTit.sizeDelta = new Vector2(600f, 32f);

            // Estado / Subtítulo
            var estObj = new GameObject("TextoEstado");
            estObj.transform.SetParent(go.transform, false);
            var txtEst = estObj.AddComponent<Text>();
            txtEst.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtEst.fontSize = 11;
            txtEst.alignment = TextAnchor.MiddleCenter;
            txtEst.color = new Color(0.75f, 0.85f, 1f, 1f);
            txtEst.text = "Haz clic en una tecla para cambiarla. Presiona ESC para cancelar.";
            var rtEst = estObj.GetComponent<RectTransform>();
            rtEst.anchorMin = new Vector2(0.5f, 1f);
            rtEst.anchorMax = new Vector2(0.5f, 1f);
            rtEst.pivot = new Vector2(0.5f, 1f);
            rtEst.anchoredPosition = new Vector2(0f, -60f);
            rtEst.sizeDelta = new Vector2(600f, 24f);
            comp.textoEstado = txtEst;

            // Contenedor Filas con VerticalLayoutGroup
            var contFilasObj = new GameObject("ContenedorFilas");
            contFilasObj.transform.SetParent(go.transform, false);
            var vlg = contFilasObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 6f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            var rtFilas = contFilasObj.GetComponent<RectTransform>();
            rtFilas.anchorMin = new Vector2(0.5f, 0.5f);
            rtFilas.anchorMax = new Vector2(0.5f, 0.5f);
            rtFilas.pivot = new Vector2(0.5f, 0.5f);
            rtFilas.anchoredPosition = new Vector2(0f, 10f);
            rtFilas.sizeDelta = new Vector2(500f, 380f);
            comp.contenedorFilas = contFilasObj.transform;

            // Botón Restaurar por Defecto
            var btnRestObj = new GameObject("BotonRestaurar");
            btnRestObj.transform.SetParent(go.transform, false);
            var imgRest = btnRestObj.AddComponent<Image>();
            imgRest.color = new Color(0.45f, 0.25f, 0.25f, 1f);
            var rtRest = btnRestObj.GetComponent<RectTransform>();
            rtRest.anchorMin = new Vector2(0.5f, 0f);
            rtRest.anchorMax = new Vector2(0.5f, 0f);
            rtRest.pivot = new Vector2(0.5f, 0f);
            rtRest.anchoredPosition = new Vector2(-120f, 25f);
            rtRest.sizeDelta = new Vector2(210f, 38f);
            comp.botonRestaurar = btnRestObj.AddComponent<Button>();

            var txtRestObj = new GameObject("Texto");
            txtRestObj.transform.SetParent(btnRestObj.transform, false);
            var txtRest = txtRestObj.AddComponent<Text>();
            txtRest.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtRest.fontSize = 11;
            txtRest.fontStyle = FontStyle.Bold;
            txtRest.alignment = TextAnchor.MiddleCenter;
            txtRest.color = Color.white;
            txtRest.text = "RESTAURAR POR DEFECTO";
            var rtTxtRest = txtRestObj.GetComponent<RectTransform>();
            rtTxtRest.anchorMin = Vector2.zero;
            rtTxtRest.anchorMax = Vector2.one;
            rtTxtRest.sizeDelta = Vector2.zero;

            // Botón Cerrar
            var btnCerrarObj = new GameObject("BotonCerrar");
            btnCerrarObj.transform.SetParent(go.transform, false);
            var imgCerrar = btnCerrarObj.AddComponent<Image>();
            imgCerrar.color = new Color(0.2f, 0.55f, 0.3f, 1f);
            var rtCerrar = btnCerrarObj.GetComponent<RectTransform>();
            rtCerrar.anchorMin = new Vector2(0.5f, 0f);
            rtCerrar.anchorMax = new Vector2(0.5f, 0f);
            rtCerrar.pivot = new Vector2(0.5f, 0f);
            rtCerrar.anchoredPosition = new Vector2(120f, 25f);
            rtCerrar.sizeDelta = new Vector2(210f, 38f);
            comp.botonCerrar = btnCerrarObj.AddComponent<Button>();

            var txtCerrarObj = new GameObject("Texto");
            txtCerrarObj.transform.SetParent(btnCerrarObj.transform, false);
            var txtCerrar = txtCerrarObj.AddComponent<Text>();
            txtCerrar.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtCerrar.fontSize = 12;
            txtCerrar.fontStyle = FontStyle.Bold;
            txtCerrar.alignment = TextAnchor.MiddleCenter;
            txtCerrar.color = Color.white;
            txtCerrar.text = "CERRAR Y GUARDAR";
            var rtTxtCerrar = txtCerrarObj.GetComponent<RectTransform>();
            rtTxtCerrar.anchorMin = Vector2.zero;
            rtTxtCerrar.anchorMax = Vector2.one;
            rtTxtCerrar.sizeDelta = Vector2.zero;

            comp.botonCerrar.onClick.AddListener(comp.Cerrar);
            comp.botonRestaurar.onClick.AddListener(comp.RestaurarPorDefecto);
            comp.ConstruirFilas();

            go.SetActive(false);
            return comp;
        }

        void Start()
        {
            if (botonCerrar) botonCerrar.onClick.AddListener(Cerrar);
            if (botonRestaurar) botonRestaurar.onClick.AddListener(RestaurarPorDefecto);
            ConstruirFilas();
        }

        void Update()
        {
            if (_accionEsperandoTecla.HasValue)
            {
                // Esperar a que el usuario presione cualquier tecla
                foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kc))
                    {
                        if (kc == KeyCode.Escape)
                        {
                            // Cancelar
                            FinalizarEsperaTecla();
                        }
                        else
                        {
                            AsignarNuevaTecla(_accionEsperandoTecla.Value, kc);
                        }
                        break;
                    }
                }
            }
        }

        public void Abrir()
        {
            if (panelContenedor) panelContenedor.SetActive(true);
            ActualizarTextos();
        }

        public void Cerrar()
        {
            FinalizarEsperaTecla();
            if (panelContenedor) panelContenedor.SetActive(false);
        }

        private void RestaurarPorDefecto()
        {
            GestorControles.RestaurarPorDefecto();
            ActualizarTextos();
            if (textoEstado) textoEstado.text = "¡Controles restaurados por defecto!";
        }

        private void ConstruirFilas()
        {
            if (contenedorFilas == null) return;

            foreach (Transform t in contenedorFilas) Destroy(t.gameObject);
            _botonesAccion.Clear();
            _textosAccion.Clear();

            var acciones = (AccionControl[])Enum.GetValues(typeof(AccionControl));
            foreach (var accion in acciones)
            {
                var fila = new GameObject($"Fila_{accion}");
                fila.transform.SetParent(contenedorFilas, false);
                var rtFila = fila.AddComponent<RectTransform>();
                rtFila.sizeDelta = new Vector2(460, 36);

                // Nombre de la acción
                var txtNomObj = new GameObject("NombreAccion");
                txtNomObj.transform.SetParent(fila.transform, false);
                var txtNom = txtNomObj.AddComponent<Text>();
                txtNom.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtNom.fontSize = 12;
                txtNom.alignment = TextAnchor.MiddleLeft;
                txtNom.color = Color.white;
                txtNom.rectTransform.anchoredPosition = new Vector2(-110, 0);
                txtNom.rectTransform.sizeDelta = new Vector2(220, 34);
                txtNom.text = ObtenerNombreLegible(accion);

                // Botón con la tecla actual
                var btnObj = new GameObject("BotonTecla");
                btnObj.transform.SetParent(fila.transform, false);
                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
                var rtBtn = btnObj.GetComponent<RectTransform>();
                rtBtn.anchoredPosition = new Vector2(130, 0);
                rtBtn.sizeDelta = new Vector2(150, 32);

                var btn = btnObj.AddComponent<Button>();

                var txtTeclaObj = new GameObject("TextoTecla");
                txtTeclaObj.transform.SetParent(btnObj.transform, false);
                var txtTecla = txtTeclaObj.AddComponent<Text>();
                txtTecla.font = FuenteUtil.ObtenerFuentePixelArt() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txtTecla.fontSize = 12;
                txtTecla.alignment = TextAnchor.MiddleCenter;
                txtTecla.color = Color.yellow;
                txtTecla.rectTransform.sizeDelta = new Vector2(150, 32);

                AccionControl act = accion;
                btn.onClick.AddListener(() => IniciarEsperaTecla(act, txtTecla));

                _botonesAccion[accion] = btn;
                _textosAccion[accion] = txtTecla;
            }

            ActualizarTextos();
        }

        private void IniciarEsperaTecla(AccionControl accion, Text txtBoton)
        {
            _accionEsperandoTecla = accion;
            _textoBotonEsperando = txtBoton;
            txtBoton.text = "[Presiona tecla...]";
            if (textoEstado) textoEstado.text = $"Presiona cualquier tecla para '{ObtenerNombreLegible(accion)}' (ESC para cancelar)";
        }

        private void AsignarNuevaTecla(AccionControl accion, KeyCode nuevaTecla)
        {
            GestorControles.AsignarTecla(accion, nuevaTecla);
            FinalizarEsperaTecla();
            ActualizarTextos();
            if (textoEstado) textoEstado.text = $"Asignado: {ObtenerNombreLegible(accion)} -> {nuevaTecla}";
        }

        private void FinalizarEsperaTecla()
        {
            _accionEsperandoTecla = null;
            _textoBotonEsperando = null;
        }

        private void ActualizarTextos()
        {
            foreach (var kvp in _textosAccion)
            {
                KeyCode tecla = GestorControles.ObtenerTecla(kvp.Key);
                kvp.Value.text = tecla.ToString();
            }
        }

        private string ObtenerNombreLegible(AccionControl accion)
        {
            return accion switch
            {
                AccionControl.MoverArriba => "Mover Arriba",
                AccionControl.MoverAbajo => "Mover Abajo",
                AccionControl.MoverIzquierda => "Mover Izquierda",
                AccionControl.MoverDerecha => "Mover Derecha",
                AccionControl.Correr => "Correr (Sprint)",
                AccionControl.AtaquePrincipal => "Ataque Principal",
                AccionControl.Habilidad1 => "Habilidad 1 (E)",
                AccionControl.Habilidad2 => "Habilidad 2 (Q)",
                AccionControl.Ultimate => "Ultimate (R)",
                AccionControl.Pausa => "Pausa (Esc)",
                _ => accion.ToString()
            };
        }
    }
}
