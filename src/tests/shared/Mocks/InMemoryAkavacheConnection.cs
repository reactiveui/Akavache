// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Akavache.Tests.Mocks;

/// <summary>
/// A dictionary-backed implementation of <see cref="IAkavacheConnection"/> for unit
/// testing. Stores <see cref="CacheEntry"/> entities in a
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> keyed by primary key, enabling
/// tests to exercise <c>SqliteBlobCache</c> logic without a real SQLite database.
/// </summary>
/// <remarks>
/// Every method returns a deferred <see cref="Observable.Defer{TResult}(Func{IObservable{TResult}})"/>
/// chain so the dispose check and the in-memory mutation happen at subscribe time,
/// matching the real sqlite-backed connection's semantics (nothing fires until the
/// downstream pipeline subscribes). Counter-and-flag properties
/// (<see cref="CheckpointCount"/>, <see cref="FailUpsert"/>, etc.) let tests verify
/// call patterns and inject failures at specific points.
/// </remarks>
internal sealed class InMemoryAkavacheConnection : IAkavacheConnection
{
    /// <summary>Backing store keyed by primary key.</summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    /// <summary>Tracks whether <see cref="CreateSchema"/> has been invoked.</summary>
    private bool _schemaCreated;

    /// <summary>Gets or sets a value indicating whether the connection should simulate being disposed.</summary>
    public bool SimulateDisposed { get; set; }

    /// <summary>Gets the number of times <see cref="Checkpoint"/> was invoked.</summary>
    public int CheckpointCount { get; private set; }

    /// <summary>Gets the mode passed to the most recent <see cref="Checkpoint"/> call.</summary>
    public CheckpointMode? LastCheckpointMode { get; private set; }

    /// <summary>Gets the number of times <see cref="Compact"/> was invoked.</summary>
    public int CompactCount { get; private set; }

    /// <summary>Gets a mutable store of legacy V10 values that <see cref="TryReadLegacyV10Value"/> reads from.</summary>
    public Dictionary<string, byte[]> LegacyV10Store { get; } = [];

    /// <summary>Gets the internal live store of <see cref="CacheEntry"/> values. Exposed for test assertions.</summary>
    public IReadOnlyDictionary<string, CacheEntry> Store => _store;

    /// <summary>Gets or sets a value indicating whether <see cref="Checkpoint"/> should throw.</summary>
    public bool FailCheckpoint { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Checkpoint"/> should throw synchronously (not via observable).</summary>
    public bool ThrowOnCheckpointCall { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Compact"/> should throw.</summary>
    public bool FailCompact { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="CreateSchema"/> should throw.</summary>
    public bool FailCreateTable { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Upsert"/> should throw.</summary>
    public bool FailUpsert { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Get(string, string?, DateTimeOffset)"/> should throw.</summary>
    public bool FailGet { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="TryReadLegacyV10Value"/> should throw.</summary>
    public bool FailLegacyRead { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether read methods should ignore the expiration
    /// filter and return every stored entry. Used to exercise post-query defensive
    /// <c>x?.Id is not null</c> / <c>x?.Value is not null</c> branches in <c>SqliteBlobCache</c>.
    /// </summary>
    public bool BypassPredicate { get; set; }

    /// <summary>
    /// Seeds an entry directly into the underlying store without going through
    /// <see cref="Upsert"/>'s null-Id guard. Useful for driving SqliteBlobCache's
    /// defensive filters from tests.
    /// </summary>
    /// <param name="key">The dictionary key.</param>
    /// <param name="entry">The entry to store.</param>
    public void SeedRaw(string key, CacheEntry entry) => _store[key] = entry;

    /// <inheritdoc/>
    public IObservable<Unit> CreateSchema() =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (FailCreateTable)
            {
                return Observable.Throw<Unit>(new InvalidOperationException("Simulated CreateTable failure."));
            }

            _schemaCreated = true;
            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<bool> TableExists(string tableName) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            return Observable.Return(_schemaCreated && string.Equals(tableName, "CacheEntry", StringComparison.OrdinalIgnoreCase));
        });

    /// <inheritdoc/>
    public IObservable<CacheEntry?> Get(string key, string? typeFullName, DateTimeOffset now) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (FailGet)
            {
                return Observable.Throw<CacheEntry?>(new InvalidOperationException("Simulated Get failure."));
            }

            if (!_store.TryGetValue(key, out var entry))
            {
                return Observable.Return<CacheEntry?>(null);
            }

            if (!BypassPredicate && !IsUnexpired(entry, now))
            {
                return Observable.Return<CacheEntry?>(null);
            }

            if (typeFullName is not null && !string.Equals(entry.TypeName, typeFullName, StringComparison.Ordinal))
            {
                return Observable.Return<CacheEntry?>(null);
            }

            return Observable.Return<CacheEntry?>(entry);
        });

    /// <inheritdoc/>
    public IObservable<CacheEntry> GetMany(IReadOnlyList<string> keys, string? typeFullName, DateTimeOffset now) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            var rows = new List<CacheEntry>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                if (!_store.TryGetValue(keys[i], out var entry))
                {
                    continue;
                }

                if (!BypassPredicate && !IsUnexpired(entry, now))
                {
                    continue;
                }

                if (typeFullName is not null && !string.Equals(entry.TypeName, typeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                rows.Add(entry);
            }

            return rows.ToObservable();
        });

    /// <inheritdoc/>
    public IObservable<CacheEntry> GetAll(string? typeFullName, DateTimeOffset now) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            var rows = new List<CacheEntry>();
            foreach (var entry in _store.Values)
            {
                if (!BypassPredicate && !IsUnexpired(entry, now))
                {
                    continue;
                }

                if (typeFullName is not null && !string.Equals(entry.TypeName, typeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                rows.Add(entry);
            }

            return rows.ToObservable();
        });

    /// <inheritdoc/>
    public IObservable<string> GetAllKeys(string? typeFullName, DateTimeOffset now) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            var keys = new List<string>();
            foreach (var entry in _store.Values)
            {
                if (!BypassPredicate && !IsUnexpired(entry, now))
                {
                    continue;
                }

                if (typeFullName is not null && !string.Equals(entry.TypeName, typeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (entry.Id is not null)
                {
                    keys.Add(entry.Id);
                }
            }

            return keys.ToObservable();
        });

    /// <inheritdoc/>
    public IObservable<Unit> Upsert(IReadOnlyList<CacheEntry> entries) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (FailUpsert)
            {
                return Observable.Throw<Unit>(new InvalidOperationException("Simulated upsert failure."));
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.Id is not null)
                {
                    _store[entry.Id] = entry;
                }
            }

            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<Unit> Invalidate(IReadOnlyList<string> keys, string? typeFullName) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (typeFullName is null)
                {
                    _store.TryRemove(key, out _);
                    continue;
                }

                if (_store.TryGetValue(key, out var existing) && string.Equals(existing.TypeName, typeFullName, StringComparison.Ordinal))
                {
                    _store.TryRemove(key, out _);
                }
            }

            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<Unit> InvalidateAll(string? typeFullName) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (typeFullName is null)
            {
                _store.Clear();
                return Observable.Return(Unit.Default);
            }

            foreach (var kvp in _store.ToArray())
            {
                if (string.Equals(kvp.Value.TypeName, typeFullName, StringComparison.Ordinal))
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }

            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<Unit> SetExpiry(string key, string? typeFullName, DateTimeOffset? expiresAt) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (!_store.TryGetValue(key, out var entry))
            {
                return Observable.Return(Unit.Default);
            }

            if (typeFullName is not null && !string.Equals(entry.TypeName, typeFullName, StringComparison.Ordinal))
            {
                return Observable.Return(Unit.Default);
            }

            _store[key] = new(entry.Id, entry.TypeName, entry.Value, entry.CreatedAt, expiresAt);
            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<Unit> VacuumExpired(DateTimeOffset now) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            foreach (var kvp in _store.ToArray())
            {
                if (!IsUnexpired(kvp.Value, now))
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }

            return Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<Unit> Checkpoint(CheckpointMode mode)
    {
        if (ThrowOnCheckpointCall)
        {
            throw new InvalidOperationException("Simulated synchronous checkpoint failure.");
        }

        return Observable.Defer(() =>
        {
            ThrowIfDisposed();
            CheckpointCount++;
            LastCheckpointMode = mode;
            return FailCheckpoint
                ? Observable.Throw<Unit>(new InvalidOperationException("Simulated checkpoint failure."))
                : Observable.Return(Unit.Default);
        });
    }

    /// <inheritdoc/>
    public IObservable<Unit> Compact() =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            CompactCount++;
            return FailCompact
                ? Observable.Throw<Unit>(new InvalidOperationException("Simulated compact failure."))
                : Observable.Return(Unit.Default);
        });

    /// <inheritdoc/>
    public IObservable<byte[]?> TryReadLegacyV10Value(string key, DateTimeOffset now, Type? type) =>
        Observable.Defer(() =>
        {
            ThrowIfDisposed();
            if (FailLegacyRead)
            {
                return Observable.Throw<byte[]?>(new InvalidOperationException("Simulated legacy read failure."));
            }

            return LegacyV10Store.TryGetValue(key, out var value)
                ? Observable.Return<byte[]?>(value)
                : Observable.Return<byte[]?>(null);
        });

    /// <inheritdoc/>
    public void Dispose() => SimulateDisposed = true;

    /// <summary>Returns <see langword="true"/> if <paramref name="entry"/> is not expired at <paramref name="now"/>.</summary>
    /// <param name="entry">The entry to inspect.</param>
    /// <param name="now">The clock instant used for the comparison.</param>
    /// <returns>Unexpired status.</returns>
    private static bool IsUnexpired(CacheEntry entry, DateTimeOffset now) =>
        entry.ExpiresAt is null || entry.ExpiresAt > now;

    /// <summary>Throws <see cref="ObjectDisposedException"/> if <see cref="SimulateDisposed"/> is set.</summary>
    private void ThrowIfDisposed()
    {
        if (!SimulateDisposed)
        {
            return;
        }

        throw new ObjectDisposedException(nameof(InMemoryAkavacheConnection));
    }
}
