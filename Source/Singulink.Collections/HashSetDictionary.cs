using System;
using System.Collections.Generic;
using System.Text;

namespace Singulink.Collections
{
    public class HashSetDictionary<TKey, TValue> : CollectionDictionary<TKey, TValue, HashSet<TValue>, ReadOnlyHashSet<TValue>>
        where TKey : notnull
    {
        protected override bool AddToCollection(HashSet<TValue> collection, TValue value) => collection.Add(value);

        protected override (HashSet<TValue>, ReadOnlyHashSet<TValue>) CreateCollections()
        {
            var set = new HashSet<TValue>();
            return (set, new ReadOnlyHashSet<TValue>(set));
        }
    }
}
