// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveMarbles.CacheDatabase.Settings.Tests;

namespace ReactiveMarbles.CacheDatabase.EncryptedSettings.Tests
{
    /// <summary>
    /// Settings Cache Tests.
    /// </summary>
    public class SettingsCacheTests
    {
        /// <summary>
        /// Test1s this instance.
        /// </summary>
        [Fact]
        public async void TestCreateAndInsert()
        {
            await AppInfo.DeleteSettingsStore<ViewSettings>();
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
        [Fact]
        public async void TestUpdateAndRead()
        {
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
}
