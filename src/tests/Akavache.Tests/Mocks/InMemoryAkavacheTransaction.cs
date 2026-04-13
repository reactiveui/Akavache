// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Akavache.Tests.Mocks;

/// <summary>
/// An in-memory implementation of <see cref="IAkavacheTransaction"/> for unit testing.
/// Operates on the shared <see cref="ConcurrentDictionary{TKey, TValue}"/> owned by
/// the parent <see cref="InMemoryAkavacheConnection"/>.
/// </summary>
internal sealed class InMemoryAkavacheTransaction : IAkavacheTransaction
{
    /// <summary>The shared backing store owned by the parent connection.</summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _store;

    /// <summary>When <c>true</c>, <see cref="IsValid"/> always returns <c>false</c>.</summary>
    private readonly bool _simulateInvalid;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAkavacheTransaction"/> class.
    /// </summary>
    /// <param name="store">The shared data store.</param>
    /// <param name="simulateInvalid">When <c>true</c>, <see cref="IsValid"/> returns <c>false</c>.</param>
    public InMemoryAkavacheTransaction(ConcurrentDictionary<string, CacheEntry> store, bool simulateInvalid)
    {
        _store = store;
        _simulateInvalid = simulateInvalid;
    }

    /// <inheritdoc/>
    public bool IsValid
    {
        get
        {
            if (_simulateInvalid)
            {
                return false;
            }

            if (IsValidTrueCallsRemaining <= 0)
            {
                return false;
            }

            IsValidTrueCallsRemaining--;
            return true;
        }
    }

    /// <summary>
    /// Gets or sets the number of <see cref="IsValid"/> reads that return <c>true</c> before
    /// the property starts returning <c>false</c>. Used to exercise the inner mid-loop
    /// <c>IsValid</c> guard inside <c>SqliteBlobCache.Insert</c>.
    /// </summary>
    public int IsValidTrueCallsRemaining { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="InsertOrReplace{T}"/> should throw,
    /// used to exercise the inner catch path inside typed <c>Insert</c>.
    /// </summary>
    public bool FailInsertOrReplace { get; set; }

    /// <inheritdoc/>
    public void InsertOrReplace<T>(T entity)
        where T : new()
    {
        if (FailInsertOrReplace)
        {
            throw new InvalidOperationException("Simulated insert failure.");
        }

        if (entity is CacheEntry entry && entry.Id is not null)
        {
            _store[entry.Id] = entry;
        }
    }

    /// <inheritdoc/>
    public void Delete<T>(object primaryKey)
    {
        if (primaryKey is string key)
        {
            _store.TryRemove(key, out _);
        }
    }

    /// <inheritdoc/>
    public void Execute(string sql, params object?[] args)
    {
        // No-op for in-memory store.
    }

    /// <inheritdoc/>
    public List<T> Query<T>(Expression<Func<T, bool>> predicate)
        where T : new()
    {
        if (typeof(T) != typeof(CacheEntry))
        {
            return [];
        }

        var compiled = predicate.Compile();
        return _store.Values
            .Cast<T>()
            .Where(compiled)
            .ToList();
    }

    /// <inheritdoc/>
    public void SetExpiry(string key, string? typeFullName, DateTime? expiresAt)
    {
        if (!_store.TryGetValue(key, out var entry))
        {
            return;
        }

        if (typeFullName is not null && entry.TypeName != typeFullName)
        {
            return;
        }

        entry.ExpiresAt = expiresAt;
    }
}
