using InfiniteJukeboxAlgorithm.AugmentedTypes;
using Newtonsoft.Json;
using Polyglot;
using System.Collections.Generic;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber
{
    internal static class RemixableSongs
    {
        private static readonly Dictionary<string, SpotifyAnalysisInfo> _levelIdToSpotifyAnalysisInfo = new Dictionary<string, SpotifyAnalysisInfo>
        {
            // Here's the meaning of the dictionary's keys and values:
            // - Key: The Beat Saber level ID as given by `IPreviewBeatmapLevel.levelID`.
            // - Value: See the field comments on the `SpotifyAnalysisInfo` class.

            // Gangnam Style by PSY. Mapped by GreatYazer. https://bsaber.com/songs/141/
            { "custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF",
                new SpotifyAnalysisInfo("custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF.json") },
            
            // Beat Saber by Jaroslav Beck.
            { "custom_level_B68BF61AC6BE0E128BE32A85810D42E7C53F4756", new SpotifyAnalysisInfo("BeatSaber.json") },

            { "BeatSaber", new SpotifyAnalysisInfo("BeatSaber.json") },
        };

        public static (bool, string) IsDifficultyBeatmapRemixable(IDifficultyBeatmap difficultyBeatmap)
        {
            var levelId = difficultyBeatmap.level.levelID;
            var characteristicObj = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic;
            var characteristic = characteristicObj.serializedName;

            if (!_levelIdToSpotifyAnalysisInfo.ContainsKey(levelId))
            {
                return (false, "Song currently not supported by Infinite Beat Saber");
            }
            else if (
                characteristic != "Standard" &&
                characteristic != "OneSaber" &&
                characteristic != "NoArrows")
            {
                var localizedCharacteristic = Localization.Get(characteristicObj.characteristicNameLocalizationKey);
                return (false, $"{localizedCharacteristic} mode currently not supported by Infinite Beat Saber");
            }
            else
            {
                return (true, null);
            }
        }

        public static SpotifyAnalysis ReadSpotifyAnalysis(IPreviewBeatmapLevel level)
        {
            var spotifyAnalysisInfo = _levelIdToSpotifyAnalysisInfo[level.levelID];
            var spotifyAnalysis = JsonConvert.DeserializeObject<SpotifyAnalysis>(
                Util.ReadEntryFromEmbeddedZipResource("SpotifyAnalyses.zip", spotifyAnalysisInfo.ResourceName));
            ShiftSpotifyAnalysis(spotifyAnalysis, spotifyAnalysisInfo.ShiftTimestampsSeconds);
            return spotifyAnalysis;
        }

        private static void ShiftSpotifyAnalysis(SpotifyAnalysis spotifyAnalysis, double shiftTimestampsSeconds)
        {
            void ShiftQuantums(IEnumerable<Quantum> quantums)
            {
                foreach (var quantum in quantums)
                {
                    quantum.Start += shiftTimestampsSeconds;
                }
            }

            if (!AreFloatsEqual(shiftTimestampsSeconds, 0))
            {
                ShiftQuantums(spotifyAnalysis.Sections);
                ShiftQuantums(spotifyAnalysis.Bars);
                ShiftQuantums(spotifyAnalysis.Beats);
                ShiftQuantums(spotifyAnalysis.Tatums);
                ShiftQuantums(spotifyAnalysis.Segments);
            }
        }

        private class SpotifyAnalysisInfo
        {
            // The name of the file in this project's "SpotifyAnalyses" folder. These
            // files are included in the DLL. They are configured as embedded resources by
            // right-clicking on them in Solution Explorer, choosing "Properties", and then
            // selecting "Embedded Resource" as the "Build Action".
            public readonly string ResourceName;

            // Number of seconds to add to all timestamps of the SpotifyAnalysis.
            // When the Beat Saber & Spotify versions of a song aren't aligned,
            // use this to shift the SpotifyAnalysis timestamps so they align
            // with Beat Saber's version of the song.
            public readonly double ShiftTimestampsSeconds;

            public SpotifyAnalysisInfo(string resourceName, double shiftTimestampsSeconds = 0)
            {
                ResourceName = resourceName;
                ShiftTimestampsSeconds = shiftTimestampsSeconds;
            }
        }
    }
}
