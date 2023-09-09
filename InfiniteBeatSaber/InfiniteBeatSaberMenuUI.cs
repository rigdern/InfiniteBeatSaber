using BeatSaberMarkupLanguage;
using InfiniteBeatSaber.Patches;
using IPA.Utilities;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace InfiniteBeatSaber
{
    internal class InfiniteBeatSaberMenuUI : IInitializable
    {
        public static bool IsInfiniteBeatSaberMode { get; private set; } = false;

        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator;
        private readonly LevelCollectionViewController _levelCollectionViewController;

        private Button _startInfiniteBeatSaberButton;

        public InfiniteBeatSaberMenuUI(
            StandardLevelDetailViewController standardLevelDetailViewController,
            SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator,
            LevelCollectionViewController levelCollectionViewController)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
            _levelCollectionViewController = levelCollectionViewController;
        }

        public void Initialize()
        {
            // Once created, I expect this object to be around for the app's remaining
            // lifetime so this class doesn't have any cleanup code (i.e. no implementation
            // of IDisposable.Dispose which Zenject would call when destroying it).

            _standardLevelDetailViewController.didPressActionButtonEvent += OnDidPressPlayButton;
            _standardLevelDetailViewController.didPressPracticeButtonEvent += OnDidPressPracticeButton;
            _startInfiniteBeatSaberButton = AddStartInfiniteBeatSaberButton("∞", OnDidPressStartInfiniteBeatSaberButton);
            _startInfiniteBeatSaberButton.gameObject.SetActive(false);

            StandardLevelDetailViewPatches.DidChangeDifficultyBeatmap += OnDidChangeDifficultyBeatmap;
        }

        private void OnDidChangeDifficultyBeatmap(StandardLevelDetailView view, IDifficultyBeatmap difficultyBeatmap)
        {
            //var level = difficultyBeatmap.level;
            //Plugin.Log.Info($"InfiniteBeatSaberMenuUI.OnDidChangeDifficultyBeatmap: {level.songName} by {level.songAuthorName}, {level.levelAuthorName} (ID: {level.levelID})");

            var isRemixable = RemixableSongs.IsDifficultyBeatmapRemixable(difficultyBeatmap);

            _startInfiniteBeatSaberButton.gameObject.SetActive(isRemixable);
        }

        private void OnDidPressPracticeButton(StandardLevelDetailViewController controller, IBeatmapLevel level)
        {
            IsInfiniteBeatSaberMode = false;
        }

        private void OnDidPressPlayButton(StandardLevelDetailViewController controller)
        {
            IsInfiniteBeatSaberMode = false;
        }

        private void OnDidPressStartInfiniteBeatSaberButton()
        {
            IsInfiniteBeatSaberMode = true;

            // Use practice mode so the Infinite Beat Saber doesn't contribute to the
            // player's high scores.
            StartLevelInPracticeMode();
        }

        #region Code that relies directly on Beat Saber's implementation details

        private Button AddStartInfiniteBeatSaberButton(string label, UnityAction onClick)
        {
            var detailView = _standardLevelDetailViewController.GetField<StandardLevelDetailView, StandardLevelDetailViewController>("_standardLevelDetailView");
            var startInfiniteBeatSaberButton = UnityEngine.Object.Instantiate(detailView.practiceButton, detailView.practiceButton.transform.parent);
            BeatSaberUI.SetButtonText(startInfiniteBeatSaberButton, label);
            startInfiniteBeatSaberButton.onClick.RemoveAllListeners();
            startInfiniteBeatSaberButton.onClick.AddListener(onClick);
            return startInfiniteBeatSaberButton;
        }

        private void StartLevelInPracticeMode()
        {
            var practiceViewController = _soloFreePlayFlowCoordinator.GetField<PracticeViewController, SinglePlayerLevelSelectionFlowCoordinator>("_practiceViewController");
            practiceViewController.practiceSettings?.ResetToDefault();
            _soloFreePlayFlowCoordinator.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>("HandlePracticeViewControllerDidPressPlayButton");
        }

        #endregion
    }
}
