using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Singulink.Collections
{
    public class SealableCollection<T> : Collection<T>
    {
        public bool IsSealed { get; private set; }

        public SealableCollection()
        {
        }

        public SealableCollection(IList<T> list) : base(list)
        {
        }

        public void Seal() => IsSealed = true;

        public void CheckSealed()
        {
            if (IsSealed)
                throw new InvalidOperationException("Collection cannot be changed after it has been sealed.");
        }

        protected override void InsertItem(int index, T item)
        {
            CheckSealed();
            base.InsertItem(index, item);
        }

        protected override void ClearItems()
        {
            CheckSealed();
            base.ClearItems();
        }

        protected override void SetItem(int index, T item)
        {
            CheckSealed();
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            CheckSealed();
            base.RemoveItem(index);
        }
    }
}
