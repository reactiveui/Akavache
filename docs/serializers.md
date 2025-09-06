# Serializers Guide

Akavache V11.1 supports multiple serialization backends. Choose the one that best fits your project's needs.

## Available Serializers

### System.Text.Json (Recommended)

**Package:** `Akavache.SystemTextJson`

```csharp
using Akavache.SystemTextJson;

// Standard JSON serialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder => 
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// BSON serialization (faster for binary data)
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonBsonSerializer>(builder => 
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

**Best for:**
- ✅ New projects starting with .NET 8.0+
- ✅ .NET Standard 2.0+ applications
- ✅ Performance-critical applications
- ✅ AOT (Ahead-of-Time) compilation scenarios
- ✅ Blazor WebAssembly applications
- ✅ Microservices and cloud-native applications

**Benefits:**
- **High performance** - Native .NET serialization
- **Small memory footprint** - Optimized for modern .NET
- **AOT compatible** - Works with Native AOT compilation
- **Built-in support** - No external dependencies
- **BSON support** - Binary serialization for performance

### Newtonsoft.Json (Maximum Compatibility)

**Package:** `Akavache.NewtonsoftJson`

```csharp
using Akavache.NewtonsoftJson;

// Standard JSON serialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftJsonSerializer>(builder => 
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());

// BSON serialization
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<NewtonsoftBsonSerializer>(builder => 
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithSqliteDefaults());
```

**Best for:**
- ✅ Legacy projects already using Newtonsoft.Json
- ✅ Complex serialization scenarios
- ✅ Custom converters and advanced JSON features
- ✅ Migration from V10.x (maintains compatibility)
- ✅ .NET Standard 2.0+ applications

**Benefits:**
- **Maximum compatibility** - Works with .NET Standard 2.0 and .NET 8.0+
- **Rich feature set** - Extensive customization options
- **Proven stability** - Battle-tested in countless applications
- **Custom converters** - Extensive ecosystem of converters
- **BSON support** - Binary serialization option

## Choosing a Serializer

### Decision Matrix

| Factor | System.Text.Json | Newtonsoft.Json |
|--------|------------------|-----------------|
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Memory Usage** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **AOT Support** | ⭐⭐⭐⭐⭐ | ❌ |
| **Feature Richness** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Legacy Compatibility** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **.NET Standard/Core** | ⭐⭐⭐⭐⭐ (.NET Standard 2.0+) | ⭐⭐⭐⭐⭐ |

### Use System.Text.Json when:
- Starting a new project on .NET 8.0+ or .NET Standard 2.0+
- Performance is critical
- Memory usage is a concern
- Planning to use AOT compilation (.NET 8.0+ only)
- Building microservices or cloud applications

### Use Newtonsoft.Json when:
- Migrating from V10.x
- Need maximum feature richness and customization
- Using complex custom converters
- Working with .NET Standard 2.0 applications that require specific Newtonsoft features
- Have existing Newtonsoft.Json configurations
- Require specific Newtonsoft.Json features not available in System.Text.Json

## BSON Variants

Both serializers offer BSON (Binary JSON) variants for improved performance with binary data:

### When to Use BSON
- **Large objects** - BSON is more efficient for large data structures
- **Binary data** - Better handling of byte arrays and binary content
- **Performance critical** - BSON serialization is typically faster
- **Network efficiency** - Smaller payload sizes

### BSON Performance Comparison
```csharp
// For large objects, BSON can be 20-40% faster
var largeObject = new ComplexData { /* ... */ };

// JSON serialization
await cache.InsertObject("key", largeObject, TimeSpan.FromHours(1));

// BSON serialization (SystemJsonBsonSerializer or NewtonsoftBsonSerializer)
// Typically faster for large objects
await cache.InsertObject("key", largeObject, TimeSpan.FromHours(1));
```

## Custom Serializer Configuration

### System.Text.Json Configuration

```csharp
using System.Text.Json;
using Akavache.SystemTextJson;

// Create custom JsonSerializerOptions
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
};

// Use custom options with the serializer
var customSerializer = new SystemJsonSerializer(options);

// Manual cache creation with custom serializer
var cache = new SqliteBlobCache("custom.db", customSerializer);
```

### Newtonsoft.Json Configuration

```csharp
using Newtonsoft.Json;
using Akavache.NewtonsoftJson;

// Create custom JsonSerializerSettings
var settings = new JsonSerializerSettings
{
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    NullValueHandling = NullValueHandling.Ignore,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

// Use custom settings with the serializer
var customSerializer = new NewtonsoftJsonSerializer(settings);

// Manual cache creation with custom serializer
var cache = new SqliteBlobCache("custom.db", customSerializer);
```

## Cross-Serializer Compatibility

Akavache V11.1 can read data written by different serializers:

```csharp
// Data written with Newtonsoft.Json can be read with System.Text.Json
// Data written with JSON can be read with BSON variants
// This enables gradual migration between serializers

public async Task MigrateSerializers()
{
    // Read data written with Newtonsoft.Json
    var oldCache = new SqliteBlobCache("old.db", new NewtonsoftJsonSerializer());
    var data = await oldCache.GetObject<MyData>("key");
    
    // Write the same data with System.Text.Json
    var newCache = new SqliteBlobCache("new.db", new SystemJsonSerializer());
    await newCache.InsertObject("key", data);
}
```

## Serialization Best Practices

### 1. Data Transfer Objects (DTOs)
```csharp
// Use simple, serializable classes for cached data
public class UserProfileDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime LastLogin { get; set; }
    
    // Avoid complex properties that don't serialize well
    // Avoid circular references
    // Keep it simple and flat when possible
}
```

### 2. Nullable Reference Types
```csharp
// System.Text.Json handles nullable reference types well
public class ModernDto
{
    public string RequiredProperty { get; set; } = string.Empty;
    public string? OptionalProperty { get; set; }
}
```

### 3. DateTime Handling
```csharp
// Be explicit about DateTime kinds
public class EventDto
{
    // Always use UTC for cached DateTime values
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    // Or use DateTimeOffset for timezone awareness
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
```

### 4. Version Compatibility
```csharp
// Add version fields for future compatibility
public class VersionedDto
{
    public int Version { get; set; } = 1;
    public string Data { get; set; } = string.Empty;
    
    // Can add new properties in v2 while maintaining compatibility
}
```

## Performance Optimization

### 1. Choose the Right Serializer for Your Data
```csharp
// For simple objects: System.Text.Json
public class SimpleData { public string Value { get; set; } }

// For complex objects with custom logic: Newtonsoft.Json
public class ComplexData 
{ 
    [JsonConverter(typeof(CustomConverter))]
    public CustomType CustomProperty { get; set; }
}
```

### 2. Use BSON for Large Objects
```csharp
// For objects > 1KB, consider BSON
public class LargeDataSet
{
    public byte[] BinaryData { get; set; } // BSON handles this efficiently
    public List<ComplexItem> Items { get; set; } // Large collections
}
```

### 3. Minimize Serialization Overhead
```csharp
// Cache frequently accessed computed values
public class OptimizedDto
{
    public string RawData { get; set; }
    
    // Cache expensive computed properties
    public string ComputedValue { get; set; } // Pre-computed, not calculated each time
}
```

## Migration Between Serializers

### Gradual Migration Strategy
```csharp
public class HybridCacheService
{
    private readonly IBlobCache _newtonCache;
    private readonly IBlobCache _systemTextCache;
    
    public async Task<T> GetObject<T>(string key)
    {
        try
        {
            // Try new serializer first
            return await _systemTextCache.GetObject<T>(key);
        }
        catch (KeyNotFoundException)
        {
            // Fallback to old serializer and migrate
            var data = await _newtonCache.GetObject<T>(key);
            await _systemTextCache.InsertObject(key, data);
            await _newtonCache.Invalidate(key); // Clean up old data
            return data;
        }
    }
}
```

## Troubleshooting Serialization Issues

### Common Problems and Solutions

#### 1. "Cannot deserialize the current JSON"
**Problem:** Data structure changed between versions.
**Solution:** Use versioned DTOs or custom converters.

#### 2. "Circular reference detected"
**Problem:** Objects have circular references.
**Solution:** Use ReferenceLoopHandling.Ignore or redesign DTOs.

#### 3. "DateTime precision loss"
**Problem:** DateTime serialization loses precision.
**Solution:** Use DateTimeOffset or specify DateTime format.

#### 4. Performance issues with large objects
**Problem:** Slow serialization/deserialization.
**Solution:** Switch to BSON variant or optimize object structure.

## Next Steps

- [Learn about cache types](./cache-types.md)
- [Master basic operations](./basic-operations.md)
- [Review platform-specific considerations](./platform-notes.md)
- [Explore performance optimization](./performance.md)