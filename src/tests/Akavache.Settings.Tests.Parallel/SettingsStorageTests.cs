// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using Akavache.Settings.Core;
using Akavache.SystemTextJson;
using Akavache.Tests;

namespace Akavache.Settings.Tests;

/// <summary>
/// Direct tests for <see cref="SettingsStorage"/> covering the constructor argument
/// validation, the <see cref="SettingsStorage.EagerCreateStreams"/> static helper,
/// the <c>OnPropertyChanged</c> event raise path, and the
/// <c>Dispose</c> / <c>Dispose(bool)</c> code paths.
/// </summary>
[Category("Akavache")]
public class SettingsStorageTests
{
    /// <summary>
    /// Tests that constructing a storage with a null key prefix throws
    /// <see cref="ArgumentNullException"/> via the helper.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForNullKeyPrefix() =>
        await Assert.That(static () => new TestStorage(null!, new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that constructing a storage with a whitespace key prefix throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForWhitespaceKeyPrefix() =>
        await Assert.That(static () => new TestStorage("   ", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>
    /// Tests that constructing a storage with an empty key prefix throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForEmptyKeyPrefix() =>
        await Assert.That(static () => new TestStorage(string.Empty, new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerCreateStreams"/> throws when
    /// <c>target</c> is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldThrowOnNullTarget() =>
        await Assert.That(static () => SettingsStorage.EagerCreateStreams(null!, []))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerCreateStreams"/> throws when
    /// <c>properties</c> is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldThrowOnNullProperties() =>
        await Assert.That(static () => SettingsStorage.EagerCreateStreams(new(), null!))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerCreateStreams"/> calls every
    /// supplied property getter against the target.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldInvokeEveryGetter()
    {
        GetterProbe probe = new();
        var properties = typeof(GetterProbe).GetRuntimeProperties();

        SettingsStorage.EagerCreateStreams(probe, properties);

        await Assert.That(probe.AlphaCount).IsEqualTo(1);
        await Assert.That(probe.BetaCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.Initialize"/> runs its reflection-based
    /// eager-load pass and visits every property on the derived storage type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeShouldEagerLoadEveryProperty()
    {
        using ProbeStorage storage = new("test_prefix", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        await storage.InitializeAsync();

        await Assert.That(storage.AlphaCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(storage.BetaCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.EagerCreateStreams"/> tolerates an
    /// empty property sequence.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldTolerateEmptySequence()
    {
        GetterProbe probe = new();

        SettingsStorage.EagerCreateStreams(probe, []);

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
        using TestStorage storage = new("test_prefix", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

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
        using TestStorage storage = new("test_prefix", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        storage.RaisePropertyChanged("MyProperty");

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>
    /// Tests that calling <see cref="IDisposable.Dispose"/> twice is idempotent
    /// (the second call is a no-op thanks to the <c>_disposedValue</c> flag).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task DisposeShouldBeIdempotent()
    {
        TestStorage storage = new("test_prefix", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task DisposeShouldDisposeUnderlyingBlobCache()
    {
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        TestStorage storage = new("test_prefix", cache);

        storage.Dispose();

        // After dispose, the cache's GetAllKeys should fail because the cache
        // backing dictionary is gone.
        var error = cache.GetAllKeys().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
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
        InMemoryBlobCache cache = new(ImmediateScheduler.Instance, new SystemJsonSerializer());
        TestStorage storage = new("test_prefix", cache);

        storage.InvokeDispose(disposing: false);

        // Cache still works: insert/retrieve a key without throwing.
        cache.Insert("k", [1, 2, 3]).SubscribeAndComplete();
        var bytes = cache.Get("k").SubscribeGetValue();
        await Assert.That(bytes).IsNotNull();

        cache.Dispose();
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.Initialize"/> on a storage with no observable
    /// properties hits the empty loaders early-return path (lines 105-107).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeShouldReturnImmediatelyWhenNoStreamsExist()
    {
        using EmptyStorage storage = new("empty_prefix", new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        await storage.InitializeAsync();

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>
    /// <see cref="SettingsStorage.GetOrCreateObservable{T}"/> throws when the key is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObservableShouldThrowOnNullKey()
    {
        using NullKeyStorage storage = new(new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        await Assert.That(() => storage.GetWithNullKey())
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="SettingsStorage.SetObservable{T}"/> throws when the key is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetObservableShouldThrowOnNullKey()
    {
        using NullKeyStorage storage = new(new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        await Assert.That(() => storage.SetWithNullKey("value"))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// <see cref="SettingsStorage.CreateProperty{T}"/> throws when the property name is null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreatePropertyShouldThrowOnNullPropertyName()
    {
        await Assert.That(() => new NullPropertyNameStorage(new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Disposing a storage that has active streams disposes all of them. Subscribers
    /// that were active before dispose receive OnCompleted.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task DisposeShouldCompleteActiveStreamSubscribers()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var storage = new MultiPropertyStorage(cache);

        // Subscribe before dispose so we can observe OnCompleted.
        var alphaCompleted = false;
        var betaCompleted = false;
        storage.Alpha.Subscribe(_ => { }, () => alphaCompleted = true);
        storage.Beta.Subscribe(_ => { }, () => betaCompleted = true);

        storage.Dispose();

        await Assert.That(alphaCompleted).IsTrue();
        await Assert.That(betaCompleted).IsTrue();
    }

    /// <summary>
    /// <see cref="SettingsStorage.Initialize"/> with a storage that has observable
    /// property streams exercises the <c>loaders.Length != 0</c> branch at line 105,
    /// merging and awaiting the cold-load observables.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InitializeShouldMergeLoadersWhenStreamsExist()
    {
        using MultiPropertyStorage storage = new(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        // Initialize calls EagerCreateStreams which visits Alpha and Beta,
        // populating _streams via GetOrCreateObservable. The loaders array
        // then has length > 0, exercising the Merge path.
        await storage.InitializeAsync();

        await Assert.That(storage).IsNotNull();
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
    /// <see cref="SettingsStorage.Initialize"/> visited each one during its
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
    /// that <see cref="SettingsStorage.EagerCreateStreams"/> visited each one.
    /// </summary>
    private sealed class GetterProbe
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
    /// Storage subclass with no observable properties so <c>Initialize()</c> sees
    /// an empty loaders array and takes the early-return path.
    /// </summary>
    private sealed class EmptyStorage(string keyPrefix, IBlobCache cache)
        : SettingsStorage(keyPrefix, cache);

    /// <summary>
    /// Storage subclass that exposes methods calling <c>GetOrCreateObservable</c>
    /// and <c>SetObservable</c> with an explicit null key.
    /// </summary>
    private sealed class NullKeyStorage : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="NullKeyStorage"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public NullKeyStorage(IBlobCache cache)
            : base("NullKey", cache)
        {
        }

        /// <summary>Calls <c>GetOrCreateObservable</c> with a null key.</summary>
        /// <returns>The observable (never reached).</returns>
        public IObservable<string> GetWithNullKey() => GetOrCreateObservable<string>("default", key: null!);

        /// <summary>Calls <c>SetObservable</c> with a null key.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The observable (never reached).</returns>
        public IObservable<Unit> SetWithNullKey(string value) => SetObservable(value, key: null!);
    }

    /// <summary>
    /// Storage subclass that calls <c>CreateProperty</c> with a null property name,
    /// which triggers the null guard in the constructor.
    /// </summary>
    private sealed class NullPropertyNameStorage : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="NullPropertyNameStorage"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public NullPropertyNameStorage(IBlobCache cache)
            : base("NullProp", cache) =>
            _ = CreateProperty("default", propertyName: null!);
    }

    /// <summary>
    /// Storage subclass with two observable properties for exercising the
    /// multi-stream dispose path.
    /// </summary>
    private sealed class MultiPropertyStorage : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="MultiPropertyStorage"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public MultiPropertyStorage(IBlobCache cache)
            : base("Multi", cache)
        {
        }

        /// <summary>Gets the Alpha stream.</summary>
        public IObservable<string> Alpha => GetOrCreateObservable("a");

        /// <summary>Gets the Beta stream.</summary>
        public IObservable<string> Beta => GetOrCreateObservable("b");
    }
}
