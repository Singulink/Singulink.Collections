using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Singulink.Collections.Utilities;

[StackTraceHidden]
internal static class Throw
{
    [DoesNotReturn]
    public static void Arg(string message, string paramName) => throw new ArgumentException(message, paramName);

    [DoesNotReturn]
    public static void ArgOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    [DoesNotReturn]
    public static void IndexOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName, "Index is out of range.");

    [DoesNotReturn]
    public static void ItemNotFound(string paramName) => throw new ArgumentException("The specified item was not found.", paramName);

    [DoesNotReturn]
    public static void KeyNotFound() => throw new KeyNotFoundException();

    [DoesNotReturn]
    public static void InvalidEnumeration()
    {
        throw new InvalidOperationException(
            "Enumeration has not started or has already finished; please call MoveNext or MovePrevious to begin a new enumeration.");
    }

    public static void IfDisposed([DoesNotReturnIf(true)] bool condition, Type type)
    {
        if (condition)
        {
            [StackTraceHidden]
            static void Throw(Type type) => throw new ObjectDisposedException(type.FullName);
            Throw(type);
        }
    }
}
