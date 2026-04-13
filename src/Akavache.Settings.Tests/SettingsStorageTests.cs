// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.Settings.Core;
using Akavache.SystemTextJson;

namespace Akavache.Settings.Tests;

/// <summary>
/// Direct tests for <see cref="SettingsStorage"/> covering the constructor argument
/// validation, the <see cref="SettingsStorage.EagerLoadProperties"/> static helper,
/// the <c>OnPropertyChanged</c> event raise path, and the
/// <c>Dispose</c> / <c>Dispose(bool)</c> code paths.
/// </summary>
[Category("Akavache")]
[NotInParallel("CacheDatabaseState")]
[TestExecutor<AkavacheTestExecutor>]
public class SettingsStorageTests
{
    /// <summary>
    /// Tests that constructing a storage with a null key prefix throws
    /// <see cref="ArgumentNullException"/> via the helper.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForNullKeyPrefix() =>
        await Assert.That(() => new TestStorage(null!, new InMemoryBlobCache(new SystemJsonSerializer())))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that constructing a storage with a whitespace key prefix throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForWhitespaceKeyPrefix() =>
        await Assert.That(() => new TestStorage("   ", new InMemoryBlobCache(new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>
    /// Tests that constructing a storage with an empty key prefix throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForEmptyKeyPrefix() =>
        await Assert.That(() => new TestStorage(string.Empty, new InMemoryBlobCache(new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerLoadProperties"/> throws when
    /// <c>target</c> is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerLoadPropertiesShouldThrowOnNullTarget() =>
        await Assert.That(() => SettingsStorage.EagerLoadProperties(null!, []))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerLoadProperties"/> throws when
    /// <c>properties</c> is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerLoadPropertiesShouldThrowOnNullProperties() =>
        await Assert.That(() => SettingsStorage.EagerLoadProperties(new object(), null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerLoadProperties"/> calls every
    /// supplied property getter against the target.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerLoadPropertiesShouldInvokeEveryGetter()
    {
        var probe = new GetterProbe();
        var properties = typeof(GetterProbe).GetRuntimeProperties();

        SettingsStorage.EagerLoadProperties(probe, properties);

        await Assert.That(probe.AlphaCount).IsEqualTo(1);
        await Assert.That(probe.BetaCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.InitializeAsync"/> runs its reflection-
    /// based eager-load pass on a background thread and visits every property on the
    /// derived storage type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeAsyncShouldEagerLoadEveryProperty()
    {
        using var storage = new ProbeStorage("test_prefix", new InMemoryBlobCache(new SystemJsonSerializer()));

        await storage.InitializeAsync();

        await Assert.That(storage.AlphaCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(storage.BetaCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerLoadProperties"/> tolerates an
    /// empty property sequence.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerLoadPropertiesShouldTolerateEmptySequence()
    {
        var probe = new GetterProbe();

        SettingsStorage.EagerLoadProperties(probe, []);

        await Assert.That(probe.AlphaCount).IsEqualTo(0);
        await Assert.That(probe.BetaCount).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that <c>OnPropertyChanged</c> fires the
    /// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
    /// event with the expected property name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnPropertyChangedShouldRaiseEventWhenSubscribed()
    {
        using var storage = new TestStorage("test_prefix", new InMemoryBlobCache(new SystemJsonSerializer()));

        string? observed = null;
        storage.PropertyChanged += (_, args) => observed = args.PropertyName;

        storage.RaisePropertyChanged("MyProperty");

        await Assert.That(observed).IsEqualTo("MyProperty");
    }

    /// <summary>
    /// Tests that <c>OnPropertyChanged</c> is a safe no-op when no subscriber has
    /// attached to the event.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnPropertyChangedShouldBeNoOpWhenNoSubscriber()
    {
        using var storage = new TestStorage("test_prefix", new InMemoryBlobCache(new SystemJsonSerializer()));

        storage.RaisePropertyChanged("MyProperty");

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>
    /// Tests that calling <see cref="IDisposable.Dispose"/> twice is idempotent
    /// (the second call is a no-op thanks to the <c>_disposedValue</c> flag).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldBeIdempotent()
    {
        var storage = new TestStorage("test_prefix", new InMemoryBlobCache(new SystemJsonSerializer()));

        storage.Dispose();
        storage.Dispose();

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>
    /// Tests that calling <see cref="IDisposable.Dispose"/> disposes the underlying
    /// blob cache (verified via the synchronous Dispose path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldDisposeUnderlyingBlobCache()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var storage = new TestStorage("test_prefix", cache);

        storage.Dispose();

        // After dispose, the cache's GetAllKeys should fail because the cache
        // backing dictionary is gone.
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await cache.GetAllKeys().FirstAsync());
    }

    /// <summary>
    /// Tests that the protected <c>Dispose(bool disposing: false)</c> path leaves
    /// the underlying cache untouched (only managed resources are released when
    /// disposing == true).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeWithDisposingFalseShouldNotTouchManagedResources()
    {
        var cache = new InMemoryBlobCache(new SystemJsonSerializer());
        var storage = new TestStorage("test_prefix", cache);

        storage.InvokeDispose(disposing: false);

        // Cache still works: insert/retrieve a key without throwing.
        await cache.Insert("k", [1, 2, 3]).FirstAsync();
        var bytes = await cache.Get("k").FirstAsync();
        await Assert.That(bytes).IsNotNull();

        await cache.DisposeAsync();
    }

    /// <summary>
    /// Test stub exposing the protected <c>Dispose(bool)</c>, the constructor, and
    /// the <c>OnPropertyChanged</c> protected method so they can be exercised from
    /// outside the assembly.
    /// </summary>
    public class TestStorage(string keyPrefix, IBlobCache cache) : SettingsStorage(keyPrefix, cache)
    {
        /// <summary>
        /// Public re-projection of the protected <c>OnPropertyChanged</c> method so
        /// the event raise path can be tested from outside the assembly.
        /// </summary>
        /// <param name="propertyName">The property name to raise the event for.</param>
        public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        /// <summary>
        /// Public re-projection of the protected <c>Dispose(bool)</c> method so the
        /// <c>disposing</c>-false code path can be exercised directly.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        public void InvokeDispose(bool disposing) => Dispose(disposing);
    }

    /// <summary>
    /// Subclass whose runtime properties increment counters so tests can assert that
    /// <see cref="SettingsStorage.InitializeAsync"/> visited each one during its
    /// reflection pass. The property getters swallow exceptions because
    /// <c>GetOrCreate</c> requires a backing key that has not been configured here.
    /// </summary>
    /// <param name="keyPrefix">The key prefix supplied to the base.</param>
    /// <param name="cache">The backing cache supplied to the base.</param>
    public class ProbeStorage(string keyPrefix, IBlobCache cache) : SettingsStorage(keyPrefix, cache)
    {
        /// <summary>Gets the number of times <see cref="Alpha"/> was read.</summary>
        public int AlphaCount { get; private set; }

        /// <summary>Gets the number of times <see cref="Beta"/> was read.</summary>
        public int BetaCount { get; private set; }

        /// <summary>Gets a stub property whose getter increments <see cref="AlphaCount"/>.</summary>
        public string Alpha
        {
            get
            {
                AlphaCount++;
                return string.Empty;
            }
        }

        /// <summary>Gets a stub property whose getter increments <see cref="BetaCount"/>.</summary>
        public string Beta
        {
            get
            {
                BetaCount++;
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Stub object whose property getters increment counters so tests can assert
    /// that <see cref="SettingsStorage.EagerLoadProperties"/> visited each one.
    /// </summary>
    private sealed class GetterProbe
    {
        public int AlphaCount { get; private set; }

        public int BetaCount { get; private set; }

        public string Alpha
        {
            get
            {
                AlphaCount++;
                return string.Empty;
            }
        }

        public string Beta
        {
            get
            {
                BetaCount++;
                return string.Empty;
            }
        }
    }
}
