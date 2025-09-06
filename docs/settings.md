# Akavache.Settings

Akavache.Settings provides a specialized settings database for installable applications. It creates persistent settings that are stored one level down from the application folder, making application updates less painful as the settings survive reinstalls.

## Features

- **Type-Safe Settings**: Strongly-typed properties with default values
- **Automatic Persistence**: Settings are automatically saved when changed
- **Application Update Friendly**: Settings survive application reinstalls
- **Encrypted Storage**: Optional secure settings with password protection
- **Multiple Settings Classes**: Support for multiple settings categories

## Installation

```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

## Basic Usage

### 1. Create a Settings Class

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

### 2. Initialize Settings Store

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

## Advanced Configuration

### Custom Settings Cache Path

By default, settings are stored in a subfolder of your application directory. You can customize this path:

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsCachePath(@"C:\MyApp\Settings")  // Custom path
               .WithSettingsStore<AppSettings>(settings => appSettings = settings));
```

### Multiple Settings Classes

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
           .WithSqliteProvider()
           .WithSettingsStore<UserSettings>(settings => userSettings = settings)
           .WithSettingsStore<NetworkSettings>(settings => networkSettings = settings));
```

### Encrypted Settings

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
           .WithEncryptedSqliteProvider()
           .WithSecureSettingsStore<SecureSettings>("mySecurePassword", 
               settings => secureSettings = settings));

// Use encrypted settings
secureSettings.ApiKey = "sk-1234567890abcdef";
secureSettings.DatabasePassword = "super-secret-password";
```

### Override Database Names

You can specify custom database names for settings:

```csharp
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
           .WithSqliteProvider()
           .WithSettingsStore<AppSettings>(
               settings => appSettings = settings, 
               "CustomAppConfig"));  // Custom database name
```

## Complete Example

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
           .WithSqliteProvider()
           .WithSettingsStore<ComprehensiveSettings>(s => settings = s));

// Use the settings
settings.StringListSetting.Add("Item3");
settings.WindowPosition = new WindowPosition { X = 200, Y = 150, Width = 1024, Height = 768 };
settings.DictionarySetting["NewKey"] = 999;
```

## Settings Lifecycle Management

### Cleanup on Application Exit

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

### Delete Settings (Reset to Defaults)

```csharp
// Delete a specific settings store
var builder = CacheDatabase.Builder;
await builder.DeleteSettingsStore<AppSettings>();

// Settings will be recreated with default values on next access
```

### Check if Settings Exist

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

## Framework Support

Akavache.Settings supports all the same target frameworks as Akavache:

- **.NET Framework 4.6.2, 4.7.2** - Full support
- **.NET Standard 2.0** - Cross-platform compatibility
- **.NET 8.0, .NET 9.0** - Modern .NET support
- **Mobile platforms** - iOS, Android, MAUI

## Best Practices

### 1. Use Meaningful Setting Names

```csharp
// ✅ Good - descriptive names
public bool EnablePushNotifications { get; set; }
public string DefaultLanguageCode { get; set; }

// ❌ Avoid - unclear names
public bool Flag1 { get; set; }
public string Str { get; set; }
```

### 2. Provide Sensible Defaults

```csharp
// ✅ Good - sensible defaults
public int NetworkTimeoutSeconds
{
    get => GetOrCreate(30); // 30 seconds is reasonable
    set => SetOrCreate(value);
}

// ❌ Avoid - no defaults or poor defaults
public int TimeoutMs
{
    get => GetOrCreate(0); // 0 timeout makes no sense
    set => SetOrCreate(value);
}
```

### 3. Group Related Settings

```csharp
// ✅ Good - related settings in same class
public class NetworkSettings : SettingsBase
{
    public int TimeoutSeconds { get; set; }
    public int MaxRetries { get; set; }
    public bool EnableCaching { get; set; }
}

// ✅ Good - separate concerns
public class UISettings : SettingsBase
{
    public string Theme { get; set; }
    public double FontSize { get; set; }
}
```

### 4. Use Encryption for Sensitive Data

```csharp
// ✅ Good - encrypt sensitive settings
public class SecuritySettings : SettingsBase
{
    public string ApiKey
    {
        get => GetOrCreate(string.Empty);
        set => SetOrCreate(value);
    }
}

// Initialize with encryption
.WithEncryptedSqliteProvider()
.WithSecureSettingsStore<SecuritySettings>("password", settings => securitySettings = settings)
```

## Common Patterns

### 1. Validation in Setters

```csharp
public class ValidatedSettings : SettingsBase
{
    public ValidatedSettings() : base(nameof(ValidatedSettings)) { }
    
    private int _maxConnections;
    public int MaxConnections
    {
        get => GetOrCreate(10);
        set 
        {
            if (value < 1 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be between 1 and 100");
            SetOrCreate(value);
        }
    }
}
```

### 2. Settings Change Notifications

```csharp
public class NotifyingSettings : SettingsBase, INotifyPropertyChanged
{
    public NotifyingSettings() : base(nameof(NotifyingSettings)) { }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Theme
    {
        get => GetOrCreate("Light");
        set 
        {
            if (SetOrCreate(value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
            }
        }
    }
}
```

## Troubleshooting

### Settings Not Persisting

1. **Check initialization**: Ensure settings store is properly registered
2. **Verify paths**: Check that the application has write access to the settings directory
3. **Call SetOrCreate**: Settings are only saved when `SetOrCreate` is called

### Performance Issues

1. **Don't overuse complex types**: Simple types are faster to serialize/deserialize
2. **Avoid frequent writes**: Settings are saved on every `SetOrCreate` call
3. **Use appropriate data types**: Prefer enums over strings for fixed options

### Encryption Problems

1. **Password storage**: Ensure the encryption password is stored securely
2. **Key changes**: Changing encryption passwords requires data migration
3. **Performance impact**: Encrypted settings have additional overhead

For more help, see the main [Troubleshooting Guide](troubleshooting.md).