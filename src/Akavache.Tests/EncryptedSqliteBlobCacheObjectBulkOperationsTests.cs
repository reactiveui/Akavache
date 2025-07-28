// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.Tests.TestBases;

namespace Akavache.Tests;

/// <summary>
/// Tests for object bulk operations associated with the <see cref="EncryptedSqliteBlobCache"/> class.
/// </summary>
public class EncryptedSqliteBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path)
    {
        // Create separate database files for each serializer to ensure compatibility
        var serializerName = CoreRegistrations.Serializer?.GetType().Name ?? "Unknown";

        // Further separate JSON and BSON formats to prevent cross-contamination
        var formatType = serializerName.Contains("Bson") ? "bson" : "json";
        var fileName = $"encrypted-objbulk-{serializerName}-{formatType}.db";

        return new EncryptedSqliteBlobCache(Path.Combine(path, fileName), GetTestPassword());
    }

    /// <inheritdoc />
    protected override void SetupTestClassSerializer()
    {
        // Let the test base handle the serializer setup through the test parameters
        // This class doesn't force a specific serializer
    }

    private static string GetTestPassword() => "test123";
}
