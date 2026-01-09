#if !NET8_0_OR_GREATER
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Singulink.Collections;

[Embedded]
internal static class ImmutableArrayExtensions
{
    extension(ImmutableArray)
    {
        public static ImmutableArray<T> Create<T>(ReadOnlySpan<T> items)
        {
            return ImmutableCollectionsMarshal.AsImmutableArray(items.ToArray());
        }
    }
}
#endif
