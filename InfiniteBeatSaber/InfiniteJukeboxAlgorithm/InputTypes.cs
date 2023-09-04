// This file contains code derived from EternalJukebox (https://github.com/UnderMybrella/EternalJukebox/).
// Copyright 2021 UnderMybrella
// See the LICENSE file for the full MIT license terms.

using System.Collections.Generic;

namespace InfiniteJukeboxAlgorithm.InputTypes
{
    public class Track
    {
        public Info Info { get; set; }
        public AudioAnalysis Analysis { get; set; }
        public AudioSummary AudioSummary { get; set; }
    }

    public class Info
    {
        public string Service { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Url { get; set; }
        public int Duration { get; set; }
    }

    public class AudioAnalysis
    {
        public List<Section> Sections { get; set; }
        public List<Quantum> Bars { get; set; }
        public List<Quantum> Beats { get; set; }
        public List<Quantum> Tatums { get; set; }
        public List<Segment> Segments { get; set; }
    }

    public class Section
    {
        public double Start { get; set; }
        public double Duration { get; set; }
        public double Confidence { get; set; }
        public double Loudness { get; set; }
        public double Tempo { get; set; }
        public double TempoConfidence { get; set; }
        public int Key { get; set; }
        public double KeyConfidence { get; set; }
        public int Mode { get; set; }
        public double ModeConfidence { get; set; }
        public int TimeSignature { get; set; }
        public double TimeSignatureConfidence { get; set; }
    }

    public class Quantum
    {
        public double Start { get; set; }
        public double Duration { get; set; }
        public double Confidence { get; set; }
    }

    public class Segment
    {
        public double Start { get; set; }
        public double Duration { get; set; }
        public double Confidence { get; set; }
        public double LoudnessStart { get; set; }
        public double LoudnessMaxTime { get; set; }
        public double LoudnessMax { get; set; }
        public List<double> Pitches { get; set; }
        public List<double> Timbre { get; set; }
    }

    public class AudioSummary
    {
        public double Duration { get; set; }
    }
}
