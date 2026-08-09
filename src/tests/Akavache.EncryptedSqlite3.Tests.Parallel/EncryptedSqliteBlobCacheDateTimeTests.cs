// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for DateTime operations associated with the <see cref="EncryptedSqliteBlobCache"/> class. Uses <see cref="InMemoryAkavacheConnection"/> so native SQLCipher is not touched.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[InheritsTests]
public class EncryptedSqliteBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) =>
        new EncryptedSqliteBlobCache(new InMemoryAkavacheConnection(), serializer, ImmediateSequencer.Instance);
}
