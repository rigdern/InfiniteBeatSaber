// This file contains code derived from EternalJukebox (https://github.com/UnderMybrella/EternalJukebox/).
// Copyright 2021 UnderMybrella
// See the LICENSE file for the full MIT license terms.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace InfiniteJukeboxAlgorithm.AugmentedTypes
{
    public class Track
    {
        public SpotifyAnalysis Analysis { get; set; }

        public Track DeepClone()
        {
            return JsonConvert.DeserializeObject<Track>(JsonConvert.SerializeObject(this));
        }
    }

    public class SpotifyAnalysis
    {
        public List<Quantum> Sections { get; set; }
        public List<Quantum> Bars { get; set; }
        public List<Quantum> Beats { get; set; }
        public List<Quantum> Tatums { get; set; }
        public List<Segment> Segments { get; set; }
        [JsonProperty("fsegments")] public List<Segment> FSegments { get; set; } // Filtered segments

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                });
        }
    }

    public class Quantum
    {
        public double Start { get; set; }
        public double Duration { get; set; }
        public double Confidence { get; set; }
        public int Which { get; set; }
        public Track Track { get; set; }  // new property
        public Quantum Prev { get; set; }  // new property
        public Quantum Next { get; set; }  // new property
        public List<Segment> OverlappingSegments { get; set; }
        [JsonProperty("oseg")] public Segment OSeg { get; set; }  // new property
        public List<Quantum> Children { get; set; }  // new property
        public Quantum Parent { get; set; }  // new property
        public int IndexInParent { get; set; }
        public List<Edge> AllNeighbors { get; set; }
        public List<Edge> Neighbors { get; set; }
        public int Reach { get; set; }

        public Quantum ShallowClone()
        {
            return (Quantum)MemberwiseClone();
        }
    }

    public class Segment : Quantum
    {
        public double[] Timbre { get; set; }
        public double[] Pitches { get; set; }
        [JsonProperty("loudness_start")] public double LoudnessStart { get; set; }
        [JsonProperty("loudness_max")] public double LoudnessMax { get; set; }

        public double[] GetFieldValues(string field)
        {
            switch (field)
            {
                case "Timbre":
                    return Timbre;
                case "Pitches":
                    return Pitches;
                default:
                    throw new ArgumentException("Invalid field name.", nameof(field));
            }
        }
    }

    public class Edge
    {
        public int Id { get; set; }
        public Quantum Src { get; set; }
        public Quantum Dest { get; set; }
        public double Distance { get; set; }
    }
}
