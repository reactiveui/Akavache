// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

using Akavache.EncryptedSqlite3;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

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

    /// <summary>
    /// Dispose calls ShutdownAndWait; subsequent enqueue returns ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_SubsequentEnqueue_ReturnsObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"dispose-{Guid.NewGuid()}.db");
            var queue = new SqliteOperationQueue(
                new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false),
                "test-dispose");

            queue.Dispose();

            var obs = queue.Enqueue(_ => 42);
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var replies = new List<IObservable<Unit>>();
            for (var i = 0; i < 20; i++)
            {
                replies.Add(conn.Upsert(
                    [new CacheEntry($"drain-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]));
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

            await Assert.That(completedCount).IsEqualTo(20);
        }
    }

    /// <summary>
    /// Single coalescable op runs without a transaction wrapper (fast path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_SingleOp_RunsWithoutTransactionWrapper()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"single-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("single", null, [99], DateTimeOffset.UtcNow, null)])
                    .WaitForCompletion();

                var entry = conn.Get("single", null, DateTimeOffset.UtcNow).WaitForValue();
                await Assert.That(entry).IsNotNull();
                await Assert.That(entry!.Value![0]).IsEqualTo((byte)99);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// Interleaved writes and reads exercise the afterBatch path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_NonCoalescableBreaksBatch()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"break-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("seed", null, [1], DateTimeOffset.UtcNow, null)])
                    .WaitForCompletion();

                for (var i = 0; i < 5; i++)
                {
                    conn.Upsert([new CacheEntry($"brk-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)])
                        .WaitForCompletion();
                    conn.Get("seed", null, DateTimeOffset.UtcNow).WaitForValue();
                }

                var entry = conn.Get("seed", null, DateTimeOffset.UtcNow).WaitForValue();
                await Assert.That(entry).IsNotNull();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// Coalescable-only writes — RunAfterBatch with null _afterBatch is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task RunAfterBatch_NoStashedOp_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"noop-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    conn.Upsert([new CacheEntry($"noop-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)])
                        .WaitForCompletion();
                }

                for (var i = 0; i < 5; i++)
                {
                    var entry = conn.Get($"noop-{i}", null, DateTimeOffset.UtcNow).WaitForValue();
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
    /// Enqueue after dispose — reply observable receives error, row stream completes empty.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Enqueue_AfterDispose_ReturnsErrorOrEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"post-dispose-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var error = conn.Get("nonexistent", null, DateTimeOffset.UtcNow).SubscribeGetError();
            await Assert.That(error).IsTypeOf<ObjectDisposedException>();

            var keys = conn.GetAllKeys(null, DateTimeOffset.UtcNow).ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
        }
    }

    /// <summary>
    /// Fire-and-forget writes then shutdown — no deadlock or exception.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ShutdownAndWait_FireAndForget_CompletesCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rapid-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            for (var i = 0; i < 20; i++)
            {
                _ = conn.Upsert([new CacheEntry($"rapid-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]);
            }

            conn.Dispose();
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Multiple sequential dispose calls are idempotent — second call waits on _workerExited.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_MultipleSequential_IsIdempotent()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"multi-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
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
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
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
                    threads[i] = new Thread(() =>
                    {
                        go.Wait();
                        try
                        {
                            conn.Upsert([new CacheEntry($"t-{idx}", null, [(byte)idx], DateTimeOffset.UtcNow, null)])
                                .WaitForCompletion();
                        }
                        catch (Exception ex)
                        {
                            errors[idx] = ex;
                        }
                    })
                    { IsBackground = true };
                    threads[i].Start();
                }

                go.Set();

                foreach (var t in threads)
                {
                    t.Join(TimeSpan.FromSeconds(30));
                }

                for (var i = 0; i < threadCount; i++)
                {
                    await Assert.That(errors[i]).IsNull();
                    var entry = conn.Get($"t-{i}", null, DateTimeOffset.UtcNow).WaitForValue();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"conc-dispose-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[3];

            for (var i = 0; i < 3; i++)
            {
                threads[i] = new Thread(() =>
                {
                    go.Wait();
                    conn.Dispose();
                })
                { IsBackground = true };
                threads[i].Start();
            }

            go.Set();

            foreach (var t in threads)
            {
                t.Join(TimeSpan.FromSeconds(30));
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Mixed writes and reads from dedicated threads exercise afterBatch + coalescing
    /// under real concurrency.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CoalescedBatch_MixedWritesAndReads_ViaDedicatedThreads()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"mixed-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert([new CacheEntry("anchor", null, [0xFF], DateTimeOffset.UtcNow, null)])
                    .WaitForCompletion();

                const int writerCount = 4;
                const int readerCount = 2;
                using var go = new ManualResetEventSlim(false);
                var threads = new Thread[writerCount + readerCount];
                var errors = new Exception?[writerCount + readerCount];

                for (var i = 0; i < writerCount; i++)
                {
                    var idx = i;
                    threads[i] = new Thread(() =>
                    {
                        go.Wait();
                        try
                        {
                            conn.Upsert([new CacheEntry($"w-{idx}", null, [(byte)idx], DateTimeOffset.UtcNow, null)])
                                .WaitForCompletion();
                        }
                        catch (Exception ex)
                        {
                            errors[idx] = ex;
                        }
                    })
                    { IsBackground = true };
                    threads[i].Start();
                }

                for (var i = 0; i < readerCount; i++)
                {
                    var idx = writerCount + i;
                    threads[idx] = new Thread(() =>
                    {
                        go.Wait();
                        try
                        {
                            conn.Get("anchor", null, DateTimeOffset.UtcNow).WaitForValue();
                        }
                        catch (Exception ex)
                        {
                            errors[idx] = ex;
                        }
                    })
                    { IsBackground = true };
                    threads[idx].Start();
                }

                go.Set();

                foreach (var t in threads)
                {
                    t.Join(TimeSpan.FromSeconds(30));
                }

                for (var i = 0; i < writerCount + readerCount; i++)
                {
                    await Assert.That(errors[i]).IsNull();
                }

                for (var i = 0; i < writerCount; i++)
                {
                    var entry = conn.Get($"w-{i}", null, DateTimeOffset.UtcNow).WaitForValue();
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
    /// Writes from dedicated threads then dispose — exercises shutdown-as-afterBatch path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task RunAfterBatch_ShutdownDuringConcurrentWrites()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"shutdown-batch-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var observables = new List<IObservable<Unit>>();
            for (var i = 0; i < 30; i++)
            {
                observables.Add(conn.Upsert(
                    [new CacheEntry($"ab-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]));
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

            await Assert.That(totalCompleted).IsEqualTo(30);
        }
    }

    // ── TryAddToInbox ──────────────────────────────────────────────────────

    /// <summary>
    /// TryAddToInbox returns true when the inbox is open.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryAddToInbox_InboxOpen_ReturnsTrue()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-open-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            var queue = new SqliteOperationQueue(conn, "test-inbox-open");
            try
            {
                var reply = new SqliteReplyObservable<int>();
                var op = new SqliteOperation<int>(_ => 42, reply, coalescable: false);
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

    /// <summary>
    /// TryAddToInbox returns false when the inbox has been completed.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryAddToInbox_InboxCompleted_ReturnsFalse()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-completed-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            var queue = new SqliteOperationQueue(conn, "test-inbox-completed");

            // Dispose completes the inbox via ShutdownAndWait.
            queue.Dispose();

            var reply = new SqliteReplyObservable<int>();
            var op = new SqliteOperation<int>(_ => 42, reply, coalescable: false);
            var added = queue.TryAddToInbox(op);

            await Assert.That(added).IsFalse();

            conn.Dispose();
        }
    }

    // ── DrainLeftovers (static) ────────────────────────────────────────────

    /// <summary>
    /// DrainLeftovers skips SqliteShutdownOperation instances and fails regular ops
    /// with ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task DrainLeftovers_MixedOps_SkipsShutdownAndFailsRegular()
    {
        var inbox = new BlockingCollection<ISqliteOperation>();

        var reply1 = new SqliteReplyObservable<int>();
        var op1 = new SqliteOperation<int>(_ => 1, reply1, coalescable: false);
        inbox.Add(op1);

        inbox.Add(new SqliteShutdownOperation(static _ => { }));

        var reply2 = new SqliteReplyObservable<int>();
        var op2 = new SqliteOperation<int>(_ => 2, reply2, coalescable: true);
        inbox.Add(op2);

        inbox.Add(new SqliteShutdownOperation(static _ => { }));

        var reply3 = new SqliteReplyObservable<int>();
        var op3 = new SqliteOperation<int>(_ => 3, reply3, coalescable: false);
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

    /// <summary>
    /// DrainLeftovers with an empty inbox is a no-op.
    /// </summary>
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

    /// <summary>
    /// ExecuteBatchInTransaction commits all ops when none throw.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteBatchInTransaction_AllSucceed_CommitsTransaction()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-commit-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 10, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => 20, reply2, coalescable: true),
                    new SqliteOperation<int>(_ => 30, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // All replies should have completed successfully (single-subscriber).
                var val1 = reply1.SubscribeGetValue();
                var val2 = reply2.SubscribeGetValue();
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val1).IsEqualTo(10);
                await Assert.That(val2).IsEqualTo(20);
                await Assert.That(val3).IsEqualTo(30);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// ExecuteBatchInTransaction with a mid-batch failure rolls back and replays
    /// remaining ops individually.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteBatchInTransaction_MidBatchFailure_RollsBackAndReplays()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-mid-fail-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 10, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => throw new InvalidOperationException("mid-batch-boom"), reply2, coalescable: true),
                    new SqliteOperation<int>(_ => 30, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // The failing op (index 1) receives the thrown exception via Execute's catch.
                var error2 = reply2.SubscribeGetError();
                await Assert.That(error2).IsTypeOf<InvalidOperationException>();

                // The op after the failure (index 2) is replayed individually and should succeed.
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(30);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── FailAllOps (static) ────────────────────────────────────────────────

    /// <summary>
    /// FailAllOps sets InvalidOperationException on every op's reply observable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task FailAllOps_SetsInvalidOperationOnAllOps()
    {
        var reply1 = new SqliteReplyObservable<int>();
        var reply2 = new SqliteReplyObservable<int>();
        var reply3 = new SqliteReplyObservable<int>();

        var batch = new List<ISqliteOperation>
        {
            new SqliteOperation<int>(_ => 1, reply1, coalescable: true),
            new SqliteOperation<int>(_ => 2, reply2, coalescable: true),
            new SqliteOperation<int>(_ => 3, reply3, coalescable: true),
        };

        SqliteOperationQueue.FailAllOps(batch);

        var error1 = reply1.SubscribeGetError();
        var error2 = reply2.SubscribeGetError();
        var error3 = reply3.SubscribeGetError();

        await Assert.That(error1).IsTypeOf<InvalidOperationException>();
        await Assert.That(error2).IsTypeOf<InvalidOperationException>();
        await Assert.That(error3).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>
    /// FailAllOps on an empty batch is a no-op.
    /// </summary>
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

    /// <summary>
    /// ReplayRemainingOps executes only ops from the given startIndex onward.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayRemainingOps_FromStartIndex_ExecutesOnlyRemaining()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-start-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var executed = new bool[4];

                var reply0 = new SqliteReplyObservable<int>();
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(
                        _ =>
                        {
                            executed[0] = true;
                            return 0;
                        },
                        reply0,
                        coalescable: true),
                    new SqliteOperation<int>(
                        _ =>
                        {
                            executed[1] = true;
                            return 1;
                        },
                        reply1,
                        coalescable: true),
                    new SqliteOperation<int>(
                        _ =>
                        {
                            executed[2] = true;
                            return 2;
                        },
                        reply2,
                        coalescable: true),
                    new SqliteOperation<int>(
                        _ =>
                        {
                            executed[3] = true;
                            return 3;
                        },
                        reply3,
                        coalescable: true),
                };

                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 2);

                // Only ops at index 2 and 3 should have been executed.
                await Assert.That(executed[0]).IsFalse();
                await Assert.That(executed[1]).IsFalse();
                await Assert.That(executed[2]).IsTrue();
                await Assert.That(executed[3]).IsTrue();

                // The executed ops should have their results.
                var val2 = reply2.SubscribeGetValue();
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(2);
                await Assert.That(val3).IsEqualTo(3);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// ReplayRemainingOps with startIndex at batch length is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReplayRemainingOps_StartIndexAtEnd_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-end-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var executed = false;
                var reply = new SqliteReplyObservable<int>();
                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(
                        _ =>
                        {
                            executed = true;
                            return 1;
                        },
                        reply,
                        coalescable: true),
                };

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
            var conn = new SqlitePclRawConnection(dbPath, password: TestPassword, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply0 = new SqliteReplyObservable<int>();
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 0, reply0, coalescable: true),
                    new SqliteOperation<int>(_ => throw new InvalidOperationException("replay-boom"), reply1, coalescable: true),
                    new SqliteOperation<int>(_ => 2, reply2, coalescable: true),
                };

                // Replay from index 1 — includes the failing op and the one after it.
                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 1);

                // The failing op should have received the error.
                var error1 = reply1.SubscribeGetError();
                await Assert.That(error1).IsTypeOf<InvalidOperationException>();

                // The op after the failure should still succeed.
                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(2);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }
}
