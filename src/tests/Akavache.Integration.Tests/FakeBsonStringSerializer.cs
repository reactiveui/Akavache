// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>A fake BSON-named serializer that returns a fixed string.</summary>
internal sealed class FakeBsonStringSerializer : ISerializer
{
    /// <inheritdoc/>
    public DateTimeKind? ForcedDateTimeKind { get; set; }

    /// <inheritdoc/>
    public T? Deserialize<T>(byte[] bytes) =>
        typeof(T) != typeof(string)
            ? default
            : (T)(object)"fake-bson-string";

    /// <inheritdoc/>
    public byte[] Serialize<T>(T item) => [];
}
