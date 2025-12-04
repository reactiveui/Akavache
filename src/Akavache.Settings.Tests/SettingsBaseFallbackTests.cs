// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Splat;
using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for SettingsBase fallback logic when no explicit cache is configured.
/// Validates the cache selection priority: explicit BlobCaches -> CacheDatabase -> InMemoryBlobCache.
/// </summary>
[TestFixture]
[Category("Akavache")]
[Parallelizable(ParallelScope.None)]
public class SettingsBaseFallbackTests
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
            "AkavacheSettingsFallbackTests",
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
            if (CacheDatabase.IsInitialized)
            {
                var shutdownTask = Task.Run(() => CacheDatabase.Shutdown().Wait());
                shutdownTask.Wait(TimeSpan.FromSeconds(5));
            }
        }
        catch
        {
            // Best-effort: don't fail tests on shutdown.
        }

        try
        {
            // Clear all registered services in AppLocator
            if (AppLocator.CurrentMutable.HasRegistration(typeof(ISerializer)))
            {
                AppLocator.CurrentMutable.UnregisterAll(typeof(ISerializer));
            }
        }
        catch
        {
            // Best-effort
        }

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
    /// Verifies that SettingsBase works with CacheDatabase when initialized.
    /// This tests the fallback to CacheDatabase.UserAccount when no explicit cache is configured.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [CancelAfter(60000)]
    public async Task TestFallbackToCacheDatabaseUserAccount()
    {
        var appName = NewName("fallback_user_account");
        TestSettings? settings = null;

        // Initialize CacheDatabase - SettingsBase should fall back to using it
        CacheDatabase.Initialize<NewtonsoftSerializer>(
            builder =>
            {
                builder.WithInMemoryDefaults();
            },
            applicationName: appName);

        await TestHelper.EventuallyAsync(() => CacheDatabase.IsInitialized).ConfigureAwait(false);

        try
        {
            // Creating a SettingsBase-derived class should fall back to CacheDatabase.UserAccount
            settings = new TestSettings();

            // Verify that the settings instance is created successfully
            await TestHelper.EventuallyAsync(() => settings is not null).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings!.TestValue, Is.EqualTo(42));
            }
        }
        finally
        {
            if (settings is not null)
            {
                await settings.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Verifies that SettingsBase works when CacheDatabase provides LocalMachine cache.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [CancelAfter(60000)]
    public async Task TestFallbackToCacheDatabaseLocalMachine()
    {
        var appName = NewName("fallback_local_machine");
        TestSettings? settings = null;

        // Initialize CacheDatabase with LocalMachine and InMemory
        CacheDatabase.Initialize<NewtonsoftSerializer>(
            builder =>
            {
                builder
                    .WithLocalMachine(new InMemoryBlobCache(new NewtonsoftSerializer()))
                    .WithInMemory(new InMemoryBlobCache(new NewtonsoftSerializer()));
            },
            applicationName: appName);

        await TestHelper.EventuallyAsync(() => CacheDatabase.IsInitialized).ConfigureAwait(false);

        try
        {
            // Creating a SettingsBase-derived class should work with available caches
            settings = new TestSettings();

            await TestHelper.EventuallyAsync(() => settings is not null).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings!.TestValue, Is.EqualTo(42));
            }
        }
        finally
        {
            if (settings is not null)
            {
                await settings.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Verifies that SettingsBase works when CacheDatabase provides only InMemory cache.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [CancelAfter(60000)]
    public async Task TestFallbackToCacheDatabaseInMemory()
    {
        var appName = NewName("fallback_in_memory");
        TestSettings? settings = null;

        // Initialize CacheDatabase with only InMemory
        CacheDatabase.Initialize<NewtonsoftSerializer>(
            builder =>
            {
                builder.WithInMemory(new InMemoryBlobCache(new NewtonsoftSerializer()));
            },
            applicationName: appName);

        await TestHelper.EventuallyAsync(() => CacheDatabase.IsInitialized).ConfigureAwait(false);

        try
        {
            // Creating a SettingsBase-derived class should work with InMemory cache
            settings = new TestSettings();

            await TestHelper.EventuallyAsync(() => settings is not null).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings!.TestValue, Is.EqualTo(42));
            }
        }
        finally
        {
            if (settings is not null)
            {
                await settings.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Verifies that SettingsBase uses System.Text.Json serializer when available.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [CancelAfter(60000)]
    public async Task TestWithSystemTextJsonSerializer()
    {
        var appName = NewName("systemjson_test");
        TestSettings? settings = null;

        // Use CacheDatabase with SystemTextJson
        CacheDatabase.Initialize<SystemJsonSerializer>(
            builder =>
            {
                builder.WithInMemoryDefaults();
            },
            applicationName: appName);

        await TestHelper.EventuallyAsync(() => CacheDatabase.IsInitialized).ConfigureAwait(false);

        try
        {
            // Creating a SettingsBase-derived class should work with SystemJsonSerializer
            settings = new TestSettings();

            await TestHelper.EventuallyAsync(() => settings is not null).ConfigureAwait(false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings, Is.Not.Null);
                Assert.That(settings!.TestValue, Is.EqualTo(42));
            }
        }
        finally
        {
            if (settings is not null)
            {
                await settings.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Verifies that SettingsBase works with settings persistence using explicit settings store.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [CancelAfter(60000)]
    public async Task TestSettingsPersistenceAcrossInstances()
    {
        var appName = NewName("persistence_test");
        const int expectedValue = 999;
        TestSettings? settings1 = null;
        TestSettings? settings2 = null;

        RunWithAkavache<NewtonsoftSerializer>(
            appName,
            async builder =>
            {
                await builder.DeleteSettingsStore<TestSettings>().ConfigureAwait(false);
                builder.WithSettingsStore<TestSettings>(s =>
                {
                    if (settings1 == null)
                    {
                        settings1 = s;
                    }
                    else
                    {
                        settings2 = s;
                    }
                });
            },
            async instance =>
            {
                try
                {
                    // First, set a value
                    await TestHelper.EventuallyAsync(() => settings1 is not null).ConfigureAwait(false);
                    settings1!.TestValue = expectedValue;
                    await TestHelper.EventuallyAsync(() => settings1.TestValue == expectedValue).ConfigureAwait(false);

                    // Dispose the first instance
                    await settings1.DisposeAsync().ConfigureAwait(false);
                    settings1 = null;

                    // Get a new instance
                    settings2 = instance.GetSettingsStore<TestSettings>();
                    await TestHelper.EventuallyAsync(() => settings2 is not null).ConfigureAwait(false);

                    // Verify the value persisted
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(settings2, Is.Not.Null);
                        Assert.That(settings2!.TestValue, Is.EqualTo(expectedValue));
                    }
                }
                finally
                {
                    try
                    {
                        if (settings1 is not null)
                        {
                            await settings1.DisposeAsync().ConfigureAwait(false);
                        }

                        if (settings2 is not null)
                        {
                            await settings2.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // Best-effort cleanup
                    }
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

    /// <summary>
    /// A simple test settings class for verifying fallback logic.
    /// </summary>
    private class TestSettings : SettingsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSettings"/> class.
        /// </summary>
        public TestSettings()
            : base(nameof(TestSettings))
        {
        }

        /// <summary>
        /// Gets or sets the test value.
        /// </summary>
        public int TestValue
        {
            get => GetOrCreate(42);
            set => SetOrCreate(value);
        }
    }
}
