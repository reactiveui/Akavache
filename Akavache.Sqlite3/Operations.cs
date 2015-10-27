using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3.Internal;
using Splat;
using SQLitePCL;

namespace Akavache.Sqlite3
{
    static class Constants
    {
        public const int OperationQueueChunkSize = 64;
    }

    enum OperationType 
    {
        DoNothing,
        BulkSelectSqliteOperation,
        BulkSelectByTypeSqliteOperation,
        BulkInsertSqliteOperation,
        BulkInvalidateSqliteOperation,
        BulkInvalidateByTypeSqliteOperation,
        InvalidateAllSqliteOperation,
        VacuumSqliteOperation,
        DeleteExpiredSqliteOperation,
        GetKeysSqliteOperation,
    }

    interface IPreparedSqliteOperation : IEnableLogger, IDisposable 
    {
        SQLiteConnection Connection { get; }
    }

    class BulkInsertSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt insertOp = null;
        IDisposable inner;

        public BulkInsertSqliteOperation(SQLiteConnection conn)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "INSERT OR REPLACE INTO CacheElement VALUES (?,?,?,?,?)", out insertOp);
            Connection = conn;

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = insertOp;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute(IEnumerable<CacheElement> toInsert)
        {
            var insertList = toInsert.ToList();

            return () => 
            {
                foreach (var v in insertList) 
                {
                    try 
                    {
                        this.Checked(raw.sqlite3_bind_text(insertOp, 1, v.Key));

                        if (String.IsNullOrWhiteSpace(v.TypeName)) 
                        {
                            this.Checked(raw.sqlite3_bind_null(insertOp, 2));
                        } 
                        else 
                        {
                            this.Checked(raw.sqlite3_bind_text(insertOp, 2, v.TypeName));
                        }

                        this.Checked(raw.sqlite3_bind_blob(insertOp, 3, v.Value));
                        this.Checked(raw.sqlite3_bind_int64(insertOp, 4, v.Expiration.Ticks));
                        this.Checked(raw.sqlite3_bind_int64(insertOp, 5, v.CreatedAt.Ticks));

                        this.Checked(raw.sqlite3_step(insertOp));
                    } 
                    finally 
                    {
                        this.Checked(raw.sqlite3_reset(insertOp));
                    }
                }
            };
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    class BulkSelectSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt[] selectOps = null;
        IDisposable inner;
        IScheduler sched;

        public BulkSelectSqliteOperation(SQLiteConnection conn, bool useTypeInsteadOfKey, IScheduler scheduler)
        {
            var qs = new StringBuilder("?");
            var column = useTypeInsteadOfKey ? "TypeName" : "Key";
            Connection = conn;
            sched = scheduler;

            selectOps = Enumerable.Range(1, Constants.OperationQueueChunkSize)
                .Select(x => {
                    var stmt = default(sqlite3_stmt);
                    var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle,
                        String.Format("SELECT Key,TypeName,Value,Expiration,CreatedAt FROM CacheElement WHERE {0} In ({1})", column, qs), out stmt);

                    var error = raw.sqlite3_errmsg(conn.Handle);
                    if (result != SQLite3.Result.OK) throw new SQLiteException(result, "Couldn't prepare statement: " + error);

                    qs.Append(",?");
                    return stmt;
                })
                .ToArray();

            inner = new CompositeDisposable(selectOps);
        }

        public SQLiteConnection Connection { get; protected set; }

        public Func<IEnumerable<CacheElement>> PrepareToExecute(IEnumerable<string> toSelect)
        {
            var selectList = toSelect.ToList();
            if (selectList.Count == 0) return () => new List<CacheElement>();

            var selectOp = selectOps[selectList.Count - 1];
            var now = sched.Now;

            return (() => 
            {
                var result = new List<CacheElement>();
                try 
                {
                    for (int i = 0; i < selectList.Count; i++) 
                    {
                        this.Checked(raw.sqlite3_bind_text(selectOp, i+1, selectList[i]));
                    }

                    while (this.Checked(raw.sqlite3_step(selectOp)) == SQLite3.Result.Row) 
                    {
                        var ce = new CacheElement() {
                            Key = raw.sqlite3_column_text(selectOp, 0), 
                            TypeName = raw.sqlite3_column_text(selectOp, 1), 
                            Value = raw.sqlite3_column_blob(selectOp, 2),
                            Expiration = new DateTime(raw.sqlite3_column_int64(selectOp, 3)),
                            CreatedAt = new DateTime(raw.sqlite3_column_int64(selectOp, 4)),
                        };

                        if (now.UtcTicks <= ce.Expiration.Ticks) result.Add(ce);
                    }
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(selectOp));
                }

                return result;
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    // NB: This just makes OperationQueue's life easier by giving it a type
    // name.
    class BulkSelectByTypeSqliteOperation : BulkSelectSqliteOperation
    {
        public BulkSelectByTypeSqliteOperation(SQLiteConnection conn, IScheduler sched) : base(conn, true, sched) { }
    }

    class BulkInvalidateSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt[] deleteOps = null;
        IDisposable inner;

        public BulkInvalidateSqliteOperation(SQLiteConnection conn, bool useTypeInsteadOfKey)
        {
            var qs = new StringBuilder("?");
            Connection = conn;

            var column = useTypeInsteadOfKey ? "TypeName" : "Key";
            deleteOps = Enumerable.Range(1, Constants.OperationQueueChunkSize)
                .Select(x => {
                    var stmt = default(sqlite3_stmt);
                    var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle,
                        String.Format("DELETE FROM CacheElement WHERE {0} In ({1})", column, qs), out stmt);

                    if (result != SQLite3.Result.OK) throw new SQLiteException(result, "Couldn't prepare statement");

                    qs.Append(",?");
                    return stmt;
                })
                .ToArray();

            inner = new CompositeDisposable(deleteOps);
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute(IEnumerable<string> toDelete)
        {
            var deleteList = toDelete.ToList();
            if (deleteList.Count == 0) return new Action(() => {});

            var deleteOp = deleteOps[deleteList.Count - 1];
            return new Action(() => 
            {
                try 
                {
                    for (int i = 0; i < deleteList.Count; i++) 
                    {
                        this.Checked(raw.sqlite3_bind_text(deleteOp, i+1, deleteList[i]));
                    }

                    this.Checked(raw.sqlite3_step(deleteOp));
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(deleteOp));
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    // NB: This just makes OperationQueue's life easier by giving it a type
    // name.
    class BulkInvalidateByTypeSqliteOperation : BulkInvalidateSqliteOperation
    {
        public BulkInvalidateByTypeSqliteOperation(SQLiteConnection conn) : base(conn, true) { }
    }

    class InvalidateAllSqliteOperation : IPreparedSqliteOperation
    {
        SQLiteConnection conn;

        public InvalidateAllSqliteOperation(SQLiteConnection conn)
        {
            this.conn = conn;
            Connection = conn;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute()
        {
            return () => this.Checked(raw.sqlite3_exec(conn.Handle, "DELETE FROM CacheElement"));
        }

        public void Dispose() { }
    }

    class VacuumSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt vacuumOp = null;
        IScheduler scheduler;
        IDisposable inner;

        public VacuumSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var vacuumResult = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "VACUUM", out vacuumOp);
            Connection = conn;

            if (vacuumResult != SQLite3.Result.OK)
            {
                throw new SQLiteException(vacuumResult, "Couldn't prepare vacuum statement");
            }

            this.scheduler = scheduler;
            inner = vacuumOp;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute()
        {
            var now = scheduler.Now.UtcTicks;

            return new Action(() => 
            {
                try 
                {
                    this.Checked(raw.sqlite3_step(vacuumOp));
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(vacuumOp));
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    class DeleteExpiredSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt deleteOp = null;
        IScheduler scheduler;
        IDisposable inner;

        public DeleteExpiredSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var deleteResult = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "DELETE FROM CacheElement WHERE Expiration < ?", out deleteOp);
            Connection = conn;

            if (deleteResult != SQLite3.Result.OK)
            {
                throw new SQLiteException(deleteResult, "Couldn't prepare delete statement");
            }

            this.scheduler = scheduler;
            inner = deleteOp;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute()
        {
            var now = scheduler.Now.UtcTicks;

            return new Action(() =>
            {
                try
                {
                    this.Checked(raw.sqlite3_bind_int64(deleteOp, 1, now));
                    this.Checked(raw.sqlite3_step(deleteOp));
                }
                finally
                {
                    this.Checked(raw.sqlite3_reset(deleteOp));
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    class BeginTransactionSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt beginOp = null;
        IDisposable inner;

        public BeginTransactionSqliteOperation(SQLiteConnection conn)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "BEGIN TRANSACTION", out beginOp);
            Connection = conn;

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = beginOp;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute()
        {
            return new Action(() => 
            {
                try 
                {
                    this.Checked(raw.sqlite3_step(beginOp));
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(beginOp));
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    class CommitTransactionSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt commitOp = null;
        IDisposable inner;

        public CommitTransactionSqliteOperation(SQLiteConnection conn)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "COMMIT TRANSACTION", out commitOp);
            Connection = conn;

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = commitOp;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Action PrepareToExecute()
        {
            return new Action(() => 
            {
                try 
                {
                    this.Checked(raw.sqlite3_step(commitOp));
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(commitOp));
                }
            });
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    class GetKeysSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt selectOp = null;
        IScheduler scheduler;
        IDisposable inner;

        public GetKeysSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "SELECT Key FROM CacheElement WHERE Expiration >= ?", out selectOp);
            Connection = conn;

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = selectOp;
            this.scheduler = scheduler;
        }

        public SQLiteConnection Connection { get; protected set; }

        public Func<IEnumerable<string>> PrepareToExecute()
        {
            return () => 
            {
                var result = new List<string>();
                try 
                {
                    this.Checked(raw.sqlite3_bind_int64(selectOp, 1, scheduler.Now.UtcTicks));

                    while (this.Checked(raw.sqlite3_step(selectOp)) == SQLite3.Result.Row) 
                    {
                        result.Add(raw.sqlite3_column_text(selectOp, 0));
                    }
                } 
                finally 
                {
                    this.Checked(raw.sqlite3_reset(selectOp));
                }

                return result;
            };
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref inner, Disposable.Empty).Dispose();
        }
    }

    static class SqliteOperationMixin
    {
        public static SQLite3.Result Checked(this IPreparedSqliteOperation This, int sqlite3ErrorCode, string message = null)
        {
            var result = (SQLite3.Result)sqlite3ErrorCode;
            if (result == SQLite3.Result.OK || result == SQLite3.Result.Done || result == SQLite3.Result.Row) return result;

            var err = raw.sqlite3_errmsg(This.Connection.Handle);
            var ex = new SQLiteException(result, (message ?? "") + ": " + err);

            This.Log().WarnException(message, ex);
            throw ex;
        }
    }
}