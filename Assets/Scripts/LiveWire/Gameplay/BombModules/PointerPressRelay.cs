using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LiveWire
{
    public class PointerPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action<float> Released;

        float pressionadoDesde;
        bool pressionado;

        public bool IsPressed => pressionado;
        public float HeldTime => pressionado ? Time.unscaledTime - pressionadoDesde : 0f;

        void Update()
        {
            if (!pressionado || Mouse.current == null) return;
            if (!Mouse.current.leftButton.isPressed) FinalizarPressao();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressionado = true;
            pressionadoDesde = Time.unscaledTime;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            FinalizarPressao();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Mouse.current == null || Mouse.current.leftButton.isPressed) return;
            FinalizarPressao();
        }

        public void CancelPress()
        {
            pressionado = false;
        }

        void OnDisable()
        {
            pressionado = false;
        }

        void FinalizarPressao()
        {
            if (!pressionado) return;

            float duracao = Time.unscaledTime - pressionadoDesde;
            pressionado = false;
            Released?.Invoke(duracao);
        }
    }
}
