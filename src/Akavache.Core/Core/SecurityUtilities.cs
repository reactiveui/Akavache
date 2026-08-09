// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Core;
#else
namespace Akavache.Core;
#endif

/// <summary>Security utilities for path validation and sanitization to prevent path traversal attacks.</summary>
internal static class SecurityUtilities
{
    /// <summary>Cached set of characters disallowed in filenames on the current platform.</summary>
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>Cached set of characters disallowed in paths on the current platform.</summary>
    private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

    /// <summary>Characters a name may not end with, because Windows silently strips a trailing dot or space from a path component.</summary>
    private static readonly char[] _disallowedTrailingChars = ['.', ' '];

    /// <summary>Validates that a cache name is safe to use in file paths and prevents path traversal attacks.</summary>
    /// <param name="cacheName">The cache name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated cache name.</returns>
    /// <exception cref="ArgumentException">Thrown when the cache name contains invalid characters or path traversal sequences.</exception>
    internal static string ValidateCacheName(string cacheName, string parameterName = "cacheName")
    {
        var normalizedName = ValidateNoNullOrTraversal(cacheName, parameterName, "Cache name");

        ArgumentValidation.ThrowIf(
            normalizedName.IndexOfAny(_invalidFileNameChars) >= 0,
            $"Cache name '{cacheName}' contains invalid filename characters.",
            parameterName);

        ArgumentValidation.ThrowIf(
            IsReservedSystemName(normalizedName),
            $"Cache name '{cacheName}' is a reserved system name and cannot be used.",
            parameterName);

        return normalizedName;
    }

    /// <summary>Validates that an application name is safe to use in directory paths.</summary>
    /// <param name="applicationName">The application name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated application name.</returns>
    /// <exception cref="ArgumentException">Thrown when the application name contains invalid characters or path traversal sequences.</exception>
    internal static string ValidateApplicationName(string applicationName, string parameterName = "applicationName")
    {
        var normalizedName = ValidateNoNullOrTraversal(applicationName, parameterName, "Application name");

        ArgumentValidation.ThrowIf(
            normalizedName.IndexOfAny(_invalidPathChars) >= 0,
            $"Application name '{applicationName}' contains invalid path characters.",
            parameterName);

        return normalizedName;
    }

    /// <summary>Validates that a database filename override is safe to use.</summary>
    /// <param name="databaseName">The database name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated database name.</returns>
    /// <exception cref="ArgumentException">Thrown when the database name contains invalid characters or path traversal sequences.</exception>
    internal static string ValidateDatabaseName(string databaseName, string parameterName = "databaseName")
    {
        ArgumentValidation.ThrowIf(
            string.IsNullOrWhiteSpace(databaseName),
            "Database name cannot be null or empty.",
            parameterName);

        // Use the same validation as cache names since they're used similarly
        return ValidateCacheName(databaseName, parameterName);
    }

    /// <summary>Safely combines paths ensuring the result stays within the base directory.</summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="relativePath">The relative path to combine.</param>
    /// <returns>The safely combined path.</returns>
    /// <exception cref="ArgumentException">Thrown when the resulting path would escape the base directory.</exception>
    internal static string SafePathCombine(string basePath, string relativePath)
    {
        ArgumentValidation.ThrowIf(string.IsNullOrWhiteSpace(basePath), "Base path cannot be null or empty.", nameof(basePath));
        ArgumentValidation.ThrowIf(string.IsNullOrWhiteSpace(relativePath), "Relative path cannot be null or empty.", nameof(relativePath));

        var normalizedBasePath = Path.GetFullPath(basePath);
        var fullPath = Path.GetFullPath(Path.Combine(normalizedBasePath, relativePath));

        // Ensure the final path is still within the base directory
        ArgumentValidation.ThrowIf(
            !fullPath.StartsWith(normalizedBasePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase),
            $"The path '{relativePath}' would result in a location outside the base directory '{basePath}'.",
            nameof(relativePath));

        return fullPath;
    }

    /// <summary>
    /// Shared guard for the three checks every name validation in this class
    /// performs: not null/whitespace, no leading dot, no trailing dot or space,
    /// and no <c>..</c> / <c>/</c> / <c>\</c> path-traversal sequences.
    /// </summary>
    /// <param name="value">The raw value supplied by the caller.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <param name="label">Human-readable label used in exception messages (e.g. <c>"Cache name"</c>).</param>
    /// <returns>The trimmed value, ready for downstream validation.</returns>
    /// <exception cref="ArgumentException">Thrown when any of the shared rules are violated.</exception>
    internal static string ValidateNoNullOrTraversal(string value, string parameterName, string label)
    {
        ArgumentValidation.ThrowIf(
            string.IsNullOrWhiteSpace(value),
            $"{label} cannot be null or empty.",
            parameterName);

        // Check for problematic prefixes/suffixes BEFORE trimming. The guard above rules out
        // null and whitespace-only input, so index 0 always exists. A shorter result from
        // TrimEnd is exactly the "last character is a dot or a space" condition, and TrimEnd
        // hands back the original instance when there is nothing to trim, so the accepting
        // path allocates nothing.
        ArgumentValidation.ThrowIf(
            value[0] == '.' || value.TrimEnd(_disallowedTrailingChars).Length != value.Length,
            $"{label} '{value}' cannot start or end with '.' or space characters.",
            parameterName);

        var normalized = value.Trim();

        ArgumentValidation.ThrowIf(
            normalized.Contains("..") || ContainsChar(normalized, '/') || ContainsChar(normalized, '\\'),
            $"{label} '{value}' contains invalid path traversal characters. {label}s cannot contain '..' (parent directory), '/' or '\\' characters.",
            parameterName);

        return normalized;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="normalizedName"/> matches one of the
    /// Windows-reserved device names (<c>CON</c>, <c>PRN</c>, <c>AUX</c>, <c>NUL</c>,
    /// <c>COM1..9</c>, <c>LPT1..9</c>). The match is case-insensitive via
    /// <see cref="string.ToUpperInvariant"/>.
    /// </summary>
    /// <param name="normalizedName">The trimmed candidate name.</param>
    /// <returns><see langword="true"/> when the name is reserved on Windows.</returns>
    internal static bool IsReservedSystemName(string normalizedName)
    {
        string[] reservedNames =
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        ];
        return Array.IndexOf(reservedNames, normalizedName.ToUpperInvariant()) >= 0;
    }

    /// <summary>Whether <paramref name="value"/> contains <paramref name="candidate"/>.</summary>
    /// <remarks>
    /// net4x has no string.Contains(char), so the call there would bind to Enumerable.Contains
    /// and walk the string through an enumerator. Modern targets use the intrinsic directly.
    /// </remarks>
    /// <param name="value">The string to search.</param>
    /// <param name="candidate">The character to look for.</param>
    /// <returns><see langword="true"/> when the character is present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsChar(string value, char candidate) =>
#if NET
        value.Contains(candidate);
#else
        value.IndexOf(candidate) >= 0;
#endif
}
