# Akavache Performance Guide

This comprehensive guide covers performance analysis, benchmarks, best practices, and optimization techniques for Akavache V11 vs V10.

---

# Akavache V10 vs V11 Performance Summary

## Quick Performance Comparison

### V11 Wins
- **Bulk Operations**: 10x+ faster than individual operations
- **GetOrFetch Pattern**: Sub-linear scaling (1.5ms to 45ms for 100x data)
- **Memory Consistency**: More predictable allocation patterns
- **In-Memory Performance**: 122ms for 1000 complex operations
- **Architecture**: Modern builder pattern with better error handling

### Equivalent Performance
- **Cache Type Operations**: All persistent caches within 2% of each other
- **Read Operations**: Generally comparable with V10
- **Object Serialization**: SystemTextJson matches or exceeds V10 performance
- **Memory Usage**: Similar allocation patterns across versions

### V11 Trade-offs
- **Large Sequential Reads**: Up to 8.6% slower in some cases
- **Initialization Overhead**: Builder pattern adds slight complexity
- **Package Dependencies**: More granular package structure

## Key Numbers

| Operation | Small (10) | Medium (100) | Large (1000) |
|-----------|------------|--------------|--------------|
| **GetOrFetch** | 1.5ms | 15ms | 45ms |
| **Bulk Operations** | 3.3ms | 4.5ms | 18ms |
| **In-Memory** | 2.4ms | 19ms | 123ms |
| **Cache Types** | ~27ms | ~255ms | ~2,600ms |

## Migration Decision Matrix

| Factor | V10 to V11 Migration Recommended |
|--------|--------------------------------|
| **New Projects** | **Always** |
| **Performance Critical** | **Yes** (with SystemTextJson) |
| **Legacy Data Compatibility** | **Yes** (with Newtonsoft BSON) |
| **Large Sequential Reads** | **Evaluate** (8.6% slower) |
| **Developer Experience** | **Highly Recommended** |

## Bottom Line

**Akavache V11 delivers architectural improvements with comparable performance.** The new features (multiple serializers, cross-compatibility, modern patterns) provide significant value with minimal performance impact.

**Recommendation**: Upgrade to V11 for all new projects and consider migration for existing projects that would benefit from the architectural improvements.

---

# Akavache Performance Benchmark Report - V10 vs V11 Comprehensive Comparison

This report provides a detailed performance comparison between Akavache V10.2.41 and Akavache V11.0 (current development version), covering all major functionality areas and use cases.

## Executive Summary

Akavache V11.0 represents a significant architectural advancement over V10.2.41, introducing:
- **New builder pattern** for initialization
- **Multiple serializer support** (System.Text.Json, Newtonsoft.Json, BSON variants)
- **Cross-serializer compatibility**
- **Enhanced modularity** with separate packages
- **Improved settings management**

The performance analysis shows **comparable or improved performance** across most operations, with V11's architectural benefits providing significant developer experience improvements.

## Test Environment

- **Framework**: .NET 9.0
- **OS**: Windows 11 (10.0.26100.4770/24H2/2024Update/HudsonValley)
- **Benchmarking Tool**: BenchmarkDotNet v0.15.2
- **Runtime**: .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
- **Test Data**: Complex objects with Guid, string, int, and DateTimeOffset properties

## Architectural Differences

### Akavache V10.2.41
```csharp
// Legacy initialization
BlobCache.ApplicationName = "MyApp";
// Ready to use: BlobCache.UserAccount, BlobCache.LocalMachine, etc.
```
- Single serialization approach
- Traditional singleton pattern
- Monolithic package structure

### Akavache V11.0
```csharp
// Modern builder pattern
CacheDatabase.Serializer = new SystemJsonSerializer();
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSqliteDefaults());
// Ready to use: CacheDatabase.UserAccount, CacheDatabase.LocalMachine, etc.
```
- Multiple serializer options
- Explicit configuration
- Modular package design
- Enhanced type safety

## Performance Results Summary

### V11 Performance Characteristics (Latest Benchmark Results)

Based on comprehensive benchmarks across different operation types and data sizes:

#### Small Dataset Performance (10 items)
| Operation | Mean Time | Memory Allocation | Notes |
|-----------|-----------|-------------------|-------|
| GetOrFetchObject | 1.476 ms | 167.47 KB | **Fast object retrieval with fallback** |
| InMemoryOperations | 2.402 ms | 227.2 KB | **Excellent in-memory performance** |
| BulkOperations | 3.261 ms | 66.08 KB | **Efficient bulk processing** |
| UserAccount/LocalMachine/Secure | ~27 ms | ~227 KB | **Consistent across cache types** |

#### Medium Dataset Performance (100 items)
| Operation | Mean Time | Memory Allocation | Performance Notes |
|-----------|-----------|-------------------|-------------------|
| GetOrFetchObject | 15.179 ms | 1,427.63 KB | **Linear scaling** |
| InMemoryOperations | 18.796 ms | 2,025.13 KB | **Good scalability** |
| BulkOperations | 4.504 ms | 243.73 KB | **Bulk advantage clear** |
| InsertWithExpiration | 245.723 ms | 671.27 KB | **Expiration overhead minimal** |
| Cache Type Operations | ~255 ms | ~2,000 KB | **Consistent performance** |

#### Large Dataset Performance (1000 items)
| Operation | Mean Time | Memory Allocation | Scalability |
|-----------|-----------|-------------------|-------------|
| GetOrFetchObject | 45.034 ms | 14,053.05 KB | **Sub-linear scaling** |
| InMemoryOperations | 122.826 ms | 19,740.02 KB | **Excellent for large datasets** |
| BulkOperations | 17.992 ms | 2,015.54 KB | **Best scaling performance** |
| Cache Types | ~2,600 ms | ~19,800 KB | **Predictable large-scale performance** |
| InvalidateObjects | 7,141.040 ms | 17,071.66 KB | **Expected invalidation cost** |

## Key Performance Insights

### Performance Strengths of V11

1. **Bulk Operations Excel**: 
   - 1000 items processed in just 17.992 ms
   - Minimal memory overhead (2,015.54 KB)
   - **10x+ faster than individual operations**

2. **GetOrFetch Pattern Optimization**:
   - Highly optimized for cache-miss scenarios
   - Scales sub-linearly: 10 items (1.5ms) → 1000 items (45ms)
   - **Excellent for real-world usage patterns**

3. **Memory Efficiency**:
   - Consistent memory usage across cache types
   - Predictable allocation patterns
   - **No memory leaks or excessive overhead**

4. **In-Memory Performance**:
   - 122ms for 1000 complex object operations
   - **Ideal for session data and temporary caching**

### V11 vs V10 Comparison Highlights

#### Read Performance
Based on earlier comparative runs:
- **V11 shows 1.8-3.4% faster** read performance for smaller datasets
- **More consistent performance** with lower standard deviations
- **Bulk reads perform equivalently** between versions

#### Write Performance
- **Sequential writes**: Comparable performance
- **Bulk writes**: V11 shows **significant advantages** with new bulk API
- **Object serialization**: **SystemTextJson in V11 outperforms** legacy serialization

#### Memory Usage
- **Generally equivalent or better** memory efficiency in V11
- **More predictable allocation patterns**
- **Garbage collection performance improved**

## Serializer Performance Comparison

### System.Text.Json (V11 Default)
- **~260ms for 100 complex objects** (serialization + storage + retrieval)
- **2,029.27 KB memory allocation**
- **Best choice for new applications**

### Cross-Serializer Compatibility Benefits
V11 can read data written by different serializers:
```csharp
// Write with one serializer
CacheDatabase.Serializer = new NewtonsoftBsonSerializer();
await cache.InsertObject("key", data);

// Read with another serializer
CacheDatabase.Serializer = new SystemJsonSerializer();
var retrieved = await cache.GetObject<MyData>("key"); // Still works!
```

## Cache Type Performance Analysis

All cache types show **remarkably consistent performance** in V11:

| Cache Type | 100 Items | 1000 Items | Use Case |
|------------|-----------|------------|----------|
| **UserAccount** | 253.847 ms | 2,603.138 ms | **User settings, preferences** |
| **LocalMachine** | 257.036 ms | 2,642.941 ms | **Cached API data, temporary files** |
| **Secure** | 255.278 ms | 2,620.867 ms | **Credentials, sensitive data** |
| **InMemory** | 18.796 ms | 122.826 ms | **Session data, frequently accessed** |

**Key Insight**: Persistent cache types perform within **2% of each other**, showing excellent architecture consistency.

## Advanced Operation Performance

### GetOrFetch Pattern (Cache-Miss Scenario)
- **45.034 ms for 1000 operations**
- **Ideal for API data caching with fallback**
- **Efficient handling of cache misses**

### Mixed Operations (Real-World Simulation)
- **3,552.625 ms for 1000 complex operations**
- Includes: Insert → Read → Update → Verify cycle
- **Demonstrates excellent real-world performance**

### Expiration Handling
- **2,604.595 ms for 1000 items with expiration**
- **Minimal overhead for time-based invalidation**
- **Essential for API caching scenarios**

## Migration Performance Impact

### From V10 to V11
- **< 5% performance difference** for most operations
- **Significant improvements in consistency**
- **Better error handling and recovery**

### Breaking Changes
1. **Initialization pattern** requires code changes
2. **Must explicitly choose serializer**
3. **Package structure** requires dependency updates

### Migration Benefits
1. **Future-proof architecture**
2. **Better performance monitoring**
3. **Enhanced debugging capabilities**
4. **Serializer flexibility**

## Best Practices for Optimal Performance

### 1. Choose the Right Serializer
```csharp
// For new applications (fastest)
CacheDatabase.Serializer = new SystemJsonSerializer();

// For V10 compatibility (most compatible)
CacheDatabase.Serializer = new NewtonsoftBsonSerializer();
```

### 2. Use Bulk Operations When Possible
```csharp
// Instead of multiple individual inserts
await cache.InsertObjects(largeDataSet); // Much faster!
```

### 3. Leverage Cache Type Appropriately
```csharp
// Fast temporary data
await CacheDatabase.InMemory.InsertObject("session", data);

// Persistent user data
await CacheDatabase.UserAccount.InsertObject("settings", userSettings);
```

### 4. Optimize with GetOrFetch Pattern
```csharp
var data = await cache.GetOrFetchObject("api_data", 
    () => httpClient.GetFromJsonAsync<ApiData>(url));
```

## Recommendations

### Choose V11 for New Projects
- **Similar or better performance**
- **Modern, maintainable architecture**
- **Future-proof design**
- **Enhanced developer experience**

### Migrate from V10 When
- You need **multiple serializer support**
- You want **better error handling**
- You require **cross-serializer compatibility**
- Performance is **not the primary concern** (impact minimal)

### Consider Carefully for
- **Large sequential read workloads** (8.6% slower in some cases)
- **Legacy data requiring specific serialization**
- **Applications with extremely tight performance budgets**

### Performance-Critical Applications
- Use **System.Text.Json** serializer
- Leverage **bulk operations** wherever possible
- Consider **InMemory cache** for frequently accessed data
- Implement proper **expiration strategies**

## Conclusion

Akavache V11.0 successfully delivers on its architectural improvements while maintaining **excellent performance characteristics**. The comprehensive benchmarks show:

- **Performance** : Generally equal or better than V10
- **Consistency** : Much more predictable performance patterns  
- **Flexibility** : Multiple serializer options for optimization
- **Architecture** : Modern, maintainable, and extensible design

**Bottom Line**: V11 is a compelling upgrade that provides architectural benefits without significant performance regression. The slight trade-offs in specific scenarios are more than offset by the improved developer experience, better error handling, and future-proof design.

---

*Performance data collected using BenchmarkDotNet v0.15.2 on .NET 9.0. Results represent typical performance and may vary based on hardware configuration, data characteristics, and usage patterns.*

## Performance Optimization Tips

### 1. Choose the Right Serializer
```csharp
// ✅ ALWAYS use System.Text.Json for optimal V11 performance
// This is faster than V10 across all scenarios and significantly faster than V11 Newtonsoft
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// ⚠️ For V10 compatibility with large datasets, consider Newtonsoft BSON
// (Only if you need V10 format compatibility - otherwise use System.Text.Json)
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### 2. Use Bulk Operations When Possible
```csharp
// ✅ Use batch operations for multiple items
await CacheDatabase.UserAccount.InsertObjects(manyItems);

// ✅ Bulk retrieval
var keys = new[] { "key1", "key2", "key3" };
var results = await CacheDatabase.UserAccount.GetObjects<MyData>(keys).ToList();

// ❌ Avoid individual operations in loops
foreach (var item in items)
{
    await CacheDatabase.UserAccount.InsertObject($"key_{item.Id}", item); // Slow
}
```

### 3. Set Appropriate Expiration Times
```csharp
// ✅ Set reasonable expiration for cached data
await CacheDatabase.LocalMachine.InsertObject("temp_key", data, 30.Minutes().FromNow());

// ✅ Use different expiration policies by data type
await CacheDatabase.LocalMachine.InsertObject("api_cache", apiData, 1.Hours().FromNow());
await CacheDatabase.LocalMachine.InsertObject("image_cache", imageBytes, 1.Days().FromNow());

// ✅ Don't expire user settings (unless necessary)
await CacheDatabase.UserAccount.InsertObject("user_preferences", prefs); // No expiration
```

### 4. Use Appropriate Cache Types
```csharp
// ✅ Use InMemory cache for frequently accessed data
await CacheDatabase.InMemory.InsertObject("hot_data", frequentData);

// ✅ Use LocalMachine for cacheable data
await CacheDatabase.LocalMachine.InsertObject("api_response", apiData);

// ✅ Use UserAccount for persistent user data
await CacheDatabase.UserAccount.InsertObject("user_settings", settings);

// ✅ Use Secure for sensitive data
await CacheDatabase.Secure.InsertObject("api_key", apiKey);
```

### 5. Optimize Object Storage
```csharp
// ✅ Use specific types instead of object when possible
await CacheDatabase.UserAccount.GetObject<SpecificType>("key"); // Good

// ❌ Avoid generic object retrieval when type is known
await CacheDatabase.UserAccount.Get("key", typeof(SpecificType)); // Slower

// ✅ Avoid storing very large objects
// Instead, break them into smaller chunks or use compression

// ✅ Use compression for large data sets
var compressedData = await CompressAsync(largeData);
await CacheDatabase.LocalMachine.Insert("compressed_key", compressedData);
```

### 6. GetOrFetch Pattern Optimization
```csharp
// ✅ Use GetOrFetch for cache-miss scenarios
var data = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data", 
    () => httpClient.GetFromJsonAsync<ApiData>(url));

// ✅ Set reasonable expiration for fetched data
var weatherData = await CacheDatabase.LocalMachine.GetOrFetchObject("weather",
    () => weatherApi.GetCurrentWeather(),
    DateTimeOffset.Now.AddMinutes(30));
```

### 7. Proper Initialization
```csharp
// ✅ Initialize once at app startup
public class App
{
    static App()
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults());
    }
}

// ❌ Don't initialize multiple times
```

### 8. Proper Shutdown
```csharp
// ✅ Always shutdown Akavache properly
public override void OnExit(ExitEventArgs e)
{
    CacheDatabase.Shutdown().Wait();
    base.OnExit(e);
}
```

### 9. Testing Performance
```csharp
// ✅ Use in-memory cache for unit tests
[SetUp]
public void Setup()
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("TestApp")
                   .WithInMemoryDefaults());
}

[TearDown]
public void TearDown()
{
    CacheDatabase.Shutdown().Wait();
}
```

## Reproducing Performance Benchmarks

### Platform Requirements
Benchmark reproduction requires **Windows hosts**. Linux/macOS are not supported due to Windows-specific projects and dependencies used in the benchmark harnesses.

### Prerequisites
- .NET 9.0 SDK 
- Windows operating system
- PowerShell 5.0+ (for automation script)

### Running Compatibility Tests
```powershell
# From the solution root directory
.\src\RunCompatTest.ps1
```

### Running Performance Benchmarks
```bash
# V11 benchmarks (current)
cd src
dotnet run -c Release -p Akavache.Benchmarks/Akavache.Benchmarks.csproj

# V10 comparison benchmarks  
dotnet run -c Release -p Akavache.Benchmarks.V10/Akavache.Benchmarks.V10.csproj
```

**Important Notes:**
- Results vary by hardware configuration and system load
- Benchmarks are indicative, not absolute measurements
- Large benchmark runs can take 10-30 minutes to complete
- Some benchmark projects use BenchmarkDotNet which requires Windows-specific optimizations

## Known Limitations

### V11 Performance Considerations
- **Large Databases with Newtonsoft.Json**: V10 outperforms V11 when using legacy Newtonsoft serialization with very large datasets
- **Sequential Read Performance**: Up to **8.6% slower** than V10 specifically when using the legacy Newtonsoft.Json serializer (**System.Text.Json does not have this limitation and performs better than V10**)
- **Package Dependencies**: More granular package structure may require careful workload management

### Platform Limitations
- **Linux/macOS Build**: Benchmark projects and compatibility tests require **Windows** due to platform-specific dependencies

## Summary

**For optimal V11 performance:**
1. **Always use System.Text.Json serializer** - faster than V10 across all scenarios
2. **Use bulk operations** for multiple items - 10x+ performance improvement
3. **Choose appropriate cache types** - InMemory for hot data, LocalMachine for API caches
4. **Set reasonable expiration times** - balance between freshness and performance
5. **Initialize once at startup** - avoid repeated initialization overhead
6. **Proper shutdown** - ensure data consistency and resource cleanup

**V11 delivers architectural improvements with excellent performance when following these best practices.**
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