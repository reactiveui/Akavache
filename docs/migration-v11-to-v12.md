# Migration from V11 to V12

This guide covers the breaking changes and new features between Akavache V11 and V12.

## SQLite backend rewrite

V12 replaces the sqlite-net-pcl ORM with direct SQLitePCLRaw 3.x access. Every SQL statement is prepared once and cached, with parameters bound positionally. This eliminates expression-tree translation overhead and most per-operation allocations on the hot path. Encrypted databases now use SQLite3MultipleCiphers (SQLite3MC) instead of sqlcipher.

No application code changes are required — `SqliteBlobCache` and `EncryptedSqliteBlobCache` retain their public API. The on-disk database format is compatible: V12 reads V11 databases directly.

## Breaking Changes

### Settings use observable-first API

`SettingsBase` properties are now `IObservable<T>` backed by `SettingsStream<T>`. The earlier `GetOrCreate<T>`/`SetOrCreate<T>` sync-getter pattern has been removed — it called `.Wait()` on the underlying observable chain, which deadlocked against the worker-thread SQLite queue.

```diff
  public class AppSettings : SettingsBase
  {
      public AppSettings() : base(nameof(AppSettings)) { }

-     public bool Enabled
-     {
-         get => GetOrCreate(true);
-         set => SetOrCreate(value);
-     }
+     public IObservable<bool> Enabled => GetOrCreateObservable(true);
+     public IObservable<Unit> SetEnabled(bool value) => SetObservable(value, nameof(Enabled));
  }
```

Callers subscribe to `Enabled` to receive the current value plus any future updates, or call `await settings.Enabled.FirstAsync()` for a one-shot read.

### System.Text.Json package split

`Akavache.SystemTextJson` has been split into two packages:

- **`Akavache.SystemTextJson`** — Pure System.Text.Json serializer (JSON only, no Newtonsoft dependency). AOT-compatible.
- **`Akavache.SystemTextJson.Bson`** — BSON format support using Newtonsoft.Json.Bson for encoding.

**If you were using `SystemJsonBsonSerializer` or `UseSystemJsonBsonSerializer()`:**

```diff
- // v11: Single package
- using Akavache.SystemTextJson;
+ // v12: Add reference to Akavache.SystemTextJson.Bson
+ using Akavache.SystemTextJson;       // SystemJsonBsonSerializer lives here (same namespace)
+ using Akavache.SystemTextJson.Bson;  // Builder extensions (UseSystemJsonBsonSerializer)
```

**If you were using only `SystemJsonSerializer` or `WithSerializerSystemTextJson()`:** no changes needed. The `Akavache.SystemTextJson` package no longer pulls in Newtonsoft.Json.

### SystemJsonBsonSerializer uses composition instead of inheritance

`SystemJsonBsonSerializer` now implements `ISerializer` directly and delegates JSON operations to an internal `SystemJsonSerializer` instance. Code that relied on `SystemJsonBsonSerializer is SystemJsonSerializer` will need updating.

### AOT-safe JsonTypeInfo overloads live in Akavache.SystemTextJson

`ISerializer` in `Akavache.Core` stays serializer-agnostic — it does not expose any `JsonTypeInfo<T>` overloads. The AOT-safe path is provided by extension methods in the `Akavache.SystemTextJson` namespace:

```csharp
using Akavache.SystemTextJson;

var bytes  = serializer.Serialize(myModel, AppJsonContext.Default.MyModel);
var result = serializer.Deserialize<MyModel>(bytes, AppJsonContext.Default.MyModel);
```

## New Features

### AOT-safe serialization with JsonTypeInfo

`SystemJsonSerializer` implements `JsonTypeInfo<T>` overloads for fully AOT-compatible serialization:

```csharp
[JsonSerializable(typeof(MyModel))]
public partial class AppJsonContext : JsonSerializerContext { }

var serializer = new SystemJsonSerializer();
var bytes = serializer.Serialize(myModel, AppJsonContext.Default.MyModel);
var result = serializer.Deserialize(bytes, AppJsonContext.Default.MyModel);
```

### Universal serializer registry

`UniversalSerializer` uses an explicit thread-safe registry instead of runtime type discovery. Serializer packages register themselves automatically when configured through builder methods. For manual registration:

```csharp
UniversalSerializer.RegisterSerializer(() => new SystemJsonSerializer());
```

### V10 to V11 migration shim

The `Akavache.V10toV11` package provides a one-time migration path from V10 databases. This is a compatibility-only package — new applications should use `Akavache.Sqlite3` directly.
