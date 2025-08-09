// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.Tests.TestBases;

namespace Akavache.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="SqliteBlobCache"/> class.
/// </summary>
public class SqliteBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new SqliteBlobCache(Path.Combine(path, "test.db"));

    /// <inheritdoc />
    protected override void SetupTestClassSerializer()
    {
        // Use NewtonsoftBsonSerializer for maximum compatibility with existing Akavache data
        // This is the most appropriate serializer for SQLite tests to ensure Akavache compatibility
        CacheDatabase.Serializer = new NewtonsoftBsonSerializer();
    }
}
