# Installation Guide

Akavache V11.1 uses a modular package structure. Choose the packages that match your needs.

## Package Overview

### Core Package (Included with Serializers, In Memory only)
```xml
<PackageReference Include="Akavache" Version="11.1.*" />
```

### SQLite Storage Backends (recommended)
```xml
<!-- SQLite persistence -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />

<!-- Encrypted SQLite persistence -->
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
```

### Serializers (Choose One - Required!)
```xml
<!-- System.Text.Json (fastest, .NET native) -->
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />

<!-- Newtonsoft.Json (most compatible) -->
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```

### Optional Extensions
```xml
<!-- Image/Bitmap support -->
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />

<!-- Settings helpers -->
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

## Supported Target Frameworks

Akavache V11.1 supports the following target frameworks:

- **.NET Framework 4.6.2, 4.7.2** - Full support
- **.NET Standard 2.0** - Cross-platform compatibility  
- **.NET 8.0, .NET 9.0** - Modern .NET support
- **Mobile Targets**:
  - `net9.0-android` - Android applications
  - `net9.0-ios` - iOS applications
  - `net9.0-maccatalyst` - Mac Catalyst applications
  - `net9.0-windows` - Windows applications (WinUI)

## Installation Scenarios

### Scenario 1: New Application (Recommended)

For most new applications, use SQLite with System.Text.Json:

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Scenario 2: Migrating from V10.x

For maximum compatibility when migrating:

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.NewtonsoftJson" Version="11.1.*" />
```

### Scenario 3: Encrypted Storage

For applications handling sensitive data:

```xml
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Scenario 4: Image/Media Applications

For applications caching images or media:

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
```

### Scenario 5: Complete Setup

For applications using all features:

```xml
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.EncryptedSqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
<PackageReference Include="Akavache.Drawing" Version="11.1.*" />
<PackageReference Include="Akavache.Settings" Version="11.1.*" />
```

## Platform-Specific Dependencies

### Mobile Applications (iOS/Android)

**Android DllNotFoundException with SQLitePCLRaw.lib.e_sqlite3:**

If you're getting `System.DllNotFoundException: 'e_sqlite3'` when using `SQLitePCLRaw.lib.e_sqlite3` on Android, use the appropriate bundle instead:

```xml
<!-- For Android (recommended): Use bundle_e_sqlite3 instead of lib.e_sqlite3 -->
<ItemGroup>
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.11" />
</ItemGroup>

<!-- Alternative: Use bundle_green for cross-platform compatibility -->
<ItemGroup>
    <PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.11" />
</ItemGroup>

<!-- If using Encrypted SQLite, also add: -->
<ItemGroup>
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlcipher" Version="2.1.11" />
</ItemGroup>
```

**Platform-specific bundle recommendations:**
- **Android**: `SQLitePCLRaw.bundle_e_sqlite3` or `SQLitePCLRaw.bundle_green`
- **iOS**: `SQLitePCLRaw.bundle_e_sqlite3` or `SQLitePCLRaw.bundle_green`
- **Desktop/Server**: `SQLitePCLRaw.bundle_e_sqlite3` works fine

### .NET MAUI Applications

```xml
<!-- Core Akavache packages -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />

<!-- SQLite support for all platforms -->
<PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.11" />
```

### WPF/WinUI Applications

```xml
<!-- Standard setup works out of the box -->
<PackageReference Include="Akavache.Sqlite3" Version="11.1.*" />
<PackageReference Include="Akavache.SystemTextJson" Version="11.1.*" />
```

### Linker Considerations

#### iOS Linker Issues
You may need to preserve certain types to prevent the linker from stripping them out in release builds:

```xml
<!-- Add to your .csproj file: -->
<ItemGroup>
  <TrimmerRootAssembly Include="SQLitePCLRaw.lib.e_sqlite3.## YOUR-PLATFORM ##" RootMode="All" />
</ItemGroup>
```

Or add LinkerPreserve.cs to your iOS project:
```csharp
public static class LinkerPreserve
{
    static LinkerPreserve()
    {
        var sqliteBlobCacheName = typeof(SqliteBlobCache).FullName;
        var encryptedSqliteBlobCacheName = typeof(EncryptedSqliteBlobCache).FullName;
    }
}
```

#### UWP Considerations

Ensure your UWP project targets a specific platform (x86, x64, ARM) rather than "Any CPU".

## Package Version Compatibility

| Component | V11.0 | V11.1 | Notes |
|-----------|-------|-------|-------|
| **Core Framework** | ✅ | ✅ | Backward compatible |
| **Data Format** | ✅ | ✅ | V11.1 reads V11.0 and V10.x data |
| **API** | ✅ | ✅ | Additive changes only |
| **Serializers** | ✅ | ✅ | Cross-compatible |

## Serializer Decision Matrix

| Framework Target | System.Text.Json | Newtonsoft.Json | Recommended |
|------------------|-------------------|-----------------|-------------|
| .NET Framework 4.6.2+ | ✅ (via NuGet) | ✅ | Either ✅ |
| .NET Framework 4.7.2+ | ✅ (via NuGet) | ✅ | System.Text.Json ⭐ |
| .NET Standard 2.0 | ✅ | ✅ | System.Text.Json ⭐ |
| .NET 8.0+ | ✅ (built-in) | ✅ | System.Text.Json ⭐ |
| .NET 9.0+ | ✅ (built-in) | ✅ | System.Text.Json ⭐ |
| Mobile (iOS/Android) | ✅ | ✅ | System.Text.Json ⭐ |
| V10.x Migration | ✅ | ✅ (easier) | Newtonsoft.Json (compatibility) |

**Key:**
- ✅ = Supported
- ⭐ = Recommended choice
- System.Text.Json = Better performance, smaller size
- Newtonsoft.Json = Maximum compatibility, easier V10.x migration

## Next Steps

After installation, proceed to:
1. **[Configuration](configuration.md)** - Set up the builder pattern
2. **[Basic Operations](basic-operations.md)** - Learn the core API
3. **[Platform Notes](platform-notes.md)** - Platform-specific setup if needed