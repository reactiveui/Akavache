// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests;

using Splat.Builder;

[assembly: NotInParallel]

namespace Akavache.EncryptedSqlite3.Tests;

/// <summary>
/// Resets shared Akavache and encrypted SQLite static state around each encrypted SQLite test.
/// </summary>
public static class GlobalTestHooks
{
    /// <summary>
    /// Runs before every test.
    /// </summary>
    [BeforeEvery(Test)]
    public static void ResetBeforeEveryTest() => ResetState();

    /// <summary>
    /// Runs after every test.
    /// </summary>
    [AfterEvery(Test)]
    public static void ResetAfterEveryTest() => ResetState();

    /// <summary>
    /// Resets shared static state used by encrypted SQLite tests.
    /// </summary>
    private static void ResetState()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();
        Akavache.Core.RequestCache.Clear();
        Akavache.Core.UniversalSerializer.ResetCaches();
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        AppBuilder.ResetBuilderStateForTests();
    }
}
