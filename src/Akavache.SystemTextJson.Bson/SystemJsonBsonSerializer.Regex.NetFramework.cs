// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET7_0_OR_GREATER
using System.Text.RegularExpressions;

namespace Akavache.SystemTextJson;

/// <summary>
/// Compiled fallback regex for <see cref="SystemJsonBsonSerializer"/> on target
/// frameworks that do not support the <c>[GeneratedRegex]</c> source generator
/// (net462/net472/net481/netstandard2.0). Matches the same tick-based BSON date
/// representation as the source-generated variant used on net7+.
/// </summary>
public partial class SystemJsonBsonSerializer
{
    /// <summary>Compiled fallback regex matching tick-based BSON date fields.</summary>
    private static readonly Regex _dateRegex = new(
        """
        "Date":(\d{15,})
        """,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Returns the compiled BSON date regex.</summary>
    /// <returns>The compiled regex.</returns>
    private static Regex GetDateRegex() => _dateRegex;
}
#endif
