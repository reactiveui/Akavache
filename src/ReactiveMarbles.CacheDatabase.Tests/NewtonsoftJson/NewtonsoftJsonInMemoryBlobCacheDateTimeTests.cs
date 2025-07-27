// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;
using ReactiveMarbles.CacheDatabase.Tests.TestBases;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="NewtonsoftJson.InMemoryBlobCache"/> class.
/// </summary>
public class NewtonsoftJsonInMemoryBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJsonInMemoryBlobCacheDateTimeTests"/> class.
    /// </summary>
    public NewtonsoftJsonInMemoryBlobCacheDateTimeTests()
    {
        // Ensure proper serializer setup for these tests
        CoreRegistrations.Serializer = new NewtonsoftSerializer();
    }

    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new NewtonsoftJson.InMemoryBlobCache(CoreRegistrations.TaskpoolScheduler);
}
