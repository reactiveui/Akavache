# Basic Operations Guide

This guide covers the fundamental operations you'll use with Akavache on a daily basis.

## Storing Data

### InsertObject - Store data with expiration

```csharp
// Store data with automatic expiration
var user = new User { Id = 1, Name = "John Doe" };
await CacheDatabase.UserAccount.InsertObject("user_1", user, TimeSpan.FromHours(1));

// Store data without expiration (persists until manually removed)
await CacheDatabase.UserAccount.InsertObject("user_1", user);

// Store with specific expiration time
await CacheDatabase.UserAccount.InsertObject("user_1", user, DateTimeOffset.Now.AddDays(7));
```

### Batch Operations

```csharp
// Store multiple objects efficiently
var users = new Dictionary<string, User>
{
    ["user_1"] = new User { Id = 1, Name = "John" },
    ["user_2"] = new User { Id = 2, Name = "Jane" }
};

await CacheDatabase.UserAccount.InsertObjects(users, TimeSpan.FromHours(2));
```

## Retrieving Data

### GetObject - Retrieve stored data

```csharp
try
{
    var user = await CacheDatabase.UserAccount.GetObject<User>("user_1");
    Console.WriteLine($"Found user: {user.Name}");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("User not found in cache");
}
```

### GetOrFetchObject - Cache-aside pattern

```csharp
// Get from cache or fetch from source if not found
var user = await CacheDatabase.UserAccount.GetOrFetchObject("user_1",
    async () => {
        // This function is called only if the key is not in cache
        var response = await httpClient.GetAsync($"https://api.example.com/users/1");
        return await response.Content.ReadFromJsonAsync<User>();
    },
    TimeSpan.FromHours(1)); // Cache for 1 hour

Console.WriteLine($"User: {user.Name}");
```

### TryGetObject - Safe retrieval without exceptions

```csharp
var result = await CacheDatabase.UserAccount.TryGetObject<User>("user_1");
if (result.HasValue)
{
    Console.WriteLine($"Found user: {result.Value.Name}");
}
else
{
    Console.WriteLine("User not found in cache");
}
```

### Batch Retrieval

```csharp
// Get multiple objects at once
var userKeys = new[] { "user_1", "user_2", "user_3" };
var users = await CacheDatabase.UserAccount.GetObjects<User>(userKeys);

foreach (var kvp in users)
{
    Console.WriteLine($"Key: {kvp.Key}, User: {kvp.Value.Name}");
}
```

## Error Handling

### Basic Error Handling

```csharp
public async Task<User> GetUserSafely(int userId)
{
    try
    {
        return await CacheDatabase.UserAccount.GetObject<User>($"user_{userId}");
    }
    catch (KeyNotFoundException)
    {
        // Key doesn't exist - fetch from API
        return await FetchUserFromApi(userId);
    }
    catch (Exception ex)
    {
        // Other errors (serialization, storage, etc.)
        Console.WriteLine($"Cache error: {ex.Message}");
        return await FetchUserFromApi(userId);
    }
}
```

### Reactive Error Handling

```csharp
// Using reactive extensions for error handling
var user = await CacheDatabase.UserAccount
    .GetObject<User>("user_1")
    .Catch<User, KeyNotFoundException>(ex => 
        Observable.FromAsync(() => FetchUserFromApi(1)))
    .FirstOrDefaultAsync();
```

### Resilient Cache Operations

```csharp
public class ResilientCacheService
{
    private readonly IBlobCache _cache = CacheDatabase.UserAccount;
    
    public async Task<T> GetWithFallback<T>(string key, Func<Task<T>> fallback, TimeSpan? expiry = null)
    {
        try
        {
            return await _cache.GetObject<T>(key);
        }
        catch (KeyNotFoundException)
        {
            var data = await fallback();
            if (expiry.HasValue)
            {
                await _cache.InsertObject(key, data, expiry.Value);
            }
            return data;
        }
    }
}
```

## Removing Data

### Individual Key Deletion

```csharp
// Remove a specific key
await CacheDatabase.UserAccount.Invalidate("user_1");

// Remove a specific typed object (recommended for better performance)
await CacheDatabase.UserAccount.InvalidateObject<User>("user_1");
```

### Bulk Deletion

```csharp
// Remove multiple keys at once
var keysToDelete = new[] { "user_1", "user_2", "user_3" };
await CacheDatabase.UserAccount.Invalidate(keysToDelete);

// Remove multiple typed objects
await CacheDatabase.UserAccount.InvalidateObjects<User>(keysToDelete);
```

### Pattern-Based Deletion

```csharp
// Find and delete keys matching a pattern
var allKeys = await CacheDatabase.UserAccount.GetAllKeys().FirstOrDefaultAsync();
var userKeys = allKeys.Where(key => key.StartsWith("user_")).ToArray();

if (userKeys.Any())
{
    await CacheDatabase.UserAccount.InvalidateObjects<User>(userKeys);
}
```

### Clear All Data

```csharp
// Clear all data from a cache
await CacheDatabase.UserAccount.InvalidateAll();

// Clear all objects of a specific type
await CacheDatabase.UserAccount.InvalidateAllObjects<User>();
```

## Updating Expiration

### Extend Cache Lifetime

```csharp
// Update expiration without rewriting data (V11.1+ feature)
await CacheDatabase.UserAccount.UpdateExpiration("user_1", TimeSpan.FromDays(7));

// Update expiration for multiple keys
var keys = new[] { "user_1", "user_2", "user_3" };
await CacheDatabase.UserAccount.UpdateExpiration(keys, TimeSpan.FromDays(7));
```

### Conditional Expiration Updates

```csharp
// Update expiration only if key exists
try
{
    await CacheDatabase.UserAccount.UpdateExpiration("user_1", TimeSpan.FromDays(7));
    Console.WriteLine("Expiration updated successfully");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("Key not found - cannot update expiration");
}
```

### Performance Benefits

The `UpdateExpiration` method is highly optimized:

- **High Performance**: Only updates metadata, leaves cached data untouched
- **SQL Efficiency**: Uses targeted UPDATE statements rather than full record replacement  
- **Bulk Operations**: Update multiple entries in a single transaction
- **No Data Transfer**: Avoids expensive serialization/deserialization cycles (up to 250x faster)

## Advanced Operations

### Conditional Storage

```csharp
// Store only if key doesn't exist
public async Task<bool> TryInsertIfNotExists<T>(string key, T value, TimeSpan expiry)
{
    try
    {
        await CacheDatabase.UserAccount.GetObject<T>(key);
        return false; // Key already exists
    }
    catch (KeyNotFoundException)
    {
        await CacheDatabase.UserAccount.InsertObject(key, value, expiry);
        return true; // Successfully inserted
    }
}
```

### Atomic Updates

```csharp
// Update cache atomically
public async Task<T> UpdateObject<T>(string key, Func<T, T> updateFunc, TimeSpan? expiry = null)
{
    var existing = await CacheDatabase.UserAccount.GetObject<T>(key);
    var updated = updateFunc(existing);
    
    if (expiry.HasValue)
    {
        await CacheDatabase.UserAccount.InsertObject(key, updated, expiry.Value);
    }
    else
    {
        await CacheDatabase.UserAccount.InsertObject(key, updated);
    }
    
    return updated;
}
```

### Cache Statistics

```csharp
// Get cache information
var allKeys = await CacheDatabase.UserAccount.GetAllKeys().FirstOrDefaultAsync();
Console.WriteLine($"Total cached items: {allKeys.Count()}");

// Get cache size (approximate)
var cacheStats = new
{
    TotalKeys = allKeys.Count(),
    UserKeys = allKeys.Count(k => k.StartsWith("user_")),
    ApiKeys = allKeys.Count(k => k.StartsWith("api_")),
    TempKeys = allKeys.Count(k => k.StartsWith("temp_"))
};
```

## Key Naming Best Practices

### Consistent Naming Conventions

```csharp
// Use consistent, hierarchical naming
await cache.InsertObject("user:profile:123", userProfile);
await cache.InsertObject("user:settings:123", userSettings);
await cache.InsertObject("api:products:page:1", products);
await cache.InsertObject("temp:calculation:hash123", result);
```

### Type-Safe Key Generation

```csharp
public static class CacheKeys
{
    public static string UserProfile(int userId) => $"user:profile:{userId}";
    public static string UserSettings(int userId) => $"user:settings:{userId}";
    public static string ApiResponse(string endpoint, int page) => $"api:{endpoint}:page:{page}";
    public static string TempData(string identifier) => $"temp:data:{identifier}";
}

// Usage
await cache.InsertObject(CacheKeys.UserProfile(123), userProfile);
var profile = await cache.GetObject<UserProfile>(CacheKeys.UserProfile(123));
```

## Common Patterns

### Cache-Aside Pattern

```csharp
public class UserService
{
    private readonly IBlobCache _cache = CacheDatabase.UserAccount;
    private readonly HttpClient _httpClient;
    
    public async Task<User> GetUser(int userId)
    {
        var cacheKey = CacheKeys.UserProfile(userId);
        
        return await _cache.GetOrFetchObject(cacheKey,
            async () => {
                var response = await _httpClient.GetAsync($"/api/users/{userId}");
                return await response.Content.ReadFromJsonAsync<User>();
            },
            TimeSpan.FromMinutes(30));
    }
}
```

### Write-Through Pattern

```csharp
public class UserService
{
    private readonly IBlobCache _cache = CacheDatabase.UserAccount;
    private readonly HttpClient _httpClient;
    
    public async Task<User> UpdateUser(int userId, User updatedUser)
    {
        // Update the backend
        var response = await _httpClient.PutAsJsonAsync($"/api/users/{userId}", updatedUser);
        var savedUser = await response.Content.ReadFromJsonAsync<User>();
        
        // Update the cache
        var cacheKey = CacheKeys.UserProfile(userId);
        await _cache.InsertObject(cacheKey, savedUser, TimeSpan.FromMinutes(30));
        
        return savedUser;
    }
}
```

### Write-Behind Pattern

```csharp
public class UserService
{
    private readonly IBlobCache _cache = CacheDatabase.UserAccount;
    private readonly Queue<PendingUpdate> _pendingUpdates = new();
    
    public async Task UpdateUserAsync(int userId, User updatedUser)
    {
        // Update cache immediately
        var cacheKey = CacheKeys.UserProfile(userId);
        await _cache.InsertObject(cacheKey, updatedUser, TimeSpan.FromMinutes(30));
        
        // Queue backend update for later
        _pendingUpdates.Enqueue(new PendingUpdate { UserId = userId, User = updatedUser });
        
        // Process queue asynchronously (implement background service)
        _ = Task.Run(ProcessPendingUpdates);
    }
}
```

## Performance Tips

1. **Use batch operations** when working with multiple keys
2. **Choose appropriate expiration times** - not too short (cache misses) or too long (stale data)
3. **Use `UpdateExpiration`** instead of re-inserting data when you only need to extend lifetime
4. **Prefer typed operations** (`InvalidateObject<T>`) over untyped ones when possible
5. **Use consistent key naming** for easier maintenance and debugging
6. **Handle exceptions gracefully** to prevent cache issues from breaking your application
7. **Monitor cache hit/miss ratios** to optimize your caching strategy

## Next Steps

- [Learn about extension methods](./extension-methods.md)
- [Explore advanced patterns](./patterns/)
- [Review performance optimization](./performance.md)
- [Check troubleshooting guide](./troubleshooting/troubleshooting-guide.md)