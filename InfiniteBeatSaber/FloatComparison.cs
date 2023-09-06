using System;

namespace InfiniteBeatSaber
{
    internal static class FloatComparison
    {
        public static bool AreFloatsEqual(double a, double b)
        {
            return Math.Abs(a - b) <= 0.001;
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
