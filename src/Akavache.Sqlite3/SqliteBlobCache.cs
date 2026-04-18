// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core.Observables;
using Akavache.Helpers;

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
    /// <summary>Runtime class name.</summary>
#if ENCRYPTED
    private const string ClassName = nameof(EncryptedSqliteBlobCache);
#else
    private const string ClassName = nameof(SqliteBlobCache);
#endif

    /// <summary>One-shot schema-initialization gate.</summary>
    private readonly InitSignal _initialized = new();

    /// <summary>Tracks whether the instance has been disposed.</summary>
    private int _disposed;

#if ENCRYPTED
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="password">The encryption key (applied via <c>PRAGMA key</c>).</param>
    /// <param name="serializer">The serializer.</param>
    public EncryptedSqliteBlobCache(string fileName, string password, ISerializer serializer)
    {
        ArgumentExceptionHelper.ThrowIfNull(fileName);
        ArgumentExceptionHelper.ThrowIfNull(password);
        Init(new SqlitePclRawConnection(fileName, password, readOnly: false), serializer, null);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="password">The encryption key (applied via <c>PRAGMA key</c>).</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public EncryptedSqliteBlobCache(string fileName, string password, ISerializer serializer, IScheduler scheduler)
    {
        ArgumentExceptionHelper.ThrowIfNull(fileName);
        ArgumentExceptionHelper.ThrowIfNull(password);
        Init(new SqlitePclRawConnection(fileName, password, readOnly: false), serializer, scheduler);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class
    /// with an abstracted database connection.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    public EncryptedSqliteBlobCache(IAkavacheConnection connection, ISerializer serializer)
        => Init(connection, serializer, null);

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedSqliteBlobCache"/> class
    /// with an abstracted database connection.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public EncryptedSqliteBlobCache(IAkavacheConnection connection, ISerializer serializer, IScheduler scheduler)
        => Init(connection, serializer, scheduler);
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="serializer">The serializer.</param>
    public SqliteBlobCache(string fileName, ISerializer serializer)
    {
        ArgumentExceptionHelper.ThrowIfNull(fileName);
        Init(new SqlitePclRawConnection(fileName, password: null, readOnly: false), serializer, null);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class.
    /// </summary>
    /// <param name="fileName">The database file name.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public SqliteBlobCache(string fileName, ISerializer serializer, IScheduler scheduler)
    {
        ArgumentExceptionHelper.ThrowIfNull(fileName);
        Init(new SqlitePclRawConnection(fileName, password: null, readOnly: false), serializer, scheduler);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class
    /// with an abstracted database connection.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    public SqliteBlobCache(IAkavacheConnection connection, ISerializer serializer)
        => Init(connection, serializer, null);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBlobCache"/> class
    /// with an abstracted database connection.
    /// </summary>
    /// <param name="connection">The database connection abstraction.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler.</param>
    public SqliteBlobCache(IAkavacheConnection connection, ISerializer serializer, IScheduler scheduler)
        => Init(connection, serializer, scheduler);
#endif

    /// <summary>Gets the underlying <see cref="IAkavacheConnection"/>.</summary>
    public IAkavacheConnection Connection { get; private set; }

    /// <inheritdoc/>
    public IScheduler Scheduler { get; private set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public ISerializer Serializer { get; private set; }

    /// <inheritdoc/>
    public IObservable<Unit> Flush() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)

            // Passive checkpoint nudges data out of the WAL; a failure is non-fatal
            // because the WAL itself is durable.
            : _initialized.Gate(() =>
                Connection.Checkpoint(CheckpointMode.Passive)
                    .CatchReturnUnit());

    /// <inheritdoc/>
    public IObservable<Unit> Flush(Type type) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : Core.CachedObservables.UnitDefault;

    /// <inheritdoc/>
    public IObservable<byte[]?> Get(string key)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>(ClassName);
        }

        return string.IsNullOrWhiteSpace(key)
            ? Observable.Throw<byte[]>(new ArgumentNullException(nameof(key)))
            : _initialized.Gate(() => ReadValueWithLegacyFallback(key, type: null));
    }

    /// <inheritdoc/>
    public IObservable<KeyValuePair<string, byte[]>> Get(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(keys)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var keyList = MaterializeKeys(keys);

        return _initialized.Gate(() =>
            Connection.GetMany(keyList, typeFullName: null, now)
                .WhereSelect(
                    static entry => entry.Value is not null && entry.Id is not null,
                    static entry => new KeyValuePair<string, byte[]>(entry.Id!, entry.Value!)));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>(ClassName);
        }

        return _initialized.Gate(() => ReadValueWithLegacyFallback(key, type));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var keyList = MaterializeKeys(keys);
        var typeName = type.FullName;

        return _initialized.Gate(() =>
            Connection.GetMany(keyList, typeName, now)
                .WhereSelect(
                    static entry => entry.Value is not null && entry.Id is not null,
                    static entry => new KeyValuePair<string, byte[]>(entry.Id!, entry.Value!)));
    }

    /// <inheritdoc/>
    public IObservable<KeyValuePair<string, byte[]>> GetAll(Type type)
    {
        if (type is null)
        {
            return Observable.Throw<KeyValuePair<string, byte[]>>(new ArgumentNullException(nameof(type)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<KeyValuePair<string, byte[]>>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var typeName = type.FullName;

        return _initialized.Gate(() =>
            Connection.GetAll(typeName, now)
                .WhereSelect(
                    static entry => entry.Value is not null && entry.Id is not null,
                    static entry => new KeyValuePair<string, byte[]>(entry.Id!, entry.Value!)));
    }

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        return _initialized.Gate(() => Connection.GetAllKeys(typeFullName: null, now));
    }

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys(Type type)
    {
        if (type is null)
        {
            return Observable.Throw<string>(new ArgumentNullException(nameof(type)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<string>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var typeName = type.FullName;

        return _initialized.Gate(() => Connection.GetAllKeys(typeName, now));
    }

    /// <inheritdoc/>
    public IObservable<(string Key, DateTimeOffset? Time)> GetCreatedAt(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<(string Key, DateTimeOffset? Time)>(new ArgumentNullException(nameof(keys)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var keyList = MaterializeKeys(keys);

        return _initialized.Gate(() =>
            Connection.GetMany(keyList, typeFullName: null, now)
                .WhereSelect(
                    static entry => entry.Id is not null,
                    static entry => (Key: entry.Id!, Time: (DateTimeOffset?)entry.CreatedAt)));
    }

    /// <inheritdoc/>
    public IObservable<DateTimeOffset?> GetCreatedAt(string key)
    {
        if (key is null)
        {
            return Observable.Throw<DateTimeOffset?>(new ArgumentNullException(nameof(key)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(ClassName);
        }

        // Defensive null-Id filter: mock connections (and potentially corrupt rows)
        // can surface an entry whose Id is null. Treat that as "not found" rather than
        // returning a timestamp for a row that would fail every subsequent lookup. Real
        // SqlitePclRawConnection never returns such entries because CacheEntry.Id is
        // NOT NULL in the schema, but the contract check keeps the layer above robust
        // against buggy IAkavacheConnection implementations.
        var now = DateTimeOffset.UtcNow;
        return _initialized.Gate(() =>
            Connection.Get(key, typeFullName: null, now)
                .Select(static entry => entry is null || entry.Id is null ? (DateTimeOffset?)null : entry.CreatedAt));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<(string Key, DateTimeOffset? Time)>(ClassName);
        }

        var now = DateTimeOffset.UtcNow;
        var keyList = MaterializeKeys(keys);
        var typeName = type.FullName;

        return _initialized.Gate(() =>
            Connection.GetMany(keyList, typeName, now)
                .WhereSelect(
                    static entry => entry.Id is not null,
                    static entry => (Key: entry.Id!, Time: (DateTimeOffset?)entry.CreatedAt)));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<DateTimeOffset?>(ClassName);
        }

        // See GetCreatedAt(string) for the null-Id rationale.
        var now = DateTimeOffset.UtcNow;
        var typeName = type.FullName;

        return _initialized.Gate(() =>
            Connection.Get(key, typeName, now)
                .Select(static entry => entry is null || entry.Id is null ? (DateTimeOffset?)null : entry.CreatedAt));
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
    {
        if (keyValuePairs is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keyValuePairs)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        var createdAt = DateTimeOffset.Now;

        return _initialized.Gate(() =>
        {
            var entries = BuildCacheEntries(keyValuePairs, typeName: null, createdAt, expiry);
            return entries.Count > 0
                ? Connection.Upsert(entries)
                : Observable.Return(Unit.Default);
        });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null) =>
        Insert([new KeyValuePair<string, byte[]>(key, data)], absoluteExpiration);

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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        var createdAt = DateTimeOffset.Now;
        var typeName = type.FullName;

        return _initialized.Gate(() =>
        {
            var entries = BuildCacheEntries(keyValuePairs, typeName, createdAt, expiry);
            if (entries.Count == 0)
            {
                return Observable.Return(Unit.Default);
            }

            // Upsert then best-effort checkpoint. Both failures are non-fatal:
            // the WAL is durable and a transient upsert error should not crash
            // the caller's observable chain.
            return Connection.Upsert(entries)
                .SelectMany(_ => Connection.Checkpoint(CheckpointMode.Passive)
                    .CatchReturnUnit())
                .CatchReturnUnit();
        });
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
    public IObservable<Unit> Invalidate(string key) =>
        string.IsNullOrWhiteSpace(key)
            ? Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)))
            : Invalidate([key]);

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(string key, Type type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        return type is null
            ? Observable.Throw<Unit>(new ArgumentNullException(nameof(type)))
            : Invalidate([key], type);
    }

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var keyList = MaterializeKeys(keys);

        return _initialized.Gate(() =>
            keyList.Count > 0
                ? Connection.Invalidate(keyList, typeFullName: null)
                : Observable.Return(Unit.Default));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var keyList = MaterializeKeys(keys);
        var typeName = type.FullName;

        return _initialized.Gate(() =>
            keyList.Count > 0
                ? Connection.Invalidate(keyList, typeName)
                : Observable.Return(Unit.Default));
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll(Type type)
    {
        if (type is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(type)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var typeName = type.FullName;
        return _initialized.Gate(() => Connection.InvalidateAll(typeName));
    }

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.Gate(() => Connection.InvalidateAll(typeFullName: null));

    /// <inheritdoc/>
    public IObservable<Unit> Vacuum() =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName)
            : _initialized.Gate(Connection.Compact);

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(string key, DateTimeOffset? absoluteExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Observable.Throw<Unit>(new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        return _initialized.Gate(() => Connection.SetExpiry(key, typeFullName: null, expiry));
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        var typeName = type.FullName;
        return _initialized.Gate(() => Connection.SetExpiry(key, typeName, expiry));
    }

    /// <inheritdoc/>
    public IObservable<Unit> UpdateExpiration(IEnumerable<string> keys, DateTimeOffset? absoluteExpiration)
    {
        if (keys is null)
        {
            return Observable.Throw<Unit>(new ArgumentNullException(nameof(keys)));
        }

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        var keyList = MaterializeKeys(keys);
        return _initialized.Gate(() =>
            keyList.Select(k => Connection.SetExpiry(k, typeFullName: null, expiry))
                .Concat()
                .LastOrDefaultAsync());
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

        if (Volatile.Read(ref _disposed) != 0)
        {
            return IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<Unit>(ClassName);
        }

        var expiry = absoluteExpiration;
        var keyList = MaterializeKeys(keys);
        var typeName = type.FullName;
        return _initialized.Gate(() =>
            keyList.Select(k => Connection.SetExpiry(k, typeName, expiry))
                .Concat()
                .LastOrDefaultAsync());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Converts an optional offset to UTC time.
    /// </summary>
    /// <param name="absoluteExpiration">The expiration, or null.</param>
    /// <returns>The UTC time, or null.</returns>
    internal static DateTime? ToExpiryValue(DateTimeOffset? absoluteExpiration) =>
        absoluteExpiration?.UtcDateTime;

    /// <summary>
    /// Reads a value from the legacy V10 <c>CacheElement</c> table.
    /// </summary>
    /// <param name="connection">The Akavache SQLite connection.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="now">Current time for expiry checks.</param>
    /// <param name="type">Type filter.</param>
    /// <returns>The raw legacy bytes, or <see langword="null"/>.</returns>
    internal static IObservable<byte[]?> TryGetLegacyValue(IAkavacheConnection connection, string key, DateTimeOffset now, Type? type) =>
        connection.TryReadLegacyV10Value(key, now, type);

    /// <summary>
    /// Initializes the database schema.
    /// </summary>
    /// <param name="connection">The Akavache SQLite connection.</param>
    /// <param name="gate">The initialization signal.</param>
    /// <param name="scheduler">The scheduler.</param>
    internal static void InitializeDatabase(IAkavacheConnection connection, InitSignal gate, IScheduler scheduler) =>
        connection.CreateSchema()
            .SubscribeOn(scheduler)
            .Subscribe(
                onNext: static _ => { },
                onError: gate.Fail,
                onCompleted: gate.Complete);

    /// <summary>
    /// Materializes keys into a concrete list.
    /// </summary>
    /// <param name="keys">The key sequence.</param>
    /// <returns>An read-only view of the keys.</returns>
    internal static IReadOnlyList<string> MaterializeKeys(IEnumerable<string> keys)
    {
        if (keys is IReadOnlyList<string> alreadyList)
        {
            return alreadyList;
        }

        if (keys is ICollection<string> collection)
        {
            var buffer = new string[collection.Count];
            collection.CopyTo(buffer, 0);
            return buffer;
        }

        return [.. keys];
    }

    /// <summary>
    /// Constructs a list of cache entry rows.
    /// </summary>
    /// <param name="keyValuePairs">The source key/value pairs.</param>
    /// <param name="typeName">Optional type discriminator.</param>
    /// <param name="createdAt">Creation timestamp.</param>
    /// <param name="expiry">Optional absolute expiration.</param>
    /// <returns>A materialized list of rows.</returns>
    internal static List<CacheEntry> BuildCacheEntries(
        IEnumerable<KeyValuePair<string, byte[]>> keyValuePairs,
        string? typeName,
        DateTimeOffset createdAt,
        DateTimeOffset? expiry)
    {
        var entries = new List<CacheEntry>(keyValuePairs is ICollection<KeyValuePair<string, byte[]>> c ? c.Count : 4);
        foreach (var kvp in keyValuePairs)
        {
            entries.Add(new CacheEntry(kvp.Key, typeName, kvp.Value, createdAt, expiry));
        }

        return entries;
    }

    /// <summary>
    /// Reads a single value with legacy fallback.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="type">Optional type filter.</param>
    /// <returns>The stored bytes or errors if not found.</returns>
    internal IObservable<byte[]> ReadValueWithLegacyFallback(string key, Type? type) =>
        new ReadWithLegacyFallbackObservable(Connection, key, type);

    /// <summary>
    /// Shared constructor body. Validates arguments, assigns properties, and starts the
    /// database initialization observable.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="scheduler">The scheduler, or <see langword="null"/> for the default task-pool scheduler.</param>
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Connection), nameof(Serializer), nameof(Scheduler))]
    internal void Init(IAkavacheConnection connection, ISerializer serializer, IScheduler? scheduler)
    {
        ArgumentExceptionHelper.ThrowIfNull(connection);
        ArgumentExceptionHelper.ThrowIfNull(serializer);

        Serializer = serializer;
        Connection = connection;
        Scheduler = scheduler ?? CacheDatabase.TaskpoolScheduler;
        InitializeDatabase(Connection, _initialized, Scheduler);
    }

    /// <summary>
    /// Hook for encrypting data before writing to disk.
    /// </summary>
    /// <param name="data">The byte data to encrypt.</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>A Future result representing the encrypted data.</returns>
    protected internal virtual IObservable<byte[]> BeforeWriteToDiskFilter(byte[] data, IScheduler scheduler) =>
        Volatile.Read(ref _disposed) != 0
            ? IBlobCache.ExceptionHelpers.ObservableThrowObjectDisposedException<byte[]>("SqlitePersistentBlobCache")
            : Observable.Return(data, scheduler);

    /// <summary>
    /// Releases the resources used by the instance.
    /// </summary>
    /// <param name="isDisposing">true to release managed resources.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (!DisposeHelper.TryClaimDispose(isDisposing, ref _disposed))
        {
            return;
        }

        try
        {
            Connection.Checkpoint(CheckpointMode.Full).Subscribe(
                static _ => { },
                static _ => { });
        }
        catch
        {
            // Connection may already be partially torn down.
        }

        Connection.Dispose();
    }
}
