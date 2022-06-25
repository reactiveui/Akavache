// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

using Akavache.Sqlite3.Internal;

using SQLitePCL;

namespace Akavache.Sqlite3;

internal class DeleteExpiredSqliteOperation : IPreparedSqliteOperation
{
    private readonly sqlite3_stmt _deleteOp;
    private readonly IScheduler _scheduler;
    private IDisposable _inner;

    public DeleteExpiredSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
    {
        var deleteResult = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "DELETE FROM CacheElement WHERE Expiration < ?", out _deleteOp);
        Connection = conn;

        if (deleteResult != SQLite3.Result.OK)
        {
            throw new SQLiteException(deleteResult, "Couldn't prepare delete statement");
        }

        _scheduler = scheduler;
        _inner = _deleteOp;
    }

    public SQLiteConnection Connection { get; protected set; }

    public Action PrepareToExecute()
    {
        var now = _scheduler.Now.UtcTicks;

        return () =>
        {
            try
            {
                this.Checked(raw.sqlite3_bind_int64(_deleteOp, 1, now));
                this.Checked(raw.sqlite3_step(_deleteOp));
            }
            finally
            {
                this.Checked(raw.sqlite3_reset(_deleteOp));
            }
        };
    }

    public void Dispose() => Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
}