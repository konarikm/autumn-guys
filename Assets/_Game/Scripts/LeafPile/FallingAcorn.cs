using UnityEngine;

namespace AutumnGuys
{
    /// <summary>
    /// Acorn dropped by <see cref="AcornDropper"/>. Falls onto a target point in a fixed time, knocks out the tile
    /// it lands on and knocks back the player if it hits them.
    /// </summary>
    public class FallingAcorn : MonoBehaviour
    {
        public float impactRadius = 1.4f;
        public float knockbackForce = 9f;
        public float knockbackUp = 6f;
        public float stunDuration = 0.5f;
        public float lifetimeAfterImpact = 2.5f;
        public float markerMaxSize = 2.4f;

        LeafTile targetTile;
        Vector3 startPos;
        Vector3 landPos;
        float duration;
        float t;
        bool landed;
        bool hitPlayer;
        GameObject marker;

        public void Launch(LeafTile tile, Vector3 landPoint, float fallDuration, GameObject warningMarker)
        {
            targetTile = tile;
            startPos = transform.position;
            landPos = landPoint;
            duration = Mathf.Max(0.1f, fallDuration);
            marker = warningMarker;
        }

        void Update()
        {
            if (landed) return;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            // Accelerating fall (feels like gravity) and a slow spin.
            transform.position = Vector3.LerpUnclamped(startPos, landPos, k * k);
            transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);

            if (marker != null)
            {
                float s = Mathf.Lerp(0.3f, markerMaxSize, k);
                var parentScale = marker.transform.parent != null ? marker.transform.parent.lossyScale : Vector3.one;
                marker.transform.localScale = new Vector3(s / parentScale.x, 0.02f / parentScale.y, s / parentScale.z);
            }

            if (k >= 1f) Land();
        }

        void Land()
        {
            landed = true;
            if (marker != null) Destroy(marker);
            if (targetTile != null) targetTile.TriggerFall(true);

            foreach (var c in Physics.OverlapSphere(landPos + Vector3.up * 0.8f, impactRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                var player = c.GetComponentInParent<PlayerController>();
                if (player != null) { HitPlayer(player); break; }
            }

            // Tumble down with the tile.
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.down * 4f;
            }
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Destroy(gameObject, lifetimeAfterImpact);
        }

        void OnTriggerEnter(Collider other)
        {
            if (landed) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) HitPlayer(player);
        }

        void HitPlayer(PlayerController player)
        {
            if (hitPlayer) return;
            hitPlayer = true;
            Vector3 dir = player.transform.position - landPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                Vector2 r = Random.insideUnitCircle.normalized;
                dir = new Vector3(r.x, 0f, r.y);
            }
            player.Knockback(dir.normalized * knockbackForce + Vector3.up * knockbackUp, stunDuration);
        }
    }
}
