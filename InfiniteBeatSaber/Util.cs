using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = "InfiniteBeatSaber." + resourceName;

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
