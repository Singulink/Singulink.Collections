namespace Singulink.Collections.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// Yields the specified items and then throws an <see cref="InvalidOperationException"/>, to simulate a source that fails part way through enumeration.
    /// </summary>
    public static IEnumerable<T> ThenThrow<T>(this IEnumerable<T> items)
    {
        foreach (var item in items)
            yield return item;

        throw new InvalidOperationException("Enumeration failed.");
    }
}
