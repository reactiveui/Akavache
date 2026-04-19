// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3.Tests.Parallel;

/// <summary>
/// One-time assembly setup for SQLite parallel tests.
/// </summary>
public static class GlobalTestHooks
{
    /// <summary>
    /// Initializes the SQLite provider once before any test runs.
    /// </summary>
    [Before(Assembly)]
    public static void InitSqliteProvider() => SQLitePCL.Batteries_V2.Init();
}
