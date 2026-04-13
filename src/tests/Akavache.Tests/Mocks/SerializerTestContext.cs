// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Akavache.Tests.Mocks;

/// <summary>
/// Source-generated JSON serializer context for AOT testing.
/// </summary>
[JsonSerializable(typeof(SerializerTestModel))]
public partial class SerializerTestContext : JsonSerializerContext
{
}
