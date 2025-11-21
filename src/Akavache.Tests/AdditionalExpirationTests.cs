// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
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
    /// Item inserted with short expiration becomes unavailable after duration.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task InMemoryCache_ItemExpires_AfterSpecifiedDuration()
    {
        var serializer = new SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);
        await cache.Insert("expiring", [1], DateTimeOffset.UtcNow.AddMilliseconds(50)).FirstAsync();
        var immediate = await cache.Get("expiring");
        Assert.That(immediate, Is.Not.Null);
        await Task.Delay(120);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await cache.Get("expiring"));
    }

    /// <summary>
    /// UpdateExpiration extends lifetime beyond original expiry.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task UpdateExpiration_ExtendsLifetime()
    {
        var serializer = new SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);
        await cache.Insert("extend", [1, 2], DateTimeOffset.UtcNow.AddMilliseconds(50)).FirstAsync();
        await Task.Delay(30);
        await cache.UpdateExpiration("extend", DateTimeOffset.UtcNow.AddMilliseconds(100)).FirstAsync();
        await Task.Delay(70);
        var data = await cache.Get("extend");
        Assert.That(data, Is.Not.Null);
    }
}
