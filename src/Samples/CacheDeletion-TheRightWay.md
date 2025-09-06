# Cache Deletion: The Right Way

This guide shows you how to safely delete cache entries in Akavache, addressing common issues that can cause crashes on mobile platforms.

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

## The Simple Solution

Just delete the key directly! Akavache's `Invalidate()` methods are designed to be safe:

```csharp
// ✅ CORRECT - Simple and safe
await cache.Invalidate(cacheKey);

// Or for typed objects (recommended):
await cache.InvalidateObject<MyDataType>(cacheKey);

// New in V11.1 - even simpler:
await cache.Remove(cacheKey);
await cache.Remove<MyDataType>(cacheKey);
```

**Why this works:**
- `Invalidate()` does nothing if the key doesn't exist (no exception thrown)
- Simple, one-line operation
- Works reliably across all platforms
- No need to check if key exists first

## Complete Examples

### Basic Cache Deletion

```csharp
public async Task DeleteUserProfile(string userId)
{
    try
    {
        // Simple deletion - works even if key doesn't exist
        await CacheDatabase.UserAccount.Remove<UserProfile>(userId);
        Console.WriteLine($"Successfully removed user profile: {userId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error removing user profile: {ex.Message}");
    }
}
```

### Bulk Deletion

```csharp
public async Task DeleteMultipleProfiles(IEnumerable<string> userIds)
{
    try
    {
        // Delete multiple keys at once - more efficient
        await CacheDatabase.UserAccount.Remove<UserProfile>(userIds);
        Console.WriteLine($"Successfully removed {userIds.Count()} user profiles");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error removing user profiles: {ex.Message}");
    }
}
```

### Conditional Deletion (Advanced)

If you really need to check if a key exists before deleting it:

```csharp
public async Task DeleteIfExists(string cacheKey)
{
    try
    {
        // Safe way to check and delete
        bool wasDeleted = await CacheDatabase.UserAccount.TryRemove<UserProfile>(cacheKey);
        
        if (wasDeleted)
        {
            Console.WriteLine($"Key {cacheKey} was found and deleted");
        }
        else
        {
            Console.WriteLine($"Key {cacheKey} was not found");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during conditional deletion: {ex.Message}");
    }
}
```

### Safe Key Enumeration (Advanced)

If you need to list keys for some reason, use the safe version:

```csharp
public async Task CleanupOldUserProfiles()
{
    try
    {
        var keysToDelete = new List<string>();
        
        // Safe enumeration that won't crash on mobile
        await CacheDatabase.UserAccount.GetAllKeysSafe<UserProfile>()
            .Where(key => key.StartsWith("old_"))
            .ForEach(key => keysToDelete.Add(key));
        
        if (keysToDelete.Any())
        {
            await CacheDatabase.UserAccount.Remove<UserProfile>(keysToDelete);
            Console.WriteLine($"Cleaned up {keysToDelete.Count} old profiles");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during cleanup: {ex.Message}");
    }
}
```

## Method Reference

### Simple Deletion (Recommended)

| Method | Use Case |
|--------|----------|
| `cache.Remove(key)` | Delete any cache entry by key |
| `cache.Remove<T>(key)` | Delete typed cache entry (faster) |
| `cache.Remove(keys)` | Delete multiple entries |
| `cache.Remove<T>(keys)` | Delete multiple typed entries |

### Traditional Methods (Still Supported)

| Method | Use Case |
|--------|----------|
| `cache.Invalidate(key)` | Delete any cache entry |
| `cache.InvalidateObject<T>(key)` | Delete typed cache entry |
| `cache.InvalidateObjects<T>(keys)` | Delete multiple typed entries |
| `cache.InvalidateAll()` | Delete all entries |
| `cache.InvalidateAllObjects<T>()` | Delete all entries of type T |

### Safe Utilities (For Advanced Scenarios)

| Method | Use Case |
|--------|----------|
| `cache.TryRemove(key)` | Delete and check if key existed |
| `cache.TryRemove<T>(key)` | Delete typed entry and check if existed |
| `cache.GetAllKeysSafe()` | List keys without crashes |
| `cache.GetAllKeysSafe<T>()` | List typed keys safely |

## Platform Considerations

### iOS Specifics
- Never use `GetAllKeys().Subscribe()` - can cause mono trampoline crashes
- Use `Remove()` or `TryRemove()` methods instead
- Consider using `InvalidateAllObjects<T>()` for type-based cleanup

### Android Specifics  
- `GetAllKeys()` can throw `ArgumentNullException` on some Android versions
- Always wrap cache operations in try-catch blocks
- Use `GetAllKeysSafe()` if you must enumerate keys

### General Best Practices
- Always use typed deletion when possible (`Remove<T>()`)
- Use bulk operations for multiple deletions
- Don't check for key existence before deletion - just delete
- Handle exceptions gracefully with try-catch blocks

## Migration Guide

If you're currently using the problematic pattern:

```csharp
// Old problematic code:
Cache?.GetAllKeys()?.Subscribe(async keys =>
{
    if (keys != null && keys.Any(k => k == CacheKey))
        await Cache.Invalidate(CacheKey);
});

// Replace with:
await Cache.Remove(CacheKey);
// or
await Cache.Remove<MyType>(CacheKey);
```

This change will:
- ✅ Eliminate crashes on mobile platforms
- ✅ Reduce code complexity
- ✅ Improve performance
- ✅ Make your code more maintainable