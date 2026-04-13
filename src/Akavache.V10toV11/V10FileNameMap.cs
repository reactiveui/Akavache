// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.V10toV11;

/// <summary>
/// Maps V11 cache names to the database filenames used by Akavache V10 and earlier.
/// </summary>
internal static class V10FileNameMap
{
    /// <summary>
    /// Gets the V10-era database filename for a given cache name.
    /// </summary>
    /// <param name="cacheName">The V11 cache name (e.g., "LocalMachine", "UserAccount", "Secure").</param>
    /// <returns>The corresponding V10 database filename.</returns>
    internal static string GetV10FileName(string cacheName) => cacheName switch
    {
        "LocalMachine" => "blobs.db",
        "UserAccount" => "userblobs.db",
        "Secure" => "secret.db",
        _ => $"{cacheName}.db",
    };
}
