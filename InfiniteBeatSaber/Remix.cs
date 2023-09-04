using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteBeatSaber
{
    internal class Remix
    {
        public List<Slice> Slices { get; set; } = new List<Slice>();
        public List<Beat> Beats { get; set; } = new List<Beat> { };
    }

    internal class Slice
    {
        public double Clock { get; set; }
        public double Start { get; set; }
        public double Duration { get; set; }
        public int ClockBeats { get; set; }
        public int StartBeats { get; set; }
        public int DurationBeats { get; set; }

        public Slice ShallowCopy()
        {
            return (Slice)MemberwiseClone();
        }
    }

    internal class Beat
    {
        public double Clock { get; set; }
        public int BeatIndex { get; set; }
        public int ClockBeats { get; set; }

        public Beat ShallowCopy()
        {
            return (Beat)MemberwiseClone();
        }
    }
}
