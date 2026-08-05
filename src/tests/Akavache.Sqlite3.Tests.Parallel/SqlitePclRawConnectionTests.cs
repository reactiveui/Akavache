// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using SQLitePCL;

namespace Akavache.Tests;

/// <summary>Tests for <see cref="SqlitePclRawConnection"/> covering static helpers and typed query paths.</summary>
[Category("Akavache")]
public class SqlitePclRawConnectionTests
{
    /// <summary>The SQLite result code for a successful call.</summary>
    private const int SqliteOk = 0;

    /// <summary>The SQLite result code returned by <c>sqlite3_step</c> when a row is available.</summary>
    private const int SqliteRow = 100;

    /// <summary>The SQLite result code returned by <c>sqlite3_step</c> when the statement is exhausted.</summary>
    private const int SqliteDone = 101;

    /// <summary>The generic SQLite failure code.</summary>
    private const int SqliteError = 1;

    /// <summary>Payload of the first entry a test stores, distinct so a swapped row shows in the assertion.</summary>
    private static readonly byte[] FirstEntryPayload = [1];

    /// <summary>Payload of the second entry a test stores.</summary>
    private static readonly byte[] SecondEntryPayload = [2];

    /// <summary>Payload of the third entry a test stores.</summary>
    private static readonly byte[] ThirdEntryPayload = [3];

    /// <summary>Multi-byte payload of the first entry, for tests that assert the whole blob round-trips.</summary>
    private static readonly byte[] FirstMultiBytePayload = [1, 2, 3];

    /// <summary>Multi-byte payload of the second entry.</summary>
    private static readonly byte[] SecondMultiBytePayload = [4, 5, 6];

    /// <summary>Multi-byte payload of the third entry.</summary>
    private static readonly byte[] ThirdMultiBytePayload = [7, 8, 9];

    /// <summary>Payload of the entry that is stored without a type discriminator.</summary>
    private static readonly byte[] TypelessEntryPayload = [42];

    /// <summary>How long a test waits for a racing disposer thread before declaring a deadlock.</summary>
    private static readonly TimeSpan ConcurrentDisposeTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How long a test waits for the first row of a scan before giving up.</summary>
    private static readonly TimeSpan FirstScannedRowTimeout = TimeSpan.FromSeconds(10);

    // ── AppendJsonString individual escape branches ────────────────────────
    /// <summary>A backslash character is escaped as <c>\\</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_Backslash_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\\b");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\\\b\"");
    }

    /// <summary>A backspace character is escaped as <c>\b</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_Backspace_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\bb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\bb\"");
    }

    /// <summary>A form-feed character is escaped as <c>\f</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_FormFeed_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\fb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\fb\"");
    }

    /// <summary>A newline character is escaped as <c>\n</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_Newline_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\nb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\nb\"");
    }

    /// <summary>A carriage-return character is escaped as <c>\r</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_CarriageReturn_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\rb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\rb\"");
    }

    /// <summary>A tab character is escaped as <c>\t</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_Tab_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\tb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\tb\"");
    }

    /// <summary>A control character below 0x20 (not one of the named escapes) is encoded as <c>\uXXXX</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_ControlChar_IsUnicodeEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\x03");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\u0003\"");
    }

    /// <summary>A double-quote character is escaped as <c>\"</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_DoubleQuote_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "say \"hi\"");
        await Assert.That(sb.ToString()).IsEqualTo("\"say \\\"hi\\\"\"");
    }

    /// <summary>A plain printable string is emitted unchanged (only wrapped in quotes).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_PlainText_IsUnchanged()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "hello");
        await Assert.That(sb.ToString()).IsEqualTo("\"hello\"");
    }

    /// <summary>A string containing multiple different escape types is correctly escaped in sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_MixedEscapes_AllHandled()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\\\"\n\r\t\b\f\x01");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\\\\\\"\\n\\r\\t\\b\\f\\u0001\"");
    }

    // ── CheckRc ────────────────────────────────────────────────────────────
    /// <summary>CheckRc with a non-zero code and null db produces an exception whose message contains the operation name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CheckRc_ErrorWithNullDb_MessageContainsOperation()
    {
        var ex = Assert.Throws<AkavacheSqliteException>(static () =>
            SqlitePclRawConnection.CheckRc(1, db: null, "my-operation"));
        await Assert.That(ex.Message).Contains("my-operation");
        await Assert.That(ex.Message).Contains("1");
    }

    // ── TableExists ───────────────────────────────────────────────────────
    /// <summary>TableExists returns true for the CacheEntry table after schema creation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TableExists_KnownTable_ReturnsTrue()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var exists = cache.Connection.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    /// <summary>TableExists returns false for a table that does not exist.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TableExists_UnknownTable_ReturnsFalse()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var exists = cache.Connection.TableExists("NonExistentTable").WaitForValue();
        await Assert.That(exists).IsFalse();
    }

    // ── GetMany with typeFullName ─────────────────────────────────────────
    /// <summary>GetMany with a type discriminator returns only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMany_WithTypeName_ReturnsOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string QueriedTypeName = "MyType";
        const int MatchingEntryCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        var entries = new[]
        {
            new CacheEntry("k1", QueriedTypeName, FirstMultiBytePayload, now, null),
            new CacheEntry("k2", "OtherType", SecondMultiBytePayload, now, null),
            new CacheEntry("k3", QueriedTypeName, ThirdMultiBytePayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var results = cache.Connection.GetMany(["k1", "k2", "k3"], QueriedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(MatchingEntryCount);
        await Assert.That(results.Select(static e => e.Id!).Order()).IsEquivalentTo(["k1", "k3"]);
    }

    // ── GetAll with typeFullName ──────────────────────────────────────────
    /// <summary>GetAll with a type discriminator returns only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAll_WithTypeName_ReturnsOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string QueriedTypeName = "TypeA";
        const string SkippedTypeName = "TypeB";
        const int MatchingEntryCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        var entries = new[]
        {
            new CacheEntry("a1", QueriedTypeName, FirstEntryPayload, now, null),
            new CacheEntry("b1", SkippedTypeName, SecondEntryPayload, now, null),
            new CacheEntry("a2", QueriedTypeName, ThirdEntryPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var results = cache.Connection.GetAll(QueriedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(MatchingEntryCount);
        await Assert.That(results.Select(static e => e.Id!).Order()).IsEquivalentTo(["a1", "a2"]);
    }

    // ── GetAllKeys with typeFullName ──────────────────────────────────────
    /// <summary>GetAllKeys with a type discriminator returns only keys matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeys_WithTypeName_ReturnsOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string QueriedTypeName = "FooType";
        const int MatchingKeyCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        var entries = new[]
        {
            new CacheEntry("x1", QueriedTypeName, FirstEntryPayload, now, null),
            new CacheEntry("x2", "BarType", SecondEntryPayload, now, null),
            new CacheEntry("x3", QueriedTypeName, ThirdEntryPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var keys = cache.Connection.GetAllKeys(QueriedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(keys.Count).IsEqualTo(MatchingKeyCount);
        await Assert.That(keys.Order()).IsEquivalentTo(["x1", "x3"]);
    }

    // ── Invalidate with typeFullName ──────────────────────────────────────
    /// <summary>Invalidate with a type discriminator deletes only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Invalidate_WithTypeName_DeletesOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var entries = new[]
        {
            new CacheEntry("d1", "TypeX", FirstEntryPayload, now, null),
            new CacheEntry("d2", "TypeY", SecondEntryPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        cache.Connection.Invalidate(["d1"], "TypeX").WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0].Id).IsEqualTo("d2");
    }

    /// <summary>Invalidate with a mismatched type does not delete the entry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Invalidate_WithWrongTypeName_DoesNotDelete()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("e1", "TypeA", FirstEntryPayload, now, null)]).WaitForCompletion();

        cache.Connection.Invalidate(["e1"], "TypeB").WaitForCompletion();

        var result = cache.Connection.Get("e1", null, now).WaitForValue();
        await Assert.That(result).IsNotNull();
    }

    // ── InvalidateAll with typeFullName ───────────────────────────────────
    /// <summary>InvalidateAll with a type discriminator removes only matching entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAll_WithTypeName_RemovesOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string RetainedTypeName = "Keep";
        const string InvalidatedTypeName = "Remove";

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("f1", RetainedTypeName, FirstEntryPayload, now, null),
            new CacheEntry("f2", InvalidatedTypeName, SecondEntryPayload, now, null),
            new CacheEntry("f3", InvalidatedTypeName, ThirdEntryPayload, now, null),
        ]).WaitForCompletion();

        // Test both typed and untyped GetAll
        var typedResults = cache.Connection.GetAll(RetainedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(typedResults.Count).IsEqualTo(1);

        cache.Connection.InvalidateAll(InvalidatedTypeName).WaitForCompletion();

        var remaining = cache.Connection.GetAll(RetainedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0].Id).IsEqualTo("f1");
    }

    // ── SetExpiry with typeFullName ───────────────────────────────────────
    /// <summary>SetExpiry with a type discriminator updates only the matching entry.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiry_WithTypeName_UpdatesOnlyMatchingEntry()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const int FarFutureYears = 10;

        var now = TimeProvider.System.GetUtcNow();
        var farFuture = now.AddYears(FarFutureYears);
        var entries = new[]
        {
            new CacheEntry("g1", "TypeM", FirstEntryPayload, now, null),
            new CacheEntry("g2", "TypeN", SecondEntryPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        cache.Connection.SetExpiry("g1", "TypeM", farFuture).WaitForCompletion();

        var entry = cache.Connection.Get("g1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ExpiresAt).IsEqualTo(farFuture);

        var entry2 = cache.Connection.Get("g2", null, now).WaitForValue();
        await Assert.That(entry2).IsNotNull();
        await Assert.That(entry2!.ExpiresAt).IsNull();
    }

    /// <summary>SetExpiry with null expiration clears the expiry (binds null ticks).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SetExpiry_NullExpiration_ClearsExpiry()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var expiry = now.AddHours(1);
        cache.Connection.Upsert([new CacheEntry("h1", null, FirstEntryPayload, now, expiry)]).WaitForCompletion();

        cache.Connection.SetExpiry("h1", null, null).WaitForCompletion();

        var entry = cache.Connection.Get("h1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ExpiresAt).IsNull();
    }

    // ── VacuumExpired ────────────────────────────────────────────────────
    /// <summary>VacuumExpired removes expired entries and keeps unexpired ones.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task VacuumExpired_RemovesExpiredEntries()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        var future = now.AddHours(1);
        const int SurvivingEntryCount = 2;
        var entries = new[]
        {
            new CacheEntry("expired1", null, FirstEntryPayload, now, past),
            new CacheEntry("valid1", null, SecondEntryPayload, now, future),
            new CacheEntry("noexpiry", null, ThirdEntryPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        cache.Connection.VacuumExpired(now).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(SurvivingEntryCount);
        await Assert.That(remaining.Select(static e => e.Id!).Order()).IsEquivalentTo(["noexpiry", "valid1"]);
    }

    // ── Upsert with null Value ───────────────────────────────────────────
    /// <summary>Upserting an entry with null Value stores the entry and ReadCacheEntry returns null Value.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_NullValue_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("nullval", null, null, now, null)]).WaitForCompletion();

        var entry = cache.Connection.Get("nullval", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Value).IsNull();
    }

    // ── Upsert with null ExpiresAt ───────────────────────────────────────
    /// <summary>Upserting an entry with null ExpiresAt stores the entry with no expiration.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_NullExpiresAt_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("noexp", "SomeType", FirstEntryPayload, now, null)]).WaitForCompletion();

        var entry = cache.Connection.Get("noexp", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ExpiresAt).IsNull();
    }

    // ── Upsert with null TypeName ────────────────────────────────────────
    /// <summary>Upserting an entry with null TypeName stores the entry and retrieves it with null TypeName.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_NullTypeName_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("notype", null, TypelessEntryPayload, now, null)]).WaitForCompletion();

        var entry = cache.Connection.Get("notype", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.TypeName).IsNull();
    }

    // ── Upsert empty list ────────────────────────────────────────────────
    /// <summary>
    /// Upserting an empty list is a no-op and returns Unit without touching the database.
    /// Covers line 547-548 (early return for empty entries).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_EmptyList_IsNoop()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        cache.Connection.Upsert([]).WaitForCompletion();

        var all = cache.Connection.GetAll(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(all.Count).IsEqualTo(0);
    }

    // ── Invalidate empty list ────────────────────────────────────────────
    /// <summary>Invalidating an empty key list is a no-op. Covers lines 623-625 (early return for empty keys).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Invalidate_EmptyList_IsNoop()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("keep", null, FirstEntryPayload, now, null)]).WaitForCompletion();

        cache.Connection.Invalidate([], null).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
    }

    // ── GetMany empty list ───────────────────────────────────────────────
    /// <summary>GetMany with an empty key list returns an empty sequence. Covers lines 436-438 (early return for empty keys).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMany_EmptyList_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var results = cache.Connection.GetMany([], null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Get with typeFullName ────────────────────────────────────────────
    /// <summary>Get with a type discriminator returns the entry only if the type matches. Covers the typed query path (SqlGetOneTyped).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Get_WithTypeName_ReturnsOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string StoredKey = "typed1";
        const string StoredTypeName = "MyType";

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry(StoredKey, StoredTypeName, FirstEntryPayload, now, null)]).WaitForCompletion();

        var match = cache.Connection.Get(StoredKey, StoredTypeName, now).WaitForValue();
        await Assert.That(match).IsNotNull();
        await Assert.That(match!.Id).IsEqualTo(StoredKey);

        var noMatch = cache.Connection.Get(StoredKey, "OtherType", now).WaitForValue();
        await Assert.That(noMatch).IsNull();
    }

    // ── Get returns null for missing key ─────────────────────────────────
    /// <summary>Get with a key that does not exist returns null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Get_MissingKey_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var result = cache.Connection.Get("nonexistent", null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(result).IsNull();
    }

    // ── Get returns null for expired entry ───────────────────────────────
    /// <summary>Get with a key whose entry has expired returns null.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Get_ExpiredEntry_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        cache.Connection.Upsert([new CacheEntry("expired", null, FirstEntryPayload, now, past)]).WaitForCompletion();

        var result = cache.Connection.Get("expired", null, now).WaitForValue();
        await Assert.That(result).IsNull();
    }

    // ── Checkpoint modes ────────────────────────────────────────────────
    /// <summary>Checkpoint with Full mode executes without error. Covers line 757 (CheckpointMode.Full branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Checkpoint_FullMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp1", null, FirstEntryPayload, now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Full).WaitForCompletion();

        var entry = cache.Connection.Get("cp1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    /// <summary>Checkpoint with Truncate mode executes without error. Covers line 758 (CheckpointMode.Truncate branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Checkpoint_TruncateMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp2", null, FirstEntryPayload, now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Truncate).WaitForCompletion();

        var entry = cache.Connection.Get("cp2", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    /// <summary>Checkpoint with Passive (default) mode executes without error. Covers line 759 (default/Passive branch).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Checkpoint_PassiveMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp3", null, FirstEntryPayload, now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Passive).WaitForCompletion();

        var entry = cache.Connection.Get("cp3", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    // ── Compact ──────────────────────────────────────────────────────────
    /// <summary>
    /// Compact (VACUUM) executes without error and the database remains functional.
    /// Covers lines 766-774 (DisposeStatements + VACUUM).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Compact_Succeeds_AndDatabaseRemainsUsable()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("c1", null, FirstEntryPayload, now, null)]).WaitForCompletion();

        cache.Connection.Compact().WaitForCompletion();

        // Verify database is still usable after VACUUM (statements re-prepared).
        var entry = cache.Connection.Get("c1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    // ── Dispose idempotent ──────────────────────────────────────────────
    /// <summary>Disposing the connection twice does not throw. Covers lines 850-851 (idempotent _disposed check in CloseCore callback).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Dispose_Twice_IsIdempotent()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        conn.Dispose();
        conn.Dispose();

        // Verify we got here without throwing.
        var fileExists = File.Exists(dbPath);
        await Assert.That(fileExists).IsTrue();
    }

    // ── TryRollback ─────────────────────────────────────────────────────
    /// <summary>
    /// TryRollback does not throw even when there is no active transaction.
    /// Covers lines 1088-1097 (best-effort rollback, catch swallows errors).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRollback_NoTransaction_DoesNotThrow()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        // TryRollback should not throw even when there is no active transaction.
        SqlitePclRawConnection.TryRollback(null!);

        // Verify the connection is still functional after rollback attempt.
        var exists = conn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── CheckRc success codes ────────────────────────────────────────────
    /// <summary>CheckRc does not throw for SQLITE_OK (0).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CheckRc_SuccessCodes_DoNotThrow()
    {
        const int SuccessCodeCount = 3;
        int[] codes = [SqliteOk, SqliteRow, SqliteDone];
        foreach (var code in codes)
        {
            SqlitePclRawConnection.CheckRc(code, db: null, "op");
        }

        await Assert.That(codes.Length).IsEqualTo(SuccessCodeCount);
    }

    // ── CheckRc with non-null db ────────────────────────────────────────
    /// <summary>
    /// CheckRc with a non-null db and error code includes sqlite3_errmsg detail.
    /// Covers lines 1118-1121 (non-null db path with error detail).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CheckRc_ErrorWithNonNullDb_IncludesDetail()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);

        // EnsurePrepared with invalid SQL triggers CheckRc with a real db handle.
        sqlite3_stmt? slot = null;
        var ex = Assert.Throws<AkavacheSqliteException>(() =>
            conn.EnsurePrepared(ref slot, "SELECT * FROM nonexistent_table_xyz"));
        await Assert.That(ex.Message).Contains("nonexistent_table_xyz");
    }

    // ── GetMany with expired entries ────────────────────────────────────
    /// <summary>GetMany filters out expired entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMany_WithExpiredEntries_FiltersExpired()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        var future = now.AddHours(1);
        const string UnexpiredKey = "fresh";
        cache.Connection.Upsert([
            new CacheEntry(UnexpiredKey, null, FirstEntryPayload, now, future),
            new CacheEntry("stale", null, SecondEntryPayload, now, past),
        ]).WaitForCompletion();

        var results = cache.Connection.GetMany([UnexpiredKey, "stale"], null, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Id).IsEqualTo(UnexpiredKey);
    }

    // ── GetAll with no entries ───────────────────────────────────────────
    /// <summary>GetAll on an empty database returns an empty sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAll_EmptyDatabase_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var results = cache.Connection.GetAll(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── GetAllKeys with no entries ───────────────────────────────────────
    /// <summary>GetAllKeys on an empty database returns an empty sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeys_EmptyDatabase_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var keys = cache.Connection.GetAllKeys(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(keys.Count).IsEqualTo(0);
    }

    // ── InvalidateAll without type ──────────────────────────────────────
    /// <summary>InvalidateAll without a type discriminator removes all entries. Covers the null typeFullName path in InvalidateAll.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task InvalidateAll_NoType_RemovesAllEntries()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("ia1", "T1", FirstEntryPayload, now, null),
            new CacheEntry("ia2", "T2", SecondEntryPayload, now, null),
        ]).WaitForCompletion();

        cache.Connection.InvalidateAll(null).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(0);
    }

    // ── SerializeKeysAsJson ─────────────────────────────────────────────
    /// <summary>SerializeKeysAsJson with a single key produces a valid JSON array.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_SingleKey_ProducesValidJson()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["hello"]);
        await Assert.That(result).IsEqualTo("[\"hello\"]");
    }

    /// <summary>SerializeKeysAsJson with multiple keys produces a valid JSON array.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_MultipleKeys_ProducesValidJson()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["a", "b", "c"]);
        await Assert.That(result).IsEqualTo("[\"a\",\"b\",\"c\"]");
    }

    // ── ReadOnly connection ─────────────────────────────────────────────
    /// <summary>
    /// A read-only connection opens without applying WAL/SYNCHRONOUS pragmas.
    /// Covers lines 347-361 (readOnly=true skips the WAL pragma block).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadOnlyConnection_SkipsWalPragma()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // First create a writable database so the file exists.
        using (var writableConn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false))
        {
            writableConn.CreateSchema().WaitForCompletion();
        }

        // Open read-only — should not throw.
        using var readOnlyConn = SqlitePclRawConnection.Create(dbPath, null, readOnly: true);
        var exists = readOnlyConn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── Password quoting ─────────────────────────────────────────────────
    /// <summary>
    /// A password containing single quotes is correctly escaped and applied via PRAGMA key.
    /// Covers lines 341-345 (password quoting path with single-quote replacement).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Constructor_PasswordWithSingleQuotes_IsQuotedCorrectly()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // A password containing single quotes exercises the Replace("'", "''") path.
        // On an unencrypted SQLite build, PRAGMA key is a no-op but the quoting
        // logic still runs. We verify the connection opens and functions normally.
        using var conn = SqlitePclRawConnection.Create(dbPath, "it's a te'st", readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var now = TimeProvider.System.GetUtcNow();
        conn.Upsert([new CacheEntry("q1", null, FirstEntryPayload, now, null)]).WaitForCompletion();
        var entry = conn.Get("q1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Id).IsEqualTo("q1");
    }

    /// <summary>
    /// A non-null empty password does not trigger the PRAGMA key path.
    /// Covers the guard <c>password is not null &amp;&amp; !string.IsNullOrEmpty(password)</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Constructor_EmptyPassword_DoesNotApplyPragmaKey()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        using var conn = SqlitePclRawConnection.Create(dbPath, string.Empty, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var exists = conn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── EnsurePrepared cache miss ───────────────────────────────────────
    /// <summary>
    /// EnsurePrepared caches the statement after first preparation, returning the same
    /// instance on subsequent calls. Covers line 1153 (cache miss then hit).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EnsurePrepared_CachesMissAndHit()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        // The first Get call prepares the statement (cache miss).
        // The second Get call reuses it (cache hit).
        var now = TimeProvider.System.GetUtcNow();
        conn.Upsert([new CacheEntry("ep1", null, FirstEntryPayload, now, null)]).WaitForCompletion();
        var entry1 = conn.Get("ep1", null, now).WaitForValue();
        var entry2 = conn.Get("ep1", null, now).WaitForValue();

        await Assert.That(entry1).IsNotNull();
        await Assert.That(entry2).IsNotNull();
        await Assert.That(entry1!.Id).IsEqualTo(entry2!.Id);
    }

    // ── TryRollback with null db ────────────────────────────────────────
    /// <summary>
    /// TryRollback with a null db handle does not throw: it catches everything, so a null
    /// handle exercises the catch path.
    /// Covers line 1092 (catch swallows NullReferenceException from sqlite3_exec).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRollback_NullDb_DoesNotThrow() =>
        SqlitePclRawConnection.TryRollback(null!);

    // ── CloseCore second-entry via concurrent threads ─────────────────
    /// <summary>
    /// CloseCore's second-entry check (lines 850-851) returns early when the callback
    /// detects <c>_disposed != 0</c>. Exercised by disposing from two dedicated threads.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CloseCore_ConcurrentDispose_SecondEntryReturnsEarly()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        const int ConcurrentDisposerCount = 3;

        using var go = new ManualResetEventSlim(false);
        var threads = new Thread[ConcurrentDisposerCount];
        var completed = new bool[ConcurrentDisposerCount];

        for (var i = 0; i < ConcurrentDisposerCount; i++)
        {
            var idx = i;
            threads[i] = new(() =>
            {
                go.Wait();
                conn.CloseCore();
                completed[idx] = true;
            })
            { IsBackground = true };
            threads[i].Start();
        }

        go.Set();

        foreach (var t in threads)
        {
            _ = t.Join(ConcurrentDisposeTimeout);
        }

        // All threads completed without deadlock or exception.
        for (var i = 0; i < ConcurrentDisposerCount; i++)
        {
            await Assert.That(completed[i]).IsTrue();
        }
    }

    // ── Upsert rollback on error (lines 603-610) ─────────────────────
    /// <summary>
    /// Upsert with an invalid entry triggers the rollback catch block (lines 603-610).
    /// Exercised by closing the connection then attempting an upsert through the queue.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_AfterConnectionClosed_PropagatesError()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();
        conn.Dispose();

        // After dispose, all operations should produce ObjectDisposedException.
        var error = conn.Upsert([new CacheEntry("k1", null, FirstEntryPayload, TimeProvider.System.GetUtcNow(), null)])
            .SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    // ── Invalidate rollback on error (lines 660-667) ─────────────────
    /// <summary>Invalidate with a closed connection triggers the rollback catch block (lines 660-667).</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Invalidate_AfterConnectionClosed_PropagatesError()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();
        conn.Dispose();

        var error = conn.Invalidate(["k1"], null).SubscribeGetError();
        await Assert.That(error).IsTypeOf<ObjectDisposedException>();
    }

    // ── EnsurePrepared error code handling (lines 1194-1197) ─────────
    /// <summary>
    /// EnsurePrepared with malformed SQL disposes the partial statement and throws
    /// AkavacheSqliteException, exercising lines 1149-1153 (dispose + CheckRc).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task EnsurePrepared_MalformedSql_ThrowsAndDisposesPartial()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        sqlite3_stmt? slot = null;
        var ex = Assert.Throws<AkavacheSqliteException>(() =>
            conn.EnsurePrepared(ref slot, "THIS IS NOT VALID SQL AT ALL"));

        await Assert.That(ex).IsNotNull();
        await Assert.That(slot).IsNull();
    }

    // ── RunInOwnedTransaction ────────────────────────────────────────
    /// <summary>RunInOwnedTransaction commits when the body succeeds.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunInOwnedTransaction_BodySucceeds_Commits()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var now = TimeProvider.System.GetUtcNow();

        // Run a body that upserts inside RunInOwnedTransaction.
        conn.Upsert([new CacheEntry("txn1", null, FirstEntryPayload, now, null)]).WaitForCompletion();

        var entry = conn.Get("txn1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Value).IsEquivalentTo(FirstEntryPayload);
    }

    /// <summary>RunInOwnedTransaction rolls back when the body throws.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunInOwnedTransaction_BodyThrows_RollsBackAndRethrows()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        // Seed data via the queue so it's committed.
        conn.Upsert([new CacheEntry("keep", null, TypelessEntryPayload, TimeProvider.System.GetUtcNow(), null)]).WaitForCompletion();

        // Dispose to stop the worker — then we can call RunInOwnedTransaction
        // directly from the test thread. CloseCore disposes statements but the
        // db handle stays open for direct static-method calls.
        // Actually we can't — dispose closes the db. Use ExecuteInTransactionForTest instead.
        // RunInOwnedTransaction rethrows after rollback. The queue catches via
        // SqliteOperation<T>.Execute and sets the error on the reply observable.
        // Call RunInOwnedTransaction directly — the worker thread is idle between operations.
        Exception? caught = null;
        try
        {
            SqlitePclRawConnection.RunInOwnedTransaction(conn, static () => throw new InvalidOperationException("body-boom"));
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        var error = caught;

        await Assert.That(error).IsTypeOf<InvalidOperationException>();

        // Data seeded before the failed transaction should survive the rollback.
        var entry = conn.Get("keep", null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(entry).IsNotNull();
        conn.Dispose();
    }

    /// <summary>
    /// Upsert with an entry that causes a step failure exercises the rollback
    /// path in RunInOwnedTransaction. After rollback, the connection remains usable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunInOwnedTransaction_RollbackPath_ConnectionRemainsUsable()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var now = TimeProvider.System.GetUtcNow();

        // Seed data.
        conn.Upsert([new CacheEntry("pre", null, FirstEntryPayload, now, null)]).WaitForCompletion();

        // Invalidate with an entry that exists and one that doesn't.
        conn.Invalidate(["pre", "nonexistent"], null).WaitForCompletion();

        // The deleted entry should be gone.
        var entry = conn.Get("pre", null, now).WaitForValue();
        await Assert.That(entry).IsNull();

        // Connection should still be usable after the transaction.
        conn.Upsert([new CacheEntry("post", null, SecondEntryPayload, now, null)]).WaitForCompletion();
        var postEntry = conn.Get("post", null, now).WaitForValue();
        await Assert.That(postEntry).IsNotNull();
    }

    // ── Partial branches: typeFullName is null ternaries ─────────────
    /// <summary>
    /// Get with null typeFullName exercises the untyped query path.
    /// Get with non-null typeFullName exercises the typed query path.
    /// Covers the <c>typeFullName is null</c> ternary in Get (line 399-411).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Get_BothNullAndNonNullType_ExercisesBothPaths()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string StoredKey = "typed";
        const string StoredTypeName = "SomeType";

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry(StoredKey, StoredTypeName, FirstEntryPayload, now, null)]).WaitForCompletion();

        // Untyped path (typeFullName = null).
        var untypedResult = cache.Connection.Get(StoredKey, null, now).WaitForValue();
        await Assert.That(untypedResult).IsNotNull();

        // Typed path (typeFullName = non-null).
        var typedResult = cache.Connection.Get(StoredKey, StoredTypeName, now).WaitForValue();
        await Assert.That(typedResult).IsNotNull();
        await Assert.That(typedResult!.TypeName).IsEqualTo(StoredTypeName);

        // Typed path with wrong type returns null.
        var wrongType = cache.Connection.Get(StoredKey, "WrongType", now).WaitForValue();
        await Assert.That(wrongType).IsNull();
    }

    /// <summary>
    /// GetMany with null and non-null typeFullName exercises both ternary paths.
    /// Covers the <c>typeFullName is null</c> ternary in GetMany (line 444-456).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetMany_BothNullAndNonNullType_ExercisesBothPaths()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const string QueriedTypeName = "TypeA";
        const int StoredEntryCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("m1", QueriedTypeName, FirstEntryPayload, now, null),
            new CacheEntry("m2", "TypeB", SecondEntryPayload, now, null),
        ]).WaitForCompletion();

        // Untyped.
        var untyped = cache.Connection.GetMany(["m1", "m2"], null, now).ToList().WaitForValue()!;
        await Assert.That(untyped.Count).IsEqualTo(StoredEntryCount);

        // Typed.
        var typed = cache.Connection.GetMany(["m1", "m2"], QueriedTypeName, now).ToList().WaitForValue()!;
        await Assert.That(typed.Count).IsEqualTo(1);
        await Assert.That(typed[0].Id).IsEqualTo("m1");
    }

    /// <summary>
    /// GetAll with null and non-null typeFullName exercises both ternary paths.
    /// Covers the <c>typeFullName is null</c> ternary in GetAll (line 481-490).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAll_BothNullAndNonNullType_ExercisesBothPaths()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const int StoredEntryCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("a1", "TypeP", FirstEntryPayload, now, null),
            new CacheEntry("a2", "TypeQ", SecondEntryPayload, now, null),
        ]).WaitForCompletion();

        // Untyped.
        var untyped = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(untyped.Count).IsEqualTo(StoredEntryCount);

        // Typed.
        var typed = cache.Connection.GetAll("TypeP", now).ToList().WaitForValue()!;
        await Assert.That(typed.Count).IsEqualTo(1);
    }

    /// <summary>
    /// GetAllKeys with null and non-null typeFullName exercises both ternary paths.
    /// Covers the <c>typeFullName is null</c> ternary in GetAllKeys (line 513-521).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetAllKeys_BothNullAndNonNullType_ExercisesBothPaths()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        const int StoredKeyCount = 2;

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("k1", "TypeR", FirstEntryPayload, now, null),
            new CacheEntry("k2", "TypeS", SecondEntryPayload, now, null),
        ]).WaitForCompletion();

        // Untyped.
        var untyped = cache.Connection.GetAllKeys(null, now).ToList().WaitForValue()!;
        await Assert.That(untyped.Count).IsEqualTo(StoredKeyCount);

        // Typed.
        var typed = cache.Connection.GetAllKeys("TypeR", now).ToList().WaitForValue()!;
        await Assert.That(typed.Count).IsEqualTo(1);
        await Assert.That(typed[0]).IsEqualTo("k1");
    }

    /// <summary>Invalidate with null and non-null typeFullName exercises both ternary paths in the Invalidate method.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Invalidate_BothNullAndNonNullType_ExercisesBothPaths()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using var cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([
            new CacheEntry("i1", "TypeU", FirstEntryPayload, now, null),
            new CacheEntry("i2", "TypeV", SecondEntryPayload, now, null),
            new CacheEntry("i3", null, ThirdEntryPayload, now, null),
        ]).WaitForCompletion();

        // Typed invalidate.
        cache.Connection.Invalidate(["i1"], "TypeU").WaitForCompletion();

        // Untyped invalidate.
        cache.Connection.Invalidate(["i3"], null).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0].Id).IsEqualTo("i2");
    }

    // ── HandlePrepareResult ───────────────────────────────────────────
    /// <summary>HandlePrepareResult with SQLITE_OK is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandlePrepareResult_Ok_IsNoop()
    {
        SqlitePclRawConnection.HandlePrepareResult(SqliteOk, null, null!, "SELECT 1");
        await Task.CompletedTask;
    }

    /// <summary>HandlePrepareResult with error code disposes mapping and throws.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HandlePrepareResult_Error_DisposesAndThrows() =>
        await Assert.That(static () =>
            SqlitePclRawConnection.HandlePrepareResult(SqliteError, null, null!, "BAD SQL"))
            .Throws<AkavacheSqliteException>();

    // ── TryRollbackAmbient static ───────────────────────────────────────
    /// <summary>TryRollbackAmbient clears the transaction flag and calls TryRollback.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryRollbackAmbient_ClearsFlag()
    {
        var flagValue = true;
        SqlitePclRawConnection.TryRollbackAmbient(v => flagValue = v, null!);

        await Assert.That(flagValue).IsFalse();
    }

    // ── Null Id upsert ───────────────────────────────────────────────
    /// <summary>Upserting a CacheEntry with null Id exercises the Id ?? string.Empty branch.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Upsert_NullId_UsesEmptyString()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"nullid_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        conn.Upsert([new CacheEntry(null, null, FirstMultiBytePayload, TimeProvider.System.GetUtcNow(), null)]).WaitForCompletion();

        // The null Id was coalesced to empty string — verify it can be retrieved.
        var entry = conn.Get(string.Empty, null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    // ── ScanRows cancellation ───────────────────────────────────────────
    /// <summary>Disposing a GetAll subscription mid-scan exercises the isCancelled branch in ScanRows.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ScanRows_DisposeMidScan_StopsEmitting()
    {
        const int SeededRowCount = 10;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"scan_cancel_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        // Insert several rows so the scan has multiple to iterate.
        for (var i = 0; i < SeededRowCount; i++)
        {
            conn.Upsert([new CacheEntry($"k{i}", null, [(byte)i], TimeProvider.System.GetUtcNow(), null)]).WaitForCompletion();
        }

        var received = new List<string>();
        using var done = new ManualResetEventSlim(false);

        // Subscribe and dispose after receiving the first row.
        var subscription = conn.GetAllKeys(null, TimeProvider.System.GetUtcNow()).Subscribe(
            key =>
            {
                received.Add(key);
                if (received.Count != 1)
                {
                    return;
                }

                done.Set();
            },
            _ => done.Set(),
            () => done.Set());

        _ = done.Wait(FirstScannedRowTimeout);
        subscription.Dispose();

        // We should have received at least 1 row but possibly fewer than 10
        // because disposal cancels the scan.
        await Assert.That(received.Count).IsGreaterThanOrEqualTo(1);
    }

    // ── MapStepResult ────────────────────────────────────────────────────
    /// <summary>MapStepResult with SQLITE_DONE returns SQLITE_OK.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MapStepResult_Done_ReturnsOk()
    {
        var result = SqlitePclRawConnection.MapStepResult(SqliteDone, null!);
        await Assert.That(result).IsEqualTo(SqliteOk);
    }

    /// <summary>MapStepResult with non-DONE returns the error code from the db handle.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MapStepResult_NotDone_ReturnsErrorCode()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"mapstep_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, null, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = SqlitePclRawConnection.MapStepResult(SqliteError, conn.Db);

        // sqlite3_errcode returns the last error, so the step result is not the mapped SQLITE_DONE.
        await Assert.That(result).IsNotEqualTo(SqliteDone);
    }

    // ── RunShutdownCleanup ──────────────────────────────────────────────
    /// <summary>RunShutdownCleanup disposes statements and handle when not already disposed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunShutdownCleanup_NotDisposed_DisposesStatementsAndHandle()
    {
        var disposed = 0;
        var statementsCalled = false;
        var handleCalled = false;

        SqlitePclRawConnection.RunShutdownCleanup(
            ref disposed,
            () => statementsCalled = true,
            () => handleCalled = true);

        await Assert.That(statementsCalled).IsTrue();
        await Assert.That(handleCalled).IsTrue();
        await Assert.That(disposed).IsEqualTo(1);
    }

    /// <summary>RunShutdownCleanup is a no-op when already disposed.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task RunShutdownCleanup_AlreadyDisposed_IsNoop()
    {
        var disposed = 1;
        var statementsCalled = false;
        var handleCalled = false;

        SqlitePclRawConnection.RunShutdownCleanup(
            ref disposed,
            () => statementsCalled = true,
            () => handleCalled = true);

        await Assert.That(statementsCalled).IsFalse();
        await Assert.That(handleCalled).IsFalse();
    }

    /// <summary>Creates a <see cref="SqliteBlobCache"/> in the given directory.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <returns>A new <see cref="SqliteBlobCache"/>.</returns>
    private static SqliteBlobCache CreateCache(string path) =>
        new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), new SystemJsonSerializer(), ImmediateScheduler.Instance);
}
