// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.Tests.Mocks;
using Akavache.Tests.TestBases;

namespace Akavache.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="EncryptedSqliteBlobCache"/> class.
/// Uses <see cref="InMemoryAkavacheConnection"/> so native SQLCipher is not touched.
/// </summary>
[InheritsTests]
public class EncryptedSqliteBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new EncryptedSqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateScheduler.Instance);
}
