// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests;
#else
namespace Akavache.Settings.Tests;
#endif

/// <summary>
/// Tests the settings-store teardown members of <see cref="AkavacheBuilderExtensions"/>:
/// <c>DisposeSettingsStore</c> releases the registered store and its backing cache, and
/// <c>DeleteSettingsStore</c> treats removing the file as best-effort — a store whose
/// cache path cannot be resolved still tears its in-memory half down and reports success.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SettingsStoreTeardownTests
{
    /// <summary>The registry key both teardown members derive from the settings type name.</summary>
    private const string StoreKey = nameof(TeardownProbeStorage);

    /// <summary>
    /// Disposing a registered store without naming a database completes its property
    /// streams, disposes the backing cache, and drops both registry entries.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DisposeSettingsStore_RegisteredStore_ReleasesStoreAndCache()
    {
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var storage = new TeardownProbeStorage(cache);
        var instance = CreateInstanceHolding(storage, cache);

        var storeTornDown = false;
        _ = storage.Marker.Subscribe(static _ => { }, () => storeTornDown = true);

        await instance.DisposeSettingsStore<TeardownProbeStorage>();

        await Assert.That(storeTornDown).IsTrue();
        await Assert.That(instance.SettingsStores.Count).IsEqualTo(0);
        await Assert.That(instance.BlobCaches.Count).IsEqualTo(0);

        var error = cache.GetAllKeys().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// An <see cref="IOException"/> raised while the delete resolves the settings cache
    /// path is absorbed: the store is still disposed and deregistered, and the caller sees
    /// a completed sequence rather than a fault.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DeleteSettingsStore_CachePathReportsIoFailure_CompletesWithoutFaulting()
    {
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var storage = new TeardownProbeStorage(cache);
        var instance = CreateInstanceHolding(storage, cache);
        instance.SettingsCachePathFault = new IOException("The settings directory is not readable.");

        var storeTornDown = false;
        _ = storage.Marker.Subscribe(static _ => { }, () => storeTornDown = true);

        await instance.DeleteSettingsStore<TeardownProbeStorage>();

        await Assert.That(instance.SettingsCachePathReads).IsEqualTo(1);
        await Assert.That(storeTornDown).IsTrue();
        await Assert.That(instance.SettingsStores.Count).IsEqualTo(0);
    }

    /// <summary>
    /// An <see cref="UnauthorizedAccessException"/> raised while the delete resolves the
    /// settings cache path is absorbed for the same reason: the caller asked for a delete,
    /// not for the process to acquire rights it does not have.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DeleteSettingsStore_CachePathReportsAccessDenied_CompletesWithoutFaulting()
    {
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var storage = new TeardownProbeStorage(cache);
        var instance = CreateInstanceHolding(storage, cache);
        instance.SettingsCachePathFault = new UnauthorizedAccessException("The settings directory is not writable.");

        var storeTornDown = false;
        _ = storage.Marker.Subscribe(static _ => { }, () => storeTornDown = true);

        await instance.DeleteSettingsStore<TeardownProbeStorage>();

        await Assert.That(instance.SettingsCachePathReads).IsEqualTo(1);
        await Assert.That(storeTornDown).IsTrue();
        await Assert.That(instance.SettingsStores.Count).IsEqualTo(0);
    }

    /// <summary>Registers <paramref name="storage"/> and <paramref name="cache"/> under the settings type name, the key both teardown members compute.</summary>
    /// <param name="storage">The settings store to register.</param>
    /// <param name="cache">The blob cache backing <paramref name="storage"/>.</param>
    /// <returns>The instance holding both registrations.</returns>
    private static FakeAkavacheInstance CreateInstanceHolding(ISettingsStorage storage, IBlobCache cache)
    {
        FakeAkavacheInstance instance = new();
        instance.SettingsStores[StoreKey] = storage;
        instance.BlobCaches[StoreKey] = cache;
        return instance;
    }

    /// <summary>Settings store with a single live property stream, so a subscriber can observe the teardown.</summary>
    /// <param name="cache">The blob cache the settings are persisted to.</param>
    private sealed class TeardownProbeStorage(IBlobCache cache) : SettingsStorage(nameof(TeardownProbeStorage), cache)
    {
        /// <summary>Gets a live stream whose subscribers complete when the store is disposed.</summary>
        public IObservable<string> Marker => GetOrCreateObservable("marker");
    }

    /// <summary>
    /// Minimal <see cref="IAkavacheInstance"/> stub for the teardown members, which only read
    /// <see cref="IAkavacheInstance.SettingsStores"/>, <see cref="IAkavacheInstance.BlobCaches"/>
    /// and <see cref="IAkavacheInstance.SettingsCachePath"/>. The path can be made to fail, standing
    /// in for an implementation that resolves it lazily from a location the process cannot reach.
    /// </summary>
    private sealed class FakeAkavacheInstance : IAkavacheInstance
    {
        /// <summary>Gets or sets the failure the <see cref="SettingsCachePath"/> getter raises, or <see langword="null"/> to return the stored path.</summary>
        public Exception? SettingsCachePathFault { get; set; }

        /// <summary>Gets the number of times <see cref="SettingsCachePath"/> was read.</summary>
        public int SettingsCachePathReads { get; private set; }

        /// <inheritdoc/>
        public Assembly ExecutingAssembly { get; } = typeof(FakeAkavacheInstance).Assembly;

        /// <inheritdoc/>
        public string ApplicationName => nameof(SettingsStoreTeardownTests);

        /// <inheritdoc/>
        public string? ApplicationRootPath => null;

        /// <inheritdoc/>
        public string? SettingsCachePath
        {
            get
            {
                SettingsCachePathReads++;
                return SettingsCachePathFault is null ? field : throw SettingsCachePathFault;
            }

            set;
        }

        /// <inheritdoc/>
        public string? ExecutingAssemblyName => ExecutingAssembly.GetName().Name;

        /// <inheritdoc/>
        public Version? Version => ExecutingAssembly.GetName().Version;

        /// <inheritdoc/>
        public IBlobCache? InMemory => null;

        /// <inheritdoc/>
        public IBlobCache? LocalMachine => null;

        /// <inheritdoc/>
        public ISecureBlobCache? Secure => null;

        /// <inheritdoc/>
        public IBlobCache? UserAccount => null;

        /// <inheritdoc/>
        public ISerializer? Serializer { get; } = new SystemJsonSerializer();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind { get; set; }

        /// <inheritdoc/>
        public string? SerializerTypeName => typeof(SystemJsonSerializer).FullName;

        /// <inheritdoc/>
        public IDictionary<string, IBlobCache> BlobCaches { get; } = new Dictionary<string, IBlobCache>();

        /// <inheritdoc/>
        public IDictionary<string, ISettingsStorage> SettingsStores { get; } = new Dictionary<string, ISettingsStorage>();
    }
}
