// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

using ReactiveMarbles.CacheDatabase.Core;

using SQLite;

#if ENCRYPTED
namespace ReactiveMarbles.CacheDatabase.EncryptedSqlite3;
#else
namespace ReactiveMarbles.CacheDatabase.Sqlite3;
#endif

/// <summary>
/// The Sqlite blob cache.
/// </summary>
#if ENCRYPTED
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Reused file.")]
public class EncryptedSqliteBlobCache : IBlobCache
#else
public class SqliteBlobCache : IBlobCache
#endif
{
#if ENCRYPTED
    private const string ClassName = nameof(EncryptedSqliteBlobCache);
#else
    private const string ClassName = nameof(SqliteBlobCache);
#endif

    private readonly IObservable<Unit> _initialized;

    private bool _disposed;

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="password">The password.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="storeDateTimeAsTicks">
    /// Specifies whether to store DateTime properties as ticks (true) or strings (false).
    /// You absolutely do want to store them as Ticks in all new projects. The value
    /// of false is only here for backwards compatibility. There is a *significant* speed
    /// advantage, with no down sides, when setting storeDateTimeAsTicks = true. If you
    /// use DateTimeOffset properties, it will be always stored as ticks regardingless
    /// the storeDateTimeAsTicks parameter.
    /// </param>
    public EncryptedSqliteBlobCache(string fileName, string password, IScheduler? scheduler = null, bool storeDateTimeAsTicks = true)
        : this(
              new SQLiteConnectionString(fileName ?? throw new ArgumentNullException(nameof(fileName)), storeDateTimeAsTicks, key: password ?? throw new ArgumentNullException(nameof(password))),
              scheduler)
    {
    }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="storeDateTimeAsTicks">
    /// Specifies whether to store DateTime properties as ticks (true) or strings (false).
    /// You absolutely do want to store them as Ticks in all new projects. The value
    /// of false is only here for backwards compatibility. There is a *significant* speed
    /// advantage, with no down sides, when setting storeDateTimeAsTicks = true. If you
    /// use DateTimeOffset properties, it will be always stored as ticks regardingless
    /// the storeDateTimeAsTicks parameter.
    /// </param>
    public SqliteBlobCache(string fileName, IScheduler? scheduler = null, bool storeDateTimeAsTicks = true)
        : this(
              new SQLiteConnectionString(fileName ?? throw new ArgumentNullException(nameof(fileName)), storeDateTimeAsTicks),
              scheduler)
    {
    }
#endif

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="scheduler">The scheduler.</param>
    public EncryptedSqliteBlobCache(SQLiteConnectionString connectionString, IScheduler? scheduler = null)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="scheduler">The scheduler.</param>
    public SqliteBlobCache(SQLiteConnectionString connectionString, IScheduler? scheduler = null)
#endif
    {
#if NETSTANDARD
        if (connectionString is null)
        {
            throw new ArgumentNullException(nameof(connectionString));
        }
#else
        ArgumentNullException.ThrowIfNull(connectionString);
#endif

        Connection = new SQLiteAsyncConnection(connectionString);
        Scheduler = scheduler ?? CoreRegistrations.TaskpoolScheduler;
        _initialized = Initialize();
    }

    /// <summary>
    /// Gets the connection.
    /// </summary>
    public SQLiteAsyncConnection Connection { get; }

    /// <inheritdoc/>
    public IScheduler Scheduler { get; }

    /// <inheritdoc/>
    public IObservable<Unit> Flush()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        // no-op on sql.
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Flush(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        // no-op on sql.
        return Observable.Return(Unit.Default);
    }

    /// <inheritdoc/>
    public IObservable<byte[]?> Get(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<byte[]>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<byte[]>(new ArgumentNullException(nameof(key)));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().FirstAsync(x => x.Id != null && x.ExpiresAt > time && x.Id == key))
            .Catch<CacheEntry, InvalidOperationException>(ex => Observable.Throw<CacheEntry>(new KeyNotFoundException(ex.Message)))
            .Where(x => x?.Value is not null)
            .Select(x => x.Value!);
    }

    /// <inheritdoc/>
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;

        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && keys.Contains(x.Id)).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Value is not null && x?.Id is not null)
            .Select(x => new KeyValuePair<string, byte[]>(x.Id!, x.Value!));
    }

    /// <inheritdoc/>
    public IObservable<byte[]> Get(string key, Type type)
    {
        if (key is null)
        {
            return Observable.Throw<byte[]>(new ArgumentNullException(nameof(key)));
        }

        if (type is null)
        {
            return Observable.Throw<byte[]>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<byte[]>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().FirstAsync(x => x.Id != null && x.ExpiresAt > time && x.Id == key && x.TypeName == type.FullName))
            .Catch<CacheEntry, InvalidOperationException>(ex => Observable.Throw<CacheEntry>(new KeyNotFoundException(ex.Message)))
            .Where(x => x?.Value is not null)
            .Select(x => x.Value!);
    }

    /// <inheritdoc/>
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys, Type type)
    {
        if (keys is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(keys)));
        }

        if (type is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && keys.Contains(x.Id) && x.TypeName == type.FullName).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Value is not null && x?.Id is not null)
            .Select(x => new KeyValuePair<string, byte[]>(x.Id!, x.Value!));
    }

    /// <inheritdoc/>
    public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type)
    {
        if (type is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && x.TypeName == type.FullName).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Value is not null && x?.Id is not null)
            .Select(x => new KeyValuePair<string, byte[]>(x.Id!, x.Value!));
    }

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<string>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => x.Id!);
    }

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys(Type type)
    {
        if (type is null)
        {
            return Observable.Throw<string>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<string>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && x.TypeName == type.Name).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => x.Id!);
    }

    /// <inheritdoc/>
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && keys.Contains(x.Id)).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => (Key: x.Id!, Time: x?.CreatedAt));
    }

    /// <inheritdoc/>
    public IObservable<DateTimeOffset?> GetCreatedAt(string key)
    {
        if (key is null)
        {
            return Observable.Throw<DateTimeOffset?>(new ArgumentNullException(nameof(key)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<DateTimeOffset?>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && x.Id == key).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => x?.CreatedAt);
    }

    /// <inheritdoc/>
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys, Type type)
    {
        if (type is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new ArgumentNullException(nameof(type)));
        }

        if (keys is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && keys.Contains(x.Id) && x.TypeName == type.FullName).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => (Key: x.Id!, Time: x?.CreatedAt));
    }

    /// <inheritdoc/>
    public IObservable<DateTimeOffset?> GetCreatedAt(string key, Type type)
    {
        if (type is null)
        {
            return Observable.Throw<DateTimeOffset?>(new ArgumentNullException(nameof(type)));
        }

        if (key is null)
        {
            return Observable.Throw<DateTimeOffset?>(new ArgumentNullException(nameof(key)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<DateTimeOffset?>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.Table<CacheEntry>().Where(x => x.Id != null && x.ExpiresAt > time && x.Id == key && x.TypeName == type.FullName).ToListAsync())
            .SelectMany(x => x)
            .Where(x => x?.Id is not null)
            .Select(x => x?.CreatedAt);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (keyValuePairs is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keyValuePairs)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var expiry = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;

        return _initialized.SelectMany(async (_, _, _) =>
            {
                var entries = keyValuePairs.Select(x => new CacheEntry { CreatedAt = DateTime.Now, Id = x.Key, Value = x.Value, ExpiresAt = expiry });

                await Connection.RunInTransactionAsync(sql =>
                {
                    foreach (var entry in entries)
                    {
                        sql.InsertOrReplace(entry);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            })
            .Select(_ => Unit.Default);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => Insert(new[] { new KeyValuePair<string, byte[]>(key, data) }, absoluteExpiration);

    /// <inheritdoc/>
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        if (keyValuePairs is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keyValuePairs)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        var expiry = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;

        return _initialized.SelectMany(async (_, _, _) =>
            {
                var entries = keyValuePairs.Select(x => new CacheEntry { CreatedAt = DateTime.Now, Id = x.Key, Value = x.Value, ExpiresAt = expiry, TypeName = type.FullName });

                await Connection.RunInTransactionAsync(sql =>
                {
                    foreach (var entry in entries)
                    {
                        sql.InsertOrReplace(entry);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            })
            .Select(_ => Unit.Default);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(string key, byte[] data, Type type, DateTimeOffset? absoluteExpiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        if (data is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(data)));
        }

        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        return Insert(new[] { new KeyValuePair<string, byte[]>(key, data) }, type, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        return Invalidate(new[] { key });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(string key, Type type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        return Invalidate(new[] { key }, type);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        return _initialized.SelectMany(
            async (_) =>
            {
                await Connection.RunInTransactionAsync(sql =>
                {
                    foreach (var key in keys)
                    {
                        sql.Delete<CacheEntry>(key);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(IEnumerable<string> keys, Type type)
    {
        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        return _initialized.SelectMany(
            async (_) =>
            {
                await Connection.RunInTransactionAsync(sql =>
                {
                    var entries = sql.Table<CacheEntry>().Where(x => keys.Contains(x.Id) && x.TypeName == type.FullName).ToList();
                    foreach (var key in entries)
                    {
                        sql.Delete<CacheEntry>(key.Id);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll(Type type)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        return _initialized.SelectMany(
            async (_) =>
            {
                await Connection.RunInTransactionAsync(sql =>
                {
                    var entries = sql.Table<CacheEntry>().Where(x => x.TypeName == type.FullName).ToList();
                    foreach (var key in entries)
                    {
                        sql.Delete<CacheEntry>(key.Id);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        return _initialized.SelectMany(
            async (_) =>
            {
                await Connection.RunInTransactionAsync(sql =>
                {
                    var entries = sql.Table<CacheEntry>().Where(x => x.TypeName == null).ToList();
                    foreach (var key in entries)
                    {
                        sql.Delete<CacheEntry>(key.Id);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Vacuum()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        if (Connection is null)
        {
            return Observable.Throw<Unit>(new InvalidOperationException("The Connection is null and therefore no database operations can happen."));
        }

        return _initialized.SelectMany(async (_, _, _) =>
        {
            await Connection.ExecuteAsync("VACUUM").ConfigureAwait(false);
            return Unit.Default;
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(true);

        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the async resources.
    /// </summary>
    /// <returns>The value task to monitor.</returns>
    protected virtual async ValueTask DisposeAsyncCore() =>
        await Connection.CloseAsync().ConfigureAwait(false);

    /// <summary>
    /// Disposes the object.
    /// </summary>
    /// <param name="isDisposing">If being called via the dispose method.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (_disposed)
        {
            return;
        }

        if (isDisposing)
        {
        }

        _disposed = true;
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
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache");
        }

        return Observable.Return(data, scheduler);
    }

    /// <summary>
    /// Initializes the database.
    /// </summary>
    /// <returns>An observable.</returns>
    private IObservable<Unit> Initialize()
    {
        var obs = Observable.Create<Unit>(async (obs, _) =>
        {
            try
            {
                await Connection.CreateTableAsync<CacheEntry>().ConfigureAwait(false);
                obs.OnNext(Unit.Default);
                obs.OnCompleted();
            }
            catch (Exception ex)
            {
                obs.OnError(ex);
            }
        });

        var connected = obs.PublishLast();
        connected.Connect();

        return connected.SubscribeOn(Scheduler);
    }
}
