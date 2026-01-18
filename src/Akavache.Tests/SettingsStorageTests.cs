// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.Settings; // alphabetical Akavache.*
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for SettingsBase behavior with builder initialization.
/// </summary>
[Category("Settings")]
[Skip("Covered in Akavache.Settings.Tests; requires settings store initialization which is out of scope for this suite.")]
public class SettingsStorageTests
{
    /// <summary>
    /// Reinitialize cache database per test to isolate in-memory state.
    /// </summary>
    [Before(Test)]
    public void Setup()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Verifies that settings persist changes in memory via cache and can be modified.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SettingsBase_PersistsValues()
    {
        var settings = new AppSettings();
        await Assert.That(settings.UserName).IsEqualTo("Guest");
        settings.UserName = "Alice";
        await Assert.That(settings.UserName).IsEqualTo("Alice");
    }

    /// <summary>
    /// Verifies default values when not previously set.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SettingsBase_DefaultValues_WhenMissing()
    {
        var settings = new AppSettings();
        await Assert.That(settings.LaunchCount).IsZero();
    }

    /// <summary>
    /// Changing multiple values succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SettingsBase_MultipleChanges()
    {
        var settings = new AppSettings();
        settings.LaunchCount = 5;
        settings.UserName = "Bob";
        using (Assert.Multiple())
        {
            await Assert.That(settings.LaunchCount).IsEqualTo(5);
            await Assert.That(settings.UserName).IsEqualTo("Bob");
        }
    }

    /// <summary>
    /// Sample settings derived from SettingsBase for testing persistence in cache.
    /// </summary>
    private sealed class AppSettings : SettingsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings"/> class.
        /// </summary>
        public AppSettings()
            : base(nameof(AppSettings))
        {
        }

        /// <summary>
        /// Gets or sets the number of launches.
        /// </summary>
        public int LaunchCount
        {
            get => GetOrCreate(0);
            set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the current user name.
        /// </summary>
        public string UserName
        {
            get => GetOrCreate("Guest")!; // ensure non-null
            set => SetOrCreate(value);
        }
    }
}
