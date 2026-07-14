using UnityEngine;
using UnityEngine.InputSystem;

namespace LiveWire
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 4.5f;
        public float mouseSensitivity = 0.12f;
        public float gravity = -18f;
        public float interactRange = 2.8f;
        public Camera playerCamera;

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        bool inputLocked;

        Transform cachedCamParent;
        Vector3 cachedCamLocalPos;
        Quaternion cachedCamLocalRot;
        bool cameraDetached;

        public static PlayerController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            SetCursorUnlocked(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (!locked) SetCursorUnlocked(false);
        }

        public void SetCursorUnlocked(bool unlocked)
        {
            Cursor.lockState = unlocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = unlocked;
        }

        public void DetachCameraForFocus()
        {
            if (playerCamera == null || cameraDetached) return;
            Transform camT = playerCamera.transform;
            cachedCamParent = camT.parent;
            cachedCamLocalPos = camT.localPosition;
            cachedCamLocalRot = camT.localRotation;
            camT.SetParent(null, true);
            cameraDetached = true;
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.UseWorldSpaceBase(true);
                CameraShake.Instance.CaptureCurrentBase();
                CameraShake.Instance.enabled = false;
            }
        }

        public void ReattachCamera()
        {
            if (playerCamera == null || !cameraDetached) return;
            Transform camT = playerCamera.transform;
            camT.SetParent(cachedCamParent, true);
            camT.localPosition = cachedCamLocalPos;
            camT.localRotation = cachedCamLocalRot;
            cameraDetached = false;
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.UseWorldSpaceBase(false);
                CameraShake.Instance.CaptureCurrentBase();
                CameraShake.Instance.enabled = true;
            }
        }

        void Update()
        {
            if (playerCamera == null) return;

            HandleLook();
            HandleMove();
            HandleInteract();
        }

        void HandleLook()
        {
            if (inputLocked || Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            transform.Rotate(0f, delta.x, 0f, Space.World);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleMove()
        {
            Vector2 input = Vector2.zero;
            if (!inputLocked && Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (input.sqrMagnitude > 1f) input.Normalize();
            }

            Vector3 move = transform.right * input.x + transform.forward * input.y;
            move *= moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }

        void HandleInteract()
        {
            if (inputLocked || Keyboard.current == null) return;
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            BombManager bomb = BombManager.Instance;
            if (bomb != null && bomb.CanInteractFrom(this))
            {
                bomb.Interact(this);
                return;
            }

            Vector3 origin = playerCamera.transform.position;
            Vector3 dir = playerCamera.transform.forward;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, interactRange))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                interactable?.Interact(this);
            }
        }

        public bool IsNearInteractable(out IInteractable interactable)
        {
            interactable = null;
            BombManager bomb = BombManager.Instance;
            if (bomb != null && bomb.CanInteractFrom(this))
            {
                interactable = bomb;
                return true;
            }
            return false;
        }
    }

    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetPrompt();
    }
}
