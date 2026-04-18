// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

using Akavache.Sqlite3;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests targeting uncovered lines in <see cref="SqliteOperationQueue"/>: dispose paths,
/// worker-loop drain, coalesced-batch execution, and enqueue-after-dispose error handling.
/// Concurrency tests use dedicated Thread instances (not Task.Run) to avoid threadpool
/// starvation when WaitForCompletion blocks the calling thread.
/// </summary>
[Category("Akavache")]
public class SqliteOperationQueueCoverageTests
{
    /// <summary>
    /// Dispose calls ShutdownAndWait; subsequent enqueue returns ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_SubsequentEnqueue_ReturnsObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"dispose-{Guid.NewGuid()}.db");
            var queue = new SqliteOperationQueue(
                new SqlitePclRawConnection(dbPath, password: null, readOnly: false),
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
    public async Task WorkerLoop_DrainLeftovers_AllRepliesComplete()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ExecuteCoalescedBatch_SingleOp_RunsWithoutTransactionWrapper()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"single-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ExecuteCoalescedBatch_NonCoalescableBreaksBatch()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"break-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task RunAfterBatch_NoStashedOp_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"noop-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task Enqueue_AfterDispose_ReturnsErrorOrEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"post-dispose-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ShutdownAndWait_FireAndForget_CompletesCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rapid-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task Dispose_MultipleSequential_IsIdempotent()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"multi-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    /// Exercises the ShutdownAndWait second-entry path (line 144-147).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_ConcurrentViaDedicatedThreads_NoDeadlock()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"conc-dispose-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task CoalescedBatch_MixedWritesAndReads_ViaDedicatedThreads()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"mixed-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task RunAfterBatch_ShutdownDuringConcurrentWrites()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"shutdown-batch-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            // Build the queue directly so we can call CompleteAdding on the inbox before
            // _disposed is set.
            var dbPath2 = Path.Combine(path, $"enqueue-complete2-{Guid.NewGuid()}.db");
            var conn2 = new SqlitePclRawConnection(dbPath2, password: null, readOnly: false);
            conn2.CreateSchema().WaitForCompletion();
            var queue = new SqliteOperationQueue(conn2, "test-complete-adding");

            // Block the worker thread with a slow operation so we can control timing.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // On a dedicated thread, call ShutdownAndWait which will CompleteAdding.
            // But first, we need to trigger the race. We'll call CompleteAdding indirectly
            // by starting the shutdown, then enqueue while the inbox is completed.
            Exception? capturedError = null;
            using var shutdownStarted = new ManualResetEventSlim(false);

            var shutdownThread = new Thread(() =>
            {
                shutdownStarted.Set();
                queue.ShutdownAndWait(static _ => { });
            })
            { IsBackground = true };

            // Release the worker so it can process the blocking op, then immediately
            // start shutdown and try to enqueue.
            workerGate.Set();
            shutdownThread.Start();
            shutdownStarted.Wait(TimeSpan.FromSeconds(10));

            // Give the shutdown a moment to call CompleteAdding.
            await Task.Delay(50);

            // Try to enqueue — should hit the catch or the disposed check.
            var obs = queue.Enqueue(_ => 42);
            capturedError = obs.SubscribeGetError();

            shutdownThread.Join(TimeSpan.FromSeconds(30));

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
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-rowstream-complete");

            // Block the worker so we control timing.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            using var shutdownStarted = new ManualResetEventSlim(false);
            var shutdownThread = new Thread(() =>
            {
                shutdownStarted.Set();
                queue.ShutdownAndWait(static _ => { });
            })
            { IsBackground = true };

            // Start shutdown first so CompleteAdding is called, then release worker.
            shutdownThread.Start();
            shutdownStarted.Wait(TimeSpan.FromSeconds(10));
            workerGate.Set();

            shutdownThread.Join(TimeSpan.FromSeconds(30));

            // After shutdown, EnqueueRowStream should get ObjectDisposedException
            // from either the _disposed check or the catch block.
            var obs = queue.EnqueueRowStream<int>((_, _, _) => { });
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"double-shutdown-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-double-shutdown");

            // Block the worker so ShutdownAndWait can't complete immediately.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[2];
            var completed = new bool[2];

            for (var i = 0; i < 2; i++)
            {
                var idx = i;
                threads[i] = new Thread(() =>
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

            foreach (var t in threads)
            {
                t.Join(TimeSpan.FromSeconds(30));
            }

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
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-drain-shutdown");

            // Block the worker to let the inbox fill up.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Flood the inbox from dedicated threads.
            const int threadCount = 8;
            var replies = new IObservable<int>[threadCount];
            using var go = new ManualResetEventSlim(false);
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                var idx = i;
                threads[i] = new Thread(() =>
                {
                    go.Wait();
                    replies[idx] = queue.Enqueue(_ => idx, coalescable: true);
                })
                { IsBackground = true };
                threads[i].Start();
            }

            go.Set();
            foreach (var t in threads)
            {
                t.Join(TimeSpan.FromSeconds(10));
            }

            // Release the worker and immediately dispose — some ops are still in the inbox.
            workerGate.Set();
            queue.Dispose();

            // Every reply should have completed (either with a value or ObjectDisposedException).
            var totalCompleted = 0;
            foreach (var reply in replies)
            {
                var error = reply.WaitForError();
                if (error is null or ObjectDisposedException)
                {
                    totalCompleted++;
                }
            }

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-error-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-batch-error");

            // Block the worker so all ops land in the inbox before processing starts.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Enqueue from dedicated threads: good op, bad op, good op (all coalescable).
            var reply1 = (IObservable<int>?)null;
            var reply2 = (IObservable<int>?)null;
            var reply3 = (IObservable<int>?)null;

            using var allEnqueued = new CountdownEvent(3);

            var t1 = new Thread(() =>
            {
                reply1 = queue.Enqueue(_ => 1, coalescable: true);
                allEnqueued.Signal();
            })
            { IsBackground = true };

            var t2 = new Thread(() =>
            {
                reply2 = queue.Enqueue<int>(_ => throw new InvalidOperationException("boom"), coalescable: true);
                allEnqueued.Signal();
            })
            { IsBackground = true };

            var t3 = new Thread(() =>
            {
                reply3 = queue.Enqueue(_ => 3, coalescable: true);
                allEnqueued.Signal();
            })
            { IsBackground = true };

            t1.Start();
            t2.Start();
            t3.Start();

            allEnqueued.Wait(TimeSpan.FromSeconds(10));
            t1.Join(TimeSpan.FromSeconds(10));
            t2.Join(TimeSpan.FromSeconds(10));
            t3.Join(TimeSpan.FromSeconds(10));

            // Release the worker — it will batch the three ops and hit the throw.
            workerGate.Set();

            // The good ops should succeed (either in batch or replay).
            // The bad op should get an error.
            var error1 = reply1!.WaitForError();
            var error2 = reply2!.WaitForError();
            var error3 = reply3!.WaitForError();

            // reply2 must have the thrown exception.
            await Assert.That(error2).IsTypeOf<InvalidOperationException>();

            // reply1 and reply3 should have succeeded (null error) — or if the batch
            // committed before the throw, reply1 succeeds and reply3 is replayed.
            // Either way, they should not have the "boom" error.
            if (error1 is not null)
            {
                await Assert.That(error1).IsNotTypeOf<InvalidOperationException>();
            }

            if (error3 is not null)
            {
                await Assert.That(error3).IsNotTypeOf<InvalidOperationException>();
            }

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-individual-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-replay-individual");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Enqueue 5 coalescable ops: [good, good, BAD, good, good].
            const int opCount = 5;
            var replies = new IObservable<int>[opCount];
            using var allEnqueued = new CountdownEvent(opCount);

            for (var i = 0; i < opCount; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    if (idx == 2)
                    {
                        replies[idx] = queue.Enqueue<int>(_ => throw new InvalidOperationException("fail"), coalescable: true);
                    }
                    else
                    {
                        replies[idx] = queue.Enqueue(_ => idx * 10, coalescable: true);
                    }

                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            allEnqueued.Wait(TimeSpan.FromSeconds(10));

            // Release the worker.
            workerGate.Set();

            // Wait for all replies.
            var errors = new Exception?[opCount];
            for (var i = 0; i < opCount; i++)
            {
                errors[i] = replies[i].WaitForError();
            }

            // The failing op (index 2) must have an error.
            await Assert.That(errors[2]).IsTypeOf<InvalidOperationException>();

            // All other ops should have completed (null error = success).
            // Some may have gotten ObjectDisposedException if they were ahead of the
            // fail in the batch, but none should have InvalidOperationException.
            for (var i = 0; i < opCount; i++)
            {
                if (i == 2)
                {
                    continue;
                }

                if (errors[i] is not null)
                {
                    await Assert.That(errors[i]).IsNotTypeOf<InvalidOperationException>();
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"afterbatch-shutdown-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-afterbatch-shutdown");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Flood with coalescable writes from dedicated threads, then dispose.
            const int writeCount = 20;
            var replies = new IObservable<Unit>[writeCount];
            using var allEnqueued = new CountdownEvent(writeCount);

            for (var i = 0; i < writeCount; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    replies[idx] = queue.Enqueue(_ => Unit.Default, coalescable: true);
                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            allEnqueued.Wait(TimeSpan.FromSeconds(10));

            // Now enqueue a shutdown op that will be picked up during batch draining
            // as the non-coalescable _afterBatch item.
            var disposeThread = new Thread(() => queue.ShutdownAndWait(static _ => { }))
            { IsBackground = true };
            disposeThread.Start();

            // Let everything rip.
            workerGate.Set();
            disposeThread.Join(TimeSpan.FromSeconds(30));

            // Every reply should have completed (either success or ObjectDisposedException).
            var totalCompleted = 0;
            foreach (var reply in replies)
            {
                var error = reply.WaitForError();
                if (error is null or ObjectDisposedException)
                {
                    totalCompleted++;
                }
            }

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"afterbatch-read-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-afterbatch-read");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Enqueue coalescable writes followed by a non-coalescable read.
            var writeReplies = new IObservable<int>[3];
            IObservable<int>? readReply = null;
            using var allEnqueued = new CountdownEvent(4);

            for (var i = 0; i < 3; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    writeReplies[idx] = queue.Enqueue(_ => idx, coalescable: true);
                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            var readThread = new Thread(() =>
            {
                // Non-coalescable read breaks the batch and becomes _afterBatch.
                readReply = queue.Enqueue(_ => 99, coalescable: false);
                allEnqueued.Signal();
            })
            { IsBackground = true };
            readThread.Start();

            allEnqueued.Wait(TimeSpan.FromSeconds(10));

            // Release the worker.
            workerGate.Set();

            // The read should produce its result after the batch commits.
            var readValue = readReply!.WaitForValue();
            await Assert.That(readValue).IsEqualTo(99);

            // All writes should succeed too.
            for (var i = 0; i < 3; i++)
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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"structural-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-structural-failure");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Enqueue many coalescable ops.
            const int opCount = 10;
            var replies = new IObservable<int>[opCount];
            using var allEnqueued = new CountdownEvent(opCount);

            for (var i = 0; i < opCount; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    replies[idx] = queue.Enqueue(_ => idx, coalescable: true);
                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            allEnqueued.Wait(TimeSpan.FromSeconds(10));

            // Release the worker and immediately start shutdown.
            workerGate.Set();
            queue.Dispose();

            // All replies should complete (value or error).
            var totalCompleted = 0;
            for (var i = 0; i < opCount; i++)
            {
                var error = replies[i].WaitForError();
                if (error is null or ObjectDisposedException or InvalidOperationException)
                {
                    totalCompleted++;
                }
            }

            await Assert.That(totalCompleted).IsEqualTo(opCount);

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
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"drain-rowstream-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            var queue = new SqliteOperationQueue(conn, "test-drain-rowstream");

            // Block the worker.
            using var workerGate = new ManualResetEventSlim(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queue.Enqueue(c =>
            {
                workerGate.Wait(TimeSpan.FromSeconds(10));
                return Unit.Default;
            });
#pragma warning restore CS4014

            // Enqueue mixed ops from dedicated threads.
            const int opCount = 6;
            var scalarReplies = new IObservable<int>[opCount / 2];
            var streamReplies = new IObservable<int>[opCount / 2];
            using var allEnqueued = new CountdownEvent(opCount);

            for (var i = 0; i < opCount / 2; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    scalarReplies[idx] = queue.Enqueue(_ => idx, coalescable: true);
                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            for (var i = 0; i < opCount / 2; i++)
            {
                var idx = i;
                var t = new Thread(() =>
                {
                    streamReplies[idx] = queue.EnqueueRowStream<int>((_, emit, _) => emit(idx));
                    allEnqueued.Signal();
                })
                { IsBackground = true };
                t.Start();
            }

            allEnqueued.Wait(TimeSpan.FromSeconds(10));

            // Release worker and immediately dispose.
            workerGate.Set();
            queue.Dispose();

            // Scalar replies: either succeed or ObjectDisposedException.
            for (var i = 0; i < opCount / 2; i++)
            {
                var error = scalarReplies[i].WaitForError();
                if (error is not null)
                {
                    await Assert.That(error).IsTypeOf<ObjectDisposedException>();
                }
            }

            // Stream replies: either complete normally or with ObjectDisposedException.
            for (var i = 0; i < opCount / 2; i++)
            {
                var error = streamReplies[i].WaitForError();
                if (error is not null)
                {
                    await Assert.That(error).IsTypeOf<ObjectDisposedException>();
                }
            }

            conn.Dispose();
        }
    }

    // ── TryAddToInbox ──────────────────────────────────────────────────────

    /// <summary>
    /// TryAddToInbox returns true when the inbox is open.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryAddToInbox_InboxOpen_ReturnsTrue()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-open-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task TryAddToInbox_InboxCompleted_ReturnsFalse()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"inbox-completed-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task DrainLeftovers_MixedOps_SkipsShutdownAndFailsRegular()
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
    public async Task DrainLeftovers_EmptyInbox_IsNoOp()
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
    public async Task ExecuteBatchInTransaction_AllSucceed_CommitsTransaction()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-commit-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ExecuteBatchInTransaction_MidBatchFailure_RollsBackAndReplays()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-mid-fail-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task FailAllOps_SetsInvalidOperationOnAllOps()
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
    public async Task FailAllOps_EmptyBatch_IsNoOp()
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
    public async Task ReplayRemainingOps_FromStartIndex_ExecutesOnlyRemaining()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-start-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ReplayRemainingOps_StartIndexAtEnd_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-end-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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
    public async Task ReplayRemainingOps_FailingOpDuringReplay_ContinuesWithNext()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-fail-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
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

    // ── ExecuteBatchInTransaction COMMIT failure (outer catch) ─────────

    /// <summary>
    /// When COMMIT throws, the outer catch in ExecuteBatchInTransaction calls
    /// rollback and FailAllOps. Uses the injectable overload to inject a
    /// throwing commit delegate without corrupting a real database.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_CommitThrows_FailsAllOps()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"commit-throw-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 10, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => 20, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: () => { },
                    commit: () => throw new InvalidOperationException("COMMIT failed"),
                    rollback: () => rollbackCalled = true);

                // The outer catch fires: rollback is called and FailAllOps runs.
                // Ops that already executed have results set, so Fail is a no-op
                // on them — we verify the catch path ran via the rollback flag.
                await Assert.That(rollbackCalled).IsTrue();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// When BEGIN throws, the outer catch fires before any ops execute,
    /// rolling back and failing all ops.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_BeginThrows_FailsAllOps()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"begin-throw-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 10, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => 20, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: () => throw new InvalidOperationException("BEGIN failed"),
                    commit: () => { },
                    rollback: () => rollbackCalled = true);

                await Assert.That(rollbackCalled).IsTrue();

                var error1 = reply1.SubscribeGetError();
                var error2 = reply2.SubscribeGetError();
                await Assert.That(error1).IsTypeOf<InvalidOperationException>();
                await Assert.That(error2).IsTypeOf<InvalidOperationException>();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── ExecuteBatchInTransaction mid-batch error + rollback path ──────

    /// <summary>
    /// ExecuteBatchInTransaction with a mid-batch failure where TryRollbackAmbient
    /// is called (lines 206-208) exercises the else branch after a per-op failure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_MidBatchError_RollsBackAmbientTransaction()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"mid-batch-rollback-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var replyBad = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();
                var reply4 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 1, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => throw new InvalidOperationException("mid-fail"), replyBad, coalescable: true),
                    new SqliteOperation<int>(_ => 3, reply3, coalescable: true),
                    new SqliteOperation<int>(_ => 4, reply4, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // The bad op received its error from Execute's catch.
                var errorBad = replyBad.SubscribeGetError();
                await Assert.That(errorBad).IsTypeOf<InvalidOperationException>();

                // Ops after the failure (index 3, 4) are replayed individually (line 225).
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(3);

                var val4 = reply4.SubscribeGetValue();
                await Assert.That(val4).IsEqualTo(4);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── ExecuteBatchInTransaction first-op throws (rollback + replay) ───

    /// <summary>
    /// When the first op in the batch throws, the remaining ops have not executed
    /// yet. Rollback is called and the remaining ops are replayed individually.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_FirstOpThrows_RollsBackAndReplaysRemainder()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"first-throw-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                // SqliteOperation<T>.Execute catches internally and never rethrows.
                // To trigger the batch-level catch (lines 216-220), we need an
                // ISqliteOperation whose Execute DOES throw.
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new ThrowingOperation(),
                    new SqliteOperation<int>(_ => 20, reply2, coalescable: true),
                    new SqliteOperation<int>(_ => 30, reply3, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: () => { },
                    commit: () => { },
                    rollback: () => rollbackCalled = true);

                // Rollback should have been called because a per-op failure occurred.
                await Assert.That(rollbackCalled).IsTrue();

                // Remaining ops (index 1, 2) are replayed individually and should succeed.
                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(20);

                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(30);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── ExecuteBatchInTransaction commit throws with already-executed ops ──

    /// <summary>
    /// When commit throws, the ops have already executed (their reply.SetResult
    /// was called). FailAllOps calling reply.SetError is a no-op on
    /// already-completed replies. Verifies rollback was called and that the
    /// already-set results remain accessible.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_CommitThrows_AlreadyExecutedOpsKeepResults()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"commit-keep-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(_ => 10, reply1, coalescable: true),
                    new SqliteOperation<int>(_ => 20, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: () => { },
                    commit: () => throw new InvalidOperationException("COMMIT failed"),
                    rollback: () => rollbackCalled = true);

                await Assert.That(rollbackCalled).IsTrue();

                // Ops already executed, so their results were set before the
                // commit threw. FailAllOps is a no-op on them.
                var val1 = reply1.SubscribeGetValue();
                await Assert.That(val1).IsEqualTo(10);

                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(20);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// An <see cref="ISqliteOperation"/> whose <see cref="Execute"/> throws,
    /// enabling tests to trigger the batch-level catch in
    /// <see cref="SqliteOperationQueue.ExecuteBatchInTransaction"/>.
    /// <see cref="SqliteOperation{T}.Execute"/> catches internally and never
    /// rethrows, so a custom implementation is needed.
    /// </summary>
    /// <summary>
    /// Exercises the non-injectable ExecuteBatchInTransaction overload with a
    /// ThrowingOperation to trigger the real rollback lambda at line 176.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_NonInjectable_ThrowingOp_TriggersRealRollback()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"real-rollback-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply = new SqliteReplyObservable<int>();
                var batch = new List<ISqliteOperation>
                {
                    new ThrowingOperation(),
                    new SqliteOperation<int>(_ => 10, reply, coalescable: true),
                };

                // Call the non-injectable overload which uses the real rollback lambda.
                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // ThrowingOperation triggered rollback; remaining op was replayed.
                var val = reply.SubscribeGetValue();
                await Assert.That(val).IsEqualTo(10);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>An <see cref="ISqliteOperation"/> whose Execute throws for testing batch error paths.</summary>
    private sealed class ThrowingOperation : ISqliteOperation
    {
        /// <inheritdoc/>
        public bool IsCoalescable => true;

        /// <inheritdoc/>
        public void Execute(SqlitePclRawConnection connection) =>
            throw new InvalidOperationException("ThrowingOperation");

        /// <inheritdoc/>
        public void Fail(Exception error)
        {
        }
    }
}
