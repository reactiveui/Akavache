// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core.Helpers;

namespace Akavache.Tests;

/// <summary>
/// Tests for the obsolete <see cref="AssemblyInfoHelper"/>. The helper is retained as
/// a legacy compatibility shim for reflection-based assembly discovery — Akavache's
/// builder no longer uses it by default (callers opt into the AOT-safe
/// <c>WithExecutingAssembly</c> path) but the helper methods themselves still need
/// coverage because the code still ships.
/// </summary>
[Category("Akavache")]
#pragma warning disable CS0618 // Type or member is obsolete — deliberately testing the obsolete surface.
public class AssemblyInfoHelperTests
{
    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ResolveExecutingAssembly()"/> returns a
    /// non-null assembly when the runtime has an entry assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveExecutingAssemblyDefaultShouldReturnNonNull()
    {
        var assembly = AssemblyInfoHelper.ResolveExecutingAssembly();

        await Assert.That(assembly).IsNotNull();
    }

    /// <summary>
    /// Tests the testable overload returns the entry assembly when the factory yields
    /// a non-null value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveExecutingAssemblyShouldReturnEntryAssemblyWhenPresent()
    {
        var entry = typeof(AssemblyInfoHelperTests).Assembly;
        var executing = typeof(AssemblyInfoHelper).Assembly;

        var result = AssemblyInfoHelper.ResolveExecutingAssembly(() => entry, () => executing);

        await Assert.That(result).IsSameReferenceAs(entry);
    }

    /// <summary>
    /// Tests the testable overload falls back to the executing assembly when the entry
    /// factory returns <see langword="null"/>. This is the branch that is unreachable
    /// in test-host environments with the default factories, so we exercise it via the
    /// explicit-factory overload.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ResolveExecutingAssemblyShouldFallBackToExecutingWhenEntryNull()
    {
        var executing = typeof(AssemblyInfoHelper).Assembly;

        var result = AssemblyInfoHelper.ResolveExecutingAssembly(() => null, () => executing);

        await Assert.That(result).IsSameReferenceAs(executing);
    }

    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ExtractAssemblyName"/> returns the short
    /// assembly name for a normal assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtractAssemblyNameShouldReturnShortName()
    {
        var assembly = typeof(AssemblyInfoHelperTests).Assembly;
        var expected = assembly.FullName!.Split(',')[0];

        var result = AssemblyInfoHelper.ExtractAssemblyName(assembly);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ExtractAssemblyName"/> returns
    /// <see langword="null"/> when the assembly has no full name.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtractAssemblyNameShouldReturnNullWhenFullNameMissing()
    {
        NullFullNameAssembly stub = new();

        var result = AssemblyInfoHelper.ExtractAssemblyName(stub);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ExtractAssemblyVersion"/> returns a non-null
    /// <see cref="Version"/> for an assembly that carries an
    /// <see cref="AssemblyFileVersionAttribute"/> with a parseable value.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtractAssemblyVersionShouldReturnVersionWhenPresent()
    {
        var assembly = typeof(AssemblyInfoHelperTests).Assembly;

        var result = AssemblyInfoHelper.ExtractAssemblyVersion(assembly);

        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ExtractAssemblyVersion"/> returns
    /// <see langword="null"/> when the assembly has no
    /// <see cref="AssemblyFileVersionAttribute"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtractAssemblyVersionShouldReturnNullWhenAttributeMissing()
    {
        NoFileVersionAttributeAssembly stub = new();

        var result = AssemblyInfoHelper.ExtractAssemblyVersion(stub);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="AssemblyInfoHelper.ExtractAssemblyVersion"/> returns
    /// <see langword="null"/> when the attribute is present but its value is not
    /// parseable as a <see cref="Version"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExtractAssemblyVersionShouldReturnNullWhenValueUnparseable()
    {
        UnparseableFileVersionAssembly stub = new();

        var result = AssemblyInfoHelper.ExtractAssemblyVersion(stub);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Minimal stub <see cref="System.Reflection.Assembly"/> whose <see cref="Assembly.FullName"/> is
    /// <see langword="null"/>, used to exercise the null-full-name branch of
    /// <see cref="AssemblyInfoHelper.ExtractAssemblyName"/>.
    /// </summary>
    private sealed class NullFullNameAssembly : Assembly
    {
        /// <inheritdoc/>
        public override string? FullName => null;
    }

    /// <summary>
    /// Minimal stub <see cref="System.Reflection.Assembly"/> that reports no
    /// <see cref="AssemblyFileVersionAttribute"/>. The overridden attribute methods
    /// must return an <see cref="Attribute"/>[] (not a plain <see cref="object"/>[])
    /// because <see cref="CustomAttributeExtensions.GetCustomAttribute{T}(Assembly)"/>
    /// casts the result to <c>Attribute[]</c>.
    /// </summary>
    private sealed class NoFileVersionAttributeAssembly : Assembly
    {
        /// <inheritdoc/>
        public override string? FullName => "NoVersion";

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => [];
    }

    /// <summary>
    /// Minimal stub <see cref="System.Reflection.Assembly"/> that reports an
    /// <see cref="AssemblyFileVersionAttribute"/> whose value cannot be parsed.
    /// </summary>
    private sealed class UnparseableFileVersionAssembly : Assembly
    {
        /// <summary>
        /// A field of the file version assembly attributes.
        /// </summary>
        private static readonly Attribute[] _attrs = [new AssemblyFileVersionAttribute("not-a-version")];

        /// <inheritdoc/>
        public override string? FullName => "UnparseableVersion";

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            attributeType == typeof(AssemblyFileVersionAttribute) ? _attrs : [];

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => _attrs;
    }
}
#pragma warning restore CS0618
