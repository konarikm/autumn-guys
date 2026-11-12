using System.Collections.Generic;

namespace AutumnGuys
{
    /// <summary>Display name, goal and controls hint of each minigame, looked up by scene name.</summary>
    public readonly struct MinigameInfo
    {
        public readonly string Title;
        public readonly string Goal;
        public readonly string Hint;

        MinigameInfo(string title, string goal, string hint)
        {
            Title = title;
            Goal = goal;
            Hint = hint;
        }

        const string BaseHint = "WASD move · Space jump · Esc pause";

        static readonly Dictionary<string, MinigameInfo> Table = new Dictionary<string, MinigameInfo>
        {
            { SceneNames.AutumnRush, new MinigameInfo("AUTUMN RUSH",
                "Reach the finish as fast as you can!", BaseHint + " · R checkpoint") },
            { SceneNames.LeafPile, new MinigameInfo("LEAF PILE",
                "Keep moving – the leaves fall away under you!", BaseHint + " · Grab golden acorns") },
            { SceneNames.HarvestMemory, new MinigameInfo("HARVEST MEMORY",
                "Remember the icons and stand on the right one!", BaseHint) },
        };

        public static MinigameInfo For(string scene) =>
            Table.TryGetValue(scene, out var info) ? info : new MinigameInfo(scene.ToUpperInvariant(), "", BaseHint);
    }
}
