using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Selector horizontal pixel-art de 440x56px con botones de flecha izquierda/derecha
    /// y caja de valor central según la especificación de diseño oficial.
    /// </summary>
    public class SelectorOpcionesUI : MonoBehaviour
    {
        public Action OnAnterior;
        public Action OnSiguiente;

        private Button _btnIzq;
        private Button _btnDer;
        private Image _imgCajaValor;
        private Image _imgIcono;
        private Text _txtValor;

        public static SelectorOpcionesUI Crear(Transform parent, string nombre, Action onAnt, Action onSig)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 56f);

            var sel = go.AddComponent<SelectorOpcionesUI>();
            sel.Inicializar(onAnt, onSig);
            return sel;
        }

        private void Inicializar(Action onAnt, Action onSig)
        {
            OnAnterior = onAnt;
            OnSiguiente = onSig;

            // Botón Flecha Izquierda (56x56)
            var btnIzqObj = new GameObject("BotonIzq");
            btnIzqObj.transform.SetParent(transform, false);
            var rtIzq = btnIzqObj.AddComponent<RectTransform>();
            rtIzq.anchorMin = new Vector2(0f, 0.5f);
            rtIzq.anchorMax = new Vector2(0f, 0.5f);
            rtIzq.pivot = new Vector2(0f, 0.5f);
            rtIzq.anchoredPosition = new Vector2(0f, 0f);
            rtIzq.sizeDelta = new Vector2(56f, 56f);

            var imgIzq = btnIzqObj.AddComponent<Image>();
            imgIzq.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Piedra);
            imgIzq.type = Image.Type.Sliced;

            _btnIzq = btnIzqObj.AddComponent<Button>();
            ConfigurarBotonPixel(_btnIzq, () =>
            {
                GestorAudio.PlaySFX(TipoSFX.ClickUI, 1.0f);
                OnAnterior?.Invoke();
            });

            // Ícono flecha izquierda (15x24)
            var icoIzqObj = new GameObject("Icono");
            icoIzqObj.transform.SetParent(btnIzqObj.transform, false);
            var imgIcoIzq = icoIzqObj.AddComponent<Image>();
            imgIcoIzq.sprite = EstiloUIUX.ObtenerSpriteIcono("flecha_izq", EstiloUIUX.ColBtnPiedraTexto);
            imgIcoIzq.raycastTarget = false;
            var rtIcoIzq = icoIzqObj.GetComponent<RectTransform>();
            rtIcoIzq.sizeDelta = new Vector2(15f, 24f);

            // Botón Flecha Derecha (56x56)
            var btnDerObj = new GameObject("BotonDer");
            btnDerObj.transform.SetParent(transform, false);
            var rtDer = btnDerObj.AddComponent<RectTransform>();
            rtDer.anchorMin = new Vector2(1f, 0.5f);
            rtDer.anchorMax = new Vector2(1f, 0.5f);
            rtDer.pivot = new Vector2(1f, 0.5f);
            rtDer.anchoredPosition = new Vector2(0f, 0f);
            rtDer.sizeDelta = new Vector2(56f, 56f);

            var imgDer = btnDerObj.AddComponent<Image>();
            imgDer.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Piedra);
            imgDer.type = Image.Type.Sliced;

            _btnDer = btnDerObj.AddComponent<Button>();
            ConfigurarBotonPixel(_btnDer, () =>
            {
                GestorAudio.PlaySFX(TipoSFX.ClickUI, 1.0f);
                OnSiguiente?.Invoke();
            });

            // Ícono flecha derecha (15x24)
            var icoDerObj = new GameObject("Icono");
            icoDerObj.transform.SetParent(btnDerObj.transform, false);
            var imgIcoDer = icoDerObj.AddComponent<Image>();
            imgIcoDer.sprite = EstiloUIUX.ObtenerSpriteIcono("flecha_der", EstiloUIUX.ColBtnPiedraTexto);
            imgIcoDer.raycastTarget = false;
            var rtIcoDer = icoDerObj.GetComponent<RectTransform>();
            rtIcoDer.sizeDelta = new Vector2(15f, 24f);

            // Caja Central de Valor (300x56, gap 14px a cada lado)
            var cajaObj = new GameObject("CajaValor");
            cajaObj.transform.SetParent(transform, false);
            var rtCaja = cajaObj.AddComponent<RectTransform>();
            rtCaja.anchorMin = new Vector2(0.5f, 0.5f);
            rtCaja.anchorMax = new Vector2(0.5f, 0.5f);
            rtCaja.pivot = new Vector2(0.5f, 0.5f);
            rtCaja.anchoredPosition = new Vector2(0f, 0f);
            rtCaja.sizeDelta = new Vector2(300f, 56f);

            _imgCajaValor = cajaObj.AddComponent<Image>();
            _imgCajaValor.sprite = EstiloUIUX.ObtenerSpriteValorSelector();
            _imgCajaValor.type = Image.Type.Sliced;

            var hlg = cajaObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 12f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Ícono opcional dentro de la caja (ej. estrella en calidad)
            var icoValObj = new GameObject("IconoValor");
            icoValObj.transform.SetParent(cajaObj.transform, false);
            _imgIcono = icoValObj.AddComponent<Image>();
            _imgIcono.raycastTarget = false;
            var rtIcoVal = icoValObj.GetComponent<RectTransform>();
            rtIcoVal.sizeDelta = new Vector2(24f, 24f);
            icoValObj.SetActive(false);

            // Texto de valor
            var txtValObj = new GameObject("TextoValor");
            txtValObj.transform.SetParent(cajaObj.transform, false);
            _txtValor = txtValObj.AddComponent<Text>();
            _txtValor.font = FuenteUtil.ObtenerFuentePixelify();
            _txtValor.fontSize = 26;
            _txtValor.fontStyle = FontStyle.Bold;
            _txtValor.alignment = TextAnchor.MiddleCenter;
            _txtValor.raycastTarget = false;
            _txtValor.color = EstiloUIUX.ColTextoCrema;
            var rtTxt = txtValObj.GetComponent<RectTransform>();
            rtTxt.sizeDelta = new Vector2(240f, 50f);
        }

        private static void ConfigurarBotonPixel(Button btn, Action onClick)
        {
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            btn.onClick.AddListener(() => onClick?.Invoke());

            // Efecto de presión 4px hacia abajo
            var trigger = btn.gameObject.AddComponent<EventTrigger>();
            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { btn.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -4f); });
            trigger.triggers.Add(entryDown);

            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { btn.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0f, -4f); });
            trigger.triggers.Add(entryUp);
        }

        public void EstablecerValor(string texto, Color colorTexto, Sprite icono = null, Color? colorIcono = null)
        {
            if (_txtValor != null)
            {
                _txtValor.text = texto;
                _txtValor.color = colorTexto;
            }

            if (_imgIcono != null)
            {
                if (icono != null)
                {
                    _imgIcono.sprite = icono;
                    _imgIcono.color = colorIcono ?? colorTexto;
                    _imgIcono.gameObject.SetActive(true);
                }
                else
                {
                    _imgIcono.gameObject.SetActive(false);
                }
            }
        }
    }
}
