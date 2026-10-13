namespace Singulink.Collections;

/// <summary>
/// Allows a collection to return a read-only wrapper around itself.
/// </summary>
/// <remarks>
/// This interface is implemented by the value collections in collection dictionaries so that a read-only wrapper around a dictionary can also wrap the
/// collections it contains.
/// </remarks>
/// <typeparam name="T">The type of elements in the read-only wrapper.</typeparam>
public interface IReadOnlyCollectionProvider<T>
{
    /// <summary>
    /// Returns a read-only wrapper around this collection.
    /// </summary>
    public IReadOnlyCollection<T> GetReadOnlyCollection();
}