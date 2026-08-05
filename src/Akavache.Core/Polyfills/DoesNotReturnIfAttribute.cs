// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// Polyfill implementation adapted from Simon Cropp's Polyfill library
// https://github.com/SimonCropp/Polyfill
#if !NET

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that the method will not return if the associated <see cref="bool"/> parameter is passed the specified value.</summary>
/// <remarks>
/// Link: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.doesnotreturnifattribute.
/// </remarks>
[SuppressMessage(
    "Design",
    "SST2324:Do not declare a member more accessible than its containing type",
    Justification = "Mirrors the shape of the corresponding BCL type (System.DoesNotReturnIfAttribute); the polyfill compiles only where the BCL lacks it.")]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DoesNotReturnIfAttribute"/> class with the specified parameter value.</summary>
    /// <param name="parameterValue">
    /// The condition parameter value. Code after the method is considered unreachable by
    /// diagnostics if the argument to the associated parameter matches this value.
    /// </param>
    public DoesNotReturnIfAttribute(bool parameterValue) =>
        ParameterValue = parameterValue;

    /// <summary>
    /// Gets a value indicating whether code after the method is considered unreachable
    /// by diagnostics if the argument to the associated parameter matches this value.
    /// </summary>
    public bool ParameterValue { get; }
}

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute))]
#endif
