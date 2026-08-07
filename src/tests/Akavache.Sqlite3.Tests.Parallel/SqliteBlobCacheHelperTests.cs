// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>
/// Tests for the pure helpers on <see cref="SqliteBlobCache"/> — expiry conversion, key
/// materialization, cache-entry building, and the legacy-fallback read — exercised directly
/// rather than through a cache operation.
/// </summary>
[Category("Akavache")]
public class SqliteBlobCacheHelperTests
{
    /// <summary>Payload of the entry stored in the V11 table for an untyped fallback read.</summary>
    private static readonly byte[] UntypedV11Payload = [10, 20, 30];

    /// <summary>Payload of the entry stored in the V11 table for a typed fallback read.</summary>
    private static readonly byte[] TypedV11Payload = [4, 5, 6];

    /// <summary>Payload of the first entry handed to the cache-entry builder.</summary>
    private static readonly byte[] FirstEntryPayload = [1];

    /// <summary>Payload of the second entry handed to the cache-entry builder.</summary>
    private static readonly byte[] SecondEntryPayload = [2];

    /// <summary>Payload of the first entry produced by an iterator source.</summary>
    private static readonly byte[] FirstIteratedPayload = [10];

    /// <summary>Payload of the second entry produced by an iterator source.</summary>
    private static readonly byte[] SecondIteratedPayload = [20];

    /// <summary>Payload of the third entry produced by an iterator source.</summary>
    private static readonly byte[] ThirdIteratedPayload = [30];

    /// <summary>Tests SqliteBlobCache.ToExpiryValue returns the UTC DateTime component when given a non-null DateTimeOffset.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnUtcDateTimeForNonNullOffset()
    {
        const int OffsetHours = 5;
        DateTimeOffset offset = new(2025, 6, 15, 12, 30, 0, TimeSpan.FromHours(OffsetHours));

        var result = SqliteBlobCache.ToExpiryValue(offset);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(offset.UtcDateTime);
    }

    /// <summary>Tests SqliteBlobCache.ToExpiryValue returns <see langword="null"/> for a null offset.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ToExpiryValueShouldReturnNullForNullOffset()
    {
        var result = SqliteBlobCache.ToExpiryValue(null);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests SqliteBlobCache.TryGetLegacyValue delegates to
    /// IAkavacheConnection.TryReadLegacyV10Value(string, DateTimeOffset, Type?)
    /// on the supplied connection and returns whatever that returns.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryGetLegacyValueShouldReturnNullWhenLegacyRowMissing()
    {
        InMemoryAkavacheConnection connection = new();

        var result = SqliteBlobCache.TryGetLegacyValue(connection, "no-such-key", TimeProvider.System.GetUtcNow(), null).SubscribeGetValue();
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// returns the stored bytes when the V11 <c>CacheEntry</c> table contains
    /// the requested key (untyped overload).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldReturnV11ValueWhenPresent()
    {
        using var cache = CreateCache();
        cache.Insert("v11-key", UntypedV11Payload).SubscribeAndComplete();

        var bytes = cache.ReadValueWithLegacyFallback("v11-key", type: null).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo(UntypedV11Payload);
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// returns the stored bytes from the V11 table when the typed overload's
    /// <c>TypeName</c> filter matches the entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldReturnTypedV11ValueWhenPresent()
    {
        using var cache = CreateCache();
        cache.Insert("typed-key", TypedV11Payload, typeof(string)).SubscribeAndComplete();

        var bytes = cache.ReadValueWithLegacyFallback("typed-key", typeof(string)).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo(TypedV11Payload);
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// falls through to the legacy V10 store when the V11 table has no row, and
    /// returns those bytes instead.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldFallBackToLegacyV10Store()
    {
        InMemoryAkavacheConnection connection = new();
        connection.LegacyV10Store["legacy-only"] = "\t\t\t"u8.ToArray();
        using SqliteBlobCache cache = new(connection, new SystemJsonSerializer(), ImmediateSequencer.Instance);

        var bytes = cache.ReadValueWithLegacyFallback("legacy-only", type: null).SubscribeGetValue();

        await Assert.That(bytes).IsEquivalentTo("\t\t\t"u8.ToArray());
    }

    /// <summary>
    /// Tests that SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// throws KeyNotFoundException when neither the V11 nor the
    /// legacy V10 stores contain the requested key.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldThrowWhenKeyMissingInBothStores()
    {
        using var cache = CreateCache();

        var error = SqliteBlobCacheDirectTests.CaptureError(cache.ReadValueWithLegacyFallback("missing", type: null));
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
    }

    /// <summary>
    /// Tests that the KeyNotFoundException message produced by the
    /// typed branch of SqliteBlobCache.ReadValueWithLegacyFallbackAsync(string, Type?)
    /// includes the type's full name so callers can disambiguate identical keys
    /// stored under different types.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadValueWithLegacyFallbackAsyncShouldIncludeTypeNameInMissingMessage()
    {
        using var cache = CreateCache();

        var error = SqliteBlobCacheDirectTests.CaptureError(cache.ReadValueWithLegacyFallback("missing", typeof(string)));
        await Assert.That(error).IsTypeOf<KeyNotFoundException>();
        await Assert.That(error!.Message).Contains("System.String");
    }

    /// <summary>MaterializeKeys with an ICollection (HashSet) exercises the CopyTo path (lines 702-706).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithHashSet_UsesCopyToPath()
    {
        const int ExpectedKeyCount = 3;
        var keys = new HashSet<string> { "alpha", "beta", "gamma" };
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(result.Count).IsEqualTo(ExpectedKeyCount);
        await Assert.That(result).Contains("alpha");
        await Assert.That(result).Contains("beta");
        await Assert.That(result).Contains("gamma");
    }

    /// <summary>MaterializeKeys with an IReadOnlyList returns the same instance (lines 697-700).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithReadOnlyList_ReturnsSameInstance()
    {
        IReadOnlyList<string> keys = ["one", "two"];
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    /// <summary>MaterializeKeys with a plain iterator (yield return) exercises the fallback spread path (line 709).</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithIterator_CollectsViaSpread()
    {
        const int ExpectedKeyCount = 2;

        static IEnumerable<string> Generate()
        {
            yield return "x";
            yield return "y";
        }

        var result = SqliteBlobCache.MaterializeKeys(Generate());

        await Assert.That(result.Count).IsEqualTo(ExpectedKeyCount);
        await Assert.That(result[0]).IsEqualTo("x");
        await Assert.That(result[1]).IsEqualTo("y");
    }

    /// <summary>MaterializeKeys with an array (which implements IReadOnlyList) returns the same instance.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithArray_ReturnsSameInstance()
    {
        string[] keys = ["a", "b", "c"];
        var result = SqliteBlobCache.MaterializeKeys(keys);

        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    /// <summary>MaterializeKeys with a List (implements both IReadOnlyList and ICollection) takes the IReadOnlyList fast path.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task MaterializeKeys_WithList_TakesReadOnlyListPath()
    {
        var keys = new List<string> { "p", "q" };
        var result = SqliteBlobCache.MaterializeKeys(keys);

        // List<T> implements IReadOnlyList<T>, so it should be the same reference.
        await Assert.That(ReferenceEquals(result, keys)).IsTrue();
    }

    /// <summary>
    /// BuildCacheEntries with a Dictionary.ValueCollection (ICollection but not
    /// ICollection{KVP}) exercises the initial capacity heuristic.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task BuildCacheEntries_WithArrayInput_BuildsCorrectEntries()
    {
        const int ExpectedEntryCount = 2;
        KeyValuePair<string, byte[]>[] pairs =
        [
            new("k1", FirstEntryPayload),
            new("k2", SecondEntryPayload),
        ];

        var entries = SqliteBlobCache.BuildCacheEntries(
            pairs,
            "TestType",
            TimeProvider.System.GetUtcNow(),
            TimeProvider.System.GetUtcNow().AddHours(1));

        await Assert.That(entries.Count).IsEqualTo(ExpectedEntryCount);
        await Assert.That(entries[0].Id).IsEqualTo("k1");
        await Assert.That(entries[0].TypeName).IsEqualTo("TestType");
        await Assert.That(entries[1].Id).IsEqualTo("k2");
    }

    /// <summary>BuildCacheEntries with an iterator source exercises the non-ICollection initial capacity fallback.</summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task BuildCacheEntries_WithIterator_BuildsCorrectEntries()
    {
        const int ExpectedEntryCount = 3;

        static IEnumerable<KeyValuePair<string, byte[]>> Generate()
        {
            yield return new("i1", FirstIteratedPayload);
            yield return new("i2", SecondIteratedPayload);
            yield return new("i3", ThirdIteratedPayload);
        }

        var entries = SqliteBlobCache.BuildCacheEntries(
            Generate(),
            null,
            TimeProvider.System.GetUtcNow(),
            null);

        await Assert.That(entries.Count).IsEqualTo(ExpectedEntryCount);
        await Assert.That(entries[0].TypeName).IsNull();
        await Assert.That(entries[2].ExpiresAt).IsNull();
    }

    /// <summary>Creates a cache backed by an in-memory connection, so the helper tests never touch disk.</summary>
    /// <returns>A SqliteBlobCache instance backed by an in-memory connection.</returns>
    private static SqliteBlobCache CreateCache() =>
        new(new InMemoryAkavacheConnection(), new SystemJsonSerializer(), ImmediateSequencer.Instance);
}
