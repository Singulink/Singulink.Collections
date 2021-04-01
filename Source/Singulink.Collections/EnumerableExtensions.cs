using System;
using System.Collections.Generic;

namespace Singulink.Collections
{
    public static class EnumerableExtensions
    {
        public static int? KnownCount<T>(this IEnumerable<T> source)
        {
            if (source is ICollection<T> genericCollection)
                return genericCollection.Count;

            return null;
        }
    }
}
