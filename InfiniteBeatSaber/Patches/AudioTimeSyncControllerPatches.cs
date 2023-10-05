using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(AudioTimeSyncController))]
    internal static class AudioTimeSyncControllerPatches
    {
        private static readonly ConditionalWeakTable<AudioTimeSyncController, AudioTimeSyncControllerAdditions> _instanceAdditions =
            new ConditionalWeakTable<AudioTimeSyncController, AudioTimeSyncControllerAdditions>();

        public static AudioTimeSyncControllerAdditions GetAdditions(AudioTimeSyncController audioTimeSyncController) =>
            _instanceAdditions.GetOrCreateValue(audioTimeSyncController);

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

        [HarmonyPatch(nameof(AudioTimeSyncController.StartSong))]
        [HarmonyPostfix]
        public static void StartSongPostfix(AudioTimeSyncController __instance)
        {
            if (InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode)
            {
                var additions = GetAdditions(__instance);
                additions._StartSongPostfix();
            }
        }

        /// <summary>
        /// Provides additional state and behavior on top of `AudioTimeSyncController`.
        /// </summary>
        internal class AudioTimeSyncControllerAdditions
        {
            /// <summary>
            /// `true` when all of `AudioTimeSyncController's` state is initialized. The original
            /// motivation was to determine whether `dspTimeOffset` has been initialized.
            /// </summary>
            public bool IsInitialized = false;

            /// <summary>
            /// Fires as soon as all of `AudioTimeSyncController`s` state has been initialized. The
            /// original motivation was to be notified when `dspTimeOffset` has been initialized.
            /// </summary>
            public event Action DidInitialize;

            internal void _StartSongPostfix()
            {
                if (!IsInitialized)
                {
                    IsInitialized = true;
                    DidInitialize?.Invoke();
                }
            }
        }
    }
}
