// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization.Metadata;

namespace Akavache.SystemTextJson;

/// <summary>
/// Extension methods that expose the AOT-safe <see cref="JsonTypeInfo{T}"/> overloads
/// of <see cref="ISerializer"/> from the System.Text.Json-backed serializers without
/// pulling <c>System.Text.Json</c> into <c>Akavache.Core</c>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions dispatch to the concrete <see cref="SystemJsonSerializer"/> or
/// <see cref="Akavache.SystemTextJson.SystemJsonBsonSerializer"/> instance when the
/// runtime <see cref="ISerializer"/> is backed by one of them. For every other
/// serializer implementation (for example the Newtonsoft-backed ones) they throw
/// <see cref="NotSupportedException"/> — those serializers can still be used via the
/// non-typed <see cref="ISerializer.Deserialize{T}(byte[])"/> /
/// <see cref="ISerializer.Serialize{T}(T)"/> overloads.
/// </para>
/// <para>
/// This indirection keeps <c>Akavache.Core</c> free of a hard dependency on
/// <c>System.Text.Json</c>, so Newtonsoft-only consumers do not transitively pull it
/// in. Callers that need AOT-safe serialization add the
/// <c>Akavache.SystemTextJson.Bson</c> package reference (which transitively brings
/// in <c>Akavache.SystemTextJson</c>) and import this namespace.
/// </para>
/// </remarks>
public static class SerializerExtensions
{
    /// <summary>
    /// Deserializes <paramref name="bytes"/> into a <typeparamref name="T"/> using
    /// the AOT-safe <see cref="JsonTypeInfo{T}"/> metadata path.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="serializer">The serializer to dispatch through.</param>
    /// <param name="bytes">The bytes to deserialize.</param>
    /// <param name="jsonTypeInfo">The type metadata describing <typeparamref name="T"/>.</param>
    /// <returns>The deserialized value, or <c>default</c>.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="serializer"/> is not a System.Text.Json-backed Akavache serializer.</exception>
    public static T? Deserialize<T>(this ISerializer serializer, byte[] bytes, JsonTypeInfo<T> jsonTypeInfo) =>
        serializer switch
        {
            SystemJsonSerializer => SystemJsonSerializer.DeserializeAot(bytes, jsonTypeInfo),
            SystemJsonBsonSerializer => SystemJsonBsonSerializer.DeserializeAot(bytes, jsonTypeInfo),
            _ => throw new NotSupportedException(
                $"{serializer.GetType().Name} does not support AOT-safe JsonTypeInfo deserialization. Use the Deserialize<T>(byte[]) overload, or configure a System.Text.Json-backed serializer."),
        };

    /// <summary>
    /// Serializes <paramref name="item"/> to bytes using the AOT-safe
    /// <see cref="JsonTypeInfo{T}"/> metadata path.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="serializer">The serializer to dispatch through.</param>
    /// <param name="item">The item to serialize.</param>
    /// <param name="jsonTypeInfo">The type metadata describing <typeparamref name="T"/>.</param>
    /// <returns>The serialized bytes.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="serializer"/> is not a System.Text.Json-backed Akavache serializer.</exception>
    public static byte[] Serialize<T>(this ISerializer serializer, T item, JsonTypeInfo<T> jsonTypeInfo) =>
        serializer switch
        {
            SystemJsonSerializer => SystemJsonSerializer.SerializeAot(item, jsonTypeInfo),
            SystemJsonBsonSerializer => SystemJsonBsonSerializer.SerializeAot(item, jsonTypeInfo),
            _ => throw new NotSupportedException(
                $"{serializer.GetType().Name} does not support AOT-safe JsonTypeInfo serialization. Use the Serialize<T>(T) overload, or configure a System.Text.Json-backed serializer."),
        };
}
