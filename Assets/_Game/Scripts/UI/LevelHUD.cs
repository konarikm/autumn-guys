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
        Text flashText;
        Image fadeImage;
        GameObject pausePanel;
        GameObject winPanel;
        Text winText;
        Coroutine flashRoutine;

        void Awake()
        {
            gm = GetComponent<GameManager>();
            var canvas = UIFactory.CreateCanvas("HUDCanvas", 10);
            var root = canvas.transform;

            timerText = UIFactory.CreateText(root, "Timer", "00:00.00", 64, Color.white, TextAnchor.UpperCenter);
            timerText.rectTransform.offsetMax = new Vector2(0, -24);

            bestText = UIFactory.CreateText(root, "Best", "", 32, UIFactory.Cream, TextAnchor.UpperCenter);
            bestText.rectTransform.offsetMax = new Vector2(0, -100);

            var hint = UIFactory.CreateText(root, "Hint", "WASD pohyb · Mezerník skok · R checkpoint · Esc pauza",
                26, new Color(1f, 1f, 1f, 0.8f), TextAnchor.LowerLeft);
            hint.rectTransform.offsetMin = new Vector2(24, 20);

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
            var title = UIFactory.CreateText(pausePanel.transform, "Title", "PAUZA", 110, UIFactory.Orange);
            title.rectTransform.anchorMin = new Vector2(0, 0.65f);
            UIFactory.CreateButton(pausePanel.transform, "POKRAČOVAT", new Vector2(0, 60), () => gm.Resume());
            UIFactory.CreateButton(pausePanel.transform, "RESTART", new Vector2(0, -60), () => gm.Restart());
            UIFactory.CreateButton(pausePanel.transform, "MENU", new Vector2(0, -180), () => gm.GoToMenu());
            pausePanel.SetActive(false);
        }

        void BuildWinPanel(Transform root)
        {
            winPanel = UIFactory.CreateImage(root, "WinPanel", new Color(0.1f, 0.05f, 0.02f, 0.75f)).gameObject;
            var title = UIFactory.CreateText(winPanel.transform, "Title", "CÍL!", 150, UIFactory.Orange);
            title.rectTransform.anchorMin = new Vector2(0, 0.68f);
            winText = UIFactory.CreateText(winPanel.transform, "Stats", "", 52, Color.white);
            winText.rectTransform.anchorMin = new Vector2(0, 0.42f);
            winText.rectTransform.anchorMax = new Vector2(1, 0.7f);
            UIFactory.CreateButton(winPanel.transform, "ZNOVU", new Vector2(-240, -200), () => gm.Restart());
            UIFactory.CreateButton(winPanel.transform, "MENU", new Vector2(240, -200), () => gm.GoToMenu());
            winPanel.SetActive(false);
        }

        void Start()
        {
            float best = SaveData.GetBestTime(gm.LevelId);
            bestText.text = best > 0f ? "Nejlepší: " + SaveData.FormatTime(best) : "";
        }

        void Update()
        {
            timerText.text = SaveData.FormatTime(Mathf.Max(0.0001f, gm.ElapsedTime));
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

        public void ShowWin(float time, float best, bool record, int falls)
        {
            winPanel.SetActive(true);
            winText.text = $"Tvůj čas: {SaveData.FormatTime(time)}\n" +
                           (record ? "<color=#FFD24A>NOVÝ REKORD!</color>" : $"Nejlepší: {SaveData.FormatTime(best)}") +
                           $"\nPády: {falls}";
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
