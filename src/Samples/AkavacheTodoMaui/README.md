# Akavache Todo MAUI Sample Application

A comprehensive .NET MAUI application demonstrating Akavache caching capabilities across multiple platforms (iOS, Android, Windows, macOS).

## Overview

This application showcases how to implement Akavache in a cross-platform mobile and desktop application using .NET MAUI and ReactiveUI.

## Features Demonstrated

### Akavache Features
- **Multi-platform caching** across iOS, Android, Windows, and macOS
- **SQLite-based persistence** with cross-platform file system access
- **Secure credential storage** using platform-specific secure storage
- **Background synchronization** with reactive data binding
- **Offline-first architecture** with automatic conflict resolution
- **Cross-platform serialization** with System.Text.Json

### MAUI-Specific Features
- **Platform-adaptive UI** with native controls and styling
- **Touch-optimized interactions** for mobile devices
- **Platform-specific services** for notifications and file access
- **Dependency injection** with Microsoft.Extensions.DI
- **MVVM architecture** with ReactiveUI and MAUI data binding

## Architecture

```
Platforms/
??? Android/           # Android-specific implementations
??? iOS/              # iOS-specific implementations  
??? MacCatalyst/      # macOS-specific implementations
??? Windows/          # Windows-specific implementations

Models/
??? TodoItem.cs       # Shared domain model
??? AppSettings.cs    # Cross-platform settings
??? Platform/         # Platform-specific models

Services/
??? TodoCacheService.cs     # Core Akavache service
??? NotificationService.cs  # Platform notifications
??? Platform/              # Platform-specific services

ViewModels/
??? MainViewModel.cs        # Primary application logic
??? TodoItemViewModel.cs    # Individual todo behavior
??? SettingsViewModel.cs    # Configuration management

Views/
??? MainPage.xaml          # Primary todo interface
??? SettingsPage.xaml      # Configuration page
??? Components/            # Reusable UI components
```

## Platform-Specific Implementations

### Android Features
- **Material Design 3** styling with dynamic colors
- **Android notifications** with action buttons
- **Scoped storage** compliance for Android 11+
- **Adaptive icons** and app shortcuts
- **Background sync** with WorkManager integration

### iOS Features  
- **iOS design language** with SF Symbols
- **Local notifications** with rich content
- **Keychain integration** for secure storage
- **App shortcuts** and Siri integration
- **Background app refresh** capabilities

### Windows Features
- **WinUI 3** styling with Fluent Design
- **Windows notifications** with toast actions
- **Windows Hello** integration for authentication
- **Live tiles** and jump list support
- **Windows-specific file system access**

### macOS Features
- **macOS design patterns** with native styling
- **macOS notifications** with user interaction
- **Keychain integration** for credential storage
- **Menu bar integration** and dock badges
- **macOS-specific file system patterns**

## Key Implementation Highlights

### Cross-Platform Cache Configuration

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Configure Akavache for cross-platform use
        ConfigureAkavache();

        // Register services
        builder.Services.AddSingleton<TodoCacheService>();
        builder.Services.AddTransient<MainViewModel>();

        return builder.Build();
    }

    private static void ConfigureAkavache()
    {
        // Use System.Text.Json for best performance
        CoreRegistrations.Serializer = new SystemJsonSerializer();
        
        // Initialize SQLite with platform-specific paths
        Registrations.Start("AkavacheTodoMaui", GetPlatformCachePath);
        
        // Configure for mobile/desktop scenarios
        BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;
    }
}
```

### Platform Service Registration

```csharp
#if ANDROID
builder.Services.AddSingleton<INotificationService, Platforms.Android.NotificationService>();
#elif IOS
builder.Services.AddSingleton<INotificationService, Platforms.iOS.NotificationService>();
#elif WINDOWS
builder.Services.AddSingleton<INotificationService, Platforms.Windows.NotificationService>();
#elif MACCATALYST
builder.Services.AddSingleton<INotificationService, Platforms.MacCatalyst.NotificationService>();
#endif
```

### Reactive MVVM with MAUI

```csharp
public partial class MainPage : ContentPage, IViewFor<MainViewModel>
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        
        this.WhenActivated(disposables =>
        {
            // Bind UI to ViewModel with platform-optimized patterns
            this.OneWayBind(ViewModel, vm => vm.Todos, v => v.TodosList.ItemsSource)
                .DisposeWith(disposables);
                
            this.Bind(ViewModel, vm => vm.NewTodoTitle, v => v.TitleEntry.Text)
                .DisposeWith(disposables);
                
            this.BindCommand(ViewModel, vm => vm.AddTodoCommand, v => v.AddButton)
                .DisposeWith(disposables);
        });
    }
    
    public MainViewModel ViewModel { get; set; }
    object IViewFor.ViewModel { get; set; }
}
```

### Mobile-Optimized Caching Strategy

| Cache Type | Mobile Usage | Desktop Usage |
|------------|--------------|---------------|
| **UserAccount** | iCloud/Google sync data | User profile data |
| **LocalMachine** | Device-specific cache | Machine-specific cache |
| **InMemory** | Session state | UI state |
| **Secure** | Keychain/Keystore | Windows Credential Manager |

### Offline-First Architecture

```csharp
public IObservable<List<TodoItem>> GetTodosWithSync()
{
    return BlobCache.UserAccount
        .GetAndFetchLatest(
            "todos",
            () => SyncWithCloudService(),
            fetchPredicate: createdAt => 
                DateTimeOffset.Now - createdAt > TimeSpan.FromMinutes(5),
            absoluteExpiration: DateTimeOffset.Now.AddHours(1)
        );
}
```

## Running the Application

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 with MAUI workload
- Platform-specific development tools:
  - **Android**: Android SDK, Android emulator
  - **iOS**: Xcode, iOS Simulator (macOS required)
  - **Windows**: Windows SDK
  - **macOS**: Xcode (for macOS development)

### Build and Run

```bash
# Windows
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0

# Android
dotnet build -f net9.0-android
# Deploy to connected device or emulator

# iOS (requires macOS)
dotnet build -f net9.0-ios
# Deploy to simulator or device

# macOS
dotnet build -f net9.0-maccatalyst
```

## Platform-Specific Features

### Android Implementation
- **Material You** theming with dynamic colors
- **Notification channels** for categorized notifications
- **Adaptive storage** handling for different Android versions
- **Biometric authentication** for secure cache access

### iOS Implementation  
- **SwiftUI-like** declarative UI patterns in XAML
- **Core Data integration** for complex offline scenarios
- **Haptic feedback** for user interactions
- **Face ID/Touch ID** integration for security

### Windows Implementation
- **Fluent Design** with acrylic materials and animations
- **Windows 11 snap layouts** and window management
- **Windows Timeline** integration for activity tracking
- **Windows Hello** for biometric authentication

### macOS Implementation
- **macOS Big Sur+** design language
- **Mac Catalyst optimizations** for desktop workflows
- **macOS-specific navigation** patterns
- **TouchBar support** where available

## Testing and Debugging

### Platform Testing
- **Unit tests** for shared business logic
- **Platform-specific tests** for native integrations
- **UI tests** using MAUI testing framework
- **Performance profiling** on each target platform

### Debug Features
- **Real-time cache monitoring** with platform-native debugging
- **Network simulation** for offline testing
- **Platform-specific logging** integration
- **Memory profiling** for mobile optimization

## Deployment

### App Store Guidelines
- **iOS App Store** compliance with data handling policies
- **Google Play Store** compliance with storage permissions
- **Microsoft Store** compliance with Windows policies
- **Mac App Store** compliance with sandboxing requirements

### Distribution
- **CI/CD pipelines** for automated building and testing
- **Code signing** for all platforms
- **App Store submission** automation
- **Beta testing** through platform test flight programs

## Best Practices for Mobile Caching

1. **Battery Optimization** - Minimize background cache operations
2. **Memory Management** - Implement proper disposal patterns
3. **Network Awareness** - Adapt caching based on connection type
4. **Storage Limits** - Respect platform storage quotas
5. **Privacy Compliance** - Handle data according to platform policies
6. **Accessibility** - Ensure cache operations don't block UI accessibility

## Performance Considerations

- **Lazy loading** for large datasets on mobile
- **Image caching** with memory pressure handling
- **Background fetch** optimization for battery life
- **Cold start** optimization with cache preloading
- **Memory warnings** handling on mobile platforms

This MAUI application demonstrates production-ready patterns for implementing Akavache in cross-platform mobile and desktop applications with platform-specific optimizations and native user experiences.
