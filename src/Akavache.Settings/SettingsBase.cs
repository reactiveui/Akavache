// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using Akavache.Core;
using Akavache.Settings.Core;
using Splat; // AppLocator

namespace Akavache.Settings;

/// <summary>
/// Provides a base class for implementing application settings storage using Akavache.
/// This class automatically manages settings persistence and provides a foundation for typed settings classes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SettingsBase"/> class.
/// </remarks>
/// <param name="className">Name of the class.</param>
public abstract class SettingsBase(string className) : SettingsStorage($"__{className}__", GetBlobCacheForClass(className))
{
    /// <summary>
    /// Gets the blob cache for the specified class, handling override database names.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>The blob cache for the class.</returns>
    private static IBlobCache GetBlobCacheForClass(string className)
    {
        // Prefer an explicitly created settings cache for the given class name
        if (AkavacheBuilder.BlobCaches != null)
        {
            if (AkavacheBuilder.BlobCaches.TryGetValue(className, out var cache) && cache != null)
            {
                return cache;
            }

            // If not found, return the first available cache (supports override database names)
            var firstCache = AkavacheBuilder.BlobCaches
                .FirstOrDefault(kvp => kvp.Value != null)
                .Value;

            if (firstCache != null)
            {
                return firstCache;
            }
        }

        // Fallbacks to any initialized CacheDatabase cache
        try
        {
            if (CacheDatabase.IsInitialized)
            {
                if (CacheDatabase.UserAccount is IBlobCache user)
                {
                    return user;
                }

                if (CacheDatabase.LocalMachine is IBlobCache local)
                {
                    return local;
                }

                if (CacheDatabase.InMemory is IBlobCache mem)
                {
                    return mem;
                }
            }
        }
        catch
        {
            // Ignore and proceed to last resort
        }

        // Last resort: create a transient in-memory cache if a serializer is registered
        var serializer = AppLocator.Current.GetService<ISerializer>();
        if (serializer != null)
        {
            return new InMemoryBlobCache(serializer);
        }

        // If no cache is found, throw a descriptive exception
        var available = AkavacheBuilder.BlobCaches == null || AkavacheBuilder.BlobCaches.Count == 0
            ? "<none>"
            : string.Join(", ", AkavacheBuilder.BlobCaches.Keys);
        throw new InvalidOperationException($"No blob cache found for class '{className}'. Available caches: {available}");
    }
}
