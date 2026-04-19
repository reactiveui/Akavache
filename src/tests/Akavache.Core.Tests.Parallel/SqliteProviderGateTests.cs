// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="SqliteProviderGate"/>.
/// </summary>
[Category("Akavache")]
public class SqliteProviderGateTests
{
    /// <summary>
    /// <see cref="SqliteProviderGate.TryClaimInit"/> returns <see langword="true"/>
    /// exactly once per process. A subsequent call always returns <see langword="false"/>,
    /// exercising the "already claimed" branch at line 27 where
    /// <c>Interlocked.Exchange</c> returns 1.
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
