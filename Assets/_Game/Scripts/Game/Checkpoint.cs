using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Trigger volume. When the player passes it, it becomes the new respawn point.</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Order along the course. Higher index = further.")]
        public int index;
        [Tooltip("Renderers recoloured when the checkpoint is activated (flags).")]
        public Renderer[] indicators;
        public Color activeColor = new Color(0.35f, 0.85f, 0.25f);

        public Vector3 SpawnPosition => transform.position + Vector3.up * 0.2f;
        public Quaternion SpawnRotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance != null && GameManager.Instance.TrySetCheckpoint(this))
                Activate();
        }

        void Activate()
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in indicators)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", activeColor);
                r.SetPropertyBlock(block);
            }
        }
    }
}
