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

### Development History

Akavache V11.0 represents a significant evolution in the library's architecture, developed through extensive testing and community feedback in our incubator project. The new features and improvements in V11.0 were first prototyped and battle-tested in the [ReactiveMarbles.CacheDatabase](https://github.com/reactivemarbles/CacheDatabase) repository, which served as an experimental ground for exploring new caching concepts and architectural patterns.

**Key Development Milestones:**

- **🧪 Incubation Phase**: The builder pattern, modular serialization system, and enhanced API were first developed and tested in ReactiveMarbles.CacheDatabase
- **🔬 Community Testing**: Early adopters and contributors provided valuable feedback on the new architecture through real-world usage scenarios
- **🚀 Production Validation**: The incubator project allowed us to validate performance improvements, API ergonomics, and cross-platform compatibility before integrating into Akavache
- **📈 Iterative Refinement**: Multiple iterations based on community feedback helped shape the final V11.0 API design and feature set

This careful incubation process ensured that V11.0 delivers not just new features, but a more robust, flexible, and maintainable caching solution that builds upon years of community experience and testing. The ReactiveMarbles organization continues to serve as a proving ground for innovative reactive programming concepts that eventually make their way into the broader ReactiveUI ecosystem.

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
> **Note:** 

> `WithAkavache` `WithAkavacheCacheDatabase` and `Initialize` always requires an `ISerializer` defined as a generic type, such as `WithAkavache<SystemJsonSerializer>`. This ensures the cache instance is properly configured for serialization.

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
               .WithSqliteDefaults());
```

#### Dependency Injection Registration (for DI containers)

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

// Example: Register Akavache with Splat DI
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        "MyApp",
        builder => builder.WithSqliteDefaults(),
        (splat, instance) => splat.RegisterLazySingleton(() => instance));

// For in-memory cache (testing or lightweight scenarios):
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        "Akavache",
        builder => builder.WithInMemoryDefaults(),
        (splat, instance) => splat.RegisterLazySingleton(() => instance));
```

#### Manual Instance Creation (advanced scenarios)
```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;

var akavacheInstance = CacheDatabase.CreateBuilder()
    .WithSerializer<SystemJsonSerializer>()
    .WithApplicationName("MyApp")
    .WithSqliteDefaults()
    .Build();

// Use akavacheInstance.UserAccount, akavacheInstance.LocalMachine, etc.
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

### Core Package (Included with Serializers, In Memory only)
```xml
<PackageReference Include="Akavache" Version="11.0.**" />
```

### Sqlite Storage Backends (recommended)
```xml
<!-- SQLite persistence -->
<PackageReference Include="Akavache.Sqlite3" Version="11.0.**" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.0.**" />
```

### Serializers (Choose One (Required!))
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.0.**" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.0.**" />
```

### Optional Extensions
```xml
<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.0.**" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.0.**" />
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

AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
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
        CacheDatabase
            .Initialize<SystemJsonSerializer>(builder =>
                builder.WithSqliteDefaults(),
                appName);
    }
}

// Then in your app:
AkavacheMigration.InitializeV11("MyApp");
```

## Configuration

### Builder Pattern

Akavache V11.0 uses a fluent builder pattern for configuration:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")           // Required
               .WithSqliteDefaults());                 // SQLite persistence
```

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
               .WithSqliteDefaults());
```

#### 3. Encrypted SQLite
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteDefaults("mySecretPassword"));
```

#### 4. Custom Cache Instances
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithUserAccount(new SqliteBlobCache("custom-user.db"))
           .WithLocalMachine(new SqliteBlobCache("custom-local.db"))
           .WithSecure(new EncryptedSqliteBlobCache("secure.db", "password"))
           .WithInMemory(new InMemoryBlobCache()));
```

#### 5. DateTime Handling
```csharp
// Set global DateTime behavior
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithForcedDateTimeKind(DateTimeKind.Utc)
           .WithSqliteDefaults());
```

## Serializers

Akavache V11.0 supports multiple serialization formats with automatic cross-compatibility.

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
<PackageReference Include="Akavache.Drawing" Version="11.0.*" />
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
           .WithSqliteDefaults());

// Register platform-specific bitmap loader using Splat (if needed (Net 8.0+))
Locator.CurrentMutable.RegisterPlatformBitmapLoader();
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
<PackageReference Include="Akavache.Settings" Version="11.0.*" />
```

### Basic Usage

#### 1. Create a Settings Class

```csharp
using Akavache.Settings;

public class AppSettings : SettingsBase
{
    public AppSettings() : base(nameof(AppSettings))
    {
    }

    // Boolean setting with default value
    public bool EnableNotifications
    {
        get => GetOrCreate(true);
        set => SetOrCreate(value);
    }

    // String setting with default value
    public string UserName
    {
        get => GetOrCreate("DefaultUser");
        set => SetOrCreate(value);
    }

    // Numeric settings
    public int MaxRetries
    {
        get => GetOrCreate(3);
        set => SetOrCreate(value);
    }

    public double CacheTimeout
    {
        get => GetOrCreate(30.0);
        set => SetOrCreate(value);
    }

    // Enum setting
    public LogLevel LoggingLevel
    {
        get => GetOrCreate(LogLevel.Information);
        set => SetOrCreate(value);
    }
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error
}
```

#### 2. Initialize Settings Store

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Settings;

// Initialize Akavache with settings support
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder().WithAkavache<SystemJsonSerializer>(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer())
           .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// Now use the settings
appSettings.EnableNotifications = false;
appSettings.UserName = "John Doe";
appSettings.MaxRetries = 5;

Console.WriteLine($"User: {appSettings.UserName}");
Console.WriteLine($"Notifications: {appSettings.EnableNotifications}");
```

### Advanced Configuration

#### Custom Settings Cache Path

By default, settings are stored in a subfolder of your application directory. You can customize this path:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSettingsCachePath(@"C:\MyApp\Settings")  // Custom path
           .WithSettingsStore<AppSettings>(settings => appSettings = settings));
```

#### Multiple Settings Classes

You can create multiple settings classes for different categories:

```csharp
public class UserSettings : SettingsBase
{
    public UserSettings() : base(nameof(UserSettings)) { }
    
    public string Theme
    {
        get => GetOrCreate("Light");
        set => SetOrCreate(value);
    }
}

public class NetworkSettings : SettingsBase
{
    public NetworkSettings() : base(nameof(NetworkSettings)) { }
    
    public int TimeoutSeconds
    {
        get => GetOrCreate(30);
        set => SetOrCreate(value);
    }
}

// Initialize multiple settings
var userSettings = default(UserSettings);
var networkSettings = default(NetworkSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSettingsStore<UserSettings>(settings => userSettings = settings)
           .WithSettingsStore<NetworkSettings>(settings => networkSettings = settings));
```

#### Encrypted Settings

For sensitive settings, use encrypted storage:

```csharp
public class SecureSettings : SettingsBase
{
    public SecureSettings() : base(nameof(SecureSettings)) { }
    
    public string ApiKey
    {
        get => GetOrCreate(string.Empty);
        set => SetOrCreate(value);
    }
    
    public string DatabasePassword
    {
        get => GetOrCreate(string.Empty);
        set => SetOrCreate(value);
    }
}

// Initialize with encryption
var secureSettings = default(SecureSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSecureSettingsStore<SecureSettings>("mySecurePassword", 
               settings => secureSettings = settings));

// Use encrypted settings
secureSettings.ApiKey = "sk-1234567890abcdef";
secureSettings.DatabasePassword = "super-secret-password";
```

#### Override Database Names

You can specify custom database names for settings:

```csharp
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSettingsStore<AppSettings>(
               settings => appSettings = settings, 
               "CustomAppConfig"));  // Custom database name
```

### Complete Example

Here's a comprehensive example showing all data types and features:

```csharp
public class ComprehensiveSettings : SettingsBase
{
    public ComprehensiveSettings() : base(nameof(ComprehensiveSettings))
    {
    }

    // Basic types with defaults
    public bool BoolSetting
    {
        get => GetOrCreate(true);
        set => SetOrCreate(value);
    }

    public byte ByteSetting
    {
        get => GetOrCreate((byte)123);
        set => SetOrCreate(value);
    }

    public short ShortSetting
    {
        get => GetOrCreate((short)16);
        set => SetOrCreate(value);
    }

    public int IntSetting
    {
        get => GetOrCreate(42);
        set => SetOrCreate(value);
    }

    public long LongSetting
    {
        get => GetOrCreate(123456L);
        set => SetOrCreate(value);
    }

    public float FloatSetting
    {
        get => GetOrCreate(2.5f);
        set => SetOrCreate(value);
    }

    public double DoubleSetting
    {
        get => GetOrCreate(3.14159);
        set => SetOrCreate(value);
    }

    public string StringSetting
    {
        get => GetOrCreate("Default Value");
        set => SetOrCreate(value);
    }

    // Nullable types
    public string? NullableStringSetting
    {
        get => GetOrCreate<string?>(null);
        set => SetOrCreate(value);
    }

    // Complex types (automatically serialized)
    public List<string> StringListSetting
    {
        get => GetOrCreate(new List<string> { "Item1", "Item2" });
        set => SetOrCreate(value);
    }

    public Dictionary<string, int> DictionarySetting
    {
        get => GetOrCreate(new Dictionary<string, int> { ["Key1"] = 1, ["Key2"] = 2 });
        set => SetOrCreate(value);
    }

    // Custom objects
    public WindowPosition WindowPosition
    {
        get => GetOrCreate(new WindowPosition { X = 100, Y = 100, Width = 800, Height = 600 });
        set => SetOrCreate(value);
    }
}

public class WindowPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// Usage
var settings = default(ComprehensiveSettings);
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSettingsStore<ComprehensiveSettings>(s => settings = s));

// Use the settings
settings.StringListSetting.Add("Item3");
settings.WindowPosition = new WindowPosition { X = 200, Y = 150, Width = 1024, Height = 768 };
settings.DictionarySetting["NewKey"] = 999;
```

### Settings Lifecycle Management

#### Cleanup on Application Exit

```csharp
// In your application shutdown code
public async Task OnApplicationExit()
{
    var builder = CacheDatabase.Builder;
    
    // Dispose settings stores to ensure data is flushed
    await builder.DisposeSettingsStore<AppSettings>();
    await builder.DisposeSettingsStore<UserSettings>();
    
    // Regular Akavache shutdown
    await CacheDatabase.Shutdown();
}
```

#### Delete Settings (Reset to Defaults)

```csharp
// Delete a specific settings store
var builder = CacheDatabase.Builder;
await builder.DeleteSettingsStore<AppSettings>();

// Settings will be recreated with default values on next access
```

#### Check if Settings Exist

```csharp
var builder = CacheDatabase.Builder;
var existingSettings = builder.GetSettingsStore<AppSettings>();

if (existingSettings != null)
{
    Console.WriteLine("Settings already exist");
}
else
{
    Console.WriteLine("First run - settings will be created with defaults");
}
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
        AppBuilder.CreateSplatBuilder()
            .WithAkavache<SystemJsonSerializer>(cacheBuilder =>
                cacheBuilder.WithApplicationName("MyMauiApp")
                        .WithForceDateTimeKind(DateTimeKind.Utc)
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
        AppBuilder.CreateSplatBuilder()
            .WithAkavache<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyWpfApp")
                   .WithForceDateTimeKind(DateTimeKind.Utc)
                   .WithSqliteDefaults());
    }
}
```

### iOS Specific

```csharp
// In AppDelegate.cs or SceneDelegate.cs
public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>(builder =>
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

    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyAndroidApp")
               .WithSqliteDefaults());
}
```

### UWP Applications

```csharp
// In App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>(builder =>
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

### Performance Reports

For comprehensive performance analysis and V10 vs V11 comparison:

- 📊 **[Performance Summary](src/PERFORMANCE_SUMMARY.md)** - Quick comparison and migration decision matrix
- 📈 **[Comprehensive Benchmark Report](src/BENCHMARK_REPORT.md)** - Detailed performance analysis, architectural differences, and recommendations

### Performance Tips

```csharp
// 1. Use System.Text.Json for best performance
.WithSerializer<SystemJsonSerializer>();

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
        AppBuilder.CreateSplatBuilder()
            .WithAkavache<SystemJsonSerializer>(builder =>
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
    CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
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
// Fix: Register a suitable serializer during initialization
CacheDatabase.Initialize<SystemJsonSerializer>(/* ... */);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(/* ... */);

AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(/* ... */);
```

#### 2. "CacheDatabase has not been initialized"
```csharp
// Fix: Call Initialize before using cache
CacheDatabase.Initialize<SystemJsonSerializer>(builder => builder.WithApplicationName("MyApp").WithInMemoryDefaults());
var data = await CacheDatabase.UserAccount.GetObject<MyData>("key");
```

#### 3. Data compatibility issues
```csharp
// Fix: Use cross-compatible serializer or migration
CacheDatabase.Initialize<NewtonsoftBsonSerializer>(/* ... */); // Most compatible
```

#### 4. SQLite errors on mobile
```xml
// Fix: Ensure SQLitePCL.raw bundle is installed
// Add to your project:
<ItemGroup>
    <PackageReference Include="SQLitePCLRaw.lib.e_sqlite3" Version="2.1.11" />
    
// If using Encrypted SQLite, also add:
    <PackageReference Include="SQLitePCLRaw.lib.e_sqlcipher" Version="2.1.11" />
</ItemGroup>

```

#### 5. Linker removing types IL2104

You will need to preserve certain types to prevent the linker from stripping them out in release builds.

```xml
// Add to your .csproj file:
<ItemGroup>
  <TrimmerRootAssembly Include="SQLitePCLRaw.lib.e_sqlite3.## YOUR-PLATFORM ##" RootMode="All" />
</ItemGroup>

### Platform-Specific Issues

#### iOS Linker Issues
```csharp
// Add LinkerPreserve.cs to your iOS project:
public static class LinkerPreserve
{
    static LinkerPreserve()
    {
        var sqliteBlobCachetName = typeof(SqliteBlobCache).FullName;
        var encryptedSqliteBlobCacheName = typeof(EncryptedSqliteBlobCache).FullName;
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
