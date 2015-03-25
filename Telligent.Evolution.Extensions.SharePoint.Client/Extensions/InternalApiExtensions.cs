using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    [Flags]
    internal enum CompareStatus
    {
        IsEqual = 0,
        HasNew = 1,
        HasUpdates = 2,
        HasDeleted = 4
    }

    internal static class InternalApiExtensions
    {
        internal class ItemBaseComparer : IEqualityComparer<ItemBase>
        {
            public bool Equals(ItemBase x, ItemBase y)
            {
                return x.UniqueId == y.UniqueId;
            }

            public int GetHashCode(ItemBase obj)
            {
                return obj.UniqueId.GetHashCode();
            }
        }

        internal class ItemBaseUpdateComparer : IEqualityComparer<ItemBase>
        {
            public bool Equals(ItemBase x, ItemBase y)
            {
                return x.UniqueId == y.UniqueId && x.ModifiedDate < y.ModifiedDate;
            }

            public int GetHashCode(ItemBase obj)
            {
                return obj.UniqueId.GetHashCode();
            }
        }

        public static CompareStatus Compare(this List<ItemBase> source, List<ItemBase> target)
        {
            CompareStatus? status = null;
            var intersected = source.Intersect(target, new ItemBaseComparer()).ToList();
            var intersectedWithUpdates = intersected.Intersect(target, new ItemBaseUpdateComparer()).ToList();

            if (source.Count == target.Count && source.Count == intersectedWithUpdates.Count)
            {
                return CompareStatus.IsEqual;
            }

            if (source.Count != target.Count && intersected.Count() == source.Count)
            {
                status = CompareStatus.HasNew;
            }

            if (source.Count != target.Count && intersected.Count() == target.Count)
            {
                status = (status == null) ? CompareStatus.HasDeleted : status | CompareStatus.HasDeleted;
            }
            
            if (intersectedWithUpdates.Count() != intersected.Count())
            {
                status = (status == null) ? CompareStatus.HasUpdates : status | CompareStatus.HasUpdates;
            }

            if (source.Count != target.Count && !intersected.Any() && !intersectedWithUpdates.Any() && status == null)
            {
                status = CompareStatus.HasNew | CompareStatus.HasDeleted;
            }
            
            return status.GetValueOrDefault();
        }

        public static List<ItemBase> GetItemsToAdd(this IEnumerable<ItemBase> source, IEnumerable<ItemBase> target)
        {
            return target.Except(source, new ItemBaseComparer()).ToList();
        }

        public static List<ItemBase> GetItemsToUpdate(this IEnumerable<ItemBase> source, IEnumerable<ItemBase> target)
        {
            return source.Except(target, new ItemBaseUpdateComparer()).ToList();
        }

        public static List<ItemBase> GetItemsToDelete(this IEnumerable<ItemBase> source, List<ItemBase> target)
        {
            var sourceList = source.ToList();
            return sourceList.Except(sourceList.Intersect(target, new ItemBaseComparer()), new ItemBaseComparer()).ToList();
        }
    }
}
