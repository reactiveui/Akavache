// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;

namespace Akavache.Integration.Tests;

/// <summary>
/// Smoke coverage for the UniversalSerializer Task-returning shim.
/// </summary>
[Category("Akavache")]
public class UniversalSerializerAsyncSmokeTests
{
    /// <summary>
    /// Verifies <see cref="UniversalSerializer.TryFindDataWithAlternativeKeysAsync{T}"/>
    /// returns <see langword="default"/> when the cache is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataAsyncShouldReturnDefaultForNullCache()
    {
        var result = await UniversalSerializer
            .TryFindDataWithAlternativeKeysAsync<string>(null!, "key", new SystemJsonSerializer());

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies <see cref="UniversalSerializer.TryFindDataWithAlternativeKeysAsync{T}"/>
    /// returns <see langword="default"/> when the cache exists but contains no entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryFindDataAsyncShouldReturnDefaultForEmptyCache()
    {
        SystemJsonSerializer serializer = new();
        using var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, serializer);

        var result = await UniversalSerializer
            .TryFindDataWithAlternativeKeysAsync<string>(cache, "missing", serializer);

        await Assert.That(result).IsNull();
    }
}
