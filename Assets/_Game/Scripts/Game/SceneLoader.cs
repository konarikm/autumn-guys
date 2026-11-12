using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>
    /// Persistent scene switcher: fades to black, loads the scene, fades back in.
    /// Uses unscaled time so it also works from the pause menu.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        const float FadeOutTime = 0.25f;
        const float FadeInTime = 0.35f;

        static SceneLoader instance;

        Image overlay;
        bool loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
        }

        void Awake()
        {
            // Built directly (not via UIFactory.CreateCanvas) so no EventSystem is created before the first scene loads;
            // each scene's own canvas provides one, and the GraphicRaycaster here uses it to block clicks during fades.
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            overlay = UIFactory.CreateImage(canvas.transform, "Fade", new Color(0f, 0f, 0f, 0f));
            overlay.raycastTarget = false;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Loads a scene with a fade. Ignored while another load is in progress.</summary>
        public static void Load(string scene)
        {
            if (instance == null)
            {
                SceneManager.LoadScene(scene);
                return;
            }
            if (instance.loading) return;
            instance.StartCoroutine(instance.LoadRoutine(scene));
        }

        IEnumerator LoadRoutine(string scene)
        {
            loading = true;
            overlay.raycastTarget = true; // block clicks during the transition
            yield return Fade(overlay.color.a, 1f, FadeOutTime);

            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) yield return null;

            yield return Fade(1f, 0f, FadeInTime);
            overlay.raycastTarget = false;
            loading = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            overlay.color = new Color(0f, 0f, 0f, to);
        }
    }
}
