// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Non-generic companion holding the pure per-<see cref="Type"/> reflection logic used by
/// <see cref="KeyMetadata{T}"/>. Exposed as internal statics so both branches of the null-fallback
/// logic (null <see cref="Type.FullName"/> and null assembly simple name) can be exercised
/// directly against dynamically-constructed types in unit tests.
/// </summary>
internal static class KeyMetadata
{
    /// <summary>
    /// Returns <paramref name="type"/>'s <see cref="Type.FullName"/>, falling back to
    /// the short type name for types where the runtime reports a null full name
    /// (e.g. generic type parameters, certain emitted types).
    /// </summary>
    /// <param name="type">The type to probe.</param>
    /// <returns>The full name, or short name when the full name is null.</returns>
    internal static string BuildFullName(Type type) => type.FullName ?? type.Name;

    /// <summary>
    /// Returns <c><paramref name="type"/>.Assembly.GetName().Name + "." + <paramref name="type"/>.Name</c>,
    /// collapsing to just the short type name when the assembly's simple name is null
    /// (e.g. types defined in a dynamically-emitted assembly with a nameless manifest).
    /// </summary>
    /// <param name="type">The type to probe.</param>
    /// <returns>The assembly-qualified short name, or just the short name when the assembly has no simple name.</returns>
    internal static string BuildAssemblyQualifiedShortName(Type type) =>
        type.Assembly.GetName().Name is { } asmName
            ? asmName + "." + type.Name
            : type.Name;
}
