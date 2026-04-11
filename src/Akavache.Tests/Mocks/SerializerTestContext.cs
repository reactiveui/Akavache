// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
