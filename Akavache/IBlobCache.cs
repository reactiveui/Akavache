using System;
using System.Collections.Generic;

namespace Akavache
{
    public interface IBlobCache : IDisposable
    {
        void Insert(string key, byte[] data);
        IObservable<byte[]> GetAsync(string key);
        IEnumerable<string> GetAllKeys();
        void Invalidate(string key);
        void InvalidateAll();
    }
}