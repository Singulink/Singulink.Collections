# Singulink Collections Version History

## V3.0
### Singulink.Collections
- Breaking change: Deprecated `Singulink.Collections.Abstractions` package. Types have been merged into the main `Singulink.Collections` package.
- Performance improvements to `ListDictionary<,>.ValueList` and `HashSetDictionary<,>.ValueSet`.
- Documentation updates.

## V2.0.1
### Singulink.Collections.Weak
- Documentation updates.

## V2.0
### All packages
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations

### Singulink.Collections.Weak
- Minor bug fix to validation of `AutoCleanAddCount` property setters in `Collections.Weak` (thanks [@procudin](https://github.com/procudin)!)

### Singulink.Collections
- Breaking change: `AsTransient()` methods on value lists and value sets was renamed to `AsTransientReadOnly()`
