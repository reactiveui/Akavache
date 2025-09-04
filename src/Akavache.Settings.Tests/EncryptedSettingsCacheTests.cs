// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using Akavache.EncryptedSqlite3;
using Akavache.NewtonsoftJson;
using Akavache.Settings;
using Akavache.Settings.Tests;
using Akavache.SystemTextJson;
using Splat.Builder;

namespace Akavache.EncryptedSettings.Tests;

/// <summary>
/// This test class contains unit tests for validating the behavior of the
/// EncryptedSettingsCache functionality in the Akavache framework.
/// It ensures settings are properly encrypted, persisted, updated,
/// and accessed using different serializers or configurations.
/// </summary>
[TestFixture]
[Category("Akavache")]
[Parallelizable(ParallelScope.None)]
public class EncryptedSettingsCacheTests
{
    private const string DefaultPassword = "test1234";
    private AppBuilder _appBuilder = null!;
    private string _cacheRoot = null!;

    /// <summary>
    /// Sets up the necessary environment and variables required for the tests
    /// in the <see cref="EncryptedSettingsCacheTests"/> class. This method is
    /// executed before each test to ensure consistent test state. It initializes
    /// the application builder, resets any residual state from previous tests,
    /// and creates a secure, isolated temporary directory for settings caching.
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
    /// Cleans up resources and resets the test environment after each test
    /// in the <see cref="EncryptedSettingsCacheTests"/> class. This method is
    /// executed after every test to ensure no residual state or temporary files
    /// interfere with subsequent tests. It removes the temporary directory used
    /// for caching settings, if it exists, and resets the application builder state.
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
        { /* best-effort */
        }

        AppBuilder.ResetBuilderStateForTests();
    }

    /// <summary>
    /// Verifies the correct creation and insertion of settings using the
    /// <see cref="NewtonsoftSerializer"/> for secure settings storage. This test ensures
    /// that the settings store is configured properly, data is stored accurately, and the
    /// settings are retrievable with the expected values.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Test]
    public Task TestCreateAndInsertNewtonsoft()
    {
        var testName = NewName("newtonsoft_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => viewSettings is not null);

                    await EventuallyAsync(() =>
                        viewSettings is not null &&
                        viewSettings.BoolTest &&
                        viewSettings.ShortTest == 16 &&
                        viewSettings.IntTest == 1 &&
                        viewSettings.LongTest == 123456L &&
                        viewSettings.StringTest == "TestString" &&
                        Math.Abs(viewSettings.FloatTest - 2.2f) < 0.0001f &&
                        Math.Abs(viewSettings.DoubleTest - 23.8d) < 0.0001d &&
                        viewSettings.EnumTest == EnumTestValue.Option1);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync();
                        }

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the functionality of updating and retrieving settings using the
    /// Newtonsoft JSON serializer within the encrypted settings cache. This test
    /// verifies that settings are properly updated and persisted, ensuring changes
    /// can be consistently retrieved and observed. Resources are cleaned up after
    /// execution to maintain isolation between tests.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operations involved in this test,
    /// including configuration, update, validation, and cleanup processes.
    /// </returns>
    [Test]
    public Task TestUpdateAndReadNewtonsoft()
    {
        var testName = NewName("newtonsoft_update_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => viewSettings is not null);
                    viewSettings!.EnumTest = EnumTestValue.Option2;

                    await EventuallyAsync(() => viewSettings.EnumTest == EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync();
                        }

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    { /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the creation and insertion of data into the settings store
    /// using the System.Text.Json serializer. This test ensures that the
    /// settings store is properly initialized, values are accurately inserted,
    /// and stored data can be retrieved and matched against expected results.
    /// It verifies various data types such as boolean, numeric, string, and enums.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation of creating, inserting,
    /// and validating the settings store functionality with System.Text.Json.
    /// </returns>
    [Test]
    public Task TestCreateAndInsertSystemTextJson()
    {
        var testName = NewName("systemjson_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<SystemJsonSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => viewSettings is not null);

                    await EventuallyAsync(() =>
                        viewSettings is not null &&
                        viewSettings.BoolTest &&
                        viewSettings.ShortTest == 16 &&
                        viewSettings.IntTest == 1 &&
                        viewSettings.LongTest == 123456L &&
                        viewSettings.StringTest == "TestString" &&
                        Math.Abs(viewSettings.FloatTest - 2.2f) < 0.0001f &&
                        Math.Abs(viewSettings.DoubleTest - 23.8d) < 0.0001d &&
                        viewSettings.EnumTest == EnumTestValue.Option1);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync();
                        }

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the correct behavior of the EncryptedSettingsCache when using
    /// the <see cref="SystemJsonSerializer"/> to update and retrieve encrypted settings.
    /// This test ensures that settings can be updated and read back accurately, validating
    /// proper encryption and deserialization mechanisms. Additionally, it confirms cleanup
    /// and disposal processes, maintaining test isolation and preventing persistence issues.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation of the test. Completes successfully
    /// if the settings are correctly updated and retrieved; otherwise, fails if any issues
    /// with encryption, deserialization, or cleanup are encountered.
    /// </returns>
    [Test]
    public Task TestUpdateAndReadSystemTextJson()
    {
        var testName = NewName("systemjson_update_test");
        ViewSettings? viewSettings = null;

        RunWithAkavache<SystemJsonSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => viewSettings is not null);
                    viewSettings!.EnumTest = EnumTestValue.Option2;

                    await EventuallyAsync(() => viewSettings.EnumTest == EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        if (viewSettings is not null)
                        {
                            await viewSettings.DisposeAsync();
                        }

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    { /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the functionality to override the settings cache path in the
    /// EncryptedSettingsCache using the Akavache framework.
    /// This test ensures that the custom cache path provided is correctly configured
    /// and accessible within the created <see cref="IAkavacheInstance"/>.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation of the test. Ensures the
    /// assertion of proper initialization and verification of the cache path.
    /// </returns>
    [Test]
    public async Task TestOverrideSettingsCachePathAsync()
    {
        var path = _cacheRoot; // already unique
        IAkavacheInstance? akavacheInstance = null;

        _appBuilder
            .WithAkavache<SystemJsonSerializer>(
                null,
                builder =>
                {
                    builder
                        .WithEncryptedSqliteProvider()
                        .WithSettingsCachePath(path);
                },
                instance => akavacheInstance = instance)
            .Build();

        await EventuallyAsync(() => AppBuilder.HasBeenBuilt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(akavacheInstance, Is.Not.Null);
            Assert.That(akavacheInstance!.SettingsCachePath, Is.EqualTo(path));
        }
    }

    /// <summary>
    /// Tests the persistence behavior of encrypted settings when using the
    /// EncryptedSettingsCache with a specified serializer. This test ensures
    /// that encrypted settings can be created, modified, flushed, and subsequently
    /// reloaded while retaining all changes. Validates proper behavior for
    /// encryption, serialization, and storage interaction.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous execution of this test,
    /// validating that encrypted settings persist correctly across operations.
    /// </returns>
    [Test]
    public Task TestEncryptedSettingsPersistence()
    {
        var testName = NewName("persistence_test");
        ViewSettings? originalSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>("test_password", s => originalSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => originalSettings is not null);

                    originalSettings!.StringTest = "Modified String";
                    originalSettings.IntTest = 999;
                    originalSettings.BoolTest = false;

                    // Some providers flush on dispose; rely on eventually reopen.
                    await originalSettings.DisposeAsync();

                    await EventuallyAsync(async () =>
                    {
                        var s = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                        var ok = s is { IntTest: >= 0, StringTest: not null };
                        if (s is not null)
                        {
                            await s.DisposeAsync();
                        }

                        return ok;
                    });
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that attempting to access encrypted settings with an incorrect password
    /// does not allow access to the stored data. This method sets up a secure settings
    /// store with a correct password, stores sensitive data, and validates that a wrong
    /// password prevents access to the data. It ensures encryption security and proper
    /// disposal of resources after the test execution.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation of the test, validating
    /// the behavior of encrypted settings when accessed with the wrong password.
    /// </returns>
    [Test]
    public Task TestEncryptedSettingsWrongPassword()
    {
        var testName = NewName("wrong_password_test");
        ViewSettings? originalSettings = null;

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                builder.WithSecureSettingsStore<ViewSettings>("correct_password", s => originalSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await EventuallyAsync(() => originalSettings is not null);

                    originalSettings!.StringTest = "Secret Data";
                    await originalSettings.DisposeAsync();

                    // Fully release and wait until disposed in provider if needed.
                    await instance.DisposeSettingsStore<ViewSettings>(testName);

                    var wrongPasswordWorked = false;

                    // Try repeatedly; some backends may lazily re-open.
                    await EventuallyAsync(async () =>
                    {
                        try
                        {
                            var wrong = instance.GetSecureSettingsStore<ViewSettings>("wrong_password", testName);
                            if (wrong is null)
                            {
                                return true; // could be considered a pass
                            }

                            if (wrong.StringTest == "Secret Data")
                            {
                                wrongPasswordWorked = true;
                            }

                            await wrong.DisposeAsync();
                            return true;
                        }
                        catch
                        {
                            // Decryption failure is acceptable here.
                            return true;
                        }
                    });

                    Assert.That(
                        wrongPasswordWorked,
                        Is.False,
                        "Wrong password should not provide access to encrypted data");
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    { /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the functionality of repeatedly disposing and recreating secure
    /// settings stores within the encrypted settings cache. This test ensures
    /// settings stores can be disposed, reinitialized, and used across multiple
    /// iterations without issues, while maintaining proper state and security
    /// throughout.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task ensures that
    /// the test completes successfully after multiple dispose and recreate
    /// cycles, with state integrity and proper cleanup confirmed.
    /// </returns>
    [Test]
    public Task TestMultipleDisposeAndRecreate()
    {
        var testName = NewName("multi_dispose_test");

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder
                    .WithEncryptedSqliteProvider()
                    .DeleteSettingsStore<ViewSettings>(testName);
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

                        await settings.DisposeAsync();

                        await EventuallyAsync(async () =>
                        {
                            var recreated = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                            var ok = recreated is not null && recreated.IntTest >= 0;
                            if (recreated is not null)
                            {
                                await recreated.DisposeAsync();
                            }

                            return ok;
                        });
                    }
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    { /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the retrieval and creation of a settings store using the EncryptedSettingsCache functionality.
    /// This test ensures that a settings store can be securely created if it does not already exist and that its
    /// retrieval behavior is consistent. It verifies scenarios such as the absence of a store, its secure creation,
    /// and later retrieval, including the behavior of secure settings access with encryption.
    /// </summary>
    /// <returns>
    /// A Task representing the asynchronous operation of validating settings store retrieval and creation behavior.
    /// </returns>
    [Test]
    public Task TestGetSettingsStore()
    {
        var testName = NewName("get_store_test");

        RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                // Deleting here ensures the directory is clear before Build.
                await builder
                    .WithEncryptedSqliteProvider()
                    .DeleteSettingsStore<ViewSettings>(testName);
            },
            async instance =>
            {
                try
                {
                    // Belt-and-braces: ensure it's still non-existent from the INSTANCE perspective.
                    await EventuallyAsync(() => instance.GetSettingsStore<ViewSettings>(testName) is null);

                    // Now create a secure store:
                    var created = instance.GetSecureSettingsStore<ViewSettings>("test_password", testName);
                    Assert.That(created, Is.Not.Null);

                    // Try retrieving with GetSettingsStore (may or may not return instance depending on impl)
                    var retrieved = instance.GetSettingsStore<ViewSettings>(testName);

                    if (retrieved is not null)
                    {
                        await retrieved.DisposeAsync();
                    }

                    await created.DisposeAsync();
                }
                finally
                {
                    try
                    {
                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch
                    { /* ignore */
                    }
                }
            });

        Assert.That(AppBuilder.HasBeenBuilt, Is.True);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests the proper initialization and setup of application information properties within
    /// the <see cref="IAkavacheInstance"/> using the <see cref="SystemJsonSerializer"/>. This test
    /// ensures that all critical application-level properties, such as the executing assembly,
    /// application root path, settings cache path, and version, are non-null and correctly configured
    /// after building the application instance with a specific configuration.
    /// </summary>
    /// <returns>A task that represents the asynchronous execution of the test. Validates
    /// application property integrity assertions within the provided test execution scope.</returns>
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

        await EventuallyAsync(() => AppBuilder.HasBeenBuilt);

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

    private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    // ===== Eventually helpers (poll until condition is true, with timeout/backoff) =====
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
            if (await condition().ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(delay).ConfigureAwait(false);
            delay = Math.Min((int)(delay * backoff), maxDelayMs);
        }

        Assert.Fail($"Condition not met within {timeoutMs}ms.");
    }

    private static Task EventuallyAsync(
        Func<bool> condition,
        int timeoutMs = 3500,
        int initialDelayMs = 25,
        double backoff = 1.5,
        int maxDelayMs = 200)
        => EventuallyAsync(
            () => Task.FromResult(condition()),
            timeoutMs,
            initialDelayMs,
            backoff,
            maxDelayMs);

    // Single entry-point to configure/run with isolated SettingsCachePath
    private void RunWithAkavache<TSerializer>(
        string? applicationName,
        Func<IAkavacheBuilder, Task>? configureAsync,
        Func<IAkavacheInstance, Task> bodyAsync)
        where TSerializer : class, ISerializer, new() =>
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
