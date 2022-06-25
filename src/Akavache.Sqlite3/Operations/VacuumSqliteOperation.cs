// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

using Akavache.Sqlite3.Internal;

using SQLitePCL;

namespace Akavache.Sqlite3;

internal class VacuumSqliteOperation : IPreparedSqliteOperation
{
    private readonly sqlite3_stmt _vacuumOp;
    private readonly IScheduler _scheduler;
    private IDisposable _inner;

    public VacuumSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
    {
        var vacuumResult = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "VACUUM", out _vacuumOp);
        Connection = conn;

        if (vacuumResult != SQLite3.Result.OK)
        {
            throw new SQLiteException(vacuumResult, "Couldn't prepare vacuum statement");
        }

        _scheduler = scheduler;
        _inner = _vacuumOp;
    }

    public SQLiteConnection Connection { get; protected set; }

    public Action PrepareToExecute()
    {
        var now = _scheduler.Now.UtcTicks;

        return () =>
        {
            try
            {
                this.Checked(raw.sqlite3_step(_vacuumOp));
            }
            finally
            {
                this.Checked(raw.sqlite3_reset(_vacuumOp));
            }
        };
    }

    public void Dispose() => Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
}