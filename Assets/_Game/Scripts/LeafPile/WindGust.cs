using System.Collections;
using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Periodic, telegraphed wind gusts that push the player sideways.</summary>
    [RequireComponent(typeof(LeafPileArena))]
    public class WindGust : MonoBehaviour
    {
        public float firstGustDelay = 12f;
        public float intervalStart = 18f;
        public float intervalEnd = 10f;
        public float warningTime = 1.5f;
        public float gustDuration = 2.5f;
        public float strengthStart = 3f;
        public float strengthEnd = 5f;
        [Tooltip("Seconds to ramp the wind in and out.")]
        public float rampTime = 0.3f;
        [Tooltip("Leaf burst played along the wind direction (optional).")]
        public ParticleSystem leafBurst;

        LeafPileArena arena;

        void Awake() => arena = GetComponent<LeafPileArena>();

        IEnumerator Start()
        {
            while (!arena.IsRunning) yield return null;
            yield return new WaitForSeconds(firstGustDelay);

            while (true)
            {
                if (arena.IsRunning) yield return Gust();
                yield return new WaitForSeconds(Mathf.Lerp(intervalStart, intervalEnd, arena.Difficulty01));
            }
        }

        IEnumerator Gust()
        {
            var player = arena.Player;
            var hud = GameManager.Instance != null ? GameManager.Instance.Hud : null;
            if (player == null) yield break;

            Vector2 r = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(r.x, 0f, r.y);
            float strength = Mathf.Lerp(strengthStart, strengthEnd, arena.Difficulty01);

            if (hud != null)
            {
                hud.Flash("WIND!");
                hud.ShowWind(dir, true);
            }
            yield return new WaitForSeconds(warningTime);
            if (!arena.IsRunning) { End(player, hud); yield break; }

            if (hud != null) hud.ShowWind(dir, false);
            AudioManager.Play(Sfx.Wind, 1f, 0.05f);
            if (leafBurst != null)
            {
                leafBurst.transform.SetPositionAndRotation(
                    player.transform.position - dir * 10f + Vector3.up * 1.5f, Quaternion.LookRotation(dir));
                leafBurst.Play();
            }

            for (float t = 0f; t < gustDuration; t += Time.deltaTime)
            {
                if (!arena.IsRunning) break;
                float ramp = Mathf.Min(1f, t / rampTime, (gustDuration - t) / rampTime);
                player.ExternalVelocity = dir * (strength * Mathf.Clamp01(ramp));
                yield return null;
            }
            End(player, hud);
        }

        static void End(PlayerController player, LevelHUD hud)
        {
            player.ExternalVelocity = Vector3.zero;
            if (hud != null) hud.HideWind();
        }
    }
}
