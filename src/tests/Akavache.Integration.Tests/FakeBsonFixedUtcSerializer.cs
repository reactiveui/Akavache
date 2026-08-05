// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Integration.Tests;

/// <summary>A fake BSON-named serializer that returns a fixed UTC DateTime.</summary>
internal sealed class FakeBsonFixedUtcSerializer : ISerializer
{
    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes) =>
        typeof(T) != typeof(DateTime)
            ? default
            : (T)(object)new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item) => [];
}
