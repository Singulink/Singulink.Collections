using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Utilities;

[StackTraceHidden]
internal static class Throw
{
    [DoesNotReturn]
    public static void Arg(string message) => throw new ArgumentException(message);

    [DoesNotReturn]
    public static void ArgOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    public static void IfEnumerationCollectionChanged(int enumeratorVersion, int collectionVersion)
    {
        if (collectionVersion != enumeratorVersion)
            EnumerationCollectionChanged();
    }

    [DoesNotReturn]
    public static void EnumerationCollectionChanged() => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
}