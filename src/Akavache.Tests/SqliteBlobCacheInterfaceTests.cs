// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SqliteBlobCache"/> class interface implementation.
/// </summary>
public class SqliteBlobCacheInterfaceTests : BlobCacheTestsBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = CacheDatabase.Serializer?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"sqlite-interface-{serializerName}-{formatType}.db";

        return new SqliteBlobCache(Path.Combine(path, fileName));
    }

    /// <inheritdoc />
    protected override void SetupTestClassSerializer()
    {
        // Ensure proper serializer setup for these tests
        CacheDatabase.Serializer = new SystemJsonSerializer();
    }
}
