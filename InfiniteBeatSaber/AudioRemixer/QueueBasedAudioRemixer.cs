/* QueueBasedAudioRemixer.cs
 *
 * This `IAudioRemixer` works with both custom songs and builtin songs.
 *
 * Remixes audio by queueing `AudioClips` at the appropriate time via `AudioSource.PlayScheduled`.
 *
 * All time in this class is based on `AudioSettings.dspTime`. That's because we schedule audio with
 * `AudioSource.PlayScheduled` and its parameter is in `dspTime`. The `AudioSource.time` of all
 * `AudioSources` should change at a rate of `AudioSettings.dspTime * AudioSource.pitch`.
 *
 * Beat Saber's primary clock seems to be `AudioTimeSyncController.songTime`. For example,
 * `songTime` is used to decide the positions of all of the note blocks, bombs, obstacles, and all
 * of the other parts of the level that approach the player at the pace of the music. The
 * implementation of `AudioTimeSyncController.songTime` appears to view its `AudioSource.time` as
 * the primary clock.
 *
 * Because `AudioSettings.dspTime * AudioSource.pitch` changes at the same rate as Beat Saber's
 * primary clock (`AudioSource.time`), it seems safe enough for this class to use `dspTime` as its
 * clock.
 */

using InfiniteBeatSaber.Extensions;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using static InfiniteBeatSaber.FloatComparison;
using System.Collections.Generic;
using InfiniteBeatSaber.Patches;

namespace InfiniteBeatSaber
{
    internal class QueueBasedAudioRemixer : IAudioRemixer
    {
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly AudioTimeSyncControllerPatches.AudioTimeSyncControllerAdditions _audioTimeSyncControllerAdditions;
        private readonly AudioClip _audioClip;
        private readonly AudioSource _audioSource;
        private readonly bool _audioSourceInitialLoopValue;
        private readonly float _audioSourceInitialVolume;

        private readonly AsyncQueue<Slice> _remixSliceQueue = new AsyncQueue<Slice>();
        private readonly GameObject _gameObject = new GameObject("AudioRemixer");
        private readonly Queue<ScheduledAudioSource> _audioSourcePoolQueue = new Queue<ScheduledAudioSource>();

        // The next `dspTime` at which we should schedule audio.
        private double _nextDspTime;

        private CancellationTokenSource _loopCts;

        public QueueBasedAudioRemixer(
            AudioTimeSyncController audioTimeSyncController,
            AudioClip audioClip,
            AudioSource audioSource)
        {
            if (audioClip.ambisonic)
                throw new Exception("Not sure how to handle ambisonic audio");

            _audioTimeSyncController = audioTimeSyncController;
            _audioTimeSyncControllerAdditions = AudioTimeSyncControllerPatches.GetAdditions(_audioTimeSyncController);
            _audioClip = audioClip;
            _audioSource = audioSource;

            // Loop the `AudioSource` so Beat Saber doesn't end the level when the `AudioSource`
            // reaches its end. Fortunately Beat Saber supports looping songs.
            // `AudioTimeSyncController` detects loops and incorporates this into the time values
            // that it exposes to the rest of the game.
            _audioSourceInitialLoopValue = _audioSource.loop;
            _audioSource.loop = true;

            // Mute Beat Saber's music. We'll be playing the remixed song using our own
            // `AudioSources`.
            _audioSourceInitialVolume = _audioSource.volume;
            _audioSource.volume = 0;

            _audioTimeSyncControllerAdditions.DidInitialize += OnAudioTimeSyncControllerInitialized;
            _audioTimeSyncController.stateChangedEvent += OnAudioTimeSyncControllerStateChanged;

            if (_audioTimeSyncControllerAdditions.IsInitialized)
                throw new Exception("AudioTimeSyncController initialized before we had a chance to initialize");

            if (_audioTimeSyncController.state != AudioTimeSyncController.State.Stopped)
                throw new Exception("The song started before we had a chance to initialize it");
        }

        public void Dispose()
        {
            StopLoop();

            // `_audioSource` was non-null during initialization but it can appear
            // null now because Unity's C# Objects make themselves look null when
            // their underlying native objects are deallocated.
            if (_audioSource != null)
            {
                // Restore `_audioSource` to its original state.
                _audioSource.loop = _audioSourceInitialLoopValue;
                _audioSource.volume = _audioSourceInitialVolume;
            }

            _audioTimeSyncControllerAdditions.DidInitialize -= OnAudioTimeSyncControllerInitialized;
            _audioTimeSyncController.stateChangedEvent -= OnAudioTimeSyncControllerStateChanged;
        }

        public void AddRemix(Remix remix)
        {
            _remixSliceQueue.EnqueueAll(remix.Slices);
        }

        private void OnAudioTimeSyncControllerInitialized()
        {
            // `_audioTimeSyncController` isn't guaranteed to be initialized until this event fires.
            // In particular, we need `dspTimeOffset` to be initialized.
            // `_audioTimeSyncController.dspTimeOffset` is the `dspTime` at which the song started.
            _nextDspTime = _audioTimeSyncController.dspTimeOffset;
            if (AreFloatsEqual(_nextDspTime, 0))
                throw new Exception("_nextDspTime should not be 0. It's likely that _audioTimeSyncController hasn't sufficiently initialized yet.");

            if (_audioTimeSyncController.state == AudioTimeSyncController.State.Playing)
            {
                StartLoop().LogOnFailure();
            }
        }

        private void OnAudioTimeSyncControllerStateChanged()
        {
            if (!_audioTimeSyncControllerAdditions.IsInitialized)
            {
                // `_audioTimeSyncController` hasn't sufficiently initialized.
                return;
            }

            switch (_audioTimeSyncController.state)
            {
                case AudioTimeSyncController.State.Stopped:
                case AudioTimeSyncController.State.Paused:
                    StopLoop();
                    break;
                case AudioTimeSyncController.State.Playing:
                    StartLoop().LogOnFailure();
                    break;
            }
        }

        private async Task StartLoop()
        {
            if (_loopCts != null)
            {
                // A loop is already running.
                return;
            }

            _loopCts = new CancellationTokenSource();
            var cancellationToken = _loopCts.Token;

            //Plugin.Log.Info("InfiniteBeatSaber.QueueBasedAudioRemixer: Started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _remixSliceQueue.HasItemsAsync().Cancelable(cancellationToken);

                    {
                        var nowDspTime = AudioSettings.dspTime;
                        var sleepUntilDspTime = _nextDspTime;
                        // Wake a little early so we don't miss the slice.
                        var sleepSeconds = sleepUntilDspTime - nowDspTime - 1;

                        if (sleepSeconds > 0)
                        {
                            //Plugin.Log.Info($"InfiniteBeatSaber.QueueBasedAudioRemixer: Sleep for {sleepSeconds}, nowDspTime: {nowDspTime}, nextDspTime: {_nextDspTime}");
                            await Task.Delay(TimeSpan.FromSeconds(sleepSeconds), cancellationToken);
                        }
                    }

                    {
                        var nowDspTime = AudioSettings.dspTime;
                        while (_remixSliceQueue.HasItems() && IsFloatLessOrEqual(_nextDspTime, nowDspTime + 1))
                        {
                            var slice = _remixSliceQueue.Dequeue();

                            //Plugin.Log.Info($"InfiniteBeatSaber.QueueBasedAudioRemixer: Schedule slice at dsp time: {_nextDspTime}. nowDspTime: {nowDspTime}, sliceDuration: {slice.Duration}");
                            ScheduleAudioSlice(slice.Start, slice.Duration, _nextDspTime);
                            _nextDspTime += slice.Duration / _audioTimeSyncController.timeScale;
                        }
                    }
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // No-op
            }

            //Plugin.Log.Info("InfiniteBeatSaber.QueueBasedAudioRemixer: Exited");
        }

        private void StopLoop()
        {
            var cts = _loopCts;
            if (cts != null)
            {
                // A loop is running. Stop it.
                _loopCts = null;
                cts.Cancel();
            }
        }

        private void ScheduleAudioSlice(double sliceStart, double sliceDuration, double scheduledDspTime)
        {
            // Here's an analogy to understand the parameters. At 5:00 PM (scheduledDspTime)
            // you're going to watch 15 minutes (sliceDuration) of a movie resuming it from
            // the 1 hour mark (sliceStart).

            var endDspTime = scheduledDspTime + sliceDuration / _audioTimeSyncController.timeScale;

            var audioSource = GetOrCreateAudioSource();

            audioSource.time = (float)sliceStart; // Seems to work just as well as setting `AudioSource.timeSamples`
            audioSource.PlayScheduled(scheduledDspTime);
            audioSource.SetScheduledEndTime(endDspTime);

            _audioSourcePoolQueue.Enqueue(new ScheduledAudioSource(audioSource, endDspTime));
        }

        private AudioSource GetOrCreateAudioSource()
        {
            // Assumes that entries in `_audioSourcePoolQueue` are sorted by `_endDspTime` in
            // ascending order.
            if (_audioSourcePoolQueue.Count > 0 && _audioSourcePoolQueue.Peek().IsCompleted)
            {
                return _audioSourcePoolQueue.Dequeue().AudioSource;
            }

            var audioSource = _gameObject.AddComponent<AudioSource>();
            audioSource.clip = _audioClip;
            // Intention: play the song at the appropriate speed. Undesirable side effect: changes
            // the song's pitch. Mitigation: the pitch is corrected by connecting the `AudioSource`
            // to the appropriate `AudioMixerGroup`.
            audioSource.pitch = _audioTimeSyncController.timeScale;

            // Copy field values from the `AudioSource` Beat Saber uses for its music. I manually
            // identified these fields as the ones where Beat Saber uses non-default values. I'm not
            // sure of the practical impact of most of these. Here are some highlights:
            //   - `outputAudioMixerGroup`: At the very least, this is important for pitch
            //     correction when playing the song on a speed other than 1.
            //   - An `AudioSource's` position only affects the sound when it's a 3D audio source.
            //     An audio source is 3D iff its `spatialBlend` is non-zero. This `AudioSource` is
            //     2D because it uses the default value of `spatialBlend` which is 0. So we don't
            //     have to worry about the `AudioSource's` position in the world.
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            audioSource.dopplerLevel = 0;
            audioSource.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
            audioSource.priority = 0;
            audioSource.reverbZoneMix = 0;

            return audioSource;
        }

        private class ScheduledAudioSource
        {
            public readonly AudioSource AudioSource;
            private readonly double _endDspTime;

            public ScheduledAudioSource(AudioSource audioSource, double endDspTime)
            {
                AudioSource = audioSource;
                _endDspTime = endDspTime;
            }

            public bool IsCompleted => IsFloatLessOrEqual(_endDspTime + 1, AudioSettings.dspTime);
        }
    }
}
