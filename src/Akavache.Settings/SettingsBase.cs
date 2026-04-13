// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Settings.Core;
using Splat; // AppLocator

namespace Akavache.Settings;

/// <summary>
/// Provides a base class for implementing application settings storage using Akavache.
/// This class automatically manages settings persistence and provides a foundation for typed settings classes.
/// </summary>
public abstract class SettingsBase : SettingsStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class that resolves
    /// its backing cache from the ambient <see cref="CacheDatabase"/>.
    /// </summary>
    /// <param name="className">Name of the class — used as the settings key prefix.</param>
    protected SettingsBase(string className)
        : base($"__{className}__", GetBlobCacheForClass(className))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsBase"/> class with explicit
    /// ambient-cache resolvers. The resolver delegates are used by
    /// <see cref="TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/> when the explicit registry does not contain
    /// an entry for <paramref name="className"/>; each resolver is wrapped in a
    /// <c>try</c>/<c>catch</c> so an unconfigured cache kind simply falls through to the
    /// next. This overload exists to make the class testable without touching the
    /// global <see cref="CacheDatabase"/> singleton.
    /// </summary>
    /// <param name="className">Name of the class — used as the settings key prefix.</param>
    /// <param name="userAccountResolver">Delegate that returns the UserAccount cache, or throws when unavailable.</param>
    /// <param name="localMachineResolver">Delegate that returns the LocalMachine cache, or throws when unavailable.</param>
    /// <param name="inMemoryResolver">Delegate that returns the InMemory cache, or throws when unavailable.</param>
    protected SettingsBase(
        string className,
        Func<IBlobCache> userAccountResolver,
        Func<IBlobCache> localMachineResolver,
        Func<IBlobCache> inMemoryResolver)
        : base(
            $"__{className}__",
            GetBlobCacheForClass(className, userAccountResolver, localMachineResolver, inMemoryResolver))
    {
    }

    /// <summary>
    /// Resolves the blob cache that backs a settings class, walking through the
    /// strategies in priority order: explicit registry → ambient
    /// <see cref="CacheDatabase"/> caches → transient fallback. Throws when none
    /// of the strategies produce a non-null cache.
    /// </summary>
    /// <param name="className">The settings class name (used both as a registry key and in the error message).</param>
    /// <param name="userAccountResolver">Delegate that returns the UserAccount cache, or throws when unavailable.</param>
    /// <param name="localMachineResolver">Delegate that returns the LocalMachine cache, or throws when unavailable.</param>
    /// <param name="inMemoryResolver">Delegate that returns the InMemory cache, or throws when unavailable.</param>
    /// <returns>The resolved <see cref="IBlobCache"/>.</returns>
    /// <exception cref="InvalidOperationException">No cache could be resolved for <paramref name="className"/>.</exception>
    internal static IBlobCache GetBlobCacheForClass(
        string className,
        Func<IBlobCache> userAccountResolver,
        Func<IBlobCache> localMachineResolver,
        Func<IBlobCache> inMemoryResolver) =>
        TryGetFromBlobCacheRegistry(className)
            ?? TryGetFromCacheDatabase(userAccountResolver, localMachineResolver, inMemoryResolver)
            ?? TryGetTransientFallback()
            ?? throw CreateNoCacheFoundException(className);

    /// <summary>
    /// Resolves the blob cache using the default ambient-cache resolvers that point at
    /// <see cref="CacheDatabase"/>. Overload of
    /// <see cref="GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>.
    /// </summary>
    /// <param name="className">The settings class name.</param>
    /// <returns>The resolved <see cref="IBlobCache"/>.</returns>
    internal static IBlobCache GetBlobCacheForClass(string className) =>
        GetBlobCacheForClass(
            className,
            ReadAmbientUserAccount,
            ReadAmbientLocalMachine,
            ReadAmbientInMemory);

    /// <summary>
    /// Default UserAccount resolver used by the parameterless <see cref="SettingsBase"/>
    /// constructor. Delegates straight to <see cref="CacheDatabase.UserAccount"/>, which
    /// throws when the UserAccount cache has not been configured — the caller wraps the
    /// invocation in <see cref="TryReadAmbientCache"/> to swallow that exception.
    /// </summary>
    /// <returns>The ambient UserAccount cache.</returns>
    internal static IBlobCache ReadAmbientUserAccount() => CacheDatabase.UserAccount;

    /// <summary>
    /// Default LocalMachine resolver used by the parameterless <see cref="SettingsBase"/>
    /// constructor. Delegates straight to <see cref="CacheDatabase.LocalMachine"/>.
    /// </summary>
    /// <returns>The ambient LocalMachine cache.</returns>
    internal static IBlobCache ReadAmbientLocalMachine() => CacheDatabase.LocalMachine;

    /// <summary>
    /// Default InMemory resolver used by the parameterless <see cref="SettingsBase"/>
    /// constructor. Delegates straight to <see cref="CacheDatabase.InMemory"/>.
    /// </summary>
    /// <returns>The ambient InMemory cache.</returns>
    internal static IBlobCache ReadAmbientInMemory() => CacheDatabase.InMemory;

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
        if (string.IsNullOrEmpty(firstPair.Key))
        {
            return null;
        }

        return firstPair.Value;
    }

    /// <summary>
    /// Returns the first cache the resolvers produce (UserAccount → LocalMachine →
    /// InMemory). Each resolver is wrapped in <see cref="TryReadAmbientCache"/>, so a
    /// throwing delegate simply falls through to the next one.
    /// </summary>
    /// <param name="userAccountResolver">Delegate that returns the UserAccount cache, or throws when unavailable.</param>
    /// <param name="localMachineResolver">Delegate that returns the LocalMachine cache, or throws when unavailable.</param>
    /// <param name="inMemoryResolver">Delegate that returns the InMemory cache, or throws when unavailable.</param>
    /// <returns>The first available cache, or <see langword="null"/>.</returns>
    internal static IBlobCache? TryGetFromCacheDatabase(
        Func<IBlobCache> userAccountResolver,
        Func<IBlobCache> localMachineResolver,
        Func<IBlobCache> inMemoryResolver) =>
        TryReadAmbientCache(userAccountResolver)
            ?? TryReadAmbientCache(localMachineResolver)
            ?? TryReadAmbientCache(inMemoryResolver);

    /// <summary>
    /// Default-resolver overload of <see cref="TryGetFromCacheDatabase(Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// that reads directly from the ambient <see cref="CacheDatabase"/>.
    /// </summary>
    /// <returns>The first available ambient cache, or <see langword="null"/>.</returns>
    internal static IBlobCache? TryGetFromCacheDatabase() =>
        TryGetFromCacheDatabase(
            static () => CacheDatabase.UserAccount,
            static () => CacheDatabase.LocalMachine,
            static () => CacheDatabase.InMemory);

    /// <summary>
    /// Safely invokes an ambient-cache resolver. The resolver delegates throw when the
    /// requested cache kind has not been configured on the current instance, so we
    /// swallow the exception and return <see langword="null"/> to let the caller fall
    /// through to the next kind.
    /// </summary>
    /// <param name="resolver">The resolver delegate to invoke.</param>
    /// <returns>The cache instance, or <see langword="null"/> when unavailable.</returns>
    internal static IBlobCache? TryReadAmbientCache(Func<IBlobCache> resolver)
    {
        try
        {
            return resolver();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a transient in-memory cache when an <see cref="ISerializer"/> has been
    /// registered with Splat. Used as the last resort before
    /// <see cref="GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// throws.
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
    /// <see cref="GetBlobCacheForClass(string, Func{IBlobCache}, Func{IBlobCache}, Func{IBlobCache})"/>
    /// when no cache can be resolved. The message lists every key currently registered
    /// in <see cref="AkavacheBuilder.BlobCaches"/> to make diagnosis easier.
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
