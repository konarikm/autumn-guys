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

        public static Sprite LoadSprite(string resourceName)
        {
            var sprite = Resources.Load<Sprite>(resourceName);
            if (sprite != null) return sprite;
            var tex = Resources.Load<Texture2D>(resourceName);
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }
    }
}
