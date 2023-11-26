// Copyright (c) 2023 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache;

/// <summary>
/// Determines how to serialize to and from a byte.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Gets the serializer.
    /// </summary>
    /// <param name="getJsonDateTimeContractResolver">The json date time contract resolver.</param>
    void CreateSerializer(Func<IDateTimeContractResolver> getJsonDateTimeContractResolver);

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The type.</returns>
    T? Deserialize<T>(byte[] bytes);

    /// <summary>
    /// Deserializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="x">The x.</param>
    /// <returns>An Observable of T.</returns>
    IObservable<T?> DeserializeObject<T>(byte[] x);

    /// <summary>
    /// Deserializes the object wrapper.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="data">The data.</param>
    /// <returns>A value of T.</returns>
    /// <exception cref="System.InvalidOperationException">You must call CreateSerializer before deserializing.</exception>
    T DeserializeObjectWrapper<T>(byte[] data);

    /// <summary>
    /// Serializes to an bytes.
    /// </summary>
    /// <typeparam name="T">The type of serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The bytes.</returns>
    byte[] Serialize<T>(T item);

    /// <summary>
    /// Serializes the object.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The bytes.</returns>
    byte[] SerializeObject<T>(T value);

    /// <summary>
    /// Serializes the object wrapper.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A byte array.</returns>
    /// <exception cref="System.InvalidOperationException">You must call CreateSerializer before serializing.</exception>
    byte[] SerializeObjectWrapper<T>(T value);
}
