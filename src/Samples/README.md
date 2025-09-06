# Akavache Sample Applications

This directory contains three comprehensive sample applications demonstrating the capabilities of Akavache, the reactive caching library for .NET.

## Overview

The sample applications showcase real-world usage patterns for Akavache across different platforms and application types:

1. **AkavacheTodoWpf** - A Windows Presentation Foundation (WPF) desktop application
2. **AkavacheTodoMaui** - A cross-platform .NET MAUI application 
3. **AkavacheTodoBlazor** - A Blazor Server web application

Each application implements a comprehensive Todo management system that demonstrates:

- ? **Basic CRUD Operations** - Create, read, update, and delete cached data
- ? **Cache Expiration** - Automatic and manual cache invalidation
- ? **Multiple Cache Types** - UserAccount, LocalMachine, InMemory, and Secure caches
- ? **Reactive Patterns** - Observable-based data flow using ReactiveUI
- ? **Error Handling** - Graceful fallbacks and error recovery
- ? **Performance Optimization** - Efficient caching strategies
- ? **Security** - Encrypted storage for sensitive data
- ? **Cross-Serializer Support** - System.Text.Json integration

## Features Demonstrated

### Core Akavache Features

| Feature | Description | Demo Location |
|---------|-------------|---------------|
| **InsertObject** | Store typed objects with automatic serialization | All todo creation/editing |
| **GetObject** | Retrieve typed objects with deserialization | Todo loading and display |
| **GetOrFetchObject** | Cache-first pattern with fallback data source | Individual todo fetching |
| **GetAndFetchLatest** | Get cached data while refreshing in background | Statistics and API data |
| **GetOrCreateObject** | Get existing object or create default | Settings initialization |
| **InvalidateObject** | Remove specific items from cache | Todo deletion |
| **InvalidateAll** | Clear entire cache | Cache cleanup operations |
| **Vacuum** | Optimize cache storage | Maintenance operations |
| **GetAllKeys** | Enumerate all cache keys | Debug and monitoring |
| **GetCreatedAt** | Get cache item timestamps | Metadata queries |
| **Flush** | Force write pending operations | Application shutdown |

### Advanced Patterns

| Pattern | Description | Implementation |
|---------|-------------|----------------|
| **Cache Expiration** | Time-based automatic invalidation | Todo items expire after 24 hours |
| **Cache Hierarchies** | Different cache types for different data | Settings (UserAccount), Stats (LocalMachine), Credentials (Secure) |
| **Bulk Operations** | Efficient batch processing | Multiple todo operations |
| **Error Recovery** | Graceful fallbacks when cache fails | Default values and retry logic |
| **Cache Warming** | Pre-populate cache with sample data | Sample data loading |
| **Background Refresh** | Update cache without blocking UI | Live statistics updates |

## Application Architecture

### Shared Components

All three applications share a common architecture pattern:

```
Models/
??? TodoItem.cs          # Core domain model with JSON serialization
??? AppSettings.cs       # Application configuration and preferences
??? Enums/              # TodoPriority, AppTheme, TodoSortOrder

Services/
??? TodoCacheService.cs  # Comprehensive Akavache wrapper
??? NotificationService.cs # Platform-specific notifications

ViewModels/
??? MainViewModel.cs     # Primary application logic with ReactiveUI
??? TodoItemViewModel.cs # Individual todo item behavior
```

### Platform-Specific Features

#### WPF Application Features
- **Native Windows UI** with Material Design styling
- **Window state persistence** using cache
- **Desktop notifications** with system tray integration
- **Dependency injection** with Microsoft.Extensions.DI
- **MVVM pattern** with ReactiveUI and WPF data binding

#### MAUI Application Features  
- **Cross-platform UI** for Windows, iOS, Android, and macOS
- **Platform-specific services** for notifications and storage
- **Native styling** with platform-adaptive controls
- **Mobile-optimized** touch interactions and layouts
- **Offline-first** data synchronization patterns

> **Note:** The MAUI sample requires **.NET 9** and targets `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, and `net9.0-windows` only.

#### Blazor Application Features
- **Server-side rendering** with real-time updates
- **Bootstrap styling** with responsive design
- **SignalR integration** for live data updates
- **Web-optimized** pagination and filtering
- **API simulation** for remote data scenarios

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code
- For MAUI: Platform-specific workloads installed

### Running the Applications

#### WPF Application
```bash
cd AkavacheTodoWpf
dotnet run
```

#### MAUI Application  
```bash
cd AkavacheTodoMaui
# For Windows
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0

# For Android (requires emulator or device)
dotnet build -f net9.0-android

# For iOS (requires macOS and Xcode)
dotnet build -f net9.0-ios

# For Mac Catalyst (requires macOS)
dotnet build -f net9.0-maccatalyst
```

#### Blazor Application
```bash
cd AkavacheTodoBlazor
dotnet run
# Navigate to https://localhost:5001
```

## Key Implementation Details

### Cache Service Architecture

The `TodoCacheService` demonstrates best practices for Akavache usage:

```csharp
// Basic operations with error handling
public IObservable<List<TodoItem>> GetAllTodos()
{
    return BlobCache.UserAccount
        .GetObject<List<TodoItem>>(TodosKey)
        .Catch(Observable.Return(new List<TodoItem>()));
}

// Cache-first with fallback
public IObservable<TodoItem?> GetTodo(string todoId)
{
    var key = $"todo_{todoId}";
    return BlobCache.UserAccount
        .GetOrFetchObject(key, async () =>
        {
            var todos = await GetAllTodos().FirstAsync();
            return todos.FirstOrDefault(t => t.Id == todoId);
        }, DateTimeOffset.Now.AddMinutes(30));
}

// Background refresh pattern
public IObservable<TodoStats> GetTodoStats()
{
    return BlobCache.LocalMachine.GetAndFetchLatest(
        CacheStatsKey,
        () => CalculateTodoStats().ToObservable(),
        createdAt => DateTimeOffset.Now - createdAt > TimeSpan.FromMinutes(5),
        DateTimeOffset.Now.AddHours(1)
    );
}
```

### Reactive UI Integration

All view models use ReactiveUI for reactive property changes and command execution:

```csharp
// Reactive properties with automatic change notification
public string NewTodoTitle
{
    get => _newTodoTitle;
    set => this.RaiseAndSetIfChanged(ref _newTodoTitle, value);
}

// Reactive commands with async execution
AddTodoCommand = ReactiveCommand.CreateFromObservable(ExecuteAddTodo);

// Computed properties with caching
_todoStats = this.WhenAnyValue(x => x.AllTodos.Count)
    .Where(_ => AllTodos.Count > 0)
    .SelectMany(_ => _cacheService.GetTodoStats())
    .ToProperty(this, x => x.TodoStats, new TodoStats());
```

### Serialization Configuration

All applications use System.Text.Json for optimal performance:

```csharp
// Configure Akavache with System.Text.Json
CoreRegistrations.Serializer = new SystemJsonSerializer();

// Initialize SQLite support
Registrations.Start("AkavacheTodo", () => SQLitePCL.Batteries_V2.Init());

// Configure DateTime handling
BlobCache.ForcedDateTimeKind = DateTimeKind.Utc;
```

## Cache Strategy

### Cache Types Used

| Cache Type | Purpose | Lifetime | Examples |
|------------|---------|----------|----------|
| **UserAccount** | User-specific data that should sync | Persistent | Todos, user preferences |
| **LocalMachine** | Machine-specific temporary data | Session/temporary | Statistics, API responses |
| **InMemory** | Session-only data | Application lifetime | UI state, temporary calculations |
| **Secure** | Encrypted sensitive data | Persistent | User credentials, API keys |

### Expiration Strategy

- **Todos**: 24 hours (configurable)
- **Settings**: No expiration (persistent)
- **Statistics**: 5 minutes with background refresh
- **API Data**: 10 minutes with stale-while-revalidate
- **Session Data**: 1 hour maximum

## Error Handling

All cache operations include comprehensive error handling:

```csharp
// Graceful fallbacks
.Catch(Observable.Return(defaultValue))

// Retry with exponential backoff
.Retry(3)

// Timeout protection
.Timeout(TimeSpan.FromSeconds(30))

// Error logging and recovery
.Do(onNext: _ => { }, onError: ex => logger.LogError(ex, "Cache operation failed"))
```

## Performance Considerations

- **Bulk Operations**: Use `InsertObjects` and `GetObjects` for batch processing
- **Cache Warming**: Pre-populate frequently accessed data
- **Background Refresh**: Use `GetAndFetchLatest` for non-blocking updates
- **Memory Management**: Implement proper disposal patterns
- **Serialization**: System.Text.Json provides optimal performance

## Testing and Debugging

Each application includes:

- **Cache Information Panel** - Real-time cache statistics
- **Debug Commands** - Manual cache operations for testing
- **Sample Data Loading** - Predefined test scenarios
- **Error Simulation** - Network failure and edge case testing
- **Performance Monitoring** - Cache hit/miss rates and timing

## Best Practices Demonstrated

1. **Separation of Concerns** - Cache logic isolated in service classes
2. **Reactive Patterns** - Observable-based data flow throughout
3. **Error Resilience** - Graceful degradation when cache fails
4. **Type Safety** - Strongly-typed cache operations
5. **Performance** - Efficient serialization and cache strategies
6. **Security** - Appropriate use of secure cache for sensitive data
7. **Testability** - Injectable dependencies and observable patterns

## Contributing

When extending these samples:

1. Maintain the reactive patterns using Observables
2. Follow the established error handling strategies
3. Use appropriate cache types for different data categories
4. Include comprehensive null checking and validation
5. Document any new Akavache features demonstrated

## GetAndFetchLatest Best Practices

For comprehensive examples and patterns specifically for `GetAndFetchLatest`, see `GetAndFetchLatestPatterns.cs` in this directory. This addresses the common question: **"What's the right way to handle GetAndFetchLatest?"**

### Quick Reference: GetAndFetchLatest Patterns

| Pattern | Use Case | Key Benefit |
|---------|----------|-------------|
| **Simple Replacement** | Most UI scenarios | Easy to implement, works perfectly for displaying data |
| **Merge Strategy** | Collections/lists | Preserves existing items while adding new ones |
| **Differential Updates** | Complex data structures | Maximum control over what changes |
| **Loading States** | Responsive UIs | Provides user feedback during operations |
| **Conditional Fetching** | Performance optimization | Reduces unnecessary network calls |

### Common Anti-Patterns to Avoid ❌

```csharp
// ❌ DON'T: Await GetAndFetchLatest - you'll miss fresh data
var data = await cache.GetAndFetchLatest("key", fetchFunc).FirstAsync();

// ❌ DON'T: Clear collections in subscriber - will clear twice!
cache.GetAndFetchLatest("key", fetchFunc)
    .Subscribe(data => { items.Clear(); items.AddRange(data); });
```

### Recommended Pattern ✅

```csharp
// ✅ DO: Use Subscribe and handle both cached and fresh data appropriately
cache.GetAndFetchLatest("key", fetchFunc)
    .Subscribe(data => { 
        // This works correctly for both cached and fresh data
        DisplayData(data); 
    });
```

See `GetAndFetchLatestPatterns.cs` for detailed examples of each pattern with full working code.

## UpdateExpiration Best Practices

For comprehensive examples and patterns specifically for `UpdateExpiration`, see `UpdateExpirationPatterns.cs` in this directory. This addresses efficient cache management without expensive data I/O operations.

### Quick Reference: UpdateExpiration Patterns

| Pattern | Use Case | Key Benefit |
|---------|----------|-------------|
| **Basic Updates** | Single cache entry extension | Simple expiration management with absolute/relative times |
| **Bulk Operations** | Multiple entries at once | Efficient transaction-based updates for related data |
| **HTTP Caching** | 304 Not Modified responses | Extend cache validity without re-downloading data |
| **Session Management** | User activity tracking | Sliding expiration with activity-based renewal |
| **Performance Optimization** | High-throughput scenarios | Metadata-only updates (250x faster than traditional) |

### Key Benefits ✅

```csharp
// ✅ DO: Use UpdateExpiration for efficient cache extension
await cache.UpdateExpiration("large_data", TimeSpan.FromHours(1));

// ✅ DO: Batch updates for related entries
await cache.UpdateExpiration(relatedKeys, TimeSpan.FromMinutes(30));

// ✅ DO: Handle HTTP 304 responses efficiently
if (response.StatusCode == HttpStatusCode.NotModified)
{
    await cache.UpdateExpiration(cacheKey, TimeSpan.FromHours(1));
    return cachedData; // No data transfer needed
}
```

### Common Anti-Patterns to Avoid ❌

```csharp
// ❌ DON'T: Read and rewrite for simple expiration updates
var data = await cache.GetObject<LargeData>("key"); // Expensive!
await cache.InsertObject("key", data, newExpiration); // More expense!

// ❌ DON'T: Update entries individually in loops
foreach (var key in manyKeys) // Inefficient!
{
    await cache.UpdateExpiration(key, newExpiration);
}
```

## Additional Resources

### Comprehensive Pattern Documentation

- [`GetAndFetchLatestPatterns.cs`](GetAndFetchLatestPatterns.cs) - Detailed examples for the `GetAndFetchLatest` method
- [`UpdateExpirationPatterns.cs`](UpdateExpirationPatterns.cs) - Efficient cache expiration management patterns  
- [`CacheInvalidationPatterns.cs`](CacheInvalidationPatterns.cs) - **NEW in V11.1.1+** - Proper cache invalidation techniques and bug fix demonstration

### Cache Invalidation Best Practices 🔧

The `CacheInvalidationPatterns.cs` file addresses a **critical bug fixed in V11.1.1+** where calling `Invalidate()` on InMemory cache didn't properly clear RequestCache entries, causing `GetOrFetchObject` to return stale data.

#### Quick Reference: Invalidation Patterns

| Pattern | Use Case | Key Benefit |
|---------|----------|-------------|
| **Basic Invalidation** | Single cache entry removal | Ensures fresh data fetch after invalidation |
| **Bulk Invalidation** | Multiple related entries | Atomic clearing of related data |
| **Type-Based Invalidation** | Schema/model changes | Clear all objects of specific type |
| **Cross-Cache Testing** | Verification across cache types | Consistent behavior validation |
| **Production Patterns** | Real-world scenarios | Error handling and verification |

#### Critical Bug Fix ⚠️

```csharp
// ❌ BROKEN in pre-V11.1.1: Returns stale data from RequestCache
var data1 = await cache.GetOrFetchObject("key", fetchFunc); // "fresh_1"
await cache.Invalidate("key");
var data2 = await cache.GetOrFetchObject("key", fetchFunc); // Returns "fresh_1" (wrong!)

// ✅ FIXED in V11.1.1+: Properly fetches fresh data
var data1 = await cache.GetOrFetchObject("key", fetchFunc); // "fresh_1"  
await cache.Invalidate("key");
var data2 = await cache.GetOrFetchObject("key", fetchFunc); // Returns "fresh_2" (correct!)
```

**Impact**: This bug primarily affected InMemory cache usage where `GetOrFetchObject` was used in combination with `Invalidate`. The fix ensures that invalidation properly clears both the main cache and the RequestCache, preventing stale data returns.

**Migration**: No code changes required - simply upgrade to V11.1.1+ to get the fix automatically.

### External Documentation

- [Akavache Documentation](https://github.com/reactiveui/Akavache)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [System.Reactive Documentation](https://github.com/dotnet/reactive)
- [.NET MAUI Documentation](https://docs.microsoft.com/en-us/dotnet/maui/)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)

These sample applications provide a comprehensive reference for implementing Akavache in production applications across different platforms and scenarios.
