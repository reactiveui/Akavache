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

## Basic Settings Usage

Akavache.Settings provides persistent application settings that survive reinstalls:

### 1. Install Settings Package

```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

### 2. Create Settings Class

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