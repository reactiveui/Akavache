// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Akavache;

/// <summary>
/// Determines how to serialize to and from a byte.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Gets or sets the DateTimeKind handling for BSON readers to be forced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, BsonReader uses a <see cref="DateTimeKind"/> of <see cref="DateTimeKind.Local"/> and see BsonWriter
    /// uses <see cref="DateTimeKind.Utc"/>. Thus, DateTimes are serialized as UTC but deserialized as local time. To force BSON readers to
    /// use some other <c>DateTimeKind</c>, you can set this value.
    /// </para>
    /// </remarks>
    DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The type.</returns>
    /// <remarks>
    /// AOT-safe deserialization (via a <c>System.Text.Json.Serialization.Metadata.JsonTypeInfo&lt;T&gt;</c>)
    /// is provided by the <c>Akavache.SystemTextJson</c> package as an extension method on
    /// <see cref="ISerializer"/>; Akavache.Core is deliberately serializer-agnostic and does not
    /// depend on <c>System.Text.Json</c>.
    /// </remarks>
    [RequiresUnreferencedCode("Implementations may use reflection-based deserialization. For AOT, use the JsonTypeInfo extension in Akavache.SystemTextJson.")]
    [RequiresDynamicCode("Implementations may use reflection-based deserialization. For AOT, use the JsonTypeInfo extension in Akavache.SystemTextJson.")]
    T? Deserialize<T>(byte[] bytes);

    /// <summary>
    /// Serializes to bytes.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The bytes.</returns>
    /// <remarks>
    /// AOT-safe serialization (via a <c>System.Text.Json.Serialization.Metadata.JsonTypeInfo&lt;T&gt;</c>)
    /// is provided by the <c>Akavache.SystemTextJson</c> package as an extension method on
    /// <see cref="ISerializer"/>; Akavache.Core is deliberately serializer-agnostic and does not
    /// depend on <c>System.Text.Json</c>.
    /// </remarks>
    [RequiresUnreferencedCode("Implementations may use reflection-based serialization. For AOT, use the JsonTypeInfo extension in Akavache.SystemTextJson.")]
    [RequiresDynamicCode("Implementations may use reflection-based serialization. For AOT, use the JsonTypeInfo extension in Akavache.SystemTextJson.")]
    byte[] Serialize<T>(T item);
}
