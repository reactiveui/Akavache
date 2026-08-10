// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif
#else
#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif
#endif

/// <summary>
/// Works out which database file a named cache maps to, and what serializes into it. The plain and
/// encrypted factories resolve this identically — they differ only in the cache they then build —
/// so the work lives here rather than once per package.
/// </summary>
internal static class SqliteCacheTarget
{
    /// <summary>Resolves the database file for a cache kind, creating its directory when the configured location does not pre-create one.</summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <returns>The database file path and the serializer to open it with.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no serializer has been registered on the builder.</exception>
    internal static (string FilePath, ISerializer Serializer) Resolve(string name, IAkavacheBuilder builder)
    {
        var serializer = builder.Serializer
            ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");

        ArgumentValidation.ThrowIfNullOrWhiteSpace(name);
        ArgumentValidation.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

        // Validate cache name to prevent path traversal attacks
        var validatedName = SecurityUtilities.ValidateCacheName(name, nameof(name));

        var directory = builder.FileLocationOption switch
        {
            FileLocationOption.Legacy => builder.GetLegacyCacheDirectory(validatedName),
            _ => builder.GetIsolatedCacheDirectory(validatedName),
        };

        // Ensure the cache directory exists (legacy paths may not be pre-created).
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory!);
        }

        return (Path.Combine(directory!, $"{validatedName}.db"), serializer);
    }

    /// <summary>Carries the builder's forced <see cref="DateTimeKind"/>, when it set one, onto the cache.</summary>
    /// <param name="cache">The freshly built cache.</param>
    /// <param name="builder">The Akavache builder the cache was built from.</param>
    internal static void ApplyBuilderOptions(IBlobCache cache, IAkavacheBuilder builder)
    {
        if (!builder.ForcedDateTimeKind.HasValue)
        {
            return;
        }

        cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
    }
}
