// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Settings.Core;
using Akavache.Sqlite3;
using Akavache.Tests;
using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for SettingsBase fallback logic when no explicit cache is configured.
/// Validates the cache selection priority: explicit BlobCaches -> CacheDatabase -> InMemoryBlobCache.
/// </summary>
[Category("Akavache")]
[TestExecutor<AkavacheTestExecutor>]
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
    [Before(Test)]
    public void Setup()
    {
        _appBuilder = AppBuilder.CreateSplatBuilder();

        _cacheRoot = Path.Combine(
            Path.GetTempPath(),
            "AkavacheSettingsFallbackTests",
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
    /// Verifies that SettingsBase works with CacheDatabase when initialized.
    /// This tests the fallback to CacheDatabase.UserAccount when no explicit cache is configured.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestFallbackToCacheDatabaseUserAccount()
    {
        var appName = NewName("fallback_user_account");
        TestSettings? settings = null;

        // Initialize CacheDatabase - SettingsBase should fall back to using it
        CacheDatabase.Initialize<NewtonsoftSerializer>(
            builder => builder.WithInMemoryDefaults(),
            applicationName: appName);

        await TestHelper.EventuallyAsync(() => CacheDatabase.IsInitialized).ConfigureAwait(false);

        try
        {
            // Creating a SettingsBase-derived class should fall back to CacheDatabase.UserAccount
            settings = new();

            // Verify that the settings instance is created successfully
            await TestHelper.EventuallyAsync(() => settings is not null).ConfigureAwait(false);

            await Assert.That(settings).IsNotNull();
            await Assert.That((int)settings.TestValue).IsEqualTo(42);
        }
        finally
        {
            settings?.Dispose();
        }
    }

    /// <summary>
    /// Verifies that SettingsBase works with settings persistence using explicit settings store.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Test deliberately uses synchronous Rx Subscribe patterns to avoid sync-over-async deadlocks.")]
    public async Task TestSettingsPersistenceAcrossInstances()
    {
        var appName = NewName("persistence_test");
        const int expectedValue = 999;

        // Set up Akavache directly — capture instance from the callback
        // to avoid relying on CacheDatabase.CurrentInstance (which the test
        // executor may have reset).
        IAkavacheInstance? instance = null;
        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                appName,
                builder => builder
                    .WithSqliteProvider()
                    .WithSettingsCachePath(_cacheRoot),
                i => instance = i)
            .Build();

        try
        {
            // Delete any leftover store from prior runs.
            instance!.DeleteSettingsStore<TestSettings>().WaitForCompletion();

            // Create the first settings instance.
            var settings1 = instance!.GetSettingsStore<TestSettings>(
                overrideDatabaseName: null,
                scheduler: ImmediateScheduler.Instance);
            settings1.TestValue.Set(expectedValue).SubscribeAndComplete();

            // Verify the value was set.
            await Assert.That((int)settings1.TestValue).IsEqualTo(expectedValue);

            // Dispose the first instance.
            settings1.Dispose();

            // Create a second instance and verify the value persisted.
            var settings2 = instance!.GetSettingsStore<TestSettings>(
                overrideDatabaseName: null,
                scheduler: ImmediateScheduler.Instance);
            settings2.Initialize().WaitForCompletion();

            await Assert.That((int)settings2.TestValue).IsEqualTo(expectedValue);

            settings2.Dispose();
        }
        finally
        {
            CacheDatabase.ResetForTests().SubscribeAndComplete();
        }

        await TestHelper.EventuallyAsync(() => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a unique, human-readable test name prefix plus a GUID segment.
    /// </summary>
    /// <param name="prefix">A short, descriptive prefix for the test resource name.</param>
    /// <returns>A unique name string suitable for use as an application name or store key.</returns>
    private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    /// <summary>
    /// A simple test settings class for verifying fallback logic. Exercises the
    /// <see cref="SettingsPropertyHelper{T}"/> pattern — sync <c>Value</c> via the
    /// implicit conversion, <c>Set</c> method for writes.
    /// </summary>
    private sealed class TestSettings : SettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="TestSettings"/> class.</summary>
        public TestSettings()
            : base(nameof(TestSettings)) => TestValue = CreateProperty(42, nameof(TestValue));

        /// <summary>Gets the test value property helper.</summary>
        public SettingsPropertyHelper<int> TestValue { get; }
    }
}
