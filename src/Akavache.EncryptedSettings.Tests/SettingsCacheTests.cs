// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.Settings.Tests;
using Akavache.SystemTextJson;

namespace Akavache.EncryptedSettings.Tests;

/// <summary>
/// Settings Cache Tests.
/// </summary>
public class SettingsCacheTests
{
    /// <summary>
    /// Test1s this instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestCreateAndInsertNewtonsoft()
    {
        await AppInfo.DeleteSettingsStore<ViewSettings>();
        AppInfo.Serializer = new NewtonsoftSerializer();
        var viewSettings = await AppInfo.SetupSettingsStore<ViewSettings>("test1234");

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
    }

    /// <summary>
    /// Tests the update and read.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestUpdateAndReadNewtonsoft()
    {
        AppInfo.Serializer = new NewtonsoftSerializer();
        var viewSettings = await AppInfo.SetupSettingsStore<ViewSettings>("test1234");
        viewSettings!.EnumTest = EnumTestValue.Option2;
        Assert.Equal(EnumTestValue.Option2, viewSettings.EnumTest);
        await viewSettings.DisposeAsync();
    }

    /// <summary>
    /// Test1s this instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestCreateAndInsertSystemTextJson()
    {
        await AppInfo.DeleteSettingsStore<ViewSettings>();
        AppInfo.Serializer = new SystemJsonSerializer();
        var viewSettings = await AppInfo.SetupSettingsStore<ViewSettings>("test1234");

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
    }

    /// <summary>
    /// Tests the update and read.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TestUpdateAndReadSystemTextJson()
    {
        AppInfo.Serializer = new SystemJsonSerializer();
        var viewSettings = await AppInfo.SetupSettingsStore<ViewSettings>("test1234");
        viewSettings!.EnumTest = EnumTestValue.Option2;
        Assert.Equal(EnumTestValue.Option2, viewSettings.EnumTest);
        await viewSettings.DisposeAsync();
    }

    /// <summary>
    /// Tests the override settings cache path.
    /// </summary>
    [Fact]
    public void TestOverrideSettingsCachePath()
    {
        const string path = "c:\\SettingsStoreage\\ApplicationSettings\\";
        AppInfo.OverrideSettingsCachePath(path);
        Assert.Equal(path, AppInfo.SettingsCachePath);
    }
}
