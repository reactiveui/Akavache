// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;

using SQLitePCL;

using static SQLitePCL.raw;

namespace Akavache.Tests;

/// <summary>
/// Backward-compatibility tests for the encrypted Sqlite cache. Akavache 11.x shipped
/// the SQLCipher-4 native bundle; v12 ships SQLite3MC. The two ciphers don't agree on
/// page-1 encryption out of the box, so without a fallback v12 cannot read databases
/// produced by v11. The connection's open path detects this case and re-opens with
/// SQLite3MC's SQLCipher-4 compatibility shim, then rekeys forward to the modern cipher.
/// </summary>
[Category("Akavache")]
[Category("Sqlite")]
public class EncryptedSqlite3LegacyV11CompatibilityTests
{
    /// <summary>
    /// A v11-style file (SQLCipher-4 cipher) must be readable through the v12 public API,
    /// and after the first open it must be rekeyed to the modern cipher so subsequent
    /// opens take the fast native path.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Test]
    public async Task EncryptedSqliteBlobCacheReadsDatabaseWrittenBySqlCipher4()
    {
        using var dir = Utility.WithEmptyDirectory(out var tempDir);
        var path = Path.Combine(tempDir, "v11.db");
        const string password = "v11-back-compat";
        const string key = "legacy-key";
        byte[] payload = [11, 22, 33, 44, 55];

        SeedSqlCipher4Database(path, password, key, payload);

        // First open: the modern-cipher fast path fails with SQLITE_NOTADB, the
        // SQLCipher-4 fallback succeeds, and the file is rekeyed forward in place.
        SystemJsonSerializer serializer = new();
        using (EncryptedSqliteBlobCache cache = new(path, password, serializer, ImmediateScheduler.Instance))
        {
            var fetched = cache.Get(key).WaitForValue();
            await Assert.That(fetched).IsEquivalentTo(payload);
        }

        // Second open: must succeed with only PRAGMA key (no SQLCipher-4 shim). If the
        // rekey hadn't run, opening with the shim disabled would surface SQLITE_NOTADB.
        AssertReadableWithModernCipher(path, password);

        // And the public API still works after the rekey.
        using (EncryptedSqliteBlobCache cache = new(path, password, serializer, ImmediateScheduler.Instance))
        {
            var fetched = cache.Get(key).WaitForValue();
            await Assert.That(fetched).IsEquivalentTo(payload);
        }
    }

    /// <summary>
    /// A wrong password must still fail loudly — the legacy fallback should not
    /// silently mask a genuine key error.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Test]
    public async Task LegacyFallbackDoesNotMaskWrongPassword()
    {
        using var dir = Utility.WithEmptyDirectory(out var tempDir);
        var path = Path.Combine(tempDir, "v11.db");
        const string correctPassword = "correct-horse-battery-staple";
        const string wrongPassword = "definitely-not-it";

        SeedSqlCipher4Database(path, correctPassword, key: "k", value: [1]);

        SystemJsonSerializer serializer = new();
        var ex = Assert.Throws<AkavacheSqliteException>(() =>
        {
            using EncryptedSqliteBlobCache cache = new(path, wrongPassword, serializer, ImmediateScheduler.Instance);
            cache.Get("k").WaitForValue();
        });

        // The legacy fallback re-validates page 1 with the supplied password and
        // surfaces the same SQLITE_NOTADB the modern path would have raised.
        await Assert.That(ex.ResultCode).IsEqualTo(SQLITE_NOTADB);
    }

    /// <summary>
    /// Writes <paramref name="path"/> in the same on-disk shape that the Akavache 11.x
    /// encrypted provider produced: a SQLCipher-4-encrypted SQLite database with the
    /// CacheEntry table populated with one row.
    /// </summary>
    /// <param name="path">Where to write the seed database.</param>
    /// <param name="password">The encryption key applied via <c>PRAGMA key</c>.</param>
    /// <param name="key">The CacheEntry row id.</param>
    /// <param name="value">The CacheEntry row payload.</param>
    private static void SeedSqlCipher4Database(string path, string password, string key, byte[] value)
    {
        Batteries_V2.Init();

        var rc = sqlite3_open_v2(path, out var db, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, null);
        ThrowIfNotOk(rc, db, "open " + path);
        try
        {
            var quotedPassword = "'" + password.Replace("'", "''") + "'";

            // SQLite3MC's SQLCipher-4 compatibility mode — what Akavache 11.x effectively
            // wrote out via the e_sqlcipher native bundle.
            Exec(db, "PRAGMA cipher = 'sqlcipher'");
            Exec(db, "PRAGMA legacy = 4");
            Exec(db, "PRAGMA key = " + quotedPassword);

            const string createTable =
                "CREATE TABLE \"CacheEntry\" (" +
                "\"Id\" TEXT PRIMARY KEY NOT NULL, " +
                "\"CreatedAt\" INTEGER NOT NULL, " +
                "\"ExpiresAt\" INTEGER NULL, " +
                "\"TypeName\" TEXT NULL, " +
                "\"Value\" BLOB NULL);";
            Exec(db, createTable);

            const string insertSql =
                "INSERT INTO \"CacheEntry\" (\"Id\", \"CreatedAt\", \"ExpiresAt\", \"TypeName\", \"Value\") VALUES (?, ?, NULL, NULL, ?)";
            rc = sqlite3_prepare_v2(db, insertSql, out var stmt);
            ThrowIfNotOk(rc, db, "prepare insert");
            try
            {
                ThrowIfNotOk(sqlite3_bind_text(stmt, 1, key), db, "bind id");
                ThrowIfNotOk(sqlite3_bind_int64(stmt, 2, DateTimeOffset.UtcNow.UtcTicks), db, "bind createdAt");
                ThrowIfNotOk(sqlite3_bind_blob(stmt, 3, value), db, "bind value");

                if (sqlite3_step(stmt) != SQLITE_DONE)
                {
                    throw new InvalidOperationException("seed insert failed: " + sqlite3_errmsg(db).utf8_to_string());
                }
            }
            finally
            {
                sqlite3_finalize(stmt);
            }
        }
        finally
        {
            db.Dispose();
        }
    }

    /// <summary>
    /// Opens <paramref name="path"/> with only <c>PRAGMA key</c> (no SQLCipher-4 shim) and
    /// touches page 1. Throws if the file isn't already in SQLite3MC's modern cipher format.
    /// </summary>
    /// <param name="path">The database file to probe.</param>
    /// <param name="password">The encryption key applied via <c>PRAGMA key</c>.</param>
    private static void AssertReadableWithModernCipher(string path, string password)
    {
        Batteries_V2.Init();

        var rc = sqlite3_open_v2(path, out var db, SQLITE_OPEN_READONLY, null);
        ThrowIfNotOk(rc, db, "open " + path);
        try
        {
            var quotedPassword = "'" + password.Replace("'", "''") + "'";
            Exec(db, "PRAGMA key = " + quotedPassword);
            Exec(db, "SELECT count(*) FROM sqlite_master");
        }
        finally
        {
            db.Dispose();
        }
    }

    /// <summary>Runs a non-result SQL statement and throws if the call fails.</summary>
    /// <param name="db">An open SQLite handle.</param>
    /// <param name="sql">The statement to execute.</param>
    private static void Exec(sqlite3 db, string sql) =>
        ThrowIfNotOk(sqlite3_exec(db, sql), db, "exec: " + sql);

    /// <summary>Throws an <see cref="InvalidOperationException"/> when a SQLite call returns an error code.</summary>
    /// <param name="rc">The SQLite return code.</param>
    /// <param name="db">An open SQLite handle, used to extract a textual error message.</param>
    /// <param name="operation">A description of the operation, included in the exception message.</param>
    private static void ThrowIfNotOk(int rc, sqlite3 db, string operation)
    {
        if (rc is SQLITE_OK or SQLITE_DONE or SQLITE_ROW)
        {
            return;
        }

        var detail = sqlite3_errmsg(db).utf8_to_string();
        throw new InvalidOperationException($"sqlite rc={rc} during {operation}: {detail}");
    }
}
