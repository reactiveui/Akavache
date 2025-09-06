# Tutorial: Mastering HTTP Caching with Akavache

Welcome to the comprehensive guide for HTTP caching operations with Akavache! This tutorial will take you from simple URL caching to building robust, production-ready services that handle API calls and offline data.

Akavache's HTTP extensions provide powerful, reactive tools for caching web content across platforms. Whether you're building a mobile app that needs to cache API responses or a desktop application that manages web content, this guide has you covered.

## Core Features

* **URL Content Caching**: Automatically cache HTTP responses with intelligent cache-aside patterns.
* **Reactive API**: A fully `IObservable`-based API for composing complex, asynchronous caching logic.
* **Custom Header Support**: Full support for authentication tokens, API keys, and custom headers.
* **Intelligent Fetching**: Built-in support for cache invalidation and fresh data fetching.
* **Cross-Platform**: Works seamlessly on iOS, Android, Windows, and other .NET platforms.

-----

## Chapter 1: Getting Started - Your First HTTP Cache

Let's start with the fundamentals. In just a few lines of code, you can set up persistent caching for any HTTP endpoint, dramatically improving your application's performance and offline capabilities.

### 1. Basic Setup

Before diving into HTTP operations, ensure you have Akavache properly initialized. For HTTP caching, you'll typically want persistent storage:

```csharp
using Akavache;
using Akavache.Core;
using Akavache.SystemTextJson;

// Correct initialization using the builder pattern
CacheDatabase.Initialize<SystemTextJsonSerializer>(builder =>
    builder.WithApplicationName("MyHttpApp")
           .WithSqliteProvider()
           .WithSqliteDefaults());
```

### 2. Your First Cached HTTP Request (The Reactive Way)

Akavache's APIs return an `IObservable<T>`, which is a stream of data. You can "subscribe" to this stream to get the result. This is the classic, powerful way to use Akavache.

```csharp
// DownloadUrl returns an IObservable<byte[]>
// We subscribe to it to receive the data when it arrives.
IDisposable subscription = CacheDatabase.LocalMachine.DownloadUrl("https://example.com/image.jpg")
    .Subscribe(
        imageData => Console.WriteLine($"Downloaded {imageData.Length} bytes and cached for future use"),
        ex => Console.WriteLine($"An error occurred: {ex.Message}")
    );

// In a real application, you would manage the 'subscription' lifetime,
// often by adding it to a CompositeDisposable that is cleared when a view is deactivated.
```

### 3. A Simpler Alternative: Directly Awaiting Observables

For simple cases where you only need one value, you can directly `await` the observable. This simplifies the code and makes it look like a standard `async/await` operation.

```csharp
using System.Reactive.Linq; // IMPORTANT - this makes await work!

try
{
    // You can now directly await the IObservable without .ToTask()
    var imageData = await CacheDatabase.LocalMachine.DownloadUrl("https://example.com/image.jpg");

    // You now have the raw bytes - perfect for images, JSON, or any web content
    Console.WriteLine($"Downloaded {imageData.Length} bytes and cached for future use");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Failed to download image: {ex.Message}");
}
```

Congratulations! You've just implemented intelligent HTTP caching that will make your app faster and more resilient.

-----

## Chapter 2: The HTTP Lifecycle - Core Operations

Understanding the fundamental patterns for HTTP caching will help you choose the right approach for each scenario in your application. We will use the direct `await` style here for simplicity.

### 1. Simple Download and Cache

The most straightforward pattern—perfect for resources that don't change frequently:

```csharp
using System.Reactive.Linq;

// Download and cache - uses URL as the cache key
var cssData = await CacheDatabase.LocalMachine.DownloadUrl("https://cdn.example.com/styles.css");

// The cached data persists across app restarts
// Next time this runs, it loads instantly from disk
```

### 2. Custom Cache Keys and Expiration

For dynamic content or when you need more control over caching behavior:

```csharp
using System.Reactive.Linq;

// Use a custom cache key instead of the URL
var apiData = await CacheDatabase.LocalMachine.DownloadUrl(
    key: "user_profile_data",
    url: "https://api.example.com/users/123/profile",
    absoluteExpiration: DateTimeOffset.Now.AddHours(6));

// This data will be cached for 6 hours
```

### 3. Authenticated Requests with Headers

Real-world APIs often require authentication tokens or custom headers:

```csharp
using System.Reactive.Linq;

// Prepare headers for authenticated API calls
var headers = new Dictionary<string, string>
{
    ["Authorization"] = $"Bearer {userToken}",
    ["X-API-Version"] = "2.0",
    ["User-Agent"] = "MyApp/1.2.0"
};

// Download with custom headers - perfect for authenticated APIs
var userData = await CacheDatabase.LocalMachine.DownloadUrl(
    "https://api.example.com/user/profile",
    HttpMethod.Get,
    headers,
    fetchAlways: false, // Use cache if available
    absoluteExpiration: DateTimeOffset.Now.AddMinutes(30));
```

-----

## Chapter 3: Practical Application - Building a Reactive Weather Service

Let's build something real-world: a weather service that caches API responses intelligently and provides offline functionality, all using a reactive approach.

### WeatherCacheService.cs

This version of the service returns `IObservable<WeatherData>`, allowing the UI layer to subscribe to weather updates reactively.

```csharp
using System.Reactive.Linq;
using System.Text.Json;
using Akavache;

public class WeatherCacheService
{
    private readonly string _apiKey;
    private readonly IBlobCache _cache;

    public WeatherCacheService(string apiKey, IBlobCache cache = null)
    {
        _apiKey = apiKey;
        _cache = cache ?? CacheDatabase.LocalMachine;
    }

    /// <summary>
    /// Gets weather data with intelligent caching. This method returns a stream
    /// that will provide the data when available.
    /// </summary>
    public IObservable<WeatherData> GetWeather(string cityName)
    {
        var cacheKey = $"weather_{cityName.ToLowerInvariant()}";
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}";
        var headers = new Dictionary<string, string> { ["User-Agent"] = "WeatherApp/1.0" };

        return _cache.DownloadUrl(cacheKey, apiUrl, headers: headers, absoluteExpiration: DateTimeOffset.Now.AddMinutes(10))
            .Select(jsonBytes =>
            {
                var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                return JsonSerializer.Deserialize<WeatherData>(jsonString);
            })
            .Catch<WeatherData, HttpRequestException>(ex =>
            {
                Console.WriteLine($"Network error, attempting to serve cached data: {ex.Message}");
                // Fallback to stale data from the cache using GetObject
                return _cache.GetObject<byte[]>(cacheKey).Select(staleBytes =>
                {
                    var jsonString = System.Text.Encoding.UTF8.GetString(staleBytes);
                    return JsonSerializer.Deserialize<WeatherData>(jsonString);
                });
            });
    }

    /// <summary>
    /// Force refresh weather data, bypassing the cache completely.
    /// </summary>
    public IObservable<WeatherData> RefreshWeather(string cityName)
    {
        var cacheKey = $"weather_{cityName.ToLowerInvariant()}";
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}";
        var headers = new Dictionary<string, string> { ["User-Agent"] = "WeatherApp/1.0" };

        // fetchAlways: true forces a fresh download
        return _cache.DownloadUrl(cacheKey, apiUrl, headers: headers, fetchAlways: true, absoluteExpiration: DateTimeOffset.Now.AddMinutes(10))
            .Select(jsonBytes =>
            {
                var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                return JsonSerializer.Deserialize<WeatherData>(jsonString);
            });
    }
}

public class WeatherData
{
    public string Name { get; set; }
    public MainWeather Main { get; set; }
    public WeatherCondition[] Weather { get; set; }
}

public class MainWeather
{
    public double Temp { get; set; }
    public double Humidity { get; set; }
}

public class WeatherCondition
{
    public string Main { get; set; }
    public string Description { get; set; }
}
```

This service demonstrates several advanced reactive concepts:

- **Declarative Pipelines**: Using `.Select()` to transform data and `.Catch()` to handle errors.
- **Error Handling**: Gracefully falling back to stale data during network failures.
- **Forced Refresh**: Providing a way for users to get the latest data on demand.
- **Immutable Results**: The `IObservable` stream delivers data without side effects.

-----

## Chapter 4: Advanced Techniques - Production-Ready Patterns

### 1. Batch Operations for Performance

When you need to download multiple resources efficiently, you can use reactive operators to merge the results into a single stream.

```csharp
using System.Reactive.Linq;

var urls = new[]
{
    "https://api.example.com/users/1",
    "https://api.example.com/users/2",
    "https://api.example.com/users/3"
};

// Create an observable for each download
var downloadObservables = urls.Select(url =>
{
    var cacheKey = $"user_{url.Split('/').Last()}";
    return CacheDatabase.LocalMachine.DownloadUrl(cacheKey, url, absoluteExpiration: DateTimeOffset.Now.AddMinutes(30));
});

// Merge the observables to run them in parallel and get a notification when all are complete
Observable.Merge(downloadObservables)
    .Subscribe(
        _ => Console.WriteLine("A user profile was downloaded and cached."),
        ex => Console.WriteLine($"An error occurred during batch download: {ex.Message}"),
        () => Console.WriteLine("All user profiles downloaded and cached successfully.")
    );
```

### 2. Cache Warming and Preloading

You can preload critical data when your application starts to ensure it's available instantly when the user needs it.

```csharp
using System.Reactive.Linq;

public IObservable<Unit> PreloadCriticalData()
{
    var criticalEndpoints = new[]
    {
        "https://api.example.com/config",
        "https://api.example.com/user/preferences",
        "https://api.example.com/notifications"
    };

    var warmupObservables = criticalEndpoints.Select(endpoint =>
        CacheDatabase.LocalMachine.DownloadUrl(
            key: $"preload_{endpoint.GetHashCode()}",
            url: endpoint,
            absoluteExpiration: DateTimeOffset.Now.AddHours(1))
        .Catch<byte[], Exception>(ex =>
        {
            // If one download fails, we don't want to stop the others.
            // We catch the exception and return an empty observable.
            Console.WriteLine($"Preload failed for {endpoint}: {ex.Message}");
            return Observable.Empty<byte[]>();
        })
    );

    // ForkJoin waits for all observables to complete
    return Observable.ForkJoin(warmupObservables).Select(_ => Unit.Default);
}

// Usage:
// await PreloadCriticalData();
// Console.WriteLine("Critical data preloaded and cached");
```

-----

## Best Practices and Troubleshooting

### Choosing Your Pattern: `IObservable` vs. `async/await`

Akavache gives you two powerful ways to handle asynchronous operations. Knowing when to use each is key.

* **Use `async/await`** for simplicity, especially in UI event handlers (`async void Button_Click`). It's perfect when you only need the **single, final value** from an operation, like a simple data fetch.
* **Use the `IObservable` pattern** (`.Select()`, `.Catch()`, `.Subscribe()`) for more complex scenarios. This is the ideal choice when you need to:
    * **Compose** multiple asynchronous steps into a declarative chain.
    * **Handle streams** of data, not just a single value.
    * Use advanced Akavache methods like `GetAndFetchLatest`, which can emit **multiple values** (first the cached data, then the fresh data from the network). `await` can only ever receive the *first* item from such a stream, which is often not what you want.

### Cache Key Strategy

* Use descriptive, consistent cache keys.
* Include user context when needed: `$"user_{userId}_profile"`
* Consider data versioning: `$"api_v2_users_{userId}"`

### Error Handling

* Always handle `HttpRequestException` for network issues when using `await`.
* Use the `.Catch()` operator for robust error handling in reactive chains.
* Implement fallback strategies (like reading from a stale cache) for offline scenarios.

### Performance Tips

* Use appropriate expiration times based on data freshness needs.
* Consider cache warming for critical data.
* Use `Observable.Merge` or `Observable.ForkJoin` for batch operations.
* Monitor cache size and implement cleanup strategies if necessary.

-----

## Conclusion

You now have a comprehensive knowledge of modern HTTP caching operations with Akavache. You've learned to build robust and performant applications by choosing between the simplicity of `async/await` and the power of fully reactive `IObservable` pipelines.
