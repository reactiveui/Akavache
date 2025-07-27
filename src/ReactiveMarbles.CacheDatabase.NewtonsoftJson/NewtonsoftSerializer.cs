// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson;

/// <summary>
/// Serializer for the Newtonsoft Serializer.
/// </summary>
public class NewtonsoftSerializer : ISerializer
{
    private readonly NewtonsoftDateTimeContractResolver _contractResolver = new();

    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerSettings? Options { get; set; }

    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind
    {
        get => _contractResolver.ForceDateTimeKind;
        set => _contractResolver.ForceDateTimeKind = value;
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for deserialization.")]
#endif
    public T? Deserialize<T>(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var textReader = new StreamReader(stream);
        var serializer = JsonSerializer.Create(GetEffectiveSettings());
        return (T?)serializer.Deserialize(textReader, typeof(T));
    }

    /// <inheritdoc/>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using Newtonsoft.Json requires types to be preserved for serialization.")]
#endif
    public byte[] Serialize<T>(T item)
    {
        var serializer = JsonSerializer.Create(GetEffectiveSettings());

        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream);
        serializer.Serialize(streamWriter, item, typeof(T));
        streamWriter.Flush();

        stream.Position = 0;

        return stream.ToArray();
    }

    private JsonSerializerSettings GetEffectiveSettings()
    {
        var settings = Options ?? new JsonSerializerSettings();

        // Create a copy to avoid modifying the original settings
        settings = new JsonSerializerSettings
        {
            ContractResolver = _contractResolver,
            DateTimeZoneHandling = settings.DateTimeZoneHandling,
            DateParseHandling = settings.DateParseHandling,
            FloatParseHandling = settings.FloatParseHandling,
            NullValueHandling = settings.NullValueHandling,
            DefaultValueHandling = settings.DefaultValueHandling,
            ObjectCreationHandling = settings.ObjectCreationHandling,
            MissingMemberHandling = settings.MissingMemberHandling,
            ReferenceLoopHandling = settings.ReferenceLoopHandling,
            CheckAdditionalContent = settings.CheckAdditionalContent,
            StringEscapeHandling = settings.StringEscapeHandling,
            Culture = settings.Culture,
            MaxDepth = settings.MaxDepth,
            Formatting = settings.Formatting,
            DateFormatHandling = settings.DateFormatHandling,
            DateFormatString = settings.DateFormatString,
            FloatFormatHandling = settings.FloatFormatHandling,
            Converters = settings.Converters,
            TypeNameHandling = settings.TypeNameHandling,
            MetadataPropertyHandling = settings.MetadataPropertyHandling,
            TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling,
            ConstructorHandling = settings.ConstructorHandling,
            Error = settings.Error
        };

        // Set our contract resolver, preserving any existing one
        _contractResolver.ExistingContractResolver = settings.ContractResolver;
        settings.ContractResolver = _contractResolver;

        return settings;
    }
}
