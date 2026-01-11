using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable RCS1242 // Do not pass non-read-only struct by read-only reference

namespace Singulink.Collections;

internal static class EquatableArrayImpl
{
    [DoesNotReturn]
    public static void ThrowReadOnlyException() => throw new NotSupportedException("Collection is read-only.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowReadOnlyException<T>()
    {
        ThrowReadOnlyException();
        return default;
    }
}

/// <summary>
/// Provides the implementation for <see cref="EquatableArray{T}"/>.
/// </summary>
internal struct EquatableArrayImpl<T>
{
    // Stores the hash code, or 0 if not yet calculated.
    // On 64-bit systems, the hash code has 1UL << 32 or'd, which allows the 0 hash code also - whereas on 32-bit 0 is replaced with 1.
    // Note: this field must be accessed carefully to ensure its thread-safety.
    private nint _hashCode;

    // Stores the array - never null.
    // Note: we store the raw array so we can perform volatile operations on it for de-duplicating.
    private T[] _array;

    // Helper to store our (approximate) creation index for de-duplicating wisely:
    // Note: this is not thread-safe, but that's okay - it's just an approximation to help with de-duplication preferring older instances.
    // Declared in EquatableArray<T>: private static nint _staticCreateIndex;
    private nint _createIndex;

    public EquatableArrayImpl(ImmutableArray<T> array, ref nint staticCreateIndex)
    {
        _array = array == default ? [] : ImmutableCollectionsMarshal.AsArray(array)!;
        _createIndex = staticCreateIndex++;
    }

    public readonly ImmutableArray<T> UnderlyingArray => ImmutableCollectionsMarshal.AsImmutableArray(_array);

    public override int GetHashCode()
    {
        // Check if we have already calculated the hash code - if so, return it:
        nint value = Volatile.Read(ref _hashCode);
        if (value != 0)
            return (int)value;

        // Otherwise, calculate it:
        return CalculateHashCode();
    }

    public int GetHashCode(IEqualityComparer<T>? equalityComparer)
    {
        // Check if we have already calculated the hash code - if so, return it:
        nint value = Volatile.Read(ref _hashCode);
        if (value != 0)
            return (int)value;

        // Otherwise, calculate it:
        return equalityComparer is null ? CalculateHashCode() : CalculateHashCode(equalityComparer);
    }

    // Helper to calculate the hash code & store it in a thread-safe way:
    private int CalculateHashCode()
    {
        HashCode hc = default;
        hc.Add(_array.Length);
        foreach (T item in _array)
            hc.Add(item);
        int result = hc.ToHashCode();
        if (IntPtr.Size < 8)
            result += (result == 0) ? 1 : 0;
        Volatile.Write(ref _hashCode, ((nint)1 << 32) | (nint)(nuint)(uint)result);
        return result;
    }

    // Helper to calculate the hash code & store it in a thread-safe way (with custom equality comparer):
    private int CalculateHashCode(IEqualityComparer<T> equalityComparer)
    {
        HashCode hc = default;
        hc.Add(_array.Length);
        foreach (T item in _array)
            hc.Add(item is null ? 0 : equalityComparer.GetHashCode(item));
        int result = hc.ToHashCode();
        if (IntPtr.Size < 8)
            result += (result == 0) ? 1 : 0;
        Volatile.Write(ref _hashCode, ((nint)1 << 32) | (nint)(nuint)(uint)result);
        return result;
    }

    // Helper to de-duplicate this instance against a new primary instance:
    private void Deduplicate(EquatableArrayImpl<T> newPrimary)
    {
        // Note: none of this needs volatile, as it's all immutable anyway, so we just do a best-effort / low-cost de-duplication:
        _array = newPrimary._array;
        _createIndex = newPrimary._createIndex;
    }

    public bool Equals([NotNullWhen(true)] ref EquatableArrayImpl<T> other)
    {
        // Check simple cases:
        T[] otherArray = other._array;
        T[] array = _array;
        if (array == otherArray)
            return true;
        if (array.Length != otherArray.Length)
            return false;

        // Compare hash codes first for speed (this will catch 99.99+% of non-equal cases):
        if (GetHashCode() != other.GetHashCode())
            return false;

        // Check if all elements are equal:
        for (int i = 0; i < array.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(array[i], otherArray[i]))
                return false;
        }

        // Equal - perform de-duplication if needed (if create index values happen to be equal, we use the hash code of the array instance to break the tie):
        if (_createIndex < other._createIndex)
        {
            other.Deduplicate(this);
        }
        else if (_createIndex > other._createIndex || RuntimeHelpers.GetHashCode(array) > RuntimeHelpers.GetHashCode(otherArray))
        {
            Deduplicate(other);
        }
        else
        {
            other.Deduplicate(this);
        }

        // Return equal:
        return true;
    }

    public bool Equals([NotNullWhen(true)] ref EquatableArrayImpl<T> other, IEqualityComparer<T>? equalityComparer)
    {
        // Check simple cases:
        T[] otherArray = other._array;
        T[] array = _array;
        if (array == otherArray)
            return true;
        if (array.Length != otherArray.Length)
            return false;

        // Compare hash codes first for speed (this will catch 99.99+% of non-equal cases):
        if (GetHashCode(equalityComparer) != other.GetHashCode(equalityComparer))
            return false;

        // Check if all elements are equal:
        equalityComparer ??= EqualityComparer<T>.Default;
        for (int i = 0; i < array.Length; i++)
        {
            if (!equalityComparer.Equals(array[i], otherArray[i]))
                return false;
        }

        // Equal - perform de-duplication if needed (if create index values happen to be equal, we use the hash code of the array instance to break the tie):
        if (_createIndex < other._createIndex)
        {
            other.Deduplicate(this);
        }
        else if (_createIndex > other._createIndex || RuntimeHelpers.GetHashCode(array) > RuntimeHelpers.GetHashCode(otherArray))
        {
            Deduplicate(other);
        }
        else
        {
            other.Deduplicate(this);
        }

        // Return equal:
        return true;
    }

    public readonly int Length => _array.Length;
    public readonly bool IsEmpty => _array.Length == 0;
    public readonly ref readonly T this[int index] => ref _array[index];

    // Gets an IEnumerator<T> enumerator.
    public readonly IEnumerator<T> IEnumerableTGetEnumerator() => ((IEnumerable<T>)ImmutableCollectionsMarshal.AsImmutableArray(_array)).GetEnumerator();

    public readonly override string ToString() => ToString(null, null);
    public readonly string ToString(IFormatProvider? formatProvider) => ToString(null, formatProvider);
    public readonly string ToString(string? elementFormat) => ToString(elementFormat, null);

    public readonly string ToString(string? elementFormat, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new();

#if NET
        sb.Append(formatProvider, $"EquatableArray<{typeof(T).Name}>[{_array.Length}] {{ ");
#else
        sb.Append("EquatableArray<");
        sb.Append(typeof(T).Name);
        sb.Append(">[");
        sb.Append(_array.Length.ToString(formatProvider));
        sb.Append("] { ");
#endif

        var array = _array;
        if (array.Length == 0)
        {
            sb.Append('}');
        }
        else
        {
            if (array[0] is IFormattable f0)
            {
                sb.Append(f0.ToString(elementFormat, formatProvider));
            }
            else
            {
                sb.Append(array[0]);
            }

            sb.EnsureCapacity(sb.Length + (2 * array.Length) + 2);

            for (int i = 1; i < array.Length; i++)
            {
                sb.Append(", ");
                if (array[i] is IFormattable fi)
                {
                    sb.Append(fi.ToString(elementFormat, formatProvider));
                }
                else
                {
                    sb.Append(array[i]);
                }
            }

            sb.Append(" }");
        }

        return sb.ToString();
    }

    public struct Enumerator
    {
        private ImmutableArray<T>.Enumerator _impl;

        internal Enumerator(in EquatableArrayImpl<T> array)
        {
            _impl = array.UnderlyingArray.GetEnumerator();
        }

        public readonly T Current => _impl.Current;
        public bool MoveNext() => _impl.MoveNext();
    }

    public readonly ReadOnlyMemory<T> AsMemory() => new(_array);

    public readonly ReadOnlySpan<T> AsSpan() => new(_array);
    public readonly ReadOnlySpan<T> AsSpan(int start, int length) => new(_array, start, length);

    public readonly ReadOnlySpan<T> AsSpan(Range range) => range.GetOffsetAndLength(Length) switch
    {
        var (start, length) => new(_array, start, length),
    };

    public readonly bool Contains(T item) => UnderlyingArray.Contains(item);
    public readonly bool Contains(T item, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.Contains(item, equalityComparer);

    public readonly void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) => UnderlyingArray.CopyTo(sourceIndex, destination, destinationIndex, length);
    public readonly void CopyTo(T[] destination, int destinationIndex) => UnderlyingArray.CopyTo(destination, destinationIndex);
    public readonly void CopyTo(T[] destination) => UnderlyingArray.CopyTo(destination);

    public readonly void CopyTo(Span<T> destination)
    {
#if NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        UnderlyingArray.CopyTo(destination);
#else
        new ReadOnlySpan<T>(_array).CopyTo(destination);
#endif
    }

    public readonly Enumerator GetEnumerator() => new(in this);

    public readonly int IndexOf(T item) => UnderlyingArray.IndexOf(item);
    public readonly int IndexOf(T item, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.IndexOf(item, equalityComparer);
    public readonly int IndexOf(T item, int startIndex) => UnderlyingArray.IndexOf(item, startIndex);
    public readonly int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.IndexOf(item, startIndex, equalityComparer);
    public readonly int IndexOf(T item, int startIndex, int count) => UnderlyingArray.IndexOf(item, startIndex, count);
    public readonly int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.IndexOf(item, startIndex, count, equalityComparer);

    public readonly int LastIndexOf(T item) => UnderlyingArray.LastIndexOf(item);
    public readonly int LastIndexOf(T item, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.LastIndexOf(item, equalityComparer);
    public readonly int LastIndexOf(T item, int startIndex) => UnderlyingArray.LastIndexOf(item, startIndex);
    public readonly int LastIndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.LastIndexOf(item, startIndex, startIndex + 1, equalityComparer);
    public readonly int LastIndexOf(T item, int startIndex, int count) => UnderlyingArray.LastIndexOf(item, startIndex, count);
    public readonly int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => UnderlyingArray.LastIndexOf(item, startIndex, count, equalityComparer);

    public readonly ImmutableArray<T> Slice(int start, int length, out bool identical)
    {
        identical = start == 0 && length == _array.Length;
        if (identical) return UnderlyingArray;

#if NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        return UnderlyingArray.Slice(start, length);
#else
        // Do the bounds check:
        var items = _array;
        items.AsSpan(start, length);

        // Create the new result array:

        var array = length == 0 ? Array.Empty<T>() : new T[length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = items[start + i];
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
#endif
    }
}
