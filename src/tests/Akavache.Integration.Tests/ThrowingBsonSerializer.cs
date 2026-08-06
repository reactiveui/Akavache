// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Integration.Tests;

/// <summary>A BSON-named serializer that throws on Deserialize.</summary>
internal sealed class ThrowingBsonSerializer : ISerializer
{
    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes) =>
        throw new InvalidOperationException("ThrowingBsonSerializer always throws on Deserialize.");

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item) =>
        throw new InvalidOperationException("ThrowingBsonSerializer always throws on Serialize.");
}
