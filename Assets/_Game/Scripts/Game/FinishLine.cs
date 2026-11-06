using UnityEngine;

namespace AutumnGuys
{
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour
    {
        public ParticleSystem confetti;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Running) return;
            if (confetti != null) confetti.Play();
            GameManager.Instance.Finish();
        }
    }
}
