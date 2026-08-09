// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Splat.Builder;

[assembly: NotInParallel]

#if REACTIVE_SHIM
namespace Akavache.Reactive.Sqlite3.Tests;
#else
namespace Akavache.Sqlite3.Tests;
#endif

/// <summary>Resets shared Akavache and SQLite static state around each SQLite test.</summary>
public static class GlobalTestHooks
{
    /// <summary>Runs before every test.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [BeforeEvery(Test)]
    public static void ResetBeforeEveryTest() => ResetState();

    /// <summary>Runs after every test, leaving no residue for whatever runs next.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [AfterEvery(Test)]
    public static void ResetAfterEveryTest() => ResetBeforeEveryTest();

    /// <summary>Resets shared static state used by SQLite tests.</summary>
    private static void ResetState()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();
        RequestCache.Clear();
        UniversalSerializer.ResetCaches();
        AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        AppBuilder.ResetBuilderStateForTests();
    }
}
