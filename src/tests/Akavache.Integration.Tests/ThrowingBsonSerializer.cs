// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

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
