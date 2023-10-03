using HarmonyLib;
using TMPro;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(SongProgressUIController))]
    internal static class SongProgressUIControllerPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPostfix(
            SongProgressUIController __instance,
            TextMeshProUGUI ____durationMinutesText,
            TextMeshProUGUI ____durationSecondsText)
        {
            if (InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode)
            {
                // When playing a level with the Advanced HUD, the song's current timestamp
                // and duration are rendered like this: "1:20 / 3:30"
                // (assuming a song of duration 3:30 currently at timestamp 1:20)
                //
                // When in Infinite Beat Saber mode, the song's duration is huge which causes
                // it to overlap the song's current timestamp. This looks bad. An easy fix
                // is to hide the duration.

                // Hide the duration so the UI looks like this: "1:20 /  :  "
                ____durationMinutesText.text = "";
                ____durationSecondsText.text = "";

                // The UI still doesn't look great because of the floating symbols "/ :"
                // We can hide these by hiding the BG GameObject.

                // Hide the BG GameObject so the UI looks like this: "1 20"
                __instance.transform.Find("BG").gameObject.SetActive(false);

                // That's good enough.
            }
        }
    }
}
