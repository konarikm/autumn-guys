using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutumnGuys
{
    public class MainMenu : MonoBehaviour
    {
        public string levelScene = SceneNames.Level01;

        GameObject mainPanel;
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
            var play = UIFactory.CreateButton(mainPanel.transform, "HRÁT", new Vector2(-560, 40), Play);
            UIFactory.CreateButton(mainPanel.transform, "OVLÁDÁNÍ", new Vector2(-560, -80), () => ShowControls(true));
            UIFactory.CreateButton(mainPanel.transform, "KONEC", new Vector2(-560, -200), Quit);

            float best = SaveData.GetBestTime(levelScene);
            var bestText = UIFactory.CreateText(mainPanel.transform, "BestTime",
                "Nejlepší čas: " + SaveData.FormatTime(best), 40, UIFactory.Cream);
            bestText.rectTransform.anchorMin = new Vector2(0, 0);
            bestText.rectTransform.anchorMax = new Vector2(0.42f, 0.25f);

            // Controls panel
            controlsPanel = UIFactory.Stretch(canvas.transform, "ControlsPanel").gameObject;
            var bg = UIFactory.CreateRect(controlsPanel.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(1000, 620));
            bg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.05f, 0.85f);
            var txt = UIFactory.CreateText(bg, "Text",
                "OVLÁDÁNÍ\n\n" +
                "WASD / šipky – pohyb\n" +
                "Mezerník – skok\n" +
                "Myš – kamera\n" +
                "R – návrat na checkpoint\n" +
                "Esc – pauza\n\n" +
                "Doběhni do cíle co nejrychleji!", 40, UIFactory.Cream);
            txt.rectTransform.offsetMin = new Vector2(0, 110);
            UIFactory.CreateButton(bg, "ZPĚT", new Vector2(0, -230), () => ShowControls(false));
            ShowControls(false);

            EventSystem.current?.SetSelectedGameObject(play.gameObject);
        }

        void ShowControls(bool show)
        {
            controlsPanel.SetActive(show);
            mainPanel.SetActive(!show);
            var sel = (show ? controlsPanel : mainPanel).GetComponentInChildren<Button>();
            if (EventSystem.current != null && sel != null) EventSystem.current.SetSelectedGameObject(sel.gameObject);
        }

        void Play() => SceneManager.LoadScene(levelScene);

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
