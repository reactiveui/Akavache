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
using ReactiveUI;
using Akavache;

namespace SQLite
{
    public interface IAsyncTableQuery<T> where T : new()
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
        KeyedOperationQueue _opQueue;
        SQLiteConnectionPool _pool;
        SQLiteOpenFlags _flags;

        public SQLiteAsyncConnection (string databasePath, SQLiteConnectionPool pool, SQLiteOpenFlags? flags = null, bool storeDateTimeAsTicks = false, IScheduler scheduler = null)
        {
            _flags = flags ?? (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.SharedCache);

            _connectionString = new SQLiteConnectionString (databasePath, storeDateTimeAsTicks);
            _opQueue = new KeyedOperationQueue(scheduler ?? RxApp.TaskpoolScheduler);
            _pool = pool;
        }

        SQLiteConnectionWithoutLock GetConnection ()
        {
            return _pool.GetConnection (_connectionString, _flags);
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
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                CreateTablesResult result = new CreateTablesResult ();
                var conn = GetConnection ();

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
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.DropTable<T> ();
            });
        }

        public IObservable<int> InsertAsync (object item)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Insert (item);
            });
        }

        public IObservable<int> InsertAsync (object item, string extra, Type type)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Insert (item, extra, type);
            });
        }


        public IObservable<int> UpdateAsync (object item)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Update (item);
            });
        }

        public IObservable<int> DeleteAsync (object item)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Delete (item);
            });
        }

        public IObservable<T> GetAsync<T>(object pk)
            where T : new()
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection();
                return conn.Get<T>(pk);
            });
        }

        public IObservable<T> FindAsync<T> (object pk)
            where T : new ()
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Find<T> (pk);
            });
        }
        
        public IObservable<T> GetAsync<T> (Expression<Func<T, bool>> predicate)
            where T : new()
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () =>
            {
                var conn = GetConnection();
                return conn.Get<T> (predicate);
            });
        }

        public IObservable<T> FindAsync<T> (Expression<Func<T, bool>> predicate)
            where T : new ()
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Find<T> (predicate);
            });
        }

        public IObservable<int> ExecuteAsync (string query, params object[] args)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Execute (query, args);
            });
        }

        public IObservable<int> InsertAllAsync (IEnumerable items)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.InsertAll (items);
            });
        }

        public IObservable<Unit>RunInTransactionAsync(Action<SQLiteConnection> action)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = this.GetConnection();

                conn.BeginTransaction();
                try {
                    action(conn);
                    conn.Commit();
                } catch (Exception) {
                    conn.Rollback();
                    throw;
                }
            });
        }

        public IAsyncTableQuery<T> Table<T> ()
            where T : new ()
        {
            //
            // This isn't async as the underlying connection doesn't go out to the database
            // until the query is performed. The Async methods are on the query iteself.
            //
            var conn = GetConnection ();
            return new AsyncTableQuery<T> (conn.Table<T> (), _opQueue, _connectionString);
        }

        public IObservable<T> ExecuteScalarAsync<T> (string sql, params object[] args)
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                var command = conn.CreateCommand (sql, args);
                return command.ExecuteScalar<T> ();
            });
        }

        public IObservable<List<T>> QueryAsync<T> (string sql, params object[] args)
            where T : new ()
        {
            return _opQueue.EnqueueOperation(_connectionString.DatabasePath, () => {
                var conn = GetConnection ();
                return conn.Query<T> (sql, args);
            });
        }

        public IObservable<Unit> Shutdown()
        {
            return _opQueue.ShutdownQueue();
        }

        //
        // TODO: Bind to AsyncConnection.GetConnection instead so that delayed
        // execution can still work after a Pool.Reset.
        //
        class AsyncTableQuery<T> : IAsyncTableQuery<T> where T : new ()
        {
            TableQuery<T> _innerQuery;
            KeyedOperationQueue _opQueue;
            SQLiteConnectionString _connString;

            public AsyncTableQuery (TableQuery<T> innerQuery, KeyedOperationQueue opQueue, SQLiteConnectionString connString)
            {
                _innerQuery = innerQuery;
                _opQueue = opQueue;
                _connString = connString;
            }

            public IAsyncTableQuery<T> Where (Expression<Func<T, bool>> predExpr)
            {
                return new AsyncTableQuery<T> (_innerQuery.Where (predExpr), _opQueue, _connString);
            }

            public IAsyncTableQuery<T> Skip (int n)
            {
                return new AsyncTableQuery<T> (_innerQuery.Skip (n), _opQueue, _connString);
            }

            public IAsyncTableQuery<T> Take (int n)
            {
                return new AsyncTableQuery<T> (_innerQuery.Take (n), _opQueue, _connString);
            }

            public IAsyncTableQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr)
            {
                return new AsyncTableQuery<T> (_innerQuery.OrderBy<U> (orderExpr), _opQueue, _connString);
            }

            public IAsyncTableQuery<T> OrderByDescending<U> (Expression<Func<T, U>> orderExpr)
            {
                return new AsyncTableQuery<T> (_innerQuery.OrderByDescending<U> (orderExpr), _opQueue, _connString);
            }

            public IObservable<List<T>> ToListAsync ()
            {
                return _opQueue.EnqueueOperation(_connString.DatabasePath, () => {
                    return _innerQuery.ToList ();
                });
            }

            public IObservable<int> CountAsync ()
            {
                return _opQueue.EnqueueOperation(_connString.DatabasePath, () => {
                    return _innerQuery.Count ();
                });
            }

            public IObservable<T> ElementAtAsync (int index)
            {
                return _opQueue.EnqueueOperation(_connString.DatabasePath, () => {
                    return _innerQuery.ElementAt (index);
                });
            }

            public IObservable<T> FirstAsync ()
            {
                return _opQueue.EnqueueOperation(_connString.DatabasePath, () => {
                    return _innerQuery.First ();
                });
            }

            public IObservable<T> FirstOrDefaultAsync ()
            {
                return _opQueue.EnqueueOperation(_connString.DatabasePath, () => {
                    return _innerQuery.FirstOrDefault ();
                });
            }
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

    public class SQLiteConnectionPool
    {
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

        readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry> ();
        readonly object _entriesLock = new object ();

        public SQLiteConnectionWithoutLock GetConnection (SQLiteConnectionString connectionString, SQLiteOpenFlags flags)
        {
            lock (_entriesLock) {
                Entry entry;
                string key = connectionString.ConnectionString;

                if (!_entries.TryGetValue (key, out entry)) {
                    entry = new Entry (connectionString, flags);
                    _entries[key] = entry;
                }

                return entry.Connection;
            }
        }

        /// <summary>
        /// Closes all connections managed by this pool.
        /// </summary>
        public void Reset ()
        {
            lock (_entriesLock) {
                foreach (var entry in _entries.Values) {
                    entry.OnApplicationSuspended ();
                }

                _entries.Clear ();
            }
        }

        /// <summary>
        /// Call this method when the application is suspended.
        /// </summary>
        /// <remarks>Behaviour here is to close any open connections.</remarks>
        public void ApplicationSuspended ()
        {
            Reset ();
        }
    }

    public class SQLiteConnectionWithoutLock : SQLiteConnection
    {
        public SQLiteConnectionWithoutLock (SQLiteConnectionString connectionString, SQLiteOpenFlags flags)
            : base (connectionString.DatabasePath, flags, connectionString.StoreDateTimeAsTicks)
        {
        }
    }
}

