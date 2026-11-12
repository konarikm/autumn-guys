using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Keeps a few golden acorns on intact tiles of the player's current floor.</summary>
    [RequireComponent(typeof(LeafPileArena))]
    public class GoldenAcornSpawner : MonoBehaviour
    {
        public GoldenAcorn goldenAcornPrefab;
        public int maxActive = 2;
        public float firstSpawnDelay = 4f;
        public float spawnInterval = 6f;
        [Tooltip("Spawn at least this far from the player so it takes some running.")]
        public float minDistanceFromPlayer = 5f;
        public float heightAboveTile = 1f;

        LeafPileArena arena;
        readonly List<GoldenAcorn> active = new List<GoldenAcorn>();

        void Awake() => arena = GetComponent<LeafPileArena>();

        IEnumerator Start()
        {
            while (!arena.IsRunning) yield return null;
            yield return new WaitForSeconds(firstSpawnDelay);

            while (true)
            {
                active.RemoveAll(a => a == null);
                if (arena.IsRunning && active.Count < maxActive) SpawnOne();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        void SpawnOne()
        {
            if (goldenAcornPrefab == null || arena.Player == null) return;
            int floor = arena.PlayerFloorIndex;
            Vector3 playerPos = arena.Player.transform.position;
            var tile = arena.GetRandomIntactTile(floor, playerPos, 0f, minDistanceFromPlayer)
                       ?? arena.GetRandomIntactTile(floor);
            if (tile == null) return;

            var acorn = Instantiate(goldenAcornPrefab, tile.TopCenter + Vector3.up * heightAboveTile, Quaternion.identity);
            acorn.Attach(tile);
            active.Add(acorn);
        }
    }
}
