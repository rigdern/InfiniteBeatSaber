using HarmonyLib;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace InfiniteBeatSaber.Patches
{
    [HarmonyPatch(typeof(MediaAsyncLoader))]
    internal static class MediaAsyncLoaderPatches
    {
        [HarmonyPatch(nameof(MediaAsyncLoader.LoadAudioClipFromFilePathAsync))]
        [HarmonyPrefix]
        public static bool LoadAudioClipFromFilePathAsyncPrefix(ref Task<AudioClip> __result, string filePath)
        {
            __result = LoadAudioClipFromFilePathAsync(filePath);
            return false; // Prevent the orginial implementation from running.
        }

        // Derived from `MediaAsyncLoader.LoadAudioClipFromFilePathAsync`. The original
        // implementation set `DownloadHandlerAudioClip.streamAudio` to true which produces a
        // streaming clip with these limitations:
        //   1. `AudioSource.PlayScheduled` causes the AudioClip to stop playing on any other
        //      AudioSource.
        //   2. `AudioClip.GetData` isn't supported.
        //
        // (1) is a blocker for our technique for remixing the clip. To get an AudioClip without
        // this constraint, we load the AudioClip without setting
        // `DownloadHandlerAudioClip.streamAudio` to true.
        private static async Task<AudioClip> LoadAudioClipFromFilePathAsync(string filePath)
        {
            AudioType audioTypeFromPath = AudioTypeHelper.GetAudioTypeFromPath(filePath);
            var www = UnityWebRequestMultimedia.GetAudioClip(FileHelpers.GetEscapedURLForFilePath(filePath), audioTypeFromPath);
            //((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
            AsyncOperation request = www.SendWebRequest();
            while (!request.isDone)
            {
                await Task.Delay(100);
            }
            if (IsError(www))
            {
                return null;
            }
            return DownloadHandlerAudioClip.GetContent(www);
        }

        private static bool IsError(UnityWebRequest request)
        {
            // In some Unity version after the one used by Beat Saber 1.29.1, `isNetworkError` and
            // `isHttpError` were marked as obsolete in favor of the new `result` property.

#if BEAT_SABER_1_29_1
            // These properties are obsolete in newer versions of Unity.
            return request.isNetworkError || request.isHttpError;
#else
            // The replacement property in newer versions of Unity.
            return request.result != UnityWebRequest.Result.Success;
#endif
        }
    }
}
