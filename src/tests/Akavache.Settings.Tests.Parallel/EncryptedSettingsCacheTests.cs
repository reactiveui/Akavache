// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.Builder;

#if REACTIVE_SHIM
namespace Akavache.Reactive.EncryptedSettings.Tests;
#else
namespace Akavache.EncryptedSettings.Tests;
#endif

/// <summary>
/// Tests for the encrypted settings cache, isolated per test to avoid static state leakage.
/// Uses eventually-consistent polling and treats transient disposal as retryable.
/// </summary>
[Category("Akavache")]
public class EncryptedSettingsCacheTests
{
    /// <summary>Default password used by a number of tests.</summary>
    private const string DefaultPassword = "test1234";

    /// <summary>Password used by the tests that reopen an encrypted store to check persistence.</summary>
    private const string PersistencePassword = "test_password";

    /// <summary>The password the wrong-password test writes the secret under.</summary>
    private const string CorrectPassword = "correct_password";

    /// <summary>The password the wrong-password test then tries to read the secret with.</summary>
    private const string IncorrectPassword = "wrong_password";

    /// <summary>The value that must stay unreadable when the wrong password is supplied.</summary>
    private const string SecretPayload = "Secret Data";

    /// <summary>The seeded <c>ShortTest</c> value expected after a round trip.</summary>
    private const short ExpectedShortSetting = 16;

    /// <summary>The seeded <c>LongTest</c> value expected after a round trip.</summary>
    private const long ExpectedLongSetting = 123_456L;

    /// <summary>The seeded <c>FloatTest</c> value expected after a round trip.</summary>
    private const float ExpectedFloatSetting = 2.2F;

    /// <summary>The seeded <c>DoubleTest</c> value expected after a round trip.</summary>
    private const double ExpectedDoubleSetting = 23.8D;

    /// <summary>Tolerance applied when comparing the single-precision setting.</summary>
    private const float FloatComparisonTolerance = 0.0001F;

    /// <summary>Tolerance applied when comparing the double-precision setting.</summary>
    private const double DoubleComparisonTolerance = 0.0001D;

    /// <summary>The <c>IntTest</c> value written over the seeded default by the persistence test.</summary>
    private const int ModifiedIntSetting = 999;

    /// <summary>How many dispose/recreate rounds the multi-dispose test performs.</summary>
    private const int RecreateRoundCount = 3;

    /// <summary>The gap between the <c>IntTest</c> values written by successive recreate rounds.</summary>
    private const int RecreateValueStride = 100;

    /// <summary>The per-test <see cref="AppBuilder"/> instance.</summary>
    private AppBuilder _appBuilder = null!;

    /// <summary>The unique per-test cache root path (directory).</summary>
    private string _cacheRoot = null!;

    /// <summary>One-time setup that runs before each test. Creates a fresh builder and an isolated cache path.</summary>
    [Before(Test)]
    public void Setup()
    {
        _appBuilder = AppBuilder.CreateSplatBuilder();

        _cacheRoot = Path.Combine(
            Path.GetTempPath(),
            "AkavacheEncryptedSettingsTests",
            Guid.NewGuid().ToString("N"),
            "ApplicationSettings");

        _ = Directory.CreateDirectory(_cacheRoot);
    }

    /// <summary>One-time teardown after each test. Best-effort cleanup.</summary>
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

    /// <summary>Verifies that a secure settings store can be created and initial values materialize (Newtonsoft).</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestCreateAndInsertNewtonsoftAsync()
    {
        var testName = NewName("newtonsoft_test");
        ViewSettings? viewSettings = null;

        await RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
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
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.ShortTest == ExpectedShortSetting))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.IntTest == 1))
                        .ConfigureAwait(false);
                    await TestHelper
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.LongTest == ExpectedLongSetting))
                        .ConfigureAwait(false);
                    await TestHelper
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.StringTest == "TestString"))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() =>
                                Math.Abs(viewSettings!.FloatTest - ExpectedFloatSetting) < FloatComparisonTolerance))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() =>
                                Math.Abs(viewSettings!.DoubleTest - ExpectedDoubleSetting) < DoubleComparisonTolerance))
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
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch (Exception ex)
                    {
                        // Ignore cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies updates are applied and readable (Newtonsoft).</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestUpdateAndReadNewtonsoftAsync()
    {
        var testName = NewName("newtonsoft_update_test");
        ViewSettings? viewSettings = null;

        await RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    // Mutate directly on the captured store
                    viewSettings!.EnumTest.Set(EnumTestValue.Option2).WaitForCompletion();

                    // Verify the value is readable from the same instance
                    await TestHelper.EventuallyAsync(
                        () => TestHelper.TryRead(() => viewSettings.EnumTest == EnumTestValue.Option2)).ConfigureAwait(false);

                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch (Exception ex)
                    {
                        // Ignore cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies that a secure settings store can be created and initial values materialize (System.Text.Json).</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestCreateAndInsertSystemTextJsonAsync()
    {
        var testName = NewName("systemjson_test");
        ViewSettings? viewSettings = null;

        await RunWithAkavache<SystemJsonSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.BoolTest))
                        .ConfigureAwait(false);
                    await TestHelper
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.ShortTest == ExpectedShortSetting))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.IntTest == 1))
                        .ConfigureAwait(false);
                    await TestHelper
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.LongTest == ExpectedLongSetting))
                        .ConfigureAwait(false);
                    await TestHelper
                        .EventuallyAsync(() => TestHelper.TryRead(() => viewSettings!.StringTest == "TestString"))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() =>
                                Math.Abs(viewSettings!.FloatTest - ExpectedFloatSetting) < FloatComparisonTolerance))
                        .ConfigureAwait(false);
                    await TestHelper.EventuallyAsync(() =>
                            TestHelper.TryRead(() =>
                                Math.Abs(viewSettings!.DoubleTest - ExpectedDoubleSetting) < DoubleComparisonTolerance))
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
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch (Exception ex)
                    {
                        // Ignore cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies updates are applied and readable (System.Text.Json).</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestUpdateAndReadSystemTextJsonAsync()
    {
        var testName = NewName("systemjson_update_test");
        ViewSettings? viewSettings = null;

        await RunWithAkavache<SystemJsonSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(DefaultPassword, s => viewSettings = s, testName);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    // Mutate directly on the captured store
                    viewSettings!.EnumTest.Set(EnumTestValue.Option2).WaitForCompletion();

                    // Verify the value is readable from the same instance
                    await TestHelper.EventuallyAsync(
                        () => TestHelper.TryRead(() => viewSettings.EnumTest == EnumTestValue.Option2)).ConfigureAwait(false);

                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>(testName);
                    }
                    catch (Exception ex)
                    {
                        // Ignore cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies explicit override of <see cref="IAkavacheInstance.SettingsCachePath"/>.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestOverrideSettingsCachePathAsync()
    {
        var path = Path.Combine(_cacheRoot, "OverridePath");
        _ = Directory.CreateDirectory(path);

        IAkavacheInstance? akavacheInstance = null;

        _ = _appBuilder
            .WithAkavache<SystemJsonSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    _ = builder
                        .WithEncryptedSqliteProvider()
                        .WithSettingsCachePath(path);
                },
                instance => akavacheInstance = instance)
            .Build();

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(akavacheInstance).IsNotNull();
        await Assert.That(akavacheInstance!.SettingsCachePath).IsEqualTo(path);
    }

    /// <summary>Verifies that encrypted settings can be accessed across instances (sanity checks only).</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestEncryptedSettingsPersistenceAsync()
    {
        var testName = NewName("persistence_test");
        ViewSettings? originalSettings = null;

        await RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(
                    PersistencePassword,
                    s => originalSettings = s,
                    testName);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => originalSettings is not null).ConfigureAwait(false);

                    // Release the store the builder created before opening a second one on the same
                    // file. Two live connections to one encrypted database contend on the schema
                    // write, which surfaces as SQLITE_BUSY.
                    originalSettings?.Dispose();

                    await WriteModifiedSettingsAsync(instance, testName).ConfigureAwait(false);

                    await ReopenAndVerifyAsync(instance, testName).ConfigureAwait(false);
                }
                finally
                {
                    await DeleteStoreQuietlyAsync(instance, testName).ConfigureAwait(false);
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies wrong password cannot read encrypted values.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestEncryptedSettingsWrongPasswordAsync()
    {
        var testName = NewName("wrong_password_test");
        ViewSettings? initialSettings = null;

        await RunWithAkavache<NewtonsoftSerializer>(
            testName,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>(testName);
                _ = builder.WithSecureSettingsStore<ViewSettings>(
                    CorrectPassword,
                    s => initialSettings = s,
                    testName);
            },
            async instance =>
            {
                try
                {
                    // Wait until the initial store is created.
                    await TestHelper.EventuallyAsync(() => initialSettings is not null).ConfigureAwait(false);

                    // Release the store the builder created, and its file handles, before opening a
                    // second one on the same file. Two live connections to one encrypted database
                    // contend on the schema write, which surfaces as SQLITE_BUSY.
                    initialSettings?.Dispose();
                    await instance.DisposeSettingsStore<ViewSettings>(testName);

                    // IMPORTANT: Do NOT write using the captured 'initialSettings'.
                    // Instead, open a *fresh* store, perform the write, and dispose it.
                    await WriteSecretAsync(instance, testName).ConfigureAwait(false);

                    var wrongPasswordWorked =
                        await SecretReadableWithWrongPasswordAsync(instance, testName).ConfigureAwait(false);

                    await Assert.That(wrongPasswordWorked)
                        .IsFalse()
                        .Because("Wrong password should not provide access to encrypted data.");
                }
                finally
                {
                    await DeleteStoreQuietlyAsync(instance, testName).ConfigureAwait(false);
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies we can dispose and recreate multiple times.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestMultipleDisposeAndRecreateAsync()
    {
        var testName = NewName("multi_dispose_test");

        await RunWithAkavache<NewtonsoftSerializer>(
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
                    for (var round = 0; round < RecreateRoundCount; round++)
                    {
                        await WriteRoundValueAsync(instance, testName, round).ConfigureAwait(false);
                        await ReopenAndVerifyAsync(instance, testName).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await DeleteStoreQuietlyAsync(instance, testName).ConfigureAwait(false);
                }
            });

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);
    }

    /// <summary>Verifies AppInfo properties are present.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TestAppInfoPropertiesAsync()
    {
        IAkavacheInstance? akavacheInstance = null;

        _ = _appBuilder
            .WithAkavache<SystemJsonSerializer>(
                applicationName: "Akavache",
                builder =>
                {
                    _ = builder
                        .WithApplicationName("TestAppInfo")
                        .WithEncryptedSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);
                },
                instance => akavacheInstance = instance)
            .Build();

        await TestHelper.EventuallyAsync(static () => AppBuilder.HasBeenBuilt).ConfigureAwait(false);

        await Assert.That(akavacheInstance).IsNotNull();
#pragma warning disable CS0618 // Type or member is obsolete — deliberately asserts the legacy surface.
        await Assert.That(akavacheInstance!.ExecutingAssembly).IsNotNull();
#pragma warning restore CS0618
        await Assert.That(akavacheInstance!.ApplicationRootPath).IsNotNull();
        await Assert.That(akavacheInstance.SettingsCachePath).IsNotNull();

        // ExecutingAssemblyName and Version are no longer populated by the
        // constructor — they default to null unless the caller opts into the
        // AOT-safe WithExecutingAssembly path. This test exercises the default
        // path and therefore expects null.
#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(akavacheInstance.ExecutingAssemblyName).IsNull();
        await Assert.That(akavacheInstance.Version).IsNull();
#pragma warning restore CS0618
    }

    /// <summary>Creates a unique, human-readable test name prefix plus a GUID segment.</summary>
    /// <param name="prefix">A short, descriptive prefix for the test resource name.</param>
    /// <returns>A unique name string suitable for use as an application name or store key.</returns>
    private static string NewName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    /// <summary>
    /// Writes the modified settings through a freshly-opened store, retrying while a
    /// previously-opened store is still tearing down.
    /// </summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <returns>A task that completes once the write is observed.</returns>
    private static Task WriteModifiedSettingsAsync(IAkavacheInstance instance, string testName) =>
        TestHelper.EventuallyAsync(() => TestHelper.WithFreshStoreAsync(
            instance,
            () => instance.GetSecureSettingsStore<ViewSettings>(PersistencePassword, testName),
            async s =>
            {
                s.StringTest.Set("Modified String").WaitForCompletion();
                s.IntTest.Set(ModifiedIntSetting).WaitForCompletion();
                s.BoolTest.Set(false).WaitForCompletion();

                var ok = TestHelper.TryRead(() =>
                    s.StringTest is not null && s.IntTest == ModifiedIntSetting && !s.BoolTest);
                await Task.Yield();
                return ok;
            }));

    /// <summary>Writes one recreate round's value through a freshly-opened store.</summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <param name="round">The zero-based recreate round, which scales the written value.</param>
    /// <returns>A task that completes once the write is observed.</returns>
    private static Task WriteRoundValueAsync(IAkavacheInstance instance, string testName, int round) =>
        TestHelper.EventuallyAsync(() => TestHelper.WithFreshStoreAsync(
            instance,
            () => instance.GetSecureSettingsStore<ViewSettings>(PersistencePassword, testName),
            async s =>
            {
                s.IntTest.Set(round * RecreateValueStride).WaitForCompletion();
                var ok = TestHelper.TryRead(() => s.IntTest >= 0);
                await Task.Yield();
                return ok;
            }));

    /// <summary>Reopens the encrypted store and polls until its persisted values read back sanely.</summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <returns>A task that completes once a reopened store yields readable values.</returns>
    private static Task ReopenAndVerifyAsync(IAkavacheInstance instance, string testName) =>
        TestHelper.EventuallyAsync(async () =>
        {
            try
            {
                var reopened = instance.GetSecureSettingsStore<ViewSettings>(PersistencePassword, testName);
                var ok = reopened is not null
                         && TestHelper.TryRead(() => reopened.IntTest >= 0 && reopened.StringTest is not null);
                reopened?.Dispose();

                return ok;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException ex) when (TestHelper.IsDisposedMessage(ex))
            {
                return false;
            }
        });

    /// <summary>Writes <see cref="SecretPayload"/> through a freshly-opened store using the correct password.</summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <returns>A task that completes once the secret round-trips in the fresh store.</returns>
    private static Task WriteSecretAsync(IAkavacheInstance instance, string testName) =>
        TestHelper.EventuallyAsync(() => TestHelper.WithFreshStoreAsync(
            instance,
            () => instance.GetSecureSettingsStore<ViewSettings>(CorrectPassword, testName),
            async s =>
            {
                s.StringTest.Set(SecretPayload).WaitForCompletion();

                // Verify the value round-trips in the same fresh store.
                var ok = TestHelper.TryRead(() => s.StringTest == SecretPayload);
                await Task.Yield();
                return ok;
            }));

    /// <summary>
    /// Opens the store with the wrong password and reports whether the secret was readable.
    /// The native SQLite backend (sqlite3mc) validates the key at connection time and throws
    /// <see cref="AkavacheSqliteException"/> for a bad key; that exception is a valid
    /// "secret protected" outcome.
    /// </summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <returns><see langword="true"/> if the secret leaked; otherwise <see langword="false"/>.</returns>
    private static async Task<bool> SecretReadableWithWrongPasswordAsync(
        IAkavacheInstance instance,
        string testName)
    {
        var leaked = false;

        await TestHelper.EventuallyAsync(async () =>
        {
            try
            {
                var wrong = instance.GetSecureSettingsStore<ViewSettings>(IncorrectPassword, testName);
                if (wrong is null)
                {
                    return true; // acceptable outcome
                }

                if (TestHelper.TryRead(() => wrong.StringTest == SecretPayload))
                {
                    leaked = true;
                }

                wrong.Dispose();
                return true;
            }
            catch (AkavacheSqliteException)
            {
                // Wrong-key surface from the SQLite backend — the secret is protected.
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException ex) when (TestHelper.IsDisposedMessage(ex))
            {
                return false;
            }
        }).ConfigureAwait(false);

        return leaked;
    }

    /// <summary>Deletes the settings store, swallowing teardown races that are not part of the assertion.</summary>
    /// <param name="instance">The Akavache instance owning the store.</param>
    /// <param name="testName">The store name scoped to the running test.</param>
    /// <returns>A task that completes when the delete attempt finishes.</returns>
    private static async Task DeleteStoreQuietlyAsync(IAkavacheInstance instance, string testName)
    {
        try
        {
            await instance.DeleteSettingsStore<ViewSettings>(testName);
        }
        catch (Exception ex)
        {
            // Ignore cleanup issues.
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Creates, configures and builds an Akavache instance using the per-test path and encrypted SQLite provider, then executes the test body.
    /// This version blocks on async delegates to avoid async-void and ensure assertion scopes close before the test ends.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type to use (e.g., <see cref="NewtonsoftSerializer"/> or <see cref="SystemJsonSerializer"/>).</typeparam>
    /// <param name="applicationName">Application name to scope the store; may be <see langword="null"/>.</param>
    /// <param name="configureAsync">An async configuration callback to register stores and/or delete existing stores before the body runs.</param>
    /// <param name="bodyAsync">The asynchronous test body that uses the configured <see cref="IAkavacheInstance"/>.</param>
    /// <returns>A task that completes when the configure callback and the body have both run.</returns>
    private async Task RunWithAkavache<TSerializer>(
        string? applicationName,
        Func<IAkavacheBuilder, Task> configureAsync,
        Func<IAkavacheInstance, Task> bodyAsync)
        where TSerializer : class, ISerializer, new()
    {
        var configured = await _appBuilder
            .WithAkavacheAsync<TSerializer>(
                applicationName!,
                async builder =>
                {
                    // base config
                    _ = builder
                        .WithEncryptedSqliteProvider()
                        .WithSettingsCachePath(_cacheRoot);

                    await configureAsync(builder).ConfigureAwait(false);
                },
                bodyAsync)
            .ConfigureAwait(false);

        _ = configured.Build();
    }
}
