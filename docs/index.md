# Akavache Documentation

Welcome to the Akavache documentation! This guide will help you get the most out of Akavache V11.1, the asynchronous key-value store for .NET applications.

## Quick Navigation

### Getting Started
- **[Installation](./installation.md)** - Package matrix and installation guide
- **[Configuration](./configuration.md)** - Builder pattern, DI, and providers
- **[Migration from V10.x](./migration-v10-to-v11.md)** - Upgrade your existing applications

### Core Concepts  
- **[Serializers](./serializers.md)** - System.Text.Json vs Newtonsoft.Json, BSON options  
- **[Cache Types](./cache-types.md)** - UserAccount, LocalMachine, Secure, and InMemory  
- **[Basic Operations](./basic-operations.md)** - Store, retrieve, update, and delete data  
- **[HTTP Operations](./http-operations.md)** - Caching URLs and managing logins  
- **[Drawing](./drawing.md)** - Storing and manipulating bitmaps  
- **[Settings](./settings.md)** - Akavache.Settings for application configuration and user preferences  
- **[Advanced Features](./advanced-features.md)** - Expiration updates, schedulers, and cache inspection

### Advanced Topics
- **[Platform Notes](./platform-notes.md)** - iOS, Android, MAUI, WinUI, and Windows specifics
- **[Performance](./performance.md)** - Optimization tips and benchmarks
- **[Best Practices](./best-practices.md)** - Recommended patterns and conventions

### Patterns and Examples
- **[Get and Fetch Latest](./patterns/get-and-fetch-latest.md)** - The right way to implement cache-aside pattern
- **[Cache Deletion](./patterns/cache-deletion.md)** - Safe cache deletion and key access patterns

### Troubleshooting
- **[Troubleshooting Guide](./troubleshooting/troubleshooting-guide.md)** - Common issues and solutions
- **[Issue 313 Fix](./troubleshooting/issue-313-cache-deletion-fix.md)** - Exception-safe key enumeration

## What is Akavache?

Akavache is an asynchronous, persistent key-value store for .NET applications. It's designed to make caching simple, reliable, and performant across desktop and mobile platforms.

### Key Features
- **🔄 Asynchronous API** - All operations are async/await friendly
- **💾 Persistent Storage** - SQLite-based with optional encryption
- **📱 Cross-Platform** - Works on iOS, Android, Windows, macOS, and Linux
- **🚀 High Performance** - Optimized for speed with modern .NET
- **🔒 Secure** - Built-in encryption for sensitive data
- **🧩 Modular** - Install only the packages you need

## Quick Start

1. **Install packages:**
   ```xml
   <PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
   <PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
   ```

2. **Initialize Akavache:**
   ```csharp
   AppBuilder.CreateSplatBuilder()
       .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
           builder.WithApplicationName("MyApp")
                  .WithSqliteProvider()
                  .WithSqliteDefaults());
   ```

3. **Use the cache:**
   ```csharp
   // Store data
   await CacheDatabase.UserAccount.InsertObject("user_profile", userProfile);
   
   // Retrieve data
   var profile = await CacheDatabase.UserAccount.GetObject<UserProfile>("user_profile");
   
   // Cache-aside pattern
   var data = await CacheDatabase.LocalMachine.GetOrFetchObject("api_data",
       () => httpClient.GetFromJsonAsync<ApiData>("https://api.example.com/data"),
       TimeSpan.FromMinutes(30));
   ```

## Architecture Overview

Akavache V11.1 introduces a modular architecture with several key components:

### Packages
- **Akavache.Core** - Foundation interfaces and base implementations
- **Akavache.Sqlite3** - SQLite-based persistent cache
- **Akavache.EncryptedSqlite3** - Encrypted persistent cache
- **Akavache.SystemTextJson** - Modern JSON serialization (recommended)
- **Akavache.NewtonsoftJson** - Legacy JSON serialization
- **Akavache.Drawing** - Image/bitmap caching support
- **Akavache.Settings** - Configuration and settings management

### Cache Types
- **UserAccount** - User-specific persistent data
- **LocalMachine** - Application-wide persistent data  
- **Secure** - Encrypted storage for sensitive data
- **InMemory** - Fast temporary storage

### Serialization Options
- **System.Text.Json** - High performance, .NET native (recommended)
- **Newtonsoft.Json** - Maximum compatibility, rich features
- **BSON variants** - Binary serialization for performance

## What's New in V11.1

### Builder Pattern
New fluent API for initialization and configuration:
```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### Multiple Serializer Support
Choose the best serializer for your needs:
- System.Text.Json for performance
- Newtonsoft.Json for compatibility
- BSON variants for binary efficiency

### Cross-Serializer Compatibility
Read data written by different serializers - enables gradual migration and maximum flexibility.

### Enhanced Performance
- Optimized bulk operations
- Efficient expiration updates
- Better memory management
- Platform-specific optimizations

## Community and Support

- **📖 Documentation**: [GitHub Repository](https://github.com/reactiveui/Akavache)
- **🐛 Bug Reports**: [GitHub Issues](https://github.com/reactiveui/Akavache/issues)
- **💬 Community Chat**: [ReactiveUI Slack](https://reactiveui.net/slack)
- **📦 NuGet Packages**: [Akavache on NuGet](https://www.nuget.org/packages?q=akavache)
- **🎯 Contributing**: [Contributing Guide](https://github.com/reactiveui/Akavache/blob/main/CONTRIBUTING.md)

## Learn More

### For Beginners
Start with the [Installation Guide](./installation.md) and [Basic Operations](./basic-operations.md) to get up and running quickly.

### For Existing Users
Check out the [Migration Guide](./migration-v10-to-v11.md) to upgrade from V10.x, or explore [Performance Optimization](./performance.md) tips.

### For Advanced Users
Dive into [Platform-Specific Notes](./platform-notes.md), [Best Practices](./best-practices.md), and advanced [Patterns](./patterns/).

### Need Help?
Visit the [Troubleshooting Guide](./troubleshooting/troubleshooting-guide.md) for common issues and solutions.

---

*Happy caching! 🚀*
