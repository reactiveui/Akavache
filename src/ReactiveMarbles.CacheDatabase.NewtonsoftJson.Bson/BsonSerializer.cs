// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// BSON serializer for the Newtonsoft JSON library.
/// </summary>
public class BsonSerializer : ISerializer
{
    /// <summary>
    /// Gets or sets the optional JSON serializer settings.
    /// </summary>
    public JsonSerializerSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the DateTimeKind handling for BSON readers to be forced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, <see cref="BsonReader"/> uses a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/> and <see cref="BsonWriter"/>
    /// uses <see cref="DateTimeKind.Utc"/>. Thus, DateTimes are serialized as UTC but deserialized as local time. To force BSON readers to
    /// use some other <c>DateTimeKind</c>, you can set this value.
    /// </para>
    /// </remarks>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        using var stream = new MemoryStream(bytes);
        using var reader = new BsonDataReader(stream);

        if (ForcedDateTimeKind.HasValue)
        {
            reader.DateTimeKindHandling = ForcedDateTimeKind.Value;
        }

        var serializer = JsonSerializer.Create(Settings);
        return serializer.Deserialize<T>(reader);
    }

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item)
    {
        using var stream = new MemoryStream();
        using var writer = new BsonDataWriter(stream);

        var serializer = JsonSerializer.Create(Settings);
        serializer.Serialize(writer, item, typeof(T));
        writer.Flush();

        return stream.ToArray();
    }
}
