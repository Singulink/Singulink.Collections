using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Singulink.Collections
{
    public class MapSide<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _lookup;
        private readonly Dictionary<TValue, TKey> _reverseLookup;

        internal MapSide(Dictionary<TKey, TValue> lookup, Dictionary<TValue, TKey> reverseLookup)
        {
            _lookup = lookup;
            _reverseLookup = reverseLookup;
        }

        public TValue this[TKey key] => _lookup[key];

        internal Dictionary<TKey, TValue> Lookup => _lookup;

        public bool Contains(TKey key) => _lookup.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => _lookup.TryGetValue(key, out value);

        /// <summary>
        /// Removes an association from the map given a specified key on this side of the map and returns true if successful.
        /// </summary>
        public bool Remove(TKey key)
        {
            if (_lookup.Remove(key, out var removedValue)) {
                bool result = _reverseLookup.Remove(removedValue);
                Debug.Assert(result, "reverse map side remove failure");

                return true;
            }

            return false;
        }
    }
}
