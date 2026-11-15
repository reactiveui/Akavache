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
            var options = SqliteCacheConfiguration.Prepare(name, builder);
            return new(options.FilePath, options.Serializer) { ForcedDateTimeKind = options.ForcedDateTimeKind };
        }
    }
}
