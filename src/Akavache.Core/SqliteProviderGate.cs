// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Process-wide gate used by the SQLite backends to ensure the native SQLitePCL provider
/// is registered exactly once, even when multiple Akavache SQLite assemblies
/// (<c>Akavache.Sqlite3</c> and <c>Akavache.EncryptedSqlite3</c>) are loaded into the
/// same process. Each assembly has its own <c>SqlitePclRawConnection</c> type compiled
/// from shared source, so a per-class static flag would fire twice — this gate lives in
/// <see cref="Akavache"/> core and is shared by both.
/// </summary>
internal static class SqliteProviderGate
{
    /// <summary>Backing field for the initialize-once flag.</summary>
    private static int _initialized;

    /// <summary>
    /// Attempts to claim the right to call <c>SQLitePCL.Batteries_V2.Init()</c>. Returns
    /// <see langword="true"/> exactly once per process; subsequent calls return
    /// <see langword="false"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the caller should perform native provider init.</returns>
    public static bool TryClaimInit() =>
        System.Threading.Interlocked.Exchange(ref _initialized, 1) == 0;
}
