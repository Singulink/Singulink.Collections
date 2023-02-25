using Singulink.Collections.Internal;

namespace Singulink.Collections;

/// <summary>
/// Provides extension methods for collection dictionaries.
/// </summary>
public static class CollectionDictionaryExtensions
{
    #region Collection Dictionaries

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a collection dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this ICollectionDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is CollectionDictionary<TKey, TValue, IList<TValue>> ld)
            return new ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>>(ld.WrappedDictionary);

        if (dictionary is CollectionDictionary<TKey, TValue, ISet<TValue>> sd)
            return new ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>>(sd.WrappedDictionary);

        return new ReadOnlyCollectionDictionary<TKey, TValue, ICollection<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyCollection{TValue}"/> values) wrapper for a collection dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnlyDictionaryOfCollection<TKey, TValue>(this IReadOnlyCollectionDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> d)
            return d;

        return new ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlyCollection<TValue>>(dictionary);
    }

    #endregion

    #region List Dictionaries

    //------------------------------
    // Editable to Editable Wrappers
    // ------------------------------

    /// <summary>
    /// Returns an <see cref="ICollectionDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static ICollectionDictionary<TKey, TValue> AsCollectionDictionary<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionary<TKey, TValue, IList<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="ICollection{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, ICollection<TValue>> AsDictionaryOfCollection<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionary<TKey, TValue, IList<TValue>>(dictionary);
    }

    //-------------------------------
    // Editable to Read-Only Wrappers
    // ------------------------------

    /// <summary>
    /// Returns an <see cref="IReadOnlyListDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyListDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyListDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyList{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> AsReadOnlyDictionaryOfList<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyListDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyCollection{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnlyDictionaryOfCollection<TKey, TValue>(this IListDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>>(dictionary);
    }

    //--------------------------------
    // Read-Only to Read-Only Wrappers
    // -------------------------------

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is ReadOnlyListDictionary<TKey, TValue> d)
            return new ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>>(d.WrappedDictionary);

        return new ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlyList<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyList{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> AsReadOnlyDictionaryOfList<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> d)
            return d;

        return new ReadOnlyListDictionaryWrapper<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyCollection{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnlyDictionaryOfCollection<TKey, TValue>(this IReadOnlyListDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is ReadOnlyListDictionary<TKey, TValue> d)
            return new ReadOnlyCollectionDictionary<TKey, TValue, IList<TValue>>(d.WrappedDictionary);

        return new ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlyList<TValue>>(dictionary);
    }

    #endregion

    #region Set Dictionaries

    //------------------------------
    // Editable to Editable Wrappers
    // ------------------------------

    /// <summary>
    /// Returns an <see cref="ICollectionDictionary{TKey, TValue}"/> wrapper for a list dictionary.
    /// </summary>
    public static ICollectionDictionary<TKey, TValue> AsCollectionDictionary<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionary<TKey, TValue, ISet<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="ICollection{TValue}"/> values) wrapper for a list dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, ICollection<TValue>> AsDictionaryOfCollection<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new CollectionDictionary<TKey, TValue, ISet<TValue>>(dictionary);
    }

    //-------------------------------
    // Editable to Read-Only Wrappers
    // ------------------------------

    /// <summary>
    /// Returns an <see cref="IReadOnlySetDictionary{TKey, TValue}"/> wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlySetDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlySetDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlySet{TValue}"/> values) wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlySet<TValue>> AsReadOnlyDictionaryOfSet<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlySetDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyCollection{TValue}"/> values) wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnlyDictionaryOfCollection<TKey, TValue>(this ISetDictionary<TKey, TValue> dictionary)
    {
        return new ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>>(dictionary);
    }

    //--------------------------------
    // Read-Only to Read-Only Wrappers
    // -------------------------------

    /// <summary>
    /// Returns an <see cref="IReadOnlyCollectionDictionary{TKey, TValue}"/> wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyCollectionDictionary<TKey, TValue> AsReadOnlyCollectionDictionary<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is ReadOnlySetDictionary<TKey, TValue> d)
            return new ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>>(d.WrappedDictionary);

        return new ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlySet<TValue>>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlySet{TValue}"/> values) wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlySet<TValue>> AsReadOnlyDictionaryOfSet<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is IReadOnlyDictionary<TKey, IReadOnlySet<TValue>> d)
            return d;

        return new ReadOnlySetDictionaryWrapper<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Returns an <see cref="IReadOnlyDictionary{TKey, TValue}"/> (with <see cref="IReadOnlyCollection{TValue}"/> values) wrapper for a set dictionary.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnlyDictionaryOfCollection<TKey, TValue>(this IReadOnlySetDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is ReadOnlySetDictionary<TKey, TValue> d)
            return new ReadOnlyCollectionDictionary<TKey, TValue, ISet<TValue>>(d.WrappedDictionary);

        return new ReadOnlyCollectionDictionaryWrapper<TKey, TValue, IReadOnlySet<TValue>>(dictionary);
    }

    #endregion
}