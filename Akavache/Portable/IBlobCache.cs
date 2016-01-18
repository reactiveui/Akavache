using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
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
        IObservable<byte[]> Get(string key);

        /// <summary>
        /// Return all keys in the cache. Note that this method is normally
        /// for diagnostic / testing purposes, and that it is not guaranteed
        /// to be accurate with respect to in-flight requests.
        /// </summary>
        /// <returns>A list of valid keys for the cache.</returns>
        IObservable<IEnumerable<string>> GetAllKeys();

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
        IObservable<Unit> Invalidate(string key);

        /// <summary>
        /// Invalidate all entries in the cache (i.e. clear it). Note that
        /// this method is blocking and incurs a significant performance
        /// penalty if used while the cache is being used on other threads. 
        /// </summary>
        /// <returns>A signal indicating when the invalidate is complete.</returns>
        IObservable<Unit> InvalidateAll();

        /// <summary>
        /// This method eagerly removes all expired keys from the blob cache, as
        /// well as does any cleanup operations that makes sense (Hint: on SQLite3
        /// it does a Vacuum)
        /// </summary>
        /// <returns>A signal indicating when the operation is complete.</returns>
        IObservable<Unit> Vacuum();

        /// <summary>
        /// This Observable fires after the Dispose completes successfully, 
        /// since there is no such thing as an AsyncDispose().
        /// </summary>
        IObservable<Unit> Shutdown { get; }

        /// <summary>
        /// The IScheduler used to defer operations. By default, this is
        /// BlobCache.TaskpoolScheduler.
        /// </summary>
        IScheduler Scheduler { get; }
    }

    public interface IBulkBlobCache : IBlobCache
    {
        /// <summary>
        /// Inserts several keys into the database at one time. If any individual
        /// insert fails, this operation should cancel the entire insert (i.e. it
        /// should *not* partially succeed)
        /// </summary>
        /// <param name="keyValuePairs">The keys and values to insert.</param>
        /// <param name="absoluteExpiration">An optional expiration date.
        /// After the specified date, the key-value pair should be removed.</param>
        /// <returns>A signal to indicate when the key has been inserted.</returns>
        IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

        /// <summary>
        /// Retrieve several values from the key-value cache. If any of 
        /// the keys are not in the cache, this method should return an 
        /// IObservable which OnError's with KeyNotFoundException.
        /// </summary>
        /// <param name="keys">The keys to return asynchronously.</param>
        /// <returns>A Future result representing the byte data for each key.</returns>
        IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys);

        /// <summary>
        /// Returns the time that the keys were added to the cache, or returns 
        /// null if the key isn't in the cache.
        /// </summary>
        /// <param name="key">The keys to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys);

        /// <summary>
        /// Remove several keys from the cache. If the key doesn't exist, this method
        /// should do nothing and return (*not* throw KeyNotFoundException).
        /// </summary>
        /// <param name="key">The key to remove from the cache.</param>
        IObservable<Unit> Invalidate(IEnumerable<string> keys);
    }

    /// <summary>
    /// This interface indicates that the underlying BlobCache implementation
    /// encrypts or otherwise secures its persisted content. 
    ///
    /// By implementing this interface, you must guarantee that the data
    /// saved to disk cannot be easily read by a third party.
    /// </summary>
    public interface ISecureBlobCache : IBlobCache { }

    /// <summary>
    /// This interface indicates that the underlying BlobCache implementation
    /// can handle objects. 
    /// </summary>
    public interface IObjectBlobCache : IBlobCache
    {
        /// <summary>
        /// Insert an object into the cache, via the JSON serializer.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null);

        /// <summary>
        /// Get an object from the cache and deserialize it via the JSON
        /// serializer.
        /// </summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        IObservable<T> GetObject<T>(string key);

        /// <summary>
        /// Return all objects of a specific Type in the cache.
        /// </summary>
        /// <returns>A Future result representing all objects in the cache
        /// with the specified Type.</returns>
        IObservable<IEnumerable<T>> GetAllObjects<T>();

        /// <summary>
        /// Returns the time that the object with the key was added to the cache, or returns 
        /// null if the key isn't in the cache.
        /// </summary>
        /// <param name="key">The key to return the date for.</param>
        /// <returns>The date the key was created on.</returns>
        IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key);

        /// <summary>
        /// Invalidates a single object from the cache. It is important that the Type
        /// Parameter for this method be correct, and you cannot use 
        /// IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        IObservable<Unit> InvalidateObject<T>(string key);

        /// <summary>
        /// Invalidates all objects of the specified type. To invalidate all
        /// objects regardless of type, use InvalidateAll.
        /// </summary>
        /// <returns>
        /// A Future result representing the completion of the invalidation.</returns>
        IObservable<Unit> InvalidateAllObjects<T>();
    }

    public interface IObjectBulkBlobCache : IObjectBlobCache, IBulkBlobCache
    {
        /// <summary>
        /// Insert several objects into the cache, via the JSON serializer. 
        /// Similarly to InsertAll, partial inserts should not happen.
        /// </summary>
        /// <param name="keyValuePairs">The data to insert into the cache</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the completion of the insert.</returns>
        IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null);

        /// <summary>
        /// Get several objects from the cache and deserialize it via the JSON
        /// serializer.
        /// </summary>
        /// <param name="keys">The key to look up in the cache.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys);

        /// <summary>
        /// Invalidates several objects from the cache. It is important that the Type
        /// Parameter for this method be correct, and you cannot use 
        /// IBlobCache.Invalidate to perform the same task.
        /// </summary>
        /// <param name="keys">The key to invalidate.</param>
        /// <returns>A Future result representing the completion of the invalidation.</returns>
        IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys);
    }
}
