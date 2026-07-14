using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LiveWire
{
    public static class SceneBuildHelpers
    {
        static Sprite cachedWhiteSprite;

        public static Sprite GetWhiteSprite()
        {
            if (cachedWhiteSprite != null) return cachedWhiteSprite;
            Texture2D tex = Texture2D.whiteTexture;
            cachedWhiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            cachedWhiteSprite.name = "LiveWireWhite";
            return cachedWhiteSprite;
        }

        public static Material MakeMat(Color color, float smoothness = 0.1f, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
            }
            return m;
        }

        public static Material MakeUnlit(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            return m;
        }

        public static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Object.Destroy(go.GetComponent<Collider>());
            }
            return go;
        }

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static EventSystem EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null) return existing;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            return go.GetComponent<EventSystem>();
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color, TextAnchor alignment, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            Text t = go.GetComponent<Text>();
            t.text = text;
            t.color = color;
            t.alignment = alignment;
            t.fontSize = size;
            t.font = HudController.GetMonoFont();
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRect = (RectTransform)textGO.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text t = textGO.GetComponent<Text>();
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.9f, 1f, 0.9f);
            t.fontSize = 32;
            t.font = HudController.GetMonoFont();

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.9f, 1f, 0.9f, 1f);
            cb.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f);
            cb.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
            btn.colors = cb;

            return btn;
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            GameObject fundoGO = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            RectTransform fundoRect = (RectTransform)fundoGO.transform;
            fundoRect.SetParent(rect, false);
            fundoRect.anchorMin = new Vector2(0f, 0.3f);
            fundoRect.anchorMax = new Vector2(1f, 0.7f);
            fundoRect.offsetMin = Vector2.zero;
            fundoRect.offsetMax = Vector2.zero;
            Image fundo = fundoGO.GetComponent<Image>();
            fundo.sprite = GetWhiteSprite();
            fundo.color = new Color(0.15f, 0.15f, 0.18f, 1f);

            GameObject areaPreenchGO = new GameObject("AreaPreenchimento", typeof(RectTransform));
            RectTransform areaPreenchRect = (RectTransform)areaPreenchGO.transform;
            areaPreenchRect.SetParent(rect, false);
            areaPreenchRect.anchorMin = new Vector2(0f, 0.3f);
            areaPreenchRect.anchorMax = new Vector2(1f, 0.7f);
            areaPreenchRect.offsetMin = Vector2.zero;
            areaPreenchRect.offsetMax = new Vector2(-10f, 0f);

            GameObject preenchGO = new GameObject("Preenchimento", typeof(RectTransform), typeof(Image));
            RectTransform preenchRect = (RectTransform)preenchGO.transform;
            preenchRect.SetParent(areaPreenchRect, false);
            preenchRect.anchorMin = Vector2.zero;
            preenchRect.anchorMax = Vector2.one;
            preenchRect.offsetMin = Vector2.zero;
            preenchRect.offsetMax = new Vector2(10f, 0f);
            Image preench = preenchGO.GetComponent<Image>();
            preench.sprite = GetWhiteSprite();
            preench.color = new Color(1f, 0.25f, 0.25f, 1f);

            GameObject areaPegadorGO = new GameObject("AreaPegador", typeof(RectTransform));
            RectTransform areaPegadorRect = (RectTransform)areaPegadorGO.transform;
            areaPegadorRect.SetParent(rect, false);
            areaPegadorRect.anchorMin = Vector2.zero;
            areaPegadorRect.anchorMax = Vector2.one;
            areaPegadorRect.offsetMin = new Vector2(10f, 0f);
            areaPegadorRect.offsetMax = new Vector2(-10f, 0f);

            GameObject pegadorGO = new GameObject("Pegador", typeof(RectTransform), typeof(Image));
            RectTransform pegadorRect = (RectTransform)pegadorGO.transform;
            pegadorRect.SetParent(areaPegadorRect, false);
            pegadorRect.anchorMin = new Vector2(0f, 0f);
            pegadorRect.anchorMax = new Vector2(0f, 1f);
            pegadorRect.sizeDelta = new Vector2(20f, 0f);
            Image pegador = pegadorGO.GetComponent<Image>();
            pegador.sprite = GetWhiteSprite();

            Slider slider = go.GetComponent<Slider>();
            slider.fillRect = preenchRect;
            slider.handleRect = pegadorRect;
            slider.targetGraphic = pegador;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            ColorBlock cb = slider.colors;
            cb.normalColor = new Color(0.9f, 1f, 0.9f, 1f);
            cb.highlightedColor = new Color(1f, 0.6f, 0.6f, 1f);
            cb.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
            slider.colors = cb;

            return slider;
        }

        public static Slider CreateVolumeControl(Transform parent, Vector2 anchoredPos, float largura = 380f)
        {
            CreateText(parent, "VolumeLabel", "VOLUME", 22,
                new Color(0.7f, 0.9f, 0.9f), TextAnchor.MiddleCenter,
                anchoredPos + new Vector2(0f, 32f), new Vector2(largura, 30f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Text percentual = CreateText(parent, "VolumePercentual", "", 22,
                new Color(0.7f, 0.9f, 0.9f), TextAnchor.MiddleLeft,
                anchoredPos + new Vector2(largura / 2f + 55f, 0f), new Vector2(90f, 30f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Slider slider = CreateSlider(parent, "VolumeSlider", anchoredPos, new Vector2(largura, 28f));
            slider.SetValueWithoutNotify(AjustesDeAudio.VolumePrincipal);
            percentual.text = Mathf.RoundToInt(slider.value * 100f) + "%";
            slider.onValueChanged.AddListener(v =>
            {
                AjustesDeAudio.VolumePrincipal = v;
                percentual.text = Mathf.RoundToInt(v * 100f) + "%";
            });
            return slider;
        }
    }
}
