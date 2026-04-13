// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
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
        string[] pathTraversalAttempts =
        [
            "test/../other",
            @"test\..\other",
            "../../etc/passwd",
            @"..\..\windows\system32",
            "./test",
            ".\\test",
            "test/subdir",
            "test\\subdir"
        ];

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
        string[] reservedNames =
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            "con", "prn", "aux", "nul" // Test case insensitive
        ];

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
        foreach (var invalidChar in invalidChars.Where(c => c is not '/' and not '\\').Take(5))
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
        string[] problematicNames =
        [
            ".hiddenfile",
            "normalfile.",
            "spacefile ",  // trailing space
            "..."
        ];

        foreach (var problematicName in problematicNames)
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(problematicName));
            await Assert.That(ex.Message).Contains("cannot start or end with");
        }

        // Test names that become empty after trimming
        foreach (var emptyName in new[] { "   ", " " })
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
        string[] validNames =
        [
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
        ];

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
        string[] pathTraversalAttempts =
        [
            "MyApp/../OtherApp",
            "App/SubApp", // Should reject paths with subdirectories
            "App\\SubApp"
        ];

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
        string[] validNames =
        [
            "MyApplication",
            "App123",
            "app_name",
            "app-name",
            "SimpleApp",
            "AkavacheTestApp"
        ];

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
        var basePath = Path.Combine(Path.GetTempPath(), "cache");
        string[] maliciousPaths =
        [
            "../../../etc/passwd",
            "subdir/../../../etc",
            "normal/../../etc/passwd"
        ];

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
        string[] safePaths =
        [
            "cache.db",
            "subdir",
            Path.Combine("subdir", "cache.db")
        ];

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
    public Task SecurityUtilities_ShouldHandleNullAndEmptyInputs()
    {
        try
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
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <summary>
    /// Tests that validation handles whitespace strictly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SecurityUtilities_ShouldRejectLeadingTrailingWhitespace()
    {
        // For security reasons, we don't want to allow trailing whitespace
        foreach (var nameWithWhitespace in new[] { "ValidCache  " })
        {
            var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(nameWithWhitespace));
            await Assert.That(ex.Message).Contains("cannot start or end with");
        }

        // But names without leading/trailing whitespace should work
        const string validName = "ValidCache";
        var result = SecurityUtilities.ValidateCacheName(validName);
        await Assert.That(result).IsEqualTo(validName);
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// application names that start with a dot character.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectLeadingDot()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(".HiddenApp"));
        await Assert.That(ex.Message).Contains("cannot start or end with");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// application names that end with a dot character.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectTrailingDot()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName("MyApp."));
        await Assert.That(ex.Message).Contains("cannot start or end with");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// application names that end with a trailing space character.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectTrailingSpace()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName("MyApp "));
        await Assert.That(ex.Message).Contains("cannot start or end with");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// application names composed solely of dots (e.g. "..." which is also a path
    /// traversal indicator) and reports the appropriate error message.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectAllDots()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName("..."));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// application names containing invalid path characters such as the NUL byte.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectInvalidPathCharacters()
    {
        const string nameWithNul = "MyApp\0Name";
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(nameWithNul));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> rejects
    /// the explicit parent directory traversal token.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldRejectParentDirectoryToken()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName("my..app"));
        await Assert.That(ex.Message).Contains("path traversal");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> trims
    /// leading whitespace and returns the normalized name when otherwise valid.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldTrimLeadingWhitespace()
    {
        var result = SecurityUtilities.ValidateApplicationName("  MyApp");
        await Assert.That(result).IsEqualTo("MyApp");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.SafePathCombine"/> allows a combined
    /// path that resolves exactly to the base directory.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SafePathCombine_ShouldAllowPathEqualToBase()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "akavache-safe-combine");
        Directory.CreateDirectory(basePath);
        try
        {
            var result = SecurityUtilities.SafePathCombine(basePath, ".");
            var normalizedBase = Path.GetFullPath(basePath);
            await Assert.That(result).IsEqualTo(normalizedBase);
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
        }
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.SafePathCombine"/> throws when the
    /// base path is whitespace only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SafePathCombine_ShouldRejectWhitespaceBasePath()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine("   ", "file.db"));
        await Assert.That(ex.Message).Contains("Base path");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.SafePathCombine"/> throws when the
    /// relative path is whitespace only.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task SafePathCombine_ShouldRejectWhitespaceRelativePath()
    {
        var basePath = Path.GetTempPath();
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.SafePathCombine(basePath, "   "));
        await Assert.That(ex.Message).Contains("Relative path");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateDatabaseName"/> rejects
    /// whitespace-only inputs with an appropriate error message.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDatabaseName_ShouldRejectWhitespace()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName("   "));
        await Assert.That(ex.Message).Contains("Database name");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateDatabaseName"/> rejects
    /// reserved system names by delegating to cache name validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateDatabaseName_ShouldRejectReservedName()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateDatabaseName("CON"));
        await Assert.That(ex.Message).Contains("reserved system name");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateCacheName"/> uses the supplied
    /// parameter name in the thrown <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldUseCustomParameterName()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateCacheName(null!, "customParam"));
        await Assert.That(ex.ParamName).IsEqualTo("customParam");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateApplicationName"/> uses the
    /// supplied parameter name in the thrown <see cref="ArgumentException"/>.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateApplicationName_ShouldUseCustomParameterName()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateApplicationName(".bad", "appParam"));
        await Assert.That(ex.ParamName).IsEqualTo("appParam");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateCacheName"/> accepts a cache
    /// name at a reasonably large length (boundary-style check for long valid names).
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateCacheName_ShouldAcceptLongValidName()
    {
        string longName = new('a', 200);
        var result = SecurityUtilities.ValidateCacheName(longName);
        await Assert.That(result).IsEqualTo(longName);
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateNoNullOrTraversal"/> trims
    /// leading whitespace from an otherwise valid name. Trailing whitespace is
    /// rejected outright by the prefix/suffix check, so this only covers the
    /// leading-whitespace path.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateNoNullOrTraversal_ShouldTrimLeadingWhitespace()
    {
        var result = SecurityUtilities.ValidateNoNullOrTraversal("  ok", "param", "Cache name");
        await Assert.That(result).IsEqualTo("ok");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateNoNullOrTraversal"/> throws
    /// when the supplied value is <see langword="null"/>, empty, or whitespace-only,
    /// and that the exception message uses the supplied label.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ValidateNoNullOrTraversal_ShouldThrowOnNullOrWhitespace(string? value)
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateNoNullOrTraversal(value!, "param", "Cache name"));
        await Assert.That(ex.Message).Contains("Cache name cannot be null or empty.");
        await Assert.That(ex.ParamName).IsEqualTo("param");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateNoNullOrTraversal"/> rejects
    /// names that start with a dot, end with a dot, or end with a space.
    /// </summary>
    /// <param name="value">The candidate value containing the disallowed prefix/suffix.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments(".leading")]
    [Arguments("trailing.")]
    [Arguments("trailingspace ")]
    public async Task ValidateNoNullOrTraversal_ShouldRejectDotOrSpacePrefixOrSuffix(string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateNoNullOrTraversal(value, "param", "Cache name"));
        await Assert.That(ex.Message).Contains("cannot start or end with '.' or space characters");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateNoNullOrTraversal"/> rejects
    /// names containing a parent-directory traversal sequence, a forward slash, or a backslash.
    /// </summary>
    /// <param name="value">The candidate containing the traversal sequence.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments("foo..bar")]
    [Arguments("a/b")]
    [Arguments("a\\b")]
    public async Task ValidateNoNullOrTraversal_ShouldRejectPathTraversalSequences(string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateNoNullOrTraversal(value, "param", "Cache name"));
        await Assert.That(ex.Message).Contains("invalid path traversal characters");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.ValidateNoNullOrTraversal"/> includes
    /// the supplied label in the thrown exception messages so the same helper can be
    /// reused for cache, application, and database name validation.
    /// </summary>
    /// <returns>A task.</returns>
    [Test]
    public async Task ValidateNoNullOrTraversal_ShouldUseSuppliedLabelInExceptionMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => SecurityUtilities.ValidateNoNullOrTraversal(".bad", "param", "Application name"));
        await Assert.That(ex.Message).StartsWith("Application name '.bad'");
    }

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.IsReservedSystemName"/> recognises every
    /// Windows-reserved device name in upper case.
    /// </summary>
    /// <param name="reservedName">The reserved Windows device name.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments("CON")]
    [Arguments("PRN")]
    [Arguments("AUX")]
    [Arguments("NUL")]
    [Arguments("COM1")]
    [Arguments("COM9")]
    [Arguments("LPT1")]
    [Arguments("LPT9")]
    public async Task IsReservedSystemName_ShouldMatchEveryWindowsReservedName(string reservedName) =>
        await Assert.That(SecurityUtilities.IsReservedSystemName(reservedName)).IsTrue();

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.IsReservedSystemName"/> matches reserved
    /// names case-insensitively because they are normalised via
    /// <see cref="string.ToUpperInvariant"/>.
    /// </summary>
    /// <param name="reservedName">The reserved name in mixed/lowercase form.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments("con")]
    [Arguments("Prn")]
    [Arguments("com5")]
    [Arguments("LpT3")]
    public async Task IsReservedSystemName_ShouldMatchCaseInsensitively(string reservedName) =>
        await Assert.That(SecurityUtilities.IsReservedSystemName(reservedName)).IsTrue();

    /// <summary>
    /// Tests that <see cref="SecurityUtilities.IsReservedSystemName"/> returns
    /// <see langword="false"/> for ordinary names and for tokens that look like
    /// reserved names but fall outside the list (e.g. <c>COM10</c>, <c>LPT0</c>).
    /// </summary>
    /// <param name="ordinaryName">The candidate name.</param>
    /// <returns>A task.</returns>
    [Test]
    [Arguments("MyApp")]
    [Arguments("BlobCache")]
    [Arguments("COM10")]
    [Arguments("LPT0")]
    [Arguments("CON1")]
    public async Task IsReservedSystemName_ShouldReturnFalseForOrdinaryNames(string ordinaryName) =>
        await Assert.That(SecurityUtilities.IsReservedSystemName(ordinaryName)).IsFalse();
}
