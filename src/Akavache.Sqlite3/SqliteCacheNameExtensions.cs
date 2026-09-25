// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>Internal factory helpers that create SQLite blob caches from a cache slot name.</summary>
internal static class SqliteCacheNameExtensions
{
    /// <summary>Extension members for <c>string</c>.</summary>
    /// <param name="name">The cache slot name, which becomes the database file name.</param>
    extension(string name)
    {
        /// <summary>Creates a <see cref="SqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.</summary>
        /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
        /// <returns>A configured <see cref="SqliteBlobCache"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <c>builder.Serializer</c> is <see langword="null"/>.</exception>
        internal SqliteBlobCache CreateSqliteCache(IAkavacheBuilder builder)
        {
            var serializer = builder.Serializer
                ?? throw new InvalidOperationException("No serializer has been registered. Call CacheDatabase.Initialize<[SerializerType]>() before using SQLite caches.");

            ArgumentValidation.ThrowIfNullOrWhiteSpace(name);
            ArgumentValidation.ThrowIfNullOrWhiteSpace(builder.ApplicationName);

            // Validate cache name to prevent path traversal attacks
            var validatedName = SecurityUtilities.ValidateCacheName(name, nameof(name));

            // Determine the cache directory.
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

            var filePath = Path.Combine(directory!, $"{validatedName}.db");
            var cache = new SqliteBlobCache(filePath, serializer);

            if (builder.ForcedDateTimeKind.HasValue)
            {
                cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
            }

            return cache;
        }
    }
}
