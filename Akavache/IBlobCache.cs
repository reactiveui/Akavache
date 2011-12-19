using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;

namespace Akavache
{
    public interface IFilesystemProvider
    {
        IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share);
        void CreateRecursive(string path);
        void Delete(string path);
    }

    public interface IBlobCache : IDisposable
    {
        void Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null);
        IObservable<byte[]> GetAsync(string key);
        IEnumerable<string> GetAllKeys();
        void Invalidate(string key);
        void InvalidateAll();

        IScheduler Scheduler { get; }
    }

    public interface ISecureBlobCache : IBlobCache
    {
    }
}