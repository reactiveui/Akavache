// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Polyfill for <c>MemberNotNullAttribute</c> which is only available on .NET 5+.</summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[SuppressMessage(
    "Design",
    "SST2324:Do not declare a member more accessible than its containing type",
    Justification = "Mirrors the shape of the corresponding BCL type (System.Diagnostics.CodeAnalysis.MemberNotNullAttribute); the polyfill compiles only where the BCL lacks it.")]
internal sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class.</summary>
    /// <param name="member">The member that is not null after the method returns.</param>
    public MemberNotNullAttribute(string member) => Members = [member];

    /// <summary>Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class.</summary>
    /// <param name="members">The members that are not null after the method returns.</param>
    public MemberNotNullAttribute(params string[] members) => Members = members;

    /// <summary>Gets the members that are not null after the method returns.</summary>
    public string[] Members { get; }
}

#else
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Diagnostics.CodeAnalysis.MemberNotNullAttribute))]
#endif
