// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;

namespace Akavache.Tests;

/// <summary>
/// Tests for security utilities to prevent path traversal attacks.
/// </summary>
[Category("Security")]
public class SecurityUtilitiesTests
{
    /// <summary>
    /// Tests that ValidateCacheName rejects path traversal attempts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldRejectPathTraversalAttempts()
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
            await Assert.That(ex).IsNotNull();
            await Assert.That(ex.Message).Contains(maliciousName);
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects reserved system names.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldRejectReservedSystemNames()
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
            await Assert.That(ex.Message).Contains("reserved system name");
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects invalid filename characters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldRejectInvalidFilenameCharacters()
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        // Test first 5 to avoid excessive test time, but skip path separators since they're checked separately
        foreach (var invalidChar in invalidChars.Where(c => c != '/' && c != '\\').Take(5))
        {
            var nameWithInvalidChar = $"test{invalidChar}cache";
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(nameWithInvalidChar));
            await Assert.That(ex.Message).Contains("invalid filename characters");
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName rejects names with problematic prefixes/suffixes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldRejectProblematicPrefixesSuffixes()
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
            await Assert.That(ex.Message).Contains("cannot start or end with");
        }

        // Test names that become empty after trimming
        var emptyAfterTrim = new[] { "   ", " " };
        foreach (var emptyName in emptyAfterTrim)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(emptyName));
            await Assert.That(ex.Message).Contains("cannot be null or empty");
        }
    }

    /// <summary>
    /// Tests that ValidateCacheName accepts valid cache names.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldAcceptValidNames()
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
            await Assert.That(result).IsEqualTo(validName.Trim());
        }
    }

    /// <summary>
    /// Tests that ValidateApplicationName rejects path traversal attempts.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectPathTraversalAttempts()
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
            await Assert.That(ex).IsNotNull();
        }
    }

    /// <summary>
    /// Tests that ValidateApplicationName accepts valid application names.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldAcceptValidNames()
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
            await Assert.That(result).IsEqualTo(validName.Trim());
        }
    }

    /// <summary>
    /// Tests that ValidateDatabaseName works identically to ValidateCacheName.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ValidateDatabaseName_ShouldWorkLikeValidateCacheName()
    {
        // Test that it rejects the same things as ValidateCacheName
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName("./malicious"));
        await Assert.That(ex.Message).Contains("cannot");

        // Test that it accepts valid names
        var result = SecurityUtilities.ValidateDatabaseName("ValidDatabase");
        await Assert.That(result).IsEqualTo("ValidDatabase");
    }

    /// <summary>
    /// Tests that SafePathCombine prevents directory traversal.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SafePathCombine_ShouldPreventDirectoryTraversal()
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
            await Assert.That(ex.Message).Contains("outside the base directory");
        }
    }

    /// <summary>
    /// Tests that SafePathCombine allows safe relative paths.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SafePathCombine_ShouldAllowSafeRelativePaths()
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
            await Assert.That(result).StartsWith(basePath);
            await Assert.That(result).Contains(safePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    /// <summary>
    /// Tests error handling for null and empty inputs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SecurityUtilities_ShouldHandleNullAndEmptyInputs()
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SecurityUtilities_ShouldRejectLeadingTrailingWhitespace()
    {
        // For security reasons, we don't want to allow trailing whitespace
        var namesWithTrailingWhitespace = new[] { "ValidCache  " };

        foreach (var nameWithWhitespace in namesWithTrailingWhitespace)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(nameWithWhitespace));
            await Assert.That(ex.Message).Contains("cannot start or end with");
        }

        // But names without leading/trailing whitespace should work
        var validName = "ValidCache";
        var result = SecurityUtilities.ValidateCacheName(validName);
        await Assert.That(result).IsEqualTo(validName);
    }
}
