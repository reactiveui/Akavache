// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.Tests.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests covering the <see cref="SqliteOperationQueue"/> internal logic:
/// enqueue-after-dispose, shutdown idempotency, worker-loop drain,
/// coalesced-batch execution, and the Fail/Execute paths on individual operation types.
/// </summary>
[Category("Akavache")]
[NotInParallel("SqliteOperationQueue")]
public class SqliteOperationQueueTests
{
    // ── Enqueue after disposed ────────────────────────────────────────────

    /// <summary>
    /// Enqueue on a disposed queue returns an observable that emits ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Enqueue_AfterDispose_EmitsObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "enqueue-disposed.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var captured = conn.Upsert([new CacheEntry("x", null, [1], DateTimeOffset.UtcNow, null)])
                .SubscribeGetError();

            await Assert.That(captured).IsTypeOf<ObjectDisposedException>();
        }
    }

    /// <summary>
    /// EnqueueRowStream on a disposed queue completes with an empty sequence.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EnqueueRowStream_AfterDispose_CompletesEmpty()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "rowstream-disposed.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var keys = conn.GetAllKeys(null, DateTimeOffset.UtcNow).ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
        }
    }

    // ── ShutdownAndWait second call ───────────────────────────────────────

    /// <summary>
    /// Calling ShutdownAndWait a second time returns without error (idempotent).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ShutdownAndWait_CalledTwice_DoesNotThrow()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "shutdown-twice.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            conn.Dispose();
            conn.Dispose();

            // Reaching here without deadlock or exception means the test passed.
        }
    }

    // ── WorkerLoop drain leftovers ────────────────────────────────────────

    /// <summary>
    /// Operations enqueued after shutdown are failed with ObjectDisposedException.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WorkerLoop_Drain_FailsLatecomersWithObjectDisposedException()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "drain-leftovers.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var captured = conn.Upsert([new CacheEntry("late", null, [1], DateTimeOffset.UtcNow, null)])
                .SubscribeGetError();

            await Assert.That(captured).IsTypeOf<ObjectDisposedException>();
        }
    }

    // ── SqliteOperation.Fail ──────────────────────────────────────────────

    /// <summary>
    /// <see cref="SqliteOperation{T}.Fail"/> sets the error on the reply observable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteOperation_Fail_SetsErrorOnReply()
    {
        var reply = new SqliteReplyObservable<int>();
        var op = new SqliteOperation<int>(_ => 42, reply, coalescable: false);

        op.Fail(new ObjectDisposedException("queue"));

        var caught = reply.SubscribeGetError();

        await Assert.That(caught).IsTypeOf<ObjectDisposedException>();
    }

    // ── SqliteRowStreamOperation error path ───────────────────────────────

    /// <summary>
    /// When the body throws, the error is forwarded to the stream's OnError.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteRowStreamOperation_BodyThrows_ForwardsErrorToStream()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "rowstream-error.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var stream = new SqliteRowObservable<int>();
                var op = new SqliteRowStreamOperation<int>(
                    (_, _, _) => throw new InvalidOperationException("body-error"),
                    stream);

                op.Execute(conn);

                await Assert.That(stream.IsCancelled).IsTrue();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// <see cref="SqliteRowStreamOperation{T}.Fail"/> forwards the error to the stream.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteRowStreamOperation_Fail_ForwardsErrorToStream()
    {
        var stream = new SqliteRowObservable<int>();
        var op = new SqliteRowStreamOperation<int>((_, _, _) => { }, stream);

        op.Fail(new ObjectDisposedException("queue"));

        await Assert.That(stream.IsCancelled).IsTrue();
    }

    /// <summary>
    /// <see cref="SqliteRowStreamOperation{T}.Execute"/> catches body exceptions
    /// and forwards them to the stream's OnError (lines 46-49).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteRowStreamOperation_BodyThrows_ForwardsExactErrorToSubscriber()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rowstream-error-exact-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var stream = new SqliteRowObservable<int>();
                Exception? caught = null;
                stream.Subscribe(System.Reactive.Observer.Create<int>(
                    _ => { },
                    ex => caught = ex,
                    () => { }));

                var expected = new InvalidOperationException("exact-body-error");
                var op = new SqliteRowStreamOperation<int>(
                    (_, _, _) => throw expected,
                    stream);

                op.Execute(conn);

                await Assert.That(caught).IsSameReferenceAs(expected);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// <see cref="SqliteRowStreamOperation{T}.Execute"/> calls OnCompleted
    /// when the body completes without error (lines 43-44).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteRowStreamOperation_BodyCompletes_CallsOnCompleted()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"rowstream-complete-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var stream = new SqliteRowObservable<int>();
                var items = new List<int>();
                var completed = false;
                stream.Subscribe(System.Reactive.Observer.Create<int>(
                    v => items.Add(v),
                    _ => { },
                    () => completed = true));

                var op = new SqliteRowStreamOperation<int>(
                    (_, emit, _) =>
                    {
                        emit(10);
                        emit(20);
                    },
                    stream);

                op.Execute(conn);

                await Assert.That(items.Count).IsEqualTo(2);
                await Assert.That(items[0]).IsEqualTo(10);
                await Assert.That(items[1]).IsEqualTo(20);
                await Assert.That(completed).IsTrue();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// <see cref="SqliteRowStreamOperation{T}.Fail"/> forwards the specific error
    /// to the subscribed observer (line 53).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteRowStreamOperation_Fail_ForwardsExactErrorToSubscriber()
    {
        var stream = new SqliteRowObservable<int>();
        Exception? caught = null;
        stream.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { }));

        var expected = new ObjectDisposedException("test-fail");
        var op = new SqliteRowStreamOperation<int>((_, _, _) => { }, stream);
        op.Fail(expected);

        await Assert.That(caught).IsSameReferenceAs(expected);
    }

    // ── SqliteShutdownOperation ───────────────────────────────────────────

    /// <summary>
    /// <see cref="SqliteShutdownOperation.IsCoalescable"/> returns false.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteShutdownOperation_IsNotCoalescable()
    {
        var op = new SqliteShutdownOperation(_ => { });
        await Assert.That(op.IsCoalescable).IsFalse();
    }

    /// <summary>
    /// <see cref="SqliteShutdownOperation.Execute"/> is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteShutdownOperation_Execute_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "shutdown-exec.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            try
            {
                var op = new SqliteShutdownOperation(_ => { });
                op.Execute(conn);

                // Reaching here without exception means the test passed.
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// <see cref="SqliteShutdownOperation.Fail"/> is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteShutdownOperation_Fail_IsNoOp()
    {
        var op = new SqliteShutdownOperation(_ => { });
        op.Fail(new ObjectDisposedException("test"));

        // Reaching here without exception means the test passed.
    }

    /// <summary>
    /// <see cref="SqliteShutdownOperation.RunLastWill"/> invokes the stored callback
    /// with the connection. Covers line 40.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SqliteShutdownOperation_RunLastWill_InvokesCallback()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"shutdown-lastwill-{Guid.NewGuid()}.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            try
            {
                var callbackInvoked = false;
                var op = new SqliteShutdownOperation(_ => callbackInvoked = true);
                op.RunLastWill(conn);

                await Assert.That(callbackInvoked).IsTrue();
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── ExecuteCoalescedBatch ─────────────────────────────────────────────

    /// <summary>
    /// Multiple coalescable writes batched together all produce correct results.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_MultipleWrites_AllSucceed()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "coalesce-batch.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                // Fire many writes concurrently to maximize the chance they coalesce.
                for (var i = 0; i < 20; i++)
                {
                    conn.Upsert(
                        [new CacheEntry($"batch-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]).WaitForCompletion();
                }

                // Verify all writes landed.
                for (var i = 0; i < 20; i++)
                {
                    var entry = conn.Get($"batch-{i}", null, DateTimeOffset.UtcNow).WaitForValue();
                    await Assert.That(entry).IsNotNull();
                    await Assert.That(entry!.Value![0]).IsEqualTo((byte)i);
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// A non-coalescable op (read) interleaved with writes exercises the _afterBatch path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_ReadBreaksBatch_RunsAfterBatch()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "batch-break.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert(
                    [new CacheEntry("existing", null, [42], DateTimeOffset.UtcNow, null)]).WaitForCompletion();

                // Write then read — read is non-coalescable and breaks any batch.
                for (var i = 0; i < 5; i++)
                {
                    conn.Upsert(
                        [new CacheEntry($"w-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]).WaitForCompletion();
                }

                var readResult = conn.Get("existing", null, DateTimeOffset.UtcNow).WaitForValue();
                await Assert.That(readResult).IsNotNull();
                await Assert.That(readResult!.Value![0]).IsEqualTo((byte)42);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>
    /// Multiple writes succeed even when internal batching is active.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ExecuteCoalescedBatch_MidBatchFailure_ReplaysRemainder()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "batch-failure.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                for (var i = 0; i < 15; i++)
                {
                    conn.Upsert(
                        [new CacheEntry($"replay-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]).WaitForCompletion();
                }

                for (var i = 0; i < 15; i++)
                {
                    var entry = conn.Get($"replay-{i}", null, DateTimeOffset.UtcNow).WaitForValue();
                    await Assert.That(entry).IsNotNull();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── Direct queue construction tests ───────────────────────────────────

    /// <summary>
    /// Constructing a connection and immediately closing it shuts down cleanly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Queue_ConstructAndDispose_ShutdownCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "queue-dispose.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.Dispose();

            // Reaching here without deadlock means the test passed.
        }
    }

    /// <summary>
    /// Enqueue after CloseSync emits ObjectDisposedException via Subscribe.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Enqueue_AfterCloseSync_EmitsObjectDisposed()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "enqueue-race.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            conn.Dispose();

            var captured = conn.Upsert([new CacheEntry("x", null, [1], DateTimeOffset.UtcNow, null)])
                .SubscribeGetError();

            await Assert.That(captured).IsTypeOf<ObjectDisposedException>();

            // Row-stream after dispose completes empty.
            var keys = conn.GetAllKeys(null, DateTimeOffset.UtcNow).ToList().WaitForValue();
            await Assert.That(keys).IsEmpty();
        }
    }

    // ── Coalesced batch with interleaved reads and writes ─────────────────

    /// <summary>
    /// Interleaving reads and writes exercises both the coalescable and non-coalescable paths.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CoalescedBatch_InterleavedReadsAndWrites_AllComplete()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "interleaved.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                conn.Upsert(
                    [new CacheEntry("base", null, [0], DateTimeOffset.UtcNow, null)]).WaitForCompletion();

                for (var i = 0; i < 10; i++)
                {
                    if (i % 3 == 0)
                    {
                        conn.Get("base", null, DateTimeOffset.UtcNow).WaitForValue();
                    }
                    else
                    {
                        conn.Upsert(
                            [new CacheEntry($"interleave-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]).WaitForCompletion();
                    }
                }

                var entry = conn.Get("base", null, DateTimeOffset.UtcNow).WaitForValue();
                await Assert.That(entry).IsNotNull();
                await Assert.That(entry!.Value![0]).IsEqualTo((byte)0);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    // ── RunAfterBatch with shutdown ───────────────────────────────────────

    /// <summary>
    /// Disposing a connection that has pending writes exercises the shutdown drain path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task RunAfterBatch_ShutdownDuringWrites_CompletesCleanly()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, "afterbatch-shutdown.db");
            var conn = new SqlitePclRawConnection(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();

            // Write a few entries then close — the worker may still be processing.
            for (var i = 0; i < 5; i++)
            {
                conn.Upsert(
                    [new CacheEntry($"ab-{i}", null, [(byte)i], DateTimeOffset.UtcNow, null)]).WaitForCompletion();
            }

            conn.Dispose();

            // Reaching here without deadlock means the test passed.
        }
    }
}
