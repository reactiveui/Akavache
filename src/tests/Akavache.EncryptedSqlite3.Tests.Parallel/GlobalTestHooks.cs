// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSqlite3.Tests.Parallel;
#else
namespace Akavache.EncryptedSqlite3.Tests.Parallel;
#endif

/// <summary>One-time assembly setup for encrypted SQLite parallel tests.</summary>
public static class GlobalTestHooks
{
    /// <summary>Initializes the SQLite provider once before any test runs.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Before(Assembly)]
    public static void InitSqliteProvider() => SQLitePCL.Batteries_V2.Init();
}
