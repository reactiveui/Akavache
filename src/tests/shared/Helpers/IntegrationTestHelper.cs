// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Helpers;
#else
namespace Akavache.Tests.Helpers;
#endif

/// <summary>Tests to help with the integration tests.</summary>
public static class IntegrationTestHelper
{
    /// <summary>Length of a CRLF line terminator.</summary>
    private const int LineTerminatorLength = 2;

    /// <summary>Length of the ": " that separates an HTTP header name from its value.</summary>
    private const int HeaderSeparatorLength = 2;

    /// <summary>Gets the blank-line byte sequence that separates an HTTP header block from the body that follows it.</summary>
    private static ReadOnlySpan<byte> HeaderTerminator => "\r\n\r\n"u8;

    /// <summary>Gets a single path combined from other paths.</summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public static string GetPath(params string[] paths)
    {
        var ret = GetIntegrationTestRootDirectory();
        return new FileInfo(paths.Aggregate(ret, Path.Combine)).FullName;
    }

    /// <summary>Gets the root folder for the integration tests.</summary>
    /// <returns>The root folder.</returns>
    public static string GetIntegrationTestRootDirectory()
    {
        // XXX: This is an evil hack, but it's okay for a unit test
        // We can't use Assembly.Location because unit test runners love
        // to move stuff to temp directories
        StackFrame st = new(true);
        DirectoryInfo di = new(Path.Combine(Path.GetDirectoryName(st.GetFileName())!));

        return di.FullName;
    }

    /// <summary>Gets a response from a web service.</summary>
    /// <param name="paths">The paths for the web service.</param>
    /// <returns>The response from the server.</returns>
    public static HttpResponseMessage GetResponse(params string[] paths)
    {
        var bytes = File.ReadAllBytes(GetPath(paths));

        // Find the body
        var bodyIndex = bytes.AsSpan().IndexOf(HeaderTerminator);
        if (bodyIndex < 0)
        {
            throw new InvalidOperationException("Couldn't find response body");
        }

        var headerText = Encoding.UTF8.GetString(bytes, 0, bodyIndex);
        var lines = headerText.Split('\n');
        var statusCode = (HttpStatusCode)int.Parse(lines[0].Split(' ')[1], CultureInfo.InvariantCulture);
        HttpResponseMessage ret = new(statusCode) { Content = new ByteArrayContent(bytes, bodyIndex + LineTerminatorLength, bytes.Length - bodyIndex - LineTerminatorLength) };

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf(':');
            var key = line[..separatorIndex];
            var val = line[(separatorIndex + HeaderSeparatorLength)..].TrimEnd();

            _ = ret.Headers.TryAddWithoutValidation(key, val);
            _ = ret.Content.Headers.TryAddWithoutValidation(key, val);
        }

        return ret;
    }
}
