# Performance Guide

This guide covers performance optimization, benchmarks, and best practices for getting the most out of Akavache.

## Performance Overview

Akavache V11.1 delivers **architectural improvements with optimal performance** when using the recommended System.Text.Json serializer. **V11 with System.Text.Json outperforms V10 across all test scenarios**, while V11 with the legacy Newtonsoft.Json may be slower than V10 for very large datasets.

## Key Performance Metrics

Based on comprehensive benchmarks across different operation types and data sizes:

| Operation | Small (10 items) | Medium (100 items) | Large (1000 items) | Notes |
|-----------|-------------------|--------------------|--------------------|-------|
| **GetOrFetch** | 1.5ms | 15ms | 45ms | Sub-linear scaling, excellent for cache-miss scenarios |
| **Bulk Operations** | 3.3ms | 4.5ms | 18ms | **10x+ faster** than individual operations |
| **In-Memory** | 2.4ms | 19ms | 123ms | Ideal for session data and frequently accessed objects |
| **Cache Types** | ~27ms | ~255ms | ~2,600ms | Consistent performance across UserAccount/LocalMachine/Secure |

## V11 vs V10 Performance Comparison

### Read Performance
- **V11 with System.Text.Json**: **1.8-3.4% faster** than V10 for smaller datasets
- **V11 with Newtonsoft.Json**: Comparable to V10 for small datasets, slower for large datasets
- **Consistency**: More consistent results with reduced performance variance

### Write Performance
- **Sequential writes**: Comparable between V11 and V10
- **Bulk writes**: **Significant advantages** in V11 with improved batch operations
- **Memory efficiency**: Generally equivalent or better memory efficiency

### Memory Usage
- **V11 System.Text.Json**: Better memory efficiency and more predictable allocation patterns
- **V11 Newtonsoft.Json**: Comparable to V10, slightly higher overhead
- **Garbage collection**: Reduced GC pressure with System.Text.Json

## Serializer Performance Comparison

### System.Text.Json (Recommended for V11)
✅ **Best overall performance** for both small and large datasets  
✅ **Faster than V10** across all test scenarios  
✅ **Modern .NET optimization** with excellent memory efficiency  
✅ **Native AOT compatibility** for deployment scenarios  

```csharp
// Optimal configuration for performance
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### Newtonsoft.Json in V11 (Legacy Compatibility)
⚠️ **Slower than V10 with large databases** - V10 Newtonsoft performs better for huge datasets  
✅ **Faster than V10** for smaller to medium datasets  
✅ **Compatible with existing V10 data** structures  
❌ **Higher memory overhead** compared to System.Text.Json  

```csharp
// Use only when V10 compatibility is required
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

## Performance Optimization Tips

### 1. Choose the Right Serializer

```csharp
// ✅ ALWAYS use System.Text.Json for optimal V11 performance
// This is faster than V10 across all scenarios
.WithAkavacheCacheDatabase<SystemJsonSerializer>()

// ⚠️ Only use Newtonsoft.Json for V10 compatibility
.WithAkavacheCacheDatabase<NewtonsoftJsonSerializer>()
```

### 2. Use Batch Operations

```csharp
// ❌ Slow - Individual operations
foreach (var item in items)
{
    await cache.InsertObject($"key_{item.Id}", item);
}

// ✅ Fast - Batch operation (10x+ faster)
var keyValuePairs = items.ToDictionary(item => $"key_{item.Id}", item => item);
await cache.InsertObjects(keyValuePairs);
```

### 3. Choose Appropriate Cache Types

```csharp
// ✅ InMemory for frequently accessed data
await CacheDatabase.InMemory.InsertObject("hot_data", frequentData);

// ✅ UserAccount for user-specific persistent data
await CacheDatabase.UserAccount.InsertObject("user_prefs", userPreferences);

// ✅ LocalMachine for shared application data
await CacheDatabase.LocalMachine.InsertObject("app_config", configuration);
```

### 4. Set Appropriate Expiration Times

```csharp
// ✅ Short expiration for frequently changing data
await cache.InsertObject("stock_price", price, TimeSpan.FromMinutes(1));

// ✅ Long expiration for stable reference data
await cache.InsertObject("country_list", countries, TimeSpan.FromDays(30));

// ✅ No expiration for settings and configuration
await cache.InsertObject("user_settings", settings);
```

### 5. Use Specific Types Instead of Object

```csharp
// ✅ Fast - Specific type
await cache.GetObject<SpecificType>("key");

// ❌ Slower - Generic object
await cache.Get("key", typeof(SpecificType));
```

### 6. Optimize Object Size

```csharp
// ❌ Large objects slow down serialization
public class HeavyObject
{
    public byte[] LargeByteArray { get; set; } // Multiple MB
    public List<ComplexNestedObject> Items { get; set; } // Thousands of items
}

// ✅ Break large objects into smaller chunks
public class OptimizedObject
{
    public string Id { get; set; }
    public string DataReference { get; set; } // Reference to chunked data
}

// Store large data separately in chunks
await cache.InsertObject("data_chunk_1", chunk1);
await cache.InsertObject("data_chunk_2", chunk2);
```

### 7. Use UpdateExpiration for Lifetime Extension

```csharp
// ❌ Expensive - Re-serializes entire object
var data = await cache.GetObject<LargeObject>("key");
await cache.InsertObject("key", data, newExpiration);

// ✅ Efficient - Only updates metadata (up to 250x faster)
await cache.UpdateExpiration("key", newExpiration);
```

## BSON for Performance

### When to Use BSON
- **Large objects** - BSON is more efficient for complex data structures
- **Binary data** - Better handling of byte arrays and binary content
- **Performance critical** - BSON serialization is typically 20-40% faster for large objects

```csharp
// System.Text.Json BSON (recommended)
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonBsonSerializer>(builder => /* ... */);

// Newtonsoft.Json BSON (for V10 compatibility)
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(builder => /* ... */);
```

## Memory Optimization

### 1. Monitor Memory Usage

```csharp
public class CacheMemoryMonitor
{
    public static void LogMemoryUsage(string operation)
    {
        var beforeGC = GC.GetTotalMemory(false);
        GC.Collect();
        var afterGC = GC.GetTotalMemory(true);
        
        Console.WriteLine($"{operation}: {beforeGC:N0} -> {afterGC:N0} bytes");
    }
}
```

### 2. Clear Unnecessary Data

```csharp
// Periodically clean up expired entries
await cache.Vacuum();

// Clear specific data types when no longer needed
await cache.InvalidateAllObjects<TemporaryData>();

// Clear InMemory cache during memory pressure
if (MemoryPressure.IsHigh())
{
    await CacheDatabase.InMemory.InvalidateAll();
}
```

### 3. Use Weak References for Large Objects

```csharp
public class SmartCacheWrapper<T> where T : class
{
    private readonly WeakReference<T> _weakRef = new(null);
    private readonly string _cacheKey;
    private readonly IBlobCache _cache;
    
    public async Task<T> GetObject()
    {
        if (_weakRef.TryGetTarget(out var cached))
            return cached;
            
        var data = await _cache.GetObject<T>(_cacheKey);
        _weakRef.SetTarget(data);
        return data;
    }
}
```

## Performance Monitoring

### 1. Measure Cache Hit Rates

```csharp
public class CacheMetrics
{
    private long _hits = 0;
    private long _misses = 0;
    
    public double HitRate => _hits / (double)(_hits + _misses);
    
    public async Task<T> GetWithMetrics<T>(string key)
    {
        try
        {
            var result = await cache.GetObject<T>(key);
            Interlocked.Increment(ref _hits);
            return result;
        }
        catch (KeyNotFoundException)
        {
            Interlocked.Increment(ref _misses);
            throw;
        }
    }
}
```

### 2. Track Operation Performance

```csharp
public class PerformanceTracker
{
    public static async Task<T> TimeOperation<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await operation();
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"{operationName}: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}

// Usage
var data = await PerformanceTracker.TimeOperation("Cache Get", 
    () => cache.GetObject<MyData>("key"));
```

### 3. Monitor SQLite Database Size

```csharp
public class CacheSizeMonitor
{
    public static long GetDatabaseSize(string dbPath)
    {
        if (File.Exists(dbPath))
            return new FileInfo(dbPath).Length;
        return 0;
    }
    
    public static void LogCacheSizes()
    {
        var basePath = CacheDatabase.GetCacheDirectory();
        
        Console.WriteLine($"UserAccount: {GetDatabaseSize(Path.Combine(basePath, "userAccount.db")):N0} bytes");
        Console.WriteLine($"LocalMachine: {GetDatabaseSize(Path.Combine(basePath, "localMachine.db")):N0} bytes");
        Console.WriteLine($"Secure: {GetDatabaseSize(Path.Combine(basePath, "secret.db")):N0} bytes");
    }
}
```

## Benchmarking Your Application

### 1. Benchmark Framework

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CacheBenchmarks
{
    private IBlobCache _cache;
    private TestData[] _testData;
    
    [GlobalSetup]
    public void Setup()
    {
        _cache = new InMemoryBlobCache(new SystemJsonSerializer());
        _testData = GenerateTestData(1000);
    }
    
    [Benchmark]
    public async Task InsertObjects()
    {
        var dict = _testData.ToDictionary(d => d.Id.ToString(), d => d);
        await _cache.InsertObjects(dict);
    }
    
    [Benchmark]
    public async Task GetObjects()
    {
        var keys = _testData.Select(d => d.Id.ToString()).ToArray();
        var results = await _cache.GetObjects<TestData>(keys);
    }
}
```

### 2. Run Benchmarks

```bash
# Install BenchmarkDotNet
dotnet add package BenchmarkDotNet

# Run benchmarks
dotnet run -c Release --project YourBenchmarkProject
```

## Known Performance Limitations

### 1. Large Databases with Newtonsoft.Json
- **Issue**: V10 outperforms V11 when using legacy Newtonsoft serialization with very large datasets
- **Solution**: Use System.Text.Json for better performance

### 2. Sequential Read Performance
- **Issue**: Up to 8.6% slower than V10 specifically when using Newtonsoft.Json serializer
- **Solution**: System.Text.Json does not have this limitation and performs better than V10

### 3. Cross-Platform Differences
- **Issue**: Performance varies between platforms due to different SQLite implementations
- **Solution**: Test on target platforms and optimize accordingly

## Performance Reports

For comprehensive performance analysis:

- 📊 **[Performance Summary](../src/PERFORMANCE_SUMMARY.md)** - Quick comparison and migration decision matrix
- 📈 **[Comprehensive Benchmark Report](../src/BENCHMARK_REPORT.md)** - Detailed performance analysis and recommendations

## Reproducing Benchmarks

### Platform Requirements
Benchmark reproduction requires **Windows hosts**. Linux/macOS are not supported due to Windows-specific dependencies.

### Prerequisites
- .NET 9.0 SDK 
- Windows operating system
- PowerShell 5.0+ (for automation script)

### Running Performance Benchmarks

```bash
# V11 benchmarks (current)
cd src
dotnet run -c Release -p Akavache.Benchmarks/Akavache.Benchmarks.csproj

# V10 comparison benchmarks  
dotnet run -c Release -p Akavache.Benchmarks.V10/Akavache.Benchmarks.V10.csproj
```

### Running Compatibility Tests

```powershell
# From the solution root directory
.\src\RunCompatTest.ps1
```

## Performance Best Practices Summary

1. **Use System.Text.Json** for optimal performance in V11
2. **Batch operations** when working with multiple items
3. **Choose appropriate cache types** based on access patterns
4. **Set reasonable expiration times** to balance freshness and performance
5. **Monitor cache hit rates** and adjust strategies accordingly
6. **Use UpdateExpiration** instead of re-inserting data when extending lifetime
7. **Break large objects** into smaller, manageable chunks
8. **Clear unnecessary data** regularly to maintain performance
9. **Monitor memory usage** and implement pressure relief mechanisms
10. **Test on target platforms** as performance characteristics vary

## Next Steps

- [Review best practices](./best-practices.md)
- [Check troubleshooting guide](./troubleshooting/troubleshooting-guide.md)
- [Explore platform-specific optimizations](./platform-notes.md)
- [Learn about cache patterns](./patterns/)