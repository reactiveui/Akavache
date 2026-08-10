// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// The encrypted sibling globs this directory in and builds it with ENCRYPTED defined; it has its
// own EncryptedSqliteCacheFactory, so this file contributes nothing to that compilation.
#if !ENCRYPTED

#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Builds the SQLite-backed caches the builder defaults wire up. Kept out of the builder extension
/// class because it constructs a cache from a name and a builder rather than acting on a receiver.
/// </summary>
internal static class SqliteCacheFactory
{
    /// <summary>Creates a <see cref="SqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.</summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <returns>A configured <see cref="SqliteBlobCache"/>.</returns>
    /// <exception cref="InvalidOperationException">No serializer has been registered on the builder.</exception>
    internal static SqliteBlobCache CreateSqliteCache(string name, IAkavacheBuilder builder)
    {
        var (filePath, serializer) = SqliteCacheTarget.Resolve(name, builder);

        var cache = new SqliteBlobCache(filePath, serializer);
        SqliteCacheTarget.ApplyBuilderOptions(cache, builder);

        return cache;
    }
}

#endif
