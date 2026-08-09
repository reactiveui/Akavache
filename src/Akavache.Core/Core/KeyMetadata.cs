// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

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
    /// <summary>Cached <c>typeof(T).Name</c>.</summary>
    internal static readonly string Name = typeof(T).Name;

    /// <summary>Cached <c>typeof(T).FullName</c> (or <c>typeof(T).Name</c> when <see cref="Type.FullName"/> is null).</summary>
    internal static readonly string FullName = KeyMetadata.BuildFullName(typeof(T));

    /// <summary>Cached <c>Assembly.Name + '.' + typeof(T).Name</c>, matching the original
    /// third-form prefix built by <see cref="UniversalSerializer"/>. If the assembly name is
    /// null it collapses to just the short type name.</summary>
    internal static readonly string AssemblyQualifiedShortName = KeyMetadata.BuildAssemblyQualifiedShortName(typeof(T));
}
