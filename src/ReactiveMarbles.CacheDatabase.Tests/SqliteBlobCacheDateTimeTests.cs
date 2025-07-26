// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Sqlite3;
using ReactiveMarbles.CacheDatabase.Tests.TestBases;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="SqliteBlobCache"/> class.
/// </summary>
public class SqliteBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new SqliteBlobCache(Path.Combine(path, "test.db"));
}
