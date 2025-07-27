// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for the <see cref="InMemoryBlobCache"/> class interface implementation.
/// </summary>
public class NewtonsoftJsonInMemoryBlobCacheInterfaceTests : BlobCacheTestsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJsonInMemoryBlobCacheInterfaceTests"/> class.
    /// Ensure proper serializer setup for these tests.
    /// </summary>
    public NewtonsoftJsonInMemoryBlobCacheInterfaceTests() =>
        CoreRegistrations.Serializer = new NewtonsoftSerializer();

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path) => new InMemoryBlobCache();
}
