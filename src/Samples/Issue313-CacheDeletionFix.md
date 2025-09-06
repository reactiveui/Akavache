# Issue #313 Fix: Safe Cache Deletion Examples

This file demonstrates the fix for the cache deletion crashes reported in issue #313.

## The Original Problem Code

```csharp
// ❌ This code caused crashes on iOS and Android
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

**Problems:**
- `GetAllKeys()` could return null on mobile platforms
- Mixing `async/await` with `Subscribe()` created complex scenarios
- Unnecessary complexity - checking if key exists before deletion

## The Fixed Code

```csharp
// ✅ Simple, safe, and works on all platforms
try
{
    await Cache.Invalidate(CacheKey);
    Console.WriteLine($"Successfully removed {CacheKey}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error removing {CacheKey}: {ex.Message}");
}
```

Or for typed objects (recommended for better performance):

```csharp
// ✅ Even better - typed deletion
try
{
    await Cache.InvalidateObject<MyDataType>(CacheKey);
    Console.WriteLine($"Successfully removed typed object {CacheKey}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error removing {CacheKey}: {ex.Message}");
}
```

## Alternative Solutions

### If you need to enumerate keys safely:

```csharp
try
{
    var keysToDelete = new List<string>();
    
    // Safe enumeration that won't crash
    await Cache.GetAllKeysSafe<MyDataType>()
        .Where(key => ShouldDelete(key))
        .ForEach(key => keysToDelete.Add(key));
    
    if (keysToDelete.Any())
    {
        await Cache.InvalidateObjects<MyDataType>(keysToDelete);
        Console.WriteLine($"Removed {keysToDelete.Count} keys");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during cleanup: {ex.Message}");
}
```

### If you need to safely check all keys without crashing:

```csharp
try
{
    // Safe alternative to GetAllKeys()
    await Cache.GetAllKeysSafe()
        .Where(key => key == CacheKey)
        .ForEach(async key => 
        {
            await Cache.Invalidate(key);
            Console.WriteLine($"Found and removed {key}");
        });
}
catch (Exception ex)
{
    Console.WriteLine($"Error during safe key enumeration: {ex.Message}");
}
```

## Why This Works

1. **No null checking needed**: `Invalidate()` methods handle non-existent keys gracefully
2. **Platform safe**: No reliance on `GetAllKeys()` which can fail on mobile
3. **Simple**: One-line deletion instead of complex subscription patterns
4. **Efficient**: Direct deletion is faster than checking + deleting
5. **Reliable**: Works consistently across all platforms and Akavache versions
6. **Safe enumeration**: `GetAllKeysSafe()` provides null-safe key access when needed

## Available Methods for Mobile-Safe Operations

| Method | Purpose |
|--------|---------|
| `Cache.Invalidate(key)` | Delete any cache entry |
| `Cache.InvalidateObject<T>(key)` | Delete typed cache entry (recommended) |
| `Cache.InvalidateObjects<T>(keys)` | Delete multiple typed entries |
| `Cache.GetAllKeysSafe()` | List all keys safely without crashes |
| `Cache.GetAllKeysSafe<T>()` | List typed keys safely |

## Migration Steps

1. Replace `GetAllKeys().Subscribe()` patterns with direct `Invalidate()` calls
2. Use `InvalidateObject<T>()` for typed objects when possible  
3. Use `GetAllKeysSafe()` instead of `GetAllKeys()` if enumeration is needed
4. Remove unnecessary key existence checks before deletion
5. Test on mobile platforms to ensure crashes are resolved

This fix addresses the core issue while providing better, more maintainable code patterns using existing Akavache methods plus the new safe key enumeration functionality.