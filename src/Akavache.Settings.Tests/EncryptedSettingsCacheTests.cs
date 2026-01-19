// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.EncryptedSqlite3;
using Akavache.NewtonsoftJson;
using Akavache.Settings;
using Akavache.Settings.Tests;
using Akavache.SystemTextJson;
using Splat.Builder;

namespace Akavache.EncryptedSettings.Tests
{
    /// <summary>
    /// Tests for the encrypted settings cache, isolated per test to avoid static state leakage.
    /// Uses eventually-consistent polling and treats transient disposal as retryable.
    /// </summary>
    [Category("Akavache")]
    [NotInParallel]
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
        [Before(Test)]
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
        /// Verifies that a secure settings store can be created and initial values materialize (Newtonsoft).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                        await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        // Read once after the store stabilizes instead of re-reading repeatedly.
                        await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.BoolTest))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.ShortTest == 16))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.IntTest == 1))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.LongTest == 123456L))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.StringTest == "TestString"))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() =>
                                TestHelper.TryRead(() => Math.Abs(viewSettings!.FloatTest - 2.2f) < 0.0001f))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() =>
                                TestHelper.TryRead(() => Math.Abs(viewSettings!.DoubleTest - 23.8d) < 0.0001d))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() =>
                                TestHelper.TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option1))
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies updates are applied and readable (Newtonsoft).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                        await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        // Perform the mutation in a fresh store, retrying on transient disposal.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            return await TestHelper.WithFreshStoreAsync(
                                instance,
                                () => instance.GetSecureSettingsStore<ViewSettings>(DefaultPassword, testName),
                                async s =>
                                {
                                    s.EnumTest = EnumTestValue.Option2;
                                    var ok = TestHelper.TryRead(() => s.EnumTest == EnumTestValue.Option2);
                                    await Task.Yield();
                                    return ok;
                                }).ConfigureAwait(false);
                        }).ConfigureAwait(false);

                        // Optional: also observe the change via the originally captured instance (retryable read).
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that a secure settings store can be created and initial values materialize (System.Text.Json).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                        await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.BoolTest))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.ShortTest == 16))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.IntTest == 1))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.LongTest == 123456L))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.StringTest == "TestString"))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() =>
                                TestHelper.TryRead(() => Math.Abs(viewSettings!.FloatTest - 2.2f) < 0.0001f))
                            .ConfigureAwait(false);
                        await TestHelper.EventuallyAsync(() =>
                                TestHelper.TryRead(() => Math.Abs(viewSettings!.DoubleTest - 23.8d) < 0.0001d))
                            .ConfigureAwait(false);
                        await TestHelper
                            .EventuallyAsync(() =>
                                TestHelper.TryRead(() => viewSettings!.EnumTest == EnumTestValue.Option1))
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies updates are applied and readable (System.Text.Json).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                        await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                        // Perform the mutation in a fresh store, retrying on transient disposal.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            return await TestHelper.WithFreshStoreAsync(
                                instance,
                                () => instance.GetSecureSettingsStore<ViewSettings>(DefaultPassword, testName),
                                async s =>
                                {
                                    s.EnumTest = EnumTestValue.Option2;
                                    var ok = TestHelper.TryRead(() => s.EnumTest == EnumTestValue.Option2);
                                    await Task.Yield();
                                    return ok;
                                }).ConfigureAwait(false);
                        }).ConfigureAwait(false);

                        // Optional: also verify via the initially captured instance.
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

                            await instance.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignore cleanup issues.
                        }
                    }
                });

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies explicit override of <see cref="IAkavacheInstance.SettingsCachePath"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                    instance => akavacheInstance = instance)
                .Build();

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

            await Assert.That(akavacheInstance).IsNotNull();
            await Assert.That(akavacheInstance!.SettingsCachePath).IsEqualTo(path);
        }

        /// <summary>
        /// Verifies that encrypted settings can be accessed across instances (sanity checks only).
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                        await TestHelper.EventuallyAsync(() => originalSettings is not null).ConfigureAwait(false);

                        // Perform all writes via a fresh store with retryable semantics.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            return await TestHelper.WithFreshStoreAsync(
                                instance,
                                () => instance.GetSecureSettingsStore<ViewSettings>("test_password", testName),
                                async s =>
                                {
                                    s.StringTest = "Modified String";
                                    s.IntTest = 999;
                                    s.BoolTest = false;

                                    var ok = TestHelper.TryRead(() =>
                                        s.StringTest is not null && s is { IntTest: 999, BoolTest: false });
                                    await Task.Yield();
                                    return ok;
                                }).ConfigureAwait(false);
                        }).ConfigureAwait(false);

                        // Dispose the initially captured instance to release any handles.
                        if (originalSettings is not null)
                        {
                            await originalSettings.DisposeAsync().ConfigureAwait(false);
                        }

                        // Re-open and sanity-check.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            try
                            {
                                var reopened = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                                var ok = reopened is not null &&
                                         TestHelper.TryRead(() =>
                                             reopened is { IntTest: >= 0, StringTest: not null });
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
                            catch (InvalidOperationException ex) when (ex.IsDisposedMessage())
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

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies wrong password cannot read encrypted values.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
        public async Task TestEncryptedSettingsWrongPasswordAsync()
        {
            var testName = NewName("wrong_password_test");
            ViewSettings? initialSettings = null;

            RunWithAkavache<NewtonsoftSerializer>(
                testName,
                async builder =>
                {
                    await builder.DeleteSettingsStore<ViewSettings>(testName).ConfigureAwait(false);
                    builder.WithSecureSettingsStore<ViewSettings>(
                        "correct_password",
                        s => initialSettings = s,
                        testName);
                },
                async instance =>
                {
                    try
                    {
                        // Wait until the initial store is created.
                        await TestHelper.EventuallyAsync(() => initialSettings is not null).ConfigureAwait(false);

                        // IMPORTANT: Do NOT write using the captured 'initialSettings'.
                        // Instead, open a *fresh* store, perform the write, and dispose it — retrying on transient disposal.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            return await TestHelper.WithFreshStoreAsync(
                                instance,
                                () => instance.GetSecureSettingsStore<ViewSettings>("correct_password", testName),
                                async s =>
                                {
                                    s.StringTest = "Secret Data";

                                    // Optionally verify the value round-trips in the same fresh store.
                                    var ok = TestHelper.TryRead(() => s.StringTest == "Secret Data");
                                    await Task.Yield();
                                    return ok;
                                }).ConfigureAwait(false);
                        }).ConfigureAwait(false);

                        // Release the initial settings instance after we've successfully written using a fresh store.
                        if (initialSettings is not null)
                        {
                            await initialSettings.DisposeAsync().ConfigureAwait(false);
                        }

                        // Fully release file handles to avoid race on Windows paths.
                        await instance.DisposeSettingsStore<ViewSettings>(testName).ConfigureAwait(false);

                        var wrongPasswordWorked = false;

                        // Now attempt to read with the *wrong* password. This should never surface the secret.
                        await TestHelper.EventuallyAsync(async () =>
                        {
                            try
                            {
                                var wrong = instance.GetSecureSettingsStore<ViewSettings>("wrong_password", testName);
                                if (wrong is null)
                                {
                                    return true; // acceptable outcome
                                }

                                if (TestHelper.TryRead(() => wrong.StringTest == "Secret Data"))
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
                            catch (InvalidOperationException ex) when (ex.IsDisposedMessage())
                            {
                                return false;
                            }
                        }).ConfigureAwait(false);

                        await Assert.That(wrongPasswordWorked)
                            .IsFalse()
                            .Because("Wrong password should not provide access to encrypted data.");
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

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies we can dispose and recreate multiple times.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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
                            var index = i;

                            // Write with retry against transient disposal using a fresh store per attempt.
                            await TestHelper.EventuallyAsync(async () =>
                            {
                                return await TestHelper.WithFreshStoreAsync(
                                    instance,
                                    () => instance.GetSecureSettingsStore<ViewSettings>("test_password", testName),
                                    async s =>
                                    {
                                        s.IntTest = index * 100;
                                        var ok = TestHelper.TryRead(() => s.IntTest >= 0);
                                        await Task.Yield();
                                        return ok;
                                    }).ConfigureAwait(false);
                            }).ConfigureAwait(false);

                            // Verify we can reopen and read something sane.
                            await TestHelper.EventuallyAsync(async () =>
                            {
                                try
                                {
                                    var recreated =
                                        instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                                    var ok = recreated is not null && TestHelper.TryRead(() => recreated.IntTest >= 0);
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
                                catch (InvalidOperationException ex) when (ex.IsDisposedMessage())
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

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies AppInfo properties are present.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Test]
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

            await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

            await Assert.That(akavacheInstance).IsNotNull();
            await Assert.That(akavacheInstance!.ExecutingAssembly).IsNotNull();
            await Assert.That(akavacheInstance.ExecutingAssemblyName).IsNotNull();
            await Assert.That(akavacheInstance.ApplicationRootPath).IsNotNull();
            await Assert.That(akavacheInstance.SettingsCachePath).IsNotNull();
            await Assert.That(akavacheInstance.Version).IsNotNull();
        }

        /// <summary>
        /// Creates a unique, human-readable test name prefix plus a GUID segment.
        /// </summary>
        /// <param name="prefix">A short, descriptive prefix for the test resource name.</param>
        /// <returns>A unique name string suitable for use as an application name or store key.</returns>
        private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

        /// <summary>
        /// Creates, configures and builds an Akavache instance using the per-test path and encrypted SQLite provider, then executes the test body.
        /// This version blocks on async delegates to avoid async-void and ensure assertion scopes close before the test ends.
        /// </summary>
        /// <typeparam name="TSerializer">The serializer type to use (e.g., <see cref="NewtonsoftSerializer"/> or <see cref="SystemJsonSerializer"/>).</typeparam>
        /// <param name="applicationName">Application name to scope the store; may be <see langword="null"/>.</param>
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
                            .WithEncryptedSqliteProvider()
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
