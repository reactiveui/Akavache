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

# Akavache: An Asynchronous Key-Value Store for Native Applications

Akavache is an *asynchronous*, *persistent* (i.e., writes to disk) key-value store created for writing desktop and mobile applications in C#, based on SQLite3. Akavache is great for both storing important data (i.e., user settings) as well as cached local data that expires.

## What's New in V12

Akavache V12 rewrites the SQLite backend for direct SQLitePCLRaw 3.x access, replacing the sqlite-net-pcl ORM layer entirely. The result is lower per-operation allocations, dedicated worker-thread serialization of all native handle access, and commit coalescing for concurrent writes.

- **SQLitePCLRaw direct access**: Prepared statements cached and reused, parameters bound positionally — no ORM overhead
- **SQLite3MultipleCiphers**: Encrypted databases use SQLite3MC instead of sqlcipher
- **Observable-first settings**: `SettingsBase` properties are `IObservable<T>` — no more `.Wait()` deadlocks
- **AOT-safe serialization**: `JsonTypeInfo<T>` overloads for System.Text.Json, trim-safe out of the box
- **System.Text.Json package split**: Pure JSON package no longer pulls in Newtonsoft.Json
- **Thread-safe disposal**: All cache types use lock-free `Interlocked` patterns for idempotent dispose

See the **[Migration Guide: V11 to V12](docs/migration-v11-to-v12.md)** for upgrade instructions.

## Quick Start

### 1. Install Packages

```xml
<PackageReference Include="Akavache.Sqlite3" Version="*" />
<PackageReference Include="Akavache.SystemTextJson" Version="*" />
```

### 2. Initialize Akavache

> **Note:** `WithAkavache`, `WithAkavacheCacheDatabase` and `Initialize` always requires an `ISerializer` defined as a generic type, such as `WithAkavache<SystemJsonSerializer>`. This ensures the cache instance is properly configured for serialization.

#### Static Initialization (Recommended for most apps)
```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;
using Splat.Builder;

// Initialize with the builder pattern
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithSqliteProvider() // REQUIRED: Explicitly initialize SQLite provider
               .WithSqliteDefaults(),
            "MyApp");
```

> **Important:** Always call `WithSqliteProvider()` explicitly before `WithSqliteDefaults()`. While `WithSqliteDefaults()` will automatically call `WithSqliteProvider()` if not already initialized (for backward compatibility), this automatic behavior is **deprecated and may be removed in future versions**. Explicit provider initialization is the recommended pattern for forward compatibility with other DI containers.

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
        builder => builder.WithSqliteProvider()    // REQUIRED: Explicit provider initialization
                          .WithSqliteDefaults(),
        (splat, instance) => splat.RegisterLazySingleton(() => instance));

// For in-memory cache (testing or lightweight scenarios):
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(
        "Akavache",
        builder => builder.WithInMemoryDefaults(),  // No provider needed for in-memory
        (splat, instance) => splat.RegisterLazySingleton(() => instance));
```

### 3. Use the Cache

#### Basic Operations

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

#### Cache Types

Akavache provides four types of caches:

- **UserAccount**: User settings and preferences that should persist and potentially sync
- **LocalMachine**: Cached data that can be safely deleted by the system
- **Secure**: Encrypted storage for sensitive data like credentials and API keys
- **InMemory**: Temporary storage that doesn't persist between app sessions

```csharp
// User preferences (persistent)
await CacheDatabase.UserAccount.InsertObject("user_settings", settings);

// API cache (temporary)
await CacheDatabase.LocalMachine.InsertObject("api_cache", apiData, DateTimeOffset.Now.AddHours(6));

// Sensitive data (encrypted)
await CacheDatabase.Secure.SaveLogin("john.doe", "secretPassword", "myapp.com");

// Session data (in-memory only)
await CacheDatabase.InMemory.InsertObject("current_session", sessionData);
```

## NuGet Packages

Install the packages that match your needs. At minimum you need the core package plus a storage backend and a serializer.

| Purpose | Package | NuGet |
| ------- | ------- | ----- |
| Core (in-memory cache) | [Akavache][AkavacheDoc] | [![AkavacheBadge]][AkavacheNuGet] |
| SQLite persistence | [Akavache.Sqlite3][Sqlite3Doc] | [![Sqlite3Badge]][Sqlite3NuGet] |
| Encrypted SQLite persistence | [Akavache.EncryptedSqlite3][EncryptedDoc] | [![EncryptedBadge]][EncryptedNuGet] |
| System.Text.Json serializer (recommended) | [Akavache.SystemTextJson][STJDoc] | [![STJBadge]][STJNuGet] |
| System.Text.Json BSON serializer | [Akavache.SystemTextJson.Bson][STJBsonDoc] | [![STJBsonBadge]][STJBsonNuGet] |
| Newtonsoft.Json serializer | [Akavache.NewtonsoftJson][NewtonsoftDoc] | [![NewtonsoftBadge]][NewtonsoftNuGet] |
| HTTP download and caching extensions | [Akavache.HttpDownloader][HttpDoc] | [![HttpBadge]][HttpNuGet] |
| Image/bitmap caching | [Akavache.Drawing][DrawingDoc] | [![DrawingBadge]][DrawingNuGet] |
| Application settings helpers | [Akavache.Settings][SettingsDoc] | [![SettingsBadge]][SettingsNuGet] |
| V10 → V11 data migration | [Akavache.V10toV11][MigrationDoc] | [![MigrationBadge]][MigrationNuGet] |

[AkavacheNuGet]: https://www.nuget.org/packages/Akavache/
[AkavacheBadge]: https://img.shields.io/nuget/v/Akavache.svg
[AkavacheDoc]: https://github.com/reactiveui/Akavache

[Sqlite3NuGet]: https://www.nuget.org/packages/Akavache.Sqlite3/
[Sqlite3Badge]: https://img.shields.io/nuget/v/Akavache.Sqlite3.svg
[Sqlite3Doc]: https://github.com/reactiveui/Akavache

[EncryptedNuGet]: https://www.nuget.org/packages/Akavache.EncryptedSqlite3/
[EncryptedBadge]: https://img.shields.io/nuget/v/Akavache.EncryptedSqlite3.svg
[EncryptedDoc]: https://github.com/reactiveui/Akavache

[STJNuGet]: https://www.nuget.org/packages/Akavache.SystemTextJson/
[STJBadge]: https://img.shields.io/nuget/v/Akavache.SystemTextJson.svg
[STJDoc]: https://github.com/reactiveui/Akavache

[STJBsonNuGet]: https://www.nuget.org/packages/Akavache.SystemTextJson.Bson/
[STJBsonBadge]: https://img.shields.io/nuget/v/Akavache.SystemTextJson.Bson.svg
[STJBsonDoc]: https://github.com/reactiveui/Akavache

[NewtonsoftNuGet]: https://www.nuget.org/packages/Akavache.NewtonsoftJson/
[NewtonsoftBadge]: https://img.shields.io/nuget/v/Akavache.NewtonsoftJson.svg
[NewtonsoftDoc]: https://github.com/reactiveui/Akavache

[HttpNuGet]: https://www.nuget.org/packages/Akavache.HttpDownloader/
[HttpBadge]: https://img.shields.io/nuget/v/Akavache.HttpDownloader.svg
[HttpDoc]: https://github.com/reactiveui/Akavache

[DrawingNuGet]: https://www.nuget.org/packages/Akavache.Drawing/
[DrawingBadge]: https://img.shields.io/nuget/v/Akavache.Drawing.svg
[DrawingDoc]: https://github.com/reactiveui/Akavache

[SettingsNuGet]: https://www.nuget.org/packages/Akavache.Settings/
[SettingsBadge]: https://img.shields.io/nuget/v/Akavache.Settings.svg
[SettingsDoc]: https://github.com/reactiveui/Akavache

[MigrationNuGet]: https://www.nuget.org/packages/Akavache.V10toV11/
[MigrationBadge]: https://img.shields.io/nuget/v/Akavache.V10toV11.svg
[MigrationDoc]: https://github.com/reactiveui/Akavache/blob/main/docs/migration-v10-to-v11.md

## Installation

Akavache uses a modular package structure. Choose the packages that match your needs:

### Core Package (In Memory only)
```xml
<PackageReference Include="Akavache" Version="*" />
```

### Storage Backends (Choose One - Recommended)
```xml
<!-- SQLite persistence (most common) -->
<PackageReference Include="Akavache.Sqlite3" Version="*" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="*" />
```

### Serializers (Choose One - Required)
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="*" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="*" />
```

### Optional Extensions
```xml
<!-- HTTP download and caching extensions -->
<PackageReference Include="Akavache.HttpDownloader" Version="*" />

<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="*" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="*" />
```

## Framework Support

Akavache supports:

- ✅ **.NET Framework 4.6.2/4.7.2** - Windows desktop applications
- ✅ **.NET Standard 2.0** - Cross-platform libraries
- ✅ **.NET 8.0** - Modern .NET applications
- ✅ **.NET 9.0** - Latest .NET applications
- ✅ **.NET 10.0** - Latest .NET applications
- ✅ **Mobile Targets** - `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`
- ✅ **Desktop Targets** - `net9.0-windows10.0.19041.0`, `net10.0-windows10.0.19041.0` (WinUI), `net9.0`, `net10.0` (cross-platform)

### Serializer Compatibility

| Serializer | .NET Framework 4.6.2+ | .NET 8.0+ | Mobile | Performance |
|------------|------------------------|-------------------|------------|--------|
| **System.Text.Json** | ✅ Via NuGet | ✅ | ✅ | **Fastest** |
| **Newtonsoft.Json** | ✅ Built-in | ✅ | ✅ | Compatible |

**Recommendation**: Use **System.Text.Json** for new projects for best performance. Use **Newtonsoft.Json** when migrating from older Akavache versions or when you need maximum compatibility.

## Akavache.Settings: Configuration Made Easy

Akavache.Settings provides a specialized settings database for application configuration that survives app updates and reinstalls.

### Quick Settings Example

```csharp
using Akavache.Settings;

// 1. Create a settings class — properties are IObservable<T>
public class AppSettings : SettingsBase
{
    public AppSettings() : base(nameof(AppSettings)) { }

    public IObservable<bool> EnableNotifications => GetOrCreateObservable(true);
    public IObservable<Unit> SetEnableNotifications(bool value) => SetObservable(value, nameof(EnableNotifications));

    public IObservable<string> UserName => GetOrCreateObservable("DefaultUser");
    public IObservable<Unit> SetUserName(string value) => SetObservable(value, nameof(UserName));
}

// 2. Initialize with your app
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// 3. Use the settings — subscribe for live updates or read once
await appSettings.SetUserName("John Doe");
await appSettings.SetEnableNotifications(false);

var name = await appSettings.UserName.FirstAsync();
Console.WriteLine($"User: {name}");
```

Settings are automatically persisted and will survive app updates, making them perfect for user preferences and application configuration.

## Documentation

📚 **Complete documentation is available in the [/docs](docs/) folder:**

- **[Installation Guide](docs/installation.md)** - Detailed installation and package selection
- **[Configuration](docs/configuration.md)** - Builder pattern, providers, and advanced setup
- **[Serializers](docs/serializers.md)** - System.Text.Json vs Newtonsoft.Json comparison
- **[Cache Types](docs/cache-types.md)** - UserAccount, LocalMachine, Secure, and InMemory caches
- **[Basic Operations](docs/basic-operations.md)** - CRUD operations and error handling
- **[Migration: V10 → V11](docs/migration-v10-to-v11.md)** - Upgrading from V10.x to V11.x
- **[Migration: V11 → V12](docs/migration-v11-to-v12.md)** - Upgrading from V11.x to V12.x
- **[Settings Management](docs/settings.md)** - Complete Akavache.Settings guide
- **[Platform Notes](docs/platform-notes.md)** - Platform-specific guidance
- **[Performance](docs/performance.md)** - Benchmarks and optimization tips
- **[Best Practices](docs/best-practices.md)** - Recommended patterns and anti-patterns
- **[Troubleshooting](docs/troubleshooting/)** - Common issues and solutions

## Support and Contributing

- 📖 **Documentation**: [https://github.com/reactiveui/Akavache](https://github.com/reactiveui/Akavache)
- 🐛 **Issues**: [GitHub Issues](https://github.com/reactiveui/Akavache/issues)
- 💬 **Chat**: [ReactiveUI Slack](https://reactiveui.net/slack)
- 📦 **NuGet**: [Akavache Packages](https://www.nuget.org/packages?q=akavache)

## Thanks 

This project is tested with BrowserStack.

We want to thank the following contributors and libraries that help make Akavache possible:

### Core Libraries

- **SQLite**: [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw) and [SQLite3MultipleCiphers](https://github.com/nicola-decao/SQLite3MultipleCiphers) - SQLite access and encryption for .NET
- **System.Reactive**: [Reactive Extensions for .NET](https://github.com/dotnet/reactive) - The foundation of Akavache's asynchronous API
- **Splat**: [Splat](https://github.com/reactiveui/splat) - Cross-platform utilities and service location
- **System.Text.Json**: Microsoft's high-performance JSON serializer
- **Newtonsoft.Json**: James Newton-King's Json.NET - The most popular .NET JSON library

### Microsoft

<a href="https://dotnetfoundation.org">
  <img src="https://theme.dotnetfoundation.org/img/logo.svg" width="100" />
</a>

We thank Microsoft for their ongoing support of the .NET ecosystem and the development tools that make Akavache possible.

## License

Akavache is licensed under the [MIT License](LICENSE).
