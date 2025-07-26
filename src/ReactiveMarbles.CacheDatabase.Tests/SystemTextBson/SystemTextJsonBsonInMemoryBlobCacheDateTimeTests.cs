// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.Tests.TestBases;

namespace ReactiveMarbles.CacheDatabase.Tests;

/// <summary>
/// Tests for the <see cref="SystemTextJson.Bson.InMemoryBlobCache"/> class with BSON serialization for DateTime handling.
/// </summary>
public class SystemTextJsonBsonInMemoryBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc/>
    protected override IBlobCache CreateBlobCache(string path) => new SystemTextJson.Bson.InMemoryBlobCache(CoreRegistrations.TaskpoolScheduler);
}
