using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Singulink.Collections
{
    public class ListDictionary<TKey, TValue> : CollectionDictionary<TKey, TValue, List<TValue>, ReadOnlyCollection<TValue>>
        where TKey : notnull
    {
        protected override bool AddToCollection(List<TValue> collection, TValue value)
        {
            collection.Add(value);
            return true;
        }

        protected override (List<TValue>, ReadOnlyCollection<TValue>) CreateCollections()
        {
            var list = new List<TValue>();
            return (list, new ReadOnlyCollection<TValue>(list));
        }
    }
}
