// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET7_0_OR_GREATER
using System.Text.RegularExpressions;

namespace Akavache.Helpers;

/// <summary>
/// Compiled ISO 8601 regex fallback for <see cref="DateTimeHelpers"/> on target
/// frameworks that do not support the <c>[GeneratedRegex]</c> source generator
/// (net462/net472/net481/netstandard2.0). The static field is eagerly compiled
/// once and returned verbatim from the same <see cref="Iso8601Regex"/> method
/// shape exposed on net7+.
/// </summary>
internal static partial class DateTimeHelpers
{
    /// <summary>
    /// Compiled fallback regex matching ISO 8601 timestamps inside arbitrary payloads.
    /// </summary>
    private static readonly Regex _iso8601Regex = new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Returns the compiled ISO 8601 regex.</summary>
    /// <returns>The compiled regex.</returns>
    private static Regex Iso8601Regex() => _iso8601Regex;
}
#endif
