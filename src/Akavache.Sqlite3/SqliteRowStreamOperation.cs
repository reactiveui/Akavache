// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if ENCRYPTED
#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3;
#else
namespace Akavache.EncryptedSqlite3;
#endif
#else
#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3;
#else
namespace Akavache.Sqlite3;
#endif
#endif

/// <summary>
/// Typed multi-row work item. Runs a body that emits N rows via an
/// <see cref="Action{T}"/> callback and checks a cancel flag between rows. The
/// worker invokes <see cref="Execute"/> once per op; the body itself iterates the
/// SQLite statement internally.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
/// <param name="body">The scan body.</param>
/// <param name="stream">The stream to emit into.</param>
internal sealed class SqliteRowStreamOperation<T>(Action<SqlitePclRawConnection, Action<T>, Func<bool>> body, SqliteRowObservable<T> stream) : ISqliteOperation
{
    /// <summary>The caller-supplied scan body.</summary>
    private readonly Action<SqlitePclRawConnection, Action<T>, Func<bool>> _body = body;

    /// <summary>The row-streaming observable the worker emits into.</summary>
    private readonly SqliteRowObservable<T> _stream = stream;

    /// <inheritdoc/>
    public bool IsCoalescable => false;

    /// <inheritdoc/>
    public void Execute(SqlitePclRawConnection connection)
    {
        try
        {
            _body(connection, _stream.OnNext, () => _stream.IsCancelled);
            _stream.OnCompleted();
        }
        catch (Exception ex)
        {
            _stream.OnError(ex);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fail(Exception error) => _stream.OnError(error);
}
