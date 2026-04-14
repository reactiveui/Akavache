// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests covering ObjectDisposedException behavior for InMemoryBlobCache operations.
/// </summary>
[Category("Akavache")]
public class InMemoryBlobCacheObjectDisposedTests
{
    /// <summary>
    /// Verifies that the <see cref="InMemoryBlobCache"/> handles <see cref="ObjectDisposedException"/>
    /// correctly when performing operations on a disposed cache instance.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CacheShouldHandleObjectDisposedExceptionCorrectly()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(serializer);

        await cache.InsertObject("test", "value").FirstAsync();
        await cache.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.GetObject<string>("test").FirstAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.InsertObject("new", "value").FirstAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.InvalidateObject<string>("test").FirstAsync());
    }
}
