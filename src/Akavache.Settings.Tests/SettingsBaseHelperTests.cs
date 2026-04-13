// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.SystemTextJson;
using Splat;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for the static helper decomposition of
/// <see cref="SettingsBase.GetBlobCacheForClass"/> — each strategy can be exercised
/// in isolation thanks to the internal helpers.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
[TestExecutor<AkavacheTestExecutor>]
public class SettingsBaseHelperTests
{
    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the registry has not been populated.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenRegistryEmpty()
    {
        AkavacheBuilder.BlobCaches = [];

        var result = SettingsBase.TryGetFromBlobCacheRegistry("MissingClass");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns the
    /// matching cache when the class name is registered with a non-null entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnExactMatch()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["KnownClass"] = cache,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("KnownClass");

        await Assert.That(result).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> falls back
    /// to the first registered non-null entry when the class name does not match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldFallBackToFirstNonNullEntry()
    {
        var fallback = new InMemoryBlobCache(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["RenamedDatabase"] = fallback,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("DifferentClassName");

        await Assert.That(result).IsSameReferenceAs(fallback);
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the registry exists but every entry is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenAllEntriesNull()
    {
        AkavacheBuilder.BlobCaches = new()
        {
            ["NullEntry"] = null,
        };

        var result = SettingsBase.TryGetFromBlobCacheRegistry("NullEntry");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> returns
    /// <see langword="null"/> when the static registry itself is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldReturnNullWhenRegistryIsNull()
    {
        AkavacheBuilder.BlobCaches = null;

        var result = SettingsBase.TryGetFromBlobCacheRegistry("AnyClass");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetFromCacheDatabase"/> returns
    /// <see langword="null"/> when <see cref="CacheDatabase"/> has not been initialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromCacheDatabaseShouldReturnNullWhenNotInitialized()
    {
        var result = SettingsBase.TryGetFromCacheDatabase();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetTransientFallback"/> returns
    /// <see langword="null"/> when no <see cref="ISerializer"/> is registered with
    /// the locator.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetTransientFallbackShouldReturnNullWhenNoSerializerRegistered()
    {
        var result = SettingsBase.TryGetTransientFallback();

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.TryGetTransientFallback"/> constructs a
    /// fresh <see cref="InMemoryBlobCache"/> when an <see cref="ISerializer"/> has
    /// been registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetTransientFallbackShouldReturnInMemoryWhenSerializerRegistered()
    {
        var serializer = new SystemJsonSerializer();
        AppLocator.CurrentMutable.RegisterConstant<ISerializer>(serializer);
        try
        {
            var result = SettingsBase.TryGetTransientFallback();

            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsTypeOf<InMemoryBlobCache>();
        }
        finally
        {
            AppLocator.CurrentMutable.UnregisterAll(typeof(ISerializer));
        }
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> includes
    /// every registered key in the message when the registry has entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldListRegisteredKeys()
    {
        AkavacheBuilder.BlobCaches = new()
        {
            ["AlphaCache"] = new InMemoryBlobCache(new SystemJsonSerializer()),
            ["BetaCache"] = new InMemoryBlobCache(new SystemJsonSerializer()),
        };

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("TargetClass");
        await Assert.That(exception.Message).Contains("AlphaCache");
        await Assert.That(exception.Message).Contains("BetaCache");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> reports
    /// <c>&lt;none&gt;</c> when the registry is empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldReport_NoneWhenRegistryEmpty()
    {
        AkavacheBuilder.BlobCaches = [];

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("<none>");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.CreateNoCacheFoundException"/> reports
    /// <c>&lt;none&gt;</c> when the registry is <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNoCacheFoundExceptionShouldReport_NoneWhenRegistryNull()
    {
        AkavacheBuilder.BlobCaches = null;

        var exception = SettingsBase.CreateNoCacheFoundException("TargetClass");

        await Assert.That(exception.Message).Contains("<none>");
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass"/> throws the
    /// descriptive exception when every strategy returns <see langword="null"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBlobCacheForClassShouldThrowWhenNoStrategyResolves()
    {
        AkavacheBuilder.BlobCaches = [];

        await Assert.That(() => SettingsBase.GetBlobCacheForClass("UnresolvableClass"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="SettingsBase.GetBlobCacheForClass"/> short-circuits to
    /// the registry when an entry exists, never touching the ambient
    /// <see cref="CacheDatabase"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetBlobCacheForClassShouldShortCircuitToRegistry()
    {
        var registered = new InMemoryBlobCache(new SystemJsonSerializer());
        AkavacheBuilder.BlobCaches = new()
        {
            ["MyClass"] = registered,
        };

        var result = SettingsBase.GetBlobCacheForClass("MyClass");

        await Assert.That(result).IsSameReferenceAs(registered);
    }
}
