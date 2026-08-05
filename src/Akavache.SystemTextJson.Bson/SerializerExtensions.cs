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
/// <see cref="SystemJsonBsonSerializer"/> instance when the
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
    /// <summary>Extension members for <c>ISerializer</c>.</summary>
    /// <param name="serializer">The serializer to dispatch through.</param>
    extension(ISerializer serializer)
    {
        /// <summary>Deserializes <paramref name="bytes"/> into a <typeparamref name="T"/> using the AOT-safe <see cref="JsonTypeInfo{T}"/> metadata path.</summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="bytes">The bytes to deserialize.</param>
        /// <param name="jsonTypeInfo">The type metadata describing <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value, or <c>default</c>.</returns>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="serializer"/> is not a System.Text.Json-backed Akavache serializer.</exception>
        public T? Deserialize<T>(byte[] bytes, JsonTypeInfo<T> jsonTypeInfo) =>
                serializer switch
                {
                    SystemJsonSerializer => SystemJsonSerializer.DeserializeAot(bytes, jsonTypeInfo),
                    SystemJsonBsonSerializer => SystemJsonBsonSerializer.DeserializeAot(bytes, jsonTypeInfo),
                    _ => throw new NotSupportedException(UnsupportedMessage(serializer, "deserialization", "Deserialize<T>(byte[])")),
                };

        /// <summary>Serializes <paramref name="item"/> to bytes using the AOT-safe <see cref="JsonTypeInfo{T}"/> metadata path.</summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="item">The item to serialize.</param>
        /// <param name="jsonTypeInfo">The type metadata describing <typeparamref name="T"/>.</param>
        /// <returns>The serialized bytes.</returns>
        /// <exception cref="NotSupportedException">Thrown when <paramref name="serializer"/> is not a System.Text.Json-backed Akavache serializer.</exception>
        public byte[] Serialize<T>(T item, JsonTypeInfo<T> jsonTypeInfo) =>
                serializer switch
                {
                    SystemJsonSerializer => SystemJsonSerializer.SerializeAot(item, jsonTypeInfo),
                    SystemJsonBsonSerializer => SystemJsonBsonSerializer.SerializeAot(item, jsonTypeInfo),
                    _ => throw new NotSupportedException(UnsupportedMessage(serializer, "serialization", "Serialize<T>(T)")),
                };
    }

    /// <summary>Builds the message thrown when a serializer has no AOT-safe metadata path.</summary>
    /// <param name="serializer">The serializer that could not be dispatched.</param>
    /// <param name="direction">Either <c>serialization</c> or <c>deserialization</c>.</param>
    /// <param name="fallbackOverload">The non-typed overload the caller should use instead.</param>
    /// <returns>The exception message.</returns>
    private static string UnsupportedMessage(ISerializer serializer, string direction, string fallbackOverload) =>
        $"{serializer.GetType().Name} does not support AOT-safe JsonTypeInfo {direction}. Use the {fallbackOverload} overload, or configure a System.Text.Json-backed serializer.";
}
