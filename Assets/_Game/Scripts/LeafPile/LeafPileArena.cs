using System.Collections.Generic;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>
    /// Survival minigame "Leaf Pile": owns the difficulty curve and the registry of leaf tiles per floor.
    /// Hazard spawners (AcornDropper, WindGust, GoldenAcornSpawner) query it.
    /// </summary>
    public class LeafPileArena : MonoBehaviour
    {
        public static LeafPileArena Instance { get; private set; }

        [Tooltip("Top surface height of each floor, top floor first.")]
        public float[] floorHeights = { 0f, -8f, -16f };
        [Tooltip("Seconds until difficulty reaches its maximum.")]
        public float rampDuration = 120f;
        [Tooltip("Optional easing of the difficulty (x = time 0..1, y = difficulty 0..1).")]
        public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Tile decay")]
        public float shakeTimeStart = 0.6f;
        public float shakeTimeEnd = 0.25f;

        public float Difficulty01
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || rampDuration <= 0f) return 0f;
                return Mathf.Clamp01(difficultyCurve.Evaluate(Mathf.Clamp01(gm.ElapsedTime / rampDuration)));
            }
        }

        public float CurrentShakeTime => Mathf.Lerp(shakeTimeStart, shakeTimeEnd, Difficulty01);

        public bool IsRunning => GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Running;
        public PlayerController Player => GameManager.Instance != null ? GameManager.Instance.player : null;
        public int FloorCount => floorHeights.Length;

        /// <summary>Index of the floor the player is on (or falling towards).</summary>
        public int PlayerFloorIndex
        {
            get
            {
                var p = Player;
                if (p == null) return 0;
                float y = p.transform.position.y;
                for (int i = 0; i < floorHeights.Length; i++)
                    if (y > floorHeights[i] - 3f) return i;
                return floorHeights.Length - 1;
            }
        }

        List<LeafTile>[] floors;
        readonly List<LeafTile> scratch = new List<LeafTile>();

        void Awake()
        {
            Instance = this;
            floors = new List<LeafTile>[floorHeights.Length];
            for (int i = 0; i < floors.Length; i++) floors[i] = new List<LeafTile>();
            foreach (var tile in FindObjectsByType<LeafTile>(FindObjectsInactive.Exclude))
                floors[Mathf.Clamp(tile.floor, 0, floors.Length - 1)].Add(tile);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Random intact tile on the floor, optionally limited to a radius around a point. Null if none.</summary>
        public LeafTile GetRandomIntactTile(int floor, Vector3? near = null, float radius = 0f, float minDistance = 0f)
        {
            if (floors == null || floor < 0 || floor >= floors.Length) return null;
            scratch.Clear();
            foreach (var t in floors[floor])
            {
                if (t == null || !t.IsIntact || !t.gameObject.activeInHierarchy) continue;
                if (near.HasValue)
                {
                    Vector3 d = t.transform.position - near.Value;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (radius > 0f && dist > radius) continue;
                    if (dist < minDistance) continue;
                }
                scratch.Add(t);
            }
            return scratch.Count > 0 ? scratch[Random.Range(0, scratch.Count)] : null;
        }

        /// <summary>Random intact tile on the given floor or any floor below it.</summary>
        public LeafTile GetRandomIntactTileAtOrBelow(int floor)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                int f = Random.Range(Mathf.Max(0, floor), FloorCount);
                var t = GetRandomIntactTile(f);
                if (t != null) return t;
            }
            for (int f = Mathf.Max(0, floor); f < FloorCount; f++)
            {
                var t = GetRandomIntactTile(f);
                if (t != null) return t;
            }
            return null;
        }
    }
}
