using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Singulink.Collections
{
    /// <content>
    /// Contains the ValueList implementation for ListDictionary.
    /// </content>
    public partial class ListDictionary<TKey, TValue>
    {
        /// <summary>
        /// Represents a value list in a <see cref="ListDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <remarks>
        /// The value list goes into a detached state when it is removed from a dictionary or becomes empty. You can check if a list is detached using the <see
        /// cref="IsDetached"/> property.
        /// </remarks>
        public sealed class ValueList : ReadOnlyList<TValue>
        {
            private ListDictionary<TKey, TValue>? _dictionary;
            private TKey? _key;

            internal ValueList(ListDictionary<TKey, TValue> dictionary, TKey key, List<TValue> list) : base(list)
            {
                _dictionary = dictionary;
                _key = key;
            }

            /// <summary>
            /// Gets the key this value list is associated with. Property throws an exception when the list is in a detached state.
            /// </summary>
            /// <exception cref="InvalidOperationException">The value list is in a detached state.</exception>
            [AllowNull]
            public TKey Key {
                get {
                    if (_dictionary == null)
                        ThrowDetached();

                    return _key!;
                }
            }

            /// <summary>
            /// Gets the dictionary this value list belongs to. Property throws an exception when the list is in a detached state.
            /// </summary>
            /// <exception cref="InvalidOperationException">The value list is in a detached state.</exception>
            [AllowNull]
            public ListDictionary<TKey, TValue> Dictionary {
                get {
                    if (_dictionary == null)
                        ThrowDetached();

                    return _dictionary!;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the value list is in a detached state. The list goes into a detached state when it is removed from a dictionary
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
                throw new InvalidOperationException("The value list is in a detached state. Detached lists are no longer associated with a key or dictionary.");
            }
        }
    }
}