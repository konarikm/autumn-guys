using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AutumnGuys
{
    /// <summary>
    /// "Harvest Memory" round loop: icons appear on the tiles, hide, the target icon is announced,
    /// and every tile without it drops away. Survive as many rounds as possible.
    /// </summary>
    public class MemoryArena : MonoBehaviour
    {
        [Serializable]
        public class Icon
        {
            public string name = "PUMPKIN";
            public GameObject prefab;
            public Color color = Color.white;
        }

        public Icon[] icons;
        [Tooltip("Big sign above the arena where the target icon appears.")]
        public Transform displayAnchor;
        public float displayScale = 2.5f;

        [Header("Difficulty (round 1 → rampRounds)")]
        public int rampRounds = 12;
        public float revealTimeStart = 4f;
        public float revealTimeEnd = 1.5f;
        public float chooseTimeStart = 5f;
        public float chooseTimeEnd = 2.5f;
        public int safeTilesStart = 6;
        public int safeTilesEnd = 2;
        [Tooltip("From this round on, safe tiles are never next to the player's tile.")]
        public int awayFromPlayerFromRound = 5;

        [Header("Timing")]
        public float firstRoundDelay = 0.5f;
        public float pauseBetweenRounds = 1.2f;

        MemoryTile[] tiles;
        GameObject displayIcon;
        GameManager gm;
        LevelHUD Hud => gm != null ? gm.Hud : null;

        bool Running => gm != null && gm.State == GameManager.GameState.Running;

        void Awake()
        {
            tiles = FindObjectsByType<MemoryTile>(FindObjectsInactive.Exclude);
        }

        IEnumerator Start()
        {
            gm = GameManager.Instance;
            while (!Running) yield return null;
            yield return new WaitForSeconds(firstRoundDelay);

            while (Running)
            {
                yield return PlayRound();
                if (!Running) yield break;
                yield return new WaitForSeconds(pauseBetweenRounds);
            }
        }

        IEnumerator PlayRound()
        {
            if (icons == null || icons.Length < 2 || tiles.Length == 0) yield break;

            float d = rampRounds > 0 ? Mathf.Clamp01(gm.Rounds / (float)rampRounds) : 1f;
            float revealTime = Mathf.Lerp(revealTimeStart, revealTimeEnd, d);
            float chooseTime = Mathf.Lerp(chooseTimeStart, chooseTimeEnd, d);
            int safeCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(safeTilesStart, safeTilesEnd, d)), 1, tiles.Length - 1);

            int target = Random.Range(0, icons.Length);
            AssignIcons(target, safeCount, gm.Rounds + 1 >= awayFromPlayerFromRound);

            // 1) Memorise
            foreach (var t in tiles) t.ShowIcon(true);
            AudioManager.Play(Sfx.Reveal, 1f, 0f);
            Hud?.ShowPrompt("Remember the icons!");
            yield return WaitRunning(revealTime);
            if (!Running) yield break;

            // 2) Hide and announce the target
            foreach (var t in tiles) t.ShowIcon(false);
            var icon = icons[target];
            Hud?.ShowPrompt($"Find the <color=#{ColorUtility.ToHtmlStringRGB(icon.color)}>{icon.name}</color>!");
            ShowDisplay(icon.prefab);

            for (float left = chooseTime; left > 0f; left -= Time.deltaTime)
            {
                if (!Running) yield break;
                string n = Mathf.CeilToInt(left).ToString();
                if (n != lastCountdown)
                {
                    Hud?.ShowCountdown(n);
                    AudioManager.Play(Sfx.Tick, 1f, 0f);
                    lastCountdown = n;
                }
                yield return null;
            }
            lastCountdown = null;
            Hud?.ShowCountdown(null);
            Hud?.ShowPrompt(null);
            if (!Running) yield break;

            // 3) Drop the wrong tiles, reveal the right ones
            AudioManager.Play(Sfx.TileFall, 1f, 0f);
            float dropDuration = 0f;
            foreach (var t in tiles)
            {
                if (t.IconIndex == target) t.ShowIcon(true);
                else
                {
                    StartCoroutine(t.Drop());
                    dropDuration = Mathf.Max(dropDuration, t.shakeTime + t.fallTime * 0.6f);
                }
            }
            yield return new WaitForSeconds(dropDuration);
            ShowDisplay(null);
            if (!Running) yield break;

            // Player is falling – the round doesn't count, wait for the game over.
            if (gm.player != null && gm.player.transform.position.y < -1f)
            {
                while (Running) yield return null;
                yield break;
            }

            gm.AddRound();
            Hud?.Flash("SAFE!");

            // 4) Wait for the floor to come back
            foreach (var t in tiles) if (t.IconIndex == target) t.ShowIcon(false);
            while (Running && AnyTileDown()) yield return null;
        }

        string lastCountdown;

        IEnumerator WaitRunning(float seconds)
        {
            for (float t = 0f; t < seconds && Running; t += Time.deltaTime) yield return null;
        }

        bool AnyTileDown()
        {
            foreach (var t in tiles) if (t.IsDown) return true;
            return false;
        }

        void AssignIcons(int target, int safeCount, bool avoidPlayer)
        {
            var order = new List<MemoryTile>(tiles);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // Tiles close to the player go to the end of the list so they don't become safe for free.
            if (avoidPlayer && gm.player != null)
            {
                Vector3 p = gm.player.transform.position;
                order.Sort((a, b) => Near(a, p).CompareTo(Near(b, p)));
            }

            for (int i = 0; i < order.Count; i++)
            {
                int index = target;
                if (i >= safeCount)
                {
                    index = Random.Range(0, icons.Length - 1);
                    if (index >= target) index++;
                }
                order[i].SetIcon(index, icons[index].prefab);
            }
        }

        static int Near(MemoryTile tile, Vector3 p)
        {
            Vector3 d = tile.transform.position - p;
            d.y = 0f;
            return d.magnitude < 4.5f ? 1 : 0;
        }

        void ShowDisplay(GameObject prefab)
        {
            if (displayIcon != null) Destroy(displayIcon);
            if (prefab == null || displayAnchor == null) return;
            displayIcon = Instantiate(prefab, displayAnchor);
            displayIcon.transform.localPosition = Vector3.zero;
            displayIcon.transform.localRotation = Quaternion.identity;
            displayIcon.transform.localScale = Vector3.one * displayScale;
        }

        void Update()
        {
            if (displayIcon != null) displayIcon.transform.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
        }
    }
}
