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

Akavache is an *asynchronous*, *persistent* (i.e., writes to disk) key-value store created for writing desktop and mobile applications in C#, based on SQLite3. Akavache is great for both storing important data (i.e., user settings) as well as cached local data that expires.

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

**Key Development Milestones:**

- **🧪 Incubation Phase**: The builder pattern, modular serialization system, and enhanced API were first developed and tested in ReactiveMarbles.CacheDatabase
- **🔬 Community Testing**: Early adopters and contributors provided valuable feedback on the new architecture through real-world usage scenarios
- **🚀 Production Validation**: The incubator project allowed us to validate performance improvements, API ergonomics, and cross-platform compatibility before integrating into Akavache
- **📈 Iterative Refinement**: Multiple iterations based on community feedback helped shape the final V11.1 API design and feature set

This careful incubation process ensured that V11.1 delivers not just new features, but a more robust, flexible, and maintainable caching solution that builds upon years of community experience and testing. The ReactiveMarbles organization continues to serve as a proving ground for innovative reactive programming concepts that eventually make their way into the broader ReactiveUI ecosystem.

## Quick Start

Get up and running with Akavache in minutes. For detailed guides, see the [full documentation](./docs/).

### 1. Install Packages

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### 2. Initialize Akavache

> **Note:** 
> `WithAkavacheCacheDatabase` always requires an `ISerializer` defined as a generic type, such as `WithAkavacheCacheDatabase<SystemJsonSerializer>`. This ensures the cache instance is properly configured for serialization.

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

> **Important:** Always call `WithSqliteProvider()` explicitly before `WithSqliteDefaults()`. While `WithSqliteDefaults()` will automatically call `WithSqliteProvider()` if not already initialized (for backward compatibility), this automatic behavior is **deprecated and may be removed in future versions**. Explicit provider initialization is the recommended pattern for forward compatibility with other DI containers.

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

## Core API Cheatsheet

Here are the most common operations you'll use daily:

### Store Data
```csharp
// Store with expiration
await CacheDatabase.UserAccount.InsertObject("user_profile", userProfile, TimeSpan.FromHours(4));

// Store permanently (until manually removed)
await CacheDatabase.UserAccount.InsertObject("user_settings", userSettings);

// Bulk operations (much faster for multiple items)
var items = new Dictionary<string, UserData>
{
    ["user_1"] = userData1,
    ["user_2"] = userData2
};
await CacheDatabase.UserAccount.InsertObjects(items, TimeSpan.FromDays(1));
```

### Retrieve Data
```csharp
// Get cached data
var profile = await CacheDatabase.UserAccount.GetObject<UserProfile>("user_profile");

// Cache-aside pattern (get from cache or fetch from source)
var apiData = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data",
    () => httpClient.GetFromJsonAsync<ApiData>("https://api.example.com/data"),
    TimeSpan.FromMinutes(30));

// Safe retrieval (no exceptions)
var result = await CacheDatabase.UserAccount.TryGetObject<UserProfile>("user_profile");
if (result.HasValue)
{
    var profile = result.Value;
}
```

### Manage Data
```csharp
// Remove specific items
await CacheDatabase.UserAccount.Invalidate("user_profile");

// Extend expiration without re-writing data (fast!)
await CacheDatabase.UserAccount.UpdateExpiration("api_data", TimeSpan.FromHours(2));

// Clear all data
await CacheDatabase.InMemory.InvalidateAll();
```

### Cache Types Quick Reference
```csharp
// User-specific data (per user, persistent)
await CacheDatabase.UserAccount.InsertObject("preferences", userPrefs);

// App-wide data (shared, persistent)  
await CacheDatabase.LocalMachine.InsertObject("app_config", config);

// Sensitive data (encrypted, persistent)
await CacheDatabase.Secure.InsertObject("auth_token", token);

// Temporary data (fast, lost on restart)
await CacheDatabase.InMemory.InsertObject("session_data", sessionData);
```

## Settings Management (Akavache.Settings)

Akavache.Settings provides persistent, type-safe settings storage that's critical for most applications:

### Install Settings Package
```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

### Define Settings Class
```csharp
using Akavache.Settings;

public class AppSettings : ISettingsStorage
{
    public string ApiEndpoint { get; set; } = "https://api.example.com";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### Configure Settings Store
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>("MyApp",
        builder => builder.WithSqliteProvider().WithSqliteDefaults(),
        instance =>
        {
            // Add settings store
            instance.WithSettingsStore<AppSettings>(settings =>
            {
                settings.ApiEndpoint = "https://api.production.com";
                settings.EnableLogging = true;
            });
        });
```

### Use Settings in Your App
```csharp
// Read settings
var settings = akavacheInstance.GetLoadedSettingsStore<AppSettings>();
var apiUrl = settings.ApiEndpoint;

// Update settings (automatically persisted)
settings.TimeoutSeconds = 60;
settings.EnableLogging = false;

// Encrypted settings for sensitive data
instance.WithSecureSettingsStore<SecureSettings>("password", secureSettings =>
{
    secureSettings.ApiKey = "secret-key";
});
```

> **See the complete [Settings Guide](./docs/settings.md)** for advanced scenarios, encrypted settings, dependency injection patterns, and framework-specific examples.

## Further Reading

The complete documentation is available in the [docs folder](./docs/):

### Getting Started
- **[Installation](./docs/installation.md)** - Package matrix and which packages to choose
- **[Configuration](./docs/configuration.md)** - Builder pattern, DI, and provider setup
- **[Migration from V10.x](./docs/migration-v10-to-v11.md)** - Upgrade your existing applications

### Core Concepts  
- **[Serializers](./docs/serializers.md)** - System.Text.Json vs Newtonsoft.Json, BSON options
- **[Cache Types](./docs/cache-types.md)** - UserAccount, LocalMachine, Secure, and InMemory
- **[Basic Operations](./docs/basic-operations.md)** - Complete guide to storing, retrieving, and managing data

### Advanced Topics
- **[Platform Notes](./docs/platform-notes.md)** - iOS, Android, MAUI, UWP, and Windows specifics
- **[Performance](./docs/performance.md)** - Optimization tips and benchmarks  
- **[Best Practices](./docs/best-practices.md)** - Recommended patterns and conventions

### Patterns and Examples
- **[Get and Fetch Latest](./docs/patterns/get-and-fetch-latest.md)** - The right way to implement cache-aside pattern
- **[Cache Deletion](./docs/patterns/cache-deletion.md)** - Safe cache deletion and key access patterns

### Help and Troubleshooting
- **[Troubleshooting Guide](./docs/troubleshooting/troubleshooting-guide.md)** - Common issues and solutions
- **[Issue 313 Fix](./docs/troubleshooting/issue-313-cache-deletion-fix.md)** - Exception-safe key enumeration

## Support and Contributing

- 📖 **Documentation**: [https://github.com/reactiveui/Akavache](https://github.com/reactiveui/Akavache)
- 🐛 **Issues**: [GitHub Issues](https://github.com/reactiveui/Akavache/issues)
- 💬 **Chat**: [ReactiveUI Slack](https://reactiveui.net/slack)
- 📦 **NuGet**: [Akavache Packages](https://www.nuget.org/packages?q=akavache)

## Thanks 

This project is tested with BrowserStack.

## License

Akavache is licensed under the [MIT License](LICENSE).