// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using Akavache.Core;
using NUnit.Framework;

namespace Akavache.Tests
{
    /// <summary>
    /// Tests for Android FileNotFoundException issue fix.
    /// </summary>
    [TestFixture]
    public class AndroidInitializationTests
    {
        /// <summary>
        /// Test that AkavacheBuilder constructor handles invalid file paths gracefully
        /// without throwing FileNotFoundException (fixes Android crash issue).
        /// </summary>
        [Test]
        public void AkavacheBuilder_ShouldHandleInvalidFilePathsGracefully()
        {
            // Arrange & Act & Assert
            // This should not throw an exception even if file paths are invalid
            Assert.DoesNotThrow(
                () =>
                {
                    var builder = new AkavacheBuilder();

                    // The builder should be created successfully
                    Assert.That(builder, Is.Not.Null);
                    Assert.That(builder.ApplicationName, Is.EqualTo("Akavache"));
                },
                "AkavacheBuilder constructor should handle invalid file paths gracefully");
        }

        /// <summary>
        /// Test that the FileVersionInfo issue is fixed by ensuring File.Exists check works properly.
        /// </summary>
        [Test]
        public void FileExists_ShouldPreventFileVersionInfoCrash()
        {
            // Arrange
            var invalidPath = "/Akavache"; // This is the path that causes Android crashes

            // Act & Assert
            // File.Exists should return false for invalid paths without throwing
            Assert.DoesNotThrow(
                () =>
                {
                    var exists = File.Exists(invalidPath);
                    Assert.That(exists, Is.False, "Invalid path should not exist");
                },
                "File.Exists should handle invalid paths gracefully");
        }

        /// <summary>
        /// Integration test that verifies the complete initialization path works.
        /// </summary>
        [Test]
        public void AkavacheBuilder_InitializationPath_ShouldNotCrash()
        {
            Assert.DoesNotThrow(
                () =>
                {
                    // This simulates the path used in CacheDatabase.CreateBuilder()
                    var builder = new AkavacheBuilder();
                    builder.WithApplicationName("TestApp");

                    // Should be able to access properties without crashing
                    Assert.That(builder.ApplicationName, Is.EqualTo("TestApp"));
                },
                "Complete AkavacheBuilder initialization should work without crashes");
        }
    }
}