using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Mushroom trampoline – launches the player upwards when they land on it.</summary>
    public class Bouncer : MonoBehaviour
    {
        public float bounceVelocity = 16f;

        void OnCollisionEnter(Collision collision)
        {
            var player = collision.collider.GetComponentInParent<PlayerController>();
            if (player == null) return;
            var rb = player.GetComponent<Rigidbody>();
            Vector3 v = rb.linearVelocity;
            v.y = bounceVelocity;
            rb.linearVelocity = v;
        }
    }
}
