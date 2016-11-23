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

        readonly Lazy<BulkSelectSqliteOperation> bulkSelectKey;
        readonly Lazy<BulkSelectByTypeSqliteOperation> bulkSelectType;
        readonly Lazy<BulkInsertSqliteOperation> bulkInsertKey;
        readonly Lazy<BulkInvalidateSqliteOperation> bulkInvalidateKey;
        readonly Lazy<BulkInvalidateByTypeSqliteOperation> bulkInvalidateType;
        readonly Lazy<InvalidateAllSqliteOperation> invalidateAll;
        readonly Lazy<VacuumSqliteOperation> vacuum;
        readonly Lazy<DeleteExpiredSqliteOperation> deleteExpired;
        readonly Lazy<GetKeysSqliteOperation> getAllKeys;
        readonly Lazy<BeginTransactionSqliteOperation> begin;
        readonly Lazy<CommitTransactionSqliteOperation> commit;

        BlockingCollection<OperationQueueItem> operationQueue =
            new BlockingCollection<OperationQueueItem>();

        public SqliteOperationQueue(SQLiteConnection conn, IScheduler scheduler)
        {
            this.scheduler = scheduler;

            bulkSelectKey = new Lazy<BulkSelectSqliteOperation>(() => new BulkSelectSqliteOperation(conn, false, scheduler));
            bulkSelectType = new Lazy<BulkSelectByTypeSqliteOperation>(() => new BulkSelectByTypeSqliteOperation(conn, scheduler));
            bulkInsertKey = new Lazy<BulkInsertSqliteOperation>(() => new BulkInsertSqliteOperation(conn));
            bulkInvalidateKey = new Lazy<BulkInvalidateSqliteOperation>(() => new BulkInvalidateSqliteOperation(conn, false));
            bulkInvalidateType = new Lazy<BulkInvalidateByTypeSqliteOperation>(() => new BulkInvalidateByTypeSqliteOperation(conn));
            invalidateAll = new Lazy<InvalidateAllSqliteOperation>(() => new InvalidateAllSqliteOperation(conn));
            vacuum = new Lazy<VacuumSqliteOperation>(() => new VacuumSqliteOperation(conn, scheduler));
            deleteExpired = new Lazy<DeleteExpiredSqliteOperation>(() => new DeleteExpiredSqliteOperation(conn, scheduler));
            getAllKeys = new Lazy<GetKeysSqliteOperation>(() => new GetKeysSqliteOperation(conn, scheduler));
            begin = new Lazy<BeginTransactionSqliteOperation>(() => new BeginTransactionSqliteOperation(conn));
            commit = new Lazy<CommitTransactionSqliteOperation>(() => new CommitTransactionSqliteOperation(conn));
        }

        // NB: This constructor is used for testing operation coalescing,
        // don't actually use it for reals
        internal SqliteOperationQueue()
        {
        }
         
        IDisposable start;
        CancellationTokenSource shouldQuit;
        public IDisposable Start()
        {
            if (start != null) return start;

            shouldQuit = new CancellationTokenSource();
            var task = new Task(async () => 
            {
                var toProcess = new List<OperationQueueItem>();

                while (!shouldQuit.IsCancellationRequested) 
                {
                    toProcess.Clear();

                    IDisposable @lock = null;

                    try
                    {
                        @lock = await flushLock.LockAsync(shouldQuit.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    using (@lock)
                    {
                        // NB: We special-case the first item because we want to 
                        // in the empty list case, we want to wait until we have an item.
                        // Once we have a single item, we try to fetch as many as possible
                        // until we've got enough items.
                        var item = default(OperationQueueItem);
                        try 
                        {
                            item = operationQueue.Take(shouldQuit.Token);
                        } 
                        catch (OperationCanceledException) 
                        {
                            break;
                        }

                        // NB: We explicitly want to bail out *here* because we 
                        // never want to bail out in the middle of processing 
                        // operations, to guarantee that we won't orphan them
                        if (shouldQuit.IsCancellationRequested && item == null) break;

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
            }, TaskCreationOptions.LongRunning);

            task.Start();

            return (start = Disposable.Create(() => 
            {
                try 
                {
                    shouldQuit.Cancel();
                    task.Wait();
                } 
                catch (OperationCanceledException) { }

                using (flushLock.LockAsync().Result)
                {
                    FlushInternal();
                }

                start = null;
            }));
        }

        public IObservable<Unit> Flush()
        {
            var noop = OperationQueueItem.CreateUnit(OperationType.DoNothing);
            operationQueue.Add(noop);

            return noop.CompletionAsUnit;
        }

        // NB: Callers must hold flushLock to call this
        void FlushInternal()
        {
            var newQueue = new BlockingCollection<OperationQueueItem>();
            var existingItems = Interlocked.Exchange(ref operationQueue, newQueue).ToList();

            ProcessItems(CoalesceOperations(existingItems));
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
            // Vacuum is a special snowflake. We want to delete all the expired rows before
            // actually vacuuming. Unfortunately vacuum can't be run in a transaction so we'll
            // claim an exclusive lock on the queue, drain it and run the delete first before
            // running our vacuum op without any transactions.
            var ret = new AsyncSubject<Unit>();

            Task.Run(async () =>
            {
                IDisposable @lock = null;
                try
                {
                    // NB. While the documentation for SemaphoreSlim (which powers AsyncLock)
                    // doesn't guarantee ordering the actual (current) implementation[1]
                    // uses a linked list to queue incoming requests so by adding ourselves
                    // to the queue first and then sending a no-op to the main queue to
                    // force it to finish up and release the lock we avoid any potential
                    // race condition where the main queue reclaims the lock before we
                    // have had a chance to acquire it.
                    //
                    // 1. http://referencesource.microsoft.com/#mscorlib/system/threading/SemaphoreSlim.cs,d57f52e0341a581f
                    var lockTask = flushLock.LockAsync(shouldQuit.Token);
                    operationQueue.Add(OperationQueueItem.CreateUnit(OperationType.DoNothing));

                    @lock = await lockTask;

                    var deleteOp = OperationQueueItem.CreateUnit(OperationType.DeleteExpiredSqliteOperation);
                    operationQueue.Add(deleteOp);

                    FlushInternal();

                    await deleteOp.CompletionAsUnit;

                    var vacuumOp = OperationQueueItem.CreateUnit(OperationType.VacuumSqliteOperation);

                    MarshalCompletion(vacuumOp.Completion, vacuum.Value.PrepareToExecute(), Observable.Return(Unit.Default));

                    await vacuumOp.CompletionAsUnit;
                }
                finally
                {
                    if (@lock != null) @lock.Dispose();
                }
            })
            .ToObservable()
            .ObserveOn(scheduler)
            .Multicast(ret)
            .PermaRef();

            return ret;
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

            begin.Value.PrepareToExecute()();

            foreach (var item in toProcess) 
            {
                switch (item.OperationType)
                {
                    case OperationType.DoNothing:
                        MarshalCompletion(item.Completion, () => { }, commitResult);
                        break;
                    case OperationType.BulkInsertSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInsertKey.Value.PrepareToExecute(item.ParametersAsElements), commitResult);
                        break;
                    case OperationType.BulkInvalidateByTypeSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInvalidateType.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkInvalidateSqliteOperation:
                        MarshalCompletion(item.Completion, bulkInvalidateKey.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkSelectByTypeSqliteOperation:
                        MarshalCompletion(item.Completion, bulkSelectType.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.BulkSelectSqliteOperation:
                        MarshalCompletion(item.Completion, bulkSelectKey.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                        break;
                    case OperationType.GetKeysSqliteOperation:
                        MarshalCompletion(item.Completion, getAllKeys.Value.PrepareToExecute(), commitResult);
                        break;
                    case OperationType.InvalidateAllSqliteOperation:
                        MarshalCompletion(item.Completion, invalidateAll.Value.PrepareToExecute(), commitResult);
                        break;
                    case OperationType.DeleteExpiredSqliteOperation:
                        MarshalCompletion(item.Completion, deleteExpired.Value.PrepareToExecute(), commitResult);
                        break;
                    case OperationType.VacuumSqliteOperation:
                        throw new ArgumentException("Vacuum operation can't run inside transaction");
                    default:
                        throw new ArgumentException("Unknown operation");
                }
            }

            try 
            {
                commit.Value.PrepareToExecute()();

                // NB: We do this in a scheduled result to stop a deadlock in
                // First and friends
                scheduler.Schedule(() => 
                {
                    commitResult.OnNext(Unit.Default);
                    commitResult.OnCompleted();
                });
            } 
            catch (Exception ex) 
            {
                scheduler.Schedule(() => commitResult.OnError(ex));
            }
        }

        void MarshalCompletion<T>(object completion, Func<T> block, IObservable<Unit> commitResult)
        {
            var subj = (AsyncSubject<T>)completion;
            try 
            {
                var result = block();
                
                subj.OnNext(result);

                commitResult
                    .SelectMany(_ => Observable.Empty<T>())
                    .Multicast(subj)
                    .PermaRef();
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

                subj.OnNext(Unit.Default);

                commitResult
                    .SelectMany(_ => Observable.Empty<Unit>())
                    .Multicast(subj)
                    .PermaRef();
            }
            catch (Exception ex)
            {
                subj.OnError(ex);
            }
        }

        public void Dispose()
        {
            if(bulkSelectKey.IsValueCreated)
            {
                bulkSelectKey.Value.Dispose();
            }

            if (bulkSelectType.IsValueCreated)
            {
                bulkSelectType.Value.Dispose();
            }

            if (bulkInsertKey.IsValueCreated)
            {
                bulkInsertKey.Value.Dispose();
            }

            if (bulkInvalidateKey.IsValueCreated)
            {
                bulkInvalidateKey.Value.Dispose();
            }

            if (bulkInvalidateType.IsValueCreated)
            {
                bulkInvalidateType.Value.Dispose();
            }

            if (invalidateAll.IsValueCreated)
            {
                invalidateAll.Value.Dispose();
            }

            if (vacuum.IsValueCreated)
            {
                vacuum.Value.Dispose();
            }

            if (deleteExpired.IsValueCreated)
            {
                deleteExpired.Value.Dispose();
            }

            if (getAllKeys.IsValueCreated)
            {
                getAllKeys.Value.Dispose();
            }

            if (begin.IsValueCreated)
            {
                begin.Value.Dispose();
            }

            if (begin.IsValueCreated)
            {
                commit.Value.Dispose();
            }
        }
    }

    class OperationQueueItem
    {
        public OperationType OperationType { get; set; }
        public IEnumerable Parameters { get; set; }
        public object Completion { get; set; }

        public static OperationQueueItem CreateInsert(OperationType opType, IEnumerable<CacheElement> toInsert, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInsert, Completion = completion ?? new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateInvalidate(OperationType opType, IEnumerable<string> toInvalidate, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInvalidate, Completion = completion ?? new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateSelect(OperationType opType, IEnumerable<string> toSelect, AsyncSubject<IEnumerable<CacheElement>> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toSelect, Completion = completion ?? new AsyncSubject<IEnumerable<CacheElement>>() };
        }

        public static OperationQueueItem CreateUnit(OperationType opType, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = null, Completion = completion ?? new AsyncSubject<Unit>() };
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
