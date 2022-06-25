// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace Akavache;

/// <summary>
/// Resolver which will handle DateTime and DateTimeOffset with our own internal resolver.
/// It will also be able to use, if set, a external provider that a user has set.
/// </summary>
internal class JsonDateTimeContractResolver : DefaultContractResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDateTimeContractResolver"/> class.
    /// </summary>
    public JsonDateTimeContractResolver()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDateTimeContractResolver"/> class.
    /// </summary>
    /// <param name="contractResolver">A inherited contract resolver.</param>
    /// <param name="forceDateTimeKindOverride">If we should override the <see cref="DateTimeKind"/>.</param>
    public JsonDateTimeContractResolver(IContractResolver? contractResolver, DateTimeKind? forceDateTimeKindOverride)
    {
        ExistingContractResolver = contractResolver;
        ForceDateTimeKindOverride = forceDateTimeKindOverride;
    }

    public IContractResolver? ExistingContractResolver { get; set; }

    public DateTimeKind? ForceDateTimeKindOverride { get; set; }

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
            contract.Converter = ForceDateTimeKindOverride == DateTimeKind.Local ? JsonDateTimeTickConverter.LocalDateTimeKindDefault : JsonDateTimeTickConverter.Default;
        }
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
        {
            contract.Converter = JsonDateTimeOffsetTickConverter.Default;
        }

        return contract;
    }
}
