// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="EncryptedSqliteBlobCache"/> class.
/// </summary>
public class EncryptedSqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = CacheDatabase.Serializer?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"encrypted-test-{serializerName}-{formatType}.db";

        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword());
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
        var fileName = "encrypted-roundtrip-test.db";
        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), "test123");
    }

    private static string GetTestPassword() => "test123";
}
