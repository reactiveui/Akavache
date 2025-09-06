# Platform-Specific Notes

This guide covers platform-specific configuration, requirements, and considerations for using Akavache across different target platforms.

## .NET MAUI

> **Note:** MAUI targets in this repository are documented for **.NET 9** only. For older TFMs, please use a previous release/tag or consult historical docs. See [MAUI .NET 9 Support Documentation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/app-lifecycle?view=net-maui-9.0) for official guidance.

### Supported Target Frameworks
- `net9.0-android` - Android applications  
- `net9.0-ios` - iOS applications
- `net9.0-maccatalyst` - Mac Catalyst applications
- `net9.0-windows` - Windows applications (WinUI)

### Configuration

```csharp
// In MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Initialize Akavache early in the MAUI startup process
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(cacheBuilder =>
                cacheBuilder.WithApplicationName("MyMauiApp")
                        .WithSqliteProvider()           // REQUIRED: Explicit provider
                        .WithForcedDateTimeKind(DateTimeKind.Utc)
                        .WithSqliteDefaults());

        return builder.Build();
    }
}
```

### Example Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net9.0-windows10.0.19041.0</TargetFrameworks>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <MauiVersion>9.0.0</MauiVersion>
    <!-- Other MAUI configuration -->
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="9.0.0" />
    <PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
    <PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
  </ItemGroup>
</Project>
```

### MAUI-Specific Considerations

- **Initialize early** in `MauiProgram.cs` before any UI components are created
- **Use UTC DateTimeKind** to avoid timezone issues across platforms
- **Consider platform differences** in file system access and caching policies
- **Test thoroughly** on all target platforms as behavior can vary

## WPF Applications

### Configuration

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
        // Important: Shutdown Akavache properly to ensure data integrity
        CacheDatabase.Shutdown().Wait();
        base.OnExit(e);
    }

    private static void ConfigureAkavache()
    {
        AppBuilder.CreateSplatBuilder()
            .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
                builder.WithApplicationName("MyWpfApp")
                   .WithSqliteProvider()            // REQUIRED: Explicit provider
                   .WithForcedDateTimeKind(DateTimeKind.Utc)
                   .WithSqliteDefaults());
    }
}
```

### WPF-Specific Considerations

- **Proper shutdown** is crucial to prevent data corruption
- **Thread safety** - Akavache is thread-safe but consider UI thread marshaling
- **File system permissions** - Ensure app has write access to cache directories
- **Application lifecycle** - Handle suspension and resumption correctly

## iOS Specific

### Configuration

```csharp
// In AppDelegate.cs or SceneDelegate.cs
public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyiOSApp")
               .WithSqliteProvider()        // REQUIRED: Explicit provider
               .WithSqliteDefaults());

    return base.FinishedLaunching(application, launchOptions);
}
```

### iOS-Specific Considerations

#### File System and Backup
- **Cache location**: `Library/Caches/` (not backed up by iTunes/iCloud)
- **Backup considerations**: UserAccount cache may be backed up to iCloud
- **Storage limitations**: iOS may delete cache data when storage is low

#### Security and Keychain Integration
```csharp
// Secure cache automatically integrates with iOS Keychain
await CacheDatabase.Secure.InsertObject("sensitive_data", secretData);
```

#### Background and Foreground Transitions
```csharp
// In AppDelegate.cs
public override void DidEnterBackground(UIApplication application)
{
    // Flush any pending writes before backgrounding
    CacheDatabase.Flush().Wait();
}
```

#### Memory Management
- **Use InMemory cache sparingly** - iOS has aggressive memory management
- **Monitor memory warnings** and clear unnecessary cached data
- **Consider cache size limits** based on device capabilities

## Android Specific

### Configuration

```csharp
// In MainActivity.cs or Application class
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyAndroidApp")
               .WithSqliteProvider()        // REQUIRED: Explicit provider
               .WithSqliteDefaults());
}
```

### Android-Specific Considerations

#### File System and Permissions
- **Cache location**: `{ApplicationData}/cache/` (internal storage)
- **External storage**: Requires additional permissions for external cache
- **Backup policies**: Respect Android's auto-backup configuration

#### Application Lifecycle
```csharp
// In MainActivity.cs
protected override void OnPause()
{
    // Ensure data is persisted before app is paused
    CacheDatabase.Flush().Wait();
    base.OnPause();
}
```

#### ProGuard/R8 Considerations
```xml
<!-- In proguard-rules.pro or rules.txt -->
-keep class Akavache.** { *; }
-keep class YourApp.Models.** { *; }
-dontwarn Akavache.**
```

#### Background Processing
- **Be aware of doze mode** and background execution limits
- **Consider using foreground services** for critical cache operations
- **Handle process termination** gracefully

## UWP Applications

### Configuration

```csharp
// In App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    AppBuilder.CreateSplatBuilder()
        .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
            builder.WithApplicationName("MyUwpApp")
               .WithSqliteProvider()        // REQUIRED: Explicit provider
               .WithSqliteDefaults());

    // Rest of initialization...
}
```

### UWP-Specific Considerations

#### Platform Architecture
- **Important**: Mark your application as `x86`, `x64`, or `ARM`, not `Any CPU`
- **SQLite native libraries** require specific architecture targeting

#### App Suspension and Termination
```csharp
// In App.xaml.cs
private async void OnSuspending(object sender, SuspendingEventArgs e)
{
    var deferral = e.SuspendingOperation.GetDeferral();
    try
    {
        await CacheDatabase.Flush();
    }
    finally
    {
        deferral.Complete();
    }
}
```

#### Package.appxmanifest Considerations
```xml
<Package>
  <!-- Ensure appropriate capabilities are declared -->
  <Capabilities>
    <Capability Name="internetClient" />
    <!-- Add others as needed -->
  </Capabilities>
</Package>
```

## Cross-Platform Considerations

### DateTime Handling
```csharp
// Always use UTC for cached DateTime values to avoid timezone issues
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithForcedDateTimeKind(DateTimeKind.Utc)  // Ensures consistency
               .WithSqliteDefaults());
```

### File Path Considerations
```csharp
// Let Akavache handle platform-specific paths
// Don't hardcode file paths - use the defaults or platform-appropriate methods
```

### Serialization Differences
```csharp
// Use consistent serialization across platforms
// System.Text.Json is recommended for new projects
// Newtonsoft.Json for maximum compatibility
```

### Performance Characteristics by Platform

| Platform | SQLite Performance | File I/O | Memory Constraints | Network |
|----------|-------------------|----------|--------------------|---------|
| **iOS** | Good | Restricted | High | Good |
| **Android** | Good | Restricted | Medium | Good |
| **Windows** | Excellent | Flexible | Low | Excellent |
| **macOS** | Excellent | Flexible | Low | Excellent |
| **UWP** | Good | Sandboxed | Medium | Good |

## Platform-Specific Package Requirements

### iOS/macOS Additional Packages
```xml
<!-- For image caching on iOS/macOS -->
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### Android Additional Packages
```xml
<!-- For encrypted storage on Android -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
```

### Windows-Specific Features
```xml
<!-- For Windows-specific drawing features -->
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

## Testing Across Platforms

### Platform-Specific Unit Tests
```csharp
[TestFixture]
public class PlatformSpecificTests
{
    [Test]
    [Platform(Include = "iOS")]
    public async Task iOS_Specific_Behavior_Test()
    {
        // iOS-specific test logic
    }
    
    [Test]
    [Platform(Include = "Android")]
    public async Task Android_Specific_Behavior_Test()
    {
        // Android-specific test logic
    }
}
```

### Cross-Platform Integration Tests
```csharp
[TestFixture]
public class CrossPlatformTests
{
    [Test]
    public async Task Data_Should_Be_Consistent_Across_Platforms()
    {
        // Test that data stored on one platform can be read on another
        var testData = new TestModel { Id = 1, Name = "Test" };
        
        await CacheDatabase.UserAccount.InsertObject("test", testData);
        var retrieved = await CacheDatabase.UserAccount.GetObject<TestModel>("test");
        
        Assert.AreEqual(testData.Name, retrieved.Name);
    }
}
```

## Troubleshooting Platform Issues

### Common Platform-Specific Problems

#### iOS
- **"SQLite library not found"** - Ensure proper architecture targeting
- **"Keychain access denied"** - Check app entitlements and permissions
- **"Cache data missing after app update"** - Check backup/restore settings

#### Android
- **"Database is locked"** - Ensure proper app lifecycle handling
- **"Permission denied"** - Verify cache directory permissions
- **"ProGuard stripped types"** - Add proper keep rules

#### Windows/UWP
- **"Architecture mismatch"** - Set specific platform target (x86/x64/ARM)
- **"Access denied to cache directory"** - Check Windows permissions
- **"App suspension data loss"** - Implement proper suspension handling

### Platform-Specific Debugging
```csharp
public class PlatformCacheDebugger
{
    public static void LogPlatformInfo()
    {
        Console.WriteLine($"Platform: {Environment.OSVersion}");
        Console.WriteLine($"Cache directory: {CacheDatabase.GetCacheDirectory()}");
        Console.WriteLine($"Available disk space: {GetAvailableDiskSpace()}");
    }
}
```

## Best Practices Summary

1. **Initialize early** in the application lifecycle
2. **Handle suspension/termination** properly on mobile platforms
3. **Use UTC DateTimeKind** for cross-platform consistency
4. **Test on all target platforms** - behavior can vary significantly
5. **Respect platform conventions** for file storage and security
6. **Monitor platform-specific constraints** like memory and storage limits
7. **Handle platform-specific exceptions** gracefully
8. **Use appropriate cache types** based on platform characteristics

## Next Steps

- [Review basic operations](./basic-operations.md)
- [Explore performance optimization](./performance.md)
- [Check troubleshooting guide](./troubleshooting/troubleshooting-guide.md)
- [See best practices guide](./best-practices.md)