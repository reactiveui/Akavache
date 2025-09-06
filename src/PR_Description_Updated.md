# Fix InMemory cache Invalidate not clearing RequestCache causing stale data in GetOrFetchObject

## Summary

This PR fixes a critical bug in Akavache's InMemory cache where calling `Invalidate()` didn't properly clear the RequestCache, causing subsequent `GetOrFetchObject` calls to return stale data instead of fetching fresh data. The issue was specific to InMemory cache implementations and affected applications relying on cache invalidation to force fresh data retrieval.

## Root Cause Analysis

### The Problem
- When `Invalidate()` was called on InMemory cache, it removed entries from the main `_cache` but left the `RequestCache` intact
- `GetOrFetchObject` uses `RequestCache.GetOrCreateRequest` for request deduplication
- After invalidation, cache lookups would miss (correctly), but `GetOrFetchObject` would return cached request results instead of executing the fetch function
- This led to stale data being returned even after explicit cache invalidation

### The Impact
```csharp
// ❌ BROKEN: This pattern failed in affected versions
var data1 = await cache.GetOrFetchObject("key", () => FetchFromApi()); // Returns "fresh_data_1"
await cache.Invalidate("key");
var data2 = await cache.GetOrFetchObject("key", () => FetchFromApi()); // Should return "fresh_data_2" but returned "fresh_data_1"
```

## Solution Implementation

### Core Changes

1. **Enhanced RequestCache** (`src/Akavache.Core/Core/RequestCache.cs`)
   - Added `RemoveRequestsForKey(string key)` method to clear all request cache entries for a specific cache key regardless of type
   - Uses efficient key suffix matching to find and remove all related request cache entries

2. **Updated InMemoryBlobCacheBase** (`src/Akavache.Core/InMemoryBlobCacheBase.cs`)
   - Added `RequestCache.RemoveRequestsForKey(key)` calls to all `Invalidate` methods:
     - `Invalidate(string key)`
     - `Invalidate(IEnumerable<string> keys)`
     - `InvalidateAllObjects<T>()`
     - `InvalidateAll()`

3. **Comprehensive Test Coverage** (`src/Akavache.Tests/SerializerExtensionsTests.cs`)
   - `InvalidateShouldClearRequestCacheForGetOrFetchObject()` - General test for the fix
   - `BugReport524_InvalidateNotWorkingProperlyForInMemory()` - Exact reproduction of the original bug report scenario

### Key Implementation Details

```csharp
// New method in RequestCache to clear entries by cache key
public static void RemoveRequestsForKey(string key)
{
    var keySuffix = $":{key}";
    var keysToRemove = new List<string>();
    
    foreach (var requestKey in _inflightRequests.Keys)
    {
        if (requestKey.EndsWith(keySuffix, StringComparison.Ordinal))
        {
            keysToRemove.Add(requestKey);
        }
    }
    
    foreach (var requestKey in keysToRemove)
    {
        RemoveRequestInternal(requestKey);
    }
}

// Updated Invalidate implementation
public virtual IObservable<Unit> Invalidate(string key)
{
    return Observable.Create<Unit>(observer =>
    {
        // ... existing cache removal logic ...
        
        // 🔧 NEW: Clear request cache to prevent stale data returns
        RequestCache.RemoveRequestsForKey(key);
        
        return Unit.Default;
    }, Scheduler);
}
```

## Testing and Validation

### Test Coverage
- ✅ Unit tests verify the fix works correctly
- ✅ Tests ensure no regression in existing functionality  
- ✅ Edge cases covered (multiple keys, type-based invalidation, bulk operations)
- ✅ All existing tests pass

### Manual Verification
The fix can be verified with this simple pattern:
```csharp
var cache = new InMemoryBlobCache(new SystemJsonSerializer());
var callCount = 0;

Func<IObservable<string>> fetchFunc = () => 
{
    callCount++;
    return Observable.Return($"data_{callCount}");
};

// Should fetch initially
var result1 = await cache.GetOrFetchObject("test", fetchFunc); // result1 = "data_1"

// Invalidate cache
await cache.Invalidate("test");

// Should fetch fresh data (not return stale request cache)
var result2 = await cache.GetOrFetchObject("test", fetchFunc); // result2 = "data_2" ✅

// Verify both calls executed
Assert.AreEqual(2, callCount); // ✅ Success
Assert.AreNotEqual(result1, result2); // ✅ Different data returned
```

## Documentation Updates

### New Documentation
- **`src/Samples/CacheInvalidationPatterns.cs`** - Comprehensive examples demonstrating:
  - Basic invalidation patterns that now work correctly
  - Bulk invalidation for related data
  - Type-based invalidation techniques
  - Cross-cache type validation
  - Production best practices
  - Demonstration of the exact bug that was fixed

### Updated Documentation
- **`README.md`** - Added troubleshooting section for this specific issue with migration guidance
- **`src/Samples/README.md`** - Added section explaining the bug fix and its importance
- **Inline code comments** - Enhanced documentation in affected methods

## Backwards Compatibility

- ✅ **Fully backwards compatible** - no breaking changes to public APIs
- ✅ **No migration required** - fix automatically applies upon upgrade
- ✅ **Existing code works better** - previously broken invalidation patterns now work correctly
- ✅ **Performance impact: Minimal** - only affects invalidation code paths

## Related Issues

- Fixes #524 - "Invalidate not working properly for InMemory"
- Addresses common community questions about cache invalidation behavior
- Resolves discrepancies between expected and actual invalidation behavior

## Verification Checklist

- [x] Bug reproduction test passes
- [x] Fix implementation is minimal and surgical
- [x] All existing tests continue to pass
- [x] New tests prevent regression
- [x] Documentation clearly explains the issue and fix
- [x] No breaking changes introduced
- [x] Cross-platform compatibility maintained

## Impact Assessment

### Before Fix
- `GetOrFetchObject` after `Invalidate` could return stale data
- Cache invalidation appeared to work but didn't force fresh data retrieval
- Developers had to resort to workarounds like manual cache deletion
- Inconsistent behavior between different cache operations

### After Fix  
- `Invalidate` properly clears both main cache and request cache
- `GetOrFetchObject` correctly fetches fresh data after invalidation
- Consistent and predictable cache invalidation behavior
- Eliminates need for workarounds and manual cache management

This fix ensures that Akavache's cache invalidation works as expected and documented, providing reliable cache management for applications that depend on being able to force fresh data retrieval through invalidation.