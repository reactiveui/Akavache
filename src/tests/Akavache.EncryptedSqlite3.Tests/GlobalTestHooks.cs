// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.Builder;

[assembly: NotInParallel]

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3.Tests;
#else
namespace Akavache.EncryptedSqlite3.Tests;
#endif

/// <summary>Resets shared Akavache and encrypted SQLite static state around each encrypted SQLite test.</summary>
public static class GlobalTestHooks
{
    /// <summary>Resets shared static state used by encrypted SQLite tests, both before and after each test.</summary>
    [BeforeEvery(Test)]
    [AfterEvery(Test)]
    public static void ResetState()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();
        RequestCache.Clear();
        UniversalSerializer.ResetCaches();
        AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        AppBuilder.ResetBuilderStateForTests();
    }
}
