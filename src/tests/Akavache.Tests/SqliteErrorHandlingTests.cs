// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// System first
using System.Reactive.Threading.Tasks;
using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for error handling scenarios in Sqlite and Encrypted caches.
/// </summary>
[Category("Sqlite")]
public class SqliteErrorHandlingTests
{
    /// <summary>
    /// Wrong password should cause failure on read attempt.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Test]
    public async Task EncryptedSqliteBlobCache_InvalidPassword_FailsGracefully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var serializer = new SystemJsonSerializer();
        var path = Path.Combine(tempDir, "enc.db");

        await using (var cache = new EncryptedSqliteBlobCache(path, "password1", serializer))
        {
            await cache.Insert("key", [1, 2, 3]);
            await cache.Flush();
            await cache.DisposeAsync();
        }

        await using var cache2 = new EncryptedSqliteBlobCache(path, "password2", serializer);

        Exception? ex = null;
        try
        {
            _ = await cache2.Get("key").ToTask();
        }
        catch (Exception e)
        {
            ex = e;
        }

        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Plain sqlite binary roundtrip.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Test]
    public async Task SqliteBlobCache_InsertAndRetrieveBinary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var serializer = new SystemJsonSerializer();
        var path = Path.Combine(tempDir, "plain.db");
        await using var cache = new SqliteBlobCache(path, serializer);
        var data = new byte[] { 10, 20, 30, 40 };
        await cache.Insert("bin", data);
        var fetched = await cache.Get("bin").ToTask();
        await Assert.That(fetched).IsEquivalentTo(data);
    }
}
