using UnityEngine;
using UnityEngine.InputSystem;

namespace LiveWire
{
    public class BombaInteractionRaycaster : MonoBehaviour
    {
        public Camera focusCamera;
        public float maxDistance = 5f;
        public LayerMask hitMask = ~0;

        BombaHotspot currentHover;

        void OnDisable()
        {
            ReleaseAndClear();
        }

        void Update()
        {
            // So aceita raycast quando a bomba esta aberta. Isso evita "clicks
            // fantasmas" no mundo enquanto o jogador anda no cenario.
            bool ativo = focusCamera != null
                && Mouse.current != null
                && GerenciadorDeBomba.Instance != null
                && GerenciadorDeBomba.Instance.IsOpen;

            if (!ativo)
            {
                ReleaseAndClear();
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = focusCamera.ScreenPointToRay(mousePos);

            BombaHotspot hit = null;
            if (Physics.Raycast(ray, out RaycastHit info, maxDistance, hitMask, QueryTriggerInteraction.Collide))
                hit = info.collider.GetComponentInParent<BombaHotspot>();

            UpdateHover(hit);
            UpdatePress();
        }

        void UpdateHover(BombaHotspot hit)
        {
            if (hit == currentHover) return;

            // Solta o press antes de trocar o hover, evitando "click fantasma" em outro hotspot.
            if (currentHover != null && currentHover.IsPressed) currentHover.RaycastReleased();
            currentHover?.RaycastHoverExit();

            currentHover = hit;
            currentHover?.RaycastHoverEnter();
        }

        void UpdatePress()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame && currentHover != null)
                currentHover.RaycastPressed();
            else if (Mouse.current.leftButton.wasReleasedThisFrame && currentHover != null && currentHover.IsPressed)
                currentHover.RaycastReleased();
        }

        void ReleaseAndClear()
        {
            if (currentHover == null) return;
            if (currentHover.IsPressed) currentHover.RaycastReleased();
            currentHover.RaycastHoverExit();
            currentHover = null;
        }
    }
}
