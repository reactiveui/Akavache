// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using System.Diagnostics;
using System.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Additional expiration tests targeting edge cases.
/// </summary>
[TestFixture]
[Category("Expiration")]
public class AdditionalExpirationTests
{
    /// <summary>
    /// Item inserted with short expiration becomes unavailable after duration (timing-safe for CI).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task InMemoryCache_ItemExpires_AfterSpecifiedDuration()
    {
        var serializer = new SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);

        var expiry = DateTimeOffset.UtcNow.AddMilliseconds(800);
        await cache.Insert("expiring", [1], expiry).FirstAsync();

        var immediate = await cache.Get("expiring");
        Assert.That(immediate, Is.Not.Null);

        var remaining = expiry - DateTimeOffset.UtcNow;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining + TimeSpan.FromMilliseconds(150));
        }

        Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("expiring"));
    }

    /// <summary>
    /// UpdateExpiration extends lifetime beyond original expiry (timing-safe for CI).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task UpdateExpiration_ExtendsLifetime()
    {
        var serializer = new SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);

        var initialExpiry = DateTimeOffset.UtcNow.AddMilliseconds(600);
        await cache.Insert("extend", [1, 2], initialExpiry).FirstAsync();
        await Task.Delay(200);

        var extendedExpiry = DateTimeOffset.UtcNow.AddMilliseconds(900);
        await cache.UpdateExpiration("extend", extendedExpiry).FirstAsync();

        var wait = (initialExpiry - DateTimeOffset.UtcNow) + TimeSpan.FromMilliseconds(300);
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait);
        }

        var data = await cache.Get("extend");
        Assert.That(data, Is.Not.Null);

        var sw = Stopwatch.StartNew();
        var expired = false;
        while (sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            try
            {
                _ = await cache.Get("extend");
                await Task.Delay(150);
            }
            catch (KeyNotFoundException)
            {
                expired = true;
                break;
            }
        }

        Assert.That(expired, Is.True, "Item did not expire within expected extended window.");
    }
}
