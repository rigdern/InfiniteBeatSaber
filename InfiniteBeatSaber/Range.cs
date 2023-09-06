using System;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber
{
    internal struct Range
    {
        public readonly double Start;
        public readonly double End;

        public Range(double start, double end)
        {
            Start = start;
            End = end;
        }

        private bool DoesIntersect(Range otherRange)
        {
            return !(IsFloatLessOrEqual(End, otherRange.Start) || IsFloatLessOrEqual(otherRange.End, Start));
        }

        private Range Intersection(Range otherRange)
        {
            return new Range(
                Math.Max(Start, otherRange.Start),
                Math.Min(End, otherRange.End));
        }

        public bool TryIntersection(Range otherRange, out Range rangeIntersection)
        {
            if (DoesIntersect(otherRange))
            {
                rangeIntersection = Intersection(otherRange);
                return true;
            }
            else
            {
                rangeIntersection = new Range(0, 0);
                return false;
            }
        }
    }
}
