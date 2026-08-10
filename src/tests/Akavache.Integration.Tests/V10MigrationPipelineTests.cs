// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SQLitePCL;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Drives the V10-to-V11 migration against real SQLite files: the sentinel that stops it running
/// twice, the row walk, the per-row conversion failures it tolerates, and the optional delete of
/// the source database.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class V10MigrationPipelineTests
{
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

    /// <summary>Expiration ticks meaning "this legacy row never expires".</summary>
    private const long NeverExpires = 0;

    /// <summary>The number of rows seeded by the two-row migration cases.</summary>
    private const int SeededRowCount = 2;

    /// <summary>The value the re-serialization cases round-trip from V10's BSON.</summary>
    private const string ReserializedValue = "a value written by V10";

    /// <summary>A tick count safely inside the range the converter accepts.</summary>
    private static readonly long ValidTicks = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    /// <summary>Payload of the first seeded legacy row.</summary>
    private static readonly byte[] FirstRowPayload = [1, 2, 3];

    /// <summary>Payload of the second seeded legacy row.</summary>
    private static readonly byte[] SecondRowPayload = [4, 5];

    /// <summary>A migration with no V10 file on disk reports the absence and does nothing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldReportAnAbsentDatabase()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var destination = CreateCache(path, "v11");
        List<string> log = [];

        _ = await V10MigrationService.Migrate(
            Path.Combine(path, "missing.db"),
            destination,
            new SystemJsonSerializer(),
            new(Logger: log.Add)).FirstAsync();

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("V10 database not found");
    }

    /// <summary>Rows in the legacy table land in the V11 cache under their original keys.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldCopyLegacyRowsIntoTheV11Cache()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Path = SeedLegacyDatabase(path, seedRows: true);
        using var destination = CreateCache(path, "v11");
        List<string> log = [];

        _ = await V10MigrationService.Migrate(
            v10Path,
            destination,
            new SystemJsonSerializer(),
            new(Logger: log.Add)).FirstAsync();

        var first = destination.Connection.Get("first", typeFullName: null, TimeProvider.System.GetUtcNow()).WaitForValue();
        var second = destination.Connection.Get("second", typeFullName: null, TimeProvider.System.GetUtcNow()).WaitForValue();

        using (Assert.Multiple())
        {
            await Assert.That(first?.Value).IsEquivalentTo(FirstRowPayload);
            await Assert.That(second?.Value).IsEquivalentTo(SecondRowPayload);
        }
    }

    /// <summary>The row count found in the legacy table is reported before the copy starts.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldReportHowManyEntriesItFound()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Path = SeedLegacyDatabase(path, seedRows: true);
        using var destination = CreateCache(path, "v11");
        List<string> log = [];

        _ = await V10MigrationService.Migrate(
            v10Path,
            destination,
            new SystemJsonSerializer(),
            new(Logger: log.Add)).FirstAsync();

        await Assert.That(log).Contains(static x => x.Contains($"Found {SeededRowCount} entries", StringComparison.Ordinal));
        await Assert.That(log).Contains(static x => x.Contains($"Migrated {SeededRowCount} entries", StringComparison.Ordinal));
    }

    /// <summary>Running the migration twice is a no-op the second time, because the sentinel is present.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldSkipWhenTheSentinelIsAlreadyWritten()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Path = SeedLegacyDatabase(path, seedRows: true);
        using var destination = CreateCache(path, "v11");
        SystemJsonSerializer serializer = new();

        _ = await V10MigrationService.Migrate(v10Path, destination, serializer, new()).FirstAsync();

        List<string> log = [];
        _ = await V10MigrationService.Migrate(v10Path, destination, serializer, new(Logger: log.Add)).FirstAsync();

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("Migration already completed");
    }

    /// <summary>
    /// A database without the legacy table is reported and left alone. This path short-circuits
    /// while still on the V10 connection's worker thread, so it is also what pins that closing the
    /// connection from there does not wedge the worker.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldReportADatabaseWithNoLegacyTable()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);

        // Materialise a database carrying only the V11 schema, then close it so the migration
        // opens the file itself.
        using (var source = CreateCache(path, "no-legacy-table"))
        {
            _ = source.Connection.Get("warm", typeFullName: null, TimeProvider.System.GetUtcNow()).WaitForValue();
        }

        using var destination = CreateCache(path, "v11");
        List<string> log = [];

        _ = await V10MigrationService.Migrate(
            Path.Combine(path, "no-legacy-table.db"),
            destination,
            new SystemJsonSerializer(),
            new(Logger: log.Add)).FirstAsync();

        await Assert.That(log).Contains(static x => x.Contains("No CacheElement table", StringComparison.Ordinal));
    }

    /// <summary>An empty legacy table still writes the sentinel, so the migration does not re-run.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldWriteTheSentinelForAnEmptyLegacyTable()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Path = SeedLegacyDatabase(path, seedRows: false);
        using var destination = CreateCache(path, "v11");

        _ = await V10MigrationService.Migrate(v10Path, destination, new SystemJsonSerializer(), new()).FirstAsync();

        var migrated = V10MigrationService.IsMigrationComplete(destination).WaitForValue();
        await Assert.That(migrated).IsTrue();
    }

    /// <summary>With the delete option on, the source database is removed once the copy succeeds.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldDeleteTheSourceDatabaseWhenAsked()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Path = SeedLegacyDatabase(path, seedRows: true);
        using var destination = CreateCache(path, "v11");
        List<string> log = [];

        _ = await V10MigrationService.Migrate(
            v10Path,
            destination,
            new SystemJsonSerializer(),
            new(DeleteOldFiles: true, Logger: log.Add)).FirstAsync();

        using (Assert.Multiple())
        {
            await Assert.That(File.Exists(v10Path)).IsFalse();
            await Assert.That(log).Contains(static x => x.Contains("Deleted V10 database", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// With re-serialization on, a V10 BSON payload lands in the V11 cache rewritten into the
    /// current serializer's format rather than as the bytes V10 stored.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldRewritePayloadsWhenReserializationIsOn()
    {
        SerializerRegistryFixture.RegisterAll();
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Payload = new NewtonsoftBsonSerializer().Serialize(ReserializedValue);
        var v10Path = SeedLegacyDatabaseWithBson(path, v10Payload);
        using var destination = CreateCache(path, "v11");

        _ = await V10MigrationService.Migrate(
            v10Path,
            destination,
            new SystemJsonSerializer(),
            new(ReserializeToCurrentFormat: true)).FirstAsync();

        var migrated = destination.Connection.Get("bson", typeFullName: null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(migrated?.Value).IsNotEquivalentTo(v10Payload);
    }

    /// <summary>With re-serialization off, the bytes V10 stored are migrated across untouched.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateShouldKeepOriginalPayloadsWhenReserializationIsOff()
    {
        SerializerRegistryFixture.RegisterAll();
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var v10Payload = new NewtonsoftBsonSerializer().Serialize(ReserializedValue);
        var v10Path = SeedLegacyDatabaseWithBson(path, v10Payload);
        using var destination = CreateCache(path, "v11");

        _ = await V10MigrationService.Migrate(
            v10Path,
            destination,
            new SystemJsonSerializer(),
            new(ReserializeToCurrentFormat: false)).FirstAsync();

        var migrated = destination.Connection.Get("bson", typeFullName: null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(migrated?.Value).IsEquivalentTo(v10Payload);
    }

    /// <summary>A cache with no sentinel row has not been migrated.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsMigrationCompleteShouldBeFalseForAFreshCache()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var destination = CreateCache(path, "fresh");

        var result = V10MigrationService.IsMigrationComplete(destination).WaitForValue();

        await Assert.That(result).IsFalse();
    }

    /// <summary>Writing the sentinel is what makes a later migration skip.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WriteMigrationSentinelShouldMarkTheCacheAsMigrated()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var destination = CreateCache(path, "sentinel");

        _ = await V10MigrationService.WriteMigrationSentinel(destination).FirstAsync();

        var result = V10MigrationService.IsMigrationComplete(destination).WaitForValue();
        await Assert.That(result).IsTrue();
    }

    /// <summary>A file that cannot be deleted is reported rather than failing the migration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeleteV10DatabaseShouldReportAFailedDelete()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        List<string> log = [];

        // A directory cannot be removed with File.Delete, which is the failure this path reports.
        V10MigrationService.TryDeleteV10Database(path, new(Logger: log.Add));

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("Failed to delete V10 database");
    }

    /// <summary>Deleting a database that is present reports the removal.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryDeleteV10DatabaseShouldReportASuccessfulDelete()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var file = Path.Combine(path, "gone.db");
        await File.WriteAllTextAsync(file, "not really a database");
        List<string> log = [];

        V10MigrationService.TryDeleteV10Database(file, new(Logger: log.Add));

        using (Assert.Multiple())
        {
            await Assert.That(File.Exists(file)).IsFalse();
            await Assert.That(log[0]).Contains("Deleted V10 database");
        }
    }

    /// <summary>Creates a SQLite-backed cache inside the supplied directory.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <param name="name">The file stem.</param>
    /// <returns>A new cache.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SqliteBlobCache CreateCache(string path, string name) =>
        new(Path.Combine(path, $"{name}.db"), new SystemJsonSerializer(), ImmediateSequencer.Instance);

    /// <summary>Creates a legacy V10 database holding a single BSON row under a resolvable type.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <param name="payload">The BSON payload to store.</param>
    /// <returns>The path to the seeded database.</returns>
    private static string SeedLegacyDatabaseWithBson(string path, byte[] payload)
    {
        var dbPath = Path.Combine(path, "v10-bson.db");
        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "bson", typeof(string).AssemblyQualifiedName, payload, NeverExpires, ValidTicks);

        return dbPath;
    }

    /// <summary>Creates a legacy V10 database, optionally seeded with two rows.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <param name="seedRows">Whether to insert rows into the legacy table.</param>
    /// <returns>The path to the seeded database.</returns>
    private static string SeedLegacyDatabase(string path, bool seedRows)
    {
        var dbPath = Path.Combine(path, "v10.db");
        CreateLegacyV10Table(dbPath);

        if (seedRows)
        {
            InsertLegacyV10Row(dbPath, "first", typeName: null, FirstRowPayload, NeverExpires, ValidTicks);
            InsertLegacyV10Row(dbPath, "second", typeName: null, SecondRowPayload, NeverExpires, ValidTicks);
        }

        return dbPath;
    }

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
        long createdAt)
    {
        Batteries_V2.Init();
        _ = raw.sqlite3_open_v2(dbPath, out var db, raw.SQLITE_OPEN_READWRITE, null);
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
                _ = raw.sqlite3_finalize(stmt);
            }
        }
        finally
        {
            db.Dispose();
        }
    }
}
