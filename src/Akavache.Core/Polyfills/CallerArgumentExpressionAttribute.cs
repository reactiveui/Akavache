// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Polyfill implementation adapted from Simon Cropp's Polyfill library
// https://github.com/SimonCropp/Polyfill
#if !NET

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates that a parameter captures the expression passed for another parameter
/// as a string.
/// </summary>
/// <remarks>
/// Link: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callerargumentexpressionattribute.
/// </remarks>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute"/>
    /// class with the name of the target parameter.
    /// </summary>
    /// <param name="parameterName">The name of the parameter whose expression should be captured.</param>
    public CallerArgumentExpressionAttribute(string parameterName) =>
        ParameterName = parameterName;

    /// <summary>
    /// Gets the name of the parameter whose expression should be captured.
    /// </summary>
    public string ParameterName { get; }
}

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute))]
#endif
