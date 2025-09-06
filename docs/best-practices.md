# Best Practices Guide

This guide covers recommended patterns, conventions, and practices for using Akavache effectively in production applications.

## 1. Initialization

### Early Initialization
```csharp
// ✅ Initialize as early as possible in your application lifecycle
public class Program
{
    public static void Main()
    {
        // Initialize before any cache operations
        ConfigureAkavache();
        
        // Start your application
        Application.Run();
    }
    
    private static void ConfigureAkavache()
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyApp")
                       .WithSqliteProvider()    // Always explicit
                       .WithSqliteDefaults());
    }
}
```

### One-Time Initialization
```csharp
// ✅ Initialize once, use everywhere
public static class CacheInitializer
{
    private static bool _initialized = false;
    private static readonly object _lock = new object();
    
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            
            AppBuilder.CreateSplatBuilder()
                .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                    builder.WithApplicationName("MyApp")
                           .WithSqliteProvider()
                           .WithSqliteDefaults());
            
            _initialized = true;
        }
    }
}
```

### Environment-Specific Configuration
```csharp
public static void ConfigureForEnvironment(string environment)
{
    var appName = environment switch
    {
        "Development" => "MyApp-Dev",
        "Testing" => "MyApp-Test", 
        "Staging" => "MyApp-Stage",
        _ => "MyApp"
    };
    
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName(appName)
                   .WithSqliteProvider()
                   .WithSqliteDefaults());
}
```

## 2. Key Naming

### Consistent Naming Conventions
```csharp
// ✅ Use hierarchical, consistent naming
public static class CacheKeys
{
    // Domain:Entity:Identifier pattern
    public static string UserProfile(int userId) => $"user:profile:{userId}";
    public static string UserSettings(int userId) => $"user:settings:{userId}";
    
    // API:Endpoint:Parameters pattern
    public static string ApiProducts(int page, int size) => $"api:products:page:{page}:size:{size}";
    public static string ApiUserData(int userId) => $"api:userdata:{userId}";
    
    // Temporary data with prefix
    public static string TempCalculation(string hash) => $"temp:calc:{hash}";
    public static string SessionData(string sessionId) => $"session:data:{sessionId}";
}
```

### Avoid Key Collisions
```csharp
// ❌ Risky - potential collisions
await cache.InsertObject("user", userData);
await cache.InsertObject("user_settings", userSettings); // Could conflict

// ✅ Safe - clear separation
await cache.InsertObject(CacheKeys.UserProfile(userId), userData);
await cache.InsertObject(CacheKeys.UserSettings(userId), userSettings);
```

### Key Length Considerations
```csharp
// ✅ Reasonable key length
"user:profile:12345" // Good

// ❌ Excessively long keys
"user:profile:with:lots:of:nested:hierarchies:and:very:long:descriptive:names:12345" // Avoid
```

## 3. Error Handling

### Graceful Degradation
```csharp
public class ResilientCacheService
{
    private readonly IBlobCache _cache = CacheDatabase.UserAccount;
    
    public async Task<T> GetWithFallback<T>(string key, Func<Task<T>> fallback)
    {
        try
        {
            return await _cache.GetObject<T>(key);
        }
        catch (KeyNotFoundException)
        {
            // Expected - key doesn't exist
            return await fallback();
        }
        catch (Exception ex)
        {
            // Unexpected error - log and fallback
            Logger.LogWarning(ex, "Cache error for key {Key}", key);
            return await fallback();
        }
    }
}
```

### Comprehensive Error Handling
```csharp
public async Task<CacheResult<T>> SafeGetObject<T>(string key)
{
    try
    {
        var value = await _cache.GetObject<T>(key);
        return CacheResult<T>.Success(value);
    }
    catch (KeyNotFoundException)
    {
        return CacheResult<T>.NotFound();
    }
    catch (SerializationException ex)
    {
        Logger.LogError(ex, "Serialization error for key {Key}", key);
        return CacheResult<T>.Error(ex);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Unexpected cache error for key {Key}", key);
        return CacheResult<T>.Error(ex);
    }
}

public class CacheResult<T>
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public T Value { get; init; }
    public Exception Error { get; init; }
    
    public static CacheResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static CacheResult<T> NotFound() => new() { IsNotFound = true };
    public static CacheResult<T> Error(Exception error) => new() { Error = error };
}
```

### Circuit Breaker Pattern
```csharp
public class CacheCircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, Func<Task<T>> fallback)
    {
        if (IsCircuitOpen())
        {
            return await fallback();
        }
        
        try
        {
            var result = await operation();
            Reset();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();
            Logger.LogWarning(ex, "Cache operation failed, failure count: {Count}", _failureCount);
            return await fallback();
        }
    }
    
    private bool IsCircuitOpen() => 
        _failureCount >= _failureThreshold && 
        DateTime.UtcNow - _lastFailureTime < _timeout;
    
    private void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
    }
    
    private void Reset()
    {
        _failureCount = 0;
        _lastFailureTime = DateTime.MinValue;
    }
}
```

## 4. Cache Types Usage

### Choose the Right Cache Type
```csharp
public class DataService
{
    // ✅ User-specific data in UserAccount
    public async Task SaveUserPreferences(int userId, UserPreferences prefs)
    {
        await CacheDatabase.UserAccount.InsertObject(
            CacheKeys.UserSettings(userId), prefs);
    }
    
    // ✅ App-wide data in LocalMachine
    public async Task CacheAppConfiguration(AppConfig config)
    {
        await CacheDatabase.LocalMachine.InsertObject("app:config", config);
    }
    
    // ✅ Sensitive data in Secure
    public async Task SaveAuthToken(string token)
    {
        await CacheDatabase.Secure.InsertObject("auth:token", token);
    }
    
    // ✅ Temporary data in InMemory
    public async Task CacheTemporaryCalculation(string key, object result)
    {
        await CacheDatabase.InMemory.InsertObject(key, result, TimeSpan.FromMinutes(30));
    }
}
```

### Cache Type Decision Matrix
```csharp
public static class CacheTypeSelector
{
    public static IBlobCache SelectCache(bool isSensitive, bool isPersistent, bool isUserSpecific)
    {
        return (isSensitive, isPersistent, isUserSpecific) switch
        {
            (true, _, _) => CacheDatabase.Secure,           // Always use Secure for sensitive data
            (false, false, _) => CacheDatabase.InMemory,    // InMemory for temporary data
            (false, true, true) => CacheDatabase.UserAccount, // UserAccount for user-specific persistent data
            (false, true, false) => CacheDatabase.LocalMachine, // LocalMachine for shared persistent data
        };
    }
}
```

## 5. Expiration

### Appropriate Expiration Times
```csharp
public static class ExpirationPolicy
{
    // Short-lived data
    public static TimeSpan StockPrices => TimeSpan.FromMinutes(1);
    public static TimeSpan WeatherData => TimeSpan.FromMinutes(15);
    public static TimeSpan NewsArticles => TimeSpan.FromMinutes(30);
    
    // Medium-lived data
    public static TimeSpan UserProfile => TimeSpan.FromHours(4);
    public static TimeSpan ProductCatalog => TimeSpan.FromHours(12);
    public static TimeSpan SearchResults => TimeSpan.FromHours(1);
    
    // Long-lived data
    public static TimeSpan AppConfiguration => TimeSpan.FromDays(1);
    public static TimeSpan ReferenceData => TimeSpan.FromDays(7);
    public static TimeSpan StaticContent => TimeSpan.FromDays(30);
    
    // No expiration for settings
    public static TimeSpan? UserSettings => null;
    public static TimeSpan? ApplicationState => null;
}
```

### Dynamic Expiration Based on Data
```csharp
public static TimeSpan CalculateExpiration(DataFreshness freshness, int priority)
{
    var baseExpiration = freshness switch
    {
        DataFreshness.RealTime => TimeSpan.FromMinutes(1),
        DataFreshness.Recent => TimeSpan.FromMinutes(15),
        DataFreshness.Hourly => TimeSpan.FromHours(1),
        DataFreshness.Daily => TimeSpan.FromHours(12),
        DataFreshness.Static => TimeSpan.FromDays(7)
    };
    
    // Adjust based on priority
    var multiplier = priority switch
    {
        1 => 0.5,  // High priority = shorter expiration
        2 => 1.0,  // Normal priority = base expiration
        3 => 2.0,  // Low priority = longer expiration
        _ => 1.0
    };
    
    return TimeSpan.FromMilliseconds(baseExpiration.TotalMilliseconds * multiplier);
}
```

## 6. Shutdown

### Proper Application Shutdown
```csharp
public class ApplicationLifecycleManager
{
    public async Task ShutdownAsync()
    {
        try
        {
            // Ensure all pending operations complete
            await CacheDatabase.Flush();
            
            // Proper shutdown to prevent data corruption
            await CacheDatabase.Shutdown();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during cache shutdown");
        }
    }
}
```

### Platform-Specific Shutdown
```csharp
// WPF
public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        CacheDatabase.Shutdown().Wait();
        base.OnExit(e);
    }
}

// MAUI
public partial class App : Application
{
    protected override void CleanUp()
    {
        CacheDatabase.Shutdown().Wait();
        base.CleanUp();
    }
}

// Console Application
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            await CacheDatabase.Shutdown();
            Environment.Exit(0);
        };
        
        // Application logic
        
        await CacheDatabase.Shutdown();
    }
}
```

## 7. Testing

### Unit Testing with Akavache
```csharp
[TestFixture]
public class CacheServiceTests
{
    private IBlobCache _testCache;
    
    [SetUp]
    public void SetUp()
    {
        // Use InMemory cache for fast, isolated tests
        _testCache = new InMemoryBlobCache(new SystemJsonSerializer());
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _testCache?.Dispose();
    }
    
    [Test]
    public async Task Should_Store_And_Retrieve_Data()
    {
        // Arrange
        var testData = new TestModel { Id = 1, Name = "Test" };
        
        // Act
        await _testCache.InsertObject("test", testData);
        var retrieved = await _testCache.GetObject<TestModel>("test");
        
        // Assert
        Assert.AreEqual(testData.Name, retrieved.Name);
    }
}
```

### Integration Testing
```csharp
[TestFixture]
public class CacheIntegrationTests
{
    private string _tempDirectory;
    
    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        // Initialize with test configuration
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("TestApp")
                       .WithSqliteProvider()
                       .WithCacheDirectory(_tempDirectory)
                       .WithSqliteDefaults());
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await CacheDatabase.Shutdown();
        
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
    
    [Test]
    public async Task Should_Persist_Data_Across_Restarts()
    {
        // Store data
        await CacheDatabase.UserAccount.InsertObject("test", "value");
        
        // Shutdown and restart
        await CacheDatabase.Shutdown();
        
        // Reinitialize
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("TestApp")
                       .WithSqliteProvider()
                       .WithCacheDirectory(_tempDirectory)
                       .WithSqliteDefaults());
        
        // Verify data persisted
        var retrieved = await CacheDatabase.UserAccount.GetObject<string>("test");
        Assert.AreEqual("value", retrieved);
    }
}
```

### Mock-Friendly Service Design
```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}

public class AkavacheCacheService : ICacheService
{
    private readonly IBlobCache _cache;
    
    public AkavacheCacheService(IBlobCache cache = null)
    {
        _cache = cache ?? CacheDatabase.UserAccount;
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        return await _cache.GetObject<T>(key);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (expiration.HasValue)
            await _cache.InsertObject(key, value, expiration.Value);
        else
            await _cache.InsertObject(key, value);
    }
    
    public async Task RemoveAsync(string key)
    {
        await _cache.Invalidate(key);
    }
}
```

## Data Design Best Practices

### Serializable Data Objects
```csharp
// ✅ Good - Simple, serializable DTO
public class UserProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime LastLoginUtc { get; set; }
    
    // Avoid complex objects, circular references, or non-serializable types
}

// ❌ Avoid - Complex object with circular references
public class ProblematicUser
{
    public UserGroup Group { get; set; }  // Contains reference back to user
    public FileStream Data { get; set; }  // Not serializable
    public Func<string, bool> Validator { get; set; }  // Not serializable
}
```

### Versioning Strategy
```csharp
public class VersionedData
{
    public int Version { get; set; } = 1;
    public string Data { get; set; }
    
    // Can safely add new properties in future versions
    public DateTime? CreatedAtUtc { get; set; }  // Added in v2
}

public static class DataMigration
{
    public static VersionedData MigrateToLatest(VersionedData data)
    {
        return data.Version switch
        {
            1 => MigrateV1ToV2(data),
            2 => data, // Already latest
            _ => throw new NotSupportedException($"Version {data.Version} not supported")
        };
    }
    
    private static VersionedData MigrateV1ToV2(VersionedData v1Data)
    {
        return new VersionedData
        {
            Version = 2,
            Data = v1Data.Data,
            CreatedAtUtc = DateTime.UtcNow // Set default for new field
        };
    }
}
```

## Monitoring and Diagnostics

### Health Checks
```csharp
public class CacheHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic cache operations
            var testKey = "health_check_test";
            var testValue = DateTime.UtcNow.ToString();
            
            await CacheDatabase.InMemory.InsertObject(testKey, testValue);
            var retrieved = await CacheDatabase.InMemory.GetObject<string>(testKey);
            await CacheDatabase.InMemory.Invalidate(testKey);
            
            if (retrieved == testValue)
            {
                return HealthCheckResult.Healthy("Cache is working properly");
            }
            else
            {
                return HealthCheckResult.Degraded("Cache returned incorrect data");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache is not working", ex);
        }
    }
}
```

### Metrics Collection
```csharp
public class CacheMetrics
{
    private static readonly Counter CacheHits = Metrics
        .CreateCounter("cache_hits_total", "Total cache hits");
    private static readonly Counter CacheMisses = Metrics
        .CreateCounter("cache_misses_total", "Total cache misses");
    private static readonly Histogram CacheOperationDuration = Metrics
        .CreateHistogram("cache_operation_duration_seconds", "Cache operation duration");
    
    public static async Task<T> MeasureGet<T>(string key, Func<Task<T>> getter)
    {
        using var timer = CacheOperationDuration.NewTimer();
        
        try
        {
            var result = await getter();
            CacheHits.Inc();
            return result;
        }
        catch (KeyNotFoundException)
        {
            CacheMisses.Inc();
            throw;
        }
    }
}
```

## Security Best Practices

### Sensitive Data Handling
```csharp
// ✅ Use Secure cache for sensitive data
public class AuthTokenManager
{
    public async Task SaveToken(string token)
    {
        // Automatically encrypted
        await CacheDatabase.Secure.InsertObject("auth_token", token);
    }
    
    public async Task<string> GetToken()
    {
        try
        {
            return await CacheDatabase.Secure.GetObject<string>("auth_token");
        }
        catch (KeyNotFoundException)
        {
            return null; // Token not found or expired
        }
    }
    
    public async Task ClearToken()
    {
        await CacheDatabase.Secure.Invalidate("auth_token");
    }
}
```

### Key Security
```csharp
// ❌ Don't expose sensitive data in keys
await cache.InsertObject($"user_credit_card_{cardNumber}", data);

// ✅ Use hashed or obfuscated identifiers
var hashedId = ComputeHash(cardNumber);
await cache.InsertObject($"user_payment_method_{hashedId}", data);
```

## Performance Best Practices Summary

1. **Initialize once** and early in application lifecycle
2. **Use consistent key naming** conventions with hierarchy
3. **Handle errors gracefully** with fallback strategies
4. **Choose appropriate cache types** for your data characteristics
5. **Set reasonable expiration times** based on data freshness requirements
6. **Shutdown properly** to prevent data corruption
7. **Design for testability** with dependency injection and mocking
8. **Use serializable data objects** and plan for versioning
9. **Monitor cache health** and performance metrics
10. **Secure sensitive data** appropriately

## Next Steps

- [Review performance optimization](./performance.md)
- [Explore advanced patterns](./patterns/)
- [Check troubleshooting guide](./troubleshooting/troubleshooting-guide.md)
- [Learn platform-specific considerations](./platform-notes.md)