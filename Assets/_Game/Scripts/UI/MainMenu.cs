using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutumnGuys
{
    public class MainMenu : MonoBehaviour
    {
        public string levelScene = SceneNames.AutumnRush;
        public string leafPileScene = SceneNames.LeafPile;
        public string memoryScene = SceneNames.HarvestMemory;

        static readonly Vector2 ButtonSize = new Vector2(520, 90);
        // Button column: x offset from the screen centre, y of the first button and spacing (1920x1080 reference).
        const float MenuX = -560f;
        const float MenuTop = -40f;
        const float MenuStep = 120f;
        static readonly Color ButtonColor = new Color(0.66f, 0.17f, 0.08f); // deep autumn red
        static readonly Color TextColor = new Color(1f, 0.83f, 0.3f);       // golden yellow

        GameObject mainPanel;
        GameObject minigamePanel;
        GameObject controlsPanel;

        void Awake()
        {
            Time.timeScale = 1f;
            GameManager.SetCursorLocked(false);

            var canvas = UIFactory.CreateCanvas("MenuCanvas");

            // Title
            var titleRt = UIFactory.CreateRect(canvas.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -170), new Vector2(1300, 234));
            var title = titleRt.gameObject.AddComponent<Image>();
            title.sprite = UIFactory.LoadSprite("GameTitle");
            title.preserveAspect = true;

            // Main panel
            mainPanel = UIFactory.Stretch(canvas.transform, "MainPanel").gameObject;
            MenuButton(mainPanel.transform, "PLAY", MenuTop, () => ShowPanel(minigamePanel));
            MenuButton(mainPanel.transform, "CONTROLS", MenuTop - MenuStep, () => ShowPanel(controlsPanel));
            MenuButton(mainPanel.transform, "EXIT GAME", MenuTop - MenuStep * 2, Quit);

            // Minigame selection – same layout as the main panel
            minigamePanel = UIFactory.Stretch(canvas.transform, "MinigamePanel").gameObject;
            AddMinigame(minigamePanel.transform, "AUTUMN RUSH", levelScene, MenuTop,
                "Best time: " + SaveData.FormatTime(SaveData.GetBestTime(levelScene)));
            AddMinigame(minigamePanel.transform, "LEAF PILE", leafPileScene, MenuTop - MenuStep,
                "Best score: " + SaveData.FormatTime(SaveData.GetBestScore(leafPileScene)));
            int bestRounds = Mathf.RoundToInt(SaveData.GetBestScore(memoryScene));
            AddMinigame(minigamePanel.transform, "HARVEST MEMORY", memoryScene, MenuTop - MenuStep * 2,
                "Best: " + (bestRounds > 0 ? $"{bestRounds} {(bestRounds == 1 ? "round" : "rounds")}" : "--"));
            MenuButton(minigamePanel.transform, "BACK", MenuTop - MenuStep * 3, () => ShowPanel(mainPanel));

            // Controls panel
            controlsPanel = UIFactory.Stretch(canvas.transform, "ControlsPanel").gameObject;
            var bg = UIFactory.CreateRect(controlsPanel.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -80), new Vector2(1300, 780));
            bg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.85f);
            var txt = UIFactory.CreateText(bg, "Text",
                "CONTROLS\n\n" +
                "WASD / arrows – move\n" +
                "Space – jump\n" +
                "Mouse – camera\n" +
                "R – back to checkpoint (Autumn Rush)\n" +
                "Esc – pause\n\n" +
                "Autumn Rush: reach the finish as fast as you can!\n" +
                "Leaf Pile: the leaf piles fall away under your feet –\n" +
                "survive as long as you can and grab golden acorns (+5 s)!\n" +
                "Harvest Memory: remember the icons, then stand on the right one!", 34, UIFactory.Cream);
            txt.rectTransform.offsetMin = new Vector2(0, 110);
            Style(UIFactory.CreateButton(bg, "BACK", new Vector2(0, -310), () => ShowPanel(mainPanel)));

            ShowPanel(mainPanel);
        }

        void AddMinigame(Transform parent, string label, string scene, float y, string bestText)
        {
            MenuButton(parent, label, y, () => SceneManager.LoadScene(scene));
            var best = UIFactory.CreateText(parent, label + "_Best", bestText, 38, UIFactory.Cream, TextAnchor.MiddleLeft);
            best.font = UIFactory.MenuFont;
            best.fontStyle = FontStyle.Normal;
            best.rectTransform.anchorMin = best.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            best.rectTransform.sizeDelta = new Vector2(600, 90);
            best.rectTransform.anchoredPosition = new Vector2(MenuX + ButtonSize.x * 0.5f + 40 + 300, y);
        }

        /// <summary>Menu button in the menu style: autumn red with golden text in the display font.</summary>
        Button MenuButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
        {
            return Style(UIFactory.CreateButton(parent, label, new Vector2(MenuX, y), onClick, ButtonSize));
        }

        static Button Style(Button btn)
        {
            btn.GetComponent<Image>().color = ButtonColor;
            // Tints multiply the image colour, so dim it a bit normally and go full brightness when selected.
            var colors = btn.colors;
            colors.normalColor = new Color(0.82f, 0.82f, 0.82f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            btn.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            text.font = UIFactory.MenuFont;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 52;
            text.color = TextColor;
            return btn;
        }

        void Update()
        {
            // Esc / gamepad B goes back to the main panel.
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back && !mainPanel.activeSelf) ShowPanel(mainPanel);
        }

        void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            minigamePanel.SetActive(panel == minigamePanel);
            controlsPanel.SetActive(panel == controlsPanel);
            var sel = panel.GetComponentInChildren<Button>();
            if (EventSystem.current != null && sel != null) EventSystem.current.SetSelectedGameObject(sel.gameObject);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
