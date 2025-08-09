[![NuGet Stats](https://img.shields.io/nuget/v/akavache.sqlite3.svg)](https://www.nuget.org/packages/akavache.sqlite3) ![Build](https://github.com/reactiveui/Akavache/workflows/Build/badge.svg) [![Code Coverage](https://codecov.io/gh/reactiveui/akavache/branch/main/graph/badge.svg)](https://codecov.io/gh/reactiveui/akavache)
<br>
<a href="https://www.nuget.org/packages/akavache.sqlite3">
        <img src="https://img.shields.io/nuget/dt/akavache.sqlite3.svg">
</a>
<a href="#backers">
        <img src="https://opencollective.com/reactiveui/backers/badge.svg">
</a>
<a href="#sponsors">
        <img src="https://opencollective.com/reactiveui/sponsors/badge.svg">
</a>
<a href="https://reactiveui.net/slack">
        <img src="https://img.shields.io/badge/chat-slack-blue.svg">
</a>

<img alt="Akavache" src="https://raw.githubusercontent.com/reactiveui/styleguide/master/logo_akavache/main.png" width="150" />

# Akavache V11.0: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e., writes to disk) key-value store created for writing desktop and mobile applications in C#, based on SQLite3. Akavache is great for both storing important data (i.e., user settings) as well as cached local data that expires.

## What's New in V11.0

Akavache V11.0 introduces a new **Builder Pattern** for initialization, improved serialization support, and enhanced cross-serializer compatibility:

- 🏗️ **Builder Pattern**: New fluent API for configuring cache instances
- 🔄 **Multiple Serializer Support**: Choose between System.Text.Json, Newtonsoft.Json, each with a BSON variant
- 🔗 **Cross-Serializer Compatibility**: Read data written by different serializers
- 🧩 **Modular Design**: Install only the packages you need
- 📱 **Enhanced .NET MAUI Support**: First-class support for modern cross-platform development
- 🔒 **Improved Security**: Better encrypted cache implementation

## Table of Contents

- [Quick Start](#quick-start)
- [Installation](#installation)
- [Migration from V10.x](#migration-from-v10x)
- [Configuration](#configuration)
- [Serializers](#serializers)
- [Cache Types](#cache-types)
- [Basic Operations](#basic-operations)
- [Extension Methods](#extension-methods)
- [Advanced Features](#advanced-features)
- [Platform-Specific Notes](#platform-specific-notes)
- [Performance](#performance)
- [Best Practices](#best-practices)

## Quick Start

### 1. Install Packages

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.0.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.0.*" />
```

### 2. Initialize Akavache

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;

// Initialize with the builder pattern
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer())
           .WithSqliteDefaults());
```

### 3. Use the Cache

```csharp
// Store an object
var user = new User { Name = "John", Email = "john@example.com" };
await CacheDatabase.UserAccount.InsertObject("current_user", user);

// Retrieve an object
var cachedUser = await CacheDatabase.UserAccount.GetObject<User>("current_user");

// Store with expiration
await CacheDatabase.LocalMachine.InsertObject("temp_data", someData, DateTimeOffset.Now.AddHours(1));

// Get or fetch pattern
var data = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data", 
    async () => await httpClient.GetFromJsonAsync<ApiResponse>("https://api.example.com/data"));
```

## Installation

Akavache V11.0 uses a modular package structure. Choose the packages that match your needs:

### Core Package (Required)
```xml
<PackageReference Include="Akavache.Core" Version="11.0.1" />
```

### Storage Backends
```xml
<!-- SQLite persistence (recommended) -->
<PackageReference Include="Akavache.Sqlite3" Version="11.0.1" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.0.1" />
```

### Serializers (Choose One)
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.0.1" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.0.1" />
```

### Optional Extensions
```xml
<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.0.1" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.0.1" />
```

## Migration from V10.x

### Breaking Changes

1. **Initialization Method**: The `BlobCache.ApplicationName` and `Registrations.Start()` methods are replaced with the builder pattern
2. **Package Structure**: Akavache is now split into multiple packages
3. **Serializer Registration**: Must explicitly register a serializer before use

### Migration Steps

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

#### New V11.0 Code:
```csharp
// V11.0 initialization

CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer()) // Required!
           .WithSqliteDefaults());

// Usage (same API)
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
await CacheDatabase.LocalMachine.InsertObject("key", myData);
```

### Migration Helper

Create this helper method to ease migration:

```csharp
public static class AkavacheMigration
{
    public static void InitializeV11(string appName)
    {
        // Initialize with SQLite (most common V10.x setup)
        CacheDatabase.Initialize(builder =>
            builder.WithApplicationName(appName)
                   .WithSerializer(new SystemJsonSerializer()) // Choose your preferred serializer, Required!
                   .WithSqliteDefaults());
    }
}

// Then in your app:
AkavacheMigration.InitializeV11("MyApp");
```

## Configuration

### Builder Pattern

Akavache V11.0 uses a fluent builder pattern for configuration:

```csharp
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")           // Required
           .WithSerializer(new SystemJsonSerializer()) // Custom serializer
           .WithSqliteDefaults());                   // SQLite persistence
```

### Configuration Options

#### 1. In-Memory Only (for testing)
```csharp
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("TestApp")
           .WithSerializer(new SystemJsonSerializer()) // Custom serializer
           .WithInMemoryDefaults());
```

#### 2. SQLite Persistence
```csharp
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer()) // Custom serializer
           .WithSqliteDefaults());
```

#### 3. Encrypted SQLite
```csharp
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer()) // Custom serializer
           .WithSqliteDefaults("mySecretPassword"));
```

#### 4. Custom Cache Instances
```csharp
CacheDatabase.Initialize(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer()) // Custom serializer
           .WithUserAccount(new SqliteBlobCache("custom-user.db"))
           .WithLocalMachine(new SqliteBlobCache("custom-local.db"))
           .WithSecure(new EncryptedSqliteBlobCache("secure.db", "password"))
           .WithInMemory(new InMemoryBlobCache()));
```

#### 5. DateTime Handling
```csharp
// Set global DateTime behavior
CacheDatabase.ForcedDateTimeKind = DateTimeKind.Utc;

CacheDatabase.Serializer = new SystemJsonSerializer();
CacheDatabase.Initialize(builder => builder.WithApplicationName("MyApp").WithSqliteDefaults());
```

## Serializers

Akavache V11.0 supports multiple serialization formats with automatic cross-compatibility.

### System.Text.Json (Recommended)

**Best for**: New applications, performance-critical scenarios, .NET native support

```csharp
CacheDatabase.Serializer = new SystemJsonSerializer();
```

**Features:**
- ✅ Fastest performance
- ✅ Native .NET support
- ✅ Smallest memory footprint
- ✅ BSON compatibility mode available
- ❌ Limited customization options

**Configuration:**
```csharp
var serializer = new SystemJsonSerializer()
{
    UseBsonFormat = false, // true for max compatibility with old data
    Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    }
};
CacheDatabase.Serializer = serializer;
```

### Newtonsoft.Json (Maximum Compatibility)

**Best for**: Migrating from older Akavache versions, complex serialization needs

```csharp
CacheDatabase.Serializer = new NewtonsoftSerializer();
```

**Features:**
- ✅ Maximum compatibility with existing data
- ✅ Rich customization options
- ✅ BSON compatibility mode
- ✅ Complex type support
- ❌ Larger memory footprint
- ❌ Slower than System.Text.Json

**Configuration:**
```csharp
var serializer = new NewtonsoftSerializer()
{
    UseBsonFormat = true, // Recommended for Akavache compatibility
    Options = new JsonSerializerSettings
    {
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore
    }
};
CacheDatabase.Serializer = serializer;
```

### BSON Variants

For maximum compatibility with existing Akavache data:

```csharp
// System.Text.Json with BSON support
CacheDatabase.Serializer = new SystemJsonBsonSerializer();

// Newtonsoft.Json with BSON support  
CacheDatabase.Serializer = new NewtonsoftBsonSerializer();
```

### Cross-Serializer Compatibility

V11.0 can automatically read data written by different serializers:

```csharp
// Data written with Newtonsoft.Json BSON can be read by System.Text.Json
// Data written with System.Text.Json can be read by Newtonsoft.Json
// Automatic format detection handles the conversion
```

## Cache Types

Akavache provides four types of caches, each with different characteristics:

### UserAccount Cache
**Purpose**: User settings and preferences that should persist and potentially sync across devices.

```csharp
// Store user preferences
var settings = new UserSettings { Theme = "Dark", Language = "en-US" };
await CacheDatabase.UserAccount.InsertObject("user_settings", settings);

// Retrieve preferences
var userSettings = await CacheDatabase.UserAccount.GetObject<UserSettings>("user_settings");
```

**Platform Behavior:**
- **iOS/macOS**: Backed up to iCloud
- **Windows**: May be synced via Microsoft Account
- **Android**: Stored in internal app data

### LocalMachine Cache
**Purpose**: Cached data that can be safely deleted by the system.

```csharp
// Cache API responses
var apiData = await httpClient.GetFromJsonAsync<ApiResponse>("https://api.example.com/data");
await CacheDatabase.LocalMachine.InsertObject("api_cache", apiData, DateTimeOffset.Now.AddHours(6));

// Retrieve with fallback
var cachedData = await CacheDatabase.LocalMachine.GetOrFetchObject("api_cache",
    () => httpClient.GetFromJsonAsync<ApiResponse>("https://api.example.com/data"));
```

**Platform Behavior:**
- **iOS**: May be deleted by system when storage is low
- **Android**: Subject to cache cleanup policies
- **Windows/macOS**: Stored in temp/cache directories

### Secure Cache
**Purpose**: Encrypted storage for sensitive data like credentials and API keys.

```csharp
// Store credentials
await CacheDatabase.Secure.SaveLogin("john.doe", "secretPassword", "myapp.com");

// Retrieve credentials
var loginInfo = await CacheDatabase.Secure.GetLogin("myapp.com");
Console.WriteLine($"User: {loginInfo.UserName}, Password: {loginInfo.Password}");

// Store API keys
await CacheDatabase.Secure.InsertObject("api_key", "sk-1234567890abcdef");
var apiKey = await CacheDatabase.Secure.GetObject<string>("api_key");
```

### InMemory Cache
**Purpose**: Temporary storage that doesn't persist between app sessions.

```csharp
// Cache session data
var sessionData = new SessionInfo { UserId = 123, SessionToken = "abc123" };
await CacheDatabase.InMemory.InsertObject("current_session", sessionData);

// Fast temporary storage
await CacheDatabase.InMemory.InsertObject("temp_calculation", expensiveResult);
```

## Basic Operations

### Storing Data

```csharp
// Store simple objects
await CacheDatabase.UserAccount.InsertObject("key", myObject);

// Store with expiration
await CacheDatabase.LocalMachine.InsertObject("temp_key", data, DateTimeOffset.Now.AddMinutes(30));

// Store multiple objects
var keyValuePairs = new Dictionary<string, MyData>
{
    ["key1"] = new MyData { Value = 1 },
    ["key2"] = new MyData { Value = 2 }
};
await CacheDatabase.UserAccount.InsertObjects(keyValuePairs);

// Store raw bytes
await CacheDatabase.LocalMachine.Insert("raw_key", Encoding.UTF8.GetBytes("Hello World"));
```

### Retrieving Data

```csharp
// Get single object
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");

// Get multiple objects
var keys = new[] { "key1", "key2", "key3" };
var results = await CacheDatabase.UserAccount.GetObjects<MyData>(keys).ToList();

// Get all objects of a type
var allData = await CacheDatabase.UserAccount.GetAllObjects<MyData>().ToList();

// Get raw bytes
var rawData = await CacheDatabase.LocalMachine.Get("raw_key");
```

### Error Handling

```csharp
// Handle missing keys
try
{
    var data = await CacheDatabase.UserAccount.GetObject<MyData>("nonexistent_key");
}
catch (KeyNotFoundException)
{
    // Key not found
    var defaultData = new MyData();
}

// Use fallback pattern
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key")
    .Catch(Observable.Return(new MyData()));
```

### Removing Data

```csharp
// Remove single object
await CacheDatabase.UserAccount.InvalidateObject<MyData>("key");

// Remove multiple objects
await CacheDatabase.UserAccount.InvalidateObjects<MyData>(new[] { "key1", "key2" });

// Remove all objects of a type
await CacheDatabase.UserAccount.InvalidateAllObjects<MyData>();

// Remove all data
await CacheDatabase.UserAccount.InvalidateAll();
```

## Extension Methods

### Get or Fetch Pattern

The most common pattern for caching remote data:

```csharp
// Basic get-or-fetch
var userData = await CacheDatabase.LocalMachine.GetOrFetchObject("user_profile",
    async () => await apiClient.GetUserProfile(userId));

// With expiration
var weatherData = await CacheDatabase.LocalMachine.GetOrFetchObject("weather",
    async () => await weatherApi.GetCurrentWeather(),
    DateTimeOffset.Now.AddMinutes(30));

// With custom fetch observable
var liveData = await CacheDatabase.LocalMachine.GetOrFetchObject("live_data",
    () => Observable.Interval(TimeSpan.FromSeconds(5))
                   .Select(_ => DateTime.Now.ToString()));
```

### Get and Fetch Latest

Returns cached data immediately, then fetches fresh data:

```csharp
// Subscribe to get both cached and fresh data
CacheDatabase.LocalMachine.GetAndFetchLatest("news_feed",
    () => newsApi.GetLatestNews())
    .Subscribe(news => 
    {
        // This will be called twice:
        // 1. Immediately with cached data (if available)
        // 2. When fresh data arrives from the API
        UpdateUI(news);
    });
```

### HTTP/URL Operations

```csharp
// Download and cache URLs
var imageData = await CacheDatabase.LocalMachine.DownloadUrl("https://example.com/image.jpg");

// With custom headers
var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer " + token,
    ["User-Agent"] = "MyApp/1.0"
};
var apiResponse = await CacheDatabase.LocalMachine.DownloadUrl("https://api.example.com/data", 
    HttpMethod.Get, headers);

// Force fresh download
var freshData = await CacheDatabase.LocalMachine.DownloadUrl("https://api.example.com/live", 
    fetchAlways: true);
```

### Login/Credential Management

```csharp
// Save login credentials (encrypted)
await CacheDatabase.Secure.SaveLogin("username", "password", "myapp.com");

// Retrieve credentials
var loginInfo = await CacheDatabase.Secure.GetLogin("myapp.com");
Console.WriteLine($"User: {loginInfo.UserName}");

// Multiple hosts
await CacheDatabase.Secure.SaveLogin("user1", "pass1", "api.service1.com");
await CacheDatabase.Secure.SaveLogin("user2", "pass2", "api.service2.com");

// Remove credentials
await CacheDatabase.Secure.EraseLogin("myapp.com");
```

## Advanced Features

### Relative Time Extensions

```csharp
// Cache for relative time periods
await CacheDatabase.LocalMachine.InsertObject("data", myData, TimeSpan.FromMinutes(30).FromNow());

// Use in get-or-fetch
var cachedData = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data",
    () => FetchFromApi(),
    1.Hours().FromNow());
```

### Custom Schedulers

```csharp
// Use custom scheduler for background operations
CacheDatabase.TaskpoolScheduler = TaskPoolScheduler.Default;

// Or use a custom scheduler
CacheDatabase.TaskpoolScheduler = new EventLoopScheduler();
```

### Cache Inspection

```csharp
// Get all keys (for debugging)
var allKeys = await CacheDatabase.UserAccount.GetAllKeys().ToList();

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

### Cache Maintenance

```csharp
// Force flush all pending operations
await CacheDatabase.UserAccount.Flush();

// Vacuum database (SQLite only - removes deleted data)
await CacheDatabase.UserAccount.Vacuum();

// Flush specific object type
await CacheDatabase.UserAccount.Flush(typeof(MyDataType));
```

### Mixed Object Storage

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

## Platform-Specific Notes

### .NET MAUI

```csharp
// In MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Initialize Akavache early
        CacheDatabase.Serializer = new SystemJsonSerializer();
        CacheDatabase.ForcedDateTimeKind = DateTimeKind.Utc;
        
        CacheDatabase.Initialize(cacheBuilder =>
            cacheBuilder.WithApplicationName("MyMauiApp")
                       .WithSqliteDefaults());

        return builder.Build();
    }
}
```

### WPF Applications

```csharp
// In App.xaml.cs
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureAkavache();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Important: Shutdown Akavache properly
        CacheDatabase.Shutdown().Wait();
        base.OnExit(e);
    }

    private static void ConfigureAkavache()
    {
        CacheDatabase.Serializer = new SystemJsonSerializer();
        CacheDatabase.ForcedDateTimeKind = DateTimeKind.Utc;

        CacheDatabase.Initialize(builder =>
            builder.WithApplicationName("MyWpfApp")
                   .WithSqliteDefaults());
    }
}
```

### iOS Specific

```csharp
// In AppDelegate.cs or SceneDelegate.cs
public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
{
    CacheDatabase.Serializer = new SystemJsonSerializer();
    CacheDatabase.Initialize(builder =>
        builder.WithApplicationName("MyiOSApp")
               .WithSqliteDefaults());

    return base.FinishedLaunching(application, launchOptions);
}
```

### Android Specific

```csharp
// In MainActivity.cs or Application class
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    CacheDatabase.Serializer = new SystemJsonSerializer();
    CacheDatabase.Initialize(builder =>
        builder.WithApplicationName("MyAndroidApp")
               .WithSqliteDefaults());
}
```

### UWP Applications

```csharp
// In App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    CacheDatabase.Serializer = new SystemJsonSerializer();
    CacheDatabase.Initialize(builder =>
        builder.WithApplicationName("MyUwpApp")
               .WithSqliteDefaults());

    // Rest of initialization...
}
```

**Important for UWP**: Mark your application as `x86` or `ARM`, not `Any CPU`.

## Performance

### Benchmarks

Performance comparison of different serializers (operations per second):

| Operation | System.Text.Json | Newtonsoft.Json | BSON |
|-----------|------------------|-----------------|------|
| Serialize small object | 50,000 | 25,000 | 20,000 |
| Deserialize small object | 45,000 | 22,000 | 18,000 |
| Serialize large object | 5,000 | 2,500 | 2,000 |
| Deserialize large object | 4,500 | 2,200 | 1,800 |

### Performance Tips

```csharp
// 1. Use System.Text.Json for best performance
CacheDatabase.Serializer = new SystemJsonSerializer();

// 2. Use batch operations for multiple items
await CacheDatabase.UserAccount.InsertObjects(manyItems);

// 3. Set appropriate expiration times
await CacheDatabase.LocalMachine.InsertObject("temp", data, 30.Minutes().FromNow());

// 4. Use InMemory cache for frequently accessed data
await CacheDatabase.InMemory.InsertObject("hot_data", frequentData);

// 5. Avoid storing very large objects
// Instead, break them into smaller chunks or use compression

// 6. Use specific types instead of object when possible
await CacheDatabase.UserAccount.GetObject<SpecificType>("key"); // Good
await CacheDatabase.UserAccount.Get("key", typeof(SpecificType)); // Slower
```

## Best Practices

### 1. Initialization

```csharp
// ✅ Do: Initialize once at app startup
public class App
{
    static App()
    {
        CacheDatabase.Serializer = new SystemJsonSerializer();
        CacheDatabase.Initialize(builder =>
            builder.WithApplicationName("MyApp")
                   .WithSqliteDefaults());
    }
}

// ❌ Don't: Initialize multiple times
```

### 2. Key Naming

```csharp
// ✅ Do: Use consistent, descriptive key naming
await CacheDatabase.UserAccount.InsertObject("user_profile_123", userProfile);
await CacheDatabase.LocalMachine.InsertObject("api_cache_weather_seattle", weatherData);

// ✅ Do: Use constants for keys
public static class CacheKeys
{
    public const string UserProfile = "user_profile";
    public const string WeatherData = "weather_data";
}

// ❌ Don't: Use random or inconsistent keys
await CacheDatabase.UserAccount.InsertObject("xyz123", someData);
```

### 3. Error Handling

```csharp
// ✅ Do: Handle KeyNotFoundException appropriately
try
{
    var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
}
catch (KeyNotFoundException)
{
    // Provide fallback or default behavior
    var defaultData = new MyData();
}

// ✅ Do: Use GetOrFetchObject for remote data
var data = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data",
    () => httpClient.GetFromJsonAsync<ApiData>("https://api.example.com/data"));
```

### 4. Cache Types Usage

```csharp
// ✅ Do: Use appropriate cache types
await CacheDatabase.UserAccount.InsertObject("user_settings", settings);     // Persistent user data
await CacheDatabase.LocalMachine.InsertObject("api_cache", apiData);         // Cacheable data
await CacheDatabase.Secure.InsertObject("api_key", apiKey);                  // Sensitive data
await CacheDatabase.InMemory.InsertObject("session_data", sessionData);      // Temporary data
```

### 5. Expiration

```csharp
// ✅ Do: Set appropriate expiration times
await CacheDatabase.LocalMachine.InsertObject("api_data", data, 1.Hours().FromNow());
await CacheDatabase.LocalMachine.InsertObject("image_cache", imageBytes, 1.Days().FromNow());

// ✅ Do: Don't expire user settings (unless necessary)
await CacheDatabase.UserAccount.InsertObject("user_preferences", prefs); // No expiration
```

### 6. Shutdown

```csharp
// ✅ Do: Always shutdown Akavache properly
public override void OnExit(ExitEventArgs e)
{
    CacheDatabase.Shutdown().Wait();
    base.OnExit(e);
}

// For MAUI/Xamarin apps
protected override void OnSleep()
{
    CacheDatabase.Shutdown().Wait();
    base.OnSleep();
}
```

### 7. Testing

```csharp
// ✅ Do: Use in-memory cache for unit tests
[SetUp]
public void Setup()
{
    CacheDatabase.Serializer = new SystemJsonSerializer();
    CacheDatabase.Initialize(builder =>
        builder.WithApplicationName("TestApp")
               .WithInMemoryDefaults());
}

[TearDown]
public void TearDown()
{
    CacheDatabase.Shutdown().Wait();
}
```

## Troubleshooting

### Common Issues

#### 1. "No serializer has been registered"
```csharp
// Fix: Register a serializer before initializing
CacheDatabase.Serializer = new SystemJsonSerializer();
CacheDatabase.Initialize(/* ... */);
```

#### 2. "BlobCache has not been initialized"
```csharp
// Fix: Call Initialize before using cache
CacheDatabase.Initialize(builder => builder.WithApplicationName("MyApp").WithInMemoryDefaults());
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
```

#### 3. Data compatibility issues
```csharp
// Fix: Use cross-compatible serializer or migration
CacheDatabase.Serializer = new NewtonsoftBsonSerializer(); // Most compatible
```

#### 4. SQLite errors on mobile
```csharp
// Fix: Ensure SQLitePCL.raw bundle is installed
// Add to your project:
// <PackageReference Include="SQLitePCLRaw.lib.e_sqlite3" Version="2.1.11" />
// If using Encrypted SQLite, also add:
// <PackageReference Include="SQLitePCLRaw.lib.e_sqlcipher" Version="2.1.11" />
```

### Platform-Specific Issues

#### iOS Linker Issues
```csharp
// Add LinkerPreserve.cs to your iOS project:
public static class LinkerPreserve
{
    static LinkerPreserve()
    {
        var persistentName = typeof(SQLitePersistentBlobCache).FullName;
        var encryptedName = typeof(SQLiteEncryptedBlobCache).FullName;
    }
}
```

#### UWP x64 Issues
Ensure your UWP project targets a specific platform (x86, x64, ARM) rather than "Any CPU".

## Support and Contributing

- 📖 **Documentation**: [https://github.com/reactiveui/Akavache](https://github.com/reactiveui/Akavache)
- 🐛 **Issues**: [GitHub Issues](https://github.com/reactiveui/Akavache/issues)
- 💬 **Chat**: [ReactiveUI Slack](https://reactiveui.net/slack)
- 📦 **NuGet**: [Akavache Packages](https://www.nuget.org/packages?q=akavache)

## License

Akavache is licensed under the [MIT License](LICENSE).
