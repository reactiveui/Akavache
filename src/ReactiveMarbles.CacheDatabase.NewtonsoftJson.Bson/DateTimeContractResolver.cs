// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// Contract resolver for handling DateTime serialization consistently with Akavache.
/// </summary>
public class DateTimeContractResolver : DefaultContractResolver
{
    /// <summary>
    /// Gets or sets the existing contract resolver to delegate to.
    /// </summary>
    public IContractResolver? ExistingContractResolver { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force DateTime serialization as local kind.
    /// </summary>
    public DateTimeKind? ForceDateTimeKindOverride { get; set; }

    /// <inheritdoc/>
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
            contract.Converter = ForceDateTimeKindOverride == DateTimeKind.Local
                ? JsonDateTimeTickConverter.LocalDateTimeKindDefault
                : JsonDateTimeTickConverter.Default;
        }
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
        {
            contract.Converter = JsonDateTimeOffsetTickConverter.Default;
        }

        return contract;
    }
}
