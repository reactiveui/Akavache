// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Mocks;
#else
namespace Akavache.Tests.Mocks;
#endif

/// <summary>Source-generated JSON serializer context for AOT testing.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[JsonSerializable(typeof(SerializerTestModel))]
public partial class SerializerTestContext : JsonSerializerContext;
