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
    partial class SqliteOperationQueue : IEnableLogger, IDisposable
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

        BlockingCollection<OperationQueueItem> operationQueue =
            new BlockingCollection<OperationQueueItem>();

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
                var toProcess = new List<OperationQueueItem>();

                while (!shouldQuit) 
                {
                    toProcess.Clear();

                    using (await flushLock.LockAsync()) 
                    {
                        // NB: We special-case the first item because we want to 
                        // in the empty list case, we want to wait until we have an item.
                        // Once we have a single item, we try to fetch as many as possible
                        // until we've got enough items.
                        var item = default(OperationQueueItem);
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
                        catch (SQLiteException)
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

                var newQueue = new BlockingCollection<OperationQueueItem>();
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
                    var newQueue = new BlockingCollection<OperationQueueItem>();
                    var existingItems = Interlocked.Exchange(ref operationQueue, newQueue).ToList();

                    ProcessItems(CoalesceOperations(existingItems));
                }
            }).ToObservable();
        }

        public AsyncSubject<IEnumerable<CacheElement>> Select(IEnumerable<string> keys)
        {
            var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectSqliteOperation, keys);
            operationQueue.Add(ret);

            return ret.CompletionAsElements;
        }

        public AsyncSubject<IEnumerable<CacheElement>> SelectTypes(IEnumerable<string> types)
        {
            var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectByTypeSqliteOperation, types);
            operationQueue.Add(ret);

            return ret.CompletionAsElements;
        }

        public AsyncSubject<Unit> Insert(IEnumerable<CacheElement> items)
        {
            var ret = OperationQueueItem.CreateInsert(OperationType.BulkInsertSqliteOperation, items);
            operationQueue.Add(ret);

            return ret.CompletionAsUnit;
        }

        public AsyncSubject<Unit> Invalidate(IEnumerable<string> keys)
        {
            var ret = OperationQueueItem.CreateInvalidate(OperationType.BulkInvalidateSqliteOperation, keys);
            operationQueue.Add(ret);

            return ret.CompletionAsUnit;
        }

        public AsyncSubject<Unit> InvalidateTypes(IEnumerable<string> types)
        {
            var ret = OperationQueueItem.CreateInvalidate(OperationType.BulkInvalidateByTypeSqliteOperation, types);
            operationQueue.Add(ret);

            return ret.CompletionAsUnit;
        }

        public AsyncSubject<Unit> InvalidateAll()
        {
            var ret = OperationQueueItem.CreateUnit(OperationType.InvalidateAllSqliteOperation);
            operationQueue.Add(ret);

            return ret.CompletionAsUnit;
        }

        public AsyncSubject<Unit> Vacuum()
        {
            var ret = OperationQueueItem.CreateUnit(OperationType.VacuumSqliteOperation);
            operationQueue.Add(ret);

            return ret.CompletionAsUnit;
        }

        public AsyncSubject<IEnumerable<string>> GetAllKeys()
        {
            var ret = OperationQueueItem.CreateGetAllKeys();
            operationQueue.Add(ret);

            return ret.CompletionAsKeys;
        }

        internal List<OperationQueueItem> DumpQueue()
        {
            return operationQueue.ToList();
        }

        void ProcessItems(List<OperationQueueItem> toProcess)
        {
            var commitResult = new AsyncSubject<Unit>();
            begin.PrepareToExecute()();

            foreach (var item in toProcess) 
            {
                switch (item.OperationType)
                {
                    case OperationType.BulkInsertSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInsertKey.PrepareToExecute(item.ParametersAsElements), commitResult);
                        break;
                    case OperationType.BulkInvalidateByTypeSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInvalidateType.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkInvalidateSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInvalidateKey.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkSelectByTypeSqliteOperation:
                        MarshalCompletion(item.Completion, bulkSelectType.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkSelectSqliteOperation:
                        MarshalCompletion(item.Completion, bulkSelectKey.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.GetKeysSqliteOperation:
                        MarshalCompletion(item.Completion, getAllKeys.PrepareToExecute(), commitResult);
                        break;
                    case OperationType.InvalidateAllSqliteOperation:
                        MarshalCompletion(item.Completion, invalidateAll.PrepareToExecute(), commitResult);
                        break;
                    case OperationType.VacuumSqliteOperation:
                        MarshalCompletion(item.Completion, vacuum.PrepareToExecute(), commitResult);
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

    class OperationQueueItem
    {
        public OperationType OperationType { get; set; }
        public IEnumerable Parameters { get; set; }
        public object Completion { get; set; }

        public static OperationQueueItem CreateInsert(OperationType opType, IEnumerable<CacheElement> toInsert)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInsert, Completion = new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateInvalidate(OperationType opType, IEnumerable<string> toInvalidate)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInvalidate, Completion = new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateSelect(OperationType opType, IEnumerable<string> toSelect)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toSelect, Completion = new AsyncSubject<IEnumerable<CacheElement>>() };
        }

        public static OperationQueueItem CreateUnit(OperationType opType)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = null, Completion = new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateGetAllKeys()
        {
            return new OperationQueueItem() { OperationType = OperationType.GetKeysSqliteOperation, Parameters = null, Completion = new AsyncSubject<IEnumerable<string>>() };
        }

        public IEnumerable<CacheElement> ParametersAsElements
        {
            get { return (IEnumerable<CacheElement>)Parameters; }
        }

        public IEnumerable<string> ParametersAsKeys
        {
            get { return (IEnumerable<string>)Parameters; }
        }

        public AsyncSubject<Unit> CompletionAsUnit
        {
            get { return (AsyncSubject<Unit>)Completion; }
        }

        public AsyncSubject<IEnumerable<CacheElement>> CompletionAsElements
        {
            get { return (AsyncSubject<IEnumerable<CacheElement>>)Completion; }
        }

        public AsyncSubject<IEnumerable<string>> CompletionAsKeys
        {
            get { return (AsyncSubject<IEnumerable<string>>)Completion; }
        }
    }
}