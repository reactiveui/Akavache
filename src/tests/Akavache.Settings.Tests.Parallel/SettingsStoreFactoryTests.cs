// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests;
#else
namespace Akavache.Settings.Tests;
#endif

/// <summary>Verifies settings registration and constructor cache scoping without global cache setup.</summary>
public class SettingsStoreFactoryTests
{
    /// <summary>The persisted key used by the test settings type.</summary>
    private const string ValueKey = "__FactorySettings__:Value";

    /// <summary>Registers the exact supplied cache and returned store under the caller's key.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Create_RegistersStoreAndUsesSuppliedCache()
    {
        const string storeKey = "custom";
        const int expected = 42;
        var instance = CacheDatabase.CreateBuilder();
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        using var store = SettingsStoreFactory.Create<FactorySettings>(instance, storeKey, cache);

        await store.SetValue(expected);

        await Assert.That(instance.BlobCaches[storeKey]).IsSameReferenceAs(cache);
        await Assert.That(instance.SettingsStores[storeKey]).IsSameReferenceAs(store);
        await Assert.That(await cache.GetObject<int>(ValueKey)).IsEqualTo(expected);
    }

    /// <summary>Restores the outer constructor cache after a successful nested creation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Create_RestoresOuterCache()
    {
        const int outerValue = 2;
        var instance = CacheDatabase.CreateBuilder();
        using InMemoryBlobCache outerCache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        using InMemoryBlobCache innerCache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        using var scope = SettingsBase.PushAmbientCache(outerCache);
        using var inner = SettingsStoreFactory.Create<FactorySettings>(instance, "inner", innerCache);
        using FactorySettings outer = new();

        await inner.SetValue(1);
        await outer.SetValue(outerValue);

        await Assert.That(await innerCache.GetObject<int>(ValueKey)).IsEqualTo(1);
        await Assert.That(await outerCache.GetObject<int>(ValueKey)).IsEqualTo(outerValue);
    }

    /// <summary>A failing constructor leaves no store registration and restores the outer cache.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Create_ConstructorThrows_RestoresOuterCacheWithoutPublishingStore()
    {
        const string storeKey = "failed";
        const int outerValue = 7;
        var instance = CacheDatabase.CreateBuilder();
        using InMemoryBlobCache outerCache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        using InMemoryBlobCache innerCache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        using var scope = SettingsBase.PushAmbientCache(outerCache);

        await Assert.That(() => SettingsStoreFactory.Create<ThrowingSettings>(instance, storeKey, innerCache)).ThrowsException();
        using FactorySettings outer = new();
        await outer.SetValue(outerValue);

        await Assert.That(instance.SettingsStores.ContainsKey(storeKey)).IsFalse();
        await Assert.That(instance.BlobCaches[storeKey]).IsSameReferenceAs(innerCache);
        await Assert.That(await outerCache.GetObject<int>(ValueKey)).IsEqualTo(outerValue);
    }

    /// <summary>A settings type whose writes expose the cache chosen by its constructor.</summary>
    public sealed class FactorySettings : SettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="FactorySettings"/> class.</summary>
        public FactorySettings()
            : base(nameof(FactorySettings))
        {
        }

        /// <summary>Writes a value through the constructor-selected cache.</summary>
        /// <param name="value">The value to persist.</param>
        /// <returns>Completion of the write.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IObservable<RxVoid> SetValue(int value) => SetObservable(value, "Value");
    }

    /// <summary>A settings type that fails during construction.</summary>
    public sealed class ThrowingSettings : SettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="ThrowingSettings"/> class.</summary>
        /// <exception cref="InvalidOperationException">Always thrown to exercise failed construction.</exception>
        public ThrowingSettings()
            : base(nameof(ThrowingSettings)) => throw new InvalidOperationException("Constructor failure.");
    }
}
