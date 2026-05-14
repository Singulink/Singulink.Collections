# Singulink Collections Version History

## Singulink.Collections

### Version 4.0

See the [v4 changes and migration guide](V4-COLLECTIONS-CHANGES.md) for full details, rationale, and migration examples.

- Drop .NET 6 support
- New keyed-collection interface family (`I[ReadOnly]KeyedCollection`/`List`/`Set`) - value collections now implement `IGrouping<TKey, TValue>` for free LINQ interop
- Restructured collection-dictionary interface hierarchy so `ICollectionDictionary<,>` derives from `IReadOnlyCollectionDictionary<,>`; `IMap<,>` now derives from `IReadOnlyMap<,>`
- New `ILookup<TKey, TValue>` integration: `AsLookup()` (live view) extension and `ToLookup()` (snapshot, hand-optimized) extension + instance method on the concrete dictionaries
- **Breaking:** Removed the BCL projection extensions (`AsReadOnlyDictionaryOfList`, `AsReadOnlyDictionaryOfSet`, `AsReadOnlyDictionaryOfCollection`, `AsDictionaryOfCollection`, and the `IListDictionary`/`ISetDictionary` overloads of `AsCollectionDictionary` / `AsReadOnlyCollectionDictionary` / `AsReadOnly`). `HashSetDictionary` and `ListDictionary` no longer directly implement `IReadOnlyDictionary<TKey, IList<TValue>>` / `IReadOnlyDictionary<TKey, ISet<TValue>>` / etc.
- Impact should be minimal if you used the concrete types or the Singulink dictionary interfaces. If you relied on the BCL adapters/shims see the migration guide for replacements - they are simpler, allocate less, and integrate better with LINQ

### Version 3.3

- Add `EquatableArray` and `ComparableEquatableArray`

### Version 3.2
- `AlternateLookup` support for collection dictionaries
- WinRT compatibility

### Version 3.1
- Full support for AOT and trimming

### Version 3.0
- Breaking change: Deprecated `Singulink.Collections.Abstractions` package. Types have been merged into the main `Singulink.Collections` package
- Performance improvements to `ListDictionary<,>.ValueList` and `HashSetDictionary<,>.ValueSet`
- Documentation updates

### Version 2.0
- Breaking change: `AsTransient()` methods on value lists and value sets was renamed to `AsTransientReadOnly()`
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations

## Singulink.Collections.Weak

### Version 3.0

 - Drop .NET 6 support
 - Add `ConcurrentWeakList`

### V2.2
- `AlternateLookup` support for `WeakDictionary`
- WinRT compatibility

### V2.1
- Full support for AOT and trimming

### V2.0
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations
- Minor bug fix to validation of `AutoCleanAddCount` property setters in `Collections.Weak` (thanks [@procudin](https://github.com/procudin)!)