using InfiniteBeatSaber.Patches;
using InfiniteJukeboxAlgorithm;
using IPA.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using InfiniteBeatSaber.Extensions;

namespace InfiniteBeatSaber
{
    internal class InfiniteBeatSaberMode : IInitializable, IDisposable
    {
#pragma warning disable 0649 // Suppress "Field X is never assigned to" b/c [Inject] assigns.
        [Inject] private readonly AudioTimeSyncController _audioTimeSyncController;
        [Inject] private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
#if DEBUG
        [Inject] private readonly DebugTools.RemixVisualizer _remixVisualizer;
#endif
#pragma warning restore 0649

        private CancellationTokenSource _generateRemixLoopCts;

        private InfiniteRemix _infiniteRemix;
        private IAudioRemixer _audioRemixer;
        private BeatmapRemixer _beatmapRemixer;

        public void Initialize()
        {
            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            var level = _gameplayCoreSceneSetupData.previewBeatmapLevel;
            // TODO: Stop blocking the thread by reading a file.
            var spotifyAnalysis = RemixableSongs.ReadSpotifyAnalysis(level);

            var random = new SystemRandom();

            var initData = _audioTimeSyncController.GetField<AudioTimeSyncController.InitData, AudioTimeSyncController>("_initData");
            var audioClip = Util.AssertNotNull(initData.audioClip, "audioClip");
            var audioSource = Util.AssertNotNull(_audioTimeSyncController.GetField<AudioSource, AudioTimeSyncController>("_audioSource"), "_audioSource");

            var originalBeatmap = Util.AssertNotNull(GameplayCoreSceneSetupDataPatches.OriginalBeatmap, "originalBeatmap");

            var readonlyBeatmap = Util.AssertNotNull(_gameplayCoreSceneSetupData.transformedBeatmapData, "readonlyBeatmap");
            var beatmap = Util.AssertNotNull(readonlyBeatmap as BeatmapData, "beatmap");

            _infiniteRemix = new InfiniteRemix(spotifyAnalysis, level.beatsPerMinute, random);
            //_audioRemixer = new RingBufferBasedAudioRemixer(audioClip, audioSource);
            _audioRemixer = new QueueBasedAudioRemixer(_audioTimeSyncController, audioClip, audioSource);
            _beatmapRemixer = new BeatmapRemixer(originalBeatmap, beatmap);
#if DEBUG
            _remixVisualizer.InitializeData(spotifyAnalysis.SerializeToJson());
#endif

            GenerateNextPartOfRemix(60);

            if (_audioTimeSyncController.state != AudioTimeSyncController.State.Stopped)
                throw new Exception("The song started before we had a chance to initialize it");

            _audioTimeSyncController.stateChangedEvent += OnAudioTimeSyncControllerStateChanged;

            Plugin.Log.Info(
                "InfiniteBeatSaberMode.Initialize" +
                $". Seed: {random.Seed}" +
                $". Level: {level.songName} by {level.songAuthorName}, {level.levelAuthorName} (ID: {level.levelID})" +
                "");
        }

        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            _generateRemixLoopCts?.Cancel();
            _audioRemixer.Dispose();
        }

        private void OnAudioTimeSyncControllerStateChanged()
        {
            switch (_audioTimeSyncController.state)
            {
                case AudioTimeSyncController.State.Stopped:
                case AudioTimeSyncController.State.Paused:
                    StopGenerateRemixLoop();
                    break;
                case AudioTimeSyncController.State.Playing:
                    StartGenerateRemixLoop().LogOnFailure();
                    break;
            }
        }

        // Generates at least the next `minSeconds` of the remix.
        private Remix GenerateNextPartOfRemix(int minSeconds)
        {
            var startDuration = _infiniteRemix.Duration;

            var remix = _infiniteRemix.GetNext(minSeconds);
            _audioRemixer.AddRemix(remix);
            _beatmapRemixer.AddRemix(remix);
#if DEBUG
            _remixVisualizer.AddRemix(remix);
#endif

            var durationDelta = _infiniteRemix.Duration - startDuration;

            //Plugin.Log.Info("InfiniteBeatSaberMode.GenerateNextPartOfRemix: " + minSeconds + ". " + startDuration + " -> " + _infiniteRemix.Duration + " (" + durationDelta + ")");
            return remix;
        }

        private async Task StartGenerateRemixLoop()
        {
            if (_generateRemixLoopCts != null)
            {
                // A loop is already running.
                return;
            }

            _generateRemixLoopCts = new CancellationTokenSource();
            var cancellationToken = _generateRemixLoopCts.Token;

            //Plugin.Log.Info("InfiniteBeatSaberMode.StartGenerateRemixLoop: Started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sleepUntilSongTime = _infiniteRemix.Duration - 25;
                    var sleepSeconds = (sleepUntilSongTime - _audioTimeSyncController.songTime) / _audioTimeSyncController.timeScale;

                    if (sleepSeconds > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(sleepSeconds), cancellationToken);
                    }

                    GenerateNextPartOfRemix(30);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // No-op
            }

            //Plugin.Log.Info("InfiniteBeatSaberMode.StartGenerateRemixLoop: Exited");
        }

        private void StopGenerateRemixLoop()
        {
            var cts = _generateRemixLoopCts;
            if (cts != null)
            {
                // A loop is running. Stop it.
                _generateRemixLoopCts = null;
                cts.Cancel();
            }
        }
    }
}
