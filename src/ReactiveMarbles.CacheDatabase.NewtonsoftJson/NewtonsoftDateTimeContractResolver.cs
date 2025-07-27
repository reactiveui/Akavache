// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson;

/// <summary>
/// Resolver which will handle DateTime and DateTimeOffset with our own internal resolver.
/// It will also be able to use, if set, a external provider that a user has set.
/// This provides consistent DateTime handling across all serializers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NewtonsoftDateTimeContractResolver"/> class.
/// </remarks>
/// <param name="contractResolver">A inherited contract resolver.</param>
/// <param name="forceDateTimeKindOverride">If we should override the <see cref="DateTimeKind"/>.</param>
internal class NewtonsoftDateTimeContractResolver(IContractResolver? contractResolver, DateTimeKind? forceDateTimeKindOverride) : DefaultContractResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftDateTimeContractResolver"/> class.
    /// </summary>
    public NewtonsoftDateTimeContractResolver()
        : this(null, null)
    {
    }

    public IContractResolver? ExistingContractResolver { get; set; } = contractResolver;

    public DateTimeKind? ForceDateTimeKindOverride { get; set; } = forceDateTimeKindOverride;

    /// <inheritdoc />
    public override JsonContract ResolveContract(Type type)
    {
        var contract = ExistingContractResolver?.ResolveContract(type);
        if (contract?.Converter is not null)
        {
            return contract;
        }

        contract ??= base.ResolveContract(type);

        if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            // Pass the ForceDateTimeKindOverride to the converter so it knows what Kind to use
            contract.Converter = new NewtonsoftDateTimeTickConverter(ForceDateTimeKindOverride);
        }
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
        {
            contract.Converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        }

        return contract;
    }
}
