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
            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            Plugin.Log.Info("InfiniteBeatSaberMode.DebugTools.DebugVisualizer: Initialize");

            _unregisterWebSocket = _webSocketServer.Register(OnWebSocketMessage);

            _audioTimeSyncController.stateChangedEvent += OnAudioTimeSyncControllerStateChanged;
        }

        // Lifecycle hook for destruction called by the dependency injection framework.
        public void Dispose()
        {
            if (!InfiniteBeatSaberMenuUI.IsInfiniteBeatSaberMode) return;

            _loopCts?.Cancel();
            _webSocketServer
                .SendMessage("endSong", null)
                .LogOnFailure();
            _unregisterWebSocket();
        }

        public void InitializeData(string spotifyAnalysisText)
        {
            Plugin.Log.Info("InfiniteBeatSaberMode.DebugTools.DebugVisualizer: InitializeData: " + spotifyAnalysisText.Length);

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

            Plugin.Log.Info("InfiniteBeatSaberMode.DebugTools.DebugVisualizer: Started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Cancelable(_beats.HasItemsAsync(), cancellationToken);

                    {
                        var beat = _beats.Peek();
                        var songTime = _audioTimeSyncController.songTime;
                        var sleepUntilSongTime = beat.Clock;
                        // Wake a little early so we don't miss the beat.
                        var sleepSeconds = (sleepUntilSongTime - songTime) / _audioTimeSyncController.timeScale - 0.01;

                        if (sleepSeconds > 0)
                        {
                            Plugin.Log.Info("InfiniteBeatSaber.DebugTools.DebugVisualizer: Sleep for " + sleepSeconds + ", " + beat.BeatIndex + " " + beat.Clock + ", " + songTime);
                            await Task.Delay(TimeSpan.FromSeconds(sleepSeconds), cancellationToken);
                        }
                    }

                    {
                        var songTime = _audioTimeSyncController.songTime;
                        Beat currentBeat = null;
                        while (_beats.HasItems() && IsFloatLessOrEqual(_beats.Peek().Clock, songTime + 0.01))
                        {
                            currentBeat = _beats.Dequeue();
                            Plugin.Log.Info("InfiniteBeatSaber.DebugTools.DebugVisualizer: Dequeue " + currentBeat.BeatIndex + " " + currentBeat.Clock + ", " + songTime);
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

            Plugin.Log.Info("InfiniteBeatSaber.DebugTools.DebugVisualizer: Exited");
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

        private static async Task Cancelable(Task task, CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);
            var result = await Task.WhenAny(task, cancellationTask);
            if (result == task)
            {
                await task;
            }
            else
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        // Like `Queue` but provides `HasItemsAsync` which enables you to be
        // notified when an empty `Queue` gets an item added to it.
        private class AsyncQueue<T>
        {
            private readonly Queue<T> _queue = new Queue<T>();
            private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

            public void Enqueue(T item)
            {
                _queue.Enqueue(item);
                if (_tcs != null)
                {
                    var tcs = _tcs;
                    _tcs = null;
                    tcs.SetResult(null);
                }
            }

            public T Peek()=> _queue.Peek();
            public T Dequeue() => _queue.Dequeue();
            public bool HasItems() => _queue.Count > 0;

            public Task HasItemsAsync()
            {
                if (_queue.Count == 0)
                {
                    if (_tcs == null)
                    {
                        _tcs = new TaskCompletionSource<object>();
                    }
                    return _tcs.Task;
                }
                else
                {
                    return Task.CompletedTask;
                }
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
