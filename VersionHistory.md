# Singulink Collections Version History

## V2.0
### All packages
- Removed support for end-of-life .NET and .NET Framework runtimes with a build warning if used on an unsupported runtime
- Minor memory usage/GC pressure/performance optimizations

### Singulink.Collections.Weak
- Minor bug fix to validation of `AutoCleanAddCount` property setters in `Collections.Weak` (thanks [@procudin](https://github.com/procudin)!)

### Singulink.Collections
- Breaking change: `AsTransient()` methods on value lists and value sets was renamed to `AsTransientReadOnly()`
