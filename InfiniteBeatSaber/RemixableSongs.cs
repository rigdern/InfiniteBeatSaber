using InfiniteJukeboxAlgorithm.AugmentedTypes;
using Newtonsoft.Json;
using Polyglot;
using System.Collections.Generic;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber
{
    internal static class RemixableSongs
    {
        private static readonly Dictionary<string, SpotifyAnalysisInfo> _levelIdToSpotifyAnalysisInfo;

        static RemixableSongs()
        {
            var beatSaber = new SpotifyAnalysisInfo("BeatSaber.json");
            var magic = new SpotifyAnalysisInfo(
                // A bass drop (around word "go") earlier in the Spotify (~46.2 secs) than Beat Saber song (~48.5 secs).
                "Magic.json", shiftTimestampsSeconds: 2.3);

            _levelIdToSpotifyAnalysisInfo = new Dictionary<string, SpotifyAnalysisInfo>
            {
                // Here's the meaning of the dictionary's keys and values:
                // - Key: The Beat Saber level ID as given by `IPreviewBeatmapLevel.levelID`.
                // - Value: See the field comments on the `SpotifyAnalysisInfo` class.

                // Custom levels
                //

                // Gangnam Style by PSY. Mapped by GreatYazer. https://bsaber.com/songs/141/
                { "custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF",
                    new SpotifyAnalysisInfo("custom_level_8E7E553099436AF31564ADF1977A5EC42A61CFFF.json") },
            
                // Beat Saber by Jaroslav Beck.
                { "custom_level_B68BF61AC6BE0E128BE32A85810D42E7C53F4756", beatSaber },

                // Magic ft. Meredith Bull by Jaroslav Beck. Mapped by Freeek.
                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "custom_level_0F3CD1E0CFC05FDB2FD59852A5F456E32F88BA9E", magic },

                // Original Soundtrack Vol. 1
                { "100Bills", new SpotifyAnalysisInfo(
                    // The Beat Saber song has a brief intro that isn't present in the Spotify song. The
                    // Beat Saber song is longer. Shift the Spotify analysis timestamps by the
                    // difference in duration between the Beat Saber and Spotify songs.
                    "100Bills.json", shiftTimestampsSeconds: 2.252919175) },
                { "BalearicPumping", new SpotifyAnalysisInfo("BalearicPumping.json") },
                { "BeatSaber", beatSaber },
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

                //
                // Original Soundtrack Vol. 4
                //

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                //{ "IntoTheDream", new SpotifyAnalysisInfo("IntoTheDream.json") }, 
                                                                                
                { "ItTakesMe", new SpotifyAnalysisInfo(
                    // Initial vocals ("yea!") start earlier in the Spotify (~17.0 secs) than Beat Saber song (~19.1 secs).
                    "ItTakesMe.json", shiftTimestampsSeconds: 2.1) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                //{ "LudicrousPlus", new SpotifyAnalysisInfo(
                //    // Initial vocals ("one") start earlier in the Spotify (~0.9 secs) than Beat Saber song (~1.2 secs).
                //    "LudicrousPlus.json", shiftTimestampsSeconds: 0.3) }, 

                { "SpinEternally", new SpotifyAnalysisInfo(
                    // Initial vocals ("spin") start earlier in the Spotify (~19.0 secs) than Beat Saber song (~21.3 secs).
                    "SpinEternally.json", shiftTimestampsSeconds: 2.3) },

                //
                // Original Soundtrack Vol. 5
                //

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "DollarSeventyEight", new SpotifyAnalysisInfo(
                //    // Initial bass drop starts later in the Spotify (~8.3 secs) than Beat Saber song (~6.3 secs).
                //    "DollarSeventyEight.json", shiftTimestampsSeconds: -2.0) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "CurtainsAllNightLong", new SpotifyAnalysisInfo(
                //    // Initial bass drop starts earlier in the Spotify (~45.1 secs) than Beat Saber song (~48.1 secs)
                //    "CurtainsAllNightLong.json", shiftTimestampsSeconds: 3.0) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "FinalBossChan", new SpotifyAnalysisInfo(
                //    // Initial bass drop starts later in the Spotify (~2.9 secs) than Beat Saber song (~1.9 secs).
                //    "FinalBossChan.json", shiftTimestampsSeconds: -1.0) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "Firestarter", new SpotifyAnalysisInfo(
                //    // A bass drop starts earlier in the Spotify (~37.5 secs) than Beat Saber song (~38.2 secs).
                //    "Firestarter.json", shiftTimestampsSeconds: 0.7) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                // - BPM changes (via multiple BPMChangeBeatmapEventData events in the song)
                //{ "IWannaBeAMachine", new SpotifyAnalysisInfo(
                //    // Initial drum of bass drop (around word "square") starts earlier in the Spotify
                //    // (~29.8 secs) than Beat Saber song (~33.3 secs).
                //    "IWannaBeAMachine.json", shiftTimestampsSeconds: 3.5) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "Magic", magic },

                //
                // Extras
                //

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "100BillsRemix", new SpotifyAnalysisInfo(
                //    // Initial drum beat earlier in the Spotify (~4.4 secs) than Beat Saber song (~4.5 secs).
                //    "100BillsRemix.json", shiftTimestampsSeconds: 0.1) },

                // Disabled due to currently unsupported beatmap item types:
                // - CustomNoteData, gameplayType: BurstSliderHead
                // - CustomSliderData
                // - LightColorBeatmapEventData
                // - LightRotationBeatmapEventData
                //{ "EscapeRemix", new SpotifyAnalysisInfo(
                //    // Initial drum beat at same timestamp in the Spotify & Beat Saber songs (~3.0).
                //    "EscapeRemix.json") },

                { "SpookyBeat", new SpotifyAnalysisInfo(
                    // Initial bell at same timestamp in the Spotify & Beat Saber songs (~2.2).
                    "SpookyBeat.json") },

                { "FitBeat", new SpotifyAnalysisInfo(
                    // Initial female vocals at same timestamp in the Spotify & Beat Saber songs (~10.0).
                    "FitBeat.json") },
                { "CrabRave", new SpotifyAnalysisInfo(
                    // Start of horn-sounding section later in the Spotify (~15.4 secs) than Beat Saber song (~9.6 secs).
                    "CrabRave.json", shiftTimestampsSeconds: -5.8) },
                { "PopStars", new SpotifyAnalysisInfo(
                    // Initial vocals earlier in the Spotify (~0.7 secs) than Beat Saber song (~1.8 secs).
                    "PopStars.json", shiftTimestampsSeconds: 1.1) },
            };
        }

        // Although the level itself may be remixable, some of its beatmaps may not be. Use
        // `IsDifficultyBeatmapRemixable` to check its beatmaps.
        public static bool IsLevelRemixable(IPreviewBeatmapLevel level)
        {
            return _levelIdToSpotifyAnalysisInfo.ContainsKey(level.levelID);
        }

        public static (bool Value, string Reason) IsDifficultyBeatmapRemixable(IDifficultyBeatmap difficultyBeatmap)
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
            // The name of the file in this project's "SpotifyAnalyses" folder. These files are
            // combined into a zip file which is included as an embedded resource in the DLL.
            public readonly string ResourceName;

            // Number of seconds to add to all timestamps of the SpotifyAnalysis. When the Beat
            // Saber & Spotify versions of a song aren't aligned, use this to shift the
            // SpotifyAnalysis timestamps so they align with Beat Saber's version of the song.
            //
            // I suspect that this doesn't have to be that accurate -- that it's okay if the Beat
            // Saber song isn't quite aligned with the Spotify one. Why do I suspect this? In trying
            // to align the Beat Saber & Spotify songs, I've been surprised by how hard it is to
            // tell whether the songs are aligned. The jumps in the Infinite Beat Saber remix sound
            // pretty good both before and after I've provided a value for `ShiftTimestampsSeconds`.
            //
            // I have a couple of potential explanations for why the remix sounds good even when the
            // songs aren't aligned:
            // - Mitigation for `ShiftTimestampsSeconds` being slightly off beat. When producing a
            //   slice of the song to play, `InfiniteRemix.cs`, if necessary, shifts the slice so
            //   that it begins and ends on a beat. Consequently, the slices will be on beat even if
            //   the Spotify analysis is aligned slightly off of the Beat Saber song's beat.
            // - Mitigation for `ShiftTimestampsSeconds` being off by a second or two. Probably, for
            //   the most part, a song gradually changes rather than undergoing sudden abrupt
            //   changes. Sudden abrupt changes would probably feel bad to the listener.
            //   Consequently, a song probably doesn't change very much in the span of 1 or 2
            //   seconds. So if the Spotify analysis determines that beat X is similar to beat Y
            //   (i.e. we can jump from X to Y without the listener noticing a seam), then this is
            //   probably true enough even when the Spotify analysis is misaligned by 1 or 2
            //   seconds.
            //
            // It would be good to try to test these hypotheses. Here are some potential tests:
            // - Remove the beat alignment logic from `InfiniteRemix.cs` so that it can return
            //   slices that start and end off beat. Update `ShiftTimestampsSeconds` to be
            //   intentionally off beat. Play the song and see whether the jumps produce noticeable
            //   seams.
            // - Update `ShiftTimestampsSeconds` so that it's off by a significant amount (10
            //   seconds? 20 seconds? 40 seconds?) Play the song and see whether the jumps produce
            //   noticeable seams.
            public readonly double ShiftTimestampsSeconds;

            public SpotifyAnalysisInfo(string resourceName, double shiftTimestampsSeconds = 0)
            {
                ResourceName = resourceName;
                ShiftTimestampsSeconds = shiftTimestampsSeconds;
            }
        }
    }
}
