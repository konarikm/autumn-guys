using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Bonus pickup: adds seconds to the survival score. Disappears when its tile falls.</summary>
    public class GoldenAcorn : MonoBehaviour
    {
        public float bonusSeconds = 5f;
        public float bobHeight = 0.25f;
        public float bobSpeed = 2.5f;
        public float spinSpeed = 120f;

        LeafTile tile;
        Vector3 baseLocalPos;
        float phase;

        public void Attach(LeafTile onTile)
        {
            tile = onTile;
            transform.SetParent(tile.transform, true);
            baseLocalPos = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            if (tile != null && (tile.HasFallen || !tile.gameObject.activeInHierarchy))
            {
                Destroy(gameObject);
                return;
            }
            float bob = Mathf.Sin((Time.time + phase) * bobSpeed) * bobHeight;
            var parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
            transform.localPosition = baseLocalPos + Vector3.up * (bob / Mathf.Max(0.0001f, parentScale));
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameManager.GameState.Running) return;
            gm.AddBonus(bonusSeconds);
            Destroy(gameObject);
        }
    }
}
