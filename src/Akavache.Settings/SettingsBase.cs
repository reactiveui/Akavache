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
    /// Resolves the blob cache that backs a settings class, walking through the
    /// strategies in priority order: explicit registry → ambient
    /// <see cref="CacheDatabase"/> caches → transient fallback. Throws when none
    /// of the strategies produce a non-null cache.
    /// </summary>
    /// <param name="className">The settings class name (used both as a registry key and in the error message).</param>
    /// <returns>The resolved <see cref="IBlobCache"/>.</returns>
    /// <exception cref="InvalidOperationException">No cache could be resolved for <paramref name="className"/>.</exception>
    internal static IBlobCache GetBlobCacheForClass(string className) =>
        TryGetFromBlobCacheRegistry(className)
            ?? TryGetFromCacheDatabase()
            ?? TryGetTransientFallback()
            ?? throw CreateNoCacheFoundException(className);

    /// <summary>
    /// Looks up <paramref name="className"/> in <see cref="AkavacheBuilder.BlobCaches"/>.
    /// </summary>
    /// <remarks>
    /// Falls back to the first registered non-null entry when the exact class name is
    /// missing — this keeps consumers that rename their settings store database working
    /// without a custom registration step.
    /// </remarks>
    /// <param name="className">The class name to look up.</param>
    /// <returns>The matching cache, or <see langword="null"/> when nothing is registered.</returns>
    internal static IBlobCache? TryGetFromBlobCacheRegistry(string className)
    {
        if (AkavacheBuilder.BlobCaches is null)
        {
            return null;
        }

        if (AkavacheBuilder.BlobCaches.TryGetValue(className, out var cache) && cache != null)
        {
            return cache;
        }

        var firstPair = AkavacheBuilder.BlobCaches.FirstOrDefault(kvp => kvp.Value != null);
        if (!string.IsNullOrEmpty(firstPair.Key))
        {
            return firstPair.Value;
        }

        return null;
    }

    /// <summary>
    /// Returns the first non-null ambient cache (UserAccount → LocalMachine → InMemory)
    /// from <see cref="CacheDatabase"/> when it has been initialized.
    /// </summary>
    /// <returns>The first available ambient cache, or <see langword="null"/>.</returns>
    internal static IBlobCache? TryGetFromCacheDatabase()
    {
        try
        {
            if (!CacheDatabase.IsInitialized)
            {
                return null;
            }

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
        catch
        {
            // Ambient cache property getters can throw when the underlying instance
            // has not been configured for the requested cache kind.
        }

        return null;
    }

    /// <summary>
    /// Builds a transient in-memory cache when an <see cref="ISerializer"/> has been
    /// registered with Splat. Used as the last resort before
    /// <see cref="GetBlobCacheForClass"/> throws.
    /// </summary>
    /// <returns>A fresh <see cref="InMemoryBlobCache"/>, or <see langword="null"/> when no serializer is registered.</returns>
    internal static IBlobCache? TryGetTransientFallback()
    {
        var serializer = AppLocator.Current.GetService<ISerializer>();
        if (serializer is null)
        {
            return null;
        }

        return new InMemoryBlobCache(serializer);
    }

    /// <summary>
    /// Builds the descriptive <see cref="InvalidOperationException"/> thrown by
    /// <see cref="GetBlobCacheForClass"/> when no cache can be resolved. The message
    /// lists every key currently registered in <see cref="AkavacheBuilder.BlobCaches"/>
    /// to make diagnosis easier.
    /// </summary>
    /// <param name="className">The class name that failed resolution.</param>
    /// <returns>A new <see cref="InvalidOperationException"/>.</returns>
    internal static InvalidOperationException CreateNoCacheFoundException(string className)
    {
        var available = AkavacheBuilder.BlobCaches is null || AkavacheBuilder.BlobCaches.Count == 0
            ? "<none>"
            : string.Join(", ", AkavacheBuilder.BlobCaches.Keys);
        return new InvalidOperationException($"No blob cache found for class '{className}'. Available caches: {available}");
    }
}
