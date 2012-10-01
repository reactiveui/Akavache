using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
    /// <summary>
    /// An abstraction for the simple file operations that an IBlobCache can
    /// perform. Create a new instance of this when adapting IBlobCache to
    /// different platforms or backing stores, or for testing purposes.
    /// </summary>
    public interface IFilesystemProvider
    {
        /// <summary>
        /// Open a file on a background thread, with the File object in 'async
        /// mode'. It is critical that this operation is deferred and returns
        /// immediately (i.e. wrapped in an Observable.Start).
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <param name="mode">The file mode</param>
        /// <param name="access">The required access privileges</param>
        /// <param name="share">The allowed file sharing modes.</param>
        /// <param name="scheduler">The scheduler to schedule the open under.</param>
        /// <returns>A Future result representing the Open file.</returns>
        IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler);

        /// <summary>
        /// Create a directory and its parents. If the directory already
        /// exists, this method does nothing (i.e. it does not throw if a
        /// directory exists)
        /// </summary>
        /// <param name="path">The path to create.</param>
        void CreateRecursive(string path);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="path">The path to the file</param>
        void Delete(string path);
    }

    /// <summary>
    /// IBlobCache is the core interface on which Akavache is built, it is an
    /// interface describing an asynchronous persistent key-value store. 
    /// </summary>
    public interface IBlobCache : IDisposable
    {
        /// <summary>
        /// Insert a blob into the cache with the specified key and expiration
        /// date.
        /// </summary>
        /// <param name="key">The key to use for the data.</param>
        /// <param name="data">The data to save in the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.
        /// After the specified date, the key-value pair should be removed.</param>
        /// <returns>A signal to indicate when the key has been inserted.</returns>
        IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null);

        /// <summary>
        /// Retrieve a value from the key-value cache. If the key is not in
        /// the cache, this method should return an IObservable which
        /// OnError's with KeyNotFoundException.
        /// </summary>
        /// <param name="key">The key to return asynchronously.</param>
        /// <returns>A Future result representing the byte data.</returns>
        IObservable<byte[]> GetAsync(string key);

        /// <summary>
        /// Return all keys in the cache. Note that this method is normally
        /// for diagnostic / testing purposes, and that it is not guaranteed
        /// to be accurate with respect to in-flight requests.
        /// </summary>
        /// <returns>A list of valid keys for the cache.</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Returns the time that the key was added to the cache, or returns 
        /// null if the key isn't in the cache.
        /// </summary>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        IObservable<DateTimeOffset?> GetCreatedAt(string key);

        /// <summary>
        /// This method guarantees that all in-flight inserts have completed
        /// and any indexes have been written to disk.
        /// </summary>
        /// <returns>A signal indicating when the flush is complete.</returns>
        IObservable<Unit> Flush();

        /// <summary>
        /// Remove a key from the cache. If the key doesn't exist, this method
        /// should do nothing and return (*not* throw KeyNotFoundException).
        /// </summary>
        /// <param name="key">The key to remove from the cache.</param>
        void Invalidate(string key);

        /// <summary>
        /// Invalidate all entries in the cache (i.e. clear it). Note that
        /// this method is blocking and incurs a significant performance
        /// penalty if used while the cache is being used on other threads. 
        /// </summary>
        void InvalidateAll();

        /// <summary>
        /// The IScheduler used to defer operations. By default, this is
        /// RxApp.TaskPoolScheduler.
        /// </summary>
        IScheduler Scheduler { get; }

        /// <summary>
        /// Service provider used to create instances of objects retrieved 
        /// from the cache. This uses the value set in BlobCache.ServiceProvider.
        /// If none is set, it just uses the old behavior.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }

    /// <summary>
    /// This interface indicates that the underlying BlobCache implementation
    /// encrypts or otherwise secures its persisted content. 
    ///
    /// By implementing this interface, you must guarantee that the data
    /// saved to disk cannot be easily read by a third party.
    /// </summary>
    public interface ISecureBlobCache : IBlobCache { }
}
