// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Abstracts the database connection used by SQLite-backed blob caches. The surface is
/// intentionally narrow and <see cref="CacheEntry"/>-specific so that implementations
/// can bind parameters and cache prepared statements without going through reflection
/// or an expression-tree translator.
/// </summary>
/// <remarks>
/// <para>
/// Every method returns <see cref="IObservable{T}"/> rather than <see cref="Task"/> —
/// this matches the observable-first shape of <see cref="IBlobCache"/> so the whole
/// sqlite pipeline composes directly with Rx operators without Task/await bridges.
/// Cancellation is expressed through subscription disposal: unsubscribing before an
/// operation completes is the observable-native "cancel" signal. Note that operations
/// already dequeued by the worker thread cannot be cancelled mid-<c>sqlite3_step</c> —
/// SQLite has no per-statement cancel primitive.
/// </para>
/// <para>
/// Single-value reads (<see cref="Get"/>, <see cref="TryReadLegacyV10Value"/>,
/// <see cref="TableExists"/>) emit exactly one value and then complete. Multi-value
/// reads (<see cref="GetMany"/>, <see cref="GetAll"/>, <see cref="GetAllKeys"/>) emit
/// one item per matching row and then complete; callers that want a materialized list
/// can apply <c>.ToList()</c> at the boundary. Writers (<see cref="Upsert"/>,
/// <see cref="Invalidate"/>, <see cref="SetExpiry"/>, etc.) emit a single
/// <see cref="Unit"/> and then complete on success or <c>OnError</c> on failure.
/// </para>
/// <para>
/// <c>Task</c>-returning adapters live on <c>IAkavacheConnectionTaskExtensions</c> for
/// callers that prefer async/await at the boundary — those are thin
/// <see cref="System.Reactive.Threading.Tasks.TaskObservableExtensions.ToTask{TResult}(IObservable{TResult})"/>
/// wrappers that do not take part in the hot path.
/// </para>
/// </remarks>
public interface IAkavacheConnection : IDisposable
{
    /// <summary>
    /// Creates the CacheEntry table (and supporting indexes) if it does not already
    /// exist. Emits a single <see cref="Unit"/> when the schema is ready.
    /// </summary>
    /// <returns>A one-shot observable that signals schema creation completion.</returns>
    IObservable<Unit> CreateSchema();

    /// <summary>
    /// Checks whether a table with the specified name exists in the database. Emits
    /// a single <see cref="bool"/>.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>A one-shot observable that emits <see langword="true"/> when the table exists, <see langword="false"/> otherwise.</returns>
    IObservable<bool> TableExists(string tableName);

    /// <summary>
    /// Reads a single cache entry by key, honouring expiration and an optional type
    /// filter. Emits exactly one value: the entry, or <see langword="null"/> when no
    /// matching row exists (or the row is expired).
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="typeFullName">Optional type discriminator. <see langword="null"/> matches rows regardless of type.</param>
    /// <param name="now">The wall-clock instant used to filter expired rows.</param>
    /// <returns>A one-shot observable that emits the matching entry or <see langword="null"/>.</returns>
    IObservable<CacheEntry?> Get(string key, string? typeFullName, DateTimeOffset now);

    /// <summary>
    /// Reads every unexpired cache entry whose key is in <paramref name="keys"/>. Emits
    /// one item per matching row, in no particular order, then completes.
    /// </summary>
    /// <param name="keys">Keys to look up.</param>
    /// <param name="typeFullName">Optional type discriminator.</param>
    /// <param name="now">The wall-clock instant used to filter expired rows.</param>
    /// <returns>An observable sequence of matching entries.</returns>
    IObservable<CacheEntry> GetMany(IReadOnlyList<string> keys, string? typeFullName, DateTimeOffset now);

    /// <summary>
    /// Reads every unexpired cache entry in the store, optionally filtered by type.
    /// Emits one item per row, then completes.
    /// </summary>
    /// <param name="typeFullName">Optional type discriminator.</param>
    /// <param name="now">The wall-clock instant used to filter expired rows.</param>
    /// <returns>An observable sequence of matching entries.</returns>
    IObservable<CacheEntry> GetAll(string? typeFullName, DateTimeOffset now);

    /// <summary>
    /// Reads every unexpired cache entry key, optionally filtered by type. Emits one
    /// item per key, then completes.
    /// </summary>
    /// <param name="typeFullName">Optional type discriminator.</param>
    /// <param name="now">The wall-clock instant used to filter expired rows.</param>
    /// <returns>An observable sequence of matching keys.</returns>
    IObservable<string> GetAllKeys(string? typeFullName, DateTimeOffset now);

    /// <summary>
    /// Inserts or replaces a batch of cache entries within a single transaction.
    /// Emits a single <see cref="Unit"/> on commit.
    /// </summary>
    /// <param name="entries">The entries to upsert.</param>
    /// <returns>A one-shot observable that fires on commit.</returns>
    IObservable<Unit> Upsert(IReadOnlyList<CacheEntry> entries);

    /// <summary>
    /// Deletes cache entries by key within a single transaction. If a type discriminator
    /// is supplied, only rows whose <see cref="CacheEntry.TypeName"/> matches are removed.
    /// Emits a single <see cref="Unit"/> on commit.
    /// </summary>
    /// <param name="keys">Keys to remove.</param>
    /// <param name="typeFullName">Optional type discriminator.</param>
    /// <returns>A one-shot observable that fires on commit.</returns>
    IObservable<Unit> Invalidate(IReadOnlyList<string> keys, string? typeFullName);

    /// <summary>
    /// Removes every row from the CacheEntry table, optionally filtered by type.
    /// Emits a single <see cref="Unit"/> on commit.
    /// </summary>
    /// <param name="typeFullName">Optional type discriminator. <see langword="null"/> wipes everything.</param>
    /// <returns>A one-shot observable that fires on commit.</returns>
    IObservable<Unit> InvalidateAll(string? typeFullName);

    /// <summary>
    /// Updates the expiration time of a cache entry by key, with an optional type
    /// filter. Emits a single <see cref="Unit"/> on commit.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="typeFullName">Optional type discriminator.</param>
    /// <param name="expiresAt">The new expiration instant, or <see langword="null"/> to clear.</param>
    /// <returns>A one-shot observable that fires on commit.</returns>
    IObservable<Unit> SetExpiry(string key, string? typeFullName, DateTimeOffset? expiresAt);

    /// <summary>
    /// Removes every row whose expiration is older than <paramref name="now"/>. Emits
    /// a single <see cref="Unit"/> on commit.
    /// </summary>
    /// <param name="now">The wall-clock instant.</param>
    /// <returns>A one-shot observable that fires on commit.</returns>
    IObservable<Unit> VacuumExpired(DateTimeOffset now);

    /// <summary>
    /// Requests a WAL checkpoint at the specified strength. Backends with no write-ahead
    /// log should treat this as a no-op and emit <see cref="Unit"/> immediately.
    /// </summary>
    /// <param name="mode">The checkpoint strength.</param>
    /// <returns>A one-shot observable that fires when the checkpoint finishes.</returns>
    IObservable<Unit> Checkpoint(CheckpointMode mode);

    /// <summary>
    /// Requests that the backend reclaim unused storage. On SQLite this maps to
    /// <c>VACUUM</c>. Emits a single <see cref="Unit"/> on completion.
    /// </summary>
    /// <returns>A one-shot observable that fires when compaction finishes.</returns>
    IObservable<Unit> Compact();

    /// <summary>
    /// Attempts to read a cache value from a V10 legacy backing store. Backends that
    /// do not support V10 legacy migration emit <see langword="null"/>. Emits exactly
    /// one value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="now">The current time, used to filter out expired entries.</param>
    /// <param name="type">Optional type filter.</param>
    /// <returns>A one-shot observable that emits the legacy bytes or <see langword="null"/>.</returns>
    IObservable<byte[]?> TryReadLegacyV10Value(string key, DateTimeOffset now, Type? type);
}
