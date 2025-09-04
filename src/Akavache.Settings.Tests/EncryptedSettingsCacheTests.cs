// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Akavache.EncryptedSqlite3;
using Akavache.NewtonsoftJson;
using Akavache.Settings;
using Akavache.Settings.Tests;
using Akavache.SystemTextJson;

using NUnit.Framework;

using Splat.Builder;

namespace Akavache.EncryptedSettings.Tests
{
    /// <summary>
    /// Tests for the encrypted settings cache, isolated per test to avoid static state leakage.
    /// Uses eventually-consistent polling and treats transient disposal as retryable.
    /// </summary>
    [TestFixture]
    [Category("Akavache")]
    [Parallelizable(ParallelScope.None)]
    public class EncryptedSettingsCacheTests
    {
        /// <summary>
        /// Default password used by a number of tests.
        /// </summary>
        private const string DefaultPassword = "test1234";

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
                "AkavacheEncryptedSettingsTests",
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
        /// Verifies that a secure settings store can be created and initial values materialize (Newtonsoft).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestCreateAndInsertNewtonsoftAsync()
        {
            var testName = NewName("newtonsoft_test");
            ViewSettings? viewSettings = null;

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
                },
                async instance =>
                {
                    try
                    {
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        // Read once after the store stabilizes instead of re-reading repeatedly.
                        await EventuallyAsync(() => TryRead(() => viewSettings!.BoolTest == true)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.ShortTest == (short)16)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.IntTest == 1)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.LongTest == 123456L)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.StringTest == "TestString")).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => Math.Abs(viewSettings!.FloatTest - 2.2f) < 0.0001f)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => Math.Abs(viewSettings!.DoubleTest - 23.8d) < 0.0001d)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option1)).ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            if (viewSettings is not null)
                            {
                                await viewSettings.DisposeAsync().ConfigureAwait(false);
                            }

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies updates are applied and readable (Newtonsoft).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestUpdateAndReadNewtonsoftAsync()
        {
            var testName = NewName("newtonsoft_update_test");
            ViewSettings? viewSettings = null;

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that a secure settings store can be created and initial values materialize (System.Text.Json).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestCreateAndInsertSystemTextJsonAsync()
        {
            var testName = NewName("systemjson_test");
            ViewSettings? viewSettings = null;

            RunWithAkavache<SystemJsonSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
                },
                async instance =>
                {
                    try
                    {
                        await EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        await EventuallyAsync(() => TryRead(() => viewSettings!.BoolTest == true)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.ShortTest == (short)16)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.IntTest == 1)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.LongTest == 123456L)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.StringTest == "TestString")).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => Math.Abs(viewSettings!.FloatTest - 2.2f) < 0.0001f)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => Math.Abs(viewSettings!.DoubleTest - 23.8d) < 0.0001d)).ConfigureAwait(false);
                        await EventuallyAsync(() => TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option1)).ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            if (viewSettings is not null)
                            {
                                await viewSettings.DisposeAsync().ConfigureAwait(false);
                            }

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies updates are applied and readable (System.Text.Json).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestUpdateAndReadSystemTextJsonAsync()
        {
            var testName = NewName("systemjson_update_test");
            ViewSettings? viewSettings = null;

            RunWithAkavache<SystemJsonSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies explicit override of <see cref="IAkavacheInstance.SettingsCachePath"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestOverrideSettingsCachePathAsync()
        {
            var path = Path.Combine(_cacheRoot, "OverridePath");
            Directory.CreateDirectory(path);

            IAkavacheInstance? akavacheInstance = null;

            _appBuilder
                .WithAkavache<SystemJsonSerializer>(
                    applicationName: null,
                    builder =>
                    {
                        builder
                            .WithEncryptedSqliteProvider()
                            .WithSettingsCachePath(path);
                    },
                    instance =>
                    {
                        akavacheInstance = instance;
                    })
                .Build();

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(akavacheInstance, Is.Not.Null);
                Assert.That(akavacheInstance!.SettingsCachePath, Is.EqualTo(path));
            }
        }

        /// <summary>
        /// Verifies that encrypted settings can be accessed across instances (sanity checks only).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(20000)]
        public async Task TestEncryptedSettingsPersistenceAsync()
        {
            var testName = NewName("persistence_test");
            ViewSettings? originalSettings = null;

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>("test_password", s => originalSettings = s, testName);
                },
                async instance =>
                {
                    try
                    {
                        await EventuallyAsync(() => originalSettings is not null).ConfigureAwait(false);

                        originalSettings!.StringTest = "Modified String";
                        originalSettings.IntTest = 999;
                        originalSettings.BoolTest = false;

                        await originalSettings.DisposeAsync().ConfigureAwait(false);

                        await EventuallyAsync(async () =>
                        {
                            try
                            {
                                var reopened = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                                var ok = reopened is not null && TryRead(() => reopened!.IntTest >= 0 && reopened.StringTest is not null);
                                if (reopened is not null)
                                {
                                    await reopened.DisposeAsync().ConfigureAwait(false);
                                }

                                return ok;
                            }
                            catch (ObjectDisposedException)
                            {
                                return false;
                            }
                            catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
                            {
                                return false;
                            }
                        }).ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies wrong password cannot read encrypted values.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(20000)]
        public async Task TestEncryptedSettingsWrongPasswordAsync()
        {
            var testName = NewName("wrong_password_test");
            ViewSettings? originalSettings = null;

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>("correct_password", s => originalSettings = s, testName);
                },
                async instance =>
                {
                    try
                    {
                        await EventuallyAsync(() => originalSettings is not null).ConfigureAwait(false);

                        originalSettings!.StringTest = "Secret Data";
                        await originalSettings.DisposeAsync().ConfigureAwait(false);

                        await instance.DisposeSettingsStore<ViewSettings>(testName).ConfigureAwait(false);

                        var wrongPasswordWorked = false;

                        await EventuallyAsync(async () =>
                        {
                            try
                            {
                                var wrong = instance.GetSecureSettingsStore<ViewSettings>("wrong_password", testName);
                                if (wrong is null)
                                {
                                    return true;
                                }

                                if (TryRead(() => wrong.StringTest == "Secret Data"))
                                {
                                    wrongPasswordWorked = true;
                                }

                                await wrong.DisposeAsync().ConfigureAwait(false);
                                return true;
                            }
                            catch (ObjectDisposedException)
                            {
                                return false;
                            }
                            catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
                            {
                                return false;
                            }
                        }).ConfigureAwait(false);

                        Assert.That(wrongPasswordWorked, Is.False, "Wrong password should not provide access to encrypted data.");
                    }
                    finally
                    {
                        try
                        {
                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies we can dispose and recreate multiple times.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(20000)]
        public async Task TestMultipleDisposeAndRecreateAsync()
        {
            var testName = NewName("multi_dispose_test");

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder
                        .WithEncryptedSqliteProvider()
                        .DeleteSettingsStore<ViewSettings>(testName)
                        .ConfigureAwait(false);
                },
                async instance =>
                {
                    try
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            var settings = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                            Assert.That(settings, Is.Not.Null);

                            settings!.IntTest = i * 100;
                            await settings.DisposeAsync().ConfigureAwait(false);

                            await EventuallyAsync(async () =>
                            {
                                try
                                {
                                    var recreated = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                                    var ok = recreated is not null && TryRead(() => recreated!.IntTest >= 0);
                                    if (recreated is not null)
                                    {
                                        await recreated.DisposeAsync().ConfigureAwait(false);
                                    }

                                    return ok;
                                }
                                catch (ObjectDisposedException)
                                {
                                    return false;
                                }
                                catch (InvalidOperationException ex) when (IsDisposedMessage(ex))
                                {
                                    return false;
                                }
                            }).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        try
                        {
                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies AppInfo properties are present.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        [CancelAfter(15000)]
        public async Task TestAppInfoPropertiesAsync()
        {
            IAkavacheInstance? akavacheInstance = null;

            _appBuilder
                .WithAkavache<SystemJsonSerializer>(
                    applicationName: null,
                    builder =>
                    {
                        builder
                            .WithApplicationName("TestAppInfo")
                            .WithEncryptedSqliteProvider()
                            .WithSettingsCachePath(_cacheRoot);
                    },
                    instance => akavacheInstance = instance)
                .Build();

            await EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(akavacheInstance, Is.Not.Null);
                Assert.That(akavacheInstance!.ExecutingAssembly, Is.Not.Null);
                Assert.That(akavacheInstance.ExecutingAssemblyName, Is.Not.Null);
                Assert.That(akavacheInstance.ApplicationRootPath, Is.Not.Null);
                Assert.That(akavacheInstance.SettingsCachePath, Is.Not.Null);
                Assert.That(akavacheInstance.Version, Is.Not.Null);
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
        /// Creates, configures and builds an Akavache instance using the per-test path and encrypted SQLite provider, then executes the test body.
        /// </summary>
        /// <typeparam name="TSerializer">The serializer type to use (e.g., <see cref="NewtonsoftSerializer"/> or <see cref="SystemJsonSerializer"/>).</typeparam>
        /// <param name="applicationName">Application name to scope the store; may be <see langword="null"/>.</param>
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
                            .WithEncryptedSqliteProvider()
                            .WithSettingsCachePath(_cacheRoot);

                        if (configureAsync is not null)
                        {
                            await configureAsync(builder).ConfigureAwait(false);
                        }
                    },
                    async instance => await bodyAsync(instance).ConfigureAwait(false))
                .Build();
        }
    }
}
