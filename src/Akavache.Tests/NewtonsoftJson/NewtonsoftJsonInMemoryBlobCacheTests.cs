// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="InMemoryBlobCache"/> class.
/// </summary>
public class NewtonsoftJsonInMemoryBlobCacheTests : BlobCacheTestsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftJsonInMemoryBlobCacheTests"/> class.
    /// Ensure proper serializer setup for these tests.
    /// </summary>
    public NewtonsoftJsonInMemoryBlobCacheTests() =>
        CoreRegistrations.Serializer = new NewtonsoftSerializer();

    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path) => new InMemoryBlobCache();
}
