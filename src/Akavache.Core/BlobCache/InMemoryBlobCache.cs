// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Splat;

namespace Akavache
{
    /// <summary>
    /// This class is an IBlobCache backed by a simple in-memory Dictionary.
    /// Use it for testing / mocking purposes.
    /// </summary>
    public class InMemoryBlobCache : ISecureBlobCache, IObjectBlobCache, IEnableLogger
    {
        [SuppressMessage("Design", "CA2213: non-disposed field.", Justification = "Used for notification of dispose.")]
        private readonly AsyncSubject<Unit> _shutdown = new AsyncSubject<Unit>();
        private readonly IDisposable? _inner;
        private Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private bool _disposed;
        private DateTimeKind? _dateTimeKind;
        private JsonDateTimeContractResolver _jsonDateTimeContractResolver = new JsonDateTimeContractResolver(); // This will make us use ticks instead of json ticks for DateTime.

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        public InMemoryBlobCache()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="scheduler">The scheduler to use for Observable based operations.</param>
        public InMemoryBlobCache(IScheduler scheduler)
            : this(scheduler, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="initialContents">The initial contents of the cache.</param>
        public InMemoryBlobCache(IEnumerable<KeyValuePair<string, byte[]>> initialContents)
            : this(null, initialContents)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="scheduler">The scheduler to use for Observable based operations.</param>
        /// <param name="initialContents">The initial contents of the cache.</param>
        public InMemoryBlobCache(IScheduler? scheduler, IEnumerable<KeyValuePair<string, byte[]>>? initialContents)
        {
            Scheduler = scheduler ?? CurrentThreadScheduler.Instance;
            foreach (var item in initialContents ?? Enumerable.Empty<KeyValuePair<string, byte[]>>())
            {
                _cache[item.Key] = new CacheEntry(null, item.Value, Scheduler.Now, null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBlobCache"/> class.
        /// </summary>
        /// <param name="disposer">A action that is called to dispose contents.</param>
        /// <param name="scheduler">The scheduler to use for Observable based operations.</param>
        /// <param name="initialContents">The initial contents of the cache.</param>
        internal InMemoryBlobCache(
            Action disposer,
            IScheduler? scheduler,
            IEnumerable<KeyValuePair<string, byte[]>> initialContents)
            : this(scheduler, initialContents)
        {
            _inner = Disposable.Create(disposer);
        }

        /// <inheritdoc />
        public DateTimeKind? ForcedDateTimeKind
        {
            get => _dateTimeKind ?? BlobCache.ForcedDateTimeKind;

            set
            {
                _dateTimeKind = value;

                if (_jsonDateTimeContractResolver is not null)
                {
                    _jsonDateTimeContractResolver.ForceDateTimeKindOverride = value;
                }
            }
        }

        /// <inheritdoc />
        public IScheduler Scheduler { get; protected set; }

        /// <inheritdoc />
        public IObservable<Unit> Shutdown => _shutdown;

        /// <summary>
        /// Overrides the global registrations with specified values.
        /// </summary>
        /// <param name="scheduler">The default scheduler to use.</param>
        /// <param name="initialContents">The default inner contents to use.</param>
        /// <returns>A generated cache.</returns>
        public static InMemoryBlobCache OverrideGlobals(IScheduler? scheduler = null, params KeyValuePair<string, byte[]>[] initialContents)
        {
            var local = BlobCache.LocalMachine;
            var user = BlobCache.UserAccount;
            var sec = BlobCache.Secure;

            var resetBlobCache = new Action(() =>
            {
                BlobCache.LocalMachine = local;
                BlobCache.Secure = sec;
                BlobCache.UserAccount = user;
            });

            var testCache = new InMemoryBlobCache(resetBlobCache, scheduler, initialContents);
            BlobCache.LocalMachine = testCache;
            BlobCache.Secure = testCache;
            BlobCache.UserAccount = testCache;

            return testCache;
        }

        /// <summary>
        /// Overrides the global registrations with specified values.
        /// </summary>
        /// <param name="initialContents">The default inner contents to use.</param>
        /// <param name="scheduler">The default scheduler to use.</param>
        /// <returns>A generated cache.</returns>
        public static InMemoryBlobCache OverrideGlobals(IDictionary<string, byte[]> initialContents, IScheduler? scheduler = null)
        {
            return OverrideGlobals(scheduler, initialContents.ToArray());
        }

        /// <summary>
        /// Overrides the global registrations with specified values.
        /// </summary>
        /// <param name="initialContents">The default inner contents to use.</param>
        /// <param name="scheduler">The default scheduler to use.</param>
        /// <returns>A generated cache.</returns>
        public static InMemoryBlobCache OverrideGlobals(IDictionary<string, object> initialContents, IScheduler? scheduler = null)
        {
            var initialSerializedContents = initialContents
                .Select(item => new KeyValuePair<string, byte[]>(item.Key, JsonSerializationMixin.SerializeObject(item.Value)))
                .ToArray();

            return OverrideGlobals(scheduler, initialSerializedContents);
        }

        /// <inheritdoc />
        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                _cache[key] = new CacheEntry(null, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<Unit> Flush()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<byte[]> Get(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<byte[]>("InMemoryBlobCache");
            }

            CacheEntry? entry;
            lock (_cache)
            {
                if (!_cache.TryGetValue(key, out entry))
                {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
                }
            }

            if (entry is null)
            {
                return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
            }

            if (entry.ExpiresAt is not null && Scheduler.Now > entry.ExpiresAt.Value)
            {
                lock (_cache)
                {
                    _cache.Remove(key);
                }

                return ExceptionHelper.ObservableThrowKeyNotFoundException<byte[]>(key);
            }

            return Observable.Return(entry.Value, Scheduler);
        }

        /// <inheritdoc />
        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<DateTimeOffset?>("InMemoryBlobCache");
            }

            CacheEntry? entry;
            lock (_cache)
            {
                if (!_cache.TryGetValue(key, out entry))
                {
                    return Observable.Return<DateTimeOffset?>(null);
                }
            }

            if (entry is null)
            {
                return ExceptionHelper.ObservableThrowKeyNotFoundException<DateTimeOffset?>(key);
            }

            return Observable.Return<DateTimeOffset?>(entry.CreatedAt, Scheduler);
        }

        /// <inheritdoc />
        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<List<string>>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                return Observable.Return(_cache
                    .Where(x => x.Value.ExpiresAt is null || x.Value.ExpiresAt >= Scheduler.Now)
                    .Select(x => x.Key)
                    .ToList());
            }
        }

        /// <inheritdoc />
        public IObservable<Unit> Invalidate(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                _cache.Remove(key);
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<Unit> InvalidateAll()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                _cache.Clear();
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            var data = SerializeObject(value);

            lock (_cache)
            {
                _cache[key] = new CacheEntry(typeof(T).FullName, data, Scheduler.Now, absoluteExpiration);
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<T> GetObject<T>(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<T>("InMemoryBlobCache");
            }

            CacheEntry? entry;
            lock (_cache)
            {
                if (!_cache.TryGetValue(key, out entry))
                {
                    return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
                }
            }

            if (entry is null)
            {
                return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
            }

            if (entry.ExpiresAt is not null && Scheduler.Now > entry.ExpiresAt.Value)
            {
                lock (_cache)
                {
                    _cache.Remove(key);
                }

                return ExceptionHelper.ObservableThrowKeyNotFoundException<T>(key);
            }

            T obj = DeserializeObject<T>(entry.Value);

            return Observable.Return(obj, Scheduler);
        }

        /// <inheritdoc />
        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return GetCreatedAt(key);
        }

        /// <inheritdoc />
        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<IEnumerable<T>>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                return Observable.Return(
                    _cache
                        .Where(x => x.Value.TypeName == typeof(T).FullName && (x.Value.ExpiresAt is null || x.Value.ExpiresAt >= Scheduler.Now))
                        .Select(x => DeserializeObject<T>(x.Value.Value))
                        .ToList(),
                    Scheduler);
            }
        }

        /// <inheritdoc />
        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            return Invalidate(key);
        }

        /// <inheritdoc />
        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                var toDelete = _cache.Where(x => x.Value.TypeName == typeof(T).FullName).ToArray();
                foreach (var obj in toDelete)
                {
                    _cache.Remove(obj.Key);
                }
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public IObservable<Unit> Vacuum()
        {
            if (_disposed)
            {
                return ExceptionHelper.ObservableThrowObjectDisposedException<Unit>("InMemoryBlobCache");
            }

            lock (_cache)
            {
                var toDelete = _cache.Where(x => x.Value.ExpiresAt is not null && Scheduler.Now > x.Value.ExpiresAt).ToArray();
                foreach (var kvp in toDelete)
                {
                    _cache.Remove(kvp.Key);
                }
            }

            return Observable.Return(Unit.Default);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the managed memory inside the class.
        /// </summary>
        /// <param name="isDisposing">If this is being called by the Dispose method.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
            {
                return;
            }

            if (isDisposing)
            {
                Scheduler = CurrentThreadScheduler.Instance;
                lock (_cache)
                {
                    _cache.Clear();
                }

                _inner?.Dispose();

                _shutdown.OnNext(Unit.Default);
                _shutdown.OnCompleted();
            }

            _disposed = true;
        }

        private byte[] SerializeObject<T>(T value)
        {
            var serializer = GetSerializer();
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonDataWriter(ms))
                {
                    serializer.Serialize(writer, new ObjectWrapper<T> { Value = value });
                    return ms.ToArray();
                }
            }
        }

        private T DeserializeObject<T>(byte[] data)
        {
#pragma warning disable CS8603 // Possible null reference return.
            var serializer = GetSerializer();
            using (var reader = new BsonDataReader(new MemoryStream(data)))
            {
                var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;

                if (forcedDateTimeKind.HasValue)
                {
                    reader.DateTimeKindHandling = forcedDateTimeKind.Value;
                }

                try
                {
                    var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);

                    if (wrapper is null)
                    {
                        return default;
                    }

                    return wrapper.Value;
                }
                catch (Exception ex)
                {
                    this.Log().Warn(ex, "Failed to deserialize data as boxed, we may be migrating from an old Akavache");
                }

                return serializer.Deserialize<T>(reader);
#pragma warning restore CS8603 // Possible null reference return.
            }
        }

        private JsonSerializer GetSerializer()
        {
            var settings = Locator.Current.GetService<JsonSerializerSettings>() ?? new JsonSerializerSettings();
            JsonSerializer serializer;

            lock (settings)
            {
                _jsonDateTimeContractResolver.ExistingContractResolver = settings.ContractResolver;
                settings.ContractResolver = _jsonDateTimeContractResolver;
                serializer = JsonSerializer.Create(settings);
                settings.ContractResolver = _jsonDateTimeContractResolver.ExistingContractResolver;
            }

            return serializer;
        }
    }
}
