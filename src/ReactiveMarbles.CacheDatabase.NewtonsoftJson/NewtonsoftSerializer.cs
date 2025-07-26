// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson;

/// <summary>
/// Serializer for the Newtonsoft Serializer.
/// </summary>
public class NewtonsoftSerializer : ISerializer
{
    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerSettings? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var textReader = new StreamReader(stream);
        var serializer = JsonSerializer.Create(Options);
        return (T?)serializer.Deserialize(textReader, typeof(T));
    }

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item)
    {
        var serializer = JsonSerializer.Create(Options);

        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream);
        serializer.Serialize(streamWriter, item, typeof(T));
        streamWriter.Flush();

        stream.Position = 0;

        return stream.ToArray();
    }
}
