// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace Akavache.Tests;

/// <summary>
/// Tests to help with the integration tests.
/// </summary>
public static class IntegrationTestHelper
{
    /// <summary>
    /// Gets a single path combined from other paths.
    /// </summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public static string GetPath(params string[] paths)
    {
        var ret = GetIntegrationTestRootDirectory();
        return new FileInfo(paths.Aggregate(ret, Path.Combine)).FullName;
    }

    /// <summary>
    /// Gets the root folder for the integration tests.
    /// </summary>
    /// <returns>The root folder.</returns>
    public static string GetIntegrationTestRootDirectory()
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        var st = new StackFrame(true);
        var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(st.GetFileName())));

        return di.FullName;
    }

    /// <summary>
    /// Gets a response from a web service.
    /// </summary>
    /// <param name="paths">The paths for the web service.</param>
    /// <returns>The response from the server.</returns>
#if NETFRAMEWORK
    public static System.Net.Http.HttpResponseMessage GetResponse(params string[] paths)
#else
    public static HttpResponseMessage GetResponse(params string[] paths)
#endif
    {
        var bytes = File.ReadAllBytes(GetPath(paths));

        // Find the body
        int bodyIndex;
        for (bodyIndex = 0; bodyIndex < bytes.Length - 3; bodyIndex++)
        {
            if (bytes[bodyIndex] != 0x0D || bytes[bodyIndex + 1] != 0x0A ||
                bytes[bodyIndex + 2] != 0x0D || bytes[bodyIndex + 3] != 0x0A)
            {
                continue;
            }

            goto foundIt;
        }

        throw new InvalidOperationException("Couldn't find response body");

    foundIt:

        var headerText = Encoding.UTF8.GetString(bytes, 0, bodyIndex);
        var lines = headerText.Split('\n');
        var statusCode = (HttpStatusCode)int.Parse(lines[0].Split(' ')[1], CultureInfo.InvariantCulture);
#if NETFRAMEWORK
        var ret = new System.Net.Http.HttpResponseMessage(statusCode)
        {
            Content = new System.Net.Http.ByteArrayContent(bytes, bodyIndex + 2, bytes.Length - bodyIndex - 2)
        };
#else
        var ret = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent(bytes, bodyIndex + 2, bytes.Length - bodyIndex - 2)
        };
#endif

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf(":", StringComparison.InvariantCulture);
            var key = line[..separatorIndex];
            var val = line[(separatorIndex + 2)..].TrimEnd();

            ret.Headers.TryAddWithoutValidation(key, val);
            ret.Content.Headers.TryAddWithoutValidation(key, val);
        }

        return ret;
    }
}
