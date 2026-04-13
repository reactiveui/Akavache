// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using Akavache.Sqlite3;
using Akavache.Tests.Mocks;
using Akavache.Tests.TestBases;

namespace Akavache.Tests;

/// <summary>
/// Tests for bulk operations associated with the <see cref="SqliteBlobCache"/> class.
/// Uses <see cref="InMemoryAkavacheConnection"/> so native SQLite is not touched.
/// </summary>
[InheritsTests]
public class SqliteBlobCacheBulkOperationsTests : BulkOperationsTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new SqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
