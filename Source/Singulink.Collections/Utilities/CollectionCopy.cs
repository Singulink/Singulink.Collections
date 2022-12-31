using System;
using System.Collections.Generic;

namespace Singulink.Collections.Utilities;

internal static class CollectionCopy
{
    public static void CheckParams<TDestination>(int sourceCount, TDestination[] array, int arrayIndex)
    {
        if ((uint)arrayIndex > array.Length)
            Throw.ArgOutOfRange(nameof(arrayIndex));

        if (array.Length - arrayIndex < sourceCount)
            ThrowNotLongEnough();

        static void ThrowNotLongEnough() => throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
    }
}