// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace AkavacheV11Reader;

/// <summary>The outcome of reading one entry back out of the V10 database.</summary>
/// <param name="Key">The cache key that was read.</param>
/// <param name="TypeName">The type the entry was read as.</param>
/// <param name="Passed">Whether the value matched what the writer stored.</param>
/// <param name="Detail">What was actually found, when it did not match or the read threw.</param>
[System.Diagnostics.DebuggerDisplay("{Key}: {Passed}")]
internal readonly record struct VerificationResult(string Key, string TypeName, bool Passed, string? Detail)
{
    /// <summary>Renders the result as the single line this tool reports for each entry.</summary>
    /// <returns>The report line.</returns>
    public override string ToString() =>
        $"VERIFY key='{Key}' type={TypeName} => {(Passed ? "PASS" : $"FAIL ({Detail})")}";
}
