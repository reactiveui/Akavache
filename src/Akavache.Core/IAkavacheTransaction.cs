// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Akavache;

/// <summary>
/// Abstracts the synchronous operations available inside a transactional scope.
/// Implementations wrap a specific storage backend's synchronous connection
/// (e.g., sqlite-net's <c>SQLiteConnection</c>).
/// </summary>
public interface IAkavacheTransaction
{
    /// <summary>
    /// Gets a value indicating whether the underlying connection is still valid and open.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Inserts or replaces a single entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert or replace.</param>
    void InsertOrReplace<T>(T entity)
        where T : new();

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="primaryKey">The primary key value.</param>
    void Delete<T>(object primaryKey);

    /// <summary>
    /// Executes a raw SQL statement with optional parameters.
    /// </summary>
    /// <param name="sql">The SQL statement.</param>
    /// <param name="args">The parameters.</param>
    void Execute(string sql, params object?[] args);

    /// <summary>
    /// Queries all entities of type <typeparamref name="T"/> matching the given predicate synchronously.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A filter expression.</param>
    /// <returns>A list of matching entities.</returns>
    List<T> Query<T>(Expression<Func<T, bool>> predicate)
        where T : new();

    /// <summary>
    /// Updates the expiration time of a cache entry by key, with an optional type filter.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="typeFullName">
    /// Optional type full name. When non-<c>null</c>, only entries whose <c>TypeName</c>
    /// column matches are updated.
    /// </param>
    /// <param name="expiresAt">
    /// The new expiration time, or <c>null</c> to clear the expiration (i.e. never expire).
    /// </param>
    void SetExpiry(string key, string? typeFullName, DateTime? expiresAt);
}
