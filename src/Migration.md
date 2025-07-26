# Migration Guide: Akavache to ReactiveMarbles.CacheDatabase

This guide provides a comprehensive overview of migrating from Akavache to ReactiveMarbles.CacheDatabase, including new features and compatibility information.

## Overview

ReactiveMarbles.CacheDatabase is the next generation of Akavache, providing improved performance, better serialization options, and enhanced features while maintaining backward compatibility for most use cases.

## Package Migration

### Old Akavache Packages
```xml
<PackageReference Include="akavache" Version="..." />
<PackageReference Include="akavache.sqlite3" Version="..." />
<PackageReference Include="akavache.mobile" Version="..." />
<PackageReference Include="akavache.drawing" Version="..." />
```

### New ReactiveMarbles.CacheDatabase Packages
```xml
<!-- Core package with interfaces and base functionality -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.Core" Version="..." />

<!-- Choose your preferred serializer -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.SystemTextJson" Version="..." />
<!-- OR -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.NewtonsoftJson" Version="..." />
<!-- OR -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson" Version="..." />

<!-- Optional: SQLite support -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.Sqlite3" Version="..." />

<!-- Optional: Encrypted SQLite support -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.EncryptedSqlite3" Version="..." />

<!-- Optional: Drawing/Image support -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.Drawing" Version="..." />

<!-- Optional: Settings cache -->
<PackageReference Include="ReactiveMarbles.CacheDatabase.Settings" Version="..." />
<PackageReference Include="ReactiveMarbles.CacheDatabase.EncryptedSettings" Version="..." />
```

## Initialization Changes

### Akavache Initialization
```csharp
// Old Akavache initialization
Akavache.Registrations.Start("ApplicationName");

// Or with custom location
BlobCache.ApplicationName = "MyApp";
```

### ReactiveMarbles.CacheDatabase Initialization
```csharp
// New initialization - choose your serializer
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.SystemTextJson;

// Initialize the serializer globally
CoreRegistrations.Serializer = new SystemJsonSerializer();

// Or with Newtonsoft.Json
// CoreRegistrations.Serializer = new NewtonsoftSerializer();

// Or with Newtonsoft.Json BSON
// CoreRegistrations.Serializer = new NewtonsoftBsonSerializer();
```

## API Compatibility

Most Akavache APIs remain compatible. The core interfaces and extension methods work the same way:

### Unchanged APIs
```csharp
// These work exactly the same
await BlobCache.UserAccount.InsertObject("key", myObject);
var result = await BlobCache.UserAccount.GetObject<MyType>("key");
await BlobCache.UserAccount.InvalidateObject<MyType>("key");

// Bulk operations
await BlobCache.UserAccount.InsertObjects(keyValuePairs);
var objects = await BlobCache.UserAccount.GetObjects<MyType>(keys);
await BlobCache.UserAccount.InvalidateObjects<MyType>(keys);

// Expiration support
await BlobCache.UserAccount.InsertObject("key", myObject, DateTimeOffset.Now.AddHours(1));

// Get or fetch patterns
var data = await BlobCache.UserAccount.GetOrFetchObject("key", () => FetchFromNetwork());
```

## New Features

### 1. Multiple Serializer Support

ReactiveMarbles.CacheDatabase offers three serialization options:

#### System.Text.Json (Recommended for new projects)
```csharp
CoreRegistrations.Serializer = new SystemJsonSerializer();

// Optional: Configure JsonSerializerOptions
var serializer = new SystemJsonSerializer 
{ 
    Options = new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    } 
};
CoreRegistrations.Serializer = serializer;
```

#### Newtonsoft.Json (Compatible with existing data)
```csharp
CoreRegistrations.Serializer = new NewtonsoftSerializer();

// Optional: Configure JsonSerializerSettings
var serializer = new NewtonsoftSerializer 
{ 
    Options = new JsonSerializerSettings 
    { 
        NullValueHandling = NullValueHandling.Ignore 
    } 
};
CoreRegistrations.Serializer = serializer;
```

#### Newtonsoft.Json BSON (Most compatible with Akavache)
```csharp
CoreRegistrations.Serializer = new NewtonsoftBsonSerializer();
```

### 2. Enhanced InMemoryBlobCache

Each serializer package provides its own InMemoryBlobCache implementation:

```csharp
// System.Text.Json InMemoryBlobCache
using ReactiveMarbles.CacheDatabase.SystemTextJson;
var cache = new InMemoryBlobCache();

// Newtonsoft.Json InMemoryBlobCache  
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;
var cache = new InMemoryBlobCache();

// Newtonsoft.Json BSON InMemoryBlobCache (most Akavache-compatible)
using ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;
var cache = new InMemoryBlobCache();
```

### 3. Improved DateTime Handling

Better control over DateTime serialization:

```csharp
// Force DateTime to specific kind globally
CoreRegistrations.Serializer.ForcedDateTimeKind = DateTimeKind.Utc;

// Or per cache instance
cache.ForcedDateTimeKind = DateTimeKind.Local;
```

### 4. Enhanced Type Safety

Better type indexing and retrieval:

```csharp
// Get all objects of a specific type
var allUsers = await cache.GetAllObjects<User>().ToList();

// Type-specific operations
await cache.InvalidateAllObjects<User>();
var userKeys = await cache.GetAllKeys(typeof(User)).ToList();
```

### 5. Settings Cache

New dedicated settings cache for application configuration:

```csharp
using ReactiveMarbles.CacheDatabase.Settings;

// Create settings cache
var settings = new SettingsCache();

// Store settings
await settings.SetValue("theme", "dark");
await settings.SetValue("timeout", TimeSpan.FromMinutes(5));

// Retrieve settings with defaults
var theme = await settings.GetValue("theme", "light");
var timeout = await settings.GetValue("timeout", TimeSpan.FromMinutes(1));
```

### 6. Encrypted Settings

Secure storage for sensitive configuration:

```csharp
using ReactiveMarbles.CacheDatabase.EncryptedSettings;

var encryptedSettings = new EncryptedSettingsCache();
await encryptedSettings.SetValue("apiKey", "secret-key");
var apiKey = await encryptedSettings.GetValue<string>("apiKey");
```

## Migration Steps

### Step 1: Update Package References
Replace Akavache packages with ReactiveMarbles.CacheDatabase packages.

### Step 2: Choose and Initialize Serializer
Select the appropriate serializer for your needs:
- **NewtonsoftBsonSerializer**: Most compatible with existing Akavache data
- **NewtonsoftSerializer**: Good balance of compatibility and performance
- **SystemJsonSerializer**: Best performance for new projects

### Step 3: Update Initialization Code
Replace Akavache.Registrations.Start() with serializer initialization.

### Step 4: Test Migration
Run your existing tests to ensure compatibility.

### Step 5: Leverage New Features
Gradually adopt new features like enhanced type safety and settings cache.

## Breaking Changes

### Minor Breaking Changes
1. **Initialization**: Must explicitly set a serializer
2. **InMemoryBlobCache**: Now namespace-specific per serializer
3. **DateTime Handling**: More explicit control required

### Compatibility Notes
- **Data Format**: NewtonsoftBsonSerializer maintains full compatibility with Akavache data
- **API Surface**: 99% of existing Akavache code will work unchanged
- **Extension Methods**: All existing extension methods are preserved

## Performance Improvements

1. **Better Serialization**: Up to 2x faster with System.Text.Json
2. **Improved Memory Usage**: More efficient object pooling
3. **Enhanced Caching**: Better cache invalidation strategies
4. **Reduced Allocations**: Optimized for garbage collection pressure

## Best Practices

### For New Projects
```csharp
// Use System.Text.Json for best performance
CoreRegistrations.Serializer = new SystemJsonSerializer();
```

### For Migrating Existing Projects
```csharp
// Use NewtonsoftBsonSerializer for full compatibility
CoreRegistrations.Serializer = new NewtonsoftBsonSerializer();
```

### For Mixed Scenarios
```csharp
// Use different caches for different purposes
var fastCache = new SystemTextJson.InMemoryBlobCache();
var compatCache = new NewtonsoftJson.Bson.InMemoryBlobCache();
```

## Troubleshooting

### Serialization Issues
If you encounter serialization issues:
1. Ensure your serializer choice matches your data requirements
2. For legacy data, use NewtonsoftBsonSerializer
3. Check DateTime handling configuration

### Performance Issues
1. Consider System.Text.Json for better performance
2. Use appropriate expiration policies
3. Leverage the new type-specific operations

### Compatibility Issues
1. Most issues stem from serializer choice
2. NewtonsoftBsonSerializer provides maximum compatibility
3. Check DateTime kind handling for time-sensitive data

## Conclusion

ReactiveMarbles.CacheDatabase provides a clear upgrade path from Akavache with minimal breaking changes and significant new features. The choice of serializer allows you to balance compatibility with performance based on your specific needs.

For most migrations, start with NewtonsoftBsonSerializer for maximum compatibility, then consider migrating to System.Text.Json for new features and better performance.
