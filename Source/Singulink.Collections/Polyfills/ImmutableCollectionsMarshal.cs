#if !NET8_0_OR_GREATER && !NETSTANDARD
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace System.Runtime.InteropServices;

[Embedded]
internal static class ImmutableCollectionsMarshal
{
    // Note: this is the recommended & supported way to do this before .NET 8: https://github.com/dotnet/runtime/issues/83141#issuecomment-1460324087.

    public static ImmutableArray<T> AsImmutableArray<T>(T[]? array)
    {
        return Unsafe.As<T[]?, ImmutableArray<T>>(ref array);
    }

    public static T[]? AsArray<T>(ImmutableArray<T> array)
    {
        return Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
    }
}
#endif
