#if !NET7_0_OR_GREATER
using Microsoft.CodeAnalysis;

namespace Singulink.Collections;

// Before .NET 7, nint does not expose the static members of IntPtr, so supply the ones this code uses.
[Embedded]
internal static class NativeIntegerPolyfills
{
    extension(nint)
    {
        public static nint Zero => 0;

        public static int Size => IntPtr.Size;
    }
}
#endif
