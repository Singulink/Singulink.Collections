# Singulink.Collections v4 — Changes & Migration Guide

Version 4 of `Singulink.Collections` is a focused restructuring release. The primary goals were to:

1. Build a clean, layered interface hierarchy for the collection dictionaries so they compose cleanly with each other and with the BCL.
2. Replace the ad‑hoc "BCL adapter" extension method surface with a small set of conceptually well‑defined interfaces that are easy to teach and easy to consume.
3. Integrate cleanly with LINQ via `IGrouping<,>` and `ILookup<,>`.

If you were consuming the concrete `HashSetDictionary<,>`, `ListDictionary<,>`, or `Map<,>` types directly, or exposing them through the Singulink `I[ReadOnly]ListDictionary` / `I[ReadOnly]SetDictionary` / `I[ReadOnly]CollectionDictionary` interfaces, your migration is minimal — almost all existing code will continue to work as‑is.

If, on the other hand, you were relying on the various BCL projection adapters (e.g. `AsReadOnlyDictionaryOfList()`, `AsDictionaryOfCollection()`, the implicit `IReadOnlyDictionary<TKey, IList<TValue>>` implementation, etc.) you will need to make some changes. The reasoning behind those breaks is explained below.

---

## 1. Target framework changes

- **Dropped:** `net6.0`, `net6.0-windows10.0.19041` (out of Microsoft support).
- **Added:** `net10.0`, `net10.0-windows10.0.19041`.
- Still supported: `netstandard2.0`, `netstandard2.1`, `net8.0(-windows10.0.19041)`, `net9.0(-windows10.0.19041)`.

**Migration:** Move off .NET 6.

---

## 2. New "keyed collection" interface family

A new family of interfaces describes a value collection that knows its key and integrates directly with LINQ grouping APIs:

```
IReadOnlyKeyedCollection<TKey, TValue>   : IGrouping<TKey, TValue>, IReadOnlyCollection<TValue>
IReadOnlyKeyedList<TKey, TValue>         : IReadOnlyKeyedCollection<TKey, TValue>, IReadOnlyList<TValue>
IReadOnlyKeyedSet<TKey, TValue>          : IReadOnlyKeyedCollection<TKey, TValue>, IReadOnlySet<TValue>

IKeyedCollection<TKey, TValue>           : IReadOnlyKeyedCollection<TKey, TValue>, ICollection<TValue>
IKeyedList<TKey, TValue>                 : IReadOnlyKeyedList<TKey, TValue>,    IKeyedCollection<TKey, TValue>, IList<TValue>
IKeyedSet<TKey, TValue>                  : IReadOnlyKeyedSet<TKey, TValue>,     IKeyedCollection<TKey, TValue>, ISet<TValue>
```

The concrete `ListDictionary<,>.ValueList` and `HashSetDictionary<,>.ValueSet` (and their read‑only variants) implement these new interfaces.

### Why this matters

Because every value collection now implements `IGrouping<TKey, TValue>`, a collection dictionary is conceptually identical to an `ILookup<TKey, TValue>` — and we now make that explicit (see §4). You also get free interoperability with `Enumerable.GroupBy` / `Enumerable.ToLookup` consumers and producers.

---

## 3. Collection dictionary interfaces are now properly layered

Previously the read‑only and mutable dictionary interfaces were largely parallel sibling hierarchies. They now form a proper inheritance chain that mirrors the BCL `IReadOnlyDictionary` ⇄ `IDictionary` relationship:

```
IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection>
        ▲                       ▲                   ▲
        │                       │                   │
IReadOnlyCollectionDictionary<TKey, TValue>   IReadOnlyListDictionary<TKey, TValue>   IReadOnlySetDictionary<TKey, TValue>

ICollectionDictionary<TKey, TValue, TValueCollection>  : IReadOnlyCollectionDictionary<TKey, TValue, TValueCollection>
ICollectionDictionary<TKey, TValue>                    : ICollectionDictionary<TKey, TValue, IKeyedCollection<TKey, TValue>>, IReadOnlyCollectionDictionary<TKey, TValue>
IListDictionary<TKey, TValue>                          : ICollectionDictionary<TKey, TValue, IKeyedList<TKey, TValue>>,       IReadOnlyListDictionary<TKey, TValue>
ISetDictionary<TKey, TValue>                           : ICollectionDictionary<TKey, TValue, IKeyedSet<TKey, TValue>>,        IReadOnlySetDictionary<TKey, TValue>
```

`IMap<,>` similarly now derives from `IReadOnlyMap<,>`.

### Why this matters

You can pass an `IListDictionary<TKey, TValue>` anywhere an `IReadOnlyCollectionDictionary<TKey, TValue>` is expected — no wrapper allocation needed. Generic constraints work as you would expect. Cast paths between the interfaces work as you would expect. Method overload resolution is unsurprising.

### Migration

Almost always: **none**. The signatures you were using are unchanged. The only edge case is generic constraints that previously required `where T : IListDictionary<,>, IReadOnlyListDictionary<,>` — the second constraint is now redundant and can be removed.

---

## 4. `ILookup<,>` integration

Two new extension methods are provided on the read‑only dictionary interfaces, and one instance method is provided on `HashSetDictionary` and `ListDictionary`:

```csharp
// Extensions on IReadOnly[Collection|List|Set]Dictionary<TKey, TValue>
public static ILookup<TKey, TValue> AsLookup<TKey, TValue>(this IReadOnly...Dictionary<TKey, TValue> dictionary);
public static ILookup<TKey, TValue> ToLookup<TKey, TValue>(this IReadOnly...Dictionary<TKey, TValue> dictionary,
                                                           IEqualityComparer<TKey>? keyComparer = null);

// Instance method on the concrete dictionaries (uses the dictionary's own KeyComparer)
public ILookup<TKey, TValue> ToLookup();
```

- `AsLookup()` returns a **live** view. Changes to the underlying dictionary are reflected in the lookup.
- `ToLookup()` returns a **snapshot**. It is hand‑optimized for collection dictionaries: the groupings are pre‑sized, no per‑element rehashing is performed, and the source dictionary's `KeyComparer` is used when calling the instance method.

This is the recommended replacement when you previously projected a dictionary to `IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>` purely to feed it to LINQ.

---

## 5. Removed: BCL projection adapters

The following extension methods have been **removed**:

- `AsCollectionDictionary(this IListDictionary<TKey, TValue>)`
- `AsCollectionDictionary(this ISetDictionary<TKey, TValue>)`
- `AsReadOnlyCollectionDictionary(this IListDictionary<TKey, TValue>)`
- `AsReadOnlyCollectionDictionary(this ISetDictionary<TKey, TValue>)`
- `AsReadOnlyCollectionDictionary(this IReadOnlySetDictionary<TKey, TValue>)`
- `AsReadOnlyDictionaryOfList(this IListDictionary<TKey, TValue>)`
- `AsReadOnlyDictionaryOfList(this IReadOnlyListDictionary<TKey, TValue>)`
- `AsReadOnlyDictionaryOfSet(this ISetDictionary<TKey, TValue>)`
- `AsReadOnlyDictionaryOfSet(this IReadOnlySetDictionary<TKey, TValue>)`
- `AsDictionaryOfCollection(this IListDictionary<TKey, TValue>)`
- `AsDictionaryOfCollection(this ISetDictionary<TKey, TValue>)`
- `AsReadOnlyDictionaryOfCollection(...)` (all overloads)

In addition, `HashSetDictionary<TKey, TValue>` and `ListDictionary<TKey, TValue>` **no longer directly implement** `IReadOnlyDictionary<TKey, IList<TValue>>` / `IReadOnlyDictionary<TKey, ISet<TValue>>` / `IReadOnlyDictionary<TKey, ICollection<TValue>>`.

### Why these were removed

The BCL projection surface had grown large, confusing, and conceptually muddled:

- **Combinatorial explosion.** Every dictionary type × every BCL value collection type × read‑only/mutable produced a new extension. The user had to read a long list of similarly‑named methods (`AsReadOnlyDictionaryOfList`, `AsReadOnlyDictionaryOfCollection`, `AsDictionaryOfCollection`, ...) to find the one that matched their exact widening.
- **Wrong abstraction.** A collection dictionary is conceptually an `ILookup<TKey, TValue>`, not an `IReadOnlyDictionary<TKey, ICollection<TValue>>`. The BCL projections forced consumers to think in terms of "a dictionary whose values are collections," which obscured the more useful grouping semantics and prevented LINQ interop.
- **Semantic mismatch.** `IReadOnlyDictionary<TKey, IList<TValue>>` says "this is a read‑only dictionary," but mutations through the inner `IList<TValue>` were still possible and would silently mutate the source. That contradicted user expectations of a read‑only contract.
- **Adapter allocations everywhere.** Each projection method allocated a wrapper plus per‑value wrappers on enumeration. The new design avoids that entirely because the value collections natively implement the appropriate BCL interfaces (`IList<T>`, `IReadOnlyList<T>`, `ISet<T>`, `IReadOnlySet<T>`).

### Replacements

The new design gives you cleaner choices and better performance. Pick whichever of the following matches what you actually need:

| Old surface | New surface |
| --- | --- |
| `dict.AsReadOnly()` returning a typed read‑only Singulink interface | Same — `AsReadOnly()` is still there for all three dictionary kinds and now returns the layered `IReadOnly*Dictionary<TKey, TValue>` interfaces. |
| `dict.AsCollectionDictionary()` (widening a list/set dictionary to a collection dictionary) | Same — still there. |
| `dict.AsReadOnlyCollectionDictionary()` | Same — still there for the read‑only widening too. |
| `dict as IReadOnlyDictionary<TKey, IList<TValue>>` (used as a BCL dictionary for indexer/`TryGetValue`/`Keys`/`Values`) | Expose as `IListDictionary<TKey, TValue>` — same shape, value collections are still `IList<TValue>`. |
| `dict.AsReadOnlyDictionaryOfList()` (used as a fully read‑only BCL dictionary) | Expose as `IReadOnlyListDictionary<TKey, TValue>` via `dict.AsReadOnly()` — same shape, value collections are `IReadOnlyList<TValue>`. |
| `dict.AsReadOnlyDictionaryOfCollection()` | Expose as `IReadOnlyCollectionDictionary<TKey, TValue>` via `dict.AsReadOnly()`. |
| Anywhere you fed a projected `IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>` into LINQ for grouping | Use `dict.AsLookup()` or `dict.ToLookup()` and consume as `ILookup<TKey, TValue>` / `IEnumerable<IGrouping<TKey, TValue>>`. |

#### Concrete migration examples

```csharp
// --- Before ---
public IReadOnlyDictionary<int, IReadOnlyList<string>> NumberNames => _numberNames.AsReadOnlyDictionaryOfList();

// --- After ---
public IReadOnlyListDictionary<int, string> NumberNames => _numberNames.AsReadOnly();
// IReadOnlyListDictionary<int, string>.this[int] returns IReadOnlyKeyedList<int, string>
// which itself implements IReadOnlyList<string> and IGrouping<int, string>.
```

```csharp
// --- Before ---
public IReadOnlyDictionary<int, ICollection<string>> NumberNames => _numberNames.AsDictionaryOfCollection();

// --- After (preferred) ---
public ICollectionDictionary<int, string> NumberNames => _numberNames.AsCollectionDictionary();
// .this[int] returns IKeyedCollection<int, string> : ICollection<string>.

// --- After (if you really need a BCL IReadOnlyDictionary shape) ---
// There is no longer a built-in adapter. Either rethink whether the consumer should accept
// IReadOnlyCollectionDictionary instead (almost always yes), or write a small project-specific
// adapter that wraps the dictionary.
```

```csharp
// --- Before ---
IEnumerable<IGrouping<int, string>> groups =
    _numberNames.AsReadOnlyDictionaryOfList()
                .SelectMany(kvp => kvp.Value.GroupBy(_ => kvp.Key));

// --- After ---
ILookup<int, string> groups = _numberNames.AsLookup();          // live view
// or:
ILookup<int, string> snapshot = _numberNames.ToLookup();        // snapshot, optimized
```

---

## 6. Internal cleanup (not a breaking change, but visible if you used reflection)

Several internal adapter classes were rewritten and renamed under `Singulink.Collections.Internal`. None of these were public. All adapter types are now `sealed partial` for AOT/CsWinRT compatibility.

---

## TL;DR

- If you used the concrete types or the Singulink dictionary interfaces — **you're done**, just retarget.
- If you used the BCL projection extensions (`AsReadOnlyDictionaryOfList`, etc.) or relied on `HashSetDictionary` / `ListDictionary` implementing `IReadOnlyDictionary<TKey, ICollection<TValue>>`/etc. directly — switch to the Singulink interface (`I[ReadOnly]ListDictionary`, etc.) or to `AsLookup()` / `ToLookup()`. The new APIs are smaller, more discoverable, allocate less, and integrate with LINQ.
