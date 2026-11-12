using System.Collections;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Tile that shakes after the player steps on it, then falls and respawns after a while.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingPlatform : MonoBehaviour
    {
        public float shakeTime = 0.7f;
        public float shakeAmount = 0.06f;
        public float respawnTime = 4f;
        public Color warningColor = new Color(0.9f, 0.25f, 0.1f);

        Rigidbody rb;
        Vector3 startPos;
        Quaternion startRot;
        Renderer rend;
        MaterialPropertyBlock block;
        Color baseColor;
        bool triggered;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            startPos = transform.position;
            startRot = transform.rotation;
            rend = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_BaseColor"))
                baseColor = rend.sharedMaterial.GetColor("_BaseColor");
        }

        void OnCollisionEnter(Collision collision)
        {
            if (triggered) return;
            if (collision.collider.GetComponentInParent<PlayerController>() == null) return;
            // Only when stepped on from above.
            if (collision.transform.position.y < GetComponent<Collider>().bounds.max.y - 0.2f) return;
            StartCoroutine(FallRoutine());
        }

        IEnumerator FallRoutine()
        {
            triggered = true;
            AudioManager.Play(Sfx.TileShake, 0.7f, 0.1f);
            float t = 0f;
            while (t < shakeTime)
            {
                t += Time.deltaTime;
                transform.position = startPos + Random.insideUnitSphere * shakeAmount;
                SetColor(Color.Lerp(baseColor, warningColor, t / shakeTime));
                yield return null;
            }

            transform.position = startPos;
            rb.isKinematic = false;
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
            AudioManager.Play(Sfx.TileFall, 0.6f, 0.1f);

            yield return new WaitForSeconds(respawnTime);

            rb.isKinematic = true;
            transform.SetPositionAndRotation(startPos, startRot);
            SetColor(baseColor);
            triggered = false;
        }

        void SetColor(Color c)
        {
            if (rend == null) return;
            rend.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            rend.SetPropertyBlock(block);
        }
    }
}
