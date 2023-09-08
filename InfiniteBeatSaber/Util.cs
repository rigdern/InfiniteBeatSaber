using System;
using System.IO;
using System.Reflection;

namespace InfiniteBeatSaber
{
    internal static class Util
    {
        public static void Assert(bool pred, string message)
        {
            if (!pred)
                throw new Exception("Assertion failed: " + message);
        }

        public static T AssertNotNull<T>(T value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);

            return value;
        }

        // Code that calls this should be configured in the csproj file so that it
        // isn't even included in release builds. This is intended as a back up
        // mechanism that catches any mistakes that might be made.
#if !DEBUG
        // Generate a *compiler* error if anyone caller is included in a release build.
        [Obsolete("This code should not be included in release builds", error: true)]
#endif
        public static void AssertDebugBuild()
        {
#if !DEBUG
            throw new Exception("This code should not be included in release builds");
#endif
        }

        public static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"InfiniteBeatSaber.{resourceName}";

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
