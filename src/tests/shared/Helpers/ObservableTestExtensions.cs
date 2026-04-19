// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Test-only extension methods for subscribing to observables without
/// <c>ToTask()</c> or <c>FirstAsync()</c> bridges. Two families:
/// <list type="bullet">
///   <item><c>SubscribeGet*</c> — for caches using <c>ImmediateScheduler</c> where
///   Subscribe completes synchronously on the calling thread.</item>
///   <item><c>WaitFor*</c> — for real SQLite caches where the worker thread delivers
///   results asynchronously; blocks via <see cref="ManualResetEventSlim"/>.</item>
/// </list>
/// </summary>
internal static class ObservableTestExtensions
{
    /// <summary>Default timeout for <c>WaitFor*</c> methods.</summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    // ── Synchronous (ImmediateScheduler) ─────────────────────────────────

    /// <summary>
    /// Subscribes and returns the single value emitted by <paramref name="source"/>.
    /// Only safe when the observable completes synchronously (e.g. <c>ImmediateScheduler</c>).
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <returns>The emitted value.</returns>
    internal static T? SubscribeGetValue<T>(this IObservable<T> source)
    {
        T? result = default;
        source.Subscribe(v => result = v);
        return result;
    }

    /// <summary>
    /// Subscribes to a <see cref="Unit"/>-producing observable, discarding the value.
    /// Only safe when the observable completes synchronously.
    /// </summary>
    /// <param name="source">The observable to subscribe to.</param>
    internal static void SubscribeAndComplete(this IObservable<Unit> source) =>
        source.Subscribe();

    /// <summary>
    /// Subscribes and captures any error emitted by <paramref name="source"/>.
    /// Only safe when the observable completes synchronously.
    /// </summary>
    /// <param name="source">The observable to subscribe to.</param>
    /// <returns>The captured exception, or <see langword="null"/> if none.</returns>
    internal static Exception? SubscribeGetError(this IObservable<Unit> source)
    {
        Exception? error = null;
        source.Subscribe(_ => { }, ex => error = ex);
        return error;
    }

    /// <summary>
    /// Subscribes and captures any error emitted by <paramref name="source"/>.
    /// Only safe when the observable completes synchronously.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <returns>The captured exception, or <see langword="null"/> if none.</returns>
    internal static Exception? SubscribeGetError<T>(this IObservable<T> source)
    {
        Exception? error = null;
        source.Subscribe(_ => { }, ex => error = ex);
        return error;
    }

    // ── Blocking (worker-thread / real SQLite) ───────────────────────────

    /// <summary>
    /// Subscribes and blocks until <paramref name="source"/> emits a value or completes.
    /// Safe for observables that deliver on a background thread.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="timeout">Optional timeout override.</param>
    /// <returns>The emitted value.</returns>
    internal static T? WaitForValue<T>(this IObservable<T> source, TimeSpan? timeout = null)
    {
        T? result = default;
        using ManualResetEventSlim done = new();
        source.Subscribe(
            v => result = v,
            _ => done.Set(),
            done.Set);
        if (!done.Wait(timeout ?? DefaultTimeout))
        {
            throw new TimeoutException($"WaitForValue timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
        }

        return result;
    }

    /// <summary>
    /// Subscribes and blocks until <paramref name="source"/> completes.
    /// Safe for observables that deliver on a background thread.
    /// </summary>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="timeout">Optional timeout override.</param>
    internal static void WaitForCompletion(this IObservable<Unit> source, TimeSpan? timeout = null)
    {
        Exception? error = null;
        using ManualResetEventSlim done = new();
        source.Subscribe(
            _ => { },
            ex =>
            {
                error = ex;
                done.Set();
            },
            done.Set);
        if (!done.Wait(timeout ?? DefaultTimeout))
        {
            throw new TimeoutException($"WaitForCompletion timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
        }

        if (error is null)
        {
            return;
        }

        throw error;
    }

    /// <summary>
    /// Subscribes and blocks until <paramref name="source"/> completes, capturing any error.
    /// Safe for observables that deliver on a background thread.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="timeout">Optional timeout override.</param>
    /// <returns>The captured exception, or <see langword="null"/> if none.</returns>
    internal static Exception? WaitForError<T>(this IObservable<T> source, TimeSpan? timeout = null)
    {
        Exception? error = null;
        using ManualResetEventSlim done = new();
        source.Subscribe(
            _ => { },
            ex =>
            {
                error = ex;
                done.Set();
            },
            done.Set);
        if (!done.Wait(timeout ?? DefaultTimeout))
        {
            throw new TimeoutException($"WaitForError timed out after {(timeout ?? DefaultTimeout).TotalSeconds}s.");
        }

        return error;
    }
}
