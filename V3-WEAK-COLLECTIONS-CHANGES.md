# Singulink.Collections.Weak v3 Changes and Migration Guide

Version 3 of `Singulink.Collections.Weak` is a ground-up rewrite of how dead values are removed. The primary goals were to:

1. Remove dead entries automatically as the garbage collector reclaims their values, instead of relying on periodic cleaning triggered by adds or by the user.
2. Make the collections safe for concurrent use without external locking.
3. Expose the structure of `WeakList` so that callers can hold on to positions, insert relative to them, and enumerate in both directions while other threads modify the list.

If you only ever added values and enumerated them, your migration is a handful of renames. If you relied on manual cleaning, capacity management or `WeakCollection`, read on.

---

## 1. Target framework changes

- **Dropped:** `net6.0`, `net6.0-windows10.0.19041` (out of Microsoft support).
- **Added:** `net10.0`, `net10.0-windows10.0.19041`.
- Still supported: `netstandard2.0`, `netstandard2.1`, `net8.0(-windows10.0.19041)`, `net9.0(-windows10.0.19041)`.

**Migration:** Move off .NET 6.

---

## 2. Automatic cleanup replaces manual cleaning

In v2 a dead value stayed in the collection, taking up space and being skipped during enumeration, until something cleaned it out. Cleaning happened when `Clean()` was called, or automatically after `AutoCleanAddCount` adds, and `TrimExcessDuringClean` and `TrimExcess()` controlled whether the backing storage shrank afterwards. `UnsafeCount` reported the size including dead entries.

In v3 every entry is tied to its value's lifetime. When the garbage collector reclaims a value, the entry is removed from the collection shortly afterwards on the finalizer thread, with no action from the caller and no scan of the collection. As a result, the following members have been **removed** from `WeakList` and `WeakValueDictionary`:

- `AutoCleanAddCount`
- `AddCountSinceLastClean`
- `TrimExcessDuringClean`
- `Clean()`
- `TrimExcess()`
- `EnsureCapacity(int)`
- `Capacity` and `ExtraTrimCapacity` (on `WeakList`)

### Why this matters

Manual cleaning was easy to forget and hard to tune. Clean too rarely and a collection that received many short-lived values grew without bound between cleans, with every enumeration paying to skip the dead entries. Clean too often and every add paid for a full scan. The right `AutoCleanAddCount` depended on the workload, which the library could not know. Tying entries to the value's lifetime removes the knob entirely, and the cost of removal is paid once per dead value rather than once per scan.

### Migration

Delete the calls. There is nothing to replace them with. Code that called `Clean()` before enumerating to get accurate results can enumerate directly. Code that tuned `AutoCleanAddCount` for memory reasons no longer needs to, since dead entries do not accumulate.

`Count` on both collections returns the number of entries, which is exact apart from values that have died but whose entries have not yet been removed by the finalizer thread. `UnsafeCount` still exists on both for compatibility but is obsolete and hidden from IntelliSense; it returns the same value as `Count`.

---

## 3. Both collections are safe for concurrent use

`WeakList` and `WeakValueDictionary` are safe for concurrent access at an operation level. Individual calls can be made from any thread at any time without external locking, including while another thread is enumerating. Compound operations that must observe a consistent view still need your own synchronization, or, for `WeakList`, the locked operation methods described in section 5.

This was a necessary consequence of automatic cleanup, since removals now happen on the finalizer thread while user code is running, but it is also a feature in its own right: event subscriber lists and caches are commonly shared across threads, and v2 required a lock around every access.

### Migration

Remove locks that existed only to protect single operations on these collections. Keep locks that protect sequences of operations, or move those sequences into `UnsafePerformLockedOperation` on `WeakList`.

---

## 4. `WeakCollection` has been removed

`WeakCollection<T>` was an unordered variant of `WeakList<T>` that existed because the v2 list stored entries in an array and paid for ordering on removal. The v3 list is a linked list, so maintaining order is free and the unordered type no longer has a reason to exist.

### Migration

Replace `WeakCollection<T>` with `WeakList<T>`, and `Add` with `AddLast`:

```csharp
// --- Before ---
var handlers = new WeakCollection<IHandler>();
handlers.Add(handler);

// --- After ---
var handlers = new WeakList<IHandler>();
handlers.AddLast(handler);
```

`Remove`, `Contains`, `Clear` and enumeration work the same way.

---

## 5. `WeakList` is now a node-based linked list

`WeakList<T>` is rebuilt around a `Node` type that represents a position in the list. Adding a value returns its node, and nodes can be used to remove the value in O(1), insert relative to it, start an enumeration from it, or check whether it is still in the list.

### Renamed members

| v2 | v3 |
| --- | --- |
| `Add(item)` | `AddLast(value)` returns `Node` |
| `InsertFirst(item)` | `AddFirst(value)` returns `Node` |
| `InsertBefore(findItem, item)` | `InsertBefore(existingValue, value)` returns `Node` |
| `InsertAfter(findItem, item)` | `InsertAfter(existingValue, value)` returns `Node` |
| `TryInsertBefore(findItem, item)` returned `bool` | `TryInsertBefore(existingValue, value)` returns `Node?` |
| `TryInsertAfter(findItem, item)` returned `bool` | `TryInsertAfter(existingValue, value)` returns `Node?` |
| `UnsafeCount` | `Count` |

The value-based `Remove(value)` and `Contains(value)` overloads with an optional comparer are unchanged.

### New members

- `AddBefore(node, value)` and `AddAfter(node, value)` insert relative to a node you already hold, in O(1), instead of searching for a value first.
- `Remove(node)` and `Node.Dispose()` remove a specific entry in O(1). Removing by node is the right choice when the same value may appear more than once, or when you want to avoid the search.
- `Find(value)` and `Find(predicate)` return the node and value of the first match.
- `Node.Value` returns the value, or `null` once it has been collected. `Node.IsRemoved` reports whether the node is still in the list. `Node.List` returns the owning list.
- `Node.TryUpdateTarget(newValue)` swaps the value a node refers to without changing its position. This is available on .NET only, since it depends on `DependentHandle`.
- `GetNodeEnumerator()` enumerates nodes rather than values, and both enumerators accept a start node, support `MovePrevious()`, and expose `WasAddedDuringEnumeration` to tell nodes added by other threads during the enumeration apart from nodes that existed when it started. The `AsEnumerable(reversed, skipNewNodes)` methods adapt an enumerator for LINQ with those options.
- `Version` and `Node.Version` are monotonically increasing values that allow callers to tell whether a node was added before or after a point in time.
- `UnsafePerformLockedOperation` and `UnsafeTryPerformLockedOperation` run a callback while holding the list's internal lock, for compound operations that must not interleave with other threads. Keep such callbacks short: the finalizer thread also needs the lock to remove dead entries, and holding it for long blocks finalization of every object in the process.
- `Dispose()` removes all nodes and makes the list unusable. It is faster than `Clear()`, which removes nodes one at a time and leaves the list reusable. Dispose lists you are finished with rather than letting the finalizer do it, since finalizing a large list is slow.

### Enumeration semantics

Enumeration is safe while other threads add or remove values. Values that die during enumeration are skipped, nodes removed by another thread are skipped, and nodes added by another thread after the current position are visited unless `skipNewNodes` is used. An enumeration never throws because the list was modified.

### Migration

Rename the methods listed above and discard the returned node if you do not need it. Code that stored values in order to remove them later can store the node instead and call `Remove(node)`.

```csharp
// --- Before ---
var subscribers = new WeakList<ISubscriber>();
subscribers.Add(subscriber);
...
subscribers.Remove(subscriber);

// --- After ---
var subscribers = new WeakList<ISubscriber>();
var node = subscribers.AddLast(subscriber);
...
subscribers.Remove(node);   // O(1), or keep using subscribers.Remove(subscriber)
```

---

## 6. `WeakValueDictionary` changes

Apart from the removed cleaning members in section 2 and thread safety in section 3, the dictionary's API is unchanged: indexer, `TryGetValue`, `TryAdd`, `Add`, the `Remove` and `Contains` overloads, `ContainsKey`, `ContainsValue`, `Keys`, `Values`, `Clear` and enumeration all behave as before, and `AlternateLookup` support on .NET 9+ is retained.

Two things are new:

- `Dispose()` removes all entries and makes the dictionary unusable. As with `WeakList`, it is faster than `Clear()` and should be preferred for dictionaries you are finished with.
- The type is now `sealed`.

One behavior is worth knowing about. When a value dies, its entry lingers until the finalizer thread removes it. If an equal key is added during that window, the dead entry is replaced in place and the dictionary keeps the key object it already had, so `Keys` and enumeration return the original key instance rather than the one passed to the later add. This is the same key-retention rule as `Dictionary<TKey, TValue>` and only matters when equal keys are distinguishable, for example by reference or by data the comparer ignores. If your code needs the new key instance to be stored, lock around the operations and call `Remove(key)` before adding.

---

## 7. .NET Standard memory fix

On .NET Standard targets, v2's `WeakList` kept memory alive longer than necessary when a removed value was still reachable elsewhere. This is fixed in v3.

---

## TL;DR

- If you used `WeakList` for add, enumerate and remove: rename `Add` to `AddLast`, delete any `Clean()` or `AutoCleanAddCount` code, and you are done. Consider keeping the returned nodes for O(1) removal.
- If you used `WeakCollection`: switch to `WeakList`.
- If you used `WeakValueDictionary`: delete any cleaning code. Everything else is unchanged.
- Remove locks that only protected single operations. The collections are now safe for concurrent use.
- Dispose collections you are done with rather than relying on the finalizer.
