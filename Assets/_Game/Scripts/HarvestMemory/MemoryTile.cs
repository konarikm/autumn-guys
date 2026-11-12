using System.Collections;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>
    /// Floor tile for "Harvest Memory". Shows an icon model on top, can drop away on command
    /// and rises back into place afterwards.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MemoryTile : MonoBehaviour
    {
        [Tooltip("Icon models are spawned under this transform (top centre of the tile).")]
        public Transform iconAnchor;
        public float iconPopTime = 0.3f;
        public float iconSpinSpeed = 60f;
        public float shakeTime = 0.35f;
        public float shakeAmount = 0.06f;
        [Tooltip("How long the tile falls before it comes back.")]
        public float fallTime = 1.4f;
        public float returnTime = 0.7f;
        public float returnDepth = 6f;
        public Color warningColor = new Color(0.35f, 0.14f, 0.06f);

        public int IconIndex { get; private set; } = -1;
        public bool IsDown { get; private set; }

        Rigidbody rb;
        Collider tileCollider;
        Vector3 startPos;
        Quaternion startRot;
        Renderer rend;
        MaterialPropertyBlock block;
        Color baseColor;
        GameObject icon;
        Coroutine iconRoutine;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            tileCollider = GetComponent<Collider>();
            startPos = transform.position;
            startRot = transform.rotation;
            rend = GetComponentInChildren<Renderer>(); // tile visual (icons are spawned later)
            block = new MaterialPropertyBlock();
            if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_BaseColor"))
                baseColor = rend.sharedMaterial.GetColor("_BaseColor");
            if (iconAnchor == null) iconAnchor = transform;
            iconAnchor.localScale = Vector3.zero;
        }

        void Update()
        {
            if (icon != null) icon.transform.Rotate(0f, iconSpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        /// <summary>Replaces the tile's icon (hidden until <see cref="ShowIcon"/>).</summary>
        public void SetIcon(int index, GameObject prefab)
        {
            if (icon != null) Destroy(icon);
            IconIndex = index;
            if (prefab == null) return;
            icon = Instantiate(prefab, iconAnchor);
            icon.transform.localPosition = Vector3.zero;
            icon.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        public void ShowIcon(bool show)
        {
            if (iconRoutine != null) StopCoroutine(iconRoutine);
            iconRoutine = StartCoroutine(ScaleIcon(show ? 1f : 0f));
        }

        IEnumerator ScaleIcon(float target)
        {
            float from = iconAnchor.localScale.x;
            for (float t = 0f; t < iconPopTime; t += Time.deltaTime)
            {
                float k = t / iconPopTime;
                // Small overshoot when popping up.
                float s = Mathf.LerpUnclamped(from, target, target > from ? 1f - Mathf.Pow(1f - k, 3f) * Mathf.Cos(k * 6f) : k);
                iconAnchor.localScale = Vector3.one * Mathf.Max(0f, s);
                yield return null;
            }
            iconAnchor.localScale = Vector3.one * target;
        }

        /// <summary>Shake, fall, then rise back into place.</summary>
        public IEnumerator Drop()
        {
            IsDown = true;
            ShowIcon(false);
            for (float t = 0f; t < shakeTime; t += Time.deltaTime)
            {
                transform.position = startPos + Random.insideUnitSphere * shakeAmount;
                SetColor(Color.Lerp(baseColor, warningColor, t / shakeTime));
                yield return null;
            }
            transform.position = startPos;

            tileCollider.enabled = false;
            rb.isKinematic = false;
            rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.VelocityChange);
            yield return new WaitForSeconds(fallTime);

            // Come back up from below.
            rb.isKinematic = true;
            Vector3 below = startPos + Vector3.down * returnDepth;
            transform.SetPositionAndRotation(below, startRot);
            SetColor(baseColor);
            for (float t = 0f; t < returnTime; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / returnTime, 3f);
                transform.position = Vector3.Lerp(below, startPos, k);
                yield return null;
            }
            transform.position = startPos;
            tileCollider.enabled = true;
            IsDown = false;
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
