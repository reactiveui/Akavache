// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SqlRawPersistentBlobCache"/>.
/// </summary>
public class SqliteBlobCacheExtensionsTests : BlobCacheExtensionsTestBase
{
    /// <summary>
    /// Checks to make sure that vacuuming compacts the file size.
    /// </summary>
    [Fact]
    public void VacuumCompactsDatabase()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "sqlite.db");

            using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
            {
                Assert.True(File.Exists(dbPath));

                var buf = new byte[256 * 1024];
                var rnd = new Random();
                rnd.NextBytes(buf);

                fixture.Insert("dummy", buf).Wait();
            }

            var size = new FileInfo(dbPath).Length;
            Assert.True(size > 0);

            using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
            {
                fixture.InvalidateAll().Wait();
                fixture.Vacuum().Wait();
            }

            Assert.True(new FileInfo(dbPath).Length <= size);
        }
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path)
    {
        BlobCache.ApplicationName = "TestRunner";
        return new BlockingDisposeObjectCache(new SqlRawPersistentBlobCache(Path.Combine(path, "sqlite.db")));
    }
}
