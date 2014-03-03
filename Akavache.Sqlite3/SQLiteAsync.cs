//
// Copyright (c) 2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3;
using Akavache;

namespace Akavache.Sqlite3.Internal
{
    internal interface IAsyncTableQuery<T> where T : new()
    {
        IAsyncTableQuery<T> Where (Expression<Func<T, bool>> predExpr);
        IAsyncTableQuery<T> Skip (int n);
        IAsyncTableQuery<T> Take (int n);
        IAsyncTableQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr);
        IAsyncTableQuery<T> OrderByDescending<U> (Expression<Func<T, U>> orderExpr);
        IObservable<List<T>> ToListAsync ();
        IObservable<int> CountAsync ();
        IObservable<T> ElementAtAsync (int index);
        IObservable<T> FirstAsync ();
        IObservable<T> FirstOrDefaultAsync ();
    }

    public class SQLiteAsyncConnection
    {
        SQLiteConnectionString _connectionString;
        SQLiteConnectionPool _pool;
        SQLiteOpenFlags _flags;

        public SQLiteAsyncConnection (string databasePath, SQLiteOpenFlags? flags = null, bool storeDateTimeAsTicks = false)
        {
            _flags = flags ?? (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.NoMutex | SQLiteOpenFlags.SharedCache);

            _connectionString = new SQLiteConnectionString (databasePath, storeDateTimeAsTicks);
            _pool = new SQLiteConnectionPool(_connectionString, _flags);
        }

        public IObservable<CreateTablesResult> CreateTableAsync<T> ()
            where T : new ()
        {
            return CreateTablesAsync (typeof (T));
        }

        public IObservable<CreateTablesResult> CreateTablesAsync<T, T2> ()
            where T : new ()
            where T2 : new ()
        {
            return CreateTablesAsync (typeof (T), typeof (T2));
        }

        public IObservable<CreateTablesResult> CreateTablesAsync<T, T2, T3> ()
            where T : new ()
            where T2 : new ()
            where T3 : new ()
        {
            return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3));
        }

        public IObservable<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4> ()
            where T : new ()
            where T2 : new ()
            where T3 : new ()
            where T4 : new ()
        {
            return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3), typeof (T4));
        }

        public IObservable<CreateTablesResult> CreateTablesAsync<T, T2, T3, T4, T5> ()
            where T : new ()
            where T2 : new ()
            where T3 : new ()
            where T4 : new ()
            where T5 : new ()
        {
            return CreateTablesAsync (typeof (T), typeof (T2), typeof (T3), typeof (T4), typeof (T5));
        }

        public IObservable<CreateTablesResult> CreateTablesAsync (params Type[] types)
        {
            return _pool.EnqueueConnectionOp(conn => {
                var result = new CreateTablesResult ();

                foreach (Type type in types) {
                    int aResult = conn.CreateTable (type);
                    result.Results[type] = aResult;
                }
                return result;
            });
        }

        public IObservable<int> DropTableAsync<T> ()
            where T : new ()
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.DropTable<T>();
            });
        }

        public IObservable<int> InsertAsync (object item)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Insert (item);
            });
        }

        public IObservable<int> InsertAsync (object item, string extra, Type type)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Insert (item, extra, type);
            });
        }


        public IObservable<int> UpdateAsync (object item)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Update (item);
            });
        }

        public IObservable<int> DeleteAsync (object item)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Delete (item);
            });
        }

        public IObservable<T> GetAsync<T>(object pk)
            where T : new()
        {
            return _pool.EnqueueConnectionOp<T>(conn => {
                return conn.Get<T>(pk);
            });
        }

        public IObservable<T> FindAsync<T> (object pk)
            where T : new ()
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Find<T> (pk);
            });
        }
        
        public IObservable<T> GetAsync<T> (Expression<Func<T, bool>> predicate)
            where T : new()
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Get<T> (predicate);
            });
        }

        public IObservable<T> FindAsync<T> (Expression<Func<T, bool>> predicate)
            where T : new ()
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Find<T> (predicate);
            });
        }

        public IObservable<int> ExecuteAsync (string query, params object[] args)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Execute (query, args);
            });
        }

        public IObservable<int> InsertAllAsync (IEnumerable items, string extra)
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.InsertAll (items, extra);
            });
        }

        public IObservable<Unit> RunInTransactionAsync(Action<SQLiteConnection> action)
        {
            return _pool.EnqueueConnectionOp(conn => {
                conn.BeginTransaction();
                try {
                    action(conn);
                    conn.Commit();
                    return Unit.Default;
                } catch (Exception) {
                    conn.Rollback();
                    throw;
                }
            });
        }

        public IObservable<T> ExecuteScalarAsync<T> (string sql, params object[] args)
        {
            return _pool.EnqueueConnectionOp(conn => {
                var command = conn.CreateCommand (sql, args);
                return command.ExecuteScalar<T> ();
            });
        }

        public IObservable<List<T>> QueryAsync<T> (string sql, params object[] args)
            where T : new ()
        {
            return _pool.EnqueueConnectionOp(conn => {
                return conn.Query<T> (sql, args);
            });
        }

        public IObservable<Unit> Shutdown()
        {
            return _pool.Reset(false);
        }
    }

    public class CreateTablesResult
    {
        public Dictionary<Type, int> Results { get; private set; }

        internal CreateTablesResult ()
        {
            this.Results = new Dictionary<Type, int> ();
        }
    }

    public class SQLiteConnectionPool : IDisposable
    {
        readonly int connectionCount;
        readonly Tuple<SQLiteConnectionString, SQLiteOpenFlags> connInfo;

        List<Entry> connections;
        KeyedOperationQueue opQueue;
        int nextConnectionToUseAtomic = 0;
        const int tableLockRetries = 3;

        public SQLiteConnectionPool(SQLiteConnectionString connectionString, SQLiteOpenFlags flags, int? connectionCount = null)
        {
            this.connectionCount = connectionCount ?? 4;
            connInfo = Tuple.Create(connectionString, flags);
            Reset().Wait();
        }

        public IObservable<T> EnqueueConnectionOp<T>(Func<SQLiteConnection, T> operation)
        {
            var idx = Interlocked.Increment(ref nextConnectionToUseAtomic) % connectionCount;
            var conn = connections[idx];

            if (connections == null)
            {
                Reset(true).Wait();
            }

            var makeRq = Observable.Defer(() => 
                Observable.Start(() => operation(conn.Connection), BlobCache.TaskpoolScheduler));

            return opQueue.EnqueueObservableOperation(idx.ToString(), () => 
                makeRq.RetryWithBackoffStrategy(tableLockRetries, retryOnError: ex =>
                {
                    var sqlex = ex as SQLiteException;
                    if (sqlex == null)
                        return false;

                    return (sqlex.Result == SQLite3.Result.Locked || sqlex.Result == SQLite3.Result.Busy);
                }));
        }

        /// <summary>
        /// Closes all connections managed by this pool.
        /// </summary>
        public IObservable<Unit> Reset (bool shouldReopen = true)
        {
            var shutdownQueue = Observable.Return(Unit.Default);

            if (opQueue != null)
            {
                shutdownQueue = opQueue.ShutdownQueue();
            }

            return shutdownQueue.Finally(() => 
            {
                if (connections != null)
                {
                    foreach(var v in connections.Where(x => x != null && x.Connection != null))
                    {
                        v.OnApplicationSuspended();
                    }

                    connections = null;
                }

                if (shouldReopen)
                {
                    connections = Enumerable.Range(0, connectionCount)
                        .Select(_ => new Entry(connInfo.Item1, connInfo.Item2))
                        .ToList();

                    opQueue = new KeyedOperationQueue();
                }
            });
        }

        public void Dispose()
        {
            Reset(false).Wait();
        }

        class Entry
        {
            public SQLiteConnectionString ConnectionString { get; private set; }
            public SQLiteConnectionWithoutLock Connection { get; private set; }

            public Entry (SQLiteConnectionString connectionString, SQLiteOpenFlags flags)
            {
                ConnectionString = connectionString;
                Connection = new SQLiteConnectionWithoutLock (connectionString, flags);
            }

            public void OnApplicationSuspended ()
            {
                Connection.Dispose ();
                Connection = null;
            }
        }
    }

    public class SQLiteConnectionWithoutLock : SQLiteConnection
    {
        public SQLiteConnectionWithoutLock (SQLiteConnectionString connectionString, SQLiteOpenFlags flags)
            : base (connectionString.DatabasePath, flags, connectionString.StoreDateTimeAsTicks)
        {
        }
    }

    public static class RetryWithBackoffMixin
    {
        /// <summary>
        /// An exponential back off strategy which starts with 1 second and then 4, 9, 16...
        /// </summary>
        public static readonly Func<int, TimeSpan> ExponentialBackoff = 
            n => TimeSpan.FromMilliseconds(Math.Pow(n, 2) * 20);

        /// <summary>
        /// Returns a cold observable which retries (re-subscribes to) the source observable on error up to the 
        /// specified number of times or until it successfully terminates. Allows for customizable back off strategy.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="retryCount">The number of attempts of running the source observable before failing.</param>
        /// <param name="strategy">The strategy to use in backing off, exponential by default.</param>
        /// <param name="retryOnError">A predicate determining for which exceptions to retry. Defaults to all</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns>
        /// A cold observable which retries (re-subscribes to) the source observable on error up to the 
        /// specified number of times or until it successfully terminates.
        /// </returns>
        public static IObservable<T> RetryWithBackoffStrategy<T>(
            this IObservable<T> source, 
            int retryCount = 3,
            Func<int, TimeSpan> strategy = null,
            Func<Exception, bool> retryOnError = null,
            IScheduler scheduler = null)
        {
            strategy = strategy ?? ExponentialBackoff;
            scheduler = scheduler ?? BlobCache.TaskpoolScheduler;

            if (retryOnError == null) 
            {
                retryOnError = _ => true;
            }

            int attempt = 0;

            return Observable.Defer(() =>
            {
                return ((++attempt == 1) ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => new Tuple<bool, T, Exception>(true, item, null))
                    .Catch<Tuple<bool, T, Exception>, Exception>(e => retryOnError(e)
                        ? Observable.Throw<Tuple<bool, T, Exception>>(e)
                        : Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e)));
            })
            .Retry(retryCount)
            .SelectMany(t => t.Item1
                ? Observable.Return(t.Item2)
                : Observable.Throw<T>(t.Item3));
        }
    }
}

