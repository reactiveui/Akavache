// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat;
using Splat.Builder;

// Akavache's public API is built on global static state (CacheDatabase, AppLocator,
// AkavacheBuilder.BlobCaches/SettingsStores, UniversalSerializer factory list, SQLite
// provider flags). Running any two of these tests in parallel races on that shared
// state, so serialise the entire assembly under one shared NotInParallel group key
// and route every test through the executor that wraps the test method body in a
// try/finally so cleanup always runs.
//
// State reset lives in [BeforeEvery(Test)] / [AfterEvery(Test)] hooks rather than
// inside the executor because TUnit runs ITestExecutor.ExecuteTest *after* class
// [Before(Test)] hooks (see TUnit's HookExecutor.cs ~line 500). Resetting state
// inside the executor would wipe per-class fixture setup (e.g. a Splat
// BitmapLoader mock) before the test body runs. [BeforeEvery(Test)] hooks run
// strictly before [Before(Test)], so the reset there is harmless to per-class
// fixtures.
// Unkeyed [NotInParallel] puts every test in the assembly into TUnit's global
// sequential bucket (TestScheduler routes those through ExecuteSequentiallyAsync).
// Class- and method-level [NotInParallel(...)] attributes have been removed so
// no test ends up in a per-key bucket that would let it overlap with another
// global-state test.
[assembly: NotInParallel]
[assembly: TestExecutor<Akavache.Tests.Executors.AkavacheTestExecutor>]

namespace Akavache.Integration.Tests;

/// <summary>
/// Assembly-level hooks that reset every piece of Akavache global static state
/// before and after every test in this assembly. Runs before the per-class
/// <c>[Before(Test)]</c> hooks (TUnit dispatches <c>BeforeEvery</c> first), so
/// per-test fixture setup such as a Splat <c>BitmapLoader</c> mock survives the
/// reset.
/// </summary>
public static class GlobalTestHooks
{
    /// <summary>Runs before every test. Wipes Akavache global state.</summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    [BeforeEvery(Test)]
    public static Task ResetBeforeEveryTest() => ResetGlobalStateAsync();

    /// <summary>Runs after every test. Wipes Akavache global state.</summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    [AfterEvery(Test)]
    public static Task CleanAfterEveryTest() => ResetGlobalStateAsync();

    /// <summary>
    /// Resets every Akavache and Splat static state holder to its constructed
    /// default. Single source of truth so adding a new state holder only requires
    /// touching this one method.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    private static async Task ResetGlobalStateAsync()
    {
        await CacheDatabase.ResetForTests();

        RequestCache.Clear();

        UniversalSerializer.ResetCaches();

        Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        var previousLocator = AppLocator.GetLocator();
        ModernDependencyResolver freshLocator = new();
        AppLocator.SetLocator(freshLocator);
        AppLocator.CurrentMutable.InitializeSplat();

        if (!ReferenceEquals(previousLocator, freshLocator))
        {
            previousLocator?.Dispose();
        }

        AppBuilder.ResetBuilderStateForTests();
    }
}
