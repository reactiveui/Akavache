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

# Akavache V11.1: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e., writes to disk) key-value store created for writing desktop and mobile applications in C#, based on SQLite3. Akavache is great for both storing important data (i.e., user settings) as well as cached local data that expires. This project is tested with BrowserStack.

## What's New in V11.1

Akavache V11.1 introduces a new **Builder Pattern** for initialization, improved serialization support, and enhanced cross-serializer compatibility:

- 🏗️ **Builder Pattern**: New fluent API for configuring cache instances
- 🔄 **Multiple Serializer Support**: Choose between System.Text.Json, Newtonsoft.Json, each with a BSON variant
- 🔗 **Cross-Serializer Compatibility**: Read data written by different serializers
- 🧩 **Modular Design**: Install only the packages you need
- 📱 **Enhanced .NET MAUI Support**: First-class support for .NET 9 cross-platform development
- 🔒 **Improved Security**: Better encrypted cache implementation

### Development History

Akavache V11.1 represents a significant evolution in the library's architecture, developed through extensive testing and community feedback in our incubator project. The new features and improvements in V11.1 were first prototyped and battle-tested in the [ReactiveMarbles.CacheDatabase](https://github.com/reactivemarbles/CacheDatabase) repository, which served as an experimental ground for exploring new caching concepts and architectural patterns.

## Quick Start

### 1. Install Packages

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### 2. Initialize Akavache

#### Static Initialization (Recommended for most apps)
```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

// Initialize with the builder pattern
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider() // REQUIRED: Explicitly initialize SQLite provider
               .WithSqliteDefaults());
```

#### Dependency Injection Registration (for DI containers)

```csharp
// Example: Register Akavache with Splat DI
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        "MyApp",
        builder => builder.WithSqliteProvider()    // REQUIRED: Explicit provider initialization
                          .WithSqliteDefaults(),
        (splat, instance) => splat.RegisterLazySingleton(() => instance));
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

### Core Package (Included with Serializers, In Memory only)
```xml
<PackageReference Include="Akavache" Version="11.1.**" />
```

### Sqlite Storage Backends (recommended)
```xml
<!-- SQLite persistence -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.**" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.**" />
```

### Serializers (Choose One (Required!))
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.**" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.**" />
```

### Optional Extensions
```xml
<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.1.**" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.1.**" />
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

#### New V11.1 Code:
```csharp
// V11.1 initialization (RECOMMENDED: Explicit provider pattern)

AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")        
           .WithSqliteProvider()    // REQUIRED: Explicit provider initialization
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

## Configuration

### Builder Pattern

Akavache V11.1 uses a fluent builder pattern for configuration:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")           // Required
               .WithSqliteProvider()                   // Initialize SQLite backend
               .WithSqliteDefaults());                 // SQLite persistence
```

#### Provider Initialization Pattern

**Explicit Provider Initialization (Recommended):**
```csharp
// ✅ RECOMMENDED: Explicit provider initialization
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()        // Explicit provider initialization
               .WithSqliteDefaults());      // Configure defaults

// ✅ For encrypted SQLite
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithEncryptedSqliteProvider()   // Explicit encrypted provider
               .WithSqliteDefaults("password"));
```

**Automatic Provider Initialization (Backward Compatibility Only):**
```csharp
// ⚠️ DEPRECATED: Automatic fallback behavior
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteDefaults());      // Automatically calls WithSqliteProvider() internally
```

> **Important:** The automatic provider initialization in `WithSqliteDefaults()` is provided for **backward compatibility only** and may be removed in future versions for forward compatibility with other DI containers. Always use explicit provider initialization in new code.

### Configuration Options

#### 1. In-Memory Only (for testing or non retensive applications)
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("TestApp")
               .WithInMemoryDefaults());
```

#### 2. SQLite Persistence
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()    // REQUIRED: Must be called before WithSqliteDefaults()
               .WithSqliteDefaults());
```

#### 3. Encrypted SQLite
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithEncryptedSqliteProvider()    // REQUIRED: Must be called before WithSqliteDefaults()
               .WithSqliteDefaults("mySecretPassword"));
```

#### 4. Custom Cache Instances
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithEncryptedSqliteProvider()    // Provider must be initialized for custom SQLite caches
           .WithUserAccount(new SqliteBlobCache("custom-user.db"))
           .WithLocalMachine(new SqliteBlobCache("custom-local.db"))
           .WithSecure(new EncryptedSqliteBlobCache("secure.db", "password"))
           .WithInMemory(new InMemoryBlobCache()));
```

### Application Name Configuration Order

> **🔧 Important Fix in V11.1.1+**: Prior versions computed the settings cache path in the constructor before `WithApplicationName()` could be called, causing the settings cache to always use the default "Akavache" directory regardless of the custom application name. In V11.1.1+, the settings cache path is now computed lazily when first accessed, ensuring it respects the custom application name set via `WithApplicationName()`.

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

#### 5. DateTime Handling
```csharp
// Set global DateTime behavior
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSqliteProvider()    // REQUIRED: Provider initialization
           .WithForcedDateTimeKind(DateTimeKind.Utc)
           .WithSqliteDefaults());
```

## Serializers

Akavache V11.1 supports multiple serialization formats with automatic cross-compatibility.

### System.Text.Json (Recommended)

**Best for**: New applications, performance-critical scenarios, .NET native support

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
```

### Newtonsoft.Json (Maximum Compatibility)

**Best for**: Migrating from older Akavache versions, complex serialization needs

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
```
Once configured, pass the serializer type to the builder:
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<NewtonsoftSerializer>(
        () => serializer,
        builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### BSON Variants

For maximum backward compatibility with existing Akavache data: use `UseBsonFormat = true` in either serializer.

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
// ✅ RECOMMENDED: Use existing invalidation methods
await CacheDatabase.UserAccount.Invalidate("key");                    // Remove any key
await CacheDatabase.UserAccount.InvalidateObject<MyData>("key");      // Remove typed key (recommended)
await CacheDatabase.UserAccount.Invalidate(new[] { "key1", "key2" }); // Remove multiple keys
await CacheDatabase.UserAccount.InvalidateObjects<MyData>(new[] { "key1", "key2" }); // Remove multiple typed keys

// Remove all objects of a type
await CacheDatabase.UserAccount.InvalidateAllObjects<MyData>();

// Remove all data
await CacheDatabase.UserAccount.InvalidateAll();
```

**Best Practices:**
- ✅ **Use** `InvalidateObject<T>()` methods for type-safe deletion
- ✅ **Use** `GetAllKeysSafe()` for exception-safe key enumeration in reactive chains
- ⚠️ **Avoid** complex `GetAllKeys().Subscribe()` patterns - use direct invalidation instead
- See [Cache Deletion Guide](src/Samples/CacheDeletion-TheRightWay.md) for detailed examples

> 🔧 **Important Fix in V11.1.1+**: Prior to V11.1.1, calling `Invalidate()` on InMemory cache didn't properly clear the RequestCache, causing subsequent `GetOrFetchObject` calls to return stale data instead of fetching fresh data. This has been fixed to ensure proper cache invalidation behavior. For comprehensive invalidation patterns and examples, see [`CacheInvalidationPatterns.cs`](src/Samples/CacheInvalidationPatterns.cs).

### Updating Expiration

```csharp
// Extend expiration for a single cache entry
await CacheDatabase.LocalMachine.UpdateExpiration("api_data", DateTimeOffset.Now.AddHours(2));

// Extend expiration using relative time
await CacheDatabase.LocalMachine.UpdateExpiration("user_session", TimeSpan.FromMinutes(30));

// Update expiration for multiple entries
var keys = new[] { "cache_key1", "cache_key2", "cache_key3" };
await CacheDatabase.LocalMachine.UpdateExpiration(keys, DateTimeOffset.Now.AddDays(1));
```

> 💡 **For comprehensive UpdateExpiration patterns and use cases**, see [`UpdateExpirationPatterns.cs`](src/Samples/UpdateExpirationPatterns.cs) in the samples directory.

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

Returns cached data immediately, then fetches fresh data. This is one of the most powerful patterns in Akavache but requires careful handling of the dual subscription behavior.

> **⚠️ Important:** Always use `Subscribe()` with GetAndFetchLatest - never `await` it directly. The method is designed to call your subscriber twice: once with cached data (if available) and once with fresh data.

> **💡 Empty Cache Behavior:** When no cached data exists (first app run, after cache clear, or expired data), GetAndFetchLatest will call your subscriber once with fresh data from the fetch function. This ensures reliable data delivery even in empty cache scenarios.

#### Basic Pattern

```csharp
// Basic usage - subscriber called 1-2 times
CacheDatabase.LocalMachine.GetAndFetchLatest("news_feed",
    () => newsApi.GetLatestNews())
    .Subscribe(news => 
    {
        // This will be called:
        // - Once with fresh data (if no cached data exists)
        // - Twice: cached data immediately + fresh data (if cached data exists)
        UpdateUI(news);
    });
```

#### Pattern 1: Simple Replacement (Most Common)

Best for data where you want to completely replace the UI content:

```csharp
// Simple replacement - just update the UI each time
CacheDatabase.LocalMachine.GetAndFetchLatest("user_profile",
    () => userApi.GetProfile(userId))
    .Subscribe(userProfile => 
    {
        // Replace entire UI content - works for both cached and fresh data
        DisplayUserProfile(userProfile);
        
        // Optional: Show loading indicator only on fresh data
        if (IsLoadingFreshData())
        {
            HideLoadingIndicator();
        }
    });
```

#### Pattern 2: Merge Strategy for Collections

Best for lists where you want to merge new items with existing ones:

```csharp
public class MessageService 
{
    private readonly List<Message> _currentMessages = new();
    private bool _isFirstLoad = true;

    public IObservable<List<Message>> GetMessages(int ticketId)
    {
        return CacheDatabase.LocalMachine.GetAndFetchLatest($"messages_{ticketId}",
            () => messageApi.GetMessages(ticketId))
            .Do(messages => 
            {
                if (_isFirstLoad)
                {
                    // First call: load cached data or initial fresh data
                    _currentMessages.Clear();
                    _currentMessages.AddRange(messages);
                    _isFirstLoad = false;
                }
                else
                {
                    // Second call: merge fresh data with existing
                    var newMessages = messages.Except(_currentMessages, new MessageComparer()).ToList();
                    _currentMessages.AddRange(newMessages);
                    
                    // Optional: Sort by timestamp
                    _currentMessages.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
                }
            })
            .Select(_ => _currentMessages.ToList()); // Return defensive copy
    }
}
```

#### Pattern 3: Differential Updates with State Tracking

Best for complex scenarios where you need fine-grained control:

```csharp
public class NewsService 
{
    private readonly Subject<List<NewsItem>> _newsSubject = new();
    private List<NewsItem> _cachedNews = new();
    private bool _hasCachedData = false;

    public IObservable<List<NewsItem>> GetNews()
    {
        CacheDatabase.LocalMachine.GetAndFetchLatest("news_feed",
            () => newsApi.GetLatestNews())
            .Subscribe(freshNews => 
            {
                if (!_hasCachedData)
                {
                    // First emission: cached data (or first fresh data if no cache)
                    _cachedNews = freshNews.ToList();
                    _hasCachedData = true;
                    _newsSubject.OnNext(_cachedNews);
                }
                else
                {
                    // Second emission: fresh data - perform smart merge
                    var updatedItems = new List<NewsItem>();
                    var newItems = new List<NewsItem>();
                    
                    foreach (var freshItem in freshNews)
                    {
                        var existingItem = _cachedNews.FirstOrDefault(c => c.Id == freshItem.Id);
                        if (existingItem != null)
                        {
                            // Update existing item if content changed
                            if (existingItem.LastModified < freshItem.LastModified)
                            {
                                updatedItems.Add(freshItem);
                                var index = _cachedNews.IndexOf(existingItem);
                                _cachedNews[index] = freshItem;
                            }
                        }
                        else
                        {
                            // New item
                            newItems.Add(freshItem);
                            _cachedNews.Add(freshItem);
                        }
                    }
                    
                    // Remove items that no longer exist
                    _cachedNews.RemoveAll(cached => !freshNews.Any(fresh => fresh.Id == cached.Id));
                    
                    // Notify subscribers with current state
                    _newsSubject.OnNext(_cachedNews.ToList());
                    
                    // Optional: Emit specific change notifications
                    if (newItems.Any()) OnNewItemsAdded?.Invoke(newItems);
                    if (updatedItems.Any()) OnItemsUpdated?.Invoke(updatedItems);
                }
            });
            
        return _newsSubject.AsObservable();
    }
}
```

#### Pattern 4: UI Loading States

Best for providing responsive UI feedback:

```csharp
public class DataService 
{
    public IObservable<DataState<List<Product>>> GetProducts()
    {
        var loadingState = Observable.Return(DataState<List<Product>>.Loading());
        
        var dataStream = CacheDatabase.LocalMachine.GetAndFetchLatest("products",
            () => productApi.GetProducts())
            .Select(products => DataState<List<Product>>.Success(products))
            .Catch<DataState<List<Product>>, Exception>(ex => 
                Observable.Return(DataState<List<Product>>.Error(ex)));
        
        return loadingState.Concat(dataStream);
    }
}

// Usage in ViewModel
public class ProductViewModel 
{
    public ProductViewModel()
    {
        _dataService.GetProducts()
            .Subscribe(state => 
            {
                switch (state.Status)
                {
                    case DataStatus.Loading:
                        IsLoading = true;
                        break;
                    case DataStatus.Success:
                        IsLoading = false;
                        Products = state.Data;
                        break;
                    case DataStatus.Error:
                        IsLoading = false;
                        ErrorMessage = state.Error?.Message;
                        break;
                }
            });
    }
}
```

#### Pattern 5: Conditional Fetching

Control when fresh data should be fetched:

```csharp
// Only fetch fresh data if cached data is older than 5 minutes
CacheDatabase.LocalMachine.GetAndFetchLatest("weather_data",
    () => weatherApi.GetCurrentWeather(),
    fetchPredicate: cachedDate => DateTimeOffset.Now - cachedDate > TimeSpan.FromMinutes(5))
    .Subscribe(weather => UpdateWeatherDisplay(weather));

// Fetch fresh data based on user preference
CacheDatabase.LocalMachine.GetAndFetchLatest("user_settings",
    () => settingsApi.GetUserSettings(),
    fetchPredicate: _ => userPreferences.AllowBackgroundRefresh)
    .Subscribe(settings => ApplySettings(settings));
```

#### Common Anti-Patterns ❌

```csharp
// ❌ DON'T: Await GetAndFetchLatest - you'll only get first result
var data = await CacheDatabase.LocalMachine.GetAndFetchLatest("key", fetchFunc).FirstAsync();

// ❌ DON'T: Mix cached retrieval with GetAndFetchLatest
var cached = await cache.GetObject<T>("key").FirstOrDefaultAsync();
cache.GetAndFetchLatest("key", fetchFunc).Subscribe(fresh => /* handle fresh */);

// ❌ DON'T: Ignore the dual nature in UI updates
cache.GetAndFetchLatest("key", fetchFunc)
    .Subscribe(data => items.Clear()); // This will clear twice!
```

#### Best Practices ✅

1. **Always use Subscribe(), never await** - GetAndFetchLatest is designed for reactive scenarios
2. **Handle both cached and fresh data appropriately** - Design your subscriber to work correctly when called 1-2 times (once if no cache, twice if cached data exists)
3. **Use state tracking for complex merges** - Keep track of whether you're handling cached or fresh data
4. **Provide loading indicators** - Show users when fresh data is being fetched
5. **Handle errors gracefully** - Network calls can fail, always have fallback logic
6. **Consider using fetchPredicate** - Avoid unnecessary network calls when cached data is still fresh
7. **Test empty cache scenarios** - Ensure your app works correctly on first run or after cache clears


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

```

## Advanced Features

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

> 📖 **For comprehensive patterns and real-world examples**, see [`UpdateExpirationPatterns.cs`](src/Samples/UpdateExpirationPatterns.cs), which includes:
> - HTTP caching with 304 Not Modified handling
> - Session management with sliding expiration 
> - Bulk operations and performance optimization
> - Error handling and best practices
> - Performance comparisons and method overload reference

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

#### GetAllKeysSafe Methods

The `GetAllKeysSafe` methods provide exception-safe alternatives to `GetAllKeys()` that handle errors within the observable chain:

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
- **Exception handling**: Catches exceptions and returns empty sequence instead of throwing
- **Null safety**: Filters out null or empty keys automatically  
- **Observable chain friendly**: Allows reactive code to continue executing even when underlying storage has issues
- **Logging**: Logs exceptions for debugging while keeping the application stable

**Use GetAllKeysSafe when:**
- Building reactive pipelines that should be resilient to storage exceptions
- You want exceptions handled within the observable chain rather than breaking it
- Working with unreliable storage scenarios or during development/testing
- You prefer continuation over immediate failure when key enumeration fails

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

## Akavache.Drawing

Akavache.Drawing provides comprehensive image caching and bitmap manipulation functionality for Akavache applications. Built on Splat, it offers cross-platform support for loading, caching, and manipulating images with enhanced features beyond basic blob storage.

### Features

- **Image Loading & Caching**: Load images from cache with automatic format detection
- **URL Image Caching**: Download and cache images from URLs with built-in HTTP support
- **Image Manipulation**: Resize, crop, and generate thumbnails with caching
- **Multiple Format Support**: PNG, JPEG, GIF, BMP, WebP, and other common formats
- **Fallback Support**: Automatic fallback to default images when loading fails
- **Batch Operations**: Load multiple images efficiently
- **Size Detection**: Get image dimensions without full loading
- **Advanced Caching**: Pattern-based cache clearing and preloading
- **Cross-Platform**: Works on all .NET platforms supported by Akavache

### Installation

```xml
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### Dependencies

Akavache.Drawing requires:
- `Akavache.Core` - Core caching functionality
- `Splat.Drawing` - Cross-platform bitmap abstractions

### Basic Usage

#### 1. Initialize Drawing Support

```csharp
using Akavache.Core;
using Akavache.Drawing;
using Akavache.SystemTextJson;
using Splat;

// Initialize Akavache with drawing support
CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
    builder.WithApplicationName("MyImageApp")
           .WithSqliteProvider()
           .WithSqliteDefaults());

// Register platform-specific bitmap loader using Splat (if needed (Net 8.0+))
AppLocator.CurrentMutable.RegisterPlatformBitmapLoader();
```

#### 2. Load Images from Cache

```csharp
// Load image from cache
var image = await CacheDatabase.LocalMachine.LoadImage("user_avatar");

// Load with custom sizing
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("user_avatar", 150, 150);

// Load with error handling
try
{
    var profileImage = await CacheDatabase.UserAccount.LoadImage("profile_pic");
    DisplayImage(profileImage);
}
catch (KeyNotFoundException)
{
    // Image not found in cache
    ShowDefaultImage();
}
```

#### 3. Load Images from URLs

```csharp
// Download and cache image from URL
var imageFromUrl = await CacheDatabase.LocalMachine
    .LoadImageFromUrl("https://example.com/images/photo.jpg");

// With custom expiration
var tempImage = await CacheDatabase.LocalMachine
    .LoadImageFromUrl("https://api.example.com/temp-image.png", 
                     absoluteExpiration: DateTimeOffset.Now.AddHours(1));

// Force fresh download (bypass cache)
var freshImage = await CacheDatabase.LocalMachine
    .LoadImageFromUrl("https://api.example.com/live-feed.jpg", fetchAlways: true);

// With custom key
var namedImage = await CacheDatabase.LocalMachine
    .LoadImageFromUrl("user_background", "https://example.com/bg.jpg");
```

#### 4. Save Images to Cache

```csharp
// Save image to cache
await CacheDatabase.LocalMachine.SaveImage("user_photo", bitmap);

// Save with expiration
await CacheDatabase.LocalMachine.SaveImage("temp_image", bitmap, 
    DateTimeOffset.Now.AddDays(7));

// Convert bitmap to bytes for manual storage
var imageBytes = await bitmap.ImageToBytes().FirstAsync();
await CacheDatabase.LocalMachine.Insert("raw_image_data", imageBytes);
```

### Advanced Features

#### Batch Image Operations

```csharp
// Load multiple images at once
var imageKeys = new[] { "image1", "image2", "image3" };
var loadedImages = await CacheDatabase.LocalMachine
    .LoadImages(imageKeys, desiredWidth: 200, desiredHeight: 200)
    .ToList();

foreach (var kvp in loadedImages)
{
    Console.WriteLine($"Loaded {kvp.Key}: {kvp.Value.Width}x{kvp.Value.Height}");
}

// Preload images from URLs (background caching)
var urls = new[]
{
    "https://example.com/image1.jpg",
    "https://example.com/image2.jpg",
    "https://example.com/image3.jpg"
};

await CacheDatabase.LocalMachine.PreloadImagesFromUrls(urls, 
    DateTimeOffset.Now.AddDays(1));
```

#### Image Fallbacks

```csharp
// Load image with automatic fallback
var defaultImageBytes = File.ReadAllBytes("default-avatar.png");

var userAvatar = await CacheDatabase.UserAccount
    .LoadImageWithFallback("user_avatar", defaultImageBytes, 100, 100);

// Load from URL with fallback
var profileImage = await CacheDatabase.LocalMachine
    .LoadImageFromUrlWithFallback("https://example.com/profile.jpg", 
                                 defaultImageBytes, 
                                 desiredWidth: 200, 
                                 desiredHeight: 200);
```

#### Thumbnail Generation

```csharp
// Create and cache thumbnail from existing image
await CacheDatabase.LocalMachine.CreateAndCacheThumbnail(
    sourceKey: "original_photo",
    thumbnailKey: "photo_thumb", 
    thumbnailWidth: 150, 
    thumbnailHeight: 150,
    absoluteExpiration: DateTimeOffset.Now.AddDays(30));

// Load the cached thumbnail
var thumbnail = await CacheDatabase.LocalMachine.LoadImage("photo_thumb");
```

#### Image Size Detection

```csharp
// Get image dimensions without fully loading
var imageSize = await CacheDatabase.LocalMachine.GetImageSize("large_image");
Console.WriteLine($"Image size: {imageSize.Width}x{imageSize.Height}");
Console.WriteLine($"Aspect ratio: {imageSize.AspectRatio:F2}");

// Use size info for layout decisions
if (imageSize.AspectRatio > 1.5)
{
    // Wide image
    SetWideImageLayout();
}
else
{
    // Square or tall image
    SetNormalImageLayout();
}
```

#### Cache Management

```csharp
// Clear images matching a pattern
await CacheDatabase.LocalMachine.ClearImageCache(key => key.StartsWith("temp_"));

// Clear all user avatars
await CacheDatabase.UserAccount.ClearImageCache(key => key.Contains("avatar"));

// Clear expired images
await CacheDatabase.LocalMachine.ClearImageCache(key => 
    key.StartsWith("cache_") && IsExpired(key));
```

### Complete Example: Photo Gallery App

```csharp
public class PhotoGalleryService
{
    private readonly IBlobCache _imageCache;
    private readonly IBlobCache _thumbnailCache;

    public PhotoGalleryService()
    {
        // Initialize Akavache with drawing support
        AppBuilder.CreateSplatBuilder().WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("PhotoGallery")
               .WithSqliteProvider()        // REQUIRED: Explicit provider
               .WithSqliteDefaults());

        _imageCache = CacheDatabase.LocalMachine;
        _thumbnailCache = CacheDatabase.UserAccount;
    }

    public async Task<IBitmap> LoadPhotoAsync(string photoId, bool generateThumbnail = false)
    {
        try
        {
            // Try to load from cache first
            var photo = await _imageCache.LoadImage($"photo_{photoId}");
            
            // Generate thumbnail if requested and not exists
            if (generateThumbnail)
            {
                await _thumbnailCache.CreateAndCacheThumbnail(
                    $"photo_{photoId}", 
                    $"thumb_{photoId}", 
                    200, 200,
                    DateTimeOffset.Now.AddMonths(1));
            }
            
            return photo;
        }
        catch (KeyNotFoundException)
        {
            // Load from remote URL if not cached
            var photoUrl = $"https://api.photos.com/images/{photoId}";
            return await _imageCache.LoadImageFromUrl($"photo_{photoId}", photoUrl, 
                absoluteExpiration: DateTimeOffset.Now.AddDays(7));
        }
    }

    public async Task<IBitmap> LoadThumbnailAsync(string photoId)
    {
        try
        {
            return await _thumbnailCache.LoadImage($"thumb_{photoId}", 200, 200);
        }
        catch (KeyNotFoundException)
        {
            // Generate thumbnail from full image
            var fullImage = await LoadPhotoAsync(photoId);
            await _thumbnailCache.SaveImage($"thumb_{photoId}", fullImage, 
                DateTimeOffset.Now.AddMonths(1));
            return await _thumbnailCache.LoadImage($"thumb_{photoId}", 200, 200);
        }
    }

    public async Task PreloadGalleryAsync(IEnumerable<string> photoIds)
    {
        var photoUrls = photoIds.Select(id => $"https://api.photos.com/images/{id}");
        await _imageCache.PreloadImagesFromUrls(photoUrls, 
            DateTimeOffset.Now.AddDays(7));
    }

    public async Task ClearOldCacheAsync()
    {
        // Clear images older than 30 days
        await _imageCache.ClearImageCache(key => 
            key.StartsWith("photo_") && IsOlderThan30Days(key));
        
        // Clear thumbnails older than 60 days  
        await _thumbnailCache.ClearImageCache(key => 
            key.StartsWith("thumb_") && IsOlderThan60Days(key));
    }

    private static bool IsOlderThan30Days(string key) => 
        /* Implementation to check cache age */ false;
        
    private static bool IsOlderThan60Days(string key) => 
        /* Implementation to check cache age */ false;
}
```

## Akavache.Settings

Akavache.Settings provides a specialized settings database for installable applications. It creates persistent settings that are stored one level down from the application folder, making application updates less painful as the settings survive reinstalls.

### Features

- **Type-Safe Settings**: Strongly-typed properties with default values
- **Automatic Persistence**: Settings are automatically saved when changed
- **Application Update Friendly**: Settings survive application reinstalls
- **Encrypted Storage**: Optional secure settings with password protection
- **Multiple Settings Classes**: Support for multiple settings categories

### Installation

```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

### Basic Usage

#### 1. Create a Settings Class

```csharp
using Akavache.Settings;

public class AppSettings : SettingsBase
{
    public AppSettings() : base(nameof(AppSettings)) { }

    public string UserName
    {
        get => GetOrCreate("DefaultUser");
        set => SetOrCreate(value);
    }

    public bool EnableNotifications
    {
        get => GetOrCreate(true);
        set => SetOrCreate(value);
    }
}
```

### 3. Initialize and Use Settings

```csharp
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder().WithAkavache<SystemJsonSerializer>(builder =>
    builder.WithApplicationName("MyApp")
           .WithSqliteProvider()
           .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// Use settings
appSettings.UserName = "John Doe";
appSettings.EnableNotifications = false;
```

## Migration from V10.x

### Old V10.x Code:
```csharp
// V10.x initialization
BlobCache.ApplicationName = "MyApp";

// Usage
var data = await BlobCache.UserAccount.GetObject<MyData>("key");
await BlobCache.LocalMachine.InsertObject("key", myData);
```

### New V11.1 Code:
```csharp
// V11.1 initialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")        
               .WithSqliteProvider()    // REQUIRED: Explicit provider initialization
               .WithSqliteDefaults());

// Usage (same API)
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
await CacheDatabase.LocalMachine.InsertObject("key", myData);
```

## Core Packages

### Required (choose one serializer)
```xml
<!-- SQLite persistence -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />

<!-- System.Text.Json (fastest, recommended) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />

<!-- OR Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```

### Optional Extensions
```xml
<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />

<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

## Documentation

For comprehensive documentation, examples, and advanced usage:

- 📖 **[Complete Documentation](docs/index.md)** - Full documentation index
- 🚀 **[Installation Guide](docs/installation.md)** - Detailed installation and setup
- 🔧 **[Configuration](docs/configuration.md)** - Advanced configuration options  
- 📚 **[Migration Guide](docs/migration-v10-to-v11.md)** - Complete V10 to V11 migration
- 🎯 **[Basic Operations](docs/basic-operations.md)** - Comprehensive API guide
- ⚙️ **[Serializers](docs/serializers.md)** - Serialization options and configuration
- 📱 **[Platform Notes](docs/platform-notes.md)** - Platform-specific guidance
- 📊 **[Performance](docs/performance.md)** - Performance benchmarks and optimization
- ✅ **[Best Practices](docs/best-practices.md)** - Recommended patterns and practices
- 🔧 **[Troubleshooting](docs/troubleshooting.md)** - Common issues and solutions
- ⚙️ **[Settings Guide](docs/settings.md)** - Comprehensive Akavache.Settings documentation

## Support and Contributing

- 📖 **Documentation**: [https://github.com/reactiveui/Akavache](https://github.com/reactiveui/Akavache)
- 🐛 **Issues**: [GitHub Issues](https://github.com/reactiveui/Akavache/issues)
- 💬 **Chat**: [ReactiveUI Slack](https://reactiveui.net/slack)
- 📦 **NuGet**: [Akavache Packages](https://www.nuget.org/packages?q=akavache)

## Thanks 

This project is tested with BrowserStack.

## Sponsors

Akavache is a [ReactiveUI Foundation](https://reactiveui.net/) project.

<a href="https://reactiveui.net/">
    <img src="https://reactiveui.net/img/logo.svg" alt="ReactiveUI Foundation" width="120" />
</a>

### Microsoft

<a href="https://dotnetfoundation.org">
  <img src="https://theme.dotnetfoundation.org/img/logo.svg" width="100" />
</a>

Microsoft is providing [Visual Studio](https://github.com/Microsoft/VisualStudio) licenses for core contributors and the project.

### JetBrains

<a href="https://www.jetbrains.com/community/opensource/">
    <img src="https://theme.dotnetfoundation.org/img/jetbrains.png" width="100" />
</a>

JetBrains is kindly supporting the project with [licenses for their excellent tools](https://www.jetbrains.com/community/opensource/).

## Backers

[Become a backer](https://opencollective.com/reactiveui#backer) and get your image on our README on GitHub with a link to your site.

<a href="https://opencollective.com/reactiveui/backer/0/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/0/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/1/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/1/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/2/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/2/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/3/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/3/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/4/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/4/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/5/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/5/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/6/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/6/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/7/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/7/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/8/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/8/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/9/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/9/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/10/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/10/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/11/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/11/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/12/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/12/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/13/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/13/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/14/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/14/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/15/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/15/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/16/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/16/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/17/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/17/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/18/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/18/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/19/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/19/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/20/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/20/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/21/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/21/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/22/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/22/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/23/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/23/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/24/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/24/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/25/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/25/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/26/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/26/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/27/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/27/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/28/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/28/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/backer/29/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/29/avatar.svg"></a>

## Sponsors

[Become a sponsor](https://opencollective.com/reactiveui#sponsor) and get your logo on our README on GitHub with a link to your site.

<a href="https://opencollective.com/reactiveui/sponsor/0/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/1/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/2/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/3/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/4/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/5/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/6/website" target="_blank"><img src="https://opencollective.com/reactiveui/backer/6/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/7/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/8/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/reactiveui/sponsor/9/website" target="_blank"><img src="https://opencollective.com/reactiveui/sponsor/9/avatar.svg"></a>

## License

Akavache is licensed under the [MIT License](LICENSE).