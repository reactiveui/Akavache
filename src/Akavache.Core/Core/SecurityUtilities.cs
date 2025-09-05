// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Core;

/// <summary>
/// Security utilities for path validation and sanitization to prevent path traversal attacks.
/// </summary>
internal static class SecurityUtilities
{
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();
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
        if (string.IsNullOrWhiteSpace(cacheName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", parameterName);
        }

        // Check for problematic prefixes/suffixes BEFORE trimming (but allow leading spaces in cache names for now)
        if (cacheName.StartsWith(".") || cacheName.EndsWith(".") || cacheName.EndsWith(" "))
        {
            throw new ArgumentException($"Cache name '{cacheName}' cannot start or end with '.' or space characters.", parameterName);
        }

        var normalizedName = cacheName.Trim();

        // Check for empty after trimming
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Cache name cannot be null or empty.", parameterName);
        }

        // Check for invalid filename characters first
        if (normalizedName.IndexOfAny(_invalidFileNameChars) >= 0)
        {
            throw new ArgumentException($"Cache name '{cacheName}' contains invalid filename characters.", parameterName);
        }

        // Check for path traversal sequences
        if (normalizedName.Contains("..") || normalizedName.Contains("/") || normalizedName.Contains("\\"))
        {
            throw new ArgumentException($"Cache name '{cacheName}' contains invalid path traversal characters. Cache names cannot contain '..' (parent directory), '/' or '\\' characters.", parameterName);
        }

        // Reject names that could be problematic on various file systems
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (reservedNames.Contains(normalizedName.ToUpperInvariant()))
        {
            throw new ArgumentException($"Cache name '{cacheName}' is a reserved system name and cannot be used.", parameterName);
        }

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
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", parameterName);
        }

        // Check for problematic prefixes/suffixes BEFORE trimming
        if (applicationName.StartsWith(".") || applicationName.EndsWith(".") || applicationName.EndsWith(" "))
        {
            throw new ArgumentException($"Application name '{applicationName}' cannot start or end with '.' or space characters.", parameterName);
        }

        var normalizedName = applicationName.Trim();

        // Check for empty after trimming
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Application name cannot be null or empty.", parameterName);
        }

        // Check for path traversal sequences
        if (normalizedName.Contains("..") || normalizedName.Contains("/") || normalizedName.Contains("\\"))
        {
            throw new ArgumentException($"Application name '{applicationName}' contains invalid path traversal characters. Application names cannot contain '..' (parent directory), '/' or '\\' characters.", parameterName);
        }

        // Check for invalid path characters (less restrictive than filename chars)
        if (normalizedName.IndexOfAny(_invalidPathChars) >= 0)
        {
            throw new ArgumentException($"Application name '{applicationName}' contains invalid path characters.", parameterName);
        }

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
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("Database name cannot be null or empty.", parameterName);
        }

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
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be null or empty.", nameof(relativePath));
        }

        // Normalize the base path
        var normalizedBasePath = Path.GetFullPath(basePath);

        // Combine the paths
        var combinedPath = Path.Combine(normalizedBasePath, relativePath);

        // Get the full path to resolve any relative components
        var fullPath = Path.GetFullPath(combinedPath);

        // Ensure the final path is still within the base directory
        if (!fullPath.StartsWith(normalizedBasePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The path '{relativePath}' would result in a location outside the base directory '{basePath}'.", nameof(relativePath));
        }

        return fullPath;
    }
}
