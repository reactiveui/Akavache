# GetAllKeysSafe: Exception-Safe Key Enumeration

This document demonstrates the new `GetAllKeysSafe` methods that provide exception-safe alternatives to `GetAllKeys()` for building resilient reactive applications.

## The Problem: Exception Handling in Observable Chains

The original issue occurred when using `GetAllKeys()` in reactive chains where exceptions would break the entire observable sequence:

```csharp
// ❌ This pattern breaks the observable chain on exceptions
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

**Problems with this approach:**
- Exceptions from `GetAllKeys()` break the observable chain
- Complex error handling with nested try-catch and error callbacks
- Unnecessary key existence checking before deletion
- Mixing async/await with reactive Subscribe patterns

## The Solution: Exception-Safe Reactive Patterns

### For Direct Deletion (Recommended)

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

### For Key Enumeration (Advanced Scenarios)

When you actually need to enumerate keys, use the `GetAllKeysSafe` methods:

```csharp
// ✅ Exception-safe key enumeration
Cache.GetAllKeysSafe()
    .Subscribe(
        keys =>
        {
            // Process keys safely - this will always be called with valid data
            var keysToDelete = keys.Where(key => key.StartsWith("temp_")).ToList();
            
            // Delete found keys
            foreach (var key in keysToDelete)
            {
                try
                {
                    Cache.Invalidate(key).Wait(); // Or use proper async handling
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete {key}: {ex.Message}");
                }
            }
        },
        error =>
        {
            // Graceful error handling that doesn't crash the app
            Console.WriteLine($"Error enumerating keys: {error.Message}");
        }
    );
```

## GetAllKeysSafe Method Reference

### Available Overloads

```csharp
// Get all keys safely
IObservable<IEnumerable<string>> GetAllKeysSafe()

// Get keys for a specific type safely
IObservable<IEnumerable<string>> GetAllKeysSafe<T>()

// Get keys for a specific type (non-generic)
IObservable<IEnumerable<string>> GetAllKeysSafe(Type type)
```

### Behavior Differences

| Method | On Success | On Exception | Observable Completion |
|--------|------------|--------------|----------------------|
| `GetAllKeys()` | Returns keys | Throws, breaks chain | May not complete |
| `GetAllKeysSafe()` | Returns keys | Catches, continues chain | Always completes |

## Practical Examples

### Example 1: Safe Cache Cleanup

```csharp
public IObservable<int> CleanupTempCacheItems()
{
    return Cache.GetAllKeysSafe()
        .SelectMany(keys => 
        {
            var tempKeys = keys.Where(k => k.StartsWith("temp_")).ToList();
            return Observable.FromAsync(async () =>
            {
                await Cache.Invalidate(tempKeys);
                return tempKeys.Count;
            });
        })
        .Catch<int, Exception>(ex =>
        {
            Console.WriteLine($"Cleanup failed: {ex.Message}");
            return Observable.Return(0); // Return 0 deleted items on error
        });
}
```

### Example 2: Reactive Cache Statistics

```csharp
public IObservable<CacheStats> GetCacheStatistics()
{
    return Cache.GetAllKeysSafe()
        .Select(keys => new CacheStats
        {
            TotalKeys = keys.Count(),
            UserDataKeys = keys.Count(k => k.StartsWith("user_")),
            TempKeys = keys.Count(k => k.StartsWith("temp_")),
            SystemKeys = keys.Count(k => k.StartsWith("system_"))
        })
        .Catch<CacheStats, Exception>(ex =>
        {
            Console.WriteLine($"Failed to get cache stats: {ex.Message}");
            return Observable.Return(CacheStats.Empty);
        });
}

public class CacheStats
{
    public int TotalKeys { get; set; }
    public int UserDataKeys { get; set; }
    public int TempKeys { get; set; }
    public int SystemKeys { get; set; }
    
    public static CacheStats Empty => new CacheStats();
}
```

### Example 3: Typed Key Management

```csharp
public async Task<List<string>> GetExpiredUserProfiles()
{
    try
    {
        var allUserKeys = await Cache.GetAllKeysSafe<UserProfile>().FirstOrDefaultAsync();
        var expiredKeys = new List<string>();
        
        foreach (var key in allUserKeys ?? Enumerable.Empty<string>())
        {
            try
            {
                // Check if the cached item is expired
                await Cache.GetObject<UserProfile>(key);
            }
            catch (KeyNotFoundException)
            {
                // Key exists in metadata but data is expired/corrupted
                expiredKeys.Add(key);
            }
        }
        
        return expiredKeys;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking for expired profiles: {ex.Message}");
        return new List<string>();
    }
}
```

## Migration from GetAllKeys()

### Before (Problematic Pattern)
```csharp
// ❌ Exception-prone and complex
Cache.GetAllKeys().Subscribe(
    keys => 
    {
        foreach (var key in keys ?? Enumerable.Empty<string>())
        {
            if (key.StartsWith("old_"))
            {
                Cache.Invalidate(key).Subscribe();
            }
        }
    },
    error => Console.WriteLine($"ERROR: {error.Message}")
);
```

### After (Recommended Pattern)
```csharp
// ✅ Exception-safe and simpler
Cache.GetAllKeysSafe()
    .SelectMany(keys => keys.Where(k => k.StartsWith("old_")))
    .SelectMany(key => Cache.Invalidate(key).Catch(Observable.Empty<Unit>()))
    .Subscribe(
        _ => { /* Deletion succeeded */ },
        error => Console.WriteLine($"Cleanup error: {error.Message}"),
        () => Console.WriteLine("Cleanup completed")
    );
```

## Exception Handling Best Practices

### Pattern 1: Graceful Degradation
```csharp
// When the operation is nice-to-have but not critical
Cache.GetAllKeysSafe()
    .Catch<IEnumerable<string>, Exception>(ex => 
    {
        Logger.LogWarning("Could not enumerate cache keys: {Error}", ex.Message);
        return Observable.Return(Enumerable.Empty<string>());
    })
    .Subscribe(keys => ProcessKeys(keys));
```

### Pattern 2: Retry with Backoff
```csharp
// When you want to retry the operation
Cache.GetAllKeysSafe()
    .Retry(3)
    .Catch<IEnumerable<string>, Exception>(ex =>
    {
        Logger.LogError("Failed to get cache keys after retries: {Error}", ex.Message);
        return Observable.Return(Enumerable.Empty<string>());
    })
    .Subscribe(keys => ProcessKeys(keys));
```

### Pattern 3: Circuit Breaker
```csharp
private bool _cacheEnumerationFailed = false;

public IObservable<IEnumerable<string>> GetKeysWithCircuitBreaker()
{
    if (_cacheEnumerationFailed)
    {
        return Observable.Return(Enumerable.Empty<string>());
    }
    
    return Cache.GetAllKeysSafe()
        .Catch<IEnumerable<string>, Exception>(ex =>
        {
            _cacheEnumerationFailed = true;
            Logger.LogError("Cache enumeration failed, disabling: {Error}", ex.Message);
            
            // Re-enable after delay
            Observable.Timer(TimeSpan.FromMinutes(5))
                .Subscribe(_ => _cacheEnumerationFailed = false);
                
            return Observable.Return(Enumerable.Empty<string>());
        });
}
```

## Platform-Specific Considerations

### iOS
- Original `GetAllKeys()` could cause mono trampoline crashes
- `GetAllKeysSafe()` includes iOS-specific exception handling
- Recommended for all iOS applications using Akavache

### Android
- Handles `ArgumentNullException` that could occur in some Android versions
- Includes proper thread context handling for Android UI updates
- Works reliably across different Android API levels

### Desktop (Windows/macOS/Linux)
- Generally more reliable, but `GetAllKeysSafe()` still recommended
- Consistent API across all platforms
- Better error reporting and debugging

## Testing Your Migration

### Unit Test Example
```csharp
[Test]
public async Task GetAllKeysSafe_Should_Handle_Exceptions_Gracefully()
{
    // Arrange
    var cache = new TestBlobCache(); // Mock that throws exceptions
    
    // Act & Assert - Should not throw
    var keys = await cache.GetAllKeysSafe()
        .Catch<IEnumerable<string>, Exception>(ex => Observable.Return(Enumerable.Empty<string>()))
        .FirstOrDefaultAsync();
        
    Assert.That(keys, Is.Not.Null);
}
```

## Summary

- **Use `GetAllKeysSafe()` instead of `GetAllKeys()`** for all new code
- **Migrate existing `GetAllKeys()` usage** to the safe variant
- **Implement proper error handling** with Catch operators
- **Test thoroughly on mobile platforms** where exceptions are more common
- **Consider whether you actually need key enumeration** - often direct operations are simpler

For more cache management patterns, see:
- [Cache deletion patterns](../patterns/cache-deletion.md)
- [Basic operations guide](../basic-operations.md)
- [Troubleshooting guide](./troubleshooting-guide.md)