// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests how a V10 row becomes a V11 entry: the tick values V10 stored, which of them mean "no
/// date at all", the type name the payload is re-serialized against, and the diagnostics the
/// migration emits when a row cannot be converted.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class V10RowConversionTests
{
    /// <summary>The type discriminator the converted rows carry.</summary>
    private const string RowTypeName = "System.String";

    /// <summary>The type name the re-serialization diagnostics are checked against.</summary>
    private const string DiagnosticTypeName = "Some.Type";

    /// <summary>A tick count inside the range the converter accepts.</summary>
    private static readonly long ValidTicks = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    /// <summary>A tick count below the year-2000 floor V10 rows are checked against.</summary>
    private static readonly long BelowFloorTicks = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    /// <summary>The payload carried through conversion.</summary>
    private static readonly byte[] RowPayload = [7, 8, 9];

    /// <summary>A valid tick count becomes the matching UTC instant.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TicksToDateTimeOffsetShouldConvertAValidTickCount()
    {
        var result = V10MigrationService.TicksToDateTimeOffset(ValidTicks);

        await Assert.That(result.UtcTicks).IsEqualTo(ValidTicks);
    }

    /// <summary>Tick counts V10 used to mean "unset" fall back to the current time.</summary>
    /// <param name="ticks">The unusable tick count.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(0L)]
    [Arguments(-1L)]
    public async Task TicksToDateTimeOffsetShouldFallBackToNowForAnUnsetTickCount(long ticks)
    {
        var before = TimeProvider.System.GetUtcNow();

        var result = V10MigrationService.TicksToDateTimeOffset(ticks);

        await Assert.That(result).IsGreaterThanOrEqualTo(before);
    }

    /// <summary>A tick count below the year-2000 floor is treated as unset.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TicksToDateTimeOffsetShouldFallBackToNowBelowTheFloor()
    {
        var before = TimeProvider.System.GetUtcNow();

        var result = V10MigrationService.TicksToDateTimeOffset(BelowFloorTicks);

        await Assert.That(result).IsGreaterThanOrEqualTo(before);
    }

    /// <summary>A tick count past the calendar's range is treated as unset rather than throwing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TicksToDateTimeOffsetShouldFallBackToNowForAnOutOfRangeTickCount()
    {
        var before = TimeProvider.System.GetUtcNow();

        var result = V10MigrationService.TicksToDateTimeOffset(long.MaxValue);

        await Assert.That(result).IsGreaterThanOrEqualTo(before);
    }

    /// <summary>A valid expiration tick count becomes the matching instant.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertExpirationShouldConvertAValidTickCount()
    {
        var result = V10MigrationService.ConvertExpiration(ValidTicks);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.UtcTicks).IsEqualTo(ValidTicks);
    }

    /// <summary>Tick counts V10 used to mean "never expires" become no expiry at all.</summary>
    /// <param name="ticks">The sentinel tick count.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(0L)]
    [Arguments(-1L)]
    public async Task ConvertExpirationShouldReturnNullForASentinelTickCount(long ticks) =>
        await Assert.That(V10MigrationService.ConvertExpiration(ticks)).IsNull();

    /// <summary>An expiration below the year-2000 floor means the row never expires.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertExpirationShouldReturnNullBelowTheFloor() =>
        await Assert.That(V10MigrationService.ConvertExpiration(BelowFloorTicks)).IsNull();

    /// <summary>An expiration past the calendar's range means the row never expires.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertExpirationShouldReturnNullForAnOutOfRangeTickCount() =>
        await Assert.That(V10MigrationService.ConvertExpiration(long.MaxValue)).IsNull();

    /// <summary>A converted entry keeps its key, type name, payload and dates.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertEntryShouldCarryEveryFieldAcross()
    {
        V10CacheElement element = new("the-key", RowTypeName, RowPayload, ValidTicks, ValidTicks);

        var result = V10MigrationService.ConvertEntry(element, new SystemJsonSerializer(), new(ReserializeToCurrentFormat: false));

        using (Assert.Multiple())
        {
            await Assert.That(result.Id).IsEqualTo("the-key");
            await Assert.That(result.TypeName).IsEqualTo(RowTypeName);
            await Assert.That(result.Value).IsEquivalentTo(RowPayload);
            await Assert.That(result.CreatedAt.UtcTicks).IsEqualTo(ValidTicks);
            await Assert.That(result.ExpiresAt!.Value.UtcTicks).IsEqualTo(ValidTicks);
        }
    }

    /// <summary>An entry with no payload is carried across without attempting re-serialization.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertEntryShouldLeaveAnEmptyPayloadAlone()
    {
        V10CacheElement element = new("empty", RowTypeName, [], ValidTicks, ValidTicks);

        var result = V10MigrationService.ConvertEntry(element, new SystemJsonSerializer(), new());

        await Assert.That(result.Value).IsEmpty();
    }

    /// <summary>An entry with a sentinel expiration becomes one that never expires.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertEntryShouldDropASentinelExpiration()
    {
        V10CacheElement element = new("no-expiry", RowTypeName, RowPayload, 0, ValidTicks);

        var result = V10MigrationService.ConvertEntry(element, new SystemJsonSerializer(), new(ReserializeToCurrentFormat: false));

        await Assert.That(result.ExpiresAt).IsNull();
    }

    /// <summary>
    /// With re-serialization on, a payload that is not BSON is carried across untouched rather
    /// than being reported as a failure.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertEntryShouldKeepANonBsonPayloadWhenReserializing()
    {
        V10CacheElement element = new("plain", RowTypeName, RowPayload, ValidTicks, ValidTicks);
        List<string> log = [];

        var result = V10MigrationService.ConvertEntry(element, new SystemJsonSerializer(), new(Logger: log.Add));

        using (Assert.Multiple())
        {
            await Assert.That(result.Value).IsEquivalentTo(RowPayload);
            await Assert.That(log).IsEmpty();
        }
    }

    /// <summary>An assembly-qualified name resolves directly.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveTypeShouldResolveAnAssemblyQualifiedName()
    {
        var result = V10MigrationService.ResolveType(typeof(string).AssemblyQualifiedName!);

        await Assert.That(result).IsEqualTo(typeof(string));
    }

    /// <summary>A bare full name is found by scanning the loaded assemblies.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveTypeShouldFindAFullNameByScanningLoadedAssemblies()
    {
        var result = V10MigrationService.ResolveType(typeof(V10RowConversionTests).FullName!);

        await Assert.That(result).IsEqualTo(typeof(V10RowConversionTests));
    }

    /// <summary>A name no loaded assembly carries resolves to nothing.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveTypeShouldReturnNullForAnUnknownName() =>
        await Assert.That(V10MigrationService.ResolveType("Akavache.Tests.NoSuchTypeExistsAnywhere")).IsNull();

    /// <summary>
    /// A payload that no serializer can rewrite keeps its original bytes and reports why, rather
    /// than failing the whole migration for one entry.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReserializeShouldKeepTheOriginalBytesWhenSerializationThrows()
    {
        // An empty registry leaves the universal serializer no fallback to try, so the write-only
        // failure is the last word.
        SerializerRegistryFixture.Reset();
        var bsonPayload = new NewtonsoftBsonSerializer().Serialize("a value");
        List<string> log = [];

        var result = V10MigrationService.TryReserialize(
            bsonPayload,
            typeof(string).AssemblyQualifiedName,
            new ReadOnlySerializer(),
            new(Logger: log.Add));

        using (Assert.Multiple())
        {
            await Assert.That(result).IsEquivalentTo(bsonPayload);
            await Assert.That(log).HasSingleItem();
            await Assert.That(log[0]).Contains("Re-serialization failed");
        }
    }

    /// <summary>A cache that cannot be read is treated as not yet migrated rather than faulting.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task IsMigrationCompleteShouldBeFalseWhenTheCacheCannotBeRead()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var cache = new SqliteBlobCache(Path.Combine(path, "closed.db"), new SystemJsonSerializer(), ImmediateSequencer.Instance);
        cache.Dispose();

        var result = V10MigrationService.IsMigrationComplete(cache).WaitForValue();

        await Assert.That(result).IsFalse();
    }

    /// <summary>A conversion failure names the key it happened on.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LogConvertEntryFailureShouldNameTheKey()
    {
        List<string> log = [];

        V10MigrationService.LogConvertEntryFailure(new(Logger: log.Add), "broken-key", new InvalidOperationException("bang"));

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("broken-key");
    }

    /// <summary>A re-serialization failure names the type it happened on.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LogReserializationFailureShouldNameTheType()
    {
        List<string> log = [];

        V10MigrationService.LogReserializationFailure(new(Logger: log.Add), DiagnosticTypeName, new InvalidOperationException("bang"));

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains(DiagnosticTypeName);
    }

    /// <summary>With no logger configured the diagnostics are simply dropped.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task LoggingShouldBeSilentWithoutALogger() =>
        await Assert.That(static () =>
        {
            V10MigrationService.LogConvertEntryFailure(new(), "key", new InvalidOperationException("bang"));
            V10MigrationService.LogReserializationFailure(new(), DiagnosticTypeName, new InvalidOperationException("bang"));
        }).ThrowsNothing();

    /// <summary>
    /// Reads a V10 BSON payload but cannot write one back, which is what drives re-serialization
    /// into its failure path — a serializer that also fails to read never gets that far, because
    /// the universal fallback simply yields no value to re-serialize.
    /// </summary>
    private sealed class ReadOnlySerializer : ISerializer
    {
        /// <summary>The reader the payloads in these tests were written with.</summary>
        private readonly NewtonsoftBsonSerializer _reader = new();

        /// <inheritdoc/>
        public DateTimeKind? ForcedDateTimeKind
        {
            get => _reader.ForcedDateTimeKind;
            set => _reader.ForcedDateTimeKind = value;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Deserialize<T>(byte[] bytes) => _reader.Deserialize<T>(bytes);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Serialize<T>(T item) =>
            throw new InvalidOperationException("This serializer cannot write.");
    }
}
