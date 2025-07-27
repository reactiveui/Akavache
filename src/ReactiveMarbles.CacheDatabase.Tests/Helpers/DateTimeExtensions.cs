// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.CacheDatabase.Tests.Helpers;

/// <summary>
/// Extensions for DateTime handling in tests.
/// </summary>
internal static class DateTimeExtensions
{
    /// <summary>
    /// Extension method to truncate DateTime to seconds (remove milliseconds).
    /// </summary>
    /// <param name="dateTime">The DateTime to truncate.</param>
    /// <returns>DateTime truncated to seconds.</returns>
    public static DateTime TruncateToSecond(this in DateTime dateTime) =>
        new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
}
