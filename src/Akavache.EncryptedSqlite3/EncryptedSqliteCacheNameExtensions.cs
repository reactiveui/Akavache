// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif

/// <summary>Internal factory helpers that create encrypted SQLite blob caches from a cache slot name.</summary>
internal static class EncryptedSqliteCacheNameExtensions
{
    /// <summary>Extension members for <c>string</c>.</summary>
    /// <param name="name">The cache slot name, which becomes the database file name.</param>
    extension(string name)
    {
        /// <summary>Creates an <see cref="EncryptedSqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.</summary>
        /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
        /// <param name="password">The password used to encrypt the SQLite database.</param>
        /// <returns>A configured <see cref="EncryptedSqliteBlobCache"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <c>builder.Serializer</c> is <see langword="null"/>.</exception>
        internal EncryptedSqliteBlobCache CreateEncryptedSqliteCache(IAkavacheBuilder builder, string password)
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

            var filePath = Path.Combine(directory!, $"{validatedName}.db");
            var cache = new EncryptedSqliteBlobCache(filePath, password, serializer);
            if (builder.ForcedDateTimeKind.HasValue)
            {
                cache.ForcedDateTimeKind = builder.ForcedDateTimeKind.Value;
            }

            return cache;
        }
    }
}
