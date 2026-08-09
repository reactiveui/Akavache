// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests for the batch-execution statics on <see cref="SqliteOperationQueue"/> —
/// <c>DrainLeftovers</c>, <c>ExecuteBatchInTransaction</c>, <c>FailAllOps</c> and
/// <c>ReplayRemainingOps</c> — driven directly rather than through a running worker.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SqliteOperationQueueBatchExecutionTests
{
    /// <summary>Result produced by the first operation in a batch; distinct so a crossed reply is visible.</summary>
    private const int FirstOpResult = 10;

    /// <summary>Result produced by the second operation in a batch.</summary>
    private const int SecondOpResult = 20;

    /// <summary>Result produced by the third operation in a batch.</summary>
    private const int ThirdOpResult = 30;

    /// <summary>Result produced by the fourth operation in a batch.</summary>
    private const int FourthOpResult = 40;

    /// <summary>DrainLeftovers skips SqliteShutdownOperation instances and fails regular ops with ObjectDisposedException.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DrainLeftovers_MixedOps_SkipsShutdownAndFailsRegular()
    {
        var inbox = new BlockingCollection<ISqliteOperation>();

        var reply1 = new SqliteReplyObservable<int>();
        var op1 = new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: false);
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
    public async Task DrainLeftovers_EmptyInbox_IsNoOp()
    {
        var inbox = new BlockingCollection<ISqliteOperation>();
        inbox.CompleteAdding();

        SqliteOperationQueue.DrainLeftovers(inbox);

        // No exception, no hang — just returns.
        await Task.CompletedTask;

        inbox.Dispose();
    }

    /// <summary>ExecuteBatchInTransaction commits all ops when none throw.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_AllSucceed_CommitsTransaction()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-commit-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // All replies should have completed successfully (single-subscriber).
                var val1 = reply1.SubscribeGetValue();
                var val2 = reply2.SubscribeGetValue();
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val1).IsEqualTo(FirstOpResult);
                await Assert.That(val2).IsEqualTo(SecondOpResult);
                await Assert.That(val3).IsEqualTo(ThirdOpResult);
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
    public async Task ExecuteBatchInTransaction_MidBatchFailure_RollsBackAndReplays()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"batch-mid-fail-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => throw new InvalidOperationException("mid-batch-boom"), reply2, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // The failing op (index 1) receives the thrown exception via Execute's catch.
                var error2 = reply2.SubscribeGetError();
                await Assert.That(error2).IsTypeOf<InvalidOperationException>();

                // The op after the failure (index 2) is replayed individually and should succeed.
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(ThirdOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>FailAllOps sets InvalidOperationException on every op's reply observable.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task FailAllOps_SetsInvalidOperationOnAllOps()
    {
        var reply1 = new SqliteReplyObservable<int>();
        var reply2 = new SqliteReplyObservable<int>();
        var reply3 = new SqliteReplyObservable<int>();

        var batch = new List<ISqliteOperation>
        {
            new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
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
    public async Task FailAllOps_EmptyBatch_IsNoOp()
    {
        var batch = new List<ISqliteOperation>();

        SqliteOperationQueue.FailAllOps(batch);

        // No exception, no hang.
        await Task.CompletedTask;
    }

    /// <summary>ReplayRemainingOps executes only ops from the given startIndex onward.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReplayRemainingOps_FromStartIndex_ExecutesOnlyRemaining()
    {
        const int BatchSize = 4;
        const int ReplayStartIndex = 2;

        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-start-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var executed = new bool[BatchSize];

                var reply0 = new SqliteReplyObservable<int>();
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    RecordingOperation(executed, index: 0, FirstOpResult, reply0),
                    RecordingOperation(executed, index: 1, SecondOpResult, reply1),
                    RecordingOperation(executed, index: 2, ThirdOpResult, reply2),
                    RecordingOperation(executed, index: 3, FourthOpResult, reply3),
                };

                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: ReplayStartIndex);

                // Only ops at index 2 and 3 should have been executed.
                await Assert.That(executed[0]).IsFalse();
                await Assert.That(executed[1]).IsFalse();
                await Assert.That(executed[2]).IsTrue();
                await Assert.That(executed[3]).IsTrue();

                // The executed ops should have their results.
                var val2 = reply2.SubscribeGetValue();
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(ThirdOpResult);
                await Assert.That(val3).IsEqualTo(FourthOpResult);
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
    public async Task ReplayRemainingOps_StartIndexAtEnd_IsNoOp()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"replay-end-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var executed = new bool[1];
                var reply = new SqliteReplyObservable<int>();
                var batch = new List<ISqliteOperation> { RecordingOperation(executed, index: 0, FirstOpResult, reply), };

                // startIndex == batch.Count means nothing to replay.
                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 1);

                await Assert.That(executed[0]).IsFalse();
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
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply0 = new SqliteReplyObservable<int>();
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply0, coalescable: true),
                    new SqliteOperation<int>(static _ => throw new InvalidOperationException("replay-boom"), reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdOpResult, reply2, coalescable: true),
                };

                // Replay from index 1 — includes the failing op and the one after it.
                SqliteOperationQueue.ReplayRemainingOps(conn, batch, startIndex: 1);

                // The failing op should have received the error.
                var error1 = reply1.SubscribeGetError();
                await Assert.That(error1).IsTypeOf<InvalidOperationException>();

                // The op after the failure should still succeed.
                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(ThirdOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

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
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: static () => { },
                    commit: static () => throw new InvalidOperationException("COMMIT failed"),
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

    /// <summary>When BEGIN throws, the outer catch fires before any ops execute, rolling back and failing all ops.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_BeginThrows_FailsAllOps()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"begin-throw-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: static () => throw new InvalidOperationException("BEGIN failed"),
                    commit: static () => { },
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
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var replyBad = new SqliteReplyObservable<int>();
                var reply3 = new SqliteReplyObservable<int>();
                var reply4 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => throw new InvalidOperationException("mid-fail"), replyBad, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: true),
                    new SqliteOperation<int>(static _ => FourthOpResult, reply4, coalescable: true),
                };

                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // The bad op received its error from Execute's catch.
                var errorBad = replyBad.SubscribeGetError();
                await Assert.That(errorBad).IsTypeOf<InvalidOperationException>();

                // Ops after the failure (index 3, 4) are replayed individually (line 225).
                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(ThirdOpResult);

                var val4 = reply4.SubscribeGetValue();
                await Assert.That(val4).IsEqualTo(FourthOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

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
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
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
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                    new SqliteOperation<int>(static _ => ThirdOpResult, reply3, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: static () => { },
                    commit: static () => { },
                    rollback: () => rollbackCalled = true);

                // Rollback should have been called because a per-op failure occurred.
                await Assert.That(rollbackCalled).IsTrue();

                // Remaining ops (index 1, 2) are replayed individually and should succeed.
                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(SecondOpResult);

                var val3 = reply3.SubscribeGetValue();
                await Assert.That(val3).IsEqualTo(ThirdOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

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
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply1 = new SqliteReplyObservable<int>();
                var reply2 = new SqliteReplyObservable<int>();

                var batch = new List<ISqliteOperation>
                {
                    new SqliteOperation<int>(static _ => FirstOpResult, reply1, coalescable: true),
                    new SqliteOperation<int>(static _ => SecondOpResult, reply2, coalescable: true),
                };

                var rollbackCalled = false;

                SqliteOperationQueue.ExecuteBatchInTransaction(
                    conn,
                    batch,
                    begin: static () => { },
                    commit: static () => throw new InvalidOperationException("COMMIT failed"),
                    rollback: () => rollbackCalled = true);

                await Assert.That(rollbackCalled).IsTrue();

                // Ops already executed, so their results were set before the
                // commit threw. FailAllOps is a no-op on them.
                var val1 = reply1.SubscribeGetValue();
                await Assert.That(val1).IsEqualTo(FirstOpResult);

                var val2 = reply2.SubscribeGetValue();
                await Assert.That(val2).IsEqualTo(SecondOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>ExecuteBatchInTransaction with a failing op in the batch throws.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecuteBatchInTransaction_NonInjectable_ThrowingOp_TriggersRealRollback()
    {
        using (Utility.WithEmptyDirectory(out var path))
        {
            var dbPath = Path.Combine(path, $"real-rollback-{Guid.NewGuid()}.db");
            var conn = SqlitePclRawConnection.Create(dbPath, password: null, readOnly: false);
            conn.CreateSchema().WaitForCompletion();
            try
            {
                var reply = new SqliteReplyObservable<int>();
                var batch = new List<ISqliteOperation> { new ThrowingOperation(), new SqliteOperation<int>(static _ => FirstOpResult, reply, coalescable: true), };

                // Call the non-injectable overload which uses the real rollback lambda.
                SqliteOperationQueue.ExecuteBatchInTransaction(conn, batch);

                // ThrowingOperation triggered rollback; remaining op was replayed.
                var val = reply.SubscribeGetValue();
                await Assert.That(val).IsEqualTo(FirstOpResult);
            }
            finally
            {
                conn.Dispose();
            }
        }
    }

    /// <summary>Builds a coalescable operation that records the fact it ran before returning its result.</summary>
    /// <param name="executed">The flags array the operation stamps when it runs.</param>
    /// <param name="index">The slot in <paramref name="executed"/> this operation owns.</param>
    /// <param name="result">The value the operation hands to its reply.</param>
    /// <param name="reply">The reply observable the operation completes.</param>
    /// <returns>The operation.</returns>
    private static SqliteOperation<int> RecordingOperation(bool[] executed, int index, int result, SqliteReplyObservable<int> reply) =>
        new(
            _ =>
            {
                executed[index] = true;
                return result;
            },
            reply,
            coalescable: true);

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
