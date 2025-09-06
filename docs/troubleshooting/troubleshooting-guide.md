# Troubleshooting Guide

This guide covers common issues, error messages, and solutions when working with Akavache.

## Common Issues

### 1. "No serializer has been registered"

**Problem:** Accessing cache before proper initialization or missing serializer configuration.

**Error Message:**
```
InvalidOperationException: No serializer has been registered for this cache instance
```

**Solution:**
```csharp
// ✅ Ensure serializer is specified during initialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>  // <-- Specify serializer here
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// ❌ Missing serializer specification
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase(builder =>  // <-- No serializer specified
        builder.WithApplicationName("MyApp"));
```

### 2. "CacheDatabase has not been initialized"

**Problem:** Trying to use cache before initialization or initialization failed silently.

**Error Message:**
```
InvalidOperationException: CacheDatabase has not been initialized. Call Initialize() first.
```

**Solution:**
```csharp
// ✅ Initialize before any cache access
public class Program
{
    public static void Main()
    {
        // Initialize first
        InitializeCache();
        
        // Then use cache
        var data = await CacheDatabase.UserAccount.GetObject<string>("key");
    }
    
    private static void InitializeCache()
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults());
    }
}
```

**Debugging initialization issues:**
```csharp
public static void InitializeWithErrorHandling()
{
    try
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults());
        
        Console.WriteLine("Cache initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cache initialization failed: {ex.Message}");
        throw; // Re-throw to prevent further issues
    }
}
```

### 3. Data compatibility issues

**Problem:** Cannot read data written with different serializer or Akavache version.

**Error Messages:**
```
JsonException: The JSON value could not be converted to...
SerializationException: Unable to deserialize object...
```

**Solutions:**

#### Cross-Serializer Compatibility
```csharp
// V11.1 can read data written by different serializers
// Migration from Newtonsoft.Json to System.Text.Json
public async Task<T> GetWithCompatibility<T>(string key)
{
    try
    {
        // Try current serializer first
        return await CacheDatabase.UserAccount.GetObject<T>(key);
    }
    catch (SerializationException)
    {
        // If it fails, the data might be from a different serializer
        // This is handled automatically in V11.1, but you can add explicit handling
        Logger.LogWarning("Serialization compatibility issue for key {Key}", key);
        
        // Delete corrupted entry and fetch fresh data
        await CacheDatabase.UserAccount.Invalidate(key);
        throw new KeyNotFoundException("Data format incompatible, removed from cache");
    }
}
```

#### Version Migration
```csharp
// Handle data structure changes between app versions
public class VersionedData
{
    public int Version { get; set; } = 1;
    public string Data { get; set; }
}

public async Task<VersionedData> GetVersionedData(string key)
{
    try
    {
        var data = await CacheDatabase.UserAccount.GetObject<VersionedData>(key);
        
        // Check if migration is needed
        if (data.Version < CurrentDataVersion)
        {
            data = MigrateData(data);
            await CacheDatabase.UserAccount.InsertObject(key, data);
        }
        
        return data;
    }
    catch (SerializationException)
    {
        // Old data format - clear and start fresh
        await CacheDatabase.UserAccount.Invalidate(key);
        throw new KeyNotFoundException("Data version incompatible");
    }
}
```

### 4. SQLite errors on mobile

**Problem:** Database corruption, locked files, or permission issues on mobile platforms.

**Common Error Messages:**
```
SQLiteException: database is locked
SQLiteException: disk I/O error
UnauthorizedAccessException: Access to the path is denied
```

**Solutions:**

#### iOS-Specific Issues
```csharp
// Handle iOS background app termination
public class iOSCacheManager
{
    public void ConfigureForIOS()
    {
        // Use shorter timeouts for mobile
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyiOSApp")
                       .WithSqliteProvider()
                       .WithConnectionTimeout(TimeSpan.FromSeconds(5))  // Shorter timeout
                       .WithSqliteDefaults());
    }
    
    // Handle app backgrounding
    public async Task OnAppBackgrounding()
    {
        try
        {
            await CacheDatabase.Flush();  // Ensure data is written
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to flush cache on backgrounding");
        }
    }
}
```

#### Android-Specific Issues
```csharp
// Handle Android app lifecycle and low storage
public class AndroidCacheManager
{
    public async Task HandleLowStorage()
    {
        try
        {
            // Clear expired entries to free space
            await CacheDatabase.LocalMachine.Vacuum();
            await CacheDatabase.UserAccount.Vacuum();
            
            // Clear non-essential caches
            await CacheDatabase.InMemory.InvalidateAll();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to handle low storage");
        }
    }
    
    public async Task OnAppPause()
    {
        try
        {
            // Ensure database is properly closed
            await CacheDatabase.Flush();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to flush cache on pause");
        }
    }
}
```

#### Universal Mobile Solutions
```csharp
// Robust mobile cache operations
public class MobileCacheService
{
    public async Task<T> SafeGetObject<T>(string key, int retryCount = 3)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                return await CacheDatabase.UserAccount.GetObject<T>(key);
            }
            catch (SQLiteException ex) when (ex.Message.Contains("database is locked"))
            {
                if (i == retryCount - 1) throw;
                
                // Wait and retry
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (i + 1)));
            }
        }
        
        throw new InvalidOperationException("Failed after retries");
    }
}
```

### 5. Linker removing types (IL2104)

**Problem:** Code trimming/linking removes required types, causing runtime errors.

**Error Messages:**
```
MissingMethodException: Method not found
TypeLoadException: Could not load type
IL2104: Assembly 'Akavache' contains a call to 'Type.GetType(String)'
```

**Solutions:**

#### Preserve Akavache Types
```xml
<!-- In your project file -->
<ItemGroup>
  <TrimmerRootAssembly Include="Akavache" />
  <TrimmerRootAssembly Include="Akavache.Core" />
  <TrimmerRootAssembly Include="Akavache.Sqlite3" />
  <TrimmerRootAssembly Include="Akavache.SystemTextJson" />
</ItemGroup>
```

#### Linker Configuration
```xml
<!-- LinkerConfig.xml -->
<linker>
  <assembly fullname="Akavache">
    <type fullname="*" />
  </assembly>
  <assembly fullname="Akavache.Core">
    <type fullname="*" />
  </assembly>
  <assembly fullname="YourApp.Models">
    <type fullname="*" />
  </assembly>
</linker>
```

#### Preserve Your Model Types
```csharp
// Add preservation attributes to your cached types
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(AppSettings))]
public partial class CacheSerializationContext : JsonSerializerContext
{
}

// Use with System.Text.Json
var options = new JsonSerializerOptions
{
    TypeInfoResolver = CacheSerializationContext.Default
};
```

## Platform-Specific Issues

### iOS Linker Issues

**Problem:** iOS linker strips required types and methods.

**Solutions:**

#### Linker Behavior Configuration
```xml
<!-- In iOS project -->
<PropertyGroup>
  <MtouchLink>SdkOnly</MtouchLink>  <!-- Less aggressive linking -->
  <MtouchUseLlvm>true</MtouchUseLlvm>
</PropertyGroup>
```

#### Preserve Cache Models
```csharp
// iOS specific preservation
public class LinkerPreserve
{
    public static void Preserve()
    {
        // Force preservation of your cached types
        var dummy = new UserProfile();
        var dummy2 = new AppSettings();
        
        // This code never runs but prevents linker removal
        if (DateTime.Now.Year == 1900)
        {
            _ = JsonSerializer.Serialize(dummy);
            _ = JsonSerializer.Serialize(dummy2);
        }
    }
}
```

### 6. Provider not found errors

**Problem:** Missing provider initialization or package references.

**Error Messages:**
```
InvalidOperationException: SQLite provider not found
NotSupportedException: Provider not initialized
```

**Solutions:**

#### Explicit Provider Initialization
```csharp
// ✅ Always use explicit provider initialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()        // REQUIRED: Explicit provider
               .WithSqliteDefaults());

// ❌ Avoid relying on automatic provider detection
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteDefaults());      // May fail if provider not auto-detected
```

#### Package Reference Verification
```xml
<!-- Ensure all required packages are referenced -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />

<!-- For encrypted cache -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
```

### 7. GetOrFetchObject returns stale data after Invalidate (Fixed in V11.1.1+)

**Problem:** Prior to V11.1.1, calling `Invalidate()` on InMemory cache didn't properly clear the RequestCache, causing subsequent `GetOrFetchObject` calls to return stale data instead of fetching fresh data.

**Error Behavior:**
```csharp
// In versions before V11.1.1, this could return stale data
await CacheDatabase.InMemory.InsertObject("key", "old_value");
await CacheDatabase.InMemory.Invalidate("key");

// This might return "old_value" instead of fetching fresh data
var result = await CacheDatabase.InMemory.GetOrFetchObject("key", 
    () => Task.FromResult("fresh_value"));
```

**Fixed in V11.1.1+:**
The issue has been resolved in V11.1.1 and later versions. `Invalidate()` now properly clears all related cache entries including the RequestCache.

**Workaround for older versions:**
```csharp
// For versions before V11.1.1, use this pattern
public async Task<T> SafeGetOrFetch<T>(string key, Func<Task<T>> fetchFunc)
{
    try
    {
        // Try to get existing data
        return await CacheDatabase.InMemory.GetObject<T>(key);
    }
    catch (KeyNotFoundException)
    {
        // Fetch fresh data
        var freshData = await fetchFunc();
        await CacheDatabase.InMemory.InsertObject(key, freshData);
        return freshData;
    }
}
```

**Verification Test:**
```csharp
[Test]
public async Task Invalidate_Should_Clear_RequestCache()
{
    // Insert initial data
    await CacheDatabase.InMemory.InsertObject("test_key", "old_value");
    
    // Invalidate the key
    await CacheDatabase.InMemory.Invalidate("test_key");
    
    // This should fetch fresh data, not return stale data
    var result = await CacheDatabase.InMemory.GetOrFetchObject("test_key", 
        () => Task.FromResult("fresh_value"));
    
    Assert.AreEqual("fresh_value", result);
}
```

## UWP x64 Issues

**Problem:** UWP applications targeting "Any CPU" fail with SQLite errors.

**Error Messages:**
```
BadImageFormatException: Could not load file or assembly 'sqlite3'
DllNotFoundException: Unable to load DLL 'sqlite3'
```

**Solution:**
Ensure your UWP project targets a specific platform (`x86`, `x64`, `ARM`) rather than "Any CPU":

```xml
<PropertyGroup>
  <Platform>x64</Platform>  <!-- Specific platform, not AnyCPU -->
</PropertyGroup>
```

## Debugging Techniques

### 1. Enable Detailed Logging

```csharp
public class CacheLogger
{
    public static void ConfigureLogging()
    {
        // Enable detailed Akavache logging
        Splat.Locator.CurrentMutable.RegisterConstant(new ConsoleLogger(), typeof(ILogger));
    }
}

public class ConsoleLogger : ILogger
{
    public LogLevel Level { get; set; } = LogLevel.Debug;
    
    public void Write(string message, LogLevel logLevel)
    {
        if (logLevel >= Level)
        {
            Console.WriteLine($"[{logLevel}] {message}");
        }
    }
}
```

### 2. Cache State Inspection

```csharp
public class CacheDebugger
{
    public static async Task InspectCache(IBlobCache cache, string name)
    {
        try
        {
            var keys = await cache.GetAllKeys().FirstOrDefaultAsync();
            Console.WriteLine($"{name} Cache - Total keys: {keys.Count()}");
            
            foreach (var key in keys.Take(10)) // Show first 10 keys
            {
                Console.WriteLine($"  Key: {key}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{name} Cache inspection failed: {ex.Message}");
        }
    }
    
    public static async Task DiagnoseAllCaches()
    {
        await InspectCache(CacheDatabase.UserAccount, "UserAccount");
        await InspectCache(CacheDatabase.LocalMachine, "LocalMachine");
        await InspectCache(CacheDatabase.Secure, "Secure");
        await InspectCache(CacheDatabase.InMemory, "InMemory");
    }
}
```

### 3. Performance Monitoring

```csharp
public class CachePerformanceMonitor
{
    public static async Task<T> MeasureOperation<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            stopwatch.Stop();
            Console.WriteLine($"{operationName}: {stopwatch.ElapsedMilliseconds}ms (Success)");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"{operationName}: {stopwatch.ElapsedMilliseconds}ms (Failed: {ex.GetType().Name})");
            throw;
        }
    }
}

// Usage
var data = await CachePerformanceMonitor.MeasureOperation("Get User Profile",
    () => CacheDatabase.UserAccount.GetObject<UserProfile>("user_123"));
```

## Getting Help

### 1. Check Existing Resources
- [Installation Guide](./installation.md) - Package and setup issues
- [Configuration Guide](./configuration.md) - Initialization problems  
- [Platform Notes](./platform-notes.md) - Platform-specific issues
- [Performance Guide](./performance.md) - Performance optimization

### 2. Gather Information
When reporting issues, include:
- Akavache version
- Target platform and .NET version
- Complete error message and stack trace
- Minimal reproducible code sample
- Package references and configuration

### 3. Common Debugging Steps
1. **Verify initialization** - Ensure cache is properly initialized before use
2. **Check package references** - All required packages are installed
3. **Test with InMemory cache** - Isolate if issue is storage-related
4. **Enable logging** - Get detailed error information
5. **Verify serializable types** - Ensure cached objects can be serialized
6. **Check platform requirements** - Review platform-specific considerations

## Related Resources

- [Cache Deletion Patterns](../patterns/cache-deletion.md) - Safe cache deletion techniques
- [GetAllKeysSafe Usage](./issue-313-cache-deletion-fix.md) - Exception-safe key enumeration
- [Cache Invalidation Patterns](../../src/Samples/CacheInvalidationPatterns.cs) - Advanced invalidation techniques
- [Best Practices](../best-practices.md) - Recommended usage patterns