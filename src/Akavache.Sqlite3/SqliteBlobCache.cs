// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;
using SQLite;

#if ENCRYPTED
using System.Diagnostics.CodeAnalysis;

namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Provides a SQLite-based implementation of IBlobCache for persistent data storage.
/// This cache stores data in a SQLite database file for reliable persistence across application restarts.
/// </summary>
#if ENCRYPTED
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Reused file.")]
public class EncryptedSqliteBlobCache : ISecureBlobCache
#else
public class SqliteBlobCache : IBlobCache
#endif
{
#if ENCRYPTED
    /// <summary>The runtime class name used in diagnostics and disposed exceptions.</summary>
    private const string ClassName = nameof(EncryptedSqliteBlobCache);
#else
    /// <summary>The runtime class name used in diagnostics and disposed exceptions.</summary>
    private const string ClassName = nameof(SqliteBlobCache);
#endif

    /// <summary>Observable that completes once the underlying database schema is initialized.</summary>
    private readonly IObservable<Unit> _initialized;

    /// <summary>Indicates whether <see cref="Dispose(bool)"/> has been invoked.</summary>
    private bool _disposed;

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache" /> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="password">The password.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="storeDateTimeAsTicks">Specifies whether to store DateTime properties as ticks (true) or strings (false).
    /// You absolutely do want to store them as Ticks in all new projects. The value
    /// of false is only here for backwards compatibility. There is a *significant* speed
    /// advantage, with no down sides, when setting storeDateTimeAsTicks = true. If you
    /// use DateTimeOffset properties, it will be always stored as ticks regardingless
    /// the storeDateTimeAsTicks parameter.</param>
    public EncryptedSqliteBlobCache(string fileName, string password, ISerializer serializer, IScheduler? scheduler = null, bool storeDateTimeAsTicks = true)
        : this(
              new SQLiteConnectionString(fileName ?? throw new ArgumentNullException(nameof(fileName)), storeDateTimeAsTicks, key: password ?? throw new ArgumentNullException(nameof(password))),
              serializer,
              scheduler)
    {
    }
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="storeDateTimeAsTicks">
    /// Specifies whether to store DateTime properties as ticks (true) or strings (false).
    /// You absolutely do want to store them as Ticks in all new projects. The value
    /// of false is only here for backwards compatibility. There is a *significant* speed
    /// advantage, with no down sides, when setting storeDateTimeAsTicks = true. If you
    /// use DateTimeOffset properties, it will be always stored as ticks regardingless
    /// the storeDateTimeAsTicks parameter.
    /// </param>
    public SqliteBlobCache(string fileName, ISerializer serializer, IScheduler? scheduler = null, bool storeDateTimeAsTicks = true)
        : this(
              new SQLiteConnectionString(fileName ?? throw new ArgumentNullException(nameof(fileName)), storeDateTimeAsTicks),
              serializer,
              scheduler)
    {
    }
#endif

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache" /> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <exception cref="ArgumentNullException">connectionString.</exception>
    public EncryptedSqliteBlobCache(SQLiteConnectionString connectionString, ISerializer serializer, IScheduler? scheduler = null)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public SqliteBlobCache(SQLiteConnectionString connectionString, ISerializer serializer, IScheduler? scheduler = null)
#endif
        : this(
              new SqliteAkavacheConnection(connectionString ?? throw new ArgumentNullException(nameof(connectionString))),
              serializer,
              scheduler)
    {
    }

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class
    /// with an abstracted database connection, enabling custom or test storage backends.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public EncryptedSqliteBlobCache(IAkavacheConnection connection, ISerializer serializer, IScheduler? scheduler = null)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class
    /// with an abstracted database connection, enabling custom or test storage backends.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public SqliteBlobCache(IAkavacheConnection connection, ISerializer serializer, IScheduler? scheduler = null)
#endif
    {
        ArgumentExceptionHelper.ThrowIfNull(connection);
        ArgumentExceptionHelper.ThrowIfNull(serializer);

        Serializer = serializer;
        Connection = connection;
        Scheduler = scheduler ?? CacheDatabase.TaskpoolScheduler;
        _initialized = InitializeDatabase(Connection, Scheduler);
    }

    /// <summary>
    /// Gets the connection.
    /// </summary>
    public IAkavacheConnection Connection { get; }

    /// <inheritdoc/>
    public IScheduler Scheduler { get; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public ISerializer Serializer { get; }

    /// <inheritdoc/>
    public IHttpService HttpService
    {
        get => field ??= new HttpService();
        set;
    }

    /// <inheritdoc/>
    public IObservable<Unit> Flush() =>
        _disposed
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.SelectMany(async (_, _, _) =>
            {
                // For SQLite, perform a WAL checkpoint to ensure data is persisted to the main database file.
                try
                {
                    await Connection.CheckpointAsync().ConfigureAwait(false);
                }
                catch
                {
                    // If WAL checkpoint fails, the data is still safe in the transaction log
                    // Continue without error as this is not critical for data integrity
                }

                return Unit.Default;
            });

    /// <inheritdoc/>
    public IObservable<Unit> Flush(Type type) =>
        _disposed
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : Observable.Return(Unit.Default);

    /// <inheritdoc/>
    public IObservable<byte[]?> Get(string key)
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>(ClassName);
        }

        return string.IsNullOrWhiteSpace(key)
            ? Observable.Throw<byte[]>(new ArgumentNullException(nameof(key)))
            : _initialized.SelectMany((_, _, _) => ReadValueWithLegacyFallbackAsync(key, type: null));
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

        var time = DateTimeOffset.UtcNow;

        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && keys.Contains(x.Id)))
            .SelectMany(x => x)
            .Where(static x => x.Value is not null && x.Id is not null)
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

        return _initialized.SelectMany((_, _, _) => ReadValueWithLegacyFallbackAsync(key, type));
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && keys.Contains(x.Id) && x.TypeName == type.FullName))
            .SelectMany(x => x)
            .Where(static x => x.Value is not null && x.Id is not null)
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.TypeName == type.FullName))
            .SelectMany(x => x)
            .Where(static x => x.Value is not null && x.Id is not null)
            .Select(x => new KeyValuePair<string, byte[]>(x.Id!, x.Value!));
    }

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys()
    {
        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(ClassName);
        }

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time)))
            .SelectMany(static x => x)
            .Where(static x => x.Id is not null)
            .Select(static x => x.Id!);
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.TypeName == type.FullName))
            .SelectMany(x => x)
            .Where(static x => x.Id is not null)
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && keys.Contains(x.Id)))
            .SelectMany(x => x)
            .Where(static x => x.Id is not null)
            .Select(static x => (Key: x.Id!, Time: (DateTimeOffset?)x.CreatedAt));
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.Id == key))
            .SelectMany(x => x)
            .Where(static x => x.Id is not null)
            .Select(static x => (DateTimeOffset?)x.CreatedAt)
            .DefaultIfEmpty();
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && keys.Contains(x.Id) && x.TypeName == type.FullName))
            .SelectMany(x => x)
            .Where(static x => x.Id is not null)
            .Select(static x => (Key: x.Id!, Time: (DateTimeOffset?)x.CreatedAt));
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

        var time = DateTimeOffset.UtcNow;
        return _initialized.SelectMany((_, _, _) => Connection.QueryAsync<CacheEntry>(x => x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.Id == key && x.TypeName == type.FullName))
            .SelectMany(x => x)
            .Where(static x => x.Id is not null)
            .Select(static x => (DateTimeOffset?)x.CreatedAt)
            .DefaultIfEmpty();
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

        var expiry = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;

        return _initialized.SelectMany(
            async (_, _, _) =>
            {
                var entries = keyValuePairs.Select(x => new CacheEntry { CreatedAt = DateTime.Now, Id = x.Key, Value = x.Value, ExpiresAt = expiry });

                await Connection.RunInTransactionAsync(tx =>
                {
                    foreach (var entry in entries)
                    {
                        tx.InsertOrReplace(entry);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            })
            .Select(_ => Unit.Default);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) => Insert([new KeyValuePair<string, byte[]>(key, data)], absoluteExpiration);

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

        var expiry = (absoluteExpiration ?? DateTimeOffset.MaxValue).UtcDateTime;

        return _initialized.SelectMany(async (_, _, _) =>
            {
                var entries = keyValuePairs.Select(x => new CacheEntry { CreatedAt = DateTime.Now, Id = x.Key, Value = x.Value, ExpiresAt = expiry, TypeName = type.FullName });
                try
                {
                    await Connection.RunInTransactionAsync(tx =>
                    {
                        if (!tx.IsValid)
                        {
                            return;
                        }

                        foreach (var entry in entries)
                        {
                            try
                            {
                                if (!tx.IsValid)
                                {
                                    return;
                                }

                                tx.InsertOrReplace(entry);
                            }
                            catch (Exception)
                            {
                                return;
                            }
                        }
                    }).ConfigureAwait(false);
                }
                catch
                {
                    return Unit.Default;
                }

                // Ensure data is immediately persisted to disk for multi-instance scenarios
                try
                {
                    await Connection.CheckpointAsync().ConfigureAwait(false);
                }
                catch
                {
                    // If WAL checkpoint fails, continue - the data is still in the transaction log
                }

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

        return type is null
            ? Observable.Throw<Unit>(new ArgumentNullException(nameof(type)))
            : Insert([new KeyValuePair<string, byte[]>(key, data)], type, absoluteExpiration);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(string key) => string.IsNullOrWhiteSpace(key) ? Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key))) : Invalidate([key]);

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(string key, Type type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        return type is null ? Observable.Throw<Unit>(new ArgumentNullException(nameof(type))) : Invalidate([key], type);
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

        return _initialized.SelectMany(
            async _ =>
            {
                await Connection.RunInTransactionAsync(tx =>
                {
                    foreach (var key in keys)
                    {
                        tx.Delete<CacheEntry>(key);
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

        return _initialized.SelectMany(
            async _ =>
            {
                await Connection.RunInTransactionAsync(tx =>
                {
                    foreach (var key in tx.Query<CacheEntry>(x => keys.Contains(x.Id) && x.TypeName == type.FullName))
                    {
                        tx.Delete<CacheEntry>(key.Id!);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll(Type type)
    {
        return _disposed
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.SelectMany(async _ =>
            {
                await Connection.RunInTransactionAsync(tx =>
                {
                    foreach (var key in tx.Query<CacheEntry>(x => x.TypeName == type.FullName))
                    {
                        tx.Delete<CacheEntry>(key.Id!);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll()
    {
        return _disposed
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.SelectMany(async _ =>
            {
                await Connection.RunInTransactionAsync(tx =>
                {
                    foreach (var key in tx.Query<CacheEntry>(_ => true))
                    {
                        tx.Delete<CacheEntry>(key.Id!);
                    }
                }).ConfigureAwait(false);

                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Vacuum()
    {
        return _disposed
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.SelectMany(async (_, _, _) =>
            {
                await Connection.CompactAsync().ConfigureAwait(false);
                return Unit.Default;
            });
    }

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiryValue = ToExpiryValue(absoluteExpiration);
        return _initialized.SelectMany(async (_, _, _) =>
        {
            await Connection.RunInTransactionAsync(tx => tx.SetExpiry(key, null, expiryValue)).ConfigureAwait(false);
            return Unit.Default;
        });
    }

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(string key, Type type, DateTimeOffset? absoluteExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiryValue = ToExpiryValue(absoluteExpiration);
        return _initialized.SelectMany(async (_, _, _) =>
        {
            await Connection.RunInTransactionAsync(tx => tx.SetExpiry(key, type.FullName, expiryValue)).ConfigureAwait(false);
            return Unit.Default;
        });
    }

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration)
    {
        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiryValue = ToExpiryValue(absoluteExpiration);
        return _initialized.SelectMany(async (_, _, _) =>
        {
            await Connection.RunInTransactionAsync(tx =>
            {
                foreach (var key in keys)
                {
                    tx.SetExpiry(key, null, expiryValue);
                }
            }).ConfigureAwait(false);
            return Unit.Default;
        });
    }

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, Type type, DateTimeOffset? absoluteExpiration)
    {
        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        if (_disposed)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiryValue = ToExpiryValue(absoluteExpiration);
        return _initialized.SelectMany(async (_, _, _) =>
        {
            await Connection.RunInTransactionAsync(tx =>
            {
                foreach (var key in keys)
                {
                    tx.SetExpiry(key, type.FullName, expiryValue);
                }
            }).ConfigureAwait(false);
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
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns <paramref name="existing"/> when it is non-null, otherwise a freshly
    /// constructed default <see cref="HttpService"/>. Used by tests that exercise the
    /// lazy-init semantics of the <see cref="HttpService"/> property directly.
    /// </summary>
    /// <param name="existing">The currently cached <see cref="IHttpService"/>, or <see langword="null"/>.</param>
    /// <returns>A non-null <see cref="IHttpService"/>.</returns>
    internal static IHttpService GetOrCreateHttpService(IHttpService? existing) =>
        existing ?? new HttpService();

    /// <summary>
    /// Converts an optional <see cref="DateTimeOffset"/> into the nullable
    /// <see cref="DateTime"/> form consumed by the transaction layer.
    /// </summary>
    /// <param name="absoluteExpiration">The expiration, or <see langword="null"/> to clear it.</param>
    /// <returns>The UTC <see cref="DateTime"/>, or <see langword="null"/>.</returns>
    internal static DateTime? ToExpiryValue(DateTimeOffset? absoluteExpiration) =>
        absoluteExpiration?.UtcDateTime;

    /// <summary>
    /// Reads a value from the legacy V10 <c>CacheElement</c> table via the
    /// supplied <paramref name="connection"/>.
    /// </summary>
    /// <param name="connection">The Akavache SQLite connection to read through.</param>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="now">The current time used for expiry checks.</param>
    /// <param name="type">The type filter, or <see langword="null"/> to read untyped entries.</param>
    /// <returns>The raw legacy bytes, or <see langword="null"/> when the key is absent.</returns>
    internal static Task<byte[]?> TryGetLegacyValueAsync(IAkavacheConnection connection, string key, DateTimeOffset now, Type? type) =>
        connection.TryReadLegacyV10ValueAsync(key, now, type);

    /// <summary>
    /// Builds the initialization observable that creates the <c>CacheEntry</c>
    /// table on <paramref name="connection"/> and completes once the schema is
    /// ready.
    /// </summary>
    /// <param name="connection">The Akavache SQLite connection to initialize.</param>
    /// <param name="scheduler">The scheduler the subscription runs on.</param>
    /// <returns>An observable that emits once and completes on successful initialization, or errors on failure.</returns>
    internal static IObservable<Unit> InitializeDatabase(IAkavacheConnection connection, IScheduler scheduler)
    {
        var obs = Observable.Create<Unit>(async (observer, _) =>
        {
            try
            {
                await connection.CreateTableAsync<CacheEntry>().ConfigureAwait(false);
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });

        var connected = obs.PublishLast();
        connected.Connect();

        return connected.SubscribeOn(scheduler);
    }

    /// <summary>
    /// Reads a single value from the V11 <c>CacheEntry</c> table, falling back
    /// to the legacy V10 <c>CacheElement</c> store, and finally throwing
    /// <see cref="KeyNotFoundException"/> when neither store has the key.
    /// </summary>
    /// <remarks>
    /// Shared by <see cref="Get(string)"/> and <see cref="Get(string, Type)"/>;
    /// <paramref name="type"/> is <see langword="null"/> for the untyped overload
    /// and a concrete type for the typed overload (which scopes the V11 lookup to
    /// rows whose <c>TypeName</c> column matches <paramref name="type"/>'s full name).
    /// Marked <c>internal</c> so unit tests can drive this method directly against
    /// an <c>InMemoryAkavacheConnection</c> without going through the full
    /// <see cref="Get(string)"/> observable plumbing.
    /// </remarks>
    /// <param name="key">The cache key to read.</param>
    /// <param name="type">The optional type filter; <see langword="null"/> for untyped reads.</param>
    /// <returns>The stored bytes for the key.</returns>
    internal async Task<byte[]> ReadValueWithLegacyFallbackAsync(string key, Type? type)
    {
        var time = DateTimeOffset.UtcNow;

        var rows = type is null
            ? await Connection.QueryAsync<CacheEntry>(x =>
                    x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.Id == key)
                .ConfigureAwait(false)
            : await Connection.QueryAsync<CacheEntry>(x =>
                    x.Id != null && (x.ExpiresAt == null || x.ExpiresAt > time) && x.Id == key && x.TypeName == type.FullName)
                .ConfigureAwait(false);

        return rows.FirstOrDefault()?.Value
            ?? await TryGetLegacyValueAsync(Connection, key, time, type).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                type is null
                    ? $"The given key '{key}' was not present in the cache."
                    : $"The given key '{key}' (type '{type.FullName}') was not present in the cache.");
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
    protected internal virtual IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler) => _disposed
        ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache")
        : Observable.Return(data, scheduler);

    /// <summary>
    /// Disposes of the async resources.
    /// </summary>
    /// <returns>The value task to monitor.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Ensure all pending operations are completed and data is persisted.
        try
        {
            // Force a full checkpoint to flush all data to the main store. Crucial for
            // multi-instance scenarios where one instance writes and another reads.
            await Connection.CheckpointAsync(CheckpointMode.Full).ConfigureAwait(false);
        }
        catch
        {
            // If checkpoint fails, fall back to compaction to ensure data persistence.
            try
            {
                await Connection.CompactAsync().ConfigureAwait(false);
            }
            catch
            {
                // If both fail, we'll rely on the normal connection close.
            }
        }

        // Release auxiliary resources (e.g. SQLite -wal/-shm files) as soon as possible.
        try
        {
            await Connection.ReleaseAuxiliaryResourcesAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        // Final close
        try
        {
            await Connection.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        // Small delay to allow OS to release file handles on Windows
        await Task.Delay(50).ConfigureAwait(false);
    }

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
            // Best-effort synchronous cleanup for cases where async dispose isn't used.
            try
            {
                Connection.CheckpointAsync(CheckpointMode.Truncate).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
            }

            try
            {
                Connection.ReleaseAuxiliaryResourcesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
            }

            try
            {
                Connection.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        _disposed = true;
    }
}
