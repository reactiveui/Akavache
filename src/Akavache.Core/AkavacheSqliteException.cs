// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Raised when a native SQLite call returns a non-success result code. Carries the raw
/// sqlite3 return code so callers can distinguish specific failures (missing-table /
/// missing-column errors, bad encryption key, etc.) from generic ones.
/// </summary>
public sealed class AkavacheSqliteException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AkavacheSqliteException"/> class.</summary>
    public AkavacheSqliteException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AkavacheSqliteException"/> class.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public AkavacheSqliteException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AkavacheSqliteException"/> class.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public AkavacheSqliteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AkavacheSqliteException"/> class.</summary>
    /// <param name="resultCode">The raw sqlite3 return code.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    public AkavacheSqliteException(int resultCode, string message)
        : base(message) => ResultCode = resultCode;

    /// <summary>Gets the raw sqlite3 return code (e.g. <c>SQLITE_ERROR</c>, <c>SQLITE_BUSY</c>).</summary>
    public int ResultCode { get; }
}
