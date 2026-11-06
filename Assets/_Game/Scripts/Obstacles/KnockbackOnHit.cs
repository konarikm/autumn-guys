using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Knocks the player back when this object hits them (logs, pumpkins, bumpers).</summary>
    public class KnockbackOnHit : MonoBehaviour
    {
        public float force = 12f;
        public float upwardForce = 5f;
        public float stunDuration = 0.6f;
        [Tooltip("Ignore hits slower than this (only for objects with a non-kinematic Rigidbody).")]
        public float minImpactSpeed = 2f;

        void OnCollisionEnter(Collision collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerController>();
            if (player == null) return;

            Vector3 dir = Vector3.zero;

            // Prefer the direction this object is moving at the contact point.
            var surface = GetComponent<IMovingSurface>();
            var body = GetComponent<Rigidbody>();
            if (surface != null)
                dir = surface.GetVelocityAtPoint(player.transform.position);
            else if (body != null && !body.isKinematic)
            {
                if (body.linearVelocity.magnitude < minImpactSpeed) return;
                dir = body.linearVelocity;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = player.transform.position - transform.position;
                dir.y = 0f;
            }

            Vector3 push = dir.normalized * force + Vector3.up * upwardForce;
            player.Knockback(push, stunDuration);
        }
    }
}
