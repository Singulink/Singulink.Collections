<div class="article">

# Singulink.Collections

## Overview

**Singulink.Collections** provides generally useful collections that are missing from .NET. They are highly optimized for performance, well documented and follow the same design principles as built-in .NET collections so they should feel instantly familiar.

The following is included in the package:
- `HashSetDictionary`: Collection of keys mapped to a hash set of unique values per key (with `AlternateLookup` support).
- `ListDictionary`: Collection of keys mapped to a list of values per key (with `AlternateLookup` support).
- `Map`: Collection of two types of values that map between each other in a bidirectional one-to-one relationship (with `AlternateLookup` support).
- `ReadOnlyHashSet`: Fast direct read-only wrapper for HashSets (instead of going through `ISet<>` like `ReadOnlySet` does).
- `ReadOnlyList`: Fast direct read-only wrapper for Lists (instead of going through `IList<>` like `ReadOnlyCollection` does).
- A full set of interfaces for the new collections as well as an `IReadOnlySet<>` polyfill for .NET Standard.

**Singulink.Collections.Weak** provides a set of collection classes that store weak references to values so that the garbage collector is free to reclaim the memory they use when they aren't being referenced anymore. The values returned by the collections will never be `null` - if the value was garbage collected then the collection behaves as if the value was removed from the collection.

The following collections are included in the package:
- `WeakCollection`: Collection of weakly referenced values that keeps items in an undefined order.
- `WeakList`: Collection of weakly referenced values that maintains relative insertion order.
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

// Lists are automatically removed from the dictionary when they become empty

threeNamesList.Clear();
numberNames.TryGetValues(3, out threeNamesList); // false

// Examples of some of the supported ways to expose the dictionary through interfaces depending on your
// preferences and needs:

public class YourClass
{
    private ListDictionary<int, string> _numberNames;

    // Expose as Singulink IListDictionary (with .NET IList<string> values)
    public IListDictionary<int, string> NumberNames => _numberNames;

    // Expose as Singulink IReadOnlyListDictionary (with .NET IReadOnlyList<string> values)
    public IReadOnlyListDictionary<int, string> NumberNames => _numberNames.AsReadOnly();

    // Expose as Singulink ICollectionDictionary (with .NET ICollection<string> values)
    public ICollectionDictionary<int, string> NumberNames => _numberNames.AsCollectionDictionary();

    // Expose as Singulink IReadOnlyCollectionDictionary (with .NET IReadOnlyCollection<string> values)
    public IReadOnlyCollectionDictionary<int, string> NumberNames => _numberNames.AsReadOnlyCollectionDictionary();

    // Expose as .NET IReadOnlyDictionary<int, IList<string>>
    // Note that values can still be added/removed/modified through the value ILists even though it is
    // an IReadOnlyDictionary. Many of the additional API's present in the IDictionary interface are
    // not sensible in the context of a collection dictionary so that interface is not supported.
    public IReadOnlyDictionary<int, IList<string>> NumberNames => _numberNames;

    // Expose as .NET IReadOnlyDictionary<int, IReadOnlyList<string>> (fully read-only)
    public IReadOnlyDictionary<int, IReadOnlyList<string>> NumberNames => _numberNames.AsReadOnlyDictionaryOfList();

    // Expose as .NET IReadOnlyDictionary<int, ICollection<string>>
    public IReadOnlyDictionary<int, ICollection<string>> NumberNames => _numberNames.AsDictionaryOfCollection();

    // Expose as .NET IReadOnlyDictionary<int, IReadOnlyCollection<string>>
    public IReadOnlyDictionary<int, IReadOnlyCollection<string>> NumberNames => _numberNames.AsReadOnlyDictionaryOfCollection();
}
```

## Information and Links

Here are some additonal links to get you started:

- [API Documentation](api/Singulink.Collections.yml) - Browse the fully documented API here.
- [Chat on Discord](https://discord.gg/EkQhJFsBu6) - Have questions or want to discuss the library? This is the place for all Singulink project discussions.
- [Github Repo](https://github.com/Singulink/Singulink.Collections) - File issues, contribute pull requests or check out the code for yourself!

</div>