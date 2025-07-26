// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// A BSON-aware serializer that can handle special DateTime serialization.
/// </summary>
internal class BsonAwareSerializer : ISerializer
{
    private readonly ISerializer _fallbackSerializer;
    private readonly JsonSerializerSettings _bsonSettings;

    public BsonAwareSerializer(ISerializer fallbackSerializer, DateTimeKind? forcedDateTimeKind = null)
    {
        _fallbackSerializer = fallbackSerializer ?? throw new ArgumentNullException(nameof(fallbackSerializer));
        _bsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DateTimeContractResolver
            {
                ForceDateTimeKindOverride = forcedDateTimeKind
            },
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
    }

    public byte[] Serialize<T>(T obj)
    {
        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        var serializer = JsonSerializer.Create(_bsonSettings);
        serializer.Serialize(writer, new ObjectWrapper<T>(obj));
        return ms.ToArray();
    }

    public T? Deserialize<T>(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BsonDataReader(ms);
        var serializer = JsonSerializer.Create(_bsonSettings);

        try
        {
            var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);
            return wrapper != null ? wrapper.Value : default;
        }
        catch
        {
            // Fallback to regular serializer for backward compatibility
            return _fallbackSerializer.Deserialize<T>(data);
        }
    }

    private class ObjectWrapper<T>
    {
        public ObjectWrapper()
        {
        }

        public ObjectWrapper(T value) => Value = value;

        public T? Value { get; set; }
    }
}
