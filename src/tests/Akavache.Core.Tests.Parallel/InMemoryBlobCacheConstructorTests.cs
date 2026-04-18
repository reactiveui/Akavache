// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="InMemoryBlobCache"/> constructor branches.
/// </summary>
[Category("Akavache")]
public class InMemoryBlobCacheConstructorTests
{
    /// <summary>
    /// The <c>InMemoryBlobCache(string)</c> constructor throws
    /// <see cref="InvalidOperationException"/> when the requested serializer
    /// contract is not registered in the service locator, exercising the
    /// <c>?? throw</c> branch at line 26.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task StringConstructor_UnregisteredContract_Throws() =>
        await Assert.That(static () => new InMemoryBlobCache("NonexistentSerializerContract__"))
            .ThrowsException();

    /// <summary>
    /// The <c>InMemoryBlobCache(ISerializer)</c> constructor throws
    /// <see cref="ArgumentNullException"/> when passed a null serializer,
    /// exercising the <c>?? throw</c> branch at line 35.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    internal async Task SerializerConstructor_Null_Throws() =>
        await Assert.That(static () => new InMemoryBlobCache((ISerializer)null!))
            .Throws<ArgumentNullException>();
}
