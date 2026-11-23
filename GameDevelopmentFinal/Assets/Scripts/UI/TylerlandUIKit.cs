using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Tylerland.UI
{
    /// <summary>
    /// Shared atmospheric horror UGUI factory for MainMenu, PauseMenu, SettingsPanel, and BetweenNightsUI.
    /// Ensures crisp responsive Canvas layouts (1920x1080 reference), proper InputSystemUIInputModule EventSystem,
    /// and consistent dark security-terminal styling with zero missing asset dependencies.
    /// </summary>
    public static class TylerlandUIKit
    {
        public static readonly Color BgBackdrop = new Color(0.03f, 0.035f, 0.055f, 0.94f);
        public static readonly Color PanelDark = new Color(0.07f, 0.08f, 0.12f, 0.97f);
        public static readonly Color CardBg = new Color(0.10f, 0.12f, 0.18f, 0.96f);
        public static readonly Color AccentCrimson = new Color(0.76f, 0.16f, 0.18f, 1f);
        public static readonly Color AccentCrimsonHover = new Color(0.92f, 0.24f, 0.26f, 1f);
        public static readonly Color AccentEmerald = new Color(0.14f, 0.56f, 0.34f, 1f);
        public static readonly Color AccentGold = new Color(0.86f, 0.66f, 0.18f, 1f);
        public static readonly Color AccentPurple = new Color(0.48f, 0.24f, 0.72f, 1f);
        public static readonly Color AccentBlue = new Color(0.18f, 0.42f, 0.68f, 1f);
        public static readonly Color BtnNeutral = new Color(0.16f, 0.18f, 0.25f, 1f);
        public static readonly Color TextPrimary = new Color(0.94f, 0.95f, 0.98f, 1f);
        public static readonly Color TextMuted = new Color(0.64f, 0.68f, 0.78f, 1f);
        public static readonly Color BorderSubtle = new Color(0.26f, 0.30f, 0.40f, 0.85f);

        private static Font _cachedFont;
        private static Sprite _whiteSprite;

        public static Font DefaultFont
        {
            get
            {
                if (_cachedFont == null)
                {
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_cachedFont == null)
                    {
                        _cachedFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
                    }
                }
                return _cachedFont;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    Color[] cols = { Color.white, Color.white, Color.white, Color.white };
                    tex.SetPixels(cols);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                }
                return _whiteSprite;
            }
        }

        public static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            else if (es.GetComponent<InputSystemUIInputModule>() == null && es.GetComponent<BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder, Transform parent = null)
        {
            EnsureEventSystem();

            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, bool stretchFull = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;

            var rt = go.GetComponent<RectTransform>();
            if (stretchFull)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return rt;
        }

        public static Outline AddBorder(GameObject go, Color color, float thickness = 2f)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
            return outline;
        }

        public static Text CreateText(
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = DefaultFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.fontStyle = style;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        public static Button CreateButton(
            Transform parent,
            string label,
            Color baseColor,
            UnityAction onClick,
            float height = 56f,
            int fontSize = 22)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = Color.white;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.22f);
            colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.14f);
            colors.disabledColor = new Color(0.18f, 0.18f, 0.22f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            AddBorder(go, new Color(1f, 1f, 1f, 0.16f), 1.5f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var txt = CreateText(go.transform, label, fontSize, TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 4f);
            trt.offsetMax = new Vector2(-12f, -4f);

            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static Slider CreateLabeledSlider(
            Transform parent,
            string labelText,
            float min,
            float max,
            float initialValue,
            bool showPercent,
            UnityAction<float> onValueChanged,
            out Text valueLabel)
        {
            var row = new GameObject("SliderRow_" + labelText, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 46f;
            rowLe.minHeight = 46f;

            // Label
            var lbl = CreateText(row.transform, labelText, 20, TextPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            var lblLe = lbl.gameObject.AddComponent<LayoutElement>();
            lblLe.preferredWidth = 270f;
            lblLe.minWidth = 270f;
            lblLe.preferredHeight = 40f;

            // Slider Root
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderLe = sliderGo.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1f;
            sliderLe.minWidth = 260f;
            sliderLe.preferredHeight = 32f;

            // Background bar
            var bgRt = CreatePanel(sliderGo.transform, "Background", new Color(0.14f, 0.16f, 0.22f, 1f), false);
            bgRt.anchorMin = new Vector2(0f, 0.30f);
            bgRt.anchorMax = new Vector2(1f, 0.70f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill Area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.30f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.70f);
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fillRt = CreatePanel(fillArea.transform, "Fill", AccentCrimson, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Handle Slide Area
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleRt = CreatePanel(handleArea.transform, "Handle", TextPrimary, false);
            handleRt.sizeDelta = new Vector2(22f, 30f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleRt.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(initialValue, min, max);

            // Value readout
            var valTxt = CreateText(
                row.transform,
                showPercent ? $"{Mathf.RoundToInt(slider.value * 100f)}%" : $"{slider.value:F2}",
                19,
                AccentGold,
                TextAnchor.MiddleRight,
                FontStyle.Bold);
            var valLe = valTxt.gameObject.AddComponent<LayoutElement>();
            valLe.preferredWidth = 76f;
            valLe.minWidth = 76f;
            valLe.preferredHeight = 40f;
            valueLabel = valTxt;

            slider.onValueChanged.AddListener(v =>
            {
                valTxt.text = showPercent ? $"{Mathf.RoundToInt(v * 100f)}%" : $"{v:F2}";
                onValueChanged?.Invoke(v);
            });

            return slider;
        }

        public static Toggle CreateLabeledToggle(
            Transform parent,
            string labelText,
            bool initialValue,
            UnityAction<bool> onValueChanged)
        {
            var row = new GameObject("ToggleRow_" + labelText, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 42f;
            rowLe.minHeight = 42f;

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var toggleLe = toggleGo.AddComponent<LayoutElement>();
            toggleLe.preferredWidth = 34f;
            toggleLe.minWidth = 34f;
            toggleLe.preferredHeight = 34f;

            var bgRt = CreatePanel(toggleGo.transform, "Background", new Color(0.14f, 0.16f, 0.22f, 1f), true);
            AddBorder(bgRt.gameObject, BorderSubtle, 1.5f);

            var checkRt = CreatePanel(bgRt, "Checkmark", AccentEmerald, false);
            checkRt.anchorMin = new Vector2(0.18f, 0.18f);
            checkRt.anchorMax = new Vector2(0.82f, 0.82f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bgRt.GetComponent<Image>();
            toggle.graphic = checkRt.GetComponent<Image>();
            toggle.isOn = initialValue;
            toggle.onValueChanged.AddListener(v => onValueChanged?.Invoke(v));

            var lbl = CreateText(row.transform, labelText, 20, TextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal);
            var lblLe = lbl.gameObject.AddComponent<LayoutElement>();
            lblLe.flexibleWidth = 1f;
            lblLe.preferredHeight = 36f;

            return toggle;
        }
    }
}
