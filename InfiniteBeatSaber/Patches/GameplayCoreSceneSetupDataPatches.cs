using CustomJSONData.CustomBeatmap;
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

            data.SetField("_transformedBeatmapData", CreateEmptyBeatmap(OriginalBeatmap));
        }

        // When the CustomJSONData plugin is in use, we need to use its
        // `CustomBeatmapData` class instead of `BeatmapData`.
        // `CustomBeatmapData` knows how to handle CustomJSONData's
        // `BeatmapDataItem` subclasses.
        private static IReadonlyBeatmapData CreateEmptyBeatmap(IReadonlyBeatmapData beatmapData)
        {
            if (beatmapData is CustomBeatmapData customBeatmapData)
            {
                return new CustomBeatmapData(
                    customBeatmapData.numberOfLines,
                    customBeatmapData.version2_6_0AndEarlier,
                    customBeatmapData.customData,
                    customBeatmapData.beatmapCustomData,
                    customBeatmapData.levelCustomData);
            }
            else
            {
                return new BeatmapData(beatmapData.numberOfLines);
            }
        }
    }
}
