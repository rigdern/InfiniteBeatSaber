// This file contains code derived from EternalJukebox (https://github.com/UnderMybrella/EternalJukebox/).
// Copyright 2021 UnderMybrella
// See the LICENSE file for the full MIT license terms.

using InfiniteJukeboxAlgorithm.AugmentedTypes;
using System;
using System.Collections.Generic;

namespace InfiniteJukeboxAlgorithm
{
    public class InfiniteBeats
    {
        // configs for chances to branch
        private const double randomBranchChanceDelta = 0.018;
        private const double maxRandomBranchChance = 0.5;
        private const double minRandomBranchChance = 0.18;
        private double curRandomBranchChance = minRandomBranchChance;

        private readonly IRandom _random;
        private readonly List<Quantum> _beats;
        private readonly int _lastBranchPoint;

        public InfiniteBeats(SpotifyAnalysis spotifyAnalysis, IRandom random=null)
        {
            var track = new Track()
            {
                Analysis = spotifyAnalysis,
            };

            // Deep clone track since we're going to modify it.
            track = track.DeepClone();

            TrackRemixer.RemixTrack(track);
            var nearestNeighborCalculatorResult = new NearestNeighborCalculator(track).CalculateNearestNeighbors();

            _random = random ?? new SystemRandom();
            _beats = track.Analysis.Beats;
            _lastBranchPoint = nearestNeighborCalculatorResult.LastBranchPoint;
        }

        public Quantum GetNextBeat(Quantum curBeat)
        {
            if (curBeat == null)
            {
                if (_beats[0].Start == 0)
                {
                    return _beats[0];
                }
                else
                {
                    return new Quantum
                    {
                        Start = 0,
                        Duration = _beats[0].Start,
                        Which = -1
                    };
                }
            }
            else
            {
                int nextIndex = curBeat.Which + 1;

                if (nextIndex < 0)
                {
                    return _beats[0];
                }
                else if (nextIndex >= _beats.Count)
                {
                    return null;
                }
                else
                {
                    return selectRandomNextBeat(_beats[nextIndex]);
                }
            }
        }

        private Quantum selectRandomNextBeat(Quantum seed)
        {
            if (seed.Neighbors.Count == 0)
            {
                return seed;
            }
            else if (shouldRandomBranch(seed))
            {
                var next = seed.Neighbors[0];
                seed.Neighbors.RemoveAt(0);
                seed.Neighbors.Add(next);
                var beat = next.Dest;
                return beat;
            }
            else
            {
                return seed;
            }
        }

        private bool shouldRandomBranch(Quantum q)
        {
            if (q.Which == _lastBranchPoint)
            {
                return true;
            }

            curRandomBranchChance += randomBranchChanceDelta;
            if (curRandomBranchChance > maxRandomBranchChance)
            {
                curRandomBranchChance = maxRandomBranchChance;
            }
            var randomValue = _random.NextDouble();
            var shouldBranch = randomValue < curRandomBranchChance;
            if (shouldBranch)
            {
                curRandomBranchChance = minRandomBranchChance;
            }
            return shouldBranch;
        }
    }

    public interface IRandom
    {
        double NextDouble();
    }

    public class SystemRandom : IRandom
    {
        public readonly int Seed;
        private readonly Random _random;

        public SystemRandom()
        {
            Seed = Environment.TickCount;
            _random = new Random(Seed);
        }

        public SystemRandom(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
