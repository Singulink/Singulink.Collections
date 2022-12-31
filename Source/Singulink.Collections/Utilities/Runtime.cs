using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Collections.Utilities;

#pragma warning disable SA1310 // Field names should not contain underscore
internal static class Runtime
{
    public static readonly bool NET8_OR_HIGHER = Environment.Version.Major >= 8;
}