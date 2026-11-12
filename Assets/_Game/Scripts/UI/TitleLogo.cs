using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>
    /// Game logo built from separate letters (display font, gold→orange gradient, thick outline, drop shadow),
    /// each slightly tilted like a party-game logo. Letters pop in one after another, then bob in a gentle wave.
    /// </summary>
    public class TitleLogo : MonoBehaviour
    {
        static readonly Color GradientTop = new Color(1f, 0.9f, 0.38f);
        static readonly Color GradientBottom = new Color(0.98f, 0.42f, 0.08f);
        static readonly Color OutlineColor = new Color(0.32f, 0.11f, 0.03f);
        static readonly Color ShadowColor = new Color(0.12f, 0.04f, 0.01f, 0.75f);
        // Tilt (degrees) and baseline offset (px) per letter, repeated – gives the hand-placed, bouncy look.
        static readonly float[] Tilts = { -6f, 4f, -3f, 5f, -4f, 3f, -5f, 4f, -2f, 6f };
        static readonly float[] Lifts = { 0f, 10f, -6f, 8f, -4f, 12f, -8f, 6f, -2f, 10f };

        const float PopDelay = 0.06f;
        const float PopTime = 0.45f;

        class Letter
        {
            public RectTransform rt;
            public Vector2 basePos;
            public float baseTilt;
        }

        readonly List<Letter> letters = new List<Letter>();
        float startTime;

        /// <summary>Creates the logo centred at <paramref name="anchoredPos"/> (anchored to the top centre of the parent).</summary>
        public static TitleLogo Create(Transform parent, string text, Vector2 anchoredPos, int fontSize)
        {
            var rootRt = UIFactory.CreateRect(parent, "TitleLogo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                anchoredPos, new Vector2(10f, fontSize));
            var logo = rootRt.gameObject.AddComponent<TitleLogo>();

            // Measure letters first so the whole word is centred.
            var measured = new List<(char ch, float width)>();
            float total = 0f, spacing = fontSize * 0.02f;
            foreach (char ch in text)
            {
                float w = ch == ' ' ? fontSize * 0.32f : MeasureWidth(ch, fontSize);
                measured.Add((ch, w));
                total += w + spacing;
            }
            total -= spacing;

            float x = -total * 0.5f;
            int index = 0;
            foreach (var (ch, w) in measured)
            {
                if (ch != ' ')
                {
                    var t = UIFactory.CreateDisplayText(rootRt, "Letter_" + ch, ch.ToString(), fontSize, Color.white, TextAnchor.MiddleCenter, false);
                    var rt = t.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(w + fontSize * 0.3f, fontSize * 1.3f);

                    var gradient = t.gameObject.AddComponent<VerticalGradient>();
                    gradient.top = GradientTop;
                    gradient.bottom = GradientBottom;
                    AddOutline(t.gameObject, new Vector2(7f, 7f));
                    AddOutline(t.gameObject, new Vector2(9f, 0.01f));
                    AddOutline(t.gameObject, new Vector2(0.01f, 9f));
                    var shadow = t.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = ShadowColor;
                    shadow.effectDistance = new Vector2(0f, -14f);

                    var letter = new Letter
                    {
                        rt = rt,
                        basePos = new Vector2(x + w * 0.5f, Lifts[index % Lifts.Length]),
                        baseTilt = Tilts[index % Tilts.Length],
                    };
                    rt.anchoredPosition = letter.basePos;
                    rt.localScale = Vector3.zero;
                    logo.letters.Add(letter);
                    index++;
                }
                x += w + spacing;
            }
            return logo;
        }

        static float MeasureWidth(char ch, int fontSize)
        {
            var font = UIFactory.MenuFont;
            font.RequestCharactersInTexture(ch.ToString(), fontSize, FontStyle.Normal);
            return font.GetCharacterInfo(ch, out var info, fontSize, FontStyle.Normal) ? info.advance : fontSize * 0.6f;
        }

        static void AddOutline(GameObject go, Vector2 distance)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = OutlineColor;
            o.effectDistance = distance;
        }

        void OnEnable() => startTime = Time.unscaledTime;

        void Update()
        {
            float t = Time.unscaledTime - startTime;
            for (int i = 0; i < letters.Count; i++)
            {
                var l = letters[i];
                // Pop in with a little overshoot, one letter after another.
                float k = Mathf.Clamp01((t - i * PopDelay) / PopTime);
                const float c1 = 1.70158f, c3 = c1 + 1f; // ease-out-back
                float scale = k <= 0f ? 0f : 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
                l.rt.localScale = Vector3.one * Mathf.Max(0f, scale);

                // Gentle wave once in place.
                float wave = Mathf.Sin(t * 2.2f - i * 0.55f);
                l.rt.anchoredPosition = l.basePos + new Vector2(0f, wave * 7f);
                l.rt.localRotation = Quaternion.Euler(0f, 0f, l.baseTilt + wave * 1.5f);
            }
        }
    }
}
