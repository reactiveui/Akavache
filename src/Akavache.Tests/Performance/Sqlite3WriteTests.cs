// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace Akavache.Tests.Performance;

/// <summary>
/// Write performance tests for the <see cref="SqlRawPersistentBlobCache"/> class.
/// </summary>
public abstract class Sqlite3WriteTests : WriteTests
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path) => new SqlRawPersistentBlobCache(Path.Combine(path, "blob.db"));
}