// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif

/// <summary>
/// Builds the encrypted SQLite-backed caches the builder defaults wire up. Kept out of the builder
/// extension class because it constructs a cache from a name, a builder, and a password rather than
/// acting on a receiver.
/// </summary>
internal static class EncryptedSqliteCacheFactory
{
    /// <summary>Creates an <see cref="EncryptedSqliteBlobCache"/> for the specified cache name using the builder's serializer and directory configuration.</summary>
    /// <param name="name">The logical cache name (e.g. <c>UserAccount</c>, <c>LocalMachine</c>, <c>Secure</c>).</param>
    /// <param name="builder">The Akavache builder supplying serializer, application name, and file location options.</param>
    /// <param name="password">The password used to encrypt the SQLite database.</param>
    /// <returns>A configured <see cref="EncryptedSqliteBlobCache"/>.</returns>
    /// <exception cref="InvalidOperationException">No serializer has been registered on the builder.</exception>
    internal static EncryptedSqliteBlobCache CreateEncryptedSqliteCache(string name, IAkavacheBuilder builder, string password)
    {
        var (filePath, serializer) = SqliteCacheTarget.Resolve(name, builder);

        var cache = new EncryptedSqliteBlobCache(filePath, password, serializer);
        SqliteCacheTarget.ApplyBuilderOptions(cache, builder);

        return cache;
    }
}
