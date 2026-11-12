using System;
using System.Collections;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>
    /// Leaf-pile hex tile for the survival minigame. Shakes after the player stands on it (or an acorn hits it),
    /// then falls and never comes back. Shake time comes from <see cref="LeafPileArena.CurrentShakeTime"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LeafTile : MonoBehaviour
    {
        [Tooltip("Floor index, 0 = top.")]
        public int floor;
        public float shakeAmount = 0.07f;
        public float fallbackShakeTime = 0.6f;
        [Tooltip("Seconds after falling before the tile is disabled.")]
        public float fallenLifetime = 3f;
        public Color warningColor = new Color(0.32f, 0.13f, 0.05f);

        /// <summary>Not yet triggered – safe to stand on.</summary>
        public bool IsIntact => !triggered;
        /// <summary>Already dropping (no longer solid).</summary>
        public bool HasFallen { get; private set; }
        /// <summary>World position of the tile's top centre.</summary>
        public Vector3 TopCenter => new Vector3(startPos.x, topY, startPos.z);

        public event Action<LeafTile> Triggered;

        Rigidbody rb;
        Collider tileCollider;
        Vector3 startPos;
        float topY;
        Renderer[] renderers;
        Color[] baseColors;
        MaterialPropertyBlock block;
        bool triggered;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            tileCollider = GetComponent<Collider>();
            startPos = transform.position;
            topY = tileCollider.bounds.max.y;
            block = new MaterialPropertyBlock();

            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].sharedMaterial;
                baseColors[i] = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            }
        }

        void OnCollisionEnter(Collision collision) => CheckPlayer(collision);
        void OnCollisionStay(Collision collision) => CheckPlayer(collision);

        void CheckPlayer(Collision collision)
        {
            if (triggered) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.GameState.Running) return;
            if (collision.collider.GetComponentInParent<PlayerController>() == null) return;
            // Only when standing on top.
            if (collision.transform.position.y < topY - 0.2f) return;
            TriggerFall(false);
        }

        /// <param name="instant">Skip the shake (acorn impact).</param>
        public void TriggerFall(bool instant)
        {
            if (triggered || !isActiveAndEnabled) return;
            float shake = instant ? 0f :
                LeafPileArena.Instance != null ? LeafPileArena.Instance.CurrentShakeTime : fallbackShakeTime;
            StartCoroutine(FallRoutine(shake));
        }

        IEnumerator FallRoutine(float shakeTime)
        {
            triggered = true;
            Triggered?.Invoke(this);

            for (float t = 0f; t < shakeTime; t += Time.deltaTime)
            {
                transform.position = startPos + UnityEngine.Random.insideUnitSphere * shakeAmount;
                SetWarning(t / shakeTime);
                yield return null;
            }

            transform.position = startPos;
            SetWarning(1f);
            HasFallen = true;
            // Not solid anymore, so falling tiles don't pile up on the floor below.
            tileCollider.enabled = false;
            rb.isKinematic = false;
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.VelocityChange);

            yield return new WaitForSeconds(fallenLifetime);
            gameObject.SetActive(false);
        }

        void SetWarning(float t)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(baseColors[i], warningColor, t));
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
