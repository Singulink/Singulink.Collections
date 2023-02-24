# Singulink.Collections

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.Collections.svg)](https://www.nuget.org/packages/Singulink.Collections/)
[![Build and Test](https://github.com/Singulink/Singulink.Collections/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Collections?query=workflow%3A%22build+and+test%22)

**Singulink.Collections** provides generally useful collections that are missing from .NET. They are highly optimized for performance, well documented and follow the same design principles as built-in .NET collections so they should feel instantly familiar.

The following collections are included in the package:
- `HashSetDictionary`: Collection of keys mapped to a hash set of unique values per key.
- `ListDictionary`: Collection of keys mapped to a list of values per key.
- `Map`: Collection of two types of values that map between each other in a bidirectional one-to-one relationship.
- `ReadOnlyHashSet`: Fast read-only wrapper around a HashSet.
- `ReadOnlyList`: Fast read-only wrapper around a List.

A full set of interfaces and read-only wrappers for the new collections is included as well. The interfaces are in a separate `Singulink.Collections.Abstractions` package that is references from the main package.

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

The package is available on NuGet - simply install the `Singulink.Collections` package.

**Supported Runtimes**: Anywhere .NET Standard 2.0+ is supported, including:
- .NET Core 2.0+
- .NET Framework 4.6.1+
- Mono 5.4+
- Xamarin.iOS 10.14+
- Xamarin.Android 8.0+

## Usage

### ListDictionary

```c#
var numberNames = new ListDictionary<int, string>();
numberNames[1].AddRange("One", "Uno");
numberNames[2].AddRange("Two", "Dos");

numberNames.ContainsValue("Two"); // true

ValueList threeNames = numberNames[3];

// Empty lists are not part of the dictionary until a value is added:
numberNames.ContainsKey(3); // false

threeNames.Add("Three");
numberNames.ContainsKey(3); // true

// Lists are automatically removed from the dictionary when they become empty again
threeNames.Clear();
numberNames.TryGetValues(3, out threeNames); // false
```

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

## Further Reading

You can view the fully documented API on the [project documentation site](https://www.singulink.com/Docs/Singulink.Collections/api/Singulink.Collections.html).