using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Singulink.Collections
{
    /// <summary>
    /// Provides a read-only wrapper around a <see cref="List{T}"/>.
    /// </summary>
    public class ReadOnlyList<T> : IList<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// Gets an empty read-only list.
        /// </summary>
        public static ReadOnlyList<T> Empty { get; } = new ReadOnlyList<T>(new List<T>());

        private readonly List<T> _list;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyList{T}"/> class.
        /// </summary>
        /// <param name="list">The list to wrap.</param>
        public ReadOnlyList(List<T> list)
        {
            _list = list;
        }

        /// <inheritdoc/>
        public T this[int index] => _list[index];

        /// <inheritdoc/>
        T IList<T>.this[int index] {
            get => _list[index];
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.Capacity"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.Capacity"/>
        public int Capacity {
            get => _list.Capacity;
            set => _list.Capacity = value;
        }

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.Count"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.Count"/>
        public int Count => _list.Count;

        /// <inheritdoc/>
        bool ICollection<T>.IsReadOnly => true;

        internal List<T> WrappedList => _list;

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.BinarySearch(int, int, T, IComparer{T}?)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.BinarySearch(int, int, T, IComparer{T}?)"/>
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer) => _list.BinarySearch(index, count, item, comparer);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.BinarySearch(T)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.BinarySearch(T)"/>
        public int BinarySearch(T item) => _list.BinarySearch(item);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T}?)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T}?)"/>
        public int BinarySearch(T item, IComparer<T>? comparer) => _list.BinarySearch(item, comparer);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.Contains(T)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.Contains(T)"/>
        public bool Contains(T item) => _list.Contains(item);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.ConvertAll{TOutput}(Converter{T, TOutput})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.ConvertAll{TOutput}(Converter{T, TOutput})"/>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => _list.ConvertAll(converter);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.CopyTo(T[])"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.CopyTo(T[])"/>
        public void CopyTo(T[] array) => _list.CopyTo(array);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.CopyTo(T[], int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.CopyTo(T[], int)"/>
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.CopyTo(int, T[], int, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.CopyTo(int, T[], int, int)"/>
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => CopyTo(index, array, arrayIndex, count);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.Exists(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.Exists(Predicate{T})"/>
        public bool Exists(Predicate<T> match) => _list.Exists(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.Find(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.Find(Predicate{T})"/>
        public T? Find(Predicate<T> match) => _list.Find(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindAll(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindAll(Predicate{T})"/>
        public List<T> FindAll(Predicate<T> match) => _list.FindAll(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})"/>
        public int FindIndex(int startIndex, int count, Predicate<T> match) => _list.FindIndex(startIndex, count, match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})"/>
        public int FindIndex(int startIndex, Predicate<T> match) => _list.FindIndex(startIndex, match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})"/>
        public int FindIndex(Predicate<T> match) => _list.FindIndex(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindLast(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindLast(Predicate{T})"/>
        public T? FindLast(Predicate<T> match) => _list.FindLast(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _list.FindLastIndex(startIndex, count, match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, Predicate<T> match) => _list.FindLastIndex(startIndex, match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})"/>
        public int FindLastIndex(Predicate<T> match) => _list.FindLastIndex(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
        public void ForEach(Action<T> action) => _list.ForEach(action);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.GetRange(int, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.GetRange(int, int)"/>
        public List<T> GetRange(int index, int count) => _list.GetRange(index, count);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.IndexOf(T)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.IndexOf(T)"/>
        public int IndexOf(T item) => _list.IndexOf(item);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.IndexOf(T, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.IndexOf(T, int)"/>
        public int IndexOf(T item, int index) => _list.IndexOf(item, index);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.IndexOf(T, int, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.IndexOf(T, int, int)"/>
        public int IndexOf(T item, int index, int count) => _list.IndexOf(item, index, count);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.LastIndexOf(T)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.LastIndexOf(T)"/>
        public int LastIndexOf(T item) => _list.LastIndexOf(item);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int)"/>
        public int LastIndexOf(T item, int index) => _list.LastIndexOf(item, index);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int, int)"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int, int)"/>
        public int LastIndexOf(T item, int index, int count) => _list.LastIndexOf(item, index, count);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.ToArray"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.ToArray"/>
        public T[] ToArray() => _list.ToArray();

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.TrimExcess"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.TrimExcess"/>
        public void TrimExcess() => _list.TrimExcess();

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.TrueForAll(Predicate{T})"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.TrueForAll(Predicate{T})"/>
        public bool TrueForAll(Predicate<T> match) => _list.TrueForAll(match);

        /// <summary>
        /// (<see cref="List{T}"/> wrapper)
        /// <inheritdoc cref="List{T}.GetEnumerator"/>
        /// </summary>
        /// <inheritdoc cref="List{T}.GetEnumerator"/>
        public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Not supported.
        /// </summary>
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        void ICollection<T>.Clear() => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        /// <summary>
        /// Not supported.
        /// </summary>
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
    }
}