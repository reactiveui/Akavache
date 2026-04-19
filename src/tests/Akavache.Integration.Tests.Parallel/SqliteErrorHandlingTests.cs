// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests;
using Akavache.Tests.Helpers;

namespace Akavache.Integration.Tests;

/// <summary>
/// Error handling scenarios for Sqlite and Encrypted caches.
/// </summary>
[Category("Sqlite")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "RCS1261:Resource can be disposed asynchronously", Justification = "Tests deliberately use synchronous Dispose to avoid sync-over-async deadlocks.")]
public class SqliteErrorHandlingTests
{
    /// <summary>
    /// Wrong password should cause failure on read attempt.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Test]
    public async Task EncryptedSqliteBlobCache_InvalidPassword_FailsGracefully()
    {
        using var dir = Utility.WithEmptyDirectory(out var tempDir);
        SystemJsonSerializer serializer = new();
        var path = Path.Combine(tempDir, "enc.db");

        using (EncryptedSqliteBlobCache cache = new(path, "password1", serializer, ImmediateScheduler.Instance))
        {
            cache.Insert("key", [1, 2, 3]).WaitForCompletion();
            cache.Flush().WaitForCompletion();
        }

        // Opening with the wrong key fail-fasts during construction — the
        // first PRAGMA after sqlite3_open reads the header page and surfaces
        // SQLITE_NOTADB as AkavacheSqliteException.
        Exception? ex = null;
        try
        {
            using EncryptedSqliteBlobCache cache2 = new(path, "password2", serializer, ImmediateScheduler.Instance);
            var error = cache2.Get("key").WaitForError();
            if (error is not null)
            {
                throw error;
            }
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
        using var dir = Utility.WithEmptyDirectory(out var tempDir);
        SystemJsonSerializer serializer = new();
        var path = Path.Combine(tempDir, "plain.db");

        using SqliteBlobCache cache = new(path, serializer, ImmediateScheduler.Instance);
        byte[] data = [10, 20, 30, 40];
        cache.Insert("bin", data).WaitForCompletion();

        var fetched = cache.Get("bin").WaitForValue();
        await Assert.That(fetched).IsEquivalentTo(data);
    }
}
