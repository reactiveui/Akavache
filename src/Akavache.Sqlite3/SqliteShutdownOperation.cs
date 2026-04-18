// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Sentinel inbox item posted by <see cref="SqliteOperationQueue.ShutdownAndWait"/>.
/// When the worker dequeues it, it runs the supplied last-will callback and breaks
/// out of the consume loop.
/// </summary>
internal sealed class SqliteShutdownOperation : ISqliteOperation
{
    /// <summary>Cleanup callback the worker runs as its final act before exiting the consume loop.</summary>
    private readonly Action<SqlitePclRawConnection> _lastWill;

    /// <summary>Initializes a new instance of the <see cref="SqliteShutdownOperation"/> class.</summary>
    /// <param name="lastWill">Cleanup callback to run on the worker thread.</param>
    public SqliteShutdownOperation(Action<SqlitePclRawConnection> lastWill) => _lastWill = lastWill;

    /// <inheritdoc/>
    public bool IsCoalescable => false;

    /// <inheritdoc/>
    public void Execute(SqlitePclRawConnection connection)
    {
    }

    /// <inheritdoc/>
    public void Fail(Exception error)
    {
    }

    /// <summary>Invokes the stored last-will callback against <paramref name="connection"/>.</summary>
    /// <param name="connection">The connection to tear down on the worker thread.</param>
    public void RunLastWill(SqlitePclRawConnection connection) => _lastWill(connection);
}
