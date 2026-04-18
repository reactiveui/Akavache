// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Owns a dedicated background thread that is the sole owner of a
/// <see cref="SqlitePclRawConnection"/>'s native <c>sqlite3*</c> handle and its prepared
/// statement cache. Every operation against the connection is enqueued onto this worker
/// instead of running on the caller's thread, guaranteeing that the native handles and
/// prepared statements are only ever touched by one thread — removing the race conditions
/// that produced SIGSEGVs under Rx scheduling.
/// </summary>
/// <remarks>
/// The queue is per-connection, not process-wide. Two blob-cache instances get two
/// independent worker threads. This keeps the model DI-friendly and lets users control
/// the thread lifetime by controlling the cache lifetime.
/// </remarks>
internal sealed class SqliteOperationQueue : IDisposable
{
    /// <summary>Inbox of pending operations. Producers call <c>Add</c>, the worker thread drains via <c>GetConsumingEnumerable</c>.</summary>
    private readonly BlockingCollection<ISqliteOperation> _inbox = [];

    /// <summary>The owning connection. The worker thread is the only thread that invokes methods on it after construction.</summary>
    private readonly SqlitePclRawConnection _connection;

    /// <summary>The dedicated worker thread. One per queue instance, lives for the duration of the connection.</summary>
    private readonly Thread _worker;

    /// <summary>Signalled by the worker thread as it exits so <see cref="ShutdownAndWait"/> can block the caller until the worker is fully done.</summary>
    /// <remarks>Not disposed explicitly: concurrent <see cref="ShutdownAndWait"/> callers may still be blocking on <see cref="ManualResetEventSlim.Wait()"/> when the winner returns.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Concurrent ShutdownAndWait callers may still be blocking on Wait when the winner disposes the inbox.")]
    private readonly ManualResetEventSlim _workerExited = new(initialState: false);

    /// <summary>Reusable buffer for collecting coalescable operations in the worker loop. Only accessed from the worker thread.</summary>
    private readonly List<ISqliteOperation> _batchBuffer = [];

    /// <summary>Non-zero once <see cref="ShutdownAndWait"/> has been entered — gates second-entry and signals the <c>Enqueue</c> path to fail fast.</summary>
    private int _disposed;

    /// <summary>Holds a non-coalescable operation that broke the current batch, to be executed after the batch commits. Only accessed from the worker thread.</summary>
    private ISqliteOperation? _afterBatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteOperationQueue"/> class and
    /// starts the background worker thread. The worker keeps running until
    /// <see cref="ShutdownAndWait"/> is called.
    /// </summary>
    /// <param name="connection">The owning connection. The worker thread is the only thread that invokes methods on it after construction.</param>
    /// <param name="threadName">Name attached to the worker thread for debuggers/profilers.</param>
    public SqliteOperationQueue(SqlitePclRawConnection connection, string threadName)
    {
        _connection = connection;
        _worker = new(WorkerLoop)
        {
            IsBackground = true,
            Name = threadName,
        };
        _worker.Start();
    }

    /// <summary>
    /// Enqueues <paramref name="body"/> for execution on the worker thread and returns
    /// an observable that emits the result once the worker has run it. The returned
    /// observable is a <see cref="SqliteReplyObservable{T}"/> — single-subscriber, one-shot,
    /// deliberately cheaper than <c>AsyncSubject&lt;T&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The result type produced by the body.</typeparam>
    /// <param name="body">The work to run against the owning connection. Runs on the worker thread — must not touch state owned by other threads without synchronization.</param>
    /// <param name="coalescable">When <see langword="true"/>, the worker loop may batch this operation with adjacent coalescable writes into a single transaction.</param>
    /// <returns>A one-shot observable that emits the body's return value.</returns>
    public IObservable<T> Enqueue<T>(Func<SqlitePclRawConnection, T> body, bool coalescable = false)
    {
        var reply = new SqliteReplyObservable<T>();

        if (Volatile.Read(ref _disposed) != 0)
        {
            reply.SetError(new ObjectDisposedException(nameof(SqliteOperationQueue)));
            return reply;
        }

        _inbox.Add(new SqliteOperation<T>(body, reply, coalescable));
        return reply;
    }

    /// <summary>
    /// Enqueues a multi-row work body and returns a
    /// <see cref="SqliteRowObservable{T}"/> that emits each row as the worker produces
    /// it. The body runs on the worker thread with two delegates: one to emit each row,
    /// and one to check whether the subscriber has disposed and the scan should
    /// short-circuit. The body returns normally on end-of-rows, or throws on SQLite
    /// errors (the thrown exception is forwarded to <see cref="IObserver{T}.OnError"/>).
    /// </summary>
    /// <typeparam name="T">The row type emitted.</typeparam>
    /// <param name="body">The work to run. Arguments: the connection, the per-row emit callback, and a cancel-check that returns <see langword="true"/> once the caller has disposed the subscription.</param>
    /// <returns>An observable sequence of rows.</returns>
    public IObservable<T> EnqueueRowStream<T>(Action<SqlitePclRawConnection, Action<T>, Func<bool>> body)
    {
        var stream = new SqliteRowObservable<T>();

        if (Volatile.Read(ref _disposed) != 0)
        {
            stream.OnError(new ObjectDisposedException(nameof(SqliteOperationQueue)));
            return stream;
        }

        _inbox.Add(new SqliteRowStreamOperation<T>(body, stream));
        return stream;
    }

    /// <summary>
    /// Signals the worker thread to drain any remaining work items and then exit, runs
    /// the supplied last-will callback on the worker thread as its final act (so native
    /// prepared statements and the <c>sqlite3*</c> handle are finalized on the same
    /// thread that created them), and blocks the caller until the worker has finished.
    /// </summary>
    /// <param name="lastWill">Cleanup callback invoked on the worker thread after all queued operations have drained. Typical use: dispose prepared statements and close the <c>sqlite3*</c> handle.</param>
    public void ShutdownAndWait(Action<SqlitePclRawConnection> lastWill)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            _workerExited.Wait();
            return;
        }

        TryAddToInbox(new SqliteShutdownOperation(lastWill));

        _inbox.CompleteAdding();
        _workerExited.Wait();
        _inbox.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose() => ShutdownAndWait(static _ => { });

    /// <summary>
    /// Drains operations that raced into the inbox after the shutdown sentinel was
    /// processed. Shutdown ops are skipped; regular ops receive
    /// <see cref="ObjectDisposedException"/> via <see cref="ISqliteOperation.Fail"/>.
    /// </summary>
    /// <param name="inbox">The inbox to drain.</param>
    internal static void DrainLeftovers(BlockingCollection<ISqliteOperation> inbox)
    {
        while (inbox.TryTake(out var leftover))
        {
            if (leftover is SqliteShutdownOperation)
            {
                continue;
            }

            leftover.Fail(new ObjectDisposedException(nameof(SqliteOperationQueue)));
        }
    }

    /// <summary>
    /// Runs a list of operations inside a BEGIN IMMEDIATE … COMMIT transaction.
    /// On per-op failure, rolls back and replays the remainder individually.
    /// On structural failure (COMMIT throws), rolls back and fails all ops.
    /// </summary>
    /// <param name="connection">The SQLite connection.</param>
    /// <param name="batch">The batch of operations to execute.</param>
    internal static void ExecuteBatchInTransaction(
        SqlitePclRawConnection connection,
        List<ISqliteOperation> batch) =>
        ExecuteBatchInTransaction(
            connection,
            batch,
            connection.BeginImmediate,
            connection.Commit,
            () => SqlitePclRawConnection.TryRollbackAmbient(v => connection.InTransaction = v, connection.Db));

    /// <summary>
    /// Core batch-in-transaction implementation with injectable transaction lifecycle
    /// delegates, enabling tests to simulate structural failures (COMMIT throws) without
    /// corrupting a real database connection.
    /// </summary>
    /// <param name="connection">The SQLite connection (used for replay only).</param>
    /// <param name="batch">The batch of operations to execute.</param>
    /// <param name="begin">Delegate that opens the transaction.</param>
    /// <param name="commit">Delegate that commits the transaction.</param>
    /// <param name="rollback">Delegate that rolls back on failure.</param>
    internal static void ExecuteBatchInTransaction(
        SqlitePclRawConnection connection,
        List<ISqliteOperation> batch,
        Action begin,
        Action commit,
        Action rollback)
    {
        var failedIndex = -1;
        Exception? batchError = null;
        try
        {
            begin();
            for (var i = 0; i < batch.Count; i++)
            {
                try
                {
                    batch[i].Execute(connection);
                }
                catch (Exception ex)
                {
                    failedIndex = i;
                    batchError = ex;
                    break;
                }
            }

            if (batchError is null)
            {
                commit();
            }
            else
            {
                rollback();
            }
        }
        catch
        {
            // COMMIT or structural failure — rollback and fail everything.
            rollback();
            FailAllOps(batch);
            return;
        }

        if (batchError is null)
        {
            return;
        }

        // The faulted op already received its error via Execute → catch in its own
        // SqliteOperation<T>.Execute. Replay the remainder individually.
        ReplayRemainingOps(connection, batch, failedIndex + 1);
    }

    /// <summary>
    /// Fails all operations in a batch with an <see cref="InvalidOperationException"/>.
    /// Used when COMMIT or structural failure makes the entire batch unrecoverable.
    /// </summary>
    /// <param name="batch">The batch whose operations should receive errors.</param>
    internal static void FailAllOps(List<ISqliteOperation> batch)
    {
        for (var i = 0; i < batch.Count; i++)
        {
            batch[i].Fail(new InvalidOperationException("Coalesced batch commit failed."));
        }
    }

    /// <summary>
    /// Replays operations individually after a mid-batch failure. The faulted op has
    /// already received its error; this method re-executes the remaining ops outside the
    /// transaction so they have a chance to succeed on their own.
    /// </summary>
    /// <param name="connection">The SQLite connection.</param>
    /// <param name="batch">The full batch.</param>
    /// <param name="startIndex">The index of the first op to replay.</param>
    internal static void ReplayRemainingOps(
        SqlitePclRawConnection connection,
        List<ISqliteOperation> batch,
        int startIndex)
    {
        for (var i = startIndex; i < batch.Count; i++)
        {
            batch[i].Execute(connection);
        }
    }

    /// <summary>
    /// Attempts to add an operation to the inbox. Returns <see langword="false"/> if the
    /// collection was already completed (i.e. <see cref="BlockingCollection{T}.CompleteAdding"/>
    /// raced between the disposed check and the <c>Add</c> call).
    /// </summary>
    /// <param name="op">The operation to enqueue.</param>
    /// <returns><see langword="true"/> if the op was added; <see langword="false"/> if the inbox was already completed.</returns>
    internal bool TryAddToInbox(ISqliteOperation op)
    {
        try
        {
            _inbox.Add(op);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Worker-thread entry point. Drains <see cref="_inbox"/> until a
    /// <see cref="SqliteShutdownOperation"/> arrives, runs its last-will callback, then fails
    /// any latecomers and signals <see cref="_workerExited"/> so the caller of
    /// <see cref="ShutdownAndWait"/> can unblock.
    /// </summary>
    /// <remarks>
    /// Adjacent coalescable writes (those with <see cref="ISqliteOperation.IsCoalescable"/>
    /// set) are batched into a single <c>BEGIN IMMEDIATE … COMMIT</c> transaction. This
    /// reduces per-op transaction overhead when multiple callers enqueue writes concurrently.
    /// Non-coalescable ops (reads, vacuum, shutdown) break the batch and run individually.
    /// </remarks>
    internal void WorkerLoop()
    {
        try
        {
            foreach (var op in _inbox.GetConsumingEnumerable())
            {
                if (op is SqliteShutdownOperation shutdown)
                {
                    shutdown.RunLastWill(_connection);
                    break;
                }

                if (!op.IsCoalescable)
                {
                    op.Execute(_connection);
                    continue;
                }

                // Start a coalesced batch — peek ahead for additional coalescable ops.
                ExecuteCoalescedBatch(op);
            }

            // Drain anything that raced in after the shutdown op was posted.
            DrainLeftovers(_inbox);
        }
        finally
        {
            _workerExited.Set();
        }
    }

    /// <summary>
    /// Executes a batch of coalescable operations inside a single transaction.
    /// The first op has already been dequeued by the caller; this method peeks ahead
    /// for additional coalescable siblings via <c>TryTake</c> with zero timeout, wraps
    /// them all in a single <c>BEGIN IMMEDIATE … COMMIT</c>, and delivers results to
    /// each op's reply observable individually.
    /// </summary>
    /// <param name="first">The first coalescable operation, already dequeued from the inbox.</param>
    internal void ExecuteCoalescedBatch(ISqliteOperation first)
    {
        // Fast path: no siblings waiting — run the single op without transaction wrapper.
        if (!_inbox.TryTake(out var second))
        {
            first.Execute(_connection);
            return;
        }

        // We have at least two ops — collect the batch.
        _batchBuffer.Clear();
        _batchBuffer.Add(first);
        _batchBuffer.Add(second);

        while (_inbox.TryTake(out var next))
        {
            if (!next.IsCoalescable)
            {
                // Non-coalescable op breaks the batch. Stash it for after the commit.
                _afterBatch = next;
                break;
            }

            _batchBuffer.Add(next);
        }

        // Execute the batch inside a single transaction.
        ExecuteBatchInTransaction(_connection, _batchBuffer);

        _batchBuffer.Clear();
        RunAfterBatch();
    }

    /// <summary>
    /// Runs the stashed non-coalescable op (if any) that broke the current batch,
    /// then clears the slot.
    /// </summary>
    internal void RunAfterBatch()
    {
        if (_afterBatch is null)
        {
            return;
        }

        var op = _afterBatch;
        _afterBatch = null;

        if (op is SqliteShutdownOperation shutdown)
        {
            shutdown.RunLastWill(_connection);

            // Drain leftovers the same way the main loop does after a shutdown op.
            DrainLeftovers(_inbox);
        }
        else
        {
            op.Execute(_connection);
        }
    }
}
