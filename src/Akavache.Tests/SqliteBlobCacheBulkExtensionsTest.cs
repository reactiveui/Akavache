// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for bulk wrapper.
/// </summary>
public class SqliteBlobCacheBulkExtensionsTest : BlobCacheExtensionsTestBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path)
    {
        BlobCache.ApplicationName = "TestRunner";
        return new BlockingDisposeBulkCache(new SqlRawPersistentBlobCache(Path.Combine(path, "sqlite.db")));
    }
}