# Cache Deletion and Safe Key Access: The Right Way

This guide shows you how to safely delete cache entries and access keys in Akavache, addressing common issues that can cause crashes on mobile platforms.

## The Problem

Many developers try to delete cache entries using this pattern:

```csharp
// ❌ WRONG - This can cause crashes on mobile platforms
try
{
    Cache?.GetAllKeys()?.Subscribe(async keys =>
    {
        if (keys != null && keys.Any(k => k == CacheKey))
            await Cache.Invalidate(CacheKey);
    }, ex =>
    {
        Console.WriteLine("SUBSCRIBE ERROR:" + ex.Message);
    });
}
catch (Exception ex)
{
    Console.WriteLine($"DELETE KEY {CacheKey} TO CACHE ERROR: {ex.Message}");
}
```

**Why this fails:**
- `GetAllKeys()` can return null on some platforms (iOS/Android)
- Mixing `async/await` with `Subscribe()` creates complex async scenarios
- Unnecessarily complex - you don't need to check if a key exists before deleting it

## The Simple Solution for Deletion

Just delete the key directly! Akavache's `Invalidate()` methods are designed to be safe:

```csharp
// ✅ CORRECT - Simple and safe
await cache.Invalidate(cacheKey);

// Or for typed objects (recommended for better performance):
await cache.InvalidateObject<MyDataType>(cacheKey);

// Bulk deletion:
await cache.Invalidate(new[] { "key1", "key2", "key3" });
await cache.InvalidateObjects<MyDataType>(new[] { "key1", "key2", "key3" });
```

**Why this works:**
- `Invalidate()` does nothing if the key doesn't exist (no exception thrown)
- Works reliably on all platforms (iOS, Android, Windows, etc.)
- Much simpler code - no need for complex error handling
- Better performance - no need to enumerate all keys first

## Safe Key Enumeration (When You Actually Need It)

If you genuinely need to enumerate keys (rare), use the safe patterns:

### Option 1: Exception-Safe GetAllKeys
```csharp
// ✅ Handle GetAllKeys safely with proper error handling
try
{
    cache.GetAllKeys()
        .Subscribe(
            keys =>
            {
                foreach (var key in keys ?? Enumerable.Empty<string>())
                {
                    Console.WriteLine($"Key: {key}");
                }
            },
            error =>
            {
                Console.WriteLine($"Error getting keys: {error.Message}");
                // Handle the error appropriately for your app
            }
        );
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to start key enumeration: {ex.Message}");
}
```

### Option 2: Use GetAllKeysSafe (V11.1+)
```csharp
// ✅ V11.1+ includes GetAllKeysSafe for better error handling
cache.GetAllKeysSafe()
    .Subscribe(
        keys =>
        {
            // keys is never null here
            foreach (var key in keys)
            {
                Console.WriteLine($"Key: {key}");
            }
        },
        error =>
        {
            // Graceful error handling without breaking the observable chain
            Console.WriteLine($"Error: {error.Message}");
        }
    );
```

## Common Scenarios and Solutions

### Scenario 1: Clear All Cache Entries
```csharp
// ✅ CORRECT - Use the built-in method
await cache.InvalidateAll();

// Alternative for specific patterns:
await cache.Vacuum(); // Removes expired entries only
```

### Scenario 2: Clear Entries by Pattern
```csharp
// ✅ CORRECT - Get keys safely, then delete
try
{
    var keys = await cache.GetAllKeys().FirstOrDefaultAsync() ?? new string[0];
    var keysToDelete = keys.Where(key => key.StartsWith("user_data_")).ToArray();
    
    if (keysToDelete.Any())
    {
        await cache.Invalidate(keysToDelete);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error clearing user data: {ex.Message}");
}
```

### Scenario 3: Conditional Deletion
```csharp
// ✅ CORRECT - Just try to delete, handle errors if needed
try
{
    await cache.Invalidate(cacheKey);
    Console.WriteLine($"Successfully cleared {cacheKey}");
}
catch (Exception ex)
{
    Console.WriteLine($"Could not clear {cacheKey}: {ex.Message}");
    // App continues normally - deletion failure is often not critical
}
```

## Platform-Specific Considerations

### iOS/Android Mobile Apps
- File system access can be restricted or fail
- Background task limitations can interrupt operations
- Always use try/catch around cache operations
- Consider user experience - don't block UI for cache operations

### Desktop Applications
- Generally more reliable file system access
- Still good practice to handle exceptions gracefully
- Can use more aggressive caching strategies

## Best Practices Summary

1. **✅ DO**: Use `await cache.Invalidate(key)` for simple deletion
2. **✅ DO**: Use `await cache.InvalidateAll()` to clear everything
3. **✅ DO**: Handle exceptions appropriately for your application
4. **✅ DO**: Use bulk operations when deleting multiple keys
5. **❌ DON'T**: Check if keys exist before deleting them
6. **❌ DON'T**: Mix Subscribe() with await in complex ways
7. **❌ DON'T**: Assume GetAllKeys() will never fail

## Error Handling Patterns

### For Non-Critical Operations
```csharp
// When cache deletion failure won't break your app
try
{
    await cache.Invalidate(key);
}
catch
{
    // Log the error but continue - cache deletion failure is often not critical
}
```

### For Critical Operations
```csharp
// When you need to know if the operation succeeded
try
{
    await cache.Invalidate(key);
    return true;
}
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to invalidate cache key {Key}", key);
    return false;
}
```

## Related Issues and References

- For more about cache invalidation patterns, see [`CacheInvalidationPatterns.cs`](../../src/Samples/CacheInvalidationPatterns.cs)
- For GetAllKeysSafe usage, see the [troubleshooting guide](../troubleshooting/issue-313-cache-deletion-fix.md)
- For general cache patterns, see [basic operations](../basic-operations.md)

The key takeaway: **Akavache is designed to be safe by default. Trust the built-in methods and keep your code simple.**