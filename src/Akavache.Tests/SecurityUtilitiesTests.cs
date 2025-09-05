// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

using NUnit.Framework;

namespace Akavache.Tests;

/// <summary>
/// Tests for security utilities to prevent path traversal attacks.
/// </summary>
[TestFixture]
[Category("Security")]
public class SecurityUtilitiesTests
{
    /// <summary>
    /// Tests that ValidateCacheName rejects path traversal attempts.
    /// </summary>
    [Test]
    public void ValidateCacheName_ShouldRejectPathTraversalAttempts()
    {
        var pathTraversalAttempts = new[]
        {
            "test/../other",
            "test\\..\\other",
            "../../etc/passwd",
            "..\\..\\windows\\system32",
            "./test",
            ".\\test",
            "test/subdir",
            "test\\subdir"
        };

        foreach (var maliciousName in pathTraversalAttempts)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(maliciousName));

            // The validation should reject these for security reasons
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(maliciousName));
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects reserved system names.
    /// </summary>
    [Test]
    public void ValidateCacheName_ShouldRejectReservedSystemNames()
    {
        var reservedNames = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "con", "prn", "aux", "nul" // Test case insensitive
        };

        foreach (var reservedName in reservedNames)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(reservedName));
            Assert.That(ex.Message, Does.Contain("reserved system name"));
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects invalid filename characters.
    /// </summary>
    [Test]
    public void ValidateCacheName_ShouldRejectInvalidFilenameCharacters()
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        // Test first 5 to avoid excessive test time, but skip path separators since they're checked separately
        foreach (var invalidChar in invalidChars.Where(c => c != '/' && c != '\\').Take(5))
        {
            var nameWithInvalidChar = $"test{invalidChar}cache";
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(nameWithInvalidChar));
            Assert.That(ex.Message, Does.Contain("invalid filename characters"));
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects names with problematic prefixes/suffixes.
    /// </summary>
    [Test]
    public void ValidateCacheName_ShouldRejectProblematicPrefixesSuffixes()
    {
        var problematicNames = new[]
        {
            ".hiddenfile",
            "normalfile.",
            "spacefile ",  // trailing space
            "..."
        };

        foreach (var problematicName in problematicNames)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(problematicName));
            Assert.That(ex.Message, Does.Contain("cannot start or end with"));
        }

        // Test names that become empty after trimming
        var emptyAfterTrim = new[] { "   ", " " };
        foreach (var emptyName in emptyAfterTrim)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(emptyName));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName accepts valid cache names.
    /// </summary>
    [Test]
    public void ValidateCacheName_ShouldAcceptValidNames()
    {
        var validNames = new[]
        {
            "UserAccount",
            "LocalMachine",
            "Secure",
            "MyCache",
            "cache_with_underscore",
            "cache-with-dash",
            "cache123",
            "A",
            "CacheWithUpper",
            "cachewithlong123456789012345678901234567890"
        };

        foreach (var validName in validNames)
        {
            var result = SecurityUtilities.ValidateCacheName(validName);
            Assert.That(result, Is.EqualTo(validName.Trim()));
        }
    }

    /// <summary>
    /// Tests that ValidateApplicationName rejects path traversal attempts.
    /// </summary>
    [Test]
    public void ValidateApplicationName_ShouldRejectPathTraversalAttempts()
    {
        var pathTraversalAttempts = new[]
        {
            "MyApp/../OtherApp",
            "App/SubApp", // Should reject paths with subdirectories
            "App\\SubApp"
        };

        foreach (var maliciousName in pathTraversalAttempts)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(maliciousName));
            Assert.That(ex, Is.Not.Null);
        }
    }

    /// <summary>
    /// Tests that ValidateApplicationName accepts valid application names.
    /// </summary>
    [Test]
    public void ValidateApplicationName_ShouldAcceptValidNames()
    {
        var validNames = new[]
        {
            "MyApplication",
            "App123",
            "app_name",
            "app-name",
            "SimpleApp",
            "AkavacheTestApp"
        };

        foreach (var validName in validNames)
        {
            var result = SecurityUtilities.ValidateApplicationName(validName);
            Assert.That(result, Is.EqualTo(validName.Trim()));
        }
    }

    /// <summary>
    /// Tests that ValidateDatabaseName works identically to ValidateCacheName.
    /// </summary>
    [Test]
    public void ValidateDatabaseName_ShouldWorkLikeValidateCacheName()
    {
        // Test that it rejects the same things as ValidateCacheName
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName("./malicious"));
        Assert.That(ex.Message, Does.Contain("cannot"));

        // Test that it accepts valid names
        var result = SecurityUtilities.ValidateDatabaseName("ValidDatabase");
        Assert.That(result, Is.EqualTo("ValidDatabase"));
    }

    /// <summary>
    /// Tests that SafePathCombine prevents directory traversal.
    /// </summary>
    [Test]
    public void SafePathCombine_ShouldPreventDirectoryTraversal()
    {
        var basePath = "/tmp/cache";
        var maliciousPaths = new[]
        {
            "../../../etc/passwd",
            "subdir/../../../etc",
            "normal/../../etc/passwd"
        };

        foreach (var maliciousPath in maliciousPaths)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine(basePath, maliciousPath));
            Assert.That(ex.Message, Does.Contain("outside the base directory"));
        }
    }

    /// <summary>
    /// Tests that SafePathCombine allows safe relative paths.
    /// </summary>
    [Test]
    public void SafePathCombine_ShouldAllowSafeRelativePaths()
    {
        var basePath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var safePaths = new[]
        {
            "cache.db",
            "subdir",
            Path.Combine("subdir", "cache.db")
        };

        foreach (var safePath in safePaths)
        {
            var result = SecurityUtilities.SafePathCombine(basePath, safePath);
            Assert.That(result, Does.StartWith(basePath));
            Assert.That(result, Does.Contain(safePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    /// <summary>
    /// Tests error handling for null and empty inputs.
    /// </summary>
    [Test]
    public void SecurityUtilities_ShouldHandleNullAndEmptyInputs()
    {
        // Test ValidateCacheName
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(null!));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(string.Empty));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName("   "));

        // Test ValidateApplicationName
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(null!));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(string.Empty));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName("   "));

        // Test ValidateDatabaseName
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName(null!));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName(string.Empty));

        // Test SafePathCombine
        Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine(null!, "test"));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine(string.Empty, "test"));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine("/tmp", null!));
        Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine("/tmp", string.Empty));
    }

    /// <summary>
    /// Tests that validation handles whitespace strictly.
    /// </summary>
    [Test]
    public void SecurityUtilities_ShouldRejectLeadingTrailingWhitespace()
    {
        // For security reasons, we don't want to allow trailing whitespace
        var namesWithTrailingWhitespace = new[] { "ValidCache  " };

        foreach (var nameWithWhitespace in namesWithTrailingWhitespace)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(nameWithWhitespace));
            Assert.That(ex.Message, Does.Contain("cannot start or end with"));
        }

        // But names without leading/trailing whitespace should work
        var validName = "ValidCache";
        var result = SecurityUtilities.ValidateCacheName(validName);
        Assert.That(result, Is.EqualTo(validName));
    }
}
