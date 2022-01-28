using System;
using System.Collections.Generic;

namespace Singulink.Collections
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the count if the source is an <see cref="ICollection{T}"/>, otherwise <see langword="null"/> is returned.
        /// </summary>
        public static int? KnownCount<T>(this IEnumerable<T> source)
        {
            if (source is ICollection<T> collection)
                return collection.Count;

            return null;
        }
    }
}