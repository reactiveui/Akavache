// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests;

using Splat.Builder;

[assembly: NotInParallel]

namespace Akavache.EncryptedSqlite3.Tests;

/// <summary>Resets shared Akavache and encrypted SQLite static state around each encrypted SQLite test.</summary>
public static class GlobalTestHooks
{
    /// <summary>Resets shared static state used by encrypted SQLite tests, both before and after each test.</summary>
    [BeforeEvery(Test)]
    [AfterEvery(Test)]
    public static void ResetState()
    {
        CacheDatabase.ResetForTests().WaitForCompletion();
        Akavache.Core.RequestCache.Clear();
        Akavache.Core.UniversalSerializer.ResetCaches();
        Akavache.EncryptedSqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        AppBuilder.ResetBuilderStateForTests();
    }
}
