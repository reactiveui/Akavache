// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;
using Splat;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// Serializer for the Newtonsoft Serializer.
/// </summary>
public class NewtonsoftBsonSerializer : ISerializer, IEnableLogger
{
    private readonly JsonDateTimeContractResolver _jsonDateTimeContractResolver = new(); // This will make us use ticks instead of json ticks for DateTime.
    private DateTimeKind? _dateTimeKind;

    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerSettings? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => _dateTimeKind ?? CacheDatabase.ForcedDateTimeKind;
        set => _dateTimeKind = _jsonDateTimeContractResolver.ForceDateTimeKindOverride = value;
    }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes)
    {
        var serializer = GetSerializer();
        using var reader = new BsonDataReader(new MemoryStream(bytes));
        var forcedDateTimeKind = ForcedDateTimeKind;

        if (forcedDateTimeKind.HasValue)
        {
            reader.DateTimeKindHandling = forcedDateTimeKind.Value;
        }

        try
        {
            var wrapper = serializer.Deserialize<ObjectWrapper<T>>(reader);
            return wrapper is null ? default : wrapper.Value;
        }
        catch (Exception ex)
        {
            this.Log().Warn(ex, "Failed to deserialize data as boxed, we may be migrating from an old Akavache");
        }

        return serializer.Deserialize<T>(reader);
    }

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item)
    {
        var serializer = GetSerializer();
        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        serializer.Serialize(writer, new ObjectWrapper<T>(item));
        return ms.ToArray();
    }

    private JsonSerializer GetSerializer()
    {
        var settings = Options ?? new JsonSerializerSettings();
        JsonSerializer serializer;

        lock (settings)
        {
            _jsonDateTimeContractResolver.ExistingContractResolver = settings.ContractResolver;
            _jsonDateTimeContractResolver.ForceDateTimeKindOverride = ForcedDateTimeKind;
            settings.ContractResolver = _jsonDateTimeContractResolver;
            serializer = JsonSerializer.Create(settings);
            settings.ContractResolver = _jsonDateTimeContractResolver.ExistingContractResolver;
            Options = settings; // Update the options to the new settings with the resolver.
        }

        return serializer;
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
