// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akavache.Integration.Tests;

/// <summary>
/// Tests for NewtonsoftDateTimeTickConverter and NewtonsoftDateTimeOffsetTickConverter.
/// </summary>
[Category("Akavache")]
public class NewtonsoftDateConvertersTests
{
    /// <summary>
    /// Tests DateTime tick converter CanConvert.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterCanConvertShouldReturnTrueForDateTime()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        await Assert.That(converter.CanConvert(typeof(DateTime))).IsTrue();
        await Assert.That(converter.CanConvert(typeof(DateTime?))).IsTrue();
        await Assert.That(converter.CanConvert(typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests DateTime ReadJson with null reader throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldThrowOnNullReader()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        await Assert.That(() => converter.ReadJson(null!, typeof(DateTime), null, new()))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests DateTime round-trip with default UTC handling.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterShouldRoundTripUtc()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var json = JsonConvert.SerializeObject(date, settings);
        var result = JsonConvert.DeserializeObject<DateTime>(json, settings);

        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests DateTime round-trip with Local kind override.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterShouldRoundTripLocal()
    {
        var converter = NewtonsoftDateTimeTickConverter.LocalDateTimeKindDefault;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var json = JsonConvert.SerializeObject(date, settings);
        var result = JsonConvert.DeserializeObject<DateTime>(json, settings);

        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests DateTime round-trip with Unspecified kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterShouldHandleUnspecifiedKind()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        DateTime date = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var json = JsonConvert.SerializeObject(date, settings);

        // Should not throw
        await Assert.That(json).IsNotNull();
    }

    /// <summary>
    /// Tests DateTimeOffset converter CanConvert.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterCanConvertShouldReturnTrueForDateTimeOffset()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        await Assert.That(converter.CanConvert(typeof(DateTimeOffset))).IsTrue();
        await Assert.That(converter.CanConvert(typeof(DateTimeOffset?))).IsTrue();
        await Assert.That(converter.CanConvert(typeof(string))).IsFalse();
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson with null reader throws.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldThrowOnNullReader()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        await Assert.That(() => converter.ReadJson(null!, typeof(DateTimeOffset), null, new()))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests DateTimeOffset round-trip preserves ticks and offset.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterShouldRoundTrip()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        DateTimeOffset dto = new(2025, 6, 15, 12, 0, 0, TimeSpan.FromHours(5));
        var json = JsonConvert.SerializeObject(dto, settings);
        var result = JsonConvert.DeserializeObject<DateTimeOffset>(json, settings);

        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Offset).IsEqualTo(TimeSpan.FromHours(5));
    }

    /// <summary>
    /// Tests DateTimeOffset converter handles legacy integer-only format.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterShouldHandleLegacyIntegerFormat()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        // Construct a legacy integer-only payload (raw ticks)
        var ticks = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero).Ticks;
        var json = ticks.ToString();

        var result = JsonConvert.DeserializeObject<DateTimeOffset>(json, settings);
        await Assert.That(result.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests DateTimeOffset converter handles direct date format.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterShouldHandleDirectDate()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JsonSerializerSettings settings = new() { DateParseHandling = DateParseHandling.DateTime };
        settings.Converters.Add(converter);

        const string json = "\"2025-06-15T12:00:00Z\"";
        var result = JsonConvert.DeserializeObject<DateTimeOffset>(json, settings);
        await Assert.That(result.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests DateTime ReadJson returns null for unsupported token types (e.g. string).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReturnNullForStringToken()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        using StringReader stringReader = new("\"hello\"");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTime ReadJson returns null for Boolean token type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReturnNullForBooleanToken()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        using StringReader stringReader = new("true");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTime ReadJson handles a Date token with Local kind override.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldHandleDateTokenWithLocalOverride()
    {
        var converter = NewtonsoftDateTimeTickConverter.LocalDateTimeKindDefault;
        using StringReader stringReader = new("\"2025-06-15T12:00:00Z\"");
        await using JsonTextReader reader = new(stringReader) { DateParseHandling = DateParseHandling.DateTime };
        await reader.ReadAsync();

        var result = (DateTime?)converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests DateTime ReadJson handles a Date token with Unspecified kind override.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldHandleDateTokenWithUnspecifiedOverride()
    {
        NewtonsoftDateTimeTickConverter converter = new(DateTimeKind.Unspecified);
        using StringReader stringReader = new("\"2025-06-15T12:00:00Z\"");
        await using JsonTextReader reader = new(stringReader) { DateParseHandling = DateParseHandling.DateTime };
        await reader.ReadAsync();

        var result = (DateTime?)converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Tests DateTime ReadJson with Integer token but an unrelated objectType falls through to null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReturnNullForIntegerWithUnrelatedObjectType()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        using StringReader stringReader = new("12345");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(object), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTime ReadJson reads ticks with the Local kind override.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReadTicksWithLocalOverride()
    {
        var converter = NewtonsoftDateTimeTickConverter.LocalDateTimeKindDefault;
        var ticks = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local).Ticks;
        using StringReader stringReader = new(ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = (DateTime?)converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests DateTime WriteJson with a Local kind value stores UTC-equivalent ticks.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterWriteJsonShouldHandleLocalKind()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JTokenWriter writer = new();
        DateTime local = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);

        converter.WriteJson(writer, local, new());

        await Assert.That(writer.Token).IsNotNull();
        await Assert.That(writer.Token!.Type).IsEqualTo(JTokenType.Integer);
        await Assert.That((long)writer.Token!).IsEqualTo(local.ToUniversalTime().Ticks);
    }

    /// <summary>
    /// Tests DateTime WriteJson with an Unspecified kind value stores raw ticks.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterWriteJsonShouldHandleUnspecifiedKind()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JTokenWriter writer = new();
        DateTime unspecified = new(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

        converter.WriteJson(writer, unspecified, new());

        await Assert.That(writer.Token).IsNotNull();
        await Assert.That((long)writer.Token!).IsEqualTo(unspecified.Ticks);
    }

    /// <summary>
    /// Tests DateTime WriteJson with a non-DateTime value is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterWriteJsonShouldNoOpForNonDateTimeValue()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JTokenWriter writer = new();

        converter.WriteJson(writer, "not-a-date", new());

        await Assert.That(writer.Token).IsNull();
    }

    /// <summary>
    /// Tests DateTime WriteJson with a null value is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterWriteJsonShouldNoOpForNullValue()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JTokenWriter writer = new();

        converter.WriteJson(writer, null, new());

        await Assert.That(writer.Token).IsNull();
    }

    /// <summary>
    /// Tests DateTime converter handles DateTime.MinValue ticks edge case.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterShouldHandleMinValue()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        var date = DateTime.MinValue;
        var json = JsonConvert.SerializeObject(date, settings);
        var result = JsonConvert.DeserializeObject<DateTime>(json, settings);

        await Assert.That(result.Ticks).IsEqualTo(0L);
    }

    /// <summary>
    /// Tests DateTime ReadJson returns null for a Null token type,
    /// covering the remaining branch of the token type check.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReturnNullForNullToken()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        using StringReader stringReader = new("null");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTime ReadJson returns null for a Float token type,
    /// covering an additional branch of the token type check.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldReturnNullForFloatToken()
    {
        var converter = NewtonsoftDateTimeTickConverter.Default;
        using StringReader stringReader = new("3.14");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Exercises the <c>_ =&gt;</c> default arm of the switch at line 51 in
    /// <see cref="NewtonsoftDateTimeTickConverter.ReadJson"/> when reading a
    /// <see cref="JsonToken.Date"/> token with <c>forceDateTimeKindOverride</c>
    /// set to <see cref="DateTimeKind.Unspecified"/>. The default arm calls
    /// <c>DateTime.SpecifyKind(dateTime, targetKind)</c> without conversion.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldHitDefaultSwitchArmForUnspecifiedDate()
    {
        NewtonsoftDateTimeTickConverter converter = new(DateTimeKind.Unspecified);
        JsonSerializerSettings settings = new() { DateParseHandling = DateParseHandling.DateTime };
        settings.Converters.Add(converter);

        // JSON date string that the reader parses as a Date token.
        const string json = "\"2025-03-10T08:30:00Z\"";
        var result = JsonConvert.DeserializeObject<DateTime>(json, settings);

        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
        await Assert.That(result.Year).IsEqualTo(2025);
        await Assert.That(result.Month).IsEqualTo(3);
    }

    /// <summary>
    /// Exercises the <c>DateTimeKind.Unspecified</c> arm of the <c>targetKind switch</c>
    /// at line 51 in <see cref="NewtonsoftDateTimeTickConverter.ReadJson"/> when the
    /// converter has no explicit kind override (so <c>forceDateTimeKindOverride</c> is
    /// null and <c>targetKind</c> defaults to <see cref="DateTimeKind.Utc"/>). Reading
    /// an integer-ticks token with objectType <see cref="DateTime"/> exercises the
    /// ticks-to-DateTime path with the default UTC kind.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeConverterReadJsonShouldHandleUnspecifiedKindOverrideWithIntegerToken()
    {
        NewtonsoftDateTimeTickConverter converter = new(DateTimeKind.Unspecified);
        var ticks = new DateTime(2025, 3, 10, 8, 30, 0, DateTimeKind.Unspecified).Ticks;
        using StringReader stringReader = new(ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = (DateTime?)converter.ReadJson(reader, typeof(DateTime), null, new());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson returns null for unsupported token types (e.g. string).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldReturnNullForStringToken()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        using StringReader stringReader = new("\"hello\"");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTimeOffset), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson returns null for Boolean token type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldReturnNullForBooleanToken()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        using StringReader stringReader = new("false");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(DateTimeOffset), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson with Integer token but an unrelated objectType falls through to null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldReturnNullForIntegerWithUnrelatedObjectType()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        using StringReader stringReader = new("12345");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = converter.ReadJson(reader, typeof(object), null, new());
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson handles a StartObject payload that contains unknown properties.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldIgnoreUnknownObjectProperties()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        var ticks = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.FromHours(2)).Ticks;
        var offsetTicks = TimeSpan.FromHours(2).Ticks;
        var json = "{\"Ticks\":" + ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ",\"Extra\":\"ignored\",\"OffsetTicks\":" + offsetTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
        using StringReader stringReader = new(json);
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = (DateTimeOffset?)converter.ReadJson(reader, typeof(DateTimeOffset), null, new());
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
        await Assert.That(result.Value.Offset).IsEqualTo(TimeSpan.FromHours(2));
    }

    /// <summary>
    /// Tests DateTimeOffset WriteJson with a non-DateTimeOffset value is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterWriteJsonShouldNoOpForNonDateTimeOffsetValue()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JTokenWriter writer = new();

        converter.WriteJson(writer, "not-a-date", new());

        await Assert.That(writer.Token).IsNull();
    }

    /// <summary>
    /// Tests DateTimeOffset WriteJson with a null value is a no-op.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterWriteJsonShouldNoOpForNullValue()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JTokenWriter writer = new();

        converter.WriteJson(writer, null, new());

        await Assert.That(writer.Token).IsNull();
    }

    /// <summary>
    /// Tests DateTimeOffset converter handles MinValue edge case.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterShouldHandleMinValue()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        JsonSerializerSettings settings = new();
        settings.Converters.Add(converter);

        var dto = DateTimeOffset.MinValue;
        var json = JsonConvert.SerializeObject(dto, settings);
        var result = JsonConvert.DeserializeObject<DateTimeOffset>(json, settings);

        await Assert.That(result.Ticks).IsEqualTo(0L);
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson handles an empty object payload by exiting the read
    /// loop on the first iteration (covers the EndObject early-exit branch).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldHandleEmptyObject()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        using StringReader stringReader = new("{}");
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = (DateTimeOffset?)converter.ReadJson(reader, typeof(DateTimeOffset), null, new());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Ticks).IsEqualTo(0L);
    }

    /// <summary>
    /// Tests DateTimeOffset ReadJson handles an object whose property values come in non-PropertyName
    /// token forms (e.g. nested arrays). Covers the false branch of the
    /// <c>if (reader.TokenType == JsonToken.PropertyName)</c> check inside the read loop.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DateTimeOffsetConverterReadJsonShouldSkipNonPropertyNameTokens()
    {
        var converter = NewtonsoftDateTimeOffsetTickConverter.Default;
        var ticks = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero).Ticks;
        var json = "{\"Ticks\":" + ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"OffsetTicks\":0}";

        using StringReader stringReader = new(json);

        // SupportMultipleContent off; iterate through the object normally.
        await using JsonTextReader reader = new(stringReader);
        await reader.ReadAsync();

        var result = (DateTimeOffset?)converter.ReadJson(reader, typeof(DateTimeOffset), null, new());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Year).IsEqualTo(2025);
    }

    /// <summary>
    /// Tests <see cref="NewtonsoftDateTimeOffsetTickConverter.ReadLongProperty"/> returns
    /// the long value when the property is present.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadLongPropertyShouldReturnValueWhenPresent()
    {
        var jobject = JObject.Parse("{\"Ticks\":42}");

        var result = NewtonsoftDateTimeOffsetTickConverter.ReadLongProperty(jobject, "Ticks");

        await Assert.That(result).IsEqualTo(42L);
    }

    /// <summary>
    /// Tests <see cref="NewtonsoftDateTimeOffsetTickConverter.ReadLongProperty"/> returns
    /// <c>0</c> when the property is missing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadLongPropertyShouldReturnZeroWhenMissing()
    {
        var jobject = JObject.Parse("{\"OtherProp\":123}");

        var result = NewtonsoftDateTimeOffsetTickConverter.ReadLongProperty(jobject, "Ticks");

        await Assert.That(result).IsEqualTo(0L);
    }

    // ── ConvertDateTimeKind ─────────────────────────────────────────────

    /// <summary>
    /// ConvertDateTimeKind with Utc returns a UTC DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertDateTimeKind_Utc_ReturnsUtc()
    {
        var input = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = NewtonsoftDateTimeTickConverter.ConvertDateTimeKind(input, DateTimeKind.Utc);

        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// ConvertDateTimeKind with Local returns a Local DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertDateTimeKind_Local_ReturnsLocal()
    {
        var input = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var result = NewtonsoftDateTimeTickConverter.ConvertDateTimeKind(input, DateTimeKind.Local);

        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// ConvertDateTimeKind with Unspecified returns an Unspecified DateTime.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConvertDateTimeKind_Unspecified_ReturnsUnspecified()
    {
        var input = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var result = NewtonsoftDateTimeTickConverter.ConvertDateTimeKind(input, DateTimeKind.Unspecified);

        await Assert.That(result.Kind).IsEqualTo(DateTimeKind.Unspecified);
    }
}
