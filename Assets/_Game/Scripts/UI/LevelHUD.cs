using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>
    /// In-level UI, created by GameManager: top bar (minigame title, timer / round, best), a minigame-specific info
    /// widget, intro banner during the countdown, prompts and flashes, wind indicator, pause card and results card.
    /// </summary>
    public class LevelHUD : MonoBehaviour
    {
        const float Margin = 24f;

        GameManager gm;
        MinigameInfo info;
        Transform root;

        Text timerText;
        Text bestText;
        Text countdownText;
        Text promptText;
        Text flashText;
        Image fadeImage;
        CanvasGroup intro;
        Coroutine flashRoutine;

        GameObject pausePanel;
        GameObject resultsPanel;
        Text resultsTitle;
        RectTransform statsRoot;
        Text recordBadge;
        readonly List<GameObject> statRows = new List<GameObject>();

        RectTransform windRoot;
        RectTransform windArrow;
        readonly List<Image> windImages = new List<Image>();
        Vector3 windDirection;
        bool windWarning;

        void Awake()
        {
            gm = GetComponent<GameManager>();
            info = MinigameInfo.For(gm.LevelId);
            var canvas = UIFactory.CreateCanvas("HUDCanvas", 10);
            root = canvas.transform;

            BuildTopBar();
            BuildHint();
            BuildWindIndicator();

            promptText = UIFactory.CreateDisplayText(root, "Prompt", "", 72, Color.white);
            promptText.supportRichText = true;
            promptText.rectTransform.anchorMin = new Vector2(0, 0.66f);
            promptText.rectTransform.anchorMax = new Vector2(1, 0.78f);

            countdownText = UIFactory.CreateDisplayText(root, "Countdown", "", 200, UIFactory.Gold);
            ThickOutline(countdownText);

            flashText = UIFactory.CreateDisplayText(root, "Flash", "", 96, Color.white);
            flashText.rectTransform.anchorMin = new Vector2(0, 0.52f);
            flashText.rectTransform.anchorMax = new Vector2(1, 0.8f);
            ThickOutline(flashText);

            BuildIntro();

            fadeImage = UIFactory.CreateImage(root, "Fade", new Color(0, 0, 0, 0));
            fadeImage.raycastTarget = false;

            BuildPausePanel();
            BuildResultsPanel();
        }

        void Start()
        {
            HudWidget.Create(root, gm, new Vector2(Margin, -Margin - 76f - 16f));
            bestText.text = BestString();
            // Keep transient texts above the widget cards.
            promptText.transform.SetAsLastSibling();
            countdownText.transform.SetAsLastSibling();
            flashText.transform.SetAsLastSibling();
            intro.transform.SetAsLastSibling();
            fadeImage.transform.SetAsLastSibling();
            pausePanel.transform.SetAsLastSibling();
            resultsPanel.transform.SetAsLastSibling();
        }

        string BestString()
        {
            if (gm.IsSurvival)
            {
                float bestScore = SaveData.GetBestScore(gm.LevelId);
                return bestScore > 0f ? gm.FormatScore(bestScore) : "--";
            }
            float best = SaveData.GetBestTime(gm.LevelId);
            return best > 0f ? SaveData.FormatTime(best) : "--";
        }

        // ───────────────────────────── Building ─────────────────────────────

        void BuildTopBar()
        {
            // Left: minigame title.
            var titleCard = UIFactory.CreateCard(root, "TitleCard", new Vector2(0f, 1f), new Vector2(Margin, -Margin),
                new Vector2(HudWidget.Width, 76f)).transform;
            UIFactory.CreateDisplayText(titleCard, "Title", info.Title, 44, UIFactory.Gold);

            // Centre: timer or round counter.
            var timerCard = UIFactory.CreateCard(root, "TimerCard", new Vector2(0.5f, 1f), new Vector2(0f, -Margin + 4f),
                new Vector2(400f, 104f)).transform;
            timerText = UIFactory.CreateDisplayText(timerCard, "Timer", "00:00.00", 72, Color.white);

            // Right: best result.
            var bestCard = UIFactory.CreateCard(root, "BestCard", new Vector2(1f, 1f), new Vector2(-Margin, -Margin),
                new Vector2(360f, 76f)).transform;
            var label = UIFactory.CreateDisplayText(bestCard, "Label", "BEST", 28, UIFactory.Cream, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            bestText = UIFactory.CreateDisplayText(bestCard, "Value", "--", 38, UIFactory.Gold, TextAnchor.MiddleRight);
            bestText.rectTransform.offsetMax = new Vector2(-24f, 0f);
        }

        void BuildHint()
        {
            var card = UIFactory.CreateCard(root, "HintCard", new Vector2(0f, 0f), new Vector2(Margin, Margin),
                new Vector2(100f, 48f), new Color(0.17f, 0.08f, 0.04f, 0.6f));
            var hint = UIFactory.CreateText(card.transform, "Hint", info.Hint, 24, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter, false);
            card.rectTransform.sizeDelta = new Vector2(hint.preferredWidth + 48f, 48f);
        }

        void BuildIntro()
        {
            var card = UIFactory.CreateCard(root, "Intro", new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(980f, 150f));
            intro = card.gameObject.AddComponent<CanvasGroup>();
            intro.blocksRaycasts = false;
            var title = UIFactory.CreateDisplayText(card.transform, "Title", info.Title, 64, UIFactory.Gold);
            title.rectTransform.offsetMin = new Vector2(0f, 60f);
            var goal = UIFactory.CreateDisplayText(card.transform, "Goal", info.Goal, 36, UIFactory.Cream);
            goal.rectTransform.offsetMax = new Vector2(0f, -78f);
            if (string.IsNullOrEmpty(info.Goal)) card.gameObject.SetActive(false);
        }

        void BuildPausePanel()
        {
            pausePanel = UIFactory.CreateImage(root, "PausePanel", new Color(0.05f, 0.02f, 0.01f, 0.55f)).gameObject;
            pausePanel.GetComponent<Image>().raycastTarget = true;
            var card = UIFactory.CreateCard(pausePanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(660f, 640f)).transform;
            var title = UIFactory.CreateDisplayText(card, "Title", "PAUSED", 100, UIFactory.Gold);
            title.rectTransform.offsetMin = new Vector2(0f, 460f);
            ThickOutline(title);
            UIFactory.StyleButton(UIFactory.CreateButton(card, "RESUME", new Vector2(0, 110), () => gm.Resume(), new Vector2(460, 90)));
            UIFactory.StyleButton(UIFactory.CreateButton(card, "RESTART", new Vector2(0, -10), () => gm.Restart(), new Vector2(460, 90)));
            UIFactory.StyleButton(UIFactory.CreateButton(card, "MENU", new Vector2(0, -130), () => gm.GoToMenu(), new Vector2(460, 90)));
            var hint = UIFactory.CreateText(card, "Hint", info.Hint.Replace(" · ", "\n"), 24, UIFactory.Cream, TextAnchor.MiddleCenter, false);
            hint.rectTransform.offsetMax = new Vector2(0f, -490f);
            pausePanel.SetActive(false);
        }

        void BuildResultsPanel()
        {
            resultsPanel = UIFactory.CreateImage(root, "ResultsPanel", new Color(0.05f, 0.02f, 0.01f, 0.6f)).gameObject;
            resultsPanel.GetComponent<Image>().raycastTarget = true;
            var card = UIFactory.CreateCard(resultsPanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(900f, 780f)).transform;

            resultsTitle = UIFactory.CreateDisplayText(card, "Title", "FINISH!", 120, UIFactory.Gold);
            resultsTitle.rectTransform.offsetMin = new Vector2(0f, 600f);
            ThickOutline(resultsTitle);

            var subtitle = UIFactory.CreateDisplayText(card, "Minigame", info.Title, 32, UIFactory.Cream);
            subtitle.rectTransform.offsetMin = new Vector2(0f, 560f);
            subtitle.rectTransform.offsetMax = new Vector2(0f, -170f);

            statsRoot = UIFactory.CreateRect(card, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(680f, 0f));

            recordBadge = UIFactory.CreateDisplayText(card, "Record", "NEW RECORD!", 60, UIFactory.Gold);
            recordBadge.rectTransform.anchorMin = recordBadge.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            recordBadge.rectTransform.sizeDelta = new Vector2(700f, 80f);
            recordBadge.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            ThickOutline(recordBadge);

            UIFactory.StyleButton(UIFactory.CreateButton(card, "PLAY AGAIN", new Vector2(-200, -300), () => gm.Restart(), new Vector2(360, 90)), 46);
            UIFactory.StyleButton(UIFactory.CreateButton(card, "MENU", new Vector2(200, -300), () => gm.GoToMenu(), new Vector2(360, 90)), 46);
            resultsPanel.SetActive(false);
        }

        /// <summary>Wind arrow: a shaft and a chevron head, rotated to show the gust direction relative to the camera.</summary>
        void BuildWindIndicator()
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
            var label = UIFactory.CreateDisplayText(windRoot, "Label", "WIND", 44, Color.white);
            label.rectTransform.offsetMin = new Vector2(0, -130);
            label.rectTransform.offsetMax = new Vector2(0, -130);
            windRoot.gameObject.SetActive(false);
        }

        static void ThickOutline(Text text)
        {
            var o = text.GetComponent<Outline>();
            if (o == null) o = text.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.2f, 0.08f, 0.02f, 1f);
            o.effectDistance = new Vector2(5, -5);
        }

        // ───────────────────────────── Runtime ─────────────────────────────

        void Update()
        {
            if (gm.IsSurvival && gm.ScoresRounds)
                timerText.text = $"ROUND {gm.Rounds + 1}";
            else
                timerText.text = SaveData.FormatTime(Mathf.Max(0.0001f, gm.IsSurvival ? gm.Score : gm.ElapsedTime));
            UpdateWindIndicator();

            if (recordBadge.gameObject.activeInHierarchy)
                recordBadge.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
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
            // The intro banner stays up for 3-2-1 and leaves at "START!".
            if (intro.gameObject.activeSelf && text != null && text.Length > 0 && !char.IsDigit(text[0]))
                StartCoroutine(HideIntro());
        }

        IEnumerator HideIntro()
        {
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                intro.alpha = 1f - t / 0.4f;
                yield return null;
            }
            intro.gameObject.SetActive(false);
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

        // ───────────────────────────── Results ─────────────────────────────

        public void ShowGameOver(float time, float bonus, int bonusCount, float score, float best, bool record)
        {
            HideWind();
            ShowPrompt(null);
            ShowCountdown(null);
            if (gm.ScoresRounds)
            {
                OpenResults("GAME OVER!", record,
                    ("Rounds survived", $"{score:0}"),
                    ("Best", gm.FormatScore(best)));
                return;
            }
            OpenResults("GAME OVER!", record,
                ("Survived", SaveData.FormatTime(time)),
                ("Golden acorns", $"{bonusCount}  (+{bonus:0} s)"),
                ("Score", SaveData.FormatTime(score)),
                ("Best", SaveData.FormatTime(best)));
        }

        public void ShowWin(float time, float best, bool record, int falls)
        {
            OpenResults("FINISH!", record,
                ("Your time", SaveData.FormatTime(time)),
                ("Falls", falls.ToString()),
                ("Best", SaveData.FormatTime(best)));
        }

        void OpenResults(string title, bool record, params (string label, string value)[] rows)
        {
            resultsTitle.text = title;
            foreach (var row in statRows) Destroy(row);
            statRows.Clear();
            const float rowHeight = 66f;
            float top = (rows.Length - 1) * rowHeight * 0.5f;
            for (int i = 0; i < rows.Length; i++)
                statRows.Add(AddStatRow(rows[i].label, rows[i].value, top - i * rowHeight, i % 2 == 0));
            recordBadge.gameObject.SetActive(record);
            bestText.text = BestString();
            resultsPanel.SetActive(true);
            Select(resultsPanel);
        }

        GameObject AddStatRow(string label, string value, float y, bool shaded)
        {
            var row = UIFactory.CreateRect(statsRoot, "Row_" + label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, y), new Vector2(680f, 60f));
            if (shaded)
            {
                var bg = row.gameObject.AddComponent<Image>();
                bg.sprite = UIFactory.RoundedSprite;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(1f, 1f, 1f, 0.06f);
                bg.raycastTarget = false;
            }
            var l = UIFactory.CreateDisplayText(row, "Label", label, 40, UIFactory.Cream, TextAnchor.MiddleLeft);
            l.rectTransform.offsetMin = new Vector2(24f, 0f);
            var v = UIFactory.CreateDisplayText(row, "Value", value, 44, Color.white, TextAnchor.MiddleRight);
            v.rectTransform.offsetMax = new Vector2(-24f, 0f);
            return row.gameObject;
        }

        static void Select(GameObject panel)
        {
            var b = panel.GetComponentInChildren<Button>();
            if (EventSystem.current != null && b != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }
}
