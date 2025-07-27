// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Sqlite3;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for the <see cref="SqliteBlobCache"/> class.
/// </summary>
public class SqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path)
    {
        // Create separate database files for each serializer AND format type to ensure compatibility
        var serializerName = CoreRegistrations.Serializer?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"test-{serializerName}-{formatType}.db";

        return new SqliteBlobCache(Path.Combine(path, fileName));
    }

    /// <summary>
    /// Creates a blob cache for a specific path, ensuring the path is used directly.
    /// </summary>
    /// <param name="path">The path for the cache.</param>
    /// <returns>The cache instance.</returns>
    protected override IBlobCache CreateBlobCacheForPath(string path)
    {
        // For round-trip tests, use a consistent database file name to ensure
        // both cache instances (write and read) use the same database file
        var fileName = "roundtrip-test.db";
        return new SqliteBlobCache(Path.Combine(path, fileName));
    }
}
