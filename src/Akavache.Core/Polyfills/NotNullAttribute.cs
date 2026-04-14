// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Polyfill implementation adapted from Simon Cropp's Polyfill library
// https://github.com/SimonCropp/Polyfill
#if !NET

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that an output will not be null even if the corresponding type allows it.
/// Specifies that an input argument was not null when the call returns.
/// </summary>
/// <remarks>
/// Link: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullattribute.
/// </remarks>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(
    AttributeTargets.Field |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.ReturnValue,
    Inherited = false)]
internal sealed class NotNullAttribute : Attribute;

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.NotNullAttribute))]
#endif
