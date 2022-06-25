// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

using Akavache.Sqlite3.Internal;

using SQLitePCL;

namespace Akavache.Sqlite3;

internal class BulkInsertSqliteOperation : IPreparedSqliteOperation
{
    private readonly sqlite3_stmt _insertOp;
    private IDisposable _inner;

    public BulkInsertSqliteOperation(SQLiteConnection conn)
    {
        var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "INSERT OR REPLACE INTO CacheElement VALUES (?,?,?,?,?)", out _insertOp);
        Connection = conn;

        if (result != SQLite3.Result.OK)
        {
            throw new SQLiteException(result, "Couldn't prepare statement");
        }

        _inner = _insertOp;
    }

    public SQLiteConnection Connection { get; protected set; }

    public Action PrepareToExecute(IEnumerable<CacheElement>? toInsert)
    {
        var insertList = toInsert?.ToList() ?? Enumerable.Empty<CacheElement>();

        return () =>
        {
            foreach (var v in insertList)
            {
                try
                {
                    this.Checked(raw.sqlite3_bind_text(_insertOp, 1, v.Key));

                    this.Checked(string.IsNullOrWhiteSpace(v.TypeName)
                        ? raw.sqlite3_bind_null(_insertOp, 2)
                        : raw.sqlite3_bind_text(_insertOp, 2, v.TypeName));

                    this.Checked(raw.sqlite3_bind_blob(_insertOp, 3, v.Value));
                    this.Checked(raw.sqlite3_bind_int64(_insertOp, 4, v.Expiration.Ticks));
                    this.Checked(raw.sqlite3_bind_int64(_insertOp, 5, v.CreatedAt.Ticks));

                    this.Checked(raw.sqlite3_step(_insertOp));
                }
                finally
                {
                    this.Checked(raw.sqlite3_reset(_insertOp));
                }
            }
        };
    }

    public void Dispose() => Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
}
