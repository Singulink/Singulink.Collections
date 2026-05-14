namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="ListVersion"/> nested type for <see cref="ConcurrentWeakList{T}"/>.
/// </content>
public sealed partial class ConcurrentWeakList<T>
{
    /// <summary>
    /// Represents a version of the list at a point in time - the version is increased only whenever an item is added to the list.
    /// </summary>
    /// <remarks>
    /// This allows callers to detect whether a node was added after they begun a long operation they split up into multiple steps to not starve finalizers.
    /// </remarks>
    public readonly struct ListVersion : IEquatable<ListVersion>, IComparable<ListVersion>
    {
        internal readonly ulong _version;

        internal ListVersion(ulong version) => _version = version;

        /// <summary>
        /// Determines whether two versions are equal.
        /// </summary>
        public static bool operator ==(ListVersion left, ListVersion right) => left._version == right._version;

        /// <summary>
        /// Determines whether two versions are not equal.
        /// </summary>
        public static bool operator !=(ListVersion left, ListVersion right) => left._version != right._version;

        /// <summary>
        /// Determines whether the first version is greater than or equal to the second.
        /// </summary>
        public static bool operator >=(ListVersion left, ListVersion right) => left._version >= right._version;

        /// <summary>
        /// Determines whether the first version is less than or equal to the second.
        /// </summary>
        public static bool operator <=(ListVersion left, ListVersion right) => left._version <= right._version;

        /// <summary>
        /// Determines whether the first version is greater than the second.
        /// </summary>
        public static bool operator >(ListVersion left, ListVersion right) => left._version > right._version;

        /// <summary>
        /// Determines whether the first version is less than the second.
        /// </summary>
        public static bool operator <(ListVersion left, ListVersion right) => left._version < right._version;

        /// <inheritdoc />
        public readonly override int GetHashCode() => _version.GetHashCode();

        /// <inheritdoc />
        public readonly override bool Equals(object? obj) => obj is ListVersion other && _version == other._version;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public readonly bool Equals(ListVersion other) => _version == other._version;

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public readonly int CompareTo(ListVersion other) => _version.CompareTo(other._version);
    }
}
