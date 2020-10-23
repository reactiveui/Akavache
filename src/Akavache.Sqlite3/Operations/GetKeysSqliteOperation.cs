// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using Akavache.Sqlite3.Internal;
using SQLitePCL;

namespace Akavache.Sqlite3
{
    internal class GetKeysSqliteOperation : IPreparedSqliteOperation
    {
        private readonly sqlite3_stmt _selectOp;
        private readonly IScheduler _scheduler;
        private IDisposable _inner;

        public GetKeysSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "SELECT Key FROM CacheElement WHERE Expiration >= ?", out _selectOp);
            Connection = conn;

            if (result != SQLite3.Result.OK)
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            _inner = _selectOp;
            _scheduler = scheduler;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Func<IEnumerable<string>> PrepareToExecute()
        {
            return () =>
            {
                var result = new List<string>();
                try
                {
                    this.Checked(raw.sqlite3_bind_int64(_selectOp, 1, _scheduler.Now.UtcTicks));

                    while (this.Checked(raw.sqlite3_step(_selectOp)) == SQLite3.Result.Row)
                    {
                        result.Add(raw.sqlite3_column_text(_selectOp, 0).utf8_to_string());
                    }
                }
                finally
                {
                    this.Checked(raw.sqlite3_reset(_selectOp));
                }

                return result;
            };
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
        }
    }
}
