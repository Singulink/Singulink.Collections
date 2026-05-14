using Singulink.Collections.Internal;

namespace Singulink.Collections;

/// <summary>
/// Provides extension methods for collection dictionaries.
/// </summary>
public static class CollectionDictionaryExtensions
{
    #region AsReadOnly

    /// <summary>
    /// Returns a true read-only <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a collection dictionary. The returned wrapper cannot be
    /// downcast back to a mutable collection dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this ICollectionDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionaryAdapter<TKey, TValue, IKeyedCollection<TKey, TValue>>(dictionary);
    }

    /// <summary>
    /// Returns a true read-only <see cref="IReadOnlyListDictionary{TKey, TValue}"/> wrapper for a list dictionary. The returned wrapper cannot be downcast
    /// back to a mutable list dictionary.
    /// </summary>
    public static IReadOnlyListDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyListDictionaryAdapter<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns a true read-only <see cref="IReadOnlySetDictionary{TKey, TValue}"/> wrapper for a set dictionary. The returned wrapper cannot be downcast back
    /// to a mutable set dictionary.
    /// </summary>
    public static IReadOnlySetDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlySetDictionaryAdapter<TKey, TValue>(dictionary);
    }

    #endregion

    #region Widening Wrappers

    /// <summary>
    /// Returns an <see cref="ICollectionDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static ICollectionDictionary<TKey, TValue> AsCollectionDictionary<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionaryAdapter<TKey, TValue, IKeyedList<TKey, TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="ICollectionDictionary{TKey, TValue}"/> wrapper for a set dictionary.
    /// </summary>
    public static ICollectionDictionary<TKey, TValue> AsCollectionDictionary<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionaryAdapter<TKey, TValue, IKeyedSet<TKey, TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a read-only list dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionaryAdapter<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a read-only set dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionaryAdapter<TKey, TValue, IReadOnlyKeyedSet<TKey, TValue>>(dictionary);
    }

    #endregion

    #region ILookup Integration

    /// <summary>
    /// Returns a live <see cref="ILookup{TKey, TElement}"/> view of the collection dictionary. The view reflects changes made to the underlying dictionary.
    /// </summary>
    public static ILookup<TKey, TValue> AsLookup<TKey, TValue>(this IReadOnlyCollectionDictionary<TKey, TValue> dictionary)
    {
        return new LookupAdapter<TKey, TValue, IReadOnlyKeyedCollection<TKey, TValue>>(dictionary);
    }

    /// <inheritdoc cref="AsLookup{TKey, TValue}(IReadOnlyCollectionDictionary{TKey, TValue})"/>
    public static ILookup<TKey, TValue> AsLookup<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        return new LookupAdapter<TKey, TValue, IReadOnlyKeyedList<TKey, TValue>>(dictionary);
    }

    /// <inheritdoc cref="AsLookup{TKey, TValue}(IReadOnlyCollectionDictionary{TKey, TValue})"/>
    public static ILookup<TKey, TValue> AsLookup<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        return new LookupAdapter<TKey, TValue, IReadOnlyKeyedSet<TKey, TValue>>(dictionary);
    }

    /// <summary>
    /// Creates a snapshot <see cref="ILookup{TKey, TElement}"/> by copying the current contents of the collection dictionary. Subsequent changes to the
    /// dictionary are not reflected in the returned lookup.
    /// </summary>
    /// <param name="dictionary">The dictionary to snapshot.</param>
    /// <param name="keyComparer">The key comparer to use for the returned lookup, or <see langword="null"/> to use the default comparer. Note: this does
    /// <em>not</em> default to any comparer the source dictionary may be using internally.</param>
    public static ILookup<TKey, TValue> ToLookup<TKey, TValue>(this IReadOnlyCollectionDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
    {
        return new SnapshotLookup<TKey, TValue>(dictionary.ValueCollections, keyComparer);
    }

    /// <inheritdoc cref="ToLookup{TKey, TValue}(IReadOnlyCollectionDictionary{TKey, TValue}, IEqualityComparer{TKey})"/>
    public static ILookup<TKey, TValue> ToLookup<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
    {
        return new SnapshotLookup<TKey, TValue>(dictionary.ValueCollections, keyComparer);
    }

    /// <inheritdoc cref="ToLookup{TKey, TValue}(IReadOnlyCollectionDictionary{TKey, TValue}, IEqualityComparer{TKey})"/>
    public static ILookup<TKey, TValue> ToLookup<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
    {
        return new SnapshotLookup<TKey, TValue>(dictionary.ValueCollections, keyComparer);
    }

    #endregion
}

