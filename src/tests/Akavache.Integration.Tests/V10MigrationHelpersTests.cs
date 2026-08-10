// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>
/// Tests the path resolution, cache construction and per-kind step building that sit behind the
/// V10 builder extensions. Each branch short-circuits to "nothing to migrate", so these pin which
/// input produces which outcome rather than only that the happy path runs.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class V10MigrationHelpersTests
{
    /// <summary>The V10 filename the UserAccount cache maps to.</summary>
    private const string UserAccountV10FileName = "userblobs.db";

    /// <summary>A cache kind that resolves to a legacy directory on every desktop platform.</summary>
    private const string UserAccount = "UserAccount";

    /// <summary>A migration step for a disabled cache kind completes without touching the file system.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildMigrationShouldCompleteImmediatelyWhenTheKindIsDisabled()
    {
        var builder = CreateBuilder();
        List<string> log = [];

        var result = V10MigrationHelpers.BuildMigration(
            builder,
            UserAccount,
            enabled: false,
            sqliteCache: null,
            builder.Serializer!,
            new(Logger: log.Add));

        _ = await result.FirstAsync();
        await Assert.That(log).IsEmpty();
    }

    /// <summary>A cache kind whose V11 destination is not SQLite-backed has nothing to migrate into.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildMigrationShouldCompleteImmediatelyWhenTheDestinationIsNotSqlite()
    {
        var builder = CreateBuilder();
        List<string> log = [];

        var result = V10MigrationHelpers.BuildMigration(
            builder,
            UserAccount,
            enabled: true,
            sqliteCache: null,
            builder.Serializer!,
            new(Logger: log.Add));

        _ = await result.FirstAsync();
        await Assert.That(log).IsEmpty();
    }

    /// <summary>
    /// An enabled kind with a SQLite destination resolves the V10 path and hands off to the
    /// migration, which reports the absent database rather than failing.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task BuildMigrationShouldReportAnAbsentV10DatabaseForAnEnabledKind()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var builder = CreateBuilder();
        using SqliteBlobCache destination = new(Path.Combine(path, "v11.db"), new SystemJsonSerializer(), ImmediateSequencer.Instance);
        List<string> log = [];

        var result = V10MigrationHelpers.BuildMigration(
            builder,
            UserAccount,
            enabled: true,
            destination,
            builder.Serializer!,
            new(Logger: log.Add));

        _ = await result.FirstAsync();

        await Assert.That(log).HasSingleItem();
        await Assert.That(log[0]).Contains("V10 database not found");
    }

    /// <summary>The V10 database path is the legacy directory joined with the kind's V10 filename.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetV10DatabasePathShouldUseTheLegacyFileNameForTheKind()
    {
        var builder = CreateBuilder();

        var result = V10MigrationHelpers.GetV10DatabasePath(builder, UserAccount);

        await Assert.That(result).IsNotNull();
        await Assert.That(Path.GetFileName(result!)).IsEqualTo(UserAccountV10FileName);
    }

    /// <summary>A cache built for a V10 kind points at the V10 filename and creates its directory.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateV10CacheShouldOpenTheLegacyFileAndCreateItsDirectory()
    {
        var builder = CreateBuilder();
        var expectedPath = V10MigrationHelpers.GetV10DatabasePath(builder, UserAccount)!;

        using var cache = V10MigrationHelpers.CreateV10Cache(UserAccount, builder);

        await Assert.That(cache).IsNotNull();
        await Assert.That(Directory.Exists(Path.GetDirectoryName(expectedPath))).IsTrue();
    }

    /// <summary>A forced DateTimeKind on the builder is carried onto the cache it creates.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateV10CacheShouldCarryTheForcedDateTimeKind()
    {
        var builder = CreateBuilder();
        _ = builder.UseForcedDateTimeKind(DateTimeKind.Utc);

        using var cache = V10MigrationHelpers.CreateV10Cache(UserAccount, builder);

        await Assert.That(cache.ForcedDateTimeKind).IsEqualTo(DateTimeKind.Utc);
    }

    /// <summary>A serializer type the locator does not know about is reported rather than dereferenced.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task CreateV10CacheShouldThrowWhenTheSerializerIsNotRegistered()
    {
        var builder = CreateBuilder();
        ((AkavacheBuilder)builder).SerializerTypeName = "Akavache.Tests.NoSuchSerializer, Akavache.Tests.NoSuchAssembly";

        await Assert.That(() => V10MigrationHelpers.CreateV10Cache(UserAccount, builder))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("is registered in the service locator");
    }

    /// <summary>
    /// A wrapped secure cache yields the cache it wraps, not the wrapper. The wrapper type is
    /// compiled into several assemblies, so it is reached through the builder that produces it
    /// rather than named here.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetUnderlyingBlobCacheShouldUnwrapAWrappedCache()
    {
        var builder = CreateBuilder();
        _ = builder.WithV10FileNames();

        var result = V10MigrationHelpers.GetUnderlyingBlobCache(builder.Secure);

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotSameReferenceAs(builder.Secure);
        await Assert.That(result).IsTypeOf<SqliteBlobCache>();
    }

    /// <summary>A secure cache that is already a blob cache is returned as-is.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetUnderlyingBlobCacheShouldReturnAnUnwrappedCacheUnchanged()
    {
        using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, new SystemJsonSerializer());

        var result = V10MigrationHelpers.GetUnderlyingBlobCache(cache);

        await Assert.That(result).IsSameReferenceAs(cache);
    }

    /// <summary>No secure cache means there is nothing to migrate into.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task GetUnderlyingBlobCacheShouldReturnNullForNoCache()
    {
        var result = V10MigrationHelpers.GetUnderlyingBlobCache(null);

        await Assert.That(result).IsNull();
    }

    /// <summary>A configured application name passes validation.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationNameShouldAcceptAConfiguredName() =>
        await Assert.That(static () => V10MigrationHelpers.ValidateApplicationName("ConfiguredApp")).ThrowsNothing();

    /// <summary>An unset application name is reported, because the V10 paths are built from it.</summary>
    /// <param name="applicationName">The missing or blank name.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ValidateApplicationNameShouldThrowWhenTheNameIsMissing(string? applicationName) =>
        await Assert.That(() => V10MigrationHelpers.ValidateApplicationName(applicationName))
            .Throws<InvalidOperationException>()
            .WithMessageContaining("Application name must be set");

    /// <summary>Builds a serializer-backed builder with an application name unique to the calling test.</summary>
    /// <returns>The configured builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IAkavacheBuilder CreateBuilder() =>
        new AkavacheBuilder()
            .WithApplicationName($"AkavacheV10HelperTests_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>();
}
