// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests for object bulk operations associated with the <see cref="InMemoryBlobCache"/> class.</summary>
[InheritsTests]
public class SystemTextJsonInMemoryBlobCacheObjectBulkOperationsTests : ObjectBulkOperationsTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) => new InMemoryBlobCache(ImmediateSequencer.Instance, serializer);
}
