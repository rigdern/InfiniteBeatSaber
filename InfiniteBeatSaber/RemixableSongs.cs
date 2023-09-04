using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteBeatSaber
{
    internal static class RemixableSongs
    {
        private static readonly Dictionary<string, string> _levelIdToAnalysisPath = new Dictionary<string, string>
        {
            // Gangnam Style by PSY. Mapped by GreatYazer.
            { "custom_level_42365031DFBEFB4286E413A5BB9B3A64031FC6C3", "SpotifyAnalyses.custom_level_42365031DFBEFB4286E413A5BB9B3A64031FC6C3.json" },
        };

        public static bool IsDifficultyBeatmapRemixable(IDifficultyBeatmap difficultyBeatmap)
        {
            var levelId = difficultyBeatmap.level.levelID;
            var characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            return _levelIdToAnalysisPath.ContainsKey(levelId) && characteristic == "Standard";
        }

        public static string SpotifyAnalysisPath(string levelId)
        {
            return _levelIdToAnalysisPath[levelId];
        }
    }
}
