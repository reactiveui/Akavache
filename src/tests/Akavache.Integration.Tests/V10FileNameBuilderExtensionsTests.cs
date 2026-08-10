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
/// Tests the builder entry points that point Akavache at V10-era database files and run the
/// one-time migration. Both refuse to run without a serializer, and both are the only public
/// surface of the V10-to-V11 package.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[Category("Akavache")]
public class V10FileNameBuilderExtensionsTests
{
    /// <summary>The V10 filename the UserAccount cache maps to.</summary>
    private const string UserAccountV10FileName = "userblobs.db";

    /// <summary>The V10 filename the LocalMachine cache maps to.</summary>
    private const string LocalMachineV10FileName = "blobs.db";

    /// <summary>The V10 filename the Secure cache maps to.</summary>
    private const string SecureV10FileName = "secret.db";

    /// <summary>File stem of the V11 LocalMachine destination the migration tests write to.</summary>
    private const string LocalMachineCacheFile = "local";

    /// <summary>File stem of the V11 UserAccount destination the migration tests write to.</summary>
    private const string UserAccountCacheFile = "user";

    /// <summary>Pointing the builder at V10 filenames opens a cache of every kind against its legacy file.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithV10FileNamesShouldOpenEachCacheAgainstItsLegacyFile()
    {
        var builder = CreateBuilder();

        var result = builder.WithV10FileNames();

        using (Assert.Multiple())
        {
            await Assert.That(result).IsSameReferenceAs(builder);
            await Assert.That(builder.UserAccount).IsTypeOf<SqliteBlobCache>();
            await Assert.That(builder.LocalMachine).IsTypeOf<SqliteBlobCache>();
            await Assert.That(V10MigrationHelpers.GetUnderlyingBlobCache(builder.Secure)).IsTypeOf<SqliteBlobCache>();
            await Assert.That(File.Exists(V10MigrationHelpers.GetV10DatabasePath(builder, "UserAccount")!)).IsTrue();
            await Assert.That(File.Exists(V10MigrationHelpers.GetV10DatabasePath(builder, "LocalMachine")!)).IsTrue();
            await Assert.That(File.Exists(V10MigrationHelpers.GetV10DatabasePath(builder, "Secure")!)).IsTrue();
        }
    }

    /// <summary>Each cache kind resolves to the V10 filename that kind used, not the V11 one.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithV10FileNamesShouldResolveTheV10FileNameForEachKind()
    {
        var builder = CreateBuilder();

        using (Assert.Multiple())
        {
            await Assert.That(Path.GetFileName(V10MigrationHelpers.GetV10DatabasePath(builder, "UserAccount")!)).IsEqualTo(UserAccountV10FileName);
            await Assert.That(Path.GetFileName(V10MigrationHelpers.GetV10DatabasePath(builder, "LocalMachine")!)).IsEqualTo(LocalMachineV10FileName);
            await Assert.That(Path.GetFileName(V10MigrationHelpers.GetV10DatabasePath(builder, "Secure")!)).IsEqualTo(SecureV10FileName);
        }
    }

    /// <summary>Pointing the builder at V10 filenames also switches it onto the legacy directory layout.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithV10FileNamesShouldSwitchToTheLegacyFileLocation()
    {
        var builder = CreateBuilder();

        _ = builder.WithV10FileNames();

        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>A builder already on the legacy layout is left on it rather than switched again.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithV10FileNamesShouldKeepAnExistingLegacyFileLocation()
    {
        var builder = CreateBuilder();
        _ = builder.WithLegacyFileLocation();

        _ = builder.WithV10FileNames();

        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>Without a serializer there is no way to read the legacy files, so the call is refused.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task WithV10FileNamesShouldThrowWithoutASerializer() =>
        await Assert.That(static () => new AkavacheBuilder().WithApplicationName("NoSerializerApp").WithV10FileNames())
            .Throws<InvalidOperationException>()
            .WithMessageContaining("No serializer has been registered");

    /// <summary>Migrating with no V10 files present is a no-op that still returns the builder.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldReturnTheBuilderWhenThereIsNothingToMigrate()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var builder = CreateBuilder();
        _ = builder
            .WithUserAccount(CreateSqliteCache(path, UserAccountCacheFile))
            .WithLocalMachine(CreateSqliteCache(path, LocalMachineCacheFile));

        var result = builder.MigrateFromV10();

        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>A logger supplied through the options receives the migration's diagnostics.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldUseTheSuppliedLogger()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var builder = CreateBuilder();
        _ = builder
            .WithUserAccount(CreateSqliteCache(path, UserAccountCacheFile))
            .WithLocalMachine(CreateSqliteCache(path, LocalMachineCacheFile));
        List<string> log = [];

        _ = builder.MigrateFromV10(new(Logger: log.Add));

        await Assert.That(log).IsNotEmpty();
        await Assert.That(log).Contains(static x => x.Contains("V10 database not found", StringComparison.Ordinal));
    }

    /// <summary>Turning every cache kind off keeps the migration away from all of them.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldSkipTheKindsTheOptionsTurnOff()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var builder = CreateBuilder();
        _ = builder
            .WithUserAccount(CreateSqliteCache(path, UserAccountCacheFile))
            .WithLocalMachine(CreateSqliteCache(path, LocalMachineCacheFile));
        List<string> log = [];

        _ = builder.MigrateFromV10(new(
            MigrateLocalMachine: false,
            MigrateUserAccount: false,
            MigrateSecure: false,
            Logger: log.Add));

        await Assert.That(log).IsEmpty();
    }

    /// <summary>Leaving a single kind on migrates only that one.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldMigrateOnlyTheKindsTheOptionsLeaveOn()
    {
        using var tempDir = Utility.WithEmptyDirectory(out var path);
        var builder = CreateBuilder();
        _ = builder
            .WithUserAccount(CreateSqliteCache(path, UserAccountCacheFile))
            .WithLocalMachine(CreateSqliteCache(path, LocalMachineCacheFile));
        List<string> log = [];

        _ = builder.MigrateFromV10(new(
            MigrateLocalMachine: false,
            MigrateSecure: false,
            Logger: log.Add));

        await Assert.That(log).HasSingleItem();
    }

    /// <summary>Migrating without options refuses a null rather than silently running the defaults.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldThrowOnNullOptions()
    {
        var builder = CreateBuilder();

        await Assert.That(() => builder.MigrateFromV10(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>The defaults are the ones the options document.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task DefaultMigrationOptionsShouldMatchTheDocumentedBehaviour()
    {
        V10MigrationOptions options = new();

        using (Assert.Multiple())
        {
            await Assert.That(options.ReserializeToCurrentFormat).IsTrue();
            await Assert.That(options.DeleteOldFiles).IsFalse();
            await Assert.That(options.MigrateUserAccount).IsTrue();
            await Assert.That(options.MigrateLocalMachine).IsTrue();
            await Assert.That(options.MigrateSecure).IsTrue();
            await Assert.That(options.Logger).IsNull();
        }
    }

    /// <summary>A secure cache backed by SQLite is migrated alongside the other kinds.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldWalkTheSecureCache()
    {
        var builder = CreateBuilder();
        _ = builder.WithV10FileNames();

        var result = builder.MigrateFromV10();

        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>Without a serializer the migration cannot convert anything, so the call is refused.</summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task MigrateFromV10ShouldThrowWithoutASerializer() =>
        await Assert.That(static () => new AkavacheBuilder().WithApplicationName("NoSerializerApp").MigrateFromV10())
            .Throws<InvalidOperationException>()
            .WithMessageContaining("No serializer has been registered");

    /// <summary>Builds a serializer-backed builder with an application name unique to the calling test.</summary>
    /// <returns>The configured builder.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IAkavacheBuilder CreateBuilder() =>
        new AkavacheBuilder()
            .WithApplicationName($"AkavacheV10BuilderTests_{Guid.NewGuid():N}")
            .WithSerializer<SystemJsonSerializer>();

    /// <summary>Creates a SQLite-backed V11 destination cache inside the supplied directory.</summary>
    /// <param name="path">The directory for the database file.</param>
    /// <param name="name">The file stem.</param>
    /// <returns>A new cache.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SqliteBlobCache CreateSqliteCache(string path, string name) =>
        new(Path.Combine(path, $"{name}.db"), new SystemJsonSerializer(), ImmediateSequencer.Instance);
}
