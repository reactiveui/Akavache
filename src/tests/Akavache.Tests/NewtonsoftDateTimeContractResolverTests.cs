// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="NewtonsoftDateTimeContractResolver"/>.
/// </summary>
[Category("Akavache")]
public class NewtonsoftDateTimeContractResolverTests
{
    /// <summary>
    /// Resolving a DateTime type on a resolver with no existing resolver should attach the tick converter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldAttachDateTimeConverterByDefault()
    {
        NewtonsoftDateTimeContractResolver resolver = new();
        var contract = resolver.ResolveContract(typeof(DateTime));

        await Assert.That(contract).IsNotNull();
        await Assert.That(contract.Converter).IsTypeOf<NewtonsoftDateTimeTickConverter>();
    }

    /// <summary>
    /// Resolving a nullable DateTime should also attach the tick converter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldAttachDateTimeConverterForNullable()
    {
        NewtonsoftDateTimeContractResolver resolver = new(null, DateTimeKind.Utc);
        var contract = resolver.ResolveContract(typeof(DateTime?));

        await Assert.That(contract.Converter).IsTypeOf<NewtonsoftDateTimeTickConverter>();
    }

    /// <summary>
    /// Resolving a DateTimeOffset type should attach the DateTimeOffset tick converter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldAttachDateTimeOffsetConverter()
    {
        NewtonsoftDateTimeContractResolver resolver = new();
        var contract = resolver.ResolveContract(typeof(DateTimeOffset));

        await Assert.That(contract.Converter).IsSameReferenceAs(NewtonsoftDateTimeOffsetTickConverter.Default);
    }

    /// <summary>
    /// Resolving a nullable DateTimeOffset type should also attach the DateTimeOffset tick converter.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldAttachDateTimeOffsetConverterForNullable()
    {
        NewtonsoftDateTimeContractResolver resolver = new();
        var contract = resolver.ResolveContract(typeof(DateTimeOffset?));

        await Assert.That(contract.Converter).IsSameReferenceAs(NewtonsoftDateTimeOffsetTickConverter.Default);
    }

    /// <summary>
    /// Resolving a non-Date type should leave the converter null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldNotAttachConverterForOtherTypes()
    {
        NewtonsoftDateTimeContractResolver resolver = new();
        var contract = resolver.ResolveContract(typeof(string));

        await Assert.That(contract).IsNotNull();
        await Assert.That(contract.Converter).IsNull();
    }

    /// <summary>
    /// When an existing resolver is supplied that already attaches a converter, that contract should be returned as-is.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldHonorExistingResolverWithConverter()
    {
        ResolverWithConverter inner = new();
        NewtonsoftDateTimeContractResolver resolver = new(inner, null);

        var contract = resolver.ResolveContract(typeof(DateTime));

        // Should return the inner contract directly (with its own converter) and not overwrite.
        await Assert.That(contract.Converter).IsTypeOf<MarkerConverter>();
    }

    /// <summary>
    /// When an existing resolver returns a contract without a converter, the tick converter should still be applied.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldApplyConverterWhenExistingReturnsNoConverter()
    {
        DefaultContractResolver inner = new();
        NewtonsoftDateTimeContractResolver resolver = new(inner, DateTimeKind.Local);

        var contract = resolver.ResolveContract(typeof(DateTime));

        await Assert.That(contract.Converter).IsTypeOf<NewtonsoftDateTimeTickConverter>();
    }

    /// <summary>
    /// An existing resolver of the same type should be ignored to prevent infinite recursion.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverShouldIgnoreSameTypeExistingResolver()
    {
        NewtonsoftDateTimeContractResolver inner = new();
        NewtonsoftDateTimeContractResolver resolver = new()
        {
            ExistingContractResolver = inner,
        };

        var contract = resolver.ResolveContract(typeof(DateTime));

        // Should fall back to base.ResolveContract and apply our converter.
        await Assert.That(contract.Converter).IsTypeOf<NewtonsoftDateTimeTickConverter>();
    }

    /// <summary>
    /// The ForceDateTimeKind property should be settable after construction.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ContractResolverForceDateTimeKindShouldBeSettable()
    {
        NewtonsoftDateTimeContractResolver resolver = new()
        {
            ForceDateTimeKind = DateTimeKind.Utc,
        };

        await Assert.That(resolver.ForceDateTimeKind).IsEqualTo(DateTimeKind.Utc);

        var contract = resolver.ResolveContract(typeof(DateTime));
        await Assert.That(contract.Converter).IsTypeOf<NewtonsoftDateTimeTickConverter>();
    }

    /// <summary>
    /// A minimal inner resolver that always attaches a marker converter to contracts.
    /// </summary>
    private sealed class ResolverWithConverter : DefaultContractResolver
    {
        /// <inheritdoc />
        public override JsonContract ResolveContract(Type type)
        {
            var contract = base.ResolveContract(type);
            contract.Converter = new MarkerConverter();
            return contract;
        }
    }

    /// <summary>
    /// A no-op converter used to detect when an existing resolver's converter is preserved.
    /// </summary>
    private sealed class MarkerConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType) => true;

        /// <inheritdoc />
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => null;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
        }
    }
}
