// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using Akavache.Tests;
using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Tests for the unencrypted settings cache, isolated per test to avoid static state leakage.
/// Uses eventually-consistent polling and treats transient disposal as retryable.
/// </summary>
[Category("Akavache")]
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
        _appBuilder = AppBuilder.CreateSplatBuilder();

        _cacheRoot = Path.Combine(
            Path.GetTempPath(),
            "AkavacheSettingsTests",
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
    /// Repro step 2: adds the settings-store plumbing (<c>WithSettingsStore&lt;T&gt;</c>) to
    /// the minimal repro — which is what the failing test path does next after
    /// <c>DeleteSettingsStore</c>. No <c>DeleteSettingsStore</c> in this variant.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReproWithSettingsStoreOnly()
    {
        var appName = NewName("repro_wss");
        ViewSettings? captured = null;

        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                appName,
                builder =>
                {
                    Func<Task> configure = async () =>
                    {
                        await Task.CompletedTask.ConfigureAwait(false);
                        builder
                            .WithSqliteProvider()
                            .WithSettingsCachePath(_cacheRoot)
                            .WithSettingsStore<ViewSettings>(s => captured = s);
                    };
                    configure().GetAwaiter().GetResult();
                },
                _ => { })
            .Build();

        await Assert.That(captured).IsNotNull();
        captured?.Dispose();
    }

    /// <summary>
    /// Regression test for the 1180 sqlite-net-pcl removal: reading a property off a
    /// freshly-constructed <see cref="SettingsBase"/>-derived class inside a
    /// sync-over-async configure callback used to deadlock (or natively crash) because
    /// the old <c>GetOrCreate&lt;T&gt;</c> getter called <c>observable.Wait()</c> against
    /// a storage backend whose own observable chain continued on the thread pool. The
    /// new <c>SettingsPropertyHelper&lt;T&gt;</c> model never blocks — property reads
    /// return the latest cached value synchronously and the cold load is opt-in via
    /// <c>SettingsStorage.InitializeAsync</c>. Uses the timeout-asserted
    /// <see cref="RunWithAkavacheAsync{TSerializer}"/> wrapper so any regression surfaces
    /// as a <see cref="TimeoutException"/> rather than a native crash.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ColdReadOfSettingsPropertyShouldNotDeadlock()
    {
        var appName = NewName("cold_read");
        ViewSettings? captured = null;

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            (builder, _) =>
            {
                builder.WithSettingsStore<ViewSettings>(s => captured = s, null, ImmediateScheduler.Instance);
                return Task.CompletedTask;
            },
            async (instance, _) =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => captured is not null).ConfigureAwait(false);
                    await Assert.That(captured).IsNotNull();

                    // Reading the observable property must complete without hanging. The
                    // initial value is the seeded default because the observable emits it
                    // immediately on subscribe while the cold load happens in the background.
                    var boolValue = (bool)captured!.BoolTest;
                    await Assert.That(boolValue).IsTrue();
                }
                finally
                {
                    captured?.Dispose();

                    await instance.DeleteSettingsStore<ViewSettings>();
                }
            },
            timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Regression variant that exercises the settings-store read path through the
    /// <see cref="RunWithAkavacheAsync{TSerializer}"/> wrapper — which runs the body
    /// under <c>Task.Run</c> + <c>WaitAsync(timeout)</c> so any deadlock surfaces as a
    /// timeout rather than a hung test process.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Repro_AsyncRunWithAkavacheWrapper_ReadProperty()
    {
        var appName = NewName("repro_async_runwith");
        ViewSettings? captured = null;

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            (builder, ct) =>
            {
                builder.WithSettingsStore<ViewSettings>(s => captured = s);
                return Task.CompletedTask;
            },
            async (instance, ct) =>
            {
                await TestHelper.EventuallyAsync(() => captured is not null).ConfigureAwait(false);
                var boolValue = (bool)captured!.BoolTest;
                await Assert.That(boolValue).IsTrue();
            },
            timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Regression test variant: configure lambda is ASYNC with an awaited completed task
    /// before <c>WithSettingsStore</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task Repro_AsyncConfigure_AwaitedCompletedTask()
    {
        var appName = NewName("repro_async_config");
        ViewSettings? captured = null;

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            async (builder, ct) =>
            {
                await Task.CompletedTask.ConfigureAwait(false);
                builder.WithSettingsStore<ViewSettings>(s => captured = s);
            },
            async (instance, ct) =>
            {
                await TestHelper.EventuallyAsync(() => captured is not null).ConfigureAwait(false);
                var boolValue = (bool)captured!.BoolTest;
                await Assert.That(boolValue).IsTrue();
            },
            timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Regression test for the 1180 sqlite-net-pcl removal — variant A: builds the cache
    /// and reads <c>BoolTest</c> but intentionally does NOT dispose <c>captured</c>. If
    /// this passes but the disposal variant crashes, the bug is in the disposal path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReproDeleteThenRead_NoDispose()
    {
        var appName = NewName("repro_no_dispose");
        ViewSettings? captured = null;

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            async (builder, ct) =>
            {
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => captured = s);
            },
            async (instance, ct) =>
            {
                await TestHelper.EventuallyAsync(() => captured is not null).ConfigureAwait(false);
                await Assert.That(captured).IsNotNull();
                var boolValue = (bool)captured!.BoolTest;
                await Assert.That(boolValue).IsTrue();
            },
            timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Regression test for the 1180 sqlite-net-pcl removal — variant B: same as A but with
    /// an explicit <c>captured.Dispose()</c> at the end.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReproDeleteThenRead_WithDispose()
    {
        var appName = NewName("repro_with_dispose");
        ViewSettings? captured = null;

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            async (builder, ct) =>
            {
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => captured = s);
            },
            async (instance, ct) =>
            {
                await TestHelper.EventuallyAsync(() => captured is not null).ConfigureAwait(false);
                await Assert.That(captured).IsNotNull();
                var boolValue = (bool)captured!.BoolTest;
                await Assert.That(boolValue).IsTrue();
                captured?.Dispose();
            },
            timeout: TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Minimal repro for the 1180 rewrite regression: exercises only the pieces that the
    /// failing <c>TestCreateAndInsertNewtonsoftAsync</c> does, one at a time, so we can
    /// isolate which step hangs / segfaults the test process. Does not depend on Akavache's
    /// settings store at all — just exercises <see cref="SqliteBlobCache"/> directly via
    /// <c>WithAkavache</c> + the ambient <c>CacheDatabase</c>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ReproSyncOverAsyncCacheConstruction()
    {
        var appName = NewName("repro");
        var dbPath = Path.Combine(_cacheRoot, "ReproCache.db");

        SqliteBlobCache? cache = null;
        _appBuilder
            .WithAkavache<NewtonsoftSerializer>(
                appName,
                builder =>
                {
                    // async-lambda + await-Task.CompletedTask mirrors
                    // TestCreateAndInsertNewtonsoftAsync's configure body, which does
                    // `await DeleteSettingsStore<T>()` — a no-op on a
                    // fresh filesystem that still forces the state machine.
                    Func<Task> configure = async () =>
                    {
                        await Task.CompletedTask.ConfigureAwait(false);
                        cache = new(dbPath, builder.Serializer!, ImmediateScheduler.Instance);
                    };
                    configure().GetAwaiter().GetResult();
                },
                _ => { })
            .Build();

        await Assert.That(cache).IsNotNull();

        try
        {
            cache!.Insert("k", [1, 2, 3]).WaitForCompletion();
            var data = cache.Get("k").WaitForValue();
            await Assert.That(data).IsNotNull();
            await Assert.That(data!).IsEquivalentTo(new byte[] { 1, 2, 3 });
        }
        finally
        {
            cache!.Dispose();
        }
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

        await RunWithAkavacheAsync<NewtonsoftSerializer>(
            appName,
            async (builder, _) =>
            {
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async (instance, _) =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That((bool)viewSettings!.BoolTest).IsTrue();
                    await Assert.That((short)viewSettings.ShortTest).IsEqualTo((short)16);
                    await Assert.That((int)viewSettings.IntTest).IsEqualTo(1);
                    await Assert.That((long)viewSettings.LongTest).IsEqualTo(123456L);
                    await Assert.That((string?)viewSettings.StringTest).IsEqualTo("TestString");
                    await Assert.That((float)viewSettings.FloatTest).IsEqualTo(2.2f).Within(0.0001f);
                    await Assert.That((double)viewSettings.DoubleTest).IsEqualTo(23.8d).Within(0.0001d);
                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option1);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>();
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            },
            timeout: TimeSpan.FromSeconds(30));

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
                    // Wait for the initially captured store to exist with longer timeout for CI
                    await TestHelper.EventuallyAsync(
                        () => viewSettings is not null,
                        timeoutMs: 10000,
                        initialDelayMs: 50).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();

                    // Wait a moment for the store to be fully initialized
                    await Task.Delay(100).ConfigureAwait(false);

                    // Perform the mutation directly on the captured store
                    viewSettings!.EnumTest.Set(EnumTestValue.Option2).SubscribeAndComplete();

                    // Wait for the value to be readable via the property helper
                    await TestHelper.EventuallyAsync(
                        () => viewSettings.EnumTest == EnumTestValue.Option2,
                        timeoutMs: 10000,
                        initialDelayMs: 50).ConfigureAwait(false);

                    // Final assertion
                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>();
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(
            () => AppBuilder.HasBeenBuilt,
            timeoutMs: 10000,
            initialDelayMs: 50).ConfigureAwait(false);
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
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                try
                {
                    await TestHelper.EventuallyAsync(() => viewSettings is not null).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();
                    await Assert.That((bool)viewSettings!.BoolTest).IsTrue();
                    await Assert.That((short)viewSettings.ShortTest).IsEqualTo((short)16);
                    await Assert.That((int)viewSettings.IntTest).IsEqualTo(1);
                    await Assert.That((long)viewSettings.LongTest).IsEqualTo(123456L);
                    await Assert.That((string?)viewSettings.StringTest).IsEqualTo("TestString");
                    await Assert.That((float)viewSettings.FloatTest).IsEqualTo(2.2f).Within(0.0001f);
                    await Assert.That((double)viewSettings.DoubleTest).IsEqualTo(23.8d).Within(0.0001d);
                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option1);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>();
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
                    // Wait for the initially captured store to exist with longer timeout for CI
                    await TestHelper.EventuallyAsync(
                        () => viewSettings is not null,
                        timeoutMs: 10000,
                        initialDelayMs: 50).ConfigureAwait(false);

                    await Assert.That(viewSettings).IsNotNull();

                    // Wait a moment for the store to be fully initialized
                    await Task.Delay(100).ConfigureAwait(false);

                    // Perform the mutation directly on the captured store
                    viewSettings!.EnumTest.Set(EnumTestValue.Option2).SubscribeAndComplete();

                    // Wait for the value to be readable via the property helper
                    await TestHelper.EventuallyAsync(
                        () => viewSettings.EnumTest == EnumTestValue.Option2,
                        timeoutMs: 10000,
                        initialDelayMs: 50).ConfigureAwait(false);

                    // Final assertion
                    await Assert.That((EnumTestValue)viewSettings.EnumTest).IsEqualTo(EnumTestValue.Option2);
                }
                finally
                {
                    try
                    {
                        viewSettings?.Dispose();

                        await instance.DeleteSettingsStore<ViewSettings>();
                    }
                    catch (Exception ex)
                    {
                        // Swallow cleanup issues.
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            });

        await TestHelper.EventuallyAsync(
            () => AppBuilder.HasBeenBuilt,
            timeoutMs: 10000,
            initialDelayMs: 50).ConfigureAwait(false);
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
                applicationName: "Akavache",
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
                applicationName: "Akavache", // Don't set via parameter
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
            .Because(
                "SettingsCachePath should contain the custom application name when WithApplicationName() is called before accessing the path");

        // Additional validation: ensure it doesn't contain the default name when a custom name is set
        await Assert.That(akavache.SettingsCachePath)
            .DoesNotContain("Akavache")
            .Because(
                "SettingsCachePath should not contain the default 'Akavache' directory when a custom application name is specified");
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
                applicationName: "Akavache", // No custom application name
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
            .Because(
                "SettingsCachePath should contain the default 'Akavache' directory when no custom application name is specified");
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
                applicationName!,
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
    /// Async-friendly variant of <see cref="RunWithAkavache{TSerializer}"/> that enforces a
    /// hard timeout via a <see cref="CancellationTokenSource"/>. If the configure or body
    /// lambda hangs (for instance because sqlite-net-pcl removal introduced a deadlock) the
    /// timeout fires, the wrapper throws <see cref="TimeoutException"/>, and the test fails
    /// cleanly instead of hanging the test host until it segfaults at shutdown.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type to use.</typeparam>
    /// <param name="applicationName">Application name to scope the store.</param>
    /// <param name="configureAsync">Async configure callback. Receives the shared CT so the
    /// callback body itself observes the timeout when awaiting downstream async work.</param>
    /// <param name="bodyAsync">Async test body. Also receives the shared CT.</param>
    /// <param name="timeout">Optional hard timeout. Defaults to 30 seconds.</param>
    /// <returns>A task that completes when both the configure and body lambdas complete.</returns>
    private async Task RunWithAkavacheAsync<TSerializer>(
        string? applicationName,
        Func<IAkavacheBuilder, CancellationToken, Task> configureAsync,
        Func<IAkavacheInstance, CancellationToken, Task> bodyAsync,
        TimeSpan? timeout = null)
        where TSerializer : class, ISerializer, new()
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(effectiveTimeout);

        var work = Task.Run(
            () =>
                _appBuilder
                    .WithAkavache<TSerializer>(
                        applicationName!,
                        builder =>
                        {
                            builder
                                .WithSqliteProvider()
                                .WithSettingsCachePath(_cacheRoot);
                            configureAsync(builder, cts.Token).GetAwaiter().GetResult();
                        },
                        instance => bodyAsync(instance, cts.Token).GetAwaiter().GetResult())
                    .Build(),
            cts.Token);

        try
        {
            await work.WaitAsync(effectiveTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException(
                $"RunWithAkavacheAsync timed out after {effectiveTimeout}. The configure or body callback hung — likely a sync-over-async deadlock in the SQLite stack.");
        }

        await Assert.That(cts.Token.IsCancellationRequested)
            .IsFalse()
            .Because("The timeout cancellation token fired before the test body completed.");
    }
}
