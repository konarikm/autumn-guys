using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>Small helper for building uGUI from code (keeps scenes simple and diff-friendly).</summary>
    public static class UIFactory
    {
        public static readonly Color Orange = new Color(0.95f, 0.47f, 0.13f);
        public static readonly Color DarkBrown = new Color(0.24f, 0.12f, 0.05f);
        public static readonly Color Cream = new Color(1f, 0.95f, 0.85f);
        /// <summary>Menu style: deep autumn red buttons with golden text.</summary>
        public static readonly Color MenuRed = new Color(0.66f, 0.17f, 0.08f);
        public static readonly Color Gold = new Color(1f, 0.83f, 0.3f);
        /// <summary>Background of HUD cards and panels.</summary>
        public static readonly Color CardColor = new Color(0.17f, 0.08f, 0.04f, 0.82f);

        static Sprite roundedSprite;
        /// <summary>Rounded-rectangle sprite generated at runtime (9-sliced), so cards need no image assets.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite != null) return roundedSprite;
                const int size = 64, radius = 20;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "RoundedRect",
                };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the rounded corner, with 1 px anti-aliasing.
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return roundedSprite;
            }
        }

        static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static Font menuFont;
        /// <summary>Chunky display font for menu buttons (Lilita One, SIL OFL); falls back to the default font.</summary>
        public static Font MenuFont
        {
            get
            {
                if (menuFont == null) menuFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                return menuFont != null ? menuFont : Font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos = default, Vector2 size = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var rt = CreateRect(parent, name, Vector2.zero, Vector2.one);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rt = Stretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.preserveAspect = sprite != null;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Stretch(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.2f, 0.08f, 0.02f, 0.9f);
                o.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, UnityAction onClick,
            Vector2? size = null)
        {
            var rt = CreateRect(parent, "Button_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                anchoredPos, size ?? new Vector2(420, 90));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Orange;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.selectedColor = new Color(1f, 0.85f, 0.6f);
            colors.pressedColor = new Color(0.8f, 0.6f, 0.4f);
            btn.colors = colors;
            btn.onClick.AddListener(() => AudioManager.Play(Sfx.Click, 0.8f, 0.03f));
            btn.onClick.AddListener(onClick);

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.3f, 0.12f, 0.02f, 0.8f);
            shadow.effectDistance = new Vector2(0, -8);

            CreateText(rt, "Label", label, 44, Color.white);
            return btn;
        }

        /// <summary>Text in the chunky display font (Lilita One).</summary>
        public static Text CreateDisplayText(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var t = CreateText(parent, name, text, size, color, align, outline);
            t.font = MenuFont;
            t.fontStyle = FontStyle.Normal;
            return t;
        }

        /// <summary>Rounded card (sliced sprite) with a soft drop shadow.</summary>
        public static Image CreateCard(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            Color? color = null)
        {
            var rt = CreateRect(parent, name, anchor, anchor, anchoredPos, size);
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color ?? CardColor;
            img.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0, -6);
            return img;
        }

        /// <summary>Horizontal progress bar inside a parent rect. Returns the fill image (set fillAmount 0..1).</summary>
        public static Image CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
        {
            var bg = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, size);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = false;

            var fillRt = Stretch(bg, "Fill");
            fillRt.offsetMin = new Vector2(4, 4);
            fillRt.offsetMax = new Vector2(-4, -4);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = RoundedSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = fillColor;
            fill.raycastTarget = false;
            return fill;
        }

        /// <summary>Applies the menu button look: autumn red, golden display-font label, brighter when selected.</summary>
        public static Button StyleButton(Button btn, int fontSize = 52)
        {
            var img = btn.GetComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = MenuRed;
            // Tints multiply the image colour, so dim it a bit normally and go full brightness when selected.
            var colors = btn.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            btn.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            text.font = MenuFont;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = fontSize;
            text.color = Gold;
            return btn;
        }

        public static Sprite LoadSprite(string resourceName)
        {
            var sprite = Resources.Load<Sprite>(resourceName);
            if (sprite != null) return sprite;
            var tex = Resources.Load<Texture2D>(resourceName);
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }
    }
}
