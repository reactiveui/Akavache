# Migration from V10.x to V11.1

This guide helps you migrate from Akavache V10.x to V11.1 while preserving your existing data and functionality.

## Overview of Changes

Akavache V11.1 introduces significant architectural improvements while maintaining backward compatibility with your data. The main changes are in how you initialize and configure Akavache, not in the core API you use daily.

### Breaking Changes

1. **Initialization Method**: The `BlobCache.ApplicationName` and `Registrations.Start()` methods are replaced with the builder pattern
2. **Package Structure**: Akavache is now split into multiple packages
3. **Serializer Registration**: Must explicitly register a serializer before use

### What Stays the Same

- ✅ **Core API**: `GetObject`, `InsertObject`, `GetOrFetchObject` work identically
- ✅ **Data Compatibility**: V11.1 reads all V10.x data without conversion
- ✅ **Cache Types**: UserAccount, LocalMachine, Secure, InMemory work the same
- ✅ **Extension Methods**: All your existing extension method calls work

## Migration Steps

### Step 1: Update Package References

#### Old V10.x packages:
```xml
<PackageReference Include="Akavache" Version="10.*" />
```

#### New V11.1 packages:
```xml
<!-- Choose storage backend -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />

<!-- Choose serializer -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<!-- OR for maximum compatibility: -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
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

### Step 3: Migration Helper

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

## Detailed Migration Scenarios

### Scenario 1: Basic V10.x App

**Before (V10.x):**
```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        BlobCache.ApplicationName = "MyWpfApp";
        base.OnStartup(e);
    }
}

public class DataService
{
    public async Task<User> GetUser(int userId)
    {
        return await BlobCache.UserAccount.GetOrFetchObject($"user_{userId}",
            () => apiClient.GetUser(userId),
            TimeSpan.FromMinutes(30));
    }
}
```

**After (V11.1):**
```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyWpfApp")
                       .WithSqliteProvider()
                       .WithSqliteDefaults());
        base.OnStartup(e);
    }
}

public class DataService
{
    public async Task<User> GetUser(int userId)
    {
        // Same API!
        return await CacheDatabase.UserAccount.GetOrFetchObject($"user_{userId}",
            () => apiClient.GetUser(userId),
            TimeSpan.FromMinutes(30));
    }
}
```

### Scenario 2: Encrypted Cache

**Before (V10.x):**
```csharp
BlobCache.ApplicationName = "MySecureApp";
BlobCache.SecureFileStorage = new EncryptedBlobStorage("password");
```

**After (V11.1):**
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MySecureApp")
               .WithEncryptedSqliteProvider()
               .WithSqliteDefaults("password"));
```

### Scenario 3: Custom Storage Locations

**Before (V10.x):**
```csharp
BlobCache.ApplicationName = "MyApp";
BlobCache.LocalMachine = new SqliteBlobCache(@"C:\MyApp\cache.db");
```

**After (V11.1):**
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithLocalMachine(new SqliteBlobCache(@"C:\MyApp\cache.db"))
               .WithSqliteDefaults());
```

### Scenario 4: Dependency Injection

**Before (V10.x):**
```csharp
// V10.x didn't have good DI support
BlobCache.ApplicationName = "MyApp";
container.RegisterInstance(BlobCache.UserAccount);
```

**After (V11.1):**
```csharp
// V11.1 has first-class DI support
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        "MyApp",
        builder => builder.WithSqliteProvider().WithSqliteDefaults(),
        (splat, instance) => container.RegisterInstance(instance));
```

## Serializer Migration

### For Maximum Compatibility (Recommended for Migration)

Use Newtonsoft.Json serializer to maintain 100% compatibility:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### For Best Performance (Recommended for New Code)

Use System.Text.Json for better performance:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

## Data Migration

### Cross-Serializer Compatibility

V11.1 can read data written by V10.x and different serializers:

```csharp
// This works! V11.1 can read V10.x data regardless of serializer choice
var oldData = await CacheDatabase.UserAccount.GetObject<MyData>("key_from_v10");
```

### Gradual Migration

You can migrate serializers gradually:

```csharp
// 1. Start with Newtonsoft for compatibility
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(/* ... */);

// 2. Later, change to System.Text.Json for performance
// Old data will still be readable!
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(/* ... */);
```

## Platform-Specific Migration

### .NET MAUI Migration

**Before (V10.x):**
```csharp
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        BlobCache.ApplicationName = "MyMauiApp";
        MainPage = new AppShell();
    }
}
```

**After (V11.1):**
```csharp
// In MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Initialize Akavache
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(cacheBuilder =>
                cacheBuilder.WithApplicationName("MyMauiApp")
                        .WithSqliteProvider()
                        .WithSqliteDefaults());

        return builder.Build();
    }
}
```

### Mobile Platform Packages

Add platform-specific SQLite support:

```xml
<!-- For iOS/Android -->
<PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.11" />
```

## Testing Your Migration

### 1. Verify Data Access

```csharp
// Test that old data is still accessible
try
{
    var oldData = await CacheDatabase.UserAccount.GetObject<MyOldDataType>("existing_key");
    Console.WriteLine("✅ Migration successful - old data accessible");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Migration issue: {ex.Message}");
}
```

### 2. Test New Data Storage

```csharp
// Test that new data can be stored and retrieved
var testData = new MyDataType { Value = "test" };
await CacheDatabase.UserAccount.InsertObject("migration_test", testData);
var retrieved = await CacheDatabase.UserAccount.GetObject<MyDataType>("migration_test");
Console.WriteLine(retrieved.Value == "test" ? "✅ New storage working" : "❌ Storage issue");
```

### 3. Performance Comparison

```csharp
// Compare V10 vs V11 performance
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    await CacheDatabase.UserAccount.InsertObject($"perf_test_{i}", new MyData { Value = i });
}
stopwatch.Stop();
Console.WriteLine($"1000 inserts: {stopwatch.ElapsedMilliseconds}ms");
```

## Troubleshooting Migration Issues

### Common Issues

#### 1. "No serializer has been registered"
```csharp
// Fix: Ensure you register a serializer
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(/* ... */);
```

#### 2. "Provider not found"
```csharp
// Fix: Call WithSqliteProvider() before WithSqliteDefaults()
.WithSqliteProvider()
.WithSqliteDefaults()
```

#### 3. Data not found after migration
```csharp
// Check if application name changed
builder.WithApplicationName("SameAsV10App") // Must match V10 app name
```

### Migration Checklist

- [ ] Updated package references
- [ ] Replaced initialization code
- [ ] Added explicit provider initialization
- [ ] Tested existing data access
- [ ] Verified new data storage
- [ ] Updated DI container registration (if applicable)
- [ ] Added platform-specific packages (mobile)
- [ ] Updated application shutdown code

## Performance Considerations

V11.1 generally performs better than V10.x, especially with System.Text.Json:

- **System.Text.Json**: Faster than V10 across all scenarios
- **Newtonsoft.Json**: Comparable to V10 for small/medium data, may be slower for very large datasets

For best migration experience:
1. Start with Newtonsoft.Json for compatibility
2. Migrate to System.Text.Json when ready for optimal performance

## Getting Help

If you encounter issues during migration:

1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Review the [Configuration Documentation](configuration.md)
3. Ask on [ReactiveUI Slack](https://reactiveui.net/slack)
4. File an issue on [GitHub](https://github.com/reactiveui/Akavache/issues)

Remember: V11.1 is designed to be a drop-in replacement for V10.x with better performance and more features. Your existing data and most of your code will work without changes!