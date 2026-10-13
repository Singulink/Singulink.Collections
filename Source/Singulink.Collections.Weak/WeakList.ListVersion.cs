namespace Singulink.Collections;

/// <content>
/// Contains the <see cref="ListVersion"/> nested type for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    /// <summary>
    /// Represents the version of a <see cref="WeakList{T}"/> at a point in time. The version increases every time a node is added to the list, so versions
    /// can be compared to determine whether a node was added before or after a given point.
    /// </summary>
    /// <remarks>
    /// Versions are obtained from <see cref="WeakList{T}.Version"/> and <see cref="Node.Version"/>.
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
        /// Determines whether the first version is the same as or later than the second.
        /// </summary>
        public static bool operator >=(ListVersion left, ListVersion right) => left._version >= right._version;

        /// <summary>
        /// Determines whether the first version is the same as or earlier than the second.
        /// </summary>
        public static bool operator <=(ListVersion left, ListVersion right) => left._version <= right._version;

        /// <summary>
        /// Determines whether the first version is later than the second.
        /// </summary>
        public static bool operator >(ListVersion left, ListVersion right) => left._version > right._version;

        /// <summary>
        /// Determines whether the first version is earlier than the second.
        /// </summary>
        public static bool operator <(ListVersion left, ListVersion right) => left._version < right._version;

        /// <inheritdoc/>
        public readonly override int GetHashCode() => _version.GetHashCode();

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is ListVersion other && _version == other._version;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public readonly bool Equals(ListVersion other) => _version == other._version;

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        public readonly int CompareTo(ListVersion other) => _version.CompareTo(other._version);
    }
}
