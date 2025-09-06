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
    await Cache.Remove(CacheKey);
    Console.WriteLine($"Successfully removed {CacheKey}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error removing {CacheKey}: {ex.Message}");
}
```

Or for typed objects (recommended):

```csharp
// ✅ Even better - typed deletion
try
{
    await Cache.Remove<MyDataType>(CacheKey);
    Console.WriteLine($"Successfully removed typed object {CacheKey}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error removing {CacheKey}: {ex.Message}");
}
```

## Alternative Solutions

### If you need to check if the key existed:

```csharp
try
{
    bool wasRemoved = await Cache.TryRemove<MyDataType>(CacheKey);
    if (wasRemoved)
    {
        Console.WriteLine($"Key {CacheKey} was found and removed");
    }
    else
    {
        Console.WriteLine($"Key {CacheKey} was not found in cache");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during removal: {ex.Message}");
}
```

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
        await Cache.Remove<MyDataType>(keysToDelete);
        Console.WriteLine($"Removed {keysToDelete.Count} keys");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during cleanup: {ex.Message}");
}
```

## Why This Works

1. **No null checking needed**: `Remove()` methods handle non-existent keys gracefully
2. **Platform safe**: No reliance on `GetAllKeys()` which can fail on mobile
3. **Simple**: One-line deletion instead of complex subscription patterns
4. **Efficient**: Direct deletion is faster than checking + deleting
5. **Reliable**: Works consistently across all platforms and Akavache versions

## Migration Steps

1. Replace `GetAllKeys().Subscribe()` patterns with direct `Remove()` calls
2. Use `Remove<T>()` for typed objects when possible
3. Use `TryRemove()` only when you need to know if the key existed
4. Use `GetAllKeysSafe()` instead of `GetAllKeys()` if enumeration is needed
5. Test on mobile platforms to ensure crashes are resolved

This fix addresses the core issue while providing better, more maintainable code patterns.