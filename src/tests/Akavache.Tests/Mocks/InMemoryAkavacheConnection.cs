// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Akavache.Tests.Mocks;

/// <summary>
/// A dictionary-backed implementation of <see cref="IAkavacheConnection"/> for unit testing.
/// Stores <see cref="CacheEntry"/> entities in a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// keyed by primary key, enabling tests to exercise <c>SqliteBlobCache</c> logic without a real
/// SQLite database.
/// </summary>
internal sealed class InMemoryAkavacheConnection : IAkavacheConnection
{
    /// <summary>Backing store of cache entries keyed by primary key.</summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    /// <summary>Set of table names for which <see cref="CreateTableAsync{T}"/> has been invoked.</summary>
    private readonly HashSet<string> _createdTables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether the connection should simulate being disposed.
    /// When set to <c>true</c>, all methods throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public bool SimulateDisposed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether transactions created by this connection
    /// should report <see cref="IAkavacheTransaction.IsValid"/> as <c>false</c>,
    /// simulating a null or invalid underlying connection.
    /// </summary>
    public bool SimulateNullConnection { get; set; }

    /// <summary>Gets the number of times <see cref="CheckpointAsync"/> was invoked.</summary>
    public int CheckpointCount { get; private set; }

    /// <summary>Gets the mode passed to the most recent <see cref="CheckpointAsync"/> call.</summary>
    public CheckpointMode? LastCheckpointMode { get; private set; }

    /// <summary>Gets the number of times <see cref="CompactAsync"/> was invoked.</summary>
    public int CompactCount { get; private set; }

    /// <summary>Gets the number of times <see cref="ReleaseAuxiliaryResourcesAsync"/> was invoked.</summary>
    public int ReleaseAuxiliaryResourcesCount { get; private set; }

    /// <summary>
    /// Gets a mutable store of legacy V10 values that <see cref="TryReadLegacyV10ValueAsync"/> reads from.
    /// Tests can populate this to simulate a V10 database containing pre-existing data.
    /// </summary>
    public Dictionary<string, byte[]> LegacyV10Store { get; } = new();

    /// <summary>
    /// Gets the internal live store of <see cref="CacheEntry"/> values. Exposed for test assertions.
    /// </summary>
    public IReadOnlyDictionary<string, CacheEntry> Store => _store;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="CheckpointAsync"/> should throw.
    /// Used to exercise the catch paths in <c>SqliteBlobCache</c> around checkpoint failures.
    /// </summary>
    public bool FailCheckpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="CompactAsync"/> should throw.
    /// </summary>
    public bool FailCompact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ReleaseAuxiliaryResourcesAsync"/> should throw.
    /// </summary>
    public bool FailReleaseAuxiliaryResources { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="CloseAsync"/> should throw.
    /// </summary>
    public bool FailClose { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="CreateTableAsync{T}"/> should throw,
    /// used to exercise the error path in <c>SqliteBlobCache.Initialize</c>.
    /// </summary>
    public bool FailCreateTable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="RunInTransactionAsync"/> should throw,
    /// used to exercise the outer <c>catch</c> in typed Insert.
    /// </summary>
    public bool FailRunInTransaction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="IAkavacheTransaction.InsertOrReplace"/>
    /// should throw (inside a transaction body), used to exercise the inner <c>catch</c>
    /// in typed Insert.
    /// </summary>
    public bool FailInsertOrReplaceInTransaction { get; set; }

    /// <summary>
    /// Gets or sets the number of <see cref="IAkavacheTransaction.IsValid"/> reads that
    /// return <c>true</c> on transactions created by this connection. Used to exercise
    /// the inner mid-loop <c>IsValid</c> guard in <c>SqliteBlobCache.Insert</c>.
    /// </summary>
    public int TransactionIsValidTrueCallsRemaining { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="QueryAsync{T}(System.Linq.Expressions.Expression{System.Func{T, bool}})"/>
    /// should bypass the predicate filter and return every entry in the store. Used to
    /// exercise post-query defensive <c>x?.Id is not null</c> / <c>x?.Value is not null</c>
    /// branches in <c>SqliteBlobCache</c> by surfacing entries the predicate would normally
    /// have rejected.
    /// </summary>
    public bool BypassPredicate { get; set; }

    /// <summary>
    /// Seeds an entry directly into the underlying store without going through
    /// <see cref="InsertOrReplaceAsync{T}"/>'s null-Id guard. Used to inject malformed
    /// entries (null Id, null Value) for defensive-branch coverage tests.
    /// </summary>
    /// <param name="key">The dictionary key.</param>
    /// <param name="entry">The entry to store.</param>
    public void SeedRaw(string key, CacheEntry entry) => _store[key] = entry;

    /// <inheritdoc/>
    public Task CreateTableAsync<T>()
        where T : new()
    {
        ThrowIfDisposed();

        if (FailCreateTable)
        {
            return Task.FromException(new InvalidOperationException("Simulated CreateTable failure."));
        }

        _createdTables.Add(typeof(T).Name);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<List<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate)
        where T : new()
    {
        ThrowIfDisposed();

        if (typeof(T) != typeof(CacheEntry))
        {
            return Task.FromResult(new List<T>());
        }

        if (BypassPredicate)
        {
            return Task.FromResult(_store.Values.Cast<T>().ToList());
        }

        var compiled = predicate.Compile();
        var results = _store.Values
            .Cast<T>()
            .Where(compiled)
            .ToList();

        return Task.FromResult(results);
    }

    /// <inheritdoc/>
    public Task<List<T>> QueryAsync<T>(string sql, params object[] args)
        where T : new()
    {
        ThrowIfDisposed();

        // SQL-based queries are not supported in the in-memory implementation.
        // Return all entries for CacheEntry, empty for others.
        if (typeof(T) != typeof(CacheEntry))
        {
            return Task.FromResult(new List<T>());
        }

        return Task.FromResult(_store.Values.Cast<T>().ToList());
    }

    /// <inheritdoc/>
    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate)
        where T : new()
    {
        ThrowIfDisposed();

        if (typeof(T) != typeof(CacheEntry))
        {
            return Task.FromResult(default(T));
        }

        var compiled = predicate.Compile();
        var result = _store.Values
            .Cast<T>()
            .FirstOrDefault(compiled);

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task InsertOrReplaceAsync<T>(T entity)
        where T : new()
    {
        ThrowIfDisposed();

        if (entity is CacheEntry entry && entry.Id is not null)
        {
            _store[entry.Id] = entry;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(string sql, params object[] args)
    {
        ThrowIfDisposed();

        // No-op for in-memory store; SQL statements like PRAGMA or DELETE are ignored.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
    {
        ThrowIfDisposed();
        return Task.FromResult(default(T)!);
    }

    /// <inheritdoc/>
    public Task RunInTransactionAsync(Action<IAkavacheTransaction> body)
    {
        ThrowIfDisposed();

        if (FailRunInTransaction)
        {
            return Task.FromException(new InvalidOperationException("Simulated transaction failure."));
        }

        var transaction = new InMemoryAkavacheTransaction(_store, SimulateNullConnection)
        {
            FailInsertOrReplace = FailInsertOrReplaceInTransaction,
            IsValidTrueCallsRemaining = TransactionIsValidTrueCallsRemaining,
        };
        body(transaction);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> TableExistsAsync(string tableName)
    {
        ThrowIfDisposed();
        return Task.FromResult(_createdTables.Contains(tableName));
    }

    /// <inheritdoc/>
    public Task CheckpointAsync(CheckpointMode mode = CheckpointMode.Passive)
    {
        ThrowIfDisposed();
        CheckpointCount++;
        LastCheckpointMode = mode;

        if (FailCheckpoint)
        {
            return Task.FromException(new InvalidOperationException("Simulated checkpoint failure."));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CompactAsync()
    {
        ThrowIfDisposed();
        CompactCount++;

        if (FailCompact)
        {
            return Task.FromException(new InvalidOperationException("Simulated compact failure."));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReleaseAuxiliaryResourcesAsync()
    {
        ThrowIfDisposed();
        ReleaseAuxiliaryResourcesCount++;

        if (FailReleaseAuxiliaryResources)
        {
            return Task.FromException(new InvalidOperationException("Simulated release-auxiliary failure."));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<byte[]?> TryReadLegacyV10ValueAsync(string key, DateTimeOffset now, Type? type)
    {
        ThrowIfDisposed();

        if (LegacyV10Store.TryGetValue(key, out var value))
        {
            return Task.FromResult<byte[]?>(value);
        }

        return Task.FromResult<byte[]?>(null);
    }

    /// <inheritdoc/>
    public Task CloseAsync()
    {
        if (FailClose)
        {
            return Task.FromException(new InvalidOperationException("Simulated close failure."));
        }

        SimulateDisposed = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        SimulateDisposed = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> when <see cref="SimulateDisposed"/> is set.</summary>
    private void ThrowIfDisposed()
    {
        if (SimulateDisposed)
        {
            throw new ObjectDisposedException(nameof(InMemoryAkavacheConnection));
        }
    }
}
