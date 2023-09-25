using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static InfiniteBeatSaber.FloatComparison;

namespace InfiniteBeatSaber
{
    internal class BeatmapRemixer
    {
        private readonly IEnumerable<BeatmapDataItem> _sortedBeatmapDataItems; // Sorted by time
        private readonly IEnumerable<ObstacleData> _sortedObstacleDataItems; // Sorted by time
        private readonly BeatmapData _remixedBeatmap;

        public BeatmapRemixer(IReadonlyBeatmapData beatmap, BeatmapData remixedBeatmap)
        {
            (_sortedBeatmapDataItems, _sortedObstacleDataItems) = FilterAndSortBeatmapDataItems(beatmap.allBeatmapDataItems);
            _remixedBeatmap = remixedBeatmap;

            AddBeatmapDataItemsInOrder(_remixedBeatmap, MapPrologue(_sortedBeatmapDataItems));
        }

        // Adds `remix` to the remixed beatmap.
        public void AddRemix(Remix remix)
        {
            foreach (var slice in remix.Slices)
            {
                AddBeatmapDataItemsInOrder(_remixedBeatmap, SliceMap(slice.Clock, slice.Start, slice.Duration));
                AddBeatmapDataItemsInOrder(_remixedBeatmap, SliceObstacles(slice.Clock, slice.Start, slice.Duration));
            }
        }

        private static (IEnumerable<BeatmapDataItem>, IEnumerable<ObstacleData>) FilterAndSortBeatmapDataItems(IEnumerable<BeatmapDataItem> beatmapDataItems)
        {
            var omittedItemTypes = new SortedSet<string>();
            var keptBeatmapDataItems = new LinkedList<BeatmapDataItem>();
            var obstacleDataItems = new LinkedList<ObstacleData>();
            foreach (var item in beatmapDataItems.OrderBy(item => item))
            {
                if (item is BPMChangeBeatmapEventData ||
                    item is BasicBeatmapEventData)
                {
                    keptBeatmapDataItems.AddLast(item);
                }
                else if (item is NoteData noteData)
                {
                    if (noteData.gameplayType == NoteData.GameplayType.Normal ||
                        noteData.gameplayType == NoteData.GameplayType.Bomb)
                    {
                        keptBeatmapDataItems.AddLast(item);
                    }
                    else
                    {
                        omittedItemTypes.Add($"{noteData.GetType().Name}, gameplayType: {noteData.gameplayType}");
                    }
                }
                else if (item is ObstacleData obstacleData)
                {
                    // `ObstacleData` is handled specially because, unlike other
                    // beatmap data items, it has a `duration`. It might be
                    // appropriate to include in a slice even though it begins
                    // before the slice's start time. Due to the need to handle
                    // obstacles specially, they get put into their own list,
                    // `obstacleDataItems`, rather than into
                    // `keptBeatmapDataItems`.
                    obstacleDataItems.AddLast(obstacleData);
                }
                else
                {
                    omittedItemTypes.Add(item.GetType().Name);
                }
            }

            if (omittedItemTypes.Count > 0)
            {
                var output = new StringBuilder();
                output.AppendLine("BeatmapRemixer.FilterBeatmapDataItems: Ignoring unsupported beatmap item types:");
                foreach (var item in omittedItemTypes)
                {
                    output.AppendLine($"  {item}");
                }
                Plugin.Log.Info(output.ToString());
            }
            else
            {
                Plugin.Log.Info("BeatmapRemixer.FilterBeatmapDataItems: All beatmap item types are supported.");
            }

            return (keptBeatmapDataItems, obstacleDataItems);
        }

        private static void SetTime(BeatmapDataItem item, float time)
        {
            var fieldInfo = typeof(BeatmapDataItem).GetField("<time>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(item, time);
        }

        private static void SetDuration(ObstacleData obstacleData, float duration)
        {
            var fieldInfo = typeof(ObstacleData).GetField("<duration>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(obstacleData, duration);
        }

        private static IEnumerable<BeatmapDataItem> MapPrologue(IEnumerable<BeatmapDataItem> beatmapDataItems)
        {
            // TODO: There seems to be a BPMChangeBeatmapEventData at time -100. Questions:
            //   - Are there other events that come before 0?
            //   - Are there times when we wouldn't want to include such events here?
            //   - Are there times when we should be including events that have a time >= 0?
            return beatmapDataItems
                .TakeWhile(item => IsFloatGreater(0, item.time));
        }

        private IEnumerable<BeatmapDataItem> SliceMap(double clock, double start, double duration)
        {
            var end = start + duration;
            return _sortedBeatmapDataItems
                .SkipWhile(item => IsFloatGreater(start, item.time))
                .TakeWhile(item => IsFloatGreater(end, item.time))
                .Select(item =>
                {
                    var origTime = item.time;
                    var newTime = origTime - start + clock;
                    //var delta = Math.Abs(origTime - newTime);
                    var result = item.GetCopy();
                    SetTime(result, (float)newTime);
                    return result;
                });
        }

        private IEnumerable<BeatmapDataItem> SliceObstacles(double clock, double start, double duration)
        {
            var sliceRange = new Range(start, start + duration);

            var result = new List<BeatmapDataItem>();
            foreach (var obstacleData in _sortedObstacleDataItems)
            {
                if (IsFloatGreaterOrEqual(obstacleData.time, sliceRange.End))
                {
                    // All subsequent obstacles are further in the song than the requested slice.
                    break;
                }

                var obstacleRange = new Range(obstacleData.time, obstacleData.time + obstacleData.duration);
                if (sliceRange.TryIntersection(obstacleRange, out var rangeIntersection))
                {
                    var newStart = rangeIntersection.Start - start + clock;
                    var newDuration = rangeIntersection.End - rangeIntersection.Start;
                    var newObstacle = (ObstacleData)obstacleData.GetCopy();
                    SetTime(newObstacle, (float)newStart);
                    SetDuration(newObstacle, (float)newDuration);
                    result.Add(newObstacle);
                }
            }
            return result;
        }

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

        private static void AddBeatmapDataItemsInOrder(BeatmapData map, IEnumerable<BeatmapDataItem> items)
        {
            foreach (var item in items)
            {
                AddBeatmapDataItemInOrder(map, item);
            }
        }
    }
}
