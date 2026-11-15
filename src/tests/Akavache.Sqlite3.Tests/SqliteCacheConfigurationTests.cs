// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests;
#else
namespace Akavache.Tests;
#endif

/// <summary>Verifies shared SQLite path preparation before a database is opened.</summary>
public class SqliteCacheConfigurationTests
{
    /// <summary>The cache slot used to select the per-user directory.</summary>
    private const string CacheName = "UserAccount";

    /// <summary>Creates the chosen directory, normalizes the file name and captures serializer options.</summary>
    /// <param name="location">The directory selection policy.</param>
    /// <param name="kind">The optional date kind.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(FileLocationOption.Legacy, DateTimeKind.Utc)]
    [Arguments(FileLocationOption.Default, null)]
    public async Task Prepare_ValidConfiguration_CreatesSelectedPath(FileLocationOption location, DateTimeKind? kind)
    {
        SystemJsonSerializer serializer = new();
        Sqlite3BuilderExtensionsTests.FakeAkavacheBuilder builder = new()
        {
            ApplicationName = $"SqliteConfiguration_{Guid.NewGuid():N}",
            Serializer = serializer,
            FileLocationOption = location,
            ForcedDateTimeKind = kind,
        };
        var directory = location == FileLocationOption.Legacy
            ? builder.GetLegacyCacheDirectory(CacheName)!
            : builder.GetIsolatedCacheDirectory(CacheName)!;

        try
        {
            // Both a missing directory and an existing directory must be accepted.
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            var options = Sqlite3.SqliteCacheConfiguration.Prepare(" UserAccount", builder);
            var repeated = Sqlite3.SqliteCacheConfiguration.Prepare(CacheName, builder);

            await Assert.That(options.FilePath).IsEqualTo(Path.Combine(directory, "UserAccount.db"));
            await Assert.That(Directory.Exists(directory)).IsTrue();
            await Assert.That(File.Exists(options.FilePath)).IsFalse();
            await Assert.That(options.Serializer).IsSameReferenceAs(serializer);
            await Assert.That(options.ForcedDateTimeKind).IsEqualTo(kind);
            await Assert.That(repeated).IsEqualTo(options);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>Missing serializers are reported before invalid names, preserving validation order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Prepare_NoSerializer_ThrowsBeforeNameValidation()
    {
        Sqlite3BuilderExtensionsTests.FakeAkavacheBuilder builder = new();
        await Assert.That(() => Sqlite3.SqliteCacheConfiguration.Prepare(string.Empty, builder)).Throws<InvalidOperationException>();
    }

    /// <summary>Rejects invalid cache names before creating a directory.</summary>
    /// <param name="name">The invalid cache slot.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("../outside")]
    [Arguments("nested/cache")]
    [Arguments("CON")]
    public async Task Prepare_InvalidCacheName_Throws(string name)
    {
        Sqlite3BuilderExtensionsTests.FakeAkavacheBuilder builder = new() { ApplicationName = "SqliteConfigurationInvalid", Serializer = new SystemJsonSerializer() };
        await Assert.That(() => Sqlite3.SqliteCacheConfiguration.Prepare(name, builder)).Throws<ArgumentException>();
    }

    /// <summary>Rejects an empty application name even when a serializer is registered.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Prepare_EmptyApplicationName_Throws()
    {
        Sqlite3BuilderExtensionsTests.FakeAkavacheBuilder builder = new() { Serializer = new SystemJsonSerializer() };
        await Assert.That(() => Sqlite3.SqliteCacheConfiguration.Prepare(CacheName, builder)).Throws<ArgumentException>();
    }
}
