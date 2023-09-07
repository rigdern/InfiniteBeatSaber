using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InfiniteBeatSaber.DebugTools
{
    public static class EvalProgram
    {
        public static void EvalMain(IDictionary<string, object> state)
        {
            Log.Info($"value: {0}");
        }

        private static IReadonlyBeatmapData OriginalMap
        {
            get
            {
                var tGameplayCoreSceneSetupDataPatches = Type.GetType("InfiniteBeatSaber.Patches.GameplayCoreSceneSetupDataPatches, InfiniteBeatSaber");
                PropertyInfo propertyInfo = tGameplayCoreSceneSetupDataPatches.GetProperty("OriginalBeatmap", BindingFlags.Static | BindingFlags.Public);
                return (IReadonlyBeatmapData)propertyInfo.GetValue(null);
            }
        }

        #region Printing generic C# objects

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

        #endregion

        #region Printing C# inheritance hierarchy

        private static void LogInheritanceHierarchy(Type rootType)
        {
            Log.Info(PrintInheritanceHierarchy(rootType));
        }

        private static string PrintInheritanceHierarchy(Type rootType)
        {
            var output = new StringBuilder();

            void Walk(Type type, int indentLevel)
            {
                output.AppendLine($"{new string('\t', indentLevel)}- `{type.Name}`");

                var subclasses =
                    rootType.Assembly
                    .GetTypes()
                    .Where(t => t.BaseType == type);

                foreach (var subclass in subclasses)
                {
                    Walk(subclass, indentLevel + 1);
                }
            }

            Walk(rootType, 0);

            return output.ToString();
        }

        #endregion

        #region Printing implementers of a C# interface

        private static void LogInterfaceImplementers(Type interfaceType)
        {
            Log.Info(PrintInterfaceImplementers(interfaceType));
        }

        private static string PrintInterfaceImplementers(Type interfaceType)
        {
            var output = new StringBuilder();

            var implementers =
                interfaceType.Assembly
                .GetTypes()
                .Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface);

            foreach (var implementer in implementers)
            {
                output.AppendLine(implementer.Name);
            }

            return output.ToString();
        }

        #endregion

        #region Printing BeatmapDataItems

        public static string PrintBeatmapDataItems(IEnumerable<BeatmapDataItem> beatmapDataItems)
        {
            var output = new StringBuilder();
            foreach (var item in beatmapDataItems)
            {
                output.Append(PrintObj(item));
            }
            return output.ToString();
        }

        #endregion

        #region Printing BeatmapDataItems histogram

        private static string BasicBeatmapEventTypeToString(BasicBeatmapEventType type)
        {
            // Type information from https://bsmg.wiki/mapping/difficulty-format-v2.html#type-2
            switch (type)
            {
                case BasicBeatmapEventType.Event0: return "Light.BackLasers";
                case BasicBeatmapEventType.Event1: return "Light.RingLights";
                case BasicBeatmapEventType.Event2: return "Light.LeftLasers";
                case BasicBeatmapEventType.Event3: return "Light.RightLasers";
                case BasicBeatmapEventType.Event4: return "Light.CenterLights";
                case BasicBeatmapEventType.Event5: return "Boost.BoostColors or Unused (V3)";
                case BasicBeatmapEventType.Event6: return type.ToString();
                case BasicBeatmapEventType.Event7: return type.ToString();
                case BasicBeatmapEventType.Event8: return "Trigger/Value.RingSpin";
                case BasicBeatmapEventType.Event9: return "Trigger/Value.RingZoom";
                case BasicBeatmapEventType.Event10: return "Light.ExtraLights or V1BPM.V1BPMChanges";
                case BasicBeatmapEventType.Event11: return type.ToString();
                case BasicBeatmapEventType.Event12: return "Value.LeftLaserSpeed";
                case BasicBeatmapEventType.Event13: return "Value.RightLaserSpeed";
                case BasicBeatmapEventType.Event14: return "Rotation.360°90°_EarlyRotation or Unused (V3)";
                case BasicBeatmapEventType.Event15: return "Rotation.360°90°_LateRotation or Unused (V3)";
                case BasicBeatmapEventType.Event16: return type.ToString();
                case BasicBeatmapEventType.Event17: return type.ToString();
                case BasicBeatmapEventType.Event18: return type.ToString();
                case BasicBeatmapEventType.Event19: return type.ToString();
                case BasicBeatmapEventType.Event20: return type.ToString();
                case BasicBeatmapEventType.Event21: return type.ToString();
                case BasicBeatmapEventType.VoidEvent: return type.ToString();
                case BasicBeatmapEventType.Special0: return type.ToString();
                case BasicBeatmapEventType.Special1: return type.ToString();
                case BasicBeatmapEventType.Special2: return type.ToString();
                case BasicBeatmapEventType.Special3: return type.ToString();
                case BasicBeatmapEventType.BpmChange: return "BPM.V2BPMChange or Unused (V3)";
                default:
                    throw new Exception("BasicBeatmapEventTypeToString: Unknown type: " + type.ToString());
            }
        }

        private static string PrintBeatmapDataItemsHistogram(IEnumerable<BeatmapDataItem> beatmapDataItems)
        {
            var histogram =
                beatmapDataItems
                .GroupBy(item =>
                {
                    if (item is NoteData noteData)
                    {
                        return $"{noteData.GetType().Name}, gameplayType: {noteData.gameplayType}";
                    }
                    else if (item is BasicBeatmapEventData basicBeatmapEventData)
                    {
                        return $"{basicBeatmapEventData.GetType().Name}, basicBeatmapEventType: {BasicBeatmapEventTypeToString(basicBeatmapEventData.basicBeatmapEventType)}";
                    }
                    else
                    {
                        return item.GetType().Name;
                    }
                })
                .OrderBy(group => group.Key);

            var output = new StringBuilder();
            foreach (var group in histogram)
            {
                output.AppendLine($"{group.Key}: {group.Count()}");
            }
            return output.ToString();
        }

        #endregion

        public static void WriteAllText(string fileName, string text)
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
