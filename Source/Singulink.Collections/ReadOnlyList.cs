using System.Collections;

namespace Singulink.Collections;

/// <summary>
/// Provides a read-only wrapper around a <see cref="List{T}"/>.
/// </summary>
public class ReadOnlyList<T> : IList<T>, IReadOnlyList<T>
{
    /// <summary>
    /// Gets an empty read-only list.
    /// </summary>
    public static ReadOnlyList<T> Empty { get; } = new ReadOnlyList<T>([]);

    private List<T> _list;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyList{T}"/> class.
    /// </summary>
    /// <param name="list">The list to wrap.</param>
    public ReadOnlyList(List<T> list)
    {
        _list = list;
    }

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    public T this[int index] => _list[index];

    /// <summary>
    /// Gets the number of items contained in the list.
    /// </summary>
    public int Count => _list.Count;

    internal List<T> WrappedList
    {
        get => _list;
        set => _list = value;
    }

    /// <summary>
    /// Searches a range of items in the sorted list and returns the index of the item.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise
    /// complement of the index of the next item that is larger than <paramref name="item"/> or, if there is no larger item, the bitwise complement of
    /// <see cref="Count"/>.
    /// </returns>
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer) => _list.BinarySearch(index, count, item, comparer);

    /// <summary>
    /// Searches the items in the sorted list and returns the index of the item.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise
    /// complement of the index of the next item that is larger than <paramref name="item"/> or, if there is no larger item, the bitwise complement of
    /// <see cref="Count"/>.
    /// </returns>
    public int BinarySearch(T item) => _list.BinarySearch(item);

    /// <summary>
    /// Searches the items in the sorted list and returns the index of the item.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="item"/> in the sorted list, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise
    /// complement of the index of the next item that is larger than <paramref name="item"/> or, if there is no larger item, the bitwise complement of
    /// <see cref="Count"/>.
    /// </returns>
    public int BinarySearch(T item, IComparer<T>? comparer) => _list.BinarySearch(item, comparer);

    /// <summary>
    /// Determines whether an item is in the list.
    /// </summary>
    public bool Contains(T item) => _list.Contains(item);

    /// <summary>
    /// Converts the items in the current list to another type, and returns a list containing the converted items.
    /// </summary>
    public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => _list.ConvertAll(converter);

    /// <summary>
    /// Copies the list to an array.
    /// </summary>
    public void CopyTo(T[] array) => _list.CopyTo(array);

    /// <summary>
    /// Copies the list to an array starting at the specified array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    /// <summary>
    /// Copies a range of items from the list to an array starting at the specified array index.
    /// </summary>
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => _list.CopyTo(index, array, arrayIndex, count);

    /// <summary>
    /// Determines whether the list contains an item that matches the condition specified by the specified predicate.
    /// </summary>
    public bool Exists(Predicate<T> match) => _list.Exists(match);

    /// <summary>
    /// Searches the list for an item that matches the conditions in the specified predicate and returns the first matching item.
    /// </summary>
    public T? Find(Predicate<T> match) => _list.Find(match);

    /// <summary>
    /// Returns all the items in the list that match the condition in the specified predicate.
    /// </summary>
    public List<T> FindAll(Predicate<T> match) => _list.FindAll(match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within the range
    /// of items in the list that starts at the specified index and contains the specified number of items.
    /// </summary>
    public int FindIndex(int startIndex, int count, Predicate<T> match) => _list.FindIndex(startIndex, count, match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within
    /// the range of items in the list that extends from the specified index to the last item.
    /// </summary>
    public int FindIndex(int startIndex, Predicate<T> match) => _list.FindIndex(startIndex, match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the first occurrence within the entire
    /// list.
    /// </summary>
    public int FindIndex(Predicate<T> match) => _list.FindIndex(match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the last occurrence within the entire list.
    /// </summary>
    public T? FindLast(Predicate<T> match) => _list.FindLast(match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the range of
    /// items in the list that contains the specified number of items and ends at the specified index.
    /// </summary>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _list.FindLastIndex(startIndex, count, match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the range of
    /// items in the list that extends from the first item to the specified index.
    /// </summary>
    public int FindLastIndex(int startIndex, Predicate<T> match) => _list.FindLastIndex(startIndex, match);

    /// <summary>
    /// Searches for an item that matches the conditions defined by the specified predicate, and returns the index of the last occurrence within the entire
    /// list.
    /// </summary>
    public int FindLastIndex(Predicate<T> match) => _list.FindLastIndex(match);

    /// <summary>
    /// Performs the specified action on each item of the list.
    /// </summary>
    public void ForEach(Action<T> action) => _list.ForEach(action);

    /// <summary>
    /// Creates a shallow copy of a range of items in the source list.
    /// </summary>
    public List<T> GetRange(int index, int count) => _list.GetRange(index, count);

    /// <summary>
    /// Searches for the specified object and returns the index of the first occurrence within the entire list.
    /// </summary>
    public int IndexOf(T item) => _list.IndexOf(item);

    /// <summary>
    /// Searches for the specified object and returns the index of the first occurrence within the range of items in the list that extends from the specified
    /// index to the last item.
    /// </summary>
    public int IndexOf(T item, int index) => _list.IndexOf(item, index);

    /// <summary>
    /// Searches for the specified object and returns the index of the first occurrence within the range of items in the list that starts at the specified
    /// index and contains the specified number of items.
    /// </summary>
    public int IndexOf(T item, int index, int count) => _list.IndexOf(item, index, count);

    /// <summary>
    /// Searches for the specified object and returns the index of the last occurrence within the entire list.
    /// </summary>
    public int LastIndexOf(T item) => _list.LastIndexOf(item);

    /// <summary>
    /// Searches for the specified object and returns the index of the last occurrence within the range of items in the list that extends from the first
    /// item to the specified index.
    /// </summary>
    public int LastIndexOf(T item, int index) => _list.LastIndexOf(item, index);

    /// <summary>
    /// Searches for the specified object and returns the index of the last occurrence within the range of items in the list that contains the specified
    /// number of items and ends at the specified index.
    /// </summary>
    public int LastIndexOf(T item, int index, int count) => _list.LastIndexOf(item, index, count);

    /// <summary>
    /// Copies the items of the list to a new array.
    /// </summary>
    public T[] ToArray() => _list.ToArray();

    /// <summary>
    /// Determines whether every item in the list matches the conditions defined by the specified predicate.
    /// </summary>
    public bool TrueForAll(Predicate<T> match) => _list.TrueForAll(match);

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();

    #region Explicit Interface Implementations

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <exception cref="NotSupportedException">The setter is not supported.</exception>
    T IList<T>.this[int index]
    {
        get => _list[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value indicating whether the list is read-only. Always returns <see langword="true"/>.
    /// </summary>
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="GetEnumerator"/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Not Supported

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void ICollection<T>.Clear() => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    #endregion
}