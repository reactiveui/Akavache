// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET7_0_OR_GREATER
using System.Text.RegularExpressions;

namespace Akavache.SystemTextJson;

/// <summary>
/// Source-generated <c>"Date":(\d{15,})</c> regex partial for
/// <see cref="SystemJsonBsonSerializer"/> on net7+.
/// </summary>
public partial class SystemJsonBsonSerializer
{
    /// <summary>
    /// Creates a regular expression to identify JSON date fields containing a date
    /// represented as ticks in a specific format.
    /// </summary>
    /// <returns>A regular expression to match tick-based date representations.</returns>
    [GeneratedRegex("""
                    "Date":(\d{15,})
                    """)]
    private static partial Regex GetDateRegex();
}
#endif
