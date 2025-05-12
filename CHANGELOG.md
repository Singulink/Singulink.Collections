# Singulink Collections Version History

## Singulink.Collections

### V3.2
- `AlternateLookup` support for keyed collections
- WinRT compatibility

### V3.1
- Full support for AOT and trimming

### V3.0
- Breaking change: Deprecated `Singulink.Collections.Abstractions` package. Types have been merged into the main `Singulink.Collections` package
- Performance improvements to `ListDictionary<,>.ValueList` and `HashSetDictionary<,>.ValueSet`
- Documentation updates

### V2.0
- Breaking change: `AsTransient()` methods on value lists and value sets was renamed to `AsTransientReadOnly()`
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations

## Singulink.Collections.Weak

### V2.2
- `AlternateLookup` support for `WeakDictionary`
- WinRT compatibility

### V2.1
- Full support for AOT and trimming

### V2.0.1
- Documentation updates

### V2.0
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations
- Minor bug fix to validation of `AutoCleanAddCount` property setters in `Collections.Weak` (thanks [@procudin](https://github.com/procudin)!)