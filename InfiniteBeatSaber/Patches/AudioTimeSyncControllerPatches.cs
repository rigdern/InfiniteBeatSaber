using HarmonyLib;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(AudioTimeSyncController))]
    internal static class AudioTimeSyncControllerPatches
    {
        [HarmonyPatch(nameof(AudioTimeSyncController.songLength), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool SongLengthGetterPrefix(ref float __result)
        {
            if (InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode)
            {
                // Return a large number of seconds so the song is effectively of
                // infinite length. 12,000+ years should be enough.
                __result = 4e+11f;

                return false; // Prevent the orginial implementation from running.
            }
            else
            {
                return true; // Run the original implementation.
            }
        }
    }
}
