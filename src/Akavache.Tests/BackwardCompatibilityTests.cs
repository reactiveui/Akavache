// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.Sqlite3;
using Akavache.SystemTextJson;
using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for backward compatibility scenarios, especially for mobile platforms.
/// </summary>
[TestFixture]
[Category("Akavache")]
public class BackwardCompatibilityTests
{
    /// <summary>
    /// Verifies that WithSqliteDefaults() works automatically without requiring WithSqliteProvider() for backward compatibility.
    /// This test verifies the fix for the issue.
    /// </summary>
    [Test]
    public void WithSqliteDefaults_WithoutProvider_ShouldWorkAfterFix()
    {
        // This test verifies that the fix enables backward compatibility
        var testAppName = "BackwardCompatibilityFixTest";

        // Clear any previous state
        AkavacheBuilderTestExtensions.ResetSqliteProvider();

        try
        {
            // This call should now work without explicitly calling WithSqliteProvider() first
            Assert.DoesNotThrow(() =>
            {
                CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
                {
                    builder.WithApplicationName(testAppName);
                    builder.WithSqliteDefaults(); // This should now work
                });
            });
        }
        finally
        {
            // Clean up
            AkavacheBuilderTestExtensions.ResetSqliteProvider();
        }
    }

    /// <summary>
    /// Verifies that the new pattern with explicit WithSqliteProvider() works.
    /// </summary>
    [Test]
    public void WithSqliteProvider_ThenDefaults_ShouldWork()
    {
        var testAppName = "NewPatternTest";

        // Clear any previous state
        AkavacheBuilderTestExtensions.ResetSqliteProvider();

        try
        {
            // New pattern: explicit provider initialization
            Assert.DoesNotThrow(() =>
            {
                CacheDatabase.Initialize<SystemJsonSerializer>(builder =>
                {
                    builder.WithApplicationName(testAppName);
                    builder.WithSqliteProvider();
                    builder.WithSqliteDefaults();
                });
            });
        }
        finally
        {
            // Clean up
            AkavacheBuilderTestExtensions.ResetSqliteProvider();
        }
    }

    /// <summary>
    /// Test helper extension to expose internal state for testing.
    /// </summary>
    private static class AkavacheBuilderTestExtensions
    {
        /// <summary>
        /// Reset the SQLite provider state for testing purposes.
        /// </summary>
        public static void ResetSqliteProvider()
        {
            // We need to use reflection to reset the static field for testing
            var field = typeof(Akavache.Sqlite3.AkavacheBuilderExtensions)
                .GetField("_sqliteProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }
    }
}
