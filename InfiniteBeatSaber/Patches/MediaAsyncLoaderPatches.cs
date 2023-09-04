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
            Plugin.Log.Info("AHC LoadAudioClipFromFilePathAsyncPrefix loading: " + filePath);
            __result = LoadAudioClipFromFilePathAsync(filePath);
            return false; // Prevent the orginial implementation from running.
        }

        // Derived from `MediaAsyncLoader.LoadAudioClipFromFilePathAsync`. The
        // original implementation set `DownloadHandlerAudioClip.streamAudio` to
        // true but a limitation is that streaming clips do not support
        // `AudioClip.GetData`. `AudioClip.GetData` is important so that we can
        // remix the clip. To get an AudioClip without this constraint, we load
        // the AudioClip without setting `DownloadHandlerAudioClip.streamAudio` to
        // true.
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
            if (www.result != UnityWebRequest.Result.Success)
            {
                return null;
            }
            return DownloadHandlerAudioClip.GetContent(www);
        }
    }
}
