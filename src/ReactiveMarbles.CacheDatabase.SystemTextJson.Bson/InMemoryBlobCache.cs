// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson.Bson;

/// <summary>
/// This class is an IBlobCache backed by a simple in-memory Dictionary with System.Text.Json BSON serialization.
/// Use it for testing / mocking purposes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
/// </remarks>
/// <param name="scheduler">The scheduler to use for Observable based operations.</param>
public sealed class InMemoryBlobCache(IScheduler scheduler) : InMemoryBlobCacheBase(scheduler, CreateAndRegisterSystemTextJsonSerializer())
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class with default scheduler.
    /// </summary>
    public InMemoryBlobCache()
        : this(CoreRegistrations.TaskpoolScheduler)
    {
    }

    /// <summary>
    /// Insert an object into the cache using System.Text.Json BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to serialize.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>A Future result representing the completion of the insert.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("InsertObject for ReactiveMarbles.CacheDatabase.SystemTextJson")]
    [RequiresDynamicCode("InsertObject for ReactiveMarbles.CacheDatabase.SystemTextJson")]
#endif
    public new IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        return base.InsertObject(key, value, absoluteExpiration);
    }

    /// <summary>
    /// Get an object from the cache and deserialize it using System.Text.Json BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="key">The key to look up in the cache.</param>
    /// <returns>A Future result representing the object in the cache.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("GetObject for ReactiveMarbles.CacheDatabase.SystemTextJson")]
    [RequiresDynamicCode("GetObject for ReactiveMarbles.CacheDatabase.SystemTextJson")]
#endif
    public new IObservable<T?> GetObject<T>(string key)
    {
        return base.GetObject<T>(key);
    }

    /// <summary>
    /// Return all objects of a specific Type in the cache.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <returns>A Future result representing all objects in the cache with the specified Type.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("GetAllObjects for ReactiveMarbles.CacheDatabase.SystemTextJson")]
    [RequiresDynamicCode("GetAllObjects for ReactiveMarbles.CacheDatabase.SystemTextJson")]
#endif
    public new IObservable<IEnumerable<T>> GetAllObjects<T>()
    {
        return base.GetAllObjects<T>();
    }

    /// <summary>
    /// Creates a System.Text.Json BSON serializer for this cache instance.
    /// </summary>
    /// <returns>A new System.Text.Json BSON serializer instance.</returns>
    private static ISerializer CreateAndRegisterSystemTextJsonSerializer()
    {
        var serializer = new SystemJsonBsonSerializer();

        // Don't override the global serializer to avoid cross-contamination between different cache types
        // This cache uses its own System.Text.Json serializer directly through the Serializer property
        return serializer;
    }
}
