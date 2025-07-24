// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// Contract resolver for handling DateTime serialization consistently with Akavache.
/// </summary>
public class DateTimeContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
    /// <summary>
    /// Gets or sets a value indicating whether to force DateTime serialization as ticks.
    /// </summary>
    public bool ForceDateTimeAsTicks { get; set; } = true;

    /// <inheritdoc/>
    protected override JsonContract CreateContract(Type objectType)
    {
        var contract = base.CreateContract(objectType);

        if (ForceDateTimeAsTicks && (objectType == typeof(DateTime) || objectType == typeof(DateTime?)))
        {
            contract.Converter = new JavaScriptDateTimeConverter();
        }

        return contract;
    }
}
