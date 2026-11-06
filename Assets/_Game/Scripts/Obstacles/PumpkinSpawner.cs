using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Periodically spawns rolling pumpkins that roll down along the spawner's forward direction.</summary>
    public class PumpkinSpawner : MonoBehaviour
    {
        public GameObject pumpkinPrefab;
        public float interval = 1.6f;
        public float randomInterval = 0.6f;
        public float lateralSpread = 5f;
        public float initialSpeed = 4f;
        public float pumpkinLifetime = 14f;

        float timer;

        void Start() => timer = Random.Range(0f, interval);

        void Update()
        {
            if (pumpkinPrefab == null) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = interval + Random.Range(-randomInterval, randomInterval);

            Vector3 pos = transform.position + transform.right * Random.Range(-lateralSpread, lateralSpread);
            var go = Instantiate(pumpkinPrefab, pos, Random.rotation);
            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.linearVelocity = transform.forward * initialSpeed;
            Destroy(go, pumpkinLifetime);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position - transform.right * lateralSpread, transform.position + transform.right * lateralSpread);
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
    }
}
