using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Singulink.Collections
{
    /// <summary>
    /// Thread-safe append-only dictionary with internal copy-on-write behavior that enables the fastest possible lock-free lookups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The performance characteristics of this dictionary make it perfect for use as a permanent object cache. Updates to the dictionary are temporarily
    /// cached in a synchronized mutable collection which allows copying to be delayed so that updates are batched together to reduce copying and improve write
    /// performance. This dictionary has identical lookup performance to <see cref="Dictionary{TKey, TValue}"/> for values that have been copied into the main
    /// lookup.
    /// </para>
    /// <para>
    /// Delayed updates are stored in a temporary synchronized mutable lookup. Once there have been no updates to the dictionary for the amount of time set on
    /// <see cref="CopyDelay"/> then the internal copy operation is performed on a background thread and lookups are restored to full speed.
    /// </para>
    /// </remarks>
    public class CopyOnWriteDictionary<TKey, TValue> where TKey : notnull
    {
        private Dictionary<TKey, TValue> _lookup;
        private Dictionary<TKey, TValue>? _pendingAdds;

        private int _copyDelay = 100;
        private Timer? _copyTimer;

        private readonly object _syncRoot = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyOnWriteDictionary{TKey, TValue}"/> class.
        /// </summary>
        public CopyOnWriteDictionary(IEqualityComparer<TKey>? comparer = null)
        {
            _lookup = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyOnWriteDictionary{TKey, TValue}"/> class with elements copied from the specified collection.
        /// </summary>
        public CopyOnWriteDictionary(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs, IEqualityComparer<TKey>? comparer = null)
        {
            _lookup = new Dictionary<TKey, TValue>(keyValuePairs, comparer);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key. Setter can only be used to add values, not overwrite them.
        /// </summary>
        public TValue this[TKey key] {
            get {
                if (TryGetValue(key, out var value))
                    return value;

                throw new KeyNotFoundException();
            }
            set {
                Add(key, value);
            }
        }

        /// <summary>
        /// Gets the keys contained in this dictionary.
        /// </summary>
        public ICollection<TKey> Keys {
            get {
                lock (_syncRoot) {
                    if (_pendingAdds != null)
                        return new MergedCollection<TKey>(_lookup.Keys, _pendingAdds.Keys.ToList());

                    return _lookup.Keys;
                }
            }
        }

        /// <summary>
        /// Gets the values contained in this dictionary.
        /// </summary>
        public ICollection<TValue> Values {
            get {
                lock (_syncRoot) {
                    if (_pendingAdds != null)
                        return new MergedCollection<TValue>(_lookup.Values, _pendingAdds.Values.ToList());

                    return _lookup.Values;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to delay copying updates to the internal dictionary. Default is 100ms and a value of 0 disables the copy
        /// delay feature, causing any updates to the dictionary to immediately trigger a copy operation.
        /// </summary>
        public int CopyDelay {
            get => Volatile.Read(ref _copyDelay);
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                lock (_syncRoot) {
                    DebugCheckState();
                    _copyDelay = value;

                    if (_pendingAdds != null) {
                        if (value == 0)
                            CopyUpdates();
                        else
                            _copyTimer?.Change(value, Timeout.Infinite);
                    }

                    DebugCheckState();
                }
            }
        }

        /// <summary>
        /// Gets the number of items in this dictionary.
        /// </summary>
        public int Count {
            get {
                DebugNoSync();

                lock (_syncRoot)
                    return CountInternal;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a copy operation is pending due to delayed updates.
        /// </summary>
        public bool IsCopyPending {
            get {
                DebugNoSync();

                lock (_syncRoot)
                    return _pendingAdds != null;
            }
        }

        private int CountInternal {
            get {
                DebugSyncRequired();
                int count = _lookup.Count;

                if (_pendingAdds != null)
                    count += _pendingAdds.Count;

                return count;
            }
        }

        /// <summary>
        /// Performs a lookup for a value with the specified key. This method has a fast lock-free path with identical performance to <see
        /// cref="Dictionary{TKey, TValue}"/> if the key has been copied to the main dictionary and is not waiting in delayed pending updates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_lookup.TryGetValue(key, out value))
                return true;

            return TryGetValueSlow(key, out value);

            [MethodImpl(MethodImplOptions.NoInlining)]
            bool TryGetValueSlow(TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                lock (_syncRoot) {
                    // Check pending updates first since we just checked the main lookup and it wasn't there.
                    if (_pendingAdds != null && _pendingAdds.TryGetValue(key, out value))
                        return true;

                    return _lookup.TryGetValue(key, out value);
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether this dictionary contains the specified key.
        /// </summary>
        public bool ContainsKey(TKey key) => TryGetValue(key, out _);

        /// <summary>
        /// Adds the key and value to a copy of the internal dictionary under a synchronized lock.
        /// </summary>
        public void Add(TKey key, TValue value, bool delayCopy = true)
        {
            lock (_syncRoot) {
                AddInternal(key, value, delayCopy, false);
            }
        }

        /// <summary>
        /// Adds the key and value to a copy of the internal dictionary under a synchronized lock.
        /// </summary>
        public bool TryAdd(TKey key, TValue value, bool delayCopy = true)
        {
            lock (_syncRoot) {
                return TryAddInternal(key, value, delayCopy, false);
            }
        }

        /// <summary>
        /// Performs a lookup for a value with the specified key under a synchronized lock. If the key is not found then the value is added to the dictionary.
        /// </summary>
        public TValue GetOrAdd(TKey key, TValue value, bool delayCopy = true)
        {
            lock (_syncRoot) {
                if (TryGetValue(key, out var existingValue))
                    return existingValue;

                AddInternal(key, value, delayCopy, true);
                return value;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates though the dictionary.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            IEnumerable<KeyValuePair<TKey, TValue>> items;

            lock (_syncRoot) {
                items = _lookup;

                if (_pendingAdds != null)
                    items = items.Concat(_pendingAdds.ToList());
            }

            foreach (var item in items)
                yield return item;
        }

        private bool TryAddInternal(TKey key, TValue value, bool delayCopy, bool lookupPrechecked)
        {
            #if DEBUG
            DebugSyncRequired();
            DebugCheckState();
            int preAddCount = CountInternal;
            #endif

            if (!lookupPrechecked && _lookup.ContainsKey(key))
                return false;

            ////////////

            if (_copyDelay == 0)
                delayCopy = false;

            if (_pendingAdds == null) {
                if (!delayCopy) {
                    var newLookup = new Dictionary<TKey, TValue>(_lookup.Count + 1, _lookup.Comparer);

                    foreach (var item in _lookup)
                        newLookup.Add(item.Key, item.Value);

                    newLookup.Add(key, value);

                    _lookup = newLookup;
                    return true;
                }

                _pendingAdds = new Dictionary<TKey, TValue>(31, _lookup.Comparer);
            }

            if (!_pendingAdds.TryAdd(key, value))
                return false;

            if (delayCopy) {
                if (_copyTimer == null)
                    _copyTimer = new Timer(CopyUpdatesCallback, null, _copyDelay, Timeout.Infinite);
                else
                    _copyTimer.Change(_copyDelay, Timeout.Infinite);
            }
            else {
                CopyUpdates();
            }

            #if DEBUG
            DebugCheckState();
            Debug.Assert(CountInternal == preAddCount + 1, "incorrect final count");
            #endif

            return true;
        }

        private void AddInternal(TKey key, TValue value, bool delayCopy, bool lookupPrechecked)
        {
            if (!TryAddInternal(key, value, delayCopy, lookupPrechecked))
                throw new ArgumentException("An element with the same key already exists.", nameof(key));
        }

        private void CopyUpdates()
        {
            #if DEBUG
            DebugSyncRequired();
            DebugCheckState();
            int preCopyCount = CountInternal;
            #endif

            if (_copyTimer != null) {
                _copyTimer.Dispose();
                _copyTimer = null;
            }

            if (_pendingAdds != null) {
                var newLookup = new Dictionary<TKey, TValue>(_lookup.Count + _pendingAdds.Count, _lookup.Comparer);

                foreach (var item in _lookup)
                    newLookup.Add(item.Key, item.Value);

                foreach (var item in _pendingAdds)
                    newLookup.Add(item.Key, item.Value);

                _lookup = newLookup;
                _pendingAdds = null;
            }

            #if DEBUG
            DebugCheckState();
            Debug.Assert(CountInternal == preCopyCount, "unexpected count");
            #endif
        }

        private void CopyUpdatesCallback(object? state)
        {
            lock (_syncRoot)
                CopyUpdates();
        }

        #region Debug

        [Conditional("DEBUG")]
        private void DebugCheckState()
        {
            Debug.Assert(Monitor.IsEntered(_syncRoot), "synchronization required to check state");
            Debug.Assert((_copyTimer == null) == (_pendingAdds == null), "invalid state");
        }

        [Conditional("DEBUG")]
        private void DebugSyncRequired() => Debug.Assert(Monitor.IsEntered(_syncRoot), "synchronization required");

        [Conditional("DEBUG")]
        private void DebugNoSync() => Debug.Assert(!Monitor.IsEntered(_syncRoot), "nested synchronization - use unsynchronized method instead");

        #endregion

        #region Explicit Interface Members

        // Removed until ICollection<T> is safe to implement in a concurent collection without breaking LINQ due to ICollection optimization race conditions.

        // bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        // TValue IDictionary<TKey, TValue>.this[TKey key] {
        //     get => this[key];
        //     set => throw new NotSupportedException();
        // }

        // void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, value);

        // void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        // bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        // {
        //     if (TryGetValue(item.Key, out var value))
        //         return EqualityComparer<TValue>.Default.Equals(item.Value, value);
        //
        //     return false;
        // }

        // void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        // {
        //     ICollection<KeyValuePair<TKey, TValue>> lookup;
        //
        //     lock (_syncRoot) {
        //         if (_pendingAdds != null)
        //             lookup = new MergedCollection<KeyValuePair<TKey, TValue>>(_lookup, _pendingAdds.ToList());
        //         else
        //             lookup = _lookup;
        //     }
        //
        //     lookup.CopyTo(array, arrayIndex);
        // }

        // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // #endregion

        // #region Not Supported

        // bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();

        // bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        // void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();

        #endregion
    }
}