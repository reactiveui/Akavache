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
        public const int OperationQueueChunkSize = 32;
    }

    interface IPreparedSqliteOperation : IEnableLogger, IDisposable { }

    sealed class BulkInsertSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt insertOp = null;
        IDisposable inner;

        public BulkInsertSqliteOperation(SQLiteConnection conn)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "INSERT OR REPLACE INTO CacheElement VALUES (?,?,?,?,?)", out insertOp);

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = insertOp;
        }

        public Action PrepareToExecute(IEnumerable<CacheElement> toInsert)
        {
            var insertList = toInsert.ToList();

            return () => 
            {
                foreach (var v in insertList) 
                {
                    try 
                    {
                        this.Checked(raw.sqlite3_bind_text(insertOp, 0, v.Key));

                        if (String.IsNullOrWhiteSpace(v.TypeName)) 
                        {
                            this.Checked(raw.sqlite3_bind_null(insertOp, 1));
                        } 
                        else 
                        {
                            this.Checked(raw.sqlite3_bind_text(insertOp, 1, v.TypeName));
                        }

                        this.Checked(raw.sqlite3_bind_text(insertOp, 1, v.TypeName ?? ""));
                        this.Checked(raw.sqlite3_bind_blob(insertOp, 2, v.Value));
                        this.Checked(raw.sqlite3_bind_int64(insertOp, 3, v.Expiration.ToUniversalTime().Ticks));
                        this.Checked(raw.sqlite3_bind_int64(insertOp, 4, v.CreatedAt.ToUniversalTime().Ticks));

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

    sealed class BulkSelectSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt[] selectOps = null;
        IDisposable inner;

        public BulkSelectSqliteOperation(SQLiteConnection conn, bool useTypeInsteadOfKey)
        {
            var qs = new StringBuilder("?");
            var column = useTypeInsteadOfKey ? "TypeName" : "Key";

            selectOps = Enumerable.Range(1, Constants.OperationQueueChunkSize)
                .Select(x => {
                    var stmt = default(sqlite3_stmt);
                    var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle,
                        String.Format("SELECT Value,Expiration FROM CacheElement WHERE {0} In ({1})", column, qs), out stmt);

                    if (result != SQLite3.Result.OK) throw new SQLiteException(result, "Couldn't prepare statement");

                    qs.Append(",?");
                    return stmt;
                })
                .ToArray();

            inner = new CompositeDisposable(selectOps);
        }

        public Func<List<CacheElement>> PrepareToExecute(IEnumerable<string> toSelect)
        {
            var selectList = toSelect.ToList();
            if (selectList.Count == 0) return () => new List<CacheElement>();

            var selectOp = selectOps[selectList.Count - 1];
            return (() => 
            {
                var result = new List<CacheElement>();
                try 
                {
                    for (int i = 0; i < selectList.Count; i++) 
                    {
                        this.Checked(raw.sqlite3_bind_text(selectOp, i, selectList[i]));
                    }

                    int idx = 0;
                    while (this.Checked(raw.sqlite3_step(selectOp)) == SQLite3.Result.Row) 
                    {
                        var key = selectList[idx++];
                        var ce = new CacheElement() {
                            Key = key, TypeName = key,
                            Value = raw.sqlite3_column_blob(selectOp, 1),
                            Expiration = new DateTime(raw.sqlite3_column_int64(selectOp, 2)),
                        };

                        result.Add(ce);
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

    sealed class BulkInvalidateSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt[] deleteOps = null;
        IDisposable inner;

        public BulkInvalidateSqliteOperation(SQLiteConnection conn, bool useTypeInsteadOfKey)
        {
            var qs = new StringBuilder("?");

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
                        this.Checked(raw.sqlite3_bind_text(deleteOp, i, deleteList[i]));
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

    sealed class InvalidateAllSqliteOperation : IPreparedSqliteOperation
    {
        SQLiteConnection conn;
        IDisposable inner;

        public InvalidateAllSqliteOperation(SQLiteConnection conn)
        {
            this.conn = conn;
        }

        public Action PrepareToExecute(IEnumerable<CacheElement> toInsert)
        {
            return () => this.Checked(raw.sqlite3_exec(conn.Handle, "DELETE FROM CacheElement"));
        }

        public void Dispose() { }
    }

    sealed class VacuumSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt vacuumOp = null;
        IScheduler scheduler;
        IDisposable inner;

        public VacuumSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "DELETE FROM CacheElement WHERE Expiration < ?; VACUUM", out vacuumOp);

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            this.scheduler = scheduler;
            inner = vacuumOp;
        }

        public Action PrepareToExecute()
        {
            var now = scheduler.Now.UtcTicks;

            return new Action(() => 
            {
                try 
                {
                    this.Checked(raw.sqlite3_bind_int64(vacuumOp, 0, now));
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

    sealed class GetKeysSqliteOperation : IPreparedSqliteOperation
    {
        sqlite3_stmt selectOp = null;
        IScheduler scheduler;
        IDisposable inner;

        public GetKeysSqliteOperation(SQLiteConnection conn, IScheduler scheduler)
        {
            var result = (SQLite3.Result)raw.sqlite3_prepare_v2(conn.Handle, "SELECT Key FROM CacheElement WHERE Expiration >= ?", out selectOp);

            if (result != SQLite3.Result.OK) 
            {
                throw new SQLiteException(result, "Couldn't prepare statement");
            }

            inner = selectOp;
            this.scheduler = scheduler;
        }

        public Func<List<string>> PrepareToExecute()
        {
            return () => 
            {
                var result = new List<string>();
                try 
                {
                    this.Checked(raw.sqlite3_bind_int64(selectOp, 0, scheduler.Now.UtcTicks));

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

            var ex = new SQLiteException(result, message ?? "");

            This.Log().WarnException(message, ex);
            throw ex;
        }
    }
}