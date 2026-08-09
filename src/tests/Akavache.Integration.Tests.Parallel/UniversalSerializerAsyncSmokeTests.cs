// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Smoke coverage for the UniversalSerializer Task-returning shim.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class UniversalSerializerAsyncSmokeTests
{
    /// <summary>Verifies <see cref="UniversalSerializer.TryFindDataWithAlternativeKeysAsync{T}"/> returns <see langword="default"/> when the cache is <see langword="null"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataAsyncShouldReturnDefaultForNullCache()
    {
        var result = await UniversalSerializer
            .TryFindDataWithAlternativeKeysAsync<string>(null!, "key", new SystemJsonSerializer());

        await Assert.That(result).IsNull();
    }

    /// <summary>Verifies <see cref="UniversalSerializer.TryFindDataWithAlternativeKeysAsync{T}"/> returns <see langword="default"/> when the cache exists but contains no entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataAsyncShouldReturnDefaultForEmptyCache()
    {
        SystemJsonSerializer serializer = new();
        using var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, serializer);

        var result = await UniversalSerializer
            .TryFindDataWithAlternativeKeysAsync<string>(cache, "missing", serializer);

        await Assert.That(result).IsNull();
    }
}
