// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Helpers;

namespace Akavache.Core;

/// <summary>
/// Security utilities for path validation and sanitization to prevent path traversal attacks.
/// </summary>
internal static class SecurityUtilities
{
    /// <summary>Cached set of characters disallowed in filenames on the current platform.</summary>
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>Cached set of characters disallowed in paths on the current platform.</summary>
    private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// Validates that a cache name is safe to use in file paths and prevents path traversal attacks.
    /// </summary>
    /// <param name="cacheName">The cache name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated cache name.</returns>
    /// <exception cref="ArgumentException">Thrown when the cache name contains invalid characters or path traversal sequences.</exception>
    public static string ValidateCacheName(string cacheName, string parameterName = "cacheName")
    {
        var normalizedName = ValidateNoNullOrTraversal(cacheName, parameterName, "Cache name");

        ArgumentExceptionHelper.ThrowArgumentIf(
            normalizedName.IndexOfAny(_invalidFileNameChars) >= 0,
            $"Cache name '{cacheName}' contains invalid filename characters.",
            parameterName);

        ArgumentExceptionHelper.ThrowArgumentIf(
            IsReservedSystemName(normalizedName),
            $"Cache name '{cacheName}' is a reserved system name and cannot be used.",
            parameterName);

        return normalizedName;
    }

    /// <summary>
    /// Validates that an application name is safe to use in directory paths.
    /// </summary>
    /// <param name="applicationName">The application name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated application name.</returns>
    /// <exception cref="ArgumentException">Thrown when the application name contains invalid characters or path traversal sequences.</exception>
    public static string ValidateApplicationName(string applicationName, string parameterName = "applicationName")
    {
        var normalizedName = ValidateNoNullOrTraversal(applicationName, parameterName, "Application name");

        ArgumentExceptionHelper.ThrowArgumentIf(
            normalizedName.IndexOfAny(_invalidPathChars) >= 0,
            $"Application name '{applicationName}' contains invalid path characters.",
            parameterName);

        return normalizedName;
    }

    /// <summary>
    /// Validates that a database filename override is safe to use.
    /// </summary>
    /// <param name="databaseName">The database name to validate.</param>
    /// <param name="parameterName">The parameter name for error reporting.</param>
    /// <returns>The validated database name.</returns>
    /// <exception cref="ArgumentException">Thrown when the database name contains invalid characters or path traversal sequences.</exception>
    public static string ValidateDatabaseName(string databaseName, string parameterName = "databaseName")
    {
        ArgumentExceptionHelper.ThrowArgumentIf(
            string.IsNullOrWhiteSpace(databaseName),
            "Database name cannot be null or empty.",
            parameterName);

        // Use the same validation as cache names since they're used similarly
        return ValidateCacheName(databaseName, parameterName);
    }

    /// <summary>
    /// Safely combines paths ensuring the result stays within the base directory.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="relativePath">The relative path to combine.</param>
    /// <returns>The safely combined path.</returns>
    /// <exception cref="ArgumentException">Thrown when the resulting path would escape the base directory.</exception>
    public static string SafePathCombine(string basePath, string relativePath)
    {
        ArgumentExceptionHelper.ThrowArgumentIf(string.IsNullOrWhiteSpace(basePath), "Base path cannot be null or empty.", nameof(basePath));
        ArgumentExceptionHelper.ThrowArgumentIf(string.IsNullOrWhiteSpace(relativePath), "Relative path cannot be null or empty.", nameof(relativePath));

        var normalizedBasePath = Path.GetFullPath(basePath);
        var fullPath = Path.GetFullPath(Path.Combine(normalizedBasePath, relativePath));

        // Ensure the final path is still within the base directory
        ArgumentExceptionHelper.ThrowArgumentIf(
            !fullPath.StartsWith(normalizedBasePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fullPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase),
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
        ArgumentExceptionHelper.ThrowArgumentIf(
            string.IsNullOrWhiteSpace(value),
            $"{label} cannot be null or empty.",
            parameterName);

        // Check for problematic prefixes/suffixes BEFORE trimming.
        ArgumentExceptionHelper.ThrowArgumentIf(
            value.StartsWith(".") || value.EndsWith(".") || value.EndsWith(" "),
            $"{label} '{value}' cannot start or end with '.' or space characters.",
            parameterName);

        var normalized = value.Trim();

        ArgumentExceptionHelper.ThrowArgumentIf(
            normalized.Contains("..") || normalized.Contains("/") || normalized.Contains("\\"),
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
        return reservedNames.Contains(normalizedName.ToUpperInvariant());
    }
}
