using System;

namespace InfiniteBeatSaber
{
    internal static class FloatComparison
    {
        private const double _threshold = 0.001;

        public static bool AreFloatsEqual(double a, double b, double threshold = _threshold)
        {
            return Math.Abs(a - b) <= threshold;
        }

        public static bool IsFloatAnInteger(double n, double threshold = _threshold)
        {
            var closestInteger = Math.Round(n);
            var distanceFromInteger = Math.Abs(n - closestInteger);
            return AreFloatsEqual(distanceFromInteger, 0, threshold);

        }

        public static bool IsFloatGreater(double a, double b)
        {
            return a > b && !AreFloatsEqual(a, b);
        }

        public static bool IsFloatGreaterOrEqual(double a, double b)
        {
            return a > b || AreFloatsEqual(a, b);
        }

        public static bool IsFloatLess(double a, double b)
        {
            return a < b && !AreFloatsEqual(a, b);
        }

        public static bool IsFloatLessOrEqual(double a, double b)
        {
            return a < b || AreFloatsEqual(a, b);
        }
    }
}
