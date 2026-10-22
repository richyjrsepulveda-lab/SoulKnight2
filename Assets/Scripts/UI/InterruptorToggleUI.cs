using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoulKnight
{
    /// <summary>
    /// Interruptor pixel-art de 220x56px con perilla deslizable y estados ACTIVADO / DESACTIVADO
    /// según la especificación de diseño oficial de "Just a Cat Adventure".
    /// </summary>
    public class InterruptorToggleUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsOn { get; private set; }
        public Action<bool> OnValueChanged;

        private Image _imgPista;
        private RectTransform _rtKnob;
        private Text _txtEstado;
        private Button _btn;
        private RectTransform _rtRoot;

        public static InterruptorToggleUI Crear(Transform parent, string nombre, bool valorInicial, Action<bool> onToggle)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 56f);

            var toggle = go.AddComponent<InterruptorToggleUI>();
            toggle.Inicializar(valorInicial, onToggle);
            return toggle;
        }

        private void Inicializar(bool valorInicial, Action<bool> onToggle)
        {
            _rtRoot = GetComponent<RectTransform>();
            IsOn = valorInicial;
            OnValueChanged = onToggle;

            _imgPista = gameObject.AddComponent<Image>();
            _imgPista.type = Image.Type.Sliced;

            _btn = gameObject.AddComponent<Button>();
            _btn.transition = Selectable.Transition.ColorTint;
            var cb = _btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cb.selectedColor = Color.white;
            _btn.colors = cb;
            _btn.onClick.AddListener(AlHacerClick);

            // Texto de Estado ("ACTIVADO" / "DESACTIVADO")
            var txtObj = new GameObject("TextoEstado");
            txtObj.transform.SetParent(transform, false);
            _txtEstado = txtObj.AddComponent<Text>();
            _txtEstado.font = FuenteUtil.ObtenerFuentePixelify();
            _txtEstado.fontSize = 24;
            _txtEstado.fontStyle = FontStyle.Bold;
            _txtEstado.raycastTarget = false;
            var rtTxt = txtObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = new Vector2(0f, 0f);
            rtTxt.anchorMax = new Vector2(1f, 1f);
            rtTxt.sizeDelta = Vector2.zero;

            // Perilla (Knob 36x36)
            var knobObj = new GameObject("Knob");
            knobObj.transform.SetParent(transform, false);
            var imgKnob = knobObj.AddComponent<Image>();
            imgKnob.sprite = EstiloUIUX.ObtenerSpriteKnob();
            imgKnob.raycastTarget = false;
            _rtKnob = knobObj.GetComponent<RectTransform>();
            _rtKnob.sizeDelta = new Vector2(36f, 36f);

            ActualizarVisual();
        }

        public void SetValor(bool nuevoValor, bool dispararEvento = false)
        {
            IsOn = nuevoValor;
            ActualizarVisual();
            if (dispararEvento)
            {
                OnValueChanged?.Invoke(IsOn);
            }
        }

        private void AlHacerClick()
        {
            IsOn = !IsOn;
            ActualizarVisual();
            GestorAudio.PlaySFX(TipoSFX.ClickUI, 1.05f);
            OnValueChanged?.Invoke(IsOn);
        }

        public void ActualizarVisual()
        {
            if (_imgPista == null || _rtKnob == null || _txtEstado == null) return;

            if (IsOn)
            {
                _imgPista.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Verde);
                _txtEstado.text = "ACTIVADO";
                _txtEstado.color = EstiloUIUX.ColBtnVerdeTexto;
                _txtEstado.alignment = TextAnchor.MiddleLeft;

                var rtTxt = _txtEstado.GetComponent<RectTransform>();
                rtTxt.offsetMin = new Vector2(22f, 0f);
                rtTxt.offsetMax = new Vector2(-48f, 0f);

                // Perilla a la derecha
                _rtKnob.anchorMin = new Vector2(1f, 0.5f);
                _rtKnob.anchorMax = new Vector2(1f, 0.5f);
                _rtKnob.pivot = new Vector2(1f, 0.5f);
                _rtKnob.anchoredPosition = new Vector2(-10f, 0f);
            }
            else
            {
                _imgPista.sprite = EstiloUIUX.ObtenerSpriteBoton(TipoBotonUI.Piedra);
                _txtEstado.text = "DESACTIVADO";
                _txtEstado.color = EstiloUIUX.ColBtnPiedraTexto;
                _txtEstado.alignment = TextAnchor.MiddleRight;

                var rtTxt = _txtEstado.GetComponent<RectTransform>();
                rtTxt.offsetMin = new Vector2(48f, 0f);
                rtTxt.offsetMax = new Vector2(-22f, 0f);

                // Perilla a la izquierda
                _rtKnob.anchorMin = new Vector2(0f, 0.5f);
                _rtKnob.anchorMax = new Vector2(0f, 0.5f);
                _rtKnob.pivot = new Vector2(0f, 0.5f);
                _rtKnob.anchoredPosition = new Vector2(10f, 0f);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rtRoot != null) _rtRoot.anchoredPosition += new Vector2(0f, -4f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_rtRoot != null) _rtRoot.anchoredPosition -= new Vector2(0f, -4f);
        }
    }
}
