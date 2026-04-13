// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using Akavache.Sqlite3;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SqliteBlobCache"/> class. Runs the inherited
/// <see cref="BlobCacheTestsBase"/> suite against an
/// <see cref="InMemoryAkavacheConnection"/> so that native SQLite is not touched on the
/// parallel path — the native provider is exercised in the dedicated integration tests
/// marked <c>NotInParallel("NativeSqlite")</c>.
/// </summary>
[InheritsTests]
public class SqliteBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new SqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
