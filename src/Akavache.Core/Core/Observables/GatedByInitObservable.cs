// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace Akavache.Core.Observables;

/// <summary>
/// Observable that gates subscription behind an <see cref="InitSignal"/>. When the signal
/// fires (init succeeded), the inner factory is subscribed; when the signal fails, the
/// error is forwarded to the observer.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="signal">The init signal to gate on.</param>
/// <param name="factory">Factory that produces the inner observable once init succeeds.</param>
internal sealed class GatedByInitObservable<T>(InitSignal signal, Func<IObservable<T>> factory)
    : IObservable<T>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        // Re-check the fast path: the signal may have fired between the Gate() call
        // and now, in which case we can subscribe inline and skip the parking list.
        if (signal.IsReady)
        {
            return SubscribeToInner(factory, observer);
        }

        if (signal.IsCompleted)
        {
            // Failed state — InitSignal.Gate<T> usually catches this and returns
            // Observable.Throw, but there is a narrow race window between the check
            // in Gate and the call here where the signal could have transitioned.
            // Close the race here.
            return SubscribeAfterPark(factory, observer, capturedError: null);
        }

        var inner = new SingleAssignmentDisposable();
        var parked = signal.TryPark(
            err =>
            {
                if (inner.IsDisposed)
                {
                    return;
                }

                if (err is not null)
                {
                    observer.OnError(err);
                    return;
                }

                try
                {
                    inner.Disposable = factory().Subscribe(observer);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            },
            out var error);

        return HandleParkResult(parked, inner, factory, observer, error);
    }

    /// <summary>
    /// Routes the result of <see cref="InitSignal.TryPark"/>: if parked, returns the
    /// inner disposable; otherwise dispatches inline via <see cref="SubscribeAfterPark"/>.
    /// </summary>
    /// <param name="parked">Whether TryPark succeeded.</param>
    /// <param name="inner">The disposable holding the parked subscription.</param>
    /// <param name="innerFactory">The factory that produces the inner observable.</param>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="error">The captured error from TryPark, or null.</param>
    /// <returns>The subscription disposable.</returns>
    internal static IDisposable HandleParkResult(
        bool parked,
        IDisposable inner,
        Func<IObservable<T>> innerFactory,
        IObserver<T> observer,
        Exception? error)
    {
        if (parked)
        {
            return inner;
        }

        return SubscribeAfterPark(innerFactory, observer, error);
    }

    /// <summary>Subscribes directly to the inner factory observable.</summary>
    /// <param name="innerFactory">The factory that produces the inner observable.</param>
    /// <param name="observer">The outer observer.</param>
    /// <returns>The inner subscription.</returns>
    internal static IDisposable SubscribeToInner(Func<IObservable<T>> innerFactory, IObserver<T> observer)
    {
        try
        {
            return innerFactory().Subscribe(observer);
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
            return Disposable.Empty;
        }
    }

    /// <summary>
    /// Delivers a terminal error to the observer and returns an empty disposable.
    /// </summary>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="capturedError">The error from the failed signal.</param>
    /// <returns>An empty disposable.</returns>
    internal static IDisposable DeliverError(IObserver<T> observer, Exception capturedError)
    {
        observer.OnError(capturedError);
        return Disposable.Empty;
    }

    /// <summary>
    /// Inline dispatch used when <see cref="InitSignal.TryPark"/> reports the signal
    /// has already terminated. Routes either the captured error or a fresh inner
    /// subscription to <paramref name="observer"/>.
    /// </summary>
    /// <param name="innerFactory">The factory that produces the inner observable.</param>
    /// <param name="observer">The outer observer.</param>
    /// <param name="capturedError">The captured terminal error, or <see langword="null"/> if the signal completed successfully.</param>
    /// <returns>The forwarded subscription.</returns>
    internal static IDisposable SubscribeAfterPark(Func<IObservable<T>> innerFactory, IObserver<T> observer, Exception? capturedError)
    {
        if (capturedError is null)
        {
            return SubscribeToInner(innerFactory, observer);
        }

        return DeliverError(observer, capturedError);
    }
}
