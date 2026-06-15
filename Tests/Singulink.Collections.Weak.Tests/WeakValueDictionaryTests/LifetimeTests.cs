namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class LifetimeTests
{
    [TestMethod]
    public void ValueKeepsEntryInDictionary()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        object value = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            return value;
        });

        Helpers.ForceGC();

        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ValueKeepsInternalNodeAlive()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var (nodeWeakRef, value) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            return (new WeakReference<object?>(node), value);
        });

        Helpers.ForceGC();

        // While the value is alive, the internal node backing the entry must stay alive too.
        nodeWeakRef.TryGetTarget(out _).ShouldBeTrue();
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ValueDiesWhenOnlyReferenceIsDictionary()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var (valueRef, internalNodeHelperWeakRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node));
            GC.KeepAlive(value);
            return (new WeakReference<object>(value), internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        valueRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void EntryDoesNotKeepValueAlive()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var (node, valueRef, internalNodeHelperWeakRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node));
            GC.KeepAlive(value);
            return (node, new WeakReference<object>(value), internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        valueRef.TryGetTarget(out _).ShouldBeFalse();
        Helpers.GetNode(dictionary, 1).ShouldBeNull();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(node);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ValueStoringKeyDoesNotKeepValueAliveAndReleasesKey()
    {
        var dictionary = new WeakValueDictionary<object, List<object>>();

        var (keyRef, valueRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object key = new();
            var value = new List<object> { key }; // The value holds a strong reference back to its own key.
            dictionary.TryAdd(key, value);
            GC.KeepAlive(key);
            GC.KeepAlive(value);
            return (new WeakReference<object>(key), new WeakReference<object>(value));
        });

        Helpers.ForceGC();

        // The value is only weakly held, so storing the key inside the value does not keep the value alive.
        valueRef.TryGetTarget(out _).ShouldBeFalse();

        // Once the value is collected the entry is removed and the strongly-held key is released as well.
        keyRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void KeyStoringValueKeepsValueAlive()
    {
        var dictionary = new WeakValueDictionary<List<object>, object>();

        var (keyRef, valueRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            var key = new List<object> { value }; // The key holds a strong reference to the value.
            dictionary.TryAdd(key, value);
            GC.KeepAlive(key);
            GC.KeepAlive(value);
            return (new WeakReference<object>(key), new WeakReference<object>(value));
        });

        Helpers.ForceGC();

        // Keys are held strongly, so a key that references its value keeps that value reachable: the entry is self-sustaining and neither dies.
        keyRef.TryGetTarget(out _).ShouldBeTrue();
        valueRef.TryGetTarget(out _).ShouldBeTrue();
        dictionary.UnsafeCount.ShouldBe(1);

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void KeyKeptAliveOnlyByDictionaryStaysAliveWhileValueAlive()
    {
        var dictionary = new WeakValueDictionary<object, object>();

        var (keyRef, value) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object key = new();
            object value = new();
            dictionary.TryAdd(key, value);
            return (new WeakReference<object>(key), value);
        });

        Helpers.ForceGC();

        // Keys are held strongly, so a key whose only reference is the dictionary stays alive as long as its value is alive.
        keyRef.TryGetTarget(out _).ShouldBeTrue();

        GC.KeepAlive(value);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void KeyReleasedWhenValueCollected()
    {
        var dictionary = new WeakValueDictionary<object, object>();

        var (keyRef, valueRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object key = new();
            object value = new();
            dictionary.TryAdd(key, value);
            GC.KeepAlive(value);
            return (new WeakReference<object>(key), new WeakReference<object>(value));
        });

        Helpers.ForceGC();

        // When the weakly-held value is collected the entry is removed, which releases the strongly-held key too.
        valueRef.TryGetTarget(out _).ShouldBeFalse();
        keyRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ValueReferencingDictionaryDoesNotLeak()
    {
        var (dictionaryWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef) = Helpers.NotInlined(() =>
        {
            var dictionary = new WeakValueDictionary<int, object>();
            List<object> value = [dictionary];
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            value.Add(node);
            value.Add(value);
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(dictionary, node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node));
            GC.KeepAlive(value);
            return (new WeakReference<object>(dictionary), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        dictionaryWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();
    }

    [TestMethod]
    public void AliveValueInClearedDictionaryDoesNotLeakUnreferencedDictionary()
    {
        var (dictionaryWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef, o) = Helpers.NotInlined(() =>
        {
            var dictionary = new WeakValueDictionary<int, object>();
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(dictionary, node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node));
            dictionary.Clear();
            GC.KeepAlive(value);
            return (new WeakReference<object>(dictionary), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef, value);
        });

        Helpers.ForceGC();

        dictionaryWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(o);
    }

    [TestMethod]
    public void AliveValueInUnclearedDictionaryDoesNotLeakUnreferencedDictionary()
    {
        var (dictionaryWeakRef, nodeWeakRef, internalNodeWeakRef, internalNodeHelperWeakRef, o) = Helpers.NotInlined(() =>
        {
            var dictionary = new WeakValueDictionary<int, object>();
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            var internalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(dictionary, node));
            var internalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node));

            // Note: the dictionary is not cleared - it is allowed to die naturally via the finalizer while the value stays alive.
            GC.KeepAlive(value);
            return (new WeakReference<object>(dictionary), new WeakReference<object?>(node), internalNodeWeakRef, internalNodeHelperWeakRef, value);
        });

        Helpers.ForceGC();

        dictionaryWeakRef.TryGetTarget(out _).ShouldBeFalse();
        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        internalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(o);
    }

    [TestMethod]
    public void ClearAllowsEntryToDie()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var nodeWeakRef = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            object node = Helpers.GetNode(dictionary, 1)!;
            dictionary.Clear();
            return new WeakReference<object?>(node);
        });

        Helpers.ForceGC();

        nodeWeakRef.TryGetTarget(out _).ShouldBeFalse();

        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void IndexerOverwriteAllowsOldEntryToDie()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        object newValue = new();

        var (oldNodeWeakRef, oldInternalNodeWeakRef, oldInternalNodeHelperWeakRef) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object oldValue = new();
            dictionary[1] = oldValue;
            object oldNode = Helpers.GetNode(dictionary, 1)!;
            var oldInternalNodeWeakRef = new WeakReference<object?>(Helpers.GetInternalNode(dictionary, oldNode));
            var oldInternalNodeHelperWeakRef = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, oldNode));

            // Overwrite the entry: the old node is disposed and should become collectable while the old value is allowed to die.
            dictionary[1] = newValue;
            return (new WeakReference<object?>(oldNode), oldInternalNodeWeakRef, oldInternalNodeHelperWeakRef);
        });

        Helpers.ForceGC();

        oldNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        oldInternalNodeWeakRef.TryGetTarget(out _).ShouldBeFalse();
        oldInternalNodeHelperWeakRef.TryGetTarget(out _).ShouldBeFalse();

        // The new value should still be present and retrievable.
        dictionary.TryGetValue(1, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(newValue);

        GC.KeepAlive(newValue);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ValueKeepsRemainingEntryAliveAfterOtherKeyRemoved()
    {
        var dictionary = new WeakValueDictionary<int, object>();

        var (node1, node1InternalNode, node1InternalNodeHelper, node2, value) = Helpers.NotInlined(dictionary, (dictionary) =>
        {
            object value = new();
            dictionary.TryAdd(1, value);
            dictionary.TryAdd(2, value);
            object node1Obj = Helpers.GetNode(dictionary, 1)!;
            object node2Obj = Helpers.GetNode(dictionary, 2)!;
            var node1InternalNode = new WeakReference<object?>(Helpers.GetInternalNode(dictionary, node1Obj));
            var node1InternalNodeHelper = new WeakReference<object?>(Helpers.GetInternalNodeFinalizeHelper(dictionary, node1Obj));
            dictionary.Remove(1);
            return (
                new WeakReference<object?>(node1Obj),
                node1InternalNode,
                node1InternalNodeHelper,
                new WeakReference<object?>(node2Obj),
                value);
        });

        Helpers.ForceGC();

        node1.TryGetTarget(out _).ShouldBeFalse();
        node1InternalNode.TryGetTarget(out _).ShouldBeFalse();
        node1InternalNodeHelper.TryGetTarget(out _).ShouldBeFalse();
        node2.TryGetTarget(out _).ShouldBeTrue();
        dictionary.TryGetValue(2, out object? current).ShouldBeTrue();
        current.ShouldBeSameAs(value);

        GC.KeepAlive(value);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void RemovingEntriesEvictsCwtEntriesWhileValuesStayAlive()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var values = new List<object>();

        for (int i = 0; i < 100; i++)
        {
            object value = new();
            values.Add(value);
            dictionary.TryAdd(i, value);
        }

        foreach (object value in values)
            Helpers.CwtContainsValue(dictionary, value)?.ShouldBeTrue();

        for (int i = 0; i < 100; i++)
            dictionary.Remove(i);

        // The per-value CWT entries should be evicted immediately even though the values are still alive, rather than lingering until the
        // values are eventually collected.
        foreach (object value in values)
            Helpers.CwtContainsValue(dictionary, value)?.ShouldBeFalse();

        GC.KeepAlive(values);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    public void ClearEvictsCwtEntriesWhileValuesStayAlive()
    {
        var dictionary = new WeakValueDictionary<int, object>();
        var values = new List<object>();

        for (int i = 0; i < 100; i++)
        {
            object value = new();
            values.Add(value);
            dictionary.TryAdd(i, value);
        }

        foreach (object value in values)
            Helpers.CwtContainsValue(dictionary, value)?.ShouldBeTrue();

        dictionary.Clear();

        // Clearing the dictionary should evict the per-value CWT entries immediately even though the values are still alive.
        foreach (object value in values)
            Helpers.CwtContainsValue(dictionary, value)?.ShouldBeFalse();

        GC.KeepAlive(values);
        GC.KeepAlive(dictionary);
    }

    [TestMethod]
    [Retry(99)] // We could get unlucky and have a GC occur that stops us from capturing the resurrected instance - this should make that effectively impossible
    public void ResurrectedDictionaryThrowsOnUse()
    {
        var (shortRef, longRef) = Helpers.NotInlined(() =>
        {
            var dictionary = new WeakValueDictionary<int, object>();
            dictionary.TryAdd(1, new object());

            // A non-tracking reference goes null as soon as the instance is collected/finalized, while the resurrection-tracking reference keeps the instance
            // retrievable until its memory is actually reclaimed.
            return (
                new WeakReference<WeakValueDictionary<int, object>>(dictionary, trackResurrection: false),
                new WeakReference<WeakValueDictionary<int, object>>(dictionary, trackResurrection: true));
        });

        // Run the dictionary's finalizer (which marks it as disposed) and capture the instance after it has been finalized but before its memory is reclaimed,
        // simulating resurrection.
        WeakValueDictionary<int, object>? resurrected = null;

        for (int i = 0; i < 20; i++)
        {
            GC.Collect(int.MaxValue, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            if (!shortRef.TryGetTarget(out _) && longRef.TryGetTarget(out resurrected))
                break;
        }

        resurrected.ShouldNotBeNull();

        // Using a resurrected (finalized) dictionary must throw rather than silently misbehaving, as its internal state is no longer safe to use.
        Should.Throw<ObjectDisposedException>(() => resurrected!.TryAdd(2, new object()));

        GC.KeepAlive(resurrected);
    }
}
