// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Akavache.Sqlite3.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Splat;

namespace Akavache.Sqlite3
{
    /// <summary>
    /// This class represents an IBlobCache backed by a SQLite3 database, and
    /// it is the default (and best!) implementation.
    /// </summary>
    public class SqlRawPersistentBlobCache : IObjectBlobCache, IEnableLogger, IDisposable
    {
        private static readonly object DisposeGate = 42;
        private readonly IObservable<Unit> _initializer;
        private readonly AsyncSubject<Unit> _shutdown = new AsyncSubject<Unit>();
        private SqliteOperationQueue _opQueue;
        private IDisposable _queueThread;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlRawPersistentBlobCache"/> class.
        /// </summary>
        /// <param name="databaseFile">The location of the database file.</param>
        /// <param name="scheduler">The scheduler to perform operations on.</param>
        public SqlRawPersistentBlobCache(string databaseFile, IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? BlobCache.TaskpoolScheduler;

            BlobCache.EnsureInitialized();

            Connection = new SQLiteConnection(databaseFile, storeDateTimeAsTicks: true);
            _initializer = Initialize();
        }

        /// <summary>
        /// Gets the scheduler used for the operations.
        /// </summary>
        public IScheduler Scheduler { get; }

        /// <summary>
        /// Gets the connection to the sqlite file.
        /// </summary>
        public SQLiteConnection Connection { get; }

        /// <summary>
        /// Gets a observable that signals when the blob cache shuts down.
        /// </summary>
        public IObservable<Unit> Shutdown => _shutdown;

        /// <summary>
        /// Inserts a item into the database.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="data">The data to insert.</param>
        /// <param name="absoluteExpiration">A optional expiration date.</param>
        /// <returns>An observable that signals when the insert is completed.</returns>
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            if (key == null)
            {
                return Observable.Throw<Unit>(new ArgumentNullException(nameof(key)));
            }

            if (data == null)
            {
                return Observable.Throw<Unit>(new ArgumentNullException(nameof(data)));
            }

            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = Scheduler.Now.UtcDateTime;

            return _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .SelectMany(encData => _opQueue.Insert(new[]
                {
                    new CacheElement
                    {
                        Key = key,
                        Value = encData,
                        CreatedAt = createdAt,
                        Expiration = exp,
                    },
                }))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets the value associated with the key.
        /// </summary>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <returns>An observable that triggers with the value.</returns>
        public IObservable<byte[]> Get(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");
            }

            if (key == null)
            {
                return Observable.Throw<byte[]>(new ArgumentNullException(nameof(key)));
            }

            return _initializer.SelectMany(_ => _opQueue.Select(new[] { key }))
                .SelectMany(x =>
                {
                    var cacheElements = x.ToList();
                    return cacheElements.Count == 1
                            ? Observable.Return(cacheElements.First().Value)
                            : ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
                })
                .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets all the keys stored in the cache.
        /// </summary>
        /// <returns>An observable that signals with all the keys.</returns>
        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<List<string>>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.GetAllKeys())
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets the created at date and time for a key.
        /// </summary>
        /// <param name="key">The key to get the date and time for.</param>
        /// <returns>The created date and time.</returns>
        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>("SqlitePersistentBlobCache");
            }

            if (key == null)
            {
                return Observable.Throw<DateTimeOffset?>(new ArgumentNullException(nameof(key)));
            }

            return _initializer.SelectMany(_ => _opQueue.Select(new[] { key }))
                .Select(x =>
                {
                    var cacheElements = x.ToList();
                    return cacheElements.Count() == 1
                            ? (DateTimeOffset?)new DateTimeOffset(cacheElements.First().CreatedAt, TimeSpan.Zero)
                            : default(DateTimeOffset?);
                })
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets the date time a object was created at.
        /// </summary>
        /// <param name="key">The key for the value.</param>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <returns>An observable that signals with the created date time.</returns>
        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return GetCreatedAt(key);
        }

        /// <summary>
        /// Flushes the cache.
        /// </summary>
        /// <returns>An observable that signals when the flush is completed.</returns>
        public IObservable<Unit> Flush()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.Flush())
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Invalidates the entry at the specified key.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        /// <returns>An observable that signals when the invalidation is finished.</returns>
        public IObservable<Unit> Invalidate(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.Invalidate(new[] { key }))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Invalidates all keys contained within the cache.
        /// </summary>
        /// <returns>An observable that signals when the invalidation is finished.</returns>
        public IObservable<Unit> InvalidateAll()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.InvalidateAll())
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Inserts a object into the cache.
        /// </summary>
        /// <param name="key">The key for the entry.</param>
        /// <param name="value">The value for the entry.</param>
        /// <param name="absoluteExpiration">A optional expiration date time.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>An observable which signals when the insert is complete.</returns>
        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            if (key == null)
            {
                return Observable.Throw<Unit>(new ArgumentNullException(nameof(key)));
            }

            var data = SerializeObject(value);
            var exp = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;
            var createdAt = Scheduler.Now.UtcDateTime;

            return _initializer
                .SelectMany(_ => BeforeWriteToDiskFilter(data, Scheduler))
                .SelectMany(encData => _opQueue.Insert(new[]
                {
                    new CacheElement
                    {
                        Key = key,
                        TypeName = typeof(T).FullName,
                        Value = encData,
                        CreatedAt = createdAt,
                        Expiration = exp,
                    },
                }))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets the value of the entry specified by the key.
        /// </summary>
        /// <param name="key">The key for the value we want the entry for.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>An observable which signals with the value.</returns>
        public IObservable<T> GetObject<T>(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<T>("SqlitePersistentBlobCache");
            }

            if (key == null)
            {
                return Observable.Throw<T>(new ArgumentNullException(nameof(key)));
            }

            return _initializer.SelectMany(_ => _opQueue.Select(new[] { key }))
                .SelectMany(x =>
                {
                    var cacheElements = x.ToList();
                    return cacheElements.Count == 1
                            ? Observable.Return(cacheElements.First().Value)
                            : ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
                })
                .SelectMany(x => AfterReadFromDiskFilter(x, Scheduler))
                .SelectMany(DeserializeObject<T>)
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets all the items values contained within the cache of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <returns>An observable that triggers with the values.</returns>
        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.SelectTypes(new[] { typeof(T).FullName })
                    .SelectMany(x => x.ToObservable()
                        .SelectMany(y => AfterReadFromDiskFilter(y.Value, Scheduler))
                        .SelectMany(DeserializeObject<T>)
                        .ToList()))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Invalidates the entry at the specified key.
        /// </summary>
        /// <param name="key">The key to invalidate.</param>
        /// <typeparam name="T">The type of object value.</typeparam>
        /// <returns>An observable that signals when the invalidation is complete.</returns>
        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return Invalidate(key);
        }

        /// <summary>
        /// Invalidates all objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to invalidate.</typeparam>
        /// <returns>An observable that signals when the invalidation is complete.</returns>
        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.InvalidateTypes(new[] { typeof(T).FullName }))
                .PublishLast().PermaRef();
        }

        /// <summary>
        /// Cleans up any items that have been invalidated due to date time expiration and
        /// other cache expiration reasons.
        /// </summary>
        /// <returns>An observable that signals when the vacuum is complete.</returns>
        public IObservable<Unit> Vacuum()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("SqlitePersistentBlobCache");
            }

            return _initializer.SelectMany(_ => _opQueue.Vacuum())
                .PublishLast().PermaRef();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void ReplaceOperationQueue(SqliteOperationQueue queue)
        {
            _initializer.Wait();

            _opQueue.Dispose();

            _opQueue = queue;
            _opQueue.Start();
        }

        /// <summary>
        /// Disposes of any managed objects that are IDisposable.
        /// </summary>
        /// <param name="isDisposing">If the method is being called by the Dispose() method.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
            {
                return;
            }

            if (isDisposing)
            {
                var disp = Interlocked.Exchange(ref _queueThread, null);
                if (disp == null)
                {
                    return;
                }

                var cleanup = Observable.Start(
                    () =>
                    {
                        // NB: While we intentionally dispose the operation queue 
                        // from a background thread so that we don't park the UI
                        // while we're waiting for background operations to 
                        // complete, we must serialize calls to sqlite3_close or
                        // else SQLite3 will start throwing back "busy" at us.
                        //
                        // We intentionally serialize even the shutdown of the
                        // background queue to be extra paranoid about not getting
                        // 'busy' while cleaning up.
                        lock (DisposeGate)
                        {
                            disp.Dispose();
                            _opQueue.Dispose();
                            Connection.Dispose();
                        }
                    }, Scheduler);

                cleanup.Multicast(_shutdown).PermaRef();
            }

            _disposed = true;
        }

        /// <summary>
        /// Initializes the cache.
        /// </summary>
        /// <returns>An observable that signals when the initialization is complete.</returns>
        protected IObservable<Unit> Initialize()
        {
            var ret = Observable.Create<Unit>(subj =>
            {
                // NB: This is in its own try block because depending on the 
                // platform, we may not have a modern SQLite3, where these
                // PRAGMAs are supported. These aren't critical, so let them
                // fail silently
                try 
                {
                    // NB: Setting journal_mode returns a row, nfi
                    Connection.ExecuteScalar<int>("PRAGMA journal_mode=WAL");
                    Connection.Execute("PRAGMA temp_store=MEMORY");
                    Connection.Execute("PRAGMA synchronous=OFF");
                }
                catch (SQLiteException) 
                {
                }

                try
                {
                    Connection.CreateTable<CacheElement>();

                    var schemaVersion = GetSchemaVersion();

                    if (schemaVersion < 2)
                    {
                        Connection.Execute("ALTER TABLE CacheElement RENAME TO VersionOneCacheElement;");
                        Connection.CreateTable<CacheElement>();

                        var sql = "INSERT INTO CacheElement SELECT Key,TypeName,Value,Expiration,\"{0}\" AS CreatedAt FROM VersionOneCacheElement;";
                        Connection.Execute(string.Format(sql, BlobCache.TaskpoolScheduler.Now.UtcDateTime.Ticks));
                        Connection.Execute("DROP TABLE VersionOneCacheElement;");

                        Connection.Insert(new SchemaInfo { Version = 2, });
                    }

                    // NB: We have to do this here because you can't prepare
                    // statements until you've got the backing table
                    _opQueue = new SqliteOperationQueue(Connection, Scheduler);
                    _queueThread = _opQueue.Start();

                    subj.OnNext(Unit.Default);
                    subj.OnCompleted();
                }
                catch (Exception ex)
                {
                    subj.OnError(ex);
                }

                return Disposable.Empty;
            });

            return ret.PublishLast().PermaRef();
        }

        /// <summary>
        /// Gets the current version of schema being used.
        /// </summary>
        /// <returns>The version of the schema.</returns>
        protected int GetSchemaVersion()
        {
            bool shouldCreateSchemaTable = false;
            int versionNumber = 0;

            try
            {
                versionNumber = Connection.ExecuteScalar<int>("SELECT Version from SchemaInfo ORDER BY Version DESC LIMIT 1");
            }
            catch (Exception)
            {
                shouldCreateSchemaTable = true;
            }

            if (shouldCreateSchemaTable)
            {
                Connection.CreateTable<SchemaInfo>();
                versionNumber = 1;
            }

            return versionNumber;
        }

        /// <summary>
        /// This method is called immediately before writing any data to disk.
        /// Override this in encrypting data stores in order to encrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data about to be written to disk.</param>
        /// <param name="scheduler">The scheduler to use if an operation has
        /// to be deferred. If the operation can be done immediately, use
        /// Observable.Return and ignore this parameter.</param>
        /// <returns>A Future result representing the encrypted data.</returns>
        protected virtual IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");
            }

            return Observable.Return(data, scheduler);
        }

        /// <summary>
        /// This method is called immediately after reading any data to disk.
        /// Override this in encrypting data stores in order to decrypt the
        /// data.
        /// </summary>
        /// <param name="data">The byte data that has just been read from
        /// disk.</param>
        /// <param name="scheduler">The scheduler to use if an operation has
        /// to be deferred. If the operation can be done immediately, use
        /// Observable.Return and ignore this parameter.</param>
        /// <returns>A Future result representing the decrypted data.</returns>
        protected virtual IObservable<byte[]> AfterReadFromDiskFilter(byte[] data, IScheduler scheduler)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");
            }

            return Observable.Return(data, scheduler);
        }

        private byte[] SerializeObject<T>(T value)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            settings.ContractResolver = new JsonDateTimeContractResolver(settings.ContractResolver); // This will make us use ticks instead of json ticks for DateTime.
            var ms = new MemoryStream();
            var serializer = JsonSerializer.Create(settings);
            var writer = new BsonDataWriter(ms);

            serializer.Serialize(writer, new ObjectWrapper<T> { Value = value });
            return ms.ToArray();
        }

        private IObservable<T> DeserializeObject<T>(byte[] data)
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            settings.ContractResolver = new JsonDateTimeContractResolver(settings.ContractResolver); // This will make us use ticks instead of json ticks for DateTime.
            var serializer = JsonSerializer.Create(settings);
            var reader = new BsonDataReader(new MemoryStream(data));
            var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;

            if (forcedDateTimeKind.HasValue)
            {
                reader.DateTimeKindHandling = forcedDateTimeKind.Value;
            }

            try
            {
                try
                {
                    var boxedVal = serializer.Deserialize<ObjectWrapper<T>>(reader).Value;
                    return Observable.Return(boxedVal);
                }
                catch (Exception ex)
                {
                    this.Log().WarnException("Failed to deserialize data as boxed, we may be migrating from an old Akavache", ex);
                }

                var rawVal = serializer.Deserialize<T>(reader);
                return Observable.Return(rawVal);
            }
            catch (Exception ex)
            {
                return Observable.Throw<T>(ex);
            }
        }
    }
}