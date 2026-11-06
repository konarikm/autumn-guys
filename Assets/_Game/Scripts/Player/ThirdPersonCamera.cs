using UnityEngine;
using UnityEngine.InputSystem;

namespace AutumnGuys
{
    /// <summary>Orbit camera behind the player, controlled by mouse / right stick, with wall collision.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 7f;
        public float minDistance = 1.5f;
        public float mouseSensitivity = 0.12f;
        public float stickSensitivity = 160f;
        public float minPitch = -25f;
        public float maxPitch = 65f;
        public float followSmooth = 18f;
        public float collisionRadius = 0.3f;
        public LayerMask collisionMask = ~(1 << 2); // ignore "Ignore Raycast" (player)

        public bool InputEnabled { get; set; } = true;

        float yaw;
        float pitch = 18f;
        float currentDistance;
        Vector3 smoothedTarget;

        void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                smoothedTarget = target.position + targetOffset;
            }
            currentDistance = distance;
        }

        public void SnapBehindTarget()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            pitch = 18f;
            smoothedTarget = target.position + targetOffset;
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (InputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = Vector2.zero;
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * stickSensitivity * Time.unscaledDeltaTime;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            smoothedTarget = Vector3.Lerp(smoothedTarget, target.position + targetOffset, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;

            float wanted = distance;
            if (Physics.SphereCast(smoothedTarget, collisionRadius, dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance - 0.1f);

            // Move in fast, ease out slowly.
            currentDistance = wanted < currentDistance ? wanted : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-4f * Time.deltaTime));

            transform.SetPositionAndRotation(smoothedTarget + dir * currentDistance, rot);
        }
    }
}
