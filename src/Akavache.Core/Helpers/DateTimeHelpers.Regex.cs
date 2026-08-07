// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

#if REACTIVE_SHIM
namespace Akavache.Reactive.Helpers;
#else
namespace Akavache.Helpers;
#endif

/// <summary>
/// ISO 8601 regex partial for <see cref="DateTimeHelpers"/>. On net7+ the <c>[GeneratedRegex]</c>
/// source generator emits <see cref="Iso8601Regex"/> as a compiled, AOT-friendly state machine;
/// older targets, which have no source generator, compile an equivalent regex once into a static
/// field and hand it back from the same method shape.
/// </summary>
internal static partial class DateTimeHelpers
{
#if NET7_0_OR_GREATER
    /// <summary>Source-generated regex matching ISO 8601 timestamps inside arbitrary payloads.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")]
    private static partial Regex Iso8601Regex();
#else
    /// <summary>Compiled fallback regex matching ISO 8601 timestamps inside arbitrary payloads.</summary>
    private static readonly Regex _iso8601Regex = new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Returns the compiled ISO 8601 regex.</summary>
    /// <returns>The compiled regex.</returns>
    private static Regex Iso8601Regex() => _iso8601Regex;
#endif
}
