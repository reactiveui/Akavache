// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for <see cref="KeyMetadata"/> / <see cref="KeyMetadata{T}"/>. The static generic class
/// materialises its reflection strings exactly once per closed type, so exercising the
/// null-fallback branches requires the non-generic <see cref="KeyMetadata"/> companion's
/// <c>Build*</c> helpers that take an explicit <see cref="Type"/>.
/// </summary>
[Category("Akavache")]
public class KeyMetadataTests
{
    /// <summary>
    /// Verifies <see cref="KeyMetadata.BuildFullName"/> returns <see cref="Type.FullName"/>
    /// verbatim for an ordinary concrete type whose full name is non-null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildFullNameShouldReturnFullNameForConcreteType() =>
        await Assert.That(KeyMetadata.BuildFullName(typeof(string))).IsEqualTo("System.String");

    /// <summary>
    /// Verifies <see cref="KeyMetadata.BuildFullName"/> falls back to the short type name
    /// when <see cref="Type.FullName"/> is null. Generic type parameters reflected from an open
    /// generic definition are the standard example: their <c>FullName</c> is null by design.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildFullNameShouldFallBackToNameWhenFullNameIsNull()
    {
        // typeof(List<>).GetGenericArguments()[0] is the generic parameter "T" — the runtime
        // reports FullName = null, Name = "T" for it, which drives the ?? fallback branch.
        var genericParameter = typeof(List<>).GetGenericArguments()[0];

        await Assert.That(genericParameter.FullName).IsNull();
        await Assert.That(KeyMetadata.BuildFullName(genericParameter)).IsEqualTo("T");
    }

    /// <summary>
    /// Verifies <see cref="KeyMetadata.BuildAssemblyQualifiedShortName"/> produces the
    /// <c>Assembly.Name + "." + Type.Name</c> form for an ordinary runtime type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildAssemblyQualifiedShortNameShouldCombineAssemblyAndTypeName()
    {
        var result = KeyMetadata.BuildAssemblyQualifiedShortName(typeof(string));

        // System.String lives in the core library — the exact assembly simple name varies
        // across runtimes ("System.Private.CoreLib", "mscorlib") but it is always non-null
        // and always ends with ".String" here.
        await Assert.That(result).EndsWith(".String");
        await Assert.That(result).DoesNotStartWith(".");
    }

    /// <summary>
    /// Verifies <see cref="KeyMetadata.BuildAssemblyQualifiedShortName"/> collapses to just the
    /// short type name when the declaring assembly has no simple name. Exercised via a shim
    /// <see cref="Type"/> whose Assembly reports a null <see cref="AssemblyName.Name"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildAssemblyQualifiedShortNameShouldFallBackWhenAssemblyNameIsNull()
    {
        // We can't easily construct a Type whose Assembly.GetName().Name is truly null from
        // user code, but we can exercise the equivalent fallback path by asking BuildShortName
        // on a type whose assembly simple name is empty/whitespace-ish via a dynamic assembly.
        var asmName = new AssemblyName("dyn-asm-for-keymetadata-tests");
        var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var module = asm.DefineDynamicModule("m");
        var typeBuilder = module.DefineType("DynType", TypeAttributes.Public);
        var dynType = typeBuilder.CreateType()!;

        // Sanity: the dynamic assembly's simple name is non-null here — so this test drives
        // the true branch of the pattern match, confirming it produces the concatenated form.
        var result = KeyMetadata.BuildAssemblyQualifiedShortName(dynType);
        await Assert.That(result).IsEqualTo("dyn-asm-for-keymetadata-tests.DynType");

        // And now the null-simple-name fallback: call BuildAssemblyQualifiedShortName on a
        // shim type via a custom Type subclass that reports a null assembly simple name.
        var shimType = new NullAssemblyNameShimType("ShimmedType");
        var shimResult = KeyMetadata.BuildAssemblyQualifiedShortName(shimType);
        await Assert.That(shimResult).IsEqualTo("ShimmedType");
    }

    /// <summary>
    /// Verifies the static <see cref="KeyMetadata{T}"/> cache fields are populated exactly once
    /// per closed generic and line up with the <see cref="KeyMetadata"/> helper outputs for the
    /// same type.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task KeyMetadataGenericFieldsShouldMatchHelperOutputs()
    {
        await Assert.That(KeyMetadata<string>.FullName).IsEqualTo(KeyMetadata.BuildFullName(typeof(string)));
        await Assert.That(KeyMetadata<string>.Name).IsEqualTo(nameof(String));
        await Assert.That(KeyMetadata<string>.AssemblyQualifiedShortName).IsEqualTo(KeyMetadata.BuildAssemblyQualifiedShortName(typeof(string)));
    }

    /// <summary>
    /// Minimal <see cref="Type"/> subclass whose <see cref="Assembly"/> reports an
    /// <see cref="AssemblyName.Name"/> of <see langword="null"/> — the only way to reach the
    /// second branch of <see cref="KeyMetadata.BuildAssemblyQualifiedShortName"/> from managed
    /// code without reflecting a distro-specific emitted type.
    /// </summary>
    private sealed class NullAssemblyNameShimType(string name) : TypeDelegator(typeof(object))
    {
        /// <inheritdoc/>
        public override string Name => name;

        /// <inheritdoc/>
        public override Assembly Assembly { get; } = new NullNameAssemblyShim();

        /// <summary>Dynamic assembly shim whose <see cref="AssemblyName.Name"/> is null.</summary>
        private sealed class NullNameAssemblyShim : Assembly
        {
            /// <inheritdoc/>
            public override AssemblyName GetName() => new() { Name = null };

            /// <inheritdoc/>
            public override AssemblyName GetName(bool copiedName) => GetName();
        }
    }
}
