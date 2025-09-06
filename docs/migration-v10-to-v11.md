# Migration from V10.x to V11.1

This guide will help you migrate your existing Akavache V10.x application to the new V11.1 architecture.

## Breaking Changes

1. **Initialization Method**: The `BlobCache.ApplicationName` and `Registrations.Start()` methods are replaced with the builder pattern
2. **Package Structure**: Akavache is now split into multiple packages
3. **Serializer Registration**: Must explicitly register a serializer before use

## Migration Steps

### Step 1: Update Package References

**Remove V10.x packages:**
```xml
<!-- Remove these -->
<PackageReference Include="Akavache" Version="10.*" />
```

**Add V11.1 packages:**
```xml
<!-- Add these -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Step 2: Update Initialization Code

#### Old V10.x Code:
```csharp
// V10.x initialization
BlobCache.ApplicationName = "MyApp";
// or
Akavache.Registrations.Start("MyApp");

// Usage
var data = await BlobCache.UserAccount.GetObject<MyData>("key");
await BlobCache.LocalMachine.InsertObject("key", myData);
```

#### New V11.1 Code:
```csharp
// V11.1 initialization (RECOMMENDED: Explicit provider pattern)
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")        
           .WithSqliteProvider()    // REQUIRED: Explicit provider initialization
           .WithSqliteDefaults());

// Usage (same API)
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
await CacheDatabase.LocalMachine.InsertObject("key", myData);
```

### Step 3: Update Using Statements

**Old:**
```csharp
using Akavache;
```

**New:**
```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;
```

### Step 4: Update Cache Access

**Old:**
```csharp
BlobCache.UserAccount
BlobCache.LocalMachine
BlobCache.Secure
BlobCache.InMemory
```

**New:**
```csharp
CacheDatabase.UserAccount
CacheDatabase.LocalMachine
CacheDatabase.Secure
CacheDatabase.InMemory
```

## Migration Helper

Create this helper method to ease migration:

```csharp
public static class AkavacheMigration
{
    public static void InitializeV11(string appName)
    {
        // Initialize with SQLite (most common V10.x setup)
        // RECOMMENDED: Use explicit provider initialization
        CacheDatabase
            .Initialize<SystemJsonSerializer>(builder =>
                builder
                .WithSqliteProvider()    // Explicit provider initialization
                .WithSqliteDefaults(),
                appName);
    }
}

// Then in your app:
AkavacheMigration.InitializeV11("MyApp");
```

## Data Compatibility

### Existing Data
- **✅ V11.1 can read V10.x data** - Your existing cache files are compatible
- **✅ No data migration required** - Existing SQLite files work as-is
- **✅ Cross-serializer compatibility** - Can read data written with different serializers

### Cache File Locations
Cache files remain in the same locations as V10.x:
- **Windows**: `%LocalAppData%\YourApp\BlobCache`
- **macOS**: `~/Library/Caches/YourApp`
- **iOS**: `Library/Caches`
- **Android**: `{PackageInfo.ApplicationInfo.DataDir}/cache`

## Common Migration Scenarios

### Basic Desktop Application
```csharp
// V10.x
public void InitializeCache()
{
    BlobCache.ApplicationName = "MyDesktopApp";
}

// V11.1
public void InitializeCache()
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyDesktopApp")
                   .WithSqliteProvider()
                   .WithSqliteDefaults());
}
```

### Mobile Application with Encryption
```csharp
// V10.x
public void InitializeCache()
{
    BlobCache.ApplicationName = "MyMobileApp";
    // Encryption was configured separately
}

// V11.1
public void InitializeCache()
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyMobileApp")
                   .WithEncryptedSqliteProvider() // Built-in encryption
                   .WithSqliteDefaults());
}
```

### Dependency Injection Application
```csharp
// V10.x
public void ConfigureServices(IServiceCollection services)
{
    BlobCache.ApplicationName = "MyWebApp";
    services.AddSingleton<IBlobCache>(_ => BlobCache.LocalMachine);
}

// V11.1
public void ConfigureServices(IServiceCollection services)
{
    services.AddSplat(builder =>
        builder.WithAkavacheCacheDatabase<SystemJsonSerializer>(cacheBuilder =>
            cacheBuilder.WithApplicationName("MyWebApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults()));
}
```

## Testing Your Migration

### Unit Tests
```csharp
[Test]
public async Task Migration_Should_Read_V10_Data()
{
    // Arrange - Initialize V11.1 cache pointing to V10.x data
    var tempPath = Path.GetTempPath();
    CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
        builder.WithSqliteProvider()
               .WithSqliteDefaults()
               .WithCacheDirectory(tempPath),
        "TestApp");

    // Act - Try to read existing data
    var data = await CacheDatabase.UserAccount.GetObject<TestData>("existing_key");

    // Assert
    Assert.That(data, Is.Not.Null);
}
```

### Gradual Migration
You can run both versions side-by-side during migration:

```csharp
public class HybridCacheService
{
    private readonly IBlobCache _v10Cache;
    private readonly IBlobCache _v11Cache;

    public async Task<T> GetObject<T>(string key)
    {
        try
        {
            // Try V11.1 first
            return await _v11Cache.GetObject<T>(key);
        }
        catch (KeyNotFoundException)
        {
            // Fallback to V10.x and migrate data
            var data = await _v10Cache.GetObject<T>(key);
            await _v11Cache.InsertObject(key, data);
            return data;
        }
    }
}
```

## Troubleshooting Migration Issues

### "No serializer has been registered"
**Problem:** Forgot to specify serializer in initialization.

**Solution:**
```csharp
// Ensure you specify the serializer type
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder => // <-- Specify serializer here
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### "CacheDatabase has not been initialized"
**Problem:** Accessing cache before initialization or initialization failed.

**Solution:** 
1. Ensure initialization happens before any cache access
2. Check for exceptions during initialization
3. Verify all required packages are installed

### Data Compatibility Issues
**Problem:** Cannot read existing data after migration.

**Solutions:**
1. Use the same serializer as V10.x initially (Newtonsoft.Json)
2. Gradually migrate to System.Text.Json if needed
3. Check cache file permissions and locations

## Performance Considerations

### V11.1 Performance Benefits
- **Faster initialization** with builder pattern
- **Better memory usage** with modular packages
- **Improved serialization** with System.Text.Json
- **Enhanced async patterns** throughout

### Migration Performance
- **No data copying required** - V11.1 reads V10.x files directly
- **Gradual migration possible** - Can migrate usage patterns incrementally
- **Background optimization** - Consider rebuilding caches for optimal performance

## Next Steps

After completing migration:

1. **Test thoroughly** - Verify all cache operations work as expected
2. **Update documentation** - Update team documentation and README files
3. **Consider new features** - Explore V11.1-specific features like better DI support
4. **Optimize configuration** - Fine-tune cache settings for your use case
5. **Plan for V12** - Stay updated with future releases and migration paths

## Additional Resources

- [Configuration Guide](./configuration.md) - Learn about new configuration options
- [Serializers Guide](./serializers.md) - Choose the right serializer for your needs
- [Best Practices](./best-practices.md) - Updated patterns for V11.1
- [Troubleshooting Guide](./troubleshooting/troubleshooting-guide.md) - Common issues and solutions