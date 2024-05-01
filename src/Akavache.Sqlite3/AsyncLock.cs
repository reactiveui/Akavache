// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3.Internal;

/// <summary>
/// A lock that allows for async based operations and returns a IDisposable which allows for unlocking.
/// </summary>
/// <remarks>Straight-up thieved from
/// http://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx
/// and all credit to that article.</remarks>
public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Task<IDisposable?> _releaser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLock"/> class.
    /// </summary>
    public AsyncLock() => _releaser = Task.FromResult((IDisposable?)new Releaser(this));

    /// <summary>
    /// Performs a lock which will be either released when the cancellation token is cancelled,
    /// or the returned disposable is disposed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token which allows for release of the lock.</param>
    /// <returns>A disposable which when Disposed will release the lock.</returns>
#if NETSTANDARD2_0 || XAMARINIOS || XAMARINMAC || XAMARINTVOS || MONOANDROID13_0 || TIZEN
    public Task<IDisposable?> LockAsync(CancellationToken cancellationToken = default)
#else
    public Task<IDisposable?> LockAsync(in CancellationToken cancellationToken = default)
#endif
    {
        var wait = _semaphore.WaitAsync(cancellationToken);

        // Happy path. We synchronously acquired the lock.
        return wait.IsCompleted && !wait.IsFaulted && !wait.IsCanceled
            ? _releaser
            : wait
            .ContinueWith(
                (task, state) => task.IsCanceled || state is null ? null : (IDisposable)state,
                _releaser.Result,
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore?.Dispose();
        _releaser?.Dispose();
    }

    private sealed class Releaser : IDisposable
    {
        private readonly AsyncLock _toRelease;

        internal Releaser(AsyncLock toRelease) => _toRelease = toRelease;

        public void Dispose() => _toRelease._semaphore.Release();
    }
}
