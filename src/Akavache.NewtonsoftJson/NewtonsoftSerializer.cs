// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Splat;

namespace Akavache;

/// <summary>
/// Serializer for the Newtonsoft Serializer.
/// </summary>
public class NewtonsoftSerializer : ISerializer, IEnableLogger
{
    private JsonSerializer? _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftSerializer"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public NewtonsoftSerializer(JsonSerializerSettings options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        Options = options;
    }

    /// <summary>
    /// Gets or sets the optional options.
    /// </summary>
    public JsonSerializerSettings Options { get; set; }

    /// <summary>
    /// Gets the serializer.
    /// </summary>
    /// <param name="getJsonDateTimeContractResolver">The get json date time contract resolver.</param>
    public void CreateSerializer(Func<IDateTimeContractResolver> getJsonDateTimeContractResolver)
    {
        if (getJsonDateTimeContractResolver is null)
        {
            throw new ArgumentNullException(nameof(getJsonDateTimeContractResolver));
        }

        var jsonDateTimeContractResolver = getJsonDateTimeContractResolver() as JsonDateTimeContractResolver;

        lock (Options)
        {
            jsonDateTimeContractResolver!.ExistingContractResolver = Options.ContractResolver;
            Options.ContractResolver = jsonDateTimeContractResolver;
            _serializer = JsonSerializer.Create(Options);
            Options.ContractResolver = jsonDateTimeContractResolver.ExistingContractResolver;
        }
    }

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item)
    {
        if (_serializer is null)
        {
            throw new InvalidOperationException("You must call CreateSerializer before serializing");
        }

        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        _serializer.Serialize(writer, new ObjectWrapper<T>(item));
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
    public byte[] SerializeObject<T>(T value) =>
        Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, Options));

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes)
    {
        if (_serializer is null)
        {
            throw new InvalidOperationException("You must call CreateSerializer before deserializing");
        }

        using var reader = new BsonDataReader(new MemoryStream(bytes));
        var forcedDateTimeKind = BlobCache.ForcedDateTimeKind;
        if (forcedDateTimeKind.HasValue)
        {
            reader.DateTimeKindHandling = forcedDateTimeKind.Value;
        }

        try
        {
            var wrapper = _serializer.Deserialize<ObjectWrapper<T>>(reader);
            return wrapper is null ? default : wrapper.Value;
        }
        catch (Exception ex)
        {
            this.Log().Warn(ex, "Failed to deserialize data as boxed, we may be migrating from an old Akavache");
        }

        return _serializer.Deserialize<T>(reader);
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
            var ret = JsonConvert.DeserializeObject<T>(bytes, Options);
            return Observable.Return(ret);
        }
        catch (Exception ex)
        {
            return Observable.Throw<T>(ex);
        }
    }
}
