# Migration from V11 to V12

This guide covers the changes between Akavache V11 and V12.

## Breaking Changes

#### System.Text.Json package split

`Akavache.SystemTextJson` has been split into two packages:

- **`Akavache.SystemTextJson`** - Pure System.Text.Json serializer (JSON only, no Newtonsoft dependency). AOT-compatible.
- **`Akavache.SystemTextJson.Bson`** - BSON format support using Newtonsoft.Json.Bson for encoding. Use this if you need to read/write BSON data with STJ.

**If you were using `SystemJsonBsonSerializer` or `UseSystemJsonBsonSerializer()`:**

```diff
- // v11: Single package
- using Akavache.SystemTextJson;
+ // v12: Add reference to Akavache.SystemTextJson.Bson
+ using Akavache.SystemTextJson;       // SystemJsonBsonSerializer lives here (same namespace)
+ using Akavache.SystemTextJson.Bson;  // Builder extensions (UseSystemJsonBsonSerializer)
```

Add a package/project reference to `Akavache.SystemTextJson.Bson`. The `SystemJsonBsonSerializer` class remains in the `Akavache.SystemTextJson` namespace so most code using the type directly won't need changes. Builder extension methods (`UseSystemJsonBsonSerializer`) moved to the `Akavache.SystemTextJson.Bson` namespace.

**If you were using only `SystemJsonSerializer` or `WithSerializerSystemTextJson()`:**

No changes needed. The `Akavache.SystemTextJson` package no longer pulls in Newtonsoft.Json.

#### SystemJsonBsonSerializer no longer inherits SystemJsonSerializer

`SystemJsonBsonSerializer` now uses composition instead of inheritance. It implements `ISerializer` directly and delegates JSON operations to an internal `SystemJsonSerializer` instance. Code that relied on `SystemJsonBsonSerializer is SystemJsonSerializer` will need updating.

#### AOT-safe `JsonTypeInfo` overloads live in Akavache.SystemTextJson, not ISerializer

`ISerializer` in `Akavache.Core` stays deliberately serializer-agnostic — it knows nothing about `System.Text.Json` and does not expose any `JsonTypeInfo<T>` overloads. The base interface still defines just the two reflection-based members:

```csharp
[RequiresUnreferencedCode("...")]
[RequiresDynamicCode("...")]
T? Deserialize<T>(byte[] bytes);

[RequiresUnreferencedCode("...")]
[RequiresDynamicCode("...")]
byte[] Serialize<T>(T item);
```

The AOT-safe `JsonTypeInfo<T>` path is provided by extension methods in the `Akavache.SystemTextJson.Bson` package (which transitively brings in `Akavache.SystemTextJson`). Importing the `Akavache.SystemTextJson` namespace gives you:

```csharp
using Akavache.SystemTextJson;

// At call sites, the call looks identical to an instance method:
var bytes  = serializer.Serialize(myModel, AppJsonContext.Default.MyModel);
var result = serializer.Deserialize<MyModel>(bytes, AppJsonContext.Default.MyModel);
```

Under the hood the extension methods type-check the runtime serializer:

- `SystemJsonSerializer` → routes to its static `DeserializeAot` / `SerializeAot` methods (pure `JsonTypeInfo` path, no reflection)
- `SystemJsonBsonSerializer` → routes through the same path (BSON cannot be AOT-encoded, so the AOT overload emits plain JSON bytes)
- Any other `ISerializer` (for example Newtonsoft-backed) → throws `NotSupportedException`, because those implementations do not have an AOT path. Use the non-typed `Deserialize<T>(byte[])` / `Serialize<T>(T)` overloads on Newtonsoft.

This indirection keeps `Akavache.Core` free of a hard dependency on `System.Text.Json`, so Newtonsoft-only consumers do not transitively pull it in.

#### Trim/AOT attributes still apply to the reflection path

The `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` attributes remain on the reflection-based `ISerializer.Deserialize<T>(byte[])` / `ISerializer.Serialize<T>(T)` members — they have not been removed. Callers that publish with trimming or NativeAOT will still see the analyzer warnings for those methods, which is intentional: the correct AOT-safe path is the `JsonTypeInfo<T>` extension method described above.

### New Features

#### AOT-safe serialization with JsonTypeInfo

`SystemJsonSerializer` now implements the `JsonTypeInfo<T>` overloads for fully AOT-compatible serialization:

```csharp
// Define your serializer context
[JsonSerializable(typeof(MyModel))]
public partial class AppJsonContext : JsonSerializerContext { }

// Use AOT-safe overloads
var serializer = new SystemJsonSerializer();
var bytes = serializer.Serialize(myModel, AppJsonContext.Default.MyModel);
var result = serializer.Deserialize(bytes, AppJsonContext.Default.MyModel);
```

#### Universal serializer registry

The `UniversalSerializer` fallback mechanism now uses an explicit registry instead of runtime type discovery. Serializer packages register themselves automatically when configured through builder methods. For manual registration:

```csharp
UniversalSerializer.RegisterSerializer(() => new SystemJsonSerializer());
```
