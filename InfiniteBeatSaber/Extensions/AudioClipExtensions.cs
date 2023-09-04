using System;
using UnityEngine;

namespace InfiniteBeatSaber.Extensions
{
    internal static class AudioClipExtensions
    {
        public static float[] GetAllData(this AudioClip audioClip)
        {
            var data = new float[audioClip.samples * audioClip.channels];
            if (!audioClip.GetData(data, 0))
            {
                throw new Exception("AudioClipExtensions.GetAllData: GetData failed");
            }
            return data;
        }
    }
}
