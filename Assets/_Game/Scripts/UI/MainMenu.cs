using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutumnGuys
{
    public class MainMenu : MonoBehaviour
    {
        public string levelScene = SceneNames.Level01;
        public string leafPileScene = SceneNames.LeafPile;
        public string memoryScene = SceneNames.HarvestMemory;

        static readonly Vector2 ButtonSize = new Vector2(520, 90);

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
            UIFactory.CreateButton(mainPanel.transform, "PLAY", new Vector2(-560, 40), () => ShowPanel(minigamePanel), ButtonSize);
            UIFactory.CreateButton(mainPanel.transform, "CONTROLS", new Vector2(-560, -80), () => ShowPanel(controlsPanel), ButtonSize);
            UIFactory.CreateButton(mainPanel.transform, "EXIT GAME", new Vector2(-560, -200), Quit, ButtonSize);

            // Minigame selection – same layout as the main panel
            minigamePanel = UIFactory.Stretch(canvas.transform, "MinigamePanel").gameObject;
            AddMinigame(minigamePanel.transform, "OBSTACLE RACE", levelScene, 40,
                "Best time: " + SaveData.FormatTime(SaveData.GetBestTime(levelScene)));
            AddMinigame(minigamePanel.transform, "LEAF PILE", leafPileScene, -80,
                "Best score: " + SaveData.FormatTime(SaveData.GetBestScore(leafPileScene)));
            int bestRounds = Mathf.RoundToInt(SaveData.GetBestScore(memoryScene));
            AddMinigame(minigamePanel.transform, "HARVEST MEMORY", memoryScene, -200,
                "Best: " + (bestRounds > 0 ? $"{bestRounds} {(bestRounds == 1 ? "round" : "rounds")}" : "--"));
            UIFactory.CreateButton(minigamePanel.transform, "BACK", new Vector2(-560, -320), () => ShowPanel(mainPanel), ButtonSize);

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
                "R – back to checkpoint (race)\n" +
                "Esc – pause\n\n" +
                "Obstacle Race: reach the finish as fast as you can!\n" +
                "Leaf Pile: the leaf piles fall away under your feet –\n" +
                "survive as long as you can and grab golden acorns (+5 s)!\n" +
                "Harvest Memory: remember the icons, then stand on the right one!", 34, UIFactory.Cream);
            txt.rectTransform.offsetMin = new Vector2(0, 110);
            UIFactory.CreateButton(bg, "BACK", new Vector2(0, -310), () => ShowPanel(mainPanel));

            ShowPanel(mainPanel);
        }

        void AddMinigame(Transform parent, string label, string scene, float y, string bestText)
        {
            UIFactory.CreateButton(parent, label, new Vector2(-560, y), () => SceneManager.LoadScene(scene), ButtonSize);
            var best = UIFactory.CreateText(parent, label + "_Best", bestText, 34, UIFactory.Cream, TextAnchor.MiddleLeft);
            best.rectTransform.anchorMin = best.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            best.rectTransform.sizeDelta = new Vector2(600, 90);
            best.rectTransform.anchoredPosition = new Vector2(-560 + ButtonSize.x * 0.5f + 40 + 300, y);
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
