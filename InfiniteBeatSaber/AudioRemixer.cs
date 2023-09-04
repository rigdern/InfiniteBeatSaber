using InfiniteBeatSaber.Extensions;
using System;
using System.Linq;
using UnityEngine;

namespace InfiniteBeatSaber
{
    internal class AudioRemixer : IDisposable
    {
        private readonly AudioClip _audioClip;
        private readonly float[] _channelSamples;
        private readonly AudioSource _audioSource;
        private readonly bool _audioSourceInitialLoopValue;
        // The next index of `_audioClip` to write to. Measured in *samples* (not *channel-samples*).
        private int _audioClipSampleIndex = 0;

        public AudioRemixer(AudioClip audioClip, AudioSource audioSource)
        {
            if (audioClip.ambisonic)
                throw new Exception("Not sure how to handle ambisonic audio");

            _audioClip = audioClip;
            _channelSamples = audioClip.GetAllData();
            _audioSource = audioSource;

            // Loop the `AudioSource` so we can get the effect of an infinite song by
            // using `AudioClip` as a ring buffer. Fortunately Beat Saber supports looping
            // songs. `AudioTimeSyncController` detects loops and incorporates this into
            // the time values that it exposes to the rest of the game.
            _audioSourceInitialLoopValue = _audioSource.loop;
            _audioSource.loop = true;
        }

        public void Dispose()
        {
            // Restore `_audioClip` to its original value.
            _audioClip.SetData(_channelSamples, 0);

            // `_audioSource` was non-null during initialization but it can appear
            // null now because Unity's C# Objects make themselves look null when
            // their underlying native objects are deallocated.
            if (_audioSource != null)
            {
                // Restore `_audioSource.loop` to its original value.
                _audioSource.loop = _audioSourceInitialLoopValue;
            }
        }

        // Adds `remix` to the `AudioClip`.
        public void AddRemix(Remix remix)
        {
            var remixedChannelSamples = remix.Slices
                .SelectMany(slice => Slice(slice.Start, slice.Duration))
                .ToArray();
            LogAndResetCounts();

            var remixSamplesCount = remixedChannelSamples.Length / ChannelsPerSample;
            
            //var frequency = SamplesPerSecond;
            //Plugin.Log.Info("AudioRemixer.AddRemix:");
            //Plugin.Log.Info("  numberOfSamples: " + remixSamplesCount);
            //Plugin.Log.Info("  NumChannels: " + ChannelsPerSample);
            //Plugin.Log.Info("  frequency: " + frequency);

            // `SetData` wraps back to the beginning of `_audioClip` if the parameters would have
            // caused a write past the end of `_audioClip`.
            _audioClip.SetData(remixedChannelSamples, _audioClipSampleIndex);
            _audioClipSampleIndex = (_audioClipSampleIndex + remixSamplesCount) % _audioClip.samples;
        }

        // Terminology:
        //   - Each "channel-sample" contains an amplitude for 1 channel.
        //   - Each "sample" contains 1 amplitude for each channel.
        //
        // If an audio file contains X "samples", then it contains X * channelCount "channel-samples".
        //
        // Unity's AudioClip API (e.g. GetData, SetData) represents the audio as a `float[]` where each
        // element is a "channel-sample" in the range of -1 to 1.

        private int ChannelsPerSample => _audioClip.channels;
        private int SamplesPerSecond => _audioClip.frequency;

        private static T[] SliceArray<T>(T[] array, int startIndex, int length)
        {
            T[] slice = new T[length];
            Array.Copy(array, startIndex, slice, 0, length);
            return slice;
        }

        private static bool IsInt(double x)
        {
            var diff = Math.Abs(x - Math.Round(x));
            return diff <= 0.01;
        }

        private double SecondsToSamples(double seconds)
        {
            return seconds * SamplesPerSecond;
        }

        private int SamplesToChannelSamples(int samples)
        {
            return samples * ChannelsPerSample;
        }

        private static uint intCount = 0;
        private static uint floatCount = 0;
        private static void LogAndResetCounts()
        {
            //Plugin.Log.Info("intCount: " + intCount + ", floatCount: " + floatCount);
            intCount = 0;
            floatCount = 0;
        }

        private float[] Slice(double startTime, double duration)
        {
            var endTime = startTime + duration;
            var startSample = SecondsToSamples(startTime);
            var endSample = SecondsToSamples(endTime);
            if (!IsInt(startSample) || !IsInt(endSample))
            {
                //Plugin.Log.Info("startSample or endSample is not an int:");
                //Plugin.Log.Info($"  startSample: {startSample}");
                //Plugin.Log.Info($"  endSample: {endSample}");
                //Plugin.Log.Info($"  startTime: {startTime}");
                //Plugin.Log.Info($"  endTime: {endTime}");
                //Plugin.Log.Info($"  duration: {duration}");
                //Plugin.Log.Info($"  SamplesPerSecond: {SamplesPerSecond}");
            }

            if (!IsInt(startSample))
            {
                ++floatCount;
            }
            else
            {
                ++intCount;
                startSample = Math.Round(startSample);
            }

            if (!IsInt(endSample))
            {
                ++floatCount;
            }
            else
            {
                ++intCount;
                endSample = Math.Round(endSample);
            }

            var startIndex = SamplesToChannelSamples((int)Math.Ceiling(startSample));
            var endIndex = SamplesToChannelSamples((int)Math.Floor(endSample));
            var length = endIndex - startIndex;
            if (length % 2 != 0)
                throw new Exception("Odd # channel samples: " + length + ", " + startIndex + " -> " + endIndex);

            return SliceArray(_channelSamples, startIndex, length);
        }
    }
}
