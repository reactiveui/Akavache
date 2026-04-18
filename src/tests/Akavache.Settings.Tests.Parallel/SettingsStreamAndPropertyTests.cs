// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Akavache.Settings.Core;
using Akavache.SystemTextJson;
using Akavache.Tests;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests targeting missed coverage lines in <see cref="SettingsStorage"/>,
/// <see cref="SettingsStream{T}"/>, <see cref="SettingsPropertyHelper{T}"/>,
/// and <see cref="SettingsBase"/>.
/// </summary>
[Category("Akavache")]
public class SettingsStreamAndPropertyTests
{
    // ───────────────────────── SettingsStorage: EagerCreateStreams swallows exceptions ─────

    /// <summary>
    /// <see cref="SettingsStorage.EagerCreateStreams"/> swallows exceptions thrown by
    /// individual property getters — a faulty getter should not prevent the rest of the
    /// properties from being visited.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EagerCreateStreamsShouldSwallowGetterExceptions()
    {
        var probe = new ThrowingGetterProbe();
        var properties = typeof(ThrowingGetterProbe).GetRuntimeProperties();

        // Should not throw despite the Faulty getter throwing.
        SettingsStorage.EagerCreateStreams(probe, properties);

        await Assert.That(probe.GoodCount).IsGreaterThanOrEqualTo(1);
    }

    // ───────────────────────── SettingsStorage: GetOrCreateObservable default-value path ──

    /// <summary>
    /// <see cref="SettingsStorage"/> creates a stream with the supplied default value and
    /// returns it to the subscriber.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObservableShouldEmitDefaultValue()
    {
        using var storage = new ObservableTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        var value = storage.Name.SubscribeGetValue();

        await Assert.That(value).IsEqualTo("default_name");
    }

    /// <summary>
    /// Calling the same property getter twice returns the same stream instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetOrCreateObservableShouldReturnSameStreamOnSecondAccess()
    {
        using var storage = new ObservableTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        var first = storage.Name;
        var second = storage.Name;

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    // ───────────────────────── SettingsStorage: SetObservable creates stream on the fly ───

    /// <summary>
    /// <see cref="SettingsStorage"/> creates the stream on the fly when
    /// the setter is called before any getter, then persists the value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetObservableShouldCreateStreamAndPersistValue()
    {
        using var storage = new ObservableTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        // Set before any getter access — forces the on-the-fly stream creation.
        storage.SetName("hello").SubscribeAndComplete();

        var value = storage.Name.SubscribeGetValue();

        await Assert.That(value).IsEqualTo("hello");
    }

    /// <summary>
    /// <see cref="SettingsStorage"/> raises PropertyChanged with the key name on set.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetObservableShouldRaisePropertyChanged()
    {
        using var storage = new ObservableTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        string? changedProperty = null;
        storage.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        storage.SetName("world").SubscribeAndComplete();

        await Assert.That(changedProperty).IsEqualTo("Name");
    }

    // ───────────────────────── SettingsStorage: DisposeStreams error swallowing ────────────

    /// <summary>
    /// A stream that throws on <see cref="IDisposable.Dispose"/> should not crash the
    /// <c>DisposeStreams</c> loop — the error is swallowed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    [SuppressMessage(
        "Performance",
        "CA1849:Call async methods when in an async method",
        Justification = "Test deliberately exercises the synchronous Dispose path.")]
    public async Task DisposeStreamsShouldSwallowStreamDisposeExceptions()
    {
        var storage = new ThrowingDisposeStorage(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        // Access the property to create the stream, then inject a throwing stream.
        storage.Name.SubscribeGetValue();
        storage.InjectThrowingStream();

        // Dispose should complete without throwing.
        storage.Dispose();

        await Assert.That(storage).IsNotNull();
    }

    // ───────────────────────── SettingsStream: Double EnsureLoaded ────────────────────────

    /// <summary>
    /// Calling <see cref="SettingsStream{T}.EnsureLoaded"/> twice returns the same cached
    /// observable — the second call hits the early return.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EnsureLoadedShouldReturnSameObservableOnSecondCall()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var stream = new SettingsStream<string>(cache, "test:key", "seed");

        var first = stream.EnsureLoaded();
        var second = stream.EnsureLoaded();

        await Assert.That(ReferenceEquals(first, second)).IsTrue();

        // Both should complete successfully.
        first.SubscribeAndComplete();
        second.SubscribeAndComplete();
    }

    // ───────────────────────── SettingsPropertyHelper: Value, Set, Subscribe, Dispose ─────

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}.Value"/> returns the default before any update.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperValueShouldReturnDefaultBeforeUpdate()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        await Assert.That(storage.Score.Value).IsEqualTo(100);
    }

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}.Set"/> updates the Value synchronously and
    /// persists.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperSetShouldUpdateValue()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        storage.Score.Set(200).SubscribeAndComplete();

        await Assert.That(storage.Score.Value).IsEqualTo(200);
    }

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}.Subscribe"/> delegates to the backing stream.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperSubscribeShouldDelegateToStream()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        var received = new List<int>();
        var sub = storage.Score.Subscribe(Observer.Create<int>(v => received.Add(v)));

        storage.Score.Set(42).SubscribeAndComplete();
        sub.Dispose();

        await Assert.That(received).Contains(100); // initial replay
        await Assert.That(received).Contains(42); // set value
    }

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}.Dispose"/> disposes the internal subscription
    /// without throwing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperDisposeShouldNotThrow()
    {
        var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        storage.Score.Dispose();

        await Assert.That(storage.Score).IsNotNull();
        storage.Dispose();
    }

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}"/> raises PropertyChanged when the backing
    /// stream pushes a new value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperShouldRaisePropertyChangedOnUpdate()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        string? changedProp = null;
        storage.Score.PropertyChanged += (_, args) => changedProp = args.PropertyName;

        storage.Score.Set(55).SubscribeAndComplete();

        await Assert.That(changedProp).IsEqualTo("Value");
    }

    /// <summary>
    /// The implicit conversion operator on <see cref="SettingsPropertyHelper{T}"/> returns
    /// the current value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperImplicitConversionShouldReturnCurrentValue()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        int value = storage.Score;

        await Assert.That(value).IsEqualTo(100);
    }

    /// <summary>
    /// <see cref="SettingsPropertyHelper{T}.ToT"/> returns the current value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperToTShouldReturnCurrentValue()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        var value = storage.Score.ToT();

        await Assert.That(value).IsEqualTo(100);
    }

    // ───────────────────────── SettingsBase: TryGetFromBlobCacheRegistry fallback ─────────

    /// <summary>
    /// <see cref="SettingsBase.TryGetFromBlobCacheRegistry"/> iterates past a non-matching
    /// entry and falls back to the first entry in the registry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetFromBlobCacheRegistryShouldFallBackToFirstWhenNoExactMatch()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>("TestApp_RegistryFallbackToFirst");
        var first = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        CacheDatabase.CurrentInstance!.BlobCaches["SomeOtherName"] = first;

        var result = SettingsBase.TryGetFromBlobCacheRegistry("CompletelyDifferentName");

        await Assert.That(result).IsSameReferenceAs(first);
    }

    // ───────────────────────── SettingsStream: Set after EnsureLoaded ──────────────────────

    /// <summary>
    /// Calling <see cref="SettingsStream{T}.Set"/> after <see cref="SettingsStream{T}.EnsureLoaded"/>
    /// takes the <c>_coldLoad is not null</c> branch in Set, skipping the cold-load seed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetAfterEnsureLoadedShouldSkipColdLoadSeed()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var stream = new SettingsStream<string>(cache, "test:key", "seed");

        // Trigger the cold load first.
        stream.EnsureLoaded().SubscribeAndComplete();

        // Now Set — should hit the _coldLoad is not null branch.
        stream.Set("updated").SubscribeAndComplete();

        var value = ((IObservable<string>)stream).SubscribeGetValue();
        await Assert.That(value).IsEqualTo("updated");
    }

    /// <summary>
    /// Calling <see cref="SettingsStream{T}.Set"/> before <see cref="SettingsStream{T}.EnsureLoaded"/>
    /// initializes the cold load sentinel so a subsequent EnsureLoaded does not overwrite the
    /// just-set value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetBeforeEnsureLoadedShouldPreventOverwrite()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var stream = new SettingsStream<string>(cache, "test:set_first", "seed");

        stream.Set("explicit").SubscribeAndComplete();

        // EnsureLoaded after Set should return the sentinel immediately.
        stream.EnsureLoaded().SubscribeAndComplete();

        var value = ((IObservable<string>)stream).SubscribeGetValue();
        await Assert.That(value).IsEqualTo("explicit");
    }

    // ───────────────────────── SettingsStream: Dispose ───────────────────────────────────

    /// <summary>
    /// <see cref="SettingsStream{T}.Dispose"/> disposes the backing subject, completing
    /// any active subscriptions.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SettingsStreamDisposeShouldCompleteSubscribers()
    {
        var cache = new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer());
        var stream = new SettingsStream<int>(cache, "test:dispose", 42);

        var completed = false;
        ((IObservable<int>)stream).Subscribe(
            _ => { },
            () => completed = true);

        stream.Dispose();

        await Assert.That(completed).IsTrue();
    }

    // ───────────────────────── SettingsPropertyHelper: no PropertyChanged handler ────────

    /// <summary>
    /// When no <c>PropertyChanged</c> handler is registered on a
    /// <see cref="SettingsPropertyHelper{T}"/>, setting a value should not throw
    /// (exercises the <c>PropertyChanged?.Invoke</c> null-conditional path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperSetWithNoHandlerShouldNotThrow()
    {
        using var storage = new PropertyHelperTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        // No PropertyChanged handler registered — should not throw.
        storage.Score.Set(42).SubscribeAndComplete();

        await Assert.That(storage.Score.Value).IsEqualTo(42);
    }

    /// <summary>
    /// The <see cref="SettingsPropertyHelper{T}"/> implicit operator throws when
    /// given a null helper reference.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task PropertyHelperImplicitConversionShouldThrowOnNull()
    {
        SettingsPropertyHelper<int>? helper = null;

        await Assert.That(() =>
        {
            var unused = (int)helper!;
        }).Throws<ArgumentNullException>();
    }

    // ───────────────────────── SettingsStorage: SetObservable on already-created stream ──

    /// <summary>
    /// Calling <c>SetObservable</c> on a key whose stream was already created by
    /// <c>GetOrCreateObservable</c> reuses the existing stream instance.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetObservableOnExistingStreamShouldReuseAndUpdate()
    {
        using var storage = new ObservableTestSettings(
            new InMemoryBlobCache(ImmediateScheduler.Instance, new SystemJsonSerializer()));

        // Create stream via getter.
        var initial = storage.Name.SubscribeGetValue();
        await Assert.That(initial).IsEqualTo("default_name");

        // Set through the setter — should reuse the existing stream.
        storage.SetName("updated").SubscribeAndComplete();

        var updated = storage.Name.SubscribeGetValue();
        await Assert.That(updated).IsEqualTo("updated");
    }

    // ───────────────────────── Test fixtures ──────────────────────────────────────────────

    /// <summary>
    /// Minimal <see cref="SettingsStorage"/> subclass that exposes observable getter/setter
    /// for testing <c>GetOrCreateObservable</c> and <c>SetObservable</c>.
    /// </summary>
    internal sealed class ObservableTestSettings : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="ObservableTestSettings"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public ObservableTestSettings(IBlobCache cache)
            : base("Test", cache)
        {
        }

        /// <summary>Gets the live Name stream, defaulting to "default_name".</summary>
        public IObservable<string> Name => GetOrCreateObservable("default_name");

        /// <summary>Sets the Name property.</summary>
        /// <param name="value">The new value.</param>
        /// <returns>An observable that completes when the write is persisted.</returns>
        public IObservable<Unit> SetName(string value) => SetObservable(value, nameof(Name));
    }

    /// <summary>
    /// Minimal <see cref="SettingsStorage"/> subclass that exposes a
    /// <see cref="SettingsPropertyHelper{T}"/> property for testing the helper pattern.
    /// </summary>
    internal sealed class PropertyHelperTestSettings : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="PropertyHelperTestSettings"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public PropertyHelperTestSettings(IBlobCache cache)
            : base("PropHelper", cache) =>
            Score = CreateProperty(100, nameof(Score));

        /// <summary>Gets the Score property helper.</summary>
        public SettingsPropertyHelper<int> Score { get; }
    }

    /// <summary>
    /// <see cref="SettingsStorage"/> subclass that lets us inject a throwing stream into
    /// the internal dictionary so the <c>DisposeStreams</c> error-swallowing path can be
    /// exercised.
    /// </summary>
    internal sealed class ThrowingDisposeStorage : SettingsStorage
    {
        /// <summary>Initializes a new instance of the <see cref="ThrowingDisposeStorage"/> class.</summary>
        /// <param name="cache">The backing blob cache.</param>
        public ThrowingDisposeStorage(IBlobCache cache)
            : base("ThrowDispose", cache)
        {
        }

        /// <summary>Gets the live Name stream.</summary>
        public IObservable<string> Name => GetOrCreateObservable("x");

        /// <summary>
        /// Injects a stream implementation whose Dispose throws, alongside the real
        /// stream, so the DisposeStreams loop encounters the exception.
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2000:Dispose objects before losing scope",
            Justification = "Intentionally leaking for test.")]
        public void InjectThrowingStream()
        {
            var field = typeof(SettingsStorage)
                .GetField("_streams", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var dict = (ConcurrentDictionary<string, ISettingsStream>)field.GetValue(this)!;
            dict.TryAdd("_throwOnDispose", new ThrowingStream());
        }

        /// <summary>
        /// A fake <see cref="ISettingsStream"/> whose Dispose throws.
        /// </summary>
        private sealed class ThrowingStream : ISettingsStream
        {
            /// <inheritdoc/>
            public IObservable<Unit> EnsureLoaded() => Observable.Return(Unit.Default);

            /// <inheritdoc/>
            public void Dispose() => throw new InvalidOperationException("dispose failure");
        }
    }

    /// <summary>
    /// Probe object with a property getter that throws, used to verify that
    /// <see cref="SettingsStorage.EagerCreateStreams"/> swallows getter exceptions.
    /// </summary>
    private sealed class ThrowingGetterProbe
    {
        /// <summary>Gets the number of times <see cref="Good"/> was read.</summary>
        public int GoodCount { get; private set; }

        /// <summary>Gets a well-behaved property.</summary>
        public string Good
        {
            get
            {
                GoodCount++;
                return string.Empty;
            }
        }

        /// <summary>Gets a property that always throws.</summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        [SuppressMessage(
            "Performance",
            "CA1822:Member does not access instance data",
            Justification = "Instance property required for reflection-based EagerCreateStreams.")]
        public string Faulty => throw new InvalidOperationException("boom");
    }
}
