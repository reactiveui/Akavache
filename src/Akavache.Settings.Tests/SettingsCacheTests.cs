// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for the unencrypted settings cache, isolated per test to avoid static state leakage.
/// Uses eventually-consistent polling and treats transient disposal as retryable.
/// </summary>
[Category("Akavache")]
[NotInParallel]
public class SettingsCacheTests
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
        AppBuilder.ResetBuilderStateForTests();
        _appBuilder = AppBuilder.CreateSplatBuilder();

        _cacheRoot = Path.Combine(
            Path.GetTempPath(),
            "AkavacheSettingsTests",
            Guid.NewGuid().ToString("N"),
            "ApplicationSettings");

        Directory.CreateDirectory(_cacheRoot);
    }

    /// <summary>
    /// One-time teardown after each test. Best-effort cleanup and static reset.
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
        catch
        {
            // Best-effort: don't fail tests on IO cleanup.
        }

        AppBuilder.ResetBuilderStateForTests();
    }

    /// <summary>
    /// Verifies that a settings store can be created and initial values materialize (Newtonsoft serializer).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestCreateAndInsertNewtonsoftAsync()
    {
        var appName = NewName("newtonsoft_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            appName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That(viewSettings!.BoolTest).IsTrue();
                    await Assert.That(viewSettings.ShortTest).IsEqualTo((short)16);
                    await Assert.That(viewSettings.IntTest).IsEqualTo(1);
                    await Assert.That(viewSettings.LongTest).IsEqualTo(123456L);
                    await Assert.That(viewSettings.StringTest).IsEqualTo("TestString");
                    await Assert.That(viewSettings.FloatTest).IsEqualTo(2.2f).Within(0.0001f);
                    await Assert.That(viewSettings.DoubleTest).IsEqualTo(23.8d).Within(0.0001d);
                    await Assert.That(viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option1);
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
                    catch
                    {
                        // Swallow cleanup issues.
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies updates are applied and readable (Newtonsoft serializer).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestUpdateAndReadNewtonsoftAsync()
    {
        var appName = NewName("newtonsoft_update_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            appName,
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    // Ensure the initially captured store exists.
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    // Perform the mutation in a FRESH store, retrying on transient disposal.
                    await TestHelper.EventuallyAsync(async () =>
                    {
                        return await TestHelper.WithFreshStoreAsync(
                            instance,
                            () => instance.GetSettingsStore<ViewSettings>(),
                            async s =>
                            {
                                s.EnumTest = EnumTestValue.Option2;
                                var ok = TestHelper.TryRead(() => s.EnumTest == EnumTestValue.Option2);
                                await Task.Yield();
                                return ok;
                            }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    // Optionally also verify via the initially captured instance (retryable read).
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option2))
                        .ConfigureAwait(false);
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
                    catch
                    {
                        // Swallow cleanup issues.
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that a settings store can be created and initial values materialize (System.Text.Json serializer).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestCreateAndInsertSystemTextJsonAsync()
    {
        var appName = NewName("systemjson_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<SystemJsonSerializer>(
            appName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>().ConfigureAwait(false);
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That(viewSettings!.BoolTest).IsTrue();
                    await Assert.That(viewSettings.ShortTest).IsEqualTo((short)16);
                    await Assert.That(viewSettings.IntTest).IsEqualTo(1);
                    await Assert.That(viewSettings.LongTest).IsEqualTo(123456L);
                    await Assert.That(viewSettings.StringTest).IsEqualTo("TestString");
                    await Assert.That(viewSettings.FloatTest).IsEqualTo(2.2f).Within(0.0001f);
                    await Assert.That(viewSettings.DoubleTest).IsEqualTo(23.8d).Within(0.0001d);
                    await Assert.That(viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option1);
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
                    catch
                    {
                        // Swallow cleanup issues.
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies updates are applied and readable (System.Text.Json serializer).
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestUpdateAndReadSystemTextJsonAsync()
    {
        var appName = NewName("systemjson_update_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<SystemJsonSerializer>(
            appName,
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
                return Task.CompletedTask;
            },
            async instance =>
            {
                try
                {
                    // Ensure the initially captured store exists.
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    // Perform the mutation in a FRESH store, retrying on transient disposal.
                    await TestHelper.EventuallyAsync(async () =>
                    {
                        return await TestHelper.WithFreshStoreAsync(
                            instance,
                            () => instance.GetSettingsStore<ViewSettings>(),
                            async s =>
                            {
                                s.EnumTest = EnumTestValue.Option2;
                                var ok = TestHelper.TryRead(() => s.EnumTest == EnumTestValue.Option2);
                                await Task.Yield();
                                return ok;
                            }).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    // Optionally also verify via the initially captured instance (retryable read).
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option2))
                        .ConfigureAwait(false);
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
                    catch
                    {
                        // Swallow cleanup issues.
                    }
                }
            });

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheInstance.SettingsCachePath"/> honors an explicit override.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestOverrideSettingsCachePathAsync()
    {
        var path = Path.Combine(_cacheRoot, "OverridePath");
        Directory.CreateDirectory(path);

        IAkavacheInstance? akavache = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: null,
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(path);
                },
                instance => akavache = instance)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(akavache).IsNotNull();
        await Assert.That(akavache!.SettingsCachePath).IsEqualTo(path);
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheInstance.SettingsCachePath"/> is computed lazily and respects <see cref="IAkavacheBuilder.WithApplicationName(string)"/> order.
    /// This test validates the fix for the constructor ordering issue where SettingsCachePath was computed before WithApplicationName() could be called.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestSettingsCachePathRespectsApplicationNameOrderAsync()
    {
        var customAppName = NewName("CustomAppTest");
        IAkavacheInstance? akavache = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: null, // Don't set via parameter
                builder =>
                {
                    builder
                        .WithSqliteProvider()
                        .WithApplicationName(customAppName); // Set via fluent API after builder creation
                },
                instance => akavache = instance)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(akavache).IsNotNull();
        await Assert.That(akavache!.SettingsCachePath).IsNotNull();

        // The settings cache path should contain the custom application name, not the default "Akavache"
        await Assert.That(akavache.SettingsCachePath)
            .Contains(customAppName)
            .Because("SettingsCachePath should contain the custom application name when WithApplicationName() is called before accessing the path");

        // Additional validation: ensure it doesn't contain the default name when a custom name is set
        await Assert.That(akavache.SettingsCachePath)
            .DoesNotContain("Akavache")
            .Because("SettingsCachePath should not contain the default 'Akavache' directory when a custom application name is specified");
    }

    /// <summary>
    /// Verifies that <see cref="IAkavacheInstance.SettingsCachePath"/> uses the default application name when no custom name is provided.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestSettingsCachePathUsesDefaultApplicationNameAsync()
    {
        IAkavacheInstance? akavache = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                applicationName: null, // No custom application name
                builder =>
                {
                    builder.WithSqliteProvider();

                    // Don't call WithApplicationName() - should use default
                },
                instance => akavache = instance)
            .Build();

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(akavache).IsNotNull();
        await Assert.That(akavache!.SettingsCachePath).IsNotNull();

        // Should contain the default application name when no custom name is provided
        await Assert.That(akavache.SettingsCachePath)
            .Contains("Akavache")
            .Because("SettingsCachePath should contain the default 'Akavache' directory when no custom application name is specified");
    }

    /// <summary>
    /// Creates a unique, human-readable test name prefix plus a GUID segment.
    /// </summary>
    /// <param name="prefix">A short, descriptive prefix for the test resource name.</param>
    /// <returns>A unique name string suitable for use as an application name or store key.</returns>
    private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    /// <summary>
    /// Creates, configures and builds an Akavache instance using the per-test path and SQLite provider, then executes the test body.
    /// This version blocks on async delegates to avoid async-void and ensure assertion scopes close before the test ends.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type to use (e.g., <see cref="NewtonsoftSerializer"/> or <see cref="SystemJsonSerializer"/>).</typeparam>
    /// <param name="applicationName">Optional application name to scope the store; may be <see langword="null"/>.</param>
    /// <param name="configureAsync">An async configuration callback to register stores and/or delete existing stores before the body runs.</param>
    /// <param name="bodyAsync">The asynchronous test body that uses the configured <see cref="IAkavacheInstance"/>.</param>
    private void RunWithAkavache<TSerializer>(
        string? applicationName,
        Func<IAkavacheBuilder, Task> configureAsync,
        Func<IAkavacheInstance, Task> bodyAsync)
        where TSerializer : class, ISerializer, new() =>
        _appBuilder
            .WithAkavache<TSerializer>(
                applicationName,
                builder =>
                {
                    // base config
                    builder
                        .WithSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);

                    // IMPORTANT: block here so we don't create async-void
                    configureAsync(builder).GetAwaiter().GetResult();
                },
                instance =>
                {
                    // IMPORTANT: block here so the body completes before Build() returns
                    bodyAsync(instance).GetAwaiter().GetResult();
                })
            .Build();
}
