using UnityEngine;

namespace LiveWire
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        Vector3 basePosition;
        float intensity;
        float decay;
        bool useWorldSpace;

        void Awake()
        {
            Instance = this;
            CaptureCurrentBase();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetContinuousIntensity(float value)
        {
            intensity = Mathf.Max(intensity, value);
            decay = 0f;
        }

        public void Impulse(float value, float decayRate = 6f)
        {
            intensity = Mathf.Max(intensity, value);
            decay = decayRate;
        }

        void LateUpdate()
        {
            if (intensity > 0.0001f)
            {
                Vector3 offset = new Vector3(
                    (Random.value * 2f - 1f) * intensity,
                    (Random.value * 2f - 1f) * intensity,
                    0f) * 0.08f;
                ApplyPosition(basePosition + offset);

                if (decay > 0f) intensity = Mathf.MoveTowards(intensity, 0f, decay * Time.deltaTime);
            }
            else
            {
                ApplyPosition(basePosition);
            }
        }

        public void ResetIntensity()
        {
            intensity = 0f;
            ApplyPosition(basePosition);
        }

        public void UseWorldSpaceBase(bool worldSpace)
        {
            useWorldSpace = worldSpace;
        }

        public void CaptureCurrentBase()
        {
            useWorldSpace = useWorldSpace || transform.parent == null;
            basePosition = useWorldSpace ? transform.position : transform.localPosition;
        }

        void ApplyPosition(Vector3 target)
        {
            if (useWorldSpace || transform.parent == null)
            {
                transform.position = target;
                return;
            }

            transform.localPosition = target;
        }
    }
}
