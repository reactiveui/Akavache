// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.EncryptedSqlite3;
using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;

using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for <see cref="AkavacheBuilderExtensions"/> covering null guards, edge cases,
/// and the IBlobCache-based settings store overloads.
/// </summary>
[Category("Akavache")]
[TestExecutor<AkavacheTestExecutor>]
public class AkavacheBuilderExtensionsTests
{
    /// <summary>
    /// The per-test <see cref="AppBuilder"/> instance.
    /// </summary>
    private AppBuilder _appBuilder = null!;

    /// <summary>
    /// The unique per-test cache root path (directory).
    /// </summary>
    private string _cacheRoot = null!;

    /// <summary>
    /// One-time setup that runs before each test. Creates a fresh builder and an isolated cache path.
    /// </summary>
    [Before(Test)]
    public void Setup()
    {
        _appBuilder = AppBuilder.CreateSplatBuilder();

        _cacheRoot = Path.Combine(
            Path.GetTempPath(),
            "AkavacheBuilderExtTests",
            Guid.NewGuid().ToString("N"),
            "ApplicationSettings");

        Directory.CreateDirectory(_cacheRoot);
    }

    /// <summary>
    /// One-time teardown after each test. Best-effort cleanup.
    /// </summary>
    [After(Test)]
    public void Teardown()
    {
        try
        {
            if (Directory.Exists(_cacheRoot))
            {
                Directory.Delete(_cacheRoot, recursive: true);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: don't fail tests on IO cleanup.
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Verifies that WithSettingsCachePath throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsCachePath_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheBuilder)null!).WithSettingsCachePath("/some/path");
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that DeleteSettingsStore throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DeleteSettingsStore_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheInstance)null!).DeleteSettingsStore<ViewSettings>();
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetLoadedSettingsStore returns null when SettingsStores is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetLoadedSettingsStore_NullSettingsStores_ReturnsNullAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var savedStores = AkavacheBuilder.SettingsStores;
        AkavacheBuilder.SettingsStores = null;

        try
        {
            var result = instance!.GetLoadedSettingsStore<ViewSettings>();
            await Assert.That(result).IsNull();
        }
        finally
        {
            AkavacheBuilder.SettingsStores = savedStores;
        }
    }

    /// <summary>
    /// Verifies that GetLoadedSettingsStore returns null when key is not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetLoadedSettingsStore_KeyNotFound_ReturnsNullAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var result = instance!.GetLoadedSettingsStore<ViewSettings>("nonexistent_key");
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies that DisposeSettingsStore returns early when stores are null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DisposeSettingsStore_NullStores_ReturnsEarlyAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var savedStores = AkavacheBuilder.SettingsStores;
        var savedCaches = AkavacheBuilder.BlobCaches;
        AkavacheBuilder.SettingsStores = null;
        AkavacheBuilder.BlobCaches = null;

        try
        {
            await instance!.DisposeSettingsStore<ViewSettings>().ConfigureAwait(false);
        }
        finally
        {
            AkavacheBuilder.SettingsStores = savedStores;
            AkavacheBuilder.BlobCaches = savedCaches;
        }
    }

    /// <summary>
    /// Verifies that WithSecureSettingsStore throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSecureSettingsStore_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheBuilder)null!).WithSecureSettingsStore<ViewSettings>("password", _ => { });
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSecureSettingsStore throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSecureSettingsStore_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheInstance)null!).GetSecureSettingsStore<ViewSettings>("password");
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSecureSettingsStore throws when AkavacheBuilder has not been initialized.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSecureSettingsStore_NullBuilderState_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var savedStores = AkavacheBuilder.SettingsStores;
        var savedCaches = AkavacheBuilder.BlobCaches;
        AkavacheBuilder.SettingsStores = null;
        AkavacheBuilder.BlobCaches = null;

        try
        {
            var action = () => instance!.GetSecureSettingsStore<ViewSettings>("password");
            await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            AkavacheBuilder.SettingsStores = savedStores;
            AkavacheBuilder.BlobCaches = savedCaches;
        }
    }

    /// <summary>
    /// Verifies that GetSecureSettingsStore throws when serializer is not configured.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSecureSettingsStore_NullSerializer_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(instance).IsNotNull();
        var akavacheInstance = instance!;

        // Null out the serializer type name so Serializer resolves to null
        var builder = (AkavacheBuilder)akavacheInstance;
        var savedTypeName = builder.SerializerTypeName;
        builder.SerializerTypeName = null;

        try
        {
            var action = () => akavacheInstance.GetSecureSettingsStore<ViewSettings>("password");
            await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            builder.SerializerTypeName = savedTypeName;
        }
    }

    /// <summary>
    /// Verifies that GetSettingsStore throws when serializer is not configured.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStore_NullSerializer_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(instance).IsNotNull();
        var akavacheInstance = instance!;

        var builder = (AkavacheBuilder)akavacheInstance;
        var savedTypeName = builder.SerializerTypeName;
        builder.SerializerTypeName = null;

        try
        {
            var action = () => akavacheInstance.GetSettingsStore<ViewSettings>();
            await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            builder.SerializerTypeName = savedTypeName;
        }
    }

    /// <summary>
    /// Verifies that WithSettingsStore throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStore_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheBuilder)null!).WithSettingsStore<ViewSettings>(_ => { });
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSettingsStore throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStore_NullBuilder_ThrowsAsync()
    {
        var action = () => ((IAkavacheInstance)null!).GetSettingsStore<ViewSettings>();
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSettingsStore throws when AkavacheBuilder has not been initialized.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStore_NullBuilderState_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var savedStores = AkavacheBuilder.SettingsStores;
        var savedCaches = AkavacheBuilder.BlobCaches;
        AkavacheBuilder.SettingsStores = null;
        AkavacheBuilder.BlobCaches = null;

        try
        {
            var action = () => instance!.GetSettingsStore<ViewSettings>();
            await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            AkavacheBuilder.SettingsStores = savedStores;
            AkavacheBuilder.BlobCaches = savedCaches;
        }
    }

    /// <summary>
    /// Verifies that WithSettingsStore with IBlobCache throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStoreWithCache_NullBuilder_ThrowsAsync()
    {
        var cache = new InMemoryBlobCache(new NewtonsoftSerializer());
        var action = () => ((IAkavacheBuilder)null!).WithSettingsStore<ViewSettings>(cache, _ => { });
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that WithSettingsStore with IBlobCache throws when cache is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStoreWithCache_NullCache_ThrowsAsync()
    {
        IAkavacheBuilder? builder = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                b =>
                {
                    b.WithSqliteProvider()
                     .WithSettingsCachePath(_cacheRoot);
                    builder = b;
                },
                _ => { })
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var action = () => builder!.WithSettingsStore<ViewSettings>((IBlobCache)null!, _ => { });
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSettingsStore with IBlobCache throws when builder is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStoreWithCache_NullBuilder_ThrowsAsync()
    {
        var cache = new InMemoryBlobCache(new NewtonsoftSerializer());
        var action = () => ((IAkavacheInstance)null!).GetSettingsStore<ViewSettings>(cache);
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSettingsStore with IBlobCache throws when cache is null.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStoreWithCache_NullCache_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var action = () => instance!.GetSettingsStore<ViewSettings>((IBlobCache)null!);
        await Assert.That(action).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSettingsStore with IBlobCache throws when AkavacheBuilder has not been initialized.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStoreWithCache_NullBuilderState_ThrowsAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        var savedStores = AkavacheBuilder.SettingsStores;
        var savedCaches = AkavacheBuilder.BlobCaches;
        AkavacheBuilder.SettingsStores = null;
        AkavacheBuilder.BlobCaches = null;

        try
        {
            var cache = new InMemoryBlobCache(new NewtonsoftSerializer());
            var action = () => instance!.GetSettingsStore<ViewSettings>(cache);
            await Assert.That(action).ThrowsExactly<InvalidOperationException>();
        }
        finally
        {
            AkavacheBuilder.SettingsStores = savedStores;
            AkavacheBuilder.BlobCaches = savedCaches;
        }
    }

    /// <summary>
    /// Verifies that a settings store can be created using a custom IBlobCache instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStoreWithCache_CreatesAndConfiguresStoreAsync()
    {
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            NewName("cache_store_test"),
            builder =>
            {
                var cache = new InMemoryBlobCache(builder.Serializer!);
                builder.WithSettingsStore<ViewSettings>(cache, s => viewSettings = s);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);
                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That(viewSettings!.IntTest).IsEqualTo(1);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync().ConfigureAwait(false);
                        }

                        await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that GetSettingsStore with IBlobCache creates a store using the provided cache.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetSettingsStoreWithCache_CreatesStoreAsync()
    {
        RunWithAkavache<NewtonsoftSerializer>(
            NewName("get_cache_store_test"),
            _ => Task.CompletedTask,
            async instance =>
            {
                ViewSettings? viewSettings = null;
                try
                {
                    var cache = new InMemoryBlobCache(instance.Serializer!);
                    var dbName = NewName("custom_cache");
                    viewSettings = instance.GetSettingsStore<ViewSettings>(cache, dbName);

                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That(viewSettings!.IntTest).IsEqualTo(1);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that GetLoadedSettingsStore returns a previously registered store.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task GetLoadedSettingsStore_ReturnsRegisteredStoreAsync()
    {
        RunWithAkavache<NewtonsoftSerializer>(
            NewName("loaded_store_test"),
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(_ => { });
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    var loaded = instance.GetLoadedSettingsStore<ViewSettings>();
                    await Assert.That(loaded).IsNotNull();
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that DeleteSettingsStore handles deletion when the file does not exist.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DeleteSettingsStore_NoFile_DoesNotThrowAsync()
    {
        RunWithAkavache<NewtonsoftSerializer>(
            NewName("delete_nofile_test"),
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(_ => { });
                return Task.CompletedTask;
            },
            async instance =>
            {
                // Delete twice - second time the file won't exist
                await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that <c>DeleteSettingsStore</c> swallows exceptions raised inside its
    /// try block. An <c>overrideDatabaseName</c> containing a path-traversal sequence
    /// makes <c>SecurityUtilities.ValidateDatabaseName</c> throw before
    /// <see cref="File.Delete(string)"/> is reached, which is precisely the kind of
    /// failure the catch clause exists to absorb.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DeleteSettingsStore_DeletionThrows_IsSwallowedAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        // A name containing ".." trips SecurityUtilities.ValidateDatabaseName, which
        // throws inside the try — the catch block must swallow the exception and the
        // call must complete without bubbling up.
        await instance!.DeleteSettingsStore<ViewSettings>(overrideDatabaseName: "../evil").ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that DeleteSettingsStore handles empty SettingsCachePath gracefully.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DeleteSettingsStore_EmptyPath_DoesNotThrowAsync()
    {
        IAkavacheInstance? instance = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(string.Empty);
                },
                inst => instance = inst)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        // Should not throw even with empty cache path
        await instance!.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that WithSettingsStore works with an override database name.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStore_OverrideDatabaseName_UsesCustomKeyAsync()
    {
        var customName = NewName("custom_db");
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            NewName("override_name_test"),
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s, customName);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    var loaded = instance.GetLoadedSettingsStore<ViewSettings>(customName);
                    await Assert.That(loaded).IsNotNull();

                    var notFound = instance.GetLoadedSettingsStore<ViewSettings>();
                    await Assert.That(notFound).IsNull();
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync().ConfigureAwait(false);
                        }

                        await instance.DeleteSettingsStore<ViewSettings>(customName).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that WithSecureSettingsStore with null settings action does not throw.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSecureSettingsStore_NullSettingsAction_DoesNotThrowAsync()
    {
        var testName = NewName("null_secure_action_test");

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                testName,
                builder =>
                {
                    builder
                        .WithEncryptedSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot)
                        .WithSecureSettingsStore<ViewSettings>("password", (Action<ViewSettings?>)null!);
                },
                instance =>
                {
                    // Verify a store was registered even with null action
                    instance.GetLoadedSettingsStore<ViewSettings>();
                    instance.DeleteSettingsStore<ViewSettings>().GetAwaiter().GetResult();
                })
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that WithSettingsStore with null settings action does not throw.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStore_NullSettingsAction_DoesNotThrowAsync()
    {
        RunWithAkavache<NewtonsoftSerializer>(
            NewName("null_action_test"),
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>((Action<ViewSettings?>)null!);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    var loaded = instance.GetLoadedSettingsStore<ViewSettings>();
                    await Assert.That(loaded).IsNotNull();
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that WithSettingsStore with IBlobCache and null settings action does not throw.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WithSettingsStoreWithCache_NullSettingsAction_DoesNotThrowAsync()
    {
        RunWithAkavache<NewtonsoftSerializer>(
            NewName("null_cache_action_test"),
            builder =>
            {
                var cache = new InMemoryBlobCache(builder.Serializer!);
                builder.WithSettingsStore<ViewSettings>(cache, (Action<ViewSettings?>)null!);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    var loaded = instance.GetLoadedSettingsStore<ViewSettings>();
                    await Assert.That(loaded).IsNotNull();
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that DeleteSettingsStore handles IO errors gracefully by catching exceptions.
    /// Creates a directory where the .db file would be, so File.Delete throws an IOException.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DeleteSettingsStore_ProtectedDirectory_HandlesExceptionGracefullyAsync()
    {
        var dbName = NewName("delete_err");

        RunWithAkavache<NewtonsoftSerializer>(
            NewName("delete_protected_test"),
            _ => Task.CompletedTask,
            async instance =>
            {
                // Create a directory where the .db file would be.
                // File.Delete on a directory path always throws an IOException/UnauthorizedAccessException.
                var fakePath = Path.Combine(_cacheRoot, $"{dbName}.db");
                Directory.CreateDirectory(fakePath);

                // Should not throw - the catch block in DeleteSettingsStore handles IO errors
                await instance.DeleteSettingsStore<ViewSettings>(dbName).ConfigureAwait(false);

                // Clean up the fake directory
                try
                {
                    Directory.Delete(fakePath, recursive: true);
                }
                catch (Exception ex)
                {
                    // Best-effort cleanup.
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a unique, human-readable test name prefix plus a GUID segment.
    /// </summary>
    /// <param name="prefix">A short, descriptive prefix for the test resource name.</param>
    /// <returns>A unique name string suitable for use as an application name or store key.</returns>
    private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    /// <summary>
    /// Creates, configures and builds an Akavache instance using the per-test path and SQLite provider, then executes the test body.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type to use.</typeparam>
    /// <param name="applicationName">Optional application name to scope the store.</param>
    /// <param name="configureAsync">An async configuration callback.</param>
    /// <param name="bodyAsync">The asynchronous test body.</param>
    private void RunWithAkavache<TSerializer>(
        string? applicationName,
        Func<IAkavacheBuilder, Task> configureAsync,
        Func<IAkavacheInstance, Task> bodyAsync)
        where TSerializer : class, ISerializer, new() =>
        _appBuilder
            .WithAkavache<TSerializer>(
                applicationName!,
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);

                    configureAsync(builder).GetAwaiter().GetResult();
                },
                instance =>
                {
                    bodyAsync(instance).GetAwaiter().GetResult();
                })
            .Build();
}
