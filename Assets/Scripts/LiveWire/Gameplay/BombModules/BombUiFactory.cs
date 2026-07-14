using UnityEngine;
using UnityEngine.UI;

namespace LiveWire
{
    public static class BombUiFactory
    {
        public static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        public static Image CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = go.GetComponent<Image>();
            image.sprite = SceneBuildHelpers.GetWhiteSprite();
            image.color = color;
            return image;
        }

        public static Image CreateImage(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            return CreatePanel(parent, name, color, anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot);
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2? pivot = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Text label = go.GetComponent<Text>();
            label.font = HudController.GetMonoFont();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            return label;
        }

        public static Button AddButton(Image image, Color normal, Color highlighted, Color pressed)
        {
            Button button = image.GetComponent<Button>();
            if (button == null) button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.selectedColor = highlighted;
            colors.pressedColor = pressed;
            colors.disabledColor = Color.Lerp(normal, Color.black, 0.42f);
            button.colors = colors;
            return button;
        }
    }
}
