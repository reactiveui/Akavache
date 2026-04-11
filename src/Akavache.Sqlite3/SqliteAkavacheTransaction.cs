// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq.Expressions;

using SQLite;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// SQLite implementation of <see cref="IAkavacheTransaction"/> that wraps a synchronous
/// <see cref="SQLiteConnection"/> received inside a <c>RunInTransactionAsync</c> callback.
/// </summary>
internal sealed class SqliteAkavacheTransaction : IAkavacheTransaction
{
    private readonly SQLiteConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteAkavacheTransaction"/> class.
    /// </summary>
    /// <param name="connection">The synchronous SQLite connection provided by the transaction scope.</param>
    public SqliteAkavacheTransaction(SQLiteConnection connection) =>
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    /// <inheritdoc/>
    public bool IsValid => _connection.Handle != null;

    /// <inheritdoc/>
    public void InsertOrReplace<T>(T entity)
        where T : new() =>
        _connection.InsertOrReplace(entity);

    /// <inheritdoc/>
    public void Delete<T>(object primaryKey) =>
        _connection.Delete<T>(primaryKey);

    /// <inheritdoc/>
    public void Execute(string sql, params object?[] args) =>
        _connection.Execute(sql, args);

    /// <inheritdoc/>
    public List<T> Query<T>(Expression<Func<T, bool>> predicate)
        where T : new() =>
        _connection.Table<T>().Where(predicate).ToList();

    /// <inheritdoc/>
    public void SetExpiry(string key, string? typeFullName, DateTime? expiresAt)
    {
        if (typeFullName is null)
        {
            _connection.Execute("UPDATE CacheEntry SET ExpiresAt = ? WHERE Id = ?", expiresAt, key);
        }
        else
        {
            _connection.Execute("UPDATE CacheEntry SET ExpiresAt = ? WHERE Id = ? AND TypeName = ?", expiresAt, key, typeFullName);
        }
    }
}
