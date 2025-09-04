// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Sqlite3;

using Splat.Builder;

namespace Akavache.Settings.Tests;

/// <summary>
/// Settings Cache Tests.
/// </summary>
[TestFixture]
[Category("Akavache")]
[Parallelizable(ParallelScope.None)]
public class SettingsCacheTests
{
    private readonly AppBuilder _appBuilder = AppBuilder.CreateSplatBuilder();

    /// <summary>
    /// Test1s this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Test]
    public async Task TestCreateAndInsertNewtonsoft()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            async builder =>
            {
                builder.WithSqliteProvider();
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                // Initial delay to ensure settings are created
                await Task.Delay(100);
                Assert.That(viewSettings, Is.Not.Null);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewSettings!.BoolTest, Is.True);
                    Assert.That(viewSettings.ShortTest, Is.EqualTo((short)16));
                    Assert.That(viewSettings.IntTest, Is.EqualTo(1));
                    Assert.That(viewSettings.LongTest, Is.EqualTo(123456L));
                    Assert.That(viewSettings.StringTest, Is.EqualTo("TestString"));
                    Assert.That(viewSettings.FloatTest, Is.EqualTo(2.2f));
                    Assert.That(viewSettings.DoubleTest, Is.EqualTo(23.8d));
                    Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option1));
                }

                await viewSettings.DisposeAsync();
            }).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Tests the update and read.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Test]
    public async Task TestUpdateAndReadNewtonsoft()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder => builder.WithSqliteProvider().WithSettingsStore<ViewSettings>(s => viewSettings = s),
            async instance =>
            {
                // Initial delay to ensure settings are created
                await Task.Delay(100);
                viewSettings!.EnumTest = EnumTestValue.Option2;
                Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option2));
                await viewSettings.DisposeAsync();
            }).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Test1s this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Test]
    public async Task TestCreateAndInsertSystemTextJson()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            async builder =>
        {
            builder.WithSqliteProvider();
            await builder.DeleteSettingsStore<ViewSettings>();
            builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
        },
            async instance =>
            {
                // Initial delay to ensure settings are created
                await Task.Delay(100);
                Assert.That(viewSettings, Is.Not.Null);
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(viewSettings!.BoolTest, Is.True);
                    Assert.That(viewSettings.ShortTest, Is.EqualTo((short)16));
                    Assert.That(viewSettings.IntTest, Is.EqualTo(1));
                    Assert.That(viewSettings.LongTest, Is.EqualTo(123456L));
                    Assert.That(viewSettings.StringTest, Is.EqualTo("TestString"));
                    Assert.That(viewSettings.FloatTest, Is.EqualTo(2.2f));
                    Assert.That(viewSettings.DoubleTest, Is.EqualTo(23.8d));
                    Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option1));
                }

                await viewSettings.DisposeAsync();
            }).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Tests the update and read.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Test]
    public async Task TestUpdateAndReadSystemTextJson()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder => builder.WithSqliteProvider().WithSettingsStore<ViewSettings>(s => viewSettings = s),
            async instance =>
            {
                // Initial delay to ensure settings are created
                await Task.Delay(100);
                viewSettings!.EnumTest = EnumTestValue.Option2;
                Assert.That(viewSettings.EnumTest, Is.EqualTo(EnumTestValue.Option2));
                await viewSettings.DisposeAsync();
            }).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Tests the override settings cache path.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Test]
    public async Task TestOverrideSettingsCachePath()
    {
        const string path = "c:\\SettingsStoreage\\ApplicationSettings\\";
        var akavacheBuilder = default(IAkavacheInstance);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder => builder.WithSqliteProvider().WithSettingsCachePath(path),
            instance => akavacheBuilder = instance).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }

        Assert.That(akavacheBuilder!.SettingsCachePath, Is.EqualTo(path));
    }

    private AppBuilder GetBuilder()
    {
        AppBuilder.ResetBuilderStateForTests();
        return _appBuilder;
    }
}
