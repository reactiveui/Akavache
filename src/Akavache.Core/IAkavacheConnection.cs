// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Akavache;

/// <summary>
/// Abstracts the asynchronous database connection used by cache implementations.
/// Implementations wrap a specific storage backend (e.g., sqlite-net's <c>SQLiteAsyncConnection</c>).
/// </summary>
public interface IAkavacheConnection : IAsyncDisposable
{
    /// <summary>
    /// Creates the table for the specified type if it does not already exist.
    /// </summary>
    /// <typeparam name="T">The entity type whose schema defines the table.</typeparam>
    /// <returns>A task that completes when the table is created.</returns>
    Task CreateTableAsync<T>()
        where T : class, new();

    /// <summary>
    /// Queries all entities of type <typeparamref name="T"/> matching the given predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A filter expression.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate)
        where T : class, new();

    /// <summary>
    /// Executes a raw SQL query and maps results to entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type to map results into.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="args">The parameters.</param>
    /// <returns>A list of mapped entities.</returns>
    Task<List<T>> QueryAsync<T>(string sql, params object[] args)
        where T : class, new();

    /// <summary>
    /// Returns the first entity matching the predicate, or <c>default</c> if none match.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A filter expression.</param>
    /// <returns>The first matching entity, or <c>default</c>.</returns>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate)
        where T : class, new();

    /// <summary>
    /// Inserts or replaces a single entity asynchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert or replace.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    Task InsertOrReplaceAsync<T>(T entity)
        where T : class, new();

    /// <summary>
    /// Executes a raw SQL statement with optional parameters.
    /// </summary>
    /// <param name="sql">The SQL statement.</param>
    /// <param name="args">The parameters.</param>
    /// <returns>A task that completes when the statement executes.</returns>
    Task ExecuteAsync(string sql, params object[] args);

    /// <summary>
    /// Executes a raw SQL query that returns a scalar value.
    /// </summary>
    /// <typeparam name="T">The scalar result type.</typeparam>
    /// <param name="sql">The SQL statement.</param>
    /// <param name="args">The parameters.</param>
    /// <returns>The scalar result.</returns>
    Task<T> ExecuteScalarAsync<T>(string sql, params object[] args);

    /// <summary>
    /// Runs a set of operations inside a transaction.
    /// The <paramref name="body"/> receives a synchronous <see cref="IAkavacheTransaction"/> handle
    /// that is valid only for the duration of the callback.
    /// </summary>
    /// <param name="body">An action receiving the synchronous transaction handle.</param>
    /// <returns>A task that completes when the transaction commits.</returns>
    Task RunInTransactionAsync(Action<IAkavacheTransaction> body);

    /// <summary>
    /// Checks whether a table with the specified name exists in the database.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns><c>true</c> if the table exists; otherwise, <c>false</c>.</returns>
    Task<bool> TableExistsAsync(string tableName);

    /// <summary>
    /// Requests that the backend flush committed data to durable storage.
    /// On SQLite this maps to <c>PRAGMA wal_checkpoint</c>; on backends with no
    /// write-ahead log concept this should be a no-op.
    /// </summary>
    /// <param name="mode">The checkpoint strength.</param>
    /// <returns>A task that completes when the checkpoint finishes.</returns>
    Task CheckpointAsync(CheckpointMode mode = CheckpointMode.Passive);

    /// <summary>
    /// Requests that the backend reclaim unused storage. On SQLite this maps to
    /// <c>VACUUM</c>. Backends that cannot compact should treat this as a no-op.
    /// </summary>
    /// <returns>A task that completes when compaction finishes.</returns>
    Task CompactAsync();

    /// <summary>
    /// Requests that the backend release any auxiliary resources it holds open beyond
    /// the primary data file, in preparation for close. On SQLite this switches the
    /// journal mode to <c>DELETE</c>, allowing <c>-wal</c> and <c>-shm</c> files to be
    /// removed. Backends with nothing to release should treat this as a no-op.
    /// </summary>
    /// <returns>A task that completes when auxiliary resources are released.</returns>
    Task ReleaseAuxiliaryResourcesAsync();

    /// <summary>
    /// Attempts to read a cache value from a V10 legacy backing store. Backends that
    /// do not support V10 legacy migration should return <c>null</c>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="now">The current time, used to filter out expired entries.</param>
    /// <param name="type">Optional type filter; when supplied, only entries matching this type are returned.</param>
    /// <returns>The legacy value bytes, or <c>null</c> if not present / not supported.</returns>
    Task<byte[]?> TryReadLegacyV10ValueAsync(string key, DateTimeOffset now, Type? type);

    /// <summary>
    /// Closes the connection and releases resources.
    /// </summary>
    /// <returns>A task that completes when the connection is closed.</returns>
    Task CloseAsync();
}
