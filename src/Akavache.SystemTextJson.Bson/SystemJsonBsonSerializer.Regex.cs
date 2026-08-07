// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

#if REACTIVE_SHIM
namespace Akavache.Reactive.SystemTextJson;
#else
namespace Akavache.SystemTextJson;
#endif

/// <summary>
/// Supplies the <c>"Date":(\d{15,})</c> regex that recognises tick-based BSON date fields.
/// net7+ gets it from the <c>[GeneratedRegex]</c> source generator; older targets, which have
/// no generator, fall back to a compiled instance built once at type initialisation. Both
/// branches match exactly the same pattern.
/// </summary>
public partial class SystemJsonBsonSerializer
{
#if NET7_0_OR_GREATER
    /// <summary>Gets a regular expression matching tick-based BSON date fields.</summary>
    /// <returns>A regular expression matching tick-based date representations.</returns>
    [GeneratedRegex("""
                    "Date":(\d{15,})
                    """)]
    private static partial Regex GetDateRegex();
#else
    /// <summary>Compiled fallback regex matching tick-based BSON date fields.</summary>
    private static readonly Regex DateRegex = new(
        """
        "Date":(\d{15,})
        """,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets a regular expression matching tick-based BSON date fields.</summary>
    /// <returns>A regular expression matching tick-based date representations.</returns>
    private static Regex GetDateRegex() => DateRegex;
#endif
}
