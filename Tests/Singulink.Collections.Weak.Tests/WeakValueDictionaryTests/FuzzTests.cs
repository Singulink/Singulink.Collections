namespace Singulink.Collections.Weak.Tests.WeakValueDictionaryTests;

[PrefixTestClass]
public class FuzzTests
{
    public static IEnumerable<object[]> BasicFuzzTestData => Enumerable.Range(0, 20).Select((x) => new object[] { x });

    [DynamicData(nameof(BasicFuzzTestData))]
    [TestMethod]
    public void BasicFuzzTest(int seed)
    {
        // In this, we make an actual dictionary, perform random operations on both it and a WeakValueDictionary, and ensure they stay the same (we keep all
        // values alive, so nothing should be collected during the test). This exercises all of the core (non-wrapper) APIs other than the alternate lookup and
        // custom comparer based ones: TryAdd, Add, the indexer setter, TryGetValue, Remove(key, out value), Remove(key, value), Clear, UnsafeCount and
        // enumeration.
        Random r = new(seed);
        var weakDictionary = new WeakValueDictionary<int, object>();
        Dictionary<int, object> actualDictionary = [];
        List<object> keepAlive = []; // Keep every value alive so the weak references never get collected mid-test.
        const int Operations = 10000;
        const int MaxKeys = 50;

        for (int i = 0; i < Operations; i++)
        {
            // Determine which structural operation to perform:
            //   0 => clear, 1 => add, 2 => remove.
            int operation;

            if (actualDictionary.Count == 0)
                operation = 1;
            else if (actualDictionary.Count == MaxKeys)
                operation = r.Next(2) == 0 ? 0 : 2;
            else
                operation = r.Next(20) == 0 ? 0 : (r.Next(2) == 0 ? 1 : 2);

            if (operation == 0)
            {
                // Clear: empties the dictionary.
                weakDictionary.Clear();
                actualDictionary.Clear();
            }
            else if (operation == 1)
            {
                int key = r.Next(MaxKeys);
                object newValue = new();
                keepAlive.Add(newValue);

                switch (r.Next(3))
                {
                    case 0:
                        // TryAdd: succeeds only if the key is absent (all values are alive, so existing entries are never stale).
                        if (actualDictionary.ContainsKey(key))
                        {
                            weakDictionary.TryAdd(key, newValue).ShouldBeFalse();
                        }
                        else
                        {
                            weakDictionary.TryAdd(key, newValue).ShouldBeTrue();
                            actualDictionary[key] = newValue;
                        }

                        break;
                    case 1:
                        // Add: throws if the key is already present.
                        if (actualDictionary.ContainsKey(key))
                        {
                            Should.Throw<ArgumentException>(() => weakDictionary.Add(key, newValue));
                        }
                        else
                        {
                            weakDictionary.Add(key, newValue);
                            actualDictionary[key] = newValue;
                        }

                        break;
                    case 2:
                        // Indexer set: inserts or overwrites unconditionally.
                        weakDictionary[key] = newValue;
                        actualDictionary[key] = newValue;
                        break;
                }
            }
            else
            {
                var keys = actualDictionary.Keys.ToList();
                int key = keys[r.Next(keys.Count)];

                switch (r.Next(4))
                {
                    case 0:
                        // Remove(key, out value): removes and hands back the previous value.
                        weakDictionary.Remove(key, out object? removed).ShouldBeTrue();
                        removed.ShouldBeSameAs(actualDictionary[key]);
                        actualDictionary.Remove(key);
                        break;
                    case 1:
                        // Remove(key, value) with the matching value: removes the entry.
                        weakDictionary.Remove(key, actualDictionary[key]).ShouldBeTrue();
                        actualDictionary.Remove(key);
                        break;
                    case 2:
                        // Remove(key, value) with a non-matching value: leaves the entry in place.
                        weakDictionary.Remove(key, new object()).ShouldBeFalse();
                        break;
                    case 3:
                        // Remove(key, value) with a non-matching value for a key that is absent: returns false.
                        int absentKey = MaxKeys + r.Next(MaxKeys);
                        weakDictionary.Remove(absentKey, new object()).ShouldBeFalse();
                        break;
                }
            }

            // TryGetValue should agree with the reference dictionary for both present and absent keys.
            int probeKey = r.Next(MaxKeys);

            if (actualDictionary.TryGetValue(probeKey, out object? expected))
            {
                weakDictionary.TryGetValue(probeKey, out object? actual).ShouldBeTrue();
                actual.ShouldBeSameAs(expected);
            }
            else
            {
                weakDictionary.TryGetValue(probeKey, out _).ShouldBeFalse();
            }

            // Check they're the same (order independent):
            weakDictionary.UnsafeCount.ShouldBe(actualDictionary.Count);
            weakDictionary.ToList().ShouldBe(actualDictionary.ToList(), ignoreOrder: true);
        }

        // Keep all values alive until the end:
        GC.KeepAlive(keepAlive);
    }

    public static IEnumerable<object[]> WeakFuzzTestData => Enumerable.Range(0, 10).Select((x) => new object[] { x });

    [DynamicData(nameof(WeakFuzzTestData))]
    [TestMethod]
    public void WeakCollectionBehaviorFuzzTest(int seed)
    {
        // This fuzzes the weak collection behaviour itself: we perform a bunch of random operations while deliberately allowing some values to die in the
        // background, then periodically force a GC and assert the core invariants of a weak value dictionary:
        //   - values we still hold a strong reference to are never lost, and
        //   - values that have actually been collected are never handed back.
        Random r = new(seed);
        var weakDictionary = new WeakValueDictionary<int, object>();
        var kept = new Dictionary<int, object>(); // Keys whose current value we hold a strong reference to (guaranteed live).
        var dying = new HashSet<int>(); // Keys whose current value has no strong reference and may be collected.
        const int Operations = 2000;
        const int MaxKeys = 50;
        const int GcInterval = 500;

        // Allocates a value inside a non-inlined helper so that it isn't accidentally rooted by a leftover stack/register reference, and adds it to the
        // dictionary.
        void AddDyingValue(int key)
        {
            Helpers.NotInlined((dictionary: weakDictionary, key), static state =>
            {
                state.dictionary[state.key] = new();
            });
        }

        void VerifyAfterGc()
        {
            Helpers.ForceGC();

            // Every value we still hold a strong reference to must remain retrievable and identical.
            foreach (var entry in kept)
            {
                weakDictionary.TryGetValue(entry.Key, out object? current).ShouldBeTrue();
                current.ShouldBeSameAs(entry.Value);
            }

            // Any value we let die must never be handed back once it has actually been collected.
            foreach (int key in dying)
                weakDictionary.TryGetValue(key, out _).ShouldBeFalse();

            // Enumeration must expose every guaranteed-live entry (and, by design, never yields collected values).
            var snapshot = weakDictionary.ToList();

            foreach (var entry in kept)
                snapshot.ShouldContain(entry);
        }

        for (int i = 0; i < Operations; i++)
        {
            int key = r.Next(MaxKeys);

            switch (r.Next(4))
            {
                case 0:
                    // Add a value we keep alive: it must survive every subsequent GC until removed or overwritten.
                    object value = new();
                    weakDictionary[key] = value;
                    kept[key] = value;
                    dying.Remove(key);
                    break;
                case 1:
                    // Add a value we allow to die in the background.
                    AddDyingValue(key);
                    dying.Add(key);
                    kept.Remove(key);
                    break;
                case 2:
                    // Remove the key entirely.
                    weakDictionary.Remove(key);
                    kept.Remove(key);
                    dying.Remove(key);
                    break;
                case 3:
                    // Look up a key we are guaranteeing is alive.
                    if (kept.TryGetValue(key, out object? expected))
                    {
                        weakDictionary.TryGetValue(key, out object? actual).ShouldBeTrue();
                        actual.ShouldBeSameAs(expected);
                    }

                    break;
            }

            if ((i + 1) % GcInterval == 0)
                VerifyAfterGc();
        }

        VerifyAfterGc();

        GC.KeepAlive(kept);
        GC.KeepAlive(weakDictionary);
    }
}
