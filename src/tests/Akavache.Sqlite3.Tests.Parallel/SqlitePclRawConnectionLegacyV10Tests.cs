// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using SQLitePCL;

namespace Akavache.Tests;

/// <summary>
/// Tests for the V10 compatibility reads on <see cref="SqlitePclRawConnection"/> — the paths that
/// look for an Akavache 10 <c>CacheElement</c> table alongside the V11 schema.
/// </summary>
[Category("Akavache")]
public class SqlitePclRawConnectionLegacyV10Tests
{
    /// <summary>Type discriminator carried by the legacy rows a typed probe is expected to match.</summary>
    private const string MatchingTypeName = "MyType";

    /// <summary>Type discriminator carried by legacy rows a typed probe must skip.</summary>
    private const string SkippedTypeName = "OtherType";

    /// <summary>Bind index of the <c>Key</c> column on the legacy insert statement.</summary>
    private const int KeyParameterIndex = 1;

    /// <summary>Bind index of the <c>TypeName</c> column on the legacy insert statement.</summary>
    private const int TypeNameParameterIndex = 2;

    /// <summary>Bind index of the <c>Value</c> column on the legacy insert statement.</summary>
    private const int ValueParameterIndex = 3;

    /// <summary>Bind index of the <c>Expiration</c> column on the legacy insert statement.</summary>
    private const int ExpirationParameterIndex = 4;

    /// <summary>Bind index of the <c>CreatedAt</c> column on the legacy insert statement.</summary>
    private const int CreatedAtParameterIndex = 5;

    /// <summary>Expiration ticks that mean "this legacy row never expires".</summary>
    private const long NeverExpires = 0;

    /// <summary>Payload of the legacy row found through its assembly-qualified type name.</summary>
    private static readonly byte[] AssemblyQualifiedRowPayload = [42, 43, 44];

    /// <summary>Payload of the legacy row found through its type's full name.</summary>
    private static readonly byte[] FullNameRowPayload = [10, 20];

    /// <summary>Payload of the legacy row found without any type discriminator.</summary>
    private static readonly byte[] UntypedRowPayload = [99];

    /// <summary>Payload of the legacy row whose expiry has already passed.</summary>
    private static readonly byte[] ExpiredRowPayload = [1];

    /// <summary>Payload of the legacy row stored under a type the probe does not ask for.</summary>
    private static readonly byte[] MismatchedTypeRowPayload = [1, 2];

    /// <summary>Payload of the first row read back by a whole-table scan.</summary>
    private static readonly byte[] FirstScannedRowPayload = [1, 2];

    /// <summary>Payload of the second row read back by a whole-table scan.</summary>
    private static readonly byte[] SecondScannedRowPayload = [3, 4, 5];

    /// <summary>
    /// TryReadLegacyV10Value returns null when the database has no legacy CacheElement table.
    /// Covers the AkavacheSqliteException catch paths in TryLegacyTyped and TryLegacyUntyped
    /// (lines 876-879, 915-918).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_NoLegacyTable_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var result = cache.Connection.TryReadLegacyV10Value("somekey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// TryReadLegacyV10Value with a null type falls back to untyped search only.
    /// Covers the null type-name branch (lines 790-791).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_NullType_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var result = cache.Connection.TryReadLegacyV10Value("somekey", TimeProvider.System.GetUtcNow(), null).WaitForValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// TryReadLegacyV10Value reads data from a manually created legacy CacheElement table
    /// using an assembly-qualified type name match.
    /// Covers lines 794-795 (assemblyQualifiedName match path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_WithLegacyTable_ReturnsValueByAssemblyQualifiedName()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // Pre-create the legacy table before opening the connection.
        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "legacyKey", typeof(string).AssemblyQualifiedName!, AssemblyQualifiedRowPayload, expiration: NeverExpires);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("legacyKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(AssemblyQualifiedRowPayload);
    }

    /// <summary>
    /// TryReadLegacyV10Value falls back to FullName match when AssemblyQualifiedName does not match.
    /// Covers lines 798-800 (typeFullNameMatch path).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_WithLegacyTable_FallsBackToFullName()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // Store the entry with FullName (not AQN) so the AQN probe misses and the FQN probe hits.
        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "fqnKey", typeof(string).FullName!, FullNameRowPayload, expiration: NeverExpires);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("fqnKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(FullNameRowPayload);
    }

    /// <summary>TryReadLegacyV10Value falls back to untyped search when type is null. Covers TryLegacyUntyped path (lines 915-937).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_WithLegacyTable_UntypedFallback()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "untypedKey", typeName: null, UntypedRowPayload, expiration: NeverExpires);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("untypedKey", TimeProvider.System.GetUtcNow(), type: null).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(UntypedRowPayload);
    }

    /// <summary>TryReadLegacyV10Value returns null for an expired legacy row. Covers the expiration check in TryLegacyUntyped.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_ExpiredRow_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        CreateLegacyV10Table(dbPath);
        var pastTicks = TimeProvider.System.GetUtcNow().AddHours(-1).UtcTicks;
        InsertLegacyV10Row(dbPath, "expiredLegacy", typeName: null, ExpiredRowPayload, expiration: pastTicks);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("expiredLegacy", TimeProvider.System.GetUtcNow(), type: null).WaitForValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// TryReadLegacyV10Value with a typed query returns the value via untyped fallback
    /// when the legacy row's type does not match the requested type.
    /// Covers the typed search miss then untyped hit path in TryLegacyTyped/TryLegacyUntyped.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReadLegacyV10Value_TypeMismatch_FallsBackToUntyped()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "typedKey", typeof(int).FullName!, MismatchedTypeRowPayload, expiration: NeverExpires);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        // Search for typeof(string) which won't match the stored typeof(int).
        // Both AQN and FQN typed probes miss, but the untyped probe succeeds.
        var result = conn.TryReadLegacyV10Value("typedKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
    }

    /// <summary>ReadAllLegacyV10Rows reads all rows from a manually created legacy CacheElement table. Covers lines 810-834.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadAllLegacyV10Rows_ReturnsAllLegacyRows()
    {
        const int ExpectedRowCount = 3;
        const long ExpiryOffsetTicks = 1000;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        var nowTicks = TimeProvider.System.GetUtcNow().UtcTicks;
        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "row1", MatchingTypeName, FirstScannedRowPayload, expiration: NeverExpires, createdAt: nowTicks);
        InsertLegacyV10Row(dbPath, "row2", typeName: null, SecondScannedRowPayload, expiration: nowTicks + ExpiryOffsetTicks, createdAt: nowTicks);
        InsertLegacyV10Row(dbPath, "row3", SkippedTypeName, value: null, expiration: NeverExpires, createdAt: nowTicks);

        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var rows = conn.ReadAllLegacyV10Rows().ToList().WaitForValue()!;
        await Assert.That(rows.Count).IsEqualTo(ExpectedRowCount);

        var row1 = rows.First(static r => r.Key == "row1");
        await Assert.That(row1.TypeName).IsEqualTo(MatchingTypeName);
        await Assert.That(row1.Value).IsEquivalentTo(FirstScannedRowPayload);

        var row2 = rows.First(static r => r.Key == "row2");
        await Assert.That(row2.TypeName).IsNull();
        await Assert.That(row2.Value).IsEquivalentTo(SecondScannedRowPayload);

        var row3 = rows.First(static r => r.Key == "row3");
        await Assert.That(row3.TypeName).IsEqualTo(SkippedTypeName);
        await Assert.That(row3.Value).IsNull();
    }

    /// <summary>Creates a <see cref="SqliteBlobCache"/> in the given directory.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <returns>A new <see cref="SqliteBlobCache"/>.</returns>
    private static SqliteBlobCache CreateCache(string path) =>
        new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), new SystemJsonSerializer(), ImmediateScheduler.Instance);

    /// <summary>Creates the legacy V10 CacheElement table using a direct SQLite connection.</summary>
    /// <param name="dbPath">The database file path.</param>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Multi line sql statement. Needs the span.")]
    private static void CreateLegacyV10Table(string dbPath)
    {
        Batteries_V2.Init();
        _ = raw.sqlite3_open_v2(
            dbPath,
            out var db,
            raw.SQLITE_OPEN_READWRITE | raw.SQLITE_OPEN_CREATE,
            null);
        try
        {
            _ = raw.sqlite3_exec(
                db,
                """
                CREATE TABLE IF NOT EXISTS "CacheElement" (
                "Key" TEXT PRIMARY KEY,
                "TypeName" TEXT,
                "Value" BLOB,
                "Expiration" INTEGER,
                "CreatedAt" INTEGER)
                """);
        }
        finally
        {
            db.Dispose();
        }
    }

    /// <summary>Inserts a row into the legacy V10 CacheElement table using a direct SQLite connection.</summary>
    /// <param name="dbPath">The database file path.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="typeName">The type name, or null.</param>
    /// <param name="value">The value blob, or null.</param>
    /// <param name="expiration">The expiration ticks (0 = never expires).</param>
    /// <param name="createdAt">The creation ticks.</param>
    private static void InsertLegacyV10Row(
        string dbPath,
        string key,
        string? typeName,
        byte[]? value,
        long expiration,
        long createdAt = 0)
    {
        Batteries_V2.Init();
        _ = raw.sqlite3_open_v2(
            dbPath,
            out var db,
            raw.SQLITE_OPEN_READWRITE,
            null);
        try
        {
            const string sql = "INSERT INTO \"CacheElement\" (\"Key\", \"TypeName\", \"Value\", \"Expiration\", \"CreatedAt\") VALUES (?, ?, ?, ?, ?)";
            _ = raw.sqlite3_prepare_v2(db, sql, out var stmt);
            try
            {
                _ = raw.sqlite3_bind_text(stmt, KeyParameterIndex, key);
                if (typeName is null)
                {
                    _ = raw.sqlite3_bind_null(stmt, TypeNameParameterIndex);
                }
                else
                {
                    _ = raw.sqlite3_bind_text(stmt, TypeNameParameterIndex, typeName);
                }

                if (value is null)
                {
                    _ = raw.sqlite3_bind_null(stmt, ValueParameterIndex);
                }
                else
                {
                    _ = raw.sqlite3_bind_blob(stmt, ValueParameterIndex, value);
                }

                _ = raw.sqlite3_bind_int64(stmt, ExpirationParameterIndex, expiration);
                _ = raw.sqlite3_bind_int64(stmt, CreatedAtParameterIndex, createdAt);
                _ = raw.sqlite3_step(stmt);
            }
            finally
            {
                stmt.Dispose();
            }
        }
        finally
        {
            db.Dispose();
        }
    }
}
