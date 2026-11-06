using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Platform smoothly moving back and forth between its start position and start + offset.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovingPlatform : MonoBehaviour, IMovingSurface
    {
        public Vector3 offset = new Vector3(6f, 0f, 0f);
        [Tooltip("Seconds for a full there-and-back cycle.")]
        public float period = 4f;
        [Range(0f, 1f)] public float phase;

        Rigidbody rb;
        Vector3 startPos;
        Vector3 velocity;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            startPos = transform.position;
        }

        void FixedUpdate()
        {
            float t = (Time.time / period + phase) * Mathf.PI * 2f;
            float k = (1f - Mathf.Cos(t)) * 0.5f; // 0..1, eased at both ends
            Vector3 next = startPos + offset * k;
            velocity = (next - rb.position) / Time.fixedDeltaTime;
            rb.MovePosition(next);
        }

        public Vector3 GetVelocityAtPoint(Vector3 worldPoint) => velocity;

        void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying ? startPos : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(from, from + offset);
            Gizmos.DrawWireCube(from + offset, transform.lossyScale);
        }
    }
}
