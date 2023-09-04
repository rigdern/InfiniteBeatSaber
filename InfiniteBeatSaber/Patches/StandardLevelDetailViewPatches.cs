using HarmonyLib;
using System;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(StandardLevelDetailView))]
    internal static class StandardLevelDetailViewPatches
    {
        // Like `StandardLevelDetailView.didChangeDifficultyBeatmapEvent` but additionally fires
        // when `selectedDifficultyBeatmap` is initialized (not only when the player changes it
        // in the UI). An example of when this fires but when the built-in event does not: player
        // selects a different song.
        public static event Action<StandardLevelDetailView, IDifficultyBeatmap> DidChangeDifficultyBeatmap;

        [HarmonyPatch(nameof(StandardLevelDetailView.RefreshContent))]
        [HarmonyPostfix]
        public static void RefreshContentPostfix(StandardLevelDetailView __instance)
        {
            // `RefreshContent` is the only place that assigns `selectedDifficultyBeatmap` so
            // it seems like a good place to raise this event.
            DidChangeDifficultyBeatmap(__instance, __instance.selectedDifficultyBeatmap);
        }
    }
}
