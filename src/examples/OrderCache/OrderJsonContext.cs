// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace OrderCache;

/// <summary>Provides generated JSON metadata for cached orders.</summary>
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(bool))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class OrderJsonContext : JsonSerializerContext;
