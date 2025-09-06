# **Akavache.Settings**

Welcome to the `Akavache.Settings` guide\! This library provides a specialized, type-safe database for managing application settings. It creates persistent settings that are stored securely, making application updates and reinstalls seamless, as your users' configurations will survive the process.

It’s built on the powerful and asynchronous Akavache key-value store, offering a simple, object-oriented way to handle everything from user preferences to complex configuration objects.

### **Features**

  * **Type-Safe Settings**: Define settings as strongly-typed properties with default values. No more magic strings\!
  * **Automatic Persistence**: Settings are automatically saved to a SQLite database when changed.
  * **Update Friendly**: Settings are stored in a way that allows them to persist even when your application is updated or reinstalled.
  * **Encrypted Storage**: Built-in support for password-protecting sensitive settings like API keys or tokens.
  * **Multiple Settings Classes**: Easily organize your settings by grouping them into separate classes (e.g., `UserSettings`, `NetworkSettings`).
  * **DI Friendly**: Designed from the ground up to integrate with modern dependency injection patterns.

-----

## **Tutorial: Mastering Application Settings**

This tutorial will guide you through setting up and using `Akavache.Settings` in a modern .NET application. We'll cover everything from creating your first settings class to advanced topics like encryption and lifecycle management.

### **A Note on Modern Initialization**

If you've used older versions of Akavache, you might be familiar with setting a static `BlobCache.ApplicationName`. Akavache has since moved to a modern, **fluent builder pattern** for initialization. This change was made for several important reasons:

  * **Testability**: The new approach makes it easy to use an in-memory cache during unit tests, isolating tests from the file system.
  * **Clarity & Control**: Configuration is now explicit and declarative. You can see exactly how the cache is configured in one place.
  * **Dependency Injection (DI)**: It integrates perfectly with DI containers like Splat, which is the recommended approach for all modern applications.

While the core static class `CacheDatabase` is the engine that powers this, we'll be using the `Splat` integration builder (`AppBuilder.CreateSplatBuilder()`) in this guide, as it represents the best practice for building maintainable applications.

### **Step 1: Installation**

First, add the `Akavache.Settings` package to your project. You'll also need a serializer like `Akavache.SystemTextJson` and a backend like `Akavache.Sqlite3`.

```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
```

### **Step 2: Create Your First Settings Class**

Define a class that inherits from `SettingsBase`. This class will hold your application's settings as properties.

```csharp
using Akavache.Settings;

public class AppSettings : SettingsBase
{
    // The constructor must call the base constructor with a unique name for this
    // settings store. This name becomes the database filename.
    public AppSettings() : base(nameof(AppSettings))
    {
    }

    // A boolean setting.
    // get => GetOrCreate(true): If a value exists in the database, it's returned.
    // If not, the default value 'true' is returned and saved for next time.
    public bool EnableNotifications
    {
        get => GetOrCreate(true);
        set => SetOrCreate(value); // Saves the new value to the database.
    }

    // A string setting with a default value.
    public string UserName
    {
        get => GetOrCreate("DefaultUser");
        set => SetOrCreate(value);
    }

    // An enum setting.
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

The magic happens in the **`GetOrCreate()`** and **`SetOrCreate()`** methods. They handle all the database interaction, so you can work with your settings as simple C\# properties.

### **Step 3: Initialize the Settings Store**

At your application's startup (e.g., in `MauiProgram.cs` or `App.xaml.cs`), initialize Akavache and register your new settings class.

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Settings;

// This variable will be populated with our live settings object after initialization.
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder().WithAkavache<SystemJsonSerializer>(builder =>
    builder
        .WithApplicationName("MyApp")
        .WithSerializer(new SystemJsonSerializer())
        .WithSqliteProvider()
        // This is the key method for settings. It creates an instance of
        // AppSettings and returns it in the callback.
        .WithSettingsStore<AppSettings>(settings => appSettings = settings));

// Now the 'appSettings' variable can be used anywhere in your app.
// For DI-heavy apps, you would typically register it as a singleton.
```

### **Step 4: Use Your Settings**

Once initialized, you can use the `appSettings` object to read and write settings. Changes are saved to the database automatically.

```csharp
// Read a setting (will return the default "DefaultUser" on first run)
Console.WriteLine($"Username: {appSettings.UserName}");

// Write a setting
appSettings.UserName = "John Doe";
appSettings.EnableNotifications = false;

Console.WriteLine($"New Username: {appSettings.UserName}"); // Prints "John Doe"

// The next time the app starts, appSettings.UserName will be "John Doe".
```

-----

## **Advanced Scenarios**

`Akavache.Settings` is flexible enough to handle more complex requirements with ease.

### **Multiple Settings Classes**

You can organize your settings by creating multiple classes. For example, you could have `UserSettings` and `NetworkSettings`. Simply register each one in the builder.

```csharp
public class UserSettings : SettingsBase
{
    public UserSettings() : base(nameof(UserSettings)) { }
    public string Theme { get => GetOrCreate("Light"); set => SetOrCreate(value); }
}

public class NetworkSettings : SettingsBase
{
    public NetworkSettings() : base(nameof(NetworkSettings)) { }
    public int TimeoutSeconds { get => GetOrCreate(30); set => SetOrCreate(value); }
}

// In your initialization:
var userSettings = default(UserSettings);
var networkSettings = default(NetworkSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsStore<UserSettings>(s => userSettings = s)
               .WithSettingsStore<NetworkSettings>(s => networkSettings = s));
```

This will create two separate database files, `UserSettings.db` and `NetworkSettings.db`, keeping your configurations neatly separated.

### **Encrypted Settings**

For sensitive data like API keys, you should use an encrypted store. The setup is nearly identical, but you use the **`WithEncryptedSqliteProvider`** and **`WithSecureSettingsStore`** methods and provide a password.

```csharp
public class SecureSettings : SettingsBase
{
    public SecureSettings() : base(nameof(SecureSettings)) { }
    public string ApiKey { get => GetOrCreate(string.Empty); set => SetOrCreate(value); }
}

var secureSettings = default(SecureSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithEncryptedSqliteProvider()
               .WithSecureSettingsStore<SecureSettings>("my-super-secret-password",
                   settings => secureSettings = settings));

// Use it just like a normal settings object.
secureSettings.ApiKey = "sk-1234567890abcdef";
```

### **Customizing the Storage Path**

By default, settings are stored in a standard application data location. You can override this using **`WithSettingsCachePath`**.

```csharp
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsCachePath(@"C:\MyApp\Settings") // Custom path
               .WithSettingsStore<AppSettings>(settings => appSettings = settings));
```

### **Override Database Names**

You can specify a custom database file name for a settings store. This is useful if you need to manage multiple instances of the same settings class, perhaps for different users or documents.

To do this, simply provide a second argument to the `WithSettingsStore` method.

```csharp
var appSettings = default(AppSettings);

AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemTextJson>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsStore<AppSettings>(
                   settings => appSettings = settings, 
                   "CustomAppConfig"));  // This will create "CustomAppConfig.db"
```

-----

## **Working with Complex & Custom Types**

Akavache.Settings isn't limited to simple types. Thanks to its serializer, it can handle collections, nullable types, and even your own custom objects out of the box.

Here's a comprehensive example showing a variety of data types in action:

```csharp
public class ComprehensiveSettings : SettingsBase
{
    public ComprehensiveSettings() : base(nameof(ComprehensiveSettings))
    {
    }

    // Basic types with defaults
    public bool IsFeatureEnabled { get => GetOrCreate(true); set => SetOrCreate(value); }
    public int RetryCount { get => GetOrCreate(5); set => SetOrCreate(value); }
    public double ScaleFactor { get => GetOrCreate(1.25); set => SetOrCreate(value); }
    public string LastUser { get => GetOrCreate("Guest"); set => SetOrCreate(value); }

    // Nullable types
    public string? LastKnownLocation
    {
        get => GetOrCreate<string?>(null);
        set => SetOrCreate(value);
    }

    // Complex types (automatically serialized to JSON)
    public List<string> FavoriteCities
    {
        get => GetOrCreate(new List<string> { "New York", "London" });
        set => SetOrCreate(value);
    }

    public Dictionary<string, int> UserScores
    {
        get => GetOrCreate(new Dictionary<string, int> { ["Player1"] = 100, ["Player2"] = 250 });
        set => SetOrCreate(value);
    }

    // Custom objects
    public WindowPosition LastWindowPosition
    {
        get => GetOrCreate(new WindowPosition { X = 100, Y = 100, Width = 800, Height = 600 });
        set => SetOrCreate(value);
    }
}

// Your custom class - no special attributes needed!
public class WindowPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// --- Usage ---
var settings = default(ComprehensiveSettings);
AppBuilder.CreateSplatBuilder()
    .WithAkavache<SystemTextJson>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSettingsStore<ComprehensiveSettings>(s => settings = s));

// Now you can manipulate the complex properties directly
settings.FavoriteCities.Add("Tokyo");
settings.UserScores["Player3"] = 500;
settings.LastWindowPosition = new WindowPosition { X = 200, Y = 150, Width = 1024, Height = 768 };
```

-----

## **Lifecycle Management**

Properly managing the lifecycle of your settings is crucial for a robust application.

### **Resetting Settings to Defaults**

Implementing a "Reset Settings" feature is simple. The **`DeleteSettingsStore`** method removes the underlying database file. The next time the settings class is accessed, it will be recreated with its default values.

```csharp
// In a button click handler or command
var builder = CacheDatabase.Builder;
await builder.DeleteSettingsStore<AppSettings>();

// You would typically inform the user that a restart is required.
```

### **Proper Application Shutdown**

To prevent data loss, it's important to flush any pending writes to the disk when your application closes.

```csharp
// In your application shutdown logic (e.g., OnExit)
public async Task OnApplicationExit()
{
    var builder = CacheDatabase.Builder;

    // Dispose all your settings stores
    await builder.DisposeSettingsStore<AppSettings>();
    await builder.DisposeSettingsStore<UserSettings>();

    // And finally, shut down Akavache itself
    await CacheDatabase.Shutdown();
}
```

### **Check if Settings Exist**

Sometimes, you need to know if settings have been created before. This is useful for onboarding experiences or migrations. The `GetSettingsStore` method (without a callback) allows you to do this. It will return `null` if the store has not been created yet.

```csharp
// After the main Akavache initialization...
var builder = CacheDatabase.Builder;
var existingSettings = builder.GetSettingsStore<AppSettings>();

if (existingSettings != null)
{
    Console.WriteLine("Settings already exist. Welcome back!");
}
else
{
    Console.WriteLine("This is the first run. Creating default settings.");
}
```

-----

## **Common Patterns**

Here are some common patterns that can make your settings classes even more powerful.

### **Validation in Setters**

You can add validation logic directly into the property setter to ensure that only valid data is saved.

```csharp
public class ValidatedSettings : SettingsBase
{
    public ValidatedSettings() : base(nameof(ValidatedSettings)) { }
    
    public int MaxConnections
    {
        get => GetOrCreate(10);
        set 
        {
            if (value < 1 || value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxConnections must be between 1 and 100.");
            }
            SetOrCreate(value);
        }
    }
}
```

### **Settings Change Notifications**

If you are using your settings class with a UI framework like WPF, MAUI, or Avalonia, you can implement `INotifyPropertyChanged` to have the UI update automatically when a setting changes.

```csharp
using System.ComponentModel;
using Akavache.Settings;

public class NotifyingSettings : SettingsBase, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public NotifyingSettings() : base(nameof(NotifyingSettings)) { }
    
    public string Theme
    {
        get => GetOrCreate("Light");
        set 
        {
            // SetOrCreate returns 'true' if the value was changed
            if (SetOrCreate(value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
            }
        }
    }
}
```

-----

## ** Best Practices**

### **1. Use Meaningful Setting Names**

Descriptive names make your code easier to understand and maintain.

```csharp
// ✅ Good - Clear and descriptive
public bool EnablePushNotifications { get; set; }
public string DefaultLanguageCode { get; set; }

// ❌ Avoid - Ambiguous and unclear
public bool Flag1 { get; set; }
public string Str { get; set; }
```

### **2. Provide Sensible Defaults**

Every setting should have a reasonable default value in `GetOrCreate()` to ensure your app works correctly on the first run.

```csharp
// ✅ Good - A sensible default that works out-of-the-box
public int NetworkTimeoutSeconds
{
    get => GetOrCreate(30); // 30 seconds is a reasonable starting point
    set => SetOrCreate(value);
}

// ❌ Avoid - A default that could cause issues
public int TimeoutMs
{
    get => GetOrCreate(0); // A 0ms timeout is likely an error
    set => SetOrCreate(value);
}
```

### **3. Group Related Settings**

Organize settings into logical classes to improve separation of concerns.

```csharp
// ✅ Good - Related settings are grouped together
public class NetworkSettings : SettingsBase
{
    public int TimeoutSeconds { get; set; }
    public int MaxRetries { get; set; }
}

// ✅ Good - UI settings are in their own class
public class UISettings : SettingsBase
{
    public string Theme { get; set; }
    public double FontSize { get; set; }
}
```

### **4. Use Encryption for Sensitive Data**

Always use an encrypted store for any data that should not be stored in plain text.

```csharp
// ✅ Good - Encrypt sensitive data like API keys
public class SecuritySettings : SettingsBase { /* ... */ }

// In initialization:
builder.WithEncryptedSqliteProvider()
       .WithSecureSettingsStore<SecuritySettings>("password", ...);
```

## **Troubleshooting**

If you run into issues, check these common solutions.

  * **Settings Not Persisting?**

    1.  **Check Initialization**: Make sure `WithSettingsStore<T>()` is being called at startup.
    2.  **Verify Write Access**: Ensure your application has permission to write to its data directory.
    3.  **Call `SetOrCreate`**: A value is only saved when the `set` accessor is called. Reading a default value with `GetOrCreate` persists it, but subsequent changes require calling the setter.

  * **Performance Issues?**

    1.  **Avoid Overly Complex Objects**: While custom objects work, very large and deeply nested ones will be slower to serialize and deserialize.
    2.  **Batch Changes**: Settings are written to disk on every `SetOrCreate` call. If you need to change multiple settings at once, consider doing so in a single method to avoid frequent, small writes.

  * **Encryption Problems?**

    1.  **Store Passwords Securely**: Be sure to store your encryption password in a secure location, such as the platform's keychain or secure storage, rather than hardcoding it.
    2.  **Password Changes**: Changing the encryption password will make the old database file unreadable. This requires a migration path where you open the store with the old password, read the values, and save them to a new store with the new password.

-----

## **Conclusion & Best Practices**

You've now seen how `Akavache.Settings` provides a powerful, type-safe, and modern way to manage configuration. By following these best practices, you can build more robust and maintainable software.

