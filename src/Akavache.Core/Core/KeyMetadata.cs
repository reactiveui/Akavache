// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Per-type reflection-string cache used by <see cref="UniversalSerializer"/>'s key-candidate
/// search. Each string is materialised exactly once per closed generic instantiation and then
/// served directly from the static fields. Avoids walking
/// <see cref="Type.FullName"/>, the short type name, and the assembly simple name on every
/// cache lookup.
/// </summary>
/// <typeparam name="T">The value type whose reflection strings are being cached.</typeparam>
internal static class KeyMetadata<T>
{
    /// <summary>Cached <c>typeof(T).FullName</c> (or <c>typeof(T).Name</c> when <see cref="Type.FullName"/> is null).</summary>
    public static readonly string FullName = typeof(T).FullName ?? typeof(T).Name;

    /// <summary>Cached <c>typeof(T).Name</c>.</summary>
    public static readonly string Name = typeof(T).Name;

    /// <summary>Cached <c>Assembly.Name + '.' + typeof(T).Name</c>, matching the original
    /// third-form prefix built by <see cref="UniversalSerializer"/>. If the assembly name is
    /// null it collapses to just the short type name.</summary>
    public static readonly string AssemblyQualifiedShortName =
        typeof(T).Assembly.GetName().Name is { } asmName
            ? asmName + "." + typeof(T).Name
            : typeof(T).Name;
}
