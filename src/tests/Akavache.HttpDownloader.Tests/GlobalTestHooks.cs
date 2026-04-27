// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// HTTP tests spin up TestHttpServer instances and make real network calls.
// Run sequentially to avoid TCP resource contention across simultaneous MTP assemblies.
[assembly: NotInParallel]

namespace Akavache.HttpDownloader.Tests;

/// <summary>
/// One-time assembly setup for HTTP tests.
/// </summary>
public static class GlobalTestHooks
{
    /// <summary>
    /// Initializes the SQLite provider once before any test runs.
    /// </summary>
    [Before(Assembly)]
    public static void InitSqliteProvider() => SQLitePCL.Batteries_V2.Init();
}
