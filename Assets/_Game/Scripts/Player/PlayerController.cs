using UnityEngine;
using UnityEngine.InputSystem;

namespace AutumnGuys
{
    /// <summary>
    /// Physics-based (Rigidbody) third-person character controller.
    /// Movement is relative to the camera, supports jump buffering, coyote time,
    /// riding moving/rotating platforms and being knocked back by obstacles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 7f;
        public float groundAcceleration = 60f;
        public float airAcceleration = 18f;
        public float turnSpeed = 14f;

        [Header("Jump")]
        public float jumpVelocity = 8.5f;
        public float extraGravity = 14f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.15f;

        [Header("Ground check")]
        public float groundCheckDistance = 0.15f;
        public float maxGroundAngle = 50f;

        [Header("Knockback")]
        public float stunnedDeceleration = 6f;

        [Header("References")]
        public Animator animator;
        public Transform cameraTransform;

        public bool ControlsEnabled { get; set; } = true;
        public bool IsGrounded => grounded;
        public bool IsStunned => stunTimer > 0f;

        /// <summary>Extra horizontal velocity applied on top of player input (e.g. wind gusts).</summary>
        public Vector3 ExternalVelocity { get; set; }

        Rigidbody rb;
        CapsuleCollider capsule;
        readonly RaycastHit[] hits = new RaycastHit[8];

        Vector2 moveInput;
        float jumpBufferTimer;
        float coyoteTimer;
        float stunTimer;
        float airTime;
        bool grounded;
        Collider groundCollider;
        string currentAnim;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Frictionless capsule so the player doesn't stick to walls.
            capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            moveInput = Vector2.zero;
            bool jumpPressed = false;

            if (ControlsEnabled)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1;
                    jumpPressed |= kb.spaceKey.wasPressedThisFrame;
                }

                var pad = Gamepad.current;
                if (pad != null)
                {
                    moveInput += pad.leftStick.ReadValue();
                    jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
                }
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            jumpBufferTimer -= dt;
            stunTimer -= dt;

            CheckGround();

            // Landing thump after a real jump/fall (not tiny steps).
            if (grounded && airTime > 0.3f) AudioManager.Play(Sfx.Land, Mathf.Clamp01(airTime), 0.08f);

            if (grounded) { coyoteTimer = coyoteTime; airTime = 0f; }
            else { coyoteTimer -= dt; airTime += dt; }

            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 platformVelocity = GetPlatformVelocity() + new Vector3(ExternalVelocity.x, 0f, ExternalVelocity.z);

            Vector3 wishDir = GetWishDirection();

            if (IsStunned)
            {
                // Knocked back: no control, just slow down.
                horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, stunnedDeceleration * dt);
            }
            else
            {
                Vector3 target = wishDir * moveSpeed + new Vector3(platformVelocity.x, 0f, platformVelocity.z);
                float accel = grounded ? groundAcceleration : airAcceleration;
                horizontal = Vector3.MoveTowards(horizontal, target, accel * dt);
            }

            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            // Jump
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && !IsStunned)
            {
                velocity.y = jumpVelocity + Mathf.Max(0f, platformVelocity.y);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                PlayAnim("jump");
            }

            rb.linearVelocity = velocity;

            // Extra gravity for snappier, less floaty jumps.
            if (!grounded) rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);

            // Face the movement direction.
            if (wishDir.sqrMagnitude > 0.01f && !IsStunned)
            {
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * dt));
            }

            UpdateAnimation(horizontal - new Vector3(platformVelocity.x, 0f, platformVelocity.z));
        }

        Vector3 GetWishDirection()
        {
            if (moveInput.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            if (cameraTransform != null)
            {
                fwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }
            return Vector3.ClampMagnitude(fwd * moveInput.y + right * moveInput.x, 1f);
        }

        void CheckGround()
        {
            grounded = false;
            groundCollider = null;

            // Don't snap to the ground while moving up fast (just jumped).
            if (rb.linearVelocity.y > jumpVelocity * 0.5f) return;

            float radius = capsule.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (capsule.radius + 0.05f);
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, hits,
                groundCheckDistance + 0.1f, ~0, QueryTriggerInteraction.Ignore);

            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.collider == capsule || h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) continue; // overlapping at start
                if (Vector3.Angle(h.normal, Vector3.up) > maxGroundAngle) continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    grounded = true;
                    groundCollider = h.collider;
                }
            }
        }

        Vector3 GetPlatformVelocity()
        {
            if (!grounded || groundCollider == null) return Vector3.zero;
            var surface = groundCollider.GetComponentInParent<IMovingSurface>();
            return surface != null ? surface.GetVelocityAtPoint(transform.position) : Vector3.zero;
        }

        /// <summary>Pushes the player away and briefly removes control.</summary>
        public void Knockback(Vector3 velocityChange, float stunDuration)
        {
            rb.linearVelocity = velocityChange;
            stunTimer = Mathf.Max(stunTimer, stunDuration);
            grounded = false;
            PlayAnim("fall");
            AudioManager.Play(Sfx.Hit);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            stunTimer = 0f;
            jumpBufferTimer = 0f;
            PlayAnim("idle", true);
        }

        public void Celebrate()
        {
            ControlsEnabled = false;
            PlayAnim("feel", true);
        }

        void UpdateAnimation(Vector3 relativeHorizontalVelocity)
        {
            if (animator == null || currentAnim == "feel") return;

            if (IsStunned) return;
            if (grounded)
            {
                PlayAnim(relativeHorizontalVelocity.magnitude > 0.8f ? "run" : "idle");
            }
            else if (airTime > 0.35f && rb.linearVelocity.y < -2f)
            {
                PlayAnim("fall");
            }
        }

        void PlayAnim(string trigger, bool force = false)
        {
            if (animator == null || (!force && currentAnim == trigger)) return;
            if (!HasParameter(trigger)) return;
            foreach (var p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(p.name);
            animator.SetTrigger(trigger);
            currentAnim = trigger;
        }

        bool HasParameter(string name)
        {
            foreach (var p in animator.parameters) if (p.name == name) return true;
            return false;
        }
    }
}
