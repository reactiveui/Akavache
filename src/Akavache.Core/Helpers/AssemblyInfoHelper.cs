// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Akavache.Core.Helpers;

/// <summary>
/// Reflection-based helpers that discover the currently executing assembly and
/// read metadata from it (short name, <see cref="AssemblyFileVersionAttribute"/>).
/// </summary>
/// <remarks>
/// Marked <see cref="ObsoleteAttribute"/> because reflection-based discovery does
/// not always yield the expected assembly in trimmed or NativeAOT-published apps.
/// Prefer <c>IAkavacheBuilder.WithExecutingAssembly(Assembly)</c> with a
/// caller-owned type reference.
/// </remarks>
[Obsolete("Prefer IAkavacheBuilder.WithExecutingAssembly(Assembly) for AOT-safe explicit assembly configuration.", error: false)]
internal static class AssemblyInfoHelper
{
    /// <summary>
    /// Resolves the executing assembly via <see cref="Assembly.GetEntryAssembly"/>
    /// with a fallback to <see cref="Assembly.GetExecutingAssembly"/>.
    /// </summary>
    /// <returns>The first non-null assembly of the two.</returns>
    public static Assembly ResolveExecutingAssembly() =>
        ResolveExecutingAssembly(Assembly.GetEntryAssembly, Assembly.GetExecutingAssembly);

    /// <summary>
    /// Resolves the executing assembly using caller-supplied factories.
    /// </summary>
    /// <remarks>
    /// Accepting explicit factories lets unit tests cover both the
    /// entry-assembly-present and entry-assembly-null paths of the <c>??</c>
    /// fallback.
    /// </remarks>
    /// <param name="getEntryAssembly">Factory that returns the entry assembly, or <see langword="null"/> if none.</param>
    /// <param name="getExecutingAssembly">Factory that returns the executing assembly.</param>
    /// <returns>The first non-null assembly.</returns>
    public static Assembly ResolveExecutingAssembly(Func<Assembly?> getEntryAssembly, Func<Assembly> getExecutingAssembly) =>
        getEntryAssembly() ?? getExecutingAssembly();

    /// <summary>
    /// Extracts the short assembly name from an assembly's fully-qualified name.
    /// </summary>
    /// <remarks>
    /// Parses full names of the form <c>MyAssembly, Version=..., Culture=..., PublicKeyToken=...</c>
    /// by returning the portion before the first comma. Returns <see langword="null"/>
    /// when <see cref="Assembly.FullName"/> itself is <see langword="null"/>.
    /// </remarks>
    /// <param name="assembly">The assembly whose short name to extract.</param>
    /// <returns>The short assembly name, or <see langword="null"/>.</returns>
    public static string? ExtractAssemblyName(Assembly assembly)
    {
        var fullName = assembly.FullName;
        return fullName?.Split(',')[0];
    }

    /// <summary>
    /// Reads and parses the <see cref="AssemblyFileVersionAttribute"/> from an
    /// assembly into a <see cref="System.Version"/>.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if the attribute is missing, or if its value
    /// fails <see cref="System.Version.TryParse(string, out System.Version)"/>.
    /// </remarks>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The parsed version, or <see langword="null"/>.</returns>
    public static Version? ExtractAssemblyVersion(Assembly assembly)
    {
        var versionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        if (versionAttr is null)
        {
            return null;
        }

        return Version.TryParse(versionAttr.Version, out var version) ? version : null;
    }
}
