﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace InfiniteBeatSaber.DebugTools
{
    public static class EvalProgram
    {
        // `EvalMain` is intended to be used like a REPL. While Beat Saber is
        // running, write code in `EvalMain` and then build the "Eval" project
        // to have it injected into and executed in the running Beat Saber game.
        // Only works in debug builds.
        public static void EvalMain(IDictionary<string, object> state)
        {
            // Example: Log the name of a Beat Saber pack.
            var beatmapLevelsModel = UnityEngine.Object.FindObjectOfType<BeatmapLevelsModel>();
            var pack = beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.First();
            Log.Info($"Name of a Beat Saber pack: {pack.packName}");

            // Example: Log properties of a root `GameObject`.
            LogObj(SceneManager.GetActiveScene().GetRootGameObjects().First(), "a root GameObject");

            // Example: Log all classes that derive from `BeatmapDataItem`.
            LogSubclassHierarchy(typeof(BeatmapDataItem));
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

        #region Utilities for EvailMain's `state` parameter

        private static TValue GetOrAdd<TKey, TValue>(
            IDictionary<TKey, object> dictionary,
            TKey key,
            Func<TValue> generateValue)
        {
            if (!dictionary.TryGetValue(key, out object value))
            {
                value = generateValue();
                dictionary[key] = value;
            }

            return (TValue)value;
        }

        private static void RedoEffect<TKey>(
            IDictionary<TKey, object> dictionary,
            TKey key,
            Func<Action> doEffect)
        {
            if (dictionary.TryGetValue(key, out object cleanUp))
            {
                ((Action)cleanUp)();
            }

            dictionary[key] = doEffect();
        }

        private static TValue Update<TKey, TValue>(
            IDictionary<TKey, object> dictionary,
            TKey key,
            TValue initialValue,
            Func<TValue, TValue> updatedValue)
        {
            if (dictionary.TryGetValue(key, out object currentValue))
            {
                dictionary[key] = updatedValue((TValue)currentValue);
            }
            else
            {
                dictionary[key] = initialValue;
            }


            return (TValue)dictionary[key];
        }

        #endregion

        #region Utilities for acquiring Zenject dependencies

        private static IEnumerable<(string, DiContainer)> GetAllActiveDiContainers()
        {
            List<(string, DiContainer)> containers = new List<(string, DiContainer)>();

            // Get ProjectContext's container
            if (ProjectContext.HasInstance)
            {
                containers.Add(("ProjectContext", ProjectContext.Instance.Container));
            }

            // Get all SceneContext's containers
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene currentScene = SceneManager.GetSceneAt(i);
                if (currentScene.isLoaded)
                {
                    var sceneContexts = currentScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<SceneContext>());

                    foreach (var sceneContext in sceneContexts)
                    {
                        if (sceneContext != null)
                        {
                            containers.Add((sceneContext.name, sceneContext.Container));
                        }
                    }
                }
            }

            return containers;
        }

        private static T Resolve<T>()
        {
            var resolvedInstance = default(T);
            var isResolved = false;

            foreach (var (_, container) in GetAllActiveDiContainers())
            {
                if (container.HasBinding<T>())
                {
                    if (isResolved)
                    {
                        throw new ZenjectException($"Type: {typeof(T).Name} found in multiple active containers.");
                    }

                    resolvedInstance = container.Resolve<T>();
                    isResolved = true;
                }
            }

            if (!isResolved)
            {
                throw new ZenjectException($"Unable to resolve type: {typeof(T).Name} across all active containers.");
            }

            return resolvedInstance;
        }

        #endregion

        #region CSV utilities

        private static string EscapeCsvCellIfNeeded(string cell)
        {
            char[] specialCharacters = { '\r', '\n', ',', '"' };
            return (
                cell.IndexOfAny(specialCharacters) == -1 ? cell :
                "\"" + cell.Replace("\"", "\"\"") + "\""
            );
        }

        private static string ToCsv(IEnumerable<IEnumerable<string>> rows)
        {
            return string.Join(
                "\n",
                rows.Select(row =>
                    string.Join(
                        ",",
                        row.Select(cell => EscapeCsvCellIfNeeded(cell)))));
        }

        #endregion

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

        private static void LogEnumerable<T>(IEnumerable<T> enumerable, string label = null)
        {
            Log.Info(PrintEnumerable(enumerable));
        }

        private static string PrintEnumerable<T>(IEnumerable<T> enumerable, string label = null)
        {
            if (label != null) label = " [" + label + "]";

            var output = new StringBuilder();
            output.AppendLine($"Enumerable{label}:");

            foreach (var item in enumerable)
            {
                output.AppendLine($"  {item.ToString()}");
            }

            return output.ToString();
        }

        #endregion

        #region Printing C# subclass hierarchy

        private static void LogSubclassHierarchyAllAssemblies(Type rootType)
        {
            Log.Info(PrintSubclassHierarchyAllAssemblies(rootType));
        }

        private static string PrintSubclassHierarchyAllAssemblies(Type rootType)
        {
            return PrintSubclassHierarchy(rootType, AppDomain.CurrentDomain.GetAssemblies());
        }

        private static void LogSubclassHierarchy(Type rootType, IEnumerable<Assembly> assemblies = null)
        {
            Log.Info(PrintSubclassHierarchy(rootType, assemblies));
        }

        private static string PrintSubclassHierarchy(Type rootType, IEnumerable<Assembly> assemblies = null)
        {
            if (assemblies == null)
            {
                assemblies = new List<Assembly> { rootType.Assembly };
            }

            var types = assemblies.SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Log.Info("Skipping assembly: " + assembly.GetName().Name);
                    return new Type[] { };
                }
            }).ToList();

            var output = new StringBuilder();

            void Walk(Type type, int indentLevel)
            {
                output.AppendLine($"{new string('\t', indentLevel)}- `{type.Name}` ({type.Assembly.GetName().Name})");

                var subclasses =
                    types
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

        #region Printing Beat Saber song catalog

        private static void LogSongCatalog()
        {
            Log.Info(PrintSongCatalog());
        }

        private static string PrintSongCatalog()
        {
            var output = new StringBuilder();

            var beatmapLevelsModel = UnityEngine.Object.FindObjectOfType<BeatmapLevelsModel>();
            foreach (var pack in beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
            {
                output.AppendLine($"{pack.packName} (ID: {pack.packID})");
                foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                {
                    var mappedBy = string.IsNullOrEmpty(level.levelAuthorName) ? "" : $". Mapped by {level.levelAuthorName}";
                    output.AppendLine($"  {level.songName} by {level.songAuthorName}{mappedBy} (ID: {level.levelID})");
                }
            }

            return output.ToString();
        }

        #endregion

        #region Printing Unity UI hierarchy

        private static string PrintFullUIHierarchy()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var output = new StringBuilder();
            foreach (var c in roots)
            {
                output.Append(PrintUIHierarchy(c.gameObject));
            }
            return output.ToString();
        }

        private static void LogUIHierarchy(GameObject root)
        {
            Log.Info(PrintUIHierarchy(root));
        }

        private static string PrintUIHierarchy(GameObject root)
        {
            var output = new StringBuilder();

            void Walk(GameObject node, string indent)
            {
                if (node == null) return;

                // Get some relevant UI components for additional info
                var textComp = node.GetComponent<Text>();
                var imageComp = node.GetComponent<Image>();

                string additionalInfo = "";

                if (textComp != null)
                    additionalInfo += $"Text: \"{textComp.text}\" ";
                if (imageComp != null)
                    additionalInfo += $"Image: {imageComp.sprite?.name} ";

                // Log current GameObject
                output.AppendLine($"{indent}{node.name} ({node.GetType().Name}) {additionalInfo}");

                // Recurse on children
                foreach (Transform child in node.transform)
                {
                    Walk(child.gameObject, indent + "  ");
                }
            };

            Walk(root, "");
            return output.ToString();
        }

        #endregion

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
