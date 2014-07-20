using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reactive;
using System.Threading;
using Akavache.Sqlite3.Internal;
using SQLitePCL;
using Splat;

using AsyncLock = Akavache.Sqlite3.Internal.AsyncLock;

namespace Akavache.Sqlite3
{
    class SqliteOperationQueue : IEnableLogger, IDisposable
    {
        readonly AsyncLock flushLock = new AsyncLock();
        readonly IScheduler scheduler;

        readonly BulkSelectSqliteOperation bulkSelectKey;
        readonly BulkSelectByTypeSqliteOperation bulkSelectType;
        readonly BulkInsertSqliteOperation bulkInsertKey;
        readonly BulkInvalidateSqliteOperation bulkInvalidateKey;
        readonly BulkInvalidateByTypeSqliteOperation bulkInvalidateType;
        readonly InvalidateAllSqliteOperation invalidateAll;
        readonly VacuumSqliteOperation vacuum;
        readonly GetKeysSqliteOperation getAllKeys;
        readonly BeginTransactionSqliteOperation begin;
        readonly CommitTransactionSqliteOperation commit;

        BlockingCollection<Tuple<OperationType, IEnumerable, object>> operationQueue = 
            new BlockingCollection<Tuple<OperationType, IEnumerable, object>>();

        public SqliteOperationQueue(SQLiteConnection conn, IScheduler scheduler)
        {
            this.scheduler = scheduler;

            bulkSelectKey = new BulkSelectSqliteOperation(conn, false, scheduler);
            bulkSelectType = new BulkSelectByTypeSqliteOperation(conn, scheduler);
            bulkInsertKey = new BulkInsertSqliteOperation(conn);
            bulkInvalidateKey = new BulkInvalidateSqliteOperation(conn, false);
            bulkInvalidateType = new BulkInvalidateByTypeSqliteOperation(conn);
            invalidateAll = new InvalidateAllSqliteOperation(conn);
            vacuum = new VacuumSqliteOperation(conn, scheduler);
            getAllKeys = new GetKeysSqliteOperation(conn, scheduler);
            begin = new BeginTransactionSqliteOperation(conn);
            commit = new CommitTransactionSqliteOperation(conn);
        }

        // NB: This constructor is used for testing operation coalescing,
        // don't actually use it for reals
        internal SqliteOperationQueue()
        {
        }
         
        IDisposable start;
        public IDisposable Start()
        {
            if (start != null) return start;

            bool shouldQuit = false;
            var task = Task.Run(async () => 
            {
                var toProcess = new List<Tuple<OperationType, IEnumerable, object>>();

                while (!shouldQuit) 
                {
                    toProcess.Clear();

                    using (await flushLock.LockAsync()) 
                    {
                        // NB: We special-case the first item because we want to 
                        // in the empty list case, we want to wait until we have an item.
                        // Once we have a single item, we try to fetch as many as possible
                        // until we've got enough items.
                        var item = default(Tuple<OperationType, IEnumerable, object>);
                        if (!operationQueue.TryTake(out item, 2000)) continue;

                        toProcess.Add(item);
                        while (toProcess.Count < Constants.OperationQueueChunkSize && operationQueue.TryTake(out item)) 
                        {
                            toProcess.Add(item);
                        }

                        try 
                        {
                            ProcessItems(CoalesceOperations(toProcess));
                        } 
                        catch (SQLiteException ex) 
                        {
                            // NB: If ProcessItems Failed, it explicitly means 
                            // that the "BEGIN TRANSACTION" failed and that items
                            // have **not** been processed. We should add them back
                            // to the queue
                            foreach (var v in toProcess) operationQueue.Add(v);
                        }
                    }
                }
            });

            return (start = Disposable.Create(() => 
            {
                shouldQuit = true;
                task.Wait();

                var newQueue = new BlockingCollection<Tuple<OperationType, IEnumerable, object>>();
                ProcessItems(CoalesceOperations(Interlocked.Exchange(ref operationQueue, newQueue).ToList()));
                start = null;
            }));
        }

        public IObservable<Unit> Flush()
        {
            var ret = new AsyncSubject<Unit>();

            return Task.Run(async () => 
            {
                using (await flushLock.LockAsync()) 
                {
                    var newQueue = new BlockingCollection<Tuple<OperationType, IEnumerable, object>>();
                    var existingItems = Interlocked.Exchange(ref operationQueue, newQueue).ToList();

                    ProcessItems(CoalesceOperations(existingItems));
                }
            }).ToObservable();
        }

        public AsyncSubject<List<CacheElement>> Select(IEnumerable<string> keys)
        {
            var ret = new AsyncSubject<List<CacheElement>>();
            
            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.BulkSelectSqliteOperation, keys, ret));
            return ret;
        }

        public AsyncSubject<List<CacheElement>> SelectTypes(IEnumerable<string> types)
        {
            var ret = new AsyncSubject<List<CacheElement>>();
            
            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.BulkSelectByTypeSqliteOperation, types, ret));
            return ret;
        }

        public AsyncSubject<Unit> Insert(IEnumerable<CacheElement> items)
        {
            var ret = new AsyncSubject<Unit>();
            
            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.BulkInsertSqliteOperation, items, ret));
            return ret;
        }

        public AsyncSubject<Unit> Invalidate(IEnumerable<string> keys)
        {
            var ret = new AsyncSubject<Unit>();
                
            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.BulkInvalidateSqliteOperation, keys, ret));
            return ret;
        }

        public AsyncSubject<Unit> InvalidateTypes(IEnumerable<string> types)
        {
            var ret = new AsyncSubject<Unit>();

            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.BulkInvalidateByTypeSqliteOperation, types, ret));
            return ret;
        }

        public AsyncSubject<Unit> InvalidateAll()
        {
            var ret = new AsyncSubject<Unit>();
            
            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.InvalidateAllSqliteOperation, null, ret));
            return ret;
        }

        public AsyncSubject<Unit> Vacuum()
        {
            var ret = new AsyncSubject<Unit>();

            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.VacuumSqliteOperation, null, ret));
            return ret;
        }

        public AsyncSubject<List<string>> GetAllKeys()
        {
            var ret = new AsyncSubject<List<string>>();

            operationQueue.Add(new Tuple<OperationType, IEnumerable, object>(OperationType.GetKeysSqliteOperation, null, ret));
            return ret;
        }

        internal List<Tuple<OperationType, IEnumerable, object>> DumpQueue()
        {
            return operationQueue.ToList();
        }

        void ProcessItems(List<Tuple<OperationType, IEnumerable, object>> toProcess)
        {
            var commitResult = new AsyncSubject<Unit>();
            begin.PrepareToExecute()();

            foreach (var item in toProcess) 
            {
                switch (item.Item1) {
                case OperationType.BulkInsertSqliteOperation:
                    MarshalCompletion(item.Item3, bulkInsertKey.PrepareToExecute((IEnumerable<CacheElement>)item.Item2), commitResult);
                    break;
                case OperationType.BulkInvalidateByTypeSqliteOperation:
                    MarshalCompletion(item.Item3, bulkInvalidateType.PrepareToExecute((IEnumerable<string>)item.Item2), commitResult);
                    break;
                case OperationType.BulkInvalidateSqliteOperation:
                    MarshalCompletion(item.Item3, bulkInvalidateKey.PrepareToExecute((IEnumerable<string>)item.Item2), commitResult);
                    break;
                case OperationType.BulkSelectByTypeSqliteOperation:
                    MarshalCompletion(item.Item3, bulkSelectType.PrepareToExecute((IEnumerable<string>)item.Item2), commitResult);
                    break;
                case OperationType.BulkSelectSqliteOperation:
                    MarshalCompletion(item.Item3, bulkSelectKey.PrepareToExecute((IEnumerable<string>)item.Item2), commitResult);
                    break;
                case OperationType.GetKeysSqliteOperation:
                    MarshalCompletion(item.Item3, getAllKeys.PrepareToExecute(), commitResult);
                    break;
                case OperationType.InvalidateAllSqliteOperation:
                    MarshalCompletion(item.Item3, invalidateAll.PrepareToExecute(), commitResult);
                    break;
                case OperationType.VacuumSqliteOperation:
                    MarshalCompletion(item.Item3, vacuum.PrepareToExecute(), commitResult);
                    break;
                default:
                    throw new ArgumentException("Unknown operation");
                }
            }

            try 
            {
                commit.PrepareToExecute()();
                commitResult.OnNext(Unit.Default);
                commitResult.OnCompleted();
            } 
            catch (Exception ex) 
            {
                commitResult.OnError(ex);
            }
        }

        static internal List<Tuple<OperationType, IEnumerable, object>> CoalesceOperations(List<Tuple<OperationType, IEnumerable, object>> inputItems)
        {
            return inputItems;
        }

        void MarshalCompletion<T>(object completion, Func<T> block, IObservable<Unit> commitResult)
        {
            var subj = (AsyncSubject<T>)completion;
            try 
            {
                var result = block();
                
                // NB: We do this in a scheduled result to stop First() and friends
                // from blowing up
                scheduler.Schedule(() => 
                {
                    subj.OnNext(result);

                    commitResult
                        .SelectMany(_ => Observable.Empty<T>())
                        .Multicast(subj)
                        .PermaRef();
                });
            }
            catch (Exception ex)
            {
                scheduler.Schedule(() => subj.OnError(ex));
            }
        }

        void MarshalCompletion(object completion, Action block, IObservable<Unit> commitResult)
        {
            var subj = (AsyncSubject<Unit>)completion;
            try 
            {
                block();

                scheduler.Schedule(() => 
                {
                    subj.OnNext(Unit.Default);

                    commitResult
                        .SelectMany(_ => Observable.Empty<Unit>())
                        .Multicast(subj)
                        .PermaRef();
                });
            }
            catch (Exception ex)
            {
                scheduler.Schedule(() => subj.OnError(ex));
            }
        }

        public void Dispose()
        {
            var toDispose = new IDisposable[] {
                bulkSelectKey, bulkSelectType, bulkInsertKey, bulkInvalidateKey,
                bulkInvalidateType, invalidateAll, vacuum, getAllKeys, begin, 
                commit,
            };

            foreach (var v in toDispose) v.Dispose();
        }
    }
}