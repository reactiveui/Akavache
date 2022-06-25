// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;

using Akavache.Sqlite3.Internal;

using Splat;

using AsyncLock = Akavache.Sqlite3.Internal.AsyncLock;

namespace Akavache.Sqlite3;

/// <summary>
/// A queue which will perform Sqlite based operations.
/// </summary>
internal partial class SqliteOperationQueue : IEnableLogger, IDisposable
{
    private readonly AsyncLock _flushLock = new();
    private readonly IScheduler _scheduler;

    private readonly Lazy<BulkSelectSqliteOperation> _bulkSelectKey;
    private readonly Lazy<BulkSelectByTypeSqliteOperation> _bulkSelectType;
    private readonly Lazy<BulkInsertSqliteOperation> _bulkInsertKey;
    private readonly Lazy<BulkInvalidateSqliteOperation> _bulkInvalidateKey;
    private readonly Lazy<BulkInvalidateByTypeSqliteOperation> _bulkInvalidateType;
    private readonly Lazy<InvalidateAllSqliteOperation> _invalidateAll;
    private readonly Lazy<VacuumSqliteOperation> _vacuum;
    private readonly Lazy<DeleteExpiredSqliteOperation> _deleteExpired;
    private readonly Lazy<GetKeysSqliteOperation> _getAllKeys;
    private readonly Lazy<BeginTransactionSqliteOperation> _begin;
    private readonly Lazy<CommitTransactionSqliteOperation> _commit;

    private BlockingCollection<OperationQueueItem> _operationQueue = new();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA2213: dispose field", Justification = "Will be invalid")]
    private IDisposable? _start;
    private CancellationTokenSource? _shouldQuit;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteOperationQueue"/> class.
    /// </summary>
    /// <param name="connection">A sql lite connection where to perform queue operation against.</param>
    /// <param name="scheduler">The scheduler to perform operations against.</param>
    public SqliteOperationQueue(SQLiteConnection connection, IScheduler scheduler)
    {
        _scheduler = scheduler;

        _bulkSelectKey = new(() => new(connection, false, scheduler));
        _bulkSelectType = new(() => new(connection, scheduler));
        _bulkInsertKey = new(() => new(connection));
        _bulkInvalidateKey = new(() => new(connection, false));
        _bulkInvalidateType = new(() => new(connection));
        _invalidateAll = new(() => new(connection));
        _vacuum = new(() => new(connection, scheduler));
        _deleteExpired = new(() => new(connection, scheduler));
        _getAllKeys = new(() => new(connection, scheduler));
        _begin = new(() => new(connection));
        _commit = new(() => new(connection));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteOperationQueue"/> class.
    /// </summary>
    /// <remarks>
    /// NB: This constructor is used for testing operation coalescing,
    /// don't actually use it for reals.
    /// </remarks>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    internal SqliteOperationQueue()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    {
    }

    /// <summary>
    /// Starts the operation queue.
    /// </summary>
    /// <returns>A disposable which when Disposed will stop the queue.</returns>
    public IDisposable Start()
    {
        if (_start is not null)
        {
            return _start;
        }

        _shouldQuit = new();
        var task = Task.Run(async () =>
        {
            var toProcess = new List<OperationQueueItem>();

            while (!_shouldQuit.IsCancellationRequested)
            {
                toProcess.Clear();

                IDisposable? @lock;

                try
                {
                    @lock = await _flushLock.LockAsync(_shouldQuit.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Verify lock was acquired
                if (@lock is null)
                {
                    break;
                }

                using (@lock)
                {
                    // NB: We special-case the first item because we want to
                    // in the empty list case, we want to wait until we have an item.
                    // Once we have a single item, we try to fetch as many as possible
                    // until we've got enough items.
                    OperationQueueItem? item;
                    try
                    {
                        item = _operationQueue.Take(_shouldQuit.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // NB: We explicitly want to bail out *here* because we
                    // never want to bail out in the middle of processing
                    // operations, to guarantee that we won't orphan them
                    if (_shouldQuit.IsCancellationRequested && item is null)
                    {
                        break;
                    }

                    toProcess.Add(item);
                    while (toProcess.Count < Constants.OperationQueueChunkSize && _operationQueue.TryTake(out item) && item is not null)
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
                        foreach (var v in toProcess)
                        {
                            _operationQueue.Add(v);
                        }
                    }
                }
            }
        });

        return _start = Disposable.Create(
            () =>
            {
                try
                {
                    _shouldQuit.Cancel();
                    task.Wait();
                }
                catch (OperationCanceledException)
                {
                }

                try
                {
                    using (_flushLock.LockAsync().Result)
                    {
                        FlushInternal();
                    }
                }
                catch (OperationCanceledException)
                {
                }

                _start = null;
            });
    }

    public IObservable<Unit> Flush()
    {
        var noop = OperationQueueItem.CreateUnit(OperationType.DoNothing);
        _operationQueue.Add(noop);

        return noop.CompletionAsUnit;
    }

    public AsyncSubject<IEnumerable<CacheElement>> Select(IEnumerable<string> keys)
    {
        var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectSqliteOperation, keys);
        _operationQueue.Add(ret);

        return ret.CompletionAsElements;
    }

    public AsyncSubject<IEnumerable<CacheElement>> SelectTypes(IEnumerable<string> types)
    {
        var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectByTypeSqliteOperation, types);
        _operationQueue.Add(ret);

        return ret.CompletionAsElements;
    }

    public AsyncSubject<Unit> Insert(IEnumerable<CacheElement> items)
    {
        var ret = OperationQueueItem.CreateInsert(OperationType.BulkInsertSqliteOperation, items);
        _operationQueue.Add(ret);

        return ret.CompletionAsUnit;
    }

    public AsyncSubject<Unit> Invalidate(IEnumerable<string> keys)
    {
        var ret = OperationQueueItem.CreateInvalidate(OperationType.BulkInvalidateSqliteOperation, keys);
        _operationQueue.Add(ret);

        return ret.CompletionAsUnit;
    }

    public AsyncSubject<Unit> InvalidateTypes(IEnumerable<string> types)
    {
        var ret = OperationQueueItem.CreateInvalidate(OperationType.BulkInvalidateByTypeSqliteOperation, types);
        _operationQueue.Add(ret);

        return ret.CompletionAsUnit;
    }

    public AsyncSubject<Unit> InvalidateAll()
    {
        var ret = OperationQueueItem.CreateUnit(OperationType.InvalidateAllSqliteOperation);
        _operationQueue.Add(ret);

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
                IDisposable? @lock = null;
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
                    var lockTask = _flushLock.LockAsync(_shouldQuit?.Token ?? CancellationToken.None);
                    _operationQueue.Add(OperationQueueItem.CreateUnit(OperationType.DoNothing));

                    try
                    {
                        @lock = await lockTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    var deleteOp = OperationQueueItem.CreateUnit(OperationType.DeleteExpiredSqliteOperation);
                    _operationQueue.Add(deleteOp);

                    FlushInternal();

                    await deleteOp.CompletionAsUnit;

                    var vacuumOp = OperationQueueItem.CreateUnit(OperationType.VacuumSqliteOperation);

                    MarshalCompletion(vacuumOp.Completion, _vacuum.Value.PrepareToExecute(), Observable.Return(Unit.Default));

                    await vacuumOp.CompletionAsUnit;
                }
                finally
                {
                    @lock?.Dispose();
                }
            })
            .ToObservable()
            .ObserveOn(_scheduler)
            .Multicast(ret)
            .PermaRef();

        return ret;
    }

    public AsyncSubject<IEnumerable<string>> GetAllKeys()
    {
        var ret = OperationQueueItem.CreateGetAllKeys();
        _operationQueue.Add(ret);

        return ret.CompletionAsKeys;
    }

    public void Dispose()
    {
        if (_bulkSelectKey.IsValueCreated)
        {
            _bulkSelectKey.Value.Dispose();
        }

        if (_bulkSelectType.IsValueCreated)
        {
            _bulkSelectType.Value.Dispose();
        }

        if (_bulkInsertKey.IsValueCreated)
        {
            _bulkInsertKey.Value.Dispose();
        }

        if (_bulkInvalidateKey.IsValueCreated)
        {
            _bulkInvalidateKey.Value.Dispose();
        }

        if (_bulkInvalidateType.IsValueCreated)
        {
            _bulkInvalidateType.Value.Dispose();
        }

        if (_invalidateAll.IsValueCreated)
        {
            _invalidateAll.Value.Dispose();
        }

        if (_vacuum.IsValueCreated)
        {
            _vacuum.Value.Dispose();
        }

        if (_deleteExpired.IsValueCreated)
        {
            _deleteExpired.Value.Dispose();
        }

        if (_getAllKeys.IsValueCreated)
        {
            _getAllKeys.Value.Dispose();
        }

        if (_begin.IsValueCreated)
        {
            _begin.Value.Dispose();
        }

        if (_begin.IsValueCreated)
        {
            _commit.Value.Dispose();
        }

        _operationQueue?.Dispose();
        _flushLock?.Dispose();
        _shouldQuit?.Dispose();
    }

    internal List<OperationQueueItem> DumpQueue() => _operationQueue.ToList();

    private static void MarshalCompletion(object completion, Action block, IObservable<Unit> commitResult)
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

    // NB: Callers must hold flushLock to call this
    private void FlushInternal()
    {
        var newQueue = new BlockingCollection<OperationQueueItem>();
        var existingItems = Interlocked.Exchange(ref _operationQueue, newQueue).ToList();

        ProcessItems(CoalesceOperations(existingItems));
    }

    private void ProcessItems(List<OperationQueueItem> toProcess)
    {
        var commitResult = new AsyncSubject<Unit>();

        _begin.Value.PrepareToExecute()();

        foreach (var item in toProcess)
        {
            switch (item.OperationType)
            {
                case OperationType.DoNothing:
                    MarshalCompletion(item.Completion, () => { }, commitResult);
                    break;
                case OperationType.BulkInsertSqliteOperation:
                    MarshalCompletion(item.Completion, _bulkInsertKey.Value.PrepareToExecute(item.ParametersAsElements), commitResult);
                    break;
                case OperationType.BulkInvalidateByTypeSqliteOperation:
                    MarshalCompletion(item.Completion, _bulkInvalidateType.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                    break;
                case OperationType.BulkInvalidateSqliteOperation:
                    MarshalCompletion(item.Completion, _bulkInvalidateKey.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                    break;
                case OperationType.BulkSelectByTypeSqliteOperation:
                    MarshalCompletion(item.Completion, _bulkSelectType.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                    break;
                case OperationType.BulkSelectSqliteOperation:
                    MarshalCompletion(item.Completion, _bulkSelectKey.Value.PrepareToExecute(item.ParametersAsKeys), commitResult);
                    break;
                case OperationType.GetKeysSqliteOperation:
                    MarshalCompletion(item.Completion, _getAllKeys.Value.PrepareToExecute(), commitResult);
                    break;
                case OperationType.InvalidateAllSqliteOperation:
                    MarshalCompletion(item.Completion, _invalidateAll.Value.PrepareToExecute(), commitResult);
                    break;
                case OperationType.DeleteExpiredSqliteOperation:
                    MarshalCompletion(item.Completion, _deleteExpired.Value.PrepareToExecute(), commitResult);
                    break;
                case OperationType.VacuumSqliteOperation:
                    throw new ArgumentException("Vacuum operation can't run inside transaction", nameof(toProcess));
                default:
                    throw new ArgumentException("Unknown operation", nameof(toProcess));
            }
        }

        try
        {
            _commit.Value.PrepareToExecute()();

            // NB: We do this in a scheduled result to stop a deadlock in
            // First and friends
            _scheduler.Schedule(() =>
            {
                commitResult.OnNext(Unit.Default);
                commitResult.OnCompleted();
            });
        }
        catch (Exception ex)
        {
            _scheduler.Schedule(() => commitResult.OnError(ex));
        }
    }

    private void MarshalCompletion<T>(object completion, Func<T> block, IObservable<Unit> commitResult)
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
            _scheduler.Schedule(() => subj.OnError(ex));
        }
    }
}