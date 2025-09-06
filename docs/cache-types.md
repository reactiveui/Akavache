# Cache Types Guide

Akavache provides four different cache types, each designed for specific use cases and data storage needs.

## Overview

| Cache Type | Purpose | Persistence | Sharing | Encryption |
|------------|---------|-------------|---------|-------------|
| **UserAccount** | User-specific data | ✅ Persistent | Per-user | Optional |
| **LocalMachine** | App-wide data | ✅ Persistent | All users | Optional |
| **Secure** | Sensitive data | ✅ Persistent | Per-user | ✅ Required |
| **InMemory** | Temporary data | ❌ Memory only | Current session | ❌ None |

## UserAccount Cache

**Purpose:** Store user-specific data that should persist across application sessions.

**Characteristics:**
- ✅ Persistent across app restarts
- ✅ Separate cache for each user account
- ✅ Automatically cleared when user logs out (if implemented)
- ✅ Survives app updates

**Best for:**
- User preferences and settings
- User-specific cached API responses
- Personalized content
- User session data

**Example:**
```csharp
// Store user-specific data
await CacheDatabase.UserAccount.InsertObject("user_preferences", userPrefs);
await CacheDatabase.UserAccount.InsertObject("user_profile", userProfile);

// Retrieve user data
var preferences = await CacheDatabase.UserAccount.GetObject<UserPreferences>("user_preferences");
var profile = await CacheDatabase.UserAccount.GetObject<UserProfile>("user_profile");
```

**File Location:**
- **Windows**: `%LocalAppData%\[AppName]\BlobCache\userAccount.db`
- **macOS**: `~/Library/Caches/[AppName]/userAccount.db`
- **iOS**: `Library/Caches/userAccount.db`
- **Android**: `{ApplicationData}/cache/userAccount.db`

## LocalMachine Cache

**Purpose:** Store application-wide data shared across all users of the device.

**Characteristics:**
- ✅ Persistent across app restarts
- ✅ Shared between all users on the same device
- ✅ Survives user account changes
- ✅ Survives app updates

**Best for:**
- Application metadata and configuration
- Shared reference data (e.g., country lists, categories)
- Application-wide cached API responses
- Global settings and defaults

**Example:**
```csharp
// Store app-wide data
await CacheDatabase.LocalMachine.InsertObject("app_config", appConfig);
await CacheDatabase.LocalMachine.InsertObject("country_list", countries);

// Retrieve app data
var config = await CacheDatabase.LocalMachine.GetObject<AppConfig>("app_config");
var countries = await CacheDatabase.LocalMachine.GetObject<List<Country>>("country_list");
```

**File Location:**
- **Windows**: `%LocalAppData%\[AppName]\BlobCache\localMachine.db`
- **macOS**: `~/Library/Caches/[AppName]/localMachine.db`
- **iOS**: `Library/Caches/localMachine.db`
- **Android**: `{ApplicationData}/cache/localMachine.db`

## Secure Cache

**Purpose:** Store sensitive data that requires encryption at rest.

**Characteristics:**
- ✅ Persistent across app restarts
- ✅ Encrypted storage (AES-256)
- ✅ Per-user isolation
- ✅ Protected against casual inspection
- ⚠️ Not protected against determined attackers with device access

**Best for:**
- Authentication tokens and credentials
- Personally identifiable information (PII)
- Payment information (temporarily)
- Sensitive user data
- API keys and secrets

**Example:**
```csharp
// Store sensitive data (automatically encrypted)
await CacheDatabase.Secure.InsertObject("auth_token", authToken);
await CacheDatabase.Secure.InsertObject("user_credentials", credentials);

// Retrieve sensitive data (automatically decrypted)
var token = await CacheDatabase.Secure.GetObject<AuthToken>("auth_token");
var credentials = await CacheDatabase.Secure.GetObject<UserCredentials>("user_credentials");
```

**Security Notes:**
- Uses industry-standard AES-256 encryption
- Keys are derived from device-specific information
- Data is encrypted before writing to disk
- Automatically decrypted when retrieved
- **Not suitable** for highly sensitive data like credit card numbers

**File Location:**
- **Windows**: `%LocalAppData%\[AppName]\BlobCache\secret.db` (encrypted)
- **macOS**: `~/Library/Caches/[AppName]/secret.db` (encrypted)
- **iOS**: `Library/Caches/secret.db` (encrypted + iOS keychain integration)
- **Android**: `{ApplicationData}/cache/secret.db` (encrypted)

## InMemory Cache

**Purpose:** Store temporary data that doesn't need to persist beyond the current session.

**Characteristics:**
- ❌ Lost when application terminates
- ✅ Very fast access (RAM-based)
- ✅ No disk I/O overhead
- ✅ Automatic memory management
- ✅ Thread-safe

**Best for:**
- Temporary caching during processing
- Request/response caching for current session
- Computed values that are expensive to recalculate
- Testing and development scenarios
- Short-lived cache needs

**Example:**
```csharp
// Store temporary data
await CacheDatabase.InMemory.InsertObject("temp_data", tempData, TimeSpan.FromMinutes(30));
await CacheDatabase.InMemory.InsertObject("computed_result", result);

// Retrieve temporary data
var temp = await CacheDatabase.InMemory.GetObject<TempData>("temp_data");
var result = await CacheDatabase.InMemory.GetObject<ComputedResult>("computed_result");
```

## Choosing the Right Cache Type

### Decision Tree

```
Is the data sensitive (passwords, tokens, PII)?
├── YES: Use Secure Cache
└── NO: Continue...

Should the data persist across app restarts?
├── NO: Use InMemory Cache
└── YES: Continue...

Is the data user-specific?
├── YES: Use UserAccount Cache
└── NO: Use LocalMachine Cache
```

### Examples by Use Case

#### User Management
```csharp
// User profile - UserAccount (user-specific, persistent)
await CacheDatabase.UserAccount.InsertObject("profile", userProfile);

// Auth token - Secure (sensitive, persistent)
await CacheDatabase.Secure.InsertObject("auth_token", token);

// User session - InMemory (temporary, current session only)
await CacheDatabase.InMemory.InsertObject("session_data", sessionData);
```

#### Application Data
```csharp
// App settings - LocalMachine (shared, persistent)
await CacheDatabase.LocalMachine.InsertObject("app_settings", settings);

// Reference data - LocalMachine (shared, persistent)
await CacheDatabase.LocalMachine.InsertObject("categories", categories);

// API responses - UserAccount (user-specific, persistent)
await CacheDatabase.UserAccount.InsertObject("api_data", apiResponse);
```

#### E-commerce Application
```csharp
// Product catalog - LocalMachine (shared across users)
await CacheDatabase.LocalMachine.InsertObject("products", productList);

// User's cart - UserAccount (user-specific)
await CacheDatabase.UserAccount.InsertObject("shopping_cart", cart);

// Payment token - Secure (sensitive)
await CacheDatabase.Secure.InsertObject("payment_token", paymentToken);

// Search results - InMemory (temporary)
await CacheDatabase.InMemory.InsertObject("search_results", results, TimeSpan.FromMinutes(15));
```

## Cache Isolation and Data Safety

### User Account Isolation
- Each user account gets separate UserAccount and Secure cache files
- Data from one user cannot be accessed by another user
- Switching users automatically switches cache contexts

### App Instance Isolation
- Each application has separate cache directories
- Multiple versions of the same app can coexist safely
- Cache data is isolated by application name

### Platform-Specific Behavior

#### iOS
- Caches integrate with iOS security features
- Secure cache uses iOS Keychain for key storage
- Caches are backed up unless configured otherwise

#### Android
- Caches respect Android security model
- Secure cache uses Android security features
- Cache files are protected by app sandboxing

#### Windows
- Caches stored in user-specific directories
- DPAPI integration for secure cache encryption
- Supports multi-user scenarios

## Performance Characteristics

| Cache Type | Read Speed | Write Speed | Storage Overhead | Memory Usage |
|------------|------------|-------------|------------------|--------------|
| **InMemory** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | None | High |
| **UserAccount** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Low | Low |
| **LocalMachine** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Low | Low |
| **Secure** | ⭐⭐⭐ | ⭐⭐⭐ | Medium | Low |

## Advanced Usage

### Custom Cache Instances
```csharp
// Create custom cache instances for specific needs
AppBuilder.CreateSplatBuilder()
    .WithAkavacheCacheDatabase<SystemJsonSerializer>(builder =>
        builder.WithApplicationName("MyApp")
               .WithSqliteProvider()
               .WithUserAccount(new SqliteBlobCache("custom-user.db"))
               .WithLocalMachine(new SqliteBlobCache("custom-machine.db"))
               .WithSecure(new EncryptedSqliteBlobCache("custom-secure.db", "password"))
               .WithInMemory(new InMemoryBlobCache()));
```

### Multiple Cache Instances
```csharp
// Use different caches for different purposes
public class CacheManager
{
    private readonly IBlobCache _userCache = CacheDatabase.UserAccount;
    private readonly IBlobCache _appCache = CacheDatabase.LocalMachine;
    private readonly IBlobCache _tempCache = CacheDatabase.InMemory;
    
    public async Task StoreUserData<T>(string key, T data)
    {
        await _userCache.InsertObject(key, data);
    }
    
    public async Task StoreAppData<T>(string key, T data)
    {
        await _appCache.InsertObject(key, data);
    }
    
    public async Task StoreTempData<T>(string key, T data, TimeSpan expiry)
    {
        await _tempCache.InsertObject(key, data, expiry);
    }
}
```

## Best Practices

1. **Use the most appropriate cache type** for your data
2. **Don't store highly sensitive data** in any cache (use secure storage APIs instead)
3. **Set appropriate expiration times** especially for InMemory cache
4. **Consider data size** when choosing between cache types
5. **Test cache behavior** across app updates and user switches
6. **Handle cache misses gracefully** in your application logic
7. **Use consistent key naming conventions** across cache types

## Next Steps

- [Learn basic operations](./basic-operations.md)
- [Explore advanced patterns](./patterns/)
- [Review platform-specific notes](./platform-notes.md)
- [Understand performance implications](./performance.md)