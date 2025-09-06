# Tutorial: Mastering HTTP Operations and Credentials with Akavache

Welcome to the comprehensive guide for HTTP operations and credential management with Akavache! This tutorial will take you from simple URL caching to building robust, production-ready services that handle API calls, authentication, and secure credential storage.

Akavache's HTTP extensions provide powerful tools for caching web content and managing user credentials securely across platforms. Whether you're building a mobile app that needs to cache API responses or a desktop application that manages multiple user accounts, this guide has you covered.

## Core Features

* **URL Content Caching**: Automatically cache HTTP responses with intelligent cache-aside patterns
* **Custom Header Support**: Full support for authentication tokens, API keys, and custom headers
* **Intelligent Fetching**: Built-in support for cache invalidation and fresh data fetching
* **Secure Credential Storage**: Encrypted storage for passwords, tokens, and sensitive data
* **Multi-Service Support**: Manage credentials for multiple APIs and services
* **Cross-Platform**: Works seamlessly on iOS, Android, Windows, and other .NET platforms

## Chapter 1: Getting Started - Your First HTTP Cache

Let's start with the fundamentals. In just a few lines of code, you can set up persistent caching for any HTTP endpoint, dramatically improving your application's performance and offline capabilities.

### 1. Basic Setup

Before diving into HTTP operations, ensure you have Akavache properly initialized. For HTTP caching, you'll typically want persistent storage:

```csharp
using Akavache.Core;
using Akavache.SystemTextJson;
using Akavache.Sqlite3;

// Initialize Akavache for HTTP caching
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyHttpApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

### 2. Your First Cached HTTP Request

Now for the magic. With a single method call, you can download and cache any URL. Subsequent requests will be served instantly from the local cache:

```csharp
// This downloads the content, caches it, and returns the raw bytes
// If you call this again, it loads instantly from cache
var imageData = await CacheDatabase.LocalMachine.DownloadUrl("https://example.com/image.jpg");

// You now have the raw bytes - perfect for images, JSON, or any web content
Console.WriteLine($"Downloaded {imageData.Length} bytes and cached for future use");
```

Congratulations! You've just implemented intelligent HTTP caching that will make your app faster and more resilient.

## Chapter 2: The HTTP Lifecycle - Core Operations

Understanding the three fundamental patterns for HTTP caching will help you choose the right approach for each scenario in your application.

### 1. Simple Download and Cache

The most straightforward pattern - perfect for resources that don't change frequently:

```csharp
// Download and cache - uses URL as the cache key
var cssData = await CacheDatabase.LocalMachine.DownloadUrl("https://cdn.example.com/styles.css");

// The cached data persists across app restarts
// Next time this runs, it loads instantly from disk
```

### 2. Custom Cache Keys and Expiration

For dynamic content or when you need more control over caching behavior:

```csharp
// Use a custom cache key instead of the URL
var apiData = await CacheDatabase.LocalMachine.DownloadUrl(
    key: "user_profile_data", 
    url: "https://api.example.com/users/123/profile",
    absoluteExpiration: DateTimeOffset.Now.AddHours(6));

// This data will be cached for 6 hours, then automatically refreshed
```

### 3. Authenticated Requests with Headers

Real-world APIs often require authentication tokens or custom headers:

```csharp
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

## Chapter 3: Practical Application - Building a Weather Service

Let's build something real-world: a weather service that caches API responses intelligently, handles authentication, and provides offline functionality.

### WeatherCacheService.cs

```csharp
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
    /// Gets weather data with intelligent caching. Fresh data every 10 minutes,
    /// but serves cached data instantly while fetching updates in background.
    /// </summary>
    public async Task<WeatherData> GetWeatherAsync(string cityName)
    {
        var cacheKey = $"weather_{cityName.ToLowerInvariant()}";
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}";
        
        // Prepare headers for the API request
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = "WeatherApp/1.0",
            ["Accept"] = "application/json"
        };

        try
        {
            // Download with 10-minute cache expiration
            var jsonBytes = await _cache.DownloadUrl(
                key: cacheKey,
                url: apiUrl,
                method: HttpMethod.Get,
                headers: headers,
                fetchAlways: false,
                absoluteExpiration: DateTimeOffset.Now.AddMinutes(10));

            // Deserialize the cached JSON
            var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var weatherData = JsonSerializer.Deserialize<WeatherData>(jsonString);
            
            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            // Network error - try to serve stale cached data
            Console.WriteLine($"Network error, attempting to serve cached data: {ex.Message}");
            
            try
            {
                // Attempt to get any cached version, even if expired
                var staleData = await GetStaleWeatherData(cacheKey);
                return staleData;
            }
            catch
            {
                throw new Exception($"Unable to fetch weather for {cityName}. Check your internet connection.", ex);
            }
        }
    }

    /// <summary>
    /// Force refresh weather data, bypassing cache completely
    /// </summary>
    public async Task<WeatherData> RefreshWeatherAsync(string cityName)
    {
        var cacheKey = $"weather_{cityName.ToLowerInvariant()}";
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={_apiKey}";
        
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = "WeatherApp/1.0",
            ["Accept"] = "application/json"
        };

        // Force fresh download, ignoring any cached version
        var jsonBytes = await _cache.DownloadUrl(
            key: cacheKey,
            url: apiUrl,
            method: HttpMethod.Get,
            headers: headers,
            fetchAlways: true, // Always fetch fresh data
            absoluteExpiration: DateTimeOffset.Now.AddMinutes(10));

        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
        return JsonSerializer.Deserialize<WeatherData>(jsonString);
    }

    private async Task<WeatherData> GetStaleWeatherData(string cacheKey)
    {
        // Try to get expired data directly from cache
        var cachedBytes = await _cache.GetObject<byte[]>(cacheKey);
        var jsonString = System.Text.Encoding.UTF8.GetString(cachedBytes);
        return JsonSerializer.Deserialize<WeatherData>(jsonString);
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

This service demonstrates several advanced concepts:
- **Intelligent caching** with 10-minute expiration
- **Authentication** using API keys in headers
- **Error handling** with fallback to stale data
- **Forced refresh** for when users want the latest data
- **Proper resource management** and offline support

## Chapter 4: Credential Management - Secure Storage Made Simple

Modern applications need to manage multiple user accounts, API tokens, and sensitive data. Akavache's credential management makes this secure and straightforward.

### 1. Basic Credential Storage

All credential operations automatically use the secure, encrypted cache:

```csharp
// Save user login credentials (automatically encrypted)
await CacheDatabase.Secure.SaveLogin("john.doe@example.com", "user_password", "myapp.com");

// The credentials are now safely stored and encrypted
// They'll persist across app restarts and device reboots
Console.WriteLine("Login credentials saved securely");
```

### 2. Multi-Service Authentication

Modern apps often integrate with multiple services. Manage them all easily:

```csharp
// Save credentials for different services
await CacheDatabase.Secure.SaveLogin("user123", "github_token", "github.com");
await CacheDatabase.Secure.SaveLogin("user123", "slack_token", "slack.com");  
await CacheDatabase.Secure.SaveLogin("user123", "api_key", "weather_service");

// Each service has isolated, secure storage
```

### 3. Retrieving and Using Credentials

```csharp
public async Task<string> AuthenticatedApiCall(string endpoint)
{
    try
    {
        // Retrieve stored credentials
        var loginInfo = await CacheDatabase.Secure.GetLogin("weather_service");
        
        // Use the credentials in your API call
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {loginInfo.Password}", // API token
            ["X-User-ID"] = loginInfo.UserName
        };
        
        var response = await CacheDatabase.LocalMachine.DownloadUrl(
            endpoint, 
            HttpMethod.Get, 
            headers);
            
        return System.Text.Encoding.UTF8.GetString(response);
    }
    catch (KeyNotFoundException)
    {
        // No credentials found - redirect to login
        throw new UnauthorizedAccessException("Please log in first");
    }
}
```

### 4. Credential Lifecycle Management

Handle token expiration, logout, and credential updates:

```csharp
// Save temporary tokens with expiration
await CacheDatabase.Secure.SaveLogin(
    username: "api_access",
    password: temporaryToken,
    host: "api.example.com",
    absoluteExpiration: DateTimeOffset.Now.AddHours(2)); // Token expires in 2 hours

// Clear credentials on logout
await CacheDatabase.Secure.EraseLogin("myapp.com");

// Update existing credentials
await CacheDatabase.Secure.SaveLogin("user@example.com", newPassword, "myapp.com");
```

## Chapter 5: Advanced Techniques - Production-Ready Patterns

### 1. Handling HTTP Status Codes

```csharp
public async Task<ApiResponse<T>> SafeApiCall<T>(string url, Dictionary<string, string> headers = null)
{
    try
    {
        var data = await CacheDatabase.LocalMachine.DownloadUrl(url, HttpMethod.Get, headers);
        var json = System.Text.Encoding.UTF8.GetString(data);
        var result = JsonSerializer.Deserialize<T>(json);
        
        return new ApiResponse<T>
        {
            Success = true,
            Data = result,
            FromCache = false // DownloadUrl doesn't indicate cache source
        };
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("404"))
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = "Resource not found",
            HttpStatusCode = 404
        };
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
    {
        // Handle authentication errors
        await CacheDatabase.Secure.EraseLogin("api_service"); // Clear bad credentials
        
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = "Authentication required",
            HttpStatusCode = 401
        };
    }
    catch (Exception ex)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = ex.Message
        };
    }
}
```

### 2. Batch Operations for Performance

```csharp
// Download multiple resources efficiently
var urls = new[]
{
    "https://api.example.com/users/1",
    "https://api.example.com/users/2", 
    "https://api.example.com/users/3"
};

var downloadTasks = urls.Select(async url =>
{
    var cacheKey = $"user_{url.Split('/').Last()}";
    return await CacheDatabase.LocalMachine.DownloadUrl(
        cacheKey, 
        url,
        absoluteExpiration: DateTimeOffset.Now.AddMinutes(30));
});

var results = await Task.WhenAll(downloadTasks);
Console.WriteLine($"Downloaded and cached {results.Length} user profiles");
```

### 3. Cache Warming and Preloading

```csharp
public async Task PreloadCriticalData()
{
    var criticalEndpoints = new[]
    {
        "https://api.example.com/config",
        "https://api.example.com/user/preferences", 
        "https://api.example.com/notifications"
    };
    
    // Start all downloads in parallel
    var warmupTasks = criticalEndpoints.Select(async endpoint =>
    {
        try
        {
            await CacheDatabase.LocalMachine.DownloadUrl(
                key: $"preload_{endpoint.GetHashCode()}",
                url: endpoint,
                absoluteExpiration: DateTimeOffset.Now.AddHours(1));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Preload failed for {endpoint}: {ex.Message}");
        }
    });
    
    await Task.WhenAll(warmupTasks);
    Console.WriteLine("Critical data preloaded and cached");
}
```

## Best Practices and Troubleshooting

### Cache Key Strategy
- Use descriptive, consistent cache keys
- Include user context when needed: `$"user_{userId}_profile"`
- Consider data versioning: `$"api_v2_users_{userId}"`

### Error Handling
- Always handle `HttpRequestException` for network issues
- Use `KeyNotFoundException` to detect missing credentials
- Implement fallback strategies for offline scenarios

### Performance Tips
- Use appropriate expiration times based on data freshness needs
- Consider cache warming for critical data
- Use batch operations when downloading multiple resources
- Monitor cache size and implement cleanup strategies

### Security Considerations
- Always use `CacheDatabase.Secure` for sensitive data
- Never log passwords or tokens
- Implement proper token refresh mechanisms
- Clear credentials on user logout

## Conclusion & API Quick Reference

You now have comprehensive knowledge of HTTP operations and credential management with Akavache. You've learned to build robust, secure, and performant applications that handle network requests intelligently.
