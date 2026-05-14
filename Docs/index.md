<div class="article">

# Singulink.Collections

## Overview

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
- `WeakCollection`: Collection of weakly referenced values that keeps items in an undefined order.
- `WeakList` / `ConcurrentWeakList`: Collection of weakly referenced values that maintains relative insertion order.
- `WeakValueDictionary`: Collection of keys and weakly referenced values (with `AlternateLookup` support).

> [!IMPORTANT]
> `Singulink.Collections` v4 contains breaking changes. See the [v4 changes and migration guide](https://github.com/Singulink/Singulink.Collections/blob/main/V4-COLLECTIONS-CHANGES.md) on GitHub for details.

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

## Information and Links

Here are some additional links to get you started:

- [API Documentation](api/Singulink.Collections.yml) - Browse the fully documented API here.
- [Chat on Discord](https://discord.gg/EkQhJFsBu6) - Have questions or want to discuss the library? This is the place for all Singulink project discussions.
- [Github Repo](https://github.com/Singulink/Singulink.Collections) - File issues, contribute pull requests or check out the code for yourself!

</div>
