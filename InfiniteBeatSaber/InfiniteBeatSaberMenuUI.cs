using InfiniteBeatSaber.Patches;
using IPA.Utilities;
using System;
using TMPro;
using UnityEngine.UI;
using Zenject;

namespace InfiniteBeatSaber
{
    internal class InfiniteBeatSaberMenuUI : IInitializable, IDisposable
    {
        public static bool IsInfiniteBeatSaberMode { get; private set; } = false;

        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator;

        private Button _startInfiniteBeatSaberButton;

        public InfiniteBeatSaberMenuUI(
            StandardLevelDetailViewController standardLevelDetailViewController,
            SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
        }

        public void Initialize()
        {
            _standardLevelDetailViewController.didPressActionButtonEvent += OnDidPressPlayButton;
            _standardLevelDetailViewController.didPressPracticeButtonEvent += OnDidPressPracticeButton;
            _startInfiniteBeatSaberButton = AddStartInfiniteBeatSaberButton("∞");
            _startInfiniteBeatSaberButton.onClick.AddListener(OnDidPressStartInfiniteBeatSaberButton);
            _startInfiniteBeatSaberButton.gameObject.SetActive(false);

            StandardLevelDetailViewPatches.DidChangeDifficultyBeatmap += OnDidChangeDifficultyBeatmap;
        }

        public void Dispose()
        {
            _standardLevelDetailViewController.didPressActionButtonEvent -= OnDidPressPlayButton;
            _standardLevelDetailViewController.didPressPracticeButtonEvent -= OnDidPressPracticeButton;
            _startInfiniteBeatSaberButton.onClick.RemoveListener(OnDidPressStartInfiniteBeatSaberButton);
            UnityEngine.Object.Destroy(_startInfiniteBeatSaberButton);

            StandardLevelDetailViewPatches.DidChangeDifficultyBeatmap -= OnDidChangeDifficultyBeatmap;
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

        private Button AddStartInfiniteBeatSaberButton(string label)
        {
            void ShrinkActionButtonMinWidth(Button actionButton, float minWidth)
            {
                var content = actionButton.transform.Find("Content");
                var layoutElement = content.GetComponent<LayoutElement>();

                // We're just trying to make the buttons thinner so we can fit more of them.
                // If somebody made them even thinner, that's fine. Let's not override them.
                if (layoutElement.minWidth > minWidth)
                {
                    layoutElement.minWidth = minWidth;
                }
            }

            var detailView = _standardLevelDetailViewController.GetField<StandardLevelDetailView, StandardLevelDetailViewController>("_standardLevelDetailView");

            var startInfiniteBeatSaberButton = UnityEngine.Object.Instantiate(detailView.practiceButton, detailView.practiceButton.transform.parent);
            startInfiniteBeatSaberButton.name = "StartInfiniteBeatSaberButton";
            SetButtonText(startInfiniteBeatSaberButton, label);
            ShrinkActionButtonMinWidth(startInfiniteBeatSaberButton, 10);
            startInfiniteBeatSaberButton.onClick.RemoveAllListeners();

            // Make the "play" button thinner. This enables us to fit several buttons in the same row:
            // - Beat Saber "practice" button
            // - Beat Saber "play" button
            // - Infinite Beat Saber "start" button
            // - Better Song List "delete song" button
            ShrinkActionButtonMinWidth(detailView.actionButton, 14);

            return startInfiniteBeatSaberButton;
        }

        private void StartLevelInPracticeMode()
        {
            var practiceViewController = _soloFreePlayFlowCoordinator.GetField<PracticeViewController, SinglePlayerLevelSelectionFlowCoordinator>("_practiceViewController");
            practiceViewController.practiceSettings?.ResetToDefault();
            _soloFreePlayFlowCoordinator.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>("HandlePracticeViewControllerDidPressPlayButton");
        }

        // `SetButtonText` is derived from BeatSaberMarkupLanguage (https://github.com/monkeymanboy/BeatSaberMarkupLanguage).
        //
        // MIT License
        //
        // Copyright (c) 2019 David L
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.
        public static void SetButtonText(Button button, string text)
        {
            Polyglot.LocalizedTextMeshProUGUI localizer = button.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>(true);
            if (localizer != null)
            {
                UnityEngine.Object.Destroy(localizer);
            }

            TextMeshProUGUI tmpUgui = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpUgui != null)
            {
                tmpUgui.SetText(text);
            }
        }

        #endregion
    }
}
