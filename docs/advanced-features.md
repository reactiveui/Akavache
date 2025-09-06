# Advanced Features

### Efficient Expiration Updates

Akavache provides `UpdateExpiration` methods that efficiently update cache entry expiration dates without reading or writing the cached data. This is particularly useful for HTTP caching scenarios and session management.

#### Key Benefits

- **High Performance**: Only updates metadata, leaving cached data untouched  
- **SQL Efficiency**: Uses targeted UPDATE statements rather than full record replacement  
- **Bulk Operations**: Update multiple entries in a single transaction  
- **No Data Transfer**: Avoids expensive serialization/deserialization cycles (up to 250x faster)

#### Quick Examples

```csharp
// Single entry with absolute expiration
await cache.UpdateExpiration("api_response", DateTimeOffset.Now.AddHours(6));

// Single entry with relative time
await cache.UpdateExpiration("user_session", TimeSpan.FromMinutes(30));

// Bulk update multiple entries
var keys = new[] { "weather_seattle", "weather_portland", "weather_vancouver" };
await cache.UpdateExpiration(keys, TimeSpan.FromHours(2));

// HTTP 304 Not Modified response handling
if (response.StatusCode == HttpStatusCode.NotModified)
{
    await cache.UpdateExpiration(cacheKey, TimeSpan.FromHours(1));
    return cachedData; // Serve existing data with extended lifetime
}
```

### **Relative Time Extensions**

```csharp
// Cache for relative time periods
await CacheDatabase.LocalMachine.InsertObject("data", myData, TimeSpan.FromMinutes(30).FromNow());

// Use in get-or-fetch
var cachedData = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data",
    () => FetchFromApi(),
    1.Hours().FromNow());
```

### **Custom Schedulers**

```csharp
// Use custom scheduler for background operations
CacheDatabase.TaskpoolScheduler = TaskPoolScheduler.Default;

// Or use a custom scheduler
CacheDatabase.TaskpoolScheduler = new EventLoopScheduler();
```

### **Cache Inspection**

```csharp
// Get all keys (for debugging)
var allKeys = await CacheDatabase.UserAccount.GetAllKeys().ToList();

// Safe key enumeration with exception handling in observable chain
var safeKeys = await CacheDatabase.UserAccount.GetAllKeysSafe().ToList();
// GetAllKeysSafe catches exceptions and continues the observable chain
// instead of throwing - useful for robust error handling

// Get keys for specific types safely
var typedKeys = await CacheDatabase.UserAccount.GetAllKeysSafe<MyDataType>().ToList();
var specificTypeKeys = await CacheDatabase.UserAccount.GetAllKeysSafe(typeof(string)).ToList();

// Check when item was created
var createdAt = await CacheDatabase.UserAccount.GetCreatedAt("my_key");
if (createdAt.HasValue)
{
    Console.WriteLine($"Item created at: {createdAt.Value}");
}

// Get creation times for multiple keys
var creationTimes = await CacheDatabase.UserAccount.GetCreatedAt(new[] { "key1", "key2" })
   .ToList();
```

#### **GetAllKeysSafe Methods**

The GetAllKeysSafe methods provide exception-safe alternatives to GetAllKeys() that handle errors within the observable chain:

```csharp
// Standard GetAllKeys() - exceptions break the observable chain
try 
{
    var keys = await CacheDatabase.UserAccount.GetAllKeys().ToList();
    // Process keys...
}
catch (Exception ex)
{
    // Handle exception outside observable chain
}

// GetAllKeysSafe() - exceptions are caught and logged, chain continues
await CacheDatabase.UserAccount.GetAllKeysSafe()
   .Do(key => Console.WriteLine($"Found key: {key}"))
   .Where(key => ShouldProcess(key))
   .ForEach(key => ProcessKey(key));
    // If GetAllKeys() would throw, this continues with empty sequence instead
```

**Key differences:**

* **Exception handling**: Catches exceptions and returns empty sequence instead of throwing  
* **Null safety**: Filters out null or empty keys automatically  
* **Observable chain friendly**: Allows reactive code to continue executing even when underlying storage has issues  
* **Logging**: Logs exceptions for debugging while keeping the application stable

**Use GetAllKeysSafe when:**

* Building reactive pipelines that should be resilient to storage exceptions  
* You want exceptions handled within the observable chain rather than breaking it  
* Working with unreliable storage scenarios or during development/testing  
* You prefer continuation over immediate failure when key enumeration fails

### **Cache Maintenance**

```csharp
// Force flush all pending operations
await CacheDatabase.UserAccount.Flush();

// Vacuum database (SQLite only - removes deleted data)
await CacheDatabase.UserAccount.Vacuum();

// Flush specific object type
await CacheDatabase.UserAccount.Flush(typeof(MyDataType));
```

### **Mixed Object Storage**

```csharp
// Store different types with one operation
var mixedData = new Dictionary<string, object>
{
    ["string_data"] = "Hello World",
    ["number_data"] = 42,
    ["object_data"] = new MyClass { Value = "test" },
    ["date_data"] = DateTime.Now
};

await CacheDatabase.UserAccount.InsertObjects(mixedData);
```
