// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System; // System first
using Akavache.Settings; // alphabetical Akavache.*
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for SettingsBase behavior with builder initialization.
/// </summary>
[TestFixture]
[Category("Settings")]
[Ignore("Covered in Akavache.Settings.Tests; requires settings store initialization which is out of scope for this suite.")]
public class SettingsStorageTests
{
    /// <summary>
    /// Reinitialize cache database per test to isolate in-memory state.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Verifies that settings persist changes in memory via cache and can be modified.
    /// </summary>
    [Test]
    public void SettingsBase_PersistsValues()
    {
        var settings = new AppSettings();
        Assert.That(settings.UserName, Is.EqualTo("Guest"));
        settings.UserName = "Alice";
        Assert.That(settings.UserName, Is.EqualTo("Alice"));
    }

    /// <summary>
    /// Verifies default values when not previously set.
    /// </summary>
    [Test]
    public void SettingsBase_DefaultValues_WhenMissing()
    {
        var settings = new AppSettings();
        Assert.That(settings.LaunchCount, Is.Zero);
    }

    /// <summary>
    /// Changing multiple values succeeds.
    /// </summary>
    [Test]
    public void SettingsBase_MultipleChanges()
    {
        var settings = new AppSettings();
        settings.LaunchCount = 5;
        settings.UserName = "Bob";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings.LaunchCount, Is.EqualTo(5));
            Assert.That(settings.UserName, Is.EqualTo("Bob"));
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
