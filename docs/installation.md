# Installation Guide

Akavache V11.1 uses a modular package structure. Choose the packages that match your needs:

## Package Matrix

### Core Package (In-Memory Only)
```xml
<PackageReference Include="Akavache" Version="11.1.*" />
```
**Use when:** Testing, temporary caching, or when persistence is not needed.

### Storage Backends (Choose One)

#### SQLite Persistence (Recommended)
```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
```
**Use when:** You need persistent caching for most applications.

#### Encrypted SQLite Persistence
```xml
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
```
**Use when:** You need to store sensitive data like credentials or personal information.

### Serializers (Choose One - Required!)

#### System.Text.Json (Recommended)
```xml
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```
**Best for:** 
- New projects
- .NET Framework 4.6.2+ applications
- .NET 8.0+ applications  
- .NET Standard 2.0+ applications
- Performance-critical scenarios
- AOT (Ahead-of-Time) compilation compatibility (.NET 8.0+ only)

#### Newtonsoft.Json (Maximum Compatibility)
```xml
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```
**Best for:**
- Legacy projects already using Newtonsoft.Json
- Complex serialization requirements
- Migrating from V10.x
- Custom converters and advanced JSON features

### Optional Extensions

#### Image/Bitmap Support
```xml
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```
**Use when:** Caching images, thumbnails, or other bitmap data.

#### Settings Helpers
```xml
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```
**Use when:** Managing application settings and user preferences.

## Common Package Combinations

### Basic Web API Caching
```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Mobile App with Images
```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### Desktop App with Settings
```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

### Secure Application
```xml
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Legacy Application Migration
```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```

## Platform Requirements

| Platform | SQLite | Encrypted SQLite | Drawing | Settings |
|----------|--------|------------------|---------|----------|
| **iOS** | ✅ | ✅ | ✅ | ✅ |
| **Android** | ✅ | ✅ | ✅ | ✅ |
| **Windows** | ✅ | ✅ | ✅ | ✅ |
| **macOS** | ✅ | ✅ | ✅ | ✅ |
| **Linux** | ✅ | ✅ | ⚠️* | ✅ |
| **WASM/Blazor** | ⚠️** | ❌ | ❌ | ✅ |

\* Drawing support on Linux requires additional native dependencies  
\** SQLite in WASM has limitations; consider in-memory cache instead

## BSON Variants

Both serializers offer BSON (Binary JSON) variants for better performance with binary data:

### System.Text.Json BSON
```xml
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```
Use `SystemJsonBsonSerializer` instead of `SystemJsonSerializer`.

### Newtonsoft.Json BSON
```xml
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```
Use `NewtonsoftBsonSerializer` instead of `NewtonsoftJsonSerializer`.

**When to use BSON:**
- Storing large amounts of binary data
- Performance-critical scenarios with complex objects
- When JSON text overhead is significant

## Version Compatibility

### Cross-Serializer Data Reading
Akavache V11.1 can read data written by different serializers:
- Data written with Newtonsoft.Json can be read with System.Text.Json
- Data written with JSON can be read with BSON variants
- Data written with V10.x can be read in V11.1

### Migration Path
1. **Install V11.1 packages** alongside existing V10.x references
2. **Initialize V11.1 system** using migration helper (see [migration guide](./migration-v10-to-v11.md))
3. **Test compatibility** with existing data
4. **Remove V10.x packages** once migration is complete

## Next Steps

After installing packages:

1. **Configure your application** - see [Configuration Guide](./configuration.md)
2. **Choose your serializer** - see [Serializers Guide](./serializers.md)
3. **Learn basic operations** - see [Basic Operations](./basic-operations.md)
4. **Review platform-specific notes** - see [Platform Notes](./platform-notes.md)