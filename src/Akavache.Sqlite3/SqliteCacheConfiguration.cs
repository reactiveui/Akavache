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

/// <summary>Prepares validated SQLite cache files using the builder's directory policy.</summary>
internal static class SqliteCacheConfiguration
{
    /// <summary>Validates the configuration and ensures the selected directory exists.</summary>
    /// <param name="name">The cache slot name used for the database file.</param>
    /// <param name="builder">The serializer and directory configuration.</param>
    /// <returns>The file path and serialization options for a cache.</returns>
    /// <exception cref="InvalidOperationException">No serializer is registered on the builder.</exception>
    internal static SqliteCacheOptions Prepare(string name, IAkavacheBuilder builder)
    {
        var serializer = builder.Serializer
            ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");

        ArgumentValidation.ThrowIfNullOrWhiteSpace(name);
        ArgumentValidation.ThrowIfNullOrWhiteSpace(builder.ApplicationName);
        var validatedName = SecurityUtilities.ValidateCacheName(name, nameof(name));
        var directory = builder.FileLocationOption switch
        {
            FileLocationOption.Legacy => builder.GetLegacyCacheDirectory(validatedName),
            _ => builder.GetIsolatedCacheDirectory(validatedName),
        };

        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory!);
        }

        return new(Path.Combine(directory!, $"{validatedName}.db"), serializer, builder.ForcedDateTimeKind);
    }
}
