// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Core.Observables;

namespace Akavache.Settings.Core;

/// <summary>
/// Live observable stream backing a single settings property. Wraps a
/// <see cref="SettingsValueSubject{T}"/> seeded with the property's default value,
/// supports an explicit cold load from the owning <see cref="IBlobCache"/> (via
/// <see cref="EnsureLoaded"/>), and re-emits whenever <see cref="Set"/> is called so
/// every live subscriber observes the change.
/// </summary>
/// <remarks>
/// <para>
/// Exactly one instance exists per property per <see cref="SettingsStorage"/>. The
/// stream is created on first access to the property getter and lives until the
/// settings store is disposed.
/// </para>
/// <para>
/// The backing subject is our hand-rolled <see cref="SettingsValueSubject{T}"/> rather
/// than <c>BehaviorSubject&lt;T&gt;</c> from System.Reactive. This is deliberate: the
/// custom primitive avoids the immutable-observer-list allocation that the standard
/// Subject types pay, uses <c>System.Threading.Lock</c> on net9+, and skips the error
/// bookkeeping we don't need (cold-load failures are swallowed upstream).
/// </para>
/// </remarks>
/// <typeparam name="T">The property value type.</typeparam>
[RequiresUnreferencedCode("Settings streams serialize via the blob cache which requires types to be preserved.")]
[RequiresDynamicCode("Settings streams serialize via the blob cache which requires types to be preserved.")]
internal sealed class SettingsStream<T> : ISettingsStream, IObservable<T>
{
    /// <summary>The owning blob cache — persistent store for cold loads and writes.</summary>
    private readonly IBlobCache _cache;

    /// <summary>Prefixed key this stream reads/writes in <see cref="_cache"/>.</summary>
    private readonly string _storageKey;

    /// <summary>Live value + broadcast primitive. One per stream, seeded with the default at construction.</summary>
    private readonly SettingsValueSubject<T> _current;

#if NET9_0_OR_GREATER
    /// <summary>Synchronizes lazy construction of <see cref="_coldLoad"/>. Uses <c>System.Threading.Lock</c> on net9+, monitor-on-object on older TFMs.</summary>
    private readonly System.Threading.Lock _gate = new();
#else
    /// <summary>
    /// Synchronization gate used to control access to critical sections, ensuring thread safety for operations
    /// that interact with the underlying storage and observable state.
    /// </summary>
    private readonly object _gate = new();
#endif

    /// <summary>Cached single-fire cold-load observable — created on first <c>EnsureLoaded</c>/<c>Subscribe</c> so every subsequent caller sees the same completion.</summary>
    private IObservable<Unit>? _coldLoad;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStream{T}"/> class.
    /// </summary>
    /// <param name="cache">The blob cache this stream reads from and writes to.</param>
    /// <param name="storageKey">The fully-qualified (prefix + property name) key used in <paramref name="cache"/>.</param>
    /// <param name="defaultValue">The default value emitted to subscribers before any persisted value has been loaded from disk.</param>
    public SettingsStream(IBlobCache cache, string storageKey, T defaultValue)
    {
        _cache = cache;
        _storageKey = storageKey;
        _current = new(defaultValue);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Subscribing returns the current value via the backing
    /// <see cref="SettingsValueSubject{T}"/> and any subsequent updates from
    /// <see cref="Set"/>. It does <b>not</b> trigger a cold load from disk — callers who
    /// need the persisted value must invoke <see cref="EnsureLoaded"/> explicitly
    /// (typically via <see cref="SettingsStorage.Initialize"/>). Keeping cold load
    /// out of the <c>Subscribe</c> path is deliberate: the helper auto-subscribes in its
    /// constructor, so a subscription-triggered cold load would fan out N concurrent
    /// blob-cache reads the moment a derived settings class is instantiated, which
    /// interacted badly with the sqlite worker-thread dispatch and native-handle lifetime.
    /// </remarks>
    public IDisposable Subscribe(IObserver<T> observer) => _current.Subscribe(observer);

    /// <inheritdoc/>
    public IObservable<Unit> EnsureLoaded()
    {
        lock (_gate)
        {
            if (_coldLoad is not null)
            {
                return _coldLoad;
            }

            // Start a cold observable for the GetObject call, Publish it, and Connect
            // immediately so every caller — internal stream consumers and Initialize
            // alike — waits on the same single execution. GetObject<T> is typed as
            // IObservable<T?> because the deserializer may return null for a missing or
            // empty blob, so the forwarding to the subject (which takes a non-null T)
            // uses a null-forgiving cast rather than producing a CS8622 warning.
            var load = _cache.GetObject<T>(_storageKey)
                .Do(loaded => _current.OnNext(loaded!))
                .Select(static _ => Unit.Default)
                .CatchReturnUnit();

            var published = load.PublishLast();
            published.Connect();
            _coldLoad = published;
            return _coldLoad;
        }
    }

    /// <summary>
    /// Updates the current value: pushes it to every live subscriber via the backing
    /// <see cref="SettingsValueSubject{T}"/> and enqueues a background write to the blob
    /// cache. Returns an observable that emits <see cref="Unit"/> when the write has
    /// been accepted by the cache (propagates errors from the underlying insert).
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <returns>A one-shot observable that fires when the persistent write completes.</returns>
    public IObservable<Unit> Set(T value)
    {
        // Mark the cold load as satisfied so a future subscribe doesn't overwrite the
        // just-set value with whatever was on disk before (common ctor-then-set pattern).
        lock (_gate)
        {
            _coldLoad ??= Observable.Return(Unit.Default);
        }

        _current.OnNext(value);
        return _cache.InsertObject(_storageKey, value).Select(static _ => Unit.Default);
    }

    /// <inheritdoc/>
    public void Dispose() => _current.Dispose();
}
