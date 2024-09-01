// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using SQLite;
using SQLitePCL;

namespace Akavache.Sqlite3;

[SuppressMessage("Design", "CA2213: Non disposed field", Justification = "Disposed, just as part of interlock.")]
internal class BulkSelectSqliteOperation : IPreparedSqliteOperation
{
    private readonly sqlite3_stmt[] _selectOps;
    private readonly IScheduler _scheduler;
    private IDisposable _inner;

    public BulkSelectSqliteOperation(SQLiteConnection conn, bool useTypeInsteadOfKey, IScheduler scheduler)
    {
        var qs = new StringBuilder("?");
        var column = useTypeInsteadOfKey ? "TypeName" : "Key";
        Connection = conn;
        _scheduler = scheduler;

        _selectOps = Enumerable.Range(1, Constants.OperationQueueChunkSize)
            .Select(_ =>
            {
                var result = (SQLite3.Result)raw.sqlite3_prepare_v2(
                    conn.Handle,
                    $"SELECT Key,TypeName,Value,Expiration,CreatedAt FROM CacheElement WHERE {column} In ({qs})",
                    out var stmt);

                var error = raw.sqlite3_errmsg(conn.Handle).utf8_to_string();
                if (result != SQLite3.Result.OK)
                {
                    throw SQLiteException.New(result, "Couldn't prepare statement: " + error);
                }

                qs.Append(",?");
                return stmt;
            })
            .ToArray();

        _inner = new CompositeDisposable(_selectOps.OfType<IDisposable>().ToArray());
    }

    public SQLiteConnection Connection { get; protected set; }

    public Func<IEnumerable<CacheElement>> PrepareToExecute(IEnumerable<string>? toSelect)
    {
        var selectList = (toSelect ?? []).ToList();
        if (selectList.Count == 0)
        {
            return () => [];
        }

        var selectOp = _selectOps[selectList.Count - 1];
        var now = _scheduler.Now;

        return () =>
        {
            var result = new List<CacheElement>();
            try
            {
                for (var i = 0; i < selectList.Count; i++)
                {
                    this.Checked(raw.sqlite3_bind_text(selectOp, i + 1, selectList[i]));
                }

                while (this.Checked(raw.sqlite3_step(selectOp)) == SQLite3.Result.Row)
                {
                    var ce = new CacheElement
                    {
                        Key = raw.sqlite3_column_text(selectOp, 0).utf8_to_string(),
                        TypeName = raw.sqlite3_column_text(selectOp, 1).utf8_to_string(),
                        Value = raw.sqlite3_column_blob(selectOp, 2).ToArray(),
                        Expiration = new(raw.sqlite3_column_int64(selectOp, 3)),
                        CreatedAt = new(raw.sqlite3_column_int64(selectOp, 4)),
                    };

                    if (now.UtcTicks <= ce.Expiration.Ticks)
                    {
                        result.Add(ce);
                    }
                }
            }
            finally
            {
                this.Checked(raw.sqlite3_reset(selectOp));
            }

            return result;
        };
    }

    public void Dispose() => Interlocked.Exchange(ref _inner, Disposable.Empty).Dispose();
}
