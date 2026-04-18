// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Splat;
using Splat.Builder;

// Settings tests mutate the same global state as Akavache.Tests (CacheDatabase,
// AppLocator, AkavacheBuilder.SettingsStores, SQLite provider flags). Serialise
// the whole assembly under a shared NotInParallel group key, route every test
// through the executor that wraps the test method body in try/finally, and run
// the state reset in [BeforeEvery(Test)] / [AfterEvery(Test)] hooks so it lands
// before any per-class [Before(Test)] fixture setup.
// Unkeyed [NotInParallel] puts every test in this assembly into TUnit's global
// sequential bucket. Class- and method-level [NotInParallel] attributes have
// been removed so no test ends up in a per-key bucket.
[assembly: NotInParallel]
[assembly: TestExecutor<AkavacheTestExecutor>]

namespace Akavache.Settings.Tests;

/// <summary>
/// Assembly-level hooks that reset every piece of Akavache global static state
/// before and after every test in this assembly.
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
