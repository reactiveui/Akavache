// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// JSON converter for DateTimeOffset that preserves ticks.
/// </summary>
internal class JsonDateTimeOffsetTickConverter : JsonConverter
{
    /// <summary>
    /// Gets the default instance.
    /// </summary>
    public static JsonDateTimeOffsetTickConverter Default { get; } = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader?.Value is long ticks)
        {
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        return null;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            serializer.Serialize(writer, dateTimeOffset.Ticks);
        }
    }
}
