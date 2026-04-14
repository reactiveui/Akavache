// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Tests.TestBases;

namespace Akavache.Tests;

/// <summary>
/// Tests for DateTime operations associated with the <see cref="InMemoryBlobCache"/> class with BSON serialization.
/// </summary>
[InheritsTests]
public class NewtonsoftBsonInMemoryBlobCacheDateTimeTests : DateTimeTestBase
{
    /// <inheritdoc />
    protected override IBlobCache CreateBlobCache(string path, ISerializer serializer) => new InMemoryBlobCache(serializer);
}
