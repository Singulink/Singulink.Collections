namespace Singulink.Collections.Utilities;

internal static class Runtime
{
    public static readonly bool IsNet8OrHigher = Environment.Version.Major >= 8;
}