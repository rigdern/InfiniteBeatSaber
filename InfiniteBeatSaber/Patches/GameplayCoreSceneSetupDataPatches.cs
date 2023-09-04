using HarmonyLib;
using IPA.Utilities;
using System.Threading.Tasks;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(GameplayCoreSceneSetupData))]
    internal static class GameplayCoreSceneSetupDataPatches
    {
        public static IReadonlyBeatmapData OriginalBeatmap { get; private set; } = null;

        // Seems to be called whenever a level is started or restarted. It sets `_transformedBeatmapData`.
        [HarmonyPatch(nameof(GameplayCoreSceneSetupData.LoadTransformedBeatmapDataAsync))]
        [HarmonyPostfix]
        public static void LoadTransformedBeatmapDataAsyncPostfix(
            GameplayCoreSceneSetupData __instance,
            ref Task __result)
        {
            if (InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode)
            {
                __result = ReplaceBeatmapDataAsync(__instance, __result);
            }
        }

        // Stash the original beatmap in `OriginalBeatmap` and replace it with an empty beatmap.
        // The content for the empty beatmap will be populated later dynamically.
        public static async Task ReplaceBeatmapDataAsync(GameplayCoreSceneSetupData data, Task loadBeatmapTask)
        {
            await loadBeatmapTask;

            OriginalBeatmap = data.transformedBeatmapData;

            IReadonlyBeatmapData emptyBeatmap = new BeatmapData(data.transformedBeatmapData.numberOfLines);
            data.SetField("_transformedBeatmapData", emptyBeatmap);
        }
    }
}
