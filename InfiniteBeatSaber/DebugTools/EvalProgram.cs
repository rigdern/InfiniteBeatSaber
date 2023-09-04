using InfiniteBeatSaber.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace InfiniteBeatSaber.DebugTools
{
    public static class EvalProgram
    {
        public static void EvalMain(IDictionary<string, object> state)
        {
            Type type = Type.GetType("InfiniteBeatSaber.Patches.GameplayCoreSceneSetupDataPatches, InfiniteBeatSaber");
            // Accessing a static property
            PropertyInfo propertyInfo = type.GetProperty("OriginalBeatmap", BindingFlags.Static | BindingFlags.Public);
            var beatmap = (IReadonlyBeatmapData)propertyInfo.GetValue(null);

            var output = new StringBuilder();
            foreach (var item in beatmap.allBeatmapDataItems)
            {
                output.Append(PrintObj(item));
                //output.AppendLine(item.GetType().Name);
            }
            WriteAllText("beatmap.txt", output.ToString());

            Log.Info($"value: {beatmap}");
        }

        private static void LogObj(object obj, string label = null)
        {
            Log.Info(PrintObj(obj, label));
        }

        private static string PrintObj(object obj, string label = null)
        {
            if (label != null) label = " [" + label + "]";

            var output = new StringBuilder();

            if (obj != null)
            {
                var type = obj.GetType();
                var bindingAttr = BindingFlags.Public | BindingFlags.Instance;
                var values = new Dictionary<string, object>();

                output.AppendLine(type.Name + label);

                PropertyInfo[] properties = type.GetProperties(bindingAttr);
                foreach (PropertyInfo property in properties)
                {
                    values[property.Name] = property.GetValue(obj, null);
                }

                FieldInfo[] fields = type.GetFields(bindingAttr);
                foreach (FieldInfo field in fields)
                {
                    values[field.Name] = field.GetValue(obj);
                }

                foreach (var pair in values.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    output.AppendLine($"  {pair.Key}: {pair.Value}");
                }
            }
            else
            {
                output.AppendLine("null" + label);
            }

            return output.ToString();
        }

        private static void WriteAllText(string fileName, string text)
        {
            File.WriteAllText(Path.Combine(ScratchDirectory, fileName), text);
        }

        private static string ScratchDirectory => Path.Combine(Application.temporaryCachePath, "InfiniteBeatSaber");

        private static readonly IPA.Logging.Logger Log = (IPA.Logging.Logger)(
            typeof(Plugin)
            .GetProperty("Log", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null));
    }
}
