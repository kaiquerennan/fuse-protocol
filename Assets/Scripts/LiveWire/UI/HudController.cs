using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class HudController : MonoBehaviour
    {
        public Text timerText;
        public Text phaseText;
        public Text strikesText;
        public Text promptText;
        public Text messageText;
        public Image redBorderTop;
        public Image redBorderBottom;
        public Image redBorderLeft;
        public Image redBorderRight;

        static Font cachedMonoFont;
        Coroutine messageRoutine;

        public static Font GetMonoFont()
        {
            if (cachedMonoFont != null) return cachedMonoFont;

            string[] osFonts = { "Courier New", "Courier", "Consolas", "Monaco", "DejaVu Sans Mono", "Liberation Mono", "Menlo", "monospace" };
            try
            {
                cachedMonoFont = Font.CreateDynamicFontFromOSFont(osFonts, 32);
            }
            catch { cachedMonoFont = null; }

            if (cachedMonoFont == null)
            {
                cachedMonoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return cachedMonoFont;
        }

        void Update()
        {
            UpdateTimer();
            UpdatePhase();
            UpdateStrikes();
            UpdatePrompt();
            UpdateBorderPulse();
        }

        void UpdateTimer()
        {
            if (timerText == null || TimerController.Instance == null) return;

            float remaining = TimerController.Instance.Remaining;
            timerText.text = FormatTime(remaining);

            if (remaining <= 10f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                timerText.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(1f, 0.8f, 0.6f), pulse);
                timerText.fontSize = Mathf.RoundToInt(Mathf.Lerp(78f, 96f, pulse));
            }
            else
            {
                timerText.color = new Color(0.9f, 1f, 0.9f);
                timerText.fontSize = 78;
            }
        }

        void UpdatePhase()
        {
            if (phaseText == null || GameManager.Instance == null) return;
            phaseText.text = $"FASE {GameManager.Instance.CurrentPhase:00}";
        }

        void UpdateStrikes()
        {
            if (strikesText == null)
                return;

            GerenciadorDeBomba gerenciador = GerenciadorDeBomba.Instance;
            if (gerenciador == null)
            {
                strikesText.text = string.Empty;
                return;
            }

            strikesText.text = $"ERROS {gerenciador.Strikes}/{gerenciador.MaxStrikes}";
            strikesText.color = gerenciador.Strikes switch
            {
                0 => new Color(0.78f, 0.94f, 0.84f),
                1 => new Color(1f, 0.86f, 0.48f),
                2 => new Color(1f, 0.58f, 0.34f),
                _ => new Color(1f, 0.32f, 0.32f),
            };
        }

        void UpdatePrompt()
        {
            if (promptText == null || PlayerController.Instance == null)
            {
                if (promptText != null) promptText.text = string.Empty;
                return;
            }

            BombManager bomb = BombManager.Instance;
            bool painelAberto = bomb != null && bomb.IsFocused;

            if (!painelAberto && PlayerController.Instance.IsNearInteractable(out var interactable))
            {
                float blink = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5.2f);
                promptText.text = interactable.GetPrompt();
                promptText.color = new Color(1f, 0.9f, 0.5f, blink);
            }
            else
            {
                promptText.text = string.Empty;
            }
        }

        void UpdateBorderPulse()
        {
            if (TimerController.Instance == null) return;
            float remaining = TimerController.Instance.Remaining;
            float alpha = 0f;

            if (remaining <= 10f && TimerController.Instance.Running)
            {
                float beat = Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 5.6f) * 0.5f + 0.5f);
                float intensityScale = Mathf.InverseLerp(10f, 2f, remaining);
                alpha = Mathf.Lerp(0.25f, 0.9f, beat) * Mathf.Lerp(0.6f, 1f, intensityScale);
            }

            Color borderCol = new Color(1f, 0.05f, 0.05f, alpha);
            if (redBorderTop != null) redBorderTop.color = borderCol;
            if (redBorderBottom != null) redBorderBottom.color = borderCol;
            if (redBorderLeft != null) redBorderLeft.color = borderCol;
            if (redBorderRight != null) redBorderRight.color = borderCol;
        }

        string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int s = Mathf.FloorToInt(seconds);
            int ms = Mathf.FloorToInt((seconds - s) * 100f);
            return $"{s:00}:{ms:00}";
        }

        public void ShowPhaseComplete(float remainingTime)
        {
            ShowTemporaryMessage($"FASE {GameManager.Instance?.CurrentPhase ?? 0} COMPLETA\n{remainingTime:0.0}s", 1.8f, new Color(0.4f, 1.2f, 0.6f, 1f));
        }

        public void ShowTemporaryMessage(string text, float duration, Color color)
        {
            if (messageRoutine != null)
                StopCoroutine(messageRoutine);

            messageRoutine = StartCoroutine(ShowMessage(text, duration, color));
        }

        IEnumerator ShowMessage(string text, float duration, Color color)
        {
            if (messageText == null) yield break;
            messageText.text = text;
            messageText.color = color;
            yield return new WaitForSecondsRealtime(duration);
            messageText.text = string.Empty;
            messageRoutine = null;
        }
    }
}
