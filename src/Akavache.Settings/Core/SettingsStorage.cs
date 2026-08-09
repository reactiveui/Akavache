// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Core;
#else
namespace Akavache.Settings.Core;
#endif

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
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public class SettingsStorage : ISettingsStorage
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
    private int _disposedValue;

    /// <summary>Initializes a new instance of the <see cref="SettingsStorage"/> class.</summary>
    /// <param name="keyPrefix">The prefix used for all settings keys in the blob cache. Should be unique to avoid key collisions.</param>
    /// <param name="cache">The blob cache implementation where settings will be stored and retrieved.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPrefix"/> is null, empty, or whitespace.</exception>
    protected SettingsStorage(string keyPrefix, IBlobCache cache)
    {
        ArgumentValidation.ThrowIfNullOrWhiteSpace(keyPrefix);

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Settings initialization requires types to be preserved for reflection.")]
    [RequiresDynamicCode("Settings initialization requires types to be preserved for reflection.")]
    public IObservable<RxVoid> Initialize() =>
        Signal.Defer(() =>
        {
            EagerCreateStreams(this, GetType().GetRuntimeProperties());

            List<IObservable<RxVoid>> loaders = new(_streams.Count);
            foreach (var entry in _streams)
            {
                loaders.Add(entry.Value.EnsureLoaded());
            }

            return loaders.Count == 0
                ? ImmutableReturnRxVoidSignal.Instance
                : loaders.Merge().IgnoreElements().Concat(ImmutableReturnRxVoidSignal.Instance);
        });

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
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
                _ = property.GetValue(target);
            }
            catch (TargetInvocationException)
            {
                // The getter itself threw. Pre-warming is best-effort, so one faulty property
                // must not take down the rest of the sweep.
            }
            catch (TargetParameterCountException)
            {
                // An indexer needs arguments this sweep cannot supply; it has no stream to warm.
            }
            catch (MethodAccessException)
            {
                // The getter is not reachable from here, so there is nothing to pre-warm.
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

        var stream = _streams.GetOrAddWithState(
            key,
            static (k, state) => (ISettingsStream)new SettingsStream<T>(state.Cache, $"{state.Prefix}:{k}", state.DefaultValue),
            (Cache: _blobCache, Prefix: _keyPrefix, DefaultValue: defaultValue));

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

        var stream = (SettingsStream<T>)_streams.GetOrAddWithState(
            propertyName,
            static (k, state) => (ISettingsStream)new SettingsStream<T>(state.Cache, $"{state.Prefix}:{k}", state.DefaultValue),
            (Cache: _blobCache, Prefix: _keyPrefix, DefaultValue: defaultValue));

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
    /// <param name="key">
    /// The property name. Unlike the getter, the setter cannot rely on
    /// <see cref="CallerMemberNameAttribute"/> because the caller is <c>SetFoo(value)</c>, not
    /// <c>Foo</c> — pass the matching getter's name explicitly with <c>nameof(Foo)</c>.
    /// </param>
    /// <returns>An observable that fires <see cref="RxVoid"/> when the persistent write completes.</returns>
    [RequiresUnreferencedCode("SetObservable requires types to be preserved for serialization.")]
    [RequiresDynamicCode("SetObservable requires types to be preserved for serialization.")]
    protected IObservable<RxVoid> SetObservable<T>(T value, [CallerMemberName] string? key = null)
    {
        ArgumentExceptionHelper.ThrowIfNull(key);

        var stream = _streams.GetOrAddWithState(
            key,
            static (k, state) => (ISettingsStream)new SettingsStream<T>(state.Cache, $"{state.Prefix}:{k}", state.Seed),
            (Cache: _blobCache, Prefix: _keyPrefix, Seed: value));

        var result = ((SettingsStream<T>)stream).Set(value);
        OnPropertyChanged(key);
        return result;
    }

    /// <summary>Raises the <see cref="PropertyChanged"/> event for the specified property name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPropertyChanged() =>
        OnPropertyChanged((string?)null);

    /// <summary>Raises the <see cref="PropertyChanged"/> event for the specified property name.</summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnPropertyChanged(string? propertyName) => PropertyChanged?.Invoke(this, new(propertyName));

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        // Claimed up front so a second caller returns immediately rather than racing the first
        // through the disposal below.
        if (Interlocked.Exchange(ref _disposedValue, 1) != 0)
        {
            return;
        }

        if (!disposing)
        {
            return;
        }

        DisposeStreams();
        _blobCache.Dispose();
    }

    /// <summary>
    /// Disposes every active per-property stream and clears the registry. Called from
    /// <see cref="Dispose(bool)"/> to release the backing <see cref="SettingsValueSubject{T}"/> resources.
    /// </summary>
    private void DisposeStreams()
    {
        foreach (var entry in _streams)
        {
            try
            {
                entry.Value.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already torn down by a concurrent disposal; nothing left to release.
            }
            catch (InvalidOperationException)
            {
                // A stream refused to close cleanly. Disposal is best-effort — one faulty
                // stream must not strand the rest.
            }
        }

        _streams.Clear();
    }
}
