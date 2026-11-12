using System.Collections;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Drops acorns on random tiles. Each drop is announced by a shadow marker on the target tile.</summary>
    [RequireComponent(typeof(LeafPileArena))]
    public class AcornDropper : MonoBehaviour
    {
        public FallingAcorn acornPrefab;
        public Material markerMaterial;

        public float firstDropDelay = 5f;
        public float intervalStart = 4f;
        public float intervalEnd = 1.2f;
        [Tooltip("Time between the marker appearing and the acorn landing.")]
        public float warningTime = 1.2f;
        public float dropHeight = 18f;
        [Tooltip("Chance that an acorn targets a tile close to the player (keeps the pressure on).")]
        [Range(0f, 1f)] public float targetPlayerChance = 0.5f;
        public float nearPlayerRadius = 7f;

        LeafPileArena arena;

        void Awake() => arena = GetComponent<LeafPileArena>();

        IEnumerator Start()
        {
            while (!arena.IsRunning) yield return null;
            yield return new WaitForSeconds(firstDropDelay);

            while (true)
            {
                if (arena.IsRunning) DropOne();
                yield return new WaitForSeconds(Mathf.Lerp(intervalStart, intervalEnd, arena.Difficulty01));
            }
        }

        void DropOne()
        {
            if (acornPrefab == null) return;
            int floor = arena.PlayerFloorIndex;
            LeafTile tile = null;
            if (Random.value < targetPlayerChance && arena.Player != null)
                tile = arena.GetRandomIntactTile(floor, arena.Player.transform.position, nearPlayerRadius);
            if (tile == null) tile = arena.GetRandomIntactTileAtOrBelow(floor);
            if (tile == null) return;

            Vector3 land = tile.TopCenter;
            var marker = CreateMarker(tile, land);
            var acorn = Instantiate(acornPrefab, land + Vector3.up * dropHeight, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            acorn.Launch(tile, land, warningTime, marker);
        }

        GameObject CreateMarker(LeafTile tile, Vector3 land)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "AcornMarker";
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(tile.transform, true);
            marker.transform.position = land + Vector3.up * 0.04f;
            marker.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            if (markerMaterial != null) marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            marker.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return marker;
        }
    }
}
