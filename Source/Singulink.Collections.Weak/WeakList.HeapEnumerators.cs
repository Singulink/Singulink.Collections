using System.Collections;
using System.Diagnostics;

namespace Singulink.Collections;

#pragma warning disable RCS1043 // Remove 'partial' modifier from type with a single part

/// <content>
/// Contains the heap-allocated enumerator wrappers for <see cref="WeakList{T}"/>.
/// </content>
public sealed partial class WeakList<T>
{
    private sealed partial class HeapValueEnumerator(Enumerator impl, bool reversed, bool skipNewNodes) : IEnumerator<T>
    {
        private Enumerator _impl = impl;

        public T Current => _impl.Current;
        object IEnumerator.Current => _impl.Current;
        public void Dispose() => _impl = default;

        public bool MoveNext()
        {
            do
            {
                if (!(reversed ? _impl.MovePrevious() : _impl.MoveNext())) return false;
            }
            while (skipNewNodes && _impl.WasAddedDuringEnumeration);
            return true;
        }

        public void Reset() => throw new NotSupportedException();
    }

    private sealed partial class HeapValueEnumerable(Enumerator impl, bool reversed, bool skipNewNodes) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            var inst = impl;
            inst._nodeEnumerator._listVersion = impl._nodeEnumerator._list!.Version._version;
            return new HeapValueEnumerator(inst, reversed, skipNewNodes);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed partial class HeapNodeEnumerator(NodeEnumerator impl, bool reversed, bool skipNewNodes) : IEnumerator<Node>
    {
        private NodeEnumerator _impl = impl;

        public Node Current => _impl.Current;
        object IEnumerator.Current => _impl.Current;
        public void Dispose() => _impl = default;

        public bool MoveNext()
        {
            do
            {
                if (!(reversed ? _impl.MovePrevious() : _impl.MoveNext())) return false;
            }
            while (skipNewNodes && _impl.WasAddedDuringEnumeration);
            return true;
        }

        public void Reset() => throw new NotSupportedException();
    }

    private sealed partial class HeapNodeEnumerable(NodeEnumerator impl, bool reversed, bool skipNewNodes) : IEnumerable<Node>
    {
        public IEnumerator<Node> GetEnumerator()
        {
            var inst = impl;
            var list = impl._list;
            Debug.Assert(list is not null, "List should not be null, as we can only box enumerators while they're not disposed.");
            inst._listVersion = list.Version._version;
            return new HeapNodeEnumerator(inst, reversed, skipNewNodes);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
