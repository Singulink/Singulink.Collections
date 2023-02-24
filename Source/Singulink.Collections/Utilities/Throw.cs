using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Utilities;

internal static class Throw
{
    [DoesNotReturn]
    public static void ArgOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
    public static void Arg(string message) => throw new ArgumentException(message);

    public static void IfEnumeratedCollectionChanged(int enumeratorVersion, int collectionVersion)
    {
        if (collectionVersion != enumeratorVersion)
            EnumerationCollectionChanged();
    }

    [DoesNotReturn]
    public static void EnumerationCollectionChanged() => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
}