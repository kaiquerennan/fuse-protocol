using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public class UIFlasher : MonoBehaviour
    {
        public Image flashImage;

        void Awake()
        {
            if (flashImage != null)
            {
                flashImage.color = new Color(0f, 0f, 0f, 0f);
                flashImage.raycastTarget = false;
            }
        }

        public void FlashRed(float duration = 0.35f)
        {
            StopAllCoroutines();
            StartCoroutine(Flash(new Color(1f, 0f, 0f, 0.85f), duration));
        }

        public void FlashWhite(float duration = 1.2f)
        {
            StopAllCoroutines();
            StartCoroutine(Flash(new Color(1f, 1f, 1f, 1f), duration));
        }

        IEnumerator Flash(Color color, float duration)
        {
            if (flashImage == null) yield break;
            flashImage.color = color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / duration);
                flashImage.color = new Color(color.r, color.g, color.b, color.a * t);
                yield return null;
            }
            flashImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
