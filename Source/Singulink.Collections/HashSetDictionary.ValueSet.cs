using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the ValueSet implementation for HashSetDictionary.
    /// </content>
    public partial class HashSetDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents a value set in a <see cref="HashSetDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <remarks>
        /// The value set goes into a detached state when it is removed from a dictionary or becomes empty. You can check if a set is detached using the <see
        /// cref="IsDetached"/> property.
        /// </remarks>
        public sealed class ValueSet : ReadOnlyHashSet<TValue>
        {
            private HashSetDictionary<TKey, TValue>? _dictionary;
            private TKey? _key;

            internal ValueSet(HashSetDictionary<TKey, TValue> dictionary, TKey key, HashSet<TValue> set) : base(set)
            {
                _dictionary = dictionary;
                _key = key;
            }

            /// <summary>
            /// Gets the key this value set is associated with. Property throws an exception when the set is in a detached state.
            /// </summary>
            /// <exception cref="InvalidOperationException">The value set is in a detached state.</exception>
            [AllowNull]
            public TKey Key {
                get {
                    if (_dictionary == null)
                        ThrowDetached();

                    return _key!;
                }
            }

            /// <summary>
            /// Gets the dictionary this value set belongs to. Property throws an exception when the set is in a detached state.
            /// </summary>
            /// <exception cref="InvalidOperationException">The value set is in a detached state.</exception>
            [AllowNull]
            public HashSetDictionary<TKey, TValue> Dictionary {
                get {
                    if (_dictionary == null)
                        ThrowDetached();

                    return _dictionary!;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the value set is in a detached state. The set goes into a detached state when it is removed from a dictionary
            /// or becomes empty.
            /// </summary>
            public bool IsDetached => _dictionary == null;

            internal void Detach()
            {
                Debug.Assert(_dictionary != null, "already detached");

                // Clear key and dictionary for GC

                _dictionary = null;

                if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
                    _key = default;
            }

            private static void ThrowDetached()
            {
                throw new InvalidOperationException("The value set is in a detached state. Detached sets are no longer associated with a key or dictionary.");
            }
        }
    }
}