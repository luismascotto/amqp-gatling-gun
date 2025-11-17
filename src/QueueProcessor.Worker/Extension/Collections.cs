using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueProcessor.Worker.Extension;

internal static class Collections
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static int CountSafe<T>(this ICollection<T> collection)
    {
        if (collection is null)
        {
            return 0;
        }

        return collection.Count;
    }
    //public static int CountSafe<T>(this IReadOnlyCollection<T> collection)
    //{
    //    if (collection is null)
    //    {
    //        return 0;
    //    }

    //    return collection.Count;
    //}

    public static bool AnySafe<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is null)
        {
            return false;
        }

        return enumerable.Any();
    }
}
