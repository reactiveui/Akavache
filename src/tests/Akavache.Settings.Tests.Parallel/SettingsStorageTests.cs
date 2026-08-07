// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Reflection;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests;
#else
namespace Akavache.Settings.Tests;
#endif

/// <summary>
/// Direct tests for <see cref="SettingsStorage"/> covering the constructor argument
/// validation, the <see cref="SettingsStorage.EagerCreateStreams"/> static helper,
/// the <c>OnPropertyChanged</c> event raise path, and the
/// <c>Dispose</c> / <c>Dispose(bool)</c> code paths.
/// </summary>
[Category("Akavache")]
public class SettingsStorageTests
{
    /// <summary>The key prefix handed to the storage fixtures under test.</summary>
    private const string StorageKeyPrefix = "test_prefix";

    /// <summary>The property name raised through <c>OnPropertyChanged</c>.</summary>
    private const string RaisedPropertyName = "MyProperty";

    /// <summary>Bytes written to the blob cache to prove it is still usable.</summary>
    private static readonly byte[] SamplePayload = [1, 2, 3];

    /// <summary>Tests that constructing a storage with a null key prefix throws <see cref="ArgumentNullException"/> via the helper.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForNullKeyPrefix() =>
        await Assert.That(static () => new TestStorage(null!, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that constructing a storage with a whitespace key prefix throws <see cref="ArgumentException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForWhitespaceKeyPrefix() =>
        await Assert.That(static () => new TestStorage("   ", new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>Tests that constructing a storage with an empty key prefix throws <see cref="ArgumentException"/>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldThrowForEmptyKeyPrefix() =>
        await Assert.That(static () => new TestStorage(string.Empty, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentException>();

    /// <summary>Tests that <see cref="SettingsStorage.EagerCreateStreams"/> throws when <c>target</c> is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldThrowOnNullTarget() =>
        await Assert.That(static () => SettingsStorage.EagerCreateStreams(null!, []))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that <see cref="SettingsStorage.EagerCreateStreams"/> throws when <c>properties</c> is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldThrowOnNullProperties() =>
        await Assert.That(static () => SettingsStorage.EagerCreateStreams(new(), null!))
            .Throws<ArgumentNullException>();

    /// <summary>Tests that <see cref="SettingsStorage.EagerCreateStreams"/> calls every supplied property getter against the target.</summary>
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

    /// <summary>Tests that <see cref="SettingsStorage.Initialize"/> runs its reflection-based eager-load pass and visits every property on the derived storage type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeShouldEagerLoadEveryProperty()
    {
        using ProbeStorage storage = new(StorageKeyPrefix, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        await storage.InitializeAsync();

        await Assert.That(storage.AlphaCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(storage.BetaCount).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Tests that an indexer — which the pre-warm sweep cannot supply arguments for, so
    /// <see cref="PropertyInfo.GetValue(object)"/> raises <see cref="TargetParameterCountException"/> —
    /// does not stop <see cref="SettingsStorage.EagerCreateStreams"/> reading the remaining properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldContinuePastAnIndexer()
    {
        IndexedProbe probe = new();

        SettingsStorage.EagerCreateStreams(probe, typeof(IndexedProbe).GetRuntimeProperties());

        // The indexer needs an argument the sweep has none to give, so its body never runs...
        await Assert.That(probe.IndexerCount).IsEqualTo(0);

        // ...and the plain property is still read.
        await Assert.That(probe.AlphaCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that a property whose getter cannot be reached — <see cref="PropertyInfo.GetValue(object)"/>
    /// raises <see cref="MethodAccessException"/> rather than running any code — does not stop
    /// <see cref="SettingsStorage.EagerCreateStreams"/> reading the remaining properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldContinuePastAnUnreachableGetter()
    {
        GetterProbe probe = new();
        UnreachableGetterProperty unreachable = new();
        List<PropertyInfo> properties = [unreachable, .. typeof(GetterProbe).GetRuntimeProperties()];

        SettingsStorage.EagerCreateStreams(probe, properties);

        await Assert.That(unreachable.ReadAttempts).IsEqualTo(1);
        await Assert.That(probe.AlphaCount).IsEqualTo(1);
        await Assert.That(probe.BetaCount).IsEqualTo(1);
    }

    /// <summary>Tests that <see cref="SettingsStorage.EagerCreateStreams"/> tolerates an empty property sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldTolerateEmptySequence()
    {
        GetterProbe probe = new();

        SettingsStorage.EagerCreateStreams(probe, []);

        await Assert.That(probe.AlphaCount).IsEqualTo(0);
        await Assert.That(probe.BetaCount).IsEqualTo(0);
    }

    /// <summary>Tests that <c>OnPropertyChanged</c> fires the <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> event with the expected property name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnPropertyChangedShouldRaiseEventWhenSubscribed()
    {
        using TestStorage storage = new(StorageKeyPrefix, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        string? observed = null;
        storage.PropertyChanged += (_, args) => observed = args.PropertyName;

        storage.RaisePropertyChanged(RaisedPropertyName);

        await Assert.That(observed).IsEqualTo(RaisedPropertyName);
    }

    /// <summary>
    /// Tests that the name-less <c>OnPropertyChanged</c> overload raises the event with a
    /// <see langword="null"/> property name — the <see cref="System.ComponentModel.INotifyPropertyChanged"/>
    /// convention for "every property on this instance may have changed".
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnPropertyChangedWithoutNameShouldSignalEveryProperty()
    {
        using TestStorage storage = new(StorageKeyPrefix, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        var raiseCount = 0;
        string? observed = RaisedPropertyName;
        storage.PropertyChanged += (_, args) =>
        {
            raiseCount++;
            observed = args.PropertyName;
        };

        storage.RaisePropertyChanged();

        await Assert.That(raiseCount).IsEqualTo(1);
        await Assert.That(observed).IsNull();
    }

    /// <summary>Tests that <c>OnPropertyChanged</c> is a safe no-op when no subscriber has attached to the event.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task OnPropertyChangedShouldBeNoOpWhenNoSubscriber()
    {
        using TestStorage storage = new(StorageKeyPrefix, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        storage.RaisePropertyChanged(RaisedPropertyName);

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>Tests that calling <see cref="IDisposable.Dispose"/> twice is idempotent (the second call is a no-op thanks to the <c>_disposedValue</c> flag).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldBeIdempotent()
    {
        TestStorage storage = new(StorageKeyPrefix, new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        storage.Dispose();
        storage.Dispose();

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>Tests that calling <see cref="IDisposable.Dispose"/> disposes the underlying blob cache (verified via the synchronous Dispose path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldDisposeUnderlyingBlobCache()
    {
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        TestStorage storage = new(StorageKeyPrefix, cache);

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
        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());
        TestStorage storage = new(StorageKeyPrefix, cache);

        storage.InvokeDispose(disposing: false);

        // Cache still works: insert/retrieve a key without throwing.
        cache.Insert("k", SamplePayload).SubscribeAndComplete();
        var bytes = cache.Get("k").SubscribeGetValue();
        await Assert.That(bytes).IsNotNull();

        cache.Dispose();
    }

    /// <summary>Tests that <see cref="SettingsStorage.Initialize"/> on a storage with no observable properties hits the empty loaders early-return path (lines 105-107).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InitializeShouldReturnImmediatelyWhenNoStreamsExist()
    {
        using EmptyStorage storage = new("empty_prefix", new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        await storage.InitializeAsync();

        await Assert.That(storage).IsNotNull();
    }

    /// <summary>Tests that <see cref="SettingsStorage.GetOrCreateObservable{T}"/> throws when the key is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObservableShouldThrowOnNullKey()
    {
        using NullKeyStorage storage = new(new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        await Assert.That(() => storage.GetWithNullKey())
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests that <see cref="SettingsStorage.SetObservable{T}"/> throws when the key is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetObservableShouldThrowOnNullKey()
    {
        using NullKeyStorage storage = new(new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

        await Assert.That(() => storage.SetWithNullKey("value"))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests that <see cref="SettingsStorage.CreateProperty{T}"/> throws when the property name is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreatePropertyShouldThrowOnNullPropertyName() =>
        await Assert.That(static () => new NullPropertyNameStorage(
                new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer())))
            .Throws<ArgumentNullException>();

    /// <summary>
    /// Disposing a storage that has active streams disposes all of them. Subscribers
    /// that were active before dispose receive OnCompleted.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldCompleteActiveStreamSubscribers()
    {
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var storage = new MultiPropertyStorage(cache);

        // Subscribe before dispose so we can observe OnCompleted.
        var alphaCompleted = false;
        var betaCompleted = false;
        _ = storage.Alpha.Subscribe(static _ => { }, () => alphaCompleted = true);
        _ = storage.Beta.Subscribe(static _ => { }, () => betaCompleted = true);

        storage.Dispose();

        await Assert.That(alphaCompleted).IsTrue();
        await Assert.That(betaCompleted).IsTrue();
    }

    /// <summary>
    /// Tests that a stream whose teardown reports it has already been disposed does not
    /// strand the rest of the disposal: the remaining stream still completes its
    /// subscribers and the underlying blob cache is still released.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DisposeShouldContinuePastAStreamThatReportsItIsAlreadyDisposed()
    {
        var cache = new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer());
        var storage = new MultiPropertyStorage(cache);

        var alphaTeardownReached = false;
        var betaCompleted = false;

        // Completing this subscriber throws out of the subject's broadcast loop, so the
        // stream's Dispose surfaces an ObjectDisposedException to the storage.
        _ = storage.Alpha.Subscribe(
            static _ => { },
            () =>
            {
                alphaTeardownReached = true;
                throw new ObjectDisposedException(nameof(SettingsStream<>));
            });
        _ = storage.Beta.Subscribe(static _ => { }, () => betaCompleted = true);

        storage.Dispose();

        await Assert.That(alphaTeardownReached).IsTrue();
        await Assert.That(betaCompleted).IsTrue();

        var error = cache.GetAllKeys().SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    /// <summary>
    /// Tests that <see cref="SettingsStorage.Initialize"/> with a storage that has
    /// observable property streams exercises the <c>loaders.Length != 0</c> branch,
    /// merging and awaiting the cold-load observables.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InitializeShouldMergeLoadersWhenStreamsExist()
    {
        using MultiPropertyStorage storage = new(
            new InMemoryBlobCache(ImmediateSequencer.Instance, new SystemJsonSerializer()));

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
    /// <param name="keyPrefix">The prefix applied to every settings key.</param>
    /// <param name="cache">The blob cache the settings are persisted to.</param>
    public class TestStorage(string keyPrefix, IBlobCache cache) : SettingsStorage(keyPrefix, cache)
    {
        /// <summary>
        /// Public re-projection of the protected <c>OnPropertyChanged</c> method so
        /// the event raise path can be tested from outside the assembly.
        /// </summary>
        /// <param name="propertyName">The property name to raise the event for.</param>
        public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        /// <summary>
        /// Public re-projection of the protected name-less <c>OnPropertyChanged</c> overload,
        /// which signals that every property on the instance may have changed.
        /// </summary>
        public void RaisePropertyChanged() => OnPropertyChanged();

        /// <summary>Public re-projection of the protected <c>Dispose(bool)</c> method so the <c>disposing</c>-false code path can be exercised directly.</summary>
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

    /// <summary>Stub object whose property getters increment counters so tests can assert that <see cref="SettingsStorage.EagerCreateStreams"/> visited each one.</summary>
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
    /// Stub object exposing an indexer alongside a plain property. The eager-create sweep
    /// has no arguments to pass an indexer, so reading it raises
    /// <see cref="TargetParameterCountException"/> before the indexer body runs.
    /// </summary>
    private sealed class IndexedProbe
    {
        /// <summary>Gets the number of times <see cref="Alpha"/> was read.</summary>
        public int AlphaCount { get; private set; }

        /// <summary>Gets the number of times the indexer body ran.</summary>
        public int IndexerCount { get; private set; }

        /// <summary>Gets a stub property whose getter increments <see cref="AlphaCount"/>.</summary>
        public string Alpha
        {
            get
            {
                AlphaCount++;
                return string.Empty;
            }
        }

        /// <summary>Gets a stub indexer whose body increments <see cref="IndexerCount"/>.</summary>
        /// <param name="index">Unused positional argument.</param>
        /// <returns>An empty string.</returns>
        public string this[int index]
        {
            get
            {
                IndexerCount++;
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// A property whose getter cannot be reached at all: the read fails with
    /// <see cref="MethodAccessException"/> instead of invoking a getter, which is the one
    /// failure shape the eager-create sweep cannot produce from an ordinary compiled property.
    /// </summary>
    private sealed class UnreachableGetterProperty : PropertyInfo
    {
        /// <summary>The accessor set reported for a property that has none to hand out.</summary>
        private static readonly MethodInfo[] NoAccessors = [];

        /// <summary>The (absent) index parameters of this non-indexed property.</summary>
        private static readonly ParameterInfo[] NoIndexParameters = [];

        /// <summary>The attribute set reported for a property that carries none.</summary>
        private static readonly object[] NoAttributes = [];

        /// <summary>Gets the number of times the sweep attempted to read this property.</summary>
        public int ReadAttempts { get; private set; }

        /// <inheritdoc/>
        public override PropertyAttributes Attributes => PropertyAttributes.None;

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override Type PropertyType => typeof(string);

        /// <inheritdoc/>
        public override Type? DeclaringType => typeof(GetterProbe);

        /// <inheritdoc/>
        public override string Name => "Unreachable";

        /// <inheritdoc/>
        public override Type? ReflectedType => typeof(GetterProbe);

        /// <inheritdoc/>
        public override MethodInfo[] GetAccessors(bool nonPublic) => NoAccessors;

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => NoAttributes;

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => GetCustomAttributes(inherit);

        /// <inheritdoc/>
        public override MethodInfo? GetGetMethod(bool nonPublic) => null;

        /// <inheritdoc/>
        public override ParameterInfo[] GetIndexParameters() => NoIndexParameters;

        /// <inheritdoc/>
        public override MethodInfo? GetSetMethod(bool nonPublic) => null;

        /// <inheritdoc/>
        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            ReadAttempts++;
            throw new MethodAccessException("The getter is not reachable from the pre-warm sweep.");
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit) => false;

        /// <inheritdoc/>
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Storage subclass with no observable properties so <c>Initialize()</c> sees
    /// an empty loaders array and takes the early-return path.
    /// </summary>
    /// <param name="keyPrefix">The prefix applied to every settings key.</param>
    /// <param name="cache">The blob cache the settings are persisted to.</param>
    private sealed class EmptyStorage(string keyPrefix, IBlobCache cache) : SettingsStorage(keyPrefix, cache);

    /// <summary>Storage subclass that exposes methods calling <c>GetOrCreateObservable</c> and <c>SetObservable</c> with an explicit null key.</summary>
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
        public IObservable<string> GetWithNullKey() => GetOrCreateObservable("default", null!);

        /// <summary>Calls <c>SetObservable</c> with a null key.</summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The observable (never reached).</returns>
        public IObservable<RxVoid> SetWithNullKey(string value) => SetObservable(value, null!);
    }

    /// <summary>Storage subclass that calls <c>CreateProperty</c> with a null property name, which triggers the null guard in the constructor.</summary>
    private sealed class NullPropertyNameStorage : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="NullPropertyNameStorage"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public NullPropertyNameStorage(IBlobCache cache)
            : base("NullProp", cache) =>
            _ = CreateProperty("default", null!);
    }

    /// <summary>Storage subclass with two observable properties for exercising the multi-stream dispose path.</summary>
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
