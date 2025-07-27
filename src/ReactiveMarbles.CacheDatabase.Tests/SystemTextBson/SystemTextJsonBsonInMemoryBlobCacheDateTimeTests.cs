// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.SystemTextJson;
using ReactiveMarbles.CacheDatabase.Tests.TestBases;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="SystemTextJson.InMemoryBlobCache"/> class with BSON serialization.
/// </summary>
public class SystemTextJsonBsonInMemoryBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonBsonInMemoryBlobCacheDateTimeTests"/> class.
    /// </summary>
    public SystemTextJsonBsonInMemoryBlobCacheDateTimeTests()
    {
        // Ensure proper serializer setup for these tests
        SystemJsonBsonRegistrations.EnsureRegistered();
        CoreRegistrations.Serializer = new SystemJsonBsonSerializer();
    }

    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new SystemTextJson.InMemoryBlobCache(CoreRegistrations.TaskpoolScheduler);
}
