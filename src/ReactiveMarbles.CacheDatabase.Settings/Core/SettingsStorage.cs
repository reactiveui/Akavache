// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ReactiveMarbles.CacheDatabase.Core;
using ReactiveMarbles.CacheDatabase.NewtonsoftJson;

namespace ReactiveMarbles.CacheDatabase.Settings.Core
{
    /// <summary>
    /// Settings Storage.
    /// </summary>
    public abstract class SettingsStorage : ISettingsStorage
    {
        private readonly IBlobCache _blobCache;
        private readonly Dictionary<string, object?> _cache;
        private readonly ReaderWriterLockSlim _cacheLock;
        private readonly string _keyPrefix;
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsStorage"/> class.
        /// </summary>
        /// <param name="keyPrefix">
        /// This value will be used as prefix for all settings keys. It should be reasonably unique,
        /// so that it doesn't collide with other keys in the same <see cref="IBlobCache"/>.
        /// </param>
        /// <param name="cache">
        /// An <see cref="IBlobCache"/> implementation where you want your settings to be stored.
        /// </param>
        protected SettingsStorage(string keyPrefix, IBlobCache cache)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException("Invalid key prefix", nameof(keyPrefix));
            }

            CoreRegistrations.Serializer = new NewtonsoftSerializer();
            _keyPrefix = keyPrefix;
            _blobCache = cache;

            _cache = new();
            _cacheLock = new();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Loads every setting in this storage into the internal cache, or, if the value doesn't
        /// exist in the storage, initializes it with its default value. You dont HAVE to call this
        /// method, but it's handy for applications with a high number of settings where you want to
        /// load all settings on startup at once into the internal cache and not one-by-one at each request.
        /// </summary>
        /// <returns>A Task.</returns>
        public Task InitializeAsync() =>
            Task.Run(() =>
                {
                    foreach (var property in GetType().GetRuntimeProperties())
                    {
                        property.GetValue(this);
                    }
                });

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous dispose operation.
        /// </returns>
        public ValueTask DisposeAsync()
        {
            var result = _blobCache.DisposeAsync();
            GC.SuppressFinalize(this);
            return result;
        }

        /// <summary>
        /// Gets the value for the specified key, or, if the value doesn't exist, saves the <paramref name="defaultValue" /> and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the value to get or create.</typeparam>
        /// <param name="defaultValue">The default value, if no value is saved yet.</param>
        /// <param name="key">The key of the setting. Automatically set through the <see cref="CallerMemberNameAttribute" />.</param>
        /// <returns>The Type.</returns>
        /// <exception cref="ArgumentNullException">A ArgumentNullException.</exception>
        protected T? GetOrCreate<T>(T defaultValue, [CallerMemberName] string? key = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _cacheLock.EnterReadLock();

            try
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    return (T?)value;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return _blobCache.GetOrCreateObject($"{_keyPrefix}:{key}", () => defaultValue)
                .Do(x => AddToInternalCache(key, x)).Wait();
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void OnPropertyChanged(string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Overwrites the existing value or creates a new settings entry. The value is serialized
        /// via the Json.Net serializer.
        /// </summary>
        /// <typeparam name="T">The type of the value to set or create.</typeparam>
        /// <param name="value">The value to be set or created.</param>
        /// <param name="key">The key of the setting. Automatically set through the <see cref="CallerMemberNameAttribute"/>.</param>
        protected void SetOrCreate<T>(T value, [CallerMemberName] string? key = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            AddToInternalCache(key, value);

            // Fire and forget, we retrieve the value from the in-memory cache from now on
            _blobCache.InsertObject($"{_keyPrefix}:{key}", value).Subscribe();

            OnPropertyChanged(key);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cacheLock.Dispose();
                    _blobCache.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        private void AddToInternalCache(string key, object? value)
        {
            _cacheLock.EnterWriteLock();

            _cache[key] = value;

            _cacheLock.ExitWriteLock();
        }
    }
}
