using System.Collections.Generic;
using static IPA.Logging.Logger;

namespace InfiniteBeatSaber
{
    internal static class RemixableSongs
    {
        private static readonly Dictionary<string, string> _levelIdToAnalysisPath = new Dictionary<string, string>
        {
            // Here's the meaning of the dictionary's keys and values:
            // - Key: The Beat Saber level ID as given by `IPreviewBeatmapLevel.levelID`.
            // - Value: The name of the file in this project's "SpotifyAnalyses" folder prepended with "SpotifyAnalysis."
            //   Note that these files are included in the DLL. They are configured as embedded resources by right-
            //   clicking on them in Solution Explorer, choosing "Properties", and then selecting "Embedded Resource"
            //   as the "Build Action".

            // Gangnam Style by PSY. Mapped by GreatYazer. https://bsaber.com/songs/141/
            { "custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF", "SpotifyAnalyses.custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF.json" },
            
            // Beat Saber by Jaroslav Beck.
            { "custom_level_B68BF61AC6BE0E128BE32A85810D42E7C53F4756", "SpotifyAnalyses.custom_level_B68BF61AC6BE0E128BE32A85810D42E7C53F4756.json" },
        };

        public static bool IsDifficultyBeatmapRemixable(IDifficultyBeatmap difficultyBeatmap)
        {
            var levelId = difficultyBeatmap.level.levelID;
            var characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            return _levelIdToAnalysisPath.ContainsKey(levelId) && characteristic == "Standard";
        }

        public static string ReadSpotifyAnalysis(IPreviewBeatmapLevel level)
        {
            var spotifyAnalysisPath = _levelIdToAnalysisPath[level.levelID];
            return Util.ReadEmbeddedResource(spotifyAnalysisPath);
        }
    }
}
