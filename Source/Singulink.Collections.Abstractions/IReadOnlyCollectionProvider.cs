using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Collections;

/// <summary>
/// Allows a collection to return a read-only wrapper. This interface should be implemented by the value collections in collection dictionaries to
/// facilitate full read-only wrapping of not just the dictionary but also the collections it contains.
/// </summary>
public interface IReadOnlyCollectionProvider<T>
{
    /// <summary>
    /// Returns a read-only collection wrapper for the given collection.
    /// </summary>
    public IReadOnlyCollection<T> GetReadOnlyCollection();
}