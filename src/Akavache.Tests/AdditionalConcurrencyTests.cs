// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using System.Linq;
using System.Threading.Tasks;
using Akavache.Core;
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Additional concurrency stress tests.
/// </summary>
[TestFixture]
[Category("Concurrency")]
public class AdditionalConcurrencyTests
{
    /// <summary>
    /// Concurrent writes and reads should not corrupt entries.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InMemoryCache_ConcurrentWrites_DoNotCorrupt()
    {
        var serializer = new SystemJsonSerializer();
        using var cache = new InMemoryBlobCache(serializer);
        var writeTasks = Enumerable.Range(0, 50)
            .Select(async i => await cache.InsertObject($"user_{i}", new Mocks.UserObject { Name = $"User{i}" }).FirstAsync());
        await Task.WhenAll(writeTasks);
        var readTasks = Enumerable.Range(0, 50)
            .Select(async i => await cache.GetObject<Mocks.UserObject>($"user_{i}").FirstAsync());
        var results = await Task.WhenAll(readTasks);
        Assert.That(results.All(r => r.Name.StartsWith("User", StringComparison.Ordinal)), Is.True);
    }
}
