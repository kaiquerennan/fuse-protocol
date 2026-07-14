using System;
using UnityEngine;

namespace LiveWire
{
    public class TimerController : MonoBehaviour
    {
        public static TimerController Instance { get; private set; }

        public float Remaining { get; private set; }
        public float TotalTime { get; private set; }
        public bool Running { get; private set; }
        float freezeCountdown;

        public event Action<float> OnTick;
        public event Action OnTimeout;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(float seconds)
        {
            TotalTime = seconds;
            Remaining = seconds;
            Running = true;
            freezeCountdown = 0f;
            AudioManager.Instance?.SetTickIntensity(0f);
            AudioManager.Instance?.StartTicking();
        }

        public void Stop()
        {
            Running = false;
            freezeCountdown = 0f;
            AudioManager.Instance?.StopTicking();
            AudioManager.Instance?.StopHiss();
        }

        public void ApplyPenalty(float seconds)
        {
            Remaining = Mathf.Max(0f, Remaining - seconds);
        }

        public void AddTime(float seconds)
        {
            Remaining = Mathf.Max(0f, Remaining + seconds);
            TotalTime = Mathf.Max(TotalTime, Remaining);
            OnTick?.Invoke(Remaining);
        }

        public void PauseCountdown(float seconds)
        {
            freezeCountdown = Mathf.Max(freezeCountdown, seconds);
        }

        void Update()
        {
            if (!Running) return;

            if (freezeCountdown > 0f)
            {
                freezeCountdown = Mathf.Max(0f, freezeCountdown - Time.unscaledDeltaTime);
                OnTick?.Invoke(Remaining);
                return;
            }

            Remaining -= Time.deltaTime;
            OnTick?.Invoke(Remaining);

            float urgency = 1f - Mathf.Clamp01(Remaining / TotalTime);
            AudioManager.Instance?.SetTickIntensity(urgency);

            if (Remaining <= 10f)
            {
                float hissT = Mathf.InverseLerp(10f, 0f, Remaining);
                AudioManager.Instance?.SetHissIntensity(hissT);
            }

            if (Remaining <= 5f && CameraShake.Instance != null)
            {
                float shakeT = Mathf.InverseLerp(5f, 0f, Remaining);
                CameraShake.Instance.SetContinuousIntensity(Mathf.Lerp(0.1f, 0.6f, shakeT));
            }

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;
                AudioManager.Instance?.StopTicking();
                AudioManager.Instance?.StopHiss();
                OnTimeout?.Invoke();
            }
        }
    }
}
