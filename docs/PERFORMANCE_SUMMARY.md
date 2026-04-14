# Akavache V10 vs V11 Performance Summary

## Quick Performance Comparison

### V11 Wins
- **Bulk Operations**: 10x+ faster than individual operations
- **GetOrFetch Pattern**: Sub-linear scaling (1.5ms to 45ms for 100x data)
- **Memory Consistency**: More predictable allocation patterns
- **In-Memory Performance**: 122ms for 1000 complex operations
- **Architecture**: Modern builder pattern with better error handling

### Equivalent Performance
- **Cache Type Operations**: All persistent caches within 2% of each other
- **Read Operations**: Generally comparable with V10
- **Object Serialization**: SystemTextJson matches or exceeds V10 performance
- **Memory Usage**: Similar allocation patterns across versions

### V11 Trade-offs
- **Large Sequential Reads**: Up to 8.6% slower in some cases
- **Initialization Overhead**: Builder pattern adds slight complexity
- **Package Dependencies**: More granular package structure

## Key Numbers

| Operation | Small (10) | Medium (100) | Large (1000) |
|-----------|------------|--------------|--------------|
| **GetOrFetch** | 1.5ms | 15ms | 45ms |
| **Bulk Operations** | 3.3ms | 4.5ms | 18ms |
| **In-Memory** | 2.4ms | 19ms | 123ms |
| **Cache Types** | ~27ms | ~255ms | ~2,600ms |

## Migration Decision Matrix

| Factor | V10 to V11 Migration Recommended |
|--------|--------------------------------|
| **New Projects** | **Always** |
| **Performance Critical** | **Yes** (with SystemTextJson) |
| **Legacy Data Compatibility** | **Yes** (with Newtonsoft BSON) |
| **Large Sequential Reads** | **Evaluate** (8.6% slower) |
| **Developer Experience** | **Highly Recommended** |

## Bottom Line

**Akavache V11 delivers architectural improvements with comparable performance.** The new features (multiple serializers, cross-compatibility, modern patterns) provide significant value with minimal performance impact.

**Recommendation**: Upgrade to V11 for all new projects and consider migration for existing projects that would benefit from the architectural improvements.
