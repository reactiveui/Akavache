// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests;
using Splat.Builder;

[assembly: NotInParallel]

namespace Akavache.Sqlite3.Tests;

/// <summary>
/// Resets shared Akavache and SQLite static state around each SQLite test.
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
    /// Resets shared static state used by SQLite tests.
    /// </summary>
    private static void ResetState()
    {
        CacheDatabase.ResetForTests().SubscribeAndComplete();
        Akavache.Core.RequestCache.Clear();
        Akavache.Core.UniversalSerializer.ResetCaches();
        Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        AppBuilder.ResetBuilderStateForTests();
    }
}
