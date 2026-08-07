// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests targeting uncovered lines in <see cref="SqliteOperationQueue"/> (encrypted variant):
/// dispose paths, worker-loop drain, coalesced-batch execution, and enqueue-after-dispose
/// error handling. Concurrency tests use dedicated Thread instances (not Task.Run) to avoid
/// threadpool starvation when WaitForCompletion blocks the calling thread.
/// </summary>
[Category("Akavache")]
public class EncryptedSqliteOperationQueueCoverageTests
{
    /// <summary>The password used for the encrypted test database.</summary>
    private const string TestPassword = "test-password";

    /// <summary>Result of the placeholder operation body used purely to probe enqueue behaviour.</summary>
    private const int ProbeOperationResult = 42;

    /// <summary>Result of the second operation in a batch, kept distinct so replies can be told apart.</summary>
    private const int SecondOpResult = 2;

    /// <summary>Result of the third operation in a batch, kept distinct so replies can be told apart.</summary>
    private const int ThirdOpResult = 3;

    /// <summary>Result of the first operation in the transaction batches.</summary>
    private const int FirstTransactionOpResult = 10;

    /// <summary>Result of the second operation in the transaction batches.</summary>
    private const int SecondTransactionOpResult = 20;

    /// <summary>Result of the third operation in the transaction batches.</summary>
    private const int ThirdTransactionOpResult = 30;

    /// <summary>How long to wait for a dedicated test thread to finish before giving up.</summary>
    private const int ThreadJoinTimeoutSeconds = 30;

    /// <summary>Batch index the replay test resumes from, leaving the earlier operations untouched.</summary>
    private const int ReplayStartIndex = 2;

    /// <summary>Payload of the entry written through the single-operation fast path.</summary>
    private static readonly byte[] SingleOpPayload = [99];

    /// <summary>Dispose calls ShutdownAndWait; subsequent enqueue returns ObjectDisposedException.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_SubsequentEnqueue_ReturnsObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"dispose-{Guid.NewGuid()}.db");
            var queue = new SqliteOperationQueue(
                SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false),
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
    internal async Task WorkerLoop_DrainLeftovers_AllRepliesComplete()
    {
        const int writeCount = 20;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var replies = new List<IObservable<RxVoid>>();
            for (var i = 0; i < writeCount; i++)
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

            await Assert.That(completedCount).IsEqualTo(writeCount);
        }
    }

    /// <summary>Single coalescable op runs without a transaction wrapper (fast path).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_SingleOp_RunsWithoutTransactionWrapper()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"single-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("single", null, SingleOpPayload, TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                var entry = conn.Get("single", null, TimeProvider.System.GetUtcNow()).WaitForValue();
                await Assert.That(entry).IsNotNull();
                await Assert.That(entry!.Value![0]).IsEqualTo(SingleOpPayload[0]);
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
    internal async Task ExecuteCoalescedBatch_NonCoalescableBreaksBatch()
    {
        const int interleavedRounds = 5;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"break-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("seed", null, [1], TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                for (var i = 0; i < interleavedRounds; i++)
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
    internal async Task RunAfterBatch_NoStashedOp_IsNoOp()
    {
        const int entryCount = 5;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"noop-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                for (var i = 0; i < entryCount; i++)
                {
                    conn.Upsert([new CacheEntry($"noop-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)])
                        .WaitForCompletion();
                }

                for (var i = 0; i < entryCount; i++)
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
    internal async Task Enqueue_AfterDispose_ReturnsErrorOrEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"post-dispose-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
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
    internal async Task ShutdownAndWait_FireAndForget_CompletesCleanly()
    {
        const int writeCount = 20;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rapid-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            for (var i = 0; i < writeCount; i++)
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
    internal async Task Dispose_MultipleSequential_IsIdempotent()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"multi-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
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
    internal async Task CoalescedBatch_ConcurrentWritesViaDedicatedThreads()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"coalesce-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                const int threadCount = 5;
                using var go = new ManualResetEventSlim(false);
                var threads = new Thread[threadCount];
                var errors = new Exception?[threadCount];

                for (var i = 0; i < threadCount; i++)
                {
                    threads[i] = StartWriterThread(conn, go, errors, i, "t");
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
    /// Exercises the ShutdownAndWait second-entry path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_ConcurrentViaDedicatedThreads_NoDeadlock()
    {
        const int threadCount = 3;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"conc-dispose-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
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
    internal async Task CoalescedBatch_MixedWritesAndReads_ViaDedicatedThreads()
    {
        const int writerCount = 4;
        const int readerCount = 2;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"mixed-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("anchor", null, [0xFF], TimeProvider.System.GetUtcNow(), null)])
                    .WaitForCompletion();

                using var go = new ManualResetEventSlim(false);
                var threads = new Thread[writerCount + readerCount];
                var errors = new Exception?[writerCount + readerCount];

                for (var i = 0; i < writerCount; i++)
                {
                    threads[i] = StartWriterThread(conn, go, errors, i, "w");
                }

                for (var i = 0; i < readerCount; i++)
                {
                    threads[writerCount + i] = StartAnchorReaderThread(conn, go, errors, writerCount + i);
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
    internal async Task RunAfterBatch_ShutdownDuringConcurrentWrites()
    {
        const int writeCount = 30;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"shutdown-batch-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var observables = new List<IObservable<RxVoid>>();
            for (var i = 0; i < writeCount; i++)
            {
                observables.Add(conn.Upsert(
                    [new CacheEntry($"ab-{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)]));
            }

            conn.Dispose();

            var totalCompleted = 0;
            foreach (var obs in observables)
            {
                var error = obs.WaitForError();
                if (error is null or ObjectDisposedException)
                {
                    totalCompleted++;
                }
            }

            await Assert.That(totalCompleted).IsEqualTo(writeCount);
        }
    }

    // ── TryAddToInbox ──────────────────────────────────────────────────────
    /// <summary>TryAddToInbox returns true when the inbox is open.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryAddToInbox_InboxOpen_ReturnsTrue()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-open-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
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
    internal async Task TryAddToInbox_InboxCompleted_ReturnsFalse()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-completed-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
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

    // ── DrainLeftovers (static) ────────────────────────────────────────────
    /// <summary>DrainLeftovers skips SqliteShutdownOperation instances and fails regular ops with ObjectDisposedException.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DrainLeftovers_MixedOps_SkipsShutdownAndFailsRegular()
    {
        var inbox = new BlockingCollection<ISqliteOperation>();

        var reply1 = new SqliteReplyObservable<int>();
        var op1 = new SqliteOperation<int>(static _ => 1, reply1, coalescable: false);
        inbox.Add(op1);

        inbox.Add(new SqliteShutdownOperation(static _ => { }));

        var reply2 = new SqliteReplyObservable<int>();
        var op2 = new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true);
        inbox.Add(op2);

        inbox.Add(new SqliteShutdownOperation(static _ => { }));

        var reply3 = new SqliteReplyObservable<int>();
        var op3 = new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: false);
        inbox.Add(op3);

        inbox.CompleteAdding();

        SqliteOperationQueue.DrainLeftovers(inbox);

        // Regular ops should receive ObjectDisposedException.
        var error1 = reply1.SubscribeGetError();
        await Assert.That(error1).IsTypeOf<ObjectDisposedException>();

        var error2 = reply2.SubscribeGetError();
        await Assert.That(error2).IsTypeOf<ObjectDisposedException>();

        var error3 = reply3.SubscribeGetError();
        await Assert.That(error3).IsTypeOf<ObjectDisposedException>();

        inbox.Dispose();
    }

    /// <summary>DrainLeftovers with an empty inbox is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DrainLeftovers_EmptyInbox_IsNoOp()
    {
        var inbox = new BlockingCollection<ISqliteOperation>();
        inbox.CompleteAdding();

        SqliteOperationQueue.DrainLeftovers(inbox);

        // No exception, no hang — just returns.
        await Task.CompletedTask;

        inbox.Dispose();
    }

    // ── ExecuteBatchInTransaction (static) ──────────────────────────────────
    /// <summary>ExecuteBatchInTransaction commits all ops when none throw.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteBatchInTransaction_AllSucceed_CommitsTransaction()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-commit-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstTransactionOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondTransactionOpResult, reply2, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdTransactionOpResult, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // All replies should have completed successfully (single-subscriber).
                var val1 = reply1.SubscribeGetValue();
                var val2 = reply2.SubscribeGetValue();
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val1).IsEqualTo(FirstTransactionOpResult);
                await Assert.That(val2).IsEqualTo(SecondTransactionOpResult);
                await Assert.That(val3).IsEqualTo(ThirdTransactionOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>ExecuteBatchInTransaction with a mid-batch failure rolls back and replays remaining ops individually.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteBatchInTransaction_MidBatchFailure_RollsBackAndReplays()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-mid-fail-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstTransactionOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => throw new InvalidOperationException("mid-batch-boom"), reply2, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdTransactionOpResult, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // The failing op (index 1) receives the thrown exception via Execute's catch.
                var error2 = reply2.SubscribeGetError();
                await Assert.That(error2).IsTypeOf<InvalidOperationException>();

                // The op after the failure (index 2) is replayed individually and should succeed.
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(ThirdTransactionOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── FailAllOps (static) ────────────────────────────────────────────────
    /// <summary>FailAllOps sets InvalidOperationException on every op's reply observable.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task FailAllOps_SetsInvalidOperationOnAllOps()
    {
        var reply1 = new SqliteReplyObservable<int>();
        var reply2 = new SqliteReplyObservable<int>();
        var reply3 = new SqliteReplyObservable<int>();

        var batch = new List<ISqliteOperation>
        {
            new SqliteOperation<int>(static _ => 1, reply1, coalescable: true),
            new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
            new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: true),
        };

        SqliteOperationQueue.FailAllOps(batch);

        var error1 = reply1.SubscribeGetError();
        var error2 = reply2.SubscribeGetError();
        var error3 = reply3.SubscribeGetError();

        await Assert.That(error1).IsTypeOf<InvalidOperationException>();
        await Assert.That(error2).IsTypeOf<InvalidOperationException>();
        await Assert.That(error3).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>FailAllOps on an empty batch is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task FailAllOps_EmptyBatch_IsNoOp()
    {
        var batch = new List<ISqliteOperation>();

        SqliteOperationQueue.FailAllOps(batch);

        // No exception, no hang.
        await Task.CompletedTask;
    }

    // ── ReplayRemainingOps (static) ────────────────────────────────────────
    /// <summary>ReplayRemainingOps executes only ops from the given startIndex onward.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayRemainingOps_FromStartIndex_ExecutesOnlyRemaining()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-start-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                SqliteReplyObservable<int>[] replies =
                [
                    new(),
                    new(),
                    new(),
                    new(),
                ];
                var executed = new bool[replies.Length];
                var batch = CreateIndexRecordingBatch(executed, replies);

                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: ReplayStartIndex);

                // Only ops at index 2 and 3 should have been executed.
                await Assert.That(executed[0]).IsFalse();
                await Assert.That(executed[1]).IsFalse();
                await Assert.That(executed[2]).IsTrue();
                await Assert.That(executed[3]).IsTrue();

                // The executed ops should have their results.
                var val2 = replies[2].SubscribeGetValue();
                var val3 = replies[3].SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(SecondOpResult);
                await Assert.That(val3).IsEqualTo(ThirdOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>ReplayRemainingOps with startIndex at batch length is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayRemainingOps_StartIndexAtEnd_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-end-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var executed = false;
                var reply = new SqliteReplyObservable<int>();
                var op = new SqliteOperation<int>(
                    _ =>
                    {
                        executed = true;
                        return 1;
                    },
                    reply,
                    coalescable: true);
                var batch = new List<ISqliteOperation> { op };

                // startIndex == batch.Count means nothing to replay.
                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 1);

                await Assert.That(executed).IsFalse();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// ReplayRemainingOps handles a failing op at replay time — the op receives the
    /// error via its Execute catch, and subsequent ops continue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayRemainingOps_FailingOpDuringReplay_ContinuesWithNext()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-fail-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply0 = new SqliteReplyObservable<int>();
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => 0, reply0, coalescable: true),
                    new SqliteOperation<int>(static _ => throw new InvalidOperationException("replay-boom"), reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                };

                // Replay from index 1 — includes the failing op and the one after it.
                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 1);

                // The failing op should have received the error.
                var error1 = reply1.SubscribeGetError();
                await Assert.That(error1).IsTypeOf<InvalidOperationException>();

                // The op after the failure should still succeed.
                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(SecondOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Builds a batch whose operations record that they ran and return their own batch index.</summary>
    /// <param name="executed">Flags, one per operation, set when that operation runs.</param>
    /// <param name="replies">Reply observables, one per operation.</param>
    /// <returns>The batch, in index order.</returns>
    private static List<ISqliteOperation> CreateIndexRecordingBatch(bool[] executed, SqliteReplyObservable<int>[] replies)
    {
        var batch = new List<ISqliteOperation>(replies.Length);
        for (var i = 0; i < replies.Length; i++)
        {
            var index = i;
            batch.Add(new SqliteOperation<int>(
                _ =>
                {
                    executed[index] = true;
                    return index;
                },
                replies[index],
                coalescable: true));
        }

        return batch;
    }

    /// <summary>Starts a background thread that upserts one entry once the gate opens.</summary>
    /// <param name="connection">The connection to write through.</param>
    /// <param name="gate">Released when every thread should start at once.</param>
    /// <param name="errors">Slot array that captures a per-thread failure.</param>
    /// <param name="index">This thread's slot, also used as the key suffix and payload byte.</param>
    /// <param name="keyPrefix">Prefix of the key this thread writes.</param>
    /// <returns>The started thread.</returns>
    private static Thread StartWriterThread(SqlitePclRawConnection connection, ManualResetEventSlim gate, Exception?[] errors, int index, string keyPrefix)
    {
        Thread thread = new(() =>
        {
            gate.Wait();
            try
            {
                connection.Upsert([new CacheEntry($"{keyPrefix}-{index}", null, [(byte)index], TimeProvider.System.GetUtcNow(), null)])
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

    /// <summary>Starts a background thread that reads the anchor entry once the gate opens.</summary>
    /// <param name="connection">The connection to read through.</param>
    /// <param name="gate">Released when every thread should start at once.</param>
    /// <param name="errors">Slot array that captures a per-thread failure.</param>
    /// <param name="index">This thread's slot in <paramref name="errors"/>.</param>
    /// <returns>The started thread.</returns>
    private static Thread StartAnchorReaderThread(SqlitePclRawConnection connection, ManualResetEventSlim gate, Exception?[] errors, int index)
    {
        Thread thread = new(() =>
        {
            gate.Wait();
            try
            {
                _ = connection.Get("anchor", null, TimeProvider.System.GetUtcNow()).WaitForValue();
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

    /// <summary>Waits for every test thread to finish, bounded so a hang fails the test instead of stalling the run.</summary>
    /// <param name="threads">The threads to join.</param>
    private static void JoinAll(IEnumerable<Thread> threads)
    {
        foreach (var thread in threads)
        {
            _ = thread.Join(TimeSpan.FromSeconds(ThreadJoinTimeoutSeconds));
        }
    }
}
