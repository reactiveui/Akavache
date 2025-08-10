// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for serialization.")]
#endif
    T? Deserialize<T>(byte[] bytes);

    /// <summary>
    /// Serializes to an bytes.
    /// </summary>
    /// <typeparam name="T">The type of serialize.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The bytes.</returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Using System.Text.Json requires types to be preserved for serialization.")]
    [RequiresDynamicCode("Using System.Text.Json requires types to be preserved for serialization.")]
#endif
    byte[] Serialize<T>(T item);
}
