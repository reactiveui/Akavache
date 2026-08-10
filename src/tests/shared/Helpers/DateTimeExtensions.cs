// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Tests.Helpers;
#else
namespace Akavache.Tests.Helpers;
#endif

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
