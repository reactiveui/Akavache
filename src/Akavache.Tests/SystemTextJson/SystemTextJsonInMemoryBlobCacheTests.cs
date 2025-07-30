// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the <see cref="InMemoryBlobCache"/> class.
/// </summary>
public class SystemTextJsonInMemoryBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path) => new InMemoryBlobCache();

    /// <inheritdoc />
    protected override void SetupTestClassSerializer()
    {
        // Ensure proper serializer setup for these tests
        CoreRegistrations.Serializer = new SystemJsonSerializer();
    }
}
