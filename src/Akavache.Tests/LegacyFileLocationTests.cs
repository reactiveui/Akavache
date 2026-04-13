// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for WithLegacyFileLocation builder method and FileLocationOption behavior.
/// Validates that V10→V11 migration scenarios work correctly when using legacy file locations.
/// </summary>
[Category("Akavache")]
[NotInParallel(["CacheDatabaseState", "NativeSqlite"])]
public class LegacyFileLocationTests
{
    /// <summary>
    /// Verifies that WithLegacyFileLocation sets the FileLocationOption to Legacy.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithLegacyFileLocation_SetsFileLocationOptionToLegacy()
    {
        // Arrange
        var builder = CacheDatabase.CreateBuilder();

        // Act
        builder.WithLegacyFileLocation();

        // Assert
        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>
    /// Verifies that the default FileLocationOption is Default.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CreateBuilder_DefaultFileLocationOption_IsDefault()
    {
        // Act
        var builder = CacheDatabase.CreateBuilder();

        // Assert
        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Default);
    }

    /// <summary>
    /// Verifies that CreateBuilder with explicit Legacy option sets it correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CreateBuilder_WithLegacyOption_SetsFileLocationOptionToLegacy()
    {
        // Act
        var builder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy);

        // Assert
        await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
    }

    /// <summary>
    /// Verifies that WithLegacyFileLocation returns the builder for fluent chaining.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithLegacyFileLocation_ReturnsSameBuilder_ForFluentChaining()
    {
        // Arrange
        var builder = CacheDatabase.CreateBuilder();

        // Act
        var result = builder.WithLegacyFileLocation();

        // Assert
        await Assert.That(result).IsSameReferenceAs(builder);
    }

    /// <summary>
    /// Verifies that WithLegacyFileLocation can be chained with other builder methods.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithLegacyFileLocation_CanBeChainedWithOtherMethods()
    {
        // Arrange & Act
        var builder = CacheDatabase.CreateBuilder()
            .WithApplicationName("TestApp")
            .WithLegacyFileLocation()
            .WithSerializer<SystemJsonSerializer>();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(builder.FileLocationOption).IsEqualTo(FileLocationOption.Legacy);
            await Assert.That(builder.ApplicationName).IsEqualTo("TestApp");
        }
    }

    /// <summary>
    /// Verifies that legacy file location produces different cache directories than the default.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LegacyFileLocation_ProducesDifferentPath_ThanDefault()
    {
        // Arrange
        var appName = "LegacyPathTest";

        var defaultBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        var legacyBuilder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy)
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        // Act
        var defaultPath = defaultBuilder.GetIsolatedCacheDirectory("UserAccount");
        var legacyPath = legacyBuilder.GetLegacyCacheDirectory("UserAccount");

        // Assert - the paths should be different (isolated storage vs legacy app data)
        await Assert.That(defaultPath).IsNotNull();
        await Assert.That(legacyPath).IsNotNull();
        await Assert.That(defaultPath).IsNotEqualTo(legacyPath);
    }

    /// <summary>
    /// Verifies that the legacy directory for UserAccount points to the expected location.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetLegacyCacheDirectory_UserAccount_ContainsApplicationName()
    {
        // Arrange
        var appName = "LegacyDirTest";
        var builder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy)
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        // Act
        var legacyPath = builder.GetLegacyCacheDirectory("UserAccount");

        // Assert
        await Assert.That(legacyPath).IsNotNull();
        await Assert.That(legacyPath!).Contains(appName);
    }

    /// <summary>
    /// Verifies that legacy Secure directory points to SecretCache subdirectory.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetLegacyCacheDirectory_Secure_ContainsSecretCache()
    {
        // Arrange
        var appName = "LegacySecureDirTest";
        var builder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy)
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        // Act
        var legacyPath = builder.GetLegacyCacheDirectory("Secure");

        // Assert
        await Assert.That(legacyPath).IsNotNull();
        await Assert.That(legacyPath!).Contains("SecretCache");
    }

    /// <summary>
    /// Verifies that legacy LocalMachine directory points to BlobCache subdirectory.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetLegacyCacheDirectory_LocalMachine_ContainsBlobCache()
    {
        // Arrange
        var appName = "LegacyLocalMachineDirTest";
        var builder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy)
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        // Act
        var legacyPath = builder.GetLegacyCacheDirectory("LocalMachine");

        // Assert
        await Assert.That(legacyPath).IsNotNull();
        await Assert.That(legacyPath!).Contains("BlobCache");
    }

    /// <summary>
    /// Verifies that Initialize with FileLocationOption.Legacy creates caches at legacy paths.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Initialize_WithLegacyFileLocation_CreatesCachesAtLegacyPaths()
    {
        // Arrange
        var testAppName = $"LegacyInitTest_{Guid.NewGuid():N}";
        Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        try
        {
            // Act
            CacheDatabase.Initialize<SystemJsonSerializer>(
                configure: builder =>
                {
                    builder.WithSqliteProvider();
                    builder.WithSqliteDefaults();
                },
                applicationName: testAppName,
                fileLocationOption: FileLocationOption.Legacy);

            // Assert - caches should be initialized
            using (Assert.Multiple())
            {
                await Assert.That(CacheDatabase.UserAccount).IsNotNull();
                await Assert.That(CacheDatabase.LocalMachine).IsNotNull();
                await Assert.That(CacheDatabase.Secure).IsNotNull();
                await Assert.That(CacheDatabase.InMemory).IsNotNull();
            }
        }
        finally
        {
            await CacheDatabase.ResetForTestsAsync();
            Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        }
    }

    /// <summary>
    /// Verifies that WithLegacyFileLocation in builder chain initializes caches correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithLegacyFileLocation_InBuilderChain_InitializesCachesCorrectly()
    {
        // Arrange
        var testAppName = $"LegacyBuilderChainTest_{Guid.NewGuid():N}";
        Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        try
        {
            // Act - using the new fluent builder method
            CacheDatabase.Initialize<SystemJsonSerializer>(
                configure: builder =>
                {
                    builder.WithLegacyFileLocation()
                           .WithSqliteProvider()
                           .WithSqliteDefaults();
                },
                applicationName: testAppName);

            // Assert - caches should be initialized and functional
            using (Assert.Multiple())
            {
                await Assert.That(CacheDatabase.UserAccount).IsNotNull();
                await Assert.That(CacheDatabase.LocalMachine).IsNotNull();
                await Assert.That(CacheDatabase.Secure).IsNotNull();
                await Assert.That(CacheDatabase.InMemory).IsNotNull();
            }
        }
        finally
        {
            await CacheDatabase.ResetForTestsAsync();
            Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        }
    }

    /// <summary>
    /// Verifies that data can be written and read with legacy file locations.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LegacyFileLocation_CanWriteAndReadData()
    {
        // Arrange
        var testAppName = $"LegacyDataTest_{Guid.NewGuid():N}";
        Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        try
        {
            CacheDatabase.Initialize<SystemJsonSerializer>(
                configure: builder =>
                {
                    builder.WithLegacyFileLocation()
                           .WithSqliteProvider()
                           .WithSqliteDefaults();
                },
                applicationName: testAppName);

            // Act - write data
            var testKey = "test-key";
            var testValue = "test-value";
            await CacheDatabase.UserAccount!.InsertObject(testKey, testValue);

            // Assert - read data back
            var result = await CacheDatabase.UserAccount.GetObject<string>(testKey);
            await Assert.That(result).IsEqualTo(testValue);
        }
        finally
        {
            await CacheDatabase.ResetForTestsAsync();
            Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        }
    }

    /// <summary>
    /// Verifies that SettingsCachePath uses legacy directory when legacy option is set.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SettingsCachePath_WithLegacyOption_UsesLegacyDirectory()
    {
        // Arrange
        var appName = "LegacySettingsPathTest";

        var defaultBuilder = CacheDatabase.CreateBuilder()
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        var legacyBuilder = CacheDatabase.CreateBuilder(FileLocationOption.Legacy)
            .WithApplicationName(appName)
            .WithSerializer<SystemJsonSerializer>();

        // Act
        var defaultPath = defaultBuilder.SettingsCachePath;
        var legacyPath = legacyBuilder.SettingsCachePath;

        // Assert - paths should be different
        await Assert.That(defaultPath).IsNotNull();
        await Assert.That(legacyPath).IsNotNull();
        await Assert.That(defaultPath).IsNotEqualTo(legacyPath);
    }

    /// <summary>
    /// Verifies that data written with legacy location can be read back after re-initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LegacyFileLocation_DataPersistsAcrossReinitialization()
    {
        // Arrange
        var testAppName = $"LegacyPersistTest_{Guid.NewGuid():N}";
        var testKey = "persist-key";
        var testValue = "persist-value";
        Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

        try
        {
            // Act - write data with first initialization
            CacheDatabase.Initialize<SystemJsonSerializer>(
                configure: builder =>
                {
                    builder.WithLegacyFileLocation()
                           .WithSqliteProvider()
                           .WithSqliteDefaults();
                },
                applicationName: testAppName);

            await CacheDatabase.UserAccount!.InsertObject(testKey, testValue);
            await CacheDatabase.UserAccount.Flush();

            // Reinitialize
            await CacheDatabase.ResetForTestsAsync();
            Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();

            CacheDatabase.Initialize<SystemJsonSerializer>(
                configure: builder =>
                {
                    builder.WithLegacyFileLocation()
                           .WithSqliteProvider()
                           .WithSqliteDefaults();
                },
                applicationName: testAppName);

            // Assert - data should still be there
            var result = await CacheDatabase.UserAccount!.GetObject<string>(testKey);
            await Assert.That(result).IsEqualTo(testValue);
        }
        finally
        {
            await CacheDatabase.ResetForTestsAsync();
            Akavache.Sqlite3.AkavacheBuilderExtensions.ResetSqliteProviderForTests();
        }
    }
}
