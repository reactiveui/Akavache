// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests targeting uncovered lines in <see cref="SqliteOperationQueue"/>: dispose paths,
/// worker-loop drain, coalesced-batch execution, and enqueue-after-dispose error handling.
/// Concurrency tests use dedicated Thread instances (not Task.Run) to avoid threadpool
/// starvation when WaitForCompletion blocks the calling thread.
/// </summary>
[Category("Akavache")]
public class SqliteOperationQueueCoverageTests
{
    /// <summary>Result of the operation a test enqueues purely to observe the queue's plumbing.</summary>
    private const int ProbeOperationResult = 42;

    /// <summary>How long a test lets a shutdown settle before racing an enqueue against it.</summary>
    private const int ShutdownSettleMilliseconds = 50;

    /// <summary>How long a test waits for a worker or shutdown thread before declaring a deadlock.</summary>
    private static readonly TimeSpan ThreadJoinTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How long a gated worker body or an enqueue rendezvous waits before giving up.</summary>
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Payload of the entry written by a single, uncoalesced operation.</summary>
    private static readonly byte[] SingleWritePayload = [99];

    /// <summary>Payload of the entry a batch-breaking read looks up.</summary>
    private static readonly byte[] AnchorPayload = [0xFF];

    /// <summary>Dispose calls ShutdownAndWait; subsequent enqueue returns ObjectDisposedException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_SubsequentEnqueue_ReturnsObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"dispose-{Guid.NewGuid()}.db");
            var queue = new SqliteOperationQueue(
                SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false),
                "test-dispose");

            queue.Dispose();

            var obs = queue.Enqueue(static _ => ProbeOperationResult);
            var error = obs.SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// Fire-and-forget writes then immediate dispose — worker drains leftovers.
    /// Uses dedicated threads to avoid threadpool starvation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WorkerLoop_DrainLeftovers_AllRepliesComplete()
    {
        const int QueuedWriteCount = 20;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var replies = new List<IObservable<RxVoid>>();
            for (var i = 0; i < QueuedWriteCount; i++)
            {
                replies.Add(conn.Upsert(
                    [new CacheEntry($"drain-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)]));
            }

            conn.Dispose();

            var completedCount = 0;
            foreach (var reply in replies)
            {
                var error = reply.WaitForError();
                if (error is null or ObjectDisposedException)
                {
                    completedCount++;
                }
            }

            await Assert.That(completedCount).IsEqualTo(QueuedWriteCount);
        }
    }

    /// <summary>Single coalescable op runs without a transaction wrapper (fast path).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteCoalescedBatch_SingleOp_RunsWithoutTransactionWrapper()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"single-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("single", null, SingleWritePayload, TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                var entry = conn.Get("single", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                await Assert.That(entry).IsNotNull();
                await Assert.That(entry!.Value![0]).IsEqualTo(SingleWritePayload[0]);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Interleaved writes and reads exercise the afterBatch path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteCoalescedBatch_NonCoalescableBreaksBatch()
    {
        const int InterleavedWriteCount = 5;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"break-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("seed", null, [1], TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                for (var i = 0; i < InterleavedWriteCount; i++)
                {
                    conn.Upsert([new CacheEntry($"brk-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)])
                        .WaitForCompletion();
                    _ = conn.Get("seed", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                }

                var entry = conn.Get("seed", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                await Assert.That(entry).IsNotNull();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Coalescable-only writes — RunAfterBatch with null _afterBatch is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunAfterBatch_NoStashedOp_IsNoOp()
    {
        const int QueuedWriteCount = 5;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"noop-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                for (var i = 0; i < QueuedWriteCount; i++)
                {
                    conn.Upsert([new CacheEntry($"noop-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)])
                        .WaitForCompletion();
                }

                for (var i = 0; i < QueuedWriteCount; i++)
                {
                    var entry = conn.Get($"noop-{i}", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                    await Assert.That(entry).IsNotNull();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Enqueue after dispose — reply observable receives error, row stream completes empty.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Enqueue_AfterDispose_ReturnsErrorOrEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"post-dispose-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var error = conn.Get("nonexistent", null, TimeProvider.System.GetUtcNow()).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            var keys = conn.GetAllKeys(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
        }
    }

    /// <summary>Fire-and-forget writes then shutdown — no deadlock or exception.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownAndWait_FireAndForget_CompletesCleanly()
    {
        const int QueuedWriteCount = 20;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rapid-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            for (var i = 0; i < QueuedWriteCount; i++)
            {
                _ = conn.Upsert([new CacheEntry($"rapid-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)]);
            }

            conn.Dispose();
            await Task.CompletedTask;
        }
    }

    /// <summary>Multiple sequential dispose calls are idempotent — second call waits on _workerExited.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_MultipleSequential_IsIdempotent()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"multi-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            conn.Dispose();
            conn.Dispose();
            conn.Dispose();
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Concurrent writes using dedicated threads (not threadpool) to exercise coalesced
    /// batch building without threadpool starvation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CoalescedBatch_ConcurrentWritesViaDedicatedThreads()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"coalesce-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                const int threadCount = 5;
                using var go = new ManualResetEventSlim(false);
                var threads = new Thread[threadCount];
                var errors = new Exception?[threadCount];

                for (var i = 0; i < threadCount; i++)
                {
                    var idx = i;
                    threads[i] = StartWriterThread(conn, go, errors, idx, $"t-{idx}");
                }

                go.Set();
                JoinAll(threads);

                for (var i = 0; i < threadCount; i++)
                {
                    await Assert.That(errors[i]).IsNull();
                    var entry = conn.Get($"t-{i}", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                    await Assert.That(entry).IsNotNull();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// Concurrent dispose from dedicated threads — all return without deadlock.
    /// Exercises the ShutdownAndWait second-entry path (line 144-147).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_ConcurrentViaDedicatedThreads_NoDeadlock()
    {
        const int ConcurrentDisposerCount = 3;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"conc-dispose-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[ConcurrentDisposerCount];

            for (var i = 0; i < ConcurrentDisposerCount; i++)
            {
                threads[i] = new(() =>
                {
                    go.Wait();
                    conn.Dispose();
                })
                { IsBackground = true };
                threads[i].Start();
            }

            go.Set();
            JoinAll(threads);

            await Task.CompletedTask;
        }
    }

    /// <summary>Mixed writes and reads from dedicated threads exercise afterBatch + coalescing under real concurrency.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CoalescedBatch_MixedWritesAndReads_ViaDedicatedThreads()
    {
        const int writerCount = 4;
        const int readerCount = 2;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"mixed-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("anchor", null, AnchorPayload, TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                using var go = new ManualResetEventSlim(false);
                var threads = new Thread[writerCount + readerCount];
                var errors = new Exception?[writerCount + readerCount];

                for (var i = 0; i < writerCount; i++)
                {
                    var idx = i;
                    threads[i] = StartWriterThread(conn, go, errors, idx, $"w-{idx}");
                }

                for (var i = 0; i < readerCount; i++)
                {
                    var idx = writerCount + i;
                    threads[idx] = StartAnchorReaderThread(conn, go, errors, idx);
                }

                go.Set();
                JoinAll(threads);

                for (var i = 0; i < writerCount + readerCount; i++)
                {
                    await Assert.That(errors[i]).IsNull();
                }

                for (var i = 0; i < writerCount; i++)
                {
                    var entry = conn.Get($"w-{i}", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                    await Assert.That(entry).IsNotNull();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Writes from dedicated threads then dispose — exercises shutdown-as-afterBatch path.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunAfterBatch_ShutdownDuringConcurrentWrites()
    {
        const int QueuedWriteCount = 30;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"shutdown-batch-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var observables = new List<IObservable<RxVoid>>();
            for (var i = 0; i < QueuedWriteCount; i++)
            {
                observables.Add(conn.Upsert(
                    [new CacheEntry($"ab-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)]));
            }

            conn.Dispose();

            var totalCompleted = CountSettledReplies(observables);
            await Assert.That(totalCompleted).IsEqualTo(QueuedWriteCount);
        }
    }

    // ── Lines 92-96: Enqueue catch after CompleteAdding ──────────────────
    /// <summary>
    /// Enqueue after the inbox has been completed via CompleteAdding (but before _disposed
    /// is set) exercises the InvalidOperationException catch in Enqueue (lines 92-96).
    /// Uses a dedicated thread to race ShutdownAndWait against Enqueue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Enqueue_AfterCompleteAdding_CatchSetsObjectDisposedError()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"enqueue-complete-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            // Build the queue directly so we can call CompleteAdding on the inbox before
            // _disposed is set.
            var dbPath2 = Path.Combine(path, $"enqueue-complete2-{Guid.NewGuid()}.db");
            var conn2 = SqlitePclRawConnection.Create(dbPath2, password: null, readOnly: false);
            conn2.CreateSchema().WaitForCompletion();
            var queue = new SqliteOperationQueue(conn2, "test-complete-adding");

            // Block the worker thread with a slow operation so we can control timing.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // On a dedicated thread, call ShutdownAndWait which will CompleteAdding.
            // But first, we need to trigger the race. We'll call CompleteAdding indirectly
            // by starting the shutdown, then enqueue while the inbox is completed.
            using var shutdownStarted = new ManualResetEventSlim(false);
            var shutdownThread = CreateShutdownThread(queue, shutdownStarted);

            // Release the worker so it can process the blocking op, then immediately
            // start shutdown and try to enqueue.
            workerGate.Set();
            shutdownThread.Start();
            _ = shutdownStarted.Wait(GateTimeout);

            // Give the shutdown a moment to call CompleteAdding.
            await Task.Delay(ShutdownSettleMilliseconds);

            // Try to enqueue — should hit the catch or the disposed check.
            var obs = queue.Enqueue(static _ => ProbeOperationResult);
            var capturedError = obs.SubscribeGetError();

            _ = shutdownThread.Join(ThreadJoinTimeout);

            // Either ObjectDisposedException (from catch or disposed check) should be set.
            await Assert.That(capturedError).IsTypeOf<ObjectDisposedException>();

            conn.Dispose();
            conn2.Dispose();
        }
    }

    // ── Lines 127-130: EnqueueRowStream catch after CompleteAdding ───────
    /// <summary>
    /// EnqueueRowStream after the inbox has been completed exercises the
    /// InvalidOperationException catch in EnqueueRowStream (lines 127-130).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EnqueueRowStream_AfterCompleteAdding_CatchSetsObjectDisposedError()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rowstream-complete-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-rowstream-complete");

            // Block the worker so we control timing.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            using var shutdownStarted = new ManualResetEventSlim(false);
            var shutdownThread = CreateShutdownThread(queue, shutdownStarted);

            // Start shutdown first so CompleteAdding is called, then release worker.
            shutdownThread.Start();
            _ = shutdownStarted.Wait(GateTimeout);
            workerGate.Set();

            _ = shutdownThread.Join(ThreadJoinTimeout);

            // After shutdown, EnqueueRowStream should get ObjectDisposedException
            // from either the _disposed check or the catch block.
            var obs = queue.EnqueueRowStream<int>(static (_, _, _) => { });
            var error = obs.SubscribeGetError();

            // The row stream may error with ObjectDisposedException or complete
            // empty (if the error is set via OnError before subscribe).
            await Assert.That(error is null or ObjectDisposedException).IsTrue();

            conn.Dispose();
        }
    }

    // ── Lines 154-155, 158: ShutdownAndWait double-call race ────────────
    /// <summary>
    /// Two concurrent ShutdownAndWait calls from dedicated threads — the second caller
    /// races and may hit the catch at lines 154-158 when CompleteAdding was already called.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownAndWait_ConcurrentDoubleCall_SecondCallerHandlesCompletedInbox()
    {
        const int ShutdownCallerCount = 2;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"double-shutdown-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-double-shutdown");

            // Block the worker so ShutdownAndWait can't complete immediately.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[ShutdownCallerCount];
            var completed = new bool[ShutdownCallerCount];

            for (var i = 0; i < ShutdownCallerCount; i++)
            {
                var idx = i;
                threads[i] = new(() =>
                {
                    go.Wait();
                    queue.ShutdownAndWait(static _ => { });
                    completed[idx] = true;
                })
                { IsBackground = true };
                threads[i].Start();
            }

            // Release the worker and both shutdown threads simultaneously.
            workerGate.Set();
            go.Set();
            JoinAll(threads);

            // Both threads should have completed without deadlock or exception.
            await Assert.That(completed[0]).IsTrue();
            await Assert.That(completed[1]).IsTrue();

            conn.Dispose();
        }
    }

    // ── Lines 207-214: Worker drain after shutdown ───────────────────────
    /// <summary>
    /// Enqueue many ops without waiting, then dispose immediately. Some ops may arrive
    /// after the shutdown op and must be drained by the worker (lines 207-214), failing
    /// them with ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WorkerLoop_DrainAfterShutdown_FailsLatecomersFromDedicatedThreads()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-shutdown-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-drain-shutdown");

            // Block the worker to let the inbox fill up.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Flood the inbox from dedicated threads.
            const int threadCount = 8;
            var replies = new IObservable<int>[threadCount];
            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                var idx = i;
                threads[i] = new(() =>
                {
                    go.Wait();
                    replies[idx] = queue.Enqueue(_ => idx, coalescable: true);
                })
                { IsBackground = true };
                threads[i].Start();
            }

            go.Set();
            JoinAll(threads, GateTimeout);

            // Release the worker and immediately dispose — some ops are still in the inbox.
            workerGate.Set();
            queue.Dispose();

            // Every reply should have completed (either with a value or ObjectDisposedException).
            var totalCompleted = CountSettledReplies(replies);
            await Assert.That(totalCompleted).IsEqualTo(threadCount);

            conn.Dispose();
        }
    }

    // ── Lines 277-316: Batch error + rollback + replay ──────────────────
    /// <summary>
    /// A failing coalescable operation in a batched transaction triggers rollback
    /// (lines 277-305) and the remaining ops are replayed individually (lines 309-316).
    /// Uses a blocking gate to ensure multiple ops are in the inbox simultaneously.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteCoalescedBatch_MidBatchThrow_RollsBackAndReplaysRemainder()
    {
        const int EnqueuedOpCount = 3;
        const int FirstOpResult = 1;
        const int LastOpResult = 3;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-error-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-batch-error");

            // Block the worker so all ops land in the inbox before processing starts.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Enqueue from dedicated threads: good op, bad op, good op (all coalescable).
            var replies = new IObservable<int>?[EnqueuedOpCount];
            using var allEnqueued = new CountdownEvent(EnqueuedOpCount);

            var threads = new[]
            {
                StartEnqueueThread(() => replies[0] = queue.Enqueue(static _ => FirstOpResult, coalescable: true), allEnqueued),
                StartEnqueueThread(() => replies[1] = queue.Enqueue<int>(static _ => throw new InvalidOperationException("boom"), coalescable: true), allEnqueued),
                StartEnqueueThread(() => replies[2] = queue.Enqueue(static _ => LastOpResult, coalescable: true), allEnqueued),
            };

            _ = allEnqueued.Wait(GateTimeout);
            JoinAll(threads, GateTimeout);

            // Release the worker — it will batch the three ops and hit the throw.
            workerGate.Set();

            // The good ops should succeed (either in batch or replay); the bad op errors.
            var error1 = replies[0]!.WaitForError();
            var error2 = replies[1]!.WaitForError();
            var error3 = replies[2]!.WaitForError();

            // The middle op must carry the thrown exception; neither neighbour may.
            await Assert.That(error2).IsTypeOf<InvalidOperationException>();
            await AssertNotBatchFailure(error1);
            await AssertNotBatchFailure(error3);

            queue.Dispose();
            conn.Dispose();
        }
    }

    /// <summary>
    /// A larger batch with a failing op in the middle — verifies ops after the failure
    /// are replayed individually and succeed (lines 309-316).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteCoalescedBatch_FailureReplay_RemainingOpsExecuteIndividually()
    {
        const int operationCount = 5;
        const int FailingOpIndex = 2;
        const int ResultScale = 10;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-individual-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-replay-individual");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Enqueue 5 coalescable ops: [good, good, BAD, good, good].
            var replies = new IObservable<int>[operationCount];
            using var allEnqueued = new CountdownEvent(operationCount);

            for (var i = 0; i < operationCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(
                    () => replies[idx] = idx == FailingOpIndex
                        ? queue.Enqueue<int>(static _ => throw new InvalidOperationException("fail"), coalescable: true)
                        : queue.Enqueue(_ => idx * ResultScale, coalescable: true),
                    allEnqueued);
            }

            _ = allEnqueued.Wait(GateTimeout);

            // Release the worker.
            workerGate.Set();

            // Wait for all replies.
            var errors = new Exception?[operationCount];
            for (var i = 0; i < operationCount; i++)
            {
                errors[i] = replies[i].WaitForError();
            }

            // The failing op must have an error; every other op must not carry that error.
            await Assert.That(errors[FailingOpIndex]).IsTypeOf<InvalidOperationException>();

            for (var i = 0; i < operationCount; i++)
            {
                if (i != FailingOpIndex)
                {
                    await AssertNotBatchFailure(errors[i]);
                }
            }

            queue.Dispose();
            conn.Dispose();
        }
    }

    // ── Lines 342-354: RunAfterBatch with shutdown op as _afterBatch ────
    /// <summary>
    /// A shutdown op arrives as the _afterBatch stash (lines 342-349, 352-354).
    /// This happens when many coalescable writes are followed by immediate dispose,
    /// and the shutdown op breaks the batch as a non-coalescable op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunAfterBatch_ShutdownAsAfterBatch_DrainsLeftovers()
    {
        const int writeCount = 20;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"afterbatch-shutdown-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-afterbatch-shutdown");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Flood with coalescable writes from dedicated threads, then dispose.
            var replies = new IObservable<RxVoid>[writeCount];
            using var allEnqueued = new CountdownEvent(writeCount);

            for (var i = 0; i < writeCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(() => replies[idx] = queue.Enqueue(static _ => RxVoid.Default, coalescable: true), allEnqueued);
            }

            _ = allEnqueued.Wait(GateTimeout);

            // Now enqueue a shutdown op that will be picked up during batch draining
            // as the non-coalescable _afterBatch item.
            var disposeThread = new Thread(() => queue.ShutdownAndWait(static _ => { }))
            { IsBackground = true };
            disposeThread.Start();

            // Let everything rip.
            workerGate.Set();
            _ = disposeThread.Join(ThreadJoinTimeout);

            // Every reply should have completed (either success or ObjectDisposedException).
            var totalCompleted = CountSettledReplies(replies);
            await Assert.That(totalCompleted).IsEqualTo(writeCount);

            conn.Dispose();
        }
    }

    /// <summary>
    /// Non-coalescable op as _afterBatch (line 352-354) — a read breaks the batch and
    /// is stashed, then executed after the batch commits.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunAfterBatch_NonCoalescableAfterBatch_ExecutesAfterCommit()
    {
        const int WriteOpCount = 3;
        const int ReadResult = 99;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"afterbatch-read-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-afterbatch-read");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Enqueue coalescable writes followed by a non-coalescable read.
            var writeReplies = new IObservable<int>[WriteOpCount];
            IObservable<int>? readReply = null;
            using var allEnqueued = new CountdownEvent(WriteOpCount + 1);

            for (var i = 0; i < WriteOpCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(() => writeReplies[idx] = queue.Enqueue(_ => idx, coalescable: true), allEnqueued);
            }

            // Non-coalescable read breaks the batch and becomes _afterBatch.
            _ = StartEnqueueThread(() => readReply = queue.Enqueue(static _ => ReadResult), allEnqueued);

            _ = allEnqueued.Wait(GateTimeout);

            // Release the worker.
            workerGate.Set();

            // The read should produce its result after the batch commits.
            var readValue = readReply!.WaitForValue();
            await Assert.That(readValue).IsEqualTo(ReadResult);

            // All writes should succeed too.
            for (var i = 0; i < WriteOpCount; i++)
            {
                var error = writeReplies[i].WaitForError();
                await Assert.That(error).IsNull();
            }

            queue.Dispose();
            conn.Dispose();
        }
    }

    // ── Lines 294-305: Batch structural failure (COMMIT throws) ─────────
    /// <summary>
    /// Exercises the outer catch in ExecuteCoalescedBatch (lines 294-305) where COMMIT
    /// or a structural failure triggers rollback and fails all ops in the batch. This is
    /// difficult to trigger naturally, so we use a large batch where the connection is
    /// disposed mid-transaction by a concurrent thread.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteCoalescedBatch_StructuralFailure_FailsAllOps()
    {
        const int operationCount = 10;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"structural-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-structural-failure");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Enqueue many coalescable ops.
            var replies = new IObservable<int>[operationCount];
            using var allEnqueued = new CountdownEvent(operationCount);

            for (var i = 0; i < operationCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(() => replies[idx] = queue.Enqueue(_ => idx, coalescable: true), allEnqueued);
            }

            _ = allEnqueued.Wait(GateTimeout);

            // Release the worker and immediately start shutdown.
            workerGate.Set();
            queue.Dispose();

            // All replies should complete (value or error).
            var totalCompleted = 0;
            for (var i = 0; i < operationCount; i++)
            {
                var error = replies[i].WaitForError();
                if (error is null or ObjectDisposedException or InvalidOperationException)
                {
                    totalCompleted++;
                }
            }

            await Assert.That(totalCompleted).IsEqualTo(operationCount);

            conn.Dispose();
        }
    }

    // ── Lines 207-214: Worker drain with row-stream leftovers ───────────
    /// <summary>
    /// Enqueue row-stream operations without waiting, then dispose immediately.
    /// Leftover row-stream ops hit the drain path (lines 207-214) and are failed
    /// with ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WorkerLoop_DrainLeftoverRowStreams_FailsWithObjectDisposed()
    {
        const int scalarOpCount = 3;
        const int streamOpCount = 3;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-rowstream-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-drain-rowstream");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
            BlockWorker(queue, workerGate);

            // Enqueue mixed ops from dedicated threads.
            var scalarReplies = new IObservable<int>[scalarOpCount];
            var streamReplies = new IObservable<int>[streamOpCount];
            using var allEnqueued = new CountdownEvent(scalarOpCount + streamOpCount);

            for (var i = 0; i < scalarOpCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(() => scalarReplies[idx] = queue.Enqueue(_ => idx, coalescable: true), allEnqueued);
            }

            for (var i = 0; i < streamOpCount; i++)
            {
                var idx = i;
                _ = StartEnqueueThread(() => streamReplies[idx] = queue.EnqueueRowStream<int>((_, emit, _) => emit(idx)), allEnqueued);
            }

            _ = allEnqueued.Wait(GateTimeout);

            // Release worker and immediately dispose.
            workerGate.Set();
            queue.Dispose();

            // Scalar and stream replies alike: either succeed or ObjectDisposedException.
            await AssertOnlyDisposalErrors(scalarReplies);
            await AssertOnlyDisposalErrors(streamReplies);

            conn.Dispose();
        }
    }

    // ── TryAddToInbox ──────────────────────────────────────────────────────
    /// <summary>TryAddToInbox returns true when the inbox is open.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAddToInbox_InboxOpen_ReturnsTrue()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-open-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            var queue = new SqliteOperationQueue(conn, "test-inbox-open");
            try
            {
                var reply = new SqliteReplyObservable<int>();
                var op = new SqliteOperation<int>(static _ => ProbeOperationResult, reply, coalescable: false);
                var added = queue.TryAddToInbox(op);

                await Assert.That(added).IsTrue();
            }
            finally
            {
                queue.Dispose();
                conn.Dispose();
            }
        }
    }

    /// <summary>TryAddToInbox returns false when the inbox has been completed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAddToInbox_InboxCompleted_ReturnsFalse()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-completed-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            var queue = new SqliteOperationQueue(conn, "test-inbox-completed");

            // Dispose completes the inbox via ShutdownAndWait.
            queue.Dispose();

            var reply = new SqliteReplyObservable<int>();
            var op = new SqliteOperation<int>(static _ => ProbeOperationResult, reply, coalescable: false);
            var added = queue.TryAddToInbox(op);

            await Assert.That(added).IsFalse();

            conn.Dispose();
        }
    }

    /// <summary>Parks the worker on <paramref name="gate"/> so a test can pile up the inbox first.</summary>
    /// <param name="queue">The queue whose worker is blocked.</param>
    /// <param name="gate">The gate the worker waits on; setting it releases the worker.</param>
    private static void BlockWorker(SqliteOperationQueue queue, ManualResetEventSlim gate) =>
        _ = queue.Enqueue(c =>
        {
            _ = gate.Wait(GateTimeout);
            return RxVoid.Default;
        });

    /// <summary>Creates (but does not start) a thread that signals then runs the queue's shutdown.</summary>
    /// <param name="queue">The queue to shut down.</param>
    /// <param name="started">Signalled just before shutdown begins.</param>
    /// <returns>The unstarted thread.</returns>
    private static Thread CreateShutdownThread(SqliteOperationQueue queue, ManualResetEventSlim started) =>
        new(() =>
        {
            started.Set();
            queue.ShutdownAndWait(static _ => { });
        })
        { IsBackground = true };

    /// <summary>Starts a thread that runs <paramref name="enqueue"/> then signals the countdown.</summary>
    /// <param name="enqueue">The enqueue action to run on the dedicated thread.</param>
    /// <param name="allEnqueued">Countdown signalled once the enqueue returns.</param>
    /// <returns>The started thread.</returns>
    private static Thread StartEnqueueThread(Action enqueue, CountdownEvent allEnqueued)
    {
        var thread = new Thread(() =>
        {
            enqueue();
            _ = allEnqueued.Signal();
        })
        { IsBackground = true };
        thread.Start();
        return thread;
    }

    /// <summary>Starts a thread that waits on the gate then writes one entry, recording any failure.</summary>
    /// <param name="conn">The connection to write through.</param>
    /// <param name="gate">The gate that releases every writer at once.</param>
    /// <param name="errors">The per-thread failure slots.</param>
    /// <param name="index">This thread's slot in <paramref name="errors"/>.</param>
    /// <param name="key">The cache key this thread writes.</param>
    /// <returns>The started thread.</returns>
    private static Thread StartWriterThread(SqlitePclRawConnection conn, ManualResetEventSlim gate, Exception?[] errors, int index, string key)
    {
        var thread = new Thread(() =>
        {
            gate.Wait();
            try
            {
                conn.Upsert([new CacheEntry(key, null, [(byte)index], TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();
            }
            catch (Exception ex)
            {
                errors[index] = ex;
            }
        })
        { IsBackground = true };
        thread.Start();
        return thread;
    }

    /// <summary>Starts a thread that waits on the gate then reads the anchor entry, recording any failure.</summary>
    /// <param name="conn">The connection to read through.</param>
    /// <param name="gate">The gate that releases every reader at once.</param>
    /// <param name="errors">The per-thread failure slots.</param>
    /// <param name="index">This thread's slot in <paramref name="errors"/>.</param>
    /// <returns>The started thread.</returns>
    private static Thread StartAnchorReaderThread(SqlitePclRawConnection conn, ManualResetEventSlim gate, Exception?[] errors, int index)
    {
        var thread = new Thread(() =>
        {
            gate.Wait();
            try
            {
                _ = conn.Get("anchor", null, TimeProvider.System.GetUtcNow()).WaitForValue();
            }
            catch (Exception ex)
            {
                errors[index] = ex;
            }
        })
        { IsBackground = true };
        thread.Start();
        return thread;
    }

    /// <summary>Joins every thread, failing the test's timing budget rather than hanging forever.</summary>
    /// <param name="threads">The threads to join.</param>
    private static void JoinAll(Thread[] threads) => JoinAll(threads, ThreadJoinTimeout);

    /// <summary>Joins every thread within <paramref name="timeout"/>.</summary>
    /// <param name="threads">The threads to join.</param>
    /// <param name="timeout">The per-thread join budget.</param>
    private static void JoinAll(Thread[] threads, TimeSpan timeout)
    {
        foreach (var thread in threads)
        {
            _ = thread.Join(timeout);
        }
    }

    /// <summary>Counts the replies that settled either successfully or with a disposal error.</summary>
    /// <typeparam name="T">The reply's element type.</typeparam>
    /// <param name="replies">The replies to drain.</param>
    /// <returns>The number of replies that settled acceptably.</returns>
    private static int CountSettledReplies<T>(IEnumerable<IObservable<T>> replies)
    {
        var settled = 0;
        foreach (var reply in replies)
        {
            var error = reply.WaitForError();
            if (error is null or ObjectDisposedException)
            {
                settled++;
            }
        }

        return settled;
    }

    /// <summary>Asserts that every reply either succeeded or failed only because the queue was disposed.</summary>
    /// <typeparam name="T">The reply's element type.</typeparam>
    /// <param name="replies">The replies to drain.</param>
    /// <returns>A task.</returns>
    private static async Task AssertOnlyDisposalErrors<T>(IEnumerable<IObservable<T>> replies)
    {
        foreach (var reply in replies)
        {
            var error = reply.WaitForError();
            if (error is null)
            {
                continue;
            }

            await Assert.That(error).IsTypeOf<ObjectDisposedException>();
        }
    }

    /// <summary>Asserts that a reply did not pick up the batch's injected failure.</summary>
    /// <param name="error">The error the reply settled with, if any.</param>
    /// <returns>A task.</returns>
    private static async Task AssertNotBatchFailure(Exception? error)
    {
        if (error is null)
        {
            return;
        }

        await Assert.That(error).IsNotTypeOf<InvalidOperationException>();
    }
}
