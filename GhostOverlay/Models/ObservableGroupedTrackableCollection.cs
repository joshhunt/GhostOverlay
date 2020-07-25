using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using ColorCode.Common;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;

namespace GhostOverlay.Models
{
    public static class ObservableGroupedCollectionTrackableExtensions
    {
        public static void AddTrackable<TKey>(
            this ObservableGroupedCollection<TKey, ITrackable> source,
            TKey key,
            ITrackable item)
        {
            var group = source.FirstOrDefault(key);

            if (group is null)
            {
                group = new ObservableGroup<TKey, ITrackable>(key);
                source.Add(group);
            }

            var existingItem = group.FirstOrDefault(v => v.TrackedEntry.UniqueKey == item.TrackedEntry.UniqueKey);

            if (existingItem is null)
            {
                group.Add(item);
            }
            else
            {
                // TODO: investigating what replacing the item looks like
                existingItem.UpdateTo(item);
            }
        }

        public static void SetTrackables(
            this ObservableGroupedCollection<string, ITrackable> source,
            List<ITrackable> trackables
        )
        {
            var keys = new List<string>();

            foreach (var trackable in trackables)
            {
                keys.Add(trackable.TrackedEntry.UniqueKey);
                source.AddTrackable(trackable.GroupByKey, trackable);
            }

            var toRemove = (
                from @group in source
                from trackable in @group
                where !keys.Contains(trackable.TrackedEntry.UniqueKey)
                select trackable).ToList();

            foreach (var trackable in toRemove)
                source.RemoveItem(trackable.GroupByKey, trackable);

            foreach (var group in source)
                group.SortTrackables();
        }

        public static void SortTrackables(this ObservableGroup<string, ITrackable> group)
        {
            QuickSort(group, 0, group.Count - 1 );
        }

        private static void QuickSort(IList<ITrackable> arr, int start, int end)
        {
            if (start >= end) return;

            var i = Partition(arr, start, end);

            QuickSort(arr, start, i - 1);
            QuickSort(arr, i + 1, end);
        }

        private static int Partition(IList<ITrackable> arr, int start, int end)
        {
            ITrackable temp;
            var p = arr[end];
            var i = start - 1;

            for (var j = start; j <= end - 1; j++)
            {
                var comparison = string.CompareOrdinal(arr[j].SortValue, p.SortValue);
                if (comparison > 0) continue;

                i++;
                temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }

            temp = arr[i + 1];
            arr[i + 1] = arr[end];
            arr[end] = temp;
            return i + 1;
        }
    }
}