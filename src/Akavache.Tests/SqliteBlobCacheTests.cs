// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SqliteBlobCache"/> class.
/// </summary>
public class SqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        if (serializer == null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        // Create separate database files for each serializer AND format type to ensure compatibility
        var serializerName = serializer.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"test-{serializerName}-{formatType}.db";

        return new SqliteBlobCache(Path.Combine(path, fileName), serializer);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCacheForPath(string path, ISerializer serializer)
    {
        // For round-trip tests, use a consistent database file name to ensure
        // both cache instances (write and read) use the same database file
        var fileName = "roundtrip-test.db";
        return new SqliteBlobCache(Path.Combine(path, fileName), serializer);
    }
}
