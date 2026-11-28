// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace AkavacheTodoWpf.Models;

/// <summary>Describes the data the WPF application persists in the user cache.</summary>
[JsonSourceGenerationOptions(IgnoreReadOnlyProperties = true)]
[JsonSerializable(typeof(List<TodoItem>))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(DateTimeOffset))]
internal sealed partial class TodoJsonContext : JsonSerializerContext;
