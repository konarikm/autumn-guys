using UnityEngine;

namespace AutumnGuys
{
    /// <summary>Best time persistence (PlayerPrefs) and shared helpers.</summary>
    public static class SaveData
    {
        const string BestTimeKey = "AutumnGuys_BestTime_";

        public static float GetBestTime(string level) => PlayerPrefs.GetFloat(BestTimeKey + level, 0f);

        /// <returns>true if this is a new record.</returns>
        public static bool SubmitTime(string level, float time)
        {
            float best = GetBestTime(level);
            if (best > 0f && time >= best) return false;
            PlayerPrefs.SetFloat(BestTimeKey + level, time);
            PlayerPrefs.Save();
            return true;
        }

        const string BestScoreKey = "AutumnGuys_BestScore_";

        /// <summary>Best survival score (higher is better), 0 if none.</summary>
        public static float GetBestScore(string level) => PlayerPrefs.GetFloat(BestScoreKey + level, 0f);

        /// <returns>true if this is a new record.</returns>
        public static bool SubmitScore(string level, float score)
        {
            if (score <= GetBestScore(level)) return false;
            PlayerPrefs.SetFloat(BestScoreKey + level, score);
            PlayerPrefs.Save();
            return true;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f) return "--:--.--";
            int min = (int)(seconds / 60f);
            float sec = seconds - min * 60;
            return $"{min:00}:{sec:00.00}";
        }
    }

    public static class SceneNames
    {
        public const string Splash = "Splash";
        public const string MainMenu = "MainMenu";
        public const string Level01 = "Level01";
        public const string LeafPile = "LeafPile";
        public const string HarvestMemory = "HarvestMemory";
    }
}
