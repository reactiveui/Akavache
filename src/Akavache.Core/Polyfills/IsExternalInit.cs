// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Polyfill that enables the C# 9+ `init`-only setter (and therefore record types) on
// target frameworks that don't ship this marker type.
#if !NET && !NETSTANDARD2_1_OR_GREATER

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Marker type recognized by the C# compiler to enable <c>init</c> setters and records
/// on frameworks that do not ship <see cref="IsExternalInit"/>.
/// </summary>
/// <remarks>
/// Link: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.isexternalinit.
/// </remarks>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit;

#endif
