using UnityEngine;

namespace AutumnGuys
{
    /// <summary>
    /// Continuously rotating kinematic obstacle (sweeper log, rolling log bridge, spinning disc).
    /// Pushes the player physically and carries them when they stand on it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Rotator : MonoBehaviour, IMovingSurface
    {
        [Tooltip("Rotation axis in local space.")]
        public Vector3 localAxis = Vector3.up;
        [Tooltip("Degrees per second (negative = opposite direction).")]
        public float speed = 90f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(speed * Time.fixedDeltaTime, localAxis));
        }

        public Vector3 WorldAngularVelocity => transform.TransformDirection(localAxis.normalized) * (speed * Mathf.Deg2Rad);

        public Vector3 GetVelocityAtPoint(Vector3 worldPoint)
        {
            return Vector3.Cross(WorldAngularVelocity, worldPoint - transform.position);
        }
    }
}
