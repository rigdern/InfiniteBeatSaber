// This file contains code derived from EternalJukebox (https://github.com/UnderMybrella/EternalJukebox/).
// Copyright 2021 UnderMybrella
// See the LICENSE file for the full MIT license terms.

using InfiniteJukeboxAlgorithm.AugmentedTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfiniteJukeboxAlgorithm
{
    public class NearestNeighborCalculatorResult
    {
        public int LastBranchPoint { get; set; }
    }

    public class NearestNeighborCalculator
    {
        private const int MaxBranches = 4;
        private const int MaxBranchThreshold = 80;

        private int nextEdgeId = 0;
        private Track _track;

        public NearestNeighborCalculator(Track track)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));

            _track = track;
        }

        public NearestNeighborCalculatorResult CalculateNearestNeighbors()
        {
            var beats = _track.Analysis.Beats;
            if (beats == null || beats.Count == 0)
                throw new ArgumentException("No beats data found in the track analysis.", nameof(_track));

            var type = "beats";
            var targetBranchCount = beats.Count / 6;

            PrecalculateNearestNeighbors(type, MaxBranches, MaxBranchThreshold);

            int count;
            int threshold;
            for (threshold = 10; threshold < MaxBranchThreshold; threshold += 5)
            {
                count = CollectNearestNeighbors(type, threshold);
                if (count >= targetBranchCount)
                {
                    break;
                }
            }

            var lastBranchPoint = PostProcessNearestNeighbors(type, threshold);
            return new NearestNeighborCalculatorResult
            {
                LastBranchPoint = lastBranchPoint
            };
        }

        private void PrecalculateNearestNeighbors(string type, int maxNeighbors, int maxThreshold)
        {
            var analysisData = GetAnalysisData(type);
            if (analysisData == null || analysisData.Count == 0 || analysisData[0].AllNeighbors != null)
                return;

            foreach (var q1 in analysisData)
            {
                CalculateNearestNeighborsForQuantum(type, maxNeighbors, maxThreshold, q1);
            }
        }

        private void CalculateNearestNeighborsForQuantum(string type, int maxNeighbors, int maxThreshold, Quantum q1)
        {
            var edges = new List<Edge>();
            int id = 0;
            var analysisData = GetAnalysisData(type);

            for (var i = 0; i < analysisData.Count; i++)
            {
                if (i == q1.Which)
                    continue;

                var q2 = analysisData[i];
                var sum = 0.0;

                for (var j = 0; j < q1.OverlappingSegments.Count; j++)
                {
                    var seg1 = q1.OverlappingSegments[j];
                    double distance = 100;

                    if (j < q2.OverlappingSegments.Count)
                    {
                        var seg2 = q2.OverlappingSegments[j];

                        if (seg1.Which == seg2.Which)
                        {
                            distance = 100;
                        }
                        else
                        {
                            distance = GetSegmentDistances(seg1, seg2);
                        }
                    }

                    sum += distance;
                }

                var pdistance = q1.IndexInParent == q2.IndexInParent ? 0 : 100;
                var totalDistance = sum / q1.OverlappingSegments.Count + pdistance;

                if (totalDistance < maxThreshold)
                {
                    var edge = new Edge
                    {
                        Id = id,
                        Src = q1,
                        Dest = q2,
                        Distance = totalDistance,
                    };

                    edges.Add(edge);
                    id++;
                }
            }

            edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            q1.AllNeighbors = edges.Take(maxNeighbors).ToList();
            foreach (var edge in q1.AllNeighbors)
            {
                edge.Id = nextEdgeId;
                nextEdgeId++;
            }
        }

        private double GetSegmentDistances(Segment seg1, Segment seg2)
        {
            var timbre = SegDistance(seg1, seg2, "Timbre", true);
            var pitch = SegDistance(seg1, seg2, "Pitches");
            var sloudStart = Math.Abs(seg1.LoudnessStart - seg2.LoudnessStart);
            var sloudMax = Math.Abs(seg1.LoudnessMax - seg2.LoudnessMax);
            var duration = Math.Abs(seg1.Duration - seg2.Duration);
            var confidence = Math.Abs(seg1.Confidence - seg2.Confidence);

            var distance = timbre * TimbreWeight + pitch * PitchWeight +
                           sloudStart * LoudStartWeight + sloudMax * LoudMaxWeight +
                           duration * DurationWeight + confidence * ConfidenceWeight;

            return distance;
        }

        private double SegDistance(Segment seg1, Segment seg2, string field, bool weighted = false)
        {
            if (weighted)
            {
                return WeightedEuclideanDistance(seg1.GetFieldValues(field), seg2.GetFieldValues(field));
            }
            else
            {
                return EuclideanDistance(seg1.GetFieldValues(field), seg2.GetFieldValues(field));
            }
        }

        private double WeightedEuclideanDistance(double[] v1, double[] v2)
        {
            double sum = 0;

            for (var i = 0; i < v1.Length; i++)
            {
                var delta = v2[i] - v1[i];
                var weight = 1.0; // Adjust this if required
                sum += delta * delta * weight;
            }

            return Math.Sqrt(sum);
        }

        private double EuclideanDistance(double[] v1, double[] v2)
        {
            double sum = 0;

            for (var i = 0; i < v1.Length; i++)
            {
                var delta = v2[i] - v1[i];
                sum += delta * delta;
            }

            return Math.Sqrt(sum);
        }

        private int CollectNearestNeighbors(string type, int maxThreshold)
        {
            var branchingCount = 0;
            var analysisData = GetAnalysisData(type);

            foreach (var q1 in analysisData)
            {
                q1.Neighbors = ExtractNearestNeighbors(q1, maxThreshold);
                if (q1.Neighbors.Count > 0)
                {
                    branchingCount++;
                }
            }

            return branchingCount;
        }

        private List<Edge> ExtractNearestNeighbors(Quantum q, int maxThreshold)
        {
            return q.AllNeighbors.Where(neighbor => neighbor.Distance <= maxThreshold).ToList();
        }

        private int PostProcessNearestNeighbors(string type, int threshold)
        {
            if (LongestBackwardBranch(type) < 50)
            {
                InsertBestBackwardBranch(type, threshold, 65);
            }
            else
            {
                InsertBestBackwardBranch(type, threshold, 55);
            }

            CalculateReachability(type);
            var lastBranchPoint = FindBestLastBeat(type);
            FilterOutBadBranches(type, lastBranchPoint);
            return lastBranchPoint;
        }

        private int LongestBackwardBranch(string type)
        {
            var longest = 0;
            var analysisData = GetAnalysisData(type);

            foreach (var q in analysisData)
            {
                foreach (var neighbor in q.Neighbors)
                {
                    var which = neighbor.Dest.Which;
                    var delta = q.Which - which;
                    if (delta > longest)
                    {
                        longest = delta;
                    }
                }
            }

            var lbb = longest * 100 / analysisData.Count;
            return lbb;
        }

        private void InsertBestBackwardBranch(string type, int threshold, int maxThreshold)
        {
            var branches = new List<double[]>();
            var analysisData = GetAnalysisData(type);

            for (var i = 0; i < analysisData.Count; i++)
            {
                var q = analysisData[i];
                foreach (var neighbor in q.AllNeighbors)
                {
                    var which = neighbor.Dest.Which;
                    var thresh = neighbor.Distance;
                    var delta = q.Which - which;

                    if (delta > 0 && thresh < maxThreshold)
                    {
                        var percent = delta * 100.0 / analysisData.Count;
                        var edge = new double[] { percent, i, which };
                        branches.Add(edge);
                    }
                }
            }

            if (branches.Count == 0)
            {
                return;
            }

            branches = branches.OrderByDescending(edge => edge[0]).ToList();
            var best = branches[0];
            var bestIndex = (int)best[1];
            var bestNeighbor = analysisData[bestIndex].AllNeighbors.FirstOrDefault(edge => edge.Dest.Which == (int)best[2]);

            if (bestNeighbor != null && bestNeighbor.Distance > threshold)
            {
                analysisData[bestIndex].Neighbors.Add(bestNeighbor);
            }
        }

        private void CalculateReachability(string type)
        {
            const int maxIter = 1000;
            var iter = 0;
            var analysisData = GetAnalysisData(type);

            foreach (var q in analysisData)
            {
                q.Reach = analysisData.Count - q.Which;
            }

            for (iter = 0; iter < maxIter; iter++)
            {
                var changeCount = 0;

                foreach (var q in analysisData)
                {
                    var changed = false;

                    foreach (var neighbor in q.Neighbors)
                    {
                        var q2 = neighbor.Dest;

                        if (q2.Reach > q.Reach)
                        {
                            q.Reach = q2.Reach;
                            changed = true;
                        }
                    }

                    if (q.Which < analysisData.Count - 1)
                    {
                        var q2 = analysisData[q.Which + 1];
                        if (q2.Reach > q.Reach)
                        {
                            q.Reach = q2.Reach;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        changeCount++;
                        for (var j = 0; j < q.Which; j++)
                        {
                            var q2 = analysisData[j];
                            if (q2.Reach < q.Reach)
                            {
                                q2.Reach = q.Reach;
                            }
                        }
                    }
                }

                if (changeCount == 0)
                {
                    break;
                }
            }
        }

        private int FindBestLastBeat(string type)
        {
            const int reachThreshold = 50;
            var analysisData = GetAnalysisData(type);
            var longest = 0;
            var longestReach = 0.0;

            for (var i = analysisData.Count - 1; i >= 0; i--)
            {
                var q = analysisData[i];
                var distanceToEnd = analysisData.Count - i;
                var reach = (q.Reach - distanceToEnd) * 100.0 / analysisData.Count;

                if (reach > longestReach && q.Neighbors.Count > 0)
                {
                    longestReach = reach;
                    longest = i;
                    if (reach >= reachThreshold)
                    {
                        break;
                    }
                }
            }

            return longest;
        }

        private void FilterOutBadBranches(string type, int lastIndex)
        {
            var analysisData = GetAnalysisData(type);

            for (var i = 0; i < lastIndex; i++)
            {
                var q = analysisData[i];
                q.Neighbors = q.Neighbors.Where(neighbor => neighbor.Dest.Which < lastIndex).ToList();
            }
        }

        private List<Quantum> GetAnalysisData(string type)
        {
            switch (type)
            {
                case "beats":
                    return _track.Analysis.Beats;
                case "sections":
                    return _track.Analysis.Sections;
                case "bars":
                    return _track.Analysis.Bars;
                case "tatums":
                    return _track.Analysis.Tatums;
                default:
                    throw new ArgumentException("Invalid analysis data type.", nameof(type));
            }
        }

        // Constants for weights (you can adjust them as required)
        private const double TimbreWeight = 1;
        private const double PitchWeight = 10;
        private const double LoudStartWeight = 1;
        private const double LoudMaxWeight = 1;
        private const double DurationWeight = 100;
        private const double ConfidenceWeight = 1;
    }
}
