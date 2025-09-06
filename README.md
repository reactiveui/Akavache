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

This careful incubation process ensured that V11.1 delivers not just new features, but a more robust, flexible, and maintainable caching solution that builds upon years of community experience and testing.

## Quick Start

### 1. Install Packages

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
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
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider() // REQUIRED: Explicitly initialize SQLite provider
               .WithSqliteDefaults());
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

## Installation

Akavache V11.1 uses a modular package structure. Choose the packages that match your needs:

### Core Package (In Memory only)
```xml
<PackageReference Include="Akavache" Version="11.1.*" />
```

### Storage Backends (Choose One - Recommended)
```xml
<!-- SQLite persistence (most common) -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
```

### Serializers (Choose One - Required)
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```

### Optional Extensions
```xml
<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

## Framework Support

Akavache V11.1 supports:

- ✅ **.NET Framework 4.6.2/4.7.2** - Windows desktop applications
- ✅ **.NET Standard 2.0** - Cross-platform libraries
- ✅ **.NET 8.0** - Modern .NET applications
- ✅ **.NET 9.0** - Latest .NET applications
- ✅ **Mobile Targets** - `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`
- ✅ **Desktop Targets** - `net9.0-windows` (WinUI), `net9.0` (cross-platform)

### Serializer Compatibility

| Serializer | .NET Framework 4.6.2+ | .NET Standard 2.0 | .NET 8.0+ | Mobile | Performance |
|------------|------------------------|-------------------|------------|--------|-------------|
| **System.Text.Json** | ✅ Via NuGet | ✅ | ✅ | ✅ | **Fastest** |
| **Newtonsoft.Json** | ✅ Built-in | ✅ | ✅ | ✅ | Compatible |

**Recommendation**: Use **System.Text.Json** for new projects for best performance. Use **Newtonsoft.Json** when migrating from older Akavache versions or when you need maximum compatibility.

## Akavache.Settings: Configuration Made Easy

Akavache.Settings provides a specialized settings database for application configuration that survives app updates and reinstalls.

### Quick Settings Example

```csharp
using Akavache.Settings;

// 1. Create a settings class
public class AppSettings : SettingsBase
{
    public AppSettings() : base(nameof(AppSettings)) { }

    public bool EnableNotifications
    {
        get => GetOrCreate(true);  // Default: true
        set => SetOrCreate(value);
    }

    public string UserName
    {
        get => GetOrCreate("DefaultUser");
        set => SetOrCreate(value);
    }

    public int MaxRetries
    {
        get => GetOrCreate(3);
        set => SetOrCreate(value);
    }
}

// 2. Initialize with your app
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// 3. Use the settings
appSettings.EnableNotifications = false;
appSettings.UserName = "John Doe";
appSettings.MaxRetries = 5;

Console.WriteLine($"User: {appSettings.UserName}");
Console.WriteLine($"Notifications: {appSettings.EnableNotifications}");
```

Settings are automatically persisted and will survive app updates, making them perfect for user preferences and application configuration.

## Documentation

📚 **Complete documentation is available in the [/docs](docs/) folder:**

- **[Installation Guide](docs/installation.md)** - Detailed installation and package selection
- **[Configuration](docs/configuration.md)** - Builder pattern, providers, and advanced setup
- **[Serializers](docs/serializers.md)** - System.Text.Json vs Newtonsoft.Json comparison
- **[Cache Types](docs/cache-types.md)** - UserAccount, LocalMachine, Secure, and InMemory caches
- **[Basic Operations](docs/basic-operations.md)** - CRUD operations and error handling
- **[Migration Guide](docs/migration-v10-to-v11.md)** - Upgrading from V10.x to V11.1
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

- **SQLite**: [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) and [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw) - Essential SQLite support for .NET
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