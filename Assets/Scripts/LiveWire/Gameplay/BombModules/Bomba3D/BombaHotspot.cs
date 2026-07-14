using System;
using UnityEngine;

namespace LiveWire
{
    [RequireComponent(typeof(Collider))]
    public class BombaHotspot : MonoBehaviour
    {
        public event Action Pressed;
        public event Action<float> Released;
        public event Action HoverEnter;
        public event Action HoverExit;

        [SerializeField] bool startInteractable = true;

        bool interactable;
        bool isHovered;
        bool isPressed;
        float pressStart;

        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value) return;
                interactable = value;
                if (!interactable && isPressed) RaycastReleased();
                if (!interactable && isHovered) RaycastHoverExit();
            }
        }

        public bool IsHovered => isHovered;
        public bool IsPressed => isPressed;
        public float HeldTime => isPressed ? Time.unscaledTime - pressStart : 0f;

        void Awake()
        {
            interactable = startInteractable;
        }

        void OnDisable()
        {
            if (isPressed) RaycastReleased();
            if (isHovered) RaycastHoverExit();
        }

        public void RaycastHoverEnter()
        {
            if (!interactable || isHovered) return;
            isHovered = true;
            HoverEnter?.Invoke();
        }

        public void RaycastHoverExit()
        {
            if (!isHovered) return;
            isHovered = false;
            if (isPressed) RaycastReleased();
            HoverExit?.Invoke();
        }

        public void RaycastPressed()
        {
            if (!interactable || isPressed) return;
            isPressed = true;
            pressStart = Time.unscaledTime;
            Pressed?.Invoke();
        }

        public void RaycastReleased()
        {
            if (!isPressed) return;
            float duration = Time.unscaledTime - pressStart;
            isPressed = false;
            Released?.Invoke(duration);
        }
    }
}
