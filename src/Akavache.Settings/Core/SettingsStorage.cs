// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Akavache.Helpers;

namespace Akavache.Settings.Core;

/// <summary>
/// Provides a base class for implementing observable application settings storage using
/// Akavache. Each property is exposed as a live <see cref="IObservable{T}"/> backed by
/// a <see cref="SettingsStream{T}"/> — subscribers see the current value immediately,
/// are updated on every write, and never block the calling thread. Persistence goes
/// through the underlying <see cref="IBlobCache"/> asynchronously.
/// </summary>
/// <remarks>
/// <para>
/// This is a deliberate break from the earlier <c>GetOrCreate&lt;T&gt;</c>/<c>SetOrCreate&lt;T&gt;</c>
/// sync getter pattern. That model called <c>.Wait()</c> on the underlying blob cache's
/// observable chain — a synchronous bridge that deadlocked (and occasionally crashed
/// natively) against async backends like the worker-thread sqlite queue. The observable
/// shape fits Akavache's core API and eliminates the whole class of <c>.Wait()</c>
/// hazards from settings code.
/// </para>
/// <para>
/// Typical derived class:
/// <code>
/// public sealed class MySettings : SettingsBase
/// {
///     public MySettings() : base(nameof(MySettings)) { }
///
///     public IObservable&lt;bool&gt; Enabled =&gt; GetOrCreateObservable(true);
///
///     public IObservable&lt;Unit&gt; SetEnabled(bool value) =&gt;
///         SetObservable(value, nameof(Enabled));
/// }
/// </code>
/// Callers subscribe to <c>Enabled</c> to receive the current value + any future
/// updates, or call <c>await settings.Enabled.FirstAsync()</c> for a one-shot read.
/// </para>
/// </remarks>
public abstract class SettingsStorage : ISettingsStorage
{
    /// <summary>The underlying blob cache used for persistent storage of settings values.</summary>
    private readonly IBlobCache _blobCache;

    /// <summary>
    /// Registry of live per-property observable streams. Keyed by property name (as
    /// supplied via <see cref="CallerMemberNameAttribute"/> to the getter/setter helpers),
    /// values are <see cref="SettingsStream{T}"/> instances erased to <see cref="ISettingsStream"/>
    /// so the dictionary can hold heterogeneous types.
    /// </summary>
    private readonly ConcurrentDictionary<string, ISettingsStream> _streams = new();

    /// <summary>Prefix prepended to every settings key in the blob cache to avoid collisions.</summary>
    private readonly string _keyPrefix;

    /// <summary>Tracks whether <see cref="Dispose(bool)"/> has already run.</summary>
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStorage"/> class.
    /// </summary>
    /// <param name="keyPrefix">The prefix used for all settings keys in the blob cache. Should be unique to avoid key collisions.</param>
    /// <param name="cache">The blob cache implementation where settings will be stored and retrieved.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPrefix"/> is null, empty, or whitespace.</exception>
    protected SettingsStorage(string keyPrefix, IBlobCache cache)
    {
        ArgumentExceptionHelper.ThrowIfNullOrWhiteSpace(keyPrefix);

        _keyPrefix = keyPrefix;
        _blobCache = cache;
    }

    /// <summary>
    /// Occurs when a property value changes. Raised by <see cref="SetObservable{T}"/>
    /// after updating the underlying stream so plain <see cref="INotifyPropertyChanged"/>
    /// consumers (e.g. data-binding frameworks that don't speak Rx) can still observe
    /// mutations. Observable-aware consumers should prefer subscribing to the property
    /// stream directly.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Pre-warms every settings property by triggering its getter (which lazily creates
    /// the backing <see cref="SettingsStream{T}"/>) and waiting for each stream's cold
    /// load from disk to complete. Calling this at startup is optional — subscribing to
    /// a property without having initialized will still work; you'll just see the
    /// default value briefly before the disk-loaded value arrives.
    /// </summary>
    /// <returns>A one-shot observable that completes when every stream's cold load has finished.</returns>
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
    public IObservable<Unit> Initialize() =>
        Observable.Defer(() =>
        {
            EagerCreateStreams(this, GetType().GetRuntimeProperties());

            var loaders = _streams.Values.Select(static s => s.EnsureLoaded()).ToArray();
            if (loaders.Length == 0)
            {
                return Observable.Return(Unit.Default);
            }

            return loaders.Merge().IgnoreElements().Concat(Observable.Return(Unit.Default));
        });

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Invokes each getter on <paramref name="target"/> (ignoring the returned observable)
    /// so every property's <see cref="SettingsStream{T}"/> is eagerly constructed and
    /// registered in the per-instance stream dictionary. Called from
    /// <see cref="Initialize"/>; separated for unit-test isolation.
    /// </summary>
    /// <param name="target">The instance whose property getters should be invoked.</param>
    /// <param name="properties">The property set to enumerate — usually <c>GetType().GetRuntimeProperties()</c>.</param>
    internal static void EagerCreateStreams(object target, IEnumerable<PropertyInfo> properties)
    {
        ArgumentExceptionHelper.ThrowIfNull(target);
        ArgumentExceptionHelper.ThrowIfNull(properties);

        foreach (var property in properties)
        {
            try
            {
                property.GetValue(target);
            }
            catch
            {
                // Swallow reflection/getter failures — the goal is best-effort pre-warm,
                // not a strict contract assertion. A faulty getter should not take down
                // the rest of the initialization sweep.
            }
        }
    }

    /// <summary>
    /// Returns the live observable stream for a settings property, creating it on first
    /// access. The returned observable emits the current value on subscribe (starting
    /// with <paramref name="defaultValue"/> until the cold load completes, then the
    /// persisted value) and re-emits whenever <see cref="SetObservable{T}(T, string?)"/>
    /// is called for the same key.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="defaultValue">The value emitted before any persisted value has been loaded from disk.</param>
    /// <param name="key">The property name — usually filled in automatically via <see cref="CallerMemberNameAttribute"/>.</param>
    /// <returns>A live observable that emits the current value and any subsequent updates.</returns>
    [RequiresUnreferencedCode("GetOrCreateObservable requires types to be preserved for serialization.")]
    [RequiresDynamicCode("GetOrCreateObservable requires types to be preserved for serialization.")]
    protected IObservable<T> GetOrCreateObservable<T>(T defaultValue, [CallerMemberName] string? key = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(key);

        // GetOrAdd's 3-arg state overload is .NET 6+, so capture locals via closure for
        // net462/472/481 compatibility. The closure allocation is amortized across the
        // lifetime of the stream (one-time per property) — not a hot-path cost.
        var cache = _blobCache;
        var prefix = _keyPrefix;
        var stream = _streams.GetOrAdd(
            key,
            k => new SettingsStream<T>(cache, $"{prefix}:{k}", defaultValue));

        return (SettingsStream<T>)stream;
    }

    /// <summary>
    /// Creates a property facade backed by the live observable stream for
    /// <paramref name="propertyName"/>. The returned <see cref="SettingsPropertyHelper{T}"/>
    /// exposes a sync <c>Value</c> getter, a <c>Set</c> method, an observable surface, and
    /// <see cref="INotifyPropertyChanged"/> notifications — letting derived settings
    /// classes publish plain C# properties whose type is
    /// <see cref="SettingsPropertyHelper{T}"/> without giving up the observable-first
    /// backbone.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="defaultValue">The value emitted before any persisted value has been loaded from disk.</param>
    /// <param name="propertyName">The property name. Caller-member-name is resolved automatically when the helper is constructed from a property initializer or a constructor.</param>
    /// <returns>A new <see cref="SettingsPropertyHelper{T}"/> bound to the backing stream.</returns>
    [RequiresUnreferencedCode("CreateProperty requires types to be preserved for serialization.")]
    [RequiresDynamicCode("CreateProperty requires types to be preserved for serialization.")]
    protected SettingsPropertyHelper<T> CreateProperty<T>(T defaultValue, [CallerMemberName] string? propertyName = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(propertyName);

        var cache = _blobCache;
        var prefix = _keyPrefix;
        var stream = (SettingsStream<T>)_streams.GetOrAdd(
            propertyName,
            k => new SettingsStream<T>(cache, $"{prefix}:{k}", defaultValue));

        return new(stream, defaultValue);
    }

    /// <summary>
    /// Updates the live stream for a settings property and enqueues a persistent write.
    /// Also raises <see cref="PropertyChanged"/> for non-Rx consumers. If the stream
    /// doesn't exist yet (setter called before any getter), it's created on the fly
    /// using <paramref name="value"/> as the seeded default.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="value">The new value to publish and persist.</param>
    /// <param name="key">The property name. Unlike the getter, the setter cannot rely on <see cref="CallerMemberNameAttribute"/> because the caller is <c>SetFoo(value)</c>, not <c>Foo</c> — pass the matching getter's name explicitly with <c>nameof(Foo)</c>.</param>
    /// <returns>An observable that fires <see cref="Unit"/> when the persistent write completes.</returns>
    [RequiresUnreferencedCode("SetObservable requires types to be preserved for serialization.")]
    [RequiresDynamicCode("SetObservable requires types to be preserved for serialization.")]
    protected IObservable<Unit> SetObservable<T>(T value, [CallerMemberName] string? key = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(key);

        var cache = _blobCache;
        var prefix = _keyPrefix;
        var seed = value;
        var stream = _streams.GetOrAdd(
            key,
            k => new SettingsStream<T>(cache, $"{prefix}:{k}", seed));

        var result = ((SettingsStream<T>)stream).Set(value);
        OnPropertyChanged(key);
        return result;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            DisposeStreams();
            _blobCache.Dispose();
        }

        _disposedValue = true;
    }

    /// <summary>
    /// Disposes every active per-property stream and clears the registry. Called from
    /// <see cref="Dispose(bool)"/> to release the backing <see cref="BehaviorSubject{T}"/> resources.
    /// </summary>
    private void DisposeStreams()
    {
        foreach (var stream in _streams.Values)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                // Best-effort: one faulty stream should not block disposal of the rest.
            }
        }

        _streams.Clear();
    }
}
