// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.Design;
using Akavache.Core;
using Akavache.Settings.Core;

namespace Akavache.Settings;

/// <summary>
/// Empty Base.
/// </summary>
public abstract class SettingsBase : SettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    protected SettingsBase(string className)
        : base($"__{className}__", GetBlobCacheForClass(className))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class.
    /// </summary>
    /// <param name="className">Name of the class.</param>
    /// <param name="serializer">The serializer.</param>
    protected SettingsBase(string className, ISerializer serializer)
        : base($"__{className}__", GetBlobCacheForClass(className), serializer)
    {
    }

    /// <summary>
    /// Gets the blob cache for the specified class, handling override database names.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>The blob cache for the class.</returns>
    private static IBlobCache GetBlobCacheForClass(string className)
    {
        // First try to get the cache with the exact class name
        if (BlobCacheBuilderExtensions.BlobCaches.TryGetValue(className, out var cache) && cache != null)
        {
            return cache;
        }

        // If not found, look for any cache in the collection (for override database names)
        foreach (var kvp in BlobCacheBuilderExtensions.BlobCaches)
        {
            if (kvp.Value != null)
            {
                return kvp.Value;
            }
        }

        // If no cache is found, throw a descriptive exception
        throw new InvalidOperationException($"No blob cache found for class '{className}'. Available caches: {string.Join(", ", BlobCacheBuilderExtensions.BlobCaches.Keys)}");
    }
}
