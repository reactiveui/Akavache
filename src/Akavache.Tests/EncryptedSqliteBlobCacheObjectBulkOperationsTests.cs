﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Encrypted bulk operation tests for the <see cref="Sqlite3.SQLiteEncryptedBlobCache"/> class.
/// </summary>
public class EncryptedSqliteBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new BlockingDisposeBulkCache(new Sqlite3.SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
}
