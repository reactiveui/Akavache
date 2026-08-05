// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests.Helpers;

/// <summary>Extensions for DateTime handling in tests.</summary>
internal static class DateTimeExtensions
{
    /// <summary>Extension members for <see cref="DateTime"/>.</summary>
    /// <param name="dateTime">The DateTime the members operate on.</param>
    extension(in DateTime dateTime)
    {
        /// <summary>Truncates the DateTime to whole seconds (removes milliseconds).</summary>
        /// <returns>DateTime truncated to seconds.</returns>
        internal DateTime TruncateToSecond() =>
            new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
    }
}
