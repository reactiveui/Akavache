// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Splat;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="EncryptedSqliteBlobCache"/> class.
/// </summary>
public class EncryptedSqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = AppLocator.Current.GetService<ISerializer>()?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"encrypted-test-{serializerName}-{formatType}.db";

        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword(), serializer);
    }

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCacheForPath(string path, ISerializer serializer)
    {
        // For round-trip tests, use a consistent database file name to ensure
        // both cache instances (write and read) use the same database file
        var fileName = "encrypted-roundtrip-test.db";
        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword(), serializer);
    }

    private static string GetTestPassword() => "test123";
}
