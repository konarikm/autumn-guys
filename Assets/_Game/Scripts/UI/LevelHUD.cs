using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>In-level UI: timer, countdown, messages, pause menu and win screen. Created by GameManager.</summary>
    public class LevelHUD : MonoBehaviour
    {
        GameManager gm;
        Text timerText;
        Text bestText;
        Text countdownText;
        Text promptText;
        Text flashText;
        Image fadeImage;
        GameObject pausePanel;
        GameObject winPanel;
        Text winText;
        Text winTitle;
        Coroutine flashRoutine;

        RectTransform windRoot;
        RectTransform windArrow;
        readonly System.Collections.Generic.List<Image> windImages = new System.Collections.Generic.List<Image>();
        Vector3 windDirection;
        bool windWarning;

        void Awake()
        {
            gm = GetComponent<GameManager>();
            var canvas = UIFactory.CreateCanvas("HUDCanvas", 10);
            var root = canvas.transform;

            timerText = UIFactory.CreateText(root, "Timer", "00:00.00", 64, Color.white, TextAnchor.UpperCenter);
            timerText.rectTransform.offsetMax = new Vector2(0, -24);

            bestText = UIFactory.CreateText(root, "Best", "", 32, UIFactory.Cream, TextAnchor.UpperCenter);
            bestText.rectTransform.offsetMax = new Vector2(0, -100);

            string hintText = !gm.IsSurvival ? "WASD move · Space jump · R checkpoint · Esc pause"
                : gm.ScoresRounds ? "WASD move · Space jump · Esc pause · Remember the icons!"
                : "WASD move · Space jump · Esc pause · Keep moving!";
            var hint = UIFactory.CreateText(root, "Hint", hintText, 26, new Color(1f, 1f, 1f, 0.8f), TextAnchor.LowerLeft);
            hint.rectTransform.offsetMin = new Vector2(24, 20);

            BuildWindIndicator(root);

            promptText = UIFactory.CreateText(root, "Prompt", "", 72, Color.white);
            promptText.supportRichText = true;
            promptText.rectTransform.anchorMin = new Vector2(0, 0.7f);
            promptText.rectTransform.anchorMax = new Vector2(1, 0.82f);

            countdownText = UIFactory.CreateText(root, "Countdown", "", 180, UIFactory.Orange);
            flashText = UIFactory.CreateText(root, "Flash", "", 90, Color.white);
            flashText.rectTransform.anchorMin = new Vector2(0, 0.55f);
            flashText.rectTransform.anchorMax = new Vector2(1, 0.85f);

            fadeImage = UIFactory.CreateImage(root, "Fade", new Color(0, 0, 0, 0));
            fadeImage.raycastTarget = false;

            BuildPausePanel(root);
            BuildWinPanel(root);
        }

        void BuildPausePanel(Transform root)
        {
            pausePanel = UIFactory.CreateImage(root, "PausePanel", new Color(0.1f, 0.05f, 0.02f, 0.7f)).gameObject;
            var title = UIFactory.CreateText(pausePanel.transform, "Title", "PAUSED", 110, UIFactory.Orange);
            title.rectTransform.anchorMin = new Vector2(0, 0.65f);
            UIFactory.CreateButton(pausePanel.transform, "RESUME", new Vector2(0, 60), () => gm.Resume());
            UIFactory.CreateButton(pausePanel.transform, "RESTART", new Vector2(0, -60), () => gm.Restart());
            UIFactory.CreateButton(pausePanel.transform, "MENU", new Vector2(0, -180), () => gm.GoToMenu());
            pausePanel.SetActive(false);
        }

        void BuildWinPanel(Transform root)
        {
            winPanel = UIFactory.CreateImage(root, "WinPanel", new Color(0.1f, 0.05f, 0.02f, 0.75f)).gameObject;
            winTitle = UIFactory.CreateText(winPanel.transform, "Title", "FINISH!", 150, UIFactory.Orange);
            winTitle.rectTransform.anchorMin = new Vector2(0, 0.68f);
            winText = UIFactory.CreateText(winPanel.transform, "Stats", "", 52, Color.white);
            winText.rectTransform.anchorMin = new Vector2(0, 0.42f);
            winText.rectTransform.anchorMax = new Vector2(1, 0.7f);
            UIFactory.CreateButton(winPanel.transform, "PLAY AGAIN", new Vector2(-240, -200), () => gm.Restart());
            UIFactory.CreateButton(winPanel.transform, "MENU", new Vector2(240, -200), () => gm.GoToMenu());
            winPanel.SetActive(false);
        }

        /// <summary>Wind arrow: a shaft and a chevron head, rotated to show the gust direction relative to the camera.</summary>
        void BuildWindIndicator(Transform root)
        {
            windRoot = UIFactory.CreateRect(root, "WindIndicator", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 250), new Vector2(160, 160));
            windArrow = UIFactory.CreateRect(windRoot, "Arrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(160, 160));
            var shaft = UIFactory.CreateRect(windArrow, "Shaft", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -15), new Vector2(26, 110));
            windImages.Add(shaft.gameObject.AddComponent<Image>());
            foreach (float side in new[] { -1f, 1f })
            {
                var head = UIFactory.CreateRect(windArrow, "Head", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(side * 22f, 38f), new Vector2(26, 80));
                head.localRotation = Quaternion.Euler(0, 0, side * 40f);
                windImages.Add(head.gameObject.AddComponent<Image>());
            }
            foreach (var img in windImages)
            {
                img.raycastTarget = false;
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.2f, 0.08f, 0.02f, 0.9f);
                o.effectDistance = new Vector2(3, -3);
            }
            var label = UIFactory.CreateText(windRoot, "Label", "WIND", 40, Color.white);
            label.rectTransform.offsetMin = new Vector2(0, -130);
            label.rectTransform.offsetMax = new Vector2(0, -130);
            windRoot.gameObject.SetActive(false);
        }

        void Start()
        {
            if (gm.IsSurvival)
            {
                float bestScore = SaveData.GetBestScore(gm.LevelId);
                bestText.text = bestScore > 0f ? "Best: " + gm.FormatScore(bestScore) : "";
                return;
            }
            float best = SaveData.GetBestTime(gm.LevelId);
            bestText.text = best > 0f ? "Best: " + SaveData.FormatTime(best) : "";
        }

        void Update()
        {
            if (gm.IsSurvival && gm.ScoresRounds)
                timerText.text = $"ROUND {gm.Rounds + 1}";
            else
                timerText.text = SaveData.FormatTime(Mathf.Max(0.0001f, gm.IsSurvival ? gm.Score : gm.ElapsedTime));
            UpdateWindIndicator();
        }

        /// <summary>Persistent instruction line (rich text allowed). Null or empty hides it.</summary>
        public void ShowPrompt(string text)
        {
            bool changed = promptText.text != (text ?? "");
            promptText.text = text ?? "";
            if (changed && !string.IsNullOrEmpty(text)) StartCoroutine(Punch(promptText.rectTransform));
        }

        /// <summary>Shows a wind arrow pointing in the given world direction. Warning = blinking, otherwise solid.</summary>
        public void ShowWind(Vector3 worldDirection, bool warning)
        {
            windDirection = worldDirection;
            windWarning = warning;
            windRoot.gameObject.SetActive(true);
        }

        public void HideWind() => windRoot.gameObject.SetActive(false);

        void UpdateWindIndicator()
        {
            if (!windRoot.gameObject.activeSelf) return;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                float angle = Mathf.Atan2(Vector3.Dot(windDirection, right), Vector3.Dot(windDirection, fwd)) * Mathf.Rad2Deg;
                windArrow.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            bool on = !windWarning || Mathf.Repeat(Time.time * 4f, 1f) < 0.6f;
            Color c = windWarning ? UIFactory.Orange : Color.white;
            foreach (var img in windImages) img.color = on ? c : new Color(c.r, c.g, c.b, 0.25f);
        }

        public void ShowCountdown(string text)
        {
            countdownText.text = text ?? "";
            if (text != null) StartCoroutine(Punch(countdownText.rectTransform));
        }

        public void Flash(string text)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(text));
        }

        IEnumerator FlashRoutine(string text)
        {
            flashText.text = text;
            flashText.color = Color.white;
            yield return Punch(flashText.rectTransform);
            yield return new WaitForSeconds(0.7f);
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                flashText.color = new Color(1, 1, 1, 1 - t / 0.4f);
                yield return null;
            }
            flashText.text = "";
        }

        static IEnumerator Punch(RectTransform rt)
        {
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float s = 1f + 0.5f * (1f - t / 0.25f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        public IEnumerator FadeRoutine(float from, float to, float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, to);
        }

        public void ShowPause(bool show)
        {
            pausePanel.SetActive(show);
            if (show) Select(pausePanel);
        }

        public void ShowGameOver(float time, float bonus, int bonusCount, float score, float best, bool record)
        {
            HideWind();
            ShowPrompt(null);
            ShowCountdown(null);
            winTitle.text = "GAME OVER!";
            winPanel.SetActive(true);
            winText.supportRichText = true;
            if (gm.ScoresRounds)
            {
                winText.text = $"Rounds survived: {score:0}\n" +
                               (record ? "<color=#FFD24A>NEW RECORD!</color>" : $"Best: {gm.FormatScore(best)}");
                Select(winPanel);
                return;
            }
            winText.text = $"Survived: {SaveData.FormatTime(time)}\n" +
                           $"Golden acorns: {bonusCount} (+{bonus:0} s)\n" +
                           $"Score: {SaveData.FormatTime(score)}\n" +
                           (record ? "<color=#FFD24A>NEW RECORD!</color>" : $"Best: {SaveData.FormatTime(best)}");
            Select(winPanel);
        }

        public void ShowWin(float time, float best, bool record, int falls)
        {
            winPanel.SetActive(true);
            winText.text = $"Your time: {SaveData.FormatTime(time)}\n" +
                           (record ? "<color=#FFD24A>NEW RECORD!</color>" : $"Best: {SaveData.FormatTime(best)}") +
                           $"\nFalls: {falls}";
            winText.supportRichText = true;
            Select(winPanel);
        }

        static void Select(GameObject panel)
        {
            var b = panel.GetComponentInChildren<Button>();
            if (EventSystem.current != null && b != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }
}
