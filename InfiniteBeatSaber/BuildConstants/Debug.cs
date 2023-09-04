// These placeholder values are only used by development builds. Through code generation,
// the build process populates the constants with meaningful values during Release builds.

// Any information that needs to be communicated from the build process to the
// program should be included in here.

using System;

namespace InfiniteBeatSaber
{
    internal static class BuildConstants
    {
        public const string GitFullHash = "development";
        public const string GitShortHash = "dev";
        public static readonly DateTime BuildDate = DateTime.UtcNow;
    }
}
