// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

using Splat.Builder;

namespace Akavache.Settings.Tests
{
    /// <summary>
    /// Tests for the unencrypted settings cache, isolated per test to avoid static state leakage.
    /// Uses eventually-consistent polling and treats transient disposal as retryable.
    /// </summary>
    [TestFixture]
    [Category("Akavache")]
    [Parallelizable(ParallelScope.None)]
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
        [SetUp]
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
        [TearDown]
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
        [CancelAfter(15000)]
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

                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(viewSettings, Is.Not.Null);
                            Assert.That(viewSettings!.BoolTest, Is.True);
                            Assert.That(viewSettings.ShortTest, Is.EqualTo((short)16));
                            Assert.That(viewSettings.IntTest, Is.EqualTo(1));
                            Assert.That(viewSettings.LongTest, Is.EqualTo(123456L));
                            Assert.That(viewSettings.StringTest, Is.EqualTo("TestString"));
                            Assert.That(viewSettings.FloatTest, Is.EqualTo(2.2f).Within(0.0001f));
                            Assert.That(viewSettings.DoubleTest, Is.EqualTo(23.8d).Within(0.0001d));
                            Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option1));
                        }
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
        [CancelAfter(15000)]
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
        [CancelAfter(15000)]
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

                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(viewSettings, Is.Not.Null);
                            Assert.That(viewSettings!.BoolTest, Is.True);
                            Assert.That(viewSettings.ShortTest, Is.EqualTo((short)16));
                            Assert.That(viewSettings.IntTest, Is.EqualTo(1));
                            Assert.That(viewSettings.LongTest, Is.EqualTo(123456L));
                            Assert.That(viewSettings.StringTest, Is.EqualTo("TestString"));
                            Assert.That(viewSettings.FloatTest, Is.EqualTo(2.2f).Within(0.0001f));
                            Assert.That(viewSettings.DoubleTest, Is.EqualTo(23.8d).Within(0.0001d));
                            Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option1));
                        }
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
        [CancelAfter(15000)]
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
        [CancelAfter(15000)]
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
                    instance => { akavache = instance; })
                .Build();

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(akavache, Is.Not.Null);
                Assert.That(akavache!.SettingsCachePath, Is.EqualTo(path));
            }
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
}
