// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET7_0_OR_GREATER
using System.Text.RegularExpressions;

namespace Akavache.Helpers;

/// <summary>
/// Source-generated ISO 8601 regex partial for <see cref="DateTimeHelpers"/> on net7+.
/// The <c>[GeneratedRegex]</c> source generator emits the implementation of
/// <see cref="Iso8601Regex"/> as a compiled, AOT-friendly state machine.
/// </summary>
internal static partial class DateTimeHelpers
{
    /// <summary>Source-generated regex matching ISO 8601 timestamps inside arbitrary payloads.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")]
    private static partial Regex Iso8601Regex();
}
#endif
