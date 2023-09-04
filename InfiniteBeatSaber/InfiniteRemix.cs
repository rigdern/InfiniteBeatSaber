using InfiniteJukeboxAlgorithm.AugmentedTypes;
using InfiniteJukeboxAlgorithm;
using System;
using System.Linq;

namespace InfiniteBeatSaber
{
    internal class InfiniteRemix
    {
        private readonly InfiniteBeats _infiniteBeats;
        private readonly float _beatsPerMinute;

        private Quantum _prevBeat = null;
        private double _trackDuration = 0;

        public InfiniteRemix(SpotifyAnalysis spotifyAnalysis, float beatsPerMinute, IRandom random = null)
        {
            _infiniteBeats = new InfiniteBeats(spotifyAnalysis, random);
            _beatsPerMinute = beatsPerMinute;
        }

        // Returns at least the next `minSeconds` of the remix.
        public Remix GetNext(int minSeconds)
        {
            Remix remix = new Remix();

            double trackDurationDelta = _prevBeat?.Duration ?? 0;
            var curSlice = _prevBeat == null ? null :
                new Slice
                {
                    Clock = _trackDuration - _prevBeat.Duration,
                    Start = _prevBeat.Start,
                    Duration = _prevBeat.Duration
                };
            var beatsAreAdjacent = true;

            // Only break when the beats are not adjacent so we don't split a `Slice`
            // across two calls to `GetNext`. I expect this will enable the beatmap
            // remixer to avoid truncating objects that are multiple beats in length.
            while (!(trackDurationDelta >= minSeconds && !beatsAreAdjacent))
            {
                beatsAreAdjacent = true;

                Quantum curBeat = BeatAlignedObject(_infiniteBeats.GetNextBeat(_prevBeat));

                if (_prevBeat == null)
                {
                    curSlice = new Slice
                    {
                        Clock = 0,
                        Start = 0,
                        Duration = curBeat.Start + curBeat.Duration
                    };
                }
                else if (curBeat == null)
                {
                    throw new Exception("InfiniteBeats didn't return a beat.");
                }
                else if (_prevBeat.Which + 1 == curBeat.Which)
                {
                    // The beats are adjacent.
                    curSlice.Duration += curBeat.Duration;
                }
                else
                {
                    // The beats are not adjacent so we need to seek.
                    beatsAreAdjacent = false;

                    remix.Slices.Add(curSlice);

                    curSlice = new Slice
                    {
                        Clock = _trackDuration,
                        Start = curBeat.Start,
                        Duration = curBeat.Duration
                    };
                }

                remix.Beats.Add(new Beat
                {
                    Clock = _trackDuration,
                    BeatIndex = curBeat.Which
                });

                if (_prevBeat == null)
                {
                    trackDurationDelta += curBeat.Start;
                    _trackDuration += curBeat.Start;
                }

                // TODO: Could `trackDuration` increasingly drift due to floating point inaccuracies?
                // Maybe it'd be better to recalculate `trackDuration` each time from the current
                // beat.
                trackDurationDelta += curBeat.Duration;
                _trackDuration += curBeat.Duration;
                _prevBeat = curBeat;
            }

            remix.Slices = remix.Slices.Select(SliceWithBeats).ToList();
            remix.Beats = remix.Beats.Select(BeatWithBeats).ToList();

            return remix;
        }

        // `Duration` of the remix so far in seconds.
        public double Duration
        {
            get
            {
                if (_prevBeat == null)
                {
                    // The remix is empty.
                    return _trackDuration;
                }
                else
                {
                    // `_prevBeat` isn't a part of the remix yet.
                    return _trackDuration - _prevBeat.Duration;
                }
            }
        }

        private static bool AreFloatsEqual(double a, double b)
        {
            return Math.Abs(a - b) <= 0.001;
        }

        private double BeatsToSeconds(double beats)
        {
            return beats / _beatsPerMinute * 60;
        }

        private double SecondsToBeats(double seconds)
        {
            return seconds / 60 * _beatsPerMinute;
        }

        private double BeatAlignedSeconds(double seconds)
        {
            double beatsFloat = SecondsToBeats(seconds);
            int beats = (int)Math.Round(beatsFloat);
            return BeatsToSeconds(beats);
        }

        private Quantum BeatAlignedObject(Quantum beat)
        {
            double alignedDuration = BeatAlignedSeconds(beat.Duration);
            Util.Assert(
                // beat.Which of -1 means its the beginning of the song before Spotify's analysis
                // identified any beats. Consequently, it might be longer than 1 beat.
                beat.Which == -1 || AreFloatsEqual(SecondsToBeats(alignedDuration), 1),
                "Duration should be 1 beat but it is: " + SecondsToBeats(alignedDuration));
            var result = beat.ShallowClone();
            result.Start = BeatAlignedSeconds(result.Start);
            result.Duration = alignedDuration;
            return result;
        }

        private int SecondsToAlignedBeats(double seconds)
        {
            double beatsFloat = SecondsToBeats(seconds);
            int beats = (int)Math.Round(beatsFloat);
            return beats;
        }

        private Slice SliceWithBeats(Slice slice)
        {
            var result = slice.ShallowCopy();
            result.ClockBeats = SecondsToAlignedBeats(result.Clock);
            result.StartBeats = SecondsToAlignedBeats(result.Start);
            result.DurationBeats = SecondsToAlignedBeats(result.Duration);
            return result;
        }

        private Beat BeatWithBeats(Beat beat)
        {
            var result = beat.ShallowCopy();
            result.ClockBeats = SecondsToAlignedBeats(result.Clock);
            return result;
        }
    }
}
