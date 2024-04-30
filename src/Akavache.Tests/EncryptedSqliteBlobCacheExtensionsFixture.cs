// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="Sqlite3.SQLiteEncryptedBlobCache"/> class.
/// </summary>
public class EncryptedSqliteBlobCacheExtensionsFixture : BlobCacheExtensionsTestBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path)
    {
        BlobCache.ApplicationName = "TestRunner";
        return new BlockingDisposeObjectCache(new Sqlite3.SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
    }
}