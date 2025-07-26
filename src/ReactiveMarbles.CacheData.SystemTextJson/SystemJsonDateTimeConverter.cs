// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson;

/// <summary>
/// Custom DateTime converter for System.Text.Json that respects ForcedDateTimeKind.
/// </summary>
internal class SystemJsonDateTimeConverter : JsonConverter<DateTime>
{
    private readonly DateTimeKind _forcedKind;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJsonDateTimeConverter"/> class.
    /// </summary>
    /// <param name="forcedKind">The forced DateTime kind.</param>
    public SystemJsonDateTimeConverter(DateTimeKind forcedKind)
    {
        _forcedKind = forcedKind;
    }

    /// <inheritdoc/>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTime = reader.GetDateTime();

        // Apply the forced kind
        return _forcedKind switch
        {
            DateTimeKind.Utc => DateTime.SpecifyKind(dateTime.ToUniversalTime(), DateTimeKind.Utc),
            DateTimeKind.Local => DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Local),
            _ => DateTime.SpecifyKind(dateTime, _forcedKind)
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert to appropriate time before writing
        var dateTimeToWrite = value.Kind switch
        {
            DateTimeKind.Local when _forcedKind == DateTimeKind.Utc => value.ToUniversalTime(),
            DateTimeKind.Utc when _forcedKind == DateTimeKind.Local => value.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, _forcedKind),
            _ => value
        };

        writer.WriteStringValue(dateTimeToWrite);
    }
}
