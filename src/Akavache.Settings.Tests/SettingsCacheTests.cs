// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Splat.Builder;
using SQLitePCL;

namespace Akavache.Settings.Tests;

/// <summary>
/// Settings Cache Tests.
/// </summary>
public class SettingsCacheTests
{
    private readonly AppBuilder _appBuilder = AppBuilder.CreateSplatBuilder();

    /// <summary>
    /// Test1s this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous unit test.
    /// </returns>
    [Fact]
    public async Task TestCreateAndInsertNewtonsoft()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            async builder =>
            {
                await builder.DeleteSettingsStore<ViewSettings>();
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                await Task.Delay(500);
                Assert.NotNull(viewSettings);
                Assert.True(viewSettings!.BoolTest);
                Assert.Equal((short)16, viewSettings.ShortTest);
                Assert.Equal(1, viewSettings.IntTest);
                Assert.Equal(123456L, viewSettings.LongTest);
                Assert.Equal("TestString", viewSettings.StringTest);
                Assert.Equal(2.2f, viewSettings.FloatTest);
                Assert.Equal(23.8d, viewSettings.DoubleTest);
                Assert.Equal(EnumTestValue.Option1, viewSettings.EnumTest);
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
    [Fact]
    public async Task TestUpdateAndReadNewtonsoft()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder => builder.WithSettingsStore<ViewSettings>(s => viewSettings = s),
            async instance =>
            {
                viewSettings!.EnumTest = EnumTestValue.Option2;
                Assert.Equal(EnumTestValue.Option2, viewSettings.EnumTest);
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
    [Fact]
    public async Task TestCreateAndInsertSystemTextJson()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            async builder =>
        {
            await builder.DeleteSettingsStore<ViewSettings>();
            builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
        },
            async instance =>
            {
                Assert.NotNull(viewSettings);
                Assert.True(viewSettings!.BoolTest);
                Assert.Equal((short)16, viewSettings.ShortTest);
                Assert.Equal(1, viewSettings.IntTest);
                Assert.Equal(123456L, viewSettings.LongTest);
                Assert.Equal("TestString", viewSettings.StringTest);
                Assert.Equal(2.2f, viewSettings.FloatTest);
                Assert.Equal(23.8d, viewSettings.DoubleTest);
                Assert.Equal(EnumTestValue.Option1, viewSettings.EnumTest);
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
    [Fact]
    public async Task TestUpdateAndReadSystemTextJson()
    {
        var viewSettings = default(ViewSettings);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder =>
            {
                builder.WithSettingsStore<ViewSettings>(s => viewSettings = s);
            },
            async instance =>
            {
                viewSettings!.EnumTest = EnumTestValue.Option2;
                Assert.Equal(EnumTestValue.Option2, viewSettings.EnumTest);
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
    [Fact]
    public async Task TestOverrideSettingsCachePath()
    {
        const string path = "c:\\SettingsStoreage\\ApplicationSettings\\";
        var akavacheBuilder = default(IAkavacheInstance);
        GetBuilder().WithAkavache<NewtonsoftSerializer>(
            null,
            builder => builder.WithSettingsCachePath(path),
            instance => akavacheBuilder = instance).Build();

        while (!AppBuilder.HasBeenBuilt)
        {
            await Task.Delay(100);
        }

        Assert.Equal(path, akavacheBuilder!.SettingsCachePath);
    }

    private AppBuilder GetBuilder()
    {
        AppBuilder.ResetBuilderStateForTests();
        Batteries_V2.Init();
        return _appBuilder;
    }
}
