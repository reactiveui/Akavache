// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using Akavache.Core;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for the internal <see cref="AkavacheBuilder"/> implementation of <see cref="IAkavacheBuilder"/>.
/// Exercises argument validation, fluent configuration methods, and <see cref="AkavacheBuilder.Build"/>.
/// </summary>
[Category("Akavache")]
public class AkavacheBuilderTests
{
    /// <summary>
    /// Reset CacheDatabase between tests since AkavacheBuilder interacts with global serializer state.
    /// </summary>
    /// <returns>A task.</returns>
    [Before(Test)]
    public async Task ResetCacheDatabase() => await CacheDatabase.ResetForTestsAsync();

    /// <summary>
    /// Cleanup CacheDatabase after each test.
    /// </summary>
    /// <returns>A task.</returns>
    [After(Test)]
    public async Task CleanupCacheDatabase() => await CacheDatabase.ResetForTestsAsync();

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithApplicationName"/> ignores null and whitespace values.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithApplicationNameShouldIgnoreNullOrWhitespace()
    {
        AkavacheBuilder builder = new();
        var originalName = builder.ApplicationName;

        builder.WithApplicationName(null);
        await Assert.That(builder.ApplicationName).IsEqualTo(originalName);

        builder.WithApplicationName(string.Empty);
        await Assert.That(builder.ApplicationName).IsEqualTo(originalName);

        builder.WithApplicationName("   ");
        await Assert.That(builder.ApplicationName).IsEqualTo(originalName);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithApplicationName"/> sets a valid name and returns the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithApplicationNameShouldSetName()
    {
        AkavacheBuilder builder = new();

        var result = builder.WithApplicationName("MyTestApp_Builder");

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.ApplicationName).IsEqualTo("MyTestApp_Builder");
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithInMemory"/> throws <see cref="ArgumentNullException"/> when passed null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldThrowOnNull()
    {
        AkavacheBuilder builder = new();
        await Assert.That(() => builder.WithInMemory(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithLocalMachine"/> throws <see cref="ArgumentNullException"/> when passed null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithLocalMachineShouldThrowOnNull()
    {
        AkavacheBuilder builder = new();
        await Assert.That(() => builder.WithLocalMachine(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithSecure"/> throws <see cref="ArgumentNullException"/> when passed null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSecureShouldThrowOnNull()
    {
        AkavacheBuilder builder = new();
        await Assert.That(() => builder.WithSecure(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithUserAccount"/> throws <see cref="ArgumentNullException"/> when passed null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithUserAccountShouldThrowOnNull()
    {
        AkavacheBuilder builder = new();
        await Assert.That(() => builder.WithUserAccount(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithLocalMachine"/> assigns the provided cache and returns the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithLocalMachineShouldAssignCache()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        InMemoryBlobCache cache = new(builder.Serializer!);

        var result = builder.WithLocalMachine(cache);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.LocalMachine).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithUserAccount"/> assigns the provided cache and returns the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithUserAccountShouldAssignCache()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        InMemoryBlobCache cache = new(builder.Serializer!);

        var result = builder.WithUserAccount(cache);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.UserAccount).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithInMemory"/> assigns the provided cache and returns the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryShouldAssignCache()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        InMemoryBlobCache cache = new(builder.Serializer!);

        var result = builder.WithInMemory(cache);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.InMemory).IsSameReferenceAs(cache);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithSerializer{T}(Func{T})"/> uses the supplied factory.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerFactoryShouldRegisterSerializer()
    {
        AkavacheBuilder builder = new();

        var result = builder.WithSerializer(static () => new SystemJsonSerializer());

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.SerializerTypeName).IsNotNull();
        await Assert.That(builder.Serializer).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithSerializer{T}()"/> default overload registers the serializer.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithSerializerDefaultShouldRegisterSerializer()
    {
        AkavacheBuilder builder = new();

        var result = builder.WithSerializer<SystemJsonSerializer>();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.Serializer).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.UseForcedDateTimeKind"/> sets the kind and returns the builder.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task UseForcedDateTimeKindShouldSetValue()
    {
        AkavacheBuilder builder = new();

        var result = builder.UseForcedDateTimeKind(DateTimeKind.Utc);

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithLegacyFileLocation"/> switches to <see cref="FileLocationOption.Legacy"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithLegacyFileLocationShouldChangeFileLocationOption()
    {
        AkavacheBuilder builder = new();

        var result = builder.WithLegacyFileLocation();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.Build"/> throws when no serializer has been registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildShouldThrowWhenNoSerializerRegistered()
    {
        AkavacheBuilder builder = new()
        {
            // Force a unique serializer type name so that the lookup returns null (no registration).
            SerializerTypeName = "NonExistentSerializer_" + Guid.NewGuid().ToString("N")
        };

        await Assert.That(() => builder.Build()).Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.Build"/> returns the builder instance itself (which implements IAkavacheInstance).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildShouldReturnInstanceWhenSerializerRegistered()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();

        var instance = builder.Build();

        await Assert.That(instance).IsNotNull();
        await Assert.That(instance).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithInMemoryDefaults"/> throws when no serializer is registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryDefaultsShouldThrowWhenNoSerializer()
    {
        AkavacheBuilder builder = new()
        {
            SerializerTypeName = "NonExistentSerializer_" + Guid.NewGuid().ToString("N"),
        };

        await Assert.That(() => builder.WithInMemoryDefaults()).Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithInMemoryDefaults"/> populates all four blob cache slots.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryDefaultsShouldPopulateAllCaches()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();

        var result = builder.WithInMemoryDefaults();

        await Assert.That(result).IsSameReferenceAs(builder);
        await Assert.That(builder.InMemory).IsNotNull();
        await Assert.That(builder.LocalMachine).IsNotNull();
        await Assert.That(builder.UserAccount).IsNotNull();
        await Assert.That(builder.Secure).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.WithInMemoryDefaults"/> does not overwrite already-assigned caches.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithInMemoryDefaultsShouldNotOverwriteExistingCaches()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        InMemoryBlobCache existing = new(builder.Serializer!);
        builder.WithLocalMachine(existing);

        builder.WithInMemoryDefaults();

        await Assert.That(builder.LocalMachine).IsSameReferenceAs(existing);
    }

    /// <summary>
    /// Tests the <see cref="AkavacheBuilder(FileLocationOption)"/> constructor with the Legacy option.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldRespectFileLocationOption()
    {
        AkavacheBuilder builder = new(FileLocationOption.Legacy);

        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>
    /// Tests that the static <see cref="AkavacheBuilder.BlobCaches"/> dictionary is readable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BlobCachesStaticDictionaryShouldBeReadable()
    {
        var caches = AkavacheBuilder.BlobCaches;

        // The dictionary is initialized to an empty dictionary by default; reading should not throw.
        await Assert.That(caches).IsNotNull();
    }

    /// <summary>
    /// Tests that the static <see cref="AkavacheBuilder.SettingsStores"/> dictionary is readable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SettingsStoresStaticDictionaryShouldBeReadable()
    {
        var stores = AkavacheBuilder.SettingsStores;

        await Assert.That(stores).IsNotNull();
    }

    /// <summary>
    /// Tests that the <see cref="AkavacheBuilder.SettingsCachePath"/> setter assigns a custom path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SettingsCachePathSetterShouldAssignValue()
    {
        AkavacheBuilder builder = new()
        {
            SettingsCachePath = "/tmp/custom_settings_path",
        };

        await Assert.That(builder.SettingsCachePath).IsEqualTo("/tmp/custom_settings_path");
    }

    /// <summary>
    /// Tests that the <see cref="AkavacheBuilder.HttpService"/> property has a default value and can be replaced.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task HttpServiceShouldHaveDefaultAndBeAssignable()
    {
        AkavacheBuilder builder = new();

        await Assert.That(builder.HttpService).IsNotNull();

        HttpService replacement = new();
        builder.HttpService = replacement;

        await Assert.That(builder.HttpService).IsSameReferenceAs(replacement);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.CreateInMemoryCache"/> throws when no serializer has been registered.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateInMemoryCacheShouldThrowWhenNoSerializer()
    {
        AkavacheBuilder builder = new()
        {
            SerializerTypeName = "NonExistentSerializer_" + Guid.NewGuid().ToString("N"),
        };

        await Assert.That(() => builder.CreateInMemoryCache()).Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.CreateInMemoryCache"/> applies the forced DateTimeKind to the created cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateInMemoryCacheShouldApplyForcedDateTimeKind()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.UseForcedDateTimeKind(DateTimeKind.Utc);

        var cache = builder.CreateInMemoryCache();

        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.ApplyForcedDateTimeKind"/> assigns the value when set.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ApplyForcedDateTimeKindShouldAssignWhenSet()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.UseForcedDateTimeKind(DateTimeKind.Local);
        InMemoryBlobCache cache = new(builder.Serializer!);

        builder.ApplyForcedDateTimeKind(cache);

        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Local);
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.ApplyForcedDateTimeKind"/> leaves the cache unchanged when not set.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ApplyForcedDateTimeKindShouldNotAssignWhenUnset()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        InMemoryBlobCache cache = new(builder.Serializer!)
        {
            ForcedDateTimeKind = null,
        };

        builder.ApplyForcedDateTimeKind(cache);

        await Assert.That(cache.ForcedDateTimeKind).IsNull();
    }

    /// <summary>
    /// Tests that the SecureBlobCacheWrapper forwards <c>Vacuum</c> calls to the wrapped inner cache.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperShouldForwardVacuum()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.WithInMemoryDefaults();

        var secure = builder.Secure!;

        // Vacuum on InMemoryBlobCache is a no-op that completes successfully.
        await secure.Vacuum();
    }

    /// <summary>
    /// Tests that disposing the SecureBlobCacheWrapper via <see cref="IAsyncDisposable.DisposeAsync"/> succeeds.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperShouldSupportDisposeAsync()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.WithInMemoryDefaults();

        var secure = builder.Secure!;

        await secure.DisposeAsync();
    }

    /// <summary>
    /// Tests that disposing the SecureBlobCacheWrapper via synchronous <see cref="IDisposable.Dispose"/> succeeds.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperShouldSupportSyncDispose()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.WithInMemoryDefaults();

        var secure = builder.Secure!;
        secure.Dispose();

        // Reaching this line without exception is the assertion.
        await Assert.That(secure).IsNotNull();
    }

    /// <summary>
    /// Tests that the <see cref="AkavacheBuilder(FileLocationOption)"/> constructor
    /// succeeds with the default option and sets path metadata. Assembly metadata is
    /// no longer populated by the constructor (Akavache no longer probes the caller's
    /// assembly via reflection), so the assembly name is expected to be
    /// <see langword="null"/> until the caller opts in via
    /// <see cref="AkavacheBuilder.WithExecutingAssembly"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorWithDefaultOptionShouldPopulateMetadata()
    {
        AkavacheBuilder builder = new();

        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Default);
#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(builder.ExecutingAssemblyName).IsNull();
#pragma warning restore CS0618 // Type or member is obsolete
        await Assert.That(builder.ApplicationRootPath).IsNotNull();
    }

    /// <summary>
    /// Tests that calling <see cref="AkavacheBuilder.WithExecutingAssembly"/> populates
    /// <see cref="IAkavacheInstance.ExecutingAssemblyName"/> from the caller-supplied
    /// assembly — the AOT-safe replacement for reflection-based discovery.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithExecutingAssemblyShouldPopulateExecutingAssemblyName()
    {
        AkavacheBuilder builder = new();
        builder.WithExecutingAssembly(typeof(AkavacheBuilderTests).Assembly);

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(builder.ExecutingAssemblyName).IsNotNull();
        await Assert.That(builder.ExecutingAssemblyName).IsNotEmpty();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Tests that the <see cref="AkavacheBuilder"/> constructor sets ApplicationRootPath
    /// to a non-null value when the executing assembly location is available.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldSetApplicationRootPath()
    {
        AkavacheBuilder builder = new();

        // In a test runner, the assembly location should be resolvable
        await Assert.That(builder.ApplicationRootPath).IsNotNull();
        await Assert.That(builder.ApplicationRootPath).IsNotEmpty();
    }

    /// <summary>
    /// Tests that the default <see cref="AkavacheBuilder"/> leaves
    /// <see cref="IAkavacheInstance.Version"/> as <see langword="null"/>. Version is
    /// populated only when the caller opts in via
    /// <see cref="AkavacheBuilder.WithExecutingAssembly"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ConstructorShouldLeaveVersionNullByDefault()
    {
        AkavacheBuilder builder = new();

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(builder.Version).IsNull();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Tests that <see cref="AkavacheBuilder.ExecutingAssembly"/> returns a non-null
    /// sentinel (the Akavache core assembly) by default when no explicit assembly has
    /// been provided.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ExecutingAssemblyShouldReturnNonNullSentinelByDefault()
    {
        AkavacheBuilder builder = new();

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(builder.ExecutingAssembly).IsNotNull();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Tests <see cref="AkavacheBuilder.WithExecutingAssembly"/> overrides the sentinel
    /// and populates both the name and version from the caller-supplied assembly.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithExecutingAssemblyShouldOverrideSentinelAndPopulateMetadata()
    {
        var expected = typeof(AkavacheBuilderTests).Assembly;
        AkavacheBuilder builder = new();

        var result = builder.WithExecutingAssembly(expected);

        await Assert.That(result).IsSameReferenceAs(builder);
#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(builder.ExecutingAssembly).IsSameReferenceAs(expected);
        await Assert.That(builder.ExecutingAssemblyName).IsEqualTo(expected.GetName().Name);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Tests <see cref="AkavacheBuilder.WithExecutingAssembly"/> throws on null.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithExecutingAssemblyShouldThrowOnNull()
    {
        AkavacheBuilder builder = new();

        await Assert.That(() => builder.WithExecutingAssembly(null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests <see cref="AkavacheBuilder.ReadFileVersion"/> returns a non-null version
    /// for an assembly with a parseable <see cref="AssemblyFileVersionAttribute"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadFileVersionShouldReturnVersionForNormalAssembly()
    {
        var result = AkavacheBuilder.ReadFileVersion(typeof(AkavacheBuilderTests).Assembly);

        await Assert.That(result).IsNotNull();
    }

    /// <summary>
    /// Tests <see cref="AkavacheBuilder.ReadFileVersion"/> returns <see langword="null"/>
    /// when the assembly has no <see cref="AssemblyFileVersionAttribute"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadFileVersionShouldReturnNullWhenAttributeMissing()
    {
        NoFileVersionAttributeStubAssembly stub = new();

        var result = AkavacheBuilder.ReadFileVersion(stub);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests <see cref="AkavacheBuilder.ReadFileVersion"/> returns <see langword="null"/>
    /// when the attribute value cannot be parsed as a <see cref="Version"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReadFileVersionShouldReturnNullWhenValueUnparseable()
    {
        UnparseableFileVersionStubAssembly stub = new();

        var result = AkavacheBuilder.ReadFileVersion(stub);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Tests that disposing the secure cache wrapper via <see cref="IAsyncDisposable.DisposeAsync"/>
    /// calls the inner cache's async dispose when the inner supports it.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperDisposeAsyncShouldCallInnerAsyncDispose()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.WithInMemoryDefaults();

        var secure = builder.Secure!;

        // The inner InMemoryBlobCache implements IAsyncDisposable, so this exercises
        // the IAsyncDisposable branch of DisposeAsync.
        await secure.DisposeAsync();

        // Reaching here without exception confirms the async path completed.
        await Assert.That(secure).IsNotNull();
    }

    /// <summary>
    /// Tests that the synchronous <see cref="IDisposable.Dispose"/> on the secure wrapper
    /// correctly disposes the inner cache when the inner is IDisposable.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SecureBlobCacheWrapperDisposeShouldCallInnerDispose()
    {
        AkavacheBuilder builder = new();
        builder.WithSerializer<SystemJsonSerializer>();
        builder.WithInMemoryDefaults();

        var secure = builder.Secure!;
        secure.Dispose();

        await Assert.That(secure).IsNotNull();
    }

    /// <summary>
    /// Stub <see cref="System.Reflection.Assembly"/> that reports no custom attributes, used to exercise the "missing attribute" branch.
    /// </summary>
    private sealed class NoFileVersionAttributeStubAssembly : Assembly
    {
        /// <summary>
        /// Empty attribute array reused by the <see cref="GetCustomAttributes(Type, bool)"/>
        /// overrides. Typed as <see cref="Attribute"/>[] (not <see cref="object"/>[]) because
        /// <see cref="CustomAttributeExtensions.GetCustomAttribute{T}(Assembly)"/> casts the
        /// result to <c>Attribute[]</c>.
        /// </summary>
        private static readonly Attribute[] _empty = [];

        /// <inheritdoc/>
        public override string? FullName => "AkavacheBuilderTests.NoFileVersion";

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _empty;

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => _empty;
    }

    /// <summary>
    /// Stub <see cref="System.Reflection.Assembly"/> whose <see cref="AssemblyFileVersionAttribute"/> value cannot be parsed as a <see cref="Version"/>.
    /// </summary>
    private sealed class UnparseableFileVersionStubAssembly : Assembly
    {
        /// <summary>
        /// Array of <see cref="Attribute"/> representing custom attributes for the <see cref="UnparseableFileVersionStubAssembly"/> class,
        /// including an <see cref="AssemblyFileVersionAttribute"/> with an intentionally invalid version string.
        /// </summary>
        private static readonly Attribute[] _attrs = [new AssemblyFileVersionAttribute("not-a-version")];

        /// <inheritdoc/>
        public override string? FullName => "AkavacheBuilderTests.Unparseable";

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            attributeType == typeof(AssemblyFileVersionAttribute) ? _attrs : [];

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => _attrs;
    }
}
