// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

namespace Akavache.Tests;

/// <summary>
/// Tests for internal helper methods exposed for testability in the Sqlite3 package.
/// </summary>
[Category("Akavache")]
public class InternalHelperTests
{
    // ── SerializeKeysAsJson ────────────────────────────────────────────────

    /// <summary>
    /// An empty key list produces an empty JSON array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_EmptyList_ReturnsEmptyArray()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson([]);
        await Assert.That(result).IsEqualTo("[]");
    }

    /// <summary>
    /// A single key produces a single-element JSON array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_SingleKey_ReturnsSingleElementArray()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["hello"]);
        await Assert.That(result).IsEqualTo("[\"hello\"]");
    }

    /// <summary>
    /// Multiple keys produce a comma-separated JSON array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_MultipleKeys_ProducesValidJson()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["a", "b", "c"]);
        await Assert.That(result).IsEqualTo("[\"a\",\"b\",\"c\"]");
    }

    /// <summary>
    /// Keys containing JSON-special characters are correctly escaped.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SerializeKeysAsJson_SpecialCharacters_AreEscaped()
    {
        var result = SqlitePclRawConnection.SerializeKeysAsJson(["he\"llo", "back\\slash"]);
        await Assert.That(result).IsEqualTo("[\"he\\\"llo\",\"back\\\\slash\"]");
    }

    // ── AppendJsonString ───────────────────────────────────────────────────

    /// <summary>
    /// Control characters below 0x20 are escaped as \uXXXX.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_ControlCharacters_AreUnicodeEscaped()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\x01\x1F");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\u0001\\u001F\"");
    }

    /// <summary>
    /// Standard escape sequences (\n, \r, \t, etc.) are correctly emitted.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task AppendJsonString_StandardEscapes_AreCorrect()
    {
        var sb = new StringBuilder();
        SqlitePclRawConnection.AppendJsonString(sb, "\n\r\t\b\f");
        await Assert.That(sb.ToString()).IsEqualTo("\"\\n\\r\\t\\b\\f\"");
    }

    // ── CheckRc ────────────────────────────────────────────────────────────

    /// <summary>
    /// SQLITE_OK does not throw.
    /// </summary>
    [Test]
    public void CheckRc_SqliteOk_DoesNotThrow()
    {
        // 0 = SQLITE_OK
        SqlitePclRawConnection.CheckRc(0, db: null, "test");
    }

    /// <summary>
    /// A non-success result code throws <see cref="AkavacheSqliteException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CheckRc_ErrorCode_ThrowsAkavacheSqliteException()
    {
        // 1 = SQLITE_ERROR
        await Assert.ThrowsAsync<AkavacheSqliteException>(() =>
        {
            SqlitePclRawConnection.CheckRc(1, db: null, "test-op");
            return Task.CompletedTask;
        });
    }

    // ── MaterializeKeys ────────────────────────────────────────────────────

    /// <summary>
    /// An input that is already an <see cref="IReadOnlyList{T}"/> is returned directly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MaterializeKeys_ReadOnlyList_ReturnsSameInstance()
    {
        string[] keys = ["a", "b"];
        var result = SqliteBlobCache.MaterializeKeys(keys);
        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    /// <summary>
    /// An <see cref="ICollection{T}"/> is materialized into an array.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MaterializeKeys_Collection_MaterializesToArray()
    {
        var keys = new HashSet<string> { "x", "y", "z" };
        var result = SqliteBlobCache.MaterializeKeys(keys);
        await Assert.That(result.Count).IsEqualTo(3);
    }

    /// <summary>
    /// A bare <see cref="IEnumerable{T}"/> (not a list or collection) is materialized.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MaterializeKeys_BareEnumerable_Materializes()
    {
        static IEnumerable<string> Generate()
        {
            yield return "one";
            yield return "two";
        }

        var result = SqliteBlobCache.MaterializeKeys(Generate());
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo("one");
        await Assert.That(result[1]).IsEqualTo("two");
    }

    // ── BuildCacheEntries ──────────────────────────────────────────────────

    /// <summary>
    /// BuildCacheEntries stamps the shared type name, creation time, and expiry on each entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildCacheEntries_StampsSharedFieldsOnEveryEntry()
    {
        var created = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expiry = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var kvps = new[]
        {
            new KeyValuePair<string, byte[]>("k1", [1]),
            new KeyValuePair<string, byte[]>("k2", [2]),
        };

        var entries = SqliteBlobCache.BuildCacheEntries(kvps, "MyType", created, expiry);

        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Id).IsEqualTo("k1");
        await Assert.That(entries[0].TypeName).IsEqualTo("MyType");
        await Assert.That(entries[0].CreatedAt).IsEqualTo(created);
        await Assert.That(entries[0].ExpiresAt).IsEqualTo(expiry);
        await Assert.That(entries[1].Id).IsEqualTo("k2");
    }

    /// <summary>
    /// An empty input produces an empty list (no entries).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildCacheEntries_EmptyInput_ReturnsEmptyList()
    {
        var entries = SqliteBlobCache.BuildCacheEntries(
            [],
            typeName: null,
            DateTimeOffset.UtcNow,
            expiry: null);

        await Assert.That(entries.Count).IsEqualTo(0);
    }

    // ── ReadWithLegacyFallbackObservable.CreateNotFound ─────────────────────

    /// <summary>
    /// Untyped not-found includes the key in the message.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNotFound_Untyped_IncludesKey()
    {
        var ex = ReadWithLegacyFallbackObservable.CreateNotFound("mykey", type: null);
        await Assert.That(ex.Message).Contains("mykey");
    }

    /// <summary>
    /// Typed not-found includes both the key and type name in the message.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateNotFound_Typed_IncludesKeyAndType()
    {
        var ex = ReadWithLegacyFallbackObservable.CreateNotFound("mykey", typeof(string));
        await Assert.That(ex.Message).Contains("mykey");
        await Assert.That(ex.Message).Contains("System.String");
    }

    // ── SqliteReplyObservable.ReplayTo ──────────────────────────────────────

    /// <summary>
    /// ReplayTo with a success state delivers OnNext+OnCompleted.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReplayTo_Success_DeliversOnNextThenOnCompleted()
    {
        var received = false;
        var completed = false;
        var observer = System.Reactive.Observer.Create<int>(
            v => received = v == 42,
            _ => { },
            () => completed = true);

        // StateSuccess = 1
        SqliteReplyObservable<int>.ReplayTo(observer, 1, 42, error: null);

        await Assert.That(received).IsTrue();
        await Assert.That(completed).IsTrue();
    }

    /// <summary>
    /// ReplayTo with an error state delivers OnError.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReplayTo_Error_DeliversOnError()
    {
        Exception? caught = null;
        var observer = System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { });

        var expected = new InvalidOperationException("boom");

        // StateError = 2
        SqliteReplyObservable<int>.ReplayTo(observer, 2, 0, expected);

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("boom");
    }

    // ── SqliteOperation ────────────────────────────────────────────────────

    /// <summary>
    /// An operation created with coalescable=true reports <see cref="ISqliteOperation.IsCoalescable"/> as true.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteOperation_CoalescableTrue_ReportsIsCoalescable()
    {
        var reply = new SqliteReplyObservable<int>();
        var op = new SqliteOperation<int>(_ => 1, reply, coalescable: true);
        await Assert.That(op.IsCoalescable).IsTrue();
    }

    /// <summary>
    /// An operation created with coalescable=false reports <see cref="ISqliteOperation.IsCoalescable"/> as false.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteOperation_CoalescableFalse_ReportsNotCoalescable()
    {
        var reply = new SqliteReplyObservable<int>();
        var op = new SqliteOperation<int>(_ => 1, reply, coalescable: false);
        await Assert.That(op.IsCoalescable).IsFalse();
    }

    /// <summary>
    /// <see cref="SqliteRowStreamOperation{T}"/> is never coalescable (reads).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteRowStreamOperation_IsNeverCoalescable()
    {
        var stream = new SqliteRowObservable<int>();
        var op = new SqliteRowStreamOperation<int>((_, _, _) => { }, stream);
        await Assert.That(op.IsCoalescable).IsFalse();
    }

    /// <summary>
    /// <see cref="SqliteShutdownOperation"/> is never coalescable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteShutdownOperation_IsNeverCoalescable()
    {
        var op = new SqliteShutdownOperation(_ => { });
        await Assert.That(op.IsCoalescable).IsFalse();
    }

    // ── SqliteOperation.Fail ───────────────────────────────────────────────

    /// <summary>
    /// Calling <see cref="SqliteOperation{T}.Fail"/> delivers the error through the reply observable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SqliteOperation_Fail_DeliversErrorToReply()
    {
        var reply = new SqliteReplyObservable<int>();
        var op = new SqliteOperation<int>(_ => 1, reply, coalescable: false);

        Exception? caught = null;
        reply.Subscribe(System.Reactive.Observer.Create<int>(
            _ => { },
            ex => caught = ex,
            () => { }));

        op.Fail(new InvalidOperationException("test-fail"));

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).IsEqualTo("test-fail");
    }
}
