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

            // Original Soundtrack Vol. 1
            { "100Bills", new SpotifyAnalysisInfo(
                // The Beat Saber song has a brief intro that isn't present in the Spotify song. The
                // Beat Saber song is longer. Shift the Spotify analysis timestamps by the
                // difference in duration between the Beat Saber and Spotify songs.
                "100Bills.json", shiftTimestampsSeconds: 2.252919175) },
            { "BalearicPumping", new SpotifyAnalysisInfo("BalearicPumping.json") },
            { "BeatSaber", new SpotifyAnalysisInfo("BeatSaber.json") },
            { "Breezer", new SpotifyAnalysisInfo("Breezer.json") },
            { "CommercialPumping", new SpotifyAnalysisInfo(
                // Initial "crash" sound seems to come slightly later in the Spotify (~3.5 secs)
                // than Beat Saber song (~2.3 secs).
                "CommercialPumping.json", shiftTimestampsSeconds: -1.2) },
            { "CountryRounds", new SpotifyAnalysisInfo(
                // The Beat Saber song has a brief intro that isn't present in the Spotify song. The
                // Beat Saber song is longer. Shift the Spotify analysis timestamps by the
                // difference in duration between the Beat Saber and Spotify songs.
                "CountryRounds.json", shiftTimestampsSeconds: 4.571420825) },
            { "Escape", new SpotifyAnalysisInfo(
                // Initial singing starts earlier in the Spotify (~10.2 secs) than Beat Saber song
                // (~13.0 secs).
                "Escape.json", shiftTimestampsSeconds: 2.8) },
            { "Legend", new SpotifyAnalysisInfo(
                // Initial singing starts later in the Spotify (~3.3 secs) than Beat Saber song (~2
                // secs). Shift the Spotify analysis timestamps by the difference in duration
                // between the Beat Saber and Spotify songs.
                "Legend.json", shiftTimestampsSeconds: -1.33818954589844) },
            { "LvlInsane", new SpotifyAnalysisInfo("LvlInsane.json") },
            { "TurnMeOn", new SpotifyAnalysisInfo(
                // The Beat Saber song has a brief intro that isn't present in the Spotify song. The
                // Beat Saber song is longer. Shift the Spotify analysis timestamps by the
                // difference in duration between the Beat Saber and Spotify songs.
                "TurnMeOn.json", shiftTimestampsSeconds: 3) },

            // Original Soundtrack Vol. 2
            { "BeThereForYou", new SpotifyAnalysisInfo(
                // Initial singing starts earlier in the Spotify (~1.6 secs) than Beat Saber song (~3.4 secs).
                "BeThereForYou.json", shiftTimestampsSeconds: 1.8) },
            { "Elixia", new SpotifyAnalysisInfo(
                // Initial kick drum(?) starts earlier in the Spotify (~30.1 secs) than Beat Saber song (~31.9 secs).
                "Elixia.json", shiftTimestampsSeconds: 1.8) },
            { "INeedYou",new SpotifyAnalysisInfo(
                // Initial horn(?) starts earlier in the Spotify (~7.5 secs) than Beat Saber song (~9.0 secs).
                "INeedYou.json", shiftTimestampsSeconds: 1.5) },
            { "RumNBass", new SpotifyAnalysisInfo(
                // Initial vocals start later in the Spotify (~7.6 secs) than Beat Saber song (~4.7 secs).
                "RumNBass.json", shiftTimestampsSeconds: -2.9) },
            { "UnlimitedPower", new SpotifyAnalysisInfo(
                // Base drop after "we're playing" vocals starts earlier in the Spotify (~21.8 secs) than Beat Saber song (~23.0 secs).
                "UnlimitedPower.json", shiftTimestampsSeconds: 1.2) },

            // Original Soundtrack Vol. 3
            { "Origins", new SpotifyAnalysisInfo(
                // Initial vocals start earlier in the Spotify (~13.5 secs) than Beat Saber song (~13.6 secs).
                "Origins.json", shiftTimestampsSeconds: 0.1) },
            { "ReasonForLiving", new SpotifyAnalysisInfo(
                // Inhale starts earlier in the Spotify (~0.3 secs) than Beat Saber song (~2.3 secs).
                "ReasonForLiving.json", shiftTimestampsSeconds: 2) },
            { "GiveALittleLove", new SpotifyAnalysisInfo(
                // Initial vocals start earlier in the Spotify (~7.4 secs) than Beat Saber song (~9.4 secs).
                "GiveALittleLove.json", shiftTimestampsSeconds: 2) },
            { "FullCharge", new SpotifyAnalysisInfo(
                // Initial brass(?) starts earlier in the Spotify (~21.2 secs) than Beat Saber song (~23.2 secs).
                "FullCharge.json", shiftTimestampsSeconds: 2) },
            { "Immortal", new SpotifyAnalysisInfo(
                // Initial bass kick(?) starts earlier in the Spotify (~2.2 secs) than Beat Saber song (~2.5 secs).
                "Immortal.json", shiftTimestampsSeconds: 0.3) },
            { "BurningSands", new SpotifyAnalysisInfo(
                // Initial bell(?) starts later in the Spotify (~2.5 secs) than Beat Saber song (~1.7 secs).
                "BurningSands.json", shiftTimestampsSeconds: -0.8) },
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
            void ShiftQuantums<T>(List<T> quantums) where T : Quantum
            {
                foreach (var quantum in quantums)
                {
                    quantum.Start += shiftTimestampsSeconds;
                }

                quantums.RemoveAll(quantum => IsFloatLess(quantum.Start, 0));
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
