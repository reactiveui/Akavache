// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Non-generic inbox interface used by <see cref="SqliteOperationQueue"/> to hold
/// heterogeneous operations in one collection. Each operation knows how to run itself
/// against the owning connection (on the worker thread) and how to fail itself if the
/// queue shuts down before it got a turn (so waiting subscribers are notified rather
/// than hanging).
/// </summary>
internal interface ISqliteOperation
{
    /// <summary>Gets a value indicating whether this operation can be batched with adjacent writes into a single transaction by the worker loop.</summary>
    bool IsCoalescable { get; }

    /// <summary>Executes the operation body against <paramref name="connection"/> on the worker thread.</summary>
    /// <param name="connection">The owning connection.</param>
    void Execute(SqlitePclRawConnection connection);

    /// <summary>Fails any pending reply observable for this operation — invoked when the queue is torn down before the operation ran.</summary>
    /// <param name="error">The exception to report to the waiting subscriber.</param>
    void Fail(Exception error);
}
