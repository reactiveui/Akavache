// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Tests for <see cref="SqliteProviderGate"/>.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class SqliteProviderGateTests
{
    /// <summary>
    /// Claiming initialisation succeeds exactly once per process, so a subsequent
    /// <see cref="SqliteProviderGate.TryClaimInit"/> call always returns <see langword="false"/>,
    /// exercising the "already claimed" branch where <c>Interlocked.Exchange</c> returns 1.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryClaimInit_SecondCall_ReturnsFalse()
    {
        // Consume the first-call slot (may already have been consumed by another
        // test or hook in this process, in which case this returns false too).
        _ = SqliteProviderGate.TryClaimInit();

        // The second call must always return false.
        var result = SqliteProviderGate.TryClaimInit();

        await Assert.That(result).IsFalse();
    }
}
