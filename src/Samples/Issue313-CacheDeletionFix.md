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

### For Exception-Safe Key Enumeration

When you need to enumerate keys in reactive chains that should continue working even when exceptions occur:

```csharp
// ✅ Exception-safe reactive pattern
await Cache.GetAllKeysSafe<MyDataType>()
    .Do(key => Console.WriteLine($"Processing key: {key}"))
    .Where(key => ShouldDelete(key))
    .Select(key => Cache.InvalidateObject<MyDataType>(key))
    .Merge()
    .Do(_ => Console.WriteLine("Deletion completed"))
    .Catch<Unit, Exception>(ex => 
    {
        Console.WriteLine($"Handled exception in reactive chain: {ex.Message}");
        return Observable.Return(Unit.Default);
    });
```

## Alternative Solutions

### Resilient Reactive Patterns

```csharp
// Exception-safe enumeration that continues on errors
await Cache.GetAllKeysSafe<MyDataType>()
    .Where(key => ShouldDelete(key))
    .Do(key => Console.WriteLine($"Deleting key: {key}"))
    .Select(key => Cache.InvalidateObject<MyDataType>(key))
    .Merge()
    .LastOrDefaultAsync();
```

### Comparing Exception Handling Approaches

```csharp
// Traditional GetAllKeys() - exceptions break the chain
Cache.GetAllKeys()
    .Where(key => key == CacheKey)
    .Subscribe(
        key => Console.WriteLine($"Found: {key}"),
        ex => Console.WriteLine($"Chain broken by: {ex.Message}") // Chain stops here
    );

// GetAllKeysSafe() - exceptions are handled, chain continues
Cache.GetAllKeysSafe()
    .Where(key => key == CacheKey)
    .Subscribe(
        key => Console.WriteLine($"Found: {key}"),
        ex => Console.WriteLine($"This won't be called - exceptions handled internally")
    );
```

### Advanced Reactive Patterns

```csharp
// Build resilient cleanup operations
var cleanupOperation = Cache.GetAllKeysSafe<MyDataType>()
    .Where(key => IsExpired(key))
    .Buffer(TimeSpan.FromSeconds(1)) // Batch operations
    .Where(batch => batch.Any())
    .Do(batch => Console.WriteLine($"Processing batch of {batch.Count} keys"))
    .SelectMany(batch => Cache.InvalidateObjects<MyDataType>(batch))
    .Retry(3) // Retry failed batches
    .Subscribe(
        _ => Console.WriteLine("Cleanup batch completed"),
        ex => Console.WriteLine($"Cleanup failed after retries: {ex.Message}")
    );
```

## Why This Works

1. **Exception Resilience**: `GetAllKeysSafe()` catches exceptions and continues the observable chain instead of breaking it
2. **No null checking needed**: `Invalidate()` methods handle non-existent keys gracefully  
3. **Reactive-friendly**: Designed for building robust reactive pipelines that don't fail on storage exceptions
4. **Simple deletion**: Direct deletion is faster and simpler than checking + deleting
5. **Observable chain continuity**: Errors are logged but don't stop the reactive sequence
6. **Flexible error handling**: Choose between immediate failure (GetAllKeys) or continuation (GetAllKeysSafe)

## GetAllKeysSafe Method Variants

| Method | Purpose | Exception Behavior |
|--------|---------|------------------|
| `Cache.GetAllKeys()` | Standard key enumeration | Throws exceptions, breaks observable chain |
| `Cache.GetAllKeysSafe()` | Exception-safe enumeration of all keys | Catches exceptions, continues with empty sequence |
| `Cache.GetAllKeysSafe<T>()` | Exception-safe enumeration of typed keys | Catches exceptions, continues with empty sequence |
| `Cache.GetAllKeysSafe(Type)` | Exception-safe enumeration for specific type | Catches exceptions, continues with empty sequence |

## When to Use Each Approach

### Use `GetAllKeys()` when:
- You want immediate failure and exception propagation
- Building non-reactive, imperative code patterns
- Exceptions should stop execution entirely

### Use `GetAllKeysSafe()` when:
- Building reactive pipelines that should be resilient to storage issues
- You want exceptions handled within the observable chain
- Building background services or cleanup operations that should continue running
- Working with unreliable storage or during development/testing

## Migration Steps

1. **For simple deletion**: Replace complex `GetAllKeys().Subscribe()` patterns with direct `Invalidate()` calls
2. **For reactive patterns**: Replace `GetAllKeys()` with `GetAllKeysSafe()` in observable chains where you want exception resilience
3. **Use type-safe methods**: Prefer `InvalidateObject<T>()` and `GetAllKeysSafe<T>()` for better performance  
4. **Remove unnecessary checks**: Remove key existence checks before deletion - `Invalidate()` handles missing keys
5. **Test exception scenarios**: Verify that your reactive chains continue working when storage exceptions occur

This approach provides both robust exception handling for reactive applications and maintains the simplicity of direct deletion using existing Akavache methods.