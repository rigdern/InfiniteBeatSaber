// This file contains code derived from EternalJukebox (https://github.com/UnderMybrella/EternalJukebox/).
// Copyright 2021 UnderMybrella
// See the LICENSE file for the full MIT license terms.

using InfiniteJukeboxAlgorithm.AugmentedTypes;
using System;
using System.Collections.Generic;

namespace InfiniteJukeboxAlgorithm
{
    public static class TrackRemixer
    {
        public static void RemixTrack(Track track)
        {
            PreprocessTrack(track);
        }

        private static void PreprocessTrack(Track track)
        {
            var trackAnalysis = track.Analysis;
            var types = new List<IReadOnlyList<Quantum>> { trackAnalysis.Sections, trackAnalysis.Bars, trackAnalysis.Beats, trackAnalysis.Tatums, trackAnalysis.Segments };

            foreach (var qlist in types)
            {
                for (int index = 0; index < qlist.Count; index++)
                {
                    var q = qlist[index];
                    q.Track = track;
                    q.Which = index;
                    q.Prev = index > 0 ? qlist[index - 1] : null;
                    q.Next = index < qlist.Count - 1 ? qlist[index + 1] : null;
                }
            }

            ConnectQuanta(trackAnalysis.Sections, trackAnalysis.Bars);
            ConnectQuanta(trackAnalysis.Bars, trackAnalysis.Beats);
            ConnectQuanta(trackAnalysis.Beats, trackAnalysis.Tatums);
            ConnectQuanta(trackAnalysis.Tatums, trackAnalysis.Segments);

            ConnectFirstOverlappingSegment(trackAnalysis, trackAnalysis.Bars);
            ConnectFirstOverlappingSegment(trackAnalysis, trackAnalysis.Beats);
            ConnectFirstOverlappingSegment(trackAnalysis, trackAnalysis.Tatums);

            ConnectAllOverlappingSegments(trackAnalysis, trackAnalysis.Bars);
            ConnectAllOverlappingSegments(trackAnalysis, trackAnalysis.Beats);
            ConnectAllOverlappingSegments(trackAnalysis, trackAnalysis.Tatums);

            FilterSegments(trackAnalysis);
        }

        private static void FilterSegments(SpotifyAnalysis trackAnalysis)
        {
            double threshold = .3;
            var fsegs = new List<Segment> { trackAnalysis.Segments[0] };
            for (var i = 1; i < trackAnalysis.Segments.Count; i++)
            {
                var seg = trackAnalysis.Segments[i];
                var last = fsegs[fsegs.Count - 1];
                if (IsSimilar(seg, last) && seg.Confidence < threshold)
                {
                    fsegs[fsegs.Count - 1].Duration += seg.Duration;
                }
                else
                {
                    fsegs.Add(seg);
                }
            }
            trackAnalysis.FSegments = fsegs;
        }

        private static bool IsSimilar(Segment seg1, Segment seg2)
        {
            double threshold = 1;
            double distance = TimbralDistance(seg1, seg2);
            return (distance < threshold);
        }

        private static double TimbralDistance(Segment s1, Segment s2)
        {
            return EuclideanDistance(s1.Timbre, s2.Timbre);
        }

        private static double EuclideanDistance(double[] v1, double[] v2)
        {
            double sum = 0;
            for (var i = 0; i < 3; i++)
            {
                var delta = v2[i] - v1[i];
                sum += delta * delta;
            }
            return Math.Sqrt(sum);
        }

        private static void ConnectQuanta(IReadOnlyList<Quantum> qparents, IReadOnlyList<Quantum> qchildren)
        {
            var last = 0;
            foreach (var qparent in qparents)
            {
                qparent.Children = new List<Quantum>();
                for (var j = last; j < qchildren.Count; j++)
                {
                    var qchild = qchildren[j];
                    if (qchild.Start >= qparent.Start && qchild.Start < qparent.Start + qparent.Duration)
                    {
                        qchild.Parent = qparent;
                        qchild.IndexInParent = qparent.Children.Count;
                        qparent.Children.Add(qchild);
                        last = j;
                    }
                    else if (qchild.Start > qparent.Start)
                    {
                        break;
                    }
                }
            }
        }

        private static void ConnectFirstOverlappingSegment(SpotifyAnalysis trackAnalysis, IReadOnlyList<Quantum> quanta)
        {
            var last = 0;
            var segs = trackAnalysis.Segments;
            foreach (var q in quanta)
            {
                for (var j = last; j < segs.Count; j++)
                {
                    var qseg = segs[j];
                    if (qseg.Start >= q.Start)
                    {
                        q.OSeg = qseg;
                        last = j;
                        break;
                    }
                }
            }
        }

        private static void ConnectAllOverlappingSegments(SpotifyAnalysis trackAnalysis, IReadOnlyList<Quantum> quanta)
        {
            var last = 0;
            var segs = trackAnalysis.Segments;
            foreach (var q in quanta)
            {
                q.OverlappingSegments = new List<Segment>();
                for (var j = last; j < segs.Count; j++)
                {
                    var qseg = segs[j];
                    if ((qseg.Start + qseg.Duration) < q.Start)
                    {
                        continue;
                    }
                    if (qseg.Start > (q.Start + q.Duration))
                    {
                        break;
                    }
                    last = j;
                    q.OverlappingSegments.Add(qseg);
                }
            }
        }
    }
}
