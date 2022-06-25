// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

using Akavache.Sqlite3.Internal;

using SQLitePCL;

namespace Akavache.Sqlite3;

internal class BeginTransactionSqliteOperation : IPreparedSqliteOperation
{
    private readonly sqlite3_stmt _beginOp;
    private IDisposable _inner;

    public BeginTransactionSqliteOperation(SQLiteConnection conn)
    {
        var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "BEGIN TRANSACTION", out _beginOp);
        Connection = conn;

        if (result != SQLite3.Result.OK)
        {
            throw new SQLiteException(result, "Couldn't prepare statement");
        }

        _inner = _beginOp;
    }

    public SQLiteConnection Connection { get; protected set; }

    public Action PrepareToExecute() =>
        () =>
        {
            try
            {
                this.Checked(raw.sqlite3_step(_beginOp));
            }
            finally
            {
                this.Checked(raw.sqlite3_reset(_beginOp));
            }
        };

    public void Dispose() => Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
}