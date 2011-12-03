using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace Akavache
{
    public interface IBlobCache : IDisposable
    {
        void Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null);
        IObservable<byte[]> GetAsync(string key);
        IEnumerable<string> GetAllKeys();
        void Invalidate(string key);
        void InvalidateAll();

        IScheduler Scheduler { get; }
    }
}