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
/// Tests <see cref="V10MigrationService.TryReserialize"/>, which rewrites a V10 BSON payload into
/// the current serializer's format. Every branch of that method falls back to the original bytes, so
/// these tests pin the difference between "deliberately kept the bytes" and "the rewrite failed and
/// kept them" — the rewrite runs through reflection over two overloaded generic methods, which is
/// exactly the kind of lookup that fails silently into the fallback.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class V10PayloadReserializationTests
{
    /// <summary>The head count carried through both serialization formats.</summary>
    private const int ExpectedHeads = 2;

    /// <summary>The name carried through both serialization formats.</summary>
    private const string ExpectedName = "Zaphod";

    /// <summary>
    /// A V10 BSON payload for a resolvable type is rewritten into the target serializer's format.
    /// Asserts the bytes actually changed, not merely that the call returned something.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReserializeShouldRewriteBsonPayloadIntoTheTargetFormat()
    {
        // Registered inside the test body: the assembly's executor resets the registry between the
        // hooks and the test, so a [Before(Test)] registration would be wiped before it is read.
        SerializerRegistryFixture.RegisterAll();

        var v10Payload = CreateV10Payload();
        SystemJsonSerializer target = new();
        List<string> log = [];

        var result = V10MigrationService.TryReserialize(
            v10Payload,
            typeof(ReserializationSubject).AssemblyQualifiedName,
            target,
            new(Logger: log.Add));

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEquivalentTo(v10Payload);
        await Assert.That(log).IsEmpty();

        var roundTripped = target.Deserialize<ReserializationSubject>(result!);
        await Assert.That(roundTripped).IsNotNull();
        await Assert.That(roundTripped!.Name).IsEqualTo(ExpectedName);
        await Assert.That(roundTripped.Heads).IsEqualTo(ExpectedHeads);
    }

    /// <summary>A payload whose recorded type cannot be resolved keeps its original bytes and says why.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReserializeShouldKeepOriginalBytesWhenTheTypeIsUnresolvable()
    {
        var v10Payload = CreateV10Payload();
        List<string> log = [];

        var result = V10MigrationService.TryReserialize(
            v10Payload,
            "Akavache.Tests.NoSuchTypeExists, Akavache.Tests.NoSuchAssembly",
            new SystemJsonSerializer(),
            new(Logger: log.Add));

        await Assert.That(result).IsEquivalentTo(v10Payload);
        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("Cannot resolve type");
    }

    /// <summary>A payload that is not BSON is left untouched without consulting the type name.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task TryReserializeShouldKeepOriginalBytesWhenThePayloadIsNotBson()
    {
        var plain = "not bson at all"u8.ToArray();
        List<string> log = [];

        var result = V10MigrationService.TryReserialize(
            plain,
            typeof(ReserializationSubject).AssemblyQualifiedName,
            new SystemJsonSerializer(),
            new(Logger: log.Add));

        await Assert.That(result).IsEquivalentTo(plain);
        await Assert.That(log).IsEmpty();
    }

    /// <summary>A BSON payload with no usable recorded type name is left untouched.</summary>
    /// <param name="typeName">The absent or blank type name recorded with the V10 entry.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task TryReserializeShouldKeepOriginalBytesWhenTheTypeNameIsAbsent(string? typeName)
    {
        var v10Payload = CreateV10Payload();
        List<string> log = [];

        var result = V10MigrationService.TryReserialize(
            v10Payload,
            typeName,
            new SystemJsonSerializer(),
            new(Logger: log.Add));

        await Assert.That(result).IsEquivalentTo(v10Payload);
        await Assert.That(log).IsEmpty();
    }

    /// <summary>The payload a V10 database would hold, written by the BSON serializer V10 shipped with.</summary>
    /// <returns>BSON bytes for <see cref="ReserializationSubject"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] CreateV10Payload() =>
        new NewtonsoftBsonSerializer().Serialize(CreateSubject());

    /// <summary>Builds the value both serializers round-trip in these tests.</summary>
    /// <returns>A populated subject.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReserializationSubject CreateSubject() =>
        new() { Name = ExpectedName, Heads = ExpectedHeads };

    /// <summary>The value re-serialized by these tests.</summary>
    [System.Diagnostics.DebuggerDisplay("{Name} ({Heads})")]
    public class ReserializationSubject
    {
        /// <summary>Gets or sets the name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the head count.</summary>
        public int Heads { get; set; }
    }
}
