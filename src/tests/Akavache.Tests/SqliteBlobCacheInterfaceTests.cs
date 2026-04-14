// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.Tests.Mocks;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="SqliteBlobCache"/> class interface implementation.
/// Uses <see cref="InMemoryAkavacheConnection"/> so native SQLite is not touched.
/// </summary>
[InheritsTests]
public class SqliteBlobCacheInterfaceTests : BlobCacheTestsBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new SqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
