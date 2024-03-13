// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Splat;

namespace Akavache.Json;

/// <summary>
/// SystemJsonSerializer.
/// </summary>
/// <seealso cref="Akavache.ISerializer" />
public class SystemJsonSerializer : ISerializer, IEnableLogger
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJsonSerializer"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public SystemJsonSerializer(JsonSerializerOptions options)
    {
        options.ThrowArgumentNullExceptionIfNull(nameof(options));

        Options = options;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <value>
    /// The options.
    /// </value>
    public JsonSerializerOptions Options { get; }

    /// <summary>
    /// Gets the serializer.
    /// </summary>
    /// <param name="getJsonDateTimeContractResolver">The json date time contract resolver.</param>
    public void CreateSerializer(Func<IDateTimeContractResolver> getJsonDateTimeContractResolver)
    {
        // TODO: Implement this
    }

    /// <summary>
    /// Serializes to an bytes.
    /// </summary>
    /// <typeparam name="T">The type of serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>
    /// The bytes.
    /// </returns>
    public byte[] Serialize<T>(T item)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);
        JsonSerializer.Serialize(writer, new ObjectWrapper<T>(item));
        return ms.ToArray();
    }

    /// <summary>
    /// Serializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The bytes.
    /// </returns>
    public byte[] SerializeObject<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <returns>
    /// The type.
    /// </returns>
    public T? Deserialize<T>(byte[] bytes)
    {
#pragma warning disable CS8603 // Possible null reference return.

        ////var options = new JsonReaderOptions
        ////{
        ////    AllowTrailingCommas = true,
        ////    CommentHandling = JsonCommentHandling.Skip
        ////};
        ////ReadOnlySpan<byte> jsonReadOnlySpan = bytes;
        ////var reader = new Utf8JsonReader(jsonReadOnlySpan, options);

        var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;

        ////if (forcedDateTimeKind.HasValue)
        ////{
        ////    reader.DateTimeKindHandling = forcedDateTimeKind.Value;
        ////}

        try
        {
            var wrapper = JsonSerializer.Deserialize<ObjectWrapper<T>>(bytes);

            return wrapper is null ? default : wrapper.Value;
        }
        catch (Exception ex)
        {
            this.Log().Warn(ex, "Failed to deserialize data as boxed, we may be migrating from an old Akavache");
        }

        return JsonSerializer.Deserialize<T>(bytes);
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Deserializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="x">The x.</param>
    /// <returns>
    /// An Observable of T.
    /// </returns>
    public IObservable<T?> DeserializeObject<T>(byte[] x)
    {
        if (x is null)
        {
            throw new ArgumentNullException(nameof(x));
        }

        try
        {
            var bytes = Encoding.UTF8.GetString(x, 0, x.Length);
            var ret = JsonSerializer.Deserialize<T>(bytes, Options);
            return Observable.Return(ret);
        }
        catch (Exception ex)
        {
            return Observable.Throw<T>(ex);
        }
    }
}
