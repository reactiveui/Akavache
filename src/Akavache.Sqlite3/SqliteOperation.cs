// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if ENCRYPTED
namespace Akavache.EncryptedSqlite3;
#else
namespace Akavache.Sqlite3;
#endif

/// <summary>
/// Typed single-value work item. Runs a body that computes one result of type
/// <typeparamref name="T"/> against the connection and forwards the result (or
/// exception) to the one-shot reply observable.
/// </summary>
/// <typeparam name="T">The result type produced by the body.</typeparam>
internal sealed class SqliteOperation<T> : ISqliteOperation
{
    /// <summary>The caller-supplied work to run against the owning connection.</summary>
    private readonly Func<SqlitePclRawConnection, T> _body;

    /// <summary>One-shot reply observable the worker signals on completion or failure.</summary>
    private readonly SqliteReplyObservable<T> _reply;

    /// <summary>Initializes a new instance of the <see cref="SqliteOperation{T}"/> class.</summary>
    /// <param name="body">The work to run.</param>
    /// <param name="reply">The reply observable to signal on completion.</param>
    /// <param name="coalescable">Whether this operation can be batched with adjacent writes.</param>
    public SqliteOperation(Func<SqlitePclRawConnection, T> body, SqliteReplyObservable<T> reply, bool coalescable)
    {
        _body = body;
        _reply = reply;
        IsCoalescable = coalescable;
    }

    /// <inheritdoc/>
    public bool IsCoalescable { get; }

    /// <inheritdoc/>
    public void Execute(SqlitePclRawConnection connection)
    {
        try
        {
            var result = _body(connection);
            _reply.SetResult(result);
        }
        catch (Exception ex)
        {
            _reply.SetError(ex);
        }
    }

    /// <inheritdoc/>
    public void Fail(Exception error) => _reply.SetError(error);
}
