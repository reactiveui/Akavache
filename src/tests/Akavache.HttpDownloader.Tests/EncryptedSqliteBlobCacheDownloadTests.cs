// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Download tests exercising <see cref="EncryptedSqliteBlobCache"/> with a real HTTP server.</summary>
[InheritsTests]
public class EncryptedSqliteBlobCacheDownloadTests : BlobCacheDownloadTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer)
    {
        var dbPath = Path.Combine(path, $"encrypted_blob{Guid.NewGuid()}.db");
        return new EncryptedSqliteBlobCache(dbPath, "test-password", serializer, ImmediateSequencer.Instance);
    }
}
