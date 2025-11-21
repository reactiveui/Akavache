// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Skeleton tests for error handling scenarios in Sqlite and Encrypted caches.
/// </summary>
[TestFixture]
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

        using (var cache = new EncryptedSqliteBlobCache(path, "password1", serializer))
        {
            await cache.Insert("key", [1, 2, 3]);
            await cache.Flush();
            await cache.DisposeAsync();
        }

        using var cache2 = new EncryptedSqliteBlobCache(path, "password2", serializer);

        Exception? ex = null;
        try
        {
            _ = await cache2.Get("key").ToTask();
        }
        catch (Exception e)
        {
            ex = e;
        }

        Assert.That(ex, Is.Not.Null);
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
        using var cache = new SqliteBlobCache(path, serializer);
        var data = new byte[] { 10, 20, 30, 40 };
        await cache.Insert("bin", data);
        var fetched = await cache.Get("bin").ToTask();
        Assert.That(fetched, Is.EqualTo(data));
    }
}
