# Akavache.Settings Guide

Akavache.Settings provides a powerful settings storage system that extends Akavache's caching capabilities to handle application configuration, user preferences, and persistent settings with type safety and encryption support.

## Quick Start

### 1. Install the Package

```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
<!-- Also need one of the core packages -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### 2. Define Your Settings Class

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

### 3. Configure Settings Store

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Settings;

// Initialize Akavache with settings support
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder().WithAkavache<SystemJsonSerializer>(builder =>
    builder.WithApplicationName("MyApp")
           .WithSerializer(new SystemJsonSerializer())
           .WithSqliteProvider()
           .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// Now use the settings
appSettings.EnableNotifications = false;
appSettings.UserName = "John Doe";
appSettings.MaxRetries = 5;

Console.WriteLine($"User: {appSettings.UserName}");
Console.WriteLine($"Notifications: {appSettings.EnableNotifications}");
```

### 4. Use Settings in Your Application

```csharp
public class MyService
{
    private readonly AppSettings _settings;
    
    public MyService(AppSettings settings)
    {
        _settings = settings;
    }
    
    public void UpdateUserSettings(string newUserName)
    {
        _settings.UserName = newUserName;
        // Settings are automatically persisted
    }
    
    public void ConfigureApp()
    {
        if (_settings.EnableNotifications)
        {
            // Configure notifications
        }
        
        var retryPolicy = CreateRetryPolicy(_settings.MaxRetries);
    }
}
```

## Core Concepts

### ISettingsStorage Interface

All settings classes must implement `ISettingsStorage`:

```csharp
public interface ISettingsStorage : IDisposable, IAsyncDisposable
{
    // No additional members required - just the disposal methods
}
```

### Settings Store Lifecycle

1. **Creation** - Settings stores are created per type
2. **Persistence** - Changes are automatically saved to SQLite
3. **Loading** - Settings are loaded from storage on first access
4. **Disposal** - Proper cleanup when settings are no longer needed

## Configuration Methods

### WithSettingsStore<T> - Standard Settings

```csharp
builder.WithSettingsStore<AppSettings>(settings =>
{
    // Configure initial values
    settings.DatabaseConnectionString = "Server=localhost;Database=MyApp";
    settings.MaxRetryCount = 3;
});
```

### WithSecureSettingsStore<T> - Encrypted Settings

```csharp
builder.WithSecureSettingsStore<SecureSettings>("mySecretPassword", settings =>
{
    // Configure encrypted settings
    settings.ApiKey = "sensitive-api-key";
    settings.DatabasePassword = "secret-password";
});
```

### WithSettingsCachePath - Custom Storage Location

```csharp
builder.WithSettingsCachePath("/custom/settings/path")
       .WithSettingsStore<AppSettings>(settings => { });
```

## Working with Settings

### Reading Settings

```csharp
// Get loaded settings store
var settings = akavacheInstance.GetLoadedSettingsStore<AppSettings>();
if (settings != null)
{
    var apiEndpoint = settings.ApiEndpoint;
    var timeout = settings.TimeoutSeconds;
}

// Or get/create settings store
var settings = akavacheInstance.GetSettingsStore<AppSettings>();
```

### Updating Settings

```csharp
var settings = akavacheInstance.GetSettingsStore<AppSettings>();
if (settings != null)
{
    settings.EnableLogging = false;
    settings.RecentFiles.Add("/path/to/new/file");
    // Changes are automatically persisted
}
```

### Settings with Custom Database Names

```csharp
// Use custom database name instead of type name
builder.WithSettingsStore<AppSettings>(
    settings => { /* configure */ }, 
    overrideDatabaseName: "MyCustomSettingsDb");

// Access with custom name
var settings = akavacheInstance.GetSettingsStore<AppSettings>("MyCustomSettingsDb");
```

## Advanced Scenarios

### Multiple Settings Types

```csharp
public class UserPreferences : ISettingsStorage
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public int FontSize { get; set; } = 12;
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class ConnectionSettings : ISettingsStorage  
{
    public string ServerUrl { get; set; } = "";
    public int Port { get; set; } = 443;
    public bool UseSsl { get; set; } = true;
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Configure multiple settings types
instance.WithSettingsStore<UserPreferences>(prefs => prefs.Theme = "Light");
instance.WithSecureSettingsStore<ConnectionSettings>("password", conn => conn.UseSsl = true);
```

### Settings Factory Pattern

```csharp
public class SettingsFactory
{
    private readonly IAkavacheInstance _cache;
    
    public SettingsFactory(IAkavacheInstance cache)
    {
        _cache = cache;
    }
    
    public T GetSettings<T>() where T : ISettingsStorage, new()
    {
        return _cache.GetSettingsStore<T>() ?? throw new InvalidOperationException($"Settings {typeof(T).Name} not configured");
    }
    
    public T GetSecureSettings<T>(string password) where T : ISettingsStorage, new()
    {
        return _cache.GetSecureSettingsStore<T>(password) ?? throw new InvalidOperationException($"Secure settings {typeof(T).Name} not configured");
    }
}
```

### Settings Validation

```csharp
public class ValidatedSettings : ISettingsStorage
{
    private string _apiEndpoint = "";
    
    public string ApiEndpoint 
    { 
        get => _apiEndpoint;
        set 
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("API endpoint cannot be empty");
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                throw new ArgumentException("API endpoint must be a valid URL");
            _apiEndpoint = value;
        }
    }
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Settings Store Management

### Checking if Settings Exist

```csharp
var settings = akavacheInstance.GetLoadedSettingsStore<AppSettings>();
if (settings == null)
{
    // Settings not loaded yet - create new store
    settings = akavacheInstance.GetSettingsStore<AppSettings>();
}
```

### Deleting Settings

```csharp
// Delete settings store (removes from memory and deletes file)
await akavacheInstance.DeleteSettingsStore<AppSettings>();

// Delete with custom database name
await akavacheInstance.DeleteSettingsStore<AppSettings>("CustomDbName");
```

### Disposing Settings

```csharp
// Dispose settings store (cleanup memory, keep file)
await akavacheInstance.DisposeSettingsStore<AppSettings>();
```

## Security Considerations

### Encrypted Settings

Use `WithSecureSettingsStore` for sensitive data:

```csharp
public class SecuritySettings : ISettingsStorage
{
    public string EncryptionKey { get; set; } = "";
    public string DatabasePassword { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Configure with encryption
builder.WithSecureSettingsStore<SecuritySettings>("strongPassword123!", settings =>
{
    // Set encrypted defaults
});
```

### Password Management

```csharp
public class PasswordManager
{
    private readonly IAkavacheInstance _cache;
    private readonly string _masterPassword;
    
    public PasswordManager(IAkavacheInstance cache, string masterPassword)
    {
        _cache = cache;
        _masterPassword = masterPassword;
    }
    
    public SecuritySettings GetSecuritySettings()
    {
        return _cache.GetSecureSettingsStore<SecuritySettings>(_masterPassword)
            ?? throw new UnauthorizedAccessException("Cannot access security settings");
    }
}
```

## Best Practices

### 1. Design Settings Classes Carefully

```csharp
// ✅ Good - Simple, focused settings class
public class DatabaseSettings : ISettingsStorage
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetry { get; set; } = true;
    
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// ❌ Avoid - Too many responsibilities
public class EverythingSettings : ISettingsStorage
{
    public string DatabaseConnection { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public List<UserProfile> Users { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
    // ... too much stuff
}
```

### 2. Use Appropriate Security Levels

```csharp
// Standard settings for non-sensitive data
builder.WithSettingsStore<AppSettings>(settings => { });

// Secure settings for sensitive data
builder.WithSecureSettingsStore<ApiKeys>("password", settings => { });
```

### 3. Initialize Settings Early

```csharp
// Initialize settings during application startup
public async Task InitializeAsync()
{
    // Configure Akavache with settings
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>("MyApp",
            builder => builder.WithSqliteProvider().WithSqliteDefaults(),
            instance => ConfigureSettings(instance));
}

private void ConfigureSettings(IAkavacheInstance instance)
{
    instance.WithSettingsStore<AppSettings>(settings =>
    {
        // Set reasonable defaults
        settings.ApiEndpoint = "https://api.production.com";
        settings.TimeoutSeconds = 30;
    });
}
```

### 4. Handle Settings Disposal

```csharp
public class SettingsService : IDisposable
{
    private readonly IAkavacheInstance _cache;
    
    public SettingsService(IAkavacheInstance cache)
    {
        _cache = cache;
    }
    
    public void Dispose()
    {
        // Clean disposal of settings when service is disposed
        Task.Run(async () =>
        {
            await _cache.DisposeSettingsStore<AppSettings>();
            await _cache.DisposeSettingsStore<UserPreferences>();
        });
    }
}
```

## Framework-Specific Examples

### ASP.NET Core

```csharp
// Program.cs or Startup.cs
builder.Services.AddSingleton<IAkavacheInstance>(sp =>
{
    IAkavacheInstance? instance = null;
    
    AppBuilder.CreateSplatBuilder()
        .WithAkavache<SystemJsonSerializer>("MyWebApp",
            cacheBuilder => cacheBuilder.WithSqliteProvider().WithSqliteDefaults(),
            akavacheInstance => 
            {
                instance = akavacheInstance;
                instance.WithSettingsStore<AppSettings>(settings => { });
            });
    
    return instance!;
});

// Controller
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly IAkavacheInstance _cache;
    
    public SettingsController(IAkavacheInstance cache)
    {
        _cache = cache;
    }
    
    [HttpGet("settings")]
    public ActionResult<AppSettings> GetSettings()
    {
        var settings = _cache.GetLoadedSettingsStore<AppSettings>();
        return settings != null ? Ok(settings) : NotFound();
    }
}
```

### MAUI / Xamarin

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        
        // Configure Akavache settings
        builder.Services.AddSingleton<IAkavacheInstance>(sp =>
        {
            IAkavacheInstance? instance = null;
            
            AppBuilder.CreateSplatBuilder()
                .WithAkavache<SystemJsonSerializer>("MyMauiApp",
                    cacheBuilder => cacheBuilder.WithSqliteProvider().WithSqliteDefaults(),
                    akavacheInstance => 
                    {
                        instance = akavacheInstance;
                        instance.WithSettingsStore<UserPreferences>(prefs => prefs.Theme = "System");
                    });
            
            return instance!;
        });
        
        return builder.Build();
    }
}
```

### WPF / Desktop

```csharp
// App.xaml.cs
public partial class App : Application
{
    public IAkavacheInstance Cache { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize settings
        AppBuilder.CreateSplatBuilder()
            .WithAkavache<SystemJsonSerializer>("MyWpfApp",
                builder => builder.WithSqliteProvider().WithSqliteDefaults(),
                instance =>
                {
                    Cache = instance;
                    instance.WithSettingsStore<AppSettings>(settings => 
                    {
                        settings.WindowWidth = 800;
                        settings.WindowHeight = 600;
                    });
                });
    }
}
```

## Troubleshooting

### Common Issues

**Settings not persisting:**
```csharp
// Ensure settings store is properly configured
var settings = instance.GetLoadedSettingsStore<AppSettings>();
if (settings == null)
{
    // Store not initialized - create it
    settings = instance.GetSettingsStore<AppSettings>();
}
```

**Cannot access settings after restart:**
```csharp
// Check if custom database name is consistent
instance.WithSettingsStore<AppSettings>(settings => { }, "MySettings");
// Must use same name when accessing
var settings = instance.GetSettingsStore<AppSettings>("MySettings");
```

**Encryption/decryption errors:**
```csharp
// Ensure password is consistent across app sessions
const string SETTINGS_PASSWORD = "consistent-password-123";
instance.WithSecureSettingsStore<SecureSettings>(SETTINGS_PASSWORD, settings => { });
```

## Performance Considerations

1. **Initialize once** - Don't recreate settings stores repeatedly
2. **Use appropriate caching** - Settings are automatically cached in memory
3. **Batch updates** - Multiple property changes are efficient
4. **Consider store size** - Don't store large objects in settings stores
5. **Dispose properly** - Clean up settings stores when done

## Migration from V10

If you're upgrading from Akavache V10.x, see the [migration guide](./migration-v10-to-v11.md) for complete migration instructions. The settings system in V11 provides much better type safety and configuration options.

## Next Steps

- [Learn about basic cache operations](./basic-operations.md)
- [Understand dependency injection patterns](./configuration.md#dependency-injection-pattern)
- [Review security best practices](./best-practices.md)
- [Explore platform-specific considerations](./platform-notes.md)