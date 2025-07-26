// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReactiveMarbles.CacheDatabase.SystemTextJson.Bson;

/// <summary>
/// Custom nullable DateTime converter for System.Text.Json that respects ForcedDateTimeKind.
/// </summary>
internal class SystemJsonNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly SystemJsonDateTimeConverter _innerConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJsonNullableDateTimeConverter"/> class.
    /// </summary>
    /// <param name="forcedKind">The forced DateTime kind.</param>
    public SystemJsonNullableDateTimeConverter(DateTimeKind forcedKind)
    {
        _innerConverter = new SystemJsonDateTimeConverter(forcedKind);
    }

    /// <inheritdoc/>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _innerConverter.Read(ref reader, typeof(DateTime), options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            _innerConverter.Write(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
