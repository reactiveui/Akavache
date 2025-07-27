// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.SystemTextJson;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for the <see cref="InMemoryBlobCache"/> class.
/// </summary>
public class SystemTextJsonInMemoryBlobCacheTests : BlobCacheTestsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonInMemoryBlobCacheTests"/> class.
    /// Ensure proper serializer setup for these tests.
    /// </summary>
    public SystemTextJsonInMemoryBlobCacheTests() =>
        CoreRegistrations.Serializer = new SystemJsonSerializer();

    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new InMemoryBlobCache();
}
