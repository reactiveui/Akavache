// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Akavache.EncryptedSqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests.Helpers;
using SQLitePCL;

namespace Akavache.Tests;

/// <summary>Tests for <see cref="SqlitePclRawConnection"/> (encrypted variant) covering static helpers and typed query paths.</summary>
[Category("Akavache")]
public class EncryptedSqlitePclRawConnectionTests
{
    /// <summary>The password used for the encrypted test database.</summary>
    private const string TestPassword = "test-password";

    /// <summary>Type discriminator a typed query is expected to match.</summary>
    private const string MatchingTypeName = "MyType";

    /// <summary>Type discriminator a typed query must not match.</summary>
    private const string NonMatchingTypeName = "OtherType";

    /// <summary>Type discriminator the entries under test were stored with.</summary>
    private const string StoredTypeName = "TypeA";

    /// <summary>Type discriminator used by the key-only typed queries.</summary>
    private const string KeyQueryTypeName = "FooType";

    /// <summary>Type discriminator whose entries are meant to be invalidated.</summary>
    private const string RemovableTypeName = "Remove";

    /// <summary>Key of the entry stored with a type discriminator.</summary>
    private const string TypedEntryKey = "typed1";

    /// <summary>Key of the entry whose expiry is still in the future.</summary>
    private const string UnexpiredKey = "fresh";

    /// <summary>Bind index of the legacy CacheElement "Key" column.</summary>
    private const int KeyParameterIndex = 1;

    /// <summary>Bind index of the legacy CacheElement "TypeName" column.</summary>
    private const int TypeNameParameterIndex = 2;

    /// <summary>Bind index of the legacy CacheElement "Value" column.</summary>
    private const int ValueParameterIndex = 3;

    /// <summary>Bind index of the legacy CacheElement "Expiration" column.</summary>
    private const int ExpirationParameterIndex = 4;

    /// <summary>Bind index of the legacy CacheElement "CreatedAt" column.</summary>
    private const int CreatedAtParameterIndex = 5;

    // ── AppendJsonString individual escape branches ────────────────────────
    /// <summary>A backslash character is escaped as <c>\\</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_Backslash_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\\b");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\\\b\"");
    }

    /// <summary>A backspace character is escaped as <c>\b</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_Backspace_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\bb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\bb\"");
    }

    /// <summary>A form-feed character is escaped as <c>\f</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_FormFeed_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\fb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\fb\"");
    }

    /// <summary>A newline character is escaped as <c>\n</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_Newline_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\nb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\nb\"");
    }

    /// <summary>A carriage-return character is escaped as <c>\r</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_CarriageReturn_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\rb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\rb\"");
    }

    /// <summary>A tab character is escaped as <c>\t</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_Tab_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "a\tb");
        await Assert.That(sb.ToString()).IsEqualTo("\"a\\tb\"");
    }

    /// <summary>A control character below 0x20 (not one of the named escapes) is encoded as <c>\uXXXX</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_ControlChar_IsUnicodeEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\x03");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\u0003\"");
    }

    /// <summary>A double-quote character is escaped as <c>\"</c>.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_DoubleQuote_IsEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "say \"hi\"");
        await Assert.That(sb.ToString()).IsEqualTo("\"say \\\"hi\\\"\"");
    }

    /// <summary>A plain printable string is emitted unchanged (only wrapped in quotes).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_PlainText_IsUnchanged()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "hello");
        await Assert.That(sb.ToString()).IsEqualTo("\"hello\"");
    }

    /// <summary>A string containing multiple different escape types is correctly escaped in sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task AppendJsonString_MixedEscapes_AllHandled()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\\\"\n\r\t\b\f\x01");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\\\\\\"\\n\\r\\t\\b\\f\\u0001\"");
    }

    // ── CheckRc ────────────────────────────────────────────────────────────
    /// <summary>CheckRc with a non-zero code and null db produces an exception whose message contains the operation name.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CheckRc_ErrorWithNullDb_MessageContainsOperation()
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
    internal async Task TableExists_KnownTable_ReturnsTrue()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var exists = cache.Connection.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    /// <summary>TableExists returns false for a table that does not exist.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TableExists_UnknownTable_ReturnsFalse()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var exists = cache.Connection.TableExists("NonExistentTable").WaitForValue();
        await Assert.That(exists).IsFalse();
    }

    // ── GetMany with typeFullName ─────────────────────────────────────────
    /// <summary>GetMany with a type discriminator returns only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetMany_WithTypeName_ReturnsOnlyMatchingType()
    {
        const int expectedMatches = 2;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] firstPayload = [1, 2, 3];
        byte[] secondPayload = [4, 5, 6];
        byte[] thirdPayload = [7, 8, 9];
        var entries = new[]
        {
            new CacheEntry("k1", MatchingTypeName, firstPayload, now, null),
            new CacheEntry("k2", NonMatchingTypeName, secondPayload, now, null),
            new CacheEntry("k3", MatchingTypeName, thirdPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var results = cache.Connection.GetMany(["k1", "k2", "k3"], MatchingTypeName, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(expectedMatches);
        await Assert.That(results.Select(static e => e.Id!).Order()).IsEquivalentTo(["k1", "k3"]);
    }

    // ── GetAll with typeFullName ──────────────────────────────────────────
    /// <summary>GetAll with a type discriminator returns only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetAll_WithTypeName_ReturnsOnlyMatchingType()
    {
        const int expectedMatches = 2;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] secondPayload = [2];
        byte[] thirdPayload = [3];
        var entries = new[]
        {
            new CacheEntry("a1", StoredTypeName, [1], now, null),
            new CacheEntry("b1", "TypeB", secondPayload, now, null),
            new CacheEntry("a2", StoredTypeName, thirdPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var results = cache.Connection.GetAll(StoredTypeName, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(expectedMatches);
        await Assert.That(results.Select(static e => e.Id!).Order()).IsEquivalentTo(["a1", "a2"]);
    }

    // ── GetAllKeys with typeFullName ──────────────────────────────────────
    /// <summary>GetAllKeys with a type discriminator returns only keys matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetAllKeys_WithTypeName_ReturnsOnlyMatchingType()
    {
        const int expectedMatches = 2;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] secondPayload = [2];
        byte[] thirdPayload = [3];
        var entries = new[]
        {
            new CacheEntry("x1", KeyQueryTypeName, [1], now, null),
            new CacheEntry("x2", "BarType", secondPayload, now, null),
            new CacheEntry("x3", KeyQueryTypeName, thirdPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        var keys = cache.Connection.GetAllKeys(KeyQueryTypeName, now).ToList().WaitForValue()!;
        await Assert.That(keys.Count).IsEqualTo(expectedMatches);
        await Assert.That(keys.Order()).IsEquivalentTo(["x1", "x3"]);
    }

    // ── Invalidate with typeFullName ──────────────────────────────────────
    /// <summary>Invalidate with a type discriminator deletes only entries matching that type.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Invalidate_WithTypeName_DeletesOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] secondPayload = [2];
        var entries = new[]
        {
            new CacheEntry("d1", "TypeX", [1], now, null),
            new CacheEntry("d2", "TypeY", secondPayload, now, null),
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
    internal async Task Invalidate_WithWrongTypeName_DoesNotDelete()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("e1", StoredTypeName, [1], now, null)]).WaitForCompletion();

        cache.Connection.Invalidate(["e1"], "TypeB").WaitForCompletion();

        var result = cache.Connection.Get("e1", null, now).WaitForValue();
        await Assert.That(result).IsNotNull();
    }

    // ── InvalidateAll with typeFullName ───────────────────────────────────
    /// <summary>InvalidateAll with a type discriminator removes only matching entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateAll_WithTypeName_RemovesOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] secondPayload = [2];
        byte[] thirdPayload = [3];
        cache.Connection.Upsert([
            new CacheEntry("f1", "Keep", [1], now, null),
            new CacheEntry("f2", RemovableTypeName, secondPayload, now, null),
            new CacheEntry("f3", RemovableTypeName, thirdPayload, now, null),
        ]).WaitForCompletion();

        var typedResults = cache.Connection.GetAll("Keep", now).ToList().WaitForValue()!;
        await Assert.That(typedResults.Count).IsEqualTo(1);

        cache.Connection.InvalidateAll(RemovableTypeName).WaitForCompletion();

        var remaining = cache.Connection.GetAll("Keep", now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
        await Assert.That(remaining[0].Id).IsEqualTo("f1");
    }

    // ── SetExpiry with typeFullName ───────────────────────────────────────
    /// <summary>SetExpiry with a type discriminator updates only the matching entry.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SetExpiry_WithTypeName_UpdatesOnlyMatchingEntry()
    {
        const int farFutureYears = 10;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var farFuture = now.AddYears(farFutureYears);
        byte[] secondPayload = [2];
        var entries = new[]
        {
            new CacheEntry("g1", "TypeM", [1], now, null),
            new CacheEntry("g2", "TypeN", secondPayload, now, null),
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
    internal async Task SetExpiry_NullExpiration_ClearsExpiry()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var expiry = now.AddHours(1);
        cache.Connection.Upsert([new CacheEntry("h1", null, [1], now, expiry)]).WaitForCompletion();

        cache.Connection.SetExpiry("h1", null, null).WaitForCompletion();

        var entry = cache.Connection.Get("h1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ExpiresAt).IsNull();
    }

    // ── VacuumExpired ────────────────────────────────────────────────────
    /// <summary>VacuumExpired removes expired entries and keeps unexpired ones.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task VacuumExpired_RemovesExpiredEntries()
    {
        const int expectedSurvivors = 2;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        var future = now.AddHours(1);
        byte[] secondPayload = [2];
        byte[] thirdPayload = [3];
        var entries = new[]
        {
            new CacheEntry("expired1", null, [1], now, past),
            new CacheEntry("valid1", null, secondPayload, now, future),
            new CacheEntry("noexpiry", null, thirdPayload, now, null),
        };
        cache.Connection.Upsert(entries).WaitForCompletion();

        cache.Connection.VacuumExpired(now).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(expectedSurvivors);
        await Assert.That(remaining.Select(static e => e.Id!).Order()).IsEquivalentTo(["noexpiry", "valid1"]);
    }

    // ── Upsert with null Value ───────────────────────────────────────────
    /// <summary>Upserting an entry with null Value stores the entry and ReadCacheEntry returns null Value.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Upsert_NullValue_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

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
    internal async Task Upsert_NullExpiresAt_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("noexp", "SomeType", [1], now, null)]).WaitForCompletion();

        var entry = cache.Connection.Get("noexp", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ExpiresAt).IsNull();
    }

    // ── Upsert with null TypeName ────────────────────────────────────────
    /// <summary>Upserting an entry with null TypeName stores the entry and retrieves it with null TypeName.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Upsert_NullTypeName_RoundTripsAsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] payload = [42];
        cache.Connection.Upsert([new CacheEntry("notype", null, payload, now, null)]).WaitForCompletion();

        var entry = cache.Connection.Get("notype", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.TypeName).IsNull();
    }

    // ── Upsert empty list ────────────────────────────────────────────────
    /// <summary>Upserting an empty list is a no-op and returns Unit without touching the database.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Upsert_EmptyList_IsNoop()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        cache.Connection.Upsert([]).WaitForCompletion();

        var all = cache.Connection.GetAll(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(all.Count).IsEqualTo(0);
    }

    // ── Invalidate empty list ────────────────────────────────────────────
    /// <summary>Invalidating an empty key list is a no-op.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Invalidate_EmptyList_IsNoop()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("keep", null, [1], now, null)]).WaitForCompletion();

        cache.Connection.Invalidate([], null).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(1);
    }

    // ── GetMany empty list ───────────────────────────────────────────────
    /// <summary>GetMany with an empty key list returns an empty sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetMany_EmptyList_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var results = cache.Connection.GetMany([], null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── Get with typeFullName ────────────────────────────────────────────
    /// <summary>Get with a type discriminator returns the entry only if the type matches.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Get_WithTypeName_ReturnsOnlyMatchingType()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry(TypedEntryKey, MatchingTypeName, [1], now, null)]).WaitForCompletion();

        var match = cache.Connection.Get(TypedEntryKey, MatchingTypeName, now).WaitForValue();
        await Assert.That(match).IsNotNull();
        await Assert.That(match!.Id).IsEqualTo(TypedEntryKey);

        var noMatch = cache.Connection.Get(TypedEntryKey, NonMatchingTypeName, now).WaitForValue();
        await Assert.That(noMatch).IsNull();
    }

    // ── Get returns null for missing key ─────────────────────────────────
    /// <summary>Get with a key that does not exist returns null.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Get_MissingKey_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var result = cache.Connection.Get("nonexistent", null, TimeProvider.System.GetUtcNow()).WaitForValue();
        await Assert.That(result).IsNull();
    }

    // ── Get returns null for expired entry ───────────────────────────────
    /// <summary>Get with a key whose entry has expired returns null.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Get_ExpiredEntry_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        cache.Connection.Upsert([new CacheEntry("expired", null, [1], now, past)]).WaitForCompletion();

        var result = cache.Connection.Get("expired", null, now).WaitForValue();
        await Assert.That(result).IsNull();
    }

    // ── Checkpoint modes ────────────────────────────────────────────────
    /// <summary>Checkpoint with Full mode executes without error.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Checkpoint_FullMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp1", null, [1], now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Full).WaitForCompletion();

        var entry = cache.Connection.Get("cp1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    /// <summary>Checkpoint with Truncate mode executes without error.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Checkpoint_TruncateMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp2", null, [1], now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Truncate).WaitForCompletion();

        var entry = cache.Connection.Get("cp2", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    /// <summary>Checkpoint with Passive (default) mode executes without error.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Checkpoint_PassiveMode_Succeeds()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("cp3", null, [1], now, null)]).WaitForCompletion();
        cache.Connection.Checkpoint(CheckpointMode.Passive).WaitForCompletion();

        var entry = cache.Connection.Get("cp3", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    // ── Compact ──────────────────────────────────────────────────────────
    /// <summary>Compact (VACUUM) executes without error and the database remains functional.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Compact_Succeeds_AndDatabaseRemainsUsable()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        cache.Connection.Upsert([new CacheEntry("c1", null, [1], now, null)]).WaitForCompletion();

        cache.Connection.Compact().WaitForCompletion();

        var entry = cache.Connection.Get("c1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
    }

    // ── Dispose idempotent ──────────────────────────────────────────────
    /// <summary>Disposing the connection twice does not throw.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Dispose_Twice_IsIdempotent()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        conn.Dispose();
        conn.Dispose();

        var fileExists = File.Exists(dbPath);
        await Assert.That(fileExists).IsTrue();
    }

    // ── TryReadLegacyV10Value on a non-v10 database ─────────────────────
    /// <summary>TryReadLegacyV10Value returns null when the database has no legacy CacheElement table.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_NoLegacyTable_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var result = cache.Connection.TryReadLegacyV10Value("somekey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>TryReadLegacyV10Value with a null type falls back to untyped search only.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_NullType_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var result = cache.Connection.TryReadLegacyV10Value("somekey", TimeProvider.System.GetUtcNow(), null).WaitForValue();
        await Assert.That(result).IsNull();
    }

    // ── TryRollback ─────────────────────────────────────────────────────
    /// <summary>TryRollback does not throw even when there is no active transaction.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryRollback_NoTransaction_DoesNotThrow()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        SqlitePclRawConnection.TryRollback(null!);

        var exists = conn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── CheckRc success codes ────────────────────────────────────────────
    /// <summary>CheckRc does not throw for SQLITE_OK (0).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CheckRc_SuccessCodes_DoNotThrow()
    {
        const int successCodeCount = 3;

        var codes = new[] { 0, 100, 101 };
        foreach (var code in codes)
        {
            SqlitePclRawConnection.CheckRc(code, db: null, "op");
        }

        await Assert.That(codes.Length).IsEqualTo(successCodeCount);
    }

    // ── CheckRc with non-null db ────────────────────────────────────────
    /// <summary>CheckRc with a non-null db and error code includes sqlite3_errmsg detail.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task CheckRc_ErrorWithNonNullDb_IncludesDetail()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);

        sqlite3_stmt? slot = null;
        var ex = Assert.Throws<AkavacheSqliteException>(() =>
            conn.EnsurePrepared(ref slot, "SELECT * FROM nonexistent_table_xyz"));
        await Assert.That(ex.Message).Contains("nonexistent_table_xyz");
    }

    // ── GetMany with expired entries ────────────────────────────────────
    /// <summary>GetMany filters out expired entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetMany_WithExpiredEntries_FiltersExpired()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        var future = now.AddHours(1);
        byte[] stalePayload = [2];
        cache.Connection.Upsert([
            new CacheEntry(UnexpiredKey, null, [1], now, future),
            new CacheEntry("stale", null, stalePayload, now, past),
        ]).WaitForCompletion();

        var results = cache.Connection.GetMany([UnexpiredKey, "stale"], null, now).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].Id).IsEqualTo(UnexpiredKey);
    }

    // ── GetAll with no entries ───────────────────────────────────────────
    /// <summary>GetAll on an empty database returns an empty sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetAll_EmptyDatabase_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var results = cache.Connection.GetAll(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(results.Count).IsEqualTo(0);
    }

    // ── GetAllKeys with no entries ───────────────────────────────────────
    /// <summary>GetAllKeys on an empty database returns an empty sequence.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task GetAllKeys_EmptyDatabase_ReturnsEmpty()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var keys = cache.Connection.GetAllKeys(null, TimeProvider.System.GetUtcNow()).ToList().WaitForValue()!;
        await Assert.That(keys.Count).IsEqualTo(0);
    }

    // ── InvalidateAll without type ──────────────────────────────────────
    /// <summary>InvalidateAll without a type discriminator removes all entries.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task InvalidateAll_NoType_RemovesAllEntries()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        using EncryptedSqliteBlobCache cache = CreateCache(path);

        var now = TimeProvider.System.GetUtcNow();
        byte[] secondPayload = [2];
        cache.Connection.Upsert([
            new CacheEntry("ia1", "T1", [1], now, null),
            new CacheEntry("ia2", "T2", secondPayload, now, null),
        ]).WaitForCompletion();

        cache.Connection.InvalidateAll(null).WaitForCompletion();

        var remaining = cache.Connection.GetAll(null, now).ToList().WaitForValue()!;
        await Assert.That(remaining.Count).IsEqualTo(0);
    }

    // ── SerializeKeysAsJson ─────────────────────────────────────────────
    /// <summary>SerializeKeysAsJson with a single key produces a valid JSON array.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SerializeKeysAsJson_SingleKey_ProducesValidJson()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["hello"]);
        await Assert.That(result).IsEqualTo("[\"hello\"]");
    }

    /// <summary>SerializeKeysAsJson with multiple keys produces a valid JSON array.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SerializeKeysAsJson_MultipleKeys_ProducesValidJson()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["a", "b", "c"]);
        await Assert.That(result).IsEqualTo("[\"a\",\"b\",\"c\"]");
    }

    // ── ReadOnly connection ─────────────────────────────────────────────
    /// <summary>A read-only connection opens without applying WAL/SYNCHRONOUS pragmas.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReadOnlyConnection_SkipsWalPragma()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // First create a writable database so the file exists.
        using (var writableConn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false))
        {
            writableConn.CreateSchema().WaitForCompletion();
        }

        // Open read-only — should not throw.
        using var readOnlyConn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: true);
        var exists = readOnlyConn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── Password quoting ─────────────────────────────────────────────────
    /// <summary>A password containing single quotes is correctly escaped and applied via PRAGMA key.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Constructor_PasswordWithSingleQuotes_IsQuotedCorrectly()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        // A password containing single quotes exercises the Replace("'", "''") path.
        using var conn = SqlitePclRawConnection.Create(dbPath, "it's a te'st", readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var now = TimeProvider.System.GetUtcNow();
        conn.Upsert([new CacheEntry("q1", null, [1], now, null)]).WaitForCompletion();
        var entry = conn.Get("q1", null, now).WaitForValue();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Id).IsEqualTo("q1");
    }

    /// <summary>A non-null empty password does not trigger the PRAGMA key path.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task Constructor_EmptyPassword_DoesNotApplyPragmaKey()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        using var conn = SqlitePclRawConnection.Create(dbPath, string.Empty, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var exists = conn.TableExists(nameof(CacheEntry)).WaitForValue();
        await Assert.That(exists).IsTrue();
    }

    // ── Legacy V10 table paths ──────────────────────────────────────────
    /// <summary>
    /// TryReadLegacyV10Value reads data from a manually created legacy CacheElement table
    /// using an assembly-qualified type name match.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_WithLegacyTable_ReturnsValueByAssemblyQualifiedName()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        byte[] legacyPayload = [42, 43, 44];

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "legacyKey", typeof(string).AssemblyQualifiedName!, legacyPayload, expiration: 0);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("legacyKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo([.. "*+,"u8]);
    }

    /// <summary>TryReadLegacyV10Value falls back to FullName match when AssemblyQualifiedName does not match.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_WithLegacyTable_FallsBackToFullName()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        byte[] legacyPayload = [10, 20];

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "fqnKey", typeof(string).FullName!, legacyPayload, expiration: 0);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("fqnKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(legacyPayload);
    }

    /// <summary>TryReadLegacyV10Value falls back to untyped search when type is null.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_WithLegacyTable_UntypedFallback()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        byte[] legacyPayload = [99];

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "untypedKey", typeName: null, legacyPayload, expiration: 0);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("untypedKey", TimeProvider.System.GetUtcNow(), type: null).WaitForValue();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEquivalentTo(legacyPayload);
    }

    /// <summary>TryReadLegacyV10Value returns null for an expired legacy row.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_ExpiredRow_ReturnsNull()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");

        CreateLegacyV10Table(dbPath);
        var pastTicks = TimeProvider.System.GetUtcNow().AddHours(-1).UtcTicks;
        InsertLegacyV10Row(dbPath, "expiredLegacy", typeName: null, [1], expiration: pastTicks);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("expiredLegacy", TimeProvider.System.GetUtcNow(), type: null).WaitForValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// TryReadLegacyV10Value with a typed query returns the value via untyped fallback
    /// when the legacy row's type does not match the requested type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryReadLegacyV10Value_TypeMismatch_FallsBackToUntyped()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        byte[] legacyPayload = [1, 2];

        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "typedKey", typeof(int).FullName!, legacyPayload, expiration: 0);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var result = conn.TryReadLegacyV10Value("typedKey", TimeProvider.System.GetUtcNow(), typeof(string)).WaitForValue();
        await Assert.That(result).IsNotNull();
    }

    // ── ReadAllLegacyV10Rows ────────────────────────────────────────────
    /// <summary>ReadAllLegacyV10Rows reads all rows from a manually created legacy CacheElement table.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task ReadAllLegacyV10Rows_ReturnsAllLegacyRows()
    {
        const int expectedRowCount = 3;
        const long unexpiredOffsetTicks = 1000;

        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        byte[] firstRowPayload = [1, 2];
        byte[] secondRowPayload = [3, 4, 5];

        var nowTicks = TimeProvider.System.GetUtcNow().UtcTicks;
        CreateLegacyV10Table(dbPath);
        InsertLegacyV10Row(dbPath, "row1", MatchingTypeName, firstRowPayload, expiration: 0, createdAt: nowTicks);
        InsertLegacyV10Row(dbPath, "row2", typeName: null, secondRowPayload, expiration: nowTicks + unexpiredOffsetTicks, createdAt: nowTicks);
        InsertLegacyV10Row(dbPath, "row3", NonMatchingTypeName, value: null, expiration: 0, createdAt: nowTicks);

        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var rows = conn.ReadAllLegacyV10Rows().ToList().WaitForValue()!;
        await Assert.That(rows.Count).IsEqualTo(expectedRowCount);

        var row1 = rows.First(static r => r.Key == "row1");
        await Assert.That(row1.TypeName).IsEqualTo(MatchingTypeName);
        await Assert.That(row1.Value).IsEquivalentTo(firstRowPayload);

        var row2 = rows.First(static r => r.Key == "row2");
        await Assert.That(row2.TypeName).IsNull();
        await Assert.That(row2.Value).IsEquivalentTo(secondRowPayload);

        var row3 = rows.First(static r => r.Key == "row3");
        await Assert.That(row3.TypeName).IsEqualTo(NonMatchingTypeName);
        await Assert.That(row3.Value).IsNull();
    }

    // ── EnsurePrepared cache miss ───────────────────────────────────────
    /// <summary>EnsurePrepared caches the statement after first preparation, returning the same instance on subsequent calls.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task EnsurePrepared_CachesMissAndHit()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var dbPath = Path.Combine(path, $"test_{Guid.NewGuid():N}.db");
        using var conn = SqlitePclRawConnection.Create(dbPath, TestPassword, readOnly: false);
        conn.CreateSchema().WaitForCompletion();

        var now = TimeProvider.System.GetUtcNow();
        conn.Upsert([new CacheEntry("ep1", null, [1], now, null)]).WaitForCompletion();
        var entry1 = conn.Get("ep1", null, now).WaitForValue();
        var entry2 = conn.Get("ep1", null, now).WaitForValue();

        await Assert.That(entry1).IsNotNull();
        await Assert.That(entry2).IsNotNull();
        await Assert.That(entry1!.Id).IsEqualTo(entry2!.Id);
    }

    // ── TryRollback with null db ────────────────────────────────────────
    /// <summary>TryRollback with a null db handle does not throw: it catches all exceptions, so passing null exercises the catch path.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task TryRollback_NullDb_DoesNotThrow() =>
        SqlitePclRawConnection.TryRollback(null!);

    /// <summary>Creates a cache with a unique DB file and ImmediateScheduler.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <returns>A new <see cref="EncryptedSqliteBlobCache"/>.</returns>
    private static EncryptedSqliteBlobCache CreateCache(string path) =>
        new(Path.Combine(path, $"test_{Guid.NewGuid():N}.db"), TestPassword, new SystemJsonSerializer(), ImmediateScheduler.Instance);

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
            _ = raw.sqlite3_exec(db, $"PRAGMA key = '{TestPassword}'");
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
            _ = raw.sqlite3_exec(db, $"PRAGMA key = '{TestPassword}'");
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
