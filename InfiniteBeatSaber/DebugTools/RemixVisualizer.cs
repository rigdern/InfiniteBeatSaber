using InfiniteBeatSaber.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zenject;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber.DebugTools
{
    internal class RemixVisualizer : IInitializable, IDisposable
    {
#pragma warning disable 0649 // Suppress "Field X is never assigned to" b/c [Inject] assigns.
        [Inject] private readonly AudioTimeSyncController _audioTimeSyncController;
        [Inject] private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        [Inject] private readonly WebSocketServer _webSocketServer;
#pragma warning restore 0649

        private readonly AsyncQueue<Beat> _beats = new AsyncQueue<Beat>();

        private string _spotifyAnalysis;
        private Action _unregisterWebSocket;

        private CancellationTokenSource _loopCts;
            
        public void Initialize()
        {
            Util.AssertDebugBuild();

            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            _unregisterWebSocket = _webSocketServer.Register(OnWebSocketMessage);

            _audioTimeSyncController.stateChangedEvent += OnAudioTimeSyncControllerStateChanged;
        }

        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            StopLoop();
            _webSocketServer
                .SendMessage("endSong", null)
                .LogOnFailure();
            _unregisterWebSocket();
            _audioTimeSyncController.stateChangedEvent -= OnAudioTimeSyncControllerStateChanged;
        }

        public void InitializeData(string spotifyAnalysisText)
        {
            _spotifyAnalysis = spotifyAnalysisText;
            SendSongInfoIfAvailable();
        }

        public void AddRemix(Remix remix)
        {
            foreach (var beat in remix.Beats)
            {
                _beats.Enqueue(beat);
            }
        }

        private bool OnWebSocketMessage(string cmdName, string cmdArgs)
        {
            switch (cmdName)
            {
                case "getSongInfo":
                    SendSongInfoIfAvailable();
                    return true;
                default:
                    return false;
            }
        }

        private void OnAudioTimeSyncControllerStateChanged()
        {
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

        private void SendSongInfoIfAvailable()
        {
            if (_spotifyAnalysis != null)
            {
                var level = _gameplayCoreSceneSetupData.previewBeatmapLevel;
                _webSocketServer
                    .SendMessage("setSongInfo", new SetSongInfoArgs
                    {
                        levelId = level.levelID,
                        songName = level.songName,
                        songAuthorName = level.songAuthorName,
                        spotifyAnalysis = _spotifyAnalysis,
                    })
                    .LogOnFailure();
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

            //Plugin.Log.Info("InfiniteBeatSaberMode.DebugTools.RemixVisualizer: Started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _beats.HasItemsAsync().Cancelable(cancellationToken);

                    {
                        var beat = _beats.Peek();
                        var songTime = _audioTimeSyncController.songTime;
                        var sleepUntilSongTime = beat.Clock;
                        // Wake a little early so we don't miss the beat.
                        var sleepSeconds = (sleepUntilSongTime - songTime) / _audioTimeSyncController.timeScale - 0.01;

                        if (sleepSeconds > 0)
                        {
                            //Plugin.Log.Info("InfiniteBeatSaber.DebugTools.RemixVisualizer: Sleep for " + sleepSeconds + ", " + beat.BeatIndex + " " + beat.Clock + ", " + songTime);
                            await Task.Delay(TimeSpan.FromSeconds(sleepSeconds), cancellationToken);
                        }
                    }

                    {
                        var songTime = _audioTimeSyncController.songTime;
                        Beat currentBeat = null;
                        while (_beats.HasItems() && IsFloatLessOrEqual(_beats.Peek().Clock, songTime + 0.01))
                        {
                            currentBeat = _beats.Dequeue();
                            //var delta = songTime - currentBeat.Clock;
                            //Plugin.Log.Info("InfiniteBeatSaber.DebugTools.RemixVisualizer: Dequeue " + currentBeat.BeatIndex + " " + delta + ": " + currentBeat.Clock + ", " + songTime);
                        }

                        if (currentBeat != null)
                        {
                            _webSocketServer
                                .SendMessage("setBeatIndex", new SetBeatIndexArgs
                                {
                                    beatIndex = currentBeat.BeatIndex,
                                    clock = currentBeat.Clock,
                                })
                                .LogOnFailure();
                        }
                    }
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // No-op
            }

            //Plugin.Log.Info("InfiniteBeatSaber.DebugTools.RemixVisualizer: Exited");
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

        private class SetSongInfoArgs
        {
            [JsonProperty] public string levelId { get; set; }
            [JsonProperty] public string songName { get; set; }
            [JsonProperty] public string songAuthorName { get; set; }
            [JsonProperty] public string spotifyAnalysis { get; set; }
        }

        private class SetBeatIndexArgs
        {
            [JsonProperty] public int beatIndex { get; set; }
            [JsonProperty] public double clock { get; set; }
        }
    }
}
