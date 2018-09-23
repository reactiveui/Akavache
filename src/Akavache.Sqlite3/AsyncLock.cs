using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akavache.Sqlite3.Internal
{
    // Straight-up thieved from http://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx 
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> m_releaser;

        public AsyncLock()
        {
            m_releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync(CancellationToken ct = default(CancellationToken))
        {
            var wait = m_semaphore.WaitAsync(ct);

            // Happy path. We synchronously acquired the lock.
            if (wait.IsCompleted && !wait.IsFaulted) {
                return m_releaser;
            }

            return wait
                .ContinueWith((_, state) => (IDisposable)state,
                    m_releaser.Result, ct,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
            public void Dispose() { m_toRelease.m_semaphore.Release(); }
        }
    }
}
