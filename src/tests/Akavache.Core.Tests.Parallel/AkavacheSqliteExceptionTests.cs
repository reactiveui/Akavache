// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="AkavacheSqliteException"/> covering all constructor overloads.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class AkavacheSqliteExceptionTests
{
    /// <summary>The SQLite result code for SQLITE_BUSY, used to prove the code is round-tripped verbatim.</summary>
    private const int SqliteBusyResultCode = 5;

    /// <summary>The parameterless constructor creates an exception with a default message and zero result code.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Ctor_Parameterless_CreatesExceptionWithDefaults()
    {
        var ex = new AkavacheSqliteException();

        await Assert.That(ex.Message).IsNotNull();
        await Assert.That(ex.InnerException).IsNull();
        await Assert.That(ex.ResultCode).IsEqualTo(0);
    }

    /// <summary>The message-only constructor stores the supplied message.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Ctor_WithMessage_StoresMessage()
    {
        var ex = new AkavacheSqliteException("test message");

        await Assert.That(ex.Message).IsEqualTo("test message");
        await Assert.That(ex.InnerException).IsNull();
        await Assert.That(ex.ResultCode).IsEqualTo(0);
    }

    /// <summary>The message-and-inner constructor stores both the message and inner exception.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Ctor_WithMessageAndInner_StoresBoth()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AkavacheSqliteException("outer", inner);

        await Assert.That(ex.Message).IsEqualTo("outer");
        await Assert.That(ex.InnerException).IsSameReferenceAs(inner);
        await Assert.That(ex.ResultCode).IsEqualTo(0);
    }

    /// <summary>The result-code-and-message constructor stores both the result code and message.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Ctor_WithResultCodeAndMessage_StoresBoth()
    {
        var ex = new AkavacheSqliteException(SqliteBusyResultCode, "SQLITE_BUSY");

        await Assert.That(ex.Message).IsEqualTo("SQLITE_BUSY");
        await Assert.That(ex.ResultCode).IsEqualTo(SqliteBusyResultCode);
        await Assert.That(ex.InnerException).IsNull();
    }
}
