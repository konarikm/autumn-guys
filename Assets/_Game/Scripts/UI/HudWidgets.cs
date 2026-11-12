using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>
    /// Minigame-specific info card in the HUD (top-left, under the title). LevelHUD picks the right one for the scene
    /// with <see cref="Create"/>; each widget builds its own content into the card and refreshes it every frame.
    /// </summary>
    public abstract class HudWidget : MonoBehaviour
    {
        public const float Width = 420f;
        protected GameManager gm;
        protected RectTransform card;

        /// <summary>Creates the widget that fits the current scene, or null if none applies.</summary>
        public static HudWidget Create(Transform parent, GameManager gm, Vector2 topLeft)
        {
            HudWidget widget = null;
            float height;
            if (FindAnyObjectByType<MemoryArena>() != null) { height = 150f; widget = Make<MemoryHudWidget>(); }
            else if (LeafPileArena.Instance != null) { height = 196f; widget = Make<LeafPileHudWidget>(); }
            else if (!gm.IsSurvival) { height = 130f; widget = Make<RaceHudWidget>(); }
            else return null;

            widget.gm = gm;
            widget.card = UIFactory.CreateCard(parent, widget.GetType().Name, new Vector2(0f, 1f), topLeft,
                new Vector2(Width, height)).rectTransform;
            widget.transform.SetParent(widget.card, false);
            widget.Build();
            return widget;

            T Make<T>() where T : HudWidget => new GameObject(typeof(T).Name).AddComponent<T>();
        }

        protected abstract void Build();

        /// <summary>Small label in the card at a vertical offset from its top.</summary>
        protected Text Label(string name, string text, float y, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, float x = 0f, float width = Width - 48f)
        {
            var t = UIFactory.CreateDisplayText(card, name, text, size, color, align);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, size + 12f);
            rt.anchoredPosition = new Vector2(x, y);
            return t;
        }

        protected Image Bar(string name, float y, float height, Color color)
        {
            var fill = UIFactory.CreateBar(card, name, Vector2.zero, new Vector2(Width - 48f, height), color);
            var bg = (RectTransform)fill.transform.parent;
            bg.anchorMin = bg.anchorMax = new Vector2(0.5f, 1f);
            bg.anchoredPosition = new Vector2(0f, y);
            return fill;
        }
    }

    /// <summary>Autumn Rush: course progress with checkpoint marks, checkpoint and fall counters.</summary>
    public class RaceHudWidget : HudWidget
    {
        Image progress;
        Text checkpointText;
        Text fallsText;
        float startZ, finishZ;
        readonly List<(int index, Image mark)> marks = new List<(int, Image)>();
        int checkpointCount;

        protected override void Build()
        {
            Label("Title", "COURSE", -26f, 28, UIFactory.Cream);
            progress = Bar("Progress", -64f, 26f, UIFactory.Gold);

            startZ = gm.startPoint != null ? gm.startPoint.position.z : 0f;
            var finish = FindAnyObjectByType<FinishLine>();
            finishZ = finish != null ? finish.transform.position.z : startZ + 1f;

            // Checkpoint marks on the bar.
            var barRt = (RectTransform)progress.transform.parent;
            var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude);
            checkpointCount = checkpoints.Length;
            foreach (var cp in checkpoints)
            {
                float f = Progress(cp.transform.position.z);
                var rt = UIFactory.CreateRect(barRt, "Checkpoint" + cp.index, new Vector2(f, 0.5f), new Vector2(f, 0.5f),
                    Vector2.zero, new Vector2(8f, 38f));
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = UIFactory.RoundedSprite;
                img.type = Image.Type.Sliced;
                img.raycastTarget = false;
                marks.Add((cp.index, img));
            }

            checkpointText = Label("Checkpoints", "", -104f, 30, Color.white);
            fallsText = Label("Falls", "", -104f, 30, Color.white, TextAnchor.MiddleRight);
        }

        float Progress(float z) => Mathf.Clamp01(Mathf.InverseLerp(startZ, finishZ, z));

        void Update()
        {
            if (gm.player != null) progress.fillAmount = Progress(gm.player.transform.position.z);
            int reached = gm.CheckpointIndex;
            foreach (var (index, mark) in marks)
                mark.color = index <= reached ? new Color(0.45f, 0.9f, 0.3f) : new Color(1f, 1f, 1f, 0.6f);
            checkpointText.text = $"CHECKPOINT {reached}/{checkpointCount}";
            fallsText.text = $"FALLS {gm.FallCount}";
        }
    }

    /// <summary>Leaf Pile: current floor pips, golden acorn count and intensity meter.</summary>
    public class LeafPileHudWidget : HudWidget
    {
        static readonly Color PipOn = new Color(1f, 0.6f, 0.2f);
        static readonly Color PipOff = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color IntensityLow = new Color(1f, 0.83f, 0.3f);
        static readonly Color IntensityHigh = new Color(0.9f, 0.2f, 0.12f);

        Text floorText;
        Text acornText;
        Image intensity;
        Image[] pips;

        protected override void Build()
        {
            var arena = LeafPileArena.Instance;
            floorText = Label("Floor", "", -30f, 32, Color.white);

            pips = new Image[arena.FloorCount];
            for (int i = 0; i < pips.Length; i++)
            {
                var rt = UIFactory.CreateRect(card, "Pip" + i, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-40f - (pips.Length - 1 - i) * 40f, -30f), new Vector2(30f, 30f));
                pips[i] = rt.gameObject.AddComponent<Image>();
                pips[i].sprite = UIFactory.RoundedSprite;
                pips[i].type = Image.Type.Sliced;
                pips[i].raycastTarget = false;
            }

            acornText = Label("Acorns", "", -76f, 30, UIFactory.Gold);
            Label("IntensityLabel", "INTENSITY", -120f, 24, UIFactory.Cream);
            intensity = Bar("Intensity", -156f, 24f, IntensityLow);
        }

        void Update()
        {
            var arena = LeafPileArena.Instance;
            if (arena == null) return;
            int floor = arena.PlayerFloorIndex;
            floorText.text = $"FLOOR {floor + 1}/{arena.FloorCount}";
            for (int i = 0; i < pips.Length; i++) pips[i].color = i == floor ? PipOn : PipOff;
            acornText.text = $"GOLDEN ACORNS {gm.BonusCount}  (+{gm.BonusTime:0} s)";
            float d = arena.Difficulty01;
            intensity.fillAmount = Mathf.Max(0.02f, d);
            intensity.color = Color.Lerp(IntensityLow, IntensityHigh, d);
        }
    }

    /// <summary>Harvest Memory: what to do right now (remember / find X) and how much time is left for it.</summary>
    public class MemoryHudWidget : HudWidget
    {
        MemoryArena arena;
        Text phaseText;
        Text roundText;
        Image timer;

        protected override void Build()
        {
            arena = FindAnyObjectByType<MemoryArena>();
            roundText = Label("Round", "", -28f, 26, UIFactory.Cream);
            phaseText = Label("Phase", "", -72f, 40, Color.white);
            phaseText.supportRichText = true;
            timer = Bar("Timer", -118f, 22f, UIFactory.Gold);
        }

        void Update()
        {
            roundText.text = $"ROUNDS SURVIVED: {gm.Rounds}";
            switch (arena != null ? arena.CurrentPhase : MemoryArena.Phase.Waiting)
            {
                case MemoryArena.Phase.Reveal:
                    phaseText.text = "REMEMBER!";
                    timer.fillAmount = 1f - arena.PhaseProgress01;
                    timer.color = UIFactory.Gold;
                    break;
                case MemoryArena.Phase.Choose:
                    var target = arena.CurrentTarget;
                    phaseText.text = target != null
                        ? $"FIND: <color=#{ColorUtility.ToHtmlStringRGB(target.color)}>{target.name}</color>"
                        : "FIND IT!";
                    timer.fillAmount = 1f - arena.PhaseProgress01;
                    timer.color = Color.Lerp(new Color(0.9f, 0.2f, 0.12f), UIFactory.Gold, timer.fillAmount * 1.5f);
                    break;
                case MemoryArena.Phase.Resolve:
                    phaseText.text = "HOLD ON!";
                    timer.fillAmount = 0f;
                    break;
                default:
                    phaseText.text = "GET READY";
                    timer.fillAmount = 0f;
                    break;
            }
        }
    }
}
