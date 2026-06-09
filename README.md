# Singulink Collections

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)
[![Build and Test](https://github.com/Singulink/Singulink.Collections/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Collections?query=workflow%3A%22build+and+test%22)

| Library | Package |
| --- | --- |
| **Singulink.Collections** | [![View nuget packages](https://img.shields.io/nuget/v/Singulink.Collections.svg)](https://www.nuget.org/packages/Singulink.Collections/) |
| **Singulink.Collections.Weak** | [![View nuget packages](https://img.shields.io/nuget/v/Singulink.Collections.Weak.svg)](https://www.nuget.org/packages/Singulink.Collections.Weak/) |

> [!IMPORTANT]
> **`Singulink.Collections` v4 contains breaking changes.** The interface hierarchies have been restructured, value collections now implement `IGrouping<TKey, TValue>` for free LINQ interop, and the BCL projection extension methods (e.g. `AsReadOnlyDictionaryOfList`, `AsDictionaryOfCollection`) have been removed in favor of a smaller, cleaner surface. Impact is minimal for code that uses the concrete types or Singulink dictionary interfaces. See the [**v4 changes and migration guide**](V4-COLLECTIONS-CHANGES.md) if you were relying on the BCL adapters/shims.
>
> `Singulink.Collections.Weak` v3 also dropped .NET 6 support and adds `ConcurrentWeakList`.
> `Singulink.Collections.Weak` v4 removes the old `WeakList` and `WeakCollection` types; `WeakList` is now a self-cleaning, thread-safe collection that maintains relative insertion order (formerly known as `ConcurrentWeakList`, which has been renamed).

**Singulink.Collections** provides generally useful collections that are missing from .NET. They are highly optimized for performance, well documented and follow the same design principles as built-in .NET collections so they should feel instantly familiar.

The following is included in the package:
- `HashSetDictionary`: Collection of keys mapped to a hash set of unique values per key (with `AlternateLookup` support).
- `ListDictionary`: Collection of keys mapped to a list of values per key (with `AlternateLookup` support).
- `Map`: Collection of two types of values that map between each other in a bidirectional one-to-one relationship (with `AlternateLookup` support).
- `EquatableArray` / `ComparerEquatableArray`: Array wrappers that implement value equality semantics based on the contents of the array.
- `ReadOnlyHashSet`: Fast direct read-only wrapper for HashSets (instead of going through `ISet<>` like `ReadOnlySet` does).
- `ReadOnlyList`: Fast direct read-only wrapper for Lists (instead of going through `IList<>` like `ReadOnlyCollection` does).
- A full set of interfaces for the new collections, including the keyed-collection family (`IKeyedList`, `IKeyedSet`, ...) that implements `IGrouping<TKey, TValue>` for natural LINQ interop, plus an `IReadOnlySet<>` polyfill for .NET Standard.

**Singulink.Collections.Weak** provides a set of collection classes that store weak references to values so that the garbage collector is free to reclaim the memory they use when they aren't being referenced anymore. The values returned by the collections will never be `null` - if the value was garbage collected then the collection behaves as if the value was removed from the collection.

The following collections are included in the package:
- `WeakList`: Collection of weakly referenced values that maintains relative insertion order, is safe for concurrent use, and automatically removes values as they die.
- `WeakValueDictionary`: Collection of keys and weakly referenced values (with `AlternateLookup` support).

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

The packages are available on NuGet - simply install the `Singulink.Collections` and/or `Singulink.Collections.Weak` packages.

**Supported Runtimes**: Everywhere .NET Standard 2.0 is supported, including:
- .NET
- .NET Framework
- Mono / Xamarin

End-of-life runtime versions that are no longer officially supported are not tested or supported by this library.

## Example Usages

### Map

```c#
// Create a map and optionally specify left and right value equality comparers
var numberToNameMap = new Map<int, string>(null, StringComparer.OrdinalIgnoreCase);

numberToNameMap.Add(1, "One");
numberToNameMap.Add(2, "Two");

numberToNameMap.ContainsLeft(2); // true

// Override values with indexer
numberToNameMap[2] = "Dos";
numberToNameMap.ContainsRight("Two"); // false, was overridden above

// Get a map with the reverse relationship
var nameToNumberMap = numberToNameMap.Reverse;

int one = nameToNumberMap["ONE"]; // 1

```

### ListDictionary

```c#
var numberNames = new ListDictionary<int, string>();
numberNames[1].AddRange("One", "Uno");
numberNames[2].AddRange("Two", "Dos");

numberNames.ContainsValue("Two"); // true

// Empty lists are not part of the dictionary until a value is added

var threeNamesList = numberNames[3];
numberNames.ContainsKey(3); // false

threeNamesList.Add("Three");
numberNames.ContainsKey(3); // true

// Lists are automatically removed from the dictionary when they are empty

threeNamesList.Clear();
numberNames.TryGetValues(3, out threeNamesList); // false

// Examples of some of the supported ways to expose the dictionary through interfaces depending on your
// preferences and needs:

public class YourClass
{
    private ListDictionary<int, string> _numberNames;

    // Expose as Singulink IListDictionary (value collections are IKeyedList<int, string>,
    // which implements IList<string> and IGrouping<int, string>).
    public IListDictionary<int, string> NumberNames => _numberNames;

    // Expose as Singulink IReadOnlyListDictionary (value collections are IReadOnlyKeyedList<int, string>,
    // which implements IReadOnlyList<string> and IGrouping<int, string>). True read-only — cannot be
    // downcast back to a mutable dictionary.
    public IReadOnlyListDictionary<int, string> NumberNames => _numberNames.AsReadOnly();

    // Expose as Singulink ICollectionDictionary if the consumer doesn't need list semantics
    // (value collections are IKeyedCollection<int, string> : ICollection<string>).
    public ICollectionDictionary<int, string> NumberNames => _numberNames.AsCollectionDictionary();

    // Expose as Singulink IReadOnlyCollectionDictionary.
    public IReadOnlyCollectionDictionary<int, string> NumberNames => _numberNames.AsReadOnlyCollectionDictionary();

    // Expose as an ILookup<int, string> for LINQ-style consumption.
    // AsLookup() is a live view; ToLookup() returns an optimized snapshot using the dictionary's key comparer.
    public ILookup<int, string> NumberNames => _numberNames.AsLookup();
}
```

> [!TIP]
> Coming from v3? See the [v4 changes and migration guide](V4-COLLECTIONS-CHANGES.md) for the rationale and replacements for the removed BCL projection extensions (`AsReadOnlyDictionaryOfList`, `AsDictionaryOfCollection`, ...).

### WeakList

```c#
var subscribers = new WeakList<EventSubscriber>();
subscribers.AddLast(subscriber1);
subscribers.AddLast(subscriber2);

// The list type automatically drops any items that have been garbage collected, and you can even enumerate concurrently to this automatic process.
foreach (var s in subscribers)
    s.Notify();
```

`WeakList<T>` automatically cleans up references to garbage collected values and is safe for concurrent access at an operation level, so individual operations are thread-safe without external locking.

### WeakValueDictionary

```c#
var cache = new WeakValueDictionary<string, Image>();
cache["logo"] = LoadImage("logo.png");

// Returns true only if the value is still alive (not GC'd):
if (cache.TryGetValue("logo", out var logo))
    Render(logo);

// Entries whose values have been collected behave as if they were removed:
cache.ContainsKey("logo"); // false once the Image has been GC'd
```

## Further Reading

You can view the fully documented API on the [project documentation site](https://www.singulink.com/Docs/Singulink.Collections/api/Singulink.Collections.html).
