using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InfiniteBeatSaber
{
    internal class BeatmapRemixer
    {
        private readonly IReadonlyBeatmapData _originalBeatmap;
        private readonly BeatmapData _remixedBeatmap;

        public BeatmapRemixer(IReadonlyBeatmapData beatmap, BeatmapData remixedBeatmap)
        {
            _originalBeatmap = beatmap;
            _remixedBeatmap = remixedBeatmap;

            AddBeatmapDataItemsInOrder(_remixedBeatmap, MapPrologue(_originalBeatmap));
        }

        // Adds `remix` to the remixed beatmap.
        public void AddRemix(Remix remix)
        {
            foreach (var slice in remix.Slices)
            {
                AddBeatmapDataItemsInOrder(_remixedBeatmap, SliceMap(_originalBeatmap, slice.Clock, slice.Start, slice.Duration));
            }
        }

        private static bool AreFloatsEqual(double a, double b)
        {
            return Math.Abs(a - b) <= 0.001;
        }

        private static bool IsFloatGreater(double a, double b)
        {
            return a > b && !AreFloatsEqual(a, b);
        }

        private static bool ShouldKeepBeetmapDataItem(BeatmapDataItem item)
        {
            return
                item is BPMChangeBeatmapEventData ||
                (item is NoteData noteData && noteData.gameplayType == NoteData.GameplayType.Normal);
        }

        private static void SetTime(BeatmapDataItem item, float time)
        {
            var fieldInfo = typeof(BeatmapDataItem).GetField("<time>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(item, time);
        }

        private static IEnumerable<BeatmapDataItem> MapPrologue(IReadonlyBeatmapData beatmap)
        {
            // TODO: There seems to be a BPMChangeBeatmapEventData at time -100. Questions:
            //   - Are there other events that come before 0?
            //   - Are there times when we wouldn't want to include such events here?
            //   - Are there times when we should be including events that have a time >= 0?
            return beatmap.allBeatmapDataItems
                .TakeWhile(item => IsFloatGreater(0, item.time))
                .Where(ShouldKeepBeetmapDataItem);
        }

        private static IEnumerable<BeatmapDataItem> SliceMap(IReadonlyBeatmapData beatmap, double clock, double start, double duration)
        {
            var end = start + duration;
            return beatmap.allBeatmapDataItems
                .SkipWhile(item => IsFloatGreater(start, item.time))
                .TakeWhile(item => IsFloatGreater(end, item.time))
                .Where(ShouldKeepBeetmapDataItem)
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
