using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>Team logo splash screen shown when the game starts; any key skips it.</summary>
    public class SplashScreen : MonoBehaviour
    {
        public float fadeTime = 0.8f;
        public float holdTime = 2f;

        CanvasGroup group;

        void Awake()
        {
            var canvas = UIFactory.CreateCanvas("SplashCanvas");
            UIFactory.CreateImage(canvas.transform, "Background", new Color(0.09f, 0.07f, 0.06f));

            var content = UIFactory.Stretch(canvas.transform, "Content");
            group = content.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var logoRt = UIFactory.CreateRect(content, "Logo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(560, 560));
            var img = logoRt.gameObject.AddComponent<Image>();
            img.sprite = UIFactory.LoadSprite("TeamLogo");
            img.preserveAspect = true;

            var caption = UIFactory.CreateText(content, "Caption", "Gin & Tonic presents", 44, UIFactory.Cream);
            caption.rectTransform.anchorMin = new Vector2(0, 0);
            caption.rectTransform.anchorMax = new Vector2(1, 0.2f);
        }

        IEnumerator Start()
        {
            yield return Fade(0f, 1f);
            float t = 0f;
            while (t < holdTime && !SkipPressed()) { t += Time.deltaTime; yield return null; }
            yield return Fade(1f, 0f);
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }

        static bool SkipPressed()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                   (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }
    }
}
