// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using Akavache.Helpers;

namespace Akavache.Settings.Core;

/// <summary>
/// Lightweight sync-readable wrapper around a settings <see cref="SettingsStream{T}"/>.
/// Exposes a latest-value <see cref="Value"/> accessor (so derived <see cref="SettingsStorage"/>
/// classes can still publish plain-C# properties), an <see cref="IObservable{T}"/> surface
/// (so reactive consumers can subscribe), a <see cref="Set"/> method (for writes),
/// <see cref="INotifyPropertyChanged"/> notifications (so WPF/MAUI data bindings to
/// <c>BoolTest.Value</c> update automatically), and an implicit conversion to
/// <typeparamref name="T"/> (so comparisons and assignments read naturally without
/// <c>.Value</c>).
/// </summary>
/// <remarks>
/// <para>
/// Deliberately simpler than ReactiveUI's <c>ObservableAsPropertyHelper&lt;T&gt;</c> — no
/// scheduler plumbing, no thrown-exception observable, no deferred subscription mode.
/// Settings properties are long-lived, low-frequency, and single-subject; the extra
/// ceremony buys nothing here.
/// </para>
/// <para>
/// Typical derived usage:
/// <code>
/// public sealed class MySettings : SettingsBase
/// {
///     public MySettings() : base(nameof(MySettings))
///     {
///         Enabled = CreateProperty(true, nameof(Enabled));
///     }
///
///     public SettingsPropertyHelper&lt;bool&gt; Enabled { get; }
/// }
///
/// // Consumers
/// bool enabled = settings.Enabled;              // implicit conversion, sync
/// var current = settings.Enabled.Value;         // explicit sync read
/// if (settings.Enabled) { ... }                 // condition, implicit
/// settings.Enabled.Subscribe(v =&gt; Log(v));      // reactive
/// await settings.Enabled.Set(false);            // write + commit
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="T">The property value type.</typeparam>
public sealed class SettingsPropertyHelper<T> : IObservable<T>, INotifyPropertyChanged, IDisposable
{
    /// <summary>The backing stream that owns the persistent BehaviorSubject + blob cache integration.</summary>
    private readonly SettingsStream<T> _stream;

    /// <summary>Disposable returned by our internal <see cref="SettingsStream{T}.Subscribe"/> call — released when the helper disposes.</summary>
    private readonly IDisposable _subscription;

    /// <summary>Cached latest value so <see cref="Value"/> stays allocation-free. Writes go through <see cref="_stream"/>'s OnNext which re-notifies us via the internal subscription.</summary>
    private T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPropertyHelper{T}"/> class.
    /// </summary>
    /// <param name="stream">The backing stream to read/write through.</param>
    /// <param name="initialValue">The seed value used for <see cref="Value"/> before the stream emits its first notification.</param>
    internal SettingsPropertyHelper(SettingsStream<T> stream, T initialValue)
    {
        _stream = stream;
        _value = initialValue;

        // Subscribe to the stream's BehaviorSubject so Value tracks Set() updates.
        // This does NOT trigger a cold load — that's a separate opt-in via
        // SettingsStorage.InitializeAsync, so constructing a settings class (which
        // creates every property helper) stays free of disk IO.
        _subscription = _stream.Subscribe(OnStreamValue);
    }

    /// <summary>
    /// Occurs when <see cref="Value"/> changes. Fires with <c>"Value"</c> as the property
    /// name so WPF/MAUI data bindings to <c>MyProperty.Value</c> update automatically.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the latest value published by the underlying stream. Reads are synchronous
    /// and allocation-free; the value is kept up to date by an internal subscription.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Implicit conversion from the helper to the underlying value type. Lets callers
    /// write <c>bool enabled = settings.Enabled;</c>, <c>if (settings.Enabled) { ... }</c>,
    /// or <c>settings.IntValue == 5</c> without having to reach through <see cref="Value"/>.
    /// The conversion is equivalent to <see cref="Value"/> — same latest-cached value,
    /// same synchronous read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The conversion does not fire in every context — in particular, generic
    /// constraints like <c>Assert.That&lt;T&gt;(T)</c> pin <c>T</c> to
    /// <see cref="SettingsPropertyHelper{T}"/> itself, not the underlying type, so
    /// assertions need <c>.Value</c> or an explicit cast. Comparisons, conditions, and
    /// assignments into a typed variable all flow through naturally.
    /// </para>
    /// </remarks>
    /// <param name="helper">The helper to unwrap.</param>
    public static implicit operator T(SettingsPropertyHelper<T> helper)
    {
        ArgumentExceptionHelper.ThrowIfNull(helper);
        return helper._value;
    }

    /// <summary>
    /// Named alternate for the implicit conversion operator, required by CA2225 for
    /// languages that can't invoke user-defined implicit operators. Equivalent to
    /// <see cref="Value"/> — returns the current cached value synchronously.
    /// </summary>
    /// <returns>The current <see cref="Value"/>.</returns>
    public T ToT() => _value;

    /// <summary>
    /// Writes a new value through to the underlying blob cache. The returned observable
    /// fires <see cref="Unit"/> once the persistent write has been accepted, or errors
    /// if the cache insert fails. The live stream is updated synchronously so any
    /// subsequent read of <see cref="Value"/> sees the new value immediately, regardless
    /// of whether the caller awaits the commit.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <returns>A one-shot observable that fires <see cref="Unit"/> when the persistent write commits.</returns>
    public IObservable<Unit> Set(T value) => _stream.Set(value);

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer) => _stream.Subscribe(observer);

    /// <inheritdoc/>
    public void Dispose() => _subscription.Dispose();

    /// <summary>
    /// Internal observer callback invoked every time the backing stream emits a new
    /// value. Updates the cached <see cref="_value"/> and raises
    /// <see cref="PropertyChanged"/> for consumers that bind on <c>Value</c>.
    /// </summary>
    /// <param name="value">The new value from the stream.</param>
    private void OnStreamValue(T value)
    {
        _value = value;
        PropertyChanged?.Invoke(this, new(nameof(Value)));
    }
}
