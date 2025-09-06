# Configuration Guide

Akavache V11.1 uses a flexible builder pattern for initialization and configuration. This guide shows you how to set up Akavache for different scenarios.

## Builder Pattern Overview

The new builder pattern provides a fluent API for configuring cache instances:

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

## Provider Initialization Pattern

**Important:** Always explicitly initialize providers before using defaults:

```csharp
// ✅ CORRECT - Explicit provider initialization
builder.WithApplicationName("MyApp")
       .WithSqliteProvider()      // Initialize provider first
       .WithSqliteDefaults();     // Then apply defaults

// ⚠️ DEPRECATED - While this works, it's not recommended
builder.WithApplicationName("MyApp")
       .WithSqliteDefaults();     // This will auto-call WithSqliteProvider()
```

The explicit pattern is recommended for:
- Forward compatibility with other DI containers
- Clearer code intent and better maintainability
- Avoiding deprecated automatic behaviors

## Configuration Options

### 1. In-Memory Only (for testing or non-persistent applications)

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyTestApp")
               .WithInMemoryProvider()
               .WithInMemoryDefaults());
```

**Use when:**
- Unit testing
- Temporary caching without persistence
- Development environments

### 2. SQLite Persistence (Recommended)

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

**Use when:**
- Most production applications
- Need persistent data across app restarts
- Want automatic cleanup of expired entries

### 3. Encrypted SQLite

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MySecureApp")
               .WithEncryptedSqliteProvider()
               .WithSqliteDefaults());
```

**Use when:**
- Storing sensitive data
- Compliance requirements
- User credentials or personal information

### 4. Custom Cache Instances

```csharp
// Advanced: Custom cache directory and configuration
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithCacheDirectory("/custom/path")
               .WithConnectionPooling(true)
               .WithVacuumOnStartup(true));
```

#### Manual Instance Creation (advanced scenarios)

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;

var akavacheInstance = CacheDatabase.CreateBuilder()
   .WithSerializer<SystemJsonSerializer>()
   .WithApplicationName("MyApp")
   .WithSqliteProvider()    // REQUIRED: Explicit provider initialization
   .WithSqliteDefaults()  
   .Build();

// Use akavacheInstance.UserAccount, akavacheInstance.LocalMachine, etc.
```

### **Application Name Configuration Order**

**🔧 Important Fix in V11.1.1+**: Prior versions computed the settings cache path in the constructor before WithApplicationName() could be called, causing the settings cache to always use the default "Akavache" directory regardless of the custom application name. In V11.1.1+, the settings cache path is now computed lazily when first accessed, ensuring it respects the custom application name set via WithApplicationName().

**Best Practice:**

```csharp
// ✅ FIXED in V11.1.1+: Settings cache will correctly use "MyCustomApp" directory
var akavacheInstance = CacheDatabase.CreateBuilder()
   .WithSerializer<SystemJsonSerializer>()
   .WithApplicationName("MyCustomApp")    // Settings cache respects this name
   .WithSqliteProvider()
   .WithSqliteDefaults()
   .Build();

// The SettingsCachePath will now correctly be based on "MyCustomApp" instead of "Akavache"
```

This fix ensures that mobile platforms (especially iOS) get the correct isolated storage directories based on the actual application name rather than the default value.

### 5. DateTime Handling

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults()
               .WithDateTimeKind(DateTimeKind.Utc)); // Ensure UTC timestamps
```

## Dependency Injection Patterns

### Static Initialization (Simple Applications)

```csharp
public class Program
{
    public static void Main()
    {
        // Initialize once at startup
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults());
        
        // Use throughout the application
        var cache = CacheDatabase.UserAccount;
    }
}
```

### Dependency Injection Registration (Web Applications)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register Akavache with the DI container
    services.AddSplat(builder =>
        builder.WithAkavacheCacheDatabase<SystemJsonSerializer>(cacheBuilder =>
            cacheBuilder.WithApplicationName("MyWebApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults()));
    
    // Register your services that depend on Akavache
    services.AddScoped<IDataService, CachedDataService>();
}
```

### Manual Instance Creation (Advanced)

```csharp
public class CustomCacheManager
{
    private readonly IBlobCache _primaryCache;
    private readonly IBlobCache _secondaryCache;
    
    public CustomCacheManager()
    {
        // Create specific cache instances for different purposes
        _primaryCache = new SqliteBlobCache(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "primary.db"),
            new SystemJsonSerializer()
        );
        
        _secondaryCache = new InMemoryBlobCache(new SystemJsonSerializer());
    }
}
```

## Advanced Configuration Options

### Connection Pooling
```csharp
builder.WithSqliteProvider()
       .WithConnectionPooling(true)
       .WithMaxPoolSize(10);
```

### Custom Cache Directory
```csharp
builder.WithSqliteProvider()
       .WithCacheDirectory("/custom/cache/path")
       .WithSqliteDefaults();
```

### Vacuum and Maintenance
```csharp
builder.WithSqliteProvider()
       .WithVacuumOnStartup(true)
       .WithAutoVacuum(true)
       .WithSqliteDefaults();
```

### Custom SQLite Flags
```csharp
builder.WithSqliteProvider()
       .WithSqliteOpenFlags(SQLiteOpenFlags.ReadWriteCreate | SQLiteOpenFlags.FullMutex)
       .WithSqliteDefaults();
```

## Serializer Configuration

### System.Text.Json Configuration
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// Custom JsonSerializerOptions
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

var serializer = new SystemJsonSerializer(options);
```

### Newtonsoft.Json Configuration
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// Custom JsonSerializerSettings
var settings = new JsonSerializerSettings
{
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    NullValueHandling = NullValueHandling.Ignore
};

var serializer = new NewtonsoftJsonSerializer(settings);
```

## Platform-Specific Configuration

### Mobile Applications (iOS/Android)
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyMobileApp")
               .WithSqliteProvider()
               .WithMobileDefaults()); // Mobile-optimized settings
```

### Desktop Applications
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyDesktopApp")
               .WithSqliteProvider()
               .WithDesktopDefaults()); // Desktop-optimized settings
```

### Web Applications
```csharp
// In ConfigureServices
services.AddSplat(builder =>
    builder.WithAkavacheCacheDatabase<SystemJsonSerializer>(cacheBuilder =>
        cacheBuilder.WithApplicationName("MyWebApp")
                   .WithSqliteProvider()
                   .WithWebDefaults())); // Web-optimized settings
```

## Dependency Injection Pattern

Akavache V11.1 supports dependency injection (DI) patterns through the `.WithAkavache<T>` extension method, which provides a non-static `IAkavacheInstance` that can be registered with your DI container.

### Basic Dependency Injection Setup

```csharp
using Akavache;
using Akavache.SystemTextJson;
using Splat.Builder;

// Create a non-static Akavache instance for DI
var akavacheInstance = AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        applicationName: "MyApp",
        configure: builder => 
        {
            builder.WithSqliteProvider()
                   .WithSqliteDefaults();
        },
        instance: akavacheInstance =>
        {
            // Configure the instance
            akavacheInstance.ForcedDateTimeKind = DateTimeKind.Utc;
        });
```

### DI Container Registration

```csharp
// ASP.NET Core / Microsoft.Extensions.DependencyInjection
services.AddSingleton<IAkavacheInstance>(serviceProvider =>
{
    IAkavacheInstance? instance = null;
    
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>(
            "MyWebApp",
            builder => builder.WithSqliteProvider().WithSqliteDefaults(),
            akavacheInstance => instance = akavacheInstance);
    
    return instance!;
});

// Autofac
builder.Register(c =>
{
    IAkavacheInstance? instance = null;
    
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>(
            "MyApp",
            configBuilder => configBuilder.WithSqliteProvider().WithSqliteDefaults(),
            akavacheInstance => instance = akavacheInstance);
    
    return instance!;
}).As<IAkavacheInstance>().SingleInstance();
```

### Using IAkavacheInstance in Services

```csharp
public class MyService
{
    private readonly IAkavacheInstance _cache;
    
    public MyService(IAkavacheInstance cache)
    {
        _cache = cache;
    }
    
    public async Task<T> GetCachedDataAsync<T>(string key)
    {
        // Access cache instances through the injected instance
        return await _cache.UserAccount.GetObject<T>(key);
    }
    
    public async Task SetCachedDataAsync<T>(string key, T value)
    {
        await _cache.UserAccount.InsertObject(key, value);
    }
}
```

### IAkavacheInstance Properties

The `IAkavacheInstance` provides access to all standard cache types and configuration:

```csharp
public interface IAkavacheInstance
{
    // Cache instances
    IBlobCache? InMemory { get; }           // Temporary cache
    IBlobCache? LocalMachine { get; }       // Machine-wide persistent cache  
    ISecureBlobCache? Secure { get; }       // Encrypted cache
    IBlobCache? UserAccount { get; }        // User-specific persistent cache
    
    // Configuration
    ISerializer? Serializer { get; }        // Configured serializer
    string ApplicationName { get; }         // Application identifier
    DateTimeKind? ForcedDateTimeKind { get; set; } // DateTime handling
    
    // Application info
    string? ApplicationRootPath { get; }
    string? ExecutingAssemblyName { get; }
    Version? Version { get; }
    
    // Settings (for Akavache.Settings)
    string? SettingsCachePath { get; }
}
```

### Advanced DI Patterns

```csharp
// Multiple cache instances for different purposes
services.AddSingleton<IAkavacheInstance>(sp => CreateCacheInstance("MainApp"));
services.AddSingleton<IAkavacheInstance>("Analytics", sp => CreateCacheInstance("Analytics"));
services.AddSingleton<IAkavacheInstance>("UserData", sp => CreateCacheInstance("UserData"));

// Factory pattern for cache instances
services.AddSingleton<ICacheFactory, CacheFactory>();

public class CacheFactory : ICacheFactory
{
    public IAkavacheInstance CreateCache(string applicationName)
    {
        IAkavacheInstance? instance = null;
        
        AppBuilder.CreateSplatBuilder()
            .WithAkavache<SystemJsonSerializer>(
                applicationName,
                builder => builder.WithSqliteProvider().WithSqliteDefaults(),
                akavacheInstance => instance = akavacheInstance);
        
        return instance!;
    }
}
```

## Environment-Specific Configuration

### Development Environment
```csharp
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    builder.WithApplicationName("MyApp-Dev")
           .WithInMemoryProvider()      // Faster for development
           .WithInMemoryDefaults();
}
else
{
    builder.WithApplicationName("MyApp")
           .WithSqliteProvider()
           .WithSqliteDefaults();
}
```

### Testing Configuration
```csharp
// Use a separate test database
builder.WithApplicationName("MyApp-Test")
       .WithSqliteProvider()
       .WithCacheDirectory(Path.GetTempPath())
       .WithSqliteDefaults();
```

### Production Configuration
```csharp
builder.WithApplicationName("MyApp")
       .WithEncryptedSqliteProvider()   // Security for production
       .WithConnectionPooling(true)     // Performance optimization
       .WithVacuumOnStartup(true)      // Maintenance
       .WithSqliteDefaults();
```

## Configuration Validation

### Startup Validation
```csharp
public void ValidateCacheConfiguration()
{
    try
    {
        // Test cache accessibility
        var testData = await CacheDatabase.UserAccount.GetObject<string>("test-key");
        Console.WriteLine("Cache configuration is valid");
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Cache configuration failed", ex);
    }
}
```

### Health Checks
```csharp
public class CacheHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await CacheDatabase.UserAccount.GetAllKeys().FirstOrDefaultAsync();
            return HealthCheckResult.Healthy("Cache is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache is not accessible", ex);
        }
    }
}
```

## Best Practices

1. **Initialize early** - Set up Akavache before using any cache operations
2. **Use explicit providers** - Always call `WithSqliteProvider()` before `WithSqliteDefaults()`
3. **One initialization per app** - Don't re-initialize unless absolutely necessary
4. **Environment-specific config** - Use different settings for dev/test/prod
5. **Validate configuration** - Test cache accessibility during startup
6. **Handle initialization errors** - Gracefully handle configuration failures

## Next Steps

- [Learn about serializers](./serializers.md)
- [Understand cache types](./cache-types.md)
- [Master basic operations](./basic-operations.md)
- [Review platform-specific notes](./platform-notes.md)
