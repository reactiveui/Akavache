// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Akavache.Json;

/// <summary>
/// SystemJsonSerializer.
/// </summary>
/// <seealso cref="Akavache.ISerializer" />
public class SystemJsonSerializer : ISerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemJsonSerializer"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public SystemJsonSerializer(JsonSerializerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

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
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <returns>
    /// The type.
    /// </returns>
    public T? Deserialize<T>(byte[] bytes) => (T?)JsonSerializer.Deserialize(bytes, typeof(T), Options);
    /// <summary>
    /// Deserializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="x">The x.</param>
    /// <returns>
    /// An Observable of T.
    /// </returns>
    public IObservable<T?> DeserializeObject<T>(byte[] x) => throw new NotImplementedException();
    /// <summary>
    /// Deserializes the object wrapper.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="data">The data.</param>
    /// <returns>
    /// A value of T.
    /// </returns>
    public T DeserializeObjectWrapper<T>(byte[] data) => throw new NotImplementedException();
    /// <summary>
    /// Serializes to an bytes.
    /// </summary>
    /// <typeparam name="T">The type of serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>
    /// The bytes.
    /// </returns>
    public byte[] Serialize<T>(T item) => JsonSerializer.SerializeToUtf8Bytes(item, Options);
    /// <summary>
    /// Serializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The bytes.
    /// </returns>
    public byte[] SerializeObject<T>(T value) => throw new NotImplementedException();
    /// <summary>
    /// Serializes the object wrapper.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>
    /// A byte array.
    /// </returns>
    public byte[] SerializeObjectWrapper<T>(T value) => throw new NotImplementedException();
}
