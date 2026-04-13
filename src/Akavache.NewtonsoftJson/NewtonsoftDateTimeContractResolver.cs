// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace Akavache.NewtonsoftJson;

/// <summary>
/// Resolver which will handle DateTime and DateTimeOffset with our own internal resolver.
/// It will also be able to use, if set, a external provider that a user has set.
/// This provides consistent DateTime handling across all serializers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NewtonsoftDateTimeContractResolver"/> class.
/// </remarks>
/// <param name="contractResolver">A inherited contract resolver.</param>
/// <param name="forceDateTimeKind">If we should override the <see cref="DateTimeKind"/>.</param>
internal class NewtonsoftDateTimeContractResolver(IContractResolver? contractResolver, DateTimeKind? forceDateTimeKind) : DefaultContractResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftDateTimeContractResolver"/> class.
    /// </summary>
    public NewtonsoftDateTimeContractResolver()
        : this(null, null)
    {
    }

    /// <summary>
    /// Gets or sets an existing contract resolver to delegate to before applying the DateTime overrides.
    /// </summary>
    public IContractResolver? ExistingContractResolver { get; set; } = contractResolver;

    /// <summary>
    /// Gets or sets the <see cref="DateTimeKind"/> that resolved DateTime values should be forced to, if any.
    /// </summary>
    public DateTimeKind? ForceDateTimeKind { get; set; } = forceDateTimeKind;

    /// <inheritdoc />
    public override JsonContract ResolveContract(Type type)
    {
        // Check if we have an existing contract resolver and it's not another instance of this class
        // to prevent infinite recursion
        var contract = ExistingContractResolver is not null &&
                       ExistingContractResolver.GetType() != typeof(NewtonsoftDateTimeContractResolver)
                       ? ExistingContractResolver.ResolveContract(type)
                       : null;

        if (contract?.Converter is not null)
        {
            return contract;
        }

        contract ??= base.ResolveContract(type);

        if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            // Pass the ForceDateTimeKind to the converter so it knows what Kind to use
            contract.Converter = new NewtonsoftDateTimeTickConverter(ForceDateTimeKind);
        }
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
        {
            contract.Converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        }

        return contract;
    }
}
