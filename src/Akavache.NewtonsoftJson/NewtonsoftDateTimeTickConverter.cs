// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

#if REACTIVE_SHIM
namespace Akavache.Reactive.NewtonsoftJson;
#else
namespace Akavache.NewtonsoftJson;
#endif

/// <summary>
/// JSON converter for DateTime that preserves ticks and handles DateTimeKind appropriately.
/// This converter matches the behavior of the NewtonsoftBson serializer for consistent DateTime handling.
/// </summary>
/// <param name="forceDateTimeKindOverride">Optional DateTime kind override.</param>
/// <remarks>
/// Initializes a new instance of the <see cref="NewtonsoftDateTimeTickConverter"/> class.
/// </remarks>
internal class NewtonsoftDateTimeTickConverter(DateTimeKind? forceDateTimeKindOverride = null) : JsonConverter
{
    /// <summary>Gets a instance of the DateTimeConverter that handles the DateTime in UTC mode.</summary>
    internal static NewtonsoftDateTimeTickConverter Default { get; } = new();

    /// <summary>Gets a instance of the DateTimeConverter that handles the DateTime in Local mode.</summary>
    internal static NewtonsoftDateTimeTickConverter LocalDateTimeKindDefault { get; } = new(DateTimeKind.Local);

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime) || objectType == typeof(DateTime?);

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ArgumentExceptionHelper.ThrowIfNull(reader);

        if (reader.TokenType is not JsonToken.Integer and not JsonToken.Date)
        {
            return null;
        }

        if (reader is { TokenType: JsonToken.Date, Value: not null })
        {
            return ConvertDateTimeKind((DateTime)reader.Value, forceDateTimeKindOverride ?? DateTimeKind.Utc);
        }

        return (objectType != typeof(DateTime) && objectType != typeof(DateTime?)) || reader.Value is null ? null : new DateTime((long)reader.Value, forceDateTimeKindOverride ?? DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not DateTime dateTime)
        {
            return;
        }

        // Store ticks in a way that preserves the intent while allowing proper deserialization.
        // Convert to UTC for consistent storage, but handle each kind appropriately.
        var ticksToStore = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime.Ticks,
            DateTimeKind.Local => dateTime.ToUniversalTime().Ticks,
            _ => dateTime.Ticks, // Unspecified — preserve original ticks
        };

        writer.WriteValue(ticksToStore);
    }

    /// <summary>Converts a <see cref="DateTime"/> to the specified <see cref="DateTimeKind"/>.</summary>
    /// <param name="dateTime">The source DateTime.</param>
    /// <param name="targetKind">The target kind.</param>
    /// <returns>A DateTime with the specified kind.</returns>
    internal static DateTime ConvertDateTimeKind(DateTime dateTime, DateTimeKind targetKind)
    {
        if (targetKind == DateTimeKind.Utc)
        {
            return DateTime.SpecifyKind(dateTime.ToUniversalTime(), DateTimeKind.Utc);
        }

        return targetKind == DateTimeKind.Local ? DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Local) : DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
    }
}
