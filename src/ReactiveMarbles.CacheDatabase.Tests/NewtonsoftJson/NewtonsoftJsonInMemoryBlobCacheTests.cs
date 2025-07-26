// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for the <see cref="NewtonsoftJson.InMemoryBlobCache"/> class.
/// </summary>
public class NewtonsoftJsonInMemoryBlobCacheTests : BlobCacheTestsBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path) => new NewtonsoftJson.InMemoryBlobCache(CoreRegistrations.TaskpoolScheduler);
}
