// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;

using NUnit.Framework;

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
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

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

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
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
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        viewSettings!.EnumTest = EnumTestValue.Option2;
                        await EventuallyAsync(() => TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option2)).ConfigureAwait(false);
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

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
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
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

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

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
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
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        viewSettings!.EnumTest = EnumTestValue.Option2;
                        await EventuallyAsync(() => TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option2)).ConfigureAwait(false);
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

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
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
                    instance =>
                    {
                        akavache = instance;
                    })
                .Build();

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

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
        private static string NewName(string prefix)
        {
            return $"{prefix}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Returns <see langword="true"/> if the supplied exception message looks like a "disposed" transient from Rx.
        /// </summary>
        /// <param name="ex">The exception to inspect.</param>
        /// <returns>True if the message indicates a disposed resource; otherwise, false.</returns>
        private static bool IsDisposedMessage(InvalidOperationException ex)
        {
            return ex.Message?.IndexOf("disposed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Attempts to evaluate a getter/condition that may touch a cache; treats disposal as transient.
        /// </summary>
        /// <param name="probe">A function that evaluates to <see langword="true"/> when the condition is satisfied.</param>
        /// <returns>True if the probe succeeded and returned true; false on transient disposal or false condition.</returns>
        private static bool TryRead(Func<bool> probe)
        {
            try
            {
                return probe();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
            {
                return false;
            }
        }

        /// <summary>
        /// Polls a condition until it returns <see langword="true"/> or the timeout expires.
        /// Handles transient disposal exceptions as retryable.
        /// </summary>
        /// <param name="condition">A synchronous function that returns <see langword="true"/> when the condition is satisfied.</param>
        /// <param name="timeoutMs">The maximum time, in milliseconds, to wait before failing the assertion. Default is 3500ms.</param>
        /// <param name="initialDelayMs">The initial delay between polls, in milliseconds. Default is 25ms.</param>
        /// <param name="backoff">The multiplicative backoff applied to the delay between retries. Default is 1.5.</param>
        /// <param name="maxDelayMs">The maximum delay between polls, in milliseconds. Default is 200ms.</param>
        /// <returns>A task that completes when the condition is satisfied or fails the test on timeout.</returns>
        private static Task EventuallyAsync(
            Func<bool> condition,
            int timeoutMs = 3500,
            int initialDelayMs = 25,
            double backoff = 1.5,
            int maxDelayMs = 200)
        {
            return EventuallyAsync(() => Task.FromResult(condition()), timeoutMs, initialDelayMs, backoff, maxDelayMs);
        }

        /// <summary>
        /// Polls a condition until it returns <see langword="true"/> or the timeout expires.
        /// Handles transient disposal exceptions as retryable.
        /// </summary>
        /// <param name="condition">An asynchronous function that returns <see langword="true"/> when the condition is satisfied.</param>
        /// <param name="timeoutMs">The maximum time, in milliseconds, to wait before failing the assertion. Default is 3500ms.</param>
        /// <param name="initialDelayMs">The initial delay between polls, in milliseconds. Default is 25ms.</param>
        /// <param name="backoff">The multiplicative backoff applied to the delay between retries. Default is 1.5.</param>
        /// <param name="maxDelayMs">The maximum delay between polls, in milliseconds. Default is 200ms.</param>
        /// <returns>A task that completes when the condition is satisfied or fails the test on timeout.</returns>
        private static async Task EventuallyAsync(
            Func<Task<bool>> condition,
            int timeoutMs = 3500,
            int initialDelayMs = 25,
            double backoff = 1.5,
            int maxDelayMs = 200)
        {
            var sw = Stopwatch.StartNew();
            var delay = initialDelayMs;

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                bool ok;
                try
                {
                    ok = await condition().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    ok = false;
                }
                catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
                {
                    ok = false;
                }

                if (ok)
                {
                    return;
                }

                await Task.Delay(delay).ConfigureAwait(false);
                delay = Math.Min((int)(delay * backoff), maxDelayMs);
            }

            Assert.Fail($"Condition not met within {timeoutMs}ms.");
        }

        /// <summary>
        /// Creates, configures and builds an Akavache instance using the per-test path and SQLite provider, then executes the test body.
        /// </summary>
        /// <typeparam name="TSerializer">The serializer type to use (e.g., <see cref="NewtonsoftSerializer"/> or <see cref="SystemJsonSerializer"/>).</typeparam>
        /// <param name="applicationName">Optional application name to scope the store; may be <see langword="null"/>.</param>
        /// <param name="configureAsync">An async configuration callback to register stores and/or delete existing stores before the body runs.</param>
        /// <param name="bodyAsync">The asynchronous test body that uses the configured <see cref="IAkavacheInstance"/>.</param>
        private void RunWithAkavache<TSerializer>(
            string? applicationName,
            Func<IAkavacheBuilder, Task> configureAsync,
            Func<IAkavacheInstance, Task> bodyAsync)
            where TSerializer : class, ISerializer, new()
        {
            _appBuilder
                .WithAkavache<TSerializer>(
                    applicationName,
                    async builder =>
                    {
                        builder
                            .WithSqliteProvider()
                            .WithSettingsCachePath(_cacheRoot);

                        if (configureAsync is not null)
                        {
                            await configureAsync(builder).ConfigureAwait(false);
                        }
                    },
                    async instance =>
                    {
                        await bodyAsync(instance).ConfigureAwait(false);
                    })
                .Build();
        }
    }
}
