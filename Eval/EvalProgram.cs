﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using InfiniteJukeboxAlgorithm.AugmentedTypes;
using System.Diagnostics;
using InfiniteBeatSaber.Extensions;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber.DebugTools
{
    public static class EvalProgram
    {
        private static IDictionary<string, object> _state;
        private static BeatmapCache _beatmapCache;

        private static Lazy<BeatmapLevelsModel> _beatmapLevelsModelLazy = ResolveLazy<BeatmapLevelsModel>();
        private static BeatmapLevelsModel _beatmapLevelsModel => _beatmapLevelsModelLazy.Value;

        private static Lazy<AdditionalContentModel> _additionalContentModelLazy = ResolveLazy<AdditionalContentModel>();
        private static AdditionalContentModel _additionalContentModel => _additionalContentModelLazy.Value;

        private static Lazy<GameplayCoreSceneSetupData> _gameplayCoreSceneSetupDataLazy = ResolveLazy<GameplayCoreSceneSetupData>();
        private static GameplayCoreSceneSetupData _gameplayCoreSceneSetupData => _gameplayCoreSceneSetupDataLazy.Value;

        private static Lazy<AudioTimeSyncController> _audioTimeSyncControllerLazy = ResolveLazy<AudioTimeSyncController>();
        private static AudioTimeSyncController _audioTimeSyncController => _audioTimeSyncControllerLazy.Value;

        // `EvalMain` is intended to be used like a REPL. While Beat Saber is running, write code in
        // `EvalMain` and then build the "Eval" project to have it injected into and executed in the
        // running Beat Saber game.
        //
        // Limitations:
        // - Only works in debug builds.
        // - Severely limited in Beat Saber 1.29.1. For reasons I don't understand, only the first
        //   eval works properly. Subsequent evals rerun the code from the first eval rather than
        //   running the new code you're trying to eval.
        public static void EvalMain(IDictionary<string, object> state)
        {
            _state = state;
            _beatmapCache = new BeatmapCache(GetOrAdd(state, "_beatmapCache", () => new Dictionary<string, object>()));

            // Example: Log the name of a Beat Saber pack.
            var pack = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.First();
            Info($"Name of a Beat Saber pack: {pack.packName}");

            // Example: Log properties of a root `GameObject`.
            LogObj(SceneManager.GetActiveScene().GetRootGameObjects().First(), "a root GameObject");

            // Example: Log all classes that derive from `BeatmapDataItem`.
            Info("Classes that derive from `BeatmapDataItem`:");
            LogSubclassHierarchy(typeof(BeatmapDataItem));
        }

        #region Tests: Compatibility of song catalog with Infinite Beat Saber

        /// <summary>
        /// Generates a report listing things in the song catalog that aren't supported by Infinite
        /// Beat Saber.
        /// </summary>
        private static async Task<string> GenerateSongCatalogCompatibilityReport()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fullySupportedLevels = new TreeNode("Fully supported levels");
            var compatibilityIssueLevels = new TreeNode("Levels with compatibility issues");
            var warningLevels = new TreeNode("Level warnings");
            var unsupportedTypes = new SortedSet<string>();

            var (remixableLevels, nonremixableLevels) = GetRemixableLevels();
            foreach (var (pack, levelPreview) in remixableLevels)
            {
                var levelNode = new TreeNode(levelPreview.levelID);
                var levelWarningNode = new TreeNode(levelPreview.levelID);

                // Check for non-zero songTimeOffset
                //
                if (levelPreview.songTimeOffset != 0)
                {
                    levelNode.Add($"songTimeOffset is not 0: {levelPreview.songTimeOffset}");
                }

                // Check for beats in the Spotify audio analysis that have a length that is different than 1 beat.
                //
                var spotifyAnalysis = _beatmapCache.ReadSpotifyAnalysis(levelPreview);
                var beatsPerMinute = spotifyAnalysis.Track.Tempo;
                var nonsingleBeats = new TreeNode("SpotifyAnalysis.Beats contains beats that have a length other than 1 beat");
                foreach (var beat in spotifyAnalysis.Beats)
                {
                    if (!IsOneBeat(beatsPerMinute, beat.Duration))
                    {
                        var durationBeats = SecondsToBeats(beatsPerMinute, beat.Duration);
                        nonsingleBeats.Add($"{beat.Start}: {durationBeats}");
                    }
                }
                levelWarningNode.AddIfHasChildren(nonsingleBeats);

                // Check if Spotify & Beat Saber BPMs are multiples of each other.
                //
                var bpmMultiple =
                    Math.Max(levelPreview.beatsPerMinute, spotifyAnalysis.Track.Tempo) /
                    Math.Min(levelPreview.beatsPerMinute, spotifyAnalysis.Track.Tempo);
                if (!IsFloatAnInteger(bpmMultiple, threshold: 0.01))
                {
                    levelWarningNode.Add($"Spotify & Beat Saber BPMs aren't multiples. Spotify: {spotifyAnalysis.Track.Tempo}, BS: {levelPreview.beatsPerMinute}, multiple: {bpmMultiple}");
                }

                var level = await _beatmapCache.GetBeatmapLevelAsync(levelPreview.levelID);
                foreach (var difficultyBeatmapSet in level.beatmapLevelData.difficultyBeatmapSets)
                {
                    var beatmapCharacteristicNode = new TreeNode(difficultyBeatmapSet.beatmapCharacteristic.serializedName);

                    foreach (var difficultyBeatmap in difficultyBeatmapSet.difficultyBeatmaps)
                    {
                        if (RemixableSongs.IsDifficultyBeatmapRemixable(difficultyBeatmap).Value)
                        {
                            var beatmapNode = new TreeNode(difficultyBeatmap.difficulty.ToString());

                            var beatmap = await _beatmapCache.GetBeatmapDataAsync(difficultyBeatmap);

                            // Check for unsupported beatmap item types
                            //
                            var (_, _, omittedItemTypes) = BeatmapRemixer.FilterAndSortBeatmapDataItems(beatmap.allBeatmapDataItems);
                            if (omittedItemTypes.Count() > 0)
                            {
                                beatmapNode.Add(new TreeNode("Unsupported beatmap item types", omittedItemTypes));
                                foreach (var item in omittedItemTypes) unsupportedTypes.Add(item);
                            }

                            // Check for BPM changes
                            //
                            var bpmChanges = beatmap.allBeatmapDataItems.Where(beatmapDataItem => beatmapDataItem is BPMChangeBeatmapEventData);
                            if (bpmChanges.Count(beatmapDataItem => beatmapDataItem.time > 0) > 0)
                            {
                                beatmapNode.Add(new TreeNode(
                                    "BPM changes aren't supported",
                                    bpmChanges.Select(bpmChange => new TreeNode($"{bpmChange.time}: {(bpmChange as BPMChangeBeatmapEventData).bpm}"))));
                            }

                            beatmapCharacteristicNode.AddIfHasChildren(beatmapNode);
                        }
                    }

                    levelNode.AddIfHasChildren(beatmapCharacteristicNode);
                }

                if (levelNode.Children.Count() > 0)
                {
                    compatibilityIssueLevels.Add(levelNode);
                }
                else
                {
                    fullySupportedLevels.Add(levelNode);
                }
                warningLevels.AddIfHasChildren(levelWarningNode);
            }

            var nonremixableLevelsOwned = new TreeNode("Levels you own");
            var nonremixableLevelsNotOwned = new TreeNode("Levels you do not own");
            foreach (var (pack, level) in nonremixableLevels)
            {
                var stringified = $"{pack.packID}/{level.levelID}";
                if (await IsLevelOwned(level.levelID))
                {
                    nonremixableLevelsOwned.Add(stringified);
                }
                else
                {
                    nonremixableLevelsNotOwned.Add(stringified);
                }
            }

            var printedTree = PrintTree(new List<TreeNode>
            {
                compatibilityIssueLevels,
                warningLevels,
                new TreeNode("Unsupported item types", unsupportedTypes),
                new TreeNode(
                    "Levels currently missing from `RemixableSongs.cs`", new List<TreeNode>
                    {
                        nonremixableLevelsOwned,
                        nonremixableLevelsNotOwned,
                    }),
                fullySupportedLevels,
            });

            stopwatch.Stop();

            return
                $"Checked {remixableLevels.Count()} levels in {stopwatch.ElapsedMilliseconds:N0} ms. Skipped levels missing from `RemixableSongs.cs`:\n" +
                $"  {nonremixableLevelsOwned.Children.Count} you own\n" +
                $"  {nonremixableLevelsNotOwned.Children.Count} you do not own\n" +
                $"\n" +
                printedTree;
        }

        private class TreeNode
        {
            public readonly string Value;
            public readonly IList<TreeNode> Children;

            public TreeNode(string value, IEnumerable<TreeNode> children = null)
            {
                Value = value;
                Children = children != null ? children.ToList() : new List<TreeNode>();
            }

            public TreeNode(string value, IEnumerable<string> children)
            {
                Value = value;
                Children = children.Select(child => new TreeNode(child)).ToList();
            }

            public void Add(TreeNode child)
            {
                Children.Add(child);
            }

            public void Add(string child)
            {
                Children.Add(new TreeNode(child));
            }

            public void AddIfHasChildren(TreeNode child)
            {
                if (child.Children.Count() > 0)
                {
                    Children.Add(child);
                }
            }
        }

        private static string PrintTree(IEnumerable<TreeNode> nodes)
        {
            var output = new StringBuilder();

            void Visit(TreeNode node, int indentLevel = 0)
            {
                output.AppendLine($"{new string(' ', indentLevel * 2)}{node.Value} ({node.Children.Count()})");
                foreach (var child in node.Children)
                {
                    Visit(child, indentLevel + 1);
                }
            }

            foreach (var node in nodes)
            {
                Visit(node);
            }

            return output.ToString();
        }

        #endregion

        #region Tests: Assumptions about arcs & chains

        /// <summary>
        /// Generates a report listing any occurrences in the beatmaps of the songs you own that
        /// violate our assumptions about Beat Saber's data representation for arcs & chains.
        /// </summary>
        private static async Task<string> GenerateArcsAndChainsAssumptionsReport()
        {
            bool MatchesHead(SliderData slider, NoteData note)
            {
                Util.Assert(slider.hasHeadNote, "MatchesHead should only be called with sliders that have a head");

                return
                    note.colorType == slider.colorType &&

                    note.time == slider.time &&
                    note.lineIndex == slider.headLineIndex &&
                    note.noteLineLayer == slider.headLineLayer &&
                    note.beforeJumpNoteLineLayer == slider.headBeforeJumpLineLayer &&
                    note.cutDirection == slider.headCutDirection &&
                    //AreFloatsEqual(note.cutDirectionAngleOffset, slider.headCutDirectionAngleOffset) && // The angle fields don't match in some cases

                    true;
            }

            bool MatchesTail(SliderData slider, NoteData note)
            {
                Util.Assert(slider.hasTailNote, "MatchesTail should only be called with sliders that have a tail");

                return
                    note.colorType == slider.colorType &&

                    note.time == slider.tailTime &&
                    note.lineIndex == slider.tailLineIndex &&
                    note.noteLineLayer == slider.tailLineLayer &&
                    note.beforeJumpNoteLineLayer == slider.tailBeforeJumpLineLayer &&
                    //note.cutDirection == slider.tailCutDirection && // The cut dir fields don't match in some cases
                    //AreFloatsEqual(note.cutDirectionAngleOffset, slider.tailCutDirectionAngleOffset) && // The angle fields don't match in some cases

                    true;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var successLevels = new TreeNode("Levels that fully match our assumptions");
            var violationLevels = new TreeNode("Levels that violate our assumptions");
            var notOwnedLevels = new TreeNode("Levels you don't own (so they couldn't be checked)");
            var checkedCount = 0;
            var skippedCount = 0;

            foreach (var pack in _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
            {
                foreach (var levelPreview in pack.beatmapLevelCollection.beatmapLevels)
                {
                    var levelNode = new TreeNode(levelPreview.levelID);
                    var isLevelOwned = await IsLevelOwned(levelPreview.levelID);

                    if (isLevelOwned)
                    {
                        ++checkedCount;
                        var level = await _beatmapCache.GetBeatmapLevelAsync(levelPreview.levelID);
                        foreach (var difficultyBeatmapSet in level.beatmapLevelData.difficultyBeatmapSets)
                        {
                            var beatmapCharacteristicNode = new TreeNode(difficultyBeatmapSet.beatmapCharacteristic.serializedName);

                            foreach (var difficultyBeatmap in difficultyBeatmapSet.difficultyBeatmaps)
                            {
                                var beatmapNode = new TreeNode(difficultyBeatmap.difficulty.ToString());

                                var normalSliderNode = new TreeNode("Normal sliders");
                                var headlessNormalNode = new TreeNode("Headless");
                                var headMismatchNormalNode = new TreeNode("Head mismatch");
                                var tailMismatchNormalNode = new TreeNode("Tail mismatch");

                                var burstSliderNode = new TreeNode("Burst sliders");
                                var headlessBurstNode = new TreeNode("Headless");
                                var tailedBurstNode = new TreeNode("Tailed");
                                var headMismatchBurstNode = new TreeNode("Head mismatch");

                                var beatmap = await _beatmapCache.GetBeatmapDataAsync(difficultyBeatmap);

                                var itemsByTime = new Dictionary<float, List<BeatmapDataItem>>();
                                foreach (var item in beatmap.allBeatmapDataItems)
                                {
                                    if (!itemsByTime.ContainsKey(item.time)) itemsByTime[item.time] = new List<BeatmapDataItem>();
                                    itemsByTime[item.time].Add(item);
                                }

                                foreach (var item in beatmap.allBeatmapDataItems)
                                {
                                    if (item is SliderData sliderData)
                                    {
                                        if (sliderData.sliderType == SliderData.Type.Normal)
                                        {
                                            // Arc
                                            //

                                            if (sliderData.hasHeadNote)
                                            {
                                                var headMatches = itemsByTime[sliderData.time].Where(
                                                    x => x is NoteData noteData &&
                                                    noteData.gameplayType == NoteData.GameplayType.Normal &&
                                                    MatchesHead(sliderData, noteData));
                                                if (headMatches.Count() != 1)
                                                {
                                                    // Violated assumption: All arcs have exactly 1 head `NoteData, gameplayType: Normal`.
                                                    headMismatchNormalNode.Add($"{sliderData.time}: Unexpected number of heads: {headMatches.Count()}");
                                                }
                                            }
                                            else
                                            {
                                                // Violated assumption: All arcs have a head (`hasHeadNote == true`).
                                                headlessNormalNode.Add(PrintBeatmapDataItem(sliderData));
                                            }

                                            if (sliderData.hasTailNote)
                                            {
                                                var tailMatches = itemsByTime[sliderData.tailTime].Where(
                                                    x => x is NoteData noteData &&
                                                    (
                                                        noteData.gameplayType == NoteData.GameplayType.Normal ||
                                                        noteData.gameplayType == NoteData.GameplayType.BurstSliderHead
                                                    ) &&
                                                    MatchesTail(sliderData, noteData));
                                                if (tailMatches.Count() != 1)
                                                {
                                                    // Violated assumption: All arcs have exactly 1 tail `NoteData, gameplayType: Normal or BurstSliderHead`.
                                                    tailMismatchNormalNode.Add($"{sliderData.time}: Unexpected number of tails: {tailMatches.Count()}");
                                                }
                                            }
                                        }
                                        else if (sliderData.sliderType == SliderData.Type.Burst)
                                        {
                                            // Chain
                                            //

                                            if (sliderData.hasTailNote)
                                            {
                                                // Violated assumption: No chains have a tail (`hasTailNote == false`).
                                                tailedBurstNode.Add(PrintBeatmapDataItem(sliderData));
                                            }

                                            if (sliderData.hasHeadNote)
                                            {
                                                var headMatches = itemsByTime[sliderData.time].Where(
                                                    x => x is NoteData noteData &&
                                                    noteData.gameplayType == NoteData.GameplayType.BurstSliderHead &&
                                                    MatchesHead(sliderData, noteData));
                                                if (headMatches.Count() != 1)
                                                {
                                                    // Violated assumption: All chains have exactly 1 head `NoteData, gameplayType: BurstSliderHead`.
                                                    headMismatchBurstNode.Add($"{sliderData.time}: Unexpected number of heads: {headMatches.Count()}");
                                                }
                                            }
                                            else
                                            {
                                                // Violated assumption: All chains have a head (`hasHeadNote == true`).
                                                headlessBurstNode.Add(PrintBeatmapDataItem(sliderData));
                                            }
                                        }
                                        else
                                        {
                                            throw new Exception($"Unexpected `sliderType`: {sliderData.sliderType}");
                                        }
                                    }
                                }

                                normalSliderNode.AddIfHasChildren(headlessNormalNode);
                                normalSliderNode.AddIfHasChildren(headMismatchNormalNode);
                                normalSliderNode.AddIfHasChildren(tailMismatchNormalNode);

                                burstSliderNode.AddIfHasChildren(headlessBurstNode);
                                burstSliderNode.AddIfHasChildren(tailedBurstNode);
                                burstSliderNode.AddIfHasChildren(headMismatchBurstNode);

                                beatmapNode.AddIfHasChildren(normalSliderNode);
                                beatmapNode.AddIfHasChildren(burstSliderNode);

                                beatmapCharacteristicNode.AddIfHasChildren(beatmapNode);
                            }

                            levelNode.AddIfHasChildren(beatmapCharacteristicNode);
                        }
                    }
                    else
                    {
                        ++skippedCount;
                    }

                    if (levelNode.Children.Count() > 0)
                    {
                        violationLevels.Add(levelNode);
                    }
                    else if (isLevelOwned)
                    {
                        successLevels.Add(levelNode);
                    }
                    else
                    {
                        notOwnedLevels.Add(levelNode);
                    }
                }
            }

            var printedTree = PrintTree(new List<TreeNode>
            {
                violationLevels,
                successLevels,
                notOwnedLevels,
            });

            stopwatch.Stop();

            return
                $"Checked {checkedCount} levels in {stopwatch.ElapsedMilliseconds:N0} ms. Skipped {skippedCount} levels you do not own.\n" +
                $"\n" +
                printedTree;
        }


        #endregion

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

        private static async Task<TValue> GetOrAddAsync<TKey, TValue>(
            IDictionary<TKey, object> dictionary,
            TKey key,
            Func<Task<TValue>> generateValue)
        {
            if (!dictionary.TryGetValue(key, out object value))
            {
                value = await generateValue();
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

        private static Lazy<T> ResolveLazy<T>()
        {
            return new Lazy<T>(() => Resolve<T>());
        }

        private static T Resolve<T>()
        {
            return ResolveAll<T>().Single();
        }

        private static IEnumerable<T> ResolveAll<T>()
        {
            var resolvedInstances = new HashSet<T>();

            foreach (var (_, container) in GetAllActiveDiContainers())
            {
                if (container.HasBinding<T>())
                {
                    resolvedInstances.Add(container.Resolve<T>());
                }
            }

            return resolvedInstances;
        }

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

        /// <summary>
        /// Writes the beatmap to a file with the name "{levelId}_{characteristic}_{difficulty}.txt".
        /// </summary>
        private static async Task WriteBeatmap(string levelId, string beatmapCharacteristic, BeatmapDifficulty difficulty)
        {
            var level = await _beatmapCache.GetBeatmapLevelAsync(levelId);
            var beatmap = await _beatmapCache.GetBeatmapDataAsync(GetBeatmap(level, beatmapCharacteristic, difficulty));
            WriteAllText($"{levelId}_{beatmapCharacteristic}_{difficulty}.txt", PrintBeatmapDataItems(beatmap.allBeatmapDataItems));
        }

        private static string PrintBeatmapDataItems(IEnumerable<BeatmapDataItem> beatmapDataItems)
        {
            var output = new StringBuilder();
            foreach (var item in beatmapDataItems)
            {
                output.Append(PrintBeatmapDataItem(item));
            }
            return output.ToString();
        }

        private static string PrintBeatmapDataItem(BeatmapDataItem item)
        {
            if (item is NoteData noteData)
            {
                if (noteData.gameplayType == NoteData.GameplayType.Bomb)
                {
                    return $"{item.time.ToString() + ":",-10} {noteData.GetType().Name}(gameplayType: {noteData.gameplayType}, lineIndex: {noteData.lineIndex}, lineLayer: {noteData.noteLineLayer})\n";
                }
                else
                {
                    return
                        $"{item.time.ToString() + ":",-10} {noteData.GetType().Name}(gameplayType: {noteData.gameplayType}, color: {noteData.colorType}, lineIndex: {noteData.lineIndex}, lineLayer: {noteData.noteLineLayer}, cutDir: {noteData.cutDirection})\n" +
                        $"  scoringType: {noteData.scoringType}\n" +
                        $"  beforeJumpNoteLineLayer: {noteData.beforeJumpNoteLineLayer}\n" +
                        $"  flipLineIndex: {noteData.flipLineIndex}\n" +
                        $"  flipYSide: {noteData.flipYSide}\n" +
                        $"  cutDirectionAngleOffset: {noteData.cutDirectionAngleOffset}\n" +
                        $"  cutSfxVolumeMultiplier: {noteData.cutSfxVolumeMultiplier}\n" +
                        "";
                }
            }
            else if (item is ObstacleData obstacleData)
            {
                return
                    $"{item.time.ToString() + ":",-10} {item.GetType().Name}(duration: {obstacleData.duration}, width: {obstacleData.width}, height: {obstacleData.height})\n" +
                    $"  lineIndex: {obstacleData.lineIndex}\n" +
                    $"  lineLayer: {obstacleData.lineLayer}\n" +
                    "";
            }
            else if (item is BPMChangeBeatmapEventData bpmData)
            {
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(bpm: {bpmData.bpm})\n";
            }
            else if (item is ColorBoostBeatmapEventData colorBoostData)
            {
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(boostColorsAreOn: {colorBoostData.boostColorsAreOn})\n";
            }
            else if (item is BasicBeatmapEventData basicData)
            {
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(basicBeatmapEventType: {BasicBeatmapEventTypeToString(basicData.basicBeatmapEventType)}, value: {basicData.value}, floatValue: {basicData.floatValue})\n";
            }
            else if (item is LightColorBeatmapEventData lightColorEvent)
            {
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(group: {lightColorEvent.groupId}, el: {lightColorEvent.elementId}, transition: {lightColorEvent.transitionType}, color: {lightColorEvent.colorType}, brightness: {lightColorEvent.brightness}, strobeBeatFreq: {lightColorEvent.strobeBeatFrequency})\n";
            }
            else if (item is LightRotationBeatmapEventData lightRotEvent)
            {
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(group: {lightRotEvent.groupId}, el: {lightRotEvent.elementId}, ease: {lightRotEvent.easeType}, axis: {lightRotEvent.axis}, loopCount: {lightRotEvent.loopCount}, rotation: {lightRotEvent.rotation}, dir: {lightRotEvent.rotationDirection})\n";
            }
            else if (item is SliderData sliderData)
            {
                var headDesc =
                    !sliderData.hasHeadNote ? "(not set)" :
                    $"(lineIndex: {sliderData.headLineIndex}, lineLayer: {sliderData.headLineLayer}, beforeJumpLineLayer: {sliderData.headBeforeJumpLineLayer}, ctrlPtLngthMultiplier: {sliderData.headControlPointLengthMultiplier}, cutDir: {sliderData.headCutDirection}, cutDirAngleOffset: {sliderData.headCutDirectionAngleOffset})";
                var tailDesc =
                    !sliderData.hasTailNote ? "(not set)" :
                    $"(lineIndex: {sliderData.tailLineIndex}, lineLayer: {sliderData.tailLineLayer}, beforeJumpLineLayer: {sliderData.tailBeforeJumpLineLayer}, ctrlPtLngthMultiplier: {sliderData.tailControlPointLengthMultiplier}, cutDir: {sliderData.tailCutDirection}, cutDirAngleOffset: {sliderData.tailCutDirectionAngleOffset})";
                return $"{item.time.ToString() + ":",-10} {item.GetType().Name}(type: {sliderData.sliderType}, color: {sliderData.colorType}, midAnchorMode: {sliderData.midAnchorMode}, sliceCount: {sliderData.sliceCount}, squishAmount: {sliderData.sliceCount})\n" +
                    $"  {sliderData.time}: Head{headDesc}\n" +
                    $"  {sliderData.tailTime}: Tail{tailDesc}\n" +
                    $"  {sliderData.tailTime - sliderData.time} (duration)\n" +
                    "";
            }
            else
            {
                return $"{item.time.ToString() + ":",-10} {PrintObj(item, "Unhandled beatmap item type")}";
            }
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

            foreach (var pack in _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
            {
                output.AppendLine($"{pack.packName} (ID: {pack.packID})");
                foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                {
                    var subsongName = string.IsNullOrEmpty(level.songSubName) ? "" : $" {level.songSubName}";
                    var mappedBy = string.IsNullOrEmpty(level.levelAuthorName) ? "" : $". Mapped by {level.levelAuthorName}";
                    output.AppendLine($"  {level.songName}{subsongName} by {level.songAuthorName}{mappedBy} (ID: {level.levelID})");
                }
            }

            return output.ToString();
        }

        #endregion

        #region Printing Unity UI hierarchy

        private static string PrintFullSceneHierarchy(bool verbose = false)
        {
            var output = new StringBuilder();

            var activeScene = SceneManager.GetActiveScene();
            foreach (var scene in GetScenes())
            {
                var activePrefix = scene == activeScene ? "*" : "";
                var scenePrefix = scene.isSubScene ? "SubScene" : "Scene";
                output.AppendLine($"<{activePrefix}{scenePrefix}/{scene.name}>");
                var roots = scene.GetRootGameObjects();
                foreach (var c in roots)
                {
                    output.Append(PrintUIHierarchy(c.gameObject, verbose, "  "));
                }
                output.AppendLine($"</{activePrefix}{scenePrefix}/{scene.name}>");
            }

            return output.ToString();
        }

        private static string PrintFullUIHierarchy(bool verbose = false)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var output = new StringBuilder();
            foreach (var c in roots)
            {
                output.Append(PrintUIHierarchy(c.gameObject, verbose));
            }
            return output.ToString();
        }

        private static string PrintUIHierarchy(GameObject root, bool verbose = false, string initialIndent = "")
        {
            var output = new StringBuilder();

            string AdditionalInfo(UnityEngine.Object obj)
            {
                return
                    obj is Text text ? text.text :
                    obj is Image image ? image.sprite?.name :
                    obj is TextMeshPro textMesh ? textMesh.text :
                    obj is TextMeshProUGUI textMeshUGUI ? textMeshUGUI.text :
                    null;
            }

            string DisabledNotation(bool isDisabled)
            {
                return isDisabled ? "//" : "";
            }

            void Walk(GameObject node, string indent)
            {
                if (node == null) return;

                var selfClose = node.transform.childCount == 0 ? " /" : "";
                var disabledNotation = node.activeInHierarchy ? "" : "//";
                var components = string.Join(" ", node
                    .GetComponents<UnityEngine.Object>()
                    .Select(c =>
                    {
                        var componentDisabled = c is Behaviour behaviour && !behaviour.enabled;
                        var name = c.name == node.name ? "" : $"[{c.name}]";
                        var additionalInfoRaw = verbose ? AdditionalInfo(c) : null;
                        var additionalInfo = additionalInfoRaw == null ? null : $"({additionalInfoRaw})";
                        return $"{DisabledNotation(componentDisabled)}{c.GetType().Name}{name}{additionalInfo}";
                    }));
                output.AppendLine($"{indent}<{DisabledNotation(!node.activeInHierarchy)}{node.name} {components}{selfClose}>");

                // Recurse on children
                foreach (Transform child in node.transform)
                {
                    Walk(child.gameObject, indent + "  ");
                }

                if (node.transform.childCount > 0)
                {
                    output.AppendLine($"{indent}</{node.name}>");
                }
            };

            Walk(root, initialIndent);
            return output.ToString();
        }

        private static List<Scene> GetScenes()
        {
            var scenes = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }
            return scenes;
        }

        #endregion

        #region Retrieving Beat Saber levels & beatmaps

        private static IPreviewBeatmapLevel GetLevel(string levelId)
        {
            foreach (var pack in _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
            {
                foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                {
                    if (level.levelID == levelId)
                    {
                        return level;
                    }
                }
            }

            return null;
        }

        private static IDifficultyBeatmap GetBeatmap(IBeatmapLevel level, string beatmapCharacteristic, BeatmapDifficulty difficulty)
        {
            var difficultyBeatmapSet = level.beatmapLevelData.difficultyBeatmapSets
                .Where(set => set.beatmapCharacteristic.serializedName == beatmapCharacteristic)
                .Single();
            var beatmap = difficultyBeatmapSet.difficultyBeatmaps
                .Where(map => map.difficulty == difficulty)
                .Single();

            return beatmap;
        }

        private static (
            IEnumerable<(IBeatmapLevelPack Pack, IPreviewBeatmapLevel LevelPreview)> RemixableLevels,
            IEnumerable<(IBeatmapLevelPack Pack, IPreviewBeatmapLevel LevelPreview)> NonremixableLevels)
            GetRemixableLevels()
        {
            var remixableLevels = new List<(IBeatmapLevelPack Pack, IPreviewBeatmapLevel LevelPreview)>();
            var nonremixableLevels = new List<(IBeatmapLevelPack Pack, IPreviewBeatmapLevel LevelPreview)>();

            foreach (var pack in _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
            {
                foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                {
                    if (RemixableSongs.IsLevelRemixable(level))
                    {
                        remixableLevels.Add((pack, level));
                    }
                    else
                    {
                        nonremixableLevels.Add((pack, level));
                    }
                }
            }

            return (remixableLevels, nonremixableLevels);
        }

        private static async Task<bool> IsLevelOwned(string levelId)
        {
            var entitlementStatus = await _beatmapCache.GetLevelEntitlementStatusAsync(levelId);
            switch (entitlementStatus)
            {
                case AdditionalContentModel.EntitlementStatus.Owned: return true;
                case AdditionalContentModel.EntitlementStatus.NotOwned: return false;
                case AdditionalContentModel.EntitlementStatus.Failed: throw new Exception($"GetLevelEntitlementStatusAsync failed for level {levelId}");
                default: throw new Exception($"Unexpected EntitlementStatus: {entitlementStatus}");
            }
        }

        private class BeatmapCache
        {
            private readonly Dictionary<string, object> _cache;

            public BeatmapCache()
            {
            }

            public BeatmapCache(Dictionary<string, object> cache)
            {
                _cache = cache;
            }

            public Task<IBeatmapLevel> GetBeatmapLevelAsync(string levelId)
            {
                var key = string.Join("/",
                    "level",
                    levelId);

                return GetOrAddAsync(_cache, key, async () =>
                {
                    //Log.Info($"GetBeatmapLevelAsync: Cache miss: {key}");

                    var levelResult = await _beatmapLevelsModel.GetBeatmapLevelAsync(levelId, CancellationToken.None);
                    if (levelResult.isError)
                    {
                        throw new Exception($"Failed loading level: {levelId}");
                    }

                    return levelResult.beatmapLevel;
                });
            }

            public Task<IReadonlyBeatmapData> GetBeatmapDataAsync(IDifficultyBeatmap difficultyBeatmap)
            {
                var key = string.Join("/",
                    "beatmap",
                    difficultyBeatmap.level.levelID,
                    difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName,
                    difficultyBeatmap.difficulty.ToString());

                return GetOrAddAsync(_cache, key, () =>
                {
                    //Log.Info($"GetBeatmapDataAsync: Cache miss: {key}");
                    return difficultyBeatmap.GetBeatmapDataAsync(difficultyBeatmap.GetEnvironmentInfo(), new PlayerSpecificSettings());
                });
            }

            public SpotifyAnalysis ReadSpotifyAnalysis(IPreviewBeatmapLevel level)
            {
                var key = string.Join("/",
                    "spotifyAnalysis",
                    level.levelID);

                return GetOrAdd(_cache, key, () =>
                {
                    //Log.Info($"ReadSpotifyAnalysis: Cache miss: {key}");
                    return RemixableSongs.ReadSpotifyAnalysis(level);
                });
            }

            public Task<AdditionalContentModel.EntitlementStatus> GetLevelEntitlementStatusAsync(string levelId)
            {
                var key = string.Join("/",
                    "levelEntitlementStatus",
                    levelId);

                return GetOrAddAsync(_cache, key, () =>
                {
                    //Log.Info($"GetLevelEntitlementStatusAsync: Cache miss: {key}");
                    return _additionalContentModel.GetLevelEntitlementStatusAsync(levelId, CancellationToken.None);
                });
            }
        }

        #endregion

        #region Beat utilities

        private static double SecondsToBeats(double beatsPerMinute, double seconds)
        {
            return seconds / 60 * beatsPerMinute;
        }

        private static bool IsOneBeat(double beatsPerMinute, double duration)
        {
            var durationBeats = SecondsToBeats(beatsPerMinute, duration);
            return AreFloatsEqual(durationBeats, 1, threshold: 0.15);
        }

        #endregion

        #region Beatmap manipulation

        // Seems to do nothing if `ActiveBeatmap` already contains items that occur later than the
        // arc we're adding.
        private static void SpawnArc(bool omitTail = false)
        {
            var headTime = _audioTimeSyncController.songTime + 1;
            var tailTime = headTime + 0.5f;

            // Head note. (`NoteData, type: Normal`)
            AddBeatmapDataItemInOrder(
                ActiveBeatmap,
                NoteData.CreateBasicNoteData(
                    time: headTime,
                    lineIndex: 2,
                    noteLineLayer: NoteLineLayer.Top,
                    colorType: ColorType.ColorA,
                    cutDirection: NoteCutDirection.Up));

            // Connecting arc. (a `SliderData, sliderType: Normal` pointing from the head note to the tail note)
            AddBeatmapDataItemInOrder(
                ActiveBeatmap,
                new SliderData(
                    sliderType: SliderData.Type.Normal,
                    colorType: ColorType.ColorA,

                    // Head
                    hasHeadNote: true,
                    headTime: _audioTimeSyncController.songTime + 1,
                    headLineIndex: 2,
                    headLineLayer: NoteLineLayer.Top,
                    headBeforeJumpLineLayer: NoteLineLayer.Base,
                    headControlPointLengthMultiplier: 1f,
                    headCutDirection: NoteCutDirection.Up,
                    headCutDirectionAngleOffset: 0f,

                    // Tail
                    hasTailNote: !omitTail,
                    tailTime: tailTime,
                    tailLineIndex: 1,
                    tailLineLayer: NoteLineLayer.Base,
                    tailBeforeJumpLineLayer: NoteLineLayer.Base,
                    tailControlPointLengthMultiplier: 1f,
                    tailCutDirection: NoteCutDirection.Down,
                    tailCutDirectionAngleOffset: 0f,

                    midAnchorMode: SliderMidAnchorMode.Straight,
                    sliceCount: 0,
                    squishAmount: 1f));

            if (!omitTail)
            {
                // Tail note. (`NoteData, type: Normal`)
                AddBeatmapDataItemInOrder(
                    ActiveBeatmap,
                    NoteData.CreateBasicNoteData(
                        time: tailTime,
                        lineIndex: 1,
                        noteLineLayer: NoteLineLayer.Base,
                        colorType: ColorType.ColorA,
                        cutDirection: NoteCutDirection.Down));
            }
        }

        // Seems to do nothing if `ActiveBeatmap` already contains items that occur later than the
        // chain we're adding.
        private static void SpawnChain()
        {
            var headTime = _audioTimeSyncController.songTime + 1;
            var tailTime = headTime + 0.01249f;

            // Tail links. (a tailless `SliderData, sliderType: Burst` pointing at the head note).
            AddBeatmapDataItemInOrder(
                ActiveBeatmap,
                new SliderData(
                    sliderType: SliderData.Type.Burst,
                    colorType: ColorType.ColorA,

                    // Head
                    hasHeadNote: true,
                    headTime: headTime,
                    headLineIndex: 1,
                    headLineLayer: NoteLineLayer.Base,
                    headBeforeJumpLineLayer: NoteLineLayer.Base,
                    headControlPointLengthMultiplier: 0f,
                    headCutDirection: NoteCutDirection.UpLeft,
                    headCutDirectionAngleOffset: 0f,

                    // Tail
                    hasTailNote: false,
                    tailTime: tailTime,
                    tailLineIndex: 0,
                    tailLineLayer: NoteLineLayer.Upper,
                    tailBeforeJumpLineLayer: NoteLineLayer.Upper,
                    tailControlPointLengthMultiplier: 0f,
                    tailCutDirection: NoteCutDirection.Any,
                    tailCutDirectionAngleOffset: 0f,
                    midAnchorMode: SliderMidAnchorMode.Straight,
                    sliceCount: 4,
                    squishAmount: 1f));

            // Head note. (`NoteData, type: BurstSliderHead`)
            AddBeatmapDataItemInOrder(
                ActiveBeatmap,
                CreateBurstSliderHeadNoteData(
                    time: headTime,
                    lineIndex: 1,
                    noteLineLayer: NoteLineLayer.Base,
                    beforeJumpNoteLineLayer: NoteLineLayer.Base,
                    colorType: ColorType.ColorA,
                    cutDirection: NoteCutDirection.UpLeft,
                    cutSfxVolumeMultiplier: 1));
        }

        // Head note for *chains*.
        private static NoteData CreateBurstSliderHeadNoteData(
            float time,
            int lineIndex,
            NoteLineLayer noteLineLayer,
            NoteLineLayer beforeJumpNoteLineLayer,
            ColorType colorType,
            NoteCutDirection cutDirection,
            float cutSfxVolumeMultiplier)
        {
            var sliderHead = NoteData.CreateBurstSliderNoteData(
                time: time,
                lineIndex: lineIndex,
                noteLineLayer: noteLineLayer,
                beforeJumpNoteLineLayer: beforeJumpNoteLineLayer,
                colorType: colorType,
                cutDirection: cutDirection,
                cutSfxVolumeMultiplier: cutSfxVolumeMultiplier);
            sliderHead.ChangeToBurstSliderHead();

            return sliderHead;
        }

        private static BeatmapData ActiveBeatmap
        {
            get
            {
                var readonlyBeatmap = Util.AssertNotNull(_gameplayCoreSceneSetupData.transformedBeatmapData, "readonlyBeatmap");
                return Util.AssertNotNull(readonlyBeatmap as BeatmapData, "beatmap");
            }
        }

        // Seems to do nothing if `map` already contains items that occur later than `item`.
        private static void AddBeatmapDataItemInOrder(BeatmapData map, BeatmapDataItem item)
        {
            if (item is BeatmapEventData eventData)
            {
                map.InsertBeatmapEventDataInOrder(eventData);
            }
            else if (item is BeatmapObjectData objectData)
            {
                map.AddBeatmapObjectDataInOrder(objectData);
            }
            else
            {
                throw new Exception("BeatmapDataItem isn't event or object data. Its class is: " + item.GetType().Name);
            }
        }

        #endregion

        private static void WriteAllText(string fileName, string text)
        {
            File.WriteAllText(Path.Combine(ScratchDirectory, fileName), text);
            Info($"Wrote {fileName}");
        }

        private static string ScratchDirectory => Path.Combine(Application.temporaryCachePath, "InfiniteBeatSaber");

        private static readonly IPA.Logging.Logger Log = Plugin.Log;

        private static void Info(string message) => Log.Info(message);
    }
}
