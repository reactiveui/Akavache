// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests covering ObjectDisposedException behavior for InMemoryBlobCache operations.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class InMemoryBlobCacheObjectDisposedTests
{
    /// <summary>Verifies that the <see cref="InMemoryBlobCache"/> handles <see cref="ObjectDisposedException"/> correctly when performing operations on a disposed cache instance.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CacheShouldHandleObjectDisposedExceptionCorrectly()
    {
        SystemJsonSerializer serializer = new();
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        cache.InsertObject("test", "value").WaitForCompletion();
        cache.Dispose();

        var getError = cache.GetObject<string>("test").SubscribeGetError();
        await Assert.That(getError).IsTypeOf<ObjectDisposedException>();

        var insertError = cache.InsertObject("new", "value").SubscribeGetError();
        await Assert.That(insertError).IsTypeOf<ObjectDisposedException>();

        var invalidateError = cache.InvalidateObject<string>("test").SubscribeGetError();
        await Assert.That(invalidateError).IsTypeOf<ObjectDisposedException>();
    }
}
