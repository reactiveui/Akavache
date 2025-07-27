// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson;

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary with Newtonsoft.Json serialization.
/// Use it for testing / mocking purposes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
public sealed class InMemoryBlobCache(IScheduler scheduler) : InMemoryBlobCacheBase(scheduler, new NewtonsoftSerializer())
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class with default scheduler.
    /// </summary>
    public InMemoryBlobCache()
        : this(CoreRegistrations.TaskpoolScheduler)
    {
    }
}
